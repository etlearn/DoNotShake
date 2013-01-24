using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;

using UIDE.SyntaxRules;
using UIDE.CodeCompletion;
using CUtil = UIDE.CodeCompletion.ComplitionUtility;

namespace UIDE.SyntaxRules.ExpressionResolvers.CSharp {
	static public class ExpressionResolver:System.Object {
		
		static public UIDETextEditor editor;
		static private string PrimitiveElementToTypeName(UIDEElement element) {
			if (element.tokenDef.useParsableString) {
				string str = element.tokenDef.parsableString;
				if (str.StartsWith("new ")) {
					str = str.Substring("new ".Length);
					str = "new|"+str;
				}
				return str;
			}
			//bool isString = element.tokenDef.HasType("String");
			//bool isNumber =  element.tokenDef.HasType("Number");
			//if (isString) {
			//	return "System.String";
			//}
			//else if (isNumber) {
				//Need more number type detection.
			//	return "System.Int32";
			//}
			return "";
		}
		
		
		
		static private string ExtractArgumentsToLiterialChain(ExpressionInfo input) {
			
			if (input.text.Length < 3) {
				return "";
			}
			input.startPosition = editor.doc.IncrementPosition(input.startPosition,1);
			input.endPosition = editor.doc.IncrementPosition(input.endPosition,-1);
			//if (input.startPosition == input.endPosition) {
			//	return "";
			//}
			//UIDELine startLine = editor.doc.RealLineAt((int)input.startPosition.y);
			//UIDEElement startElement = startLine.GetElementAt((int)input.startPosition.x);
			
			string output = "";
			string walkingHistory = "";
			Vector2 pos = input.startPosition;
			bool statementBeginning = true;
			bool walkingLiteral = false;
			//Vector2 statmentStart = pos;
			bool wasExpressionOrFunction = false;
			bool isInGenericScope = false;
			
			while (true) {
				UIDELine line = editor.doc.RealLineAt((int)pos.y);
				UIDEElement element = line.GetElementAt((int)pos.x);
				int elementPosition = line.GetElementStartPos(element);
				char character = line.rawText[(int)pos.x];
				
				bool isString = element.tokenDef.HasType("String");
				bool isNumber =  element.tokenDef.HasType("Number");
				bool isWhiteSpace = element.tokenDef.HasType("WhiteSpace");
				bool isComment = element.tokenDef.HasType("Comment");
				
				bool isWord = element.tokenDef.HasType("Word");
				bool isDot = element.tokenDef.HasType("Dot");
				bool isDefaultText = element.tokenDef.HasType("DefaultText");
				
				if (character == '<' && isDefaultText) {
					string matchString = GetMatch(@"(?<match><(\w+(,)?)+>)", pos, (int)pos.x, line.rawText.Length-(int)pos.x, "match");
					
					if (matchString != "") {
						isInGenericScope = true;
					}
				}
				
				if (!isWhiteSpace && !isComment) {
					if (isInGenericScope) {
						walkingHistory += character;
					}
					else {
						
						
						if (statementBeginning) {
							if (element.rawText == "new") {
								output += "new|";
								pos.x += element.rawText.Length;
								continue;
							}
							else if (element.rawText == "out") {
								output += "out|";
								pos.x += element.rawText.Length;
								continue;
							}
							else if (element.rawText == "ref") {
								output += "ref|";
								pos.x += element.rawText.Length;
								continue;
							}
							//Starts with a literal.
							walkingLiteral = true;
							//statmentStart = pos;
							walkingHistory = "";
							statementBeginning = false;
						}
						
						
						if (walkingLiteral) {
							//if (walkingHistory.Length == 0 && (isString || isNumber)) {
							if (walkingHistory.Length == 0 && element.tokenDef.useParsableString) {
								UIDEElement nextElement = line.GetElementAt(elementPosition+element.rawText.Length);
								string typeName = PrimitiveElementToTypeName(element);
								output += typeName;
								
								pos.x = elementPosition+(element.rawText.Length-1);
								if (!nextElement.tokenDef.HasType("Dot")) {
									walkingLiteral = false;
								}
								else {
									output += ".";
								}
							}
							System.Type primitiveType = ComplitionUtility.GetTypeFromTypeAlias(element.rawText);
							if (primitiveType != null) {
								output += primitiveType.FullName;
								pos.x = elementPosition+(element.rawText.Length-1);
							}
							else {
								if (isDot || isWord) {
									walkingHistory += character;
								}
							}
							bool isBracket = CUtil.IsOpenBracket(character);
							bool isParentheses = CUtil.IsOpenParentheses(character);
							ExpressionBracketType bracketType = ExpressionBracketType.Expression;
							if (isBracket) {
								bracketType = ExpressionBracketType.Bracket;
							}
							if (isParentheses||isBracket) {
								bool isFunction = false;
								if (isParentheses && walkingHistory.Length > 0) {
									isFunction = true;
								}
								ExpressionInfo res = new ExpressionInfo();
								res.startPosition = pos;
								res.endPosition = pos;
								res = CountToExpressionEnd(res,1,bracketType);
								
								Vector2 nextPos = editor.doc.IncrementPositionToNextNonWhitespace(res.endPosition,1);
								UIDELine nextLine = editor.doc.RealLineAt((int)nextPos.y);
								char nextChar = nextLine.rawText[(int)nextPos.x];
								
								//Debug.Log(character+" "+line.rawText[(int)res.endPosition.x]);
								string literalChain = "";
								if (isFunction||isBracket) {
									//res.startPosition = editor.doc.IncrementPosition(res.startPosition,1);
									//res.endPosition = editor.doc.IncrementPosition(res.endPosition,-1);
									literalChain = ExtractLiteralChain(res,false,true,false);
									literalChain = walkingHistory+literalChain;
									
									walkingHistory = "";
								}
								else {
									literalChain = ExtractLiteralChain(res);
								}
								//Debug.Log(literalChain+" "+res.startPosition+" "+res.endPosition);
								output += literalChain;
								
								if (nextChar != '.' && nextChar != '[') {
									walkingLiteral = false;
									walkingHistory = "";
									output += ",";
									wasExpressionOrFunction = true;
								}
								pos = res.endPosition;
								
								//pos = editor.doc.IncrementPosition(res.endPosition,-1);
								//pos = editor.doc.IncrementPosition(pos,-1);
							}
							else {
								if (walkingHistory != "" && !isWord && !isDot && !isString && !isNumber) {
									walkingLiteral = false;
								}
							}
						}
						
						if (!statementBeginning) {
							bool isOpenBracket = CUtil.IsOpenBracket(character);
							bool isOpenParentheses = CUtil.IsOpenParentheses(character);
							ExpressionBracketType bracketOpenType = ExpressionBracketType.Expression;
							if (isOpenBracket) {
								bracketOpenType = ExpressionBracketType.Bracket;
							}
							if (isOpenBracket || isOpenParentheses) {
								pos = SimpleMoveToEndOfScope(pos,1,bracketOpenType);
								//Debug.Log(editor.doc.GetCharAt(pos));
								continue;
							}
						}
						if (character == ',') {
							//Debug.Log(walkingHistory);
							if (!wasExpressionOrFunction) {
								output += walkingHistory+",";
							}
							wasExpressionOrFunction = false;
							walkingHistory = "";
							statementBeginning = true;
						}
					}
					
				}
				
				if (isInGenericScope && character == '>') {
					isInGenericScope = false;
				}
				
				if (!editor.doc.CanIncrementPosition(pos,1)) {
					break;
				}
				if (!editor.doc.PositionLessThan(pos,input.endPosition)) {
					break;
				}
				pos = editor.doc.IncrementPosition(pos,1);
			}
			
			output += walkingHistory;
			
			output = output.TrimEnd(',');
			while (output.Contains(",,")) {
				output = output.Replace(",,",",");
			}
			
			return output;
			
		}
		
