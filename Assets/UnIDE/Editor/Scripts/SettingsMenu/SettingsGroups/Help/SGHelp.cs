using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;

namespace UIDE.SettingsMenu.Groups {
	
	public class SHHelp:SettingsGroup {
		private GUIStyle wordWrapStyle;
		public override void Start() {
			title = "Help";
			order = 4.0f;
		}
		
		public override void OnGUI(Rect groupRect) {
			wordWrapStyle = new GUIStyle(GUI.skin.label);
			wordWrapStyle.wordWrap = true;
			if (Application.platform == RuntimePlatform.OSXEditor) {
				DrawOSXHotkeys();
			}
			else {
				DrawWindowsHotkeys();
			}
			
			GUILayout.Label("");
			
			GUILayout.Label("There is a right click menu which gives you access to basic text editing tools, as well as plugin commands such as \"Search Unity Docs\" and \"Go To Declaration\". To close tabs you can either right click and select \"Close\", or middle mouse click them.",wordWrapStyle);
			GUILayout.Label("Holding control and using the Left and Right arrow keys will move the cursor to the previous/next text \"element\". Pressing Up or Down arrow keys while holding control will increment the cursors line position up or down in increments of 4 lines.",wordWrapStyle);
			GUILayout.Label("Holding Shift and using the arrow keys will move the cursor while expanding the text selection to include the cursors new position.",wordWrapStyle);
			GUILayout.Label("You can add custom fonts and pick them from the settings menu. Custom fonts go into UnIDE/Editor/TextEditorFonts/YourFont/. Be sure to include YourFont.ttf as well as YourFont_B.ttf, \"_B\" denotes that this is the bold variation of the font.",wordWrapStyle);
			
			GUILayout.Label("");
			DrawNotes();
		}
		
		public void DrawNotes() {
			string saveHotkey = "Ctrl+S";
			if (Application.platform == RuntimePlatform.OSXEditor) {
				saveHotkey = "Command+S";
			}
			string undoHotkey = "Ctrl+Z";
			if (Application.platform == RuntimePlatform.OSXEditor) {
				undoHotkey = "Command+Z";
			}
			GUILayout.Label("Notes:");
			GUILayout.Label("In order to be able to use the standard "+saveHotkey+" hotkey to save your current file, you must be unity loaded into a saved scene. You can also use the alternate save hotkey in the list above at any time.",wordWrapStyle);
			GUILayout.Label("If you are experiencing slowness while editing and you are using OSX, try enabling \"Force Generic Completion\" in the General Settings menu, or disable completion completely. Unfortunately there seems to be a bug in the version of Mono that Unity uses that cripples multithreaded tasks on OSX.",wordWrapStyle);
			GUILayout.Label("On rare occasions the Undo hotkey ("+undoHotkey+") may stop working. Unfortunately I dont have much control over this because of the way Unity handles hotkeys. Closing and reopening UnIDE usually fixes this though.",wordWrapStyle);
			GUILayout.Label("In Unity 3.5 when targeting mobile or flash platforms you should go into the settings menu and uncheck \"Force Dynamic Font\" in the \"Text\" settings. You may get warnings about dynamic fonts not being supported, and you can go into the offending font import settings and change their \"Character\" setting from Dyanmic to Unicode. This does not effect Unity 4.0 or higher users.",wordWrapStyle);
		}
		
		private void DrawWindowsHotkeys() {
			GUILayout.Label("Hotkeys:");
			GUILayout.Label("Save - Ctrl+S or Ctrl+Alt+S");
			GUILayout.Label("Undo - Ctrl+Z or Ctrl+Alt+Z");
			GUILayout.Label("Redo - Ctrl+Shift+Z");
			GUILayout.Label("Select All - Ctrl+A");
			GUILayout.Label("Copy - Ctrl+C");
			GUILayout.Label("Cut - Ctrl+X");
			GUILayout.Label("Paste - Ctrl+V");
			
			GUILayout.Label("Move To Line Start - Home");
			GUILayout.Label("Move To Line End - End");
			
			GUILayout.Label("Move To Doc Start - Ctrl+Home");
			GUILayout.Label("Move To Doc End - Ctrl+End");
			
			GUILayout.Label("Duplicate Line - Ctrl+D");
			GUILayout.Label("Delete Line - Ctrl+Shift+D");
			
			GUILayout.Label("Comment Lines - Ctrl+/");
			GUILayout.Label("Uncomment Lines - Ctrl+Shift+/");
			
			GUILayout.Label("Search Unity Docs - F1");
			
			GUILayout.Label("Find Next - F3");
			GUILayout.Label("Find Previous - Shift+F3");
		}
		
		private void DrawOSXHotkeys() {
			GUILayout.Label("Hotkeys:");
			GUILayout.Label("Save - Command+S or Command+Alt+S");
			GUILayout.Label("Undo - Command+Z or Command+Alt+Z");
			GUILayout.Label("Redo - Command+Shift+Z");
			GUILayout.Label("Select All - Command+A");
			GUILayout.Label("Copy - Command+C");
			GUILayout.Label("Cut - Command+X");
			GUILayout.Label("Paste - Command+V");
			
			GUILayout.Label("Move To Line Start - Home");
			GUILayout.Label("Move To Line End - End");
			
			GUILayout.Label("Move To Doc Start - Command+Home");
			GUILayout.Label("Move To Doc End - Command+End");
			
			GUILayout.Label("Duplicate Line - Command+D");
			GUILayout.Label("Delete Line - Command+Shift+D");
			
			GUILayout.Label("Comment Lines - Command+/");
			GUILayout.Label("Uncomment Lines - Command+Shift+/");
			
			GUILayout.Label("Search Unity Docs - F1");
			
			GUILayout.Label("Find Next - F3");
			GUILayout.Label("Find Previous - Shift+F3");
		}
	}
}
