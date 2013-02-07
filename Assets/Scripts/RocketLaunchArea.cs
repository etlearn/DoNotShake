using UnityEngine;
using System.Collections;

public class RocketLaunchArea : MonoBehaviour {
	public bool flattenZ = true;
	void Start () {
		if (renderer) {
			renderer.enabled = false;
		}
	}
	public Vector3 GetRandomPosition() {
		Vector3 pos = transform.position;
		pos += GetRandomOffset();
		if (flattenZ) {
			pos.z = 0.0f;
		}
		return pos;
	}
	
	public Vector3 GetRandomOffset() {
		Vector3 pos = Vector3.zero;
		pos.x += Random.Range(-transform.lossyScale.x,transform.lossyScale.x)*0.5f;
		pos.y += Random.Range(-transform.lossyScale.y,transform.lossyScale.y)*0.5f;
		pos.z += Random.Range(-transform.lossyScale.z,transform.lossyScale.z)*0.5f;
		if (flattenZ) {
			pos.z = 0.0f;
		}
		return pos;
	}
}
