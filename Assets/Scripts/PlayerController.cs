using UnityEngine;
using System.Collections;

public class PlayerController:Actor {
	public float speed = 10.0f;
	public float walkAnimSpeed = 1.0f;
	public float maxWalkSpeed = 30.0f;
	public float tiltThreashold = 0.1f;
	public float keyboardTiltRate = 0.25f;
	public float maxKeyboardTilt = 0.5f;
	public float extraGravity = 1.0f;
	public float shakeJumpMultiplyer = 5.0f;
	private Vector2 keyboardTilt;
	private bool isShakeJumping = false;
	private float shakeJumpPower = 0.0f;
	
	
	void Start() {
		anim.wrapMode = WrapMode.Loop;
		//PlayAnimation("idle",1);
		//PlayAnimation
	}
	
	void Update() {
		UpdateKeyboardTilt();
		transform.eulerAngles = new Vector3(0,180,0);
		
		if (isShakeJumping) {
			if (character.isGrounded) {
				isShakeJumping = false;
				if (shakeJumpPower >= 2.0f) {
					Explode();
				}
			}
			else {
				
			}
		}
		if (character.isGrounded) {
			Vector3 localVel = rigidbody.velocity;
			localVel= transform.InverseTransformDirection(localVel);
			if (Mathf.Abs(localVel.x) > 0.5f) {
				if (localVel.x > 0) {
					PlayAnimation("walkleft",walkAnimSpeed);
				}
				else {
					PlayAnimation("walkright",walkAnimSpeed);
				}
			}
			else {
				PlayAnimation("idle",1);
			}
		}
	}
	
	public void ShakeJump(float power) {
		if (isShakeJumping) return;
		Jump(power*shakeJumpMultiplyer);
		isShakeJumping = true;
		shakeJumpPower = power;
	}
	
	public void Explode() {
		if (game) {
			game.StartGame();
		}
		exploder.Explode();
	}
	
	
	public void Jump(float power) {
		if (character.isGrounded) {
			Vector3 newPos = rigidbody.position;
			newPos.y += character.footOffset*character.footAnchorRatio;
			newPos.y += 0.01f;
			rigidbody.position = newPos;
			transform.position = newPos;
		}
		character.isGrounded = false;
		Vector3 newVel = rigidbody.velocity;
		newVel.y += power;
		rigidbody.velocity = newVel;
		PlayAnimation("jump",1);
		//rigidbody.AddForce(Vector3.up*power*1000.0f,ForceMode.Impulse);
	}
	
	
	public void FixedUpdate() {
		float horizontalTiltForce = 0.0f;
		if (keyboardTilt.x > 0.0f) {
			horizontalTiltForce = Mathf.Max(keyboardTilt.x-tiltThreashold,0.0f);
		}
		else if (keyboardTilt.x < 0.0f) {
			horizontalTiltForce = Mathf.Min(keyboardTilt.x+tiltThreashold,0.0f);
		}
		float forceMod = speed*horizontalTiltForce;
		forceMod = Mathf.Clamp(forceMod,-maxWalkSpeed,maxWalkSpeed);
		rigidbody.AddForce(Vector3.right*forceMod);
		
		if (!character.isGrounded) {
			rigidbody.AddForce(Physics.gravity*extraGravity,ForceMode.Acceleration);
		}
	}
	
	public void UpdateKeyboardTilt() {
		float keyValue = 0.0f;
		keyValue += Input.GetAxis("Horizontal");
		keyValue += Input.acceleration.y;
		//keyboardTilt.x = keyValue;
		keyboardTilt.x = Mathf.Lerp(keyboardTilt.x,keyValue,keyboardTiltRate*Time.deltaTime);
		keyboardTilt.x = Mathf.Clamp(keyboardTilt.x,-maxKeyboardTilt,maxKeyboardTilt);
		keyboardTilt.y = 0;
	}
}
