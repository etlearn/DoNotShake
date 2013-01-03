using UnityEngine;
using System.Collections;

public class StarRocketFlyAway:MonoBehaviour {
	public float force = 10.0f;
	public Vector3 forceVector = Vector3.up;
	public Vector3 initialForce = new Vector3(0,0,0);
	
	public Vector3 initialTorque = new Vector3(0,0,0);
	public Vector3 constantTorque = new Vector3(0,0,0);
	public bool randomizeTorque = true;
	public float delay = 0.0f;
	private bool hasEnemyActivate = false;
	private float enemyActivateTime = 0.0f;
	private bool isActivated = false;
	
	public void Start() {
		//OnEnemyActivate();
	}
	
	public void OnEnemyActivate() {
		hasEnemyActivate = true;
		enemyActivateTime = Time.timeSinceLevelLoad;
		
		if (rigidbody == null) {
			Debug.LogWarning("Tried to active StarRocketFlyAway without a rigidbody.");
		}
	}
	
	public void FixedUpdate() {
		if (!rigidbody) return;
		
		if (hasEnemyActivate && !isActivated) {
			if (Time.timeSinceLevelLoad-enemyActivateTime >= delay) {
				isActivated = true;
				Character character = gameObject.GetComponent<Character>();
				if (character) {
					character.enabled = false;
				}
				
				rigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
				
				float xTorqueScaler = Random.Range(-1.0f,1.0f);
				float yTorqueScaler = Random.Range(-1.0f,1.0f);
				float zTorqueScaler = Random.Range(-1.0f,1.0f);
				
				initialTorque.x *= xTorqueScaler;
				initialTorque.y *= yTorqueScaler;
				initialTorque.z *= zTorqueScaler;
				
				constantTorque.x *= xTorqueScaler;
				constantTorque.y *= yTorqueScaler;
				constantTorque.z *= zTorqueScaler;
				
				rigidbody.AddRelativeForce(initialForce,ForceMode.Impulse);
				rigidbody.AddRelativeTorque(initialTorque,ForceMode.Impulse);
			}
		}
		if (!isActivated) return;
		
		rigidbody.AddRelativeForce(forceVector*force);
		rigidbody.AddRelativeTorque(constantTorque);
	}
	
}
