using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;
using UIDE.SyntaxRules.ExpressionResolvers;

namespace UIDE.CodeCompletion {
	
	public class TooltipItem:System.Object {
		public string text = "";
		public CompletionItem item;
		public System.Type clrType;
		
		
		public TooltipItem() {
			
		}
		
		public TooltipItem(string text) {
			this.text = text;
		}
		
		public TooltipItem(CompletionItem item) {
			this.item = item;
			this.text = item.GetSignature();
			this.clrType = item.resultingType;
		}
	}
	
	public enum CompletionItemType {Namespace,Class,Struct,Type,Enum,EnumItem,Field,Property,Method,Parameter,LocalVar,Modifier,Keyword};
	
	public enum ExpressionBracketType {Expression,Scope,Bracket,Generic};
	
	static public class ComplitionUtility {
		static public System.Type GetTypeFromTypeAlias(string alias) {
			switch (alias) {
				case "true": return typeof(System.Boolean);
				case "false": return typeof(System.Boolean);
				case "object": return typeof(System.Object);
				case "bool": return typeof(System.Boolean);
				case "char": return typeof(System.Char);
				case "decimal": return typeof(System.Decimal);
				case "double": return typeof(System.Double);
				case "float": return typeof(System.Single);
				case "int": return typeof(System.Int32);
				case "long": return typeof(System.Int64);
				case "byte": return typeof(System.Byte);
				case "sbyte": return typeof(System.SByte);
				case "string": return typeof(System.String);
				case "uint": return typeof(System.UInt32);
				case "ulong": return typeof(System.UInt64);
				case "ushort": return typeof(System.UInt16);
				//case "void": return typeof(void);
				default: return null;
			}
		}
		
		static public string TypeNameToAlias(string typeName) {
			switch (typeName) {
				case "System.Boolean": return "bool";
				case "System.Object": return "object";
				case "System.Char": return "char";
				case "System.Decimal": return "decimal";
				case "System.Double": return "double";
				case "System.Single": return "float";
				case "System.Int32": return "int";
				case "System.Int64": return "long";
				case "System.Byte": return "byte";
				case "System.SByte": return "sbyte";
				case "System.String": return "string";
				case "System.UInt32": return "uint";
				case "System.UInt64": return "ulong";
				case "System.UInt16": return "ushort";
				case "System.Void": return "void";
				default: return typeName;
			}
		}
		
		static public string CropNamespacesToShortestString(string input, string typesNamespace, string[] visibleNamespaces, string[] chainNamespaces) {
			string bestCrop = "";
			for (int i = 0; i < visibleNamespaces.Length; i++) {
				if (visibleNamespaces[i] != typesNamespace) {
					continue;
				}
				if (input.StartsWith(visibleNamespaces[i]+".")) {
					if (visibleNamespaces[i].Length > bestCrop.Length) {
						bestCrop = visibleNamespaces[i];
					}
				}
			}
			for (int i = 0; i < chainNamespaces.Length; i++) {
				//if (chainNamespaces[i] != typesNamespace) {
				//	continue;
				//}
				if (input.StartsWith(chainNamespaces[i]+".")) {
					if (chainNamespaces[i].Length > bestCrop.Length) {
						bestCrop = chainNamespaces[i];
					}
				}
			}
			/*
			if (chainNamespaces.Length > 0) {
				int lastIndex = chainNamespaces.Length-1;
				Debug.Log(input+" "+chainNamespaces[lastIndex]);
				if (input.StartsWith(chainNamespaces[lastIndex]+".")) {
					if (visibleNamespaces[lastIndex].Length > bestCrop.Length) {
						bestCrop = visibleNamespaces[lastIndex];
					}
				}
			}
			*/
			//Debug.Log(input+" "+bestCrop);
			input = input.Substring(bestCrop.Length);
			if (input.StartsWith(".")) {
				input = input.TrimStart('.');
			}
			return input;
		}
		static public string GetCleanTypeName(System.Type t) {
			if (t == null) {
				return "";
			}
			string output = "";
			output = t.Name;
			int indexOfTild = output.LastIndexOf('`');
			if (indexOfTild > 0) {
				output = output.Substring(0,indexOfTild);
			}
			return output;
		}
		
