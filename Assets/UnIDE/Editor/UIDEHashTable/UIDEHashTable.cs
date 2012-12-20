using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using TestNamespace2;






public class UIDEHashTable:System.Object {
	Dictionary<string,System.Object> dict = new Dictionary<string,System.Object>();
	
	public void Set(string key, System.Object item) {
		//System.Object getVal = null;
		//dict.TryGetValue(key,out getVal);
		if (dict.ContainsKey(key)) {
			dict[key] = item;
		}
		else {
			dict.Add(key,item);
		}
	}
	public System.Object Get(string key) {
		System.Object getVal = null;
		dict.TryGetValue(key,out getVal);
		return getVal;
	}
	/*
	public UIDEHashItem baseItem = new UIDEHashItem();
	public int length = 0;
	public UIDEHashTable() {
		baseItem.children = new UIDEHashItem[256];
	}
	
	public void Set(string key, System.Object item) {
		if (key == null) return;
		UIDEHashItem currentItem = baseItem;
		char[] chars  = key.ToCharArray();
		//int lastChar;
		for (int i = 0; i < chars.Length; i++) {
			char c = chars[i];
			int ci = c;
			if (currentItem == null || currentItem.children == null) continue;
			if (currentItem.children[ci] == null) {
				currentItem.children[ci] = new UIDEHashItem();
				//if (i < chars.length-1) {
					currentItem.children[ci].children = new UIDEHashItem[256];
				//}
			}
			currentItem = currentItem.children[ci];
			//lastChar = ci;
		}
		if (!currentItem.hasBeenSet) {
			length++;
			currentItem.hasBeenSet = true;
		}
		currentItem.value = item;
	}
	
	public System.Object Get(string key) {
		if (key == null) {
			//Debug.LogError("The key is null");
			return null;
		}
		UIDEHashItem currentItem = baseItem;
		if (currentItem.children == null) return null;
		char[] chars  = key.ToCharArray();
		//int lastChar;
		for (int i = 0; i < chars.Length; i++) {
			char c = chars[i];
			int ci = c;
			if (currentItem == null || currentItem.children == null) continue;
			if (currentItem.children[ci] == null) {
				return null;
			}
			currentItem = currentItem.children[ci];
			//lastChar = ci;
		}
		return currentItem.value;
	}
	*/
}
