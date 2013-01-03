using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System;
using System.IO;

using UnityEditor;

//TODO:

//Editor Features:
//Double click drag selects elements instead of characters.
//File backup on save.

//UI:
//Implement simple field/property animation system.
//Add more stuff to ProjectView.
//	Recent files dropdown?
//	Plugin icon tray at the bottom?
//Make ProjectView have AutoCollapse option. (nice smooth animation!).

public class UIDEEditorWindow:EditorWindow {
	//private Vector2 mousePos;
	//private Vector2 lastMousePos;
	static public EditorWindow lastFocusedWindow = null;
	static public HashSet<Type> toggleableWindowTypes = new HashSet<Type>(new Type[] {typeof(SceneView),System.Type.GetType("UnityEditor.GameView,UnityEditor")});
	public bool isLoaded = false;
	
	[SerializeField]
	private UIDEEditor _editor;
	public UIDEEditor editor {
		get {
			return _editor;
		}
		set {
			_editor = value;
			_editor.rect = position;
			UIDEEditor.current = _editor;
			Repaint();
		}
	}
	
	[MenuItem ("Window/UnIDE %e")]
	static public void Init() {
		if (EditorWindow.focusedWindow != null && toggleableWindowTypes.Contains(EditorWindow.focusedWindow.GetType())) {
			lastFocusedWindow = EditorWindow.focusedWindow;
		}
		
		if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == typeof(UIDEEditorWindow) && lastFocusedWindow != null) {
			EditorWindow.FocusWindowIfItsOpen(lastFocusedWindow.GetType());
		}
		else {
			UIDEEditorWindow.Get();
		}
	}
	
	static public UIDEEditorWindow Get() {
		UIDEEditorWindow window = (UIDEEditorWindow)EditorWindow.GetWindow(typeof(UIDEEditorWindow),false,"UnIDE");
		return window;
	}
	
	public void EditorApplicationUpdate() {
		if (EditorWindow.focusedWindow != null && toggleableWindowTypes.Contains(EditorWindow.focusedWindow.GetType())) {
			lastFocusedWindow = EditorWindow.focusedWindow;
		}
	}
	
	public void OnEnable() {
		EditorApplication.update -= EditorApplicationUpdate;
		EditorApplication.update += EditorApplicationUpdate;
		//Debug.Log("OnEnable "+isLoaded);
		if (isLoaded) return;
		this.autoRepaintOnSceneChange = false;
		this.wantsMouseMove = true;
		this.Start();
		isLoaded = true;
	}
	
	public void Ondisable() {
		EditorApplication.update -= EditorApplicationUpdate;
	}
	
	public void Start() {
		Start(false);
	}
	public void Start(bool isReinit) {
		editor = new UIDEEditor();
		editor.Start(isReinit);
	}
	
	public void OnRequestSave() {
		if (editor != null) {
			editor.OnRequestSave();
		}
	}
	
	public void OnReloadScript(string fileName) {
		if (editor != null) {
			editor.OnReloadScript(fileName);
		}
	}
	
	void OnFocus() {
		if (editor != null) {
			editor.OnFocus();
		}
		//Rect p = position;
		//p.width = 800;
		//p.height = 500;
		//position = p;
	}
	
	void OnLostFocus() {
		if (editor != null) {
			editor.OnLostFocus();
		}
	}
	
	bool IsFocused() {
		return EditorWindow.focusedWindow == this;
	}
	
	void Update() {
		if (EditorWindow.focusedWindow != null && toggleableWindowTypes.Contains(EditorWindow.focusedWindow.GetType())) {
			lastFocusedWindow = EditorWindow.focusedWindow;
		}
		this.wantsMouseMove = true;
		if (editor == null) {
			//Start(true);
			Close();
			return;
		}
		if (UIDEEditor.current != _editor) {
			UIDEEditor.current = _editor;
		}
		
		editor.isFocused = IsFocused();
		
		editor.rect = position;
		editor.Update();
		
		if (editor.wantsRepaint) {
			Repaint();
			editor.wantsRepaint = false;
		}
	}
	
	void OnGUI() {
		if (Event.current.type == EventType.MouseMove && IsFocused()) {
            Repaint();
			return;
		}
		
		if (editor == null) {
			//Start(true);
			Close();
			return;
		}
		
		if (UIDEEditor.current != _editor) {
			UIDEEditor.current = _editor;
		}
		
		editor.rect = position;
		BeginWindows();
		editor.OnGUI();
		EndWindows();
	}
}

