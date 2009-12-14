﻿// 
//  Driver.cs
//  
//  Author:
//       Antonello Provenzano <antonello@deveel.com>
//  
//  Copyright (c) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

using Deveel.Math;

namespace Deveel.Data.Client {
	internal class Driver {
		private Driver(Stream stream, int queryTimeout) {
			this.stream = stream;
			input = new BinaryReader(new BufferedStream(stream, 32768));
			output = new BinaryWriter(new BufferedStream(stream, 32768));
			this.queryTimeout = queryTimeout;
		}

		private readonly BinaryReader input;
		private readonly BinaryWriter output;
		private Stream stream;
		private bool closed;
		private Version serverVersion;
		private readonly int queryTimeout;

		private bool listenThreadClosed;
		private MemoryStream commandSream;
		private BinaryWriter commandOutput;
		private ArrayList serverResponses;
		private int currentDispatchId = 1;

		public Version ServerVersion {
			get { return serverVersion; }
		}

		public Version ClientVersion {
			get { return Assembly.GetCallingAssembly().GetName().Version; }
		}

		#region Commands

		private void WriteCommand(byte[] command, int offset, int size) {
			if (closed)
				throw new InvalidOperationException();

			output.Write(size);
			output.Write(command, offset, size);
			output.Flush();
		}

		private byte[] ReadNextCommand(int timeout) {
			if (closed)
				throw new InvalidOperationException();

			int size = input.ReadInt32();
			byte[] buffer = new byte[size];
			input.Read(buffer, 0, size);
			return buffer;
		}

		private int CloseCommand() {
			lock (this) {
				int dispatchId = NextDispatchId();
				commandOutput.Write(Commands.Close);
				commandOutput.Write(dispatchId);
				FlushCommand();
				return dispatchId;
			}
		}

		private void FlushCommand() {
			lock (this) {
				WriteCommand(commandSream.GetBuffer(), 0, (int)commandSream.Length);
				commandSream = new MemoryStream();
				commandOutput = new BinaryWriter(commandSream, Encoding.Unicode);
			}
		}

		private int QueryCommand(DeveelDbCommand command) {
			lock (this) {
				int dispatchId = NextDispatchId();
				commandOutput.Write(Commands.Query);
				commandOutput.Write(dispatchId);
				WriteQuery(command);
				FlushCommand();

				return dispatchId;
			}
		}

		private int DisposeResultCommand(int resultId) {
			lock (this) {
				int dispatchId = NextDispatchId();
				commandOutput.Write(Commands.DisposeResult);
				commandOutput.Write(dispatchId);
				commandOutput.Write(resultId);
				FlushCommand();

				return dispatchId;
			}
		}

		private int ResultSectionCommand(int resultId, int rowOffset, int rowCount) {
			lock (this) {
				int dispatch_id = NextDispatchId();
				commandOutput.Write(Commands.ResultSection);
				commandOutput.Write(dispatch_id);
				commandOutput.Write(resultId);
				commandOutput.Write(rowOffset);
				commandOutput.Write(rowCount);
				FlushCommand();

				return dispatch_id;
			}
		}

		private int ChangeDatabaseCommand(string database) {
			lock (this) {
				int dispatchId = NextDispatchId();
				commandOutput.Write(Commands.ChangeDatabase);
				commandOutput.Write(dispatchId);
				commandOutput.Write(database);
				FlushCommand();
				return dispatchId;
			}
		}

		private int StreamableObjectSectionCommand(int resultId, long streamableObjectId, long offset, int length) {
			lock (this) {
				int dispatch_id = NextDispatchId();
				commandOutput.Write(Commands.StreamableObjectSection);
				commandOutput.Write(dispatch_id);
				commandOutput.Write(resultId);
				commandOutput.Write(streamableObjectId);
				commandOutput.Write(offset);
				commandOutput.Write(length);
				FlushCommand();

				return dispatch_id;
			}
		}

