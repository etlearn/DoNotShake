using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System;

using UIDE.CodeCompletion;
using UIDE.SyntaxRules;

using UIDE.SyntaxRules.ExpressionResolvers.CSharp;
using CUtil = UIDE.CodeCompletion.ComplitionUtility;

namespace UIDE.CodeCompletion {
	
	public class UIDENamespace:System.Object {
		public string name = "";
		public string fullName = "";
		public Dictionary<string,Type> simpleTypeLookup = new Dictionary<string, Type>();
		
		private UIDENamespace _parent;
		public UIDENamespace parent {
			get {
				return _parent;
			}
			set {
				_parent = value;
				if (_parent != null && _parent.namespaces.IndexOf(this) == -1) {
					_parent.namespaces.Add(this);
				}
			}
		}
		public List<Type> types = new List<Type>();
		public List<UIDENamespace> namespaces = new List<UIDENamespace>();
		
		public UIDENamespace FindNamespace(string ns) {
			for (int i = 0; i < namespaces.Count; i++) {
				if (namespaces[i].name == ns) return namespaces[i];
			}
			return null;
		}
		public Type FindType(string ns) {
			return FindType(ns,-1);
		}
		public Type FindType(string ns,int numberOfGenericArgs) {
			string nameToFind = ns;
			if (numberOfGenericArgs > 0) {
				nameToFind += "`"+numberOfGenericArgs;
			}
			for (int i = 0; i < types.Count; i++) {
				if (types[i].Name == nameToFind) {
					if (numberOfGenericArgs == -1 || types[i].GetGenericArguments().Length == numberOfGenericArgs) {
						return types[i];
					}
				}
			}
			return null;
		}
		public Type FindTypeFullName(string ns) {
			return FindTypeFullName(ns,-1);
		}
		public Type FindTypeFullName(string ns,int numberOfGenericArgs) {
			string nameToFind = ns;
			if (numberOfGenericArgs > 0) {
				nameToFind += "`"+numberOfGenericArgs;
			}
			for (int i = 0; i < types.Count; i++) {
				if (types[i].FullName == nameToFind) {
					if (numberOfGenericArgs == -1 || types[i].GetGenericArguments().Length == numberOfGenericArgs) {
						return types[i];
					}
				}
			}
			return null;
		}
	}
	
	public class ChainItem {
		public System.Type type;
		//public ChainItem next;
		public ChainItem enclosingParent;
		public ChainItem dotChainParent;
		public ChainItem argumentParent;
		public List<ChainItem> dotChainItems = new List<ChainItem>();
		
		public List<ChainItem> methodArguments = new List<ChainItem>();
		public List<ChainItem> bracketArguments = new List<ChainItem>();
		public List<ChainItem> genericArguments = new List<ChainItem>();
		public List<string> modifiers = new List<string>();
		public string text = "";
		public string name = "";
		public string methodArgumentBlock = "";
		public string bracketArgumentBlock = "";
		public string genericArgumentBlock = "";
		public string textWithModifiers = "";
		public bool isMethod = false;
		public bool isNamespace = false;
		public bool isStatic = false;
		public bool isIndexer = false;
		public bool isArrayConstructor = false;
		public CompletionItem completionItem;
		private bool _isConstructor = false;
		
		public List<CompletionItem> autoCompleteItems = new List<CompletionItem>();
		
		public bool isConstructor {
			get {
				return _isConstructor;
			}
			set {
				_isConstructor = value;
			}
		}
		
		public bool hasGenericArguments {
			get {
				return genericArguments.Count > 0;
			}
		}
		
		public ChainItem rootLink {
			get {
				if (dotChainItems.Count == 0) {
					return null;
				}
				return dotChainItems[0];
			}
		}
		
		public ChainItem finalLink {
			get {
				if (dotChainItems.Count == 0) {
					return null;
				}
				return dotChainItems[dotChainItems.Count-1];
			}
		}
		
		public System.Type finalLinkType {
			get {
				if (finalLink != null) {
					return finalLink.type;
				}
				return null;
			}
		}
		
		public string GetFullNamespaceName() {
			string output = "";
			if (!isNamespace) return output;
			output = text;
			if (dotChainParent != null && dotChainParent.isNamespace) {
				string parentName = dotChainParent.GetFullNamespaceName();
				output = parentName+"."+output;
			}
			return output;
		}
	}
	
	public class ChainResolver:System.Object {
		public Vector2 cursorPos;
		public UIDETextEditor editor;
		public string arrayExtensionTypeRef = "System.Collections.Generic.IEnumerable`1[[TSource]]";
		public ReflectionDB reflectionDB;
		
		
		public ChainResolver(UIDETextEditor editor, Vector2 cursorPos) {
			this.editor = editor;
			this.cursorPos = cursorPos;
			reflectionDB = new ReflectionDB(editor,cursorPos);
		}
		
		public void ResolveCurrentlyVisibleCompletionItem(CompletionItem member) {
			if (member.hasBeenResolved) return;
			if (member.resultingType == null) {
				Type t = reflectionDB.ResolveTypeFromVisible(member.fullTypeName,member.genericArguments.Count);
				
				if (t == null && member.inferredType) {
					Vector2 expStart = member.expressionEnd;
					string resolvedExpression = "";
					int c = 0;
					while (true) {
						expStart = editor.doc.GoToEndOfWhitespace(expStart,-1);
						ExpressionInfo result = new ExpressionInfo();
						result.startPosition = expStart;
						result.endPosition = expStart;
						result.initialized = true;
						resolvedExpression = ExpressionResolver.ResolveExpressionAt(expStart,-1, ref result);
						expStart = editor.doc.IncrementPosition(result.startPosition,-1);
						expStart = editor.doc.GoToEndOfWhitespace(expStart,-1);
						
						//Debug.Log(resolvedExpression);
						if (editor.doc.PositionLessThan(editor.doc.IncrementPosition(expStart,-1),member.expressionStart)) {
							break;
						}
						
						if (c > 100) {
							break;
						}
						if (!editor.doc.CanIncrementPosition(expStart,-1)) {
							break;
						}
						c++;
					}
					
					ChainItem decItem = ResolveChain(resolvedExpression,false);
					
					if (decItem != null) {
						decItem = decItem.finalLink;
						if (decItem.type != null) {
							member.chainItem = decItem;
							t = decItem.type;
						}
					}
				}
				
				if (t == null && member.isVariable && member.variableDeclarationBlock != "") {
					ChainItem decItem = ResolveChain(member.variableDeclarationBlock,false);
					decItem = decItem.finalLink;
					//Debug.Log(decItem.genericArguments[0].dotChainItems[1].type);
					
					if (decItem.type != null) {
						member.chainItem = decItem;
						t = decItem.type;
					}
				}
				if (t != null) {
					member.resultingType = t;
				}
			}
			
			if (typeof(CompletionMethod).IsAssignableFrom(member.GetType())) {
				CompletionMethod method = (CompletionMethod)member;
				
				for (int j = 0; j < method.arguments.Count; j++) {
					if (method.arguments[j].resultingType == null) {
						Type newType = reflectionDB.ResolveTypeFromVisible(method.arguments[j].fullTypeName,member.genericArguments.Count);
						if (newType != null) {
							method.arguments[j].resultingType = newType;
						}
						//Debug.Log(method.name+" "+method.arguments[j].resultingType+" "+method.arguments[j].fullTypeName);
					}
				}
				for (int j = 0; j < method.genericArguments.Count; j++) {
					if (method.genericArguments[j].resultingType == null) {
						Type newType = reflectionDB.ResolveTypeFromVisible(method.genericArguments[j].fullTypeName,member.genericArguments.Count);
						if (newType != null) {
							method.genericArguments[j].resultingType = newType;
						}
					}
				}
			}
			
			member.hasBeenResolved = true;
		}
		
		private string GetNameFromFunctionString(string input) {
			Regex regex = new Regex(@"(?<name>[A-Za-z_]+\w*)");
			Match match = regex.Match(input);
			if (match.Success) {
				return match.Groups["name"].Value;
			}
			return input;
		}
		
		private string ExtractArgumentBlock(string input,ExpressionBracketType bracketType) {
			//Debug.Log(input);
			if (input.Length < 3) {
				return "";
			}
			//The first char should always be "("
			//int counter = 0;
			int inc = 0;
			int finalIndex = 0;
			for (int i = 0; i < input.Length; i++) {
				char character = input[i];
				if (CUtil.IsOpenExpression(character,bracketType)) {
					inc++;
				}
				else if (CUtil.IsCloseExpression(character,bracketType)) {
					inc--;
				}
				if (inc <= 0) {
					finalIndex = i;
					break;
				}
			}
			//Debug.Log(input);
			if (finalIndex < 1 || finalIndex > input.Length) {
				//Debug.Log(input);
				return "";
			}
			
			string resultString = input.Substring(0+1,finalIndex-1);
			
			return resultString;
		}
		
		private string[] ExtractModifers(string input) {
			List<string> output = new List<string>();
			bool hasRootModifiers = false;
			int closestScope = input.Length-1;
			if (input.IndexOf('(') != -1) {
				closestScope = input.IndexOf('(');
			}
			int firstBarIndex = input.IndexOf('|');
			if (firstBarIndex != -1 && firstBarIndex < closestScope) {
				hasRootModifiers = true;
			}
			
			if (hasRootModifiers) {
				string[] rootModifiers = input.Substring(0,closestScope).Split("|"[0]);
				if (rootModifiers.Length > 1) {
					int trimLength = 0;
					for (int m = 0; m < rootModifiers.Length-1; m++) {
						trimLength += rootModifiers[m].Length+1;
						output.Add(rootModifiers[m]);
					}
					
				}
			}
			return output.ToArray();
		}
		
