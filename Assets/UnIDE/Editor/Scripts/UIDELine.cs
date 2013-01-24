using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

//[System.Serializable]
public class UIDELine:System.Object {
	public UIDEDoc doc;
	public UIDETokenDef overrideTokenDef = null;
	//private string _rawText;
	public string rawText {
		get {
			if (index == -1) {
				Debug.LogError("UIDELine index is -1");
				return "";
			}
			if (doc.stringLines[index] == null)	{
				Debug.LogError("UIDELine index is "+index+" and the string is null");
				return "";
			}
			return doc.stringLines[index];
		}
		set {
			if (index != -1) {
				doc.stringLines[index] = value;
			}
		}
	}
	public bool isFoldable = false;
	public bool isFolded = false;
	public int foldingLength = 0;
	public bool isBlockCommented = false;
	public List<UIDEElement> elements = new List<UIDEElement>();
	public int index = -1;
	
	public int GetScreenLength() {
		int length = 0;
		int tabSize = doc.editor.editorWindow.tabSize;
		for (int i = 0; i < rawText.Length; i++) {
			if (rawText[i] == '\t') {
				int tabRemainder = tabSize-(length%tabSize);
				length += tabRemainder;
			}
			else {
				length++;
			}
		}
		return length;
	}
	
	public int GetTabSizeAtPos(int pos) {
		int tabSize = doc.editor.editorWindow.tabSize;
		pos = GetScreenPosition(pos);
		int tabRemainder = tabSize-(pos%tabSize);
		return tabRemainder;
	}
	public string GetTabStringAtPos(int pos) {
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		int size = GetTabSizeAtPos(pos);
		for (int i = 0; i < size; i++) {
			sb.Append(" ");
		}
		return sb.ToString();;
	}
		
	public int GetScreenPosition(int originalPos) {
		originalPos = Mathf.Clamp(originalPos,0,rawText.Length);
		int length = 0;
		int tabSize = doc.editor.editorWindow.tabSize;
		for (int i = 0; i < originalPos; i++) {
			if (rawText[i] == '\t') {
				int tabRemainder = tabSize-(length%tabSize);
				length += tabRemainder;
			}
			else {
				length++;
			}
		}
		return length;
	}
	public int GetPositionFromScreenPosition(int originalPos) {
		if (rawText.Length == 0) return 0;
		int indexingPos = Mathf.Clamp(originalPos,0,rawText.Length);
		
		int tabSize = doc.editor.editorWindow.tabSize;
		
		int totalScreenChars = 0;
		for (int i = 0; i < indexingPos; i++) {
			if (rawText[i] == '\t') {
				int tabRemainder = tabSize-(totalScreenChars%tabSize);
				totalScreenChars += tabRemainder;
			}
			else {
				totalScreenChars++;
			}
		}
		
		int[] positionRanges = new int[totalScreenChars];
		int positionRangeIndex = 0;
		for (int i = 0; i < indexingPos; i++) {
			if (rawText[i] == '\t') {
				int tabRemainder = tabSize-(positionRangeIndex%tabSize);
				int halfTabRemainder = tabRemainder/2;
				for (int j = 0; j < tabRemainder; j++) {
					int assignValue = i;
					if (tabRemainder > 1 && j >= halfTabRemainder+(j%2)) {
						assignValue += 1;
					}
					positionRanges[positionRangeIndex+j] = assignValue;
					
					//positionRanges[positionRangeIndex] = i;
					//positionRanges[positionRangeIndex+1] = i;
					//positionRanges[positionRangeIndex+2] = i+1;
					//positionRanges[positionRangeIndex+3] = i+1;
				}
				positionRangeIndex += tabRemainder;
			}
			else {
				positionRanges[positionRangeIndex] = i;
				positionRangeIndex += 1;
			}
		}
		
		
		if (positionRanges.Length == 0) {
			return 0;
		}
		
		int outPos = 0;
		if (originalPos >= positionRanges.Length) {
			
			outPos = positionRanges[positionRanges.Length-1]+1;
		}
		else {
			outPos = positionRanges[originalPos];
		}
		
		return outPos;
	}
	
	public void SetElementText(UIDEElement element, string text) {
		int elementIndex = elements.IndexOf(element);
		if (elementIndex == -1) return;
		int startingPoint = 0;
		for (int i = 0; i < elementIndex; i++) {
			startingPoint += elements[i].rawText.Length;
		}
		string newText = rawText;
		newText = newText.Remove(startingPoint,element.rawText.Length);
		newText = newText.Insert(startingPoint,text);
		rawText = newText;
	}
	
	public int GetElementIndex(UIDEElement element) {
		int elementIndex = elements.IndexOf(element);
		return elementIndex;
	}
	
	public UIDEElement GetElementAt(int pos) {
		pos = Mathf.Clamp(pos,0,rawText.Length-1);
		UIDEElement element = null;
		int c = 0;
		for (int i = 0; i < elements.Count; i++) {
			c += elements[i].rawText.Length;
			if (c > pos) {
				element = elements[i];
				break;
			}
		}
		return element;
	}
	
	public int GetElementStartPos(UIDEElement element) {
		int index = GetElementIndex(element);
		int c = 0;
		for (int i = 0; i < index; i++) {
			c += elements[i].rawText.Length;
		}
		return c;
	}
	
	public bool IsLineWhitespace() {
		for (int i = 0; i < elements.Count; i++) {
			if (!elements[i].tokenDef.HasType("WhiteSpace")) {
				return false;
			}
		}
		return true;
	}
	
