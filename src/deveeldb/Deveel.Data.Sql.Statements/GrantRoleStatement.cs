﻿// 
//  Copyright 2010-2016 Deveel
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

using Deveel.Data.Security;

namespace Deveel.Data.Sql.Statements {
	public sealed class GrantRoleStatement : SqlStatement {
		public GrantRoleStatement(string userName, string role) 
			: this(userName, role, false) {
		}

		public GrantRoleStatement(string userName, string role, bool withAdmin) {
			if (String.IsNullOrEmpty(userName))
				throw new ArgumentNullException("userName");
			if (String.IsNullOrEmpty(role))
				throw new ArgumentNullException("role");

			UserName = userName;
			Role = role;
			WithAdmin = withAdmin;
		}

		public string Role { get; private set; }

		public string UserName { get; private set; }

		public bool WithAdmin { get; private set; }

		protected override void ExecuteStatement(ExecutionContext context) {
			if (!context.DirectAccess.UserExists(UserName))
				throw new InvalidOperationException(String.Format("The user '{0}' does not exist", UserName));
			if (!context.DirectAccess.RoleExists(Role))
				throw new InvalidOperationException(String.Format("The role '{0}' does not exist", Role));

			if (!context.User.CanGrantRole(Role))
				throw new SecurityException(String.Format("User '{0}' cannot grant role '{1}' to '{2}'.", context.User.Name, Role, UserName));

			if (WithAdmin) {
				if (!context.User.IsRoleAdmin(Role))
					throw new SecurityException(String.Format("User '{0}' does not administrate role '{1}'.", context.User, Role));
			}

			if (!context.User.CanManageUsers())
				throw new SecurityException(String.Format("The user '{0}' has not enough rights to manage other users.", context.User.Name));

			if (!context.DirectAccess.UserIsInRole(UserName, Role)) {
				context.Request.Access().AddUserToRole(UserName, Role, WithAdmin);
			} else if (WithAdmin &&
			           !context.User.IsRoleAdmin(Role)) {
				throw new NotImplementedException();
			}
		}
	}
}
