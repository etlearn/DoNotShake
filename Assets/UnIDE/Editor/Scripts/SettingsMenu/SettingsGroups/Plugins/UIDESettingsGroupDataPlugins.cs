using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

//using UIDE.Plugins;

public class UIDESettingsGroupDataPlugins:UIDESettingsGroupData {
	public bool initialized = false;
	public List<string> knownPlugins = new List<string>();
	public List<bool> knownPluginEnableStates = new List<bool>();
}
