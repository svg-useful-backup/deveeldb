﻿using System;
using System.Collections;
using System.Data;
using System.Data.Common;

namespace Deveel.Data.Client {
	public sealed class DeveelDbDataReader : DbDataReader {
		public override void Close() {
			throw new NotImplementedException();
		}

		public override DataTable GetSchemaTable() {
			throw new NotImplementedException();
		}

		public override bool NextResult() {
			throw new NotImplementedException();
		}

		public override bool Read() {
			throw new NotImplementedException();
		}

		public override int Depth { get; }
		public override bool IsClosed { get; }
		public override int RecordsAffected { get; }

		public override bool GetBoolean(int ordinal) {
			throw new NotImplementedException();
		}

		public override byte GetByte(int ordinal) {
			throw new NotImplementedException();
		}

		public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) {
			throw new NotImplementedException();
		}

		public override char GetChar(int ordinal) {
			throw new NotImplementedException();
		}

		public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) {
			throw new NotImplementedException();
		}

		public override Guid GetGuid(int ordinal) {
			throw new NotImplementedException();
		}

		public override short GetInt16(int ordinal) {
			throw new NotImplementedException();
		}

		public override int GetInt32(int ordinal) {
			throw new NotImplementedException();
		}

		public override long GetInt64(int ordinal) {
			throw new NotImplementedException();
		}

		public override DateTime GetDateTime(int ordinal) {
			throw new NotImplementedException();
		}

		public override string GetString(int ordinal) {
			throw new NotImplementedException();
		}

		public override object GetValue(int ordinal) {
			throw new NotImplementedException();
		}

		public override int GetValues(object[] values) {
			throw new NotImplementedException();
		}

		public override bool IsDBNull(int ordinal) {
			throw new NotImplementedException();
		}

		public override int FieldCount { get; }

		public override object this[int ordinal] {
			get { throw new NotImplementedException(); }
		}

		public override object this[string name] {
			get { throw new NotImplementedException(); }
		}

		public override bool HasRows { get; }

		public override decimal GetDecimal(int ordinal) {
			throw new NotImplementedException();
		}

		public override double GetDouble(int ordinal) {
			throw new NotImplementedException();
		}

		public override float GetFloat(int ordinal) {
			throw new NotImplementedException();
		}

		public override string GetName(int ordinal) {
			throw new NotImplementedException();
		}

		public override int GetOrdinal(string name) {
			throw new NotImplementedException();
		}

		public override string GetDataTypeName(int ordinal) {
			throw new NotImplementedException();
		}

		public override Type GetFieldType(int ordinal) {
			throw new NotImplementedException();
		}

		public override IEnumerator GetEnumerator() {
			throw new NotImplementedException();
		}
	}
}