		private int PushStreamableObjectPartCommand(ReferenceType type, long objectId, long objectLength,
													byte[] buf, long offset, int length) {
			lock(this) {
				int dispatchId = NextDispatchId();
				commandOutput.Write(Commands.PushStreamableObjectPart);
				commandOutput.Write(dispatchId);
				commandOutput.Write((byte) type);
				commandOutput.Write(objectId);
				commandOutput.Write(objectLength);
				commandOutput.Write(length);
				commandOutput.Write(buf, 0, length);
				commandOutput.Write(offset);
				FlushCommand();

				return dispatchId;
			}
		}

		private int DisposeStreamableObjectCommand(int resultId, long streamableObjectId) {
			lock (this) {
				int dispatch_id = NextDispatchId();
				commandOutput.Write(Commands.DisposeStreamableObject);
				commandOutput.Write(dispatch_id);
				commandOutput.Write(resultId);
				commandOutput.Write(streamableObjectId);
				FlushCommand();

				return dispatch_id;
			}
		}

		private int PingCommand() {
			int dispatchId = NextDispatchId();
			commandOutput.Write(Commands.Ping);
			commandOutput.Write(dispatchId);
			FlushCommand();

			return dispatchId;
		}

		#endregion

		private HandShakeResponse Handshake(string database) {
			MemoryStream tempStream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(tempStream, Encoding.ASCII);

			// Write output the magic number
			writer.Write(0x0ced007);
			// Write output the driver version
			Version clientVersion = ClientVersion;
			writer.Write(clientVersion.Major);
			writer.Write(clientVersion.Minor);
			writer.Write(database);
			byte[] arr = tempStream.ToArray();
			WriteCommand(arr, 0, arr.Length);

			byte[] response = ReadNextCommand(0);
			return new HandShakeResponse(response);
		}

		private ServerStatus SendCredentials(string schema, string user, string pass) {
			MemoryStream tempStream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(tempStream);
			writer.Write(schema);
			writer.Write(user);
			writer.Write(pass);
			byte[] arr = tempStream.ToArray();
			WriteCommand(arr, 0, arr.Length);

			byte[] response = ReadNextCommand(0);
			return (ServerStatus) BitConverter.ToInt32(response, 0);
		}

		public bool Ping() {
			try {
				int dispatchId = PingCommand();
				ServerResponse response = GetResponse(dispatchId);
				if (response == null)
					return false;

				return response.Status == ServerStatus.Success;
			} catch(IOException e) {
				//TODO: log error...
				throw new DeveelDbException("IO Error: " + e.Message);
			}
		}

		public void ChangeDatabase(string database) {
			try {
				int dispatch_id = ChangeDatabaseCommand(database);
				// get the response
				ServerResponse response = GetResponse(dispatch_id);
				if (response == null)
					throw new DeveelDbException("Query timed output after " + queryTimeout + " seconds.");

				ServerStatus status = response.Status;
				if (status == ServerStatus.Failed)
					throw new DeveelDbException("Change database failed: " + response.ReadString());
				if (status == ServerStatus.DatabaseNotFound)
					throw new DeveelDbException("The database '" + database + "' was not found on the server.");
			} catch (IOException e) {
				//TODO: log exception...
				throw new DeveelDbException("IO Error: " + e.Message);
			}
		}

		public QueryResponse ExecuteQuery(DeveelDbCommand command) {
			try {
				int dispatchId = QueryCommand(command);
				ServerResponse serverResponse = GetResponse(command.CommandTimeout, dispatchId);
				if (serverResponse == null)
					throw new DeveelDbException("Query timed output after " + command.CommandTimeout + " seconds.");

				BinaryReader reader = new BinaryReader(serverResponse.GetStream());

				ServerStatus status = serverResponse.Status;
				if (status == ServerStatus.Success)
					return new QueryResponse(reader);

				if (status == ServerStatus.Exception)
					throw serverResponse.GetServerException();

				if (status == ServerStatus.AuthenticationError) {
					string access_type = reader.ReadString();
					string table_name = reader.ReadString();
					throw new DeveelDbAuthenticationException(access_type, table_name);
				}
				
				throw new DeveelDbException("Illegal response code from server.");
			} catch (IOException e) {
				//TODO: log the error...
				throw new DeveelDbException("IO Error: " + e.Message);
			}
		}

