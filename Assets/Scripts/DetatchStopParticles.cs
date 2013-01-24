using UnityEngine;
using System.Collections;

public class DetatchStopParticles : MonoBehaviour {
	public bool destroyObject = false;
	public float destroyDelay = 1.0f;
	public void OnDetatched() {
		ParticleSystem[] particles = gameObject.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < particles.Length; i++) {
			particles[i].Stop();
		}
		if (destroyObject) {
			Destroy(gameObject,destroyDelay);
		}
	}
}
