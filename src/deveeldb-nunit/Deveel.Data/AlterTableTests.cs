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
using System.Linq;

using Deveel.Data.Sql;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Statements;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Sql.Types;

using NUnit.Framework;

namespace Deveel.Data {
	[TestFixture]
	public sealed class AlterTableTests : ContextBasedTest {
		protected override bool OnSetUp(string testName, IQuery query) {
			var tableName1 = ObjectName.Parse("APP.test_table");
			query.Access().CreateTable(table => table
				.Named(tableName1)
				.WithColumn(column => column
					.Named("id")
					.HavingType(PrimitiveTypes.Integer())
					.WithDefault(SqlExpression.FunctionCall("UNIQUEKEY",
						new SqlExpression[] {SqlExpression.Constant(tableName1.FullName)})))
				.WithColumn("first_name", PrimitiveTypes.String())
				.WithColumn("last_name", PrimitiveTypes.String())
				.WithColumn("birth_date", PrimitiveTypes.DateTime())
				.WithColumn("active", PrimitiveTypes.Boolean()));

			query.Access().AddPrimaryKey(tableName1, "id", "PK_TEST_TABLE");

			var tableName2 = ObjectName.Parse("APP.test_table2");

			query.Access().CreateTable(table => table
				.Named(tableName2)
				.WithColumn(column => column
					.Named("person_id")
					.HavingType(PrimitiveTypes.Integer())
					.NotNull(testName.Equals("SetNullOnDeleteViolation")))
				.WithColumn("value", PrimitiveTypes.Boolean()));

			if (testName == "DropConstraint" ||
				testName == "DropReferencedColumn") {
				query.Session.Access().AddForeignKey(tableName2, new string[] {"person_id"}, tableName1,
					new[] {"id"}, ForeignKeyAction.Cascade, ForeignKeyAction.Cascade, "FK_1");
			}

			return true;
		}

		protected override void AssertNoErrors(string testName) {
			if (!testName.Equals("DropReferencedColumn") &&
				!testName.EndsWith("Violation"))
				base.AssertNoErrors(testName);
		}

		protected override bool OnTearDown(string testName, IQuery query) {
			query.Access().DropAllTableConstraints(ObjectName.Parse("APP.test_table"));
			query.Access().DropAllTableConstraints(ObjectName.Parse("APP.test_table2"));
			query.Access().DropObject(DbObjectType.Table, ObjectName.Parse("APP.test_table"));
			query.Access().DropObject(DbObjectType.Table, ObjectName.Parse("APP.test_table2"));
			return true;
		}

		[Test]
		public void AddColumn() {
			var tableName = ObjectName.Parse("test_table");

			AdminQuery.AddColumn(tableName, "reserved", PrimitiveTypes.Boolean());

			var testTable = AdminQuery.Access().GetTable(ObjectName.Parse("APP.test_table"));

			Assert.IsNotNull(testTable);
			Assert.AreEqual(6, testTable.TableInfo.ColumnCount);
		}

		[Test]
		public void SetDefaultToColumn() {
			var tableName = ObjectName.Parse("APP.test_table");

			AdminQuery.SetDefault(tableName, "active", SqlExpression.Constant(Field.Boolean(false)));

			var testTable = AdminQuery.Access().GetTable(ObjectName.Parse("APP.test_table"));

			Assert.IsNotNull(testTable);

			var column = testTable.TableInfo["active"];
			Assert.IsNotNull(column);
			Assert.IsTrue(column.HasDefaultExpression);
			Assert.IsNotNull(column.DefaultExpression);
		}

		[Test]
		public void DropDefaultFromColumn() {
			var tableName = ObjectName.Parse("APP.test_table");

			AdminQuery.DropDefault(tableName, "id");

			var testTable = AdminQuery.Access().GetTable(ObjectName.Parse("APP.test_table"));

			Assert.IsNotNull(testTable);

			var column = testTable.TableInfo["id"];
			Assert.IsNotNull(column);
			Assert.IsFalse(column.HasDefaultExpression);
			Assert.IsNull(column.DefaultExpression);
		}

		[Test]
		public void AddForeignKeyConstraint() {
			var tableName = ObjectName.Parse("APP.test_table2");
			var constraint = new SqlTableConstraint("FK_1", ConstraintType.ForeignKey, new[] { "person_id" }) {
				ReferenceTable = "APP.test_table",
				ReferenceColumns = new[] { "id" }
			};

			AdminQuery.AddConstraint(tableName, constraint);

			var fkeys = AdminQuery.Session.Access().QueryTableForeignKeys(tableName);

			Assert.IsNotNull(fkeys);
			Assert.IsNotEmpty(fkeys);

			var fkey = fkeys.FirstOrDefault(x => x.ConstraintName == "FK_1");
			Assert.IsNotNull(fkey);
			Assert.IsNotNull(fkey.ForeignTable);
			Assert.AreEqual("APP.test_table", fkey.ForeignTable.FullName);
			Assert.IsNotNull(fkey.ForeignColumnNames);
			Assert.IsNotEmpty(fkey.ForeignColumnNames);
		}

		[Test]
		public void DropColumn() {
			var tableName = ObjectName.Parse("APP.test_table");

			AdminQuery.DropColumn(tableName, "active");

			var testTable = AdminQuery.Access().GetTable(ObjectName.Parse("APP.test_table"));

			Assert.IsNotNull(testTable);

			Assert.AreEqual(-1, testTable.TableInfo.IndexOfColumn("active"));
		}

		[Test]
		public void DropConstraint() {
			var tableName = ObjectName.Parse("APP.test_table2");

			AdminQuery.DropConstraint(tableName, "FK_1");

			var fkeys = AdminQuery.Session.Access().QueryTableForeignKeys(tableName);

			Assert.IsNotNull(fkeys);
			Assert.IsEmpty(fkeys);
		}

		[Test]
		public void DropPrimary() {
			var tableName = ObjectName.Parse("APP.test_table");

			AdminQuery.DropPrimaryKey(tableName);

			var pkey = AdminQuery.Session.Access().QueryTablePrimaryKey(tableName);

			Assert.IsNull(pkey);
		}

		[Test]
		public void DropReferencedColumn() {
			var tableName = ObjectName.Parse("APP.test_table2");

			var expected = Is.InstanceOf<ConstraintViolationException>()
				.And.TypeOf<DropColumnViolationException>();

			Assert.Throws(expected, () => AdminQuery.DropColumn(tableName, "person_id"));
		}

		[Test]
		public void SetNullOnDeleteViolation() {
			var expected = Is.InstanceOf<ConstraintViolationException>()
				.And.TypeOf<NotNullColumnViolationException>()
				.And.Property("TableName").EqualTo(ObjectName.Parse("APP.test_table2"))
				.And.Property("ColumnName").EqualTo("person_id");
			Assert.Throws(expected, () => AdminQuery.AddForeignKey(ObjectName.Parse("test_table2"), new[] {"person_id"}, ObjectName.Parse("test_table"),
					new[] {"id"}, ForeignKeyAction.SetNull, ForeignKeyAction.NoAction));
		}
	}
}
