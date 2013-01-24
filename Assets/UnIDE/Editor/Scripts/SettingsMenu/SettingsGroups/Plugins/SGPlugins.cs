using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

//using UIDE.Plugins;

namespace UIDE.SettingsMenu.Groups {
	public class SGPlugins:SettingsGroup {
		private List<Type> pluginTypes = new List<Type>();
		private List<MethodInfo> pluginGUIMethods = new List<MethodInfo>();
		
		public UIDESettingsGroupDataPlugins data;
		public int currentPluginIndex = 0;
		
		public Type currentPluginType {
			get {
				if (currentPluginIndex < 0 || currentPluginIndex > pluginTypes.Count) {
					return null;
				}
				return pluginTypes[currentPluginIndex];
			}
		}
		private Type draggingType = null;
		private Vector2 pluginScroll;
		
		public override void Start() {
			title = "Plugins";
			order = 3.0f;
			
			data = GetOrCreateDefaultDataAsset<UIDESettingsGroupDataPlugins>();
			
			if (!data.initialized) {
				if (data.knownPlugins.Count == 0) {
					data.knownPlugins.Add("UIDE.Plugins.AutoComplete.AutoComplete");
					data.knownPluginEnableStates.Add(true);
					data.knownPlugins.Add("UIDE.Plugins.ErrorTracking.ErrorTracking");
					data.knownPluginEnableStates.Add(true);
					data.knownPlugins.Add("UIDE.Plugins.FindAndReplace.FindAndReplace");
					data.knownPluginEnableStates.Add(true);
					data.knownPlugins.Add("UIDE.Plugins.UnityAPISearch.UnityAPISearch");
					data.knownPluginEnableStates.Add(true);
					data.knownPlugins.Add("UIDE.SyntaxRules.Shared.SyntaxRuleCSharpUnityscript");
					data.knownPluginEnableStates.Add(true);
					data.knownPlugins.Add("UIDE.SyntaxRules.Generic.SyntaxRuleGeneric");
					data.knownPluginEnableStates.Add(true);
				}
				data.initialized = true;
				UIDEEditor.SetDirty(data);
			}
			
			UpdatePluginGUIMethods();
		}
		
		public override void OnSwitchTo() {
			UpdatePluginGUIMethods();
		}
		
		public override void OnGUI(Rect groupRect) {
			if (Event.current.type == EventType.MouseUp) {
				draggingType = null;
			}
			GUIStyle insetStyle = editor.theme.GetStyle("PopupWindowInset");
			GUIStyle listItemStyle = editor.theme.GetStyle("ListItem");
			GUIStyle listItemSelectedStyle = editor.theme.GetStyle("ListItemSelected");
			
			
			Vector2 itemSize = listItemStyle.CalcSize(new GUIContent("#YOLO"));
			
			Rect headerRect = groupRect;
			headerRect.width -= 20;
			headerRect.x += 10;
			headerRect.height = 100;
			headerRect.y = 10;
			
			headerRect.height = Math.Min(headerRect.height,pluginTypes.Count*itemSize.y);
			
			//GUI.Box(headerRect,"",insetStyle);
			
			Rect pluginListRect = headerRect;
			//pluginListRect.x += 10;
			//pluginListRect.width -= 20;
			//pluginListRect.y += 10;
			//pluginListRect.height -= 20;
			
			GUI.Box(pluginListRect,"",insetStyle);
			
			
			
			Rect pluginListContentRect = pluginListRect;
			pluginListContentRect.x = 0;
			pluginListContentRect.y = 0;
			pluginListContentRect.height = pluginTypes.Count*itemSize.y;
			if (pluginListContentRect.height > pluginListRect.height) {
				pluginListContentRect.width -= GUI.skin.verticalScrollbar.fixedWidth;
			}
			
			int draggingPos = -1;
			pluginScroll = GUI.BeginScrollView(pluginListRect,pluginScroll,pluginListContentRect);
			for (int i = 0; i < pluginTypes.Count; i++) {
				Rect r = new Rect(0,i*itemSize.y,pluginListContentRect.width,itemSize.y);
				
				GUIStyle style = listItemStyle;
				
				if (UIDEGUI.TestClick(0,r)) {
					currentPluginIndex = i;
					draggingType = currentPluginType;
				}
				if (draggingType != null) {
					if (draggingType == pluginTypes[i]) {
						style = listItemSelectedStyle;
					}
				}
				else {
					if (currentPluginType == pluginTypes[i]) {
						style = listItemSelectedStyle;
					}
				}
				
				
				if (draggingType != null && r.Contains(Event.current.mousePosition)) {
					draggingPos = i;
				}
				
				GUI.Button(r,pluginTypes[i].Name,style);
			}
			GUI.EndScrollView();
			
			if (draggingType != null) {
				if (Event.current.mousePosition.y < pluginListRect.y) {
					pluginScroll.y -= pluginListRect.y-Event.current.mousePosition.y;
				}
				if (Event.current.mousePosition.y > pluginListRect.y+pluginListRect.height) {
					pluginScroll.y += Event.current.mousePosition.y-(pluginListRect.y+pluginListRect.height);
				}
			}
			
			if (draggingType != null && draggingPos != -1) {
				pluginTypes.Remove(draggingType);
				draggingPos = Mathf.Clamp(draggingPos,0,pluginTypes.Count);
				pluginTypes.Insert(draggingPos,draggingType);
				currentPluginIndex = draggingPos;
				UpdateKnownPlugins();
			}
			
			GUILayout.Space(headerRect.height+headerRect.y);
			GUILayout.Space(5);
		}
		/*
		public T GetOrCreatePluginData<T>() where T:UIDEPluginData {
			if (data == null) {
				return null;
			}
			string sName = TypeToSerializableName(typeof(T));
			Debug.Log(data.pluginDatas.Count);
			for (int i = 0; i < data.pluginDatas.Count; i++) {
				if (data.pluginDatas[i] == null) continue;
				if (data.pluginDatas[i].pluginName == sName) {
					if (data.pluginDatas[i].GetType() != typeof(T)) continue;
					return (T)System.Convert.ChangeType(data.pluginDatas[i],typeof(T));
				}
			}
			
			T newData = ScriptableObject.CreateInstance<T>();
			newData.pluginName = sName;
			data.pluginDatas.Add(newData);
			UIDEEditor.SetDirty(data);
			
			return null;
		}
		*/
		public T GetOrCreatePluginData<T>() where T:UIDEPluginData {
			string sName = TypeToSerializableName(typeof(T));
			string path = dataPath+"PluginData/";
			string assetName = path+sName+".asset";
			
			T asset = GetPluginDataAsset<T>(assetName);
			
			if (asset == null) {
				if (!System.IO.Directory.Exists(path)) {
					System.IO.Directory.CreateDirectory(path);
				}
				asset = ScriptableObject.CreateInstance<T>();
				asset.pluginName = sName;
				UnityEditor.AssetDatabase.CreateAsset(asset, assetName);
				UIDEEditor.SetDirty(asset);
				UnityEditor.AssetDatabase.SaveAssets();
			}
			return asset;
		}
		
