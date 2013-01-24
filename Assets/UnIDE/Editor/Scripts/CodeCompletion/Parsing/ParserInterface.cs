using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UIDE.CodeCompletion;
using UIDE.SyntaxRules;

using UIDE.CodeCompletion.Parsing;
//using UIDE.CodeCompletion.Parsing.CSharp;


namespace UIDE.CodeCompletion {
	public class ParserInterface:System.Object {
		//string rawText = "";
		public bool isParsing = false;
		public SourceFile lastSourceFile;
		public SyntaxRule lastSyntaxRule;
		
		public SourceFile Reparse(SyntaxRule rule, string text, string language) {
			SourceFile file = null;
			//lock (this) {
				isParsing = true;
				
				//float startTime = Time.realtimeSinceStartup;
				if (language == "cs") {
					Parsing.CSharp.Parser parser = new Parsing.CSharp.Parser();
					parser.sourceFileName = rule.editor.fileNameNoExt;
					parser.sourceFilePath = rule.editor.filePath;
					parser.Parse(text);
					file = parser.file;
				}
				else if (language == "us") {
					Parsing.Unityscript.Parser parser = new Parsing.Unityscript.Parser();
					parser.sourceFileName = rule.editor.fileNameNoExt;
					parser.sourceFilePath = rule.editor.filePath;
					parser.Parse(text);
					file = parser.file;
				}
				//Debug.Log(Time.realtimeSinceStartup-startTime);
				
				
				if (file != null) {
					lastSourceFile = file;
					lastSyntaxRule = rule;
				}
				
				isParsing = false;
			//}
			return file;
		}
		
		public string[] GetNamespacesVisibleInCurrentScope(Vector2 cursorPos,SyntaxRule rule) {
			SourceFile file = lastSourceFile;
			if (file == null) return new string[0];
			
			List<string> items = new List<string>();
			
			items.AddRange(GetNamespaceChain(cursorPos,rule));
			
			foreach (UsingClauseDef usingClause in file.usingClauses) {
				items.Add(usingClause.name);
				//Debug.Log(usingClause.name+" "+cursor.GetVectorPosition());
			}
			
			return items.ToArray();
		}
		
		public string[] GetNamespaceChain(Vector2 cursorPos,SyntaxRule rule) {
			SourceFile file = lastSourceFile;
			if (file == null) return new string[0];
			
			List<string> items = new List<string>();
			
			foreach (NamespaceDef ns in file.namespaces) {
				if (!IsCursorInScope(cursorPos,ns)) continue;
				string[] parts = ns.name.Split('.');
				string builtParts = "";
				foreach (string part in parts) {
					builtParts += part;
					items.Add(builtParts);
					builtParts += ".";
				}
			}

			return items.ToArray();
		}
		
		private TypeDef GetTypeAtPos(Vector2 cursorPos,SourceFile file) {
			
			TypeDef type = null;
			//Declared namespaces
			foreach (NamespaceDef ns in file.namespaces) {
				if (!IsCursorInScope(cursorPos,ns)) continue;
				
				foreach (TypeDef t in ns.typeDefs) {
					if (!IsCursorInScope(cursorPos,t)) continue;
					TypeDef foundNestedType = GetTypeInTypeAtPos(cursorPos,t);
					if (foundNestedType != null) {
						type = foundNestedType;
					}
					else {
						type = t;
					}
					if (type != null) {
						break;
					}
				}
			}
			if (type == null) {
				//Global namespace
				foreach (TypeDef t in file.typeDefs) {
					if (!IsCursorInScope(cursorPos,t)) continue;
					TypeDef foundNestedType = GetTypeInTypeAtPos(cursorPos,t);
					if (foundNestedType != null) {
						type = foundNestedType;
					}
					else {
						type = t;
					}
					if (type != null) {
						break;
					}
				}
			}
			return type;
		}
		private TypeDef GetTypeInTypeAtPos(Vector2 cursorPos, TypeDef type) {
			TypeDef outType = null;
			foreach (TypeDef t in type.typeDefs) {
				if (!IsCursorInScope(cursorPos,t)) continue;
				
				TypeDef foundNestedType = GetTypeInTypeAtPos(cursorPos,t);
				if (foundNestedType != null) {
					outType = foundNestedType;
					break;
				}
				else {
					outType = t;
					break;
				}
			}
			return outType;
		}
		
