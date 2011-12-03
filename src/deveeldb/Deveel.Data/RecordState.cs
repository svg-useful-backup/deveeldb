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

namespace Deveel.Data {
	/// <summary>
	/// An enumeration that represents the various states of a record.
	/// </summary>
	public enum RecordState {
		///<summary>
		///</summary>
		Uncommitted = 1,
		///<summary>
		///</summary>
		CommittedAdded = 2,
		///<summary>
		///</summary>
		CommittedRemoved = 3,
		///<summary>
		///</summary>
		Deleted = 4,     // ie. available for reclaimation.

		///<summary>
		/// Denotes an erroneous record state.
		///</summary>
		Error = 0
	}
}