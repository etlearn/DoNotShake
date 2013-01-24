using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

//[System.Serializable]
public class UIDEElement:System.Object {
	public UIDELine line;
	public string rawText;
	public bool canSplit = false;
	public bool highlighted = false;
	
	public UIDETokenDef _tokenDef;
	public UIDETokenDef tokenDef {
		get {
			if (line != null && line.overrideTokenDef != null) {
				if (line.overrideTokenDef != null) {
					return line.overrideTokenDef;
				}
			}
			return _tokenDef;
		}
		set {
			_tokenDef = value;
		}
	}
	
	static public UIDEElement Create() {
		return new UIDEElement();
		//return (UIDEElement)ScriptableObject.CreateInstance(typeof(UIDEElement));
	}
	
	public string GetSpacedTabString(int startCharPos) {
		if (line == null) return rawText;
		System.Text.StringBuilder actualTextSB = new System.Text.StringBuilder();
		for (int i = 0; i < rawText.Length; i++) {
			char currentChar = rawText[i];
			if (currentChar == '\t') {
				string tabStr = line.GetTabStringAtPos(startCharPos+i);
				actualTextSB.Append(tabStr);
			}
			else {
				actualTextSB.Append(currentChar);
			}
		}
		return actualTextSB.ToString();
	}
	/*
	public int GetScreenLength() {
		int length = 0;
		int tabSize = line.doc.editor.editorWindow.tabSize;
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
	*/
	public Vector2 GetRangeInLine() {
		int elementIndex = line.elements.IndexOf(this);
		if (elementIndex == -1) return new Vector2(0,0);
		int startingPoint = 0;
		for (int i = 0; i < elementIndex; i++) {
			startingPoint += line.elements[i].rawText.Length;
		}
		
		return new Vector2(startingPoint,startingPoint+rawText.Length);
	}
}