		private string GetNestedTypePathFromRoot(TypeDef type) {
			string outstr = type.name;
			if (type.enclosingType != null) {
				outstr = GetNestedTypePathFromRoot(type.enclosingType)+"."+outstr;
			}
			return outstr;
			//Debug.Log(outstr);
		}
		
		public string GetCurrentTypeFullName(Vector2 cursorPos,SyntaxRule rule) {
			SourceFile file = lastSourceFile;
			if (file == null) return "";
			
			
			TypeDef type = GetTypeAtPos(cursorPos,file);
			if (type != null) {
				//string nestedRootPath = GetNestedTypePathFromRoot(type);
				return type.name;
			}
			
			return "";
		}
		public string GetCurrentTypeNamespace(Vector2 cursorPos,SyntaxRule rule) {
			SourceFile file = lastSourceFile;
			if (file == null) return "";
			
			
			TypeDef type = GetTypeAtPos(cursorPos,file);
			if (type != null && type.enclosingNamespace != null) {
				//string nestedRootPath = GetNestedTypePathFromRoot(type);
				return type.enclosingNamespace.name;
			}
			
			return "";
		}
		
		public string GetCurrentTypeNestedTypePath(Vector2 cursorPos,SyntaxRule rule) {
			SourceFile file = lastSourceFile;
			if (file == null) return "";
			
			
			TypeDef type = GetTypeAtPos(cursorPos,file);
			if (type != null) {
				return GetNestedTypePathFromRoot(type);
			}
			
			return "";
		}
		
		public string GetCurrentTypeBaseTypeFullName(Vector2 cursorPos,SyntaxRule rule) {
			SourceFile file = lastSourceFile;
			if (file == null) return "";
			
			
			TypeDef type = GetTypeAtPos(cursorPos,file);
			
			if (type == null) {
				return "";
			}
			
			if (type.interfaces.Count == 0) {
				return type.name;
			}
			else {
				return type.interfaces[0].name;
			}
		}
		
		public string[] GetCurrentTypeInterfaceNames(Vector2 cursorPos,SyntaxRule rule) {
			SourceFile file = lastSourceFile;
			if (file == null) return new string[0];
			
			List<string> names = new List<string>();
			
			
			TypeDef type = GetTypeAtPos(cursorPos,file);
			
			if (type == null) {
				return new string[0];
			}
			for (int i = 0; i < type.interfaces.Count; i++) {
				names.Add(type.interfaces[i].name);
			}
			return names.ToArray();
		}
		
		public CompletionItem[] GetCurrentVisibleItems(Vector2 cursorPos,SyntaxRule rule) {
			SourceFile file = lastSourceFile;
			if (file == null) return new CompletionItem[0];
			
			List<CompletionItem> items = new List<CompletionItem>();
			
			//Declared namespaces
			foreach (NamespaceDef ns in file.namespaces) {
				if (!IsCursorInScope(cursorPos,ns)) continue;
				
				foreach (EnumDef en in ns.enums) {
					items.Add(ItemFromEnum(en));
				}
				
				foreach (TypeDef t in ns.typeDefs) {
					if (!IsCursorInScope(cursorPos,t)) continue;
					
					items.AddRange(GatherTypeItems(cursorPos, t));
				}
			}
			
			//Global namespace
			foreach (TypeDef t in file.typeDefs) {
				//Debug.Log(t.name+" "+t.startLine+" "+t.endLine);
				if (!IsCursorInScope(cursorPos,t)) continue;
				
				foreach (EnumDef en in file.enums) {
					items.Add(ItemFromEnum(en));
				}
				
				items.AddRange(GatherTypeItems(cursorPos, t));
			}
			
			//GetExpressionAtPosition(file, rule);
			
			return items.ToArray();
		}
		
