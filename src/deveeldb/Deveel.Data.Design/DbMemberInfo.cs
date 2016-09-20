﻿using System;
using System.Reflection;

using Deveel.Data.Sql;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Sql.Types;

namespace Deveel.Data.Design {
	public sealed class DbMemberInfo {
		internal DbMemberInfo(TypeBuildMemberInfo buildInfo) {
			BuildInfo = buildInfo;
		}

		private TypeBuildMemberInfo BuildInfo { get; set; }

		public MemberInfo Member {
			get { return BuildInfo.Member; }
		}

		public Type MemberType {
			get { return BuildInfo.MemberType; }
		}

		public DbTypeInfo TypeInfo {
			get { return new DbTypeInfo(BuildInfo.TypeInfo); }
		}

		public string ColumnName {
			get { return BuildInfo.ColumnName; }
		}

		public string FullColumnName {
			get { return String.Format("{0}.{1}", TypeInfo.TableName, ColumnName); }
		}

		public SqlType ColumnType {
			get { return BuildInfo.ColumnType; }
		}

		public bool NotNull {
			get { return BuildInfo.NotNull; }
		}

		internal void ApplyFromRow(object obj, Row row) {
			if (row == null)
				throw new ArgumentNullException("row");

			if (String.IsNullOrEmpty(ColumnName))
				throw new InvalidOperationException(String.Format("No column name was set for the member {0} in type '{1}'.",
					Member.Name, TypeInfo.Type));

			var columnOffset = row.Table.TableInfo.IndexOfColumn(ColumnName);
			if (columnOffset == -1)
				throw new InvalidOperationException(
					String.Format(
						"The member '{0}' of type '{1}' is mapped to the column '{2}' of table '{3}' not found in selected row structure.",
						Member.Name, TypeInfo.Type, ColumnName, TypeInfo.TableName));

			var columnValue = row.GetValue(columnOffset);

			if (Field.IsNullField(columnValue) &&
			    NotNull)
				throw new InvalidOperationException(
					String.Format("The member '{0}' of type '{1}' is marked as NOT NULL but the selected field is NULL", Member.Name,
						TypeInfo.Type));

			var finalValue = columnValue.CastTo(ColumnType);
			var value = finalValue.ConvertTo(MemberType);

			Apply(obj, value);
		}

		internal void Apply(object obj, object value) {
			if (obj == null)
				throw new ArgumentNullException("obj");
			if (value == null && NotNull)
				throw new InvalidOperationException(
					String.Format("The member '{0}' of type '{1}' is marked as NOT NULL but the selected field is NULL", Member.Name,
						TypeInfo.Type));

			if (Member is PropertyInfo) {
				((PropertyInfo) Member).SetValue(obj, value, null);
			} else if (Member is FieldInfo) {
				((FieldInfo) Member).SetValue(obj, value);
			}
		}
	}
}