//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2012 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Sends a message to the remote object when something happens.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Button Find Message")]
public class UIButtonFindMessage:UIButtonMessage {
	public string objectName = "";
	
	public void Start() {
		if (objectName != "") {
			target = GameObject.Find(objectName);
		}
	}
}