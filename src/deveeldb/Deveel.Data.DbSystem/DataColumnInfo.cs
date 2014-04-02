// 
//  Copyright 2010  Deveel
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
using System.IO;

using Deveel.Data.Sql;
using Deveel.Data.Types;

namespace Deveel.Data.DbSystem {
	/// <summary>
	/// Used to managed all the informations about a column in a table
	/// (<see cref="DataTableInfo"/>).
	/// </summary>
	[Serializable]
	public sealed class DataColumnInfo : ICloneable {
		private DataTableInfo tableInfo;

		/// <summary>
		/// A flag indicating if the column must allow only not-null values.
		/// </summary>
		private bool notNull;

		/// <summary>
		/// If this is an object column, this is a constraint that the object
		/// must be derived from to be added to this column.  If not specified,
		/// it defaults to <see cref="object"/>.
		/// </summary>
		private String baseTypeConstraint = "";

		/// <summary>
		/// The constraining Type object itself.
		/// </summary>
		private Type baseType;

		/// <summary>
		/// The default expression string.
		/// </summary>
		private string defaultExpressionString;

		/// <summary>
		/// The type of index to use on this column.
		/// </summary>
		private string indexType = "";

		/// <summary>
		/// The name of the column.
		/// </summary>
		private string name;

		/// <summary>
		/// The TType object for this column.
		/// </summary>
		private readonly TType type;

		internal DataColumnInfo(DataTableInfo tableInfo, string name, TType type) {
			if (name == null)
				throw new ArgumentNullException("name");
			if (type == null) 
				throw new ArgumentNullException("type");

			this.tableInfo = tableInfo;
			this.name = name;
			this.type = type;
		}

		public DataTableInfo TableInfo {
			get { return tableInfo; }
			internal set { tableInfo = value; }
		}

		///<summary>
		///</summary>
		public string Name {
			get { return name; }
			internal set { name = value; }
		}


		///<summary>
		///</summary>
		public bool IsNotNull {
			get { return notNull; }
			set { notNull = value; }
		}

		public SqlType SqlType {
			get { return type.SqlType; }
		}

		///<summary>
		///</summary>
		public int Size {
			get { return (type is ISizeableType) ? ((ISizeableType)type).Size : -1; }
		}

		///<summary>
		///</summary>
		public int Scale {
			get { return (type is TNumericType) ? ((TNumericType)type).Scale : -1; }
		}

		/// <summary>
		/// Gets or sets the name of the scheme we use to index this column.
		/// </summary>
		/// <remarks>
		/// It will be either <i>InsertSearch</i> or <b>BlindSearch</b>.
		/// </remarks>
		public string IndexScheme {
			get {
				if (indexType.Equals(""))
					return "InsertSearch";
				return indexType;
			}
			set { indexType = value; }
		}

		/// <summary>
		/// Gets <b>true</b> if this type of column is able to be indexed,
		/// otherwise <b>false</b>.
		/// </summary>
		public bool IsIndexableType {
			get { return type.DbType != DbType.Blob && type.DbType != DbType.Object; }
		}

		///<summary>
		/// If this column represents an object, gets or sets the name of 
		/// the type the object must be derived from to be added to the
		/// column.
		///</summary>
		///<exception cref="ApplicationException"></exception>
		public string TypeConstraintString {
			get { return baseTypeConstraint; }
			set {
				baseTypeConstraint = value;
				try {
					// Denotes an array
					if (value.EndsWith("[]")) {
						string arrayType = value.Substring(0, value.Length - 2);
						Type elementType;
						// Arrays of primitive types,
						if (arrayType.Equals("bool")) {
							elementType = typeof (bool);
						} else if (arrayType.Equals("byte")) {
							elementType = typeof (byte);
						} else if (arrayType.Equals("char")) {
							elementType = typeof (char);
						} else if (arrayType.Equals("short")) {
							elementType = typeof (short);
						} else if (arrayType.Equals("int")) {
							elementType = typeof (int);
						} else if (arrayType.Equals("long")) {
							elementType = typeof (long);
						} else if (arrayType.Equals("float")) {
							elementType = typeof (float);
						} else if (arrayType.Equals("double")) {
							elementType = typeof (double);
						} else {
							// Otherwise a standard array.
							elementType = Type.GetType(arrayType, true, true);
						}
						// Make it into an array
						baseType = Array.CreateInstance(elementType, 0).GetType();
					} else {
						// Not an array
						baseType = Type.GetType(value, true, true);
					}
				} catch (TypeLoadException) {
					throw new ApplicationException("Unable to resolve class: " + value);
				}
			}
		}

