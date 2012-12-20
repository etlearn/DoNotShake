using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UIDE.CodeCompletion.Parsing;

namespace UIDE.CodeCompletion.Parsing.Expressions {
	public class AddOperator:BinaryOperator {
		public AddOperator(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
	}
	
	public class AddOperatorFind:ExpressionFindRule {
		public override Expression Find(StatementBlock block, ref int startIndex) {
			int pos = startIndex;
			char nextChar = block.GetChar(pos);
			if (nextChar != '+') return null;
			pos++;
			if (block.IsEOF(pos)) return null;
			nextChar = block.GetChar(pos);
			
			if (nextChar == '+') return null;
			if (nextChar == '=') return null;
			
			AddOperator ex = new AddOperator(block,startIndex,startIndex);
			
			startIndex = pos;
			
			return ex;
		}
	}
}
