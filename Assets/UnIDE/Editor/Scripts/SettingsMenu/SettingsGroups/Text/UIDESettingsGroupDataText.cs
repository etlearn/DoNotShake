using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UIDESettingsGroupDataText:UIDESettingsGroupData {
	public int fontSize = 12;
	public List<string> tokenDefBoldStates = null;
	public int tabSize = 4;
	
	public bool forceDynamicFont35 = true;
	public Font font;
	public Font boldFont;
	public string desiredFontPath = "";
}