		private ChainItem ExtractFirstItem(string input) {
			if (input.Length == 0) {
				return null;
			}
			int finalIndex = 0;
			
			string methodArgumentBlock = "";
			bool hasMethodArguments = false;
			List<bool> dotChainHasMethodArguments = new List<bool>();
			List<string> dotChainMethodArgumentBlocks = new List<string>();
			
			string bracketArgumentBlock = "";
			List<bool> dotChainHasBracketArguments = new List<bool>();
			List<string> dotChainBracketArgumentBlocks = new List<string>();
			bool hasBracketArguments = false;
			
			string genericArgumentBlock = "";
			List<bool> dotChainHasGenericArguments = new List<bool>();
			List<string> dotChainGenericArgumentBlocks = new List<string>();
			bool hasGenericArguments = false;
			
			List<string> dotChain = new List<string>();
			int lastDotPos = 0;
			for (int i = 0; i < input.Length; i++) {
				char character = input[i];
				
				
				if (CUtil.IsOpenParentheses(character)) {
					methodArgumentBlock = ExtractArgumentBlock(input.Substring(i),ExpressionBracketType.Expression);
					i += methodArgumentBlock.Length+1; //Add one because the () are excluded.
					
					hasMethodArguments = true;
					finalIndex = i+0;
					continue;
				}
				
				if (CUtil.IsOpenGeneric(character)) {
					genericArgumentBlock = ExtractArgumentBlock(input.Substring(i),ExpressionBracketType.Generic);
					i += genericArgumentBlock.Length+1; //Add one because the <> are excluded.
					
					hasGenericArguments = true;
					finalIndex = i+0;
					continue;
				}
				
				
				if (character == '.' || character == '[') {
					dotChain.Add(input.Substring(lastDotPos,i-lastDotPos));
					int lastDotAdd = 1;
					if (character == '[') {
						lastDotAdd = 0;
					}
					lastDotPos = i+lastDotAdd;
					
					dotChainHasMethodArguments.Add(hasMethodArguments);
					dotChainMethodArgumentBlocks.Add(methodArgumentBlock);
					
					dotChainHasBracketArguments.Add(hasBracketArguments);
					dotChainBracketArgumentBlocks.Add(bracketArgumentBlock);
					
					dotChainHasGenericArguments.Add(hasGenericArguments);
					dotChainGenericArgumentBlocks.Add(genericArgumentBlock);
					
					hasMethodArguments = false;
					hasBracketArguments = false;
					hasGenericArguments = false;
					
					methodArgumentBlock = "";
					bracketArgumentBlock = "";
					genericArgumentBlock = "";
					
					int bracketEnd = -1;
					if (CUtil.IsOpenBracket(character)) {
						bracketArgumentBlock = ExtractArgumentBlock(input.Substring(i),ExpressionBracketType.Bracket);
						bracketEnd = i + bracketArgumentBlock.Length+1; //Add one because the [] are excluded.
						
						hasBracketArguments = true;
						finalIndex = bracketEnd+0;
						//continue;
					}
					
					if (character == '[') {
						i = bracketEnd;
					}
				}
				
				//if (character == ',') {
				if (character == ',') {
					finalIndex = i-1;
					//if (character == '[') {
					//	finalIndex += 1;
					//}
					break;
				}
				finalIndex = i;
			}
			
			dotChainHasMethodArguments.Add(hasMethodArguments);
			dotChainMethodArgumentBlocks.Add(methodArgumentBlock);
			
			dotChainHasBracketArguments.Add(hasBracketArguments);
			dotChainBracketArgumentBlocks.Add(bracketArgumentBlock);
			
			dotChainHasGenericArguments.Add(hasGenericArguments);
			dotChainGenericArgumentBlocks.Add(genericArgumentBlock);
			
			if (input.Length > 0) {
				dotChain.Add(input.Substring(lastDotPos,(finalIndex-lastDotPos)+1));
			}
			
			if (dotChain.Count == 0) return null;
			
			string nextString = input.Substring(0,finalIndex+1);
			if (nextString.Length == 0) {
				return null;
			}
			
			ChainItem item = new ChainItem();
			
			item.textWithModifiers = nextString;
			
			string[] rootModifiers = ExtractModifers(dotChain[0]);
			int modifiersLength = 0;
			foreach (string m in rootModifiers) {
				modifiersLength += m.Length+1;
			}
			
			dotChain[0] = dotChain[0].Substring(modifiersLength);
			item.text = dotChain[0];
			item.name = item.text;
			item.modifiers = rootModifiers.ToList();
			
			
			bool hasSetConstructor = false;
			item.dotChainItems = new List<ChainItem>();
			//Debug.Log(dotChain.Count);
			//for (int i = 0; i < dotChain.Count; i++) {
			//	Debug.Log(dotChain[i]);
			//}
			for (int i = 0; i < dotChain.Count; i++) {
				ChainItem dotChainParent = null;
				if (item.dotChainItems.Count > 0) {
					dotChainParent = item.dotChainItems[item.dotChainItems.Count-1];
				}
				
				ChainItem dotChainItem = new ChainItem();
				item.dotChainItems.Add(dotChainItem);
				
				dotChainItem.enclosingParent = item;
				
				
				
				string text = dotChain[i];
				
				dotChainItem.textWithModifiers = dotChain[i];
				
				dotChainItem.text = text;
				dotChainItem.name = GetNameFromFunctionString(dotChainItem.text);
				dotChainItem.dotChainParent = dotChainParent;
				
				if (!dotChainHasMethodArguments[i]) {
					if (!hasSetConstructor && item.modifiers.Contains("new")) {
						if (i < dotChain.Count-1 && dotChainHasBracketArguments[i+1]) {
							dotChainItem.isArrayConstructor = true;
							dotChainItem.isConstructor = true;
							hasSetConstructor = true;
							if (dotChainGenericArgumentBlocks[i].Length > 0) {
								dotChainItem.genericArgumentBlock = dotChainGenericArgumentBlocks[i];
								ChainItem[] argumentItems = ProcessItems(dotChainGenericArgumentBlocks[i]);
								dotChainItem.genericArguments = argumentItems.ToList();
								for (int j = 0; j < argumentItems.Length; j++) {
									argumentItems[j].argumentParent = dotChainItem;
								}
							}
							i++;
							continue;
						}
					}
				}
				
				if (dotChainHasMethodArguments[i]) {
					if (!hasSetConstructor && item.modifiers.Contains("new")) {
						//Debug.Log(dotChainItem.text);
						dotChainItem.isConstructor = true;
						hasSetConstructor = true;
					}
					dotChainItem.isMethod = true;
					//dotChainItem.name = GetNameFromFunctionString(dotChainItem.name);
					if (dotChainMethodArgumentBlocks[i].Length > 0) {
						dotChainItem.methodArgumentBlock = dotChainMethodArgumentBlocks[i];
						
						ChainItem[] argumentItems = ProcessItems(dotChainMethodArgumentBlocks[i]);
						dotChainItem.methodArguments = argumentItems.ToList();
						for (int j = 0; j < argumentItems.Length; j++) {
							argumentItems[j].argumentParent = dotChainItem;
						}
					}
				}
				
				if (dotChainHasBracketArguments[i]) {
					//Debug.Log(dotChainItem.text);
					dotChainItem.isIndexer = true;
					//dotChainItem.name = GetNameFromFunctionString(dotChainItem.name);
					if (dotChainBracketArgumentBlocks[i].Length > 0) {
						dotChainItem.bracketArgumentBlock = dotChainBracketArgumentBlocks[i];
						ChainItem[] argumentItems = ProcessItems(dotChainBracketArgumentBlocks[i]);
						dotChainItem.bracketArguments = argumentItems.ToList();
						for (int j = 0; j < argumentItems.Length; j++) {
							argumentItems[j].argumentParent = dotChainItem;
						}
					}
				}
				
				if (dotChainHasGenericArguments[i]) {
					//dotChainItem.isIndexer = true;
					//dotChainItem.name = GetNameFromFunctionString(dotChainItem.name);
					if (dotChainGenericArgumentBlocks[i].Length > 0) {
						dotChainItem.genericArgumentBlock = dotChainGenericArgumentBlocks[i];
						ChainItem[] argumentItems = ProcessItems(dotChainGenericArgumentBlocks[i]);
						dotChainItem.genericArguments = argumentItems.ToList();
						for (int j = 0; j < argumentItems.Length; j++) {
							argumentItems[j].argumentParent = dotChainItem;
						}
					}
				}
			
			}
			
			
			
			return item;
		}
		
		#region Extension method helpers
		
		
		#endregion
		
		#region Get variables
		private CompletionItem GetVariableFromVisible(ChainItem item) {
			CompletionItem variable = null;
			for (int i = 0; i < reflectionDB.currentCompletionItems.Length; i++) {
				CompletionItem member = reflectionDB.currentCompletionItems[i];
				if (!member.isVariable) {
					continue;
				}
				
				if (member.name == item.name) {
					ResolveCurrentlyVisibleCompletionItem(member);
					variable = member;
					break;
				}
			}
			
			return variable;
		}
		
		private CompletionItem GetVariableFromType(ChainItem item, Type type) {
			CompletionItem result = null;
			result = GetFieldFromType(item,type);
			if (result == null) {
				result = GetPropertyFromType(item,type);
			}
			
			return result;
		}
		
		private CompletionItem GetFieldFromType(ChainItem item, Type type) {
			if (type == null) return null;
			
			BindingFlags flags = BindingFlags.Public;
			flags |= BindingFlags.NonPublic;
			flags |= BindingFlags.Instance;
			flags |= BindingFlags.Static;
			flags |= BindingFlags.FlattenHierarchy;
			flags |= BindingFlags.Default;
			
			
			FieldInfo fieldInfo = null;
			
			fieldInfo = type.GetField(item.name,flags);
			
			if (fieldInfo != null) {
				CompletionItem returnItem = new CompletionItem(fieldInfo);
				/*
				bool isRealArray = type.IsArray;
				if (isRealArray) {
					if (fieldInfo.FieldType.ToString() == "TSource" || fieldInfo.FieldType.ToString() == "T") {
						returnItem.resultingType = type.GetElementType();
						return returnItem;
					}
					else if (fieldInfo.FieldType.ToString() == "TSource[]" || fieldInfo.FieldType.ToString() == "T[]") {
						returnItem.resultingType = type;
						return returnItem;
					}
				}
				*/
				if (fieldInfo.FieldType.IsGenericParameter) {
					ChainItem genericItem = null;
					
					if (item.dotChainParent != null) {
						Type[] generics = type.GetGenericArguments();
						int c = 0;
						if (generics.Length == item.dotChainParent.genericArguments.Count) {
							foreach (Type g in generics) {
								if (g == fieldInfo.FieldType) {
									//Debug.Log(g+" "+item.dotChainParent.genericArguments[c].type);
									genericItem = item.dotChainParent.genericArguments[c];
									break;
								}
								c++;
							}
						}
					}
					
					if (genericItem != null) {
						returnItem.chainItem = genericItem;
						returnItem.resultingType = genericItem.finalLinkType;
					}
				}
				
				return returnItem;
			}
			
			return null;
		}
		
