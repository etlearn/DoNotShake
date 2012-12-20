using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Reflection;

using UIDE.SyntaxRules.ExpressionResolvers.CSharp;
using UIDE.SyntaxRules.ExpressionResolvers;
using UIDE.SyntaxRules;
using UIDE.CodeCompletion.Parsing;

//using UIDE.CodeCompletion.CSharp;
using UIDE.CodeCompletion;

namespace UIDE.SyntaxRules.Generic {
	[System.Serializable]
	public class SyntaxRuleGeneric:SyntaxRule {
		
		private bool useMultiThreadingParser = true;
		
		private Thread updateMultiLineFormattingThread;
		private bool wantsMultiLineFormattingUpdate = false;
		
		public SyntaxRuleGeneric() {
			isDefault = true;
			useGenericAutoComplete = true;
			//fileTypes = new string[] {".cs",".js"};
		}
		
		public override void OnTextEditorUpdate() {
			if (wantsMultiLineFormattingUpdate) {
				if (UpdateMultilineFormatting()) {
					//wantsMultiLineFormattingUpdate = false;
				} 
			}
		}
		
		public override void Start() {
			
		}
		
		public override void OnFocus() {
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnSwitchToTab() {
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnRebuildLines(UIDEDoc doc) {
			
		}
		
		public override void OnPostBackspace() {
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override void OnChangedCursorPosition(Vector2 pos) {
			
		}
		
		public override void OnPostEnterText(string text) {
			if (text == "\r" || text == "\n") {
				OnNewLine();
			}
			wantsMultiLineFormattingUpdate = true;
		}
		
		public override string OnPreEnterText(string text) {
			if (text == "}") {
				OnCloseCurly();
			}
			return text;
		}
		
		public void OnCloseCurly() {
			if (editor.cursor.posY <= 0) {
				return;
			}
			UIDELine line = editor.doc.LineAt(editor.cursor.posY);
			UIDELine previousLine = editor.doc.GetLastNoneWhitespaceOrCommentLine(editor.cursor.posY-1);
			if (previousLine == line) {
				return;
			}
			
			if (!line.IsLineWhitespace()) {
				return;
			}
			
			UIDEElement firstElement = previousLine.GetFirstNonWhitespaceElement();
			int previousLineStartPos = previousLine.GetElementStartPos(firstElement);
			int screenPos = previousLine.GetScreenPosition(previousLineStartPos);
			UIDEElement lastElement = previousLine.GetLastNonWhitespaceElement();
			
			int tabCount = screenPos/4;
			if (lastElement != null) {
				if (lastElement.tokenDef.HasType("LineEnd")) {
					tabCount -= 1;
				}
			}
			tabCount = Mathf.Max(tabCount,0);
			line.rawText = line.GetTrimmedWhitespaceText();
			for (int i = 0; i < tabCount; i++) {
				line.rawText = "\t"+line.rawText;
			}
			
			//line.rawText += startingText;
			line.RebuildElements();
			editor.cursor.posX = tabCount;
		}
		public void OnNewLine() {
			if (editor.cursor.posY <= 0) {
				return;
			}
			
			UIDELine line = editor.doc.LineAt(editor.cursor.posY);
			UIDELine previousLine = editor.doc.GetLastNoneWhitespaceOrCommentLine(editor.cursor.posY-1);
			if (previousLine == null) return;
			UIDEElement firstElement = previousLine.GetFirstNonWhitespaceElement();
			int previousLineStartPos = previousLine.GetElementStartPos(firstElement);
			int screenPos = previousLine.GetScreenPosition(previousLineStartPos);
			UIDEElement lastElement = previousLine.GetLastNonWhitespaceElement();
			
			string originalText = line.rawText;
			int tabCount = screenPos/4;
			if (lastElement != null && lastElement.rawText == "{") {
				tabCount += 1;
			}
			line.rawText = line.GetTrimmedWhitespaceText();
			for (int i = 0; i < tabCount; i++) {
				line.rawText = "\t"+line.rawText;
			}
			line.RebuildElements();
			
			Vector2 oldCursorPos = editor.cursor.GetVectorPosition();
			editor.cursor.posX = tabCount;
			Vector2 newCursorPos = editor.cursor.GetVectorPosition();
			//add another undo with the same name as the previous one so it gets grouped.
			if (editor.undoManager.undos.Count > 0) {
				string undoName = editor.undoManager.undos[editor.undoManager.undos.Count-1].groupID;
				editor.undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,line.index,originalText,line.rawText,oldCursorPos,newCursorPos);
			}
		}
		
		public override bool CheckIfStringIsKeyword(string str) {
			return !(UIDE.SyntaxRules.CSharp.Keywords.keywordHash.Get(str) == null);
		}
		public override bool CheckIfStringIsModifier(string str) {
			return !(UIDE.SyntaxRules.CSharp.Keywords.modifierHash.Get(str) == null);
		}
		public override bool CheckIfStringIsPrimitiveType(string str) {
			return !(UIDE.SyntaxRules.CSharp.Keywords.primitiveTypeHash.Get(str) == null);
		}
		
		public override UIDETokenDef GetKeywordTokenDef(UIDETokenDef tokenDef, string str) {
			if (CheckIfStringIsKeyword(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,Keyword");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			else if (CheckIfStringIsModifier(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,Modifier");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			else if (CheckIfStringIsPrimitiveType(str)) {
				UIDETokenDef keywordTokenDef = UIDETokenDefs.Get("Word,PrimitiveType");
				if (keywordTokenDef != null) {
					return keywordTokenDef;
				}
			}
			return tokenDef;
		}
		
		public override CompletionItem[] GetGlobalCompletionItems() {
			List<CompletionItem> items = new List<CompletionItem>();
			
			/*
			string[] keywords = UIDE.SyntaxRules.CSharp.Keywords.keywords;
			string[] modifiers = UIDE.SyntaxRules.CSharp.Keywords.modifiers;
			string[] primitiveTypes = UIDE.SyntaxRules.CSharp.Keywords.primitiveTypes;
			
			for (int i = 0; i < keywords.Length; i++) {
				CompletionItem item = CompletionItem.CreateFromKeyword(keywords[i]);
				items.Add(item);
			}
			for (int i = 0; i < modifiers.Length; i++) {
				CompletionItem item = CompletionItem.CreateFromModifier(modifiers[i]);
				items.Add(item);
			}
			for (int i = 0; i < primitiveTypes.Length; i++) {
				CompletionItem item = CompletionItem.CreateFromPrimitiveType(primitiveTypes[i]);
				items.Add(item);
			}
			*/
			
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
		
		public override bool CheckIfShouldLoad(UIDETextEditor textEditor) {
			return true;
		}
		
		private List<UIDEElement> CreateStringAndCommentElements(UIDELine line, string str) {
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
			elements = line.CreateSubElements(elements,@"(?<![0-9])[A-Za-z_]+(\w)*","Word,Keyword");
			elements = line.CreateSubElements(elements,@"(?<![0-9])[A-Za-z_]+(\w)*","Word,Modifier");
			elements = line.CreateSubElements(elements,@"(?<![0-9])[A-Za-z_]+(\w)*","Word,PrimitiveType");
			
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*\.?([0-9]+))((E|e)(\+|\-)([0-9]+))?(f|F)","Number,Float");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*\.([0-9]+))((E|e)(\+|\-)([0-9]+))?(d|D)?","Number,Double");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*)(d|D)","Number,Double");
			elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*)","Number,Int32");
			//elements = line.CreateSubElements(elements,@"(?<![A-Za-z_])([0-9]*\.?([0-9]+))((E|e)(\+|\-)([0-9]+))?(f|d|F|D)?","Number");
			
			elements = line.CreateSubElements(elements,@";","LineEnd");
			elements = line.CreateSubElements(elements,@"\.","Dot");
			
			
			line.elements = elements;
			
			wantsMultiLineFormattingUpdate = true;
		}
		
		public bool UpdateMultilineFormatting() {
			if (useMultiThreadingParser) {
				if (updateMultiLineFormattingThread != null && updateMultiLineFormattingThread.IsAlive) {
					wantsMultiLineFormattingUpdate = true;
					return false;
				}
				updateMultiLineFormattingThread = new Thread(UpdateMultilineFormattingActual);
				updateMultiLineFormattingThread.Start();
				wantsMultiLineFormattingUpdate = false;
			}
			else {
				UpdateMultilineFormattingActual();
				wantsMultiLineFormattingUpdate = false;
			}
			return true;
		}
		private void UpdateMultilineFormattingActual() {
			
			bool isInBlockComment = false;
			UIDETokenDef multiBlockTokenDef = UIDETokenDefs.Get("Comment,Block,Start");
			//Debug.Log(multiBlockTokenDef.isBold);
			for (int i = 0; i < editor.doc.lineCount; i++) {
				if (i >= editor.doc.lineCount) break;
				UIDELine line = editor.doc.RealLineAt(i);
				if (line == null) continue;
				lock (line) {
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
				}
			}
			
			//editor.editorWindow.Repaint();
		}	
		
	}
	
}