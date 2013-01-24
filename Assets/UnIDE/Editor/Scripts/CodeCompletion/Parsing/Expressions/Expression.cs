using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UIDE.CodeCompletion.Parsing;

namespace UIDE.CodeCompletion.Parsing.Expressions {
	public class Expression:Statement {
		public Expression(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
	}
	
	public class ExpressionFindRule:System.Object {
		public virtual Expression Find(StatementBlock block, ref int startIndex) {
			return null;
		}
	}
}
