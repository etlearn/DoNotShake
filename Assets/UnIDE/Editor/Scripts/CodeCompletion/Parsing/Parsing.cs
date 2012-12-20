using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UIDE.CodeCompletion;
using UIDE.SyntaxRules;

//using UIDE.CodeCompletion.Parsing.CSharp;
using UIDE.CodeCompletion.Parsing;
using UIDE.CodeCompletion.Parsing.CommonParser;

namespace UIDE.CodeCompletion.Parsing {
	public class StatementFindRule:System.Object {
		public virtual Statement Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class LanguageElement:System.Object {
		public int startPosition = 0;
		public int endPosition = 0;
		
		public int startLine {
			get {
				return file.lineNumberByChar[startPosition];
			}
		}
		public int startColumn {
			get {
				return file.columnByChar[startPosition];
			}
		}
		
		public int endLine {
			get {
				return file.lineNumberByChar[endPosition];
			}
		}
		public int endColumn {
			get {
				return file.columnByChar[endPosition];
			}
		}
		public Vector2 vectorStart {
			get {
				return new Vector2(startColumn,startLine);
			}
		}
		public Vector2 vectorEnd {
			get {
				return new Vector2(endColumn,endLine);
			}
		}
		
		public SourceFile file;
		
		public string name = "";
		
		private string _text = "";
		public string text {
			get {
				return _text;
			}
		}
		
		public LanguageElement owner;
		
		public bool isRoot {
			get {
				return owner == null;
			}
		}
		
		public LanguageElement(StatementBlock block, int start, int end) {
			if (block != null) {
				this.file = block.file;
			}
			this.owner = block;
			this.startPosition = start;
			this.endPosition = end;
			
			if (this.file != null) {
				UpdatePositions();
			}
			
			name = text;
		}
		
		public void SetText(string newText) {
			_text = newText;
		}
		
		public void UpdatePositions() {
			if (startPosition < 0) {
				startPosition = 0;
			}
			//Debug.Log(file.text);
			if (endPosition >= file.text.Length) {
				Debug.LogError("endPosition out of range "+endPosition+" >= "+file.text.Length);
				endPosition = file.text.Length-1;
			}
			_text = "";
			
			if (endPosition >= startPosition) {
				_text = file.text.Substring(startPosition,(endPosition-startPosition)+1);
			}
		}
		
		public bool IsTypeOf<T>() {
			return typeof(T).IsAssignableFrom(this.GetType());
		}
		
		public bool IsWithinTypeOf<T>() {
			bool isType = typeof(T).IsAssignableFrom(this.GetType());
			if (isType) return true;
			if (owner != null) {
				return owner.IsWithinTypeOf<T>();
			}
			return false;
		}
		public T GetOwnerOfType<T>() {
			bool isType = typeof(T).IsAssignableFrom(this.GetType());
			if (isType) return (T)System.Convert.ChangeType(this, typeof(T));
			if (owner != null) {
				return owner.GetOwnerOfType<T>();
			}
			return default(T);
		}
		public NamespaceDef GetEnclosingNamespace() {
			return (NamespaceDef)GetOwnerOfType<NamespaceDef>();
		}
		public TypeDef GetEnclosingType() {
			return (TypeDef)GetOwnerOfType<TypeDef>();
		}
		public MethodDef GetEnclosingMethod() {
			return (MethodDef)GetOwnerOfType<MethodDef>();
		}
		
		static public string GetLiteralName(string input) {
			Regex regex = new Regex(@"(?<name>[A-Za-z_]+[\w\.]*)");
			Match match = regex.Match(input);
			if (match.Success) {
				return match.Groups["name"].Value;
			}
			return input;
		}
	}
	
	public class SourceFile:System.Object {
		public Parser parser;
		public string text = "";
		public StatementBlock statementBlock;
		public int[] lineNumberByChar;
		public int[] columnByChar;
		
		public List<UsingClauseDef> usingClauses = new List<UsingClauseDef>();
		public List<NamespaceDef> namespaces = new List<NamespaceDef>();
		public List<TypeDef> typeDefs = new List<TypeDef>();
		public List<EnumDef> enums = new List<EnumDef>();
		
		public SourceFile(Parser parser,string text) {
			this.text = text;
			this.parser = parser;
		}
		
