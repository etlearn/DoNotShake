using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class UIDETokenDef:System.Object {
	public List<string> types = new List<string>();
	public string rawTypes = "";
	public Color color = new Color(1,1,1,1);
	public Color backgroundColor = new Color(1,1,1,0);
	public float mouseOverMultiply = 1.0f;
	public bool isBold = false;
	public bool originalIsBold = false;
	public bool useParsableString = false;
	public string parsableString = "";
	public bool isActualCode = true;
	
	public UIDETokenDef() {
		
	}
	public UIDETokenDef(string types, Color color) {
		this.types.AddRange(types.Split(","[0]));
		this.rawTypes = types;
		this.color = color;
	}
	public UIDETokenDef(string types, Color color,Color backgroundColor) {
		this.types.AddRange(types.Split(","[0]));
		this.rawTypes = types;
		this.color = color;
		this.backgroundColor = backgroundColor;
	}
	public UIDETokenDef(string types, Color color,Color backgroundColor,float mouseOverMultiply) {
		this.types.AddRange(types.Split(","[0]));
		this.rawTypes = types;
		this.color = color;
		this.backgroundColor = backgroundColor;
		this.mouseOverMultiply = mouseOverMultiply;
	}
	public UIDETokenDef(string types, Color color,Color backgroundColor,float mouseOverMultiply,bool isBold) {
		this.types.AddRange(types.Split(","[0]));
		this.rawTypes = types;
		this.color = color;
		this.backgroundColor = backgroundColor;
		this.mouseOverMultiply = mouseOverMultiply;
		this.isBold = isBold;
		this.originalIsBold = isBold;
	}
	
	public bool HasType(string type) {
		return HasType(type,false);
	}
	public bool HasType(string type, bool useCase) {
		if (!useCase) {
			type = type.ToLower();
		}
		for (int i = 0; i < types.Count; i++) {
			string thisType = types[i];
			if (!useCase) {
				thisType = thisType.ToLower();
			}
			if (type == thisType) {
				return true;
			}
		}
		return false;
	}
	
	public string TypesToString() {
		string str = "";
		for (int i = 0; i < types.Count; i++) {
			str += types[i];
			if (i < types.Count-1) {
				str += ",";
			}
		}
		return str;
	}
}
