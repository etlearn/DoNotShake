using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

[System.Serializable]
public class UIDEDoc:System.Object {
	[System.NonSerialized]
	public bool initialized = false;
	[System.NonSerialized]
	public UIDETextEditor editor;
	public Vector2 scroll;
	
	
	public List<string> stringLines = new List<string>();
	public int lineCount {
		get {
			return lines.Count;
		}
	}
	
	[System.NonSerialized]
	private List<UIDELine> lines = new List<UIDELine>();
	
	public string path = "";
	public string extension = "";
	public string rawText = "";
	
	[System.NonSerialized]
	public List<string> inclidedNameSpaces = new List<string>();
	
	/*
	[SerializeField]
	private UIDECursor _cursor;
	public UIDECursor cursor {
		get {
			if (_cursor == null) {
				_cursor = new UIDECursor();
			}
			_cursor.editor = editor;
			//_cursor.doc = this;
			return _cursor;
		}
	}
	*/
	/*
	[SerializeField]
	private UIDESyntaxRule _syntaxRule;
	public UIDESyntaxRule syntaxRule {
		get {
			if (_syntaxRule == null) {
				UpdateExtensionAndSyntaxRule();
			}
			return _syntaxRule;
		}
		set {
			_syntaxRule = value;
		}
	}
	*/
	
	public UIDELine LineAt(int index) {
		index = Mathf.Clamp(index,0,editor.actualLinesToRender.Count-1);
		return editor.GetLine(index);
	}
	
	//public UIDELine LineAt(int index) {
	//	index = Mathf.Clamp(index,0,lineCount-1);
	//	return lines[index];
	//}
	
	public UIDELine RealLineAt(int index) {
		index = Mathf.Clamp(index,0,lineCount-1);
		return lines[index];
	}
	
	public UIDELine AddLine(string text) {
		return InsertLine(lines.Count,text);
	}
	public UIDELine InsertLine(int index, string text) {
		stringLines.Insert(index,text);
		
		UIDELine line = new UIDELine();
		line.doc = this;
		line.index = index;
		line.rawText = text;
		
		lines.Insert(index,line);
		editor.actualLinesToRender.Insert(index,line.index);
		
		for (int i = index+1; i < editor.actualLinesToRender.Count; i++) {
			editor.actualLinesToRender[i] = editor.actualLinesToRender[i]+1;
		}
		
		for (int i = index+1; i < lines.Count; i++) {
			lines[i].index++;
		}
		return line;
	}
	public void RemoveLine(int index) {
		lines.RemoveAt(index);
		stringLines.RemoveAt(index);
		editor.actualLinesToRender.RemoveAt(index);
		
		for (int i = index; i < editor.actualLinesToRender.Count; i++) {
			editor.actualLinesToRender[i] = editor.actualLinesToRender[i]-1;
		}
		
		for (int i = index; i < lines.Count; i++) {
			lines[i].index--;
		}
	}
	
	public UIDELine GetLastNoneWhitespaceOrCommentLine(int pos) {
		for (int i = pos; i >= 0; i--) {
			UIDELine line = RealLineAt(i);
			if (!line.IsLineWhitespace() && !line.IsLineComment()) {
				return line;
			}
		}
		return null;
	}
	
	public bool CanIncrementPosition(Vector2 inPosition, int dir) {
		Vector2 position = inPosition;
		UIDELine line = RealLineAt((int)position.y);
		position.x += dir;
		if (position.x < 0) {
			position.y -= 1;
			if (position.y < 0) {
				return false;
			}
			UIDELine newLine = RealLineAt((int)position.y);
			position.x = newLine.rawText.Length-1;
		}
		else if (position.x >= line.rawText.Length) {
			position.y += 1;
			if (position.y >= lineCount) {
				return false;
			}
			//UIDELine newLine = RealLineAt((int)position.y);
			position.x = 0;
		}
		return true;
	}
	
	public Vector2 IncrementPositionToNextNonWhitespace(Vector2 inPosition, int dir) {
		inPosition = IncrementPosition(inPosition,dir);
		while (true) {
			if (!CanIncrementPosition(inPosition,dir)) {
				break;
			}
			UIDELine line = RealLineAt((int)inPosition.y);
			if (line.rawText.Length > 0) {
				if (!char.IsWhiteSpace(line.rawText[(int)inPosition.x])) {
					break;
				}
			}
			
			inPosition = IncrementPosition(inPosition,dir);
		}
		return inPosition;
	}
	
