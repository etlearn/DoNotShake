using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum UIDEUndoType {LineModify,LineAdd,LineRemove};

[System.Serializable]
public class UIDEUndo:System.Object {
	public string groupID = "";
	public UIDEUndoType type;
	public string oldData = "";
	public string newData = "";
	public int lineNumber = -1;
	public Vector2 oldCursorPosition;
	public Vector2 newCursorPosition;
	public UIDEUndoManager manager;
	
	public void ExecuteUndo() {
		switch (type) {
			case UIDEUndoType.LineModify: 
				ExecuteLineModifyUndo();
				break;
			case UIDEUndoType.LineAdd:
				ExecuteLineAddUndo();
				break;
			case UIDEUndoType.LineRemove:
				ExecuteLineRemoveUndo();
				break;
			default:
				break;
		}
	}
	
	public void ExecuteRedo() {
		switch (type) {
			case UIDEUndoType.LineModify: 
				ExecuteLineModifyRedo();
				break;
			case UIDEUndoType.LineAdd:
				ExecuteLineAddRedo();
				break;
			case UIDEUndoType.LineRemove:
				ExecuteLineRemoveRedo();
				break;
			default:
				break;
		}
	}
	
	//Undos
	public void ExecuteLineModifyUndo() {
		//string debugString = "";
		//debugString += "Undoing type \""+type+"\" GroupID is \""+groupID+"\" Data is \""+data+"\" line number is "+lineNumber;
		//Debug.Log(debugString);
		UIDELine line = manager.editor.doc.LineAt(lineNumber);
		line.rawText = oldData;
		line.RebuildElements();
		FinilizeUndoExecution();
	}
	public void ExecuteLineAddUndo() {
		UIDELine line = manager.editor.doc.LineAt(lineNumber);
		line.doc.RemoveLine(line.index);
		FinilizeUndoExecution();
	}
	public void ExecuteLineRemoveUndo() {
		UIDELine newLine = manager.editor.doc.InsertLine(lineNumber,oldData);
		newLine.RebuildElements();
		FinilizeUndoExecution();
	}
	
	//Redos
	public void ExecuteLineModifyRedo() {
		//string debugString = "";
		//debugString += "Redoing type \""+type+"\" GroupID is \""+groupID+"\" Data is \""+data+"\" line number is "+lineNumber;
		//Debug.Log(debugString);
		UIDELine line = manager.editor.doc.LineAt(lineNumber);
		line.rawText = newData;
		line.RebuildElements();
		FinilizeRedoExecution();
	}
	public void ExecuteLineAddRedo() {
		UIDELine newLine = manager.editor.doc.InsertLine(lineNumber,newData);
		newLine.RebuildElements();
		FinilizeRedoExecution();
	}
	public void ExecuteLineRemoveRedo() {
		UIDELine line = manager.editor.doc.LineAt(lineNumber);
		line.doc.RemoveLine(line.index);
		FinilizeRedoExecution();
	}
	
	public void FinilizeUndoExecution() {
		manager.editor.cursor.posY = (int)oldCursorPosition.y;
		manager.editor.cursor.posX = (int)oldCursorPosition.x;
		manager.editor.editorWindow.Repaint();
	}
	public void FinilizeRedoExecution() {
		manager.editor.cursor.posY = (int)newCursorPosition.y;
		manager.editor.cursor.posX = (int)newCursorPosition.x;
		manager.editor.editorWindow.Repaint();
	}
	
	static public UIDEUndo Create(UIDEUndoManager manager, string groupID, UIDEUndoType type, int lineNumber, string oldData, string newData, Vector2 oldCursorPosition, Vector2 newCursorPosition) {
		UIDEUndo undo = new UIDEUndo();
		undo.manager = manager;
		undo.groupID = groupID;
		undo.lineNumber = lineNumber;
		undo.type = type;
		undo.newData = newData;
		undo.oldData = oldData;
		undo.oldCursorPosition = oldCursorPosition;
		undo.newCursorPosition = newCursorPosition;
		return undo;
	}
}
