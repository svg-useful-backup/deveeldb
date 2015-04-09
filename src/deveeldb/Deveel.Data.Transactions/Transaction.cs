﻿using System;
using System.Collections.Generic;

using Deveel.Data.DbSystem;
using Deveel.Data.Index;
using Deveel.Data.Sql;
using Deveel.Data.Sql.Objects;
using Deveel.Data.Types;

namespace Deveel.Data.Transactions {
	public sealed class Transaction : ITransaction, ICallbackHandler {
		private TableManager tableManager;
		private SequenceManager sequenceManager;
		// TODO: private ViewManager viewManager;
		private VariableManager variableManager;
		private SchemaManager schemaManager;
		private List<TableCommitCallback> callbacks; 

		private static TableInfo[] IntTableInfo;

		internal Transaction(IDatabase database, long commitId, TransactionIsolation isolation, IEnumerable<TableSource> committedTables, IEnumerable<IIndexSet> indexSets) {
			CommitId = commitId;
			Database = database;
			Isolation = isolation;

			InitManagers();

			Registry = new TransactionRegistry(this);
			tableManager.AddVisibleTables(committedTables, indexSets);

			AddInternalTables();

			State = new TransactionState(this);

			OldNewTableState = new OldNewTableState();

			IsClosed = false;

			Database.OpenTransactions.AddTransaction(this);
		}

		public Transaction(IDatabase database, long commitId, TransactionIsolation isolation)
			: this(database, commitId, isolation, new TableSource[0], new IIndexSet[0]) {
		}

		static Transaction() {
			IntTableInfo = new TableInfo[9];
			IntTableInfo[0] = SystemSchema.TableInfoTableInfo;
			IntTableInfo[1] = SystemSchema.TableColumnsTableInfo;

			/*
			TODO:
			IntTableInfo[2] = SystemSchema.ProductInfoTableInfo;
			IntTableInfo[3] = SystemSchema.VariablesTableInfo;
			IntTableInfo[4] = SystemSchema.StatisticsTableInfo;
			IntTableInfo[5] = SystemSchema.ConnectionInfoTableInfo;
			IntTableInfo[6] = SystemSchema.CurrentConnectionsTableInfo;
			IntTableInfo[7] = SystemSchema.SqlTypesTableInfo;
			IntTableInfo[8] = SystemSchema.PrivilegesTableInfo;
			*/
		}

		public long CommitId { get; private set; }

		object ILockable.RefId {
			get { return CommitId; }
		}

		public TransactionIsolation Isolation { get; private set; }

		private bool IsClosed { get; set; }

		public OldNewTableState OldNewTableState { get; private set; }

		ITransactionContext ITransaction.Context {
			get { return Database; }
		}

		public IDatabase Database { get; private set; }

		public IDatabaseContext DatabaseContext {
			get { return Database.Context; }
		}

		public TableSourceComposite TableComposite {
			get { return Database.TableComposite; }
		}

		public IObjectManagerResolver ObjectManagerResolver { get; private set; }

		public TransactionRegistry Registry { get; private set; }

		public TransactionState State { get; private set; }

		private void InitManagers() {
			schemaManager = new SchemaManager(this);
			tableManager = new TableManager(this, TableComposite);
			sequenceManager = new SequenceManager(this);
			// TODO: viewManager = new ViewManager(this);
			variableManager = new VariableManager(this);

			ObjectManagerResolver = new ObjectManagersResolver(this);
		}

		private void AddInternalTables() {
			tableManager.AddInternalTable(new TransactionTableContainer(this, IntTableInfo));

			// OLD and NEW system tables (if applicable)
			tableManager.AddInternalTable(new OldAndNewTableContainer(this));

			// TODO:
			//// Model views as tables (obviously)
			//tableManager.AddInternalTable(viewManager.CreateInternalTableInfo());

			//// Model procedures as tables
			//tableManager.AddInternalTable(routineManager.CreateInternalTableInfo());

			// Model sequences as tables
			tableManager.AddInternalTable(sequenceManager.TableContainer);

			// Model triggers as tables
			//tableManager.AddInternalTable(triggerManager.CreateInternalTableInfo());
		}

		private void AssertNotReadOnly() {
			if (this.ReadOnly())
				throw new TransactionException(TransactionErrorCodes.ReadOnly, "The transaction is in read-only mode.");
		}

		public SqlNumber SetTableId(ObjectName tableName, SqlNumber value) {
			AssertNotReadOnly();

			return tableManager.SetUniqueId(tableName, value);
		}

		public SqlNumber NextTableId(ObjectName tableName) {
			AssertNotReadOnly();

			return tableManager.NextUniqueId(tableName);
		}

		void ILockable.Acquired(Lock @lock) {
		}

