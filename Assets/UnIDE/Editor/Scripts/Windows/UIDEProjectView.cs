using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UIDE;
using UIDE.SettingsMenu;

public enum UIDEProjectItemType {ScriptCS,ScriptJS,ScriptBoo,Shader};

[System.Serializable]
public class UIDEProjectItem:System.Object {
	public List<UIDEProjectItem> childern = new List<UIDEProjectItem>();
	public bool isFolder = false;
	public bool isStub = false;
	public bool expanded = false;
	public string path;
	public string name;
	public string nameNoExt;
	public string extension;
	public UIDEProjectItemType type;
	
	[SerializeField]
	private UIDEProjectItem _parent;
	public UIDEProjectItem parent {
		get {
			return _parent;
		}
		set {
			if (_parent != null) {
				_parent.childern.Remove(this);
			}
			_parent = value;
			if (_parent != null) {
				_parent.childern.Remove(this);
				_parent.childern.Add(this);
			}
		}
	}
	
	public UIDEProjectItem(string path, bool isFolder) {
		path = path.Replace("\\","/").Replace("//","/");
		this.isFolder = isFolder;
		this.path = path;
		UpdateInfo();
	}
	
	public UIDEProjectItem FindChild(string childName,bool folder) {
		for (int i = 0; i < childern.Count; i++) {
			if (childern[i].isFolder != folder) continue;
			if (childern[i].name == childName) {
				return childern[i];
			}
		}
		return null;
	}
	public void UpdateInfo() {
		if (!isFolder) {
			name = Path.GetFileName(path);
			nameNoExt = Path.GetFileNameWithoutExtension(path);
			extension = Path.GetExtension(path).ToLower();
			if (extension == "cs") {
				type = UIDEProjectItemType.ScriptCS;
			}
			else if (extension == "js") {
				type = UIDEProjectItemType.ScriptJS;
			}
			else if (extension == "boo") {
				type = UIDEProjectItemType.ScriptBoo;
			}
			else if (extension == "shader") {
				type = UIDEProjectItemType.Shader;
			}
		}
		else {
			name = path;
			if (path[path.Length-1] == '/') {
				path = path.Remove(path.Length-1);
			}
			int index = path.LastIndexOf("/");
			if (index != -1) {
				name = path.Substring(index + 1);
			}
			else {
				name = path.Replace("/","");
			}
			nameNoExt = name;
		}
	}
	
}

public class UIDEProjectView:UIDEWindow {
	public UIDEProjectItem assetsFolder;
	public UIDEProjectItem selectedItem;
	public Vector2 projectScrollPos;
	public Vector2 projectDesiredSize;
	public Vector2 projectMiddleMouseDownPos;
	public bool projectMiddleMouseDragging;
	public Vector2 mousePosition;
	public float sepSliderValue = 200.0f;
	
	
	public bool wantsFileSearchUpdate = false;
	public bool useFileSearchTerm = false;
	public string fileSearchTerm = "";
	public bool fileSearchFildIsFocused = false;
	[SerializeField]
	private List<UIDEProjectItem> searchedVisibleFileList = new List<UIDEProjectItem>();
	
	[SerializeField]
	private List<UIDEProjectItem> _visibleFileList = new List<UIDEProjectItem>();
	public List<UIDEProjectItem> visibleFileList {
		get {
			return _visibleFileList;
		}
		set {
			_visibleFileList = value;
		}
	}
	
	public bool isCollapsed {
		get {
			return editorWindow.generalSettings.GetCollapseProjectPanel();
		}
		set {
			editorWindow.generalSettings.SetCollapseProjectPanel(value);
		}
	}
	
	
	public string baseDir = "Assets";
	
	public Vector2 fileScrollPos;
	
	public Rect folderRect;
	public Rect fileRect;
	public Rect actualRect {
		get {
			if (isCollapsed) {
				Rect r = rect;
				r.width = 30;
				return r;
			}
			return rect;
		}
	}
	
	private bool folderColorToggle = false;
	
	public void Start() {
		UpdateDB();
	}
	
	public void OnEnable() {
		UpdateDB();
	}
	
