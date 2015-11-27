﻿using System;

using Deveel.Data.Security;
using Deveel.Data.Services;
using Deveel.Data.Sql;
using Deveel.Data.Sql.Expressions;

namespace Deveel.Data.Routines {
	public static class Query {
		public static bool IsSystemFunction(this IQuery query, Invoke invoke) {
			var info = query.ResolveFunctionInfo(invoke);
			if (info == null)
				return false;

			return info.FunctionType != FunctionType.External &&
				   info.FunctionType != FunctionType.UserDefined;
		}

		public static bool IsAggregateFunction(this IQuery query, Invoke invoke) {
			var function = query.ResolveFunction(invoke);
			return function != null && function.FunctionType == FunctionType.Aggregate;
		}

		public static IRoutine ResolveRoutine(this IQuery query, Invoke invoke) {
			var routine = query.ResolveSystemRoutine(invoke);
			if (routine == null)
				routine = query.ResolveUserRoutine(invoke);

			return routine;
		}

		public static IRoutine ResolveSystemRoutine(this IQuery query, Invoke invoke) {
			// return query.SystemContext().ResolveRoutine(invoke, query);

			var resolvers = query.Context.ResolveAllServices<IRoutineResolver>();
			foreach (var resolver in resolvers) {
				var routine = resolver.ResolveRoutine(invoke, query);
				if (routine != null)
					return routine;
			}

			return null;
		}

		public static IRoutine ResolveUserRoutine(this IQuery query, Invoke invoke) {
			var routine = query.Session.ResolveRoutine(invoke);
			if (routine != null &&
				!query.UserCanExecute(routine.Type, invoke))
				throw new InvalidOperationException();

			return routine;
		}

		public static IFunction ResolveFunction(this IQuery query, Invoke invoke) {
			return query.ResolveRoutine(invoke) as IFunction;
		}

		public static IFunction ResolveFunction(this IQuery query, ObjectName functionName, params SqlExpression[] args) {
			var invoke = new Invoke(functionName, args);
			return query.ResolveFunction(invoke);
		}

		public static FunctionInfo ResolveFunctionInfo(this IQuery query, Invoke invoke) {
			return query.ResolveRoutineInfo(invoke) as FunctionInfo;
		}

		public static RoutineInfo ResolveRoutineInfo(this IQuery query, Invoke invoke) {
			var routine = query.ResolveRoutine(invoke);
			if (routine == null)
				return null;

			return routine.RoutineInfo;
		}

		public static DataObject InvokeSystemFunction(this IQuery query, string functionName,
			params SqlExpression[] args) {
			var resolvedName = new ObjectName(SystemSchema.SchemaName, functionName);
			var invoke = new Invoke(resolvedName, args);
			return query.InvokeFunction(invoke);
		}

		public static DataObject InvokeFunction(this IQuery query, Invoke invoke) {
			var result = invoke.Execute(query);
			return result.ReturnValue;
		}

		public static DataObject InvokeFunction(this IQuery query, ObjectName functionName,
			params SqlExpression[] args) {
			return query.InvokeFunction(new Invoke(functionName, args));
		}
	}
}