		public void UpdateLineNumberTable() {
			lineNumberByChar = new int[text.Length];
			columnByChar = new int[text.Length];
			int currentLine = 0;
			int currentColumn = 0;
			for (int i = 0; i < text.Length; i++) {
				char character = text[i];
				
				lineNumberByChar[i] = currentLine;
				columnByChar[i] = currentColumn;
				
				currentColumn++;
				if (character == '\n') {
					currentLine++;
					currentColumn = 0;
				}
			}
		}
		
		public void Parse() {
			UpdateLineNumberTable();
			
			statementBlock = new StatementBlock(null,null,0,text.Length-1);
			statementBlock.file = this;
			statementBlock.UpdatePositions();
			statementBlock.Parse();
			
			usingClauses = statementBlock.GetStatmentsOfType<UsingClauseDef>().ToList();
			namespaces = statementBlock.GetStatmentsOfType<NamespaceDef>().ToList();
			typeDefs = statementBlock.GetStatmentsOfType<TypeDef>().ToList();
			enums = statementBlock.GetStatmentsOfType<EnumDef>().ToList();
			
			parser.OnFinishedParsingFile();
		}
		
		
		
	}
	
	public class Statement:LanguageElement {
		public TypeIdentDef resultingType;
		//public LanguageElementFindRule<Statement> findRule;
		
		public Parser parser {
			get {
				return file.parser;
			}
		}
		public Statement(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
	}
	
	public class StatementBlock:Statement {
		public List<Statement> statements = new List<Statement>();
		
		public StatementBlock(StatementBlock block, LanguageElement owner, int start, int end):base(block,start,end) {
			this.owner = owner;
		}
		
		public virtual void Parse() {
			int startPos = startPosition;
			
			while (startPos <= endPosition) {
				startPos = GoToEndOfWhitespace(startPos);
				if (IsEOF(startPos+1)) {
					break;
				}
					
				if (isRoot) {
					Statement usingClause = parser.FindFromRule("UsingClauseDef",this,ref startPos);
					if (usingClause != null) {statements.Add(usingClause);continue;}
				}
				if (isRoot || owner.IsTypeOf<NamespaceDef>()) {
					Statement ns = parser.FindFromRule("NamespaceDef",this,ref startPos);
					if (ns != null) {statements.Add(ns);continue;}
					
					Statement enumDef = parser.FindFromRule("EnumDef",this,ref startPos);
					if (enumDef != null) {statements.Add(enumDef);continue;}
					
					Statement typeDef = parser.FindFromRule("TypeDef",this,ref startPos);
					if (typeDef != null) {statements.Add(typeDef);continue;}
				}
				
				
				if (!isRoot && (owner.IsWithinTypeOf<MethodDef>()||owner.IsWithinTypeOf<AccessorDef>())) {
					Statement localVariableDef = parser.FindFromRule("LocalVariableDef",this,ref startPos);
					if (localVariableDef != null) {statements.Add(localVariableDef);continue;}
					
					Statement forEachDef = parser.FindFromRule("ForEachDef",this,ref startPos);
					if (forEachDef != null) {statements.Add(forEachDef);continue;}
					
					Statement forDef = parser.FindFromRule("ForDef",this,ref startPos);
					if (forDef != null) {statements.Add(forDef);continue;}
				}
				
				if (!isRoot && owner.IsTypeOf<TypeDef>()) {
					Statement methodDef = parser.FindFromRule("MethodDef",this,ref startPos);
					if (methodDef != null) {statements.Add(methodDef);continue;}
					
					Statement enumDef = parser.FindFromRule("EnumDef",this,ref startPos);
					if (enumDef != null) {statements.Add(enumDef);continue;}
					
					Statement typeDef = parser.FindFromRule("TypeDef",this,ref startPos);
					if (typeDef != null) {statements.Add(typeDef);continue;}
					
					Statement propertyDef = parser.FindFromRule("PropertyDef",this,ref startPos);
					if (propertyDef != null) {statements.Add(propertyDef);continue;}
					
					Statement fieldDef = parser.FindFromRule("FieldDef",this,ref startPos);
					if (fieldDef != null) {statements.Add(fieldDef);continue;}
				}
				
				startPos = GoToEndOfStatement(startPos,true,true,true);
			}
			
		}
		
		public T[] GetStatmentsOfType<T>() {
			List<T> outStatements = new List<T>();
			foreach (Statement st in statements) {
				if (st.IsTypeOf<T>()) {
					T t = (T)System.Convert.ChangeType(st, typeof(T));
					outStatements.Add(t);
				}
			}
			return outStatements.ToArray();
		}
		
