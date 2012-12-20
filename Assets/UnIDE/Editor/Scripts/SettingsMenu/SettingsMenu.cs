using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;


namespace UIDE.SettingsMenu {
	static public class SettingsMenu:System.Object {
		static public UIDEEditor editor {
			get {
				return UIDEEditor.current;
			}
		}
		static public Theme theme {
			get {
				return editor.theme;
			}
		}
		
		[SerializeField]
		static private bool _enabled = false;
		static public bool enabled {
			get {
				return _enabled;
			}
		}
		
		static public SettingsGroup selectedGroup {
			get {
				if (selectedGroupIndex < 0 || selectedGroupIndex >= groups.Count) {
					selectedGroupIndex = 0;
					return null;
				}
				return groups[selectedGroupIndex];
			}
		}
		
		static public float windowPadding = 16.0f;
		static public Vector2 desiredSize = new Vector2(600,400);
		
		static private int selectedGroupIndex = 0;
		static private Rect rect;
		static private Vector2 groupListScroll;
		
		static public List<SettingsGroup> groups = new List<SettingsGroup>();
		static private List<Type> pluginTypes = new List<Type>();
		static private Dictionary<Type,MethodInfo> pluginTypeToGUIMethodDict = new Dictionary<Type,MethodInfo>();
		static private List<MethodInfo> pluginGUIMethods = new List<MethodInfo>();
		
		
		static public void Initialize() {
			UpdateGroupInstances();
			UpdatePluginGUIMethods();
		}
		
		static public void Show() {
			if (_enabled) return;
			_enabled = true;
			if (groups != null && selectedGroupIndex < groups.Count && groups[selectedGroupIndex] != null) {
				groups[selectedGroupIndex].OnSwitchTo();
			}
		}
		static public void Hide() {
			if (!_enabled) return;
			_enabled = false;
			if (groups != null && selectedGroupIndex < groups.Count && groups[selectedGroupIndex] != null) {
				groups[selectedGroupIndex].OnSwitchFrom();
			}
		}
		
		static public void Update() {
			for (int i = 0; i < groups.Count; i++) {
				groups[i].Update();
			}
			if (enabled) {
				for (int i = 0; i < groups.Count; i++) {
					groups[i].ActiveUpdate();
				}
			}
		}
		
		static public void OnGUI() {
			if (!enabled) return;
			
			GUISkin originalSkin = GUI.skin;
			GUI.skin = theme.skin;
			
			
			UpdateRect();
			
			GUIStyle shadowStyle = theme.GetStyle("DropShadow");
			GUI.Box(rect,"",shadowStyle);
			
			
			Rect bgButtonRect = editor.rect;
			bgButtonRect.x = 0;
			bgButtonRect.y = 0;
			
			//Block clicks to whatever is behind the window.
			GUI.color = new Color(0,0,0,0);
			GUI.Window(-3,bgButtonRect,DrawDummyClickBlocker,"");
			GUI.color = new Color(1,1,1,1);
			GUI.BringWindowToFront(-3);
			GUI.FocusWindow(-3);
			
			GUI.color = new Color(0,0,0,0);
			GUI.Window(-2,rect,DrawWindow,"");
			GUI.color = new Color(1,1,1,1);
			GUI.BringWindowToFront(-2);
			GUI.FocusWindow(-2);
			
			GUI.skin = originalSkin;
		}
		
		static private void DrawWindow(int windowID) {
			GUI.color = new Color(1,1,1,1);
			Rect windowRect = rect;
			
			GUIStyle boxStyle = editor.theme.GetStyle("PopupWindowBackground");
			GUI.Box(new Rect(0,0,rect.width,rect.height),"",boxStyle);
			
			windowRect.width -= windowPadding*2;
			windowRect.height -= windowPadding*2;
			windowRect.x = windowPadding;
			windowRect.y = windowPadding;
			
			Texture2D dividerTex = editor.theme.GetResourceTexture("PopupWindowDividerRight");
			if (dividerTex != null) {
				Rect r = rect;
				r.y = 2;
				r.height -= 4;
				r.x = windowRect.x+150.0f+8.0f;
				r.width = dividerTex.width;
				GUI.DrawTexture(r,dividerTex);
			}
			
			GUIStyle closeButtonStyle = editor.theme.GetStyle("CloseButton");
			
			GUILayout.BeginArea(windowRect);
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Settings...");
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("",closeButtonStyle)) {
				Hide();
			}
			GUILayout.EndHorizontal();
			