		static private string ExtractLiteralChain(ExpressionInfo input) {
			return ExtractLiteralChain(input,false,false,false);
		}
		static private string ExtractLiteralChain(ExpressionInfo input, bool breakOnCloseExpression, bool forceArgument,bool isBrackets) {
			//if (input.text == null || input.text.Length == 0) {
			//	return "";
			//}
			System.Text.StringBuilder output = new System.Text.StringBuilder();
			
			
			Vector2 pos = input.startPosition;
			
			
			bool isInGenericScope = false;
			string currentType = "";
			
			bool hitFirstChar = false;
			
			while (true) {
				
				UIDELine line = editor.doc.RealLineAt((int)pos.y);
				UIDEElement element = line.GetElementAt((int)pos.x);
				
				if (pos.x >= line.rawText.Length || pos.x < 0) break;
				
				char currentChar = line.rawText[(int)pos.x];
				
				if (!forceArgument && currentChar == '<' && element != null && element.tokenDef.HasType("DefaultText")) {
					string matchString = GetMatch(@"(?<match><(\w+(,)?)+>)", pos, (int)pos.x, line.rawText.Length-(int)pos.x, "match");
					
					if (matchString != "") {
						isInGenericScope = true;
					}
				}
				
				if (element != null) {
					
					//&& element != currentElement
					UIDETokenDef td = element.tokenDef;
					bool isString = td.HasType("String");
					bool isComment = td.HasType("Comment");
					bool isWord = td.HasType("Word");
					bool isNumber = td.HasType("Number");
					bool isDot = td.HasType("Dot");
					bool isWhiteSpace = td.HasType("WhiteSpace");
					bool isDefaultText = td.HasType("DefaultText");
					
					bool wasCloseGeneric = output.Length > 0 && output.ToString()[output.Length-1] == '>';
					
					bool isFirstWord = !hitFirstChar && isWord;
					
					//bool isCloseParentheses = isDefaultText && CUtil.IsCloseParentheses(currentChar);
					//bool isCloseBrackets = isDefaultText && CUtil.IsCloseBracket(currentChar);
					
					bool isOpenArguments = isDefaultText && hitFirstChar && CUtil.IsOpenParentheses(currentChar);
					isOpenArguments &= (wasCloseGeneric || currentType == "Word" || currentType == "WhiteSpace" || currentType == "Dot");
					
					//bool isOpenArgumentBrackets = isDefaultText && hitFirstChar && CUtil.IsOpenBracket(currentChar);
					bool isOpenArgumentBrackets = isDefaultText && CUtil.IsOpenBracket(currentChar);
					
					bool isOpenFirstExpression = isDefaultText && !hitFirstChar && !wasCloseGeneric && CUtil.IsOpenParentheses(currentChar);
					
					bool isOpenExpression = isDefaultText && !isOpenArguments && !wasCloseGeneric && CUtil.IsOpenParentheses(currentChar);
					bool isOpenBrackets = isDefaultText && !isOpenArguments && !wasCloseGeneric && CUtil.IsOpenBracket(currentChar);
					
					bool isPrePlusMinus = !hitFirstChar && (currentChar == '-' || currentChar == '+');
					
					if (forceArgument) {
						
						if (isOpenExpression) {
							isOpenArguments = true;
						}
						else if (isOpenBrackets) {
							isOpenArgumentBrackets = true;
						}
					}
					
					if (breakOnCloseExpression && !isComment && !isString) {
						
						int elementPos = line.GetElementStartPos(element);
						UIDEElement nextElement = line.GetElementAt(elementPos+element.rawText.Length);
						if (element.rawText.Contains(")") && !nextElement.tokenDef.HasType("Dot")) {
							break;
						}
					}
					
					if (isOpenArguments) {
						
						ExpressionInfo res = input;
						res.startPosition = pos;
						
						res = CountToExpressionEnd(res,1,ExpressionBracketType.Expression);
						
						string argumentString = ExtractArgumentsToLiterialChain(res);
						
						output.Append("("+argumentString+")");
						pos = res.endPosition;
						
						pos = editor.doc.IncrementPosition(pos,1);
						if (!editor.doc.CanIncrementPosition(pos,1)) {
							break;
						}
						if (pos == input.endPosition || !editor.doc.PositionLessThan(pos,input.endPosition)) {
							break;
						}
						
						currentType = "";
						
						continue;
					}
					//Debug.Log(input.text);
					if (isOpenArgumentBrackets) {
						//Debug.Log(currentChar);
						ExpressionInfo res = input;
						res.startPosition = pos;
						
						res = CountToExpressionEnd(res,1,ExpressionBracketType.Bracket);
						
						string argumentString = ExtractArgumentsToLiterialChain(res);
						//Debug.Log(argumentString);
						output.Append("["+argumentString+"]");
						pos = res.endPosition;
						
						pos = editor.doc.IncrementPosition(pos,1);
						if (!editor.doc.CanIncrementPosition(pos,1)) {
							break;
						}
						if (pos == input.endPosition || !editor.doc.PositionLessThan(pos,input.endPosition)) {
							break;
						}
						
						currentType = "";
						
						continue;
					}
					
					if (isOpenFirstExpression) {
						
						//hitFirstChar = true;
						ExpressionInfo res = input;
						res.startPosition = pos;
						
						res = CountToExpressionEnd(res,1,ExpressionBracketType.Expression);
						
						string castType = GetMatch(@"^(\(|\s)*(\(+)(?<type>([a-zA-Z_\.]+\w*))\)(?!\.)", pos, (int)pos.x, (int)res.endPosition.x-(int)pos.x, "type");
						
						if (castType.Length > 0) {
							output.Append(castType);
							pos = res.endPosition;
							continue;
							//break;
						}
						
						pos = res.endPosition;
						
						res.endPosition = editor.doc.IncrementPosition(res.endPosition,-1);
						res.startPosition = editor.doc.IncrementPosition(res.startPosition,1);
						string literalChain = ExtractLiteralChain(res,false,false,false);
						output.Append(literalChain);
						
						
						//pos = editor.doc.IncrementPosition(pos,1);
						
						if (!editor.doc.CanIncrementPosition(pos,1)) {
							break;
						}
						if (pos == input.endPosition || !editor.doc.PositionLessThan(pos,input.endPosition)) {
							break;
						}
						
						currentType = "";
						//currentElement = null;
						continue;
					}
					
					
					//Debug.Log(element.rawText+" "+element.tokenDef.rawTypes);
					if (isFirstWord) {
						hitFirstChar = true;
						if (element.rawText == "new") {
							output.Append("new|");
							//currentElement = element;
							//hasModifier = true;
							pos.x += element.rawText.Length;
							//endModifierPos = pos;
							continue;
						}
						else if (element.rawText == "out") {
							output.Append("out|");
							pos.x += element.rawText.Length;
							continue;
						}
						else if (element.rawText == "ref") {
							output.Append("ref|");
							pos.x += element.rawText.Length;
							continue;
						}
					}
					
					if (isInGenericScope) {
						if ((currentChar == '<' || currentChar == '>' || currentChar == ',')) {
							output.Append(currentChar);
							hitFirstChar = true;
							
						}
					}
					else {
						if (isWord) {
							currentType = "Word";
						}
						else if (isNumber) {
							currentType = "Number";
						}
						else if (isDot) {
							currentType = "Dot";
						}
						else if (isString) {
							currentType = "String";
						}
						else if (isComment) {
							currentType = "Comment";
						}
						else if (isWhiteSpace) {
							currentType = "WhiteSpace";
						}
						//currentElement = element;
					}
					if (element.tokenDef.useParsableString) {
						//Debug.Log(element.tokenDef.rawTypes);
						output.Append(PrimitiveElementToTypeName(element));
						pos.x += element.rawText.Length-1;
						hitFirstChar = true;
					}
					//else if (isNumber) {
					//	output += "System.Int32";
					//	pos.x += element.rawText.Length-1;
					//	hitFirstChar = true;
					//}
					else if (isWord || isDot) {
						output.Append(currentChar);
						hitFirstChar = true;
					}
					//else if (!isCloseParentheses && !isPrePlusMinus && !isCloseBrackets && !isWhiteSpace && !isInGenericScope) {
					else if (!isPrePlusMinus && !isWhiteSpace && !isInGenericScope) {
						break;
					}
					
				}
				
				if (isInGenericScope && currentChar == '>') {
					isInGenericScope = false;
				}
			
				if (!editor.doc.CanIncrementPosition(pos,1)) {
					break;
				}
				
				if (pos == input.endPosition || !editor.doc.PositionLessThan(pos,input.endPosition)) {
					break;
				}
				
				pos = editor.doc.IncrementPosition(pos,1);
				
			}
			
			return output.ToString();
		}
		/*
		static private ExpressionInfo CropToFirstLiteralFromLeft(ExpressionInfo result) {
			Vector2 pos = result.startPosition;
			int counter = 0;
			while (true) {
				UIDELine line = editor.doc.RealLineAt((int)pos.y);
				UIDEElement element = line.GetElementAt((int)pos.x);
				if (element != null && (element.tokenDef.HasType("Word") || element.tokenDef.HasType("Number") || element.tokenDef.HasType("String"))) {
					break;
				}
				if (!editor.doc.CanIncrementPosition(pos,1)) {
					break;
				}
				if (pos == result.endPosition || !editor.doc.PositionLessThan(pos,result.endPosition)) {
					break;
				}
				counter++;
				pos = editor.doc.IncrementPosition(pos,1);
			}
			
			result.startPosition = pos;
			if (result.text.Length > 0) {
				result.text = result.text.Substring(Mathf.Clamp(counter,0,result.text.Length-1));
			}
			return result;
		}
		*/
		static private string GetMatch(string pattern, Vector2 position, int start, int length, string groupName) {
			UIDELine line = editor.doc.RealLineAt((int)position.y);
			string text = line.rawText;
			Regex regex = new Regex(pattern);
			Match match = regex.Match(text.Substring(start,length));
			if (match.Success) {
				return match.Groups[groupName].Value;
			}
			return "";
		}
		
