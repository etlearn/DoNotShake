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
	public bool isLoaded = false;
	
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
		UIDEEditorWindow.Get();
	}
	
	static public UIDEEditorWindow Get() {
		UIDEEditorWindow window = (UIDEEditorWindow)EditorWindow.GetWindow(typeof(UIDEEditorWindow),false,"UnIDE");
		return window;
	}
	
	public void OnEnable() {
		if (isLoaded) return;
		this.wantsMouseMove = true;
		this.Start();
		isLoaded = true;
	}
	
	public void Start() {
		editor = new UIDEEditor();
		editor.Start();
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
		this.wantsMouseMove = true;
		if (editor == null) {
			Start();
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
			Start();
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

