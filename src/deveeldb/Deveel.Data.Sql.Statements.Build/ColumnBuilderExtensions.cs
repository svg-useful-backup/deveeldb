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

using Deveel.Data.Sql.Expressions.Build;
using Deveel.Data.Sql.Tables;

namespace Deveel.Data.Sql.Statements.Build {
	public static class ColumnBuilderExtensions {
		public static IColumnBuilder WithDefault(this IColumnBuilder builder, Action<IExpressionBuilder> expression) {
			var expBuilder = new ExpressionBuilder();
			expression(expBuilder);

			return builder.WithDefault(expBuilder.Build());
		}

		public static IColumnBuilder WithDefault(this IColumnBuilder builder, object value) {
			return builder.WithDefault(exp => exp.Value(value));
		}

		public static IColumnBuilder PrimaryKey(this IColumnBuilder builder) {
			return builder.WithConstraint(new ColumnConstraintInfo(ConstraintType.PrimaryKey));
		}

		public static IColumnBuilder UniqueKey(this IColumnBuilder builder) {
			return builder.WithConstraint(new ColumnConstraintInfo(ConstraintType.Unique));
		}

		public static IColumnBuilder References(this IColumnBuilder builder, Action<IColumnForeignKeyBuilder> foreignKey) {
			var fkeyBuilder = new ForeignKeyBuilder();
			foreignKey(fkeyBuilder);

			return builder.WithConstraint(fkeyBuilder.Build());
		}


		public static IColumnForeignKeyBuilder Table(this IColumnForeignKeyBuilder builder, string tableName) {
			return builder.Table(ObjectName.Parse(tableName));
		}

		#region ForeignKeyBuilder

		class ForeignKeyBuilder : IColumnForeignKeyBuilder {
			private ObjectName refTableName;
			private string refColumnName;
			private ForeignKeyAction? onDelete;
			private ForeignKeyAction? onUpdate;

			public IColumnForeignKeyBuilder Table(ObjectName tableName) {
				if (tableName == null)
					throw new ArgumentNullException("tableName");

				refTableName = tableName;
				return this;
			}

			public IColumnForeignKeyBuilder Column(string columnName) {
				if (String.IsNullOrEmpty(columnName))
					throw new ArgumentNullException("columnName");

				refColumnName = columnName;
				return this;
			}

			public IColumnForeignKeyBuilder OnDelete(ForeignKeyAction action) {
				onDelete = action;
				return this;
			}

			public IColumnForeignKeyBuilder OnUpdate(ForeignKeyAction action) {
				onUpdate = action;
				return this;
			}

			public ColumnConstraintInfo Build() {
				if (refTableName == null)
					throw new InvalidOperationException("The table name referenced is required");
				if (String.IsNullOrEmpty(refColumnName))
					throw new InvalidOperationException("A referenced column is required");

				return new ColumnConstraintInfo(ConstraintType.ForeignKey) {
					ReferencedTable = refTableName,
					ReferencedColumnName = refColumnName,
					ActionOnUpdate = onUpdate ?? ForeignKeyAction.NoAction,
					ActionOnDelete = onDelete ?? ForeignKeyAction.NoAction
				};
			}
		}

		#endregion
	}
}
