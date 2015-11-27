﻿using System;

using Deveel.Data.Security;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Query;

namespace Deveel.Data.Sql.Cursors {
	public static class QueryExtensions {

		public static void DeclareCursor(this IQuery context, CursorInfo cursorInfo) {
			var queryPlan = context.Context.QueryPlanner().PlanQuery(new QueryInfo(context, cursorInfo.QueryExpression));
			var selectedTables = queryPlan.DiscoverTableNames();
			foreach (var tableName in selectedTables) {
				if (!context.UserCanSelectFromTable(tableName))
					throw new MissingPrivilegesException(context.UserName(), tableName, Privileges.Select);
			}

			context.Context.DeclareCursor(cursorInfo);
		}

		public static void DeclareCursor(this IQuery context, string cursorName, SqlQueryExpression query) {
			DeclareCursor(context, cursorName, (CursorFlags)0, query);
		}

		public static void DeclareCursor(this IQuery context, string cursorName, CursorFlags flags, SqlQueryExpression query) {
			context.DeclareCursor(new CursorInfo(cursorName, flags, query));
		}

		public static void DeclareInsensitiveCursor(this IQuery context, string cursorName, SqlQueryExpression query) {
			DeclareInsensitiveCursor(context, cursorName, query, false);
		}

		public static void DeclareInsensitiveCursor(this IQuery context, string cursorName, SqlQueryExpression query, bool withScroll) {
			var flags = CursorFlags.Insensitive;
			if (withScroll)
				flags |= CursorFlags.Scroll;

			context.DeclareCursor(cursorName, flags, query);
		}

		public static bool CursorExists(this IQuery query, string cursorName) {
			return query.Context.CursorExists(cursorName);
		}

		public static bool DropCursor(this IQuery query, string cursorName) {
			return query.Context.DropCursor(cursorName);
		}

		public static Cursor FindCursor(this IQuery query, string cursorName) {
			return query.Context.FindCursor(cursorName);
		}

		public static bool OpenCursor(this IQuery query, string cursorName, params SqlExpression[] args) {
			return query.Context.OpenCursor(query, cursorName, args);
		}

		public static bool CloseCursor(this IQuery query, string cursorName) {
			return query.Context.CloseCursor(cursorName);
		}
	}
}