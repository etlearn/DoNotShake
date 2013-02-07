using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorldDef:MonoBehaviour {
	public string friendlyName = "World";
	public Texture2D uiTexture;
	public List<LevelGroupDef> levelGroups;
	public UILabel label;
	public LevelSelect levelSelect;
	
	public void Start() {
		label.text = friendlyName;
	}
	
	public void Activate() {
		levelSelect.GoToLevelSelection(this);
	}
}