		public IList GetResultPart(int resultId, int startRow, int countRows) {
			try {
				int dispatchId = ResultSectionCommand(resultId, startRow, countRows);

				ServerResponse response = GetResponse(dispatchId);
				if (response == null)
					throw new DeveelDbException("Downloading result part timed output after " + queryTimeout + " seconds.");

				ServerStatus status = response.Status;

				if (status == ServerStatus.Success) {
					BinaryReader din = new BinaryReader(response.GetStream());
					int col_count = din.ReadInt32();
					int size = countRows * col_count;
					ArrayList list = new ArrayList(size);
					for (int i = 0; i < size; ++i) {
						list.Add(ReadObject(din));
					}
					return list;
				} 

				if (status == ServerStatus.Exception)
					throw response.GetServerException();
				
				throw new DeveelDbException("Illegal response code from server.");
			} catch (IOException e) {
				//TODO: log exception...
				throw new DeveelDbException("IO Error: " + e.Message);
			}
		}


		public void DisposeResult(int resultId) {
			try {
				int dispatchId = DisposeResultCommand(resultId);
				ServerResponse response = GetResponse(dispatchId);
				if (response == null)
					throw new DeveelDbException("Dispose result timed output after " + queryTimeout + " seconds.");

				ServerStatus status = response.Status;

				if (status == ServerStatus.Failed)
					throw new DeveelDbException("Dispose failed: " + response.ReadString());
			} catch (IOException e) {
				//TODO: log exception...
				throw new DeveelDbException("IO Error: " + e.Message);
			}
		}

		public byte[] GetStreamableObjectPart(int result_id, long streamable_object_id, long offset, int len) {
			try {
				int dispatch_id = StreamableObjectSectionCommand(result_id, streamable_object_id, offset, len);
				ServerResponse response = GetResponse(dispatch_id);
				// If command == null then we timed output
				if (response == null) {
					throw new DeveelDbException("GetStreamableObjectPart timed output after " + queryTimeout + " seconds.");
				}

				ServerStatus status = response.Status;

				if (status == ServerStatus.Exception)
					throw response.GetServerException();

				if (status == ServerStatus.Success) {
					BinaryReader din = new BinaryReader(response.GetStream());
					int contents_size = din.ReadInt32();
					byte[] buf = new byte[contents_size];
					din.Read(buf, 0, contents_size);
					return buf;
				} 
				
				throw new DeveelDbException("Illegal response code from server.");
			} catch (IOException e) {
				//TODO: log exception...
				throw new DeveelDbException("IO Error: " + e.Message);
			}
		}

		public void PushStreamableObjectPart(ReferenceType type, long object_id, long object_length, byte[] buf, long offset, int length) {
			try {
				int dispatch_id = PushStreamableObjectPartCommand(type, object_id, object_length, buf, offset, length);
				ServerResponse response = GetResponse(dispatch_id);
				if (response == null)
					throw new DeveelDbException("Query timed output after " + queryTimeout + " seconds.");

				ServerStatus status = response.Status;
				if (status == ServerStatus.Failed) {
					throw new DeveelDbException("Push object failed: " + response.ReadString());
				}
			} catch (IOException e) {
				//TODO: log the error...
				throw new DeveelDbException("IO Error: " + e.Message);
			}
		}

