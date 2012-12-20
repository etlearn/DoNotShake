using UnityEngine;
using System.Collections;

using System.IO;

using UnityEditor;

namespace UIDE.SettingsMenu {
	public class SettingsGroup:System.Object {
		[System.NonSerialized]
		public string title = "SettingsGroup";
		[System.NonSerialized]
		public float order = 0.0f;
		[System.NonSerialized]
		public Vector2 scroll;
		
		public UIDEEditor editor {
			get {
				return UIDEEditor.current;
			}
		}
		
		public string dataPath {
			get {
				return UIDEEditor.tmpDir+"SettingGroupData/"+GetType().Name+"/";
			}
		}
		public string defaultAssetPath {
			get {
				return dataPath+"DefaultData.asset";
			}
		}
		
		public virtual void Start() {
			
		}
		
		public virtual void OnSwitchTo() {
			
		}
		public virtual void OnSwitchFrom() {
			
		}
		
		public virtual void Update() {
			
		}
		public virtual void ActiveUpdate() {
			
		}
		
		public virtual void OnGUI(Rect groupRect) {
			
		}
		
		public virtual void Destroy() {
			
		}
		
		public T GetOrCreateDefaultDataAsset<T>() where T:UIDESettingsGroupData {
			T asset = GetDataAsset<T>(defaultAssetPath);
			if (asset == null) {
				if (!Directory.Exists(dataPath)) {
					Directory.CreateDirectory(dataPath);
				}
				asset = ScriptableObject.CreateInstance<T>();
				AssetDatabase.CreateAsset(asset, defaultAssetPath);
				AssetDatabase.SaveAssets();
			}
			return asset;
		}
		
		public T GetDefaultDataAsset<T>() where T:UIDESettingsGroupData {
			return GetDataAsset<T>(defaultAssetPath);
		}
		public T GetDataAsset<T>(string path) where T:UIDESettingsGroupData {
			UnityEngine.Object obj = UIDEEditor.LoadAsset(path,true);
			if (obj == null) {
				return default(T);
			}
			if (typeof(T).IsAssignableFrom(obj.GetType())) {
				return (T)System.Convert.ChangeType(obj,typeof(T));
			}
			return default(T);
		}
		
	}
}

