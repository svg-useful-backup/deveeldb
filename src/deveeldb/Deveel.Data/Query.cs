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
using System.Collections.Generic;

using Deveel.Data.Diagnostics;
using Deveel.Data.Sql;

namespace Deveel.Data {
	public sealed class Query : IQuery {
		private Dictionary<string, object> metadata;

		internal Query(ISession session) 
			: this(session, null) {
		}

		internal Query(ISession session, SqlQuery sourceQuery) {
			Session = session;
			SourceQuery = sourceQuery;

			Context = session.Context.CreateQueryContext();
			Context.RegisterInstance<IQuery>(this);

			StartedOn = DateTimeOffset.UtcNow;

			Access = new RequestAccess(this);

			metadata = GetMetadata();
		}

		private Dictionary<string, object> GetMetadata() {
			return new Dictionary<string, object> {
				{ "query.startTime", StartedOn },
				{ "query.source", SourceQuery }
			};
		}

		~Query() {
			Dispose(false);
		}

		public IBlock CreateBlock() {
			return new Block(this);
		}

		IQuery IRequest.Query {
			get { return this; }
		}

		public IQueryContext Context { get; private set; }

		IBlockContext IRequest.Context {
			get { return Context; }
		}

		public ISession Session { get; private set; }

		public DateTimeOffset StartedOn { get; private set; }

		public SqlQuery SourceQuery { get; private set; }
		
		public RequestAccess Access { get; private set; }

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				if (Context != null)
					Context.Dispose();
			}

			Context = null;
			Session = null;
		}

		IContext IEventSource.Context {
			get { return Context; }
		}

		IEventSource IEventSource.ParentSource {
			get { return Session; }
		}

		IEnumerable<KeyValuePair<string, object>> IEventSource.Metadata {
			get { return metadata; }
		}
	}
}
