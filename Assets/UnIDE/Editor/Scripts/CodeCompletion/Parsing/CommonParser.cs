using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


using UIDE.CodeCompletion.Parsing;

namespace UIDE.CodeCompletion.Parsing.CommonParser {
	
	public class Parser:System.Object {
		
		static public string[] allModifiers = new string[] {
			"new",
			"public",
			"protected",
			"internal",
			"private",
			"static",
			"readonly",
			"volatile",
			"virtual",
			"sealed",
			"override",
			"abstract",
			"extern",
			"unsafe",
			"partial",
			"const",//Probably shouldnt be here.
		};
		static public string[] methodArgModifers = new string[] {
			"out",
			"ref",
			"this",
			"params",
		};
		static private HashSet<string> _allModifiersHash;
		static public HashSet<string> allModifiersHash {
			get {
				if (_allModifiersHash == null) {
					_allModifiersHash = new HashSet<string>(allModifiers);
				}
				return 	_allModifiersHash;
			}
		}
		
		static private HashSet<string> _methodArgModifersHash;
		static public HashSet<string> methodArgModifersHash {
			get {
				if (_methodArgModifersHash == null) {
					_methodArgModifersHash = new HashSet<string>(methodArgModifers);
				}
				return 	_methodArgModifersHash;
			}
		}
		
		public Dictionary<string,StatementFindRule> findRules = new Dictionary<string,StatementFindRule>();
				
		public SourceFile file;
		public string sourceFileName = "";
		public string sourceFilePath = "";
		
		public T FindStatement<T>(StatementBlock block, ref int startIndex) where T:Statement {
			Statement s = FindFromRule(typeof(T).Name,block, ref startIndex);
			if (s == null) {
				return default(T);
			}
			T t = (T)System.Convert.ChangeType(s,typeof(T));
			return t;
		}
		public Statement FindFromRule(string name, StatementBlock block, ref int startIndex) {
			StatementFindRule findRule = null;
			findRules.TryGetValue(name,out findRule);
			if (findRule != null) {
				int start = startIndex;
				Statement result = findRule.Find(block,ref start);
				startIndex = start;
				//Debug.Log(result);
				return result;
			}
			return null;
		}
		
		
		public void AddFindRule<T>(string name) where T:StatementFindRule {
			T findRule = (T)System.Activator.CreateInstance(typeof(T));
			findRules.Add(name,findRule);
		}
		
		public virtual void OnFinishedParsingFile() {
			
		}
		
		public virtual void Parse(string text) {
			text += "\n\n";
			file = new SourceFile(this,text);
			
			try {
				file.Parse();
			}
			catch (System.Exception ex) {
				Debug.LogError(ex);
			}
		}
		
		
		static public int EndOfMatch(Match match) {
			return EndFromStartAndLength(match.Index,match.Length);
		}
		static public int EndFromStartAndLength(int start, int length) {
			return start+(length-1);
		}
		static public Match GetMatch(string input, string pattern, int startIndex) {
			return GetMatch(input,new Regex(pattern),startIndex);
		}
		static public Match GetMatch(string input, Regex regex, int startIndex) {
			int actualStart = startIndex;
			Match match = regex.Match(input,actualStart);
			return match;
		}
	}
	
	
}