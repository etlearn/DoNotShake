using UnityEngine;
using System.Collections;

public class StarRocketThrust:MonoBehaviour {
	public bool scaleForceByGravityMagnitude = true;
	public bool waitForEnemyActivate = true;
	public bool disableCollider = true;
	public float velocityRotateStrength = 1.0f;
	public float cutoffTime = 0.0f;
	public float force = 10.0f;
	public Vector3 forceVector = Vector3.up;
	public Vector3 initialForce = new Vector3(0,0,0);
	
	public Vector3 initialTorque = new Vector3(0,0,0);
	public Vector3 constantTorque = new Vector3(0,0,0);
	public bool randomizeTorque = true;
	public float delay = 0.0f;
	private bool hasEnemyActivate = false;
	private float enemyActivateTime = 0.0f;
	private float activateTime = 0.0f;
	private bool isActivated = false;
	
	public void Start() {
		if (!waitForEnemyActivate) {
			OnEnemyActivate();
		}
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
		if (isActivated && cutoffTime > 0.0f && Time.timeSinceLevelLoad-activateTime >= cutoffTime) {
			UpdateNormalRotation();
			return;
		}
		
		if (hasEnemyActivate && !isActivated) {
			if (Time.timeSinceLevelLoad-enemyActivateTime >= delay) {
				isActivated = true;
				Character character = gameObject.GetComponent<Character>();
				if (character) {
					character.enabled = false;
				}
				if (disableCollider && collider) {
					collider.enabled = false;
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
				
				Vector3 actualInitialForce = initialForce;
				if (scaleForceByGravityMagnitude) {
					actualInitialForce *= Physics.gravity.magnitude;
				}
				rigidbody.AddRelativeForce(actualInitialForce,ForceMode.Impulse);
				rigidbody.AddRelativeTorque(initialTorque,ForceMode.Impulse);
				
				activateTime = Time.timeSinceLevelLoad;
			}
		}
		if (!isActivated) return;
		
		Vector3 actualForce = forceVector*force;
		if (scaleForceByGravityMagnitude) {
			actualForce *= Physics.gravity.magnitude;
		}
				
		rigidbody.AddRelativeForce(actualForce);
		rigidbody.AddRelativeTorque(constantTorque);
		
		UpdateNormalRotation();
	}
	
	public void UpdateNormalRotation() {
		Vector3 velNormal = rigidbody.velocity.normalized;
		//Quaternion newRot = transform.rotation;
		//Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up,velNormal);
		//newRot = Quaternion.Lerp(transform.rotation, normalRotation, Time.deltaTime*velocityRotateStrength);
		
		Quaternion normalRotation = new Quaternion(0,0,0,0);
		normalRotation.SetLookRotation(transform.forward, velNormal);
		Quaternion newRot = Quaternion.Lerp(transform.rotation, normalRotation, Time.deltaTime*velocityRotateStrength);
		
		transform.rotation = newRot;
	}
	
}