		static public string ExtractScope(string input, int startIndex, ExpressionBracketType bracketType, bool includeScope) {
			return ExtractScope(input,startIndex,1,bracketType,includeScope);
		}
		
		static public string ExtractScope(string input, int startIndex, int dir, ExpressionBracketType bracketType, bool includeScope) {
			if (input == null || input.Length == 0 || startIndex >= input.Length || !IsOpenExpression(input[startIndex],bracketType)) {
				//Debug.Log(input+" "+startIndex+" "+input[0]);
				return "";
			}
			int charCounter = startIndex;
			
			int inc = 0;
			//string output = "";
			
			while (true) {
				char character = input[charCounter];
				if (IsOpenExpression(character,bracketType)) {
					inc++;
				}
				else if (IsCloseExpression(character,bracketType)) {
					inc--;
				}
				
				int nextCharCount = charCounter+1;
				if (nextCharCount >= input.Length) {
					break;
				}
				
				charCounter++;
				
				if (inc == 0) {
					break;
				}
			}
			
			string output = "";
			
			
			output = input.Substring(startIndex,charCounter-startIndex);
			
			
			if (!includeScope && output.Length >= 2) {
				output = output.Substring(1,output.Length-2);
			}
			
			return output;
		}
		
		static public bool IsOpenExpression(char c,ExpressionBracketType type) {
			return IsOpenExpression(c,1,type);
		}
		static public bool IsCloseExpression(char c,ExpressionBracketType type) {
			return IsCloseExpression(c,1,type);
		}
		
		static public bool IsOpenExpression(char c,int dir,ExpressionBracketType type) {
			if (type == ExpressionBracketType.Expression) {
				return IsOpenParentheses(c,dir);
			}
			if (type == ExpressionBracketType.Scope) {
				return IsOpenScope(c,dir);
			}
			if (type == ExpressionBracketType.Bracket) {
				return IsOpenBracket(c,dir);
			}
			if (type == ExpressionBracketType.Generic) {
				return IsOpenGeneric(c,dir);
			}
			return false;
		}
		static public bool IsCloseExpression(char c,int dir,ExpressionBracketType type) {
			if (type == ExpressionBracketType.Expression) {
				return IsCloseParentheses(c,dir);
			}
			if (type == ExpressionBracketType.Scope) {
				return IsCloseScope(c,dir);
			}
			if (type == ExpressionBracketType.Bracket) {
				return IsCloseBracket(c,dir);
			}
			if (type == ExpressionBracketType.Generic) {
				return IsCloseGeneric(c,dir);
			}
			return false;
		}
		
		static public bool IsOpenParentheses(char c) {
			if (c == '(') return true;
			return false;
		}
		static public bool IsOpenParentheses(char c,int dir) {
			if (dir == 1) {
				if (c == '(') return true;
			}
			else if (dir == -1) {
				if (c == ')') return true;
			}
			return false;
		}
		static public bool IsCloseParentheses(char c) {
			if (c == ')') return true;
			return false;
		}
		static public bool IsCloseParentheses(char c,int dir) {
			if (dir == 1) {
				if (c == ')') return true;
			}
			else if (dir == -1) {
				if (c == '(') return true;
			}
			return false;
		}
		
		static public bool IsOpenScope(char c) {
			if (c == '{') return true;
			return false;
		}
		static public bool IsOpenScope(char c,int dir) {
			if (dir == 1) {
				if (c == '{') return true;
			}
			else if (dir == -1) {
				if (c == '}') return true;
			}
			return false;
		}
		static public bool IsCloseScope(char c) {
			if (c == '}') return true;
			return false;
		}
		static public bool IsCloseScope(char c,int dir) {
			if (dir == 1) {
				if (c == '}') return true;
			}
			else if (dir == -1) {
				if (c == '{') return true;
			}
			return false;
		}
		
		static public bool IsOpenBracket(char c) {
			if (c == '[') return true;
			return false;
		}
		static public bool IsOpenBracket(char c,int dir) {
			if (dir == 1) {
				if (c == '[') return true;
			}
			else if (dir == -1) {
				if (c == ']') return true;
			}
			return false;
		}
		static public bool IsCloseBracket(char c) {
			if (c == ']') return true;
			return false;
		}
		static public bool IsCloseBracket(char c,int dir) {
			if (dir == 1) {
				if (c == ']') return true;
			}
			else if (dir == -1) {
				if (c == '[') return true;
			}
			return false;
		}
		