		private CompletionItem GetPropertyFromType(ChainItem item, Type type) {
			if (type == null) return null;
			
			BindingFlags flags = BindingFlags.Public;
			flags |= BindingFlags.NonPublic;
			flags |= BindingFlags.Instance;
			flags |= BindingFlags.Static;
			flags |= BindingFlags.FlattenHierarchy;
			flags |= BindingFlags.Default;
			
			
			PropertyInfo propertyInfo = null;
			
			propertyInfo = type.GetProperty(item.name,flags);
			
			if (propertyInfo != null) {
				CompletionItem returnItem = new CompletionItem(propertyInfo);
				
				/*
				bool isRealArray = type.IsArray;
				if (isRealArray) {
					if (propertyInfo.PropertyType.ToString() == "TSource" || propertyInfo.PropertyType.ToString() == "T") {
						returnItem.resultingType = type.GetElementType();
						return returnItem;
					}
					else if (propertyInfo.PropertyType.ToString() == "TSource[]" || propertyInfo.PropertyType.ToString() == "T[]") {
						returnItem.resultingType = type;
						return returnItem;
					}
				}
				*/
				if (propertyInfo.PropertyType.IsGenericParameter) {
					ChainItem genericItem = null;
					
					if (item.dotChainParent != null) {
						Type[] generics = type.GetGenericArguments();
						int c = 0;
						if (generics.Length == item.dotChainParent.genericArguments.Count) {
							foreach (Type g in generics) {
								if (g == propertyInfo.PropertyType) {
									genericItem = item.dotChainParent.genericArguments[c];
									break;
								}
								c++;
							}
						}
					}
					
					if (genericItem != null) {
						returnItem.chainItem = genericItem;
						returnItem.resultingType = genericItem.finalLinkType;
					}
				}
				
				return returnItem;
			}
			return null;
		}
		#endregion
		
		private CompletionItem GetNestedTypeFromType(ChainItem item, Type type) {
			if (type == null) return null;
			
			BindingFlags flags = BindingFlags.Public;
			flags |= BindingFlags.NonPublic;
			flags |= BindingFlags.Instance;
			flags |= BindingFlags.Static;
			flags |= BindingFlags.FlattenHierarchy;
			flags |= BindingFlags.Default;
			
			
			Type nestedType = null;
			
			Type[] nestedTypes = type.GetNestedTypes(flags);
			for (int i = 0; i < nestedTypes.Length; i++) {
				//if (nestedTypes[i].Name != item.name) {
				//	continue;
				//}
				if (TypeMatchedConstructorItem(nestedTypes[i],item)) {
					nestedType = nestedTypes[i];
					break;
				}
			}
			
			if (nestedType != null) {
				CompletionItem returnItem = new CompletionItem(nestedType);				
				return returnItem;
			}
			
			return null;
		}
		
		private CompletionMethod GetMethodFromType(ChainItem item, Type type) {
			if (type == null) return null;
			List<Type> argumentTypes = new List<Type>();
			bool argumentNull = false;
			for (int i = 0; i < item.methodArguments.Count; i++) {
				
				if (item.methodArguments[i] == null || (item.methodArguments[i].finalLinkType == null && item.methodArguments[i].text != "null")) {
					argumentNull = true;
					//Debug.Log(item.methodArguments[i].finalLink.name+" "+item.methodArguments[i].finalLinkType);
					break;
				}
				argumentTypes.Add(item.methodArguments[i].finalLinkType);
			}
			
			if (argumentNull) {
				//Debug.Log(item.name);
				return null;
			}
			//GameObject[] objs;
			//objs.ElementAt
			
			BindingFlags flags = BindingFlags.Public;
			flags |= BindingFlags.NonPublic;
			flags |= BindingFlags.Instance;
			flags |= BindingFlags.Static;
			flags |= BindingFlags.FlattenHierarchy;
			flags |= BindingFlags.Default;
			
			
			MethodInfo methodInfo = null;
			
			//methodInfo = type.GetMethod(item.name,flags,null,argumentTypes.ToArray(),null);
			MethodInfo[] methodInfos = type.GetMethods(flags);
			methodInfo = GetBestMethodMatchingArguments(item,methodInfos,false);
			/*
			for (int i = 0; i < methodInfos.Length; i++) {
				//Debug.Log(item.name+" "+" "+methodInfos[i]);
				if (methodInfos[i].Name != item.name) {
					continue;
				}
				bool argumentsMatch = MethodArgumentsMatch(item,methodInfos[i]);
				
				if (item.hasGenericArguments) {
					argumentsMatch &= MethodGenericArgumentsMatch(item,methodInfos[i]);
				}
				if (argumentsMatch) {
					methodInfo = methodInfos[i];
					break;
				}
			}
			*/
			//Debug.Log(item.text+" "+item.genericArguments[0].type+" "+methodInfo);
			
			
			//bool isExtensionMethod = false;
			if (methodInfo == null) {
				//methodInfo = FindExtensionMethodForType(item,type);
				MethodInfo[] extMethods = GetExtensionMethodsResursive(type);
				methodInfo = GetBestMethodMatchingArguments(item,extMethods,true);
				/*
				foreach (MethodInfo m in extMethods) {
					if (m != null) {
						if (m.Name == item.name) {
							bool argumentsMatch = MethodArgumentsMatch(item,m,true);
							//Debug.Log(m+" "+argumentsMatch);
							if (item.hasGenericArguments) {
								argumentsMatch &= MethodGenericArgumentsMatch(item,m);
							}
							if (argumentsMatch) {
								//Debug.Log(item.text+" "+m);
								methodInfo = m;
								//break;
							}
						}
					}
				}
				*/
				//if (methodInfo != null) {
				//	isExtensionMethod = true;
				//}
			}
			
			if (methodInfo != null) {
				CompletionMethod returnItem = new CompletionMethod(methodInfo);
				returnItem.memberInfo = methodInfo;
				
				/*
				bool isRealArray = type.ToString().EndsWith("[]");
				if (isRealArray) {
					if (methodInfo.ReturnType.ToString() == "TSource" || methodInfo.ReturnType.ToString() == "T") {
						returnItem.resultingType = type.GetElementType();
						//return returnItem;
					}
					else if (methodInfo.ReturnType.ToString() == "TSource[]" || methodInfo.ReturnType.ToString() == "T[]") {
						returnItem.resultingType = type;
						//return returnItem;
					}
				}
				*/
				
				bool returnTypeIsGenericParameter = methodInfo.ReturnType.IsGenericParameter;
				if (!returnTypeIsGenericParameter) {
					if (methodInfo.ReturnType.IsArray) {
						returnTypeIsGenericParameter = methodInfo.ReturnType.GetElementType().IsGenericParameter;
					}
				}
				
				if (returnTypeIsGenericParameter) {
					ChainItem genericItem = null;
					
					bool returnTypeIsArray = false;
					Type[] methodGenericArgs = methodInfo.GetGenericArguments();
					if (item.genericArguments.Count > 0) {
						if (methodGenericArgs.Length == item.genericArguments.Count) {
							for (int i = 0; i < methodGenericArgs.Length; i++) {
								Type nonArrayType = methodGenericArgs[i];
								Type nonArrayReturnType = methodInfo.ReturnType;
								bool isArray = false;
								if (nonArrayType.IsArray) {
									isArray = true;
									nonArrayType = nonArrayType.GetElementType();
								}
								if (nonArrayReturnType.IsArray) {
									isArray = true;
									nonArrayReturnType = nonArrayReturnType.GetElementType();
								}
								
								if (nonArrayType == nonArrayReturnType) {
									//Debug.Log(nonArrayType+" "+item.genericArguments[i].finalLinkType);
									genericItem = item.genericArguments[i];
									returnTypeIsArray = isArray;
									break;
								}
								
							}
						}
					}
					if (genericItem == null && item.dotChainParent != null && item.dotChainParent.type != null) {
						Type parentType = item.dotChainParent.type;
						
						Type nonArrayReturnType = methodInfo.ReturnType;
						bool isArray = false;
						if (nonArrayReturnType.IsArray) {
							isArray = true;
							nonArrayReturnType = nonArrayReturnType.GetElementType();
						}
						
						Type foundType = null;
						
						if (nonArrayReturnType.DeclaringMethod != null) {
							MethodBase decMethod = nonArrayReturnType.DeclaringMethod;
							
							Type decType = nonArrayReturnType.DeclaringMethod.DeclaringType;
							
							bool isExtension = decMethod.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute),true);
							if (isExtension) {
								ParameterInfo[] parameters = decMethod.GetParameters();
								decType = parameters[0].ParameterType;
							}
							
							foundType = ResolveGenericTypes(decType,parentType,nonArrayReturnType);
						}
						else if (nonArrayReturnType.DeclaringType != null) {
							Type decType = nonArrayReturnType.DeclaringType;
							foundType = ResolveGenericTypes(decType,parentType,nonArrayReturnType);
						}
						
						if (foundType != null) {
							//Debug.Log(foundType);
							returnItem.chainItem = genericItem;
							returnItem.resultingType = foundType;
							
							if (returnItem.resultingType != null && isArray) {
								returnItem.resultingType = returnItem.resultingType.MakeArrayType();
							}
						}
					}
					
					/*
					if (genericItem == null && item.dotChainParent != null && item.dotChainParent.type != null) {
						Type parentType = item.dotChainParent.type;
						
						Type nonArrayReturnType = methodInfo.ReturnType;
						bool isArray = false;
						if (nonArrayReturnType.IsArray) {
							isArray = true;
							nonArrayReturnType = nonArrayReturnType.GetElementType();
						}
						
						Type parentTypeGenDef = parentType;
						//if (parentType.IsGenericType) {
						//	parentTypeGenDef = parentType.GetGenericTypeDefinition();
						//}
						
						List<Type> parentGenericArgs = parentType.GetGenericArguments().ToList();
						
						for (int i = 0; i < parentGenericArgs.Count; i++) {
							if (i >= item.dotChainParent.genericArguments.Count) break;
							if (parentGenericArgs[i] == nonArrayReturnType) {
								genericItem = item.dotChainParent.genericArguments[i];
								returnTypeIsArray = isArray;
								break;
							}
						}
						if (genericItem == null) {
							parentGenericArgs = new List<Type>();
							parentGenericArgs.AddRange(GatherGenericArgumentsRecursive(parentType.GetGenericTypeDefinition()));
							Type[] parentGenericArgsNonDef = GatherGenericArgumentsRecursive(parentType.GetGenericTypeDefinition());
							for (int i = 0; i < parentGenericArgs.Count; i++) {
								//if (!parentGenericArgs[i].IsGenericType) continue;
								//Type genericArgType = parentGenericArgs[i].GetGenericTypeDefinition().MakeGenericType(parentGenericArgs[i].GetGenericArguments());
								//Debug.Log(parentGenericArgs[i].Name+" "+" "+nonArrayReturnType.ContainsGenericParameters);
								Debug.Log(parentType.Name+" "+parentGenericArgs[i].Name+" "+nonArrayReturnType);
								if (parentGenericArgs[i] == nonArrayReturnType) {
									
									//returnItem.chainItem = genericItem;
									returnItem.resultingType = parentGenericArgsNonDef[i];
									returnTypeIsArray = isArray;
									if (returnItem.resultingType != null && !returnItem.resultingType.IsArray && returnTypeIsArray) {
										returnItem.resultingType = returnItem.resultingType.MakeArrayType();
									}
									
									break;
								}
							}
						}
					}
					*/
					//parentGenericArgs.AddRange(GatherGenericArgumentsRecursive(parentType));
					
					if (genericItem != null) {
						returnItem.chainItem = genericItem;
						returnItem.resultingType = genericItem.finalLinkType;
						if (returnItem.resultingType == null) {
							returnItem.resultingType = typeof(System.Object);
						}
						if (returnItem.resultingType != null && !returnItem.resultingType.IsArray && returnTypeIsArray) {
							returnItem.resultingType = returnItem.resultingType.MakeArrayType();
						}
					}
				}
				
