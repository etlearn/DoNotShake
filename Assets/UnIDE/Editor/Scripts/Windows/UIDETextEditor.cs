using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System;
using System.IO;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UIDE.SyntaxRules;
using UIDE;
using UIDE.RightClickMenu;

public class UIDETextEditor:UIDEWindow {
	public string filePath = "";
	public string fileName = "";
	public string fileNameNoExt = "";
	public string extension = "";
	
	public string undoAssetPath;
	
	public string tabName {
		get {
			string s = fileNameNoExt;
			if (isDirty) {
				s += "*";
			}
			return s;
		}
	}
	
	public float minExtraLineCount = 10.0f;
	public float desiredExtraLineCount = 0.0f;
	
	public Vector2 textXOffset = new Vector2(0,0);
	public Vector2 charSize;
	public Vector2 windowMousePos;
	public Vector2 windowMousePosWithTabBar;
	public Vector2 currentMousePos {
		get {
			if (Event.current == null) return Vector3.zero;
			return Event.current.mousePosition;
		}
	}
	
	public GUIStyle textElementBackgroundStyle;
	
	public List<Func<UIDELine,bool>> onPreRenderLineCallbacks = new List<Func<UIDELine,bool>>();
	public List<Action<UIDELine>> onPostRenderLineCallbacks = new List<Action<UIDELine>>();
	public List<Func<UIDEElement,bool>> onPreRenderElementCallbacks = new List<Func<UIDEElement,bool>>();
	
	public Rect textEditorRect;
	public Rect textEditorRectZeroPos;
	public Rect textEditorNoScrollBarRect;
	public Rect textEditorNoScrollBarRectZeroPos;
	public Rect actualTextAreaRect;
	public Rect actualTextAreaNoScrollBarRect;
	public Rect tabRect;
	public Rect tabRectZeroPos;
	public float lineNumberWidth;
	public float desiredWidth;
	public float desiredTabBarHeight;
	
	public Vector2 mouseDownTextPos;
	[System.NonSerialized]
	public bool isDragSelecting = false;
	public bool isDoubleClickDragSelecting = false;
	private Vector4 doubleClickDragBounds; 
	
	public bool enableTextEnter = true;
	private bool _enableClick = true;
	public bool enableClick {
		get {
			return _enableClick;
		}
		set {
			if (_enableClick != value) {
				_enableClick = value;
				for (int i = 0; i < _plugins.Count; i++) {
					_plugins[i].OnTextEditorCanClickChanged();
				}
			}
		}
	}
	public List<ClickBlocker> clickBlockers = new List<ClickBlocker>();
	public List<KeyCode> disabledKeys = new List<KeyCode>();
	
	
	public bool isDirty = false;
	
	public UIDEUndoManager undoManager;
	public int undoCounter = 0;
	public bool didUndoRedo = false;
	public bool undoInfiniteLoopBlock = false;
	
	public UIDEDoc doc;
	public SyntaxRule syntaxRule;
	
	[System.NonSerialized]
	public bool reloadIsOk = false;
	
	[SerializeField]
	private UIDECursor _cursor;
	public UIDECursor cursor {
		get {
			if (_cursor == null) {
				Debug.LogError("Cursor is null!");
				return null;
			}
			_cursor.editor = this;
			_cursor.selection.cursor = _cursor;
			return _cursor;
		}
		set {
			_cursor = value;
			_cursor.editor = this;
		}
	}
	private List<UIDEPlugin> _plugins;
	public List<UIDEPlugin> plugins {
		get {
			return _plugins;
		}
	}
	
	public List<int> actualLinesReversePointer = new List<int>();
	public List<int> actualLinesToRender = new List<int>();
	public int renderedLineCount {
		get {
			return actualLinesToRender.Count;
		}
	}
	
	private string selectionHighlightText = "";
	private UIDEElement selectionHighlightElement;
	private Vector2 lastSelectionStart;
	private Vector2 lastSelectionEnd;
	
	private bool wantsPluginUpdate = false;
	private UIDECursorPos lastPos = new UIDECursorPos(0,0);
	
	private Vector2 lastMousePos;
	
	private string textAreaText = "";
	private EventType initialEventType;
	
	private bool justDidPaste = false;
	private bool justDidDuplicate = false;
	
	public UIDETextEditor textEditorToSwitchTo = null;
	
	public UIDETextEditor() {
		
	}
	public UIDETextEditor(float f) {
		
	}
	public UIDETextEditor(float f, string s) {
		
	}
	
	static public UIDETextEditor Load(UIDEEditor editorWindow, string path) {
		path = path.Replace("\\","/").Replace("//","/");
		if (!File.Exists(path)) {
			Debug.LogWarning("Tried to open non-existant file \""+path+"\"");
			return null;
		}
		
		UIDETextEditor textEditor = (UIDETextEditor)ScriptableObject.CreateInstance(typeof(UIDETextEditor));
		textEditor.doc = UIDEDoc.Load(textEditor,path);
		textEditor.cursor = new UIDECursor(textEditor);
		textEditor.filePath = path;
		
		textEditor.fileName = Path.GetFileName(path);
		textEditor.fileNameNoExt = Path.GetFileNameWithoutExtension(path);
		textEditor.extension = Path.GetExtension(path).ToLower();
		
		textEditor.undoManager = (UIDEUndoManager)ScriptableObject.CreateInstance(typeof(UIDEUndoManager));
		textEditor.undoManager.editor = textEditor;
		
		#if UNITY_EDITOR
		//AssetDatabase.DeleteAsset(assetPath);
		
		string assetPath = UIDEEditor.tmpDir+"TextEditors/"+textEditor.doc.path+".tmp.asset";
		string undoAssetPath = UIDEEditor.tmpDir+"TextEditors/"+textEditor.doc.path+".tmp.undo.asset";
		
		string assetDir = Path.GetDirectoryName(assetPath);

		if (!Directory.Exists(assetDir)) {
			Directory.CreateDirectory(assetDir);
		}
		textEditor.assetPath = assetPath;
		textEditor.undoAssetPath = undoAssetPath;
		
		AssetDatabase.CreateAsset(textEditor, assetPath);
		AssetDatabase.CreateAsset(textEditor.undoManager, undoAssetPath);
		
		//AssetDatabase.SaveAssets();
		#endif
		
		textEditor.wantsPluginUpdate = true;
		textEditor.CheckPluginUpdate();
		
		return textEditor;
	}
	
	public void UpdateActualRenderedLines() {
		actualLinesToRender = new List<int>();
		actualLinesReversePointer = new List<int>();
		for (int i = 0; i < doc.lineCount; i++) {
			UIDELine line = doc.RealLineAt(i);
			actualLinesToRender.Add(line.index);
			actualLinesReversePointer.Add(i);
			if (line.isFolded) {
				for (int j = 0; j < line.foldingLength; j++) {
					actualLinesReversePointer.Add(i+1+j);
				}
				i += line.foldingLength;
			}
		}
	}
	
	public int GetLinesRenderedPosition(UIDELine line) {
		if (line == null) return 0;
		return GetLinesRenderedPosition(line.index);
	}
	public int GetLinesRenderedPosition(int lineNumber) {
		if (lineNumber < 0 || lineNumber >= actualLinesReversePointer.Count) return lineNumber;
		return actualLinesReversePointer[lineNumber];
	}
	public UIDELine GetLine(int lineIndex) {
		UIDELine line = null;
		//Debug.Log(lineIndex+" "+actualLinesToRender.Count);
		if (lineIndex >= 0 && lineIndex < actualLinesToRender.Count) {
			int i = actualLinesToRender[lineIndex];
			
			if (i >= 0 && i < doc.lineCount) {
				line = doc.RealLineAt(i);
				
			}
		}
		return line;
	}
	
	public void UpdatePlugins() {
		//Debug.Log("Updating Plugin Instances");
		
		if (_plugins != null) {
			for (int i = 0; i < _plugins.Count; i++) {
				if (_plugins[i] != null) {
					_plugins[i].OnDestroy();
				}
			}
		}
		
		_plugins = UIDEPlugin.GetPluginInstances(this).ToList();
		
		SyntaxRule defaultRule = null;
		for (int i = 0; i < _plugins.Count; i++) {
			_plugins[i].editor = this;
			if (typeof(SyntaxRule).IsAssignableFrom(_plugins[i].GetType())) {
				SyntaxRule sr = (SyntaxRule)_plugins[i];
				if (sr.isDefault) {
					defaultRule = (SyntaxRule)_plugins[i];
				}
				else {
					syntaxRule = sr;
				}
			}
		}
		
		if (defaultRule != null) {
			if (syntaxRule == null || syntaxRule.GetType() == typeof(SyntaxRule)) {
				syntaxRule = defaultRule;
			}
			else {
				defaultRule.OnDestroy();
				_plugins.Remove(defaultRule);
			}
		}
		
		string[] apiAssemblies = editorWindow.generalSettings.GetAPITokenAssemblies().ToArray();
		if (apiAssemblies.Length == 0) {
			apiAssemblies = null;
		}
		
		APITokens.Update(apiAssemblies);
		
		for (int i = 0; i < _plugins.Count; i++) {
			_plugins[i].Start();
		}
		
		if (doc != null) {
			doc.OnPluginsReloaded();
		}
	}
	
	public bool VarifyTextEditor() {
		if (!doc.initialized) {
			doc.Initialize(this);
		}
		return true;
	}
	
	public void AddDummyUndo() {
		AddDummyUndo("UnIDE");
	}
	
	public void AddDummyUndo(string undoName) {
		UIDEEditor.SetDirty(undoManager);
		UIDEUndoManager sc = null;
		if (sc == null) {
			sc = ScriptableObject.CreateInstance<UIDEUndoManager>();
		}
		sc.tmpString = "rt"+undoName; 
		Undo.RegisterUndo(new UnityEngine.Object[] {sc}, undoName);
		sc.tmpString = "fsfsgfdgdfg"+undoName;
		EditorUtility.SetDirty(sc);
		
		GameObject.DestroyImmediate(sc);
	}
	
