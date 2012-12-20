using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System;
using System.IO;
using System.Text;

using UnityEditor;

using UIDE.SyntaxRules;

namespace lewl {
	public class loool {
		public float skin;
	}
}

public class UIDEGrammarTester:EditorWindow {
	public GUISkin skin;
	//public Parser parser;
	//public string textToParse = @"4((5)3)1";
	public string textToParse = @"4(5)1";
	//CompilationUnitNode rootNode;
	
	[MenuItem ("Window/Grammar Tester")]
	static public void Init() {
		
		UIDEGrammarTester window = EditorWindow.GetWindow<UIDEGrammarTester>("Grammar Tester");
		window.Start();
	}
	
	public void Start() {
		//skin = (GUISkin)UIDEEditor.LoadAsset("Assets/UnIDE/Style/UnIDEDefault.guiskin");
		//parser = new Parser(textToParse);
		//parser.Parse();
		Run();
	}
	
	void Update() {
		Repaint();
	}
	
	public void Run() {
		
	}

}

