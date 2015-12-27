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
using System.Collections.Generic;
using System.Linq;

using Deveel.Data;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Objects;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Types;

using NUnit.Framework;

namespace Deveel.Data.Sql.Statements {
	[TestFixture]
	public class UpdateStatementTests : ContextBasedTest {
		protected override IQuery CreateQuery(ISession session) {
			var query = base.CreateQuery(session);
			CreateTestTable(query);
			AddTestData(query);
			return query;
		}

		private void CreateTestTable(IQuery context) {
			var tableInfo = new TableInfo(ObjectName.Parse("APP.test_table"));
			var idColumn = tableInfo.AddColumn("id", PrimitiveTypes.Integer());
			idColumn.DefaultExpression = SqlExpression.FunctionCall("UNIQUE_KEY",
				new SqlExpression[] { SqlExpression.Reference(tableInfo.TableName) });
			tableInfo.AddColumn("first_name", PrimitiveTypes.String());
			tableInfo.AddColumn("last_name", PrimitiveTypes.String());
			tableInfo.AddColumn("birth_date", PrimitiveTypes.DateTime());
			tableInfo.AddColumn("active", PrimitiveTypes.Boolean());

			context.CreateTable(tableInfo);
			context.AddPrimaryKey(tableInfo.TableName, "id", "PK_TEST_TABLE");
		}

		private void AddTestData(IQuery context) {
			var table = context.GetMutableTable(ObjectName.Parse("APP.test_table"));
			var row = table.NewRow();
			row.SetValue("id", DataObject.Integer(0));
			row.SetValue("first_name", DataObject.String("John"));
			row.SetValue("last_name", DataObject.String("Doe"));
			row.SetValue("birth_date", DataObject.Date(new SqlDateTime(1977, 01, 01)));
			row.SetValue("active", DataObject.Boolean(false));
			table.AddRow(row);

			row = table.NewRow();
			row.SetValue("id", DataObject.Integer(1));
			row.SetValue("first_name", DataObject.String("Jane"));
			row.SetValue("last_name", DataObject.String("Doe"));
			row.SetValue("birth_date", DataObject.Date(new SqlDateTime(1978, 11, 01)));
			row.SetValue("active", DataObject.Boolean(true));
			table.AddRow(row);

			row = table.NewRow();
			row.SetValue("id", DataObject.Integer(2));
			row.SetValue("first_name", DataObject.String("Roger"));
			row.SetValue("last_name", DataObject.String("Rabbit"));
			row.SetValue("birth_date", DataObject.Date(new SqlDateTime(1985, 05, 05)));
			row.SetValue("active", DataObject.Boolean(true));
			table.AddRow(row);
		}

		[Test]
		public void UpdateOneRow() {
			var whereExp = SqlExpression.Parse("id = 2");
			var assignments = new[] {
				new SqlColumnAssignment("birth_date", SqlExpression.Constant(DataObject.Date(new SqlDateTime(1970, 01, 20)))) 
			};

			var statement = new UpdateStatement("APP.test_table", whereExp, assignments);
			Query.ExecuteStatement(statement);
		}
	}
}
