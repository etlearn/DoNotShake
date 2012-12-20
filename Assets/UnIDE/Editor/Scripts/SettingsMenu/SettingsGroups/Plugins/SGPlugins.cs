using UnityEngine;
using System.Collections;

namespace UIDE.SettingsMenu.Groups {
	public class SGPlugins:SettingsGroup {
		public override void Start() {
			title = "Plugins";
			order = 3.0f;
		}
	}
}
