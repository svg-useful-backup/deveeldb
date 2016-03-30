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

namespace Deveel.Data.Routines {
	public sealed class ExternalProcedure : Procedure {
		public ExternalProcedure(ExternalProcedureInfo procedureInfo) 
			: base(procedureInfo) {
			if (procedureInfo.ExternalRef == null)
				throw new ArgumentNullException("procedureInfo", "The procedure info has no external reference specified.");

			procedureInfo.ExternalRef.CheckReference(procedureInfo);
		}

		public ExternalRef ExternalRef {
			get { return ((ExternalProcedureInfo) ProcedureInfo).ExternalRef; }
		}

		public override InvokeResult Execute(InvokeContext context) {
			throw new NotImplementedException();
		}
	}
}
