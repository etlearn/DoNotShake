using UnityEngine;
using System.Collections;

public class AIMoveToggleWalk:AIMovement {
	public bool walk = true;
	public bool flying = false;
	public int walkDir = 1;
	public float speed = 5.0f;
	public float leftTurnDegrees = 90.0f;
	public float rightTurnDegrees = -90.0f;
	public float turnSpeed = 1.0f;
	public float wallCheckDist = 0.75f;
	public float dropCheckDistance = 0.5f;
	public float dropCheckDownDistance = 1.0f;
	
	public Vector3 logicalForward {
		get {
			if (walkDir == 0) return new Vector3(0,0,-1);
			if (walkDir == 1) return new Vector3(1,0,0);
			if (walkDir == -1) return new Vector3(-1,0,0);
			return new Vector3(0,0,-1);
		}
	}
	
	public Vector3 flatForward {
		get {
			Vector3 v = transform.forward;
			v.z = 0.0f;
			return v;
		}
	}
	
	public override void UpdateMovement() {
		if (!walk) {
			walkDir = 0;
		}
		else {
			if (walkDir == 0) {
				walkDir = 1;
			}
		}
		
		if (walkDir == 0 || !character.isGrounded) {
			TurnTowards(180,turnSpeed*Time.deltaTime);
		}
		else if (walkDir == 1) {
			TurnTowards(180.0f+rightTurnDegrees,turnSpeed*Time.deltaTime);
		}
		else if (walkDir == -1) {
			TurnTowards(180.0f+leftTurnDegrees,turnSpeed*Time.deltaTime);
		}
		
		if (walkDir == -1 || walkDir == 1) {
			bool shouldSwitchDirection = false;
			if (!flying) {
				if (character.isGrounded  && !TestForwardDrop()) {
					shouldSwitchDirection = true;
				}
			}
			if (!shouldSwitchDirection && TestWall()) {
				shouldSwitchDirection = true;
			}
			if (shouldSwitchDirection) {
				SwitchDirection();
				if (character.isGrounded) {
					//Vector3 vel = rigidbody.velocity;
					//vel.x = 0.0f;
				}
			}
			
			if (character.isGrounded) {
				rigidbody.AddForce(logicalForward*speed);
			}
		}
		
	}
	
	public void TurnTowards(float angle, float amount) {
		Quaternion newRot = Quaternion.identity;
		newRot.eulerAngles = new Vector3(0,angle,0);
		transform.rotation = Quaternion.RotateTowards(transform.rotation,newRot,amount*360.0f);
	}
	
	public void SwitchDirection() {
		if (walkDir == 1) {
			walkDir = -1;
		}
		else if (walkDir == -1) {
			walkDir = 1;
		}
		else {
			walkDir = 1;
		}
	}
	
	public bool TestWall() {
		if (Physics.Raycast(transform.position,logicalForward,wallCheckDist,awarenessMask)) {
			return true;
		}
		return false;
	}
	
	public bool TestForwardDrop() {
		Vector3 pos = GetDropCheckDownPos();
		//RaycastHit hit;
		if (Physics.Raycast(pos,Vector3.down,dropCheckDownDistance,awarenessMask)) {
			return true;
		}
		return false;
	}
	
	public Vector3 GetDropCheckDownPos() {
		Vector3 pos = transform.position+logicalForward*dropCheckDistance;
		return pos;
	}
	void OnDrawGizmos() {
		Vector3 pos = GetDropCheckDownPos();
		Gizmos.DrawLine(pos,pos+Vector3.down*dropCheckDownDistance);
		Gizmos.DrawLine(transform.position,transform.position+logicalForward*wallCheckDist);
	}
}
