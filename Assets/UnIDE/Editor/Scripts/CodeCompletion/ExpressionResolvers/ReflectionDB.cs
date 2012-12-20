using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System;

using UIDE.CodeCompletion;
using UIDE.SyntaxRules;

namespace UIDE.CodeCompletion {
	public class ReflectionDB:System.Object {
		public UIDEHashTable extensionMethodHash = new UIDEHashTable();
		public Dictionary<Type,List<MethodInfo>> extensionMethodDict = new Dictionary<Type, List<MethodInfo>>();
		public UIDEHashTable fullNamespaceHash = new UIDEHashTable();
		public UIDEHashTable fullNamespaceHashLower = new UIDEHashTable();
		public UIDENamespace globalNamespace;
		
		public List<UIDENamespace> allNamespaces = new List<UIDENamespace>();
		public List<UIDENamespace> allTopLevelNamespaces = new List<UIDENamespace>();
		
		
		public string[] visibleNamespaces;
		public string[] currentNamespaceChain;
		public CompletionItem[] currentCompletionItems;
		
		public string currentTypeFullname;
		public Type currentType;
		public string currentBaseTypeFullname;
		public Type currentBaseType;
		public string currentTypeNestedTypePath;
		public string currentTypeNamespace;
		
		public string[] currentTypeInterfaceNames;
		public Type[] currentTypeInterfaces;
		
		public string arrayExtensionTypeRef = "System.Collections.Generic.IEnumerable`1[[TSource]]";
		
		public Vector2 cursorPos;
		public UIDETextEditor editor;
		
		public ReflectionDB(UIDETextEditor editor, Vector2 cursorPos) {
			this.editor = editor;
			this.cursorPos = cursorPos;
			UpdateCurrentState();
		}
		
		public string[] GetAllVisibleNamespaceNames() {
			List<string> ns = new List<string>();
			foreach (string s in visibleNamespaces) {
				if (!ns.Contains(s)) {
					ns.Add(s);
				}
			}
			foreach (string s in currentNamespaceChain) {
				if (!ns.Contains(s)) {
					ns.Add(s);
				}
			}
			return ns.ToArray();
		}
		