		public void DisposeStreamableObject(int resultId, long streamableObjectId) {
			try {
				int dispatchId = DisposeStreamableObjectCommand(resultId, streamableObjectId);
				ServerResponse response = GetResponse(dispatchId);
				if (response == null) {
					throw new DeveelDbException("DisposeStreamableObject timed output after " + queryTimeout + " seconds.");
				}

				ServerStatus status = response.Status;

				if (status == ServerStatus.Failed)
					throw new DeveelDbException("Dispose failed: " + response.ReadString());
			} catch (IOException e) {
				//TODO: log the error...
				throw new DeveelDbException("IO Error: " + e.Message);
			}
		}

		public void Close() {
			int dispatchId = CloseCommand();
			ServerResponse response = GetResponse(dispatchId);
			if (response == null) {
				//TODO: log the thing...
			}

			try {
				output.Flush();
			} finally {
				stream.Close();
				stream = null;
				closed = true;
			}
		}

		private static object ReadObject(BinaryReader reader) {
			DeveelDbType type = (DeveelDbType)reader.ReadByte();

			switch (type) {
				case DeveelDbType.Null:
					return DBNull.Value;
				case DeveelDbType.Boolean:
					return new DeveelDbBoolean(reader.ReadBoolean());
				case DeveelDbType.Int4:
					return new DeveelDbNumber(reader.ReadInt32());
				case DeveelDbType.Int8:
					return new DeveelDbNumber(reader.ReadInt64());
				case DeveelDbType.Number: {
						NumberState state = (NumberState)reader.ReadByte();
						int scale = reader.ReadInt32();
						int bitLength = reader.ReadInt32();
						byte[] buffer = new byte[bitLength];
						reader.Read(buffer, 0, bitLength);

						if (state == NumberState.NegativeInfinity)
							return DeveelDbNumber.NegativeInfinity;
						if (state == NumberState.PositiveInfinity)
							return DeveelDbNumber.PositiveInfinity;
						if (state == NumberState.NotANumber)
							return DeveelDbNumber.NaN;

						return new DeveelDbNumber(new BigDecimal(new BigInteger(buffer), scale), state);
					}
				case DeveelDbType.String: {
						int length = reader.ReadInt32();
						byte[] buffer = new byte[length];
						reader.Read(buffer, 0, length);
						return new DeveelDbString(Encoding.Unicode.GetString(buffer));
					}
				case DeveelDbType.Time:
					return new DeveelDbDateTime(reader.ReadInt64());
				case DeveelDbType.Interval:
					return new DeveelDbTimeSpan(reader.ReadInt64());
				case DeveelDbType.Binary: {
						const int BufferSize = 512;
						long length = reader.ReadInt64();

						byte[] buffer = new byte[length];

						int pos = 0;
						while (pos <= length) {
							int read = reader.Read(buffer, pos, BufferSize);
							pos += read;
						}

						return new DeveelDbBinary(buffer, 0, (int)length);
					}
				case DeveelDbType.UDT:
					throw new NotSupportedException();
				default:
					throw new NotSupportedException();
			}
		}

