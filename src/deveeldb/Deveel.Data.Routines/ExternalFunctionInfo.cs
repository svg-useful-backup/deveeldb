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

using Deveel.Data.Sql;
using Deveel.Data.Sql.Types;

namespace Deveel.Data.Routines {
	public sealed class ExternalFunctionInfo : FunctionInfo {
		public ExternalFunctionInfo(ObjectName functionName, RoutineParameter[] parameters, SqlType returnType,
			ExternalRef externalRef) 
			: base(functionName, parameters, returnType, FunctionType.Static) {
			if (externalRef == null)
				throw new ArgumentNullException("externalRef");

			ExternalRef = externalRef;
		}

		public ExternalRef ExternalRef { get; private set; }
	}
}