		static public bool IsOpenGeneric(char c) {
			if (c == '<') return true;
			return false;
		}
		static public bool IsOpenGeneric(char c,int dir) {
			if (dir == 1) {
				if (c == '<') return true;
			}
			else if (dir == -1) {
				if (c == '>') return true;
			}
			return false;
		}
		static public bool IsCloseGeneric(char c) {
			if (c == '>') return true;
			return false;
		}
		static public bool IsCloseGeneric(char c,int dir) {
			if (dir == 1) {
				if (c == '>') return true;
			}
			else if (dir == -1) {
				if (c == '<') return true;
			}
			return false;
		}
	}
	
	public class CompletionMethod:CompletionItem {
		public CompletionMethod() {
			
		}
		
		public CompletionMethod(MethodInfo methodInfo) {
			this.name = methodInfo.Name;
			this.fullName = methodInfo.Name;
			//this.fullTypeName = methodInfo.ReturnType.FullName;
			this.resultingType = methodInfo.ReturnType;
			this.namespaceString = "";
			this.type = CompletionItemType.Method;
			ParameterInfo[] args = methodInfo.GetParameters();
			for (int i = 0; i < args.Length; i++) {
				CompletionItem arg = new CompletionItem(args[i]);
				this.arguments.Add(arg);
			}
			System.Type[] genericArgs = methodInfo.GetGenericArguments();
			for (int i = 0; i < genericArgs.Length; i++) {
				CompletionItem genericArg = new CompletionItem(genericArgs[i]);
				this.genericArguments.Add(genericArg);
			}
		}
		
		public CompletionMethod(ConstructorInfo methodInfo) {
			this.name = methodInfo.DeclaringType.Name;
			this.fullName = methodInfo.DeclaringType.Name;
			//this.fullTypeName = methodInfo.ReturnType.FullName;
			this.resultingType = methodInfo.DeclaringType;
			this.namespaceString = "";
			this.type = CompletionItemType.Method;
			ParameterInfo[] args = methodInfo.GetParameters();
			for (int i = 0; i < args.Length; i++) {
				CompletionItem arg = new CompletionItem(args[i]);
				this.arguments.Add(arg);
			}
			/*
			System.Type[] genericArgs = methodInfo.GetGenericArguments();
			for (int i = 0; i < genericArgs.Length; i++) {
				CompletionItem genericArg = new CompletionItem(genericArgs[i]);
				this.genericArguments.Add(genericArg);
			}
			*/
		}
		
		public CompletionMethod Copy() {
			
			CompletionMethod newMethod = new CompletionMethod();
			newMethod.arguments = arguments;
			newMethod.name = name;
			newMethod.fullName = fullName;
			newMethod.namespaceString = namespaceString;
			newMethod.variableDeclarationBlock = variableDeclarationBlock;
			newMethod.type = type;
			newMethod.isStatic = isStatic;
			newMethod.isPublic = isPublic;
			newMethod.fullTypeName = fullTypeName;
			newMethod.resultingType = resultingType;
			newMethod.genericArguments = genericArguments;
			newMethod.genericArgumentItems = genericArgumentItems;
			newMethod.chainItem = chainItem;
			newMethod.hasBeenResolved = hasBeenResolved;
			return newMethod;
		}
		/*
		public bool Compare(CompletionMethod other) {
			if (other.name != name) return false;
			if (other.resultingType != resultingType) return false;
			if (other.arguments.Count != arguments.Count) return false;
			if (other.genericArguments.Count != genericArguments.Count) return false;
			Debug.Log(resultingType);
			return true;
		}
		*/
		
	}
	public class CompletionItem:System.Object {
		public List<CompletionItem> arguments = new List<CompletionItem>();
		public List<CompletionItem> genericArguments = new List<CompletionItem>();
		public List<CompletionItem> enumItems = new List<CompletionItem>();
		public List<ChainItem> genericArgumentItems = new List<ChainItem>();
		public ChainItem chainItem;
		public bool hasBeenResolved = false;
		public string name = "";
		public string fullName = "";
		public string namespaceString = "";
		public string variableDeclarationBlock = "";
		public CompletionItemType type;
		public bool isStatic;
		public bool isPublic;
		public MemberInfo memberInfo;
		public bool isOptionalArgument = false;
		public List<string> modifiers = new List<string>();
		