		private static void WriteObject(BinaryWriter writer, object obj) {
			if (obj == null || (obj is INullable && ((INullable)obj).IsNull)) {
				writer.Write((byte)DeveelDbType.Null);
			} else if (obj is DeveelDbBoolean) {
				DeveelDbBoolean b = (DeveelDbBoolean)obj;
				writer.Write((byte)DeveelDbType.Boolean);
				writer.Write(b.Value);
			} else if (obj is DeveelDbNumber) {
				DeveelDbNumber n = (DeveelDbNumber)obj;
				if (n.IsFromInt32) {
					writer.Write((byte)DeveelDbType.Int4);
					writer.Write(n.ToInt32());
				} else if (n.IsFromInt64) {
					writer.Write((byte)DeveelDbType.Int8);
					writer.Write(n.ToInt64());
				} else {
					writer.Write((byte)DeveelDbType.Number);
					writer.Write((byte)n.State);
					writer.Write(n.Scale);

					byte[] buffer = n.ToByteArray();
					writer.Write(buffer.Length);
					writer.Write(buffer);
				}
			} else if (obj is DeveelDbString) {
				DeveelDbString s = (DeveelDbString)obj;
				writer.Write((byte)DeveelDbType.String);
				byte[] buffer = Encoding.Unicode.GetBytes(s.Value);
				writer.Write(buffer.Length);
				writer.Write(buffer);
			} else if (obj is DeveelDbDateTime) {
				DeveelDbDateTime d = (DeveelDbDateTime)obj;
				writer.Write((byte)DeveelDbType.Time);
				writer.Write(d.Value.Ticks);
			} else if (obj is DeveelDbTimeSpan) {
				DeveelDbTimeSpan t = (DeveelDbTimeSpan)obj;
				writer.Write((byte)DeveelDbType.Interval);
				writer.Write(t.Value.Ticks);
			} else if (obj is DeveelDbBinary) {
				DeveelDbBinary b = (DeveelDbBinary)obj;
				writer.Write((byte)DeveelDbType.Binary);
				byte[] buffer = b.Value;
				writer.Write(buffer.LongLength);
				writer.Write(buffer);
			} else {
				throw new NotSupportedException();
			}
		}

		internal static int SizeOf(object ob) {
			if (ob == null)
				return 9;
			if (ob is DeveelDbString)
				return (ob.ToString().Length * 2) + 9;
			if (ob is DeveelDbNumber)
				return 15 + 9;
			if (ob is DeveelDbDateTime)
				return 8 + 9;
			if (ob is DeveelDbTimeSpan)
				return 8 + 9;
			if (ob is bool)
				return 2 + 9;
			if (ob is DeveelDbBinary)
				return ((DeveelDbBinary)ob).Length + 9;

			//TODO: long objects and UDTs

			throw new IOException("Unrecognised type: " + ob.GetType());

		}

		private void WriteQuery(DeveelDbCommand command) {
			ParameterStyle paramStyle = command.Connection.Settings.ParameterStyle;
			commandOutput.Write(command.CommandText);
			commandOutput.Write((byte)paramStyle);

			int paramCount = command.Parameters.Count;
			commandOutput.Write(paramCount);
			for (int i = 0; i < paramCount; i++) {
				DbParameter parameter = command.Parameters[i];
				if (paramStyle == ParameterStyle.Named)
					commandOutput.Write(parameter.ParameterName);
				WriteObject(commandOutput, parameter.Value);
			}
		}

		private void ListenToEvents() {
			commandSream = new MemoryStream();
			commandOutput = new BinaryWriter(commandSream, Encoding.Unicode);

			serverResponses = new ArrayList();
			listenThreadClosed = false;

			try {
				while (!listenThreadClosed) {
					byte[] buf = ReadNextCommand(0);
					int dispatchId = BitConverter.ToInt32(buf, 0);

					if (dispatchId == -1)
						ProcessEvent(buf);

					lock (serverResponses) {
						serverResponses.Add(new ServerResponse(dispatchId, buf));
						Monitor.PulseAll(serverResponses);
					}

				}
			} catch (IOException e) {
				//TODO: log the error...
			} finally {
				object oldCommandsList = serverResponses;
				lock (oldCommandsList) {
					serverResponses = null;
					Monitor.PulseAll(oldCommandsList);
				}
			}
		}

		private void ProcessEvent(byte[] buf) {
			int ev = BitConverter.ToInt32(buf, 4);
			if (ev == Commands.Ping) {
				// is the client alive?
			} else if (ev == Commands.DatabaseEvent) {
				// A database event that is passed to the IDatabaseCallBack...
				MemoryStream bin = new MemoryStream(buf, 8, buf.Length - 8);
				BinaryReader din = new BinaryReader(bin);

				int event_type = din.ReadInt32();
				String event_msg = din.ReadString();
				OnDatabaseEvent(event_type, event_msg);
			} else {
				//TODO: log this...
			}
		}