		public void UpdateCurrentState() {
			currentTypeNestedTypePath = editor.syntaxRule.GetCurrentTypeNestedTypePath(cursorPos);
			currentTypeNamespace = editor.syntaxRule.GetCurrentTypeNamespace(cursorPos);
			UpdateReflectionDatabase();
			
			visibleNamespaces = editor.syntaxRule.GetNamespacesVisibleInCurrentScope(cursorPos);
			currentNamespaceChain = editor.syntaxRule.GetNamespaceChain(cursorPos);
			//currentTypeFullname = editor.syntaxRule.GetCurrentTypeFullName();
			//currentType = ResolveTypeFromVisible(currentTypeFullname);
			
			currentBaseTypeFullname = editor.syntaxRule.GetCurrentTypeBaseTypeFullName(cursorPos);
			currentBaseType = ResolveTypeFromVisible(currentBaseTypeFullname);
			
			currentTypeInterfaceNames = editor.syntaxRule.GetCurrentTypeInterfaceNames(cursorPos);
			currentTypeInterfaces = new Type[currentTypeInterfaceNames.Length];
			for (int i = 0; i < currentTypeInterfaceNames.Length; i++) {
				currentTypeInterfaces[i] = ResolveTypeFromVisible(currentTypeInterfaceNames[i]);
			}
			
			currentCompletionItems = editor.syntaxRule.GetCurrentVisibleItems(cursorPos);
			
			//for (int i = 0; i < currentCompletionItems.Length; i++) {
			//	CompletionItem member = currentCompletionItems[i];
			//	ResolveCurrentlyVisibleCompletionItem(member);
			//}
		}
		
		
		public System.Type ResolveTypeFromVisible(string input) {
			return ResolveTypeFromVisible(input,-1);
		}
		public System.Type ResolveTypeFromVisible(string input, int genericArgs) {
			Type type = null;
			
			bool isRealArray = input.EndsWith("[]");
			
			if (isRealArray) {
				input = input.Substring(0,input.Length-2);
			}
			
			Type primitiveType = ComplitionUtility.GetTypeFromTypeAlias(input);
			if (primitiveType != null) {
				
				if (isRealArray) {
					return primitiveType.MakeArrayType();
				}
				return primitiveType;
			}
			
			for (int i = 0; i < visibleNamespaces.Length; i++) {
				UIDENamespace ns = (UIDENamespace)fullNamespaceHash.Get(visibleNamespaces[i]);
				if (ns == null) continue;
				
				Type t = ns.FindType(input,genericArgs);
				if (t == null) {
					Type simpleTypeLookupType = null;
					ns.simpleTypeLookup.TryGetValue(input,out simpleTypeLookupType);
					
					if (simpleTypeLookupType != null) {
						t = simpleTypeLookupType;
					}
				}
				if (t != null) {
					type = t;
					break;
				}
			}
			if (type == null) {
				type = globalNamespace.FindType(input,genericArgs);
			}
			
			if (type == null) {
				int lastDot = input.LastIndexOf('.');
				if (lastDot > 0) {
					string namespaceString = input.Substring(0,lastDot);
					UIDENamespace ns = (UIDENamespace)fullNamespaceHash.Get(namespaceString);
					if (ns != null) {
						Type t = ns.FindTypeFullName(input,genericArgs);
						if (t != null) {
							type = t;
						}
					}
				}
			}
			
			if (type == null) {
				type = globalNamespace.FindTypeFullName(input,genericArgs);
			}
			
			if (isRealArray) {
				if (type != null) {
					type = type.MakeArrayType();
				}
			}
			
			//Debug.Log(input+" "+type);
			//Debug.Log(input+" "+type);
			return type;
		}
		/*
		public MethodInfo[] GetExtensionMethodsForType(Type type) {
			List<MethodInfo> methodInfos = new List<MethodInfo>();
			
			string typeNameToLookFor = type.FullName;
			bool isRealArray = type.IsArray;
			if (isRealArray) {
				typeNameToLookFor = arrayExtensionTypeRef;
			}
			List<MethodInfo> extensionMethods = (List<MethodInfo>)extensionMethodHash.Get(typeNameToLookFor);
			
			if (extensionMethods != null) {
				methodInfos.AddRange(extensionMethods);
			}
			if (type.BaseType != null) {
				methodInfos.AddRange(GetExtensionMethodsForType(type.BaseType));
			}
			return methodInfos.ToArray();
		}
		*/
		private void UpdateReflectionDatabase() {
			fullNamespaceHash = new UIDEHashTable();
			fullNamespaceHashLower = new UIDEHashTable();
			UIDEHashTable namespaceCheckHash = new UIDEHashTable();		
			
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			
			globalNamespace = new UIDENamespace();
			globalNamespace.name = ":global";
			globalNamespace.fullName = ":global";
			
			foreach (Assembly a in assemblies) {
				Type[] types = a.GetTypes();
				foreach (Type t in types) {
					//Debug.Log(t);
					
					//bool isNested = false;
					if (t.DeclaringType != null) {
						//isNested = true;
						//if (!t.DeclaringType.IsAssignableFrom(currentType)) {
						//Debug.Log(t.FullName);
						continue;
						//}
						//Debug.Log(t+" "+t.DeclaringType);
					}
					
					bool nullNamespace = t.Namespace == null;
					
					UIDENamespace typesNamespace = null;
					if (!nullNamespace) {
						typesNamespace = (UIDENamespace)namespaceCheckHash.Get(t.Namespace);
					}
					else {
						typesNamespace = globalNamespace;
					}
					
					if (typesNamespace == null) {
						string[] namespaceParts = t.Namespace.Split('.');
						string builtFullName = "";
						UIDENamespace lastNamespace = globalNamespace;
						for (int i = 0; i < namespaceParts.Length; i++) {
							string nsPart = namespaceParts[i];
							builtFullName += nsPart;
							UIDENamespace newNamespace = lastNamespace.FindNamespace(nsPart);
							if (newNamespace == null) {
								newNamespace = new UIDENamespace();
								newNamespace.name = nsPart;
								newNamespace.fullName = builtFullName;
								newNamespace.parent = lastNamespace;
								
								fullNamespaceHash.Set(newNamespace.fullName,newNamespace);
								fullNamespaceHashLower.Set(newNamespace.fullName.ToLower(),newNamespace);
								
								//allNamespaces.Add(newNamespace);
								
								
								
								
								//if (namespaceParts.Length == 1) {
								//	allTopLevelNamespaces.Add(newNamespace);
								//}
								
							}
							
							lastNamespace = newNamespace;
							
							if (namespaceCheckHash.Get(builtFullName) == null) {
								namespaceCheckHash.Set(builtFullName,lastNamespace);
								allNamespaces.Add(lastNamespace);
								if (i == 0) {
									//Debug.Log(builtFullName+" "+lastNamespace.fullName+" "+namespaceParts.Length);
									allTopLevelNamespaces.Add(lastNamespace);
								}
							}
							
							if (i < namespaceParts.Length-1) {
								builtFullName += ".";
							}
						}
						typesNamespace = lastNamespace;
						namespaceCheckHash.Set(t.Namespace,typesNamespace);
						
					}
					typesNamespace.types.Add(t);
					//if (t == typeof(List<>)) {
						//Debug.Log(GetNameFromFunctionString(t.Name));
						
					//}
					string simpleName = GetNameFromFunctionString(t.Name);
					if (!typesNamespace.simpleTypeLookup.ContainsKey(simpleName)) {
						typesNamespace.simpleTypeLookup.Add(simpleName,t);
					}
					
					//if (t == typeof(System.Linq.Enumerable)) {
					//	MethodInfo[] methods = t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					//	foreach (MethodInfo method in methods) {
					//		Debug.Log(method);
					//	}
					//}
					//Store extension methods
					if (t.IsSealed && !t.IsGenericType && !t.IsNested) {
						
						MethodInfo[] methods = t.GetMethods(BindingFlags.Static| BindingFlags.Public | BindingFlags.NonPublic);
						foreach (MethodInfo method in methods) {
							//method.GetParameters()[0].ParameterType == t
							if (method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)) {
								Type firstParamType = method.GetParameters()[0].ParameterType;
								if (firstParamType.FullName == null) continue;
								if (firstParamType.IsGenericType) {
									firstParamType = firstParamType.GetGenericTypeDefinition();
								}
								//if (t == typeof(System.Linq.Enumerable)) {
								//	Debug.Log(firstParamType+" "+(firstParamType == typeof(System.Collections.Generic.IEnumerable<>)));
								//}
								//Debug.Log(t+" "+firstParamType.Name+" "+method);
								List<MethodInfo> typeExtensionMethods = (List<MethodInfo>)extensionMethodHash.Get(firstParamType.FullName);
								if (typeExtensionMethods == null) {
									typeExtensionMethods = new List<MethodInfo>();
									extensionMethodHash.Set(firstParamType.FullName,typeExtensionMethods);
									extensionMethodDict.Add(firstParamType,typeExtensionMethods);
								}
								//Debug.Log(firstParamType.FullName);
								typeExtensionMethods.Add(method);
								//extensionMethodDict[firstParamType].Add(method);
							}
						}
						
					}
				}
			}
			
