using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelSelect:MonoBehaviour {
	public Camera uiCam;
	public int levelDefsPerRow = 6;
	public float worldButtonSeparation = 600.0f;
	
	public UIPanel worldSelectPanel;
	public UIPanel levelSelectPanel;
	
	public UIGrid worldGrid;
	public UIGrid levelGrid;
	
	public List<WorldDef> worldDefs = new List<WorldDef>();
	[HideInInspector]
	public List<WorldDef> loadedWorldDefs = new List<WorldDef>();
	[HideInInspector]
	public List<LevelGroupDef> loadedLevelGroups = new List<LevelGroupDef>();
	
	
	public WorldDef focusedWorld {
		get {
			return loadedWorldDefs[focusedWorldIndex];
		}
	}
	
	private int _focusedWorldIndex = 0;
	public int focusedWorldIndex {
		get {
			return _focusedWorldIndex;
		}
		set {
			_focusedWorldIndex = value;
			_focusedWorldIndex = Mathf.Clamp(_focusedWorldIndex,0,Mathf.Max(worldDefs.Count-1,0));
		}
	}
	
	public LevelGroupDef focusedLevelGroup {
		get {
			return loadedLevelGroups[focusedLevelGroupIndex];
		}
	}
	
	private int _focusedLevelGroupIndex = 0;
	public int focusedLevelGroupIndex {
		get {
			return _focusedLevelGroupIndex;
		}
		set {
			_focusedLevelGroupIndex = value;
			_focusedLevelGroupIndex = Mathf.Clamp(_focusedLevelGroupIndex,0,Mathf.Max(loadedLevelGroups.Count-1,0));
		}
	}
	
	public bool isWorldSelect = true;
	
	public float currentWorldScroll = 0.0f;
	
	private bool isDragging = false;
	private Vector2 dragDownPos;
	private Vector3 dragDownWorldGridPos;
	private Vector3 dragDownLevelGridPos;
	public float dragValue {
		get {
			if (!isDragging) return 0.0f;
			return worldMousePos.x-dragDownPos.x;
		}
	}
	
	public Vector3 worldMousePos {
		get {
			Vector3 v = uiCam.ScreenToWorldPoint(Input.mousePosition);
			v.x /= uiCam.transform.lossyScale.x;
			v.y /= uiCam.transform.lossyScale.y;
			v.z /= uiCam.transform.lossyScale.z;
			return v;
		}
	}
	
	void Start() {
		GoToWorldSelection();
	}
	
	void Update() {
		if (Input.GetMouseButtonDown(0)) {
			dragDownPos = worldMousePos;
			dragDownWorldGridPos = worldGrid.transform.localPosition;
			dragDownLevelGridPos = levelGrid.transform.localPosition;
			isDragging = true;
		}
		if (Input.GetMouseButtonUp(0)) {
			int newIndex = -Mathf.RoundToInt(dragValue/worldButtonSeparation);
			if (isWorldSelect) {
				focusedWorldIndex += newIndex;
			}
			else {
				focusedLevelGroupIndex += newIndex;
			}
			isDragging = false;
		}
		if (isDragging && Mathf.Abs(dragValue) > 20.0f) {
			DisableWorldButtons();
			DisableLevelButtons();
		}
		
		if (isWorldSelect) {
			UpdateWorldSelection();
		}
		else {
			UpdateLevelSelect();
		}
	}
	
	void LateUpdate() {
		if (Input.GetMouseButtonUp(0)) {
			EnableWorldButtons();
			EnableLevelButtons();
		}
	}
	
	public void UpdateWorldSelection() {
		float desiredScroll = focusedWorldIndex;
		desiredScroll *= worldButtonSeparation;
		
		if (isDragging) {
			desiredScroll -= dragValue;
		}
		
		currentWorldScroll = Mathf.Lerp(currentWorldScroll,desiredScroll,Time.deltaTime*6.0f);
		
		Vector3 wgPos = worldGrid.transform.localPosition;
		wgPos.x = -currentWorldScroll;
		worldGrid.transform.localPosition = wgPos;
		
		//Debug.Log(worldGrid.transform.localPosition);
		
		if (Input.GetKeyDown(KeyCode.LeftArrow)) {
			focusedWorldIndex--;
		}
		if (Input.GetKeyDown(KeyCode.RightArrow)) {
			focusedWorldIndex++;
		}
		
		if (Input.GetKeyDown(KeyCode.Return)) {
			GoToLevelSelection(focusedWorld);
		}
	}
	
	public void UpdateLevelSelect() {
		float desiredScroll = focusedLevelGroupIndex;
		desiredScroll *= worldButtonSeparation;
		
		if (isDragging) {
			desiredScroll -= dragValue;
		}
		
		currentWorldScroll = Mathf.Lerp(currentWorldScroll,desiredScroll,Time.deltaTime*6.0f);
		
		Vector3 wgPos = levelGrid.transform.localPosition;
		wgPos.x = -currentWorldScroll;
		levelGrid.transform.localPosition = wgPos;
		
		if (Input.GetKeyDown(KeyCode.LeftArrow)) {
			focusedLevelGroupIndex--;
		}
		if (Input.GetKeyDown(KeyCode.RightArrow)) {
			focusedLevelGroupIndex++;
		}
	}
	
	public void GoToNextPage() {
		if (isWorldSelect) {
			focusedWorldIndex++;
		}
		else {
			focusedLevelGroupIndex++;
		}
	}
	public void GoToPreviousPage() {
		if (isWorldSelect) {
			focusedWorldIndex--;
		}
		else {
			focusedLevelGroupIndex--;
		}
	}
	
	public void GoToMainMenu() {
		Application.LoadLevel("MainMenu");
	}
	
	public void GoToWorldSelection() {
		ClearGrid(worldGrid);
		ClearGrid(levelGrid);
		loadedWorldDefs = new List<WorldDef>();
		
		levelSelectPanel.enabled = false;
		worldSelectPanel.enabled = true;
		
		worldSelectPanel.gameObject.SetActiveRecursively(true);
		levelSelectPanel.gameObject.SetActiveRecursively(false);
		
		focusedLevelGroupIndex = 0;
		//focusedWorldIndex = 0;
		
		worldGrid.cellWidth = worldButtonSeparation;
		
		for (int i = 0; i < worldDefs.Count; i++) {
			WorldDef wd = (WorldDef)Instantiate(worldDefs[i]);
			wd.transform.parent = worldGrid.transform;
			wd.transform.localScale = Vector3.one;
			wd.levelSelect = this;
			loadedWorldDefs.Add(wd);
		}
		
		worldGrid.Reposition();
		isWorldSelect = true;
	}
	
	public void GoToLevelSelection(WorldDef worldDef) {
		ClearGrid(worldGrid);
		ClearGrid(levelGrid);
		loadedLevelGroups = new List<LevelGroupDef>();
		
		levelSelectPanel.enabled = true;
		worldSelectPanel.enabled = false;
		
		worldSelectPanel.gameObject.SetActiveRecursively(false);
		levelSelectPanel.gameObject.SetActiveRecursively(true);
		
		focusedLevelGroupIndex = 0;
		//focusedWorldIndex = 0;
		
		levelGrid.cellWidth = worldButtonSeparation;
		
		for (int i = 0; i < worldDef.levelGroups.Count; i++) {
			LevelGroupDef lg = (LevelGroupDef)Instantiate(worldDef.levelGroups[i]);
			lg.transform.parent = levelGrid.transform;
			lg.transform.localScale = Vector3.one;
			lg.levelSelect = this;
			loadedLevelGroups.Add(lg);
			
			lg.grids = new UIGrid[4];
			
			for (int j = 0; j < lg.grids.Length; j++) {
				lg.grids[j] = new GameObject("Grid").AddComponent<UIGrid>();
				lg.grids[j].cellWidth = 100.0f;
				lg.grids[j].transform.parent = lg.transform;
				lg.grids[j].transform.localPosition = new Vector3(-(100.0f*4.0f)/2.0f,((100.0f*4.0f)/2.0f)-j*100.0f,0);
				lg.grids[j].transform.localPosition += new Vector3(100.0f*0.5f,-100.0f*0.5f,0);
				lg.grids[j].transform.localScale = Vector3.one;
			}
			
			for (int j = 0; j < lg.levels.Count; j++) {
				int gridIndex = j/4;
				//Debug.Log(j+" "+gridIndex);
				LevelDef ld = (LevelDef)Instantiate(lg.levels[j]);
				ld.transform.parent = lg.grids[gridIndex].transform;
				ld.transform.localScale = Vector3.one;
				ld.levelSelect = this;
			}
			for (int j = 0; j < lg.grids.Length; j++) {
				lg.grids[j].Reposition();
			}
		}
		
		levelGrid.Reposition();
		isWorldSelect = false;
	}
	
	public void LoadLevel(LevelDef level) {
		if (level.sceneName != "") {
			Application.LoadLevel(level.sceneName);
		}
	}
	
	public void DisableWorldButtons() {
		foreach (Transform t in worldGrid.transform) {
			UIButtonMessage button = t.gameObject.GetComponent<UIButtonMessage>();
			if (button) {
				button.enabled = false;
			}
		}
	}
	public void EnableWorldButtons() {
		foreach (Transform t in worldGrid.transform) {
			UIButtonMessage button = t.gameObject.GetComponent<UIButtonMessage>();
			if (button) {
				button.enabled = true;
			}
		}
	}
	
	public void DisableLevelButtons() {
		foreach (Transform t1 in levelGrid.transform) {
			foreach (Transform t2 in t1.transform) {
				foreach (Transform t in t2.transform) {
					UIButtonMessage button = t.gameObject.GetComponent<UIButtonMessage>();
					if (button) {
						button.enabled = false;
					}
				}
			}
		}
	}
	public void EnableLevelButtons() {
		foreach (Transform t1 in levelGrid.transform) {
			foreach (Transform t2 in t1.transform) {
				foreach (Transform t in t2.transform) {
					UIButtonMessage button = t.gameObject.GetComponent<UIButtonMessage>();
					if (button) {
						button.enabled = true;
					}
				}
			}
		}
	}
	
	public void ClearGrid(UIGrid grid) {
		if (grid == null) return;
		foreach (Transform t in grid.transform) {
			Destroy(t.gameObject);
		}
		
	}
}
