using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Floater : MonoBehaviour {
	public float floatForce = 1.0f;
	public float desiredPosWeight = 1.0f;
	public float randomOffsetSize = 1.0f;
	public Transform desiredTarget;
	private Vector3 randomOffset;
	private Vector3 spawnPos;
	
	public Vector3 desiredPosition {
		get {
			Vector3 dPos = spawnPos;
			if (desiredTarget) {
				dPos = desiredTarget.position;
			}
			dPos += randomOffset;
			return dPos;
		}
	}
	
	public Vector3 gravity {
		get {
			return Physics.gravity;
		}
	}
	
	public Vector3 dirToDesiredPos {
		get {
			Vector3 dPos = spawnPos;
			if (desiredTarget) {
				dPos = desiredTarget.position;
			}
			dPos += randomOffset;
			Vector3 normal = dPos-transform.position;
			normal.z = 0.0f;
			normal.Normalize();
			return normal;
		}
	}
	
	public float distToDesiredPos {
		get {
			return Vector3.Distance(transform.position,desiredPosition);
		}
	}
	
	
	void Update() {
		randomOffset = Random.insideUnitSphere*randomOffsetSize;
	}
	void Start() {
		spawnPos = transform.position;
	}
	
	void FixedUpdate() {
		if (rigidbody.IsSleeping()) {
			rigidbody.WakeUp();
		}
		rigidbody.AddForce(-gravity*floatForce);
		
		rigidbody.AddForce(dirToDesiredPos*distToDesiredPos*desiredPosWeight);
		
	}
}