				/*
				bool isRealArray = type.ToString().EndsWith("[]");
				if (isRealArray) {
					
					if (methodInfo.ReturnType.ToString() == "TSource" || methodInfo.ReturnType.ToString() == "T") {
						returnItem.resultingType = type.GetElementType();
						return returnItem;
					}
					else if (methodInfo.ReturnType.ToString() == "TSource[]" || methodInfo.ReturnType.ToString() == "T[]") {
						
						returnItem.resultingType = type;
						return returnItem;
					}
				}
				
				//else {
				
				if (methodInfo.ReturnType.IsGenericParameter) {
					ChainItem genericItem = null;
				
					genericItem = GetGenericMethodArgumentMatchingMethodInfoReturnType(item,methodInfo);
					
					if (genericItem != null) {
						returnItem.chainItem = genericItem;
						if (genericItem.finalLinkType != null) {
							returnItem.resultingType = genericItem.finalLinkType;
						}
						else {
							returnItem.resultingType = genericItem.type;
						}
						//Debug.Log(returnItem.resultingType);
					}
				}
				
				if (methodInfo.ReturnType.IsGenericType) {
					
					//int genericArgumentPos = methodInfo.ReturnType;
					Type declaringType = methodInfo.DeclaringType;
					if (isExtensionMethod) {
						declaringType = methodInfo.GetParameters()[0].ParameterType;
					}
					
					Type[] declaringGenericArguments = declaringType.GetGenericArguments();
					
					if (isRealArray && declaringGenericArguments.Length > 0) {
						if (declaringGenericArguments[0].Name == "TSource" || declaringGenericArguments[0].Name == "T") {
							returnItem.resultingType = methodInfo.ReturnType;
							
							ChainItem newGenArgumentItem = new ChainItem();
							newGenArgumentItem.text = declaringGenericArguments[0].Name;
							newGenArgumentItem.name = declaringGenericArguments[0].Name;
							newGenArgumentItem.argumentParent = item;
							newGenArgumentItem.genericArgumentBlock = declaringGenericArguments[0].Name;
							newGenArgumentItem.type = type.GetElementType();
							item.genericArguments.Add(newGenArgumentItem);
						}
					}
					
					returnItem.resultingType = methodInfo.ReturnType;
					ChainItem genericItem = null;
					if (genericItem != null) {
						returnItem.resultingType = genericItem.finalLinkType;
					}
				}
				*/
				return returnItem;
			}
			