		public bool inferredType = false;
		public Vector2 expressionStart;
		public Vector2 expressionEnd;
		
		public bool hasDeclaredPosition;
		public string declaredFile = "";
		private Vector2 _declaredPosition;
		public Vector2 declaredPosition {
			get {
				return _declaredPosition;
			}
			set {
				_declaredPosition = value;
				hasDeclaredPosition = true;
			}
		}
		
		
		private string _fullTypeName = "";
		public string fullTypeName {
			get {
				return _fullTypeName;
			}
			set {
				_fullTypeName = value;
				if (_fullTypeName == "") {
					_resultingType = null;
					//typeResolved = true;
				}
				//else {
				//	typeResolved = false;
				//	ResolveType();
				//}
			}
		}
		//private bool typeResolved = false;
		
		
		
		private System.Type _resultingType;
		public System.Type resultingType {
			get {
				//if (!typeResolved) {
				//	ResolveType();
				//}
				return _resultingType;
			}
			set {
				_resultingType = value;
				_fullTypeName = "";
				if (value != null) {
					//_fullTypeName = _resultingType.FullName;
					_fullTypeName = _resultingType.ToString();
					//Debug.Log(_resultingType+" "+_fullTypeName);
					//if (_resultingType.IsGenericParameter) {
					//	
					//	_fullTypeName = _resultingType.ToString();
					//}
				}
				
				//typeResolved = true;
			}
		}
		/*
		private void ResolveType() {
			//string actualResultingTypeName = fullTypeName;
			System.Type aliasType = ComplitionUtility.GetTypeFromTypeAlias(fullTypeName);
			if (aliasType != null) {
				fullTypeName = aliasType.FullName;
			}
			//Debug.Log(name+" "+resultingTypeName+" "+actualResultingTypeName);
			foreach (Assembly a in System.AppDomain.CurrentDomain.GetAssemblies()){
				foreach (System.Type t in a.GetTypes()){
					if (t.FullName == fullTypeName) {
						_resultingType = t;
						break;
					}
				}
			}
			typeResolved = true;
		}
		*/
		public string displayName {
			get {
				if (name != "") {
					return name;
				}
				if (fullTypeName != "") {
					return fullTypeName;
				}
				return "[UNKNOWN]";
			}
		}
		
		public bool hasResultingType {
			get {
				return resultingType != null;
			}
		}
		public bool isType {
			get {
				return type == CompletionItemType.Type;
			}
		}
		public bool isNamespace {
			get {
				return type == CompletionItemType.Namespace;
			}
		}
		public bool isClass {
			get {
				return type == CompletionItemType.Class;
			}
		}
		public bool isStruct {
			get {
				return type == CompletionItemType.Struct;
			}
		}
		public bool isField {
			get {
				return type == CompletionItemType.Field;
			}
		}
		public bool isProperty {
			get {
				return type == CompletionItemType.Property;
			}
		}
		public bool isEnum {
			get {
				return (type == CompletionItemType.Enum || type == CompletionItemType.EnumItem);
			}
		}
		public bool isMethod {
			get {
				return type == CompletionItemType.Method;
			}
		}
		public bool isLocalVar {
			get {
				return type == CompletionItemType.LocalVar;
			}
		}
		public bool isModifier {
			get {
				return type == CompletionItemType.Modifier;
			}
		}
		public bool isKeyword {
			get {
				return type == CompletionItemType.Keyword;
			}
		}
		public bool isClassOrStruct {
			get {
				return type == CompletionItemType.Class || type == CompletionItemType.Struct;
			}
		}
		public bool isVariable {
			get {
				return type == CompletionItemType.Field || type == CompletionItemType.Property || type == CompletionItemType.LocalVar || type == CompletionItemType.Parameter;
			}
		}
		public bool isParameter {
			get {
				return type == CompletionItemType.Parameter;
			}
		}
		
		public CompletionItem() {
			
		}
		
