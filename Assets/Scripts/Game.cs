using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Game:MonoBehaviour {
	public UIRoot endLevelUI;
	public Camera cam;
	public string worldName = "DefaultWorld";
	public string nextLevelName = "Level1";
	public string friendlyLevelName = "Level 1";
	public int requiredImportantCount = 0;
	public Vector3 gravity = new Vector3(0,-5,0);
	public Transform endgameCamPosition;
	
	public Rect camBounds = new Rect(0,0,0,0);
	
	private PlayerController player;
	
	[HideInInspector]
	public List<EnemyInfo> allEnemyInfos = new List<EnemyInfo>();
	public Dictionary<Enemy,int> enemyInfoDict = new Dictionary<Enemy,int>();
	
	[HideInInspector]
	public RocketLaunchArea rocketLaunchArea;
	[HideInInspector]
	public List<Enemy> enemies = new List<Enemy>();
	[HideInInspector]
	public List<Explosion> explosions = new List<Explosion>();
	[HideInInspector]
	public bool gameHasStarted = false;
	[HideInInspector]
	public bool gameIsEnded = false;
	[HideInInspector]
	public bool showEndGameUI = false;
	[HideInInspector]
	public UIPanel endLevelUIMainPanel;
	
	
	
	void Awake() {
		endLevelUI = (UIRoot)Instantiate(endLevelUI);
		if (cam == null) {
			cam = Camera.main;
		}
		player = (PlayerController)FindObjectOfType(typeof(PlayerController));
		if (player == null) {
			Debug.LogError("Player not found.");
		}
		
		rocketLaunchArea = (RocketLaunchArea)FindObjectOfType(typeof(RocketLaunchArea));
		if (rocketLaunchArea == null) {
			Debug.LogError("RocketLaunchArea not found.");
		}
		
		Physics.gravity = gravity;
	}
	
	void Start() {
		endLevelUI.gameObject.SetActiveRecursively(true);
		UIPanel[] allPanels = endLevelUI.GetComponentsInChildren<UIPanel>();
		if (allPanels.Length > 0) {
			endLevelUIMainPanel = allPanels[allPanels.Length-1];
		}
		endLevelUI.gameObject.SetActiveRecursively(false);
	}
	
	void Update() {
		if (player) {
			if (Input.GetKeyDown(KeyCode.Space)) {
				player.ShakeJump(2.5f);
			}
		}
		if (gameHasStarted && !gameIsEnded && !HasActiveEnemies() && !HasExplosions()) {
			StartCoroutine(OnGameEnd());
		}
	}
	
	IEnumerator OnGameEnd() {
		gameIsEnded = true;
		
		bool isSuccess = false;
		List<EnemyInfo> explodedImportantInfos = GetExplodedImportantEnemyInfos();
		if (explodedImportantInfos.Count >= requiredImportantCount) {
			isSuccess = true;
		}
		if (isSuccess) {
			iTween.MoveTo(cam.gameObject,iTween.Hash("position", endgameCamPosition.position, "easeType", "easeInOutSine", "time", 1.0f, "delay", 0.0f));
			
			yield return new WaitForSeconds(1.0f+1.0f);
			
			for (int i = 0; i < explodedImportantInfos.Count; i++) {
				ShootRocket(explodedImportantInfos[i]);
				yield return new WaitForSeconds(1.0f);
			}
		}
		
		yield return new WaitForSeconds(1.0f);
		
		showEndGameUI = true;
		endLevelUI.gameObject.SetActiveRecursively(true);
		endLevelUIMainPanel.transform.localPosition = new Vector3(0,500,0);
		iTween.MoveTo(endLevelUIMainPanel.gameObject,iTween.Hash("position", new Vector3(0,0,0), "islocal", true, "easeType", "easeInOutSine", "time", 1.0f, "delay", 0.0f));
		endLevelUIMainPanel.transform.localScale = new Vector3(0.5f,1.0f,1.0f);
		iTween.ScaleTo(endLevelUIMainPanel.gameObject,iTween.Hash("scale", new Vector3(1,1,1), "islocal", true, "easeType", "easeInOutSine", "time", 1.0f, "delay", 0.0f));
		//endLevelUIMainPanel.transform.localEulerAngles = new Vector3(90.0f,0.0f,0.0f);
		//iTween.RotateTo(endLevelUIMainPanel.gameObject,iTween.Hash("rotation", new Vector3(0,0,0), "islocal", true, "easeType", "easeInOutSine", "time", 1.0f, "delay", 0.0f));
	}
	
	void ShootRocket(EnemyInfo info) {
		Vector3 pos = rocketLaunchArea.GetRandomPosition();
		
		Vector3 targetPos = cam.transform.position;
		targetPos.z = 0.0f;
		targetPos += rocketLaunchArea.GetRandomOffset()*0.2f;
		
		Vector3 launchDir = (targetPos-pos).normalized;
		
		StarRocketThrust newRocket = (StarRocketThrust)Instantiate(info.winRocket,pos,Quaternion.identity);
		newRocket.transform.up = launchDir;
		
	}
	
	void OnGUI() {
		//if (gameIsEnded && showEndGameUI) {
		//	DrawGameEndGUI();
		//}
	}
	
	/*
	void DrawGameEndGUI() {
		Rect mainRect = new Rect(0,0,400,300);
		mainRect.x = Screen.width*0.5f-mainRect.width*0.5f;
		mainRect.y = Screen.height*0.5f-mainRect.height*0.5f;
		
		GUI.Box(mainRect,"");
		
		List<EnemyInfo> importantInfos = GetImportantEnemyInfos();
		List<EnemyInfo> explodedImportantInfos = GetExplodedImportantEnemyInfos();
		
		bool isSuccess = false;
		if (explodedImportantInfos.Count >= requiredImportantCount) {
			isSuccess = true;
		}
		
		GUIStyle successStyle = new GUIStyle(GUI.skin.label);
		successStyle.fontSize = 20;
		successStyle.fontStyle = FontStyle.Bold;
		
		if (isSuccess) {
			GUILayout.Label("Success!",successStyle);
		}
		else {
			GUILayout.Label("Fail",successStyle);
		}
		
		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.fontSize = 20;
		style.fontStyle = FontStyle.Bold;
		
		string str = "Exploded "+explodedImportantInfos.Count+" out of "+importantInfos.Count+" important bombs";
		
		GUILayout.BeginArea(mainRect);
		
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		
		GUILayout.FlexibleSpace();
		GUILayout.Label(str,style);
		GUILayout.FlexibleSpace();
		
		GUILayout.EndHorizontal();
		
		if (GUILayout.Button("Level Select")) {
			GoToLevelSelect();
		}
		
		if (GUILayout.Button("Retry")) {
			RetryLevel();
		}
		
		if (GUILayout.Button("Next")) {
			GoToNextLevel();
		}
		
		GUILayout.FlexibleSpace();
		
		GUILayout.EndArea();
	}
	*/
	
	public void GoToLevelSelect() {
		Application.LoadLevel("LevelSelect");
	}
	
	public void RetryLevel() {
		Application.LoadLevel(Application.loadedLevel);
	}
	
	public void OnShakeDevice(float magnitude) {
		if (player) {
			player.ShakeJump(magnitude);
		}
	}
	
	public void GoToNextLevel() {
		Application.LoadLevel(nextLevelName);
	}
	
	public void StartGame() {
		gameHasStarted = true;
	}
	
	public bool HasActiveEnemies() {
		bool state = false;
		for (int i = 0; i < enemies.Count; i++) {
			if (enemies[i].isActivated) {
				state = true;
			}
		}
		return state;
	}
	
	public bool HasExplosions() {
		return explosions.Count > 0;
	}
	
	public List<EnemyInfo> GetExplodedImportantEnemyInfos() {
		List<int> indexes = new List<int>();
		for (int i = 0; i < enemies.Count; i++) {
			int index = GetEnemyInfoIndexByEnemy(enemies[i]);
			indexes.Add(index);
		}
		
		List<EnemyInfo> outputInfos = new List<EnemyInfo>();
		for (int i = 0; i < allEnemyInfos.Count; i++) {
			if (indexes.Contains(i)) continue;
			if (!allEnemyInfos[i].isImportant) continue;
			outputInfos.Add(allEnemyInfos[i]);
		}
		return outputInfos;
	}
	
	public List<EnemyInfo> GetImportantEnemyInfos() {
		List<EnemyInfo> infos = new List<EnemyInfo>();
		for (int i = 0; i < allEnemyInfos.Count; i++) {
			if (allEnemyInfos[i].isImportant) {
				infos.Add(allEnemyInfos[i]);
			}
		}
		return infos;
	}
	
	public List<EnemyInfo> GetUnimportantEnemyInfos() {
		List<EnemyInfo> infos = new List<EnemyInfo>();
		for (int i = 0; i < allEnemyInfos.Count; i++) {
			if (!allEnemyInfos[i].isImportant) {
				infos.Add(allEnemyInfos[i]);
			}
		}
		return infos;
	}
	
	public EnemyInfo GetEnemyInfoByEnemy(Enemy enemy) {
		int index = GetEnemyInfoIndexByEnemy(enemy);
		EnemyInfo info = null;
		if (index != -1) {
			info = allEnemyInfos[index];
		}
		return info;
	}
	public int GetEnemyInfoIndexByEnemy(Enemy enemy) {
		int index = -1;
		enemyInfoDict.TryGetValue(enemy, out index);
		return index;
	}
	
	public void AddEnemy(Enemy enemy) {
		if (!enemies.Contains(enemy)) {
			enemies.Add(enemy);
			if (enemy.enemyInfo) {
				allEnemyInfos.Add(enemy.enemyInfo);
				enemyInfoDict.Add(enemy,allEnemyInfos.Count-1);
			}
		}
	}
	public void RemoveEnemy(Enemy enemy) {
		enemies.Remove(enemy);
	}
	
	public void AddExplosion(Explosion explosion) {
		if (!explosions.Contains(explosion)) {
			explosions.Add(explosion);
		}
	}
	public void RemoveExplosion(Explosion explosion) {
		explosions.Remove(explosion);
	}
	
}
