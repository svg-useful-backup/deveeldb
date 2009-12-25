﻿//  
//  SqlConstraint.cs
//  
//  Author:
//       Antonello Provenzano <antonello@deveel.com>
// 
//  Copyright (c) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections;

namespace Deveel.Data {
	public sealed class DataTableConstraintDef {
		/// <summary>
		/// The name of the constraint.
		/// </summary>
		private string name;

		/// <summary>
		/// The type of this constraint.
		/// </summary>
		private ConstraintType type;

		/// <summary>
		/// In case of a <see cref="ConstraintType.Check"/> constraint,
		/// this is the expression to check.
		/// </summary>
		private Expression check_expression;
		
		/// <summary>
		/// The serializable plain check expression as originally parsed
		/// </summary>
		internal Expression original_check_expression;

		// The first column list
		private ArrayList columns;

		// The second column list
		private ArrayList refColumns = new ArrayList();

		// The name of the table if referenced.
		private string refTableName;

		// The foreign key update rule
		private ConstraintAction updateRule;

		// The foreign key delete rule
		private ConstraintAction deleteRule;

		// Whether this constraint is deferred to when the transaction commits.
		// ( By default we are 'initially immediate deferrable' )
		private ConstraintDeferrability deferred = ConstraintDeferrability.INITIALLY_IMMEDIATE;

		private DataTableConstraintDef(ConstraintType type) {
			this.type = type;
		}

		public ConstraintAction DeleteRule {
			get { return deleteRule; }
		}

		public ConstraintAction UpdateRule {
			get { return updateRule; }
		}

		public string ReferencedTableName {
			get { return refTableName; }
		}

		/// <summary>
		/// Gets or sets the name of the constraint.
		/// </summary>
		public string Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Gets the type of constraint.
		/// </summary>
		/// <seealso cref="ConstraintType"/>
		public ConstraintType Type {
			get { return type; }
		}

		public string [] Columns {
			get { return (string[]) columns.ToArray(typeof(string)); }
		}

		public string [] ReferencedColumns {
			get { return (string[]) refColumns.ToArray(typeof(string)); }
		}

		/// <summary>
		/// If this constraint is a <see cref="ConstraintType.Check"/>, this property
		/// gets or sets the <see cref="Expression"/> that is checked.
		/// </summary>
		/// <exception cref="ArgumentException">
		/// If this constraint is not of type <see cref="ConstraintType.Check"/> and
		/// the user tries to set this property.
		/// </exception>
		public Expression CheckExpression {
			get { return check_expression; }
			set {
				if (type != ConstraintType.Check)
					throw new ArgumentException("Cannot set the value of this constraint.");

				check_expression = value;
				try {
					original_check_expression = (Expression)value.Clone();
				} catch (Exception e) {
					throw new ApplicationException(e.Message);
				}
			}
		}

		public ConstraintDeferrability Deferred {
			get { return deferred; }
			set { deferred = value; }
		}

		public static DataTableConstraintDef PrimaryKey(string name, string[] columnNames) {
			DataTableConstraintDef constraint = new DataTableConstraintDef(ConstraintType.PrimaryKey);
			constraint.name = name;
			constraint.columns = new ArrayList(columnNames);
			return constraint;
		}

		public static DataTableConstraintDef Unique(string name, string[] columnNames) {
			DataTableConstraintDef constraint = new DataTableConstraintDef(ConstraintType.Unique);
			constraint.name = name;
			constraint.columns = new ArrayList(columnNames);
			return constraint;
		}

		public static DataTableConstraintDef ForeignKey(string name, string[] columns, string refTableName, string[] refColumns, 
			ConstraintAction onDelete, ConstraintAction onUpdate) {
			DataTableConstraintDef constraint = new DataTableConstraintDef(ConstraintType.ForeignKey);
			constraint.name = name;
			constraint.columns = new ArrayList(columns);
			constraint.refTableName = refTableName;
			constraint.refColumns = new ArrayList(refColumns);
			constraint.deleteRule = onDelete;
			constraint.updateRule = onUpdate;
			return constraint;
		}

		public static DataTableConstraintDef Check(string name, Expression expression) {
			DataTableConstraintDef constraint = new DataTableConstraintDef(ConstraintType.Check);
			constraint.name = name;
			constraint.check_expression = expression;
			constraint.original_check_expression = expression;
			return constraint;
		}
	}
}