﻿using System;
using System.Collections.Generic;

using Deveel.Data;
using Deveel.Data.Diagnostics;
using Deveel.Data.Services;
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

		IBlockContext IRequest.CreateBlockContext() {
			return Context.CreateBlockContext();
		}

		public IBlock CreateBlock() {
			return new Block(this);
		}

		IQuery IRequest.Query {
			get { return this; }
		}

		public IQueryContext Context { get; private set; }

		public ISession Session { get; private set; }

		public DateTimeOffset StartedOn { get; private set; }

		public SqlQuery SourceQuery { get; private set; }

		public bool HasSourceQuery {
			get { return SourceQuery != null; }
		}

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