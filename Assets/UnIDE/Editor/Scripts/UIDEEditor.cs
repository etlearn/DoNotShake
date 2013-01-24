using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System;
using System.IO;

using UIDE;
using UIDE.SettingsMenu;
using UIDE.SettingsMenu.Groups;
//using UIDE.Themes;
using UIDE.RightClickMenu;

#if UNITY_EDITOR
using UnityEditor;
#endif
//NotificationText SearchTextField SearchModeFilter
[System.Serializable]
public class UIDEEditor:System.Object {
	static private string _baseDir = null;
	static public string baseDir {
		get {
			if (_baseDir == null) {
				Type t = System.Type.GetType("UIDEEditorWindow");
				FieldInfo currrentField = t.GetField("current");
				ScriptableObject obj = (ScriptableObject)currrentField.GetValue(null);
				if (obj != null) {
					MonoScript ms = MonoScript.FromScriptableObject(obj);
					if (ms != null) {
						string path = AssetDatabase.GetAssetPath(ms);
						path = Path.GetDirectoryName(path);
						path = Path.GetDirectoryName(path);
						path = Path.GetDirectoryName(path);
						path += "/";
						_baseDir = path;
					}
				}
				if (_baseDir == null) {
					_baseDir = "Assets/UnIDE/";
				}
			}
			return _baseDir;
		}
	}
	
	static public EditorWindow GetEditorWindow() {
		Type t = System.Type.GetType("UIDEEditorWindow");
		FieldInfo currrentField = t.GetField("current");
		ScriptableObject obj = (ScriptableObject)currrentField.GetValue(null);
		return (EditorWindow)obj;
	}
	
	static public string tmpDir {
		get {
			return baseDir+"Editor/_TMP/";
		}
	}
	
	static public UIDEEditor current;
	static public float divWidth = 1.0f;
	
	public bool isFocused = false;
	
	public bool wantsRepaint = false;
	public Vector2 mousePos;
	
	public float time = 0.0f;
	
	private string[] filesDraggedToOpen = new String[0];
	
	public Rect rect;
	public float projectViewWidth = 200.0f;
	
	public Type currentThemeType {
		get {
			if (themeSettings == null) {
				Debug.LogError("Tried to get currentThemeType before SettingMenu.Initialize()");
				return null;
			}
			return themeSettings.GetCurrentThemeType();
		}
		set {
			if (themeSettings == null) {
				Debug.LogError("Tried to set currentThemeType before SettingMenu.Initialize()");
				return;
			}
			themeSettings.SetCurrentThemeType(value);
		}
	}
	
	public Theme theme;
	
	public GUISkin defaultSkin = null;
	public GUISkin skin {
		get {
			if (theme == null) {
				return null;
			}
			return theme.skin;
		}
	}
	
	public SGGeneral generalSettings {
		get {
			return SettingsMenu.GetGroupInstance<SGGeneral>();
		}
	}
	public SGText textSettings {
		get {
			return SettingsMenu.GetGroupInstance<SGText>();
		}
	}
	public SGTheme themeSettings {
		get {
			return SettingsMenu.GetGroupInstance<SGTheme>();
		}
	}
	public SGPlugins pluginSettings {
		get {
			return SettingsMenu.GetGroupInstance<SGPlugins>();
		}
	}
	
	
	public int tabSize {
		get {
			return textSettings.GetTabSize();
		}
	}
	
	public List<Action<UIDETextEditor>> onTextEditorOpenCallbacks = new List<Action<UIDETextEditor>>();
	public List<Action<UIDETextEditor>> onTextEditorCloseCallbacks = new List<Action<UIDETextEditor>>();
	
	public List<UIDETextEditor> textEditors = new List<UIDETextEditor>();
	public UIDETextEditor textEditor;
	
	public UIDEProjectView projectView;
	
	public bool canTextEditorInteract {
		get {
			//if (projectView.fileSearchFildIsFocused) return false;
			return isFocused && !SettingsMenu.enabled;
		}
	}
	
	private bool needsReinit = false;
	