		private Thread triggerThread;
		private readonly ArrayList triggerMessagesQueue = new ArrayList();
		private readonly Hashtable triggerListeners = new Hashtable();

		private void OnDatabaseEvent(int type, string message) {
			if (type == 99) {
				if (triggerThread == null) {
					triggerThread = new Thread(DispatchTriggers);
					triggerThread.Name = "DeveelDB::DispatchTriggers";
					triggerThread.IsBackground = true;
					triggerThread.Start();
				}
				DispatchTrigger(message);
			} else {
				throw new DeveelDbException("Unrecognised database event: " + type);
			}
		}

		private void DispatchTrigger(string event_message) {
			lock (triggerMessagesQueue) {
				triggerMessagesQueue.Add(event_message);
				Monitor.PulseAll(triggerMessagesQueue);
			}
		}

		private void DispatchTriggers() {
			while (true) {
				try {
					String message;
					lock(triggerMessagesQueue) {
						while (triggerMessagesQueue.Count == 0) {
							try {
								Monitor.Wait(triggerMessagesQueue);
							} catch(ThreadInterruptedException) {
								/* ignore */
							}
						}
						message = (String) triggerMessagesQueue[0];
						triggerMessagesQueue.RemoveAt(0);
					}

					// The format of a trigger message is:
					// "[trigger_name] [trigger_source] [trigger_fire_count]"

					string[] tok = message.Split(' ');
					TriggerEventType event_type = (TriggerEventType) Convert.ToInt32(tok[0]);
					String trigger_name = tok[1];
					String trigger_source = tok[2];
					int trigger_fire_count = Convert.ToInt32(tok[3]);

					EventHandler listener = (EventHandler) triggerListeners[trigger_name];
					if (listener != null)
						listener(this, new TriggerEventArgs(trigger_source, trigger_name, event_type, trigger_fire_count));

				} catch(Exception t) {
					Console.Error.WriteLine(t.Message);
					Console.Error.WriteLine(t.StackTrace);
				}
			}
		}

		public void AddTriggerListener(string triggerName, EventHandler listener) {			
			EventHandler oldListener = triggerListeners[triggerName] as EventHandler;
			if (oldListener != null)
				listener = (EventHandler) Delegate.Combine(oldListener, listener);
			triggerListeners[triggerName] = listener;
		}

		public void RemoveTriggerListener(string triggerName, EventHandler listener) {
			if (!triggerListeners.ContainsKey(triggerName))
				return;

			EventHandler oldListener = triggerListeners[triggerName] as EventHandler;
			if (oldListener != null)
				oldListener = (EventHandler) Delegate.Remove(oldListener, listener);

			if (oldListener == null)
				triggerListeners.Remove(triggerName);
		}

		private int NextDispatchId() {
			return currentDispatchId++;
		}

		private ServerResponse GetResponse(int dispatchId) {
			return GetResponse(queryTimeout, dispatchId);
		}

		private ServerResponse GetResponse(int timeout, int dispatchId) {
			DateTime startTime = DateTime.Now;
			DateTime timeoutHigh = startTime + new TimeSpan(((long)timeout * 1000) * TimeSpan.TicksPerMillisecond);

			lock (serverResponses) {
				if (serverResponses == null)
					throw new DeveelDbException();

				while (true) {
					for (int i = 0; i < serverResponses.Count; ++i) {
						ServerResponse response = (ServerResponse)serverResponses[i];
						if (response.DispatchId == dispatchId) {
							serverResponses.RemoveAt(i);
							return response;
						}
					}

					if (timeout > 0 && DateTime.Now > timeoutHigh)
						return null;

					try {
						Monitor.Wait(serverResponses, 1000);
					} catch (ThreadInterruptedException) {
						
					}
				}
			}
		}