			string[] currentNestedTypeRootParts = currentTypeNestedTypePath.Split('.');
			string currenNestedTypeRoot = currentNestedTypeRootParts[0];
			//Debug.Log(currentTypeNamespace);
			UIDENamespace currentNamespace = globalNamespace;
			if (currentTypeNamespace != "") {
				currentNamespace = (UIDENamespace)fullNamespaceHash.Get(currentTypeNamespace);
			}
			
			BindingFlags flags = BindingFlags.Public;
			flags |= BindingFlags.NonPublic;
			flags |= BindingFlags.Instance;
			flags |= BindingFlags.Static;
			flags |= BindingFlags.FlattenHierarchy;
			flags |= BindingFlags.Default;
			
			if (currentNamespace != null) {
				Type workingType = currentNamespace.FindType(currenNestedTypeRoot);
				if (workingType != null) {
					for (int i = 1; i < currentNestedTypeRootParts.Length; i++) {
						Type foundType = workingType.GetNestedType(currentNestedTypeRootParts[i],flags);
						if (foundType != null) {
							workingType = foundType;
							//Debug.Log(workingType);
						}
						else {
							break;
						}
					}
				}
				if (workingType != null) {
					currentType = workingType;
					
				}
			}
			//Debug.Log(currentType);
		}
		
		public static string GetNameFromFunctionString(string input) {
			Regex regex = new Regex(@"(?<name>[A-Za-z_]+\w*)");
			Match match = regex.Match(input);
			if (match.Success) {
				return match.Groups["name"].Value;
			}
			return input;
		}
		
		
	}
}