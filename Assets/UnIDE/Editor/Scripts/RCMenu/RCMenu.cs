using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UIDE.RightClickMenu {
	public class RCMenu:System.Object {
		static public RCMenu currentMenu;
		static private RCMenu menuToMakeCurrent;
		
		public Vector2 position;
		public List<RCMenuItem> items = new List<RCMenuItem>();
		public Vector2 minSize = new Vector2(100,0);
		public Vector2 maxSize = new Vector2(200,1000);
		private Vector2 scroll;
		private Rect rect;
		
		public UIDEEditor editor {
			get {
				return UIDEEditor.current;
			}
		}
		public Theme theme {
			get {
				return editor.theme;
			}
		}
		
		public RCMenu() {
			if (editor != null) {
				FitOnScreen();
			}
		}
		
		public void FitOnScreen() {
			maxSize.y = Mathf.Min(maxSize.y,editor.rect.height);
			maxSize.x = Mathf.Min(maxSize.x,editor.rect.width);
			
			position.x = Mathf.Min(position.x,editor.rect.width-rect.width);
			position.y = Mathf.Min(position.y,editor.rect.height-rect.height);
			
			position.x = Mathf.Max(position.x,0);
			position.y = Mathf.Max(position.y,0);
		}
		
		public void AddItem(RCMenuItem item) {
			if (item == null) return;
			item.menu = this;
			items.Add(item);
		}
		
		public void RemoveItem(RCMenuItem item) {
			items.Remove(item);
		}
		
		private void Render(int windowID) {
			GUIStyle shadowStyle = theme.GetStyle("DropShadow");
			GUIStyle boxStyle = theme.GetStyle("RCMenuBackground");
			FitOnScreen();
			GUI.skin = theme.skin;
			if (items.Count == 0) {
				RCMenu.CloseCurrentMenu();
			}
			
			Vector2 boxSize = new Vector2(0,0);
			for (int i = 0; i < items.Count; i++) {
				Vector2 s = items[i].CalcSize();
				boxSize.x = Mathf.Max(boxSize.x,s.x);
				boxSize.y += s.y;
			}
				
			boxSize.x = Mathf.Max(boxSize.x,minSize.x);
			boxSize.y = Mathf.Max(boxSize.y,minSize.y);
			
			Vector2 contentSize = boxSize;
			
			boxSize.x = Mathf.Min(boxSize.x,maxSize.x);
			boxSize.y = Mathf.Min(boxSize.y,maxSize.y);
			
			rect = new Rect(position.x,position.y,boxSize.x,boxSize.y);
			Rect rectZeroPos = rect;
			rectZeroPos.x = 0;
			rectZeroPos.y = 0;
			
			Rect contentRect = new Rect(0,0,contentSize.x,contentSize.y);
			
			bool hasScrollBar = false;
			if (contentSize.y > boxSize.y) {
				hasScrollBar = true;
			}
			
			if (hasScrollBar) {
				rect.width += GUI.skin.verticalScrollbar.fixedWidth;
			}
			GUI.Box(rect,"",shadowStyle);
			GUI.Box(rect,"",boxStyle);
			//GUI.BeginGroup(rectZeroPos);
			
			scroll = GUI.BeginScrollView(rect,scroll,contentRect);
			for (int i = 0; i < items.Count; i++) {
				//GUILayout.Label("dsf");
				Rect r = rectZeroPos;
				Vector2 size = items[i].CalcSize();
				r.x = 0;
				r.y = i*size.y;
				r.height = size.y;
				r.width = contentSize.x;
				items[i].Render(r);
			}
			GUI.EndScrollView();
			//GUI.EndGroup();
			
			if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseUp) && 
			(Event.current.button == 0 || Event.current.button == 2)) {
				CloseCurrentMenu();
			}
			GUI.color = new Color(1,1,1,0.2f);
			//if (GUI.Button(new Rect(0,0,editor.rect.width,editor.rect.height),"")) {
			//	Debug.Log("ddfsf");
			//	HideCurrentMenu();
			///}
			GUI.color = new Color(1,1,1,1);
		}
		
		//Statics
		static public void Update() {
			if (menuToMakeCurrent != null) {
				currentMenu = menuToMakeCurrent;
			}
		}
		static public void OnGUI() {
			if (currentMenu != null) {
				Rect r = new Rect(0,0,UIDEEditor.current.rect.width,UIDEEditor.current.rect.height);
				//GUI.BringWindowToFront(-3);
				//GUI.FocusWindow(-3);
				GUI.Window(-3,r,currentMenu.Render,"",new GUIStyle());
				GUI.BringWindowToFront(-3);
				GUI.FocusWindow(-3);
				//currentMenu.Render();
			}
		}
		
		static public void CloseCurrentMenu() {
			currentMenu = null;
			menuToMakeCurrent = null;
			UIDEEditor.current.Repaint();
		}
		
		static public void ShowMenu(RCMenu menu) {
			menuToMakeCurrent = menu;
			UIDEEditor.current.Repaint();
		}
		
	}
}