		private List<CompletionItem> GatherTypeItems(Vector2 cursorPos, TypeDef type) {
			List<CompletionItem> items = new List<CompletionItem>();
			//Debug.Log(type.name+" "+type.properties.Count);
			//Debug.Log(type.enums.Count);
			bool isInNestedType = false;
			foreach (TypeDef t in type.typeDefs) {
				if (!IsCursorInScope(cursorPos,t.statementBlock)) continue;
				isInNestedType = true;
				items.AddRange(GatherTypeItems(cursorPos,t));
			}
			if (isInNestedType) {
				//Debug.Log(type.name);
				return items;
			}
			//Debug.Log(type.name);
			foreach (EnumDef en in type.enums) {
				//Debug.Log(en.name);
				items.Add(ItemFromEnum(en));
			}
		
			foreach (MethodDef m in type.methods) {
				//Debug.Log(type.name+" "+m.name+" "+IsCursorInScope(cursorPos,m));
				if (!IsCursorInScope(cursorPos,m)) continue;
				
				foreach (MethodArgumentDef ma in m.argumentBlock.statements) {
					CompletionItem item = ItemFromMethodArgument(ma);
					items.Add(item);
				}
				items.AddRange(GatherVariablesInStatements(cursorPos,m.statementBlock.statements.ToArray()));
			}
			
			foreach (MethodDef m in type.methods) {
				CompletionMethod item = ItemFromMethod(m);
				items.Add(item);
			}
			foreach (FieldDef f in type.fields) {
				items.Add(ItemFromField(f));
			}
			foreach (PropertyDef p in type.properties) {
				items.Add(ItemFromProperty(p));
				if (p.getter != null && IsCursorInScope(cursorPos,p.getter)) {
					items.AddRange(GatherAccessorVariables(cursorPos,p.getter));
				}
				if (p.setter != null && IsCursorInScope(cursorPos,p.setter)) {
					items.AddRange(GatherAccessorVariables(cursorPos,p.setter));
				}
			}
			
			return items;
		}
		
		private List<CompletionItem> GatherAccessorVariables(Vector2 cursorPos,AccessorDef accessor) {
			List<CompletionItem> items = new List<CompletionItem>();
			items.AddRange(GatherVariablesInStatements(cursorPos,accessor.statementBlock.statements.ToArray()));
			return items;
		}
		
		private List<CompletionItem> GatherVariablesInStatements(Vector2 cursorPos, Statement[] statements) {
			List<CompletionItem> items = new List<CompletionItem>();
			foreach (Statement st in statements) {
				if (st == null) continue;
				
				if (IsCursorInScope(cursorPos,st)) {
					if (st.IsTypeOf<StatementBlock>()) {
						StatementBlock b = (StatementBlock)st;
						items.AddRange(GatherVariablesInStatements(cursorPos,b.statements.ToArray()));
					}
					
					if (st.IsTypeOf<ForEachDef>()) {
						ForEachDef b = (ForEachDef)st;
						items.AddRange(GatherVariablesInStatements(cursorPos,b.argumentBlock.statements.ToArray()));
						items.AddRange(GatherVariablesInStatements(cursorPos,b.statementBlock.statements.ToArray()));
					}
					if (st.IsTypeOf<ForDef>()) {
						ForDef b = (ForDef)st;
						items.AddRange(GatherVariablesInStatements(cursorPos,b.argumentBlock.statements.ToArray()));
						items.AddRange(GatherVariablesInStatements(cursorPos,b.statementBlock.statements.ToArray()));
					}
				}
				
				if (st.IsTypeOf<LocalVariableDef>()) {
					if (IsPositionAfterElement(cursorPos,st)) {
						LocalVariableDef localVar = (LocalVariableDef)st;
						
						if (IsPositionAfterElement(cursorPos,localVar.vectorEnd)) {
							CompletionItem item = ItemFromLocalVariable(localVar);
							items.Add(item);
						}
					}
				}
			}
			return items;
		}
		