		public string GetCleanFullTypeName() {
			if (resultingType == null) {
				return fullTypeName;
			}
			System.Text.StringBuilder output = new System.Text.StringBuilder();
			
			if (resultingType.DeclaringType != null) {
				output.Append(resultingType.DeclaringType.Namespace);
				if (output.Length != 0) {
					output.Append(".");
				}
				output.Append(ComplitionUtility.GetCleanTypeName(resultingType.DeclaringType));
				output.Append(".");
			}
			else {
				output.Append(resultingType.Namespace);
				if (output.Length != 0) {
					output.Append(".");
				}
			}
			
			output.Append(ComplitionUtility.GetCleanTypeName(resultingType));
			
			while (output.ToString().Contains("[*]")) {
				output = output.Replace("[*]","[]");
			}
			while (output.ToString().Contains("[,")) {
				output = output.Replace("[,","[");
			}
			
			return output.ToString();
		}
		public string PrettyFormatType() {
			return PrettyFormatType(true);
		}
		public string PrettyFormatType(bool spaceBetween) {
			return PrettyFormatType(spaceBetween,null,null);
		}
		public string PrettyFormatType(bool spaceBetween,string[] visibleNamespaces,string[] chainNamespaces) {
			if (resultingType == null) {
				return fullTypeName;
			}
			System.Text.StringBuilder output = new System.Text.StringBuilder();
			output.Append(GetCleanFullTypeName());
			if (genericArguments.Count > 0) {
				output.Append("<");
				//Debug.Log(name+" "+genericArguments.Count);
				for (int i = 0; i < genericArguments.Count; i++) {
					string typesNamespace = "";
					if (genericArguments[i].resultingType != null) {
						typesNamespace = genericArguments[i].resultingType.Namespace;
					}
					string argumentText = genericArguments[i].PrettyFormatType();
					if (visibleNamespaces != null && chainNamespaces != null) {
						argumentText = ComplitionUtility.CropNamespacesToShortestString(argumentText,typesNamespace,visibleNamespaces,chainNamespaces);
					}
					output.Append(argumentText);
					if (i < genericArguments.Count-1) {
						output.Append(",");
						if (spaceBetween) {
							output.Append(" ");
						}
					}
				}
				output.Append(">");
			}
			output.Replace("&","");
			string outputTypesNamespace = "";
			if (resultingType != null) {
				outputTypesNamespace = resultingType.Namespace;
			}
			string outString = ComplitionUtility.TypeNameToAlias(output.ToString());
			if (visibleNamespaces != null && chainNamespaces != null) {
				outString = ComplitionUtility.CropNamespacesToShortestString(outString,outputTypesNamespace,visibleNamespaces,chainNamespaces);
			}
			return outString;
		}
		
		public string GetCompletionFriendlyName() {
			if (resultingType == null) {
				return displayName;
			}
			System.Text.StringBuilder output = new System.Text.StringBuilder();
			
			if (genericArguments.Count > 0) {
				output.Append(ComplitionUtility.GetCleanTypeName(resultingType));
				output.Append("<");
				for (int i = 1; i < genericArguments.Count; i++) {
					output.Append(",");
				}
				output.Append(">");
			}
			else {
				output.Append(resultingType.Name);
			}
			
			return output.ToString();
		}
		
		public string GetSignature() {
			string output = "";
			
			if (type == CompletionItemType.Type) {
				if (resultingType != null) {
					output = PrettyFormatType();
				}
				else {
					output = name;
					if (genericArguments.Count > 0) {
						output += "<";
						for (int i = 0; i < genericArguments.Count; i++) {
							output += genericArguments[i].GetSignature();
							if (i < genericArguments.Count-1) {
								output += ", ";
							}
						}
						output += ">";
					}
				}
			}
			if (type == CompletionItemType.LocalVar || type == CompletionItemType.Parameter) {
				if (resultingType != null) {
					output = PrettyFormatType();
				}
				else {
					output = ComplitionUtility.TypeNameToAlias(fullTypeName);
				}
				
				output += " ";
				output += name;
			}
			if (type == CompletionItemType.Method) {
				if (resultingType != null) {
					output = PrettyFormatType();
				}
				else {
					output = ComplitionUtility.TypeNameToAlias(fullTypeName);
				}
				
				output += " ";
				output += name;
				if (genericArguments.Count > 0) {
					output += "<";
					for (int i = 0; i < genericArguments.Count; i++) {
						output += genericArguments[i].GetSignature();
						if (i < genericArguments.Count-1) {
							output += ", ";
						}
					}
					output += ">";
				}
				output += "(";
				for (int i = 0; i < arguments.Count; i++) {
					for (int p = 0; p < arguments[i].modifiers.Count; p++) {
						output += arguments[i].modifiers[p]+" ";
					}
					if (arguments[i].isOptionalArgument) {
						output += "[";
					}
					output += arguments[i].GetSignature();
					if (arguments[i].isOptionalArgument) {
						output += "]";
					}
					if (i < arguments.Count-1) {
						output += ", ";
					}
				}
				output += ")";
			}
			if (output == "") {
				if (resultingType != null) {
					output = PrettyFormatType()+" "+name;
				}
				else {
					output = ComplitionUtility.TypeNameToAlias(fullTypeName)+" "+name;
				}
			}
			return output;
		}
		
