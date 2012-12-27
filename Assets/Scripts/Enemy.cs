using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.SerializableAttribute]
public class AttachObjectInfo:System.Object {
	public GameObject obj;
	public float scaleFactor = 1.0f;
	public Transform targetParent;
	[HideInInspector]
	public GameObject instance;
}

public class Enemy:Actor {
	public EnemyInfo enemyInfo;
	public AttachObjectInfo[] fuseEffects = new AttachObjectInfo[0];
	public float explodeDelay = 0.0f;
	public float explodeAnimationWeight = 0.1f;
	public float ambientOverlayRateMin = 2.0f;
	public float ambientOverlayRateMax = 5.0f;
	public string[] ambientOverlayAnimations = new string[0];
	
	[HideInInspector]
	public bool isActivated = false;
	
	public float explodeAnimLength {
		get {
			if (anim == null) return 0;
			AnimationClip clip = anim.GetClip("explode");
			if (clip == null) return 0;
			return clip.length;
		}
	}
	
	private AIMovement _movement;
	public AIMovement movement {
		get {
			if (!_movement) {
				_movement = gameObject.GetComponent<AIMovement>();
			}
			return _movement;
		}
	}
	
	private float nextAmbientOverlayTime = 0.0f;
	
	private bool hasStartedExplosionAnim = false;
	private float activationTime = 0.0f;
	
	void Start() {
		if (game) {
			game.AddEnemy(this);
		}
		if (anim) {
			anim.wrapMode = WrapMode.Loop;
			anim.Play("idle");
		}
		nextAmbientOverlayTime = GetNextRandomOverlayTime();
		nextAmbientOverlayTime = Random.Range(0.0f,ambientOverlayRateMax);
	}
	
	void Update() {
		bool shouldPlayWalkAnim = false;
		if (character && character.isGrounded) {
			if (rigidbody.velocity.magnitude > 0.5f) {
				shouldPlayWalkAnim = true;
			}
		}
		
		if (ambientOverlayAnimations.Length > 0 && Time.timeSinceLevelLoad >= nextAmbientOverlayTime) {
			string animName = ambientOverlayAnimations[Random.Range(0,ambientOverlayAnimations.Length)];
			PlayAmbientOverlayAnimation(animName,1.0f,1.0f);
			nextAmbientOverlayTime = GetNextRandomOverlayTime();
		}
			
		if (!isActivated) {
			if (shouldPlayWalkAnim) {
				PlayAnimation("walk",1.0f);
			}
			else {
				PlayAnimation("idle",1.0f);
			}
		}
		
		if (isActivated) {
			if (!hasStartedExplosionAnim) {
				if (Time.timeSinceLevelLoad-activationTime >= explodeDelay-explodeAnimLength) {
					float startTime = Mathf.Max(explodeAnimLength-explodeDelay,0.0f);
					
					AnimationState explodeState = anim["explode"];
					explodeState.weight = explodeAnimationWeight;
					explodeState.wrapMode = WrapMode.ClampForever;
					explodeState.layer = 10;
					explodeState.blendMode = AnimationBlendMode.Additive;
					explodeState.time = startTime;
					explodeState.enabled = true;
					
					hasStartedExplosionAnim = true;
				}
			}
			if (Time.timeSinceLevelLoad-activationTime >= explodeDelay) {
				Explode();
			}
		}
	}
	
	void FixedUpdate() {
		if (movement) {
			movement.UpdateMovement();
		}
	}
	
	void OnDestroy() {
		if (game) {
			game.RemoveEnemy(this);
		}
	}
	
	public float GetNextRandomOverlayTime() {
		float v = Random.Range(ambientOverlayRateMin,ambientOverlayRateMax);
		return Time.timeSinceLevelLoad+v;
	}
	
	public void PlayAmbientOverlayAnimation(string animName, float speed, float weight) {
		AnimationState state = anim[animName];
		if (state == null) return;
		state.weight = weight;
		state.wrapMode = WrapMode.Once;
		state.layer = 5;
		state.blendMode = AnimationBlendMode.Additive;
		state.time = 0;
		state.speed = speed;
		state.enabled = true;
	}
	
	public void Explode() {
		if (exploder) {
			exploder.Explode();
		}
	}
	
	public void Activate() {
		if (isActivated) return;
		isActivated = true;
		activationTime = Time.timeSinceLevelLoad;
		
		for (int i = 0; i < fuseEffects.Length; i++) {
			fuseEffects[i].instance = (GameObject)Instantiate(fuseEffects[i].obj);
			Transform t = fuseEffects[i].targetParent;
			if (t == null) {
				t = transform;
			}
			fuseEffects[i].instance.transform.parent = t;
			fuseEffects[i].instance.transform.localPosition = Vector3.zero;
			fuseEffects[i].instance.transform.localRotation = Quaternion.identity;
			fuseEffects[i].instance.transform.localScale = Vector3.one*fuseEffects[i].scaleFactor;
		}
	}
	
}