		//Item generation
		static public CompletionMethod ItemFromMethod(MethodDef method) {
			CompletionMethod item = new CompletionMethod();
			item.name = method.name;
			
			item.fullName = method.name;
			if (method.resultingType != null) {
				item.fullTypeName = method.resultingType.text;
			}
			item.namespaceString = "";
			
			item.type = CompletionItemType.Method;
			item.declaredPosition = method.vectorStart;
			
			if (method.argumentBlock != null) {
				foreach (MethodArgumentDef ma in method.argumentBlock.statements) {
					CompletionItem arg = ItemFromMethodArgument(ma);
					item.arguments.Add(arg);
				}
			}
			if (method.genericArgumentBlock != null) {
				foreach (TypeIdentDef ti in method.genericArgumentBlock.statements) {
					CompletionItem arg = ItemFromTypeIdent(ti);
					item.genericArguments.Add(arg);
				}
			}
			return item;
		}
		
		static public CompletionItem ItemFromTypeIdent(TypeIdentDef ti) {
			CompletionItem item = new CompletionItem();
			item.name = ti.name;
			item.fullName = ti.name;
			item.fullTypeName = ti.text;
			item.namespaceString = "";
			item.type = CompletionItemType.Type;
			item.declaredPosition = ti.vectorStart;
			
			if (ti.genericArgumentBlock != null) {
				foreach (TypeIdentDef subTI in ti.genericArgumentBlock.statements) {
					CompletionItem arg = ItemFromTypeIdent(subTI);
					item.genericArguments.Add(arg);
				}
			}
			return item;
		}
		
		static public CompletionItem ItemFromEnum(EnumDef en) {
			CompletionItem item = new CompletionItem();
			item.name = en.name;
			item.fullName = en.name;
			item.fullTypeName = en.text;
			item.namespaceString = "";
			item.type = CompletionItemType.Enum;
			item.declaredPosition = en.vectorStart;
			
			foreach (LocalVariableDef lv in en.statementBlock.statements) {
				CompletionItem lvItem = ItemFromLocalVariable(lv);
				lvItem.fullTypeName = "System.Int32";
				item.enumItems.Add(lvItem);
			}
			return item;
		}
		
		static public CompletionItem ItemFromMethodArgument(MethodArgumentDef methodArgument) {
			//Debug.Log(variable.name+" "+variable.resultingType.name);
			CompletionItem item = new CompletionItem();
			item.name = methodArgument.name;
			item.fullName = methodArgument.name;
			if (methodArgument.resultingType != null) {
				item.fullTypeName = methodArgument.resultingType.text;
			}
			item.namespaceString = "";
			item.type = CompletionItemType.Parameter;
			item.declaredPosition = methodArgument.vectorStart;
			
			item.isOptionalArgument = methodArgument.isOptional;
			for (int i = 0; i < methodArgument.modifiers.Count; i++) {
				item.modifiers.Add(methodArgument.modifiers[i].text);
			}
			//Debug.Log(methodArgument.text+" "+item.fullTypeName);
			if (methodArgument.resultingType != null) {
				if (methodArgument.resultingType.genericArgumentBlock != null) {
					foreach (TypeIdentDef ti in methodArgument.resultingType.genericArgumentBlock.statements) {
						CompletionItem arg = ItemFromTypeIdent(ti);
						item.genericArguments.Add(arg);
					}
				}
				item.variableDeclarationBlock = methodArgument.resultingType.text.Replace(" ","").Replace("\t","").Replace("\n","").Replace("\r","");
				//Debug.Log(item.variableDeclarationBlock);
			}
			return item;
		}
		
		static public CompletionItem ItemFromLocalVariable(LocalVariableDef variable) {
			//Debug.Log(variable.name+" "+variable.resultingType.name);
			CompletionItem item = new CompletionItem();
			item.name = variable.name;
			item.fullName = variable.name;
			if (variable.resultingType != null) {
				item.fullTypeName = variable.resultingType.text;
			}
			item.namespaceString = "";
			item.type = CompletionItemType.LocalVar;
			item.declaredPosition = variable.vectorStart;
			if (variable.inferredType) {
				item.inferredType = true;
				item.expressionStart = variable.expressionStart;
				item.expressionEnd = variable.expressionEnd;
			}
			
			if (variable.resultingType != null) {
				if (variable.resultingType.genericArgumentBlock != null) {
					foreach (TypeIdentDef ti in variable.resultingType.genericArgumentBlock.statements) {
						CompletionItem arg = ItemFromTypeIdent(ti);
						item.genericArguments.Add(arg);
					}
				}
				item.variableDeclarationBlock = variable.resultingType.text.Replace(" ","").Replace("\t","").Replace("\n","").Replace("\r","");
				//Debug.Log(item.variableDeclarationBlock);
			}
			return item;
		}
		
