﻿// 
//  Copyright 2010-2014 Deveel
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;

using Deveel.Data.DbSystem;
using Deveel.Data.Sql.Expressions;

namespace Deveel.Data.Sql.Query {
	/// <summary>
	/// The node for performing a simple select operation on a table.
	/// </summary>
	/// <remarks>
	/// The simple select requires a LHS variable, an operator, and an expression 
	/// representing the RHS.
	/// </remarks>
	[Serializable]
	class SimpleSelectNode : SingleQueryPlanNode {
		/// <summary>
		/// The LHS variable.
		/// </summary>
		private ObjectName leftVar;

		/// <summary>
		/// The operator to select under (=, &lt;&gt;, &gt;, &lt;, &gt;=, &lt;=).
		/// </summary>
		private SqlExpressionType op;

		/// <summary>
		/// The RHS expression.
		/// </summary>
		private SqlExpression rightExpression;

		public SimpleSelectNode(QueryPlanNode child, ObjectName leftVar, SqlExpressionType op, SqlExpression rightExpression)
			: base(child) {
			this.leftVar = leftVar;
			this.op = op;
			this.rightExpression = rightExpression;
		}

		public override ITable Evaluate(IQueryContext context) {
			throw new NotImplementedException();
		}
	}
}