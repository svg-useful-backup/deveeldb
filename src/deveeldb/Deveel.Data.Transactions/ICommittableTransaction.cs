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

using Deveel.Data.DbSystem;
using Deveel.Data.Query;

namespace Deveel.Data.Transactions {
	public interface ICommitableTransaction : ITransaction {
		// Tables

		void CreateTable(DataTableInfo tableInfo, int dataSectorSize, int indexSectorSize);

		void AlterCreateTable(DataTableInfo tableInfo, int dataSectorSize, int indexSectorSize);

		void CreateTemporaryTable(DataTableInfo tableInfo);

		void AlterTable(TableName tableName, DataTableInfo tableInfo, int dataSectorSize, int indexSectorSize);

		void DropTable(TableName tableName);

		IMutableTableDataSource GetMutableTable(TableName tableName);

		void AddSelectedFromTable(TableName tableName);

		void CompactTable(TableName tableName);

		void OnDatabaseObjectCreated(TableName objName);

		void OnDatabaseObjectDropped(TableName obName);

		void CheckAllConstraints(TableName tableName);


		// Sequences

		void CreateSequenceGenerator(TableName name, long startValue, long incrementBy, long minValue, long maxValue, long cache, bool cycle);

		void DropSequenceGenerator(TableName name);

		// Cursors

		Cursor DeclareCursor(TableName name, IQueryPlanNode queryPlan, CursorAttributes attributes);

		void DropCursor(TableName name);

		void Commit();

		void Rollback();
	}
}