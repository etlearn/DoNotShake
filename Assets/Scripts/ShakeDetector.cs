using UnityEngine;
using System.Collections;

public class ShakeDetector:MonoBehaviour {
	public float outMagScaler = 1.0f;
	public float maxOutput = 3.0f;
	public float lowPassRate = 1.0f;
	
	public float shakeDetectionThreshold = 2.0f;
	
	private Vector3 lowPassValue = Vector3.zero;
	private Vector3 acceleration;
	private Vector3 deltaAcceleration;
	
	void Start() {
		lowPassValue = Input.acceleration;
	}
	
	void Update() {
		acceleration = Input.acceleration;
		deltaAcceleration = acceleration-lowPassValue;
		lowPassValue = Vector3.Lerp(lowPassValue, acceleration, lowPassRate*Time.deltaTime);
		
		if (deltaAcceleration.magnitude >= shakeDetectionThreshold){
			float output = deltaAcceleration.magnitude*outMagScaler;
			output = Mathf.Min(maxOutput,output);
			gameObject.SendMessage("OnShakeDevice",output,SendMessageOptions.DontRequireReceiver);
		}
		
	}
}