	public void UpdateDB() {
		string currentSelectionPath = "";
		if (selectedItem != null) {
			currentSelectionPath = selectedItem.path;
		}
		selectedItem = null;
		
		UIDEProjectItem oldAssetsFolder = assetsFolder;
		assetsFolder = CreateItem(baseDir+"/",true);
		if (oldAssetsFolder != null) {
			MatchItemsExpand(oldAssetsFolder,assetsFolder);
		}
		
		if (currentSelectionPath != "" && currentSelectionPath != baseDir) {
			UIDEProjectItem workingItem = assetsFolder;
			string[] pathParts = currentSelectionPath.Split('/');
			//string buildPath = "";
			if (pathParts.Length > 1) {
				for (int i = 1; i < pathParts.Length; i++) {
					UIDEProjectItem foundChild = workingItem.FindChild(pathParts[i],true);
					if (foundChild == null) {
						break;
					}
					workingItem = foundChild;
				}
			}
			selectedItem = workingItem;
		}
		if (selectedItem == null) {
			selectedItem = assetsFolder;
		}
		visibleFileList = GetSubFiles(selectedItem);
		assetsFolder.expanded = true;
	}
	
	void MatchItemsExpand(UIDEProjectItem source, UIDEProjectItem dest) {
		dest.expanded = source.expanded;
		if (!source.expanded) return;
		if (source.childern == null) return;
		for (int i = 0; i < source.childern.Count; i++) {
			if (!source.childern[i].isFolder) continue;
			//if (!source.childern[i].expanded) continue;
			UIDEProjectItem foundDest = dest.FindChild(source.childern[i].name,true);
			if (foundDest != null) {
				MatchItemsExpand(source.childern[i],foundDest);
			}
		}
	}
	
	void OnFolderSelectionChanged() {
		//Debug.Log(selectedItem.path);
		UIDEProjectItem sel = selectedItem;
		selectedItem = CreateItem(selectedItem.path,true);
		if (sel.parent == null) {
			assetsFolder = selectedItem;
			MatchItemsExpand(sel,assetsFolder);
		}
		else {
			int index = 0;
			for (int i = 0; i < sel.parent.childern.Count; i++) {
				if (sel.parent.childern[i] == sel) break;
				index++;
			}
			sel.parent.childern.Remove(sel);
			selectedItem.parent = sel.parent;
			sel.parent.childern.Remove(selectedItem);
			sel.parent.childern.Insert(index,selectedItem);
			MatchItemsExpand(sel,selectedItem);
			//selectedItem.expanded = sel.expanded;
		}
		visibleFileList = GetSubFiles(selectedItem);
		assetsFolder.expanded = true;
	}
	
	List<UIDEProjectItem> GetSubFiles(UIDEProjectItem item) {
		List<UIDEProjectItem> list = new List<UIDEProjectItem>();
		GetSubFiles(item,list);
		return list;
	}
	
	void GetSubFiles(UIDEProjectItem item, List<UIDEProjectItem> items) {
		for (int i = 0; i < item.childern.Count; i++) {
			if (item.childern[i].isFolder) {
				//if (item.childern[i].isStub) {
					GetSubFiles(item.childern[i],items);
				//}
			}
			else {
				//Debug.Log(item.childern[i].path);
				items.Add(item.childern[i]);
			}
			
		}
	}
	
	UIDEProjectItem CreateItem(string path, bool isFolder) {
		UIDEProjectItem item = new UIDEProjectItem(path,isFolder);
		if (isFolder) {
			string[] folders = Directory.GetDirectories(path);
			bool hasNonStubFolder = false;
			for (int i = 0; i < folders.Length; i++) {
				UIDEProjectItem newFolderItem = CreateItem(folders[i],true);
				newFolderItem.parent = item;
				if (!newFolderItem.isStub) {
					hasNonStubFolder = true;
				}
			}
			
			List<string> files = GetFilesOfTypeInDirectory(path,"*.cs");
			files.AddRange(GetFilesOfTypeInDirectory(path,"*.js"));
			files.AddRange(GetFilesOfTypeInDirectory(path,"*.boo"));
			files.AddRange(GetFilesOfTypeInDirectory(path,"*.shader"));
			for (int i = 0; i < files.Count; i++) {
				UIDEProjectItem newFileItem = CreateItem(files[i],false);
				newFileItem.parent = item;
			}
			if (folders.Length > 0 && !hasNonStubFolder && files.Count == 0) {
				item.isStub = true;
			}
			if (files.Count == 0 && folders.Length == 0) {
				item.isStub = true;
			}
		}
		return item;
	}
	