	public Vector2 IncrementPosition(Vector2 inPosition, int dir) {
		Vector2 position = inPosition;
		UIDELine line = RealLineAt((int)position.y);
		position.x += dir;
		if (position.x < 0) {
			position.y -= 1;
			if (position.y < 0) {
				return inPosition;
			}
			UIDELine newLine = RealLineAt((int)position.y);
			position.x = newLine.rawText.Length-1;
		}
		else if (position.x >= line.rawText.Length) {
			position.y += 1;
			if (position.y >= lineCount) {
				return inPosition;
			}
			//UIDELine newLine = RealLineAt((int)position.y);
			position.x = 0;
		}
		return position;
	}
	
	public Vector2 GoToNextRealChar(Vector2 pos,char c, int dir) {
		int counter = 0;
		while (true) {
			UIDELine line = RealLineAt((int)pos.y);
			if (line == null) {
				break;
			}
			if (pos.x >= line.rawText.Length) {
				break;
			}
			char character = line.rawText[(int)pos.x];
			if (c == character) {
				bool elementIsComment = GetElementAt(pos).tokenDef.HasType("Comment");
				if (!elementIsComment) {
					break;
				}
			}
			if (!CanIncrementPosition(pos,dir)) {
				break;
			}
			pos = IncrementPosition(pos,dir);
			counter++;
		}
		return pos;
	}
	
	public Vector2 GoToEndOfWhitespace(Vector2 pos,int dir) {
		int counter = 0;
		while (true) {
			UIDELine line = RealLineAt((int)pos.y);
			if (line == null) {
				break;
			}
			if (pos.x >= line.rawText.Length) {
				break;
			}
			char character = line.rawText[(int)pos.x];
			if (!char.IsWhiteSpace(character)) {
				bool elementIsComment = GetElementAt(pos).tokenDef.HasType("Comment");
				if (!elementIsComment) {
					break;
				}
			}
			if (!CanIncrementPosition(pos,dir)) {
				break;
			}
			pos = IncrementPosition(pos,dir);
			counter++;
		}
		return pos;
	}
	
	public Vector2 GoToEndOfWord(Vector2 pos,int dir) {
		int counter = 0;
		while (true) {
			UIDELine line = RealLineAt((int)pos.y);
			if (line == null) {
				break;
			}
			if (pos.x >= line.rawText.Length) {
				break;
			}
			char character = line.rawText[(int)pos.x];
			if (!char.IsLetterOrDigit(character) && character != '_') {
				break;
			}
			if (!CanIncrementPosition(pos,dir)) {
				break;
			}
			pos = IncrementPosition(pos,dir);
			counter++;
		}
		return pos;
	}
	
	public char GetCharAt(Vector2 inPosition) {
		char c = (char)0;
		UIDELine line = RealLineAt((int)inPosition.y);
		if (line == null) return c;
		if (inPosition.x >= line.rawText.Length) return c;
		c = line.rawText[(int)inPosition.x];
		return c;
	}
	
	public UIDEElement GetElementAt(Vector2 inPosition) {
		UIDELine line = RealLineAt((int)inPosition.y);
		if (line == null) return null;
		return line.GetElementAt((int)inPosition.x);
	}
	
	public bool PositionLessThan(Vector2 leftPos, Vector2 rightPos) {
		if (rightPos.y < leftPos.y) return false;
		if (leftPos.y == rightPos.y) {
			return leftPos.x < rightPos.x;
		}
		return true;
	}
	
	public void OnLoaded() {
		UpdateExtensionAndSyntaxRule();
		InitialBuildLines();
		Initialize(editor);
	}
	
	public void OnPluginsReloaded() {
		RebuildLines();
	}
	
	public void UpdateExtensionAndSyntaxRule() {
		extension = "";
		if (path.LastIndexOf(".") != 0) {
			extension = path.Substring(path.LastIndexOf(".")+1).ToLower();
		}
		/*
		syntaxRule = UIDESyntaxRule.GetSyntaxTypeFromFileType(extension);
		if (syntaxRule == null) {
			syntaxRule = UIDESyntaxRule.defaultRule;
		}
		*/
	}
	
	public void Initialize(UIDETextEditor textEditor) {
		editor = textEditor;
		//Dont need this because it will be called from editor.UpdatePlugins->OnPluginsReloaded->RebuildLines
		//RebuildLines();
		initialized = true;
	}
	
	public void UpdateRawText() {
		rawText = GetRawText();
	}
	
	public string GetRawText() {
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		for (int i = 0; i < lines.Count; i++) {
			sb.Append(lines[i].rawText);
			if (i < lines.Count-1) {
				sb.Append("\r\n");
				//rawText += System.Environment.NewLine;
			}
		}
		return sb.ToString();
	}
	
