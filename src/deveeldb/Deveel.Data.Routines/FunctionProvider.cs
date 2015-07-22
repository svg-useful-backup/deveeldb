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

using Deveel.Data.DbSystem;
using Deveel.Data.Sql.Fluid;
using Deveel.Data.Sql;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Objects;
using Deveel.Data.Types;

namespace Deveel.Data.Routines {
	public abstract class FunctionProvider : IRoutineResolver, IConfigurationContext {
		private bool initd;
		private readonly IList<FunctionConfiguration> configurations;
		private IDictionary<FunctionInfo, IFunction> functions;

		protected FunctionProvider() {
			configurations = new List<FunctionConfiguration>();
		}

		public abstract string SchemaName { get; }

		public IRoutine ResolveRoutine(Invoke request, IQueryContext context) {
			if (functions == null)
				return null;

			var functionName = NormalizeName(request.RoutineName);
			var normInvoke = new Invoke(functionName, request.Arguments);

			return (functions.Where(entry => entry.Key.MatchesInvoke(normInvoke, context))
				.Select(entry => entry.Value))
				.FirstOrDefault();
		}

		protected virtual ObjectName NormalizeName(ObjectName functionName) {
			return functionName;
		}

		public void Init() {
			if (!initd) {
				OnInit();

				BuildFunctions();
				initd = true;
			}
		}

		private void BuildFunctions() {
			if (functions == null)
				functions = new Dictionary<FunctionInfo, IFunction>();

			foreach (var configuration in configurations) {
				var functionInfo = configuration.FunctionInfo;
				foreach (var info in functionInfo) {
					var function = configuration.AsFunction();
					functions.Add(info, function);
				}
			}
		}

		protected abstract void OnInit();

		protected IFunctionConfiguration New() {
			var config = new FunctionConfiguration(this);
			configurations.Add(config);
			return config;
		}

		protected IFunctionConfiguration New(string name) {
			return New().Named(new ObjectName(new ObjectName(SchemaName), name));
		}

		protected void New(FunctionInfo info, IFunction function) {
			if (functions == null)
				functions = new Dictionary<FunctionInfo, IFunction>();

			functions.Add(info, function);
		}

		#region FunctionConfiguration

		class FunctionConfiguration : IAggregateFunctionConfiguration, IRoutineConfiguration {
			private readonly FunctionProvider provider;
			private readonly Dictionary<string, RoutineParameter> parameters;
			private List<ObjectName> aliases;

			private Func<ExecuteContext, DataType> returnTypeFunc;
			private Func<ExecuteContext, ExecuteResult> executeFunc;
			private Func<DataObject, DataObject, DataObject> simpleExecuteFunc;
			private Func<ExecuteContext, DataObject, DataObject> afterAggregateFunc;

			public FunctionConfiguration(FunctionProvider provider) {
				this.provider = provider;
				parameters = new Dictionary<string, RoutineParameter>();
				FunctionType = FunctionType.Static;
			}

			public FunctionType FunctionType { get; private set; }

			public FunctionInfo[] FunctionInfo {
				get {
					var result = new List<FunctionInfo> {new FunctionInfo(FunctionName, parameters.Values.ToArray())};
					if (aliases != null && aliases.Count > 0)
						result.AddRange(aliases.Select(name => new FunctionInfo(name, parameters.Values.ToArray())));

					return result.ToArray();
				}
			}

			public ObjectName FunctionName { get; private set; }

			public RoutineParameter[] Parameters {
				get { return parameters.Values.ToArray(); }
			}

			public bool HasParameter(string name) {
				return parameters.ContainsKey(name);
			}

			public bool HasUnboundedParameter() {
				return parameters.Values.Any(x => x.IsUnbounded);
			}

			public DataType ReturnType(ExecuteContext context) {
				if (returnTypeFunc != null)
					return returnTypeFunc(context);

				throw new InvalidOperationException();
			}

			public IFunctionConfiguration Named(ObjectName name) {
				if (name == null)
					throw new ArgumentNullException("name");

				var parent = name.ParentName;

				if (!provider.SchemaName.Equals(parent))
					throw new ArgumentException(String.Format(
						"The parent name ({0}) is not valid in this provider schema context ({1})", parent, provider.SchemaName));

				FunctionName = name;
				return this;
			}

			public IFunctionConfiguration WithAlias(ObjectName alias) {
				if (alias == null)
					throw new ArgumentNullException("alias");

				if (FunctionName == null)
					throw new ArgumentException("The function has no name configured and cannot be aliased.");

				var parent = alias.ParentName;

				if (!provider.SchemaName.Equals(parent))
					throw new ArgumentException();

				if (aliases == null)
					aliases = new List<ObjectName>();

				aliases.Add(alias);

				return this;
			}

			public IFunctionConfiguration WithParameter(Action<IFunctionParameterConfiguration> config) {
				var paramConfig = new FunctionParameterConfiguration(this);
				if (config != null) {
					config(paramConfig);

					var param = paramConfig.AsParameter();

					if (String.IsNullOrEmpty(param.Name))
						throw new InvalidOperationException("A parameter must define a name.");

					parameters.Add(param.Name, param);
				}

				return this;
			}

			public IAggregateFunctionConfiguration Aggregate() {
				FunctionType = FunctionType.Aggregate;
				return this;
			}

			public IFunctionConfiguration ReturnsType(Func<ExecuteContext, DataType> returns) {
				returnTypeFunc = returns;
				return this;
			}

			public IFunctionConfiguration WhenExecute(Func<ExecuteContext, ExecuteResult> execute) {
				executeFunc = execute;
				return this;
			}