	public bool IsLineComment() {
		for (int i = 0; i < elements.Count; i++) {
			if (!elements[i].tokenDef.HasType("WhiteSpace") && !elements[i].tokenDef.HasType("Comment")) {
				return false;
			}
		}
		return true;
	}
	
	public UIDEElement GetFirstNonWhitespaceElement() {
		return GetFirstNonWhitespaceElement(false);
	}
	public UIDEElement GetFirstNonWhitespaceElement(bool includeComments) {
		for (int i = 0; i < elements.Count; i++) {
			if (elements[i].tokenDef == null) continue;
			if (!elements[i].tokenDef.HasType("WhiteSpace") && (includeComments || !elements[i].tokenDef.HasType("Comment"))) {
				return elements[i];
			}
		}
		return null;
	}
	public UIDEElement GetLastNonWhitespaceElement() {
		return GetLastNonWhitespaceElement(false);
	}
	public UIDEElement GetLastNonWhitespaceElement(bool includeComments) {
		for (int i = elements.Count-1; i >= 0; i--) {
			if (elements[i].tokenDef == null) continue;
			if (!elements[i].tokenDef.HasType("WhiteSpace") && (includeComments || !elements[i].tokenDef.HasType("Comment"))) {
				return elements[i];
			}
		}
		return null;
	}
	public UIDEElement[] GetLeadingWhitespaceElements() {
		List<UIDEElement> el = new List<UIDEElement>();
		for (int i = 0; i < elements.Count; i++) {
			if (elements[i].tokenDef.HasType("WhiteSpace")) {
				el.Add(elements[i]);
			}
		}
		return el.ToArray();
	}
	
	public string GetTrimmedWhitespaceText() {
		string str = "";
		bool foundNonWhitespace = false;
		for (int i = 0; i < elements.Count; i++) {
			if (foundNonWhitespace) {
				str += elements[i].rawText;
			}
			else {
				if (!elements[i].tokenDef.HasType("WhiteSpace")) {
					foundNonWhitespace = true;
					str += elements[i].rawText;
				}
			}
		}
		return str;
	}
	
	public void RebuildElements() {
		elements = new List<UIDEElement>();
		//UIDEElement baseElement = UIDEElement.Create();
		//baseElement.rawText = rawText;
		//baseElement.canSplit = true;
		UIDEElement baseElement = CreateElement(rawText,"");
		elements.Add(baseElement);
		
		if (doc.editor.plugins != null) {
			for (int i = 0; i <  doc.editor.plugins.Count; i++) {
				doc.editor.plugins[i].OnRebuildLineElements(this);
			}
		}
		List<UIDEElement> tmpElements = new List<UIDEElement>();
		for (int i = 0; i <  elements.Count; i++) {
			if (elements[i].rawText.Length > 0) {
				tmpElements.Add(elements[i]);
			}
		}
		elements = tmpElements;
		
		//doc.syntaxRule.OnRebuildLineElements(this);
	}
	
	public List<UIDEElement> CreateSubElements(List<UIDEElement> elements,string pattern, string tokenDefName) {
		List<UIDEElement> newElements = new List<UIDEElement>();
		
		for (int i = 0; i < elements.Count; i++) {
			List<UIDEElement> subElements = CreateSubElementsFromElement(elements[i],pattern,tokenDefName);
			newElements.AddRange(subElements);
		}
		
		return newElements;
	}
	
	public List<UIDEElement> CreateSubElementsFromElement(UIDEElement element,string pattern, string tokenDefName) {
		if (!element.canSplit) return new List<UIDEElement> {element};
		List<UIDEElement> newElements = CreateElementsFromString(element.rawText,pattern,tokenDefName);
		return newElements;
	}
	
	public List<UIDEElement> CreateElementsFromString(string text,string pattern, string tokenDefName) {
		List<UIDEElement> splitElements = new List<UIDEElement>();
		
		Regex re = new Regex(pattern, RegexOptions.None);
		MatchCollection mc = re.Matches(text);
		int currentPos = 0;
		foreach (Match ma in mc) {
			UIDEElement highlightElement = CreateElement(text.Substring(ma.Index, ma.Length),tokenDefName);
			
			if (ma.Index > currentPos) {
				splitElements.Add(CreateElement(text.Substring(currentPos, ma.Index-currentPos),""));
			}
			splitElements.Add(highlightElement);
			currentPos = ma.Index+ma.Length;
		}
		
		if (currentPos < text.Length) {
			splitElements.Add(CreateElement(text.Substring(currentPos),""));
		}
		return splitElements;
	}
	
	public UIDEElement CreateElement(string str, string tokenDefName) {
		UIDETokenDef tokenDef = null;
		if (tokenDefName.Length > 0) {
			tokenDef = UIDETokenDefs.Get(tokenDefName);
		}
		
		UIDEElement element = UIDEElement.Create();
		element.line = this;
		element.rawText = str;
		element._tokenDef = tokenDef;
		
		if (element._tokenDef != null && tokenDefName == "Word") {
			element._tokenDef = doc.editor.syntaxRule.GetKeywordTokenDef(element._tokenDef,str);
		}
		
		if (element._tokenDef == null) {
			element._tokenDef = UIDETokenDefs.defaultTokenDef;
			element.canSplit = true;
			//Debug.Log(str);
		}
		
		return element;
	}
}