		public char GetChar(int pos) {
			if (IsEOF(pos)) return (char)0;
			return file.text[pos];
		}
		
		public bool IsWhiteSpace(int pos) {
			if (IsEOF(pos)) return false;
			char c = GetChar(pos);
			return char.IsWhiteSpace(c);
		}
		
		public WordDef[] GatherModifiers(ref int pos) {
			List<WordDef> modifiers = new List<WordDef>();
			while (!IsEOF(pos)) {
				int tmpPos = pos;
				WordDef modifierIdent = WordDef.Find(this,ref tmpPos);
				if (modifierIdent == null) break;
				if (Parser.allModifiersHash.Contains(modifierIdent.text)) {
					tmpPos = GoToEndOfWhitespace(tmpPos);
					pos = tmpPos;
					modifiers.Add(modifierIdent);
				}
				else {
					break;
				}
			}
			return modifiers.ToArray();
		}
		
		public WordDef[] GatherMethodArgumentModifiers(ref int pos) {
			List<WordDef> modifiers = new List<WordDef>();
			while (!IsEOF(pos)) {
				int tmpPos = pos;
				WordDef modifierIdent = WordDef.Find(this,ref tmpPos);
				if (modifierIdent == null) break;
				if (Parser.methodArgModifersHash.Contains(modifierIdent.text)) {
					tmpPos = GoToEndOfWhitespace(tmpPos);
					pos = tmpPos;
					modifiers.Add(modifierIdent);
				}
				else {
					break;
				}
			}
			return modifiers.ToArray();
		}
		
		public string GetNextWord(int pos) {
			string output = "";
			//char lastChar = (char)0;
			int counter = 0;
			while (!IsEOF(pos)) {
				char character = file.text[pos];
				if (counter == 0) {
					if (char.IsNumber(character)) {
						break;
					}
				}
				if (!char.IsLetter(character) && !char.IsNumber(character) && character != '_') {
					break;
				}
				output += character;
				pos++;
				counter++;
				//lastChar = character;
			}
			pos = ClampPosition(pos);
			return output;
		}
		
		public string GetNextIdent(int pos) {
			string output = "";
			char lastChar = (char)0;
			int counter = 0;
			while (!IsEOF(pos)) {
				char character = file.text[pos];
				if (counter == 0) {
					if (char.IsNumber(character)) {
						break;
					}
					if (character == '.') {
						break;
					}
				}
				if (lastChar == '.' && (char.IsNumber(character) || character == '.')) {
					break;
				}
				if (!char.IsLetter(character) && !char.IsNumber(character) && character != '_' && character != '.') {
					break;
				}
				output += character;
				pos++;
				counter++;
				lastChar = character;
			}
			pos = ClampPosition(pos);
			return output;
		}
		
		public int GoToEndOfWhitespace(int pos) {
			while (!IsEOF(pos)) {
				char character = file.text[pos];
				if (!char.IsWhiteSpace(character)) {
					break;
				}
				pos++;
			}
			pos = ClampPosition(pos);
			return pos;
		}
		
		public int GoToChar(int pos, char gotoChar) {
			while (!IsEOF(pos)) {
				char character = file.text[pos];
				if (character == gotoChar) {
					break;
				}
				pos++;
			}
			pos = ClampPosition(pos);
			return pos;
		}
		
		public int GoToEndOfStatement(int pos) {
			return GoToEndOfStatement(pos,false);
		}
		public int GoToEndOfStatement(int pos,bool extractStatement) {
			return GoToEndOfStatement(pos,extractStatement,true);
		}
		public int GoToEndOfStatement(int pos, bool extractStatement, bool breakOnCurly) {
			return GoToEndOfStatement(pos,extractStatement,breakOnCurly,false);
		}
		public int GoToEndOfStatement(int pos, bool extractStatement, bool breakOnCurly, bool breakOnAllScope) {
			while (!IsEOF(pos)) {
				char character = file.text[pos];
				if (character == '{') {
					int scopeStart = pos;
					string tmp = ComplitionUtility.ExtractScope(file.text,pos,ExpressionBracketType.Scope,false);
					pos += tmp.Length+2;
					if (extractStatement) {
						StatementBlock newBlock = new StatementBlock(this,this,scopeStart+1,pos-2);
						newBlock.Parse();
						statements.Add(newBlock);
					}
					if (breakOnCurly || breakOnAllScope) {
						break;
					}
					else {
						continue;
					}
					
				}
				if (character == '(') {
					string tmp = ComplitionUtility.ExtractScope(file.text,pos,ExpressionBracketType.Expression,false);
					pos += tmp.Length+2;
					if (breakOnAllScope) {
						break;
					}
					else {
						continue;
					}
				}
				if (character == '[') {
					string tmp = ComplitionUtility.ExtractScope(file.text,pos,ExpressionBracketType.Bracket,false);
					pos += tmp.Length+2;
					if (breakOnAllScope) {
						break;
					}
					else {
						continue;
					}
				}
				if (character == ';') {
					pos++;
					break;
				}
				pos++;
			}
			pos = ClampPosition(pos);
			return pos;
		}
		
