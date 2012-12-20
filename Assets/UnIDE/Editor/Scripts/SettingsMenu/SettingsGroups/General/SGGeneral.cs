using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

namespace UIDE.SettingsMenu.Groups {
	
	public class SGGeneral:SettingsGroup {
		
		public UIDESettingsGroupDataGeneral data;
		
		
		
		public override void Start() {
			title = "General";
			order = 0.0f;
			data = GetOrCreateDefaultDataAsset<UIDESettingsGroupDataGeneral>();
			editor.onTextEditorOpenCallbacks.Add(OnEditorOpen);
			editor.onTextEditorCloseCallbacks.Add(OnEditorClose);
		}
		
		public void OnEditorOpen(UIDETextEditor te) {
			data.openFiles.Remove(te.filePath);
			data.openFiles.Add(te.filePath);
			UIDEEditor.SetDirty(data);
			//UnityEditor.AssetDatabase.SaveAssets();
		}
		public void OnEditorClose(UIDETextEditor te) {
			data.openFiles.Remove(te.filePath);
			UIDEEditor.SetDirty(data);
			//UnityEditor.AssetDatabase.SaveAssets();
		}
		
		public override void Update() {
			
		}
		
		public override void OnGUI(Rect groupRect) {
			if (data == null) {
				return;
			}
			
			
			float labelWidth = (groupRect.width-16)/2.0f;
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Ctrl+Z Undo",GUILayout.Width(labelWidth));
			bool originalCtrlZUndo = data.useCtrlZUndo;
			data.useCtrlZUndo = GUILayout.Toggle(data.useCtrlZUndo,"");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Code Folding",GUILayout.Width(labelWidth));
			bool originalCodeFolding = data.useCodeFolding;
			data.useCodeFolding = GUILayout.Toggle(data.useCodeFolding,"");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("API Token Assemblies",GUILayout.Width(labelWidth));
			string originalTokenAssemblies = APITokensAssembliesToString();
			APITokensAssembliesFromString(UIDEGUI.LayoutTextField(APITokensAssembliesToString(),GUI.skin.textField,GUILayout.Width(labelWidth)));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			
			
			if (data.useCodeFolding) {
				EditorUtility.DisplayDialog("Unfinished Feature","Code folding is unfinished and cannot be used.","Ok");
				data.useCodeFolding = false;
			}
			
			if (data.useCtrlZUndo != originalCtrlZUndo) {
				UIDEEditor.SetDirty(data);
			}
			
			if (data.useCodeFolding != originalCodeFolding) {
				UIDEEditor.SetDirty(data);
			}
			
			if (APITokensAssembliesToString() != originalTokenAssemblies) {
				UIDEEditor.SetDirty(data);
			}
			
			if (GUI.changed) {
				UIDEEditor.SetDirty(data);
			}
		}
		
		
		
		private string APITokensAssembliesToString() {
			string str = "";
			foreach (string s in data.apiTokenAssemblies) {
				str += s+",";
			}
			str = str.TrimEnd(',');
			return str;
		}
		
		public void APITokensAssembliesFromString(string str) {
			data.apiTokenAssemblies = new List<string>();
			string[] parts = str.Split(',');
			for (int i = 0; i < parts.Length; i++) {
				parts[i] = parts[i].Replace(" ","");
				parts[i] = parts[i].Replace("\t","");
				parts[i] = parts[i].Replace("\n","");
				parts[i] = parts[i].Replace("\r","");
				if (parts[i] == "") continue;
				data.apiTokenAssemblies.Add(parts[i]);
			}
		}
		
		public List<string> GetAPITokenAssemblies() {
			if (data == null) return new List<string>();
			return data.apiTokenAssemblies;
		}
		
		public bool GetUseCtrlZUndo() {
			if (data == null) return true;
			return data.useCtrlZUndo;
		}
		
		public bool GetUseCodeFolding() {
			if (data == null) return true;
			return data.useCodeFolding;
		}
		
		public bool GetCollapseProjectPanel() {
			if (data == null) return false;
			return data.collapseProjectPanel;
		}
		public void SetCollapseProjectPanel(bool v) {
			if (data == null) return;
			if (v == data.collapseProjectPanel) return;
			data.collapseProjectPanel = v;
			UIDEEditor.SetDirty(data);
		}
		
		public string[] GetOpenFiles() {
			if (data == null) return new string[0];
			return data.openFiles.ToArray();
		}
	}
}
