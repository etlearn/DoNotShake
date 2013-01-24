using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class UIDETokenDefs:System.Object {
	
	//static public string tabToSpaceString = "    ";
	static private UIDETokenDef _defaultTokenDef;
	static public UIDETokenDef defaultTokenDef {
		get {
			if (_defaultTokenDef == null) {
				_defaultTokenDef = UIDETokenDefs.Get("DefaultText");
			}
			return _defaultTokenDef;
		}
	}
	
	static public UIDEHashTable tokenDefsHash;
	
	static public UIDETokenDef Get(string key) {
		//if (tokenDefsHash == null) UIDETokenDefs.Initialize();
		if (tokenDefsHash == null) return null;
		return (UIDETokenDef)tokenDefsHash.Get(key.ToLower());
	}
	static public void Set(string key, UIDETokenDef tokenDef) {
		//if (tokenDefsHash == null) UIDETokenDefs.Initialize();
		if (tokenDefsHash == null) return;
		tokenDefsHash.Set(key.ToLower(),tokenDef);
	}
	static public void AddNew(string type, Color color,Color backgroundColor) {
		AddNew(type,color,backgroundColor,1.0f);
	}
	static public void AddNew(string type, Color color,Color backgroundColor,float mouseOverMultiply) {
		Set(type,new UIDETokenDef(type,color,backgroundColor,mouseOverMultiply));
	}
	static public void AddNew(string type, Color color,Color backgroundColor,float mouseOverMultiply,bool isBold) {
		Set(type,new UIDETokenDef(type,color,backgroundColor,mouseOverMultiply,isBold));
	}
	
	static public void SetBold(string type, bool bold) {
		UIDETokenDef def = Get(type);
		if (def != null) {
			def.isBold = bold;
		}
	}
	/*
	static public void Initialize() {
		tokenDefsHash = new UIDEHashTable();
		
		AddNew("DefaultText",new Color(0.85f,0.85f,0.85f,1),new Color(1,1,1,0));
		
		AddNew("PreProcess",new Color(0.6f,0.6f,1.0f,1),new Color(1,1,1,0),1.0f,true);
		
		AddNew("String",new Color(1.0f,0.85f,0.2f,1),new Color(1.0f,0.95f,0.4f,0.25f),1.25f,true);
		
		AddNew("Comment,CommentSingleLine",new Color(0.5f,0.5f,0.5f,1),new Color(1,1,1,0.25f));
		AddNew("Comment,CommentMultiLine",new Color(0.5f,0.5f,0.5f,1),new Color(1,1,1,0.25f));
		
		AddNew("Number",new Color(1.0f,0.5f,0.35f,1),new Color(1.0f,0.75f,0.5f,0.25f),1.25f,true);
		
		AddNew("Word",new Color(0.85f,0.85f,0.85f,1),new Color(1,1,1,0.0f),1.25f,true);
		AddNew("Word,Keyword",new Color(1.0f,1.0f,1.0f,1),new Color(1,1,1,0.0f),1.25f,true);
		AddNew("Word,Modifier",new Color(0.75f,0.5f,0.6f,1),new Color(1,1,1,0.0f),1.25f,true);
		AddNew("Word,PrimitiveType",new Color(0.65f,0.75f,0.5f,1),new Color(1,1,1,0.0f),1.25f,true);
		
		AddNew("WhiteSpace",new Color(1,1,1,0),new Color(1,1,1,0));
		AddNew("WhiteSpace,Tab",new Color(1,1,1,0),new Color(1,1,1,0.0f));
		
		AddNew("LineEnd",new Color(1.0f,1.0f,1.0f,1),new Color(1,1,1,0),1.0f,true);
		AddNew("Dot",new Color(1.0f,1.0f,1.0f,1),new Color(1,1,1,0),1.0f,true);
	}
	*/
	//static public UIDETokenDef Create() {
		
	//}
}
