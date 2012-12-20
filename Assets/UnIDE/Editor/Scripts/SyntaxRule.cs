using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using System.IO;

using UIDE.CodeCompletion;


namespace UIDE.SyntaxRules {
	/*
	public class UIDESyntaxTreeElement:System.Object {
		public string type = "";
		public string textVaue = "";
		public List<UIDESyntaxTreeElement> children = new List<UIDESyntaxTreeElement>();
		private UIDESyntaxTreeElement _parent;
		public UIDESyntaxTreeElement parent {
			get {
				return _parent;
			}
			set {
				if (_parent != null && value == null) {
					_parent.children.Remove(this);
				}
				_parent = value;
				if (_parent != null && _parent.children.IndexOf(this) == -1) {
					_parent.children.Add(this);
				}
			}
		}
		
	}
	
	public class UIDESyntaxDoc:System.Object {
		public UIDESyntaxTreeElement rootElement = null;
	}
	*/
	
	public struct ExpressionInfo {
		public Vector2 startPosition;
		public Vector2 endPosition;
		public string text;
		public bool isFunction;
		public bool initialized;
		public ExpressionInfo Copy() {
			ExpressionInfo copy = new ExpressionInfo();
			copy.startPosition = startPosition;
			copy.endPosition = endPosition;
			copy.text = text;
			copy.isFunction = isFunction;
			copy.initialized = initialized;
			return copy;
		}
		public void CopyFrom(ExpressionInfo other) {
			startPosition = other.startPosition;
			endPosition = other.endPosition;
			text = other.text;
			isFunction = other.isFunction;
			initialized = other.initialized;
		}
	}
	public struct ExpressionResultingType {
		public System.Type type;
		public bool isStatic;
	}
	
	
	
	[System.Serializable]
	public class SyntaxRule:UIDEPlugin {
		static public SyntaxRule current;
		static public SyntaxRule[] instances;
		//static public SyntaxRule defaultRule;
		
		
		public string[] fileTypes = new string[0];
		public bool isDefault = false;
		public bool useGenericAutoComplete = false;
		public List<Action<List<UIDE.CodeCompletion.CompletionItem>>> completionUpdateFinishedCallbacks = new List<Action<List<UIDE.CodeCompletion.CompletionItem>>>();
		
		public SyntaxRule() {
			//Debug.Log(this.GetType());
			//excludeFromLoading = true;
		}
		
		public virtual bool CheckIfStringIsKeyword(string str) {
			return false;
		}
		public virtual bool CheckIfStringIsModifier(string str) {
			return false;
		}
		public virtual bool CheckIfStringIsPrimitiveType(string str) {
			return false;
		}
		public virtual UIDETokenDef GetKeywordTokenDef(UIDETokenDef tokenDef, string str) {
			return tokenDef;
		}
		
		public virtual string ResolveExpressionAt(Vector2 position, int dir) {
			return "";
		}
		public virtual System.Type GetTypeAtPosition(Vector2 position) {
			return null;
		}
		
		public virtual CompletionMethod[] GetMethodOverloads(Vector2 pos) {
			return new CompletionMethod[0];
		}
		public virtual TooltipItem GetTooltipItem(Vector2 pos) {
			return null;
		}
		
		public virtual CompletionItem[] GetChainCompletionItems() {
			return new CompletionItem[0];
		}
		public virtual CompletionItem[] GetGlobalCompletionItems() {
			return new CompletionItem[0];
		}
		
		public virtual string[] GetNamespacesVisibleInCurrentScope(Vector2 pos) {
			return new string[0];
		}
		public virtual string[] GetNamespaceChain(Vector2 pos) {
			return new string[0];
		}
		public virtual string[] GetAllVisibleNamespaces(Vector2 pos) {
			return new string[0];
		}
		
		public virtual string GetCurrentTypeFullName(Vector2 pos) {
			return "";
		}
		public virtual string GetCurrentTypeNamespace(Vector2 pos) {
			return "";
		}
		public virtual string GetCurrentTypeBaseTypeFullName(Vector2 pos) {
			return "";
		}
		public virtual string GetCurrentTypeNestedTypePath(Vector2 pos) {
			return "";
		}
		public virtual string[] GetCurrentTypeInterfaceNames(Vector2 pos) {
			return new string[0];
		}
		public virtual CompletionItem[] GetCurrentVisibleItems(Vector2 pos) {
			return new CompletionItem[0];
		}
		
		public bool HasFileType(string fileType) {
			fileType = fileType.ToLower();
			for (int i = 0; i < fileTypes.Length; i++) {
				if (fileTypes[i].ToLower() == fileType) {
					return true;
				}
			}
			return false;
		}
		
		public override bool CheckIfShouldLoad(UIDETextEditor textEditor) {
			//This is the base class and should never be included in the plugin list.
			return false;
		}
		
		/*
		static public void UpdateInstances() {
			List<Type> types = new List<Type>();
			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()){
				
				foreach (Type type in asm.GetTypes()){
					if (type != typeof(UIDESyntaxRule) && typeof(UIDESyntaxRule).IsAssignableFrom(type)) {
						types.Add(type);
					}
				}
			}
			instances = new UIDESyntaxRule[types.Count];
			for (int i = 0; i < types.Count; i++) {
				instances[i] = (UIDESyntaxRule)ScriptableObject.CreateInstance(types[i]);
				if (instances[0].isDefault) {
					defaultRule = instances[0];
				}
			}
		}
		
		static public UIDESyntaxRule GetSyntaxTypeFromFileType(string fileType) {
			UpdateInstances();
			UIDESyntaxRule rule = null;
			for (int i = 0; i < instances.Length; i++) {
				if (instances[i].HasFileType(fileType)) {
					rule = instances[i];
				}
			}
			return rule;
		}
		*/
	}
}