		public bool Authenticate(DeveelDbConnectionStringBuilder connectionString) {
			HandShakeResponse hsr = Handshake(connectionString.Database);

			if (hsr.Status == ServerStatus.DatabaseNotFound)
				throw new DeveelDbException();
			if (hsr.Status != ServerStatus.Acknowledgement)
				throw new DeveelDbException();

			serverVersion = hsr.ServerVersion;

			ServerStatus code = SendCredentials(connectionString.Schema, connectionString.UserName, connectionString.Password);

			if (code == ServerStatus.UserAuthenticationFailed)
				throw new DeveelDbException();
			if (code == ServerStatus.DatabaseNotFound)
				throw new DeveelDbException();
			if (code != ServerStatus.UserAuthenticationPassed)
				throw new DeveelDbException();

			Thread listenThread = new Thread(ListenToEvents);
			listenThread.Name = "DeveelDB::ListenToEvents";
			listenThread.IsBackground = true;
			listenThread.Start();

			return true;
		}

		public static Driver Create(string host, int port, int timeout) {
			Stream stream;

			try {
				Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.ReceiveTimeout = timeout;
				socket.Connect(host, port);
				stream = new NetworkStream(socket, FileAccess.ReadWrite);
			} catch(IOException) {
				throw new DeveelDbException();
			}

			return new Driver(stream, timeout);
		}

		#region HandShakeResponse

		private class HandShakeResponse {
			public HandShakeResponse(byte[] response) {
				this.response = response;
			}

			private readonly byte[] response;

			public ServerStatus Status {
				get { return (ServerStatus) BitConverter.ToInt32(response, 0); }
			}

			public Version ServerVersion {
				get {
					Version serverVersion = null;
					if (response.Length > 4 && response[4] == 1) {
						// Yes so Read the server version
						int serverMajorVersion = BitConverter.ToInt32(response, 5);
						int serverMinorVersion = BitConverter.ToInt32(response, 9);
						serverVersion = new Version(serverMajorVersion, serverMinorVersion);
					}
					return serverVersion;
				}
			}
		}

		#endregion

		#region serverResponse

		class ServerResponse {
			public ServerResponse(int dispatchId, byte[] buffer) {
				this.dispatchId = dispatchId;
				this.buffer = buffer;
				reader = new BinaryReader(GetStream(), Encoding.Unicode);
			}

			private readonly int dispatchId;
			private readonly byte[] buffer;
			private readonly BinaryReader reader;

			public int DispatchId {
				get { return dispatchId; }
			}

			public ServerStatus Status {
				get { return (ServerStatus) BitConverter.ToInt32(buffer, 4); }
			}

			public MemoryStream GetStream() {
				return new MemoryStream(buffer, 8, buffer.Length - 8);
			}

			public string ReadString() {
				return reader.ReadString();
			}

			public Exception GetServerException() {
				int dbCode = reader.ReadInt32();
				string message = reader.ReadString();
				string stackTrace = reader.ReadString();
				return new DeveelDbServerException(message, stackTrace, dbCode);
			}
		}

		#endregion

		#region Commands
		private class Commands {
			// ---------- Commands ----------

			public const int ChangeDatabase = 40;
			public const int Query = 50;
			public const int DisposeResult = 55;
			public const int ResultSection = 60;
			public const int StreamableObjectSection = 61;
			public const int DisposeStreamableObject = 62;
			public const int PushStreamableObjectPart = 63;
			public const int Ping = 65;
			public const int Close = 70;
			public const int DatabaseEvent = 75;
			public const int ServerRequest = 80;
		}
		#endregion

		#region ServerStatus

		private enum ServerStatus {
			Acknowledgement = 5,
			DatabaseNotFound = 7,
			UserAuthenticationPassed = 10,
			UserAuthenticationFailed = 15,
			Success = 20,
			Failed = 25,
			Exception = 30,
			AuthenticationError = 35
		}

		#endregion
	}
}