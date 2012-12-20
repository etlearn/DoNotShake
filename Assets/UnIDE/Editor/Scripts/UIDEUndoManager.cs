using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//[System.Serializable]
public class UIDEUndoManager:ScriptableObject {
	
	//[System.NonSerialized]
	//private bool infiniteLoopBlock = false;
	//[System.NonSerialized]
	//private string testUndoThing = "";
	//[System.NonSerialized]
	public List<UIDEUndo> undos = new List<UIDEUndo>();
	//[System.NonSerialized]
	public int currentUndoIndex = 0;
	//[System.NonSerialized]
	//private List<UIDEUndo> tmpUndoList = new List<UIDEUndo>();
	//[System.NonSerialized]
	//private List<UIDEUndo> currentStates = new List<UIDEUndo>();
	
	//[System.NonSerialized]
	//private bool isNewUndo = true;
	
	public int dummyCounter = 0;
	//[System.NonSerialized]
	public bool lastWasUndo = false;
	//[System.NonSerialized]
	public bool reachedEnd = false;
	//[SerializeField]
	public UIDETextEditor editor;
	//[SerializeField]
	public int undoCounter = 0;
	//[SerializeField]
	public string tmpString = "";
	
	private int uniqueID = 0;
	
	public int GetUniqueID() {
		uniqueID++;
		return uniqueID;
	}
	
	
	public void DoUndo() {
		ClampUndoIndex();
		if (undos.Count <= 0) return;
		
		if (!lastWasUndo && !reachedEnd) {
			currentUndoIndex--;
			ClampUndoIndex();
		}
		
		UIDEUndo undo = undos[currentUndoIndex];
		undo.ExecuteUndo();
		int previousIndex = currentUndoIndex;
		currentUndoIndex--;
		ClampUndoIndex();
		
		editor.isDirty = true;
		
		lastWasUndo = true;
		reachedEnd = false;
		if (undos.Count > 0 && currentUndoIndex >= 0 && previousIndex != currentUndoIndex) {
			UIDEUndo nextUndo = undos[currentUndoIndex];
			if (nextUndo.groupID == undo.groupID) {
				DoUndo();
			}
		}
	}
	public void DoRedo() {
		ClampUndoIndex();
		
		if (undos.Count <= 0) return;
		
		if (lastWasUndo) {
			currentUndoIndex++;
			ClampUndoIndex();
		}
		UIDEUndo undo = null;
		if (currentUndoIndex == undos.Count-1) {
			if (!reachedEnd) {
				undo = undos[currentUndoIndex];
				undo.ExecuteRedo();
				editor.isDirty = true;
			}
			reachedEnd = true;
		}
		else {
			undo = undos[currentUndoIndex];
			undo.ExecuteRedo();
			editor.isDirty = true;
		}
		
		int previousIndex = currentUndoIndex;
		currentUndoIndex++;
		ClampUndoIndex();
		
		lastWasUndo = false;
		
		if (undos.Count > 0 && previousIndex != currentUndoIndex) {
			UIDEUndo previousUndo = undos[previousIndex];
			UIDEUndo nextUndo = undos[currentUndoIndex];
			if (previousUndo.groupID == nextUndo.groupID) {
				DoRedo();
			}
		}
	}
	
	public UIDEUndo RegisterUndo(string groupID,UIDEUndoType type, int lineNumber, string oldData, string newData, Vector2 oldCursorPosition, Vector2 newCursorPosition) {
		UIDEUndo undo = UIDEUndo.Create(this,groupID,type,lineNumber,oldData,newData,oldCursorPosition,newCursorPosition);
		
		//UnityEditor.Undo.RegisterUndo(new UnityEngine.Object[] {this} ,undo.groupID);
		editor.AddDummyUndo();
		
		ClampUndoIndex();
		if (undos.Count > 0 && currentUndoIndex < undos.Count-1) {
			undos.RemoveRange(currentUndoIndex+1,undos.Count-(currentUndoIndex+1));
		}
		undos.Add(undo);
		currentUndoIndex = undos.Count-1;
		undoCounter++;
		
		lastWasUndo = false;
		reachedEnd = true;
		
		editor.undoCounter = undoCounter;
		UIDEEditor.SetDirty(this);
		
		editor.isDirty = true;
		return undo;
	}
	
	public void ClampUndoIndex() {
		currentUndoIndex = Mathf.Clamp(currentUndoIndex,0,undos.Count-1);
	}
	
}