	List<string> GetFilesOfTypeInDirectory(string path, string pattern) {
		string[] files = Directory.GetFiles(path,pattern);
		return files.ToList();
	}
	
	public void UpdateRects() {
		folderRect = actualRect;
		folderRect.x = 0;
		folderRect.y = 0;
		folderRect.height = sepSliderValue-UIDEEditor.divWidth;
		
		fileRect = actualRect;
		fileRect.x = 0;
		fileRect.y = 0;
		fileRect.y += sepSliderValue+UIDEEditor.divWidth;
		fileRect.height -= sepSliderValue+UIDEEditor.divWidth;
	}
	
	public void Update() {
		UpdateRects();
		//Middle Mouse Dragging
		if (projectMiddleMouseDragging) {
			Vector2 dif = mousePosition-projectMiddleMouseDownPos;
			if (Mathf.Abs(dif.x) < 4) {
				dif.x = 0;
			}
			else {
				dif.x -= 4.0f*Mathf.Sign(dif.x);
			}
			if (Mathf.Abs(dif.y) < 4) {
				dif.y = 0;
			}
			else {
				dif.y -= 4.0f*Mathf.Sign(dif.y);
			}
			projectScrollPos += dif*0.1f;
			//if (dif.x != 0 && dif.y != 0) {
				editorWindow.Repaint();
			//}
		}
		if (wantsFileSearchUpdate) {
			UpdateFileSearch();
		}
	}
	
	public void OnGUI() {
		UpdateRects();
		
		mousePosition = Event.current.mousePosition;
		if (UIDEGUI.TestClick(2,new Rect(0,0,folderRect.width,folderRect.height))) {
			projectMiddleMouseDownPos = Event.current.mousePosition;
			projectMiddleMouseDragging = true;
		}
		if (Event.current.type == EventType.MouseUp) {
			projectMiddleMouseDragging = false;
		}
		
		//FolderView
		DrawFolderView();
		
		//FileView
		DrawFileView();
	}
	
	void DrawFolderView() {
		GUIStyle bgStyle = editorWindow.theme.GetStyle("BoxBG");
		GUI.Box(folderRect,"",bgStyle);
		GUI.Box(fileRect,"",bgStyle);
		
		Rect actualFolderRect = folderRect;
		actualFolderRect.height -= 18;
		actualFolderRect.y += 18;
		
		folderColorToggle = false;
		if (!isCollapsed) {
			projectScrollPos = GUI.BeginScrollView(actualFolderRect,projectScrollPos,new Rect(0,0,projectDesiredSize.x,projectDesiredSize.y));
			projectDesiredSize.x = 0.0f;
			int totalHeight = DrawProjectItem(assetsFolder,0,-1);
			projectDesiredSize.y = totalHeight*16;
			GUI.EndScrollView();
		}
		
		Rect folderToolbarRect = folderRect;
		folderToolbarRect.height = 18;
		
		DrawFolderViewToolbar(folderToolbarRect);
	}
	