		void ILockable.Released(Lock @lock) {
		}

		public void Commit() {
			if (!IsClosed) {
				try {
					TableComposite.Commit(State);
				} finally {
					Finish();
				}
			}
		}

		private void Finish() {
			try {
				// Dispose all the table we touched
				try {
					tableManager.Dispose();
				} catch (Exception e) {
					// TODO: report the error
				}

				Registry = null;

				if (callbacks != null) {
					foreach (var callback in callbacks) {
						callback.OnTransactionEnd();
						callback.DetachFrom(this);
					}
				}

				callbacks = null;

				// Dispose all the objects in the transaction
			} finally {
				IsClosed = true;
			}
		}

		public void Rollback() {
			try {
				TableComposite.Rollback(State);
			} finally {
				IsClosed = true;
				Finish();
			}
		}

		public void Dispose() {
			Dispose(true);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				if (!IsClosed)
					Rollback();
			}
		}

		#region TransactionTableContainer

		class TransactionTableContainer : ITableContainer {
			private readonly Transaction transaction;
			private readonly TableInfo[] tableInfos;

			public TransactionTableContainer(Transaction transaction, TableInfo[] tableInfo) {
				this.transaction = transaction;
				this.tableInfos = tableInfo;
			}

			public int TableCount {
				get { return tableInfos.Length; }
			}

			public int FindByName(ObjectName name) {
				for (int i = 0; i < tableInfos.Length; i++) {
					var info = tableInfos[i];
					if (info != null && 
						info.TableName.Equals(name, transaction.IgnoreIdentifiersCase()))
						return i;
				}

				return -1;
			}

			public ObjectName GetTableName(int offset) {
				if (offset < 0 || offset >= tableInfos.Length)
					throw new ArgumentOutOfRangeException("offset");

				return tableInfos[offset].TableName;
			}

			public TableInfo GetTableInfo(int offset) {
				if (offset < 0 || offset >= tableInfos.Length)
					throw new ArgumentOutOfRangeException("offset");

				return tableInfos[offset];
			}

			public string GetTableType(int offset) {
				return "SYSTEM TABLE";
			}

			public bool ContainsTable(ObjectName name) {
				return FindByName(name) > 0;
			}

			public ITable GetTable(int offset) {
				if (offset == 0)
					return SystemSchema.GetTableInfoTable(transaction);

				/*
				TODO:
				if (offset == 1)
					return SystemSchema.GetTableColumnsTable(transaction);
				if (offset == 2)
					return SystemSchema.GetProductInfoTable(transaction);
				if (offset == 3)
					return SystemSchema.GetVariablesTable(transaction);
				if (offset == 4)
					return SystemSchema.GetStatisticsTable(transaction);
				if (offset == 5)
					return SystemSchema.GetConnectionInfoTable(transaction);
				if (offset == 6)
					return SystemSchema.GetCurrentConnectionsTable(transaction);
				if (offset == 7)
					return SystemSchema.GetSqlTypesTable(transaction);
				if (offset == 8)
					return SystemSchema.GetPrivilegesTable(transaction);
				*/

				throw new ArgumentOutOfRangeException("offset");
			}
		}

		#endregion

		#region OldAndNewTableContainer

		class OldAndNewTableContainer : ITableContainer {
			private readonly Transaction transaction;

			public OldAndNewTableContainer(Transaction transaction) {
				this.transaction = transaction;
			}

			private bool HasOldTable {
				get { return transaction.OldNewTableState.OldRowIndex != -1; }
			}

			private bool HasNewTable {
				get { return transaction.OldNewTableState.NewDataRow != null; }
			}


			public int TableCount {
				get {
					int count = 0;
					if (HasOldTable)
						++count;
					if (HasNewTable)
						++count;
					return count;
				}
			}

			public int FindByName(ObjectName name) {
				if (HasOldTable &&
				    name.Equals(SystemSchema.OldTriggerTableName, transaction.IgnoreIdentifiersCase()))
					return 0;
				if (HasNewTable &&
				    name.Equals(SystemSchema.NewTriggerTableName, transaction.IgnoreIdentifiersCase()))
					return HasOldTable ? 1 : 0;
				return -1;
			}

			public ObjectName GetTableName(int offset) {
				if (HasOldTable && offset == 0)
					return SystemSchema.OldTriggerTableName;

				return SystemSchema.NewTriggerTableName;
			}

			public TableInfo GetTableInfo(int offset) {
				var tableInfo = transaction.GetTableInfo(transaction.OldNewTableState.TableSource);
				return tableInfo.Alias(GetTableName(offset));
			}

			public string GetTableType(int offset) {
				return "SYSTEM TABLE";
			}

			public bool ContainsTable(ObjectName name) {
				return FindByName(name) > 0;
			}