	public string GetParsableText() {
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		//string newText = "";
		for (int i = 0; i < lines.Count; i++) {
			UIDELine line = lines[i];
			for (int j = 0; j < line.elements.Count; j++) {
				UIDEElement element = line.elements[j];
				if (element.tokenDef.HasType("Comment")) {
					for (int k = 0; k < element.rawText.Length; k++) {
						sb.Append(" ");
					}
					continue;
				}
				else if (element.tokenDef.HasType("String")) {
					//sb.Append("(new System.String())");
					for (int k = 0; k < element.rawText.Length; k++) {
						sb.Append(" ");
					}
					continue;
				}
				else if (element.tokenDef.HasType("Number")) {
					//sb.Append("(new System.Int32())");
					for (int k = 0; k < element.rawText.Length; k++) {
						sb.Append(" ");
					}
					continue;
				}
				else if (element.tokenDef.HasType("PreProcess")) {
					//sb.Append("(new System.Int32())");
					for (int k = 0; k < element.rawText.Length; k++) {
						sb.Append(" ");
					}
					continue;
				}
				sb.Append(element.rawText);
			}
			if (i < lines.Count-1) {
				sb.Append("\n");
			}
		}
		return sb.ToString();
	}
	/*
	public string GetRawTextWithoutCurrentElement() {
		UIDELine currentLine = RealLineAt(editor.cursor.posY);
		string newText = "";
		for (int i = 0; i < lines.Count; i++) {
			if (i == currentLine.index) {
				UIDEElement currentElement = currentLine.GetElementAt(editor.cursor.posX-1);
				if (currentElement != null) {
					for (int j = 0; j < currentLine.elements.Count; j++) {
						if (currentLine.elements[j] == currentElement) continue;
						newText += currentLine.elements[j].rawText;
					}
				}
				else {
					newText += lines[i].rawText;
				}
			}
			else {
				newText += lines[i].rawText;
			}
			if (i < lines.Count-1) {
				newText += "\r\n";
			}
		}
		return newText;
	}
	public string GetRawTextWithEndStatement() {
		UIDELine currentLine = RealLineAt(editor.cursor.posY);
		string newText = "";
		for (int i = 0; i < lines.Count; i++) {
			if (i == currentLine.index) {
				string lineText = lines[i].rawText;
				lineText = lineText.Insert(editor.cursor.posX,";");
				newText += lineText;
			}
			else {
				newText += lines[i].rawText;
			}
			if (i < lines.Count-1) {
				newText += "\r\n";
			}
		}
		return newText;
	}
	*/
	public void InitialBuildLines() {
		lines = new List<UIDELine>();
		char[] separators = new char[] {'\n','\r'};
		
		string[] parts = rawText.Split(separators);
		for (int i = 0; i < parts.Length; i++) {
			stringLines.Add(parts[i]);
		}
	}
	
	public void RebuildLines() {
		lines = new List<UIDELine>();
		//char[] separators = new char[] {'\n','\r'};
		//string[] parts = rawText.Split(separators);
		for (int i = 0; i < stringLines.Count; i++) {
			UIDELine line = new UIDELine();
			line.doc = this;
			line.index = i;
			line.rawText = stringLines[i];
			lines.Add(line);
			//AddLine(line);
		}
		for (int i = 0; i < lines.Count; i++) {
			lines[i].RebuildElements();
		}
		
		editor.UpdateActualRenderedLines();
		
		if (editor.plugins != null) {
			for (int i = 0; i < editor.plugins.Count; i++) {
				editor.plugins[i].OnRebuildLines(this);
			}
		}
		
		//syntaxRule.OnRebuildLines(this);
	}
	
	public bool Save() {
		UpdateRawText();
		TextWriter writer = null;
		int c = 0;
		while (writer == null && c < 10) {
			c++;
			writer = File.CreateText(path);
			if (writer == null) {
				Thread.Sleep(50);
			}
			else {
				break;
			}
		}
		if (writer != null) {
			writer.Write(rawText);
			writer.Close();
			//Debug.Log("Saved document \""+path+"\" after "+c+" tries.");
		}
		else {
			Debug.LogError("Failed to save document \""+path+"\" after "+c+" tries.");
		}
		return writer != null;
	}
	
	static public UIDEDoc Load(UIDETextEditor textEditor, string path) {
		
		//UIDEDoc doc = (UIDEDoc)ScriptableObject.CreateInstance(typeof(UIDEDoc));
		UIDEDoc doc = new UIDEDoc();
		doc.path = path;
		doc.editor = textEditor;
		
		StreamReader streamReader = new StreamReader(path);
		if (streamReader != null) {
			string text = streamReader.ReadToEnd();
			streamReader.Close();
			doc.rawText = text;
			doc.rawText = doc.rawText.Replace("\n\r","\n");
			doc.rawText = doc.rawText.Replace("\r\n","\n");
		}
		
		doc.OnLoaded();
		return doc;
	}
}