	private Texture2D _whiteTex;
	public Texture2D whiteTex {
		get {
			if (!_whiteTex) {
				_whiteTex = new Texture2D(1,1,TextureFormat.ARGB32,false);
				_whiteTex.SetPixel(0,0,Color.white);
				_whiteTex.Apply();
				_whiteTex.hideFlags = HideFlags.HideAndDontSave;
			}
			return _whiteTex;
		}
	}
	
	public void Start(bool isReinit) {
		textEditor = null;
		textEditors = new List<UIDETextEditor>();
		//currentThemeType = typeof(ThemeSlateDark);
		
		projectView = UIDEProjectView.Load(this);
		
		SettingsMenu.Initialize();
		//Debug.Log(generalSettings);
		
		RefreshEverything();
		
		string[] openFiles = generalSettings.GetOpenFiles();
		generalSettings.ClearOpenFiles();
		for (int i = 0; i < openFiles.Length; i++) {
			EditorUtility.DisplayProgressBar("Opening Files",openFiles[i],((float)i/(float)openFiles.Length));
			OpenOrFocusEditorFromFile(openFiles[i]);
		}
		EditorUtility.ClearProgressBar();
		needsReinit = false;
	}
	
	public void SetDefaultSkin(GUISkin skin) {
		if (skin == null) return;
		if (defaultSkin == null && skin != defaultSkin) {
			defaultSkin = skin;
			theme.OnDefaultSkinSet(defaultSkin);
		}
	}
	
	public void RefreshEverything() {
		onTextEditorOpenCallbacks = new List<Action<UIDETextEditor>>();
		onTextEditorCloseCallbacks = new List<Action<UIDETextEditor>>();
		SettingsMenu.Initialize();
		
		if (projectView == null) {
			projectView = UIDEProjectView.Load(this);
		}
		projectView.Start();
		
		RefreshTheme();
	}
	
	public void RefreshTheme() {
		//UnityEditor.EditorUtility.UnloadUnusedAssets();
		if (!SetTheme(currentThemeType)) {
			Debug.LogError("Failed to set theme from type '"+currentThemeType+"'");
		}
	}
	
	public bool SetTheme(Type themeType) {
		if (!typeof(Theme).IsAssignableFrom(themeType)) return false;
		Theme instance = (Theme)Activator.CreateInstance(themeType);
		return SetTheme(instance);
	}
	public bool SetTheme(Theme themeToSet) {
		if (themeToSet == null) return false;
		if (themeToSet == theme) return false;
		if (theme != null) {
			theme.Destroy();
		}
		
		theme = themeToSet;
		currentThemeType = theme.GetType();
		theme.editor = this;
		
		theme.InitializeTokenDefs();
		theme.Start();
		return true;
	}
	
	public void OnFocus() {
		//if (defaultSkin != null) {
		//	for (int i = 0; i < defaultSkin.customStyles.Length; i++) {
		//		if (!defaultSkin.customStyles[i].name.ToLower().Contains("text")) continue;
		//		Debug.Log(defaultSkin.customStyles[i].name);
		//	}
		//}
		if (textEditor) {
			textEditor.OnWindowFocus();
		}
	}
	
	public void OnLostFocus() {
		if (textEditor) {
			textEditor.OnLostWindowFocus();
		}
	}
	
	public void OnRequestSave() {
		if (textEditor) {
			textEditor.DoSave();
		}
	}
	
	public void OnCloseWindow() {
		List<UIDETextEditor> editorsToClose = new List<UIDETextEditor>();
		for (int i = 0; i < textEditors.Count; i++) {
			editorsToClose.Add(textEditors[i]);
		}
		for (int i = 0; i < editorsToClose.Count; i++) {
			CloseTextEditor(editorsToClose[i],true,false);
		}
	}
	