		static private Vector2 MovePositionToMatchBeginning(string pattern, Vector2 position, string groupName) {
			UIDELine line = editor.doc.RealLineAt((int)position.y);
			string text = line.rawText;
			Regex regex = new Regex(pattern);
			MatchCollection matches = regex.Matches(text,0);
			for (int i = matches.Count-1; i >= 0; i--) {
				if (matches[i].Success) {
					if (matches[i].Index+matches[i].Length != (int)position.x) continue;
					
					position.x = matches[i].Groups[groupName].Index;
					break;
				}
				
			}
			return position;
		}
		
		static public ExpressionInfo CountToExpressionEnd(ExpressionInfo result, int dir, ExpressionBracketType bracketType) {
			
			string history = "";
			//char incChar = ')';
			//char decChar = '(';
			//if (dir == 1) {
			//	incChar = '(';
			//	decChar = ')';
			//}
			
			char genericIncChar = '>';
			char genericDecChar = '<';
			if (dir == 1) {
				genericIncChar = '<';
				genericDecChar = '>';
			}
			
			Vector2 position = result.startPosition;
			if (dir == -1) {
				position = result.endPosition;
			}
			
			int reverseDir = 1;
			if (dir == 1) {
				reverseDir = -1;
			}
			
			int genericScopeCounter = 0;
			int counter = 0;
			bool continueToFunction = false;
			bool genericScopeOk = false;
			//bool whiteSpaceOk = false;
			bool hasValidFunctionIdentifier = false;
			bool hasFunctionWithLeadingExpression = false;
			Vector2 beforeFunctionCheckPos = position;
			while (true) {
				
				UIDELine line = editor.doc.RealLineAt((int)position.y);
				UIDEElement element = line.GetElementAt((int)position.x);
				
				if ((int)position.x < 0 || (int)position.x >= line.rawText.Length) {
					return result;
				}
				char character = line.rawText[(int)position.x];
				
				if (element.tokenDef.HasType("DefaultText")) {
					if (CUtil.IsOpenExpression(character,dir,bracketType)) {
						counter++;
					}
					else if (CUtil.IsCloseExpression(character,dir,bracketType)) {
						counter--;
					}
				}
				
				
				if (continueToFunction) {
					int previousGenericScope = genericScopeCounter;
					if (genericScopeOk && element.tokenDef.HasType("DefaultText")) {
						if (character == genericIncChar) {
							genericScopeCounter++;
						}
						else if (character == genericDecChar) {
							genericScopeCounter--;
						}
					}
					
					bool justClosedGenericScope = genericScopeOk;
					
					if (element.tokenDef.HasType("Word")) {
						if (genericScopeCounter <= 0) {
							hasValidFunctionIdentifier = true;
							genericScopeOk = false;
							//whiteSpaceOk = false;
						}
					}
					justClosedGenericScope = justClosedGenericScope && !genericScopeOk;
					
					
					bool isInGenericScope = (genericScopeOk||justClosedGenericScope) && (previousGenericScope >= 1 || genericScopeCounter >= 1);
					bool passedWhitespace = char.IsWhiteSpace(character) && !hasValidFunctionIdentifier;
					
					bool continueToNextExpression = false;
					ExpressionBracketType continuingBracketType = ExpressionBracketType.Expression;
					
					if (!isInGenericScope && CUtil.IsOpenExpression(character,dir,ExpressionBracketType.Expression)) {
						continueToNextExpression = true;
						continuingBracketType = ExpressionBracketType.Expression;
						if (CUtil.IsCloseExpression(history[history.Length-1],dir,ExpressionBracketType.Bracket)) {
							continuingBracketType = ExpressionBracketType.Expression;
						}
						else {
							continueToNextExpression &= history[history.Length-1] == '.';
						}
					}
					if (!isInGenericScope && CUtil.IsOpenExpression(character,dir,ExpressionBracketType.Bracket)) {
						continuingBracketType = ExpressionBracketType.Bracket;
						continueToNextExpression = true;
						
					}
					
					//Debug.Log(character+" "+continueToNextExpression);
					if (continueToNextExpression) {
						ExpressionInfo res = result;
						res.startPosition = position;
						res.endPosition = position;
						res = CountToExpressionEnd(res,-1,continuingBracketType);
						
						string resLiteral;
						resLiteral = res.text;
						if (resLiteral != null) {
							char[] arr = resLiteral.ToCharArray();
							arr = arr.Reverse().ToArray();
							resLiteral = new string(arr);
						}
						else {
							resLiteral = "";
						}
						result.startPosition = res.startPosition;
						history = history+resLiteral;
						hasFunctionWithLeadingExpression = true;
						break;
					}
					
					
					if (!isInGenericScope && !passedWhitespace && (!element.tokenDef.HasType("Dot") && !element.tokenDef.HasType("Word") && !element.tokenDef.HasType("String") && !element.tokenDef.HasType("Number"))) {
						position = editor.doc.IncrementPosition(position,reverseDir);
						//Debug.Log(character);
						//position = MovePositionToMatchBeginning(@"(?<match>\)(\s*))",position,"match");
						//Debug.Log(line.rawText[(int)position.x]);
						
						position = MovePositionToMatchBeginning(@"(?<match>(?<!\w+)new(\s+))",position,"match");
						
						break;
					}
				}
				
				history += character;
				
				if (counter <= 0) {
					beforeFunctionCheckPos = position;
					if (dir == -1) {
						continueToFunction = true;
						genericScopeOk = true;
						//whiteSpaceOk = true;
					}
					else {
						break;
					}
				}
				
				
				if (editor.doc.CanIncrementPosition(position,dir)) {
					position = editor.doc.IncrementPosition(position,dir);
					//if (!editor.doc.PositionLessThan(minPos,position)) {
					//	position = editor.doc.IncrementPosition(position,reverseDir);
					//	break;
					//}
					UIDELine newLine = editor.doc.RealLineAt((int)position.y);
					bool shouldBreak = false;
					while ((int)position.x >= newLine.rawText.Length) {
						if (editor.doc.CanIncrementPosition(position,dir)) {
							position = editor.doc.IncrementPosition(position,dir);
						}
						else {
							shouldBreak = true;
							break;
						}
					}
					if (shouldBreak) {
						break;
					}
				}
				else {
					break;	
				}
				
			}
			
			if (!hasValidFunctionIdentifier) {
				position = beforeFunctionCheckPos;
			}
			
			string text = history;
			if (dir == -1) {
				char[] arr = history.ToCharArray();
				arr = arr.Reverse().ToArray();
				text = new string(arr);
			}
			
			result.text = text;
			if (!hasFunctionWithLeadingExpression) {
				if (dir == -1) {
					result.endPosition = result.startPosition;
					result.startPosition = position;
				}
				else if (dir == 1) {
					result.endPosition = position;
				}
			}
			return result;
		}
		
