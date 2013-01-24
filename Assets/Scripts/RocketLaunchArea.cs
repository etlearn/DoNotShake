using UnityEngine;
using System.Collections;

public class RocketLaunchArea : MonoBehaviour {

	// Use this for initialization
	void Start () {
		if (renderer) {
			renderer.enabled = false;
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
