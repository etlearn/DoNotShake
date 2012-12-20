using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Game:MonoBehaviour {
	public string worldName = "DefaultWorld";
	public string nextLevelName = "Level1";
	public string friendlyLevelName = "Level 1";
	public int requiredImportantCount = 0;
	public Vector3 gravity = new Vector3(0,-5,0);
	
	private PlayerController player;
	
	[HideInInspector]
	public List<EnemyInfo> allEnemyInfos = new List<EnemyInfo>();
	public Dictionary<Enemy,int> enemyInfoDict = new Dictionary<Enemy,int>();
	
	[HideInInspector]
	public List<Enemy> enemies = new List<Enemy>();
	[HideInInspector]
	public List<Explosion> explosions = new List<Explosion>();
	[HideInInspector]
	public bool gameHasStarted = false;
	[HideInInspector]
	public bool gameIsEnded = false;
	
	
	void Awake() {
		player = (PlayerController)FindObjectOfType(typeof(PlayerController));
		Physics.gravity = gravity;
	}
	
	void Start() {
		
	}
	
	void Update() {
		if (player) {
			if (Input.GetKeyDown(KeyCode.Space)) {
				player.ShakeJump(2.5f);
			}
		}
		if (gameHasStarted && !gameIsEnded && !HasActiveEnemies() && !HasExplosions()) {
			OnGameEnd();
		}
	}
	
	void OnGameEnd() {
		gameIsEnded = true;
	}
	
	void OnGUI() {
		if (gameIsEnded) {
			DrawGameEndGUI();
		}
	}
	
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
			Application.LoadLevel("LevelSelect");
		}
		
		if (GUILayout.Button("Retry")) {
			Application.LoadLevel(Application.loadedLevel);
		}
		
		if (GUILayout.Button("Next")) {
			GoToNextLevel();
		}
		
		GUILayout.FlexibleSpace();
		
		GUILayout.EndArea();
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
