using UnityEngine;
using System.Collections;

public class TimeDestroy : MonoBehaviour {
	public float timer = 0.0f;
	
	void Start () {
		Destroy(gameObject,timer);
	}
}