	void DrawFolderViewToolbar(Rect toolbarRect) {
		Rect toolbarRectZeroPos = toolbarRect;
		toolbarRectZeroPos.x = 0;
		toolbarRectZeroPos.y = 0;
		
		GUIStyle shadowStyle = editorWindow.theme.GetStyle("ShadowHorizontalFromTop");
		if (shadowStyle != null) {
			Rect r = toolbarRect;
			r.height = 0;
			r.y += toolbarRect.height;
			GUI.Box(r,"",shadowStyle);
		}
		
		
		GUILayout.BeginArea(toolbarRect);
		GUI.Box(toolbarRectZeroPos,"",EditorStyles.toolbar);
		GUILayout.BeginHorizontal();
		
		GUILayout.FlexibleSpace();
		
		Texture2D settingsIcon = editorWindow.theme.GetResourceTexture("SettingsIcon");
		if (!isCollapsed) {
			if (GUILayout.Button(settingsIcon,EditorStyles.toolbarButton)) {
				SettingsMenu.Show();
			}
		}
		Texture2D expandIcon = editorWindow.theme.GetResourceTexture("ProjectExpandIcon");
		Texture2D collapseIcon = editorWindow.theme.GetResourceTexture("ProjectCollapseIcon");
		Texture2D collapseButtonIcon = collapseIcon;
		if (isCollapsed) {
			collapseButtonIcon = expandIcon;
		}
		if (GUILayout.Button(collapseButtonIcon,EditorStyles.toolbarButton)) {
			if (isCollapsed) {
				Expand();
			}
			else {
				Collapse();
			}
		}
		
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}
	
	public void Expand() {
		if (!isCollapsed) return;
		isCollapsed = false;
	}
	public void Collapse() {
		if (isCollapsed) return;
		isCollapsed = true;
	}
	
	void DrawFileView() {
		
		GUIStyle itemStyle = editorWindow.theme.GetStyle("ListItem");
		GUIStyle itemSelectedStyle = editorWindow.theme.GetStyle("ListItemSelected");
		
		Rect actualFileRect = fileRect;
		actualFileRect.height -= 18;
		actualFileRect.y += 18;
		
		Rect fileToolbarRect = fileRect;
		fileToolbarRect.height = 18;
		DrawFileViewToolbar(fileToolbarRect);
		
		List<UIDEProjectItem> itemList = visibleFileList;
		if (useFileSearchTerm) {
			itemList = searchedVisibleFileList;
		}
		
		if (isCollapsed) {
			itemList = new List<UIDEProjectItem>();
		}
		
		float itemHeight = itemStyle.CalcHeight(new GUIContent("#YOLO"),fileRect.width);
		bool hasScrollbar = false;
		if (itemList.Count*itemHeight > fileRect.height) {
			hasScrollbar = true;
		}
		float itemWidth = fileRect.width;
		if (hasScrollbar) {
			itemWidth -= GUI.skin.GetStyle("verticalscrollbar").fixedWidth;
		}
		
		fileScrollPos = GUI.BeginScrollView(actualFileRect,fileScrollPos,new Rect(0,0,itemWidth,itemList.Count*itemHeight));
		
		int start = (int)((fileScrollPos.y)/itemHeight);
		int end = (int)((fileScrollPos.y+actualFileRect.height)/itemHeight);
		end += 1;
		start = (int)Mathf.Clamp(start,0,itemList.Count);
		end = (int)Mathf.Clamp(end,0,itemList.Count);
		
		for (int i = start; i < end; i++) {
			Rect r = new Rect(0,i*itemHeight,itemWidth,itemHeight);
			GUIStyle buttonStyle = itemStyle;
			if (editorWindow.textEditor && itemList[i].path == editorWindow.textEditor.filePath) {
				buttonStyle = itemSelectedStyle;
			}
			
			//if (i % 2 == 1) {
			//	GUI.color = new Color(0.98f,0.98f,0.98f,1);
			//}
			
			//if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition) && Event.current.clickCount == 2) {
			if (GUI.Button(r,itemList[i].nameNoExt,buttonStyle)) {
				editorWindow.textEditor = editorWindow.OpenOrFocusEditorFromFile(itemList[i].path);
				//GUI.color = Color.white;
				//return;
			}
			//GUI.Button(r,visibleFileList[i].nameNoExt,buttonStyle);
			//GUI.color = Color.white;
			
			Texture iconTex = null;
			#if UNITY_EDITOR
			iconTex = AssetDatabase.GetCachedIcon(itemList[i].path);
			#endif
			
			Rect texRect = r;
			texRect.height = 16;
			texRect.width = 16;
			texRect.y += (r.height-16.0f)*0.5f;
			
			if (iconTex) {
				GUI.DrawTexture(texRect,iconTex);
			}
		}
		
		
		if (!hasScrollbar) {
			GUIStyle itemListEndStyle = editorWindow.theme.GetStyle("ListItemListEnd");
			float startPoint = itemList.Count*itemHeight;
			float height = actualFileRect.height-startPoint;
			Rect r = new Rect(0,startPoint,itemWidth,height);
			GUI.Box(r,"",itemListEndStyle);
		}
		GUI.EndScrollView();
		
		
		
	}
	