		public int GoToCharSkippingScope(int pos, char c) {
			while (!IsEOF(pos)) {
				char character = file.text[pos];
				if (character == '{') {
					string tmp = ComplitionUtility.ExtractScope(file.text,pos,ExpressionBracketType.Scope,false);
					pos += tmp.Length+1;
					//break;
				}
				if (character == '(') {
					string tmp = ComplitionUtility.ExtractScope(file.text,pos,ExpressionBracketType.Expression,false);
					pos += tmp.Length+1;
					//break;
				}
				if (character == '[') {
					string tmp = ComplitionUtility.ExtractScope(file.text,pos,ExpressionBracketType.Bracket,false);
					pos += tmp.Length+1;
					
					//break;
				}
				if (character == c) {
					
					break;
				}
				pos++;
			}
			pos = ClampPosition(pos);
			return pos;
		}
		
		public int ClampPosition(int pos) {
			if (pos > endPosition) {
				pos = endPosition;
			}
			return pos;
		}
		
		public bool IsEOF(int pos) {
			if (pos > endPosition) {
				return true;
			}
			return false;
		}
	}
	
	public class GenericArgumentBlock:StatementBlock {
		new public List<TypeIdentDef> statements = new List<TypeIdentDef>();
		public GenericArgumentBlock(StatementBlock block, LanguageElement owner, int start, int end):base(block,owner,start,end) {
			this.owner = owner;
		}
		
		public override void Parse() {
			int startPos = startPosition;
			//Debug.Log(text);
			while (startPos <= endPosition) {
				startPos = GoToEndOfWhitespace(startPos);
				if (IsEOF(startPos+1)) {
					break;
				}
				
				TypeIdentDef typeIdentDef = (TypeIdentDef)parser.FindFromRule("TypeIdentDef",this,ref startPos);
				if (typeIdentDef != null) {
					statements.Add(typeIdentDef);
					continue;
				}
				
				startPos = GoToEndOfWhitespace(startPos);
				startPos = GoToChar(startPos,',')+1;
			}
		}
	}
	public class MethodArgumentBlock:StatementBlock {
		new public List<MethodArgumentDef> statements = new List<MethodArgumentDef>();
		public MethodArgumentBlock(StatementBlock block, LanguageElement owner, int start, int end):base(block,owner,start,end) {
			this.owner = owner;
		}
		
		public override void Parse() {
			int startPos = startPosition;
			//Debug.Log(text);
			while (startPos <= endPosition) {
				startPos = GoToEndOfWhitespace(startPos);
				if (IsEOF(startPos+1)) {
					break;
				}
				MethodArgumentDef methodDef = (MethodArgumentDef)parser.FindFromRule("MethodArgumentDef",this,ref startPos);
				if (methodDef != null) {
					statements.Add(methodDef);
					continue;
				}
				
				startPos = GoToCharSkippingScope(startPos,',')+1;
			}
		}
	}
	
	public class PropertyBlock:StatementBlock {
		public PropertyBlock(StatementBlock block, LanguageElement owner, int start, int end):base(block,owner,start,end) {
			this.owner = owner;
		}
		
		public override void Parse() {
			int startPos = startPosition;
			//Debug.Log(text);
			while (startPos <= endPosition) {
				startPos = GoToEndOfWhitespace(startPos);
				
				Statement accessorDef = parser.FindFromRule("AccessorDef",this,ref startPos);
				if (accessorDef != null) {
					statements.Add(accessorDef);
					continue;
				}
				
				startPos = GoToEndOfStatement(startPos);
				
				if (IsEOF(startPos+1)) {
					break;
				}
			}
		}
	}
	
