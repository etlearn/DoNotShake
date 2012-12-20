using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

using UnityEditor;

namespace UIDE.SettingsMenu.Groups {
	public class SGTheme:SettingsGroup {
		public UIDESettingsGroupDataTheme data;
		public List<Type> themeTypes;
		public override void Start() {
			title = "Themes";
			order = 2.0f;
			data = GetOrCreateDefaultDataAsset<UIDESettingsGroupDataTheme>();
			UpdateThemeTypes();
		}
		
		
		public override void Update() {
			
		}
		
		public override void OnGUI(Rect groupRect) {
			if (data == null) {
				return;
			}
			Type originalThemeType = GetCurrentThemeType();
			
			float yPos = 0.0f;
			Type[] themes = themeTypes.ToArray();
			
			Texture2D[] themeTextures = new Texture2D[themes.Length];
			string[] themeNames = new string[themes.Length];
			Rect[] themeTexRects = new Rect[themes.Length];
			Rect[] themeButtonRects = new Rect[themes.Length];
			
			GUIStyle glassButtonStyle = editor.theme.GetStyle("GlassButton");
			GUIStyle shadowStyle = editor.theme.GetStyle("DropShadow");
			
			GUIStyle glassButtonOnStyle = UIDEGUI.MakeOnStyle(glassButtonStyle);
			
			for (int i = 0; i < themes.Length; i++) {
				string name = GetThemeTypeFriendlyName(themes[i]);
				Texture2D tex = GetThemeTypeThumbnail(themes[i]);
				
				float aspect = 0.25f;
				if (tex) {
					aspect = (float)tex.height/(float)tex.width;
				}
				
				yPos += glassButtonStyle.margin.top;
				
				Rect rect = new Rect(glassButtonStyle.margin.left,yPos,groupRect.width-(glassButtonStyle.margin.left+glassButtonStyle.margin.right),0);
				rect.width -= GUI.skin.verticalScrollbar.fixedWidth;
				if (tex != null) {
					rect.width = tex.width;
				}
				rect.height = rect.width*aspect;
				
				Rect texRect = rect;
				texRect.x += 3;
				texRect.y += 3;
				
				GUI.Box(texRect,"",shadowStyle);
				//if (tex) {
				//	GUI.DrawTexture(texRect,tex);
				//}
				rect.width += 6;
				rect.height += 6;
				//GUI.Button(rect,name,glassButtonStyle);
				
				themeTextures[i] = tex;
				themeNames[i] = name;
				themeTexRects[i] = texRect;
				themeButtonRects[i] = rect;
				
				yPos += rect.height;
				yPos += glassButtonStyle.margin.bottom;
				
			}
			
			GUILayout.Space(Mathf.Max(yPos,groupRect.height+1));
			if (yPos <= groupRect.height) {
				scroll.y = 0.0f;
			}
			
			for (int i = 0; i < themes.Length; i++) {
				bool isSelected = originalThemeType == themes[i];
				GUIStyle style = glassButtonStyle;
				if (isSelected) {
					style = glassButtonOnStyle;
				}
				Texture2D tex = themeTextures[i];
				string name = themeNames[i];
				Rect texRect = themeTexRects[i];
				Rect rect = themeButtonRects[i];
				
				GUI.DrawTexture(texRect,tex);
				
				GUI.Button(rect,name,style);
			}
			
			if (originalThemeType != GetCurrentThemeType()) {
				UIDEEditor.SetDirty(data);
			}
			
			if (GUI.changed) {
				UIDEEditor.SetDirty(data);
			}
		}
		
		public Type GetCurrentThemeType() {
			if (data == null) return GetTypeFromIDName("UIDE.Themes.ThemeSlateDark");
			if (data.currentThemeTypeName == "" || data.currentThemeTypeName == null) {
				return GetTypeFromIDName(data.defaultThemeTypeName);
			}
			Type t = GetTypeFromIDName(data.currentThemeTypeName);
			if (t == null) {
				data.currentThemeTypeName = data.defaultThemeTypeName;
				t = GetTypeFromIDName(data.currentThemeTypeName);
				UIDEEditor.SetDirty(data);
			}
			return t;
		}
		
		public void SetCurrentThemeType(Type type) {
			if (data == null) return;
			data.currentThemeTypeName = GetTypeIDName(type);
			UIDEEditor.SetDirty(data);
		}
		
		private string GetTypeIDName(Type type) {
			string str = type.Namespace;
			if (str != "") {
				str += ".";
			}
			str += type.Name;
			return str;
		}
		
		private Type GetTypeFromIDName(string typeName) {
			Type[] types = themeTypes.ToArray();
			for (int i = 0; i < types.Length; i++) {
				if (GetTypeIDName(types[i]) == typeName) {
					return types[i];
				}
			}
			return null;
		}
		
		public Type[] UpdateThemeTypes() {
			themeTypes = new List<Type>();
			
			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()){
				foreach (Type type in asm.GetTypes()){
					if (type != typeof(Theme) && typeof(Theme).IsAssignableFrom(type)) {
						themeTypes.Add(type);
					}
				}
			}
			return themeTypes.ToArray();
		}
		
		public string GetThemeTypeFriendlyName(Type type) {
			BindingFlags flags = BindingFlags.Static|BindingFlags.Public;
			MethodInfo[] allMethods = type.GetMethods(flags);
			MethodInfo foundMethod = null;
			foreach (MethodInfo m in allMethods) {
				if (m.Name != "GetThemeFriendlyName") continue;
				if (m.ReturnType != typeof(string)) continue;
				ParameterInfo[] methodParams = m.GetParameters();
				if (methodParams.Length != 0) continue;
				foundMethod = m;
				break;
			}
			if (foundMethod != null) {
				System.Object result = foundMethod.Invoke(null,null);
				if (result.GetType() != typeof(string)) return type.Name;
				return (string)result;
			}
			return type.Name;
		}
		
		public Texture2D GetThemeTypeThumbnail(Type type) {
			BindingFlags flags = BindingFlags.Static|BindingFlags.Public;
			MethodInfo[] allMethods = type.GetMethods(flags);
			MethodInfo foundMethod = null;
			foreach (MethodInfo m in allMethods) {
				if (m.Name != "GetThemeThumbnail") continue;
				if (m.ReturnType != typeof(Texture2D)) continue;
				ParameterInfo[] methodParams = m.GetParameters();
				if (methodParams.Length != 0) continue;
				foundMethod = m;
				break;
			}
			if (foundMethod != null) {
				System.Object result = foundMethod.Invoke(null,null);
				if (result.GetType() != typeof(Texture2D)) return null;
				return (Texture2D)result;
			}
			return null;
		}
		
	}
}
