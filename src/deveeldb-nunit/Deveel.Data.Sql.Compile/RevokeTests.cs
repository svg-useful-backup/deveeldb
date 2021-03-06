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

using Deveel.Data.Security;
using Deveel.Data.Sql.Statements;

using NUnit.Framework;

namespace Deveel.Data.Sql.Compile {
	[TestFixture]
	public sealed class RevokeTests : SqlCompileTestBase {
		[Test]
		public void RevokeFromOneUserOnAnObject() {
			const string sql = "REVOKE UPDATE, INSERT ON test_table FROM test_user";

			var result = Compile(sql);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);
			
			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.ElementAt(0);

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<RevokePrivilegesStatement>(statement);

			var revoke = (RevokePrivilegesStatement) statement;

			Assert.AreEqual("test_user", revoke.Grantee);
			Assert.IsNotNull(revoke.ObjectName);
			Assert.AreEqual("test_table", revoke.ObjectName.FullName);
			Assert.AreEqual(Privileges.Insert | Privileges.Update, revoke.Privileges);
		}

		[Test]
		public void RevokeRoleFromUser() {
			const string sql = "REVOKE dbadmin FROM user1";

			var result = Compile(sql);

			Assert.IsNotNull(result);
			Assert.IsFalse(result.HasErrors);

			Assert.AreEqual(1, result.Statements.Count);

			var statement = result.Statements.ElementAt(0);

			Assert.IsNotNull(statement);
			Assert.IsInstanceOf<RevokeRoleStatement>(statement);

			var revoke = (RevokeRoleStatement) statement;

			Assert.AreEqual("dbadmin", revoke.RoleName);
			Assert.AreEqual("user1", revoke.Grantee);
		}
	}
}
