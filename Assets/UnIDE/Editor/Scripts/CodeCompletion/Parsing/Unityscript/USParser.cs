using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UIDE.CodeCompletion;
using UIDE.SyntaxRules;

using UIDE.CodeCompletion.Parsing;

namespace UIDE.CodeCompletion.Parsing.Unityscript {
	public class Parser:UIDE.CodeCompletion.Parsing.CommonParser.Parser {
		bool isReparsingForImpliedType = false;
		
		public Parser() {
			findRules = new Dictionary<string,StatementFindRule>();
			AddFindRule<TypeIdentDefFind>("TypeIdentDef");
			AddFindRule<UsingClauseDefFind>("UsingClauseDef");
			AddFindRule<MethodArgumentDefFind>("MethodArgumentDef");
			AddFindRule<LocalVariableDefFind>("LocalVariableDef");
			AddFindRule<EnumDefFind>("EnumDef");
			AddFindRule<FieldDefFind>("FieldDef");
			AddFindRule<AccessorDefFind>("AccessorDef");
			AddFindRule<PropertyDefFind>("PropertyDef");
			AddFindRule<MethodDefFind>("MethodDef");
			AddFindRule<ForEachDefFind>("ForEachDef");
			AddFindRule<ForDefFind>("ForDef");
			AddFindRule<NamespaceDefFind>("NamespaceDef");
			AddFindRule<TypeDefFind>("TypeDef");
		}
		
		public override void Parse(string text) {
			string textHeader = "import UnityEngine;";
			textHeader += "import System.Collections;";
			textHeader += "import UnityScript.Lang;";
			if (sourceFilePath.Split('/').Contains("Editor")) {
				textHeader += "import UnityEditor;";
			}
			text = textHeader+""+text;
			base.Parse(text);
		}
		
		public override void OnFinishedParsingFile() {
			if (file.typeDefs.Count == 0 && !isReparsingForImpliedType) {
				isReparsingForImpliedType = true;
				//Debug.Log(file.typeDefs.Count);
				
				TypeDef type = new TypeDef(null,0,file.text.Length-1);
				type.file = file;
				type.statementBlock = new StatementBlock(null,type,0,file.text.Length-2);
				type.statementBlock.file = file;
				type.statementBlock.UpdatePositions();
				
				TypeIdentDef typeIdent = new TypeIdentDef(null,0,0);
				typeIdent.SetText(sourceFileName);
				typeIdent.name = sourceFileName;
				typeIdent.file = file;
				typeIdent.UpdatePositions();
				
				TypeIdentDef baseTypeIdent = new TypeIdentDef(null,0,0);
				baseTypeIdent.SetText("UnityEngine.MonoBehaviour");
				baseTypeIdent.name = "UnityEngine.MonoBehaviour";
				baseTypeIdent.file = file;
				baseTypeIdent.UpdatePositions();
				
				type.name = sourceFileName;
				type.resultingType = typeIdent;
				type.baseType = baseTypeIdent;
				type.interfaces = new List<TypeIdentDef>();
				type.isClass = true;
				
				type.statementBlock.Parse();
				
				type.typeDefs = type.statementBlock.GetStatmentsOfType<TypeDef>().ToList();
				type.methods = type.statementBlock.GetStatmentsOfType<MethodDef>().ToList();
				type.properties = type.statementBlock.GetStatmentsOfType<PropertyDef>().ToList();
				type.fields = type.statementBlock.GetStatmentsOfType<FieldDef>().ToList();
				type.enums = type.statementBlock.GetStatmentsOfType<EnumDef>().ToList();
				type.enclosingNamespace = null;
				type.enclosingType = null;
				
				file.typeDefs.Add(type);
				
				
				isReparsingForImpliedType = false;
			}
		}
	}
	
	public class TypeIdentDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			IdentDef ident = IdentDef.Find(block,ref pos);
			if (ident == null) return null;
			
			TypeIdentDef typeIdentDef = new TypeIdentDef(block,ident.startPosition,pos-1);
			
			int afterWhitespacePos = block.GoToEndOfWhitespace(pos);
			char nextChar = block.GetChar(afterWhitespacePos);
			
