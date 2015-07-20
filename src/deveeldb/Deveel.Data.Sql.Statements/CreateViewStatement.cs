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
using System.Collections.Generic;
using System.Linq;

using Deveel.Data.DbSystem;
using Deveel.Data.Security;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Query;

namespace Deveel.Data.Sql.Statements {
	[Serializable]
	public sealed class CreateViewStatement : SqlStatement {
		public CreateViewStatement(string viewName, SqlQueryExpression queryExpression) 
			: this(viewName, null, queryExpression) {
		}

		public CreateViewStatement(string viewName, IEnumerable<string> columnNames, SqlQueryExpression queryExpression) {
			if (viewName == null)
				throw new ArgumentNullException("viewName");
			if (queryExpression == null)
				throw new ArgumentNullException("queryExpression");

			ViewName = viewName;
			ColumnNames = columnNames;
			QueryExpression = queryExpression;
		}

		public string ViewName { get; private set; }

		public IEnumerable<string> ColumnNames { get; private set; }

		public SqlQueryExpression QueryExpression { get; private set; }

		public bool ReplaceIfExists { get; set; }

		public override StatementType StatementType {
			get { return StatementType.CreateView;}
		}

		protected override SqlPreparedStatement PrepareStatement(IExpressionPreparer preparer, IQueryContext context) {
			var viewName = context.ResolveTableName(ViewName);

			var queryFrom = QueryExpressionFrom.Create(context, QueryExpression);
			var queryPlan = context.DatabaseContext.QueryPlanner().PlanQuery(context, QueryExpression, null);

			var colList = ColumnNames == null ? new string[0] : ColumnNames.ToArray();

			// Wrap the result around a SubsetNode to alias the columns in the
			// table correctly for this view.
			int sz = colList.Length;
			var originalNames = queryFrom.GetResolvedColumns();
			var newColumnNames = new ObjectName[originalNames.Length];

			if (sz > 0) {
				if (sz != originalNames.Length)
					throw new InvalidOperationException("Column list is not the same size as the columns selected.");

				for (int i = 0; i < sz; ++i) {
					var colName = colList[i];
					newColumnNames[i] = new ObjectName(viewName, colName);
				}
			} else {
				sz = originalNames.Length;
				for (int i = 0; i < sz; ++i) {
					newColumnNames[i] = new ObjectName(viewName, originalNames[i].Name);
				}
			}

			// Check there are no repeat column names in the table.
			for (int i = 0; i < sz; ++i) {
				var columnName = newColumnNames[i];
				for (int n = i + 1; n < sz; ++n) {
					if (newColumnNames[n].Equals(columnName))
						throw new InvalidOperationException(String.Format("Duplicate column name '{0}' in view. A view may not contain duplicate column names.", columnName));
				}
			}

			// Wrap the plan around a SubsetNode plan
			queryPlan = new SubsetNode(queryPlan, originalNames, newColumnNames);

			// We have to execute the plan to get the TableInfo that represents the
			// result of the view execution.
			var table = queryPlan.Evaluate(context);
			var tableInfo = table.TableInfo.Alias(viewName);

			return new PreparedCreateView(tableInfo, QueryExpression, queryPlan);
		}

		#region PreparedCreateView

		[Serializable]
		class PreparedCreateView : SqlPreparedStatement {
			public PreparedCreateView(TableInfo tableInfo, SqlQueryExpression queryExpression, IQueryPlanNode queryPlan) {
				TableInfo = tableInfo;
				QueryPlan = queryPlan;
				QueryExpression = queryExpression;
			}

			public TableInfo TableInfo { get; private set; }

			public IQueryPlanNode QueryPlan { get; private set; }

			public SqlQueryExpression QueryExpression { get; private set; }

			protected void CheckUserSelectPermissions(IQueryContext context, IQueryPlanNode plan) {
				// Discover the list of TableName objects this command touches,
				var touchedTables = plan.DiscoverTableNames();

				// Check that the user is allowed to select from these tables.
				foreach (var tableName in touchedTables) {
					if (!context.UserCanSelectFromTable(tableName))
						throw new InvalidAccessException(tableName);
				}
			}

			public override ITable Evaluate(IQueryContext context) {
				var viewName = TableInfo.TableName;

				if (!context.UserCanCreateTable(viewName))
					throw new InvalidOperationException();	// TODO: Specialized security exception

				if (context.ObjectExists(viewName))
					throw new InvalidOperationException();	// TODO: Specialized exception here

				// Check the permissions for this user to select from the tables in the
				// given plan.
				CheckUserSelectPermissions(context, QueryPlan);

				// We have to execute the plan to get the DataTableInfo that represents the
				// result of the view execution.
				var t = QueryPlan.Evaluate(context);
				var tableInfo = t.TableInfo.Alias(viewName);
				var viewInfo = new ViewInfo(tableInfo, QueryExpression, QueryPlan);

				context.DefineView(viewInfo);

				// The initial grants for a view is to give the user who created it
				// full access.
				context.GrantToUserOnTable(viewName, Privileges.TableAll);

				return FunctionTable.ResultTable(context, 0);
			}
		}

		#endregion
	}
}
