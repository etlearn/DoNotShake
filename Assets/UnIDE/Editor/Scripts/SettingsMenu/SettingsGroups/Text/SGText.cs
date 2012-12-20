using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEditor;

namespace UIDE.SettingsMenu.Groups {
	public class SGText:SettingsGroup {
		static public string fontPath = "Assets/UnIDE/Editor/TextEditorFonts/";
		static public string defaultFontPath = "BitstreamVeraMono/BitstreamVeraMono.ttf";
		
		public UIDESettingsGroupDataText data;
		
		public string currentFontPath {
			get {
				return data.desiredFontPath;
			}
			set {
				if (data.desiredFontPath == value) return;
				data.desiredFontPath = value;
				UIDEEditor.SetDirty(data);
			}
		}
		public string currentFullFontPath {
			get {
				return fontPath+data.desiredFontPath;
			}
			set {
				if (value.Length < fontPath.Length) return;
				string trimmedName = value.Substring(fontPath.Length);
				currentFontPath = trimmedName;
			}
		}
		
		private string[] fontFiles = new string[0];
		private Vector2 fontPickScroll;
		
		private Vector2 boldStateScroll;
		private float boldStateItemHeight = 16.0f;
		
		public override void Start() {
			title = "Text";
			order = 1.0f;
			data = GetOrCreateDefaultDataAsset<UIDESettingsGroupDataText>();
			
			fontFiles = GetFontFiles();
			if (data.desiredFontPath == "") {
				data.desiredFontPath = defaultFontPath;
				UIDEEditor.SetDirty(data);
			}
			UpdateFont(false);
			
			//data.tokenDefBoldStates = null;
			if (data.tokenDefBoldStates == null || data.tokenDefBoldStates.Count == 0) {
				data.tokenDefBoldStates = new List<string>();
				data.tokenDefBoldStates.Add("DefaultText"+"|"+2);
				
				data.tokenDefBoldStates.Add("Word"+"|"+2);
				data.tokenDefBoldStates.Add("Word,Keyword"+"|"+2);
				data.tokenDefBoldStates.Add("Word,Modifier"+"|"+2);
				data.tokenDefBoldStates.Add("Word,PrimitiveType"+"|"+2);
				data.tokenDefBoldStates.Add("Word,APIToken,Type"+"|"+2);
				data.tokenDefBoldStates.Add("String"+"|"+2);
				data.tokenDefBoldStates.Add("String,CharString"+"|"+2);
				
				data.tokenDefBoldStates.Add("Comment,SingleLine"+"|"+2);
				data.tokenDefBoldStates.Add("Comment,Block,Contained"+"|"+2);
				data.tokenDefBoldStates.Add("Comment,Block,Start"+"|"+2);
				data.tokenDefBoldStates.Add("Comment,Block,End"+"|"+2);
				
				data.tokenDefBoldStates.Add("Number,Double"+"|"+2);
				data.tokenDefBoldStates.Add("Number,Float"+"|"+2);
				data.tokenDefBoldStates.Add("Number,Int32"+"|"+2);
				data.tokenDefBoldStates.Add("Number,Int64"+"|"+2);
				data.tokenDefBoldStates.Add("Number"+"|"+2);
				
				data.tokenDefBoldStates.Add("PreProcess"+"|"+2);
				
				data.tokenDefBoldStates.Add("WhiteSpace"+"|"+2);
				data.tokenDefBoldStates.Add("WhiteSpace,Tab"+"|"+2);
				
				data.tokenDefBoldStates.Add("LineEnd"+"|"+2);
				data.tokenDefBoldStates.Add("Dot"+"|"+2);
				
				UIDEEditor.SetDirty(data);
			}
		}
		
		public override void OnSwitchTo() {
			fontFiles = GetFontFiles();
		}
		
		public override void Update() {
			
		}
		
