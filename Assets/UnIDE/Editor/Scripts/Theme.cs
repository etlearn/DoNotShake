using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

//using UnityEditor;

namespace UIDE {
	[System.Serializable]
	public class Theme:System.Object {
		static public string themesFolder = "Assets/UnIDE/Editor/Themes/";
		public UIDEEditor editor;
		public string themePath;
		public GUISkin skin;
		public List<string[]> fontUpdateStyleNames = new List<string[]>();
		
		static private Texture2D _invisible16x16Tex;
		static public Texture2D invisible16x16Tex {
			get {
				if (!_invisible16x16Tex) {
					_invisible16x16Tex = new Texture2D(16,16,TextureFormat.ARGB32,false);
					_invisible16x16Tex.hideFlags = HideFlags.HideAndDontSave;
					for (int x = 0; x < _invisible16x16Tex.width; x++) {
						for (int y = 0; y < _invisible16x16Tex.height; y++) {
							_invisible16x16Tex.SetPixel(x,y,new Color(0,0,0,0));
						}
					}
					_invisible16x16Tex.Apply();
				}
				return _invisible16x16Tex;
			}
		}
		
		public string fullThemePath {
			get {
				return themesFolder+themePath;
			}
		}
		
		public Dictionary<string,Texture2D> resourceTextures = new Dictionary<string,Texture2D>();
		public Dictionary<string,Texture2D> languageIcons = new Dictionary<string,Texture2D>();
		
		
		public virtual void Start() {
			
		}
		
		public virtual void OnPreTextEditorGUI(Rect rect) {
			
		}
		public virtual void OnPostTextEditorGUI(Rect rect) {
			
		}
		
		public virtual void InitializeTokenDefs() {
			UIDETokenDefs.tokenDefsHash = new UIDEHashTable();
		}
		
		public virtual void OnDefaultSkinSet(GUISkin setSkin) {
			skin.verticalScrollbar = new GUIStyle(setSkin.verticalScrollbar);
			skin.verticalScrollbarThumb = new GUIStyle(setSkin.verticalScrollbarThumb);
			skin.verticalScrollbarUpButton = new GUIStyle(setSkin.verticalScrollbarUpButton);
			skin.verticalScrollbarDownButton = new GUIStyle(setSkin.verticalScrollbarDownButton);
			
			skin.horizontalScrollbar = new GUIStyle(setSkin.horizontalScrollbar);
			skin.horizontalScrollbarThumb = new GUIStyle(setSkin.horizontalScrollbarThumb);
			skin.horizontalScrollbarLeftButton = new GUIStyle(setSkin.horizontalScrollbarLeftButton);
			skin.horizontalScrollbarRightButton = new GUIStyle(setSkin.horizontalScrollbarRightButton);
			
			skin.horizontalSlider = new GUIStyle(setSkin.horizontalSlider);
			skin.horizontalSliderThumb = new GUIStyle(setSkin.horizontalSliderThumb);
			
			skin.verticalSlider = new GUIStyle(setSkin.verticalSlider);
			skin.verticalSliderThumb = new GUIStyle(setSkin.verticalSliderThumb);
		}
		/*
		public virtual void UpdateFontSize(int size) {
			if (skin == null) {
				Debug.LogWarning("Tried to set font size before Theme.skin has been set, or Theme.skin is null.");
			}
			for (int i = 0; i < fontUpdateStyleNames.Count; i++) {
				GUIStyle style = GetStyle(fontUpdateStyleNames[i][0]);
				if (style != null) {
					style.fontSize = size;
				}
			}
		}
		*/
		public virtual void UpdateFont(Font font, Font boldFont) {
			if (skin == null) {
				Debug.LogWarning("Tried to set font before Theme.skin has been set, or Theme.skin is null.");
			}
			for (int i = 0; i < fontUpdateStyleNames.Count; i++) {
				GUIStyle style = GetStyle(fontUpdateStyleNames[i][0]);
				if (style != null) {
					
					if (boldFont != null && fontUpdateStyleNames[i][1].ToLower() == "bold") {
						style.font = boldFont;
					}
					else {
						style.font = font;
					}
				}
			}
			UIDEEditor.SetDirty(skin);
		}
		
		public virtual void Destroy() {
			UnloadLanguageIcons();
			UnloadResourceTextures();
		}
		
		public GUIStyle GetStyle(string styleName) {
			if (skin == null) {
				Debug.LogError("Theme.skin is null.");
			}
			return skin.GetStyle(styleName);
		}
		
		protected void UnloadLanguageIcons() {
			foreach (KeyValuePair<string, Texture2D> pair in languageIcons) {
				Resources.UnloadAsset(pair.Value);
			}
		}
		protected void UnloadResourceTextures() {
			foreach (KeyValuePair<string, Texture2D> pair in languageIcons) {
				Resources.UnloadAsset(pair.Value);
			}
		}
		
		protected void LoadResourceTextures() {
			resourceTextures = new Dictionary<string,Texture2D>();
			Texture2D[] lIcons = LoadTexturesInDirectory(fullThemePath+"ResourceTextures/");
			foreach (Texture2D tex in lIcons) {
				resourceTextures.Add(tex.name,tex);
			}
		}
		public Texture2D GetResourceTexture(string texName) {
			Texture2D tex = null;
			resourceTextures.TryGetValue(texName,out tex);
			return tex;
		}
		
		protected void LoadDefaultLanguageIcons() {
			LoadLanguageIconsFromDirectory(themesFolder+"LanguageIcons/");
		}
		protected void LoadLanguageIconsFromDirectory(string path) {
			languageIcons = new Dictionary<string,Texture2D>();
			Texture2D[] lIcons = LoadTexturesInDirectory(path);
			foreach (Texture2D tex in lIcons) {
				languageIcons.Add(tex.name,tex);
			}
		}
		public Texture2D GetLanguageIcon(string iconName) {
			Texture2D tex = null;
			languageIcons.TryGetValue(iconName,out tex);
			return tex;
		}
		
		static public Texture2D[] LoadTexturesInDirectory(string path) {
			if (!Directory.Exists(path)) {
				Debug.LogWarning(path+" does not exist.");
				return new Texture2D[0];
			}
			string[] fileNames = Directory.GetFiles(path,"*.*");
			List<Texture2D> textures = new List<Texture2D>();
			foreach (string fn in fileNames) {
				if (fn.ToLower().EndsWith(".png") || fn.ToLower().EndsWith(".psd")) {
					UnityEngine.Object obj = UIDEEditor.LoadAsset(fn);
					
					if (obj.GetType() != typeof(Texture2D)) {
						Resources.UnloadAsset(obj);
						//GameObject.DestroyImmediate(obj);
						continue;
					}
					
					Texture2D tex = (Texture2D)obj;
					textures.Add(tex);
				}
			}
			return textures.ToArray();
		}
		
	}
}