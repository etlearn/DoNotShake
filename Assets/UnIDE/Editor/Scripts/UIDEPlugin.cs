using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

using UIDE;
using UIDE.RightClickMenu;

namespace UIDE {
	[System.Serializable]
	public class UIDEPlugin:System.Object {
		public UIDETextEditor editor;
		public bool excludeFromLoading = false;
		public bool useCustomWindow = false;
		public float order = 0.0f;
		
		//Called as soon as a plugin instance is created, even if its not included
		//in the current requesting UIDETextEditor.
		public virtual void OnInitialize() {
			
		}
		public virtual void Start() {
			
		}
		
		public virtual void OnDestroy() {
			
		}
		
		public virtual void OnSavePerformed() {
			
		}
		
		public virtual void OnTextEditorCanClickChanged() {
			
		}
		
		public virtual void OnChangedCursorPosition(Vector2 pos) {
			
		}
		public virtual void OnChangedCursorPosition(Vector2 pos, UIDEElement element) {
			
		}
		
		public virtual void OnPreDeleteLineRange(UIDELine line, int start, int end) {
			
		}
		
		public virtual void OnPrePaste(string text) {
			
		}
		
		public virtual void OnPostPaste(string text) {
			
		}
		
		public virtual void OnPreBackspace() {
			
		}
		
		public virtual void OnPostBackspace() {
			
		}
		
		public virtual string OnPreEnterText(string text) {
			return text;
		}
		public virtual void OnPostEnterText(string text) {
			
		}
		
		public virtual void OnPreEnterCharacter(string text) {
			
		}
		public virtual void OnPostEnterCharacter(string text) {
			
		}
		
		public virtual void OnPreTextEditorUpdate() {
			
		}
		public virtual void OnTextEditorUpdate() {
			
		}
		
		public virtual void OnPreTextEditorGUI() {
			
		}
		public virtual void OnTextEditorGUI(int windowID) {
			
		}
		
		public virtual void OnSwitchToTab() {
			
		}
		public virtual void OnSwitchToOtherTab() {
			
		}
		
		public virtual void OnRebuildLines(UIDEDoc doc) {
			
		}
		public virtual void OnRebuildLineElements(UIDELine line) {
			
		}
		
		public virtual RCMenuItem[] OnGatherRCMenuItems() {
			return new RCMenuItem[0];
		}
		
		public virtual void OnFocus() {
			
		}
		public virtual void OnLostFocus() {
			
		}
		
		public virtual bool CheckIfShouldLoad(UIDETextEditor textEditor) {
			return true;
		}
		
		
		public ClickBlocker CreateClickBlocker(Rect r) {
			ClickBlocker blocker = new ClickBlocker();
			blocker.rect = r;
			blocker.owner = this;
			return blocker;
		}
		//Statics
		static public UIDEPlugin[] GetPluginInstances(UIDETextEditor textEditor) {
			List<Type> types = new List<Type>();
			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()){
				foreach (Type type in asm.GetTypes()){
					if (type != typeof(UIDEPlugin) && type != typeof(UIDE.SyntaxRules.SyntaxRule) && typeof(UIDEPlugin).IsAssignableFrom(type)) {
						types.Add(type);
					}
				}
			}
			List<UIDEPlugin> instances = new List<UIDEPlugin>();
			for (int i = 0; i < types.Count; i++) {
				UIDEPlugin instance = (UIDEPlugin)Activator.CreateInstance(types[i]);
				
				if (!instance.excludeFromLoading && instance.CheckIfShouldLoad(textEditor)) {
					instances.Add(instance);
				}
				
			}
			return instances.ToArray();
		}
	}
}
