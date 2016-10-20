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
using System.Collections.Generic;

using Deveel.Data.Routines;
using Deveel.Data.Security;
using Deveel.Data.Services;
using Deveel.Data.Sql.Compile;
using Deveel.Data.Sql.Query;
using Deveel.Data.Sql.Schemas;
using Deveel.Data.Sql.Sequences;
using Deveel.Data.Sql.Statements;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Sql.Triggers;
using Deveel.Data.Sql.Types;
using Deveel.Data.Sql.Variables;
using Deveel.Data.Sql.Views;
using Deveel.Data.Store;

namespace Deveel.Data {
	public class SystemBuilder : ISystemBuilder {
		public SystemBuilder() {
			ServiceContainer = new ServiceContainer();
		}

		public ServiceContainer ServiceContainer { get; private set; }

		public static SystemBuilder Default {
			get {
				var builder = new SystemBuilder();
				RegisterDefaultServices(builder);
				return builder;
			}
		}

		private static void RegisterDefaultServices(ISystemBuilder builder) {
#if !MICRO
			builder.UseSecurity();

			builder.UseDefaultSqlCompiler()
				.UseDefaultStatementCache();
#endif

			builder.UseDefaultQueryPlanner();

			builder.UseLocalFileSystem()
				.UseInMemoryStoreSystem()
				.UseSingleFileStoreSystem()
				.UseJournaledStoreSystem()
				.UseScatteringFileDataFactory();
		}

		private ISystemContext BuildContext(out IEnumerable<ModuleInfo> modules) {
			modules = LoadModules();

			return new SystemContext(ServiceContainer);
		}

		private IEnumerable<ModuleInfo> LoadModules() {
			var moduleInfo = new List<ModuleInfo>();

			var modules = ServiceContainer.ResolveAll<ISystemModule>();
			foreach (var systemModule in modules) {
				systemModule.Register(ServiceContainer);

				moduleInfo.Add(new ModuleInfo(systemModule.ModuleName, systemModule.Version));
			}

			ServiceContainer.Unregister<ISystemModule>();
			return moduleInfo;
		}

		public ISystem Build() {
			// ensure the required components are configured in the builder
			// TODO: in a future revision they will be optional too
			this.UseTables()
				.UseRoutines()
				.UseSchema()
				.UseViews()
				.UseSequences()
				.UseTriggers()
				.UseTypes()
				.UseVariables();

			IEnumerable<ModuleInfo> modules;
			var context = BuildContext(out modules);
			return new DatabaseSystem(context, modules);
		}
	}
}
