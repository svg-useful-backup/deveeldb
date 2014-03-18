﻿using System;
using System.Data;

using NUnit.Framework;

namespace Deveel.Data.Client {
	[TestFixture]
	public class CommandTest {
		[Test]
		public void CreateCommandWithMarkerParameters() {
			const string connString = "Host=Heap;UserID=SA;Password=123456;Database=testdb;Parameter Style=Marker";
			var connection = new DeveelDbConnection(connString);
			var command = connection.CreateCommand();
			command.CommandText = "SELECT * FROM Person WHERE Name = ?";
			Assert.DoesNotThrow(() => command.Parameters.Add("antonelllo"));
			Assert.DoesNotThrow(connection.Open);
			Assert.Throws<InvalidOperationException>(() => command.ExecuteScalar());
		}

		[TestCase("@Name", "antonello")]
		[TestCase("Name", "antonello")]
		public void CreateCommandWithNamedParameters(string paramName, string paramValue) {
			const string connString = "Host=Heap;UserID=SA;Password=123456;Database=testdb;Parameter Style=Named";
			var connection = new DeveelDbConnection(connString);
			var command = connection.CreateCommand();
			command.CommandText = String.Format("SELECT * FROM Person WHERE Name = {0}", paramName);
			Assert.DoesNotThrow(() => command.Parameters.Add(paramName, paramValue));
			Assert.DoesNotThrow(connection.Open);
			Assert.DoesNotThrow(() => command.ExecuteReader());
		}

		[Test]
		public void CreateCommandWithMixedParameters() {
			// by default "Parameter Style" configuration is set to "Marker" in connection strings
			const string connString = "Host=Heap;UserID=SA;Password=123456;Database=testdb";
			var connection = new DeveelDbConnection(connString);
			var command = connection.CreateCommand();
			command.CommandText = "SELECT * FROM Person WHERE Name = ?";
			Assert.DoesNotThrow(() => command.Parameters.Add("antonelllo"));
			Assert.DoesNotThrow(() => command.Parameters.Add("Name", "antonello"));
			Assert.DoesNotThrow(connection.Open);
			Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
		}

		[Test]
		public void CreateCommandOnClosedConnection() {
			const string connString = "Host=Heap;UserID=SA;Password=123456;Database=testdb";
			var connection = new DeveelDbConnection(connString);
			DeveelDbCommand command = null;
			Assert.DoesNotThrow(() => command = connection.CreateCommand());
			Assert.IsNotNull(command);
			command.CommandText = "SELECT * FROM Person WHERE Name = 'antonello'";
		}

		[Test]
		public void ExecuteCommandOnClosedConnection() {
			const string connString = "Host=Heap;UserID=SA;Password=123456;Database=testdb";
			var connection = new DeveelDbConnection(connString);
			DeveelDbCommand command = null;
			Assert.DoesNotThrow(() => command = connection.CreateCommand());
			Assert.IsNotNull(command);
			command.CommandText = "SELECT * FROM Person WHERE Name = 'antonello'";
			Assert.IsTrue(connection.State == ConnectionState.Closed);
			Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
		}

		[Test]
		public void ExecuteScalarOnsingleColumn() {
			const string connString = "Host=Heap;UserID=SA;Password=123456;Database=testdb";
			var connection = new DeveelDbConnection(connString);

			// TODO: Open the connection, create a transaction, declare some variables

			DeveelDbCommand command = null;
			Assert.DoesNotThrow(() => command = connection.CreateCommand());
			Assert.IsNotNull(command);
			command.CommandText = "SELECT Age FROM Person WHERE Name = 'antonello'";
		}

		[Test]
		public void ExecuteScalarOnMultipleColumns() {
			
		}
	}
}