		private T GetPluginDataAsset<T>(string path) where T:UIDEPluginData {
			
			UnityEngine.Object obj = UIDEEditor.LoadAsset(path,true);
			if (obj == null) {
				return default(T);
			}
			if (typeof(T).IsAssignableFrom(obj.GetType())) {
				return (T)System.Convert.ChangeType(obj,typeof(T));
			}
			return default(T);
		}
		
		
		public Type[] GetPluginTypes() {
			return pluginTypes.ToArray();
		}
		
		public string TypeToSerializableName(Type type) {
			if (type == null) return "";
			string str = type.Namespace;
			if (str != "") str += ".";
			str += type.Name;
			str = str.TrimStart('.');
			return str;
		}
		
		public Type SerializableNameToType(string typeName, List<Type> typeList) {
			for (int i = 0; i < typeList.Count; i++) {
				if (TypeToSerializableName(typeList[i]) == typeName) {
					return typeList[i];
				}
			}
			return null;
		}
		
		public bool IsPluginEnabled(Type type) {
			if (data == null) return true;
			string sName = TypeToSerializableName(type);
			int index = data.knownPlugins.IndexOf(sName);
			
			if (index >= 0 && index <= data.knownPluginEnableStates.Count) {
				return data.knownPluginEnableStates[index];
			}
			
			return true;
		}
		
		public void UpdateKnownPlugins() {
			if (data == null) return;
			data.knownPlugins = new List<string>();
			bool[] tmpEnabledStates = data.knownPluginEnableStates.ToArray();
			data.knownPluginEnableStates = new List<bool>();
			for (int i = 0; i < pluginTypes.Count; i++) {
				data.knownPlugins.Add(TypeToSerializableName(pluginTypes[i]));
				if (i < tmpEnabledStates.Length) {
					data.knownPluginEnableStates.Add(tmpEnabledStates[i]);
				}
				else {
					data.knownPluginEnableStates.Add(true);
				}
			}
			
			UIDEEditor.SetDirty(data);
		}
		
		public void UpdatePluginGUIMethods() {
			if (data == null) return;
			
			pluginTypes = new List<Type>();
			pluginGUIMethods = new List<MethodInfo>();
			
			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()){
				foreach (Type type in asm.GetTypes()){
					if (type == typeof(UIDEPlugin)) {
						continue;
					}
					if (type == typeof(SyntaxRules.SyntaxRule)) {
						continue;
					}
					if (!typeof(UIDEPlugin).IsAssignableFrom(type)) {
						continue;
					}
					
					pluginTypes.Add(type);
				}
			}
			
			List<Type> tmpList = new List<Type>();
			for (int i = 0; i < data.knownPlugins.Count; i++) {
				Type t = SerializableNameToType(data.knownPlugins[i],pluginTypes);
				if (t != null) {
					tmpList.Add(t);
				}
			}
			for (int i = 0; i < pluginTypes.Count; i++) {
				if (!tmpList.Contains(pluginTypes[i])) {
					tmpList.Add(pluginTypes[i]);
				}
			}
			
			pluginTypes = tmpList;
			
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
			}
			
			UpdateKnownPlugins();
		}
	}
}
