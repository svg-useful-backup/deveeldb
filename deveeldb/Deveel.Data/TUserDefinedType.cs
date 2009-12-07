﻿//  
//  TUserDefinedType.cs
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

namespace Deveel.Data {
	/// <exclude/>
	public sealed class TUserDefinedType : TType {
		public TUserDefinedType(UserType userType) 
			: base(SqlType.Object) {
			this.userType = userType;
		}

		private readonly UserType userType;

		/// <summary>
		/// Gets the definition of the type.
		/// </summary>
		public UserType UserType {
			get { return userType; }
		}

		#region Overrides of TType

		public override int Compare(object x, object y) {
			throw new InvalidOperationException("Cannot compare two user-defined types.");
		}

		public override bool IsComparableType(TType type) {
			// it is not possible (yet) to compare UDTs
			return false;
		}

		public override int CalculateApproximateMemoryUse(object ob) {
			return 1000;
		}

		public override Type GetObjectType() {
			return typeof(UserObject);
		}

		#endregion
	}
}