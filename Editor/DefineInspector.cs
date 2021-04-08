using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace DeepInsp
{
	public class DefineInspector : EditorWindow
	{
		#region ====== Type & Parameters ======
		static bool dirty = false;
		static bool Dirty
		{
			get
			{
				if (!dirty)
				{
					if (symbols.Count != tempTexts.Count) return true;
					for (int c = symbols.Count - 1; c >= 0; --c)
					{
						if (symbols[c] != tempTexts[c]) return true;
					}
				}
				return dirty;
			}
		}
		static List<string> symbols = new List<string>();
		static List<string> tempTexts = new List<string>();
		static HashSet<string> customSymbols = new HashSet<string>();
		static Vector2 scrollPos;
		static BuildTargetGroup selectedGroup;
		static string newSymbolText = string.Empty;
		static bool drawUnitySymbols = false;
		static bool drawCustomSymbols = false;

		const string groupKey = "DeepInsp.DefineInsp.GroupKey";
		#endregion
		#region ====== Unity Events ======
		[MenuItem("Window/Deep Inspector/Define Symbol Inspector")]
		static void Open()
		{
			GetWindow<DefineInspector>("DefineSymbols").minSize = new Vector2(350f, 300f);
		}

		void OnEnable()
		{
			if (EditorPrefs.HasKey(groupKey))
				selectedGroup = (BuildTargetGroup)EditorPrefs.GetInt(groupKey);
			else
				selectedGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			GetAllSymbols();
			Repaint();
		}
		void OnDisable()
		{
			EditorPrefs.SetInt(groupKey, (int)selectedGroup);
		}

		void OnGUI()
		{
			DrawToolBar();

			if (EditorApplication.isCompiling)
			{
				EditorGUILayout.HelpBox("Editor Is Compiling...", MessageType.Info);
				return;
			}

			DrawBody();
		}

		void DrawToolBar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				GUILayout.BeginHorizontal(EditorStyles.toolbar);

				if (GUILayout.Button("Refind Symbols", EditorStyles.toolbarButton))
				{
					GetAllSymbols();
				}

				GUILayout.FlexibleSpace();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Group:", GUILayout.Width(50f));
				var nextGroup = (BuildTargetGroup)EditorGUILayout.EnumPopup(selectedGroup, EditorStyles.toolbarPopup, GUILayout.Width(150f));
				if (selectedGroup != nextGroup)
				{
					selectedGroup = nextGroup;
					GetAllSymbols();
				}

				GUILayout.EndHorizontal();
				GUILayout.EndHorizontal();
			}
			GUILayout.EndHorizontal();
		}

		void DrawBody()
		{
			if (Dirty && GUILayout.Button("Apply Changes"))
			{
				UpdateSymbol(customSymbols);
			}

			scrollPos = GUILayout.BeginScrollView(scrollPos);
			{
				if (Utilities.ToggleFolder("Unity Symbols:", GetType(), drawUnitySymbols, 0))
					drawUnitySymbols = !drawUnitySymbols;
				if (drawUnitySymbols)
				{
					int count = 0;
					for (int c = 0, max = tempTexts.Count; c < max; ++c)
					{
						string currentText = tempTexts[c];
						if (!customSymbols.Contains(currentText))
						{
							GUILayout.BeginHorizontal();
							GUILayout.Space(20f);
							GUILayout.Label("Unity :", GUILayout.Width(60f));
							EditorGUILayout.TextField(symbols[c]);
							GUILayout.EndHorizontal();
							count++;
						}
					}
					if (count == 0)
					{
						GUILayout.BeginHorizontal();
						GUILayout.Space(20f);
						EditorGUILayout.HelpBox("No symbol found. Try open a script to create .csproj file.", MessageType.Info);
						GUILayout.EndHorizontal();
					}
				}
				if (Utilities.ToggleFolder("Custom Symbols:", GetType(), drawCustomSymbols, 0))
					drawCustomSymbols = !drawCustomSymbols;
				if (drawCustomSymbols)
				{
					int count = 0;
					for (int c = 0, max = tempTexts.Count; c < max; ++c)
					{
						string currentText = tempTexts[c];
						if (customSymbols.Contains(currentText))
						{
							GUILayout.BeginHorizontal();
							GUILayout.Space(20f);
							GUILayout.Label("Custom :", GUILayout.Width(60f));
							var next = GUILayout.TextField(currentText);
							if (currentText != next)
							{
								customSymbols.Remove(currentText);
								customSymbols.Add(tempTexts[c] = next);
								dirty = true;
							}
							if (GUILayout.Button("Revert", GUILayout.Width(80f)))
							{
								customSymbols.Remove(currentText);
								customSymbols.Add(tempTexts[c] = symbols[c]);
							}
							if (GUILayout.Button("Remove", GUILayout.Width(80f)))
							{
								customSymbols.Remove(currentText);
								dirty = true;
							}
							GUILayout.EndHorizontal();
							count++;
						}
					}
					if (count == 0)
					{
						GUILayout.BeginHorizontal();
						GUILayout.Space(20f);
						GUILayout.Label("No Symbols in list.");
						GUILayout.EndHorizontal();
					}
					GUILayout.BeginHorizontal(GUI.skin.box);
					{
						GUILayout.BeginVertical();
						GUILayout.BeginHorizontal();
						GUILayout.Space(20f);
						GUILayout.Label("New Symbol :");
						newSymbolText = GUILayout.TextField(newSymbolText);
						if (GUILayout.Button("Add", GUILayout.Width(80f)) && !customSymbols.Contains(newSymbolText))
						{
							symbols.Add(newSymbolText);
							customSymbols.Add(newSymbolText);
							tempTexts.Add(newSymbolText);
							newSymbolText = string.Empty;
							dirty = true;
						}
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal();
						EditorGUILayout.HelpBox("Note: you need to apply change to make Unity recompile.", MessageType.Info);
						GUILayout.EndHorizontal();
						GUILayout.EndVertical();
					}
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndScrollView();
		}
		#endregion
		#region ====== Helper Methods ======
		static void GetAllSymbols()
		{
			symbols.Clear();
			tempTexts.Clear();
			customSymbols.Clear();

			// Get All From .csproj file
			string path = Application.dataPath.Replace("/Assets", "");
			bool noFileError = false;
			if (Directory.Exists(path))
			{
				List<string> files = new List<string>();
				foreach (var file in Directory.GetFiles(path))
				{
					var ext = Path.GetExtension(file);
					if (ext == ".csproj")
					{
						files.Add(file);
					}
				}
				if (files.Count == 0) noFileError = true;
				else
				{
					HashSet<string> symbolList = new HashSet<string>();
					foreach (var file in files)
					{
						ParseFile(File.ReadAllLines(file), symbolList);
					}
					symbols = symbolList.ToList();
					for (int c = 0, max = symbols.Count; c < max; ++c)
						tempTexts.Add(symbols[c]);
				}
			}
			else noFileError = true;
			if (noFileError)
			{
				//Debug.LogError("Cannot found any.csproj file in path: " + path);
			}

			// Get All From Unity API
			ParseText(PlayerSettings.GetScriptingDefineSymbolsForGroup(selectedGroup), customSymbols);

			dirty = false;
		}
		static void ParseFile(string[] lines, HashSet<string> symbolOut)
		{
			foreach (var line in lines)
			{
				if (line.Contains("DefineConstants"))
				{
					ParseText(Utilities.GetBetween(line, "<DefineConstants>", "</DefineConstants>"), symbolOut);
				}
			}
		}
		static void ParseText(string line, HashSet<string> symbolOut)
		{
			if (string.IsNullOrEmpty(line)) return;
			var array = line.Split(';');
			foreach (string str in array)
			{
				symbolOut.Add(str);
			}
		}
		static void UpdateSymbol(HashSet<string> nextSymbols)
		{
			var symbolText = string.Empty;
			int count = nextSymbols.Count;
			int index = 0;
			foreach (string symbol in nextSymbols)
			{
				symbolText += symbol + (index == count ? "" : ";");
				index++;
			}
			PlayerSettings.SetScriptingDefineSymbolsForGroup(selectedGroup, symbolText);
		}
		#endregion
	}
}