			public IFunctionConfiguration WhenExecute(Func<DataObject, DataObject, DataObject> execute) {
				simpleExecuteFunc = execute;
				return this;
			}

			public IAggregateFunctionConfiguration OnAfterAggregate(Func<ExecuteContext, DataObject, DataObject> afterAggregate) {
				if (FunctionType != FunctionType.Aggregate)
					throw new InvalidOperationException("The function is not aggregate.");

				afterAggregateFunc = afterAggregate;
				return this;
			}

			public IFunction AsFunction() {
				return new ConfiguredFunction(this);
			}

			public ExecuteResult Execute(ExecuteContext context) {
				if (executeFunc == null)
					throw new InvalidOperationException("The function has no body defined");

				if (FunctionType == FunctionType.Aggregate) {
					if (context.GroupResolver == null)
						throw new InvalidOperationException(String.Format("routine '{0}' can only be used as an aggregate function.",
							FunctionName));

					DataObject result = null;

					// All aggregates functions return 'null' if group size is 0
					int size = context.GroupResolver.Count;
					if (size == 0) {
						// Return a NULL of the return type
						return context.Result(new DataObject(ReturnType(context), SqlNull.Value));
					}

					DataObject val;
					var v = context.Arguments[0].AsReferenceName();

					// If the aggregate parameter is a simple variable, then use optimal
					// routine,
					if (v != null) {
						for (int i = 0; i < size; ++i) {
							val = context.GroupResolver.Resolve(v, i);

							if (simpleExecuteFunc != null) {
								result = simpleExecuteFunc(result, val);
							} else {
								var args = new SqlExpression[] {
									SqlExpression.Constant(result),
									SqlExpression.Constant(val)
								};

								var newRequest = new Invoke(FunctionName, args);
								var tempContext = new ExecuteContext(newRequest, context.Routine, context.VariableResolver,
									context.GroupResolver, context.QueryContext);

								var execResult = executeFunc(tempContext);

								if (!execResult.HasReturnValue)
									throw new InvalidOperationException();

								result = execResult.ReturnValue;
							}
						}
					} else {
						// Otherwise we must resolve the expression for each entry in group,
						// This allows for expressions such as 'sum(quantity * price)' to
						// work for a group.
						SqlExpression exp = context.Arguments[0];
						for (int i = 0; i < size; ++i) {
							val = exp.EvaluateToConstant(context.QueryContext, context.GroupResolver.GetVariableResolver(i));

							if (simpleExecuteFunc != null) {
								result = simpleExecuteFunc(result, val);
							} else {
								var args = new SqlExpression[] {
									SqlExpression.Constant(result),
									SqlExpression.Constant(val)
								};

								var newRequest = new Invoke(FunctionName, args);
								var tempContext = new ExecuteContext(newRequest, context.Routine, context.VariableResolver,
									context.GroupResolver, context.QueryContext);

								var execResult = executeFunc(tempContext);

								if (!execResult.HasReturnValue)
									throw new InvalidOperationException();

								result = execResult.ReturnValue;
							}
						}
					}

					// Post method.
					if (afterAggregateFunc != null)
						result = afterAggregateFunc(context, result);

					return context.Result(result);
				}

				return executeFunc(context);
			}

			public IConfigurationContext Context {
				get { return provider; }
			}
		}

		#endregion

		#region FunctionParemeterConfiguration

		class FunctionParameterConfiguration : IFunctionParameterConfiguration {
			private readonly FunctionConfiguration configuration;

			private string parameterName;
			private DataType dataType;
			private ParameterAttributes attributes;

			public FunctionParameterConfiguration(FunctionConfiguration configuration) {
				this.configuration = configuration;

				attributes = new ParameterAttributes();
				dataType = PrimitiveTypes.Numeric();
			}

			public IFunctionParameterConfiguration Named(string name) {
				if (String.IsNullOrEmpty(name))
					throw new ArgumentNullException("name");

				if (configuration.HasParameter(name))
					throw new ArgumentException(String.Format("A parameter with name '{0}' was already configured for the function", name), "name");

				parameterName = name;

				return this;
			}

			public IFunctionParameterConfiguration OfType(DataType type) {
				if (type == null)
					throw new ArgumentNullException("type");

				dataType = type;

				return this;
			}

			public IFunctionParameterConfiguration Unbounded(bool flag) {
				if (configuration.HasUnboundedParameter())
					throw new ArgumentException("An unbounded parameter is already configured");

				if (flag)
					attributes |= ParameterAttributes.Unbounded;

				return this;
			}

			public RoutineParameter AsParameter() {
				return new RoutineParameter(parameterName, dataType, attributes);
			}
		}

		#endregion

		#region ConfiguredFunction

		class ConfiguredFunction : IFunction {
			private readonly FunctionConfiguration configuration;

			public ConfiguredFunction(FunctionConfiguration configuration) {
				this.configuration = configuration;
			}

			RoutineType IRoutine.Type {
				get { return RoutineType.Function; }
			}

			private FunctionInfo FunctionInfo {
				get { return new FunctionInfo(configuration.FunctionName, configuration.Parameters, configuration.FunctionType); }
			}

			RoutineInfo IRoutine.RoutineInfo {
				get { return FunctionInfo; }
			}

			DbObjectType IDbObject.ObjectType {
				get { return DbObjectType.Routine; }
			}

			public ObjectName FullName {
				get { return configuration.FunctionName; }
			}

			public ExecuteResult Execute(ExecuteContext context) {
				return configuration.Execute(context);
			}

			public FunctionType FunctionType {
				get { return configuration.FunctionType; }
			}

			public DataType ReturnType(ExecuteContext context) {
				return configuration.ReturnType(context);
			}
		}

		#endregion
	} 
}