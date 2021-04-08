using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DeepInsp
{
	public class StaticInspector : EditorWindow
	{
		#region ====== Type & Parameters ======
		Dictionary<Type, DrawData> datas = new Dictionary<Type, DrawData>();

		public string typeName = string.Empty;
		public string typeNameError = string.Empty;
		Vector2 scrollPos = new Vector2(1500f, 0f);
		#endregion
		#region ====== Unity Events ======
		[MenuItem("Window/Deep Inspector/Static Inspector")]
		static void Open()
		{
			var window = GetWindow<StaticInspector>("StaticInspector");
			window.minSize = new Vector2(525f, 525f);
		}
		HashSet<Type> removeTypes = new HashSet<Type>();
		void OnGUI()
		{
			DrawToolBar();
			scrollPos = GUILayout.BeginScrollView(scrollPos);
			{
				foreach (var dataPairs in datas)
				{
					var staticData = dataPairs.Value;

					GUILayout.BeginVertical(GUI.skin.box);
					{
						Utilities.DrawObject(staticData.type, null,
							Utilities.ShowFolder(staticData, staticData.type, staticData.type.FullName, 0, true, 200f),
							staticData.type);
						if (GUILayout.Button("Remove"))
						{
							removeTypes.Add(dataPairs.Key);
						}
					}
					GUILayout.EndVertical();
					Utilities.DrawLine();
				}

				foreach (var removeType in removeTypes)
				{
					datas.Remove(removeType);
				}
				removeTypes.Clear();

				DrawAdd();
			}
			GUILayout.EndScrollView();
		}
		void DrawToolBar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				GUILayout.BeginHorizontal(EditorStyles.toolbar);

				if (GUILayout.Button(Utilities.drawType ? "Draw Type" : "Hide Type", EditorStyles.toolbarButton, GUILayout.Width(80f)))
					Utilities.drawType = !Utilities.drawType;

				GUILayout.Space(5f);

				if (GUILayout.Button(Utilities.quaternionType ? "Quaternion" : "Euler", EditorStyles.toolbarButton, GUILayout.Width(80f)))
					Utilities.quaternionType = !Utilities.quaternionType;

				GUILayout.FlexibleSpace();

				GUILayout.EndHorizontal();
			}
			GUILayout.EndHorizontal();
		}
		#endregion
		#region ====== Draw Object ======
		void DrawAdd()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Type Class Name");
			var tName = EditorGUILayout.TextField(typeName, GUILayout.MinWidth(300f));
			if (tName != typeName)
			{
				typeNameError = string.Empty;
				typeName = tName;
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Inspect"))
			{
				Type type = Type.GetType(typeName + ", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
				if (type == null)
					type = Type.GetType(typeName + ", Assembly-CSharp-Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
				if (type == null)
					type = Type.GetType(typeName + ", UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
				if (type == null)
					type = Type.GetType(typeName + ", UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
				if (type == null)
				{
					typeNameError = "Cannot find this type.";
				}
				else
				{
					typeNameError = string.Empty;
					if (!datas.ContainsKey(type))
						datas[type] = new DrawData(type, true);
					typeName = string.Empty;
				}
			}
			GUILayout.EndHorizontal();
			if (!string.IsNullOrEmpty(typeNameError))
			{
				EditorGUILayout.HelpBox(typeNameError, MessageType.Error);
			}
		}
		#endregion
	}
}