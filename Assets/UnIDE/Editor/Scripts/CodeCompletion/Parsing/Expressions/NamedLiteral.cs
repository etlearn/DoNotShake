using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UIDE.CodeCompletion.Parsing;

namespace UIDE.CodeCompletion.Parsing.Expressions {
	public class NamedLiteral:BinaryOperator {
		public NamedLiteral(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
	}
	
	public class NamedLiteralFind:ExpressionFindRule {
		public override Expression Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			
			WordDef word = WordDef.Find(block,ref pos);
			if (word == null) return null;
			
			return null;
		}
	}
}