		public override void OnGUI(Rect groupRect) {
			if (data == null) {
				return;
			}
			
			GUIStyle insetStyle = editor.theme.GetStyle("PopupWindowInset");
			GUIStyle listItemStyle = editor.theme.GetStyle("ListItem");
			GUIStyle listItemSelectedStyle = editor.theme.GetStyle("ListItemSelected");
			
			float labelWidth = groupRect.width/2.0f;
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Font Size",GUILayout.Width(labelWidth));
			int originalFontSize = data.fontSize;
			data.fontSize = UIDEGUI.LayoutIntField(data.fontSize,GUI.skin.textField);
			//GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Tab Size",GUILayout.Width(labelWidth));
			int originalTabSize = data.tabSize;
			data.tabSize = UIDEGUI.LayoutIntField(data.tabSize,GUI.skin.textField);
			//GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Force Dynamic Font",GUILayout.Width(labelWidth));
			bool originalForceDynamicFont35 = data.forceDynamicFont35;
			data.forceDynamicFont35 = GUILayout.Toggle(data.forceDynamicFont35,"");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(5);
			
			//Font selection
			GUILayout.Label("Font");
			GUILayout.Space(0.0f);
			
			Rect fontPickRect = GUILayoutUtility.GetLastRect();
			fontPickRect.x = 10.0f;
			fontPickRect.height = 150.0f;
			fontPickRect.width = groupRect.width-fontPickRect.x-10-GUI.skin.verticalScrollbar.fixedWidth;
			
			
			Vector2 fontEntrySize = listItemStyle.CalcSize(new GUIContent(""));
			fontEntrySize.x = fontPickRect.width;
			
			if (fontFiles.Length*fontEntrySize.y > fontPickRect.height) {
				fontEntrySize.x -= GUI.skin.verticalScrollbar.fixedWidth;
			}
			else {
				fontPickRect.height = fontFiles.Length*fontEntrySize.y;
			}
			GUILayout.Space(fontPickRect.height);
			GUI.Box(fontPickRect,"",insetStyle);
			
			Rect fontPickContentRect = fontPickRect;
			fontPickContentRect.x = 0; fontPickContentRect.y = 0;
			fontPickContentRect.height = fontFiles.Length*fontEntrySize.y;
			fontPickContentRect.width = fontEntrySize.x;
			
			
			
			fontPickScroll = GUI.BeginScrollView(fontPickRect,fontPickScroll,fontPickContentRect);
			for (int i = 0; i < fontFiles.Length; i++) {
				Rect r = new Rect(0,0,fontEntrySize.x,fontEntrySize.y);
				r.y = i*fontEntrySize.y;
				string name = Path.GetFileNameWithoutExtension(fontFiles[i]);
				GUIStyle style = listItemStyle;
				if (currentFullFontPath.ToLower() == fontFiles[i].ToLower()) {
					style = listItemSelectedStyle;
				}
				if (GUI.Button(r,name,style)) {
					currentFullFontPath = fontFiles[i];
					UpdateFont(true);
					
				}
			}
			GUI.EndScrollView();
			
			GUILayout.Space(5.0f);
			
			
			//Bold overrides
			GUILayout.BeginHorizontal();
			GUILayout.Label("Bold Overrides");
			GUILayout.FlexibleSpace();
			GUILayout.Label("On Off Default");
			GUILayout.EndHorizontal();
			
			GUILayout.Space(150.0f);
			
			Rect boldStateListRect = GUILayoutUtility.GetLastRect();
			boldStateListRect.x = 10.0f;
			boldStateListRect.height = 150.0f;
			boldStateListRect.width = groupRect.width-boldStateListRect.x-10-GUI.skin.verticalScrollbar.fixedWidth;
			//boldStateListRect.width = Mathf.Min(groupRect.width-boldStateListRect.x-10.0f,300.0f);
			GUI.Box(boldStateListRect,"",insetStyle);
			
			
			Vector2 checkboxSize = GUI.skin.toggle.CalcSize(new GUIContent(""));
			boldStateItemHeight = checkboxSize.y;
			//GUISkin tmpSkin = GUI.skin;
			//GUI.skin = editor.defaultSkin;
			
			Rect boldStateContentRect = boldStateListRect;
			boldStateContentRect.x = 0; boldStateContentRect.y = 0;
			boldStateContentRect.height = data.tokenDefBoldStates.Count*boldStateItemHeight;
			boldStateContentRect.width -= GUI.skin.verticalScrollbar.fixedWidth;
			
			boldStateScroll = GUI.BeginScrollView(boldStateListRect,boldStateScroll,boldStateContentRect);
			//GUI.skin = tmpSkin;
			bool boldStateChanged = false;
			int c = 0;
			foreach (string bs in data.tokenDefBoldStates) {
				string type = GetTokenDefBoldStateType(bs);
				int state = GetTokenDefBoldState(bs);
				Rect r = boldStateContentRect;
				r.height = boldStateItemHeight;
				r.y = c*boldStateItemHeight;
				
				Rect lRect = r;
				Rect cb0Rect = r;
				Rect cb1Rect = r;
				Rect cb2Rect = r;
				
				cb2Rect.x += cb2Rect.width;
				cb2Rect.x -= checkboxSize.x+8;
				cb2Rect.width = checkboxSize.x;
				
				cb1Rect = cb2Rect;
				cb1Rect.x -= cb1Rect.width+8;
				
				cb0Rect = cb1Rect;
				cb0Rect.x -= cb0Rect.width+8;
				
				lRect.width = cb0Rect.x-lRect.x;
				
				int newState = state;
				if (BoldStateToggle(cb1Rect,newState, 0)) {
					newState = 0;
				}
				if (BoldStateToggle(cb0Rect,newState, 1)) {
					newState = 1;
				}
				if (BoldStateToggle(cb2Rect,newState, 2)) {
					newState = 2;
				}
				
				if (newState != state) {
					string newStr = type+"|"+newState;
					data.tokenDefBoldStates[c] = newStr;
					boldStateChanged = true;
				}
				
				GUI.Label(lRect,type);
				
				c++;
			}
			GUI.EndScrollView();
			
			
			if (boldStateChanged) {
				UpdateTokenDefBoldStates();
				UIDEEditor.SetDirty(data);
			}
			
			if (data.tabSize != originalTabSize) {
				if (data.tabSize < 1) data.tabSize = 1;
				if (data.tabSize > 64) data.tabSize = 64;
				UIDEEditor.SetDirty(data);
			}
			
			if (data.forceDynamicFont35 != originalForceDynamicFont35) {
				UpdateFont(true);
				UIDEEditor.SetDirty(data);
			}
			
			if (data.fontSize != originalFontSize) {
				data.fontSize = Mathf.Min(data.fontSize,45);
				UpdateFont(true);
				UIDEEditor.SetDirty(data);
			}
			
			if (GUI.changed) {
				UIDEEditor.SetDirty(data);
			}
		}
		