			float topPartHeight = 25;
			
			GUILayout.EndArea();
			
			Rect groupListRect = windowRect;
			groupListRect.y += topPartHeight;
			groupListRect.height -= topPartHeight;
			groupListRect.width = 150.0f;
			DrawGroupList(groupListRect);
			
			if (selectedGroup != null) {
				Rect selRect = groupListRect;
				selRect.x += selRect.width+8.0f+16.0f+16.0f;
				selRect.width = windowRect.width-selRect.x;
				DrawGroupContent(selectedGroup,selRect);
			}
			
			
			
			
			//Block clicks to whatever is behind the window.
			Rect bgButtonRect = rect;
			bgButtonRect.x = 0;
			bgButtonRect.y = 0;
			GUI.color = new Color(0,0,0,0);
			GUI.Button(bgButtonRect,"");
			GUI.color = new Color(1,1,1,1);
		}
		
		static private void DrawGroupContent(SettingsGroup group, Rect groupRect) {
			Rect zeroPosRect = groupRect;
			zeroPosRect.x = 0; zeroPosRect.y = 0;
			
			//Rect insetRect = groupRect;
			//insetRect.x -= 1; insetRect.y -= 1;
			//insetRect.width += 2; insetRect.height += 2;
			
			//GUIStyle boxBGStyle = editor.theme.GetStyle("BoxBG");
			GUIStyle insetStyle = editor.theme.GetStyle("PopupWindowInset");
			
			GUI.Box(groupRect,"",insetStyle);
			//GUI.Box(groupRect,"",boxBGStyle);
			
			GUILayout.BeginArea(groupRect);
			
			group.scroll = GUILayout.BeginScrollView(group.scroll);
			
			group.OnGUI(zeroPosRect);
			
			GUILayout.EndScrollView();
			
			GUILayout.EndArea();
		}
		
		static private void DrawGroupList(Rect listRect) {
			bool autoShrinkGroupList = true;
			
			GUIStyle itemStyle = editor.theme.GetStyle("ListItem");
			GUIStyle itemSelectedStyle = editor.theme.GetStyle("ListItemSelected");
			
			float itemHeight = itemStyle.CalcHeight(new GUIContent("#YOLO"),listRect.width);
			
			bool hasScrollbar = false;
			if (groups.Count*itemHeight > listRect.height) {
				hasScrollbar = true;
			}
			float itemWidth = listRect.width;
			if (hasScrollbar) {
				itemWidth -= GUI.skin.GetStyle("verticalscrollbar").fixedWidth;
			}
			
			if (autoShrinkGroupList) {
				if (groups.Count*itemHeight < listRect.height) {
					listRect.height = groups.Count*itemHeight;
				}
			}
			
			Rect boxRect = listRect;
			boxRect.width += 2;
			boxRect.height += 2;
			boxRect.x -= 1;
			boxRect.y -= 1;
			
			GUI.Box(listRect,"",editor.theme.GetStyle("DropShadow"));
			GUI.Box(boxRect,"");
			
			
			
			groupListScroll = GUI.BeginScrollView(listRect,groupListScroll,new Rect(0,0,itemWidth,groups.Count*itemHeight));
			for (int i = 0; i < groups.Count; i++) {
				Rect buttonRect = new Rect(0,i*itemHeight,itemWidth,itemHeight);
				GUIStyle buttonStyle = itemStyle;
				if (i == selectedGroupIndex) {
					buttonStyle = itemSelectedStyle;
				}
				if (GUI.Button(buttonRect,groups[i].title,buttonStyle)) {
					if (i != selectedGroupIndex) {
						groups[selectedGroupIndex].OnSwitchFrom();
						groups[i].OnSwitchTo();
						selectedGroupIndex = i;
					}
				}
			}
			
			if (!autoShrinkGroupList) {
				if (!hasScrollbar) {
					GUIStyle itemListEndStyle = editor.theme.GetStyle("ListItemListEnd");
					float startPoint = groups.Count*itemHeight;
					float height = listRect.height-startPoint;
					Rect r = new Rect(0,startPoint,itemWidth,height);
					GUI.Box(r,"",itemListEndStyle);
				}
			}
			GUI.EndScrollView();
		}
		
