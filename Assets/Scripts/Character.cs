using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Character:MonoBehaviour {
	
	public float footOffset = 0.1f;
	public float footAnchorRatio = 0.5f;
	public float stepSmoothing = 50.0f;
	
	public float groundedDrag = 5.0f;
	public float airLateralDrag = 0.1f;
	public bool isGrounded = false;
	public Vector3 groundPos;
	public Vector3 groundNormal;
	public Vector3 footNormal;
	public float dum;
	void Start() {
		
	}
	
	void Update() {
		
	}
	void FixedUpdate() {
		CapsuleCollider capsuleCollider = gameObject.GetComponent<CapsuleCollider>();
		
		RaycastHit[] hits;
		
		Vector3 down = -transform.up;
		Vector3 up = transform.up;
		
		float footRadius = capsuleCollider.radius*0.75f;
		Vector3 cPoint1 = transform.position+new Vector3(0,0,0);
		float shpherecastDistance = ((capsuleCollider.height*0.5f)-footRadius)+footOffset;
		hits = Physics.SphereCastAll(cPoint1,footRadius,down,shpherecastDistance);
		Vector3 shperecastEndPoint = cPoint1+(down*shpherecastDistance);
		
		Debug.DrawLine(cPoint1,shperecastEndPoint,new Color(1,1,0,1));
		Debug.DrawLine(cPoint1+(down*shpherecastDistance),shperecastEndPoint+new Vector3(footRadius,0,0),new Color(1,0,0,1));
		Debug.DrawLine(cPoint1+(down*shpherecastDistance),shperecastEndPoint+new Vector3(0,-footRadius,0),new Color(0,1,0,1));
		
		isGrounded = false;
		
		Vector3 characterPos = transform.position;
		if (hits.Length > 0) {
			RaycastHit bestRaycastHit = new RaycastHit();
			RaycastHit bestSpherecastHit = new RaycastHit();
			
			//Ray bestRay = new Ray(Vector3.zero,Vector3.zero);
			
			for (int i = 0; i < hits.Length; i++) {
				//float hitDif = characterPos.y-hits[i].point.y;
				RaycastHit hit;
				Vector3 rayOrigin = hits[i].point;
				rayOrigin += down*capsuleCollider.height;
				//rayOrigin.y = characterPos.y;
				//rayOrigin += up*capsuleCollider.height;
				//Vector3 rayOrigin = new Vector3(hits[i].point.x,characterPos.y-capsuleCollider.height,hits[i].point.z);
				
				Ray ray = new Ray(rayOrigin,up);
				
				if (capsuleCollider.Raycast(ray,out hit,capsuleCollider.height*8.0f)) {
					float hitDif = transform.InverseTransformPoint(hit.point).y-transform.InverseTransformPoint(hits[i].point).y;
					float bestHitDif = transform.InverseTransformPoint(bestRaycastHit.point).y-transform.InverseTransformPoint(bestSpherecastHit.point).y;
					
					if (i == 0 || hitDif < bestHitDif) {
						bestRaycastHit = hit;
						bestSpherecastHit = hits[i];
						//bestRay = ray;
					}
					
				}
			}
			
			isGrounded = true;
			groundPos = bestSpherecastHit.point;
			groundNormal = bestSpherecastHit.normal;
			footNormal = bestRaycastHit.normal;
			
			Vector3 localVelocity = transform.InverseTransformPoint(rigidbody.velocity);
			if (localVelocity.y <= 0.0f) {
				float charHitDif = transform.InverseTransformPoint(characterPos).y-transform.InverseTransformPoint(bestRaycastHit.point).y;
				//float hitDif = transform.InverseTransformPoint(bestSpherecastHit.point).y-transform.InverseTransformPoint(bestRaycastHit.point).y;
				
				Vector3 bestLocalPos = transform.InverseTransformPoint(bestSpherecastHit.point);
				//float bestPosDif = bestLocalPos.y;
				Vector3 newRootPos = bestLocalPos;
				newRootPos.x = 0;
				newRootPos.z = 0;
				newRootPos = transform.TransformPoint(newRootPos);
				
				
				Vector3 newPos = newRootPos;
				newPos += up*charHitDif;
				newPos += up*(footOffset*footAnchorRatio);
				
				transform.position = newPos;
			}
			
			Debug.DrawLine(bestSpherecastHit.point,bestRaycastHit.point,new Color(0,0,1,1));
			
		}
		
		if (isGrounded) {
			Vector3 vel = transform.InverseTransformDirection(rigidbody.velocity);
			vel.y = Mathf.Max(vel.y,0.0f);
			vel.x /= 1.0f+(groundedDrag*Time.deltaTime);
			vel.z /= 1.0f+(groundedDrag*Time.deltaTime);
			rigidbody.velocity = transform.TransformDirection(vel);
		}
		else {
			Vector3 vel = transform.InverseTransformDirection(rigidbody.velocity);
			vel.x /= 1.0f+(airLateralDrag*Time.deltaTime);
			vel.z /= 1.0f+(airLateralDrag*Time.deltaTime);
			rigidbody.velocity = transform.TransformDirection(vel);
		}
		
	}
}
