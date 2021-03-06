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

namespace Deveel.Data.Store {
	/// <summary>
	/// An object that creates and manages the <see cref="IStore"/> objects that 
	/// the database engine uses to represent itself on an external medium such 
	/// as a disk, and that constitute the low level persistent data format.
	/// </summary>
	/// <remarks>
	/// This interface is an abstraction of the database persistence layer.  For
	/// example, an implementation could represent itself as 1 file per store on a
	/// disk, or as a number of stores in a single file, or as an entirely in-memory
	/// database.
	/// </remarks>
	public interface IStoreSystem : IDisposable {
		/// <summary>
		/// Returns true if the store with the given name exists within the 
		/// system, or false otherwise.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		bool StoreExists(String name);

		/// <summary>
		/// Creates and returns a new persistent Store object given the unique
		/// name of the store.
		/// </summary>
		/// <param name="name">A unique identifier string representing the name 
		/// of the store.</param>
		/// <remarks>
		/// If the system is read-only or the table otherwise can not be created 
		/// then an exception is thrown.
		/// </remarks>
		/// <returns></returns>
		IStore CreateStore(String name);

		/// <summary>
		/// Opens an existing persistent Store object in the system and returns 
		/// the <see cref="IStore"/> object that contains its data.
		/// </summary>
		/// <param name="name">a unique identifier string representing the name 
		/// of the store.</param>
		/// <remarks>
		/// An exception is thrown if the store can not be opened.
		/// </remarks>
		/// <returns></returns>
		IStore OpenStore(String name);

		/// <summary>
		/// Closes a store that has been either created or opened with the
		/// <see cref="CreateStore"/> or <see cref="OpenStore"/> methods.
		/// </summary>
		/// <param name="store"></param>
		/// <returns>
		/// Returns true if the store was successfully closed.
		/// </returns>
		bool CloseStore(IStore store);

		/// <summary>
		/// Permanently deletes a store from the system - use with care!
		/// </summary>
		/// <param name="store"></param>
		/// <remarks>
		/// Note that it is quite likely that a store may fail to delete in 
		/// which case the delete operation should be re-tried after a short 
		/// timeout.
		/// </remarks>
		/// <returns>
		/// Returns true if the store was successfully deleted and the resources 
		/// associated with it were freed. Returns false if the store could not 
		/// be deleted.
		/// </returns>
		bool DeleteStore(IStore store);

		/// <summary>
		/// Sets a new check point at the current state of this store system.
		/// </summary>
		/// <remarks>
		/// This is intended to help journalling check point and recovery systems.
		/// A check point is set whenever data is committed to the database. 
		/// Some systems can be designed to be able to roll forward or backward 
		/// to different check points. Each check point represents a stable state 
		/// in the database life cycle.
		/// <para>
		/// A checkpoint based system greatly improves stability because if a 
		/// crash occurs in an intermediate state the changes can simply be 
		/// rolled back to the last stable state.
		/// </para>
		/// <para>
		/// An implementation may choose not to implement check points in which 
		/// case this would be a no-op.
		/// </para>
		/// </remarks>
		void SetCheckPoint();

		/// <summary>
		/// Attempts to lock this store system exclusively so that no other 
		/// process may access or change the persistent data in the store.
		/// </summary>
		/// <param name="lockName"></param>
		/// <remarks>
		/// If this fails to lock, an IOException is generated, otherwise the 
		/// lock is obtained and the method returns.
		/// </remarks>
		void Lock(String lockName);

		/// <summary>
		/// Unlocks the exclusive access to the persistent store objects.
		/// </summary>
		/// <param name="lockName"></param>
		/// <remarks>
		/// After this method completes, access to the store system by other 
		/// processes is allowed.
		/// </remarks>
		void Unlock(String lockName);
	}
}