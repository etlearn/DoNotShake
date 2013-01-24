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
		public bool autoCompleteSubmitOnEnter = true;
		public bool shouldUseGenericAutoComplete = false;
		public bool useGenericAutoComplete {
			get {
				if (editor.editorWindow.generalSettings.GetForceGenericAutoComplete()) {
					return true;
				}
				return shouldUseGenericAutoComplete;
			}
		}
		
		protected bool useMultiThreading = true;
		
		public SyntaxRule() {
			//Debug.Log(this.GetType());
			//excludeFromLoading = true;
		}
		
		public virtual bool IsLineCommented(UIDELine line) {
			string trimmedText = line.rawText.TrimStart();
			if (trimmedText.StartsWith("//")) return true;
			return false;
		}
		public virtual void CommentLine(UIDELine line) {
			CommentLine(line,"");
		}
		public virtual void CommentLine(UIDELine line, string undoName) {
			UIDEUndoManager undoManager = editor.undoManager;
			
			Vector2 oldCursorPos = editor.cursor.GetVectorPosition();
			string originalText = line.rawText;
			string trimmedText = line.rawText.TrimStart();
			if (trimmedText.StartsWith("//")) return;
			
			int insertIndex = 0;
			UIDEElement firstElement = line.GetFirstNonWhitespaceElement();
			if (firstElement != null) {
				insertIndex = line.GetElementStartPos(firstElement);
			}
			else {
				insertIndex = line.rawText.Length;
			}
			
			line.rawText = line.rawText.Substring(0,insertIndex)+"//"+line.rawText.Substring(insertIndex);
			
			line.RebuildElements();
			
			Vector2 newCursorPos = editor.cursor.GetVectorPosition();
			
			if (undoName == "") {
				undoName = "Comment Line "+undoManager.GetUniqueID();
			}
			
			undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,line.index,originalText,line.rawText,oldCursorPos,newCursorPos);
		}
		public virtual void UncommentLine(UIDELine line) {
			UncommentLine(line,"");
		}
		public virtual void UncommentLine(UIDELine line, string undoName) {
			UIDEUndoManager undoManager = editor.undoManager;
			
			Vector2 oldCursorPos = editor.cursor.GetVectorPosition();
			string originalText = line.rawText;
			string trimmedText = line.rawText.TrimStart();
			if (!trimmedText.StartsWith("//")) return;
			
			int indexOfComment = line.rawText.IndexOf("//");
			
			string newText = line.rawText.Substring(0,indexOfComment);
			newText += line.rawText.Substring(indexOfComment+2);
			line.rawText = newText;
			
			line.RebuildElements();
			
			Vector2 newCursorPos = editor.cursor.GetVectorPosition();
			
			if (undoName == "") {
				undoName = "Uncomment Line "+undoManager.GetUniqueID();
			}
			
			undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,line.index,originalText,line.rawText,oldCursorPos,newCursorPos);
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
		
		public CompletionItem[] GetGenericCompletionItems() {
			List<CompletionItem> items = new List<CompletionItem>();
			
			HashSet<string> foundKeywords = new HashSet<string>();
			for (int i = 0; i < editor.doc.lineCount; i++) {
				UIDELine line = editor.doc.LineAt(i);
				bool isInspectingLine = i == editor.cursor.posY;
				for (int j = 0; j < line.elements.Count; j++) {
					UIDEElement element = line.elements[j];
					bool isInspectingElement = false;
					if (isInspectingLine) {
						int elementStart = line.GetElementStartPos(element);
						if (elementStart+element.rawText.Length == editor.cursor.posX) {
							isInspectingElement = true;
						}
					}
					if (isInspectingElement) {
						continue;
					}
					if (element.tokenDef.HasType("Word") && !foundKeywords.Contains(element.rawText)) {
						CompletionItem item = new CompletionItem();
						item.fullName = element.rawText;
						item.name = element.rawText;
						item.type = CompletionItemType.Keyword;
						items.Add(item);
					}
				}
			}
			
			return items.ToArray();
		}
		
		protected virtual List<UIDEElement> CreateStringAndCommentElements(UIDELine line, string str) {
			List<UIDEElement> elements = new List<UIDEElement>();
			UIDEElement currentElement = null;
			int c = 0;
			char previousPreviousChar = '\n';
			char previousChar = '\n';
			bool isComment = false;
			bool isBlockComment = false;
			bool isString = false;
			bool isCharString = false;
			while (c < str.Length) {
				char currentChar = str[c];
				//String
				if (!isComment && !isBlockComment && !isCharString) {
					
					if (isString) {
						if (currentChar == '"' && !(previousChar == '\\' && previousPreviousChar != '\\')) {
							isString = false;
							currentElement.rawText += currentChar.ToString();
							currentElement = null;
							previousPreviousChar = previousChar;
							previousChar = currentChar;
							c++;
							continue;
						}
					}
					else {
						if (currentChar == '"' && previousChar != '\\') {
							isString = true;
							currentElement = line.CreateElement("", "String");
							elements.Add(currentElement);
						}
					}
					
				}
				//Char string
				if (!isComment && !isBlockComment && !isString) {
					
					if (isCharString) {
						if (currentChar == '\'' && !(previousChar == '\\' && previousPreviousChar != '\\')) {
							isCharString = false;
							currentElement.rawText += currentChar.ToString();
							currentElement = null;
							previousPreviousChar = previousChar;
							previousChar = currentChar;
							c++;
							continue;
						}
					}
					else {
						if (currentChar == '\'' && previousChar != '\\') {
							isCharString = true;
							currentElement = line.CreateElement("", "String,CharString");
							elements.Add(currentElement);
						}
					}
					
				}
				if (c < str.Length-1) {
					char nextChar = str[c+1];
					//Block comments
					if (!isComment && !isString && !isCharString) {
						char cChar = '/';
						char nChar = '*';
						if (isBlockComment) {
							cChar = '*';
							nChar = '/';
						}
						
						if (currentChar == cChar && nextChar == nChar) {
							if (isBlockComment) {
								isBlockComment = false;
								currentElement.rawText += currentChar.ToString();
								currentElement.rawText += nextChar.ToString();
								currentElement.tokenDef = UIDETokenDefs.Get("Comment,Block,Contained");
								currentElement = null;
								previousPreviousChar = currentChar;
								previousChar = nextChar;
								c++;
								c++;
								continue;
							}
							else {
								isBlockComment = true;
								currentElement = line.CreateElement("", "Comment,Block,Start");
								elements.Add(currentElement);
								currentElement.rawText += currentChar.ToString();
								currentElement.rawText += nextChar.ToString();
								previousPreviousChar = currentChar;
								previousChar = nextChar;
								c++;
								c++;
								continue;
							}
						}
						else {
							if (!isBlockComment) {
								if (currentChar == '*' && nextChar == '/') {
									//a rogue */ so comment out everything up to it.
									elements = new List<UIDEElement>();
									currentElement = line.CreateElement("", "Comment,Block,End");
									currentElement.rawText = line.rawText.Substring(0,c+2);
									elements.Add(currentElement);
									currentElement = null;
									previousPreviousChar = currentChar;
									previousChar = nextChar;
									c++;
									c++;
									continue;
								}
							}
							//else {
							//	if (currentChar == '*' && nextChar == '/') {
							//		isBlockComment = false;
							//		currentElement.tokenDef = UIDETokenDefs.Get("Comment,Block,Contained");
							//		currentElement = null;
							//		previousPreviousChar = currentChar;
							//		previousChar = nextChar;
							//		c++;
							//		c++;
							//	}
							//}
						}
						
					}
					
					//Single line comments
					if (!isString && !isBlockComment && !isCharString) {
						if (currentChar == '/' && nextChar == '/') {
							isComment = true;
							currentElement = line.CreateElement("", "Comment,SingleLine");
							elements.Add(currentElement);
						}
					}
				}
				if (currentElement == null) {
					currentElement = line.CreateElement("", "");
					elements.Add(currentElement);
				}
				
				currentElement.rawText += currentChar.ToString();
				
				previousPreviousChar = previousChar;
				previousChar = currentChar;
				c++;
			}
			return elements;
		}
		
		public override void OnRebuildLineElements(UIDELine line) {
			//line.elements should contain a single element that contains all of its text and has canSplit = true
			List<UIDEElement> elements = line.elements;
			elements = CreateStringAndCommentElements(line,line.rawText);
			
			
			elements = line.CreateSubElements(elements,@"#(.|$)+","PreProcess");
			
			elements = line.CreateSubElements(elements,"\t+","WhiteSpace,Tab");
			elements = line.CreateSubElements(elements,@"\s+","WhiteSpace");
			
			elements = line.CreateSubElements(elements,@"(?<![0-9])[A-Za-z_]+(\w)*","Word");
			
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*\.?([0-9]+))((E|e)(\+|\-)([0-9]+))?(f|F)","Number,Float");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*\.([0-9]+))((E|e)(\+|\-)([0-9]+))?(d|D)?","Number,Double");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*)(d|D)","Number,Double");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*)(l|L)","Number,Int64");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*)","Number,Int32");
			//elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*\.?([0-9]+))((E|e)(\+|\-)([0-9]+))?(f|d|F|D)?","Number");
			
			elements = line.CreateSubElements(elements,@";","LineEnd");
			elements = line.CreateSubElements(elements,@"\.","Dot");
			
			
			line.elements = elements;
		}
		
		protected void UpdateMultilineFormattingGeneric() {
			bool isInBlockComment = false;
			UIDETokenDef multiBlockTokenDef = UIDETokenDefs.Get("Comment,Block,Start");
			//Debug.Log(multiBlockTokenDef.isBold);
			for (int i = 0; i < editor.doc.lineCount; i++) {
				if (i >= editor.doc.lineCount) break;
				UIDELine line = editor.doc.RealLineAt(i);
				if (line == null) continue;
				//lock (line) {
					line.overrideTokenDef = null;
					if (!isInBlockComment) {
						if (line.elements.Count > 0) {
							UIDEElement lastElement = line.GetLastNonWhitespaceElement(true);
							if (lastElement != null && lastElement.tokenDef.rawTypes == "Comment,Block,Start") {
								//Debug.Log(lastElement.line.rawText);
								isInBlockComment = true;
							}
						}
					}
					else {
						if (line.elements.Count > 0) {
							UIDEElement firstElement = line.GetFirstNonWhitespaceElement(true);
							if (firstElement != null && firstElement.tokenDef.rawTypes == "Comment,Block,End") {
								isInBlockComment = false;
							}
						}
						if (isInBlockComment) {
							line.overrideTokenDef = multiBlockTokenDef;
						}
					}
				//}
			}
		}
		
	}
}