			if (nextChar == '.' && block.GetChar(pos+1) == '<') {
				afterWhitespacePos += 1;
				nextChar = block.GetChar(afterWhitespacePos);
			}
			if (nextChar == '<') {
				pos = afterWhitespacePos;
				string body = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Generic,false);
				typeIdentDef.genericArgumentBlock = new GenericArgumentBlock(block,typeIdentDef,pos+1,pos+body.Length);
				pos = typeIdentDef.genericArgumentBlock.endPosition+1;
				typeIdentDef.endPosition = pos;
				typeIdentDef.UpdatePositions();
				typeIdentDef.genericArgumentBlock.Parse();
				typeIdentDef.SetText(typeIdentDef.text.Replace(".<","<"));
				//typeIdentDef.name = LanguageElement.GetLiteralName(typeIdentDef.name);
				pos += 1;
			}
			
			typeIdentDef.name = typeIdentDef.text;
			
			afterWhitespacePos = block.GoToEndOfWhitespace(pos);
			nextChar = block.GetChar(afterWhitespacePos);
			
			if (nextChar == '[') {
				int nextPos = afterWhitespacePos;
				while (true) {
					
					nextPos += 1;
					
					nextPos = block.GoToEndOfWhitespace(nextPos);
					nextChar = block.GetChar(nextPos);
					
					if (nextChar == ']') {
						//pos += 1;
						nextPos += 1;
					}
					else {
						return null;
					}
					int preWhitespacePos = nextPos;
					nextPos = block.GoToEndOfWhitespace(nextPos);
					nextChar = block.GetChar(nextPos);
					if (nextChar != '[') {
						pos = preWhitespacePos;
						
						typeIdentDef.endPosition = pos-1;
						typeIdentDef.UpdatePositions();
						typeIdentDef.SetText(typeIdentDef.text.Replace(".<","<"));
						//Debug.Log(typeIdentDef.text);
						break;
					}
				}
				//typeIdentDef.endPosition = pos;
				//typeIdentDef.UpdatePositions();
			}
			
			
			if (typeIdentDef.text == "String") {
				typeIdentDef.name = "string";
				typeIdentDef.SetText("string");
			}
			else if (typeIdentDef.text == "boolean") {
				typeIdentDef.name = "bool";
				typeIdentDef.SetText("bool");
			}
			
			startIndex = pos;
			
			return typeIdentDef;
			
			//return null;
		}
	}
	
	public class UsingClauseDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			IdentDef ident = IdentDef.Find(block,ref pos);
			if (ident == null) return null;
			
			if (ident.text != "import") return null;
			
			
			//pos -= 1;
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (typeIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			if (nextChar != ';') return null;
			
			UsingClauseDef usingClause = new UsingClauseDef(block,ident.startPosition,typeIdent.endPosition);
			
			usingClause.name = typeIdent.text;
			usingClause.resultingType = typeIdent;
			//Debug.Log("using "+usingClause.name+" "+usingClause.resultingType.text);
			pos += 1;
			
			startIndex = pos;
			
			//block.statements.Add(usingClause);
			return usingClause;
			
			//return null;
		}
	}
	
	public class MethodArgumentDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherMethodArgumentModifiers(ref pos);
			
			WordDef nameIdent = WordDef.Find(block,ref pos);
			if (nameIdent == null) return null;
			
			//if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			if (block.GetChar(pos) != ':') return null;
			pos++;
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (typeIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			if (nextChar == ',' || nextChar == '=' || block.IsEOF(pos+1)) {
				//if (nextChar == '=') {
				//	startIndex = block.GoToCharSkippingScope(pos,',');
				//}
				//else {
				//	startIndex = pos+1;
				//}
				startIndex = pos;
				
				MethodArgumentDef arg = new MethodArgumentDef(block,nameIdent.startPosition,typeIdent.endPosition);
				arg.name = nameIdent.text;
				arg.resultingType = typeIdent;
				
				//Debug.Log(arg.name+" "+arg.resultingType.text+" "+nextChar);
				
				//block.statements.Add(arg);
				return arg;
			}
			
			return null;
		}
	}
	
	public class LocalVariableDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			
			WordDef varWord = WordDef.Find(block,ref pos);
			if (varWord == null) return null;
			
			if (varWord.text != "var") return null;
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			IdentDef nameIdent = IdentDef.Find(block,ref pos);
			if (nameIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef typeIdent = null;
			
			if (block.GetChar(pos) == ':') {
				pos++;
				pos = block.GoToEndOfWhitespace(pos);
				
				typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
				if (typeIdent == null) return null;
				pos = block.GoToEndOfWhitespace(pos);
			}
			
			char nextChar = block.GetChar(pos);
			
			bool nextWordIsIn = false;
			if (nextChar == 'i') {
				int tmpPos = pos;
				WordDef inWord = WordDef.Find(block,ref tmpPos);
				if (inWord != null && inWord.text == "in") {
					nextWordIsIn = true;
					pos = tmpPos;
				}
			}
			
			bool isAssigned = nextChar == '=';
			
			if (nextChar == ';' || isAssigned || nextWordIsIn) {
				startIndex = block.GoToEndOfStatement(pos,false,false);
				int endPos = nameIdent.endPosition;
				if (typeIdent != null) {
					endPos = typeIdent.endPosition;
				}
				LocalVariableDef variable = new LocalVariableDef(block,nameIdent.startPosition,endPos);
				variable.name = nameIdent.text;
				variable.resultingType = typeIdent;
				if (isAssigned && typeIdent == null) {
					int expStartPos = pos+1;
					
					int expStartLine = block.file.lineNumberByChar[expStartPos];
					int expStartColumn = block.file.columnByChar[expStartPos];
					
					int expEndLine = block.file.lineNumberByChar[startIndex-2];
					int expEndColumn = block.file.columnByChar[startIndex-2];
					
					variable.inferredType = true;
					variable.expressionStart = new Vector2(expStartColumn,expStartLine);
					variable.expressionEnd = new Vector2(expEndColumn,expEndLine);
				}
				//Debug.Log(variable.name+" "+variable.resultingType.text);
				
				//block.statements.Add(variable);
				return variable;
			}
			
			return null;
		}
	}
	
	public class EnumDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			
			WordDef wordIdent = WordDef.Find(block,ref pos);
			if (wordIdent == null) return null;
			if (wordIdent.text != "enum") return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef secondIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (secondIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			if (nextChar != '{') return null;
			int bodyStart = pos;
			
			string bodyText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Scope,false);
			pos += bodyText.Length+2;
		
			EnumDef enumDef = new EnumDef(block,wordIdent.startPosition,pos);
			enumDef.name = secondIdent.text;
			enumDef.resultingType = secondIdent;
			enumDef.statementBlock = new StatementBlock(block,enumDef,bodyStart+1,pos-2);
			
			
			//enumDef.statementBlock.Parse();
			
			int contentPos = enumDef.statementBlock.startPosition;
			//Debug.Log(text);
			while (contentPos <= enumDef.statementBlock.endPosition) {
				if (enumDef.statementBlock.IsEOF(contentPos+1)) {
					break;
				}
				contentPos = enumDef.statementBlock.GoToEndOfWhitespace(contentPos);
				
				WordDef contentWordIdent = WordDef.Find(block,ref contentPos);
				if (contentWordIdent != null) {
					LocalVariableDef contentVariable = new LocalVariableDef(enumDef.statementBlock,contentWordIdent.startPosition,contentWordIdent.endPosition);
					contentVariable.name = contentWordIdent.text;
					
					TypeIdentDef contentVarType = new TypeIdentDef(enumDef.statementBlock,contentWordIdent.startPosition,contentWordIdent.endPosition);
					contentVarType.name = "(System.Int32)";
					contentVariable.resultingType = contentVarType;
					//Debug.Log(enumDef.name+" "+contentVariable.name);
					enumDef.statementBlock.statements.Add(contentVariable);
					
					continue;
				}
				
				contentPos = enumDef.statementBlock.GoToCharSkippingScope(contentPos,',')+1;
			}
			
			startIndex = pos;
			
			return enumDef;
		}
	}
	
	public class FieldDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			
			WordDef varWord = WordDef.Find(block,ref pos);
			if (varWord == null) return null;
			
			if (varWord.text != "var") return null;
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			IdentDef nameIdent = IdentDef.Find(block,ref pos);
			if (nameIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef typeIdent = null;
			
			if (block.GetChar(pos) == ':') {
				pos++;
				pos = block.GoToEndOfWhitespace(pos);
				
				typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
				if (typeIdent == null) return null;
				pos = block.GoToEndOfWhitespace(pos);
			}
			
			char nextChar = block.GetChar(pos);
			
			bool isAssigned = nextChar == '=';
			
			if (nextChar == ';' || isAssigned) {
				//Debug.Log(typeIdent.text+" "+nameIdent.text);
				startIndex = block.GoToEndOfStatement(pos,false,false);
				int endPos = nameIdent.endPosition;
				if (typeIdent != null) {
					endPos = typeIdent.endPosition;
				}
				FieldDef field = new FieldDef(block,nameIdent.startPosition,endPos);
				field.name = nameIdent.text;
				field.resultingType = typeIdent;
				if (isAssigned && typeIdent == null) {
					int expStartPos = pos+1;
					
					int expStartLine = block.file.lineNumberByChar[expStartPos];
					int expStartColumn = block.file.columnByChar[expStartPos];
					
					int expEndLine = block.file.lineNumberByChar[startIndex-2];
					int expEndColumn = block.file.columnByChar[startIndex-2];
					
					field.inferredType = true;
					field.expressionStart = new Vector2(expStartColumn,expStartLine);
					field.expressionEnd = new Vector2(expEndColumn,expEndLine);
				}
				//block.statements.Add(field);
				return field;
			}
			
			return null;
		}
	}
	
	public class AccessorDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class PropertyDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class MethodDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			int methodStart = pos;
			
			WordDef functionIdent = WordDef.Find(block,ref pos);
			if (functionIdent == null) return null;
			if (functionIdent.text != "function") return null;
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef nameIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (nameIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			if (nextChar != '(') return null;
			
			int methodArgumentStart = pos;
			string argumentBlockText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Expression,false);
			pos += argumentBlockText.Length+2;
			int methodArgumentEnd = pos-1;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			nextChar = block.GetChar(pos);
			
			TypeIdentDef typeIdent = null;
			if (nextChar == ':') {
				pos++;
				pos = block.GoToEndOfWhitespace(pos);
				typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
				if (typeIdent == null) return null;
				pos = block.GoToEndOfWhitespace(pos);
				nextChar = block.GetChar(pos);
			}
			
			if (nextChar != '{') return null;
			
			int bodyStart = pos;
			
			string bodyText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Scope,false);
			pos += bodyText.Length+2;
			
			MethodDef method = new MethodDef(block,methodStart,pos);
			method.statementBlock = new StatementBlock(block,method,bodyStart+1,pos-2);
			
			method.argumentBlock =  new MethodArgumentBlock(block,method,methodArgumentStart+1,methodArgumentEnd-1);
			
			method.name = nameIdent.name;
			method.resultingType = typeIdent;
			method.genericArgumentBlock = nameIdent.genericArgumentBlock;
			
			
			//Debug.Log("Method "+method.name+" "+method.startLine+" "+method.endLine);
			
			method.statementBlock.Parse();
			method.argumentBlock.Parse();
			
			startIndex = pos;
			
			//block.statements.Add(method);
			return method;
			
		}
	}
	
	public class ForEachDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
	
	public class ForDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			int forStart = pos;
			
			WordDef wordDef = WordDef.Find(block,ref pos);
			if (wordDef == null) return null;
			if (wordDef.text != "for") return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			if (nextChar != '(') return null;
			
			int argStart = pos;
			string argumentBlockText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Expression,false);
			pos += argumentBlockText.Length+2;
			int argEnd = pos-1;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			nextChar = block.GetChar(pos);
			
			int bodyStart = pos;
			
			if (nextChar == '{') {
				string bodyText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Scope,false);
				pos += bodyText.Length+2;
			}
			else {
				int statementEndPos = block.GoToCharSkippingScope(pos,';');
				if (block.IsEOF(statementEndPos)) return null;
				pos = statementEndPos+1;
			}
			
			ForDef forDef = new ForDef(block,forStart,pos);
			forDef.statementBlock = new StatementBlock(block,forDef,bodyStart+1,pos-2);
			
			forDef.argumentBlock = new StatementBlock(block,forDef,argStart+1,argEnd-1);
			
			//Debug.Log("Method "+method.name+" "+method.resultingType.text);
			
			forDef.statementBlock.Parse();
			forDef.argumentBlock.Parse();
			
			//Debug.Log("Foreach "+argumentVariables.Length+" "+forEachDef.argumentBlock.text);
			startIndex = pos;
			
			//block.statements.Add(forEachDef);
			return forDef;
			
		}
	}
	
	public class NamespaceDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			//block.GatherModifiers(ref pos);
			int nsStart = pos;
			
			WordDef nsIdent = WordDef.Find(block,ref pos);
			if (nsIdent == null) return null;
			
			if (nsIdent.text != "namespace") return null;
			
			
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (typeIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			if (nextChar != '{') return null;
			int bodyStart = pos;
			
			string bodyText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Scope,false);
			pos += bodyText.Length+2;
			
			NamespaceDef ns = new NamespaceDef(block,nsStart,pos);
			ns.statementBlock = new StatementBlock(block,ns,bodyStart+1,pos-2);
			
			
			ns.name = typeIdent.text;
			ns.resultingType = typeIdent;
			ns.namespaceIdent = typeIdent;
			ns.enclosingNamespace = ns.owner.GetEnclosingNamespace();
			
			if (ns.enclosingNamespace != null) {
				ns.name = ns.enclosingNamespace.name+"."+ns.name;
			}
			
			//Debug.Log("namespace "+ns.name+" "+ns.statementBlock.text);
			ns.statementBlock.Parse();
			
			ns.typeDefs = ns.statementBlock.GetStatmentsOfType<TypeDef>().ToList();
			ns.enums = ns.statementBlock.GetStatmentsOfType<EnumDef>().ToList();
			
			startIndex = pos;
			
			//block.statements.Add(ns);
			return ns;
		}
	}
	
	public class TypeDefFind:StatementFindRule {
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			int typeStart = pos;
			
			IdentDef ident = IdentDef.Find(block,ref pos);
			if (ident == null) return null;
			
			bool isClass = false;
			
			if (ident.text != "class" && ident.text != "struct") return null;
			if (ident.text == "class") {
				isClass = true;
			}
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (typeIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			//char nextChar = block.GetChar(pos);
			
			TypeIdentDef baseTypeIdent = null;
			List<TypeIdentDef> interfaces = new List<TypeIdentDef>();
			
			pos = block.GoToEndOfWhitespace(pos);
			
			IdentDef extendsIdent = IdentDef.Find(block,ref pos);
			
			if (extendsIdent != null && extendsIdent.text == "extends") {
				pos = block.GoToEndOfWhitespace(pos);
				TypeIdentDef btIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
				if (btIdent == null) return null;
				baseTypeIdent = btIdent;
				pos = block.GoToEndOfWhitespace(pos);
			}
			
			IdentDef implementsIdent = IdentDef.Find(block,ref pos);
			
			char nextChar = block.GetChar(pos);
			
			if (implementsIdent != null && implementsIdent.text == "implements") {
				BeginInterfaceLookup:
				pos = block.GoToEndOfWhitespace(pos);
				TypeIdentDef interfaceIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
				if (interfaceIdent != null) {
					interfaces.Add(interfaceIdent);
					pos = block.GoToEndOfWhitespace(pos);
					nextChar = block.GetChar(pos);
					if (nextChar == ',') goto BeginInterfaceLookup;
				}
			}
			pos = block.GoToEndOfWhitespace(pos);
			
			nextChar = block.GetChar(pos);
			
			if (nextChar != '{') return null;
			int bodyStart = pos;
			
			string bodyText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Scope,false);
			pos += bodyText.Length+2;
			
			TypeDef type = new TypeDef(block,typeStart,pos);
			type.statementBlock = new StatementBlock(block,type,bodyStart+1,pos-2);
			
			
			type.name = typeIdent.text;
			type.resultingType = typeIdent;
			type.baseType = baseTypeIdent;
			type.interfaces = interfaces;
			type.isClass = isClass;
			
			if (type.baseType == null) {
				type.baseType = new TypeIdentDef(block,0,0);
				type.baseType.name = "System.Object";
			}
			//Debug.Log("Class "+type.name+" "+type.startLine+" "+type.startColumn);
			
			type.statementBlock.Parse();
			
			type.typeDefs = type.statementBlock.GetStatmentsOfType<TypeDef>().ToList();
			type.methods = type.statementBlock.GetStatmentsOfType<MethodDef>().ToList();
			type.properties = type.statementBlock.GetStatmentsOfType<PropertyDef>().ToList();
			type.fields = type.statementBlock.GetStatmentsOfType<FieldDef>().ToList();
			type.enums = type.statementBlock.GetStatmentsOfType<EnumDef>().ToList();
			type.enclosingNamespace = block.GetEnclosingNamespace();
			type.enclosingType = block.GetEnclosingType();
			//Debug.Log(type.name+" "+type.enclosingNamespace);
			startIndex = pos;
			
			//block.statements.Add(type);
			return type;
		}
	}

}