	void DrawFileViewToolbar(Rect toolbarRect) {
		//GUI.Box(toolbarRect,"",EditorStyles.toolbar);
		
		Rect toolbarRectZeroPos = toolbarRect;
		toolbarRectZeroPos.x = 0;
		toolbarRectZeroPos.y = 0;
		
		GUILayout.BeginArea(toolbarRect);
		GUI.Box(toolbarRectZeroPos,"",EditorStyles.toolbar);
		
		if (!isCollapsed) {
			GUILayout.Space(2);
			GUILayout.BeginHorizontal();
			
			GUIStyle searchBoxStyle = editorWindow.defaultSkin.GetStyle("ToolbarSeachTextField");
			//float startTime = Time.realtimeSinceStartup;
			GUIStyle searchBoxCancelStyle = editorWindow.defaultSkin.GetStyle("ToolbarSeachCancelButtonEmpty");
			
			if (fileSearchTerm != "") {
				searchBoxCancelStyle = editorWindow.defaultSkin.GetStyle("ToolbarSeachCancelButton");
			}
			
			string previousSearchTerm = fileSearchTerm;
			GUI.SetNextControlName("UIDE_ProjectViewFileSearch");
			fileSearchTerm = EditorGUILayout.TextField(fileSearchTerm,searchBoxStyle);
			if (GUILayout.Button("",searchBoxCancelStyle)) {
				fileSearchTerm = "";
			}
			
			if (previousSearchTerm != fileSearchTerm) {
				wantsFileSearchUpdate = true;
			}
			
			fileSearchFildIsFocused = GUI.GetNameOfFocusedControl() == "UIDE_ProjectViewFileSearch";
			//Debug.Log(GUI.GetNameOfFocusedControl()+" "+UIDEEditor.focusedWindow);
			GUILayout.Space(5);
			
			GUILayout.EndHorizontal();
		}
		
		GUILayout.EndArea();
	}
	
	public void UpdateFileSearch() {
		wantsFileSearchUpdate = false;
		useFileSearchTerm = fileSearchTerm != "";
		
		searchedVisibleFileList = new List<UIDEProjectItem>();
		if (!useFileSearchTerm) return;
		
		List<UIDEFuzzySearchItem> inputItems = new List<UIDEFuzzySearchItem>();
		for (int i = 0; i < visibleFileList.Count; i++) {
			UIDEFuzzySearchItem fuzzyItem = new UIDEFuzzySearchItem();
			fuzzyItem.text = visibleFileList[i].nameNoExt;
			fuzzyItem.metaObject = visibleFileList[i];
			inputItems.Add(fuzzyItem);
		}
		
		UIDEFuzzySearchItem[] sortedFuzzyItems = UIDEFuzzySearch.GetSortedList(fileSearchTerm,inputItems.ToArray(),false,false);
		
		for (int i = 0; i < sortedFuzzyItems.Length; i++) {
			UIDEProjectItem item = (UIDEProjectItem)sortedFuzzyItems[i].metaObject;
			searchedVisibleFileList.Add(item);
		}
	}
	
