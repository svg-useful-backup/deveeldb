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

using Deveel.Data.Services;

namespace Deveel.Data.Sql.Types {
	public static class SystemContextExtensions {
		public static SqlType ResolveType(this ISystemContext context, string typeName, params DataTypeMeta[] meta) {
			var resolvers = context.ResolveAllServices<ITypeResolver>();
			var resolveContext = new TypeResolveContext(SqlTypeCode.Type, typeName, meta);
			foreach (var typeResolver in resolvers) {
				var type = typeResolver.ResolveType(resolveContext);
				if (type != null)
					return type;
			}

			return null;
		}
	}
}