			public ITable GetTable(int offset) {
				var tableInfo = GetTableInfo(offset);

				var table = new TriggeredOldNew(transaction.DatabaseContext, tableInfo);

				if (HasOldTable) {
					if (offset == 0) {
						// Copy data from the table to the new table
						var dtable = transaction.GetTable(transaction.OldNewTableState.TableSource);
						var oldRow = new Row(table);
						int rowIndex = transaction.OldNewTableState.OldRowIndex;
						for (int i = 0; i < tableInfo.ColumnCount; ++i) {
							oldRow.SetValue(i, dtable.GetValue(rowIndex, i));
						}

						// All OLD tables are immutable
						table.SetReadOnly(true);
						table.SetData(oldRow);

						return table;
					}
				}

				table.SetReadOnly(!transaction.OldNewTableState.IsNewMutable);
				table.SetData(transaction.OldNewTableState.NewDataRow);

				return table;
			}

			#region TriggeredOldNew

			class TriggeredOldNew : GeneratedTable, IMutableTable {
				private readonly TableInfo tableInfo;
				private Row data;
				private bool readOnly;

				public TriggeredOldNew(IDatabaseContext dbContext, TableInfo tableInfo) 
					: base(dbContext) {
					this.tableInfo = tableInfo;
				}

				public override TableInfo TableInfo {
					get { return tableInfo; }
				}

				public override int RowCount {
					get { return 1; }
				}

				public void SetData(Row row) {
					data = row;
				}

				public void SetReadOnly(bool flag) {
					readOnly = flag;
				}

				public override DataObject GetValue(long rowNumber, int columnOffset) {
					if (rowNumber < 0 || rowNumber >= 1)
						throw new ArgumentOutOfRangeException("rowNumber");

					return data.GetValue(columnOffset);
				}

				public TableEventRegistry EventRegistry {
					get { throw new InvalidOperationException(); }
				}

				public void AddRow(Row row) {
					throw new NotSupportedException(String.Format("Inserting data into '{0}' is not allowed.", tableInfo.TableName));
				}

				public void UpdateRow(Row row) {
					if (row.RowId.RowNumber < 0 ||
						row.RowId.RowNumber >= 1)
						throw new ArgumentOutOfRangeException();
					if (readOnly)
						throw new NotSupportedException(String.Format("Updating '{0}' is not permitted.", tableInfo.TableName));

					int sz = TableInfo.ColumnCount;
					for (int i = 0; i < sz; ++i) {
						data.SetValue(i, row.GetValue(i));
					}
				}

				public bool RemoveRow(RowId rowId) {
					throw new NotSupportedException(String.Format("Deleting data from '{0}' is not allowed.", tableInfo.TableName));
				}

				public void FlushIndexes() {
				}

				public void AssertConstraints() {
				}

				public void AddLock() {
				}

				public void RemoveLock() {
				}
			}

			#endregion
		}

		#endregion

		#region ObjectManagersResolver

		class ObjectManagersResolver : IObjectManagerResolver {
			private readonly Transaction transaction;

			public ObjectManagersResolver(Transaction transaction) {
				this.transaction = transaction;
			}

			public IEnumerable<IObjectManager> GetManagers() {
				return new IObjectManager[] {
					transaction.schemaManager,
					transaction.tableManager,
					transaction.sequenceManager,
					transaction.variableManager
				};
			}

			public IObjectManager ResolveForType(DbObjectType objType) {
				if (objType == DbObjectType.Schema)
					return transaction.schemaManager;
				if (objType == DbObjectType.Table)
					return transaction.tableManager;
				if (objType == DbObjectType.Sequence)
					return transaction.sequenceManager;
				if (objType == DbObjectType.Variable)
					return transaction.variableManager;

				return null;
			}
		}

		#endregion

		DataObject IVariableResolver.Resolve(ObjectName variable) {
			throw new NotImplementedException();
		}

		DataType IVariableResolver.ReturnType(ObjectName variable) {
			throw new NotImplementedException();
		}

		void IVariableScope.OnVariableDefined(Variable variable) {
		}

		void IVariableScope.OnVariableDropped(Variable variable) {
		}

		Variable IVariableScope.OnVariableGet(string name) {
			return null;
		}

		void ICallbackHandler.OnCallbackAttached(TableCommitCallback callback) {
			if (callbacks == null)
				callbacks = new List<TableCommitCallback>();

			callbacks.Add(callback);
		}

		void ICallbackHandler.OnCallbackDetached(TableCommitCallback callback) {
			if (callbacks == null)
				return;

			for (int i = callbacks.Count - 1; i >= 0; i--) {
				var other = callbacks[i];
				if (other.TableName.Equals(callback.TableName))
					callbacks.RemoveAt(i);
			}
		}
	}
}