		/// <summary>
		/// If this column represents a <see cref="object"/>, this returns the
		/// <see cref="Type"/> the objects stored in the column must be derived from.
		/// </summary>
		public Type TypeConstraint {
			get { return baseType; }
		}

		/// <summary>
		/// Returns the TType for this column.
		/// </summary>
		public TType TType {
			get { return type; }
		}

		///<summary>
		///</summary>
		///<param name="expression"></param>
		public void SetDefaultExpression(Expression expression) {
			if (expression == null) {
				defaultExpressionString = null;
			} else {
				defaultExpressionString = expression.Text.ToString();
			}
		}


		///<summary>
		///</summary>
		///<param name="context></param>
		///<returns></returns>
		public Expression GetDefaultExpression(SystemContext context) {
			if (defaultExpressionString == null)
				return null;

			return Expression.Parse(defaultExpressionString);
		}

		///<summary>
		///</summary>
		///<returns></returns>
		public String GetDefaultExpressionString() {
			return defaultExpressionString;
		}

		/// <summary>
		/// Dumps information about this object to the <see cref="TextWriter"/>.
		/// </summary>
		/// <param name="output"></param>
		public void Dump(TextWriter output) {
			output.Write(Name);
			output.Write(" ");
			output.Write(type.ToSqlString());
		}


		// ---------- IO Methods ----------

		/// <summary>
		/// Writes this column information output to a <see cref="BinaryWriter"/>.
		/// </summary>
		/// <param name="output"></param>
		internal void Write(BinaryWriter output) {
			output.Write(3); // The version

			output.Write(name);
			TType.ToBinaryWriter(type, output);
			output.Write(notNull);

			if (defaultExpressionString != null) {
				output.Write(true);
				output.Write(defaultExpressionString);
				//new String(default_exp.text().toString()));
			} else {
				output.Write(false);
			}

			output.Write(indexType);
			output.Write(baseTypeConstraint); // Introduced input version 2.
		}

		/// <summary>
		/// Reads this column from a <see cref="BinaryReader"/>.
		/// </summary>
		/// <param name="tableInfo"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		internal static DataColumnInfo Read(DataTableInfo tableInfo, BinaryReader input) {
			int ver = input.ReadInt32();

			string name = input.ReadString();
			TType type = TType.ReadFrom(input);
			DataColumnInfo cd = new DataColumnInfo(tableInfo, name, type);
			
			cd.notNull = input.ReadBoolean();

			bool hasExpression = input.ReadBoolean();
			if (hasExpression) {
				cd.defaultExpressionString = input.ReadString();
				//      cd.default_exp = Expression.Parse(input.readUTF());
			}

			cd.indexType = input.ReadString();
			if (ver > 1) {
				string cc = input.ReadString();
				if (!cc.Equals("")) {
					cd.TypeConstraintString = cc;
				}
			} else {
				cd.baseTypeConstraint = "";
			}

			return cd;
		}

		object ICloneable.Clone() {
			return Clone();
		}

		public DataColumnInfo Clone() {
			DataColumnInfo clone = new DataColumnInfo(tableInfo, (string)name.Clone(), type);
			clone.notNull = notNull;
			if (!String.IsNullOrEmpty(defaultExpressionString)) {
				clone.defaultExpressionString = (string) defaultExpressionString.Clone();
			}
			clone.indexType = (string)indexType.Clone();
			clone.baseTypeConstraint = (string)baseTypeConstraint.Clone();
			return clone;
		}
	}
}