		static public CompletionItem ItemFromProperty(PropertyDef property) {
			CompletionItem item = new CompletionItem();
			item.name = property.name;
			item.fullName = property.name;
			if (property.resultingType != null) {
				item.fullTypeName = property.resultingType.text;
			}
			item.namespaceString = "";
			item.type = CompletionItemType.Property;
			item.declaredPosition = property.vectorStart;
			
			if (property.resultingType != null) {
				if (property.resultingType.genericArgumentBlock != null) {
					foreach (TypeIdentDef ti in property.resultingType.genericArgumentBlock.statements) {
						CompletionItem arg = ItemFromTypeIdent(ti);
						item.genericArguments.Add(arg);
					}
				}
				item.variableDeclarationBlock = property.resultingType.text.Replace(" ","").Replace("\t","").Replace("\n","").Replace("\r","");
			}
			return item;
		}
		
		static public CompletionItem ItemFromField(FieldDef field) {
			CompletionItem item = new CompletionItem();
			item.name = field.name;
			item.fullName = field.name;
			if (field.resultingType != null) {
				item.fullTypeName = field.resultingType.text;
			}
			item.namespaceString = "";
			item.type = CompletionItemType.Field;
			item.declaredPosition = field.vectorStart;
			if (field.inferredType) {
				item.inferredType = true;
				item.expressionStart = field.expressionStart;
				item.expressionEnd = field.expressionEnd;
			}
			
			if (field.resultingType != null) {
				if (field.resultingType.genericArgumentBlock != null) {
					foreach (TypeIdentDef ti in field.resultingType.genericArgumentBlock.statements) {
						CompletionItem arg = ItemFromTypeIdent(ti);
						item.genericArguments.Add(arg);
					}
				}
				item.variableDeclarationBlock = field.resultingType.text.Replace(" ","").Replace("\t","").Replace("\n","").Replace("\r","");
			}
			return item;
		}
		
		
		static public bool IsPositionAfterElement(Vector2 pos,LanguageElement ns) {
			
			int cursorX = ((int)pos.x);
			int cursorY = ((int)pos.y);
			
			if (cursorY > ns.startLine) return true;
			if (cursorY == ns.startLine) {
				if (cursorX > ns.endColumn) return true;
			}
			return false;
		}
		
		static public bool IsPositionAfterElement(Vector2 pos, Vector2 otherPos) {
			
			int cursorX = ((int)pos.x);
			int cursorY = ((int)pos.y);
			
			int otherY = (int)otherPos.y;
			int otherX = (int)otherPos.x;
			
			if (cursorY > otherY) return true;
			if (cursorY == otherY) {
				if (cursorX > otherX) return true;
			}
			return false;
		}
		
		static public bool IsCursorInScope(Vector2 pos,LanguageElement ns) {
			bool isCursorInScope = false;
			
			int cursorX = ((int)pos.x);
			int cursorY = ((int)pos.y);
			if (cursorY >= ns.startLine && cursorY <= ns.endLine) {
				if (ns.startLine == ns.endLine) {
					if (cursorY == ns.startLine) {
						if (cursorX >= ns.startColumn && cursorX <= ns.endColumn) {
							isCursorInScope = true;
						}
					}
				}
				else {
					
					if (cursorY == ns.startLine) {
						if (cursorX >= ns.startColumn) {
							isCursorInScope = true;
						}
					}
					else if (cursorY == ns.endLine) {
						if (cursorX <= ns.endColumn) {
							isCursorInScope = true;
						}
					}
					else {
						isCursorInScope = true;
					}
				}
			}
			return isCursorInScope;
		}
		
	}
}