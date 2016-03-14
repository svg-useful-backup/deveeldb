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
using System.Collections.Generic;
using System.Linq;

using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Query;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Transactions;

namespace Deveel.Data.Sql.Cursors {
	public sealed class Cursor : IDbObject, IDisposable {
		internal Cursor(CursorInfo cursorInfo) {
			if (cursorInfo == null)
				throw new ArgumentNullException("cursorInfo");

			CursorInfo = cursorInfo;
			State = new CursorState(this);
		}

		~Cursor() {
			Dispose(false);
		}

		public CursorInfo CursorInfo { get; private set; }

		public CursorStatus Status {
			get { return State.Status; }
		}

		public CursorState State { get; private set; }

		ObjectName IDbObject.FullName {
			get { return new ObjectName(CursorInfo.CursorName); }
		}

		DbObjectType IDbObject.ObjectType {
			get { return DbObjectType.Cursor; }
		}

		public SqlQueryExpression QueryExpression {
			get { return CursorInfo.QueryExpression; }
		}

		private void AssertNotDisposed() {
			if (Status == CursorStatus.Disposed)
				throw new ObjectDisposedException("Cursor");
		}

		private SqlQueryExpression PrepareQuery(SqlExpression[] args) {
			SqlQueryExpression query = CursorInfo.QueryExpression;
			if (CursorInfo.Parameters.Count > 0) {
				var cursorArgs = BuildArgs(CursorInfo.Parameters, args);
				var preparer = new CursorArgumentPreparer(cursorArgs);
				query = query.Prepare(preparer) as SqlQueryExpression;
			}

			return query;
		}

		private Dictionary<string, SqlExpression> BuildArgs(IEnumerable<CursorParameter> parameters, SqlExpression[] args) {
			var orderedParams = parameters.OrderBy(x => x.Offset).ToArray();
			if (args == null || args.Length != orderedParams.Length)
				throw new ArgumentException();

			var result = new Dictionary<string, SqlExpression>();
			for (int i = 0; i < orderedParams.Length; i++) {
				var param = orderedParams[i];
				var arg = args[i];
				result[param.ParameterName] = arg;
			}

			return result;
		}

		private ITable Evaluate(IRequest context, SqlExpression[] args, out IList<IDbObject> refs) {
			try {
				var prepared = PrepareQuery(args);
				var queryPlan = context.Query.Context.QueryPlanner().PlanQuery(new QueryInfo(context, prepared));
				var refNames = queryPlan.DiscoverTableNames();

				refs = refNames.Select(x => context.Query.IsolatedAccess.FindObject(x)).ToArray();
				context.Query.Session.Access(refs, AccessType.Read);

				return queryPlan.Evaluate(context);
			} catch (Exception) {

				throw;
			}
		}

		public void Open(IRequest context, params SqlExpression[] args) {
			lock (this) {
				AssertNotDisposed();

				IList<IDbObject> refs = new List<IDbObject>();

				ITable result = null;
				if (CursorInfo.IsInsensitive)
					result = Evaluate(context, args, out refs);

				State.Open(refs.ToArray(), result, args);
			}
		}

		public void Close(IRequest context) {
			lock (this) {
				AssertNotDisposed();

				if (State.IsClosed)
					return;

				context.Query.Session.Exit(State.References, AccessType.Read);

				State.Close();
			}
		}

		public void FetchInto(FetchContext context) {
			if (context == null)
				throw new ArgumentNullException("context");

			if (!CursorInfo.IsScroll &&
				context.Direction != FetchDirection.Next)
				throw new ArgumentException(String.Format("Cursor '{0}' is not SCROLL: can fetch only NEXT value.", CursorInfo.CursorName));

			var table = State.Result;
			if (!CursorInfo.IsInsensitive) {
				IList<IDbObject> refs;
				table = Evaluate(context.Request, State.OpenArguments, out refs);
			}

			var fetchRow = State.FetchRowFrom(table, context.Direction, context.Offset);

			if (context.IsGlobalReference) {
				var reference = ((SqlReferenceExpression) context.Reference).ReferenceName;

				FetchIntoReference(context.Request, fetchRow, reference);
			} else if (context.IsVariableReference) {
				var varName = ((SqlVariableReferenceExpression) context.Reference).VariableName;
				FetchIntoVatiable(context.Request, fetchRow, varName);
			}
		}

		private void FetchIntoVatiable(IRequest request, Row row, string varName) {

			throw new NotImplementedException();
		}

		private void FetchIntoReference(IRequest request, Row row, ObjectName reference) {
			if (reference == null)
				throw new ArgumentNullException("reference");

			var table = request.Query.IsolatedAccess.GetMutableTable(reference);
			if (table == null)
				throw new ObjectNotFoundException(reference);

			try {
				request.Query.Session.Access(table, AccessType.Write);

				var newRow = table.NewRow();

				for (int i = 0; i < row.ColumnCount; i++) {
					var sourceValue = row.GetValue(i);
					newRow.SetValue(i, sourceValue);
				}

				newRow.SetDefault(request.Query);
				table.AddRow(newRow);
			} finally {
				request.Query.Session.Exit(new []{table}, AccessType.Write);
			}

		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				if (State != null)
					State.Dispose();
			}

			State = null;
		}

		#region CursorArgumentPreparer

		class CursorArgumentPreparer : IExpressionPreparer {
			private readonly Dictionary<string, SqlExpression> args;

			public CursorArgumentPreparer(Dictionary<string, SqlExpression> args) {
				this.args = args;
			}

			public bool CanPrepare(SqlExpression expression) {
				return expression is SqlVariableReferenceExpression;
			}

			public SqlExpression Prepare(SqlExpression expression) {
				var varRef = ((SqlVariableReferenceExpression) expression).VariableName;
				SqlExpression exp;
				if (!args.TryGetValue(varRef, out exp))
					throw new ArgumentException(String.Format("Variable '{0}' was not found in the cursor arguments", varRef));

				return exp;
			}
		}

		#endregion
	}
}
