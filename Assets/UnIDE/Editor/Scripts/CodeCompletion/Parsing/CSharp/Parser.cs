using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UIDE.CodeCompletion;
using UIDE.SyntaxRules;

using UIDE.CodeCompletion.Parsing;

namespace UIDE.CodeCompletion.Parsing.CSharp {
	public class Parser:UIDE.CodeCompletion.Parsing.CommonParser.Parser {
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
	}
	
	public class TypeIdentDefFind:StatementFindRule {
		//public GenericArgumentBlock genericArgumentBlock;
		
		//public TypeIdentDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			IdentDef ident = IdentDef.Find(block,ref pos);
			if (ident == null) return null;
			
			TypeIdentDef typeIdentDef = new TypeIdentDef(block,ident.startPosition,pos-1);
			
			int afterWhitespacePos = block.GoToEndOfWhitespace(pos);
			char nextChar = block.GetChar(afterWhitespacePos);
			
			if (nextChar == '<') {
				pos = afterWhitespacePos;
				string body = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Generic,false);
				typeIdentDef.genericArgumentBlock = new GenericArgumentBlock(block,typeIdentDef,pos+1,pos+body.Length);
				pos = typeIdentDef.genericArgumentBlock.endPosition+1;
				typeIdentDef.endPosition = pos;
				typeIdentDef.UpdatePositions();
				typeIdentDef.genericArgumentBlock.Parse();
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
						//Debug.Log(typeIdentDef.text);
						break;
					}
				}
				//typeIdentDef.endPosition = pos;
				//typeIdentDef.UpdatePositions();
			}
			
			//Debug.Log(typeIdentDef.text);
			
			//typeIdentDef.name = typeIdentDef.text;
			
			startIndex = pos;
			
			return typeIdentDef;
			
			//return null;
		}
	}
	
	public class UsingClauseDefFind:StatementFindRule {
		//public GenericArgumentBlock genericArgumentBlock;
		
		//public UsingClauseDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			IdentDef ident = IdentDef.Find(block,ref pos);
			if (ident == null) return null;
			
			if (ident.text != "using") return null;
			
			
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
		//public MethodArgumentDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			List<WordDef> modifiers = block.GatherMethodArgumentModifiers(ref pos).ToList();
			
			TypeIdentDef typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (typeIdent == null) return null;
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			WordDef secondIdent = WordDef.Find(block,ref pos);
			if (secondIdent == null) return null;
			
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
				
				MethodArgumentDef arg = new MethodArgumentDef(block,typeIdent.startPosition,secondIdent.endPosition);
				arg.name = secondIdent.text;
				arg.resultingType = typeIdent;
				arg.modifiers = modifiers;
				if (nextChar == '=') {
					arg.isOptional = true;
				}
				//Debug.Log(arg.name+" "+arg.resultingType.text+" "+nextChar);
				
				//block.statements.Add(arg);
				return arg;
			}
			
			return null;
		}
	}
	
	public class LocalVariableDefFind:StatementFindRule {
		//public LocalVariableDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			
			TypeIdentDef typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (typeIdent == null) return null;
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			WordDef secondIdent = WordDef.Find(block,ref pos);
			if (secondIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
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
				
				LocalVariableDef variable = new LocalVariableDef(block,typeIdent.startPosition,secondIdent.endPosition);
				variable.name = secondIdent.text;
				variable.resultingType = typeIdent;
				if (isAssigned && typeIdent.text == "var") {
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
		//public StatementBlock statementBlock;
		//public EnumDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
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
		//public FieldDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			
			TypeIdentDef typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (typeIdent == null) return null;
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			IdentDef secondIdent = IdentDef.Find(block,ref pos);
			if (secondIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			bool isAssigned = nextChar == '=';
			
			if (nextChar == ';' || isAssigned) {
				//Debug.Log(typeIdent.text+" "+secondIdent.text);
				startIndex = block.GoToEndOfStatement(pos,false,false);
				
				FieldDef field = new FieldDef(block,typeIdent.startPosition,secondIdent.endPosition);
				field.name = secondIdent.text;
				field.resultingType = typeIdent;
				
				if (isAssigned && typeIdent.text == "var") {
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
		//public bool isGetter = false;
		//public bool isSetter = false;
		//public StatementBlock statementBlock;
		//public AccessorDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			int accessorStart = pos;
			
			IdentDef ident = IdentDef.Find(block,ref pos);
			if (ident == null) return null;
			
			bool isGetter = false;
			if (ident.text != "get" && ident.text != "set") return null;
			if (ident.text == "get") {
				isGetter = true;
			}
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			
			if (nextChar != '{') return null;
			int bodyStart = pos;
			
			string bodyText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Scope,false);
			pos += bodyText.Length+2;
			
			AccessorDef accessor = new AccessorDef(block,accessorStart,pos);
			accessor.statementBlock = new StatementBlock(block,accessor,bodyStart+1,pos-2);
			
			accessor.owner = block;
			accessor.isGetter = isGetter;
			accessor.isSetter = !isGetter;
			
			//Debug.Log("Method "+method.name+" "+method.resultingType.text);
			
			accessor.statementBlock.Parse();
			
			startIndex = pos;
			
			//block.statements.Add(accessor);
			return accessor;
		}
	}
	
	public class PropertyDefFind:StatementFindRule {
		//public PropertyBlock statementBlock;
		//public AccessorDef getter;
		//public AccessorDef setter;
		//public PropertyDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			int propertyStart = pos;
			
			TypeIdentDef typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (typeIdent == null) return null;
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef secondIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (secondIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			
			if (nextChar != '{') return null;
			int bodyStart = pos;
			
			string bodyText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Scope,false);
			pos += bodyText.Length+2;
			
			PropertyDef property = new PropertyDef(block,propertyStart,pos);
			property.statementBlock = new PropertyBlock(block,property,bodyStart+1,pos-2);
			
			property.name = secondIdent.name;
			property.resultingType = typeIdent;
			
			//Debug.Log("Method "+method.name+" "+method.resultingType.text);
			
			property.statementBlock.Parse();
			
			startIndex = pos;
			
			AccessorDef[] accessors = property.statementBlock.GetStatmentsOfType<AccessorDef>();
			for (int i = 0; i < accessors.Length; i++) {
				if (accessors[i].isGetter) {
					property.getter = accessors[i];
				}
				if (accessors[i].isSetter) {
					property.setter = accessors[i];
				}
			}
			
			//block.statements.Add(property);
			return property;
		}
	}
	
	public class MethodDefFind:StatementFindRule {
		//static public Regex regex;
		//public StatementBlock statementBlock;
		//public GenericArgumentBlock genericArgumentBlock;
		//public MethodArgumentBlock argumentBlock;
		//public List<LocalVariableDef> arguments = new List<LocalVariableDef>();
		
		//public MethodDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			block.GatherModifiers(ref pos);
			int methodStart = pos;
			
			TypeIdentDef typeIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (typeIdent == null) return null;
			
			if (!block.IsWhiteSpace(pos)) return null;
			pos = block.GoToEndOfWhitespace(pos);
			
			TypeIdentDef secondIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
			if (secondIdent == null) return null;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			char nextChar = block.GetChar(pos);
			
			if (nextChar != '(') return null;
			
			int methodArgumentStart = pos;
			string argumentBlockText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Expression,false);
			pos += argumentBlockText.Length+2;
			int methodArgumentEnd = pos-1;
			
			pos = block.GoToEndOfWhitespace(pos);
			
			nextChar = block.GetChar(pos);
			
			if (nextChar != '{') return null;
			int bodyStart = pos;
			
			string bodyText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Scope,false);
			pos += bodyText.Length+2;
			
			MethodDef method = new MethodDef(block,methodStart,pos);
			method.statementBlock = new StatementBlock(block,method,bodyStart+1,pos-2);
			
			method.argumentBlock =  new MethodArgumentBlock(block,method,methodArgumentStart+1,methodArgumentEnd-1);
			
			method.name = secondIdent.name;
			method.resultingType = typeIdent;
			method.genericArgumentBlock = secondIdent.genericArgumentBlock;
			
			
			//Debug.Log("Method "+method.name+" "+method.resultingType.text);
			
			method.statementBlock.Parse();
			method.argumentBlock.Parse();
			
			startIndex = pos;
			
			//block.statements.Add(method);
			return method;
			
		}
	}
	
	public class ForEachDefFind:StatementFindRule {
		//public StatementBlock statementBlock;
		//public StatementBlock argumentBlock;
		//public LocalVariableDef variable;
		//public ForEachDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
		public override Statement Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			int forEachStart = pos;
			
			WordDef wordDef = WordDef.Find(block,ref pos);
			if (wordDef == null) return null;
			if (wordDef.text != "foreach") return null;
			
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
			//string bodyText = ComplitionUtility.ExtractScope(block.file.text,pos,ExpressionBracketType.Scope,false);
			//pos += bodyText.Length+2;
			
			ForEachDef forEachDef = new ForEachDef(block,forEachStart,pos);
			forEachDef.statementBlock = new StatementBlock(block,forEachDef,bodyStart+1,pos-2);
			
			forEachDef.argumentBlock = new StatementBlock(block,forEachDef,argStart+1,argEnd-1);
			
			//Debug.Log("Method "+method.name+" "+method.resultingType.text);
			
			forEachDef.statementBlock.Parse();
			forEachDef.argumentBlock.Parse();
			
			LocalVariableDef[] argumentVariables = forEachDef.argumentBlock.GetStatmentsOfType<LocalVariableDef>();
			if (argumentVariables.Length > 0) {
				forEachDef.variable = argumentVariables[0];
			}
			//Debug.Log("Foreach "+argumentVariables.Length+" "+forEachDef.argumentBlock.text);
			startIndex = pos;
			
			//block.statements.Add(forEachDef);
			return forEachDef;
			
		}
	}
	
	public class ForDefFind:StatementFindRule {
		//public StatementBlock statementBlock;
		//public StatementBlock argumentBlock;
		
		//public ForDef(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
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
		//public StatementBlock statementBlock;
		//public TypeIdentDef namespaceIdent;
		//public NamespaceDef enclosingNamespace;
		//public List<TypeDef> typeDefs = new List<TypeDef>();
		//public List<EnumDef> enums = new List<EnumDef>();
		
		//public NamespaceDefFind(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
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
		//public StatementBlock statementBlock;
		//public TypeIdentDef baseType;
		//public List<TypeIdentDef> interfaces;
		//public bool isClass = false;
		//public NamespaceDef enclosingNamespace;
		//public TypeDef enclosingType;
		//public List<TypeDef> typeDefs = new List<TypeDef>();
		//public List<MethodDef> methods = new List<MethodDef>();
		//public List<PropertyDef> properties = new List<PropertyDef>();
		//public List<FieldDef> fields = new List<FieldDef>();
		//public List<EnumDef> enums = new List<EnumDef>();
		//
		//public TypeDefFind(StatementBlock block, int start, int end):base(block,start,end) {
			
		//}
		
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
			
			char nextChar = block.GetChar(pos);
			
			TypeIdentDef baseTypeIdent = null;
			List<TypeIdentDef> interfaces = new List<TypeIdentDef>();
			
			if (nextChar == ':') {
				BeginBaseTypeLookup:
				pos += 1;
				pos = block.GoToEndOfWhitespace(pos);
				TypeIdentDef btIdent = block.parser.FindStatement<TypeIdentDef>(block,ref pos);
				if (btIdent == null) return null;
				if (baseTypeIdent == null) {
					baseTypeIdent = btIdent;
				}
				interfaces.Add(btIdent);
				pos = block.GoToEndOfWhitespace(pos);
				nextChar = block.GetChar(pos);
				if (nextChar == ',') goto BeginBaseTypeLookup;
			}
			
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

