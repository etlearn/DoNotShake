using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UIDESettingsGroupDataGeneral:UIDESettingsGroupData {
	public bool useCtrlZUndo = true;
	public bool useCodeFolding = false;
	public bool collapseProjectPanel = false;
	public bool forceGenericAutoComplete = false;
	public bool disableCompletion = false;
	public List<string> openFiles = new List<string>();
	
	public List<string> supportedFileTypes;
	
	public List<string> tokenDefBoldStates;
	public List<string> apiTokenAssemblies = new List<string>(new string[] {"UnityEngine","UnityEditor"});
}
