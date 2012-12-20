using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

//Levenshtein search algorithm
//http://www.dotnetperls.com/levenshtein

/// <summary>
/// Contains approximate string matching
/// </summary>
/// 

public class UIDEFuzzySearchItem {
	public int score = -1;
	public string text;
	public System.Object metaObject;
	public int Compute(string key) {
		score = UIDEFuzzySearch.Compute(key,text);
		return score;
	}
}

public class UIDEFuzzySearch {
	/// <summary>
	/// Compute the distance between two strings.
	/// </summary>
	public static int Compute(string s, string t, bool useCase = false) {
		if (!useCase) {
			s = s.ToLower();
			t = t.ToLower();
		}
		int n = s.Length;
		int m = t.Length;
		int[,] d = new int[n + 1, m + 1];
		
		// Step 1
		if (n == 0)
		{
			return m;
		}
		
		if (m == 0)
		{
			return n;
		}
		
		// Step 2
		for (int i = 0; i <= n; d[i, 0] = i++){}
		
		for (int j = 0; j <= m; d[0, j] = j++){}
		
		// Step 3
		for (int i = 1; i <= n; i++)
		{
			//Step 4
			for (int j = 1; j <= m; j++)
			{
			// Step 5
			int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
			
			// Step 6
			d[i, j] = Math.Min(
				Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
				d[i - 1, j - 1] + cost);
			}
		}
		// Step 7
		return d[n, m];
    }
	
	public static List<UIDEFuzzySearchItem[]> FilterList(string key, UIDEFuzzySearchItem[] inputList, bool requireFull, bool sortStartsWith) {
		key = key.ToLower();
		List<UIDEFuzzySearchItem> startsWithList = new List<UIDEFuzzySearchItem>();
		List<UIDEFuzzySearchItem> restOfList = new List<UIDEFuzzySearchItem>();
		UIDEHashTable keyHash = new UIDEHashTable();
		for (int i = 0; i < key.Length; i++) {
			keyHash.Set(key[i].ToString(),key[i].ToString());
		}
		
		for (int i = 0; i < inputList.Length; i++) {
			UIDEFuzzySearchItem item = inputList[i];
			string itemTextLower = inputList[i].text.ToLower();
			int index = itemTextLower.IndexOf(key);
			if (requireFull && index == -1) continue;
			
			bool allCharactersPresent = true;
			int lastIndexOf = -1;
			//int firstMatch = item.Length;
			//bool hasFirstMatch = false;
			for (int j = 0; j < key.Length; j++) {
				int thisIndex = itemTextLower.IndexOf(key[j],lastIndexOf+1);
				if (thisIndex == -1) {
					allCharactersPresent = false;
					break;
				}
				//if (!hasFirstMatch) {
				//	firstMatch = thisIndex;
				//	hasFirstMatch = true;
				//}
				lastIndexOf = thisIndex;
			}
			if (!allCharactersPresent) {
				continue;
			}
			if (sortStartsWith) {
				int indexOfFirstChar = itemTextLower.IndexOf(key[0]);
				if (indexOfFirstChar == 0) {
					startsWithList.Add(item);
				}
				else {
					restOfList.Add(item);
				}
			}
			else {
				startsWithList.Add(item);
			}
		}
		List<UIDEFuzzySearchItem[]> output = new List<UIDEFuzzySearchItem[]>();
		output.Add(startsWithList.ToArray());
		output.Add(restOfList.ToArray());
		return output;
		//return new string[,] {{startsWithList.ToArray()},{restOfList.ToArray()}};
	}
	
	static public UIDEFuzzySearchItem[] GetSortedList(string key, UIDEFuzzySearchItem[] inputList, bool requireFull, bool sortStartsWith) {
		List<UIDEFuzzySearchItem[]> filteredList = new List<UIDEFuzzySearchItem[]>();
		if (key != "") {
			filteredList = FilterList(key,inputList, requireFull, sortStartsWith);
		}
		else {
			filteredList.Add(inputList);
			filteredList.Add(new UIDEFuzzySearchItem[] {});
		}
		UIDEFuzzySearchItem[] filteredStartsWithList = filteredList[0];
		UIDEFuzzySearchItem[] filteredRestOfList = filteredList[1];
		
		List<UIDEFuzzySearchItem> startsWithList = new List<UIDEFuzzySearchItem>();
		List<UIDEFuzzySearchItem> restOfList = new List<UIDEFuzzySearchItem>();
		
		
		for (int i = 0; i < filteredStartsWithList.Length; i++) {
			UIDEFuzzySearchItem item = filteredStartsWithList[i];
			//UIDEFuzzySearchItem item = new UIDEFuzzySearchItem();
			//item.text = filteredStartsWithList[i];
			item.Compute(key);
			startsWithList.Add(item);
		}
		
		for (int i = 0; i < filteredRestOfList.Length; i++) {
			UIDEFuzzySearchItem item = filteredRestOfList[i];
			//UIDEFuzzySearchItem item = new UIDEFuzzySearchItem();
			//item.text = filteredRestOfList[i];
			item.Compute(key);
			restOfList.Add(item);
		}
		
		startsWithList.Sort(
			delegate(UIDEFuzzySearchItem item1, UIDEFuzzySearchItem item2)
			{
				if (item1.score < item2.score) {
					return -1;
				}
				return 1;
			}
		);
		restOfList.Sort(
			delegate(UIDEFuzzySearchItem item1, UIDEFuzzySearchItem item2)
			{
				if (item1.score < item2.score) {
					return -1;
				}
				return 1;
			}
		);
		
		startsWithList.AddRange(restOfList);
		
		return startsWithList.ToArray();
	}
}