		static private void DrawDummyClickBlocker(int windowID) {
			Rect bgButtonRect = editor.rect;
			bgButtonRect.x = 0;
			bgButtonRect.y = 0;
			
			GUI.color = new Color(0,0,0,0);
			GUI.Button(bgButtonRect,"");
			GUI.color = new Color(1,1,1,1);
		}
		
		static private void UpdateRect() {
			rect = editor.rect;
			rect.width = Mathf.Min(desiredSize.x,rect.width);
			rect.height = Mathf.Min(desiredSize.y,rect.height);
			
			rect.x = editor.rect.width*0.5f;
			rect.x -= rect.width*0.5f;
			
			rect.y = editor.rect.height*0.5f;
			rect.y -= rect.height*0.5f;
		}
		
		static public T GetGroupInstance<T>() where T:SettingsGroup {
			for (int i = 0; i < groups.Count; i++) {
				if (groups[i].GetType() == typeof(T)) {
					return (T)groups[i];
				}
			}
			return null;
		}
		
		static public void UpdateGroupInstances() {
			ClearGroups();
			List<Type> types = new List<Type>();
			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()){
				foreach (Type type in asm.GetTypes()){
					if (type != typeof(SettingsGroup) && typeof(SettingsGroup).IsAssignableFrom(type)) {
						types.Add(type);
					}
				}
			}
			for (int i = 0; i < types.Count; i++) {
				SettingsGroup instance = (SettingsGroup)Activator.CreateInstance(types[i]);
				groups.Add(instance);
			}
			for (int i = 0; i < groups.Count; i++) {
				groups[i].Start();
			}
			
			groups.Sort(
				delegate(SettingsGroup item1, SettingsGroup item2) {
					if (item2.order < 0.0f) return -1;
					if (item1.order < item2.order) {
						return -1;
					}
					return 1;
				}
			);
		}
		static private void ClearGroups() {
			for (int i = 0; i < groups.Count; i++) {
				groups[i].Destroy();
			}
			groups = new List<SettingsGroup>();
		}
		
		static public void UpdatePluginGUIMethods() {
			pluginTypes = new List<Type>();
			pluginGUIMethods = new List<MethodInfo>();
			pluginTypeToGUIMethodDict = new Dictionary<Type,MethodInfo>();
			
			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()){
				foreach (Type type in asm.GetTypes()){
					if (type != typeof(UIDEPlugin) && typeof(UIDEPlugin).IsAssignableFrom(type)) {
						pluginTypes.Add(type);
					}
				}
			}
			
			for (int i = 0; i < pluginTypes.Count; i++) {
				BindingFlags flags = BindingFlags.Static|BindingFlags.Public;
				MethodInfo[] allMethods = pluginTypes[i].GetMethods(flags);
				MethodInfo foundMethod = null;
				foreach (MethodInfo m in allMethods) {
					if (m.Name != "OnSettingsMenuGUI") continue;
					ParameterInfo[] methodParams = m.GetParameters();
					if (methodParams.Length != 1) continue;
					if (methodParams[0].ParameterType != typeof(Rect)) continue;
					foundMethod = m;
					break;
				}
				pluginGUIMethods.Add(foundMethod);
				pluginTypeToGUIMethodDict.Add(pluginTypes[i],foundMethod);
			}
			
		}
		
		
	}
}