		private bool BoldStateToggle(Rect r, int i, int target) {
			bool state = false;
			if (i == target) {
				state = true;
			}
			bool newState = GUI.Toggle(r,state,"");
			if (state) return true;
			if (state != newState && newState) return true;
			return false;
		}
		
		public void UpdateTokenDefBoldStates() {
			for (int i = 0; i < data.tokenDefBoldStates.Count; i++) {
				string type = GetTokenDefBoldStateType(data.tokenDefBoldStates[i]);
				int state = GetTokenDefBoldState(data.tokenDefBoldStates[i]);
				UIDETokenDef def = UIDETokenDefs.Get(type);
				if (def != null) {
					if (state == 2) {
						def.isBold = def.originalIsBold;
					}
					else if (state == 1) {
						def.isBold = true;
					}
					else if (state == 0) {
						def.isBold = false;
					}
				}
			}
		}
		
		public Font UpdateFont(bool forceUpdate) {
			if (data == null) return null;
			UnityEngine.Object loadedObj = UIDEEditor.LoadAsset(currentFullFontPath);
			
			if (loadedObj == null || loadedObj.GetType() != typeof(Font)) {
				Debug.LogError("Font could not be found: "+currentFullFontPath);
				currentFontPath = defaultFontPath;
				loadedObj = UIDEEditor.LoadAsset(currentFullFontPath);
				if (loadedObj == null || loadedObj.GetType() != typeof(Font)) {
					Debug.LogError("Font still could not be found: "+currentFullFontPath);
					return null;
				}
			}
			
			Font font = (Font)loadedObj;
			
			string boldPath = Path.GetDirectoryName(currentFullFontPath)+"/";
			boldPath += Path.GetFileNameWithoutExtension(currentFullFontPath)+"_b";
			boldPath += Path.GetExtension(currentFullFontPath);
			
			UnityEngine.Object loadedBoldObj = UIDEEditor.LoadAsset(boldPath,true);
			Font boldFont = null;
			if (loadedBoldObj != null && loadedBoldObj.GetType() == typeof(Font)) {
				boldFont = (Font)loadedBoldObj;
			}
			if (forceUpdate || font != data.font || boldFont != data.boldFont) {
				if (font != null) {
					TrueTypeFontImporter importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(currentFullFontPath);
					importer.fontSize = data.fontSize;
					#if UNITY_3_5
					importer.fontRenderMode = FontRenderMode.NoAntialiasing;
					if (data.forceDynamicFont35) {
						importer.fontTextureCase = FontTextureCase.Dynamic;
					}
					else {
						importer.fontTextureCase = FontTextureCase.Unicode;
					}
					#else
					importer.fontTextureCase = FontTextureCase.Dynamic;
					importer.fontRenderingMode = FontRenderingMode.HintedSmooth;
					#endif
					AssetDatabase.ImportAsset(currentFullFontPath);
				}
				if (boldFont != null) {
					TrueTypeFontImporter importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(boldPath);
					importer.fontSize = data.fontSize;
					#if UNITY_3_5
					importer.fontRenderMode = FontRenderMode.NoAntialiasing;
					if (data.forceDynamicFont35) {
						importer.fontTextureCase = FontTextureCase.Dynamic;
					}
					else {
						importer.fontTextureCase = FontTextureCase.Unicode;
					}
					#else
					importer.fontRenderingMode = FontRenderingMode.HintedSmooth;
					importer.fontTextureCase = FontTextureCase.Dynamic;
					#endif
					AssetDatabase.ImportAsset(boldPath);
				}
			}
			data.font = font;
			data.boldFont = boldFont;
			
			if (data.font != null && editor != null && editor.theme != null) {
				editor.theme.UpdateFont(data.font, data.boldFont);
			}
			UIDEEditor.SetDirty(data);
			return data.font;
		}
		
