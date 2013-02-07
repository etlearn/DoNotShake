using UnityEngine;
using System.Collections;

public class LevelDef : MonoBehaviour {
	public string friendlyName = "Level";
	public string sceneName = "";
	public UILabel label;
	public LevelSelect levelSelect;
	
	public void Start() {
		label.text = friendlyName;
	}
	
	public void Activate() {
		levelSelect.LoadLevel(this);
	}
}
