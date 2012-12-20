using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UIDE.CodeCompletion.Parsing;

namespace UIDE.CodeCompletion.Parsing.Expressions {
	public class BinaryOperator:Expression {
		public Expression leftOperand;
		public Expression rightOperand;
		public BinaryOperator(StatementBlock block, int start, int end):base(block,start,end) {
			
		}
	}
}
