using UnityEngine;
using System.Collections;

[System.Serializable]
public class BezierSpline:System.Object {
	public bool expanded = false;
	public bool linear = true;
	public bool useCustomDistance = false;
	public Transform transform;
	
	
	public Vector3 localPoint0 = Vector3.zero;
	public Vector3 point0 {
		get {
			if (transform) {
				return transform.TransformPoint(localPoint0);
			}
			return localPoint0;
		}
		set {
			if (transform) {
				localPoint0 = transform.InverseTransformPoint(value);
			}
			else {
				localPoint0 = value;
			}
		}
	}
	
	public Vector3 localPoint1 = Vector3.zero;
	public Vector3 point1 {
		get {
			if (transform) {
				return transform.TransformPoint(localPoint1);
			}
			return localPoint1;
		}
		set {
			if (transform) {
				localPoint1 = transform.InverseTransformPoint(value);
			}
			else {
				localPoint1 = value;
			}
		}
	}
	
	public Vector3 localNormal0 = Vector3.up;
	public Vector3 normal0 {
		get {
			if (linear) {
				return (point1-point0).normalized;
			}
			if (transform) {
				return transform.TransformDirection(localNormal0);
			}
			return localNormal0;
		}
		set {
			if (value.magnitude == 0.0f) return;
			if (transform) {
				localNormal0 = transform.InverseTransformDirection(value.normalized);
			}
			else {
				localNormal0 = value.normalized;
			}
		}
	}
	
	public Vector3 localNormal1 = Vector3.up;
	public Vector3 normal1 {
		get {
			if (linear) {
				return (point0-point1).normalized;
			}
			if (transform) {
				return transform.TransformDirection(localNormal1);
			}
			return localNormal1;
		}
		set {
			if (value.magnitude == 0.0f) return;
			if (transform) {
				localNormal1 = transform.InverseTransformDirection(value.normalized);
			}
			else {
				localNormal1 = value.normalized;
			}
		}
	}
	public float customDistance0 = 1.0f/3.0f;
	public float customDistance1 = 1.0f/3.0f;
	
	public float distance0 {
		get {
			if (useCustomDistance && !linear) {
				return customDistance0;
			}
			return 1.0f/3.0f;
		}
	}
	public float distance1 {
		get {
			if (useCustomDistance && !linear) {
				return customDistance1;
			}
			return 1.0f/3.0f;
		}
	}
	
	public Vector3 localTangetPos0 {
		get {
			float pointDist = Vector3.Distance(localPoint0,localPoint1);
			float d = distance0;
			//if (linear) {
			//	d = 1.0f/3.0f;
			//}
			float pActualDist = d*pointDist;
			Vector3 n = localNormal0.normalized;
			if (linear) {
				n = (localPoint1-localPoint0).normalized;
			}
			Vector3 p = localPoint0;
			
			p += n*pActualDist;
			return p;
		}
		set {
			normal0 = (value-point0).normalized;
		}
	}
	public Vector3 localTangetPos1 {
		get {
			float pointDist = Vector3.Distance(localPoint0,localPoint1);
			float d = distance1;
			//if (linear) {
			//	d = 1.0f/3.0f;
			//}
			float pActualDist = d*pointDist;
			Vector3 n = localNormal1.normalized;
			if (linear) {
				n = (localPoint0-localPoint1).normalized;
			}
			Vector3 p = localPoint1;
			
			p += n*pActualDist;
			return p;
		}
		set {
			normal1 = (value-point1).normalized;
		}
	}
	
	public Vector3 tangetPos0 {
		get {
			float pointDist = Vector3.Distance(point0,point1);
			float d = distance0;
			//if (linear) {
			//	d = 1.0f/3.0f;
			//}
			float pActualDist = d*pointDist;
			Vector3 n = normal0.normalized;
			Vector3 p = point0;
			
			p += n*pActualDist;
			return p;
		}
		set {
			normal0 = (value-point0).normalized;
		}
	}
	public Vector3 tangetPos1 {
		get {
			float pointDist = Vector3.Distance(point0,point1);
			float d = distance1;
			//if (linear) {
			//	d = 1.0f/3.0f;
			//}
			float pActualDist = d*pointDist;
			Vector3 n = normal1.normalized;
			Vector3 p = point1;
			
			p += n*pActualDist;
			return p;
		}
		set {
			normal1 = (value-point1).normalized;
		}
	}
	
	public void SetLinearTangents() {
		SetLinearTangents0();
		SetLinearTangents1();
	}
	public void SetLinearTangents0() {
		localNormal0 = (localPoint1-localPoint0).normalized;
	}
	public void SetLinearTangents1() {
		localNormal1 = (localPoint0-localPoint1).normalized;
	}
	
