using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class UIDEWindow:ScriptableObject {
	public string assetPath;
	public UIDEEditor editorWindow {
		get {
			if (UIDEEditor.current == null) {
				//Debug.LogError("UIDEEditor.current is null.");
			}
			return UIDEEditor.current;
		}
	}
	public Rect rect;
	
	static public void DirectoryDelete(string path, bool recursive) {
		System.Type type = System.Type.GetType("System.IO.Directory");
		MethodInfo methodInfo = type.GetMethod("Delete", new System.Type[] {typeof(string),typeof(bool)});
		try {
			methodInfo.Invoke(null,new object[] {path,recursive});
		}
		catch(System.Exception) {
			try {
				methodInfo.Invoke(null,new object[] {path,recursive});
			}
			catch(System.Exception) {
				
			}
		}
	}
}
