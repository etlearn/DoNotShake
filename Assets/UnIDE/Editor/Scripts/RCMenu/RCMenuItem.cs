using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UIDE.RightClickMenu {
	public class RCMenuItem:System.Object {
		public RCMenu menu;
		public string title;
		public System.Object[] metaData;
		private System.Action<System.Object[]> callback;
		private System.Action callbackNoArg;
		
		public RCMenuItem(string title) {
			this.title = title;
		}
		public RCMenuItem(string title, System.Action<System.Object[]> callback) {
			this.title = title;
			this.callback = callback;
		}
		public RCMenuItem(string title, System.Action<System.Object[]> callback, System.Object[] metaData) {
			this.title = title;
			this.callback = callback;
			this.metaData = metaData;
		}
		
		public void SetCallback(System.Action callback) {
			this.callbackNoArg = callback;
		}
		public void SetCallback(System.Action<System.Object[]> callback) {
			this.callback = callback;
		}
		
		public void Render(Rect rect) {
			GUIStyle style = menu.theme.GetStyle("RCMenuItem");
			if (GUI.Button(rect,title,style)) {
				OnClick();
			}
		}
		
		public void OnClick() {
			if (callback != null) {
				callback(metaData);
			}
			else if (callbackNoArg != null) {
				callbackNoArg();
			}
			RCMenu.CloseCurrentMenu();
		}
		
		public Vector2 CalcSize() {
			GUIStyle style = menu.theme.GetStyle("RCMenuItem");
			Vector2 size = style.CalcSize(new GUIContent(title));
			size.x += style.margin.left+style.margin.right;
			//size.y += style.margin.top+style.margin.bottom;
			size.y += style.margin.bottom;
			return size;
		}
	}
}
