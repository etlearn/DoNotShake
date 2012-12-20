using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Character))]

public class OldPlayerController:MonoBehaviour {
	
	public float lookSpeed = 5.0f;
	public float moveSpeed = 5.0f;
	public float jumpVelocity = 5.0f;
	public float jumpDirectionalFactor = 1.0f;
	public float jumpHoldFactor = 3.0f;
	public float jumpHoldTime = 0.2f;
	public float airControl = 0.2f;
	
	public Camera cam;
	
	[HideInInspector]
	public Vector3 desiredMoveDir;
	
	
	private Character _character;
	public Character character {
		get {
			if (!_character) {
				_character = gameObject.GetComponent<Character>();
			}
			return _character;
		}
	}
	
	public Transform cameraPivot {
		get {
			if (!cam) return null;
			return cam.transform.parent;
		}
	}
	
	private float yAngle = 360.0f;
	private bool wantsToJump;
	private float lastJumpTime;
	private bool isJumping;
	
	void Start () {
		
	}
	
	void Update() {
		//Mouse Lock
		if (Input.GetMouseButtonUp(0) && !Screen.lockCursor) {
			Screen.lockCursor = true;
		}
		if (Input.GetKeyDown(KeyCode.Escape)) {
			Screen.lockCursor = false;
		}
		if (Screen.lockCursor) {
			//Movement
			desiredMoveDir = new Vector3(0,0,0);
			desiredMoveDir.z += Input.GetAxisRaw("Vertical");
			desiredMoveDir.x += Input.GetAxisRaw("Horizontal");
			if (desiredMoveDir.magnitude >= 0.25f) {
				desiredMoveDir.Normalize();
			}
			
			//Jumping
			if (Input.GetButtonDown("Jump") || ((wantsToJump) && Input.GetButton("Jump"))) {
				if (character.isGrounded) {
					rigidbody.velocity += transform.up*jumpVelocity;
					rigidbody.velocity += transform.TransformDirection(desiredMoveDir)*jumpDirectionalFactor;
					transform.position += transform.up*character.footOffset;//new Vector3(0,character.footOffset,0);
					character.isGrounded = false;
					wantsToJump = false;
					isJumping = true;
					lastJumpTime = Time.timeSinceLevelLoad;
				}
				else {
					wantsToJump = true;
				}
			}
			if (character.isGrounded) {
				wantsToJump = false;
				isJumping = false;
			
			}
			
			//Mouse looking
			yAngle -= Input.GetAxis("Mouse Y")*lookSpeed;
			yAngle = Mathf.Clamp(yAngle,360-90,360+90);
			cameraPivot.transform.localEulerAngles = new Vector3(yAngle,0,0);
			
			//Vector3 currentAngles = transform.eulerAngles;
			//currentAngles.y += Input.GetAxis("Mouse X")*lookSpeed;
			//transform.eulerAngles = new Vector3(0,currentAngles.y,0);
			transform.Rotate(0,Input.GetAxis("Mouse X")*lookSpeed,0);
		}
	}
	
	void FixedUpdate() {
		float actualSpeed = moveSpeed;
		if (!character.isGrounded) {
			actualSpeed *= airControl;
		}
		rigidbody.AddRelativeForce(desiredMoveDir*10.0f*actualSpeed);
		
		if (isJumping && Input.GetButton("Jump")) {
			if (Time.timeSinceLevelLoad-lastJumpTime <= jumpHoldTime && rigidbody.velocity.y > jumpVelocity*0.5f) {
				Debug.DrawRay(transform.position,new Vector3(0,-2,0));
				Vector3 velocity = rigidbody.velocity;
				velocity.y += jumpVelocity*jumpHoldFactor*Time.fixedDeltaTime;
				rigidbody.velocity = velocity;
			}
		}
	}
}