	public Vector3 GetPoint(float t) {
		Vector3 a,b,c,p = new Vector3(0,0,0);
		
		float pointDist = Vector3.Distance(point0,point1);
		float p0ActualDist = distance0*pointDist;
		float p1ActualDist = distance1*pointDist;
		
		Vector3 n0 = normal0.normalized;
		Vector3 n1 = normal1.normalized;
		
		Vector3 p0 = point0;
		Vector3 p1 = point0+n0*p0ActualDist;
		Vector3 p2 = point1+n1*p1ActualDist;
		Vector3 p3 = point1;
		
		c.x = 3.0f * (p1.x - p0.x);
		c.y = 3.0f * (p1.y - p0.y);
		c.z = 3.0f * (p1.z - p0.z);
		b.x = 3.0f * (p2.x - p1.x) - c.x;
		b.y = 3.0f * (p2.y - p1.y) - c.y;
		b.z = 3.0f * (p2.z - p1.z) - c.z;
		a.x = p3.x - p0.x - c.x - b.x;
		a.y = p3.y - p0.y - c.y - b.y;
		a.z = p3.z - p0.z - c.z - b.z;
		
		p.x = a.x * t * t * t + b.x * t * t + c.x * t + p0.x;
		p.y = a.y * t * t * t + b.y * t * t + c.y * t + p0.y;
		p.z = a.z * t * t * t + b.z * t * t + c.z * t + p0.z;
		
		return p;
	}
	public Vector3 GetPointLocal(float t) {
		Vector3 a,b,c,p = new Vector3(0,0,0);
		
		float pointDist = Vector3.Distance(localPoint0,localPoint1);
		float p0ActualDist = distance0*pointDist;
		float p1ActualDist = distance1*pointDist;
		
		Vector3 n0 = localNormal0.normalized;
		Vector3 n1 = localNormal1.normalized;
		if (linear) {
			n0 = (localPoint1-localPoint0).normalized;
			n1 = (localPoint0-localPoint1).normalized;
		}
		
		Vector3 p0 = localPoint0;
		Vector3 p1 = localPoint0+n0*p0ActualDist;
		Vector3 p2 = localPoint1+n1*p1ActualDist;
		Vector3 p3 = localPoint1;
		
		c.x = 3.0f * (p1.x - p0.x);
		c.y = 3.0f * (p1.y - p0.y);
		c.z = 3.0f * (p1.z - p0.z);
		b.x = 3.0f * (p2.x - p1.x) - c.x;
		b.y = 3.0f * (p2.y - p1.y) - c.y;
		b.z = 3.0f * (p2.z - p1.z) - c.z;
		a.x = p3.x - p0.x - c.x - b.x;
		a.y = p3.y - p0.y - c.y - b.y;
		a.z = p3.z - p0.z - c.z - b.z;
		
		p.x = a.x * t * t * t + b.x * t * t + c.x * t + p0.x;
		p.y = a.y * t * t * t + b.y * t * t + c.y * t + p0.y;
		p.z = a.z * t * t * t + b.z * t * t + c.z * t + p0.z;
		
		return p;
	}
	static public Vector3 Interpolate(float t,Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
		Vector3 a,b,c,p = new Vector3(0,0,0);
		
		c.x = 3.0f * (p1.x - p0.x);
		c.y = 3.0f * (p1.y - p0.y);
		c.z = 3.0f * (p1.z - p0.z);
		b.x = 3.0f * (p2.x - p1.x) - c.x;
		b.y = 3.0f * (p2.y - p1.y) - c.y;
		b.z = 3.0f * (p2.z - p1.z) - c.z;
		a.x = p3.x - p0.x - c.x - b.x;
		a.y = p3.y - p0.y - c.y - b.y;
		a.z = p3.z - p0.z - c.z - b.z;
		
		p.x = a.x * t * t * t + b.x * t * t + c.x * t + p0.x;
		p.y = a.y * t * t * t + b.y * t * t + c.y * t + p0.y;
		p.z = a.z * t * t * t + b.z * t * t + c.z * t + p0.z;
		
		return p;
	}
	
	static public Vector3 GetThirdTrianglePoint(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 newP0,Vector3 newP1) {
		float distance = Vector3.Distance(p0,p1);
		Vector3 normal = (p1-p0).normalized;
		Vector3 cross = Vector3.Cross(normal,new Vector3(0,0,-1));
		Plane plane = new Plane(cross,p0);
		float planeDistance = plane.GetDistanceToPoint(p2);
		Vector3 projectedPoint = p2-cross*planeDistance;
		
		float p0ProjectedDistance = new Plane(normal,p0).GetDistanceToPoint(projectedPoint);
		float projectedPointRatio = p0ProjectedDistance/distance;
		
		Vector3 newNormal = (newP1-newP0).normalized;
		float newDistance = Vector3.Distance(newP1,newP0);
		float newDistanceRatio = newDistance/distance;
		Vector3 newCross = Vector3.Cross(newNormal,new Vector3(0,0,-1));
		Vector3 newPoint = newP0+newNormal*newDistance*projectedPointRatio;
		newPoint += newCross*planeDistance*newDistanceRatio;
		return newPoint;
	}
}