	public class WordDef:LanguageElement {
		public WordDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		static public WordDef Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			string identString = block.GetNextWord(pos);
			//Debug.Log(identString);
			if (identString.Length > 0) {
				
				WordDef word = new WordDef(block,startIndex,startIndex+identString.Length-1);
				startIndex = word.endPosition+1;
				return word;
			}
			
			return null;
		}
	}
	
	public class IdentDef:LanguageElement {
		public IdentDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		static public IdentDef Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			string identString = block.GetNextIdent(pos);
			//Debug.Log(identString);
			if (identString.Length > 0) {
				
				IdentDef ident = new IdentDef(block,startIndex,startIndex+identString.Length-1);
				startIndex = ident.endPosition+1;
				return ident;
			}
			
			return null;
		}
	}
	
	public class TypeIdentDef:Statement {
		public GenericArgumentBlock genericArgumentBlock;
		
		public TypeIdentDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public TypeIdentDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class UsingClauseDef:Statement {
		public GenericArgumentBlock genericArgumentBlock;
		
		public UsingClauseDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public UsingClauseDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class MethodArgumentDef:Statement {
		public bool isOptional = false;
		public List<WordDef> modifiers = new List<WordDef>();
		public bool isOut {
			get {
				for (int i = 0; i < modifiers.Count; i++) {
					if (modifiers[i].text == "out") {
						return true;
					}
				}
				return false;
			}
		}
		public bool isRef {
			get {
				for (int i = 0; i < modifiers.Count; i++) {
					if (modifiers[i].text == "ref") {
						return true;
					}
				}
				return false;
			}
		}
		public bool isParams {
			get {
				for (int i = 0; i < modifiers.Count; i++) {
					if (modifiers[i].text == "params") {
						return true;
					}
				}
				return false;
			}
		}
		public MethodArgumentDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public MethodArgumentDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class LocalVariableDef:Statement {
		public bool inferredType = false;
		public Vector2 expressionStart;
		public Vector2 expressionEnd;
		public LocalVariableDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public LocalVariableDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class EnumDef:Statement {
		public StatementBlock statementBlock;
		public EnumDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public EnumDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class FieldDef:Statement {
		public bool inferredType = false;
		public Vector2 expressionStart;
		public Vector2 expressionEnd;
		public FieldDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public FieldDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class AccessorDef:Statement {
		public bool isGetter = false;
		public bool isSetter = false;
		public StatementBlock statementBlock;
		public AccessorDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public AccessorDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class PropertyDef:Statement {
		public PropertyBlock statementBlock;
		public AccessorDef getter;
		public AccessorDef setter;
		public PropertyDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public PropertyDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class MethodDef:Statement {
		static public Regex regex;
		public StatementBlock statementBlock;
		public GenericArgumentBlock genericArgumentBlock;
		public MethodArgumentBlock argumentBlock;
		//public List<LocalVariableDef> arguments = new List<LocalVariableDef>();
		
		public MethodDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public MethodDef Find(StatementBlock block, ref int startIndex) {
			return null;
			
		}
	}
	
	public class ForEachDef:Statement {
		public StatementBlock statementBlock;
		public StatementBlock argumentBlock;
		public LocalVariableDef variable;
		public ForEachDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public ForEachDef Find(StatementBlock block, ref int startIndex) {
			return null;
			
		}
	}
	
	public class ForDef:Statement {
		public StatementBlock statementBlock;
		public StatementBlock argumentBlock;
		
		public ForDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public ForDef Find(StatementBlock block, ref int startIndex) {
			return null;
			
		}
	}
	
	public class NamespaceDef:Statement {
		public StatementBlock statementBlock;
		public TypeIdentDef namespaceIdent;
		public NamespaceDef enclosingNamespace;
		public List<TypeDef> typeDefs = new List<TypeDef>();
		public List<EnumDef> enums = new List<EnumDef>();
		
		public NamespaceDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public NamespaceDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class TypeDef:Statement {
		public StatementBlock statementBlock;
		public TypeIdentDef baseType;
		public List<TypeIdentDef> interfaces;
		public bool isClass = false;
		public NamespaceDef enclosingNamespace;
		public TypeDef enclosingType;
		public List<TypeDef> typeDefs = new List<TypeDef>();
		public List<MethodDef> methods = new List<MethodDef>();
		public List<PropertyDef> properties = new List<PropertyDef>();
		public List<FieldDef> fields = new List<FieldDef>();
		public List<EnumDef> enums = new List<EnumDef>();
		
		public TypeDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
		
		static public TypeDef Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
}

