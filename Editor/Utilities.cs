using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DeepInsp
{
	// Classes are not serializable, we using these classes in dictionary, which is not serialzable in unity anyway.
	/// <summary>
	/// Holds show/hide datas.
	/// </summary>
	public class ShowHideDatas
	{
		/// <param name="parent">Use Utilities.ShowHideData</param>
		/// <param name="type">Type of this data.</param>
		public ShowHideDatas(ShowHideDataTree parent, Type type)
		{
			this.type = type;
			this.parent = parent;

			string typeName = type.FullName;

			defaultCloseType = (typeName == "UnityEngine.ParticleSystem" || typeName == "UnityEngine.Video.VideoPlayer");
			if (defaultCloseType) showProperty = showObject = false;
			else showProperty = showObject = IsDefaultShow(type);
			alwaysShowObject = IsAlwaysShowType(type);
		}
		/// <summary>
		/// Is this type has a default unity draw?
		/// </summary>
		static bool IsAlwaysShowType(Type type)
		{
			if (type == null) return false;
			if (type.IsPrimitive) return true;
			if (type.IsEnum) return true;
			if (type.IsArray) return false;
			if (typeof(ICollection).IsAssignableFrom(type)
				|| typeof(IList).IsAssignableFrom(type)
				|| typeof(IDictionary).IsAssignableFrom(type)) return false;
			if (type == typeof(string) || type == typeof(StringBuilder)) return true;

			if (typeof(Component).IsAssignableFrom(type))
				return false;
			if (!string.IsNullOrEmpty(type.Namespace) &&
				(type.Namespace.Contains("UnityEngine")
				|| type.Namespace.Contains("UnityEditor")
				|| type.Namespace.Contains("UnityEngineInternal")
				|| type.Namespace.Contains("UnityEditorInternal")
				|| type.Namespace.Contains("UnityScript")))
			{
				string typeName = type.FullName;
				if (typeName == "UnityEngine.Matrix4x4" || typeName == "UnityEngine.SceneManagement.Scene" ||
					typeName == "UnityEditor.MonoScript" || typeName == "UnityEngine.TextAsset")
					return false;
				
				if (type.Name.Contains("GUI"))
					return false;
				return true;
			}
			return false;
		}
		/// <summary>
		/// Is this type can be show when first open.
		/// </summary>
		static bool IsDefaultShow(Type type)
		{
			if (type == null) return false;
			if (typeof(Component).IsAssignableFrom(type))
				return true;
			if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				if (!string.IsNullOrEmpty(type.Namespace))
				{
					if (type.Namespace.Contains("UnityEditor"))
						return false;
					else if (type.Namespace.Contains("UnityEngine"))
						return false;
				}

				return true;
			}
			return false;
		}

		/// <summary>
		/// Return type + hashCode.
		/// </summary>
		public override string ToString()
		{
			return type + ", " + GetHashCode();
		}

		Type type;
		/// <summary>
		/// Is not show by default. (Some component is not recommend to open by default, it generate errors.) 
		/// </summary>
		public bool defaultCloseType = false;

		bool showObject = true;
		/// <summary>
		/// Is currently show(as object) in inspector?
		/// </summary>
		public bool ShowObject
		{
			get { return alwaysShowObject || showObject; }
			set
			{
				if (showObject != value)
					showObject = value;
			}
		}
		/// <summary>
		/// Is currently show(as property) in inspector?
		/// </summary>
		public bool showProperty = true;
		/// <summary>
		/// Not be closable.
		/// </summary>
		public bool alwaysShowObject = false;
		/// <summary>
		/// Need to draw Method box?
		/// </summary>
		public bool showMethod = false;
		/// <summary>
		/// Need to draw Unity/System.Object method?
		/// </summary>
		public bool showBaseMethod = false;
		/// <summary>
		/// Owner of this data. (For draw log.)
		/// </summary>
		public ShowHideDataTree parent = null;

		/// <summary>
		/// Dictionary data for drawing "Add".
		/// </summary>
		public class DictionaryData
		{
			/// <param name="parentType">Type of dictionary</param>
			public DictionaryData(Type parentType)
			{
				if (parentType.IsGenericType)
				{
					var types = parentType.GetGenericArguments();
					if (types.Length == 2)
					{
						keyType = types[0];
						valueType = types[1];
					}
				}
				show = false;
			}
			bool show;
			/// <summary>
			/// Is currently show add box.
			/// </summary>
			public bool Show
			{
				get { return show && keyType != null && valueType != null; }
				set { show = value; }
			}
			/// <summary>
			/// Key type.
			/// </summary>
			public Type keyType;
			/// <summary>
			/// Key data.
			/// </summary>
			public object key;
			/// <summary>
			/// Value Type.
			/// </summary>
			public Type valueType;
			/// <summary>
			/// Value data.
			/// </summary>
			public object value;
		}
		/// <summary>
		/// Dictionary data for drawing "Add".
		/// </summary>
		public Dictionary<int, DictionaryData> dictAddFlags = new Dictionary<int, DictionaryData>();
		/// <summary>
		/// List data for drawing.
		/// </summary>
		public class LongListData
		{
			/// <param name="parent">From which show hide data</param>
			public LongListData(ShowHideDatas parent)
			{
				this.parent = parent;
				currentIndex = -1;
			}
			int currentIndex = -1;
			/// <summary>
			/// Is index foldered.
			/// </summary>
			public bool IsFoldered(int index)
			{
				return currentIndex != index;
			}
			/// <summary>
			/// Set currentIndex to index.
			/// </summary>
			public void ToggleFolder(int index)
			{
				currentIndex = currentIndex == index ? -1 : index;
			}
			/// <summary>
			/// return parent + hashCode.
			/// </summary>
			public override string ToString()
			{
				return parent.ToString() + ": " + typeof(LongListData).FullName + ", " + GetHashCode();
			}
			ShowHideDatas parent;
		}
		/// <summary>
		/// List data for drawing.
		/// </summary>
		public Dictionary<int, LongListData> longListDatas = new Dictionary<int, LongListData>();
	}

	/// <summary>
	/// Method info data for drawing.
	/// </summary>
	public class MethodData
	{
		/// <param name="info">Data from.</param>
		/// <param name="infos">All info from parent object.</param>
		public MethodData(MethodInfo info, MemberInfo[] infos)
		{
			method = info;
			parameters = method.GetParameters();
			if (parameters != null && parameters.Length > 0)
			{
				cachedParamterDatas = new object[parameters.Length];
				hasParameter = true;
			}
			else hasParameter = false;

			canDraw = !(method.IsGenericMethod
				|| method.IsGenericMethodDefinition
				|| method.ContainsGenericParameters
				|| method.IsAbstract
				|| IsProperty(method, infos)
				|| !IsParameterDrawable);
			isBaseType = IsBaseType(info);
		}

		/// <summary>
		/// Is method comes from unity engine or System.Object
		/// </summary>
		static bool IsBaseType(MethodInfo info)
		{
			var type = info.DeclaringType;
			if (type == typeof(object))
				return true;
			if (!string.IsNullOrEmpty(type.Namespace) &&
				(type.Namespace.Contains("UnityEngine")
				|| type.Namespace.Contains("UnityEditor")
				|| type.Namespace.Contains("UnityEngineInternal")
				|| type.Namespace.Contains("UnityEditorInternal")
				|| type.Namespace.Contains("UnityScript")))
				return true;
			return false;
		}

		/// <summary>
		/// Return true if methodInfo is a property method. (eg. get or set)
		/// </summary>
		static bool IsProperty(MethodInfo methodInfo, MemberInfo[] infos)
		{
			for (int c = infos.Length - 1; c >= 0; --c)
			{
				if (infos[c].MemberType == MemberTypes.Property)
				{
					var pro = infos[c] as PropertyInfo;
					if (pro.CanRead && pro.GetGetMethod(true) == methodInfo)
						return true;
					if (pro.CanWrite && pro.GetSetMethod(true) == methodInfo)
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Make sure no generic / interface or object parameter. Also not a abstract method.
		/// </summary>
		bool IsParameterDrawable
		{
			get
			{
				if (hasParameter)
				{
					for (int c = parameters.Length - 1; c >= 0; --c)
					{
						var type = parameters[c].ParameterType;
						if (type == typeof(string)) return true;
						if (type.IsAbstract || type.IsInterface || type.IsGenericType || type == typeof(object)) return false;
						if (type.IsClass)
						{
							var constructor = type.GetConstructor(Type.EmptyTypes);
							if (constructor == null) return false;
							if (!IsTypeDrawable(type)) return false;
						}
					}
				}
				return true;
			}
		}

		/// <summary>
		/// Check if type can be draw.
		/// </summary>
		bool IsTypeDrawable(Type type)
		{
			var infos = type.GetMembers(BindingFlags.Instance
								| BindingFlags.Public
								| BindingFlags.NonPublic
								| BindingFlags.GetProperty
								| BindingFlags.SetProperty
								| BindingFlags.GetField
								| BindingFlags.SetField);
			for (int c = infos.Length - 1; c >= 0; --c)
			{
				if (infos[c].MemberType == MemberTypes.Property)
				{
					var pro = infos[c] as PropertyInfo;
					return !pro.CanWrite;
				}
			}
			return true;
		}

		/// <summary>
		/// Cached info.
		/// </summary>
		public MethodInfo method = null;
		/// <summary>
		/// Cached parameter infos.
		/// </summary>
		public ParameterInfo[] parameters = null;
		/// <summary>
		/// Cached parameter datas.
		/// </summary>
		public object[] cachedParamterDatas = null;
		/// <summary>
		/// Is Data can draw in inspector.
		/// </summary>
		public bool canDraw;
		/// <summary>
		/// Is method has any parameter.
		/// </summary>
		public bool hasParameter;
		/// <summary>
		/// Is a method create by unity.
		/// </summary>
		public bool isBaseType;
	}

	/// <summary>
	/// Data of member for drawing.
	/// </summary>
	public class MemberData
	{
		/// <param name="info">PropertyInfo or FieldInfo.</param>
		public MemberData(MemberInfo info)
		{
			member = info;
			property = member.MemberType == MemberTypes.Property;
		}
		/// <summary>
		/// A PropertyInfo or FieldInfo
		/// </summary>
		public MemberInfo member;
		/// <summary>
		/// Is property?
		/// </summary>
		public bool property;
	}

	/// <summary>
	/// All member info data for drawing.
	/// </summary>
	public class DrawData
	{
		/// <param name="type">Data type.</param>
		/// <param name="isStatic">Is static data?</param>
		public DrawData(Type type, bool isStatic)
		{
			this.type = type;
			Type currentType = type;
			MemberInfo[] infos = null;
			List<MemberData> memberList = new List<MemberData>();
			List<MethodData> methodList = new List<MethodData>();
			do
			{
				if (isStatic)
				{
					infos = currentType.GetMembers(BindingFlags.Static
									| BindingFlags.Public
									| BindingFlags.NonPublic
									| BindingFlags.GetProperty
									| BindingFlags.SetProperty
									| BindingFlags.GetField
									| BindingFlags.SetField
									| BindingFlags.InvokeMethod);
				}
				else
				{
					infos = currentType.GetMembers(BindingFlags.Instance
									| BindingFlags.Public
									| BindingFlags.NonPublic
									| BindingFlags.GetProperty
									| BindingFlags.SetProperty
									| BindingFlags.GetField
									| BindingFlags.SetField
									| BindingFlags.InvokeMethod);
				}

				foreach (var info in infos)
				//for (int c = infos.Length - 1; c >= 0; --c)
				{
					//var info = infos[c];
					switch (info.MemberType)
					{
						case MemberTypes.Field:
						case MemberTypes.Property:
							memberList.Add(new MemberData(info));
							break;
						case MemberTypes.Method:
							methodList.Add(new MethodData(info as MethodInfo, infos));
							break;
					}
				}

				if (currentType.BaseType != typeof(System.Object) &&
					currentType.BaseType != typeof(UnityEngine.Object) &&
					currentType.BaseType != typeof(MonoBehaviour) &&
					currentType.BaseType != typeof(Behaviour) &&
					currentType.BaseType != typeof(Component))
					currentType = currentType.BaseType;
				else
					currentType = null;

			}
			while (currentType != null);
			memberInfos = memberList.ToArray();
			methodInfos = methodList.ToArray();
		}
		/// <summary>
		/// Which type is this object.
		/// </summary>
		public Type type;
		/// <summary>
		/// All property/field member in this object.
		/// </summary>
		public MemberData[] memberInfos = null;
		/// <summary>
		/// All method data in this object.
		/// </summary>
		public MethodData[] methodInfos = null;
	}

	/// <summary>
	/// Holds all ShowHideDatas.
	/// </summary>
	public class ShowHideDataTree
	{
		Dictionary<Type, ShowHideDatas> nullDictionary = new Dictionary<Type, ShowHideDatas>();
		Dictionary<Type, Dictionary<int, ShowHideDatas>> dictionary = new Dictionary<Type, Dictionary<int, ShowHideDatas>>();

		/// <summary>
		/// Get show hide data of this object.
		/// </summary>
		public ShowHideDatas Get(object obj, Type objType, string objName = null)
		{
			if (objType == null)
			{
				if (obj == null)
					return null;
				objType = obj.GetType();
			}
			if (!dictionary.ContainsKey(objType))
				dictionary[objType] = new Dictionary<int, ShowHideDatas>();
			if (obj == null)
			{
				if (!nullDictionary.ContainsKey(objType))
					nullDictionary[objType] = new ShowHideDatas(this, objType);
				return nullDictionary[objType];
			}

			int propertyHash = (string.IsNullOrEmpty(objName) ? Convert.ToString(obj) : objName).GetHashCode();
			if (!dictionary[objType].ContainsKey(propertyHash))
				dictionary[objType][propertyHash] = new ShowHideDatas(this, objType);
			return dictionary[objType][propertyHash];
		}
	}

	/// <summary>
	/// Helper Method for Deep/Static Inspector.
	/// </summary>
	public class Utilities
	{
		#region ====== Type & Parameters ======
		/// <summary>
		/// Debug mode.
		/// </summary>
#if DEEPINSPECTOR_DEBUG
		public static bool debug = true;
#else
		public static bool debug = false;
#endif
		[NonSerialized] static GUIStyle labelStyle = null;
		/// <summary>
		/// Label Style
		/// </summary>
		public static GUIStyle LabelStyle
		{
			get
			{
				if (labelStyle == null)
				{
					labelStyle = new GUIStyle(GUI.skin.label);
				}
				return labelStyle;
			}
		}

		[NonSerialized] static GUIStyle grayLabelStyle = null;
		/// <summary>
		/// Gray Label Style
		/// </summary>
		public static GUIStyle GrayLabelStyle
		{
			get
			{
				if (grayLabelStyle == null)
				{
					grayLabelStyle = new GUIStyle(GUI.skin.label);
					grayLabelStyle.normal.textColor = Color.gray;
				}
				return grayLabelStyle;
			}
		}

		[NonSerialized] static GUIStyle boldLabelStyle = null;
		/// <summary>
		/// Bold Label Style
		/// </summary>
		public static GUIStyle BoldLabelStyle
		{
			get
			{
				if (boldLabelStyle == null)
				{
					boldLabelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
				}
				return boldLabelStyle;
			}
		}

		[NonSerialized] static GUIStyle grayBoldLabelStyle = null;
		/// <summary>
		/// Gray Bold Label Style
		/// </summary>
		public static GUIStyle GrayBoldLabelStyle
		{
			get
			{
				if (grayBoldLabelStyle == null)
				{
					grayBoldLabelStyle = new GUIStyle(GrayLabelStyle) { fontStyle = FontStyle.Bold };
				}
				return grayBoldLabelStyle;
			}
		}

		const float fieldMinWidth = 300f;
		const float labelWidth = 14f;
		static float editorLabelWidth = 0f;
		static readonly string[] xyzw = new string[] { "X", "Y", "Z", "W" };
		static StringBuilder tempSB = new StringBuilder();
		static HashSet<object> tempRemoveList = new HashSet<object>();
		static ShowHideDataTree showHideData = new ShowHideDataTree();
		/// <summary>
		/// Show Hide Data Parent.
		/// </summary>
		public static ShowHideDataTree ShowHideData { get { return showHideData; } }
		[NonSerialized] static Dictionary<Type, DrawData> drawDatas = new Dictionary<Type, DrawData>();
		[NonSerialized] static Dictionary<Type, DrawData> staticDrawDatas = new Dictionary<Type, DrawData>();
		/// <summary>
		/// Currently drawing quaternion or euler angle.
		/// </summary>
		public static bool quaternionType = false;
		/// <summary>
		/// Is draw type?
		/// </summary>
		public static bool drawType = false;
		/// <summary>
		/// Currently in disable grouping?
		/// </summary>
		static int disabledIndex = 0;
		static bool isDisabledGrouping = false;
		static List<bool> disabledList = new List<bool>();
		#endregion
		#region ====== Helper Methods ======
		/// <summary>
		/// Draw transform inspector.
		/// </summary>
		public static void DrawTransform(Transform com)
		{
			if (com != null)
			{
				// position
				GUILayout.BeginHorizontal();
				bool reset = GUILayout.Button("Position", GUILayout.Width(60f));
				com.localPosition = DrawVector3(com.localPosition);
				if (reset) com.localPosition = Vector3.zero;
				GUILayout.EndHorizontal();
				// rotation
				GUILayout.BeginHorizontal();
				reset = GUILayout.Button("Rotation", GUILayout.Width(60f));
				com.localRotation = DrawQuaternion(com.localRotation, quaternionType);
				if (reset) com.localRotation = Quaternion.identity;
				GUILayout.EndHorizontal();
				// scale
				GUILayout.BeginHorizontal();
				reset = GUILayout.Button("Scale", GUILayout.Width(60f));
				com.localScale = DrawVector3(com.localScale);
				if (reset) com.localScale = Vector3.one;
				GUILayout.EndHorizontal();
			}
		}

		public static void DrawRectTransform(RectTransform com)
		{
			if (com != null)
			{
				// position
				GUILayout.BeginHorizontal();
				bool reset = GUILayout.Button("Position", GUILayout.Width(60f));
				com.localPosition = DrawVector3(com.localPosition);
				if (reset) com.localPosition = Vector3.zero;
				GUILayout.EndHorizontal();
				// rect size
				GUILayout.BeginHorizontal();
				var size = com.sizeDelta;
				GUILayout.Label("Width");
				size.x = EditorGUILayout.FloatField(size.x);
				GUILayout.Label("Height");
				size.y = EditorGUILayout.FloatField(size.y);
				com.sizeDelta = size;
				GUILayout.EndHorizontal();
				// anchors
				GUILayout.BeginHorizontal();
				GUILayout.Label("Anchors");
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Min");
				var min = com.anchorMin;
				GUILayout.Label("X");
				min.x = EditorGUILayout.FloatField(min.x);
				GUILayout.Label("Y");
				min.y = EditorGUILayout.FloatField(min.y);
				com.anchorMin = min;
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Max");
				var max = com.anchorMax;
				GUILayout.Label("X");
				max.x = EditorGUILayout.FloatField(max.x);
				GUILayout.Label("Y");
				max.y = EditorGUILayout.FloatField(max.y);
				com.anchorMax = max;
				GUILayout.EndHorizontal();
				// pivot
				GUILayout.BeginHorizontal();
				GUILayout.Label("Pivot");
				var pivot = com.pivot;
				GUILayout.Label("X");
				pivot.x = EditorGUILayout.FloatField(pivot.x);
				GUILayout.Label("Y");
				pivot.y = EditorGUILayout.FloatField(pivot.y);
				com.pivot = pivot;
				GUILayout.EndHorizontal();
				// rotation
				GUILayout.BeginHorizontal();
				reset = GUILayout.Button("Rotation", GUILayout.Width(60f));
				com.localRotation = DrawQuaternion(com.localRotation, quaternionType);
				if (reset) com.localRotation = Quaternion.identity;
				GUILayout.EndHorizontal();
				// scale
				GUILayout.BeginHorizontal();
				reset = GUILayout.Button("Scale", GUILayout.Width(60f));
				com.localScale = DrawVector3(com.localScale);
				if (reset) com.localScale = Vector3.one;
				GUILayout.EndHorizontal();
			}
		}

		/// <summary>
		/// Unity inspector like naming.
		/// </summary>
		public static string ToTitleName(string Name)
		{
			tempSB.Length = 0;
			bool firstCharacter = true;
			for (int i = 0, max = Name.Length; i < max; ++i)
			{
				var c = Name[i];

				if (i == 0 && c == '_')
					continue;

				if (!firstCharacter)
				{
					if (c >= 'A' && c <= 'Z')
						tempSB.Append(' ');
				}
				else
				{
					if (c >= 'a' && c <= 'z')
					{
						c = c.ToString().ToUpper()[0];
						firstCharacter = false;
					}
					else if (c >= 'A' && c <= 'Z')
						firstCharacter = false;
				}
				tempSB.Append(c);
				if (i == 1 && c == '_' && tempSB[0] == 'M')
				{
					tempSB.Length = 0;
					firstCharacter = true;
				}
			}
			return tempSB.ToString();
		}

		/// <summary>
		/// Is this type can be foldered?
		/// </summary>
		static bool IsTypeFolderable(Type type)
		{
			if (type == null) return false;
			if (type.IsClass || !type.IsPrimitive)
			{
				string typeName = type.FullName;
				switch (typeName)
				{
					case "System.String":
					case "System.Text.StringBuilder":
					case "UnityEngine.Color":
					case "UnityEngine.Color32":
					case "UnityEngine.Vector2":
					case "UnityEngine.Vector3":
					case "UnityEngine.Vector4":
					case "UnityEngine.Rect":
					case "UnityEngine.Quaternion":
					case "UnityEngine.Bounds":
					case "UnityEngine.RectInt":
					case "UnityEngine.Vector2Int":
					case "UnityEngine.Vector3Int":
					case "UnityEngine.BoundsInt":
						return false;
				}
				if (typeof(UnityEngine.Object).IsAssignableFrom(type))
					return false;
				if (typeof(Enum).IsAssignableFrom(type))
					return false;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Helper method for drawing folder.
		/// </summary>
		public static bool ToggleFolder(string title, Type type, bool showing, int depth, float titleLabelWidth = 0f)
		{
			if (disabledIndex > 0) EditorGUI.EndDisabledGroup();

			GUILayout.BeginHorizontal();

			GUILayout.Space(depth * 20f);

			string mark = showing ? "\u25BC" : "\u25BA";
			bool clicked = false;
			if (GUILayout.Button(mark, GrayBoldLabelStyle, GUILayout.Width(20f)))
				clicked = true;

			if (titleLabelWidth != 0f)
				GUILayout.Label(title, (disabledIndex != 0 && !isDisabledGrouping) ? GrayLabelStyle : LabelStyle, GUILayout.Width(titleLabelWidth));
			else
				GUILayout.Label(title, (disabledIndex != 0 && !isDisabledGrouping) ? GrayLabelStyle : LabelStyle);

			if (drawType && type != null) GUILayout.Label("(" + type.ToString() + ")", GUILayout.MinWidth(100f));

			GUILayout.EndHorizontal();

			var rect = GUILayoutUtility.GetLastRect();
			if (GUI.Button(rect, "", LabelStyle))
				clicked = true;

			if (disabledIndex > 0) EditorGUI.BeginDisabledGroup(true);

			return clicked;
		}

		/// <summary>
		/// Draw folder.
		/// </summary>
		public static ShowHideDatas ShowFolder(object obj, Type type,
			string objName, int depth, bool drawNameOrFolder = true, float titleLabelWidth = 80f)
		{
			ShowHideDatas showHides = ShowHideData.Get(obj, type, objName);

			if (drawNameOrFolder)
			{
				if (IsTypeFolderable(type))
				{
					if (ToggleFolder(objName, type, showHides.showProperty, depth, titleLabelWidth))
						showHides.showProperty = !showHides.showProperty;
				}
				else
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label(objName, (disabledIndex != 0 && !isDisabledGrouping) ? GrayLabelStyle : LabelStyle, GUILayout.Width(titleLabelWidth));
					if (drawType && type != null) GUILayout.Label("(" + type.ToString() + ")", GUILayout.MinWidth(100f));
					showHides.showProperty = true;
					GUILayout.EndHorizontal();
				}
			}
			else
			{
				if (!IsTypeFolderable(type))
					showHides.showProperty = true;
			}

			return showHides;
		}

		/// <summary>
		/// Get draw data.
		/// </summary>
		static DrawData GetDrawDatas(Type type, bool isStatic)
		{
			if (type == null) return null;
			if (isStatic)
			{
				if (!staticDrawDatas.ContainsKey(type))
				{
					staticDrawDatas[type] = new DrawData(type, true);
				}

				return staticDrawDatas[type];
			}
			if (!drawDatas.ContainsKey(type))
			{
				drawDatas[type] = new DrawData(type, false);
			}
			return drawDatas[type];
		}

		/// <summary>
		/// Draw Object.
		/// </summary>
		public static object DrawObject(Type type, object obj, ShowHideDatas showHides, Type parentType, int stack = 0, int depth = 0)
		{
			if (depth > 4 || stack > 4) return obj;
			DrawData drawData = GetDrawDatas(type, obj == null);
			if (drawData == null) return null;

			for (int c = 0, max = drawData.memberInfos.Length; c < max; ++c)
			{
				var member = drawData.memberInfos[c];
				if (member.property)
				{
					DrawProperty(member.member as PropertyInfo, obj, parentType, stack, depth, c);
				}
				else
				{
					DrawField(member.member as FieldInfo, obj, parentType, stack, depth, c);
				}
			}

			GUILayout.BeginVertical(GUI.skin.box);
			{
				GUILayout.BeginHorizontal();

				string mark = showHides.showMethod ? "\u25BC" : "\u25BA";

				var clicked = GUILayout.Button(mark, GrayBoldLabelStyle, GUILayout.Width(20f));

				GUILayout.Label(ToTitleName(drawData.type.Name) + " Methods:", BoldLabelStyle);

				showHides.showBaseMethod = GUILayout.Toggle(showHides.showBaseMethod, "Base Methods", GUILayout.Width(120f));

				GUILayout.EndHorizontal();

				// get horizntal
				var rect = GUILayoutUtility.GetLastRect();
				if (clicked || GUI.Button(rect, "", LabelStyle))
				{
					showHides.showMethod = !showHides.showMethod;
				}

				if (showHides.showMethod)
				{
					foreach (var method in drawData.methodInfos)
					{
						if (method.isBaseType && !showHides.showBaseMethod) continue;
						DrawMethod(method, obj, stack, depth);
					}
				}
			}
			GUILayout.EndVertical();
			return obj;
		}

		/// <summary>
		/// Can this type be create by Activator.CreateInstance?
		/// </summary>
		static bool CanCreateInstance(Type type, bool log)
		{
			if (type == typeof(string) || type.IsEnum) return false;
			if (type.IsAbstract || type.IsInterface || type.IsGenericType || type == typeof(object)) return false;
			if (type.IsClass)
			{
				if (typeof(UnityEngine.Object).IsAssignableFrom(type) || type.IsSubclassOf(typeof(Delegate)))
					return false;
				var constructor = type.GetConstructor(Type.EmptyTypes);
				if (constructor == null)
				{
					if (log) Debug.LogError("CreateInstance Error : Type (" + type + ") does not have a default constructor.");
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Create instance using Activator.CreateInstance.
		/// </summary>
		static object CreateInstance(Type type, bool log = false)
		{
			object result = null;
			if (CanCreateInstance(type, log))
				result = Activator.CreateInstance(type);
			else if (type == typeof(string))
				result = string.Empty;
			else if (type.IsEnum)
				result = type.GetFields(BindingFlags.Static | BindingFlags.Public).First().GetValue(null);
			return result;
		}

		/// <summary>
		/// Draw property / field.
		/// </summary>
		static object DrawPropertyOrField(bool isPrivate,
			string propertyName, Type propertyType, object property, Type parentType, int stack, int depth, int index = -1,
			bool editable = true, bool lineControl = true, float titleLabelWidth = 80f)
		{
			object result = null;

			BeginDisabledGroup(!editable);

			if (lineControl) GUILayout.BeginHorizontal();

			GUILayout.Space((depth + 1) * 20f);

			string title = isPrivate ? "(non-public) " + ToTitleName(propertyName) : ToTitleName(propertyName);
			titleLabelWidth = Mathf.Max(LabelStyle.CalcSize(new GUIContent(title)).x, titleLabelWidth);
			var showHides = ShowFolder(property, propertyType, title, depth, true, titleLabelWidth);

			string typeName = propertyType == null ? null : propertyType.FullName;

			if (showHides.showProperty)
			{
				SetLabelWidth(labelWidth);
				if (property == null)
				{
					bool drawNull = true;
					if (propertyType != null)
					{
						if (typeof(UnityEngine.Object).IsAssignableFrom(propertyType))
						{
							result = EditorGUILayout.ObjectField(property as UnityEngine.Object, propertyType, true, GUILayout.MinWidth(fieldMinWidth));
							drawNull = false;
						}
						else
							result = CreateInstance(propertyType);
					}
					if (drawNull) GUILayout.Label("null", GUILayout.MinWidth(fieldMinWidth));
				}
				else
				{
					switch (typeName)
					{
						case "System.IntPtr":
						case "System.UIntPtr":
							GUILayout.Label("pointer is not drawable", GUILayout.MinWidth(fieldMinWidth));
							break;
						case "UnityEngine.Vector2":
							result = DrawVector2((Vector2)property);
							break;
						case "UnityEngine.Vector3":
							result = DrawVector3((Vector3)property);
							break;
						case "UnityEngine.Vector4":
							result = DrawVector4((Vector4)property);
							break;
						case "UnityEngine.Quaternion":
							result = DrawQuaternion((Quaternion)property, quaternionType);
							break;
#if (UNITY_5 || UNITY_5_3_OR_NEWER)
						case "System.Int64":
							result = EditorGUILayout.LongField("    ", Convert.ToInt64(property), GUILayout.MinWidth(fieldMinWidth));
							break;
#else
						case "System.Int64":
#endif
						case "System.Int32":
						case "System.Int16":
						case "System.UInt16":
						case "System.UInt32":
						case "System.UInt64":
							result = EditorGUILayout.IntField("    ", Convert.ToInt32(property), GUILayout.MinWidth(fieldMinWidth));
							break;
						case "System.Decimal":
#if (UNITY_5 || UNITY_5_3_OR_NEWER)
						case "System.Double":
						
							result = EditorGUILayout.DoubleField("    ", Convert.ToDouble(property), GUILayout.MinWidth(fieldMinWidth));
							break;
#else
						case "System.Double":
#endif
						case "System.Single":
							result = EditorGUILayout.FloatField("    ", Convert.ToSingle(property), GUILayout.MinWidth(fieldMinWidth));
							break;
						case "System.String":
							result = DrawText(showHides, Convert.ToString(property), GUILayout.MinWidth(fieldMinWidth));
							break;
						case "System.Text.StringBuilder":
							BeginDisabledGroup(true);
							DrawText(showHides, Convert.ToString(property), GUILayout.MinWidth(fieldMinWidth));
							result = property;
							EndDisabledGroup();
							break;
						case "System.Boolean":
							result = EditorGUILayout.Toggle(Convert.ToBoolean(property), GUILayout.MinWidth(fieldMinWidth));
							break;
#if UNITY_2017_2_OR_NEWER
						case "UnityEngine.Vector2Int":
							result = DrawVector2Int((Vector2Int)property);
							break;
						case "UnityEngine.Vector3Int":
							result = DrawVector3Int((Vector3Int)property);
							break;
						case "UnityEngine.RectInt":
							result = EditorGUILayout.RectIntField((RectInt)property, GUILayout.MinWidth(fieldMinWidth));
							break;
						case "UnityEngine.BoundsInt":
							result = EditorGUILayout.BoundsIntField((BoundsInt)property, GUILayout.MinWidth(fieldMinWidth));
							break;
#endif
						case "UnityEngine.Color":
						case "UnityEngine.Color32":
							result = EditorGUILayout.ColorField((Color)property, GUILayout.MinWidth(fieldMinWidth));
							break;
						case "UnityEngine.Rect":
							result = EditorGUILayout.RectField((Rect)property, GUILayout.MinWidth(fieldMinWidth));
							break;
						case "UnityEngine.Bounds":
							result = EditorGUILayout.BoundsField((Bounds)property, GUILayout.MinWidth(fieldMinWidth));
							break;
						case "UnityEngine.AnimationCurve":
							result = EditorGUILayout.CurveField((AnimationCurve)property, GUILayout.MinWidth(fieldMinWidth));
							break;
						case "UnityEngine.Matrix4x4":
							EditorGUILayout.TextArea(property.ToString(), GUILayout.MinWidth(fieldMinWidth));
							result = property;
							break;
						default:
							if (typeof(Enum).IsAssignableFrom(propertyType))
							{
								result = EditorGUILayout.EnumPopup((Enum)property, GUILayout.MinWidth(fieldMinWidth));
							}
							else if (property is UnityEngine.Object)
							{
								result = EditorGUILayout.ObjectField((UnityEngine.Object)property, propertyType, true, GUILayout.MinWidth(fieldMinWidth));
							}
							else
							{
								try
								{
									if (property is IDictionary)
									{
										result = DrawIDictionary(showHides, stack + 1, depth + 1, index, (IDictionary)property, propertyType, parentType);
									}
									else if (property is IEnumerable)
									{
										result = DrawIEnumerable(showHides, stack + 1, depth + 1, index, (IEnumerable)property, parentType);
									}
									else
									{
										GUILayout.BeginVertical(GUI.skin.box);
										{
											result = DrawObject(propertyType, property, showHides, parentType, stack + 1, 0);
										}
										EditorGUILayout.EndVertical();
									}
								}
								catch (Exception e)
								{
									Debug.LogError(e);
								}
							}
							break;
					}
				}
				
				ResetLabelWidth();
			}
			else result = property;

			if (lineControl) GUILayout.EndHorizontal();

			EndDisabledGroup();

			return result;
		}

		static string DrawText(ShowHideDatas showHides, string str, params GUILayoutOption[] options)
		{
			if (str.Length < 60)
				return EditorGUILayout.TextField(str, options);

			if (str.Length > 15000)
			{
				EditorGUILayout.LabelField("String is too long for TextMesh Generator.");
				return str;
			}
			return EditorGUILayout.TextArea(str, options);
		}

		/// <summary>
		/// Draw field.
		/// </summary>
		static void DrawField(FieldInfo field, object obj, Type parentType, int stack, int depth, int index)
		{
			object value = null;
			try
			{
				value = field.GetValue(obj);
			}
			catch (Exception ex)
			{
				if (debug) Debug.LogError(string.Format("Component:{0} field:{1} throw errores:{2}", parentType.Name, field.Name, ex));
				return;
			}
			bool editable = !field.IsLiteral && !field.IsInitOnly;

			var result = DrawPropertyOrField(!field.IsPublic, field.Name, field.FieldType, value, parentType, stack, depth, index, editable, true, 200f);

			if (result != null && editable)
			{
				if (obj is UnityEngine.Object) Undo.RecordObject((UnityEngine.Object)obj, "Set " + field.Name);

				try
				{
					if (result is string)
						field.SetValue(obj, System.ComponentModel.TypeDescriptor.GetConverter(field.FieldType).ConvertFromString(result.ToString()));
					else
						field.SetValue(obj, result);
				}
				catch (Exception ex)
				{
					if (debug) Debug.LogError(string.Format("Component:{0} field:{1} throw errores:{2}", parentType.Name, field.Name, ex));
				}
			}
		}

		/// <summary>
		/// Draw property.
		/// </summary>
		static void DrawProperty(PropertyInfo property, object obj, Type parentType, int stack, int depth, int index)
		{
			if (DontDraw(property)) return;
			if (DrawNameOnly(property))
			{
				BeginDisabledGroup(true);
				GUILayout.BeginHorizontal();
				GUILayout.Space(20f);
				GUILayout.Label(property.Name);
				GUILayout.Label("(This property is not readable)");
				GUILayout.EndHorizontal();
				EndDisabledGroup();
				return;
			}
			try
			{
				bool draw = DrawSet(property);
				var value = property.GetValue(obj, null);
				var result = DrawPropertyOrField(false, property.Name, property.PropertyType, value, parentType, stack, depth, index, draw, true, 200f);
				if (draw)
				{
					if (result != null)
					{
						if (obj is UnityEngine.Object) Undo.RecordObject((UnityEngine.Object)obj, "Set " + property.Name);

						if (result is string)
							property.SetValue(obj, System.ComponentModel.TypeDescriptor.GetConverter(property.PropertyType).ConvertFromString(result.ToString()), null);
						else
							property.SetValue(obj, result, null);
					}
				}
			}
			catch (Exception ex)
			{
				if (debug) Debug.LogError(string.Format("Object:{0} property:{1} throw errores:{2}", parentType.Name, property.Name, ex));
			}
		}

		/// <summary>
		/// Get array/dictionary element type.
		/// </summary>
		static Type GetElementType(object property, int index = 0)
		{
			if (property == null) return null;

			Type enumerableType = property.GetType();
			Type propertyType = null;
			if (enumerableType.HasElementType)
				propertyType = enumerableType.GetElementType();
			else
				propertyType = enumerableType.GetGenericArguments()[index];

			return propertyType;
		}

		/// <summary>
		/// Draw IDictionary.
		/// </summary>
		static IDictionary DrawIDictionary(ShowHideDatas showHides, int stack, int depth, int index, IDictionary property, Type propertyType, Type parentType)
		{
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Space((depth + 1) * 20f);

			if (!showHides.dictAddFlags.ContainsKey(index))
				showHides.dictAddFlags[index] = new ShowHideDatas.DictionaryData(propertyType);

			var dictionaryData = showHides.dictAddFlags[index];

			GUILayout.Label("Size:" + property.Count);

			if (GUILayout.Button("Add", GUILayout.Width(80f)))
			{
				dictionaryData.Show = !dictionaryData.Show;
			}
			if (GUILayout.Button("Clear", GUILayout.Width(80f)))
			{
				if (EditorUtility.DisplayDialog("Depp Inspector Warning", "You're going to clear this dictionary.", "Yes", "No"))
				{
					property.Clear();
				}
			}

			GUILayout.EndHorizontal();

			if (dictionaryData.Show)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(depth * 20f);
				GUILayout.BeginVertical(GUI.skin.box);
				{
					GUILayout.BeginHorizontal();
					dictionaryData.key = DrawPropertyOrField(false, string.Format("Key ({0}):", dictionaryData.keyType), dictionaryData.keyType, dictionaryData.key, parentType, stack, depth, 0, true, false, 200f);
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					dictionaryData.value = DrawPropertyOrField(false, string.Format("Value ({0}):", dictionaryData.valueType), dictionaryData.valueType, dictionaryData.value, parentType, stack, depth, 0, true, false, 200f);
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					GUILayout.Space(depth * 20f);
					if (GUILayout.Button("Add New Object"))
					{
						try
						{
							if (!property.Contains(dictionaryData.key))
							{
								property.Add(dictionaryData.key, dictionaryData.value);
								dictionaryData.key = dictionaryData.value = null;
								dictionaryData.Show = false;
							}
							else
							{
								Debug.LogError(string.Format("Key:{0}, already existed.", dictionaryData.key));
							}
						}
						catch (Exception ex)
						{
							if (debug) Debug.LogError(ex);
						}
					}
					GUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}

			if (property.Count > 0)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space((depth + 1) * 20f);
				GUILayout.BeginVertical(GUI.skin.box);
				{
					if (!showHides.longListDatas.ContainsKey(index))
						showHides.longListDatas[index] = new ShowHideDatas.LongListData(showHides);
					var longListData = showHides.longListDatas[index];

					DrawIEnumerableFolder(property.Count, depth, longListData, (st, ed) =>
					{
						Type keyType = GetElementType(property, 0);
						Type valueType = GetElementType(property, 1);
						tempRemoveList.Clear();
						int ind = 0;
						foreach (DictionaryEntry ele in property)
						{
							if (ind >= st && ind < ed)
							{
								GUILayout.BeginHorizontal();
								DrawPropertyOrField(false, "key", keyType, ele.Key, parentType, stack + 1, depth + 1, ind, true, false, 60f);
								DrawPropertyOrField(false, "value", valueType, ele.Value, parentType, stack + 1, depth + 1, ind, true, false, 60f);
								if (GUILayout.Button("Remove", GUILayout.Width(80f)))
								{
									if (EditorUtility.DisplayDialog("Depp Inspector Warning", "You're going to remove this key.", "Yes", "No"))
									{
										tempRemoveList.Add(ele.Key);
									}
								}
								GUILayout.EndHorizontal();
							}
							ind++;
						}
						if (tempRemoveList.Count > 0)
						{
							foreach (var key in tempRemoveList)
							{
								property.Remove(key);
							}
						}
					});
				}
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
			GUILayout.BeginHorizontal();
			return property;
		}

		/// <summary>
		/// Draw IEnumerable.
		/// </summary>
		static IEnumerable DrawIEnumerable(ShowHideDatas showHides, int stack, int depth, int index, IEnumerable property, Type parentType)
		{
			GUILayout.EndHorizontal();

			int size = GetCount(property);

			GUILayout.BeginHorizontal();
			GUILayout.Space((depth + 2) * 20f);
			GUILayout.Label("Size", GUILayout.Width(80f));

			if (property is IList)
			{
				var list = property as IList;
#if !UNITY_5_3_OR_NEWER
				size = Mathf.Clamp(EditorGUILayout.IntField(list.Count), 0, int.MaxValue);
#else
				size = Mathf.Clamp(EditorGUILayout.DelayedIntField(list.Count), 0, int.MaxValue);
#endif
				if (size != list.Count)
				{
					property = list = Resize(list, size);
				}
			}
			else
				GUILayout.Label("   " + size.ToString());

			if (GUILayout.Button("Add", GUILayout.Width(80f)))
			{
				try
				{
					Type propertyType = GetElementType(property, 0);
					if (propertyType != null)
					{
						var obj = CreateInstance(propertyType, true);
						if (obj != null)
						{
							TryAdd(property, obj);
						}
					}
				}
				catch (Exception ex)
				{
					if (debug) Debug.LogError(ex);
				}
			}

			GUILayout.EndHorizontal();

			if (!showHides.longListDatas.ContainsKey(index))
				showHides.longListDatas[index] = new ShowHideDatas.LongListData(showHides);
			var longListData = showHides.longListDatas[index];

			if (property is IList)
			{
				var list = property as IList;

				DrawIEnumerableFolder(size, depth + 1, longListData, (st, ed) =>
				{
					Type type = GetElementType(property, 0);
					for (int c = st; c < ed; ++c)
					{
						var ele = list[c];
						var result = DrawPropertyOrField(false, "Element " + c, type, ele, parentType, stack + 1, depth + 1, c, true, true, 80f);
						if (result != ele)
						{
							list[c] = result;
						}
					}
				});
			}
			else
			{
				int ind = 0;
				BeginDisabledGroup(true);

				DrawIEnumerableFolder(size, depth + 1, longListData, (st, ed) =>
				{
					Type type = GetElementType(property, 0);
					foreach (var ele in property)
					{
						if (ind >= st && ind < ed)
						{
							DrawPropertyOrField(false, "Element " + ind, type, ele, parentType, stack + 1, depth + 1, ind, false, true, 80f);
						}
						ind++;
					}
				});
				EndDisabledGroup();
			}

			GUILayout.BeginHorizontal();
			return property;
		}

		/// <summary>
		/// Draw long list and folder.
		/// </summary>
		static void DrawIEnumerableFolder(int size, int depth, ShowHideDatas.LongListData longListData, Action<int, int> onDraw, int eachSize = 50)
		{
			int totalLine = (size / Mathf.Max(eachSize, 1)) + 1;
			for (int c = 0; c < totalLine; ++c)
			{
				int st = c * eachSize;
				int ed = Mathf.Min((c + 1) * eachSize, size);
				if (st >= ed) continue;
				string title = (st != (ed - 1)) ? string.Format("{0}~{1}", st, ed - 1) : st.ToString();
				if (ToggleFolder(title, null, !longListData.IsFoldered(c), depth))
				{
					longListData.ToggleFolder(c);
				}
				if (!longListData.IsFoldered(c))
				{
					onDraw(st, ed);
				}
			}
		}

		/// <summary>
		/// Draw method.
		/// </summary>
		static void DrawMethod(MethodData method, object owner, int stack, int depth)
		{
			if (method == null || !method.canDraw) return;
			GUILayout.BeginVertical(GUI.skin.box);
			{
				if (method.hasParameter)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Space((depth + 1) * 20f);
					GUILayout.Label(method.method.Name + ", Parameters:");
					GUILayout.EndHorizontal();
					int max = method.parameters.Length;
					for (int c = 0; c < max; ++c)
					{
						var parameterInfo = method.parameters[c];
						method.cachedParamterDatas[c] = DrawPropertyOrField(false, parameterInfo.Name,
							parameterInfo.ParameterType, method.cachedParamterDatas[c], typeof(MethodInfo), stack + 1, depth + 1, c,
							true, true, 80f);
					}
					GUILayout.BeginHorizontal();
					GUILayout.Space((depth + 2) * 20f);
				}
				else
				{
					GUILayout.BeginHorizontal();
					GUILayout.Space((depth + 1) * 20f);
					GUILayout.Label(method.method.Name, GUILayout.Width(300f));
				}
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Call", GUILayout.Width(120f)))
				{
					try
					{
						object methodResult = null;
						if (method.hasParameter)
							methodResult = method.method.Invoke(owner, method.cachedParamterDatas);
						else
							methodResult = method.method.Invoke(owner, null);

						if (method.method.ReturnType != null && method.method.ReturnType != typeof(void))
						{
							if (methodResult is string)
							{
								string st = methodResult as string;
								if (string.IsNullOrEmpty(st))
									Debug.Log("Method (" + method.method.Name + ") Returning Null");
								else
									Debug.Log("Method (" + method.method.Name + ") Result: " + st);
							}
							else if (methodResult == null)
								Debug.Log("Method (" + method.method.Name + ") Returning Null");
							else
								Debug.Log("Method (" + method.method.Name + ") Result: " + methodResult);
						}
						else
							Debug.Log("Method (" + method.method.Name + ") Call Succeed.");
					}
					catch (Exception ex)
					{
						if (debug) Debug.LogError(ex);
					}
				}

				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}

		/// <summary>
		/// Define if this property.set can be draw.
		/// </summary>
		static bool DrawSet(PropertyInfo info)
		{
			if (!info.CanWrite)
				return false;

			if (info.ReflectedType.Name == "NavMeshAgent" &&
				(info.Name == "destination"))
				return false;

			return true;
		}

		/// <summary>
		/// Not drawing all Unity MonoBehaviour's gameObject property.
		/// </summary>
		static bool DontDraw(PropertyInfo info)
		{
			if (info.IsDefined(typeof(ObsoleteAttribute), true)
				|| info.Name == "enabled"
				|| info.Name == "isActiveAndEnabled"
				|| info.Name == "name"
				|| info.Name == "tag")
				return true;
			return false;
		}

		/// <summary>
		/// Return true if this property generate errors when drawing.
		/// </summary>
		static bool DrawNameOnly(PropertyInfo info)
		{
			if (!info.CanRead) return true;

			Type type = info.ReflectedType;
			string typeName = info.ReflectedType.Name;
			string infoName = info.Name;

			if (typeName == "Camera" &&
				(infoName == "layerCullDistances"))
				return true;

			if (typeName == "NavMeshAgent" &&
				(infoName == "isStopped"
				|| infoName == "remainingDistance"
				|| infoName == "resume"))
				return true;

			if (typeName == "MeshFilter" &&
				(infoName == "mesh"))
				return true;

			if ((typeof(Renderer).IsAssignableFrom(type) || typeof(Collider).IsAssignableFrom(type)) &&
				infoName == "material" || infoName == "materials")
				return true;

			return false;
		}

#if UNITY_2017_2_OR_NEWER
		/// <summary>
		/// Draw Vector2Int.
		/// </summary>
		static Vector2Int DrawVector2Int(Vector2Int vec)
		{
			SetLabelWidth(labelWidth);
			for (int c = 0; c < 2; ++c)
			{
				GUILayout.BeginHorizontal();
				vec[c] = EditorGUILayout.IntField(xyzw[c], vec[c]);
				GUILayout.EndHorizontal();
			}
			ResetLabelWidth();
			return vec;
		}

		/// <summary>
		/// Draw Vector3Int.
		/// </summary>
		static Vector3Int DrawVector3Int(Vector3Int vec)
		{
			SetLabelWidth(labelWidth);
			for (int c = 0; c < 3; ++c)
			{
				GUILayout.BeginHorizontal();
				vec[c] = EditorGUILayout.IntField(xyzw[c], vec[c]);
				GUILayout.EndHorizontal();
			}
			ResetLabelWidth();
			return vec;
		}
#endif
		/// <summary>
		/// Draw Vector2.
		/// </summary>
		static Vector2 DrawVector2(Vector2 vec, bool warpAngle = false)
		{
			SetLabelWidth(labelWidth);
			for (int c = 0; c < 2; ++c)
			{
				GUILayout.BeginHorizontal();
				vec[c] = EditorGUILayout.FloatField(xyzw[c], vec[c]);
				if (warpAngle) vec[c] = WrapAngle(vec[c]);
				GUILayout.EndHorizontal();
			}
			ResetLabelWidth();
			return vec;
		}

		/// <summary>
		/// Draw Vector3.
		/// </summary>
		static Vector3 DrawVector3(Vector3 vec, bool warpAngle = false)
		{
			SetLabelWidth(labelWidth);
			for (int c = 0; c < 3; ++c)
			{
				GUILayout.BeginHorizontal();
				vec[c] = EditorGUILayout.FloatField(xyzw[c], vec[c]);
				if (warpAngle) vec[c] = WrapAngle(vec[c]);
				GUILayout.EndHorizontal();
			}
			ResetLabelWidth();
			return vec;
		}

		/// <summary>
		/// Draw Vector4.
		/// </summary>
		static Vector4 DrawVector4(Vector4 vec, bool warpAngle = false)
		{
			SetLabelWidth(labelWidth);
			for (int c = 0; c < 4; ++c)
			{
				GUILayout.BeginHorizontal();
				vec[c] = EditorGUILayout.FloatField(xyzw[c], vec[c]);
				if (warpAngle) vec[c] = WrapAngle(vec[c]);
				GUILayout.EndHorizontal();
			}
			ResetLabelWidth();
			return vec;
		}

		/// <summary>
		/// Draw Quaternion.
		/// </summary>
		static Quaternion DrawQuaternion(Quaternion quat, bool quaternionType)
		{
			if (quaternionType)
			{
				SetLabelWidth(labelWidth);
				for (int c = 0; c < 4; ++c)
				{
					GUILayout.BeginHorizontal();
					quat[c] = WrapAngle(EditorGUILayout.FloatField(xyzw[c], quat[c]));
					GUILayout.EndHorizontal();
				}
				ResetLabelWidth();
				return quat;
			}

			return Quaternion.Euler(DrawVector3(quat.eulerAngles, true));
		}

		/// <summary>
		/// Make angle between 180 and -180.
		/// </summary>
		static float WrapAngle(float angle)
		{
			while (angle > 180f) angle -= 360f;
			while (angle < -180f) angle += 360f;
			return angle;
		}

		/// <summary>
		/// Get "Count" property.
		/// </summary>
		static int GetCount(object list)
		{
			if (list == null) return 0;

			if (list is ICollection)
			{
				return (list as ICollection).Count;
			}
			else if (list is ICollection<int>)
			{
				return (list as ICollection<int>).Count;
			}
			else if (list is ICollection<string>)
			{
				return (list as ICollection<string>).Count;
			}
			else if (list is ICollection<uint>)
			{
				return (list as ICollection<uint>).Count;
			}
			else if (list is ICollection<long>)
			{
				return (list as ICollection<long>).Count;
			}
			else if (list is ICollection<ulong>)
			{
				return (list as ICollection<ulong>).Count;
			}
			else if (list is ICollection<short>)
			{
				return (list as ICollection<short>).Count;
			}
			else if (list is ICollection<ushort>)
			{
				return (list as ICollection<ushort>).Count;
			}
			else if (list is ICollection<float>)
			{
				return (list as ICollection<float>).Count;
			}
			else if (list is ICollection<double>)
			{
				return (list as ICollection<double>).Count;
			}
			else
			{
				try
				{
					var objlist = (ICollection<object>)list;
					if (objlist != null)
						return objlist.Count;
				}
				catch { }
			}
			return 0;
		}

		/// <summary>
		/// Try Add data to collection.
		/// </summary>
		static void TryAdd(object list, object obj)
		{
			if (list == null) return;

			if (list is IList)
			{
				(list as IList).Add(obj);
			}

			else if (list is ICollection<int>)
			{
				(list as ICollection<int>).Add((int)obj);
			}
			else if (list is ICollection<string>)
			{
				(list as ICollection<string>).Add((string)obj);
			}
			else if (list is ICollection<uint>)
			{
				(list as ICollection<uint>).Add((uint)obj);
			}
			else if (list is ICollection<long>)
			{
				(list as ICollection<long>).Add((long)obj);
			}
			else if (list is ICollection<ulong>)
			{
				(list as ICollection<ulong>).Add((ulong)obj);
			}
			else if (list is ICollection<short>)
			{
				(list as ICollection<short>).Add((short)obj);
			}
			else if (list is ICollection<ushort>)
			{
				(list as ICollection<ushort>).Add((ushort)obj);
			}
			else if (list is ICollection<float>)
			{
				(list as ICollection<float>).Add((float)obj);
			}
			else if (list is ICollection<double>)
			{
				(list as ICollection<double>).Add((double)obj);
			}
			else
			{
				try
				{
					var objlist = (ICollection<object>)list;
					if (objlist != null)
					{
						objlist.Add(obj);
					}
				}
				catch { }
			}
		}

		/// <summary>
		/// Try resize a list/array.
		/// </summary>
		static IList Resize(IList list, int size)
		{
			if (list is int[])
				return Resize((int[])list, size);
			if (list is int[][])
				return Resize((int[][])list, size);
			if (list is int[][][])
				return Resize((int[][][])list, size);
			if (list is float[])
				return Resize((float[])list, size);
			if (list is float[][])
				return Resize((float[][])list, size);
			if (list is float[][][])
				return Resize((float[][][])list, size);
			if (list is string[])
				return Resize((string[])list, size);
			if (list is string[][])
				return Resize((string[][])list, size);
			if (list is string[][][])
				return Resize((string[][][])list, size);
			if (list is uint[])
				return Resize((uint[])list, size);
			if (list is uint[][])
				return Resize((uint[][])list, size);
			if (list is uint[][][])
				return Resize((uint[][][])list, size);
			if (list is short[])
				return Resize((short[])list, size);
			if (list is short[][])
				return Resize((short[][])list, size);
			if (list is short[][][])
				return Resize((short[][][])list, size);
			if (list is ushort[])
				return Resize((ushort[])list, size);
			if (list is ushort[][])
				return Resize((ushort[][])list, size);
			if (list is ushort[][][])
				return Resize((ushort[][][])list, size);
			if (list is long[])
				return Resize((long[])list, size);
			if (list is long[][])
				return Resize((long[][])list, size);
			if (list is long[][][])
				return Resize((long[][][])list, size);
			if (list is ulong[])
				return Resize((ulong[])list, size);
			if (list is ulong[][])
				return Resize((ulong[][])list, size);
			if (list is ulong[][][])
				return Resize((ulong[][][])list, size);
			if (list is double[])
				return Resize((double[])list, size);
			if (list is double[][])
				return Resize((double[][])list, size);
			if (list is double[][][])
				return Resize((double[][][])list, size);

			try
			{
				while (list.Count != size)
				{
					if (list.Count > size)
						list.RemoveAt(list.Count - 1);
					else
						list.Add(list[list.Count - 1]);
				}
			}
			catch (Exception ex)
			{
				if (debug) Debug.LogError(ex);
			}
			return list;
		}
		/// <summary>
		/// Resize a array.
		/// </summary>
		static T[] Resize<T>(T[] array, int size)
		{
			int oldSize = array == null ? 0 : array.Length;
			T[] newArray = new T[size];

			for (int c = 0; c < size; ++c)
			{
				if (c >= oldSize)
					newArray[c] = default(T);
				else
					newArray[c] = array[c];
			}

			return newArray;
		}

		/// <summary>
		/// Get Text between two string.
		/// </summary>
		public static string GetBetween(string text, string start, string end)
		{
			if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end) && text.Contains(start) && text.Contains(end))
			{
				int index = text.IndexOf(start) + start.Length;
				int endIndex = text.IndexOf(end, index);
				if (endIndex != -1)
				{
					return text.Substring(index, endIndex - index);
				}
			}
			return text;
		}

		static void SetLabelWidth(float width)
		{
			if (editorLabelWidth == 0)
				editorLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = labelWidth;
		}
		static void ResetLabelWidth()
		{
			if (editorLabelWidth != 0)
				EditorGUIUtility.labelWidth = editorLabelWidth;
		}

		/// <summary>
		/// A managed version of EditorGUI.BeginDisabledGroup
		/// </summary>
		public static void BeginDisabledGroup(bool disabledGroup)
		{
			disabledList.Add(disabledGroup);

			if (disabledGroup)
			{
				if (++disabledIndex == 1)
				{
					EditorGUI.BeginDisabledGroup(true);
					isDisabledGrouping = true;
				}
			}
		}

		/// <summary>
		/// A managed version of EditorGUI.EndDisabledGroup
		/// </summary>
		public static void EndDisabledGroup()
		{
			bool last = false;
			int count = disabledList.Count;
			if (count > 0)
			{
				last = disabledList[count - 1];
				disabledList.RemoveAt(count - 1);
			}
			if (last)
			{
				if (disabledIndex-- == 1)
				{
					isDisabledGrouping = false;
					EditorGUI.EndDisabledGroup();
				}
			}
		}
		#endregion
		#region ====== Draw Line ======
		[NonSerialized] static GUIStyle line = null;
		static GUIStyle Line
		{
			get
			{
				if (line == null)
				{
					line = new GUIStyle() { stretchWidth = true, margin = new RectOffset(0, 0, 7, 7) };
					var tex = new Texture2D(2, 2);
					var colours = tex.GetPixels32();
					for (int c = colours.Length - 1; c >= 0; --c)
						colours[c] = new Color32(255, 255, 255, 255);
					tex.SetPixels32(colours);
					line.normal.background = tex;
				}
				return line;
			}
		}

		static Color LineColor
		{
			get { return EditorGUIUtility.isProSkin ? new Color(0.157f, 0.157f, 0.157f) : new Color(0.5f, 0.5f, 0.5f); }
		}

		/// <summary>
		/// Draw line with height.
		/// </summary>
		public static void DrawLine(float height = 1f)
		{
			Rect position = GUILayoutUtility.GetRect(GUIContent.none, Line, GUILayout.Height(height));

			if (Event.current.type == EventType.Repaint)
			{
				Color restoreColor = GUI.color;
				GUI.color = LineColor;
				Line.Draw(position, false, false, false, false);
				GUI.color = restoreColor;
			}
		}
		#endregion
		#region ====== Load Mono Script ======
		/// <summary>
		/// Load Script using AssetDatabase.
		/// </summary>
		public static MonoScript LoadMonoScript(string scriptName)
		{
#if UNITY_4_3
			return null;
#else
			if (string.IsNullOrEmpty(scriptName)) return null;

			string[] guids = AssetDatabase.FindAssets("t:MonoScript " + scriptName);

			MonoScript s = null;

			if (guids.Length > 0)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[0]);
				s = (MonoScript)AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript));
			}

			return s;
#endif
		}
		#endregion
	}
}