		public string[] GetFontFiles() {
			return GetFontFiles(fontPath);
		}
		public string[] GetFontFiles(string path) {
			if (!Directory.Exists(path)) {
				Debug.LogWarning(path+" does not exist.");
				return new string[0];
			}
			string[] fileNames = Directory.GetFiles(path,"*.*");
			List<string> outFiles = new List<string>();
			foreach (string fn in fileNames) {
				string thisName = fn;
				thisName = fn.Replace("\\","/").Replace("//","/");
				string fnName = Path.GetFileNameWithoutExtension(thisName.ToLower());
				bool isBold = fnName.EndsWith("_b");
				if (!isBold && thisName.ToLower().EndsWith(".ttf")) {
					outFiles.Add(thisName);
				}
			}
			string[] folderNames = Directory.GetDirectories(path);
			foreach (string fn in folderNames) {
				outFiles.AddRange(GetFontFiles(fn));
			}
			return outFiles.ToArray();
		}
		
		public int GetTokenDefBoldState(string input) {
			int state = 2;
			if (input == null) return state;
			string[] parts = input.Split('|');
			if (parts.Length == 2) {
				int.TryParse(parts[1], out state);
			}
			return state;
		}
		public string GetTokenDefBoldStateType(string input) {
			string output = "";
			if (input == null) return output;
			string[] parts = input.Split('|');
			if (parts.Length == 2) {
				output = parts[0];
			}
			return output;
		}
		
		public int GetFontSize() {
			if (data == null) return 12;
			return data.fontSize;
		}
		
		public Font GetFont() {
			if (data == null) return null;
			return data.font;
		}
		
		public Font GetBoldFont() {
			if (data == null) return null;
			return data.boldFont;
		}
		
		public int GetTabSize() {
			if (data == null) return 4;
			int output = data.tabSize;
			if (output < 1) output = 1;
			return output;
		}
	}
}