			return null;
		}
		
		public Type ResolveGenericTypes(Type t1, Type t2, Type typeToMatch) {
			List<Type> enclosingTypes = GetEnclosingTypes(t1).ToList();
			enclosingTypes.Insert(0,t1);
			
			List<Type> parentEnclosingTypes = GetEnclosingTypes(t2).ToList();
			parentEnclosingTypes.Insert(0,t2);
			
			
			Type foundType = null;;
			
			for (int i = 0; i < enclosingTypes.Count; i++) {
				Type[] typeArgs = enclosingTypes[i].GetGenericArguments();
				
				int foundIndex = -1;
				for (int j = 0; j < typeArgs.Length; j++) {
					if (typeArgs[j] == typeToMatch) {
						foundIndex = j;
					}
				}
				if (foundIndex > -1) {
					for (int j = 0; j < parentEnclosingTypes.Count; j++) {
						Type parentGenType = parentEnclosingTypes[j];
						if (parentGenType.IsGenericType) {
							parentGenType = parentGenType.GetGenericTypeDefinition();
							if (parentGenType.GetGenericArguments().Length == enclosingTypes[i].GetGenericArguments().Length) {
								try {
									parentGenType = parentGenType.MakeGenericType(enclosingTypes[i].GetGenericArguments());
								}
								catch (Exception) {
									
								}
							}
						}
						
						if (parentGenType == enclosingTypes[i]) {
							Type[] foundTypeGenerics = parentEnclosingTypes[j].GetGenericArguments();
							if (foundTypeGenerics.Length > foundIndex) {
								foundType = foundTypeGenerics[foundIndex];
								break;
							}
						}
					}	
				}
			}
			return foundType;
			
		}
		
		public Type[] GetEnclosingTypes(Type type) {
			return GetEnclosingTypes(type,new List<Type>());
		}
		public Type[] GetEnclosingTypes(Type type, List<Type> completedTypes) {
			List<Type> types = new List<Type>();
			if (type == null || completedTypes.Contains(type)) {
				return types.ToArray();
			}
			if (type.BaseType != null) {
				types.AddRange(GetEnclosingTypes(type.BaseType,completedTypes));
			}
			
			Type[] interfaces = type.GetInterfaces();
			for (int i = 0; i < interfaces.Length; i++) {
				types.AddRange(GetEnclosingTypes(interfaces[i],completedTypes));
			}
			types.Add(type);
			return types.ToArray();
		}
		
		public Type[] GatherGenericArgumentsRecursive(Type type) {
			return GatherGenericArgumentsRecursive(type,false);
		}
		public Type[] GatherGenericArgumentsRecursive(Type type, bool getGenericDefs) {
			return GatherGenericArgumentsRecursive(type,getGenericDefs,new List<Type>());
		}
		public Type[] GatherGenericArgumentsRecursive(Type type, bool getGenericDefs, List<Type> completedTypes) {
			List<Type> types = new List<Type>();
			if (type == null || completedTypes.Contains(type)) {
				return types.ToArray();
			}
			//if (type.IsGenericType) {
			//	type = type.GetGenericTypeDefinition();
			//}
			completedTypes.Add(type);
			//Debug.Log(type);
			
			Type[] genericArguments = type.GetGenericArguments();
			//if (getGenericDefs) {
			//	for (int i = 0; i < genericArguments.Length; i++) {
			//		if (!genericArguments[i].IsGenericType) continue;
			//		Debug.Log(genericArguments[i].GetGenericTypeDefinition());
			//	}
			//}
			types.AddRange(genericArguments);
			
			if (type.BaseType != null) {
				types.AddRange(GatherGenericArgumentsRecursive(type.BaseType,getGenericDefs,completedTypes));
			}
			
			Type[] interfaces = type.GetInterfaces();
			for (int i = 0; i < interfaces.Length; i++) {
				types.AddRange(GatherGenericArgumentsRecursive(interfaces[i],getGenericDefs,completedTypes));
			}
			
			return types.ToArray();
		}
		
		public Type GetMethodReturnTypeFromArrayType(MethodInfo methodInfo, Type type) {
			if (methodInfo.ReturnType.ToString() == "TSource" || methodInfo.ReturnType.ToString() == "T") {
				return type.GetElementType();
			}
			else if (methodInfo.ReturnType.ToString() == "TSource[]" || methodInfo.ReturnType.ToString() == "T[]") {
				return type;
			}
			return null;
		}
		
		public ChainItem ChainItemFromType(Type type, bool buffer) {
			ChainItem item = new ChainItem();
			item.text = "!"+type.Name+"!";
			item.name = item.text;
			item.type = type;
			
			Type[] generics = type.GetGenericArguments();
			//Debug.Log(type+" "+generics.Length);
			for (int i = 0; i < generics.Length; i++) {
				ChainItem genericArg = ChainItemFromType(generics[i],buffer);
				genericArg.argumentParent = item;
				item.genericArguments.Add(genericArg);
			}
			ChainItem actualItem = item;
			if (buffer) {
				actualItem = new ChainItem();
				actualItem.text = item.text;
				actualItem.name = item.name;
				actualItem.type = item.type;
				actualItem.dotChainItems.Add(item);
			}
			return actualItem;
		}
		
		public ChainItem GetGenericMethodArgumentMatchingMethodInfoReturnType(ChainItem item, MethodInfo method) {
			if (!method.ReturnType.IsGenericParameter) {
				Debug.LogWarning("Invalid useage of GetGenericMethodArgumentMatchingMethodInfoReturnType(). MethodInfo.ReturnType must be a generic parameter.");
				return null;
			}
			
			
			Type[] generics = method.GetGenericArguments();
			ChainItem genericArgument = null;
			
			if (method.ContainsGenericParameters && !item.hasGenericArguments) {
				if (item.dotChainParent != null && item.dotChainParent.type != null) {
					Type[] parentGenericTypes = item.dotChainParent.type.GetGenericArguments();
					
					if (parentGenericTypes.Length == generics.Length) {
						genericArgument = item.dotChainParent.genericArguments[0].finalLink;
						//Debug.Log(method);
					}
					else {
						Type[] parentInterfaces = item.dotChainParent.type.GetInterfaces();
						for (int i = 0; i < parentInterfaces.Length; i++) {
							Type pInterface = parentInterfaces[i];
							Type[] pInterfaceArgs = pInterface.GetGenericArguments();
							
							if (pInterfaceArgs.Length == generics.Length) {
								
								//genericArgument = item.dotChainParent.genericArguments[0].genericArguments[0];
								genericArgument = ChainItemFromType(pInterfaceArgs[0],true);
								break;
							}
						}
					}
				}
				
			}
			else if (item.genericArguments.Count == generics.Length) {
				genericArgument = item.genericArguments[method.ReturnType.GenericParameterPosition];
			}
			return genericArgument;
		}
		
		private CompletionItem GetIndexerFromType(ChainItem item) {
			if (item.dotChainParent == null) return null;
			
			Type type = item.dotChainParent.type;
			
			if (type == null) return null;
			
			bool isRealArray = type.IsArray;
			//
			if (isRealArray) {
				
				Type resType = type.GetElementType();
				//Debug.Log(resType.GetArrayRank());
				//if (resType.GetArrayRank() > 1) {
				//	resType = resType.MakeArrayType(resType.GetArrayRank()-1);
				//	Debug.Log(item.text+" "+resType.GetArrayRank()+" "+type.GetArrayRank());
				//}
				//else {
				//	resType = resType.GetElementType();
				//	Debug.Log("DDD "+resType+" "+resType.GetArrayRank());
				//}
				
				CompletionItem resultItem = new CompletionItem(resType);
				//if (item.dotChainParent.genericArguments.Count > 0) {
				//	item.genericArguments = item.dotChainParent.genericArguments[0].dotChainItems[0].genericArguments;
				//	resultItem.resultingType = item.dotChainParent.genericArguments[0].finalLinkType;
				//}
				if (item.dotChainParent.genericArguments.Count > 0) {
					item.genericArguments = item.dotChainParent.genericArguments;
				}
				//if (item.dotChainParent.genericArguments[0].dotChainItems.Count > 0) {
				//	item.genericArguments = item.dotChainParent.genericArguments[0].dotChainItems[0].genericArguments;
				//	resultItem.resultingType = item.dotChainParent.genericArguments[0].finalLinkType;
				//}
				//else if (item.dotChainParent.genericArguments[0].type != null) {
				//	resultItem.resultingType = item.dotChainParent.genericArguments[0].type;
				//}
				
				return resultItem;
			}
			
			//if (type.IsArray) {
			//	type = type.GetElementType();
			//}
			
			//if (type == null) return null;
			
			BindingFlags flags = BindingFlags.Public;
			flags |= BindingFlags.NonPublic;
			flags |= BindingFlags.Instance;
			flags |= BindingFlags.Static;
			flags |= BindingFlags.FlattenHierarchy;
			flags |= BindingFlags.Default;
			
			
			PropertyInfo propertyInfo = null;
			
			
			foreach (PropertyInfo pi in type.GetProperties()) {
				ParameterInfo[] parameterInfos = pi.GetIndexParameters();
				if (parameterInfos.Length == 0) continue;
				
				if (parameterInfos.Length != item.bracketArguments.Count) continue;
				bool allArgumentsMatch = true;
				for (int i = 0; i < parameterInfos.Length; i++) {
					//Debug.Log(parameterInfos[i]+" "+item.bracketArguments[i].finalLinkType);
					if (parameterInfos[i] == null) continue;
					
					bool isTypeOf = parameterInfos[i].ParameterType.IsAssignableFrom(item.bracketArguments[i].finalLinkType);
					if (!isTypeOf) {
						allArgumentsMatch = false;
						break;
					}
				}
				
				if (!allArgumentsMatch) {
					continue;
				}
				
				propertyInfo = pi;
				
			}
			
			if (propertyInfo != null) {
				CompletionItem resultItem = new CompletionItem(propertyInfo);
				//resultItem.chainItem = item;
				//Debug.Log(propertyInfo.PropertyType.Name+" "+item.text+" "+item.genericArguments.Count);
				bool isTSource = (propertyInfo.PropertyType.Name == "TSource" || propertyInfo.PropertyType.Name == "T");
				
				if (isTSource && item.dotChainParent.genericArguments.Count > 0) {
					
					if (item.dotChainParent.genericArguments[0].dotChainItems.Count > 0) {
						item.genericArguments = item.dotChainParent.genericArguments[0].dotChainItems[0].genericArguments;
						resultItem.resultingType = item.dotChainParent.genericArguments[0].finalLinkType;
					}
					else if (item.dotChainParent.genericArguments[0].type != null) {
						resultItem.resultingType = item.dotChainParent.genericArguments[0].type;
						//Debug.Log(item.dotChainParent.genericArguments[0].type);
					}
				}
				
				return resultItem;
			}
			return null;
		}
		
		private bool TypeMatchedConstructorItem(Type type,ChainItem item) {
			if (item.name != type.Name) {
				return false;
			}
			Type[] typeGenericArgs = type.GetGenericArguments();
			
			if (item.genericArguments.Count != typeGenericArgs.Length) {
				return false;
				//for (int i = 0; i < typeGenericArgs.Length; i++) {
				//	if (typeGenericArgs[i] != item.genericArguments[i].finalLinkType) {
				//		return false;
				//	}
				//}
			}
			//else {
			//	return false;
			//}
			return true;
		}
		
		private CompletionMethod GetMethodFromCurrentType(ChainItem item) {
			CompletionMethod method = null;
			
			if (reflectionDB.currentType != null) {
				//Use reflection if the item has generics.
				if (item.hasGenericArguments) {
					return GetMethodFromType(item,reflectionDB.currentType);
				}
			}
			
			//Type genericReturnTypeOverride = null;
			List<CompletionMethod> typeMethods = new List<CompletionMethod>();
			for (int i = 0; i < reflectionDB.currentCompletionItems.Length; i++) {
				CompletionItem member = reflectionDB.currentCompletionItems[i];
				if (member.name == item.name) {
					ResolveCurrentlyVisibleCompletionItem(member);
				}
				if (!typeof(CompletionMethod).IsAssignableFrom(member.GetType())) {
					continue;
				}
				CompletionMethod m = (CompletionMethod)member;
				typeMethods.Add(m);
			}
			
			method = GetBestMethodMatchingArguments(item,typeMethods.ToArray());
			
			/*
			for (int i = 0; i < reflectionDB.currentCompletionItems.Length; i++) {
				CompletionItem member = reflectionDB.currentCompletionItems[i];
				if (member.name == item.name) {
					ResolveCurrentlyVisibleCompletionItem(member);
				}
				
				if (!typeof(CompletionMethod).IsAssignableFrom(member.GetType())) {
					continue;
				}
				
				CompletionMethod m = (CompletionMethod)member;
				
				if (m.name == item.name && m.arguments.Count == item.methodArguments.Count) {
					bool argumentsMatch = MethodArgumentsMatch(item,m);
					if (argumentsMatch) {
						method = m;
						break;
					}
				}
			}
			*/
			if (method != null) {
				//method.memberInfo = method;
				return method;
			}
			
			if (reflectionDB.currentType != null && method == null) {
				method = GetMethodFromType(item,reflectionDB.currentType);
			}
			
			return method;
		}
		
		#region Method argument matching
		private MethodInfo GetBestMethodMatchingArguments(ChainItem item, MethodInfo[] methodInfos, bool isExtension) {
			
			MethodInfo methodInfo = null;
			for (int i = 0; i < methodInfos.Length; i++) {
				if (methodInfos[i].Name != item.name) {
					continue;
				}
				
				bool argumentsMatch = MethodArgumentsMatch(item,methodInfos[i],isExtension,false,false);
				
				if (item.hasGenericArguments) {
					argumentsMatch &= MethodGenericArgumentsMatch(item,methodInfos[i]);
				}
				if (argumentsMatch) {
					methodInfo = methodInfos[i];
					break;
				}
			}
			if (methodInfo != null) {
				return methodInfo;
			}
			for (int i = 0; i < methodInfos.Length; i++) {
				if (methodInfos[i].Name != item.name) {
					continue;
				}
				bool argumentsMatch = MethodArgumentsMatch(item,methodInfos[i],isExtension,true,false);
				
				if (item.hasGenericArguments) {
					argumentsMatch &= MethodGenericArgumentsMatch(item,methodInfos[i]);
				}
				if (argumentsMatch) {
					methodInfo = methodInfos[i];
					break;
				}
			}
			if (methodInfo != null) {
				return methodInfo;
			}
			for (int i = 0; i < methodInfos.Length; i++) {
				if (methodInfos[i].Name != item.name) {
					continue;
				}
				bool argumentsMatch = MethodArgumentsMatch(item,methodInfos[i],isExtension,true,true);
				
				if (item.hasGenericArguments) {
					argumentsMatch &= MethodGenericArgumentsMatch(item,methodInfos[i]);
				}
				if (argumentsMatch) {
					methodInfo = methodInfos[i];
					break;
				}
			}
			
			return methodInfo;
		}
		
		private bool MethodArgumentsMatch(ChainItem item, MethodInfo methodInfo, bool isExtension, bool allowDownCast, bool allowImplicitCast) {
			int matchingArgumentCount = 0;
			ParameterInfo[] methodInfoArguments = methodInfo.GetParameters();
			
			int miPlus = 0;
			if (isExtension) {
				miPlus = 1;
			}
			
			if (methodInfoArguments.Length != item.methodArguments.Count+miPlus) {
				return false;
			}
			//if (item.hasGenericArguments != methodInfo.ContainsGenericParameters) {
			//	return false;
			//}
			
			for (int a = 0; a < item.methodArguments.Count; a++) {
				
				if (item.methodArguments[a].finalLinkType == null && item.methodArguments[a].text != "null") {
					continue;
				}
				
				Type argTypeToMatch = methodInfoArguments[a+miPlus].ParameterType;
				
				//Dont seem to need this anymore
				/*
				if (argTypeToMatch.IsGenericType || argTypeToMatch.IsGenericParameter) {
					Type declaringType = methodInfo.DeclaringType;
					
					if (item.dotChainParent != null) {
						Type[] generics = declaringType.GetGenericArguments();
						if (argTypeToMatch.IsGenericParameter) {
							if (generics.Length == item.dotChainParent.genericArguments.Count && argTypeToMatch.GenericParameterPosition < generics.Length) {
								argTypeToMatch = item.dotChainParent.genericArguments[argTypeToMatch.GenericParameterPosition].finalLinkType;
							}
						}
						else {
							if (generics.Length == item.dotChainParent.genericArguments.Count) {
								List<Type> genericTypes = new List<Type>();
								for (int c = 0; c < generics.Length; c++) {
									
									genericTypes.Add(item.dotChainParent.genericArguments[c].finalLinkType);
								}
								argTypeToMatch = argTypeToMatch.GetGenericTypeDefinition().MakeGenericType(genericTypes.ToArray());
							}
						}
					}
				}
				*/
				
				//Debug.Log(item.methodArguments[a].finalLinkType+" "+argTypeToMatch);
				if (argTypeToMatch.IsByRef) {
					argTypeToMatch = argTypeToMatch.GetElementType();
				}
				bool isTypeOf = false;
				if (item.methodArguments[a].finalLinkType != null) {
					if (!allowDownCast) {
						isTypeOf = argTypeToMatch == item.methodArguments[a].finalLinkType;
					}
					else {
						isTypeOf = argTypeToMatch.IsAssignableFrom(item.methodArguments[a].finalLinkType);
					}
					if (allowImplicitCast) {
						isTypeOf |= HasConversionOperator(item.methodArguments[a].finalLinkType,argTypeToMatch);
					}
				}
				else {
					isTypeOf |= !argTypeToMatch.IsValueType && item.methodArguments[a].text == "null";
				}
				if (isTypeOf) {
					matchingArgumentCount++;
				}
				
			}
			if (matchingArgumentCount == item.methodArguments.Count) {
				return true;
			}
			return false;
		}
		
		private CompletionMethod GetBestMethodMatchingArguments(ChainItem item, CompletionMethod[] methodInfos) {
			
			CompletionMethod methodInfo = null;
			for (int i = 0; i < methodInfos.Length; i++) {
				CompletionMethod member = methodInfos[i];
				if (member.name == item.name) {
					ResolveCurrentlyVisibleCompletionItem(member);
				}
			}
			
			for (int i = 0; i < methodInfos.Length; i++) {
				CompletionMethod member = methodInfos[i];
				if (member.name == item.name) {
					bool argumentsMatch = MethodArgumentsMatch(item,member,false,false);
					if (argumentsMatch) {
						methodInfo = member;
						break;
					}
				}
			}
			if (methodInfo != null) {
				return methodInfo;
			}
			
			for (int i = 0; i < methodInfos.Length; i++) {
				CompletionMethod member = methodInfos[i];
				if (member.name == item.name) {
					bool argumentsMatch = MethodArgumentsMatch(item,member,true,false);
					if (argumentsMatch) {
						methodInfo = member;
						break;
					}
				}
			}
			if (methodInfo != null) {
				return methodInfo;
			}
			
			for (int i = 0; i < methodInfos.Length; i++) {
				CompletionMethod member = methodInfos[i];
				if (member.name == item.name) {
					bool argumentsMatch = MethodArgumentsMatch(item,member,true,true);
					if (argumentsMatch) {
						methodInfo = member;
						break;
					}
				}
			}
			return methodInfo;
		}
		private bool MethodArgumentsMatch(ChainItem item, CompletionMethod method, bool allowDownCast, bool allowImplicitCast) {
			
			if (method.arguments.Count == item.methodArguments.Count) {
				
				int matchingArgumentCount = 0;
				for (int a = 0; a < item.methodArguments.Count; a++) {
					Type argTypeToMatch = method.arguments[a].resultingType;
					if (method.arguments[a].resultingType == null || (item.methodArguments[a].finalLinkType == null && item.methodArguments[a].text != "null")) {
						continue;
					}
					
					//Debug.Log(item.methodArguments[a].finalLinkType+" "+argTypeToMatch);
					if (argTypeToMatch.IsByRef) {
						argTypeToMatch = argTypeToMatch.GetElementType();
					}
					bool isTypeOf = false;
					if (item.methodArguments[a].finalLinkType != null) {
						if (!allowDownCast) {
							isTypeOf = argTypeToMatch == item.methodArguments[a].finalLinkType;
						}
						else {
							isTypeOf = argTypeToMatch.IsAssignableFrom(item.methodArguments[a].finalLinkType);
						}
						if (allowImplicitCast) {
							isTypeOf |= HasConversionOperator(item.methodArguments[a].finalLinkType,argTypeToMatch);
						}
					}
					else {
						isTypeOf |= !argTypeToMatch.IsValueType && item.methodArguments[a].text == "null";
					}
					if (isTypeOf) {
						//Debug.Log(argTypeToMatch+" "+item.methodArguments[a].finalLinkType);
						matchingArgumentCount++;
					}
				}
				if (matchingArgumentCount == item.methodArguments.Count) {
					return true;
				}
			}
			return false;
		}
		
		private bool MethodGenericArgumentsMatch(ChainItem item, CompletionMethod method) {
			if (method.genericArguments.Count == item.genericArguments.Count) {
				return true;
			}
			return false;
		}
		private bool MethodGenericArgumentsMatch(ChainItem item, MethodInfo method) {
			Type[] methodGenerics = method.GetGenericArguments();
			if (methodGenerics.Length == item.genericArguments.Count) {
				return true;
			}
			return false;
		}
		#endregion
		
		
		public static bool HasConversionOperator(System.Type fromType, System.Type toType) {
			if (fromType.ContainsGenericParameters || toType.ContainsGenericParameters) {
				return false;
			}
			System.Func<System.Linq.Expressions.Expression, System.Linq.Expressions.UnaryExpression> bodyFunction = body => System.Linq.Expressions.Expression.Convert( body, toType );
			System.Linq.Expressions.ParameterExpression inp = System.Linq.Expressions.Expression.Parameter( fromType, "inp" );
			try {
				// If this succeeds then we can cast 'from' type to 'to' type using implicit coercion
				System.Linq.Expressions.Expression.Lambda( bodyFunction( inp ), inp ).Compile();
				return true;
			}
			catch( InvalidOperationException ) {
				return false;
			}
			
		}
		
		
		
		private void ResolveItem(ChainItem item) {
			if (item.methodArguments.Count > 0) {
				for (int i = 0; i < item.methodArguments.Count; i++) {
					ResolveItem(item.methodArguments[i]);
					//Debug.Log("ARGUMENT "+item.text+" "+item.methodArguments[i].text+" "+item.methodArguments[i].finalLinkType);
				}
			}
			
			if (item.bracketArguments.Count > 0) {
				for (int i = 0; i < item.bracketArguments.Count; i++) {
					ResolveItem(item.bracketArguments[i]);
					//Debug.Log("ARGUMENT "+item.text+" "+item.methodArguments[i].text+" "+item.methodArguments[i].finalLinkType);
				}
			}
			
			if (item.genericArguments.Count > 0) {
				for (int i = 0; i < item.genericArguments.Count; i++) {
					ResolveItem(item.genericArguments[i]);
					//Debug.Log("ARGUMENT "+item.text+" "+item.methodArguments[i].text+" "+item.methodArguments[i].finalLinkType);
				}
			}
			
			bool isArrayTypeIndicator = false;
			
			Type primitiveType = ComplitionUtility.GetTypeFromTypeAlias(item.name);
			
			if (primitiveType != null) {
				//Its a primitive type
				item.type = primitiveType;
				if (item.isConstructor) {
					item.type = reflectionDB.ResolveTypeFromVisible(item.name,item.genericArguments.Count);
					if (item.type == null) {
						CompletionItem nestedType = GetNestedTypeFromType(item,reflectionDB.currentType);
						if (nestedType != null) {
							item.type = nestedType.resultingType;
						}
					}
				}
				item.isStatic = true;
				if (item.isConstructor) {
					item.isStatic = false;
				}
			}
			
			if (item.isConstructor) {
				if (item.dotChainParent != null) {
					if (item.dotChainParent.isNamespace) {
						
						string fullParentName = item.dotChainParent.GetFullNamespaceName();
						
						UIDENamespace parentNS = (UIDENamespace)reflectionDB.fullNamespaceHash.Get(fullParentName);
						if (parentNS != null) {
							//TypeMatchedConstructorItem
							Type type = parentNS.FindType(item.name);
							for (int i = 0; i < parentNS.types.Count; i++) {
								if (TypeMatchedConstructorItem(parentNS.types[i],item)) {
									type = parentNS.types[i];
									break;
								}
							}
							
							if (type != null) {
								item.type = type;
								item.isStatic = false;
							}
						}
					}
					else if (item.dotChainParent.type != null) {
						CompletionItem nestedType = GetNestedTypeFromType(item,item.dotChainParent.type);
						if (nestedType != null) {
							item.type = nestedType.resultingType;
						}
					}
				}
				else {
					item.type = reflectionDB.ResolveTypeFromVisible(item.name,item.genericArguments.Count);
					if (item.type == null) {
						CompletionItem nestedType = GetNestedTypeFromType(item,reflectionDB.currentType);
						if (nestedType != null) {
							item.type = nestedType.resultingType;
						}
					}
				}
				item.isStatic = false;
			}
			else if (item.dotChainParent != null && item.text == "[]") {
				item.type = item.dotChainParent.type;
				int rank = 1;
				if (item.type.IsArray) {
					rank += item.type.GetArrayRank();
				}
				item.type = item.type.MakeArrayType(rank);
				isArrayTypeIndicator = true;
			}
			else if (item.isMethod && item.name == "typeof" && item.methodArguments.Count == 1) {
				item.type = typeof(System.Type);
			}
			else if (!item.isMethod && item.text == "this") {
				item.type = reflectionDB.currentType;
			}
			else if (!item.isMethod && item.text == "base") {
				item.type = reflectionDB.currentBaseType;
			}
			else {
				//Its something else
				
				if (item.dotChainParent == null) {
					//This should be the first item in the chain.
					if (item.isMethod) {
						//The function has to be in the current type.
						
						CompletionMethod method = GetMethodFromCurrentType(item);
						//Debug.Log(item.text+" "+method);
						if (method != null) {
							item.completionItem = method;
							item.type = method.resultingType;
						}
					}
					else {
						//This could be a namespace, type from included namespace, sub type from current type, variable in current type
						CompletionItem variable = GetVariableFromVisible(item);
						if (variable != null) {
							item.completionItem = variable;
							//Debug.Log(variable.name);
							if (variable.chainItem != null && item.genericArguments.Count == 0) {
								item.genericArguments = variable.chainItem.genericArguments;
							}
							//item = variable.chainItem;
							item.type = variable.resultingType;
						}
						else {
							variable = GetVariableFromType(item,reflectionDB.currentType);
							if (variable != null) {
								item.completionItem = variable;
								item.type = variable.resultingType;
							}
						}
						
						if (item.type == null) {
							CompletionItem nestedType = GetNestedTypeFromType(item,reflectionDB.currentType);
							if (nestedType != null) {
								item.completionItem = nestedType;
								item.type = nestedType.resultingType;
								item.isStatic = true;
							}
						}
						
						if (item.type == null) {
							Type type = reflectionDB.ResolveTypeFromVisible(item.name,item.genericArguments.Count);
							//Debug.Log(item.name+" "+item.genericArguments.Count+" "+type);
							if (type != null) {
								//Its a type in the visible namespaces.
								item.type = type;
								item.isStatic = true;
							}
							else {
								//Check if its a namespace.
								string overrideName = "";
								UIDENamespace ns = (UIDENamespace)reflectionDB.fullNamespaceHash.Get(item.text);
								if (ns == null) {
									for (int i = 0; i < reflectionDB.currentNamespaceChain.Length; i++) {
										ns = (UIDENamespace)reflectionDB.fullNamespaceHash.Get(reflectionDB.currentNamespaceChain[i]);
										if (ns != null) {
											UIDENamespace embeddedNamespace = ns.FindNamespace(item.text);
											if (embeddedNamespace != null) {
												overrideName = embeddedNamespace.fullName;
												break;
											}
										}
									}
								}
								if (ns != null) {
									//Its a namespace
									if (overrideName != "") {
										item.name = overrideName;
										item.text = overrideName;
									}
									item.isNamespace = true;
									item.isStatic = true;
								}
							}
						}
					}
				}
				else {
					//Not the first item in a chain.
					ChainItem parent = item.dotChainParent;
					
					if (item.isMethod) {
						//The function is in another type.
						//if (parent.genericArguments.Count > 0 && item.genericArguments.Count == 0) {
							//Debug.Log(item.text+" "+parent.genericArguments[1].type);
						//	item.genericArguments = parent.genericArguments;
						//}
						CompletionMethod method = GetMethodFromType(item,parent.type);
						if (method != null) {
							if (method.chainItem != null && item.genericArguments.Count == 0) {
								item.genericArguments = method.chainItem.genericArguments;
							}
							item.completionItem = method;
							item.type = method.resultingType;
						}
						
					}
					else {
						if (parent.isNamespace) {
							string fullParentName = parent.GetFullNamespaceName();
							
							//UIDE.Plugins.SyntaxRules.
							
							UIDENamespace parentNS = (UIDENamespace)reflectionDB.fullNamespaceHash.Get(fullParentName);
							
							//Debug.Log(parentNS.fullName+" "+item.text);
							if (parentNS != null) {
								
								Type type = parentNS.FindType(item.name,item.genericArguments.Count);
								
								if (type != null) {
									item.type = type;
									item.isStatic = true;
								}
								else {
									UIDENamespace subNS = parentNS.FindNamespace(item.text);
									if (subNS != null) {
										item.isNamespace = true;
										item.isStatic = true;
									}
								}
							}
						}
						else {
							CompletionItem variable = GetVariableFromType(item,parent.type);
							if (variable != null) {
								
								if (variable.chainItem != null && item.genericArguments.Count == 0) {
									item.genericArguments = variable.chainItem.genericArguments;
								}
								item.completionItem = variable;
								item.type = variable.resultingType;
							}
							
							if (item.type == null) {
								CompletionItem nestedType = GetNestedTypeFromType(item,parent.type);
								if (nestedType != null) {
									item.completionItem = nestedType;
									item.type = nestedType.resultingType;
								}
							}
						}
					}
					
					//if (item.genericArguments.Count == 0) {
					//	item.genericArguments = parent.genericArguments;
					//}
				}
			}
			
			
			//If this item has an indexer use the indexers return type instead.
			if (!isArrayTypeIndicator && item.isIndexer && item.dotChainParent != null && item.dotChainParent.type != null) {
				CompletionItem indexer = GetIndexerFromType(item);
				//Debug.Log(item.dotChainParent.type+" "+item.text+" "+indexer.resultingType);
				if (indexer != null) {
					//Debug.Log(item.text+" "+indexer.resultingType+" "+item.dotChainParent.text);
					
					item.type = indexer.resultingType;
				}
			}
			
			//Fill in generics for type
			
			if (item.type != null && item.hasGenericArguments) {
				Type[] generics = item.type.GetGenericArguments();
				//Debug.Log(item.text+" "+item.type+" "+generics.Length);
				if (generics.Length == item.genericArguments.Count) {
					List<Type> genericTypes = new List<Type>();
					bool aTypeIsNull = false;
					for (int c = 0; c < generics.Length; c++) {
						if (item.genericArguments[c].finalLinkType == null) {
							aTypeIsNull = true;
							break;
						}
						genericTypes.Add(item.genericArguments[c].finalLinkType);
					}
					if (!aTypeIsNull) {
						item.type = item.type.GetGenericTypeDefinition().MakeGenericType(genericTypes.ToArray());
					}
				}
			}
			
			if (item.type != null && item.isArrayConstructor) {
				item.type = item.type.MakeArrayType();
			}
			
			//Debug.Log(item.text+" "+item.isIndexer);
			//Debug.Log(item.text+" "+item.type+" "+item.genericArguments.Count+" "+item.isArrayConstructor);
			//if (item.type != null) {
			//	Debug.Log(item.type.IsArray);
			//}
			
			
			for (int i = 0; i < item.dotChainItems.Count; i++) {
				ResolveItem(item.dotChainItems[i]);
			}
			
		}
		
		public CompletionItem[] GetCurrentlyVisibleGlobalItems() {
			List<CompletionItem> items = new List<CompletionItem>();
			//Debug.Log(visibleNamespaces.Length);
			//Debug.Log(typeof(UIDE.Plugins.AutoComplete.AutoComplete).Namespace);
			
			for (int i = 0; i < reflectionDB.visibleNamespaces.Length; i++) {
				UIDENamespace ns = (UIDENamespace)reflectionDB.fullNamespaceHash.Get(reflectionDB.visibleNamespaces[i]);
				if (ns == null) continue;
				
				foreach (Type t in ns.types) {
					CompletionItem typeItem = new CompletionItem(t);
					items.Add(typeItem);
				}
				//foreach (UIDENamespace subNS in ns.namespaces) {
				//	CompletionItem subNSItem = new CompletionItem(subNS);
				//	items.Add(subNSItem);
				//}
				
				CompletionItem namespaceItem = new CompletionItem(ns);
				items.Add(namespaceItem);
			}
			
			for (int i = 0; i < reflectionDB.currentNamespaceChain.Length; i++) {
				UIDENamespace ns = (UIDENamespace)reflectionDB.fullNamespaceHash.Get(reflectionDB.currentNamespaceChain[i]);
				if (ns == null) continue;
				
				foreach (Type t in ns.types) {
					CompletionItem typeItem = new CompletionItem(t);
					items.Add(typeItem);
				}
				foreach (UIDENamespace subNS in ns.namespaces) {
					CompletionItem subNSItem = new CompletionItem(subNS);
					items.Add(subNSItem);
				}
				CompletionItem namespaceItem = new CompletionItem(ns);
				items.Add(namespaceItem);
			}
			
			foreach (Type t in reflectionDB.globalNamespace.types) {
				CompletionItem typeItem = new CompletionItem(t);
				items.Add(typeItem);
			}
			foreach (UIDENamespace ns in reflectionDB.allTopLevelNamespaces) {
				CompletionItem typeItem = new CompletionItem(ns);
				items.Add(typeItem);
			}
			
			return items.ToArray();
		}
		
		public void PupulateItemAutoCompleteItems(ChainItem item) {
			item.autoCompleteItems = new List<CompletionItem>();
			
			if (item.finalLink == null) {
				return;
			}
			
			//Debug.Log(item.finalLink.GetFullNamespaceName());
			if (item.finalLink.isNamespace) {
				//Debug.Log(item.name);
				UIDENamespace ns = (UIDENamespace)reflectionDB.fullNamespaceHash.Get(item.finalLink.GetFullNamespaceName());
				if (ns != null) {
					//Debug.Log(ns.fullName);
					foreach (Type t in ns.types) {
						CompletionItem typeItem = new CompletionItem(t);
						item.autoCompleteItems.Add(typeItem);
					}
					
					foreach (UIDENamespace n in ns.namespaces) {
						CompletionItem namespaceItem = new CompletionItem(n);
						item.autoCompleteItems.Add(namespaceItem);
					}
				}
				return;
			}
			
			if (item.finalLinkType == null) {
				return;
			}
			
			if (item.finalLinkType.IsEnum) {
				List<string> foundItems = new List<string>();
				for (int i = 0; i < reflectionDB.currentCompletionItems.Length; i++) {
					if (!reflectionDB.currentCompletionItems[i].isEnum) continue;
					if (reflectionDB.currentCompletionItems[i].name == item.text) {
						Type resolvedType = reflectionDB.currentCompletionItems[i].resultingType;
						if (resolvedType == null) {
							resolvedType = reflectionDB.ResolveTypeFromVisible(reflectionDB.currentCompletionItems[i].name);
						}
						if (resolvedType == item.finalLinkType) {
							for (int j = 0; j < reflectionDB.currentCompletionItems[i].enumItems.Count; j++) {
								CompletionItem currentEnumItem = reflectionDB.currentCompletionItems[i].enumItems[j];								
								CompletionItem enumItem = new CompletionItem(item.finalLinkType);
								enumItem.type = CompletionItemType.EnumItem;
								enumItem.name = currentEnumItem.name;
								item.autoCompleteItems.Add(enumItem);
								foundItems.Add(currentEnumItem.name);
								
							}
						}
					}
				}
				
				string[] enumNames = System.Enum.GetNames(item.finalLinkType);
				for (int i = 0; i < enumNames.Length; i++) {
					if (foundItems.Contains(enumNames[i])) continue;
					CompletionItem enumItem = new CompletionItem(item.finalLinkType);
					enumItem.type = CompletionItemType.EnumItem;
					enumItem.name = enumNames[i];
					item.autoCompleteItems.Add(enumItem);
				}
				
				return;
			}
			
			BindingFlags flags = GetBindingFlags(item);
			
			PupulateItemAutoCompleteItemFromType(item.autoCompleteItems, item.finalLinkType, flags);
			
			Type[] interfaces = item.finalLinkType.GetInterfaces();
			
			for (int i = 0; i < interfaces.Length; i++) {
				PupulateItemAutoCompleteItemFromType(item.autoCompleteItems, interfaces[i], flags);
			}
			//Dictionary<string,bool> dict;
			//dict.Values.
		}
		
		private void PupulateItemAutoCompleteItemFromType(List<CompletionItem> items, Type type, BindingFlags flags) {
			MethodInfo[] methodInfos = type.GetMethods(flags);
			foreach (MethodInfo methodInfo in methodInfos) {
				if (methodInfo != null) {
					if (methodInfo.IsSpecialName) {
						continue;
					}
					//if (methodInfo.IsSpecialName && (methodInfo.Name.StartsWith("set_") || methodInfo.Name.StartsWith("get_"))) {
					//	continue;
					//}
					items.Add(new CompletionMethod(methodInfo));
				}
			}
			FieldInfo[] fieldInfos = type.GetFields(flags);
			foreach (FieldInfo fieldInfo in fieldInfos) {
				if (fieldInfo != null) {
					if (fieldInfo.IsSpecialName) {
						continue;
					}
					items.Add(new CompletionItem(fieldInfo));
				}
			}
			PropertyInfo[] propertyInfos = type.GetProperties(flags);
			foreach (PropertyInfo propertyInfo in propertyInfos) {
				if (propertyInfo != null) {
					if (propertyInfo.IsSpecialName) {
						continue;
					}
					items.Add(new CompletionItem(propertyInfo));
				}
			}
			
			Type[] nestedTypes = type.GetNestedTypes(flags);
			foreach (Type nestedType in nestedTypes) {
				if (nestedType != null) {
					if (nestedType.IsSpecialName) {
						continue;
					}
					items.Add(new CompletionItem(nestedType));
				}
			}
			
			MethodInfo[] extMethods = GetExtensionMethodsResursive(type);
			foreach (MethodInfo methodInfo in extMethods) {
				if (methodInfo != null) {
					if (methodInfo.IsSpecialName) {
						continue;
					}
					items.Add(new CompletionMethod(methodInfo));
				}
			}
			
		}
		
		private MethodInfo[] GetExtensionMethodsResursive(Type type) {
			return GetExtensionMethodsResursive(type, new List<Type>());
		}
		private MethodInfo[] GetExtensionMethodsResursive(Type type, List<Type> completedTypes) {
			List<MethodInfo> methodInfos = new List<MethodInfo>();
			if (type == null) {
				return methodInfos.ToArray();
			}
			//return methodInfos.ToArray();
			if (completedTypes.Contains(type)) {
				return methodInfos.ToArray();
			}
			//completedTypes.Add(type);
			//System.Linq.Enumerable
			//System.Collections.Generic.IEnumerable`1[[TSource]]
			
    		
			bool isArray = type.IsArray;
			//Extension Methods
			//string typeNameToLookFor = type.FullName;
			Type typeToLookFor = type;
			if (type.IsGenericType){
				typeToLookFor = type.GetGenericTypeDefinition();
				//typeNameToLookFor = type.GetGenericTypeDefinition().FullName;
			}
			//else {
			//	typeNameToLookFor = type.ToString();
			//}
			if (isArray) {
				while (type.IsArray) {
					type = type.GetElementType();
				}
				type = type.MakeArrayType();
				
				//typeNameToLookFor = arrayExtensionTypeRef;
				//typeToLookFor = typeof(System.Array);
				//typeNameToLookFor = "System.Array";
				//Debug.Log(typeNameToLookFor);
			}
			
			//Debug.Log(type);
			//foreach (var t in typeof(UIDELine[]).GetInterfaces()) {
				//Debug.Log(t);
			//}
			//Debug.Log(typeToLookFor+" "+(typeToLookFor == typeof(System.Collections.Generic.IEnumerable<>)));
			//
			List<MethodInfo> extensionMethods = null;
			reflectionDB.extensionMethodDict.TryGetValue(typeToLookFor, out extensionMethods);
			//List<MethodInfo> extensionMethods = (List<MethodInfo>)reflectionDB.extensionMethodHash.Get(typeNameToLookFor);
			//Debug.Log(typeNameToLookFor+" "+extensionMethods);
			if (extensionMethods != null) {
				foreach (MethodInfo extMethod in extensionMethods) {
					//if (extMethod.GetParameters()[0].ParameterType == typeof(System.Collections.Generic.IEnumerable<>)) {
					//	Debug.Log(typeToLookFor+" "+extMethod);
					//}
					//if (MethodArgumentsMatch(item,extMethod,true)) {
						methodInfos.Add(extMethod);
					//}
				}
			}
			if (type.BaseType != null) {
				//Debug.Log(type.BaseType);
				MethodInfo[] baseMethods = GetExtensionMethodsResursive(type.BaseType,completedTypes);
				for (int i = 0; i < baseMethods.Length; i++) {
					if (!methodInfos.Contains(baseMethods[i])) {
						methodInfos.Add(baseMethods[i]);
					}
				}
			}
			
			Type[] interfaces = type.GetInterfaces();
			
			for (int i = 0; i < interfaces.Length; i++) {
				//Debug.Log(type+" "+interfaces[i]);
				MethodInfo[] interfaceMethods = GetExtensionMethodsResursive(interfaces[i],completedTypes);
				for (int j = 0; j < interfaceMethods.Length; j++) {
					if (!methodInfos.Contains(interfaceMethods[j])) {
						methodInfos.Add(interfaceMethods[j]);
					}
				}
				//methodInfos.AddRange(GetExtensionMethodsResursive(interfaces[i]));
			}
			
			return methodInfos.ToArray();
		}
		
		public CompletionMethod[] GetConstructors(Type type) {
			List<CompletionMethod> items = new List<CompletionMethod>();
			ConstructorInfo[] constructors = type.GetConstructors();
			for (int i = 0; i < constructors.Length; i++) {
				items.Add(new CompletionMethod(constructors[i]));
			}
			return items.ToArray();
		}
		
		public CompletionMethod[] GetMethodOverloads(Type type, string name, bool showStatic) {
			if (type == null) {
				return new CompletionMethod[0];
			}
			bool showPrivate = ShouldShowPrivate(type);
			bool showInstance = !showStatic;
			if (showPrivate) {
				showStatic = true;
				showInstance = true;
			}
			BindingFlags flags = GetBindingFlags(showStatic,showInstance,showPrivate);
			
			List<CompletionMethod> items = new List<CompletionMethod>();
			
			Type reflectionType = type;
			
			if (reflectionType != null) {
				MethodInfo[] methodInfos = reflectionType.GetMethods(flags);
				foreach (MethodInfo methodInfo in methodInfos) {
					if (methodInfo != null) {
						if (methodInfo.IsSpecialName) {
							continue;
						}
						if (methodInfo.Name == name) {
							items.Add(new CompletionMethod(methodInfo));
						}
					}
				}
			}
			
			MethodInfo[] extensionMethods = GetExtensionMethodsResursive(type);
			for (int i = 0; i < extensionMethods.Length; i++) {
				if (extensionMethods[i].IsSpecialName) {
					continue;
				}
				if (extensionMethods[i].Name == name) {
					items.Add(new CompletionMethod(extensionMethods[i]));
				}
			}
			
			return items.ToArray();
		}
		
		public BindingFlags GetBindingFlags(ChainItem item) {
			bool showPrivate = ShouldShowPrivate(item.finalLinkType);
			return GetBindingFlags(item.finalLink.isStatic,!item.finalLink.isStatic,showPrivate);
		}
		
		public BindingFlags GetBindingFlags(bool showStatic, bool showInstance, bool showPrivate) {
			BindingFlags flags = BindingFlags.Default;
			flags |= BindingFlags.Public;
			flags |= BindingFlags.FlattenHierarchy;
			if (showInstance) {
				flags |= BindingFlags.Instance;
			}
			if (showStatic) {
				flags |= BindingFlags.Static;
			}
			if (showPrivate) {
				flags |= BindingFlags.NonPublic;
			}
			return flags;
		}
		
		public bool ShouldShowPrivate(Type refType) {
			bool showPrivate = false;
			if (refType == null) {
				return showPrivate;
			}
			if (reflectionDB.currentType != null) {
				if (refType.IsAssignableFrom(reflectionDB.currentType)) {
					showPrivate = true;
				}
				Type tmpCurrentTypeParent = reflectionDB.currentType.DeclaringType;
				while (true) {
					if (tmpCurrentTypeParent == null) {
						break;
					}
					if (refType.IsAssignableFrom(tmpCurrentTypeParent)) {
						showPrivate = true;
						break;
					}
					tmpCurrentTypeParent = tmpCurrentTypeParent.DeclaringType;
				}
			}
			return showPrivate;
		}
		
		private ChainItem[] ProcessItems(string input) {
			string workingString = input;
			List<ChainItem> items = new List<ChainItem>();
			while (true) {
				if (workingString.Length > 0 && workingString[0] == ',') {
					workingString = workingString.Substring(1);
				}
				
				ChainItem nextItem = ExtractFirstItem(workingString);
				if (nextItem != null) {
					items.Add(nextItem);
				}
				else {
					break;
				}
				if (nextItem.text.Length >= workingString.Length) {
					break;
				}
				int lengthAdd = 1;
				if (nextItem.isConstructor) {
					//Debug.Log(nextItem.text);
					lengthAdd = 0;
				}
				if (nextItem.textWithModifiers.Length+lengthAdd >= workingString.Length) {
					break;
				}
				workingString = workingString.Substring(nextItem.textWithModifiers.Length+lengthAdd);
				
				
			}
			
			return items.ToArray();
		}
		
		public ChainItem ResolveChain(string input) {
			return ResolveChain(input,true);
		}
		public ChainItem ResolveChain(string input, bool populateFinalItem) {
			ChainItem[] items = ProcessItems(input);
			
			ChainItem finalItem = null;
			
			for (int i = 0; i < items.Length; i++) {
				ResolveItem(items[i]);
				finalItem = items[i];
				
			}
			//Debug.Log(finalItem.finalLink.methodArguments.Count);
			//for (int i = 0; i < finalItem.finalLink.methodArguments.Count; i++) {
				//Debug.Log(finalItem.finalLink.methodArguments[i].finalLink.text+" "+finalItem.finalLink.methodArguments[i].finalLinkType);
			//}
			
			if (populateFinalItem && finalItem != null) {
				PupulateItemAutoCompleteItems(finalItem);
			}
			
			return finalItem;
		}
		
		private void LogItem(ChainItem item, int depth) {
			string indentString = "";
			for (int i = 0; i < depth; i++) {
				indentString += "    ";
			}
			
			Debug.Log(indentString+item.text);
			
			for (int i = 0; i < item.dotChainItems.Count; i++) {
				LogItem(item.dotChainItems[i],depth+1);
			}
			
			if (item.methodArguments.Count > 0) {
				Debug.Log(indentString+"MethodArguments: "+item.methodArgumentBlock);
				for (int i = 0; i < item.methodArguments.Count; i++) {
					LogItem(item.methodArguments[i],depth+1);
				}
			}
			
		}
		
	}
}