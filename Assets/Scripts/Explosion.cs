using UnityEngine;
using System.Collections;

public class Explosion : MonoBehaviour {
	public float radius = 2.0f;
	public float power = 1.0f;
	public float expandRate = 1.0f;
	public bool detachChildren = false;
	
	public float explosionTime {
		get {
			return (Time.timeSinceLevelLoad-startTime);
		}
	}
	public float expandedRatio {
		get {
			return explosionTime/expandRate;
		}
	}
	public float currentRadius {
		get {
			return Mathf.Lerp(0,radius,expandedRatio);
		}
	}
	private SphereCollider sphereCollider;
	private float startTime = 0.0f;
	
	private Game _game;
	public Game game {
		get {
			if (!_game) {
				_game = (Game)FindObjectOfType(typeof(Game));
			}
			return _game;
		}
	}
	
	void Awake() {
		if (game) {
			game.AddExplosion(this);
		}
	}
	
	void Start() {
		startTime = Time.timeSinceLevelLoad;
	}
	
	void Update() {
		if (expandedRatio > 1.0f) {
			DestroyExplosion();
		}
	}
	
	void FixedUpdate() {
		Collider[] colliders = Physics.OverlapSphere(transform.position,currentRadius);
		
		for (int i = 0; i < colliders.Length; i++) {
			Exploder otherExploder = colliders[i].gameObject.GetComponent<Exploder>();
			Enemy otherEnemy = colliders[i].gameObject.GetComponent<Enemy>();
			Rigidbody rb = colliders[i].gameObject.GetComponent<Rigidbody>();
			
			if (rb && !rb.isKinematic) {
				Vector3 closestPoint = colliders[i].ClosestPointOnBounds(transform.position);
				Vector3 forceVector = (closestPoint-transform.position).normalized;
				float dist = Vector3.Distance(closestPoint,transform.position);
				float forceToApply = (1.0f-dist/radius)*power;
				forceToApply *= 25.0f;
				rb.AddForce(forceVector*forceToApply);
			}
			
			if (otherEnemy) {
				if (!otherEnemy.isActivated) {
					otherEnemy.Activate();
				}
			}
			else if (otherExploder) {
				otherExploder.Explode();
			}
		}
	}
	
	void OnDrawGizmos() {
		Gizmos.color = new Color(1,0,0,1);
		Gizmos.DrawWireSphere(transform.position,currentRadius);
	}
	
	void OnDestroy() {
		if (game) {
			game.RemoveExplosion(this);
		}
	}
	
	public void DestroyExplosion() {
		if (detachChildren) {
			transform.DetachChildren();
		}
		Destroy(gameObject);
	}
	
}
