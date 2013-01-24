using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

[System.Serializable]
public class UIDESelection:System.Object {
	public UIDECursor cursor;
	public Vector2 start;
	public Vector2 end;
	
	public bool hasSelection {
		get {
			return start != end;
		}
	}
	
	public Vector2 actualStart {
		get {
			if (end.y == start.y) {
				if (end.x < start.x) {
					return end;
				}
				else {
					return start;
				}
			}
			if (end.y < start.y) {
				return end;
			}
			return start;
		}
	}
	
	public Vector2 actualEnd {
		get {
			if (end.y == start.y) {
				if (end.x >= start.x) {
					return end;
				}
				else {
					return start;
				}
			}
			if (end.y > start.y) {
				return end;
			}
			return start;
		}
	}
	
	public string GetSelectedText() {
		UIDELine[] lines = GetAffectedLines();
		string text = "";
		for (int i = 0; i < lines.Length; i++) {
			text += GetSelectedLineText(lines[i]);
			if (i < lines.Length-1) {
				text += "\r\n";
				//text += System.Environment.NewLine;
			}
		}
		return text;
	}
	public void DeleteSelectedText(bool includeEnd) {
		DeleteSelectedText(includeEnd,"",null);
	}
	public void DeleteSelectedText(bool includeEnd, string overrideUndoname, UIDECursor c) {
		UIDELine[] lines = GetAffectedLines();
		List<UIDELine> linesToDelete = new List<UIDELine>();
		List<UIDELine> linesToRebuild = new List<UIDELine>();
		
		UIDEUndoManager undoManager = cursor.editor.undoManager;
		int uniqueUndoID = undoManager.GetUniqueID();
		string undoName = "Delete Selected Text "+uniqueUndoID;
		if (overrideUndoname != "") {
			undoName = overrideUndoname;
		}
		Vector2 oldCursorPos = cursor.GetVectorPosition();
		Vector2 newCursorPos = cursor.GetVectorPosition();
		
		bool isMultiLine = lines.Length > 1;
		UIDELine firstLine = null;
		for (int i = 0; i < lines.Length; i++) {
			string originalText = lines[i].rawText;
			
			
			if (i == 0) {
				firstLine = lines[i];
			}
			bool isLastLine = i >= lines.Length-1;
			if (IsLineBetweenStartAndEnd(lines[i])) {
				linesToDelete.Add(lines[i]);
				//undoManager.RegisterUndo(undoName,UIDEUndoType.LineRemove,lines[i].index,originalText,originalText,oldCursorPos,newCursorPos);
			}
			else {
				Vector2 partialSelRange = GetLineSelectionRange(lines[i]);
				string newLineText = lines[i].rawText.Substring(0,(int)partialSelRange.x);
				newLineText += lines[i].rawText.Substring((int)partialSelRange.y);
				
				if (isLastLine && isMultiLine) {
					string originalFirstLineText = firstLine.rawText;
					firstLine.rawText += newLineText;
					linesToDelete.Add(lines[i]);
					undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,firstLine.index,originalFirstLineText,firstLine.rawText,oldCursorPos,newCursorPos);
					//undoManager.RegisterUndo(undoName,UIDEUndoType.LineRemove,lines[i].index,originalText,originalText,oldCursorPos,newCursorPos);
				}
				else {
					lines[i].rawText = newLineText;
					linesToRebuild.Add(lines[i]);
					undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,lines[i].index,originalText,lines[i].rawText,oldCursorPos,newCursorPos);
				}
			}
		}
		for (int i = 0; i < linesToDelete.Count; i++) {
			//tricky but it works because the line indices change after each remove
			string originalText = linesToDelete[i].rawText;
			cursor.doc.RemoveLine(linesToDelete[i].index);
			undoManager.RegisterUndo(undoName,UIDEUndoType.LineRemove,linesToDelete[i].index,originalText,originalText,oldCursorPos,newCursorPos);
		}
		for (int i = 0; i < linesToRebuild.Count; i++) {
			linesToRebuild[i].RebuildElements();
		}
		//cursor.doc.re
	}
	
	public UIDELine[] GetAffectedLines() {
		if (cursor.doc == null) {
			Debug.LogError("cursor.doc is null!");
			return new UIDELine[] {};
		}
		if (!hasSelection) return new UIDELine[] {};
		List<UIDELine> lines = new List<UIDELine>();
		int startLine = (int)actualStart.y;
		int endLine = (int)actualEnd.y;
		for (int i = startLine; i <= endLine; i++) {
			if (i < 0 || i >= cursor.doc.lineCount) continue;
			lines.Add(cursor.doc.LineAt(i));
		}
		return lines.ToArray();
	}
	public string GetSelectedLineText(UIDELine line) {
		bool isEffectedBySelection = hasSelection && IsLineAffected(line);
		if (isEffectedBySelection) {
			Vector2 selectionRange = GetLineSelectionRange(line);
			return line.rawText.Substring((int)selectionRange.x,(int)(selectionRange.y-selectionRange.x));
		}
		return "";
	}
	public bool IsLineAffected(UIDELine line) {
		if (!hasSelection) return false;
		if ((int)actualStart.y == line.index || (int)actualEnd.y == line.index) {
			return true;
		}
		if ((int)actualStart.y <= line.index && (int)actualEnd.y >= line.index) {
			return true;
		}
		return false;
	}
	
	public bool IsLineBetweenStartAndEnd(UIDELine line) {
		if (!hasSelection) return false;
		if ((int)actualStart.y < line.index && (int)actualEnd.y > line.index) {
			return true;
		}
		return false;
	}
	public bool IsLineBetweenStartAndIncludingEnd(UIDELine line) {
		if (!hasSelection) return false;
		if ((int)actualStart.y < line.index && (int)actualEnd.y >= line.index) {
			return true;
		}
		return false;
	}
	
	public Vector2 GetLineSelectionRange(UIDELine line) {
		Vector2 v = GetLineSelectionRangeRaw(line);
		v.x = Mathf.Clamp(v.x,0,line.rawText.Length);
		v.y = Mathf.Clamp(v.y,0,line.rawText.Length);
		return v;
	}
	private Vector2 GetLineSelectionRangeRaw(UIDELine line) {
		if (line == null) return new Vector2(0,0);
		if ((int)actualStart.y < line.index && (int)actualEnd.y > line.index) {
			return new Vector2(0,line.rawText.Length);
		}
		if ((int)actualStart.y == line.index) {
			if ((int)actualEnd.y > line.index) {
				return new Vector2((int)actualStart.x,line.rawText.Length);
			}
			else {
			//if ((int)actualEnd.y == line.index) {
				return new Vector2((int)actualStart.x,(int)actualEnd.x);
			}
		}
		if ((int)actualStart.y < line.index) {
			if ((int)actualEnd.y == line.index) {
				return new Vector2(0,(int)actualEnd.x);
			}
		}
		//This should never happen
		Debug.LogWarning("Line \""+line.rawText+"\" has failed GetLineSelectionRange()");
		return new Vector2(0,0);
	}
}