	public void OnReloadScript(string fileName) {
		fileName = fileName.Replace("\\","/").Replace("//","/");
		string fileNameLower = fileName.ToLower();
		List<UIDETextEditor> modifiedTextEditors = new List<UIDETextEditor>();
		for (int i = 0; i < textEditors.Count; i++) {
			if (textEditors[i].filePath.ToLower() == fileNameLower) {
				modifiedTextEditors.Add(textEditors[i]);
			}
		}
		for (int i = 0; i < modifiedTextEditors.Count; i++) {
			if (!modifiedTextEditors[i].reloadIsOk) {
				if (EditorUtility.DisplayDialog("File modified externally.", "The file \""+fileName+"\" has been modified in an external program. Would you like to reload it?","Reload", "Keep current version")) {
					CloseTextEditor(modifiedTextEditors[i]);
					OpenOrFocusEditorFromFile(fileName);
				}
			}
			//Debug.Log("FILE CHANGED "+fileName);
			modifiedTextEditors[i].reloadIsOk = false;
		}
		RefreshEverything();
	}
	
	public UIDETextEditor OpenOrFocusEditorFromFile(string fileName) {
		fileName = fileName.Replace("\\","/").Replace("//","/");
		string fileNameLower = fileName.ToLower();
		UIDETextEditor te = null;
		for (int i = 0; i < textEditors.Count; i++) {
			if (textEditors[i].filePath.ToLower() == fileNameLower) {
				te = textEditors[i];
				break;
			}
		}
		if (te == null) {
			te = OpenEditorFromFile(fileName);
		}
		if (te == null) {
			return null;
		}
		SwitchToTextEditor(te);
		return te;
	}
	
	public void SwitchToTextEditor(UIDETextEditor te) {
		if (te == null || textEditor == te) return;
		if (textEditor != null) {
			textEditor.OnSwitchToOtherTab();
		}
		textEditor = te;
		textEditor.OnSwitchToTab();
		Repaint();
	}
	
	public UIDETextEditor OpenEditorFromFile(string fileName) {
		fileName = fileName.Replace("\\","/").Replace("//","/");
		if (!File.Exists(fileName)) {
			return null;
		}
		textEditor = UIDETextEditor.Load(this,fileName);
		if (textEditor == null) {
			return null;
		}
		textEditors.Add(textEditor);
		
		foreach (Action<UIDETextEditor> f in onTextEditorOpenCallbacks) {
			f(textEditor);
		}
		
		return textEditor;
	}
	
	public int CloseTextEditor(UIDETextEditor te) {
		return CloseTextEditor(te,true,true);
	}
	public int CloseTextEditor(UIDETextEditor te, bool savePrompt, bool doCloseCallback) {
		if (te == null) return -1;
		
		if (savePrompt) {
			//Debug.Log(te.name+" "+ te.isDirty);
			if (te.isDirty) {
				int confirmValue = EditorUtility.DisplayDialogComplex("Unsaved Changes", "The file \""+te.fileName+"\" has unsaved changes. Would you like to save?","Save", "Discard", "Cancel");
				if (confirmValue == 0) {
					te.DoSave();
				}
				if (confirmValue == 2) {
					return -1;
				}
			}
		}
		
		int newIndex = textEditors.IndexOf(te);
		textEditors.Remove(te);
		newIndex = Mathf.Clamp(newIndex,0,textEditors.Count-1);
		if (textEditors.Count > 0) {
			textEditor = textEditors[newIndex];
		}
		else {
			textEditor = null;
		}
		
		if (doCloseCallback) {
			foreach (Action<UIDETextEditor> f in onTextEditorCloseCallbacks) {
				f(te);
			}
		}
		
		//AssetDatabase.DeleteAsset(te.assetPath);
		//AssetDatabase.DeleteAsset(te.undoAssetPath);
		
		return newIndex;
	}
	
	public void UpdateRects() {
		if (projectView == null) {
			projectView = UIDEProjectView.Load(this);
		}
		projectView.rect = rect;
		projectView.rect.x = 0;
		projectView.rect.y = 0;
		projectView.rect.width = projectViewWidth-divWidth;
		if (textEditor) {
			textEditor.rect = rect;
			textEditor.rect.x = 0;
			textEditor.rect.y = 0;
			textEditor.rect.width -= projectView.actualRect.width+divWidth*2;
			textEditor.rect.x += projectView.actualRect.width+divWidth*2;
		}
	}
	
