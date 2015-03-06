﻿// 
//  Copyright 2010-2015 Deveel
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
//

using System;

namespace Deveel.Data.Sql.Compile {
	/// <summary>
	/// The default implementation of a <see cref="ISqlNodeVisitor"/>
	/// that implements the visitor as a protected accessor.
	/// </summary>
	public class SqlNodeVisitor : ISqlNodeVisitor {
		/// <summary>
		/// Visits the given SQL node.
		/// </summary>
		/// <param name="node">The <see cref="ISqlNode"/> to visit.</param>
		/// <seealso cref="ISqlNodeVisitor.Visit"/>
		protected virtual void VisitNode(ISqlNode node) {
			if (node is ISqlVisitableNode)
				((ISqlVisitableNode)node).Accept(this);
		}

		void ISqlNodeVisitor.Visit(ISqlNode node) {
			VisitNode(node);
		}
	}
}