	int DrawProjectItem(UIDEProjectItem item, int yPos, int indent) {
		Rect r = new Rect(indent*16,yPos*16,0,16);
		
		Rect invisButtonRect = r;
		invisButtonRect.x = projectScrollPos.x;
		invisButtonRect.width = rect.width;
		
		if (item == selectedItem) {
			UIDEGUI.ColorBox(invisButtonRect,new Color(0.23828125f,0.375f,0.56640625f,1.0f));
		}
		//if (folderColorToggle) {
		//	UIDEGUI.ColorBox(invisButtonRect,new Color(1,1,1,0.0f));
		//}
		
		bool isStub = true;
		for (int i = 0; i < item.childern.Count; i++) {
			if (!item.childern[i].isStub && item.childern[i].isFolder) {
				isStub = false;
				break;
			}
		}
		if (item.isFolder && !isStub) {
			Rect foldoutRect = r;
			foldoutRect.width = 16;
			
			if (UIDEGUI.TestClick(foldoutRect)) {
				item.expanded = !item.expanded;
			}
			
			Texture2D foldoutExpandedTex = EditorStyles.foldout.onNormal.background;
			Texture2D foldoutUnexpandedTex = EditorStyles.foldout.normal.background;
			if (item == selectedItem) {
				foldoutExpandedTex = EditorStyles.foldout.onActive.background;
				foldoutUnexpandedTex = EditorStyles.foldout.active.background;
			}
			if (foldoutExpandedTex && foldoutUnexpandedTex) {
				foldoutRect.width = foldoutExpandedTex.width;
				foldoutRect.height = foldoutExpandedTex.height;
				foldoutRect.x = foldoutRect.x+16-foldoutRect.width;
				foldoutRect.y = foldoutRect.y+r.height*0.5f-foldoutRect.height*0.5f;
				if (item.expanded) {
					GUI.DrawTexture(foldoutRect,foldoutExpandedTex);
				}
				else {
					GUI.DrawTexture(foldoutRect,foldoutUnexpandedTex);
				}
			}
			
			//Debug.Log(foldoutExpandedTex);
			//UIDEGUI.ColorBox(foldoutRect,new Color(1,0,0,1));
			//EditorGUI.Foldout(foldoutRect,item.expanded,"",EditorStyles.foldout);
		}
		
		if (UIDEGUI.TestClick(0,invisButtonRect)||UIDEGUI.TestClick(1,invisButtonRect)) {
			selectedItem = item;
			projectScrollPos.x = Mathf.Min(projectScrollPos.x,r.x);
			OnFolderSelectionChanged();
			Event.current.Use();
		}
		
		Texture iconTex = null;
		#if UNITY_EDITOR
		iconTex = AssetDatabase.GetCachedIcon(item.path);
		#endif
		r.x += 16;
		Rect texRect = r;
		texRect.width = 16;
		if (iconTex) {
			GUI.DrawTexture(texRect,iconTex);
		}
		r.x += 16;
		GUIContent labelContent = new GUIContent(item.name);
		projectDesiredSize.x = Mathf.Max(projectDesiredSize.x,EditorStyles.whiteLabel.CalcSize(labelContent).x+r.x);
		r.width = projectDesiredSize.x;
		if (selectedItem == item) {
			GUI.Label(r,labelContent,EditorStyles.whiteLabel);
		}
		else {
			GUI.Label(r,labelContent,EditorStyles.label);
		}
		
		yPos++;
		indent++;
		folderColorToggle = !folderColorToggle;
		if (!item.expanded) {
			return yPos;
		}
		
		for (int i = 0; i < item.childern.Count; i++) {
			if (item.childern[i].isStub) continue;
			if (!item.childern[i].isFolder) continue;
			yPos = DrawProjectItem(item.childern[i],yPos,indent);
		}
		return yPos;
	}
	
	
	
	static public UIDEProjectView Load(UIDEEditor editorWindow) {
		UIDEProjectView projectView = (UIDEProjectView)ScriptableObject.CreateInstance(typeof(UIDEProjectView));
		//projectView.editorWindow = editorWindow;
		#if UNITY_EDITOR
		//if (Directory.Exists("Assets/UnIDE/_TMP/ProjectView")) {
		//	DirectoryDelete("Assets/UnIDE/_TMP/ProjectView",true);
		//}
		string assetPath = UIDEEditor.tmpDir+"ProjectView/ProjectView.asset";
		string assetDir = Path.GetDirectoryName(assetPath);
		if (!Directory.Exists(assetDir)) {
			Directory.CreateDirectory(assetDir);
		}
		projectView.assetPath = assetPath;
		AssetDatabase.CreateAsset(projectView, assetPath);
		AssetDatabase.SaveAssets();
		#endif
		projectView.Start();
		return projectView;
	}
}
