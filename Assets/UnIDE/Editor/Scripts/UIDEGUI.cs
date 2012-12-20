using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

static public class UIDEGUI:System.Object {
	static private Texture2D _whiteTex;
	static public Texture2D whiteTex {
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
	
	static public GUIStyle MakeOnStyle(GUIStyle style) {
		GUIStyle newStyle = new GUIStyle(style);
		newStyle.normal = style.onNormal;
		newStyle.active = style.onActive;
		newStyle.hover = style.onHover;
		newStyle.active = style.onActive;
		return newStyle;
	}
	
	static public int LayoutIntField(int value) {
		return LayoutIntField(value, GUI.skin.textField);
	}
	static public int LayoutIntField(int value, GUIStyle style, params GUILayoutOption[] options) {
		float height = style.CalcHeight(new GUIContent("#YOLO"),100);
		List<GUILayoutOption> optionsList = options.ToList();
		optionsList.Add(GUILayout.Height(height));
		return EditorGUILayout.IntField(value,style,optionsList.ToArray());
	}
	
	static public string LayoutTextField(string value) {
		return LayoutTextField(value, GUI.skin.textField);
	}
	static public string LayoutTextField(string value, GUIStyle style, params GUILayoutOption[] options) {
		float height = style.CalcHeight(new GUIContent("#YOLO"),100);
		List<GUILayoutOption> optionsList = options.ToList();
		optionsList.Add(GUILayout.Height(height));
		return EditorGUILayout.TextField(value,style,optionsList.ToArray());
	}
	
	static public void ColorBox(Rect rect, Color color) {
		Color originalColor = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect,whiteTex);
		GUI.color = originalColor;
	}
	static public bool InvisibleButton(Rect rect) {
		Color originalColor = GUI.color;
		GUI.color = new Color(0,0,0,0);
		GUI.SetNextControlName("_InvisibleButton");
		bool state = GUI.Button(rect,"");
		GUI.color = originalColor;
		return state;
	}
	
	static public bool TestClick(int mouseIndex, Rect rect) {
		if (Event.current.button != mouseIndex) return false;
		return TestClick(rect);
	}
	static public bool TestClick(Rect rect) {
		if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
			return true;
		}
		return false;
	}
}
