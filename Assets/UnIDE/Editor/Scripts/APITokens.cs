using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

namespace UIDE.SyntaxRules {
	static public class APITokens:System.Object {
		static public HashSet<string> knownTypeTokens = new HashSet<string>();
		
		static public void Update(string[] names) {
			knownTypeTokens = new HashSet<string>();
			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
				if (names != null && !names.Contains(asm.GetName().Name)) continue;
				foreach (Type type in asm.GetTypes()) {
					if (!knownTypeTokens.Contains(type.Name)) {
						knownTypeTokens.Add(type.Name);
					}
				}
			}
		}
		
		static public bool IsTypeKeyword(string keyword) {
			return knownTypeTokens.Contains(keyword);
		}
	}
}
