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

using Deveel.Data.Sql;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Sql.Types;

using NUnit.Framework;

namespace Deveel.Data {
	[TestFixture(StorageType.InMemory)]
	[TestFixture(StorageType.SingleFile)]
	[TestFixture(StorageType.JournaledFile)]
	public sealed class DropTableTests : ContextBasedTest {
		public DropTableTests(StorageType storageType)
			: base(storageType) {
		}

		protected override bool OnSetUp(string testName, IQuery query) {
			CreateTestTables(query);
			return true;
		}

		private static void CreateTestTables(IQuery context) {
			var tn1 = ObjectName.Parse("APP.test_table1");
			context.Access().CreateTable(table => table
				.Named(tn1)
				.WithColumn("id", PrimitiveTypes.Integer())
				.WithColumn("name", PrimitiveTypes.String())
				.WithColumn("date", PrimitiveTypes.DateTime()));

			context.Session.Access().AddPrimaryKey(tn1, "id");

			var tn2 = ObjectName.Parse("APP.test_table2");

			context.Access().CreateTable(table => table
				.Named(tn2)
				.WithColumn("id", PrimitiveTypes.Integer())
				.WithColumn("other_id", PrimitiveTypes.Integer())
				.WithColumn("count", PrimitiveTypes.Integer()));

			context.Session.Access().AddPrimaryKey(tn2, "id");
			context.Session.Access().AddForeignKey(tn2, new[] { "other_id" }, tn1, new[] { "id" }, ForeignKeyAction.Cascade,
				ForeignKeyAction.Cascade, null);
		}

		protected override bool OnTearDown(string testName, IQuery query) {
			var tn1 = ObjectName.Parse("APP.test_table1");
			var tn2 = ObjectName.Parse("APP.test_table2");

			if (query.Access().TableExists(tn2)) {
				query.Access().DropAllTableConstraints(tn2);
			}

			if (query.Access().TableExists(tn1)) {
				query.Access().DropAllTableConstraints(tn1);
			}

			query.Access().DropObject(DbObjectType.Table, tn2);
			query.Access().DropObject(DbObjectType.Table, tn1);
			return true;
		}

		protected override void OnBeforeTearDown(string testName) {
			if (testName != "DropReferencedTable")
				base.OnBeforeTearDown(testName);
		}

		[Test]
		public void DropNonReferencedTable() {
			var tableName = ObjectName.Parse("APP.test_table2");
			AdminQuery.DropTable(tableName);

			var exists = AdminQuery.Session.Access().TableExists(tableName);
			Assert.IsFalse(exists);
		}

		[Test]
		public void DropIfExists_TableExists() {
			var tableName = ObjectName.Parse("APP.test_table2");

			AdminQuery.DropTable(tableName, true);

			var exists = AdminQuery.Session.Access().TableExists(tableName);
			Assert.IsFalse(exists);
		}

		[Test]
		public void DropIfExists_TableNotExists() {
			var tableName = ObjectName.Parse("APP.test_table3");

			AdminQuery.DropTable(tableName, true);

			var exists = AdminQuery.Session.Access().TableExists(tableName);
			Assert.IsFalse(exists);
		}

		[Test]
		public void DropReferencedTable() {
			var tableName = ObjectName.Parse("APP.test_table1");

			Assert.Throws<DropTableViolationException>(() => AdminQuery.DropTable(tableName));

			var exists = AdminQuery.Session.Access().TableExists(tableName);
			Assert.IsTrue(exists);
		}
	}
}
