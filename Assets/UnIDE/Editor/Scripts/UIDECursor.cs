using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

[System.Serializable]
public class UIDECursorPos {
	public int x;
	public int y;
	
	public UIDECursorPos(int x, int y) {
		this.x = x;
		this.y = y;
	}
	
	public UIDECursorPos Copy() {
		return new UIDECursorPos(x,y);
	}
}

[System.Serializable]
public class UIDECursor:System.Object {
	//[System.NonSerialized]
	public UIDEDoc doc {
		get {
			if (editor == null) {
				Debug.LogError("Cursor editor field is null!");
				return null;
			}
			return editor.doc;
		}
	}
	
	public UIDETextEditor editor;
	public UIDESelection selection;
	
	//[SerializeField]
	public UIDECursorPos pos = new UIDECursorPos(0,0);
	
	public float opacity = 1.0f;
	public float lastMoveTime;
	
	public int desiredPosX;
	
	public UIDECursor(UIDETextEditor editor) {
		this.editor = editor;
		selection = new UIDESelection();
		selection.cursor = this;
	}
	
	public int posX {
		get {
			return pos.x;
		}
		set {
			if (value != pos.x) {
				lastMoveTime = editor.editorWindow.time;
				pos.x = value;
				//doc.editor.OnChangedCursorPosition();
			}
			
			if (posY > 0 && posY < doc.lineCount) {
				desiredPosX = doc.RealLineAt(posY).GetScreenPosition(value);
			}
			else {
				desiredPosX = value;
			}
			ClampCursor();
		}
	}
	public int posXNotDesired {
		get {
			return pos.x;
		}
		set {
			if (value != pos.x) {
				pos.x = value;
				lastMoveTime = editor.editorWindow.time;
			}
		}
	}
	public int posY {
		get {
			return pos.y;
		}
		set {
			if (value != pos.y) {
				pos.y = value;
				lastMoveTime = editor.editorWindow.time;
				//doc.editor.OnChangedCursorPosition();
			}
			ClampCursor();
		}
	}
	
	public Vector2 GetVectorPosition() {
		return new Vector2(posX,posY);
	}
	
	public void ClampCursor() {
		if (posY < 0) {
			pos.y = 0;
		}
		if (posY >= doc.lineCount) {
			pos.y = doc.lineCount-1;
		}
		if (posXNotDesired < 0) {
			posXNotDesired = 0;
		}
		if (doc.lineCount > 0) {
			UIDELine line = doc.RealLineAt(posY);
			if (posXNotDesired >= line.rawText.Length+1) {
				posXNotDesired = line.rawText.Length;
			}
		}
		else {
			posXNotDesired = 0;
		}
	}
}
