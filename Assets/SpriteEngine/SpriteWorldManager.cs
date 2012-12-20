using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

[ExecuteInEditMode]
public class SpriteWorldManager : MonoBehaviour {
	private static SpriteWorldManager _current;
	public static SpriteWorldManager current {
		get {
			if (!_current) {
				_current = (SpriteWorldManager)FindObjectOfType(typeof(SpriteWorldManager));
				if (!_current) {
					_current = (new GameObject("_SpriteWorldManager")).AddComponent<SpriteWorldManager>();
				}
			}
			return _current;
		}
	}
	[System.NonSerialized]
	//public Dictionary<Mesh,Sprite> meshDictionary = new Dictionary<Mesh, Sprite>();
	public float smallestSpritePointAccuracy = 0.001f;
	public Vector3 gridSize = new Vector3(1,1,1);
	public int editorUpdateRate = 1;
	
	private bool wantsFullRebuild = false;
	
	#if UNITY_EDITOR
	private FileSystemWatcher sceneFileWatcher;
	#endif
	
	public void OnEnable() {
		#if UNITY_EDITOR
		if (Application.isEditor) {
			sceneFileWatcher = new FileSystemWatcher("Assets", "*.unity");
			sceneFileWatcher.IncludeSubdirectories = true;
			sceneFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
			sceneFileWatcher.EnableRaisingEvents = true; 
			sceneFileWatcher.Changed += OnSceneFileWatcher_Changed;
		}
		#endif
	}
	
	void LateUpdate() {
		if (wantsFullRebuild) {
			RebuildAllSprites();
		}
	}
	
	void OnDrawGizmos() {
		if (wantsFullRebuild) {
			RebuildAllSprites();
		}
	}
	
	#if UNITY_EDITOR
	void OnSceneFileWatcher_Changed(object sender, FileSystemEventArgs e) {
		wantsFullRebuild = true;
	}
	#endif
	
	void RebuildAllSprites() {
		Sprite[] sprites = (Sprite[])FindObjectsOfType(typeof(Sprite));
		foreach (Sprite sprite in sprites) {
			sprite.Rebuild();
		}
		wantsFullRebuild = false;
	}
}