		static public Vector2 SimpleMoveToEndOfScope(Vector2 pos, int dir, ExpressionBracketType bracketType) {
			if (!CodeCompletion.ComplitionUtility.IsOpenExpression(editor.doc.GetCharAt(pos),dir,bracketType) || !editor.doc.GetElementAt(pos).tokenDef.isActualCode) {
				return pos;
			}
			Vector2 originalPos = pos;
			int inc = 0;
			while (true) {
				UIDEElement element = editor.doc.GetElementAt(pos);
				if (!element.tokenDef.isActualCode) {
					int elementStart = element.line.GetElementStartPos(element);
					pos.x = elementStart;
					if (dir == 1) {
						pos.x += element.rawText.Length;
					}
					else {
						if (!editor.doc.CanIncrementPosition(pos,-1)) {
							pos = originalPos;
							break;
						}
						pos = editor.doc.IncrementPosition(pos,-1);
					}
					continue;
				}
				
				if (CodeCompletion.ComplitionUtility.IsOpenExpression(editor.doc.GetCharAt(pos),dir,bracketType)) {
					inc++;
				}
				else if (CodeCompletion.ComplitionUtility.IsCloseExpression(editor.doc.GetCharAt(pos),dir,bracketType)) {
					inc--;
				}
				
				if (!editor.doc.CanIncrementPosition(pos,dir)) {
					pos = originalPos;
					break;
				}
				pos = editor.doc.IncrementPosition(pos,dir);
				if (inc <= 0) break;
			}
			return pos;
		}
		