	public void OnSwitchToTab() {
		if (plugins != null) {
			for (int i = 0; i < plugins.Count; i++) {
				plugins[i].OnSwitchToTab();
			}
		}
	}
	public void OnSwitchToOtherTab() {
		if (plugins != null) {
			for (int i = 0; i < plugins.Count; i++) {
				plugins[i].OnSwitchToOtherTab();
			}
		}
	}
	
	public void OnWindowFocus() {
		if (editorWindow == null) return;
		if (editorWindow.generalSettings.GetUseCtrlZUndo()) {
			//Debug.Log("dsf");
			//EditorWindow.FocusWindowIfItsOpen(typeof(UIDEEditorWindow));
			//Debug.Log(editorWindow.focu);
			//Debug.Log("ddd");
			//TouchDummyAsset();
			
			Undo.ClearUndo(undoManager);
			
			undoManager.dummyCounter++;
			UIDEEditor.SetDirty(undoManager);
			
			AddDummyUndo("UnIDE Focus");
			
			TouchDummyAsset();
		}
		
		if (plugins != null) {
			for (int i = 0; i < plugins.Count; i++) {
				plugins[i].OnFocus();
			}
		}
	}
	public void OnLostWindowFocus() {
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnLostFocus();
		}
	}
	
	private void TouchDummyAsset() {
		isFocusDummyUndoRedo = true;
		Texture2D dummyTex = (Texture2D)UIDEEditor.LoadAsset(UIDEEditor.baseDir+"Editor/DUMMYASSET.psd");
		
		TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(UIDEEditor.baseDir+"Editor/DUMMYASSET.psd"); 
		textureImporter.mipmapEnabled = !textureImporter.mipmapEnabled;
		AssetDatabase.ImportAsset(UIDEEditor.baseDir+"Editor/DUMMYASSET.psd");
		
		Undo.RegisterUndo(dummyTex,"UnIDE Focus");
		Undo.PerformUndo();
		Undo.PerformRedo();
		
		isFocusDummyUndoRedo = false;
		
		//UnityEngine.Object orig = Selection.activeObject;
		//Selection.activeObject = dummyTex;
		//Selection.activeObject = orig;
	}
	
	public void OnEnable() {
		wantsPluginUpdate = true;
	}
	public void OnDisable() {
		
	}
	
	public void OnSelectionChanged() {
		selectionHighlightText = "";
		selectionHighlightElement = null;
		if (cursor.selection.hasSelection && cursor.selection.start.y == cursor.selection.end.y) {
			UIDELine line = doc.LineAt(cursor.posY);
			if (line != null) {
				string selectedLineText = cursor.selection.GetSelectedLineText(line);
				UIDEElement element = line.GetElementAt((int)cursor.selection.actualStart.x);
				if (element != null && element.tokenDef.HasType("Word")) {
					int elementStart = line.GetElementStartPos(element);
					if (elementStart == (int)cursor.selection.actualStart.x && element.rawText == selectedLineText) {
						selectionHighlightText = element.rawText;
						selectionHighlightElement = element;
					}
				}
			}
		}
	}
	
	public void OnChangedCursorPosition() {
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnChangedCursorPosition(cursor.GetVectorPosition());
		}
		UIDELine line = doc.LineAt(cursor.posY);
		if (line != null) {
			UIDEElement element = line.GetElementAt(cursor.posX);
			for (int i = 0; i < plugins.Count; i++) {
				plugins[i].OnChangedCursorPosition(cursor.GetVectorPosition(),element);
			}
		}
	}
	
	public void OnTabEntered() {
		
		if (cursor.selection.hasSelection) {
			UIDELine[] lines = cursor.selection.GetAffectedLines();
			if (lines.Length > 1) {
				string undoName = "Tab Entered "+undoManager.GetUniqueID();
				for (int i = 0; i < lines.Length; i++) {
					if (Event.current.shift) {
						if (lines[i].rawText.Length > 0 && char.IsWhiteSpace(lines[i].rawText[0])) {
							int tabSize = 4;
							if (lines[i].rawText[0] == '\t') {
								string originalText = lines[i].rawText;
								lines[i].rawText = lines[i].rawText.Substring(1);
								lines[i].RebuildElements();
								undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,lines[i].index,originalText,lines[i].rawText,cursor.GetVectorPosition(),cursor.GetVectorPosition());
							}
							else if (lines[i].rawText[0] == ' ') {
								string originalText = lines[i].rawText;
								for (int j = 0; j < tabSize; j++) {
									if (lines[i].rawText.Length <= 0) break;
									lines[i].rawText = lines[i].rawText.Substring(1);
								}
								lines[i].RebuildElements();
								undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,lines[i].index,originalText,lines[i].rawText,cursor.GetVectorPosition(),cursor.GetVectorPosition());
							}
						}
					}
					else {
						string originalText = lines[i].rawText;
						lines[i].rawText = "\t"+lines[i].rawText;
						lines[i].RebuildElements();
						undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,lines[i].index,originalText,lines[i].rawText,cursor.GetVectorPosition(),cursor.GetVectorPosition());
					}
					//if (i == lines.Length) {
					//	cursor.posX = lines[i].rawText.Length;
					//}
				}
			}
			else {
				EnterText("\t");
			}
		}
		else {
			EnterText("\t");
		}
	}
	
	public void EnterText(string text) {
		EnterText(text,"");
	}
	public void EnterText(string text,string overrideUndoID) {
		EnterText(text,"",false);
	}
	public void EnterText(string text, string overrideUndoID, bool isPaste) {
		if (cursor.selection.hasSelection) {
			DeleteSelection();
		}
		for (int i = 0; i < plugins.Count; i++) {
			text = plugins[i].OnPreEnterText(text);
		}
		
		List<UIDELine> changedLines = new List<UIDELine>();
		
		if (isPaste) {
			text = text.Replace("\r\n","\n").Replace("\n\r","\n");
			string[] pasteLines = text.Split('\n');
			
			Vector2 oldCursorPos = cursor.GetVectorPosition();
			
			int lineIndex = cursor.posY;
			UIDELine line = doc.RealLineAt(lineIndex);
			string originalLineText = line.rawText;
			string firstString = line.rawText.Substring(0,cursor.posX);
			string lastString = line.rawText.Substring(cursor.posX);
			
			line.rawText = firstString+pasteLines[0];
			
			cursor.posY = line.index;
			cursor.posX = line.rawText.Length;
			Vector2 newCursorPos = cursor.GetVectorPosition();
			
			if (pasteLines.Length == 1) {
				line.rawText = line.rawText+lastString;
			}
			
			changedLines.Add(line);
			
			string nameValue = "Create New Line "+undoManager.GetUniqueID();
			if (overrideUndoID != "") {
				nameValue = overrideUndoID;
			}
			undoManager.RegisterUndo(nameValue,UIDEUndoType.LineModify,line.index,originalLineText,line.rawText,oldCursorPos,newCursorPos);
			undoManager.RegisterUndo(nameValue,UIDEUndoType.LineModify,line.index,originalLineText,line.rawText,oldCursorPos,newCursorPos);
			
			if (pasteLines.Length > 1) {
				//UIDELine finalLine = line;
				for (int i = 1; i < pasteLines.Length; i++) {
					oldCursorPos = cursor.GetVectorPosition();
					int newLineIndex = lineIndex+i;
					UIDELine newLine = doc.InsertLine(newLineIndex,pasteLines[i]);
					cursor.posY = newLine.index;
					cursor.posX = newLine.rawText.Length;
					newCursorPos = cursor.GetVectorPosition();
					if (i >= pasteLines.Length-1) {
						newLine.rawText = newLine.rawText+lastString;
					}
					undoManager.RegisterUndo(nameValue,UIDEUndoType.LineAdd,newLineIndex,newLine.rawText,newLine.rawText,oldCursorPos,newCursorPos);
					//finalLine = newLine;
					changedLines.Add(newLine);
				}
				
				
			}
		}
		else {
			//text = text.Replace("\t",UIDETokenDefs.tabToSpaceString);
			if (text.IndexOf("\n") == -1 && text.IndexOf("\r") == -1) {
				changedLines.AddRange(EnterCharacter(text,overrideUndoID));
			}
			else {
				for (int i = 0; i < text.Length; i++) {
					UIDELine[] theseChangedLines = EnterCharacter(text[i].ToString(),overrideUndoID);
					for (int j = 0; j < theseChangedLines.Length; j++) {
						if (changedLines.IndexOf(theseChangedLines[j]) == -1) {
							changedLines.Add(theseChangedLines[j]);
						}
					}
				}
			}
		}
		
		for (int i = 0; i < changedLines.Count; i++) {
			changedLines[i].RebuildElements();
		}
		
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPostEnterText(text);
		}
		
		
	}
	
	public UIDELine[] EnterCharacter(string text, string overrideUndoID) {
		return EnterCharacter(text,overrideUndoID,false);
	}
	public UIDELine[] EnterCharacter(string text, string overrideUndoID, bool noUndo) {
		if (text == null || text.Length == 0) return new UIDELine[0];
		List<UIDELine> changedLines = new List<UIDELine>();
				
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPreEnterCharacter(text);
		}
		
		if (cursor != null) {
			UIDELine line = doc.LineAt(cursor.posY);
			
			if (text == "\n" || text == "\r") {
				
				string firstString = line.rawText.Substring(0,cursor.posX);
				string lastString = line.rawText.Substring(cursor.posX);
				
				int newLineIndex = cursor.posY+1;
				string originalLineText = line.rawText;
				Vector2 oldCursorPos = cursor.GetVectorPosition();
				
				line.rawText = firstString;
				UIDELine newLine = doc.InsertLine(newLineIndex,lastString);
				cursor.posY += 1;
				cursor.posX = 0;
				
				Vector2 newCursorPos = cursor.GetVectorPosition();
				
				string nameValue = "Create New Line "+undoManager.GetUniqueID();
				if (overrideUndoID != "") {
					nameValue = overrideUndoID;
				}
				undoManager.RegisterUndo(nameValue,UIDEUndoType.LineModify,line.index,originalLineText,line.rawText,oldCursorPos,newCursorPos);
				undoManager.RegisterUndo(nameValue,UIDEUndoType.LineAdd,newLineIndex,newLine.rawText,newLine.rawText,oldCursorPos,newCursorPos);
				
				//undoManager.RegisterCurrentState(nameValue,UIDEUndoType.LineModify,line.index,line.rawText);
				//undoManager.RegisterCurrentState(nameValue,UIDEUndoType.LineAdd,newLine.index,newLine.rawText);
				
				changedLines.Add(newLine);
			}
			else {
				Vector2 oldCursorPos = cursor.GetVectorPosition();
				string originalText = line.rawText;
				
				line.rawText = line.rawText.Insert(cursor.posX,text);
				cursor.posX += text.Length;
				
				Vector2 newCursorPos = cursor.GetVectorPosition();
				
				string undoName;
				if (overrideUndoID != "") {
					undoName = overrideUndoID;
				}
				else {
					undoName = "Enter Character "+cursor.posY+" ";
					string charType = "";
					if (char.IsLetterOrDigit(text[0])) {
						charType = "word";
					}
					else if (char.IsWhiteSpace(text[0])) {
						charType = "whitespace";
					}
					else {
						charType = "other";
					}
					undoName += charType;
				}
				undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,line.index,originalText,line.rawText,oldCursorPos,newCursorPos);
			}
			if (changedLines.IndexOf(line) == -1) {
				changedLines.Add(line);
			}
		
		}
		
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPostEnterCharacter(text);
		}
		return changedLines.ToArray();
	}
	
	public void DeleteLineRange(UIDELine line, int start, int end) {
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPreDeleteLineRange(line, start, end);
		}
		string firstStr = line.rawText.Substring(0,start);
		string lastStr = line.rawText.Substring(end);
		line.rawText = firstStr+lastStr;
		
	}
	
	public void DeleteSelection() {
		if (!cursor.selection.hasSelection) return;
		DeleteSelection("Delete Selected "+undoManager.GetUniqueID());
	}
	
	public void DeleteSelection(string undoName) {
		if (cursor.selection.hasSelection) {
			Vector2 oldCursorPosition = cursor.GetVectorPosition();
			
			cursor.selection.DeleteSelectedText(true,undoName,cursor);
			
			cursor.selection.start = cursor.selection.actualStart;
			cursor.selection.end = cursor.selection.start;
			
			cursor.posX = (int)cursor.selection.actualStart.x;
			cursor.posY = (int)cursor.selection.actualStart.y;
			
			if (undoManager.undos.Count > 0) {
				UIDEUndo undo = undoManager.undos[undoManager.undos.Count-1];
				if (undo.groupID == undoName) {
					undo.oldCursorPosition = oldCursorPosition;
					undo.newCursorPosition = cursor.GetVectorPosition();
				}
			}
		}
	}
	
	public void DoDelete() {
		UIDELine line = doc.RealLineAt(cursor.posY);
		if (cursor.selection.hasSelection) {
			DeleteSelection();
		}
		else if (cursor.posX < line.rawText.Length) {
			//UIDELine line = doc.LineAt(cursor.posY);
			string originalText = line.rawText;
			Vector2 oldCursorPos = cursor.GetVectorPosition();
			DeleteLineRange(line,cursor.posX,cursor.posX+1);
			//cursor.posX -= 1;
			line.RebuildElements();
			
			Vector2 newCursorPos = cursor.GetVectorPosition();
			
			string nameValue = "Delete Character "+undoManager.GetUniqueID();
			undoManager.RegisterUndo(nameValue,UIDEUndoType.LineModify,line.index,originalText,line.rawText,oldCursorPos,newCursorPos);
			
			
		}
		else if (cursor.posY < doc.lineCount-1) {
			//UIDELine line = doc.LineAt(cursor.posY);
			UIDELine belowLine = doc.LineAt(cursor.posY+1);
			
			string originalBelowLineText = belowLine.rawText;
			string originalText = line.rawText;
			Vector2 oldCursorPos = cursor.GetVectorPosition();
			
			//int belowLineOriginalLength = belowLine.rawText.Length;
			line.rawText += belowLine.rawText;
			doc.RemoveLine(belowLine.index);
			//cursor.posY -= 1;
			//cursor.posX = belowLineOriginalLength;
			line.RebuildElements();
			
			Vector2 newCursorPos = cursor.GetVectorPosition();
			
			string nameValue = "Delete Line "+undoManager.GetUniqueID();
			undoManager.RegisterUndo(nameValue,UIDEUndoType.LineModify,line.index,originalText,line.rawText,oldCursorPos,newCursorPos);
			undoManager.RegisterUndo(nameValue,UIDEUndoType.LineModify,belowLine.index,originalBelowLineText,originalBelowLineText,oldCursorPos,newCursorPos);
			undoManager.RegisterUndo(nameValue,UIDEUndoType.LineRemove,belowLine.index,originalBelowLineText,originalBelowLineText,oldCursorPos,newCursorPos);
			
		}
		editorWindow.Repaint();
	}
	
	public void DoBackspace() {
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPreBackspace();
		}
		
		if (cursor.selection.hasSelection) {
			DeleteSelection();
		}
		else if (cursor.posX > 0) {
			UIDELine line = doc.LineAt(cursor.posY);
			string originalText = line.rawText;
			Vector2 oldCursorPos = cursor.GetVectorPosition();
			DeleteLineRange(line,cursor.posX-1,cursor.posX);
			cursor.posX -= 1;
			line.RebuildElements();
			
			Vector2 newCursorPos = cursor.GetVectorPosition();
			
			string nameValue = "Backspace Character "+undoManager.GetUniqueID();
			undoManager.RegisterUndo(nameValue,UIDEUndoType.LineModify,line.index,originalText,line.rawText,oldCursorPos,newCursorPos);
			
			
		}
		else if (cursor.posY > 0) {
			UIDELine line = doc.LineAt(cursor.posY);
			UIDELine aboveLine = doc.LineAt(cursor.posY-1);
			
			string originalAboveLineText = aboveLine.rawText;
			string originalText = line.rawText;
			Vector2 oldCursorPos = cursor.GetVectorPosition();
			
			int aboveLineOriginalLength = aboveLine.rawText.Length;
			aboveLine.rawText += line.rawText;
			doc.RemoveLine(cursor.posY);
			
			cursor.posY -= 1;
			cursor.posX = aboveLineOriginalLength;
			aboveLine.RebuildElements();
			
			Vector2 newCursorPos = cursor.GetVectorPosition();
			
			string nameValue = "Backspace Line "+undoManager.GetUniqueID();
			undoManager.RegisterUndo(nameValue,UIDEUndoType.LineModify,aboveLine.index,originalAboveLineText,aboveLine.rawText,oldCursorPos,newCursorPos);
			undoManager.RegisterUndo(nameValue,UIDEUndoType.LineRemove,line.index,originalText,originalText,oldCursorPos,newCursorPos);
			
		}
		
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPostBackspace();
		}
		editorWindow.Repaint();
	}
	
	public void ToggleCommentLines() {
		UIDELine[] lines = new UIDELine[] {doc.RealLineAt(cursor.posY)};
		if (cursor.selection.hasSelection) {
			lines = cursor.selection.GetAffectedLines();
		}
		string undoName = "Comment Lines "+undoManager.GetUniqueID();
		int commentedLineCount = 0;
		for (int i = 0; i < lines.Length; i++) {
			if (syntaxRule.IsLineCommented(lines[i])) {
				commentedLineCount++;
			}
		}
		bool shouldComment = commentedLineCount <= ((lines.Length/2)-(lines.Length%1));
		//shouldComment = commentedLineCount == 0;
		
		for (int i = 0; i < lines.Length; i++) {
			if (shouldComment) {
				syntaxRule.CommentLine(lines[i],undoName);
			}
			else {
				syntaxRule.UncommentLine(lines[i],undoName);
			}
		}
	}
	
	public void DeleteLine(UIDELine line) {
		string originalText = line.rawText;
		Vector2 oldCursorPos = cursor.GetVectorPosition();
		
		doc.RemoveLine(line.index);
		
		string undoName = "Delete Line "+undoManager.GetUniqueID();
		undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,line.index,originalText,originalText,oldCursorPos,oldCursorPos);
		undoManager.RegisterUndo(undoName,UIDEUndoType.LineRemove,line.index,originalText,originalText,oldCursorPos,oldCursorPos);
	}
	public void DuplicateLine(UIDELine line) {
		Vector2 oldCursorPos = cursor.GetVectorPosition();
		UIDELine newLine = doc.InsertLine(line.index,line.rawText);
		newLine.RebuildElements();
		
		string undoName = "Dyplicate Line "+undoManager.GetUniqueID();
		//undoManager.RegisterUndo(undoName,UIDEUndoType.LineModify,line.index,originalText,originalText,oldCursorPos,oldCursorPos);
		undoManager.RegisterUndo(undoName,UIDEUndoType.LineAdd,newLine.index,newLine.rawText,newLine.rawText,oldCursorPos,oldCursorPos);
	}
	
	public void DoPaste() {
		string text = UIDEClipboardHelper.clipBoard;
		string undoName = "Paste "+undoManager.GetUniqueID();
		
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPrePaste(text);
		}
		
		text = text.Replace("\n\r","\n");
		text = text.Replace("\r\n","\n");
		DeleteSelection(undoName);
		EnterText(text,undoName,true);
		
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPostPaste(text);
		}
	}

	//FUN FACT: This was the first editor feature to be wrtten with itself
	//====================================================================
	public void DoCopy() {
		if (!cursor.selection.hasSelection) return;
		string text = cursor.selection.GetSelectedText();
		UIDEClipboardHelper.clipBoard = text;
	}
	
	public void DoCut() {
		if (!cursor.selection.hasSelection) return;
		string text = cursor.selection.GetSelectedText();
		UIDEClipboardHelper.clipBoard = text;
		DeleteSelection();
		editorWindow.Repaint();
	}
	public void DoSelectAll() {
		UIDELine lastLine = doc.RealLineAt(doc.lineCount-1);
		cursor.selection.start = new Vector2(0,0);
		cursor.selection.end = new Vector2(lastLine.rawText.Length,lastLine.index);
		editorWindow.Repaint();
	}
	
	public void DoSave() {
		reloadIsOk = true;
		doc.Save();
		#if UNITY_EDITOR
		AssetDatabase.ImportAsset(doc.path);
		#endif
		isDirty = false;
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnSavePerformed();
		}
	}
	
	public void DoUndo() {
		undoManager.DoUndo();
		undoCounter = undoManager.undoCounter;
		ScrollToLine(cursor.posY,3);
		ScrollToColumn(cursor.posY,cursor.posX);
	}
	
	public void DoRedo() {
		undoManager.DoRedo();
		undoCounter = undoManager.undoCounter;
		ScrollToLine(cursor.posY,3);
		ScrollToColumn(cursor.posY,cursor.posX);
	}
	
	public RCMenu CreateRCMenu(RCMenuItem[] items) {
		RCMenu menu = new RCMenu();
		menu.position = windowMousePosWithTabBar;
		menu.position.x += rect.x;
		menu.position.y += rect.y;
		menu.position.x += 1;
		menu.position.y += 1;
		
		for (int i = 0; i < items.Length; i++) {
			menu.AddItem(items[i]);
		}
		
		RCMenu.ShowMenu(menu);
		
		editorWindow.Repaint();
		return menu;
		
	}
	
	public void OnRightClickTab(UIDETextEditor te) {
		List<RCMenuItem> items = new List<RCMenuItem>();
		
		RCMenuItem copyItem = new RCMenuItem("Close",RightClickCloseTabCallback, new System.Object[] {te});
		items.Add(copyItem);
		
		CreateRCMenu(items.ToArray());
	}
	private void RightClickCloseTabCallback(System.Object[] obj) {
		editorWindow.CloseTextEditor((UIDETextEditor)obj[0]);
	}
	
	public RCMenu CreateRCMenuForTextArea() {
		List<RCMenuItem> items = new List<RCMenuItem>();
		
		for (int i = 0; i < plugins.Count; i++) {
			RCMenuItem[] pluginItems = plugins[i].OnGatherRCMenuItems();
			for (int j = 0; j < pluginItems.Length; j++) {
				items.Add(pluginItems[j]);
			}
		}
		
		RCMenuItem copyItem = new RCMenuItem("Copy");
		copyItem.SetCallback(DoCopy);
		items.Add(copyItem);
		
		RCMenuItem pasteItem = new RCMenuItem("Paste");
		pasteItem.SetCallback(DoPaste);
		items.Add(pasteItem);
		
		RCMenuItem cutItem = new RCMenuItem("Cut");
		cutItem.SetCallback(DoCut);
		items.Add(cutItem);
		
		RCMenuItem selectAllItem = new RCMenuItem("Select All");
		selectAllItem.SetCallback(DoSelectAll);
		items.Add(selectAllItem);
		
		RCMenu menu = CreateRCMenu(items.ToArray());
		
		return menu;
	}
	
	public void TestThing(System.Object objs) {
		
	}
	public void CheckPluginUpdate() {
		if (wantsPluginUpdate) {
			UpdatePlugins();
			wantsPluginUpdate = false;
		}
	}
	
	public void Update() {
		disabledKeys.Clear();
		clickBlockers.Clear();
		
		desiredExtraLineCount = 0.0f;
		
		
		CheckPluginUpdate();
		
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPreTextEditorUpdate();
		}
		
		if (cursor != null) {
			float opacity = Mathf.PingPong((Time.realtimeSinceStartup-cursor.lastMoveTime)*2.0f,2);
			if (opacity >= 1.0f || Time.realtimeSinceStartup-cursor.lastMoveTime <= 1.0f) {
				opacity = 0.9f;
			}
			else {
				opacity = 0.25f;
			}
			if (cursor.opacity != opacity) {
				cursor.opacity = opacity;
				editorWindow.Repaint();
			}
		}
		
		if (cursor.posX != lastPos.x || cursor.posY != lastPos.y) {
			lastPos.x = cursor.posX;
			lastPos.y = cursor.posY;
			OnChangedCursorPosition();
		}
		
		if (cursor.selection.start != lastSelectionStart || cursor.selection.end != lastSelectionEnd) {
			OnSelectionChanged();
			lastSelectionStart = cursor.selection.start;
			lastSelectionEnd = cursor.selection.end;
		}
		
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnTextEditorUpdate();
		}
		
		if (textEditorToSwitchTo != null) {
			editorWindow.SwitchToTextEditor(textEditorToSwitchTo);
			textEditorToSwitchTo = null;
		}
		//isDoingDummyRedo -= 1;
	}
	
	
	
	
	public bool CheckKeyDown(KeyCode key) {
		if (disabledKeys.IndexOf(key) != -1) return false;
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == key) {
			return true;
		}
		return false;
	}
	
	private bool isFocusDummyUndoRedo = false;
	
	public void OnGUI() {
		//UpdateActualRenderedLines();
		GUI.color = new Color(1,1,1,1);
		CheckPluginUpdate();
		initialEventType = Event.current.type;
		windowMousePosWithTabBar = currentMousePos;
		windowMousePos = currentMousePos;
		windowMousePos.y -= desiredTabBarHeight;
		
		charSize = editorWindow.skin.GetStyle("TextEditorLabelNormal").CalcSize(new GUIContent("A"));
		
		tabRect = rect;
		tabRect.height = desiredTabBarHeight;
		tabRectZeroPos = tabRect;
		tabRectZeroPos.x = 0;
		tabRectZeroPos.y = 0;
		
		//textEditorRect = new Rect(0,tabRect.height,rect.width,rect.height-tabRect.height);
		textEditorRect = tabRect;
		textEditorRect.x = 0;
		textEditorRect.y = tabRect.height;
		textEditorRect.height = rect.height-tabRect.height;
		textEditorRectZeroPos = textEditorRect;
		textEditorRectZeroPos.x = 0; textEditorRectZeroPos.y = 0;
		
		textEditorNoScrollBarRect = textEditorRect;
		textEditorNoScrollBarRect.width -= GUI.skin.verticalScrollbar.fixedWidth;
		textEditorNoScrollBarRect.height -= GUI.skin.horizontalScrollbar.fixedHeight;
		textEditorNoScrollBarRectZeroPos = textEditorNoScrollBarRect;
		textEditorNoScrollBarRectZeroPos.x = 0; textEditorNoScrollBarRectZeroPos.y = 0;
		
		for (int i = 0; i < plugins.Count; i++) {
			plugins[i].OnPreTextEditorGUI();
			GUI.skin = editorWindow.theme.skin;
		}
		
		if (Application.platform == RuntimePlatform.OSXEditor) {
			if (Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown) {
				justDidPaste = false;
				justDidDuplicate = false;
			}
		}
		else {
			justDidPaste = false;
			justDidDuplicate = false;
			
		}
		
		if (Event.current.type == EventType.MouseUp) {
		
			Vector2 ts = cursor.selection.actualStart;
			Vector2 te = cursor.selection.actualEnd;
			cursor.selection.start = ts;
			cursor.selection.end = te;
			isDragSelecting = false;
			isDoubleClickDragSelecting = false;
		}
		
		//Debug.Log(editorWindow.canTextEditorInteract+" "+EditorWindow.focusedWindow.title);
		if (editorWindow.canTextEditorInteract) {
			
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Z &&
				(Event.current.control || Event.current.command) && Event.current.shift) {
				DoRedo();
				Event.current.Use();
			}
			
			if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed") {
				if (editorWindow.generalSettings.GetUseCtrlZUndo()) {
					//Debug.Log("Undoing");
					//Debug.Log("Going to redo");
					if (!isFocusDummyUndoRedo) {
						//Debug.Log("undo");
						//Undo.ClearUndo(undoManager);
						AddDummyUndo();
						//Undo.IncrementCurrentEventIndex();
						//isDoingDummyRedo = true;
						//Undo.PerformRedo();sd
						//Undo.PerformRedo();
						//Undo.PerformRedo();
						//isDoingDummyRedo = false;
						//Undo.IncrementCurrentEventIndex();
						DoUndo();
						//editorWindow.Repaint();
						//return;
					}
				}
				
				Event.current.Use();
			}
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Z &&
			(Event.current.control || Event.current.command) && Event.current.alt) {
				DoUndo();
				Event.current.Use();
			}
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S &&
			(Event.current.control || Event.current.command) && Event.current.alt) {
				DoSave();
				Event.current.Use();
			}
			
		}
		if (editorWindow.canTextEditorInteract && GUI.GetNameOfFocusedControl() == "UIDETextAreaDummy") {
			
			if(Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Paste" && !justDidPaste){
				DoPaste();
				justDidPaste = true;
				Event.current.Use();
			}
			
			if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Copy"){
				if (!isDragSelecting && !isDoubleClickDragSelecting) {
					DoCopy();
					Event.current.Use();
				}
			}
			if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Cut"){
				if (!isDragSelecting && !isDoubleClickDragSelecting) {
					DoCut();
					Event.current.Use();
				}
			}
			if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "SelectAll"){
				if (!isDragSelecting && !isDoubleClickDragSelecting) {
					DoSelectAll();
					Event.current.Use();
				}
			}
			
			//if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Delete"){
			//	DoBackspace();
			//}
			
			if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Duplicate" && !justDidDuplicate){
				DuplicateLine(doc.RealLineAt(cursor.posY));
				justDidDuplicate = true;
				editorWindow.Repaint();
			}
			
			Vector2 startCursorPos = cursor.GetVectorPosition();
			bool cursorChanged = false;
			
			if (CheckKeyDown(KeyCode.Delete)) {
				DoDelete();
			}
			if ((Event.current.control || Event.current.command) && CheckKeyDown(KeyCode.D)) {
				if (Event.current.shift) {
					DeleteLine(doc.RealLineAt(cursor.posY));
					Event.current.Use();
				}
			}
			
			if ((Event.current.control || Event.current.command) && CheckKeyDown(KeyCode.Slash)) {
				ToggleCommentLines();
				Event.current.Use();
			}
			
			bool movedUpOrDown = false;
			if (CheckKeyDown(KeyCode.LeftArrow)) {
				int newPos = cursor.posX-1;
				cursor.posX = newPos;
				
				if (newPos < 0) {
					if (cursor.posY > 0) {
						cursor.posY -= 1;
						cursor.posX = doc.LineAt(cursor.posY).rawText.Length;
						movedUpOrDown = true;
					}
				}
				else {
					if (Event.current.control || Event.current.command) {
						UIDELine line = doc.LineAt(cursor.posY);
						UIDEElement element = line.GetElementAt(cursor.posX);
						if (element != null) {
							int elementPos = line.GetElementStartPos(element);
							cursor.posX = elementPos;
						}
					}
				}
				ScrollToColumn(cursor.posY,cursor.posX);
				cursorChanged = true;
			}
			if (CheckKeyDown(KeyCode.RightArrow)) {
				int newPos = cursor.posX+1;
				cursor.posX = newPos;
				if (newPos > doc.LineAt(cursor.posY).rawText.Length) {
					if (cursor.posY < doc.lineCount) {
						cursor.posY += 1;
						cursor.posX = 0;
						movedUpOrDown = true;
					}
				}
				else {
					if (Event.current.control || Event.current.command) {
						UIDELine line = doc.LineAt(cursor.posY);
						UIDEElement element = line.GetElementAt(cursor.posX-1);
						if (element != null) {
							int elementPos = line.GetElementStartPos(element);
							cursor.posX = elementPos+element.rawText.Length;
						}
					}
				}
				ScrollToColumn(cursor.posY,cursor.posX);
				cursorChanged = true;
			}
			if (CheckKeyDown(KeyCode.UpArrow)) {
				int lineCount = 1;
				if (Event.current.control || Event.current.command) {
					lineCount = 4;
				}
				cursor.posY -= lineCount;
				int desiredX = doc.LineAt(cursor.posY).GetPositionFromScreenPosition(cursor.desiredPosX);
				cursor.posXNotDesired = (int)Mathf.Min(desiredX,doc.LineAt(cursor.posY).rawText.Length);
				ScrollToLine(cursor.posY);
				cursorChanged = true;
				movedUpOrDown = true;
			}
			if (CheckKeyDown(KeyCode.DownArrow)) {
				int lineCount = 1;
				if (Event.current.control || Event.current.command) {
					lineCount = 4;
				}
				cursor.posY += lineCount;
				int desiredX = doc.LineAt(cursor.posY).GetPositionFromScreenPosition(cursor.desiredPosX);
				cursor.posXNotDesired = (int)Mathf.Min(desiredX,doc.LineAt(cursor.posY).rawText.Length);
				ScrollToLine(cursor.posY);
				cursorChanged = true;
				movedUpOrDown = true;
			}
			
			if (CheckKeyDown(KeyCode.Home) || (Event.current.alt && CheckKeyDown(KeyCode.LeftArrow))) {
				cursor.posX = 0;
				cursorChanged = true;
				ScrollToLine(cursor.posY);
			}
			if (CheckKeyDown(KeyCode.End) || (Event.current.alt && CheckKeyDown(KeyCode.RightArrow))) {
				UIDELine line = doc.RealLineAt(cursor.posY);
				cursor.posX = line.rawText.Length;
				cursorChanged = true;
				ScrollToLine(cursor.posY);
			}
			
			if ((Event.current.control || Event.current.command) && CheckKeyDown(KeyCode.Home)) {
				cursor.posX = 0;
				cursor.posY = 0;
				cursorChanged = true;
				ScrollToLine(cursor.posY,3);
				movedUpOrDown = true;
			}
			if ((Event.current.control || Event.current.command) && CheckKeyDown(KeyCode.End)) {
				cursor.posY = doc.lineCount-1;
				UIDELine line = doc.RealLineAt(cursor.posY);
				cursor.posX = line.rawText.Length;
				cursorChanged = true;
				ScrollToLine(cursor.posY,3);
				movedUpOrDown = true;
			}
			
			
			if (cursorChanged) {
				editorWindow.Repaint();
				if (Event.current.shift) {
					if (!cursor.selection.hasSelection) {
						cursor.selection.start = startCursorPos;
					}
					cursor.selection.end = cursor.GetVectorPosition();
				}
				else {
					cursor.selection.end = cursor.GetVectorPosition();
					cursor.selection.start = cursor.selection.end;
				}
				Vector2 vPos = cursor.GetVectorPosition();
				for (int i = 0; i < plugins.Count; i++) {
					plugins[i].OnArrowKeyMoveCursor(vPos);
				}
				if (movedUpOrDown) {
					for (int i = 0; i < plugins.Count; i++) {
						plugins[i].OnArrowKeyMoveCursorLine(vPos);
					}
				}
			}
			if (CheckKeyDown(KeyCode.Backspace)) {
				DoBackspace();
			}

		}
		
		GUI.BeginGroup(textEditorRect);
		Rect textEditorInternalRect = textEditorRect;
		textEditorInternalRect.y = 0;
		DrawTextEditor(textEditorInternalRect);
		GUI.EndGroup();
		
		Rect tabExpandedRect = tabRectZeroPos;
		tabExpandedRect.x -= 1;
		tabExpandedRect.y -= 1;
		tabExpandedRect.width += 2;
		tabExpandedRect.height += 2;
		GUI.BeginGroup(tabExpandedRect);
		DrawTabBar(tabRectZeroPos);
		GUI.EndGroup();
		
		for (int i = 0; i < plugins.Count; i++) {
			if (plugins[i].useCustomWindow) {
				plugins[i].OnTextEditorGUI((i+1)*20);
			}
			else {
				GUI.color = new Color(0,0,0,0);
				//GUIStyle winStyle = GUIStyle.none;
				//winStyle.clipping = TextClipping.Overflow;
				GUI.Window((i+1)*20, new Rect(0,0,Screen.width,Screen.height), plugins[i].OnTextEditorGUI, "", new GUIStyle());
				GUI.color = new Color(1,1,1,1);
			}
			GUI.skin = editorWindow.theme.skin;
		}
		
		
		//Catch typing input
		if (!Event.current.command && !Event.current.control && editorWindow.canTextEditorInteract && enableTextEnter) {
			
			GUI.SetNextControlName("UIDETextAreaDummy");
			GUIUtility.GetControlID(FocusType.Keyboard);
			
			bool mouseDown = Event.current.type == EventType.MouseDown;
			mouseDown |= Event.current.type == EventType.MouseUp;
			if (mouseDown && textEditorNoScrollBarRectZeroPos.Contains(windowMousePos) && TestClickBlockers(windowMousePos)) {
				GUI.FocusWindow(-1);
				GUI.FocusControl("UIDETextAreaDummy");
			}
			
			if (Event.current.type == EventType.KeyDown) {
				if (Event.current.character != 0 && Event.current.character != 27) {
					if (Event.current.character == '\t') {
						OnTabEntered();
					}
					else {
						textAreaText = Event.current.character.ToString();
						EnterText(textAreaText);
						textAreaText = "";
					}
				}
				Event.current.Use();
				GUI.FocusWindow(-1);
				GUI.FocusControl("UIDETextAreaDummy");
			}
			
			
		}
	}
	
	public void DrawTabBar(Rect posRect) {
		GUIStyle shadowStyle = editorWindow.skin.GetStyle("DropShadow");
		GUIStyle bgStyle = editorWindow.skin.GetStyle("BoxBG");
		GUIStyle tabStyle = editorWindow.skin.GetStyle("Tab");
		GUIStyle tabSelectedStyle = editorWindow.skin.GetStyle("TabSelected");
		GUIStyle tabRowBGStyle = editorWindow.skin.GetStyle("TabRowBG");
		
		UIDEGUI.ColorBox(posRect,new Color(0.15f,0.15f,0.15f,1.0f));
		GUI.Box(posRect,"",bgStyle);
		
		List<List<UIDETextEditor>> rows = new List<List<UIDETextEditor>>();
		rows.Add(new List<UIDETextEditor>());
		
		float tabHeight = tabStyle.CalcHeight(new GUIContent("#YOLO"),100);
		
		float xPos = 0;
		for (int i = 0; i < editorWindow.textEditors.Count; i++) {
			UIDETextEditor te = editorWindow.textEditors[i];
			
			GUIContent content = new GUIContent(te.tabName);
			Vector2 contentSize = tabStyle.CalcSize(content);
			
			xPos += contentSize.x;
			if (xPos > posRect.width) {
				xPos = 0;
				rows.Add(new List<UIDETextEditor>());
				xPos += contentSize.x;
			}
			rows[rows.Count-1].Add(te);
		}
		
		//GUI.color = new Color(1,1,1,0.1f);
		for (int i = 0; i < rows.Count; i++) {
			Rect r = posRect;
			r.height = tabHeight;
			r.y = i*tabHeight;
			GUI.Box(r,"",tabRowBGStyle);
		}
		//GUI.color = Color.white;
		
		//Draw shadows
		GUILayout.BeginArea(posRect);
		for (int i = 0; i < rows.Count; i++) {
			float yPos = i*tabHeight;
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			for (int j = 0; j < rows[i].Count; j++) {
				UIDETextEditor te = rows[i][j];
				GUIStyle style = tabStyle;
				if (te == this) {
					style = tabSelectedStyle;
				}
				GUI.color = new Color(0,0,0,0);
				GUILayout.Box(te.tabName,style);
				if (te == editorWindow.textEditor) {
					GUI.color = Color.white;
				}
				Rect r = GUILayoutUtility.GetLastRect();
				r.y = yPos;
				
				GUI.Box(r,"",shadowStyle);
				GUI.color = Color.white;
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
		GUILayout.EndArea();
		
		//Draw Tabs
		GUILayout.BeginArea(posRect);
		for (int i = 0; i < rows.Count; i++) {
			float yPos = i*tabHeight;
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			for (int j = 0; j < rows[i].Count; j++) {
				UIDETextEditor te = rows[i][j];
				
				GUIStyle style = tabStyle;
				if (te == this) {
					style = tabSelectedStyle;
				}
				GUI.color = new Color(0,0,0,0);
				GUILayout.Box(te.tabName,style);
				GUI.color = Color.white;
				Rect r = GUILayoutUtility.GetLastRect();
				r.y = yPos;
				
				if (GUI.Button(r,te.tabName,style)) {
					
					if (Event.current.button == 1 || (Event.current.control && Event.current.button == 0)) {
						OnRightClickTab(te);
					}
					else if (Event.current.button == 0) {
						textEditorToSwitchTo = te;
					}
					else if (Event.current.button == 2) {
						editorWindow.CloseTextEditor(te);
					}
				}
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
		GUILayout.EndArea();
		desiredTabBarHeight = rows.Count*tabHeight;
	}
	
	public void DrawTextEditor(Rect posRect) {
		GUIStyle leftShadowStyle = editorWindow.skin.GetStyle("ShadowVerticalFromLeft");
		GUIStyle topShadowStyle = editorWindow.skin.GetStyle("ShadowHorizontalFromTop");
		GUIStyle textNormalStyle = editorWindow.skin.GetStyle("TextEditorLabelNormal");
		GUIStyle textBoldStyle = editorWindow.skin.GetStyle("TextEditorLabelBold");
		GUIStyle boxBGStyle = editorWindow.skin.GetStyle("BoxBG");
		GUIStyle textSelectionStyle = editorWindow.skin.GetStyle("TextSelection");
		GUIStyle textSelectionTextStyle = editorWindow.skin.GetStyle("TextSelectionText");
		GUIStyle hoverHighlightStyle = editorWindow.skin.GetStyle("CursorHoverHighlight");
		textElementBackgroundStyle = editorWindow.skin.GetStyle("TextElementBackground");
		
		float actualExtraLineCount = Mathf.Max(minExtraLineCount,desiredExtraLineCount);
		
		Rect viewRect = new Rect(0,0,10000,((float)renderedLineCount+actualExtraLineCount)*charSize.y);
		
		int startingLine = Mathf.Max(Mathf.FloorToInt(doc.scroll.y/charSize.y),0);
		int endLine = startingLine+(Mathf.FloorToInt(posRect.height/charSize.y)+1);
		endLine = (int)Mathf.Min(endLine,renderedLineCount);
		
		editorWindow.theme.OnPreTextEditorGUI(textEditorNoScrollBarRectZeroPos);
		
		bool useCodeFilding = editorWindow.generalSettings.GetUseCodeFolding();
		
		UIDELine lastLine = GetLine((int)Mathf.Min(endLine,renderedLineCount-1));
		if (lastLine == null) return;
		
		float maxLineNumberLabelWidth = (lastLine.index+1).ToString().Length*charSize.x;
		lineNumberWidth = maxLineNumberLabelWidth+4;
		Rect lineColumnRect = posRect;
		lineColumnRect.width = lineNumberWidth;
		lineNumberWidth += 2;
		if (useCodeFilding) {
			lineNumberWidth += 16;
		}
		
		Rect lineColumnBottomCoverRect = lineColumnRect;
		lineColumnBottomCoverRect.height = GUI.skin.horizontalScrollbar.fixedHeight;
		lineColumnBottomCoverRect.y = posRect.height-lineColumnBottomCoverRect.height;
		
		posRect.x += lineNumberWidth;
		posRect.width -= lineNumberWidth;
		
		bool hasHorizontalSlider = true;
		bool hasVerticalSlider = viewRect.height > posRect.height;
		
		hasVerticalSlider = viewRect.height > posRect.height-GUI.skin.horizontalScrollbar.fixedHeight;
		if (hasVerticalSlider) {
			viewRect.width = Mathf.Max(desiredWidth+200.0f,posRect.width-GUI.skin.verticalScrollbar.fixedWidth);
		}
		else {
			viewRect.width = Mathf.Max(desiredWidth+200.0f,posRect.width);
		}
		
		if (hasHorizontalSlider) {
			lineColumnRect.height -= lineColumnBottomCoverRect.height;
		}
		
		Vector2 mouseCursorPos = ScreenSpaceToCursorSpace(currentMousePos);
		Vector2 mouseCursorPosMidOffset = ScreenSpaceToCursorSpace(currentMousePos+new Vector2((charSize.x*0.5f)-1,0));
		
		if (RCMenu.currentMenu == null && editorWindow.canTextEditorInteract && TestClickBlockers(windowMousePos)) {
			Rect cursorChangeRect = posRect;
			cursorChangeRect.width -= GUI.skin.verticalScrollbar.fixedWidth;
			cursorChangeRect.height -= GUI.skin.horizontalScrollbar.fixedHeight;
			EditorGUIUtility.AddCursorRect(cursorChangeRect,MouseCursor.Text);
		}
		
		Rect lineNumberGroupRect = lineColumnRect;
		lineNumberGroupRect.width = lineNumberWidth+posRect.width-GUI.skin.verticalScrollbar.fixedWidth;
		
		//LineNumber
		GUI.BeginGroup(lineNumberGroupRect);
		for (int i = startingLine; i < endLine; i++) {
			UIDELine line = doc.LineAt(i);
			if (line == null) continue;
			string lineNumberString = (line.index+1).ToString();
			float lineNumberLabelWidth = lineNumberString.Length*charSize.x;
			Rect lineNumberRect = new Rect(maxLineNumberLabelWidth-lineNumberLabelWidth,(i*charSize.y)-doc.scroll.y,lineNumberLabelWidth,charSize.y);
			GUI.Label(lineNumberRect,lineNumberString,textBoldStyle);
			
			if (useCodeFilding) {
				if (line.isFoldable) {
					UIDEElement firstNonWhitespaceElement = line.GetFirstNonWhitespaceElement(false);
					int firstNonWhitespace = line.GetElementStartPos(firstNonWhitespaceElement);
					firstNonWhitespace = line.GetScreenPosition(firstNonWhitespace);
					
					float pixelPos = firstNonWhitespace*charSize.x;
					pixelPos += lineNumberWidth;
					Rect foldRect = new Rect(pixelPos-16,lineNumberRect.y,12,16);
					foldRect.y += charSize.y*0.5f;
					foldRect.y -= 8.0f;
					bool newFoldValue = GUI.Toggle(foldRect,!line.isFolded,"",EditorStyles.foldout);
					if (newFoldValue != !line.isFolded) {
						line.isFolded = !newFoldValue;
						UpdateActualRenderedLines();
					}
				}
			}
		}
		GUI.EndGroup();
		
		desiredWidth = 0.0f;
		//Begin scroll view
		float preScrollY = doc.scroll.y;
		doc.scroll = GUI.BeginScrollView(posRect,doc.scroll,viewRect,true,false);
		if (preScrollY != doc.scroll.y) {
			if (viewRect.height > posRect.height-GUI.skin.verticalScrollbar.fixedWidth) {
				doc.scroll.y = Mathf.Max(doc.scroll.y,0.0f);
				float v = posRect.height;
				if (hasHorizontalSlider) {
					v -= GUI.skin.horizontalScrollbar.fixedHeight;
				}
				doc.scroll.y = Mathf.Min(doc.scroll.y,(viewRect.height-(v)));
			}
			else {
				doc.scroll.y = 0.0f;
			}
			editorWindow.Repaint();
		}
		bool didDoubleClickElement = false;
		
		Vector2 pos = new Vector2(0,0);
		
		for (int i = startingLine; i < endLine; i++) {
			UIDELine line = doc.LineAt(i);
			if (line == null) {
				continue;
			}
			
			string selectedLineText = "";
			Rect lineSelectionRect = new Rect(0,0,0,0);
			bool isEffectedBySelection = cursor.selection.hasSelection && cursor.selection.IsLineAffected(line);
			if (isEffectedBySelection) {
				
				Vector2 selectionRange = cursor.selection.GetLineSelectionRange(line);
				
				string rawText = cursor.selection.GetSelectedLineText(line);
				System.Text.StringBuilder actualTextSB = new System.Text.StringBuilder();
				for (int j = 0; j < rawText.Length; j++) {
					char currentChar = rawText[j];
					if (currentChar == '\t') {
						string tabStr = line.GetTabStringAtPos(((int)selectionRange.x)+j);
						actualTextSB.Append(tabStr);
					}
					else {
						actualTextSB.Append(currentChar);
					}
				}
				selectedLineText = actualTextSB.ToString();
				
				selectionRange.x = line.GetScreenPosition((int)selectionRange.x);
				selectionRange.y = line.GetScreenPosition((int)selectionRange.y);
				lineSelectionRect = posRect;
				lineSelectionRect.x = 0;
				lineSelectionRect.height = charSize.y;
				lineSelectionRect.y = i*charSize.y;
				lineSelectionRect.x = selectionRange.x*charSize.x;
				lineSelectionRect.width = (selectionRange.y-selectionRange.x)*charSize.x;
			}
			pos.x = 0.0f;
			
			bool skipLineRendering = false;
			foreach (Func<UIDELine,bool> func in onPreRenderLineCallbacks) {
				skipLineRendering |= func(line);
			}
			int charXPos = 0;
			for (int j = 0; j < line.elements.Count; j++) {
				if (skipLineRendering) {
					break;
				}
				UIDEElement element = line.elements[j];
				
				GUIStyle style = textNormalStyle;
				if (element.tokenDef.isBold) {
					style = textBoldStyle;
				}
				
				
				bool isHoverHighlight = selectionHighlightText != "";
				isHoverHighlight &= selectionHighlightElement != element;
				isHoverHighlight &= element.rawText == selectionHighlightText;
				
				Rect elementRect = new Rect(0,0,0,0);
				
				if (isHoverHighlight) {
					elementRect = RenderElement(element, new Vector2(pos.x,i*charSize.y),charXPos,hoverHighlightStyle,false);
				}
				else {
					elementRect = RenderElement(element, new Vector2(pos.x,i*charSize.y),charXPos,style,true);
				}
				desiredWidth = Mathf.Max(desiredWidth,elementRect.x+elementRect.width);
				element.highlighted = false;
				
				if (i == (int)mouseCursorPos.y) {
					if (elementRect.Contains(currentMousePos)) {
						element.highlighted = true;
						if (Event.current.type == EventType.MouseDown && (Event.current.button == 0)) {
							if (Event.current.clickCount == 3) {
								cursor.selection.start = new Vector2(0,cursor.posY);
								cursor.selection.end = new Vector2(element.rawText.Length,cursor.posY);
								didDoubleClickElement = true;
								editorWindow.Repaint();
							}
							else if (Event.current.clickCount == 2) {
								int actualXValuePos = (int)mouseCursorPos.x;
								Vector2 p = cursor.GetVectorPosition();
								p.x = actualXValuePos;
								Vector2 startBounds = ExpandToLogicalBoundStart(p);
								Vector2 endBounds = ExpandToLogicalBoundEnd(p);
								//Vector2 elementBounds = element.GetRangeInLine();
								doubleClickDragBounds = new Vector4(startBounds.x,startBounds.y,endBounds.x,endBounds.y);
								cursor.selection.start = startBounds;
								cursor.selection.end = endBounds;
								didDoubleClickElement = true;
								isDoubleClickDragSelecting = true;
								editorWindow.Repaint();
							}
						}
					}
				}
				charXPos += element.rawText.Length;
				pos.x += elementRect.width;
			}
			
			if (isEffectedBySelection) {
				GUI.color = textSelectionStyle.normal.textColor;
				GUI.Box(lineSelectionRect,"",textSelectionStyle);
				GUI.color = Color.white;
				GUI.Label(lineSelectionRect,selectedLineText,textSelectionTextStyle);
			}
			
			
			
			foreach (Action<UIDELine> func in onPostRenderLineCallbacks) {
				func(line);
			}
			
		}
		
		if (cursor != null) {
			if (!didDoubleClickElement) {
				
				if (enableClick && textEditorNoScrollBarRect.Contains(windowMousePosWithTabBar) && TestClickBlockers(windowMousePos)) {
					if (initialEventType == EventType.MouseDown && (Event.current.button == 0)) {
						Vector2 realCursorPos = RenderedCursorSpaceToRealCursorSpace(mouseCursorPosMidOffset);
						//Debug.Log(windowMousePos+" "+textEditorNoScrollBarRect+" "+textEditorNoScrollBarRect.Contains(windowMousePos));
						cursor.posY = (int)realCursorPos.y;
						cursor.posX = (int)realCursorPos.x;
						
						cursor.selection.start = new Vector2(cursor.posX,cursor.posY);
						cursor.selection.end = cursor.selection.start;
						//Debug.Log("ddd");
						//Debug.Log(realCursorPos);
						
						mouseDownTextPos = cursor.selection.start;
						isDragSelecting = true;
						editorWindow.Repaint();
						Vector2 vPos = cursor.GetVectorPosition();
						for (int i = 0; i < plugins.Count; i++) {
							plugins[i].OnClickMoveCursor(vPos);
						}
					}
					else if (initialEventType == EventType.MouseDown && (Event.current.button == 1)) {
						CreateRCMenuForTextArea();
					}
				}
				if (enableClick && (isDragSelecting || isDoubleClickDragSelecting) && TestClickBlockers(windowMousePos)) {
					Vector2 realCursorPos = RenderedCursorSpaceToRealCursorSpace(mouseCursorPosMidOffset);
					//&& textEditorNoScrollBarRect.Contains(windowMousePos)
					cursor.posY = (int)realCursorPos.y;
					cursor.posX = (int)realCursorPos.x;
					
					if (isDoubleClickDragSelecting) {
						int actualXValuePos = (int)mouseCursorPos.x;
						Vector2 p = cursor.GetVectorPosition();
						p.x = actualXValuePos;
						Vector2 startBounds = ExpandToLogicalBoundStart(p);
						Vector2 endBounds = ExpandToLogicalBoundEnd(p);
						
						Vector2 downStartBounds = new Vector2(doubleClickDragBounds.x,doubleClickDragBounds.y);
						Vector2 downEndBounds = new Vector2(doubleClickDragBounds.z,doubleClickDragBounds.w);
						if (doc.PositionLessThan(p,downStartBounds)) {
							cursor.selection.start = downEndBounds;
						}
						else {
							cursor.selection.start = downStartBounds;
						}
						if (doc.PositionLessThan(p,cursor.selection.start)) {
							cursor.posX = (int)startBounds.x;
						}
						else {
							cursor.posX = (int)endBounds.x;
						}
					}
					
					cursor.selection.end = new Vector2(cursor.posX,cursor.posY);
					
					//Debug.Log(realCursorPos);
					
					float yScaler = 0.0f;
					if (windowMousePos.y > textEditorNoScrollBarRectZeroPos.y+textEditorNoScrollBarRectZeroPos.height) {
						yScaler = windowMousePos.y-(textEditorNoScrollBarRectZeroPos.y+textEditorNoScrollBarRectZeroPos.height);
					}
					else if (windowMousePos.y < textEditorNoScrollBarRectZeroPos.y) {
						yScaler = -(windowMousePos.y-textEditorNoScrollBarRectZeroPos.y);
					}
					
					ScrollToLine(cursor.posY, 1, yScaler*0.025f, true);
					float xScaler = 0.0f;
					if (windowMousePos.x > textEditorNoScrollBarRectZeroPos.x+textEditorNoScrollBarRectZeroPos.width) {
						xScaler = windowMousePos.x-(textEditorNoScrollBarRectZeroPos.x+textEditorNoScrollBarRectZeroPos.width);
					}
					else if (windowMousePos.x < textEditorNoScrollBarRectZeroPos.x) {
						xScaler = -(windowMousePos.x-textEditorNoScrollBarRectZeroPos.x);
					}
					ScrollToColumn(cursor.posY, cursor.posX, 1, xScaler*0.1f, true);
					editorWindow.Repaint();
					
					Vector2 vPos = cursor.GetVectorPosition();
					for (int i = 0; i < plugins.Count; i++) {
						plugins[i].OnDragMoveCursor(vPos);
					}
				}
			}
			
			if (!cursor.selection.hasSelection) {
				UIDELine cursorLine = doc.RealLineAt(cursor.posY);
				if (cursorLine != null) {
					Rect cursorRect = new Rect(cursorLine.GetScreenPosition(cursor.posX),GetLinesRenderedPosition(cursor.posY),2,charSize.y);
					cursorRect.x *= charSize.x;
					cursorRect.x -= 1;
					cursorRect.y *= charSize.y;
					
					GUI.color = new Color(1,1,1,cursor.opacity);
					GUI.DrawTexture(cursorRect,editorWindow.whiteTex);
					GUI.color = new Color(1,1,1,1);
				}
			}
		}
		
		
		
		GUI.EndScrollView();
		
		editorWindow.theme.OnPostTextEditorGUI(textEditorNoScrollBarRectZeroPos);
		
		UIDEGUI.ColorBox(lineColumnRect,new Color(0.0f,0.0f,0.0f,0.25f));
		GUI.Box(lineColumnRect,"",boxBGStyle);
		
		Rect lineColumnShadowRect = lineColumnRect;
		lineColumnShadowRect.x += lineColumnShadowRect.width;
		lineColumnShadowRect.width = 0;
		GUI.Box(lineColumnShadowRect,"",leftShadowStyle);
		
		Rect topShadowRect = posRect;
		topShadowRect.x -= lineNumberWidth;
		topShadowRect.width += lineNumberWidth;
		topShadowRect.height = 0;
		GUI.Box(topShadowRect,"",topShadowStyle);
		
		if (hasHorizontalSlider) {
			UIDEGUI.ColorBox(lineColumnBottomCoverRect,new Color(0.0f,0.0f,0.0f,0.25f));
			GUI.Box(lineColumnBottomCoverRect,"",boxBGStyle);
			if (hasVerticalSlider) {
				Rect otherSideRect = lineColumnBottomCoverRect;
				otherSideRect.width = GUI.skin.verticalScrollbar.fixedWidth;
				otherSideRect.x = (posRect.width+lineNumberWidth)-otherSideRect.width;
				UIDEGUI.ColorBox(otherSideRect,new Color(0.0f,0.0f,0.0f,0.25f));
				GUI.Box(otherSideRect,"",boxBGStyle);
			}
		}
		
		actualTextAreaRect = posRect;
		actualTextAreaRect.y += tabRect.height;
		actualTextAreaNoScrollBarRect = actualTextAreaRect;
		actualTextAreaNoScrollBarRect.width -= GUI.skin.verticalScrollbar.fixedWidth;
		actualTextAreaNoScrollBarRect.height -= GUI.skin.horizontalScrollbar.fixedHeight;
	}
	
	public Rect RenderElement(UIDEElement element, Vector2 pos, int startCharPos, GUIStyle style, bool useColorTint) {
		
		Color c = Color.white;
		Color bc = new Color(0,0,0,0);
		
		if (useColorTint) {
			c = element.tokenDef.color;
			bc = element.tokenDef.backgroundColor;
			
			if (element.highlighted) {
				c *= element.tokenDef.mouseOverMultiply;
				bc *= element.tokenDef.mouseOverMultiply;
			}
		}
		
		string actualText = element.GetSpacedTabString(startCharPos);
		
		Vector2 contentSize = new Vector2(actualText.Length*charSize.x,charSize.y);
		
		Rect boxRect = new Rect(pos.x,pos.y,contentSize.x,contentSize.y);
		Rect textRect = boxRect;
		textRect.x += textXOffset.x;
		textRect.y += textXOffset.y;
		
		if (bc.a > 0.0f) {
			GUI.color = bc;
			GUI.Box(boxRect,"",textElementBackgroundStyle);
		}

		if (c.a > 0.0f) {
			GUI.color = c;
			GUI.Label(textRect,actualText,style);
		}
		GUI.color = new Color(1,1,1,1);
		
		return boxRect;
	}
	
	public Vector2 ExpandToLogicalBoundStart(Vector2 pos) {
		UIDELine line = doc.RealLineAt((int)pos.y);
		if (line == null) return pos;
		UIDEElement element = line.GetElementAt((int)pos.x);
		if (element == null) return pos;
		if (pos.x >= line.rawText.Length) return pos;
		Vector2 outPos = new Vector2(line.GetElementStartPos(element),pos.y);
		int finalIndex = (int)pos.x;
		
		char firstChar = line.rawText[finalIndex];
		bool isWord = char.IsLetterOrDigit(firstChar) || firstChar == '_';
		bool isWhitespace = char.IsWhiteSpace(firstChar);
		
		for (int i = (int)pos.x; i >= outPos.x; i--) {
			char c = line.rawText[i];
			if (isWhitespace) {
				if (!char.IsWhiteSpace(c)) {
					break;
				}
			}
			else if (isWord) {
				if (!char.IsLetterOrDigit(c) && c != '_') {
					break;
				}
			}
			else {
				if (char.IsLetterOrDigit(c) || c == '_' || char.IsWhiteSpace(c)) {
					break;
				}
			}
			finalIndex = i;
		}
		outPos.x = finalIndex;
		return outPos;
	}
	public Vector2 ExpandToLogicalBoundEnd(Vector2 pos) {
		UIDELine line = doc.RealLineAt((int)pos.y);
		if (line == null) return pos;
		UIDEElement element = line.GetElementAt((int)pos.x);
		if (element == null) return pos;
		if (pos.x >= line.rawText.Length) return pos;
		Vector2 outPos = new Vector2(line.GetElementStartPos(element)+element.rawText.Length,pos.y);
		int finalIndex = (int)pos.x;
		
		char firstChar = line.rawText[finalIndex];
		bool isWord = char.IsLetterOrDigit(firstChar) || firstChar == '_';
		bool isWhitespace = char.IsWhiteSpace(firstChar);
		
		for (int i = (int)pos.x; i <= outPos.x; i++) {
			finalIndex = i;
			if (i >= line.rawText.Length) {
				break;
			}
			char c = line.rawText[i];
			if (isWhitespace) {
				if (!char.IsWhiteSpace(c)) {
					break;
				}
			}
			else if (isWord) {
				if (!char.IsLetterOrDigit(c) && c != '_') {
					break;
				}
			}
			else {
				if (char.IsLetterOrDigit(c) || c == '_' || char.IsWhiteSpace(c)) {
					break;
				}
			}
		}
		outPos.x = finalIndex;
		return outPos;
	}
	
	public void ScrollToLine(int index) {
		ScrollToLine(index, 0, 1.0f, false);
	}
	public void ScrollToLine(int index,int bufferLines) {
		ScrollToLine(index, bufferLines, 1.0f, false);
	}
	public void ScrollToLine(int index, int bufferLines, float scrollSpeedMod, bool softScroll) {
		//float newScrollPosition = lineNumber;
		//newScrollPosition *= charSize.y;
		//if ()
		
		if (doc.lineCount == 0) return;
		index = Mathf.Clamp(index,0,doc.lineCount);
		float itemHeight = charSize.y;
		float desiredY = index*itemHeight;
		float newY = doc.scroll.y;
		
		if (desiredY < doc.scroll.y+(itemHeight*bufferLines)) {
			if (!softScroll) {
				newY = (index*itemHeight)-(itemHeight*bufferLines);
			}
			else {
				newY -= (itemHeight*bufferLines)*scrollSpeedMod;
			}
		}
		else if (desiredY > ((doc.scroll.y+actualTextAreaNoScrollBarRect.height)-itemHeight)-(itemHeight*(bufferLines+1))) {
			if (!softScroll) {
				newY = ((index*itemHeight)-actualTextAreaNoScrollBarRect.height)+itemHeight*(bufferLines+1);
			}
			else {
				newY += (itemHeight*bufferLines)*scrollSpeedMod;
			}
		}
		
		if (newY != doc.scroll.y) {
			editorWindow.Repaint();
		}
		doc.scroll.y = newY;
	}
	
	public void ScrollToColumn(int lineNumber, int index) {
		ScrollToColumn(lineNumber,index, 0, 1.0f, false);
	}
	public void ScrollToColumn(int lineNumber, int index,int bufferLines) {
		ScrollToColumn(lineNumber, index, bufferLines, 1.0f, false);
	}
	public void ScrollToColumn(int lineNumber, int index, int bufferLines, float scrollSpeedMod, bool softScroll) {
		if (doc.lineCount == 0) return;
		
		lineNumber = Mathf.Clamp(lineNumber,0,doc.lineCount);
		UIDELine line = doc.LineAt(lineNumber);
		
		index = Mathf.Max(index,0);
		float itemWidth = charSize.x;
		float desiredX = line.GetScreenPosition(index)*itemWidth;
		float newX = doc.scroll.x;
		//Debug.Log(desiredX+" "+((doc.scroll.x+actualTextAreaNoScrollBarRect.width)-itemWidth));
		if (desiredX < (doc.scroll.x+itemWidth)+(itemWidth*bufferLines)) {
			if (!softScroll) {
				newX = index*itemWidth-(itemWidth*bufferLines);
			}
			else {
				newX -= (itemWidth*bufferLines)*scrollSpeedMod;
			}
		}
		else if (desiredX > ((doc.scroll.x+actualTextAreaNoScrollBarRect.width)-itemWidth)-(itemWidth*bufferLines)) {
			//float fat = line.GetScreenPosition(index)*itemWidth;
			if (!softScroll) {
				newX = (desiredX-actualTextAreaNoScrollBarRect.width)+itemWidth*bufferLines;
			}
			else {
				newX += (itemWidth*bufferLines)*scrollSpeedMod;
			}
		}
		
		if (newX != doc.scroll.x) {
			editorWindow.Repaint();
		}
		doc.scroll.x = newX;
	}
	
	public Vector2 RenderedCursorSpaceToRealCursorSpace(Vector2 pos) {
		UIDELine line = doc.LineAt((int)pos.y);
		if (line == null) return pos;
		//Debug.Log(pos.y+" "+line.index);
		return new Vector2(pos.x,line.index);
	}
	public Vector2 ScreenSpaceToCursorSpace(Vector2 pos) {
		pos = ToActiveTextAreaPosition(pos);
		float lineHeight = charSize.y;
		float charWidth = charSize.x;
		
		int mouseOverCurrentRow = Mathf.FloorToInt((pos.y+doc.scroll.y)/lineHeight);
		int mouseOverCurrentColumn = Mathf.RoundToInt((pos.x+doc.scroll.x)/charWidth);
		mouseOverCurrentRow = Mathf.Clamp(mouseOverCurrentRow,0,renderedLineCount-1);
		mouseOverCurrentColumn = Mathf.Max(mouseOverCurrentColumn,0);
		
		UIDELine line = doc.LineAt(mouseOverCurrentRow);
		mouseOverCurrentColumn = line.GetPositionFromScreenPosition(mouseOverCurrentColumn);
		
		return new Vector2(mouseOverCurrentColumn,mouseOverCurrentRow);
	}
	
	public Vector2 ToActiveTextAreaPosition(Vector2 v) {
		v.x -= lineNumberWidth;
		//if (editorWindow.generalSettings.GetUseCodeFolding()) {
		//	v.x += 16;
		//}
		return v;
	}
	
	public bool ClickTestClickBlockers(Vector2 pos) {
		if (Event.current.type != EventType.MouseDown) return false;
		return TestClickBlockers(pos);
	}
	
	public bool TestClickBlockers(Vector2 pos) {
		bool hit = false;
		
		for (int i = 0; i < clickBlockers.Count; i++) {
			Rect r = clickBlockers[i].rect;
			if (r.Contains(pos)) {
				hit = true;
				break;
			}
		}
		return !hit;
	}
	
	public bool ClickTestClickBlockers(Vector2 pos, UIDEPlugin owner) {
		if (Event.current.type != EventType.MouseDown) return false;
		return TestClickBlockers(pos, owner);
	}
	
	public bool TestClickBlockers(Vector2 pos, UIDEPlugin owner) {
		bool hit = false;
		
		for (int i = 0; i < clickBlockers.Count; i++) {
			if (clickBlockers[i].owner != owner) continue;
			Rect r = clickBlockers[i].rect;
			if (r.Contains(pos)) {
				hit = true;
				break;
			}
		}
		return !hit;
	}
	
	public Rect GetTextEditorRect() {
		return GetTextEditorRect(false);
	}
	public Rect GetTextEditorRect(bool excludeLineNumbers) {
		Rect r = textEditorNoScrollBarRect;
		r.x += rect.x;
		r.y += rect.y;
		if (!excludeLineNumbers) {
			r.x += lineNumberWidth;
			r.width -= lineNumberWidth;
		}
		return r;
	}
	
	public UIDECursorPos GetLineSpacePos(Vector2 pos, GUIStyle style) {
		return GetLineSpacePos(new UIDECursorPos((int)pos.x,(int)pos.y),style);
	}
	public UIDECursorPos GetLineSpacePos(UIDECursorPos pos,GUIStyle style) {
		UIDECursorPos charPos = GetScreenSpacePos(pos,style);
		UIDELine line = doc.LineAt(charPos.y);
		charPos.x = line.GetPositionFromScreenPosition(charPos.x);
		return charPos;
	}
	
	public UIDECursorPos GetScreenSpacePos(Vector2 pos, GUIStyle style) {
		return GetScreenSpacePos(new UIDECursorPos((int)pos.x,(int)pos.y),style);
	}
	public UIDECursorPos GetScreenSpacePos(UIDECursorPos pos, GUIStyle style) {
		Vector2 charSize = style.CalcSize(new GUIContent("A"));
		//float linePadding = 0.0f;
		float lineHeight = charSize.y;
		float charWidth = charSize.x;
		
		int mouseOverCurrentRow = Mathf.FloorToInt((pos.y+doc.scroll.y)/lineHeight);
		int mouseOverCurrentColumn = Mathf.RoundToInt((pos.x+doc.scroll.x)/charWidth);
		mouseOverCurrentRow = Mathf.Clamp(mouseOverCurrentRow,0,doc.lineCount-1);
		mouseOverCurrentColumn = Mathf.Max(mouseOverCurrentColumn,0);
		return new UIDECursorPos(mouseOverCurrentColumn,mouseOverCurrentRow);
	}
	
}

