using UnityEngine;
using System.Collections;

public class DelayedSpawner:MonoBehaviour {
	//public bool addToSharedParent = true;
	//public bool destroyOnSpawn = true;
	public GameObject obj;
	public float delay = 0.0f;
	public bool globalScale = true;
	private float startTime = 0.0f;
	//private bool spawned = false;
	// Use this for initialization
	void Start () {
		startTime = Time.timeSinceLevelLoad;
	}
	
	// Update is called once per frame
	void Update () {
		if (Time.timeSinceLevelLoad-startTime > delay) {
			if (obj) {
				GameObject newObj = (GameObject)Instantiate(obj);
				
				newObj.transform.parent = transform;
				newObj.transform.localPosition = Vector3.zero;
				newObj.transform.localRotation = Quaternion.identity;
				
				if (!globalScale) {
					newObj.transform.localScale = Vector3.one;
				}
			}
			Destroy(this);
		}
	}
}
