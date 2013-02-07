using UnityEngine;
using System.Collections;

public class MainMenu:MonoBehaviour {
	public RocketLaunchArea rocketLaunchArea;
	public StarRocketThrust rocketPrefab;
	public float rocketForceMultiplyerMin = 1.0f;
	public float rocketForceMultiplyerMax = 1.0f;
	
	public float minRocketSpawnTime = 0.25f;
	public float maxRocketSpawnTime = 4.0f;
	
	public string levelSelectSceneName = "LevelSelect";
	
	float nextSpawnTime = 0.0f;
	
	public void OnClickPlay() {
		Application.LoadLevel(levelSelectSceneName);
		nextSpawnTime = Time.timeSinceLevelLoad+Random.Range(minRocketSpawnTime,maxRocketSpawnTime);
	}
	
	public void Update() {
		if (Time.timeSinceLevelLoad >= nextSpawnTime) {
			SpawnRocket();
		}
		
		
	}
	
	public void SpawnRocket() {
		Vector3 pos = rocketLaunchArea.GetRandomPosition();
		
		Vector3 targetPos = rocketLaunchArea.transform.position;
		targetPos.y += 10.0f;
		targetPos += rocketLaunchArea.GetRandomOffset()*0.5f;
		
		Vector3 launchDir = (targetPos-pos).normalized;
		
		StarRocketThrust newRocket = (StarRocketThrust)Instantiate(rocketPrefab,pos,Quaternion.identity);
		newRocket.transform.up = launchDir;
		
		newRocket.transform.Rotate(0,180,0);
		newRocket.transform.Rotate(0,Random.Range(-45.0f,45.0f),0);
		
		newRocket.rigidbody.maxAngularVelocity = 100.0f;
		newRocket.rigidbody.AddRelativeTorque(0,Random.Range(-1.0f,1.0f),0,ForceMode.Impulse);
		
		newRocket.force *= Random.Range(rocketForceMultiplyerMin,rocketForceMultiplyerMax);
		
		nextSpawnTime = Time.timeSinceLevelLoad+Random.Range(minRocketSpawnTime,maxRocketSpawnTime);
	}
}