		static public string ResolveFromLiteralAt(ref Vector2 inPosition) {
			Vector2 position = inPosition;
			Vector2 initialPos = position;
			bool hitExpressionStart = false;
			ExpressionBracketType expressionStartType = ExpressionBracketType.Expression;
			System.Text.StringBuilder historySB = new System.Text.StringBuilder();
			//string history = "";
			char lastRealChar = (char)0;
			bool lastCharIsWhitespace = false;
			
			UIDELine line = editor.doc.RealLineAt((int)position.y);
			UIDEElement element = line.GetElementAt((int)position.x);
			
			if (element.tokenDef.useParsableString) {
				return PrimitiveElementToTypeName(element);
			}
			//if (element.tokenDef.HasType("String")) {
			//	return "System.String";
			//}
			//if (element.tokenDef.HasType("Number")) {
			//	return "System.Int32";
			//}
			//position = MovePositionToMatchBeginning(@"(?<match>(?<!\w+)new(\s+))",position,"match");
			while (true) {
				expressionStartType = ExpressionBracketType.Expression;
				
				line = editor.doc.RealLineAt((int)position.y);
				element = line.GetElementAt((int)position.x);
				if (position.x >= line.rawText.Length || position.x < 0) break;
				
				char character = line.rawText[(int)position.x];
				
				if (element != null && !element.tokenDef.HasType("Comment")) {
					bool isString = element.tokenDef.HasType("String");
					bool isNumber = element.tokenDef.HasType("Number");
					bool isWord = element.tokenDef.HasType("Word");
					bool isDot = element.tokenDef.HasType("Dot");
					bool isWhitespace = element.tokenDef.HasType("WhiteSpace");
					//bool isGeneric
					bool isCloseParenthese = CUtil.IsCloseParentheses(character);
					bool isCloseBracket = CUtil.IsCloseBracket(character);
					if ((isCloseParenthese || isCloseBracket) && element.tokenDef.HasType("DefaultText") && lastRealChar == '.') {
						//The beginning of an expression.
						if (isCloseParenthese) {
							expressionStartType = ExpressionBracketType.Expression;
						}
						else {
							expressionStartType = ExpressionBracketType.Bracket;
						}
						hitExpressionStart = true;
						break;
					}
					else if (lastCharIsWhitespace && element.rawText == "new") {
						position = editor.doc.IncrementPosition(position,-1);
						position = editor.doc.IncrementPosition(position,-1);
						break;
					}
					else if (lastCharIsWhitespace && lastRealChar != '.') {
						position = editor.doc.IncrementPosition(position,1);
						break;
					}
					
					if (!(isNumber || isString || isWord || isDot || isWhitespace)) {
						position = editor.doc.IncrementPosition(position,1);
						break;
					}
					//if (element != null && (element.tokenDef.HasType("Word") || element.tokenDef.HasType("Dot"))) {
						//Is a valid element type, otherwise skip over the char.
						
					//}
					if (!isWhitespace) {
						lastRealChar = character;
					}
					else {
						lastCharIsWhitespace = true;
					}
				}
				else {
					lastCharIsWhitespace = true;
				}
				
				historySB.Append(character);
				
				if (editor.doc.CanIncrementPosition(position,-1)) {
					position = editor.doc.IncrementPosition(position,-1);
					UIDELine newLine = editor.doc.RealLineAt((int)position.y);
					bool shouldBreak = false;
					while ((int)position.x >= newLine.rawText.Length) {
						if (editor.doc.CanIncrementPosition(position,-1)) {
							position = editor.doc.IncrementPosition(position,-1);
						}
						else {
							shouldBreak = true;
							break;
						}
					}
					if (shouldBreak) {
						break;
					}
				}
				else {
					break;	
				}
			}
			
			string history = historySB.ToString();
			
			//butts()
			string literalChain = history;
			char[] arr = history.ToCharArray();
			arr = arr.Reverse().ToArray();
			literalChain = new string(arr);
			
			
			ExpressionInfo result = new ExpressionInfo();
			result.startPosition = position;
			result.endPosition = initialPos;
			result.text = literalChain;
			result.initialized = true;
			
			inPosition = position;
			
			if (hitExpressionStart) {
				ExpressionInfo expResult = new ExpressionInfo();
				string expLiteralChain = "";
			
				expResult.startPosition = position;
				expResult.endPosition = position;
				expResult.initialized = true;
				
				expResult = CountToExpressionEnd(expResult,-1,expressionStartType);
				
				expResult.endPosition = position;
				inPosition = expResult.startPosition;
				
				expLiteralChain = ExtractLiteralChain(expResult);
				
				literalChain = expLiteralChain+literalChain;
			}
			else {
				literalChain = ExtractLiteralChain(result);
			}
			
			//Debug.Log(literalChain);
			return literalChain;
		}
		
