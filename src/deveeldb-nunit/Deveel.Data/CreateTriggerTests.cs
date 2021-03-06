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

using Deveel.Data.Routines;
using Deveel.Data.Sql;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Statements;
using Deveel.Data.Sql.Triggers;
using Deveel.Data.Sql.Types;

using NUnit.Framework;
using Deveel.Data.Sql.Tables;

namespace Deveel.Data {
	[TestFixture]
	public sealed class CreateTriggerTests : ContextBasedTest {
		protected override bool OnSetUp(string testName, IQuery query) {
			CreateTestTable(query);
			return true;
		}

		private static void CreateTestTable(IQuery query) {
			var tableName1 = ObjectName.Parse("APP.test_table");

			query.Access().CreateTable(table => table
				.Named(tableName1)
				.WithColumn(column => column
					.Named("id")
					.HavingType(PrimitiveTypes.Integer())
					.WithDefault(SqlExpression.FunctionCall("UNIQUEKEY",
						new SqlExpression[] {SqlExpression.Constant(tableName1.FullName)}))
					.NotNull())
				.WithColumn("first_name", PrimitiveTypes.String())
				.WithColumn("last_name", PrimitiveTypes.String())
				.WithColumn("birth_date", PrimitiveTypes.DateTime())
				.WithColumn("active", PrimitiveTypes.Boolean()));

			query.Session.Access().AddPrimaryKey(tableName1, "id", "PK_TEST_TABLE");

			query.Access().CreateTable(table => table
				.Named("APP.test_table2")
				.WithColumn("person_id", PrimitiveTypes.Integer())
				.WithColumn("value", PrimitiveTypes.Boolean()));

			var body = new PlSqlBlockStatement();
			body.Statements.Add(new CallStatement(ObjectName.Parse("system.output"), new[] {
				new InvokeArgument(SqlExpression.Constant("One row was inserted"))
			}));
			var procedureInfo = new PlSqlProcedureInfo(ObjectName.Parse("APP.proc1"), new RoutineParameter[0], body);
			query.Access().CreateObject(procedureInfo);
		}

		protected override bool OnTearDown(string testName, IQuery query) {
			var tableName1 = ObjectName.Parse("APP.test_table");
			var tableName2 = ObjectName.Parse("APP.test_table2");

			query.Access().DropAllTableConstraints(tableName1);
			query.Access().DropObject(DbObjectType.Table, tableName2);
			query.Access().DropObject(DbObjectType.Table, tableName1);
			query.Access().DropObject(DbObjectType.Routine, ObjectName.Parse("APP.proc1"));
			return true;
		}

		[Test]
		public void CallbackTrigger() {
			var tableName = ObjectName.Parse("APP.test_table");
			AdminQuery.CreateCallbackTrigger("trigger1", tableName, TriggerEventTime.Before, TriggerEventType.Insert);

			var trigger = AdminQuery.Access().GetObject(DbObjectType.Trigger, new ObjectName("trigger1")) as Trigger;

			Assert.IsNotNull(trigger);
			Assert.AreEqual("trigger1", trigger.TriggerInfo.TriggerName.FullName);
			Assert.AreEqual(tableName, trigger.TriggerInfo.TableName);
			Assert.AreEqual(TriggerEventTime.Before, trigger.TriggerInfo.EventTime);
			Assert.AreEqual(TriggerEventType.Insert, trigger.TriggerInfo.EventType);
		}

		[Test]
		public void PlSqlTrigger() {
			var body = new PlSqlBlockStatement();
			body.Statements.Add(new CallStatement(ObjectName.Parse("system.output"), new[] {
				new InvokeArgument(SqlExpression.Constant("One row was inserted"))
			}));
			var triggerName = new ObjectName("trigger1");
			var tableName = ObjectName.Parse("APP.test_table");

			AdminQuery.CreateTrigger(triggerName, tableName, body, TriggerEventTime.After, TriggerEventType.Insert);

			var exists = AdminQuery.Access().TriggerExists(ObjectName.Parse("APP.trigger1"));

			Assert.IsTrue(exists);
		}

		[Test]
		public void ProcedureTrigger() {
			var triggerName = new ObjectName("trigger1");
			var tableName = ObjectName.Parse("APP.test_table");
			var procName = new ObjectName("proc1");
			AdminQuery.CreateProcedureTrigger(triggerName, tableName, procName, TriggerEventTime.After, TriggerEventType.Insert);

			var exists = AdminQuery.Access().TriggerExists(ObjectName.Parse("APP.trigger1"));

			Assert.IsTrue(exists);
		}
	}
}