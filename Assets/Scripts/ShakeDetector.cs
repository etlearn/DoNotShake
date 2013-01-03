using UnityEngine;
using System.Collections;

public class ShakeDetector:MonoBehaviour {
	public float outMagScaler = 1.0f;
	public float maxOutput = 3.0f;
	public float lowPassRate = 1.0f;
	
	public float accelerometerUpdateIntervalRate = 30.0f;
	
	// The greater the value of LowPassKernelWidthInSeconds, the slower the filtered value will converge towards current input sample (and vice versa).
	public float lowPassKernelWidthInSeconds = 1.0f;
	
	// This next parameter is initialized to 2.0 per Apple's recommendation, or at least according to Brady! ;)
	public float shakeDetectionThreshold = 2.0f;
	
	private float accelerometerUpdateInterval;
	private float lowPassFilterFactor; 
	private Vector3 lowPassValue = Vector3.zero;
	private Vector3 acceleration;
	private Vector3 deltaAcceleration;
	
	void Start() {
		accelerometerUpdateInterval = 1.0f/accelerometerUpdateIntervalRate;
		lowPassFilterFactor = accelerometerUpdateInterval/lowPassKernelWidthInSeconds;
		//shakeDetectionThreshold *= shakeDetectionThreshold;
		lowPassValue = Input.acceleration;
	}
	
	void Update() {
		acceleration = Input.acceleration;
		deltaAcceleration = acceleration-lowPassValue;
		lowPassValue = Vector3.Lerp(lowPassValue, acceleration, lowPassRate*Time.deltaTime);
		
		if (deltaAcceleration.magnitude >= shakeDetectionThreshold){
			// Perform your "shaking actions" here, with suitable guards in the if check above, if necessary to not, to not fire again if they're already being performed.
			float output = deltaAcceleration.magnitude*outMagScaler;
			output = Mathf.Min(maxOutput,output);
			gameObject.SendMessage("OnShakeDevice",output,SendMessageOptions.DontRequireReceiver);
			//Debug.Log("Shake event detected at time "+Time.time);
		}
		
		/*
		acceleration = Input.acceleration;
		lowPassValue = Vector3.Lerp(lowPassValue, acceleration, lowPassFilterFactor);
		deltaAcceleration = acceleration-lowPassValue;
		if (deltaAcceleration.sqrMagnitude >= shakeDetectionThreshold){
			// Perform your "shaking actions" here, with suitable guards in the if check above, if necessary to not, to not fire again if they're already being performed.
			gameObject.SendMessage("OnShakeDevice",deltaAcceleration.sqrMagnitude,SendMessageOptions.DontRequireReceiver);
			Debug.Log("Shake event detected at time "+Time.time);
		}
		*/
	}
}