		static public string ResolveExpressionAt(Vector2 position, int dir) {
			ExpressionInfo result = new ExpressionInfo();
			result.startPosition = position;
			result.endPosition = position;
			result.initialized = true;
			return ResolveExpressionAt(position,dir, ref result);
		}
		static public string ResolveExpressionAt(Vector2 position, int dir, ref ExpressionInfo result) {
			
			UIDELine startLine = editor.doc.RealLineAt((int)position.y);
			if (position.x >= startLine.rawText.Length || position.x < 0) {
				return "";
			}
			UIDEElement element = startLine.GetElementAt((int)position.x);
			
			bool isWord;
			bool isDefaultText;
			bool isNumber;
			bool isString;
			
			isWord = element.tokenDef.HasType("Word");
			isDefaultText = element.tokenDef.HasType("DefaultText");
			isNumber = element.tokenDef.HasType("Number");
			isString = element.tokenDef.HasType("String");
			
			if (element != null) {
				bool isOk = isWord;
				isOk |= isDefaultText;
				isOk |= isNumber;
				isOk |= isString;
				if (!isOk) {
					return "";
				}
				
			}
			char firstChar = startLine.rawText[(int)position.x];
			
			bool isExpression = false;
			bool isBrackets = false;
			bool isLiteral = false;
			
			if (CUtil.IsOpenExpression(firstChar,dir,ExpressionBracketType.Expression)||CUtil.IsOpenExpression(firstChar,dir,ExpressionBracketType.Bracket)) {
				if (CUtil.IsOpenExpression(firstChar,dir,ExpressionBracketType.Bracket)) {
					isBrackets = true;
				}
				isExpression = true;
			}
			//if (dir == 1 && firstChar != '(') {
			//	isExpression = true;
			//}
			if (isWord || isNumber || isString) {
				isLiteral = true;
			}
			
			if (!isLiteral && !isExpression) {
				return "";
			}
			
			if (isLiteral) {
				Vector2 resPosition = position;
				string chain = ResolveFromLiteralAt(ref resPosition);
				result.startPosition = resPosition;
				chain = chain.Replace(".<","<");
				return chain;
			}
			
			
			
			ExpressionBracketType startingBracketType = ExpressionBracketType.Expression;
			if (isBrackets) {
				startingBracketType = ExpressionBracketType.Bracket;
			}
			
			ExpressionInfo beginResult = CountToExpressionEnd(result,dir,startingBracketType);
			
			//Debug.Log(beginResult.startPosition+" "+beginResult.endPosition);
			
			string literalChain = ExtractLiteralChain(beginResult);
			
			//result.CopyFrom(beginResult);
			result.startPosition = beginResult.startPosition;
			//result.endPosition = inputResult.endPosition;
			
			while (literalChain.Contains("..")) {
				literalChain = literalChain.Replace("..",".");
			}
			
			literalChain = literalChain.Replace(".<","<");
			
			return literalChain;
		}
	}
}