		public string GetLanguageIconName() {
			string output = type.ToString();
			if (type == CompletionItemType.Type) {
				output = "Class";
				if (resultingType == null) {
					resultingType = ComplitionUtility.GetTypeFromTypeAlias(name);
				}
				if (resultingType != null) {
					if (resultingType.IsEnum) {
						output = "Enum";
					}
					else if (resultingType.IsValueType) {
						output = "Struct";
					}
				}
				
			}
			return output;
		}
		
		public int Compare(CompletionItem other) {
			int thisScore = (int)type;
			int otherScore = (int)other.type;
			if (thisScore == otherScore) {
				return 0;
			}
			if (thisScore > otherScore) {
				return 1;
			}
			return -1;
		}
		
		public CompletionItem(System.Type type) {
			this.name = type.Name;
			this.fullName = type.FullName;
			this.resultingType = type;
			this.namespaceString = type.Namespace;
			this.type = CompletionItemType.Type;
			
			System.Type[] genericArgs = type.GetGenericArguments();
			for (int i = 0; i < genericArgs.Length; i++) {
				CompletionItem genericArg = new CompletionItem(genericArgs[i]);
				this.genericArguments.Add(genericArg);
			}
		}
		
		public CompletionItem(UIDENamespace ns) {
			this.name = ns.name;
			this.fullName = ns.fullName;
			//this.fullTypeName = "";
			this.namespaceString = ns.fullName;
			this.type = CompletionItemType.Namespace;
		}
		
		public CompletionItem(ParameterInfo pInfo) {
			this.name = pInfo.Name;
			this.fullName = pInfo.Name;
			this.resultingType = pInfo.ParameterType;
			//Debug.Log(pInfo.Name+" "+this.resultingType.IsGenericParameter);
			this.namespaceString = "";
			this.type = CompletionItemType.Parameter;
			this.isOptionalArgument = pInfo.IsOptional;
			
			if (pInfo.IsOut) {
				this.modifiers.Add("out");
			}
			else if (pInfo.ParameterType.IsByRef) {
				this.modifiers.Add("ref");
			}
			if (System.Attribute.IsDefined(pInfo, typeof(System.ParamArrayAttribute))) {
				this.modifiers.Add("params");
			}
		}
		
		public CompletionItem(FieldInfo field) {
			this.name = field.Name;
			this.fullName = field.Name;
			this.resultingType = field.FieldType;
			this.namespaceString = "";
			this.type = CompletionItemType.Field;
		}
		public CompletionItem(PropertyInfo property) {
			this.name = property.Name;
			this.fullName = property.Name;
			this.resultingType = property.PropertyType;
			this.namespaceString = "";
			this.type = CompletionItemType.Property;
		}
		
		static public CompletionItem CreateFromKeyword(string keyword) {
			CompletionItem item = new CompletionItem();
			item.name = keyword;
			item.fullName = keyword;
			item.fullTypeName = "";
			item.namespaceString = "";
			item.type = CompletionItemType.Keyword;
			return item;
		}
		static public CompletionItem CreateFromModifier(string keyword) {
			CompletionItem item = new CompletionItem();
			item.name = keyword;
			item.fullName = keyword;
			item.fullTypeName = "";
			item.namespaceString = "";
			item.type = CompletionItemType.Modifier;
			return item;
		}
		static public CompletionItem CreateFromPrimitiveType(string keyword) {
			CompletionItem item = new CompletionItem();
			item.name = keyword;
			item.fullName = keyword;
			item.fullTypeName = "";
			item.namespaceString = "";
			item.type = CompletionItemType.Type;
			return item;
		}
	}
}
