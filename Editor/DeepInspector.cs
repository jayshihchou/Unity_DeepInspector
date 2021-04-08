using System;
#if UNITY_4_3
using System.Linq;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DeepInsp
{
	public class DeepInspector : EditorWindow
	{
		#region ====== Type & Parameters ======
		UnityEngine.Object target;

		bool lockTarget = false;
		Vector2 scrollPos = new Vector2(1500f, 0f);
		List<Component> components = new List<Component>();
		#endregion
		#region ====== Unity Events ======
		[MenuItem("Window/Deep Inspector/Deep Inspector")]
		static void Open()
		{
			var window = GetWindow<DeepInspector>("DeepInspector");
			window.OnSelectionChange();
			window.minSize = new Vector2(540f, 525f);
		}

		void OnSelectionChange()
		{
			if (!lockTarget)
			{
				target = Selection.activeObject;
			}
			Repaint();
		}

		void OnGUI()
		{
			scrollPos = GUILayout.BeginScrollView(scrollPos);
			{
				if (target == null)
				{
					//EditorGUILayout.HelpBox("No GameObject Selected.", MessageType.Info);
				}
				else
				{
					GUILayout.BeginHorizontal();

					GameObject go = target as GameObject;
					if (go != null)
					{
						GUILayout.Space(8f);
						if (go.activeSelf != EditorGUILayout.Toggle(go.activeSelf, GUILayout.MaxWidth(20f)))
							go.SetActive(!go.activeSelf);
#if !UNITY_5_3_OR_NEWER
						target.name = EditorGUILayout.TextField(target.name);
#else
						target.name = EditorGUILayout.DelayedTextField(target.name);
#endif
						EditorGUILayout.LabelField("Lock Inspector", GUILayout.Width(90f));
						lockTarget = EditorGUILayout.Toggle(lockTarget, EditorStyles.radioButton, GUILayout.MaxWidth(15f));

						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();

						EditorGUILayout.LabelField("Tag", GUILayout.Width(25f));
						go.tag = EditorGUILayout.TagField(go.tag);
						EditorGUILayout.LabelField("Layer", GUILayout.Width(40f));
						go.layer = EditorGUILayout.LayerField(go.layer);
					}
					else
#if !UNITY_5_3_OR_NEWER
						target.name = EditorGUILayout.TextField(target.name);
#else
						target.name = EditorGUILayout.DelayedTextField(target.name);
#endif
					if (GUILayout.Button(Utilities.drawType ? "Draw Type" : "Hide Type", EditorStyles.miniButton, GUILayout.Width(80f)))
						Utilities.drawType = !Utilities.drawType;
					if (GUILayout.Button(Utilities.quaternionType ? "Quaternion" : "Euler", EditorStyles.miniButton, GUILayout.Width(80f)))
						Utilities.quaternionType = !Utilities.quaternionType;

					GUILayout.EndHorizontal();

					Utilities.DrawLine();

					if (go != null)
					{
						components = GetAllComponents(go, components);
						foreach (var component in components)
						{
							DrawComponent(component);
							Utilities.DrawLine();
						}
					}
					else
					{
						DrawComponent(target);
					}
				}
			}
			GUILayout.EndScrollView();
		}
		static List<Component> GetAllComponents(GameObject go, List<Component> list)
		{
#if UNITY_4_3
			return go.GetComponents(typeof(Component)).ToList();
#else
			go.GetComponents(list);
			return list;
#endif
		}
		#endregion
		#region ====== Draw Object ======
		void DrawComponent(UnityEngine.Object component)
		{
			ShowHideDatas showHides = Utilities.ShowHideData.Get(component, component.GetType());

			GUILayout.BeginHorizontal();

			string mark = showHides.ShowObject ? "\u25BC" : "\u25BA";

			bool clicked = false;
			if (GUILayout.Button(mark, Utilities.GrayBoldLabelStyle, GUILayout.Width(20f)))
				clicked = true;

			var enabledProperty = component.GetType().GetProperty("enabled");
			if (enabledProperty != null)
				enabledProperty.SetValue(component, GUILayout.Toggle((bool)enabledProperty.GetValue(component, null), "", GUILayout.Width(12f)), null);
			GUILayout.Label(Utilities.ToTitleName(component.GetType().Name), Utilities.BoldLabelStyle);
			GUILayout.EndHorizontal();

			// get horizntal
			var rect = GUILayoutUtility.GetLastRect();
			clicked = GUI.Button(rect, "", Utilities.LabelStyle);
			if (clicked)
			{
				if (showHides.defaultCloseType)
				{
					if (!showHides.ShowObject)
					{
						if (EditorUtility.DisplayDialog("Depp Inspector Warning", "Open this component in Deep Inspector is not recommend. Are you sure you want to open this component?", "Open anyway", "No"))
						{
							showHides.ShowObject = true;
						}
					}
					else
						showHides.ShowObject = false;
				}
				else
				{
					showHides.ShowObject = !showHides.ShowObject;
				}
			}

			if (!showHides.ShowObject) return;

			Type comType = component.GetType();
			if (comType == typeof(Transform))
			{
				Utilities.DrawTransform(component as Transform);
			}
			else if (comType == typeof(RectTransform))
			{
				Utilities.DrawRectTransform(component as RectTransform);
			}
			else
			{
				MonoScript script = Utilities.LoadMonoScript(comType.Name);
				if (script != null)
				{
					Utilities.BeginDisabledGroup(true);
					GUILayout.BeginHorizontal();
					GUILayout.Space(20f);
					GUILayout.Label("Script", GUILayout.Width(200f));
					EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
					GUILayout.EndHorizontal();
					Utilities.EndDisabledGroup();

				}
				Utilities.DrawObject(comType, component, Utilities.ShowFolder(component, comType, string.Empty, 0, false), component.GetType());
			}
		}
		#endregion
	}
}