	public void Update() {
		for (int i = 0; i < textEditors.Count; i++) {
			if (textEditors[i] == null || textEditors[i].undoManager == null) {
				needsReinit = true;
			}
		}
		if (needsReinit) {
			Start(true);
			/*
			EditorWindow editorWindow = GetEditorWindow();
			Type typeOfEditorWindow = editorWindow.GetType();
			
			editorWindow.Close();
			EditorWindow.GetWindow(typeOfEditorWindow,false,"UnIDE");
			*/
			return;
		}
		
		if (theme == null || theme.GetType() == typeof(Theme)) {
			RefreshEverything();
		}
		time = Time.realtimeSinceStartup;
		
		
		UpdateRects();
		
		if (textEditor) {
			textEditor.VarifyTextEditor();
			textEditor.Update();
		}
		
		projectView.Update();
		
		SettingsMenu.Update();
		
		RCMenu.Update();
		
		if (filesDraggedToOpen.Length > 0) {
			for (int i = 0; i < filesDraggedToOpen.Length; i++) {
				OpenOrFocusEditorFromFile(filesDraggedToOpen[i]);
			}
			filesDraggedToOpen = new String[0];
		}
		
		UIDEThreadPool.currentTime = time;
		UIDEThreadPool.Update();
		
	}
	
	public void OnGUI() {
		for (int i = 0; i < textEditors.Count; i++) {
			if (textEditors[i] == null || textEditors[i].undoManager == null) {
				needsReinit = true;
			}
		}
		if (needsReinit) {
			return;
		}
		
		if (theme == null || theme.GetType() == typeof(Theme)) {
			RefreshEverything();
		}
		UpdateRects();
		SetDefaultSkin(GUI.skin);
		
		
		if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform) {
			DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.AcceptDrag();
				string[] dropPaths = DragAndDrop.paths;
				List<string> files = new List<string>();
				string[] supportedTypes = generalSettings.GetSupportedFileTypes();
				for (int i = 0; i < dropPaths.Length; i++) {
					string ext = Path.GetExtension(dropPaths[i]).ToLower().Replace(".","");
					if (supportedTypes.Contains(ext)) {
						files.Add(dropPaths[i]);
					}
				}
				filesDraggedToOpen = files.ToArray();
			}
		}
		
		
		mousePos = Event.current.mousePosition;
		
		DrawTextEditorGUI(-1);
		
		
		GUI.skin = defaultSkin;
		
		GUIStyle bgStyle = skin.GetStyle("BoxBG");
		GUI.Box(projectView.actualRect,"",bgStyle);
		
		
		GUIStyle shadowStyle = skin.GetStyle("ShadowVerticalFromLeft");
		if (shadowStyle != null) {
			Rect r = projectView.actualRect;
			r.x = r.width;
			r.width = 0;
			GUI.Box(r,"",shadowStyle);
		}
		
		GUI.Window(-4,projectView.actualRect,DrawProjectViewGUI,"",new GUIStyle());
		if (projectView.actualRect.Contains(Event.current.mousePosition)) {
			GUI.BringWindowToFront(-4);
			if (Event.current.type == EventType.MouseDown) {
				GUI.FocusWindow(-4);
			}
		}
		
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.T && (Event.current.control || Event.current.command)) {
			GUI.FocusWindow(-4);
			GUI.FocusControl("UIDE_ProjectViewFileSearch");
			Repaint();
		}
		
		//GUI.FocusWindow(previousFocus);
		/*
		GUI.BeginGroup(projectView.actualRect);
		projectView.OnGUI();
		SetDirty(projectView);
		GUI.EndGroup();
		*/
		
		GUI.skin = defaultSkin;
		
		SettingsMenu.OnGUI();
		
		GUI.skin = defaultSkin;
		
		DrawRCMenu();
		
		GUI.skin = defaultSkin;
		
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W && (Event.current.control || Event.current.command)) {
			if (textEditor) {
				CloseTextEditor(textEditor);
			}
			Repaint();
		}
	}
	
	private void DrawRCMenu() {
		RCMenu.OnGUI();
	}
	
	private void DrawProjectViewGUI(int windowID) {
		projectView.OnGUI();
		SetDirty(projectView);
	}
	
	private void DrawTextEditorGUI(int windowID) {
		if (textEditor == null) return;
		GUI.BeginGroup(textEditor.rect);
		
		textEditor.VarifyTextEditor();
		textEditor.OnGUI();
		SetDirty(textEditor);
		if (textEditor) {
			SetDirty(textEditor.undoManager);
		}
		if (textEditor == null) {
			GUI.EndGroup();
			return;
		}
		
		if (EditorApplication.currentScene == "") {
			Rect warningRect = textEditor.rect;
			warningRect.x = warningRect.width-200-GUI.skin.verticalScrollbar.fixedWidth;
			warningRect.y = textEditor.desiredTabBarHeight;
			warningRect.width = 200;
			
			GUIStyle warningStyle = new GUIStyle(EditorStyles.toolbarButton);
			warningStyle.fixedHeight = 0;
			warningStyle.fontStyle = FontStyle.Bold;
			warningStyle.wordWrap = true;
			warningStyle.border = new RectOffset(4,4,4,4);
			warningStyle.padding = new RectOffset(4,4,4,4);
			
			string warningText = "Warning: No scene is currently loaded. To be able to use the [[HOTKEY]] hotkey to save scripts, you must be loaded into an existing saved scene. Alternately you can use [[ALTHOTKEY]] to save.";
			if (Application.platform == RuntimePlatform.OSXEditor) {
				warningText = warningText.Replace("[[HOTKEY]]","Command+S");
				warningText = warningText.Replace("[[ALTHOTKEY]]","Control+Alt+S");
			}
			else {
				warningText = warningText.Replace("[[HOTKEY]]","Ctrl+S");
				warningText = warningText.Replace("[[ALTHOTKEY]]","Ctrl+Alt+S");
			}
			
			GUIContent wanringContent = new GUIContent(warningText);
			warningRect.height = warningStyle.CalcHeight(wanringContent,warningRect.width);
			
			GUI.Label(warningRect,wanringContent,warningStyle);
		}
		
		//if (Application.loadedLevelName)
		
		GUI.EndGroup();
	}
	
	static public GUIStyle CreateDummyWindowStyle() {
		GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
		windowStyle.padding = new RectOffset(0,0,0,0);
		//windowStyle.border = new RectOffset(0,0,0,0);
		windowStyle.margin = new RectOffset(0,0,0,0);
		return windowStyle;
	}
	
	static public int focusedWindow {
		get {
			System.Reflection.FieldInfo field =
			typeof(GUI).GetField("focusedWindow",
			System.Reflection.BindingFlags.NonPublic |
			System.Reflection.BindingFlags.Static);
			
			return (int)field.GetValue(null);
		}
	}
	
	static public UnityEngine.Object LoadAsset(string path) {
		return(LoadAsset(path,false));
	}
	static public UnityEngine.Object LoadAsset(string path, bool quiet) {
		if (!quiet && !File.Exists(path)) {
			Debug.LogWarning("Tried to open non-existant file \""+path+"\"");
			return null;
		}
		#if UNITY_EDITOR
		return (UnityEngine.Object)AssetDatabase.LoadMainAssetAtPath(path);
		#endif
	}
	
	public void Repaint() {
		wantsRepaint = true;
	}
	
	
	static public void SetDirty(UnityEngine.Object obj) {
		SetDirty(obj,false);
	}
	static public void SetDirty(UnityEngine.Object obj, bool log) {
		if (obj == null) {
			return;
		}
		#if UNITY_EDITOR
		if (log) {
			Debug.Log("Setting "+obj+" dirty.");
		}
		EditorUtility.SetDirty(obj);
		#endif
	}
}