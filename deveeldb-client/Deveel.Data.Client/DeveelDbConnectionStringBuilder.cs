﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Globalization;

namespace Deveel.Data.Client {
	[DefaultProperty("DataSource")]
	public sealed class DeveelDbConnectionStringBuilder : DbConnectionStringBuilder {
		public DeveelDbConnectionStringBuilder(string connectionString) {
			InitToDefault();
			ConnectionString = connectionString;
		}

		public DeveelDbConnectionStringBuilder()
			: this(String.Empty) {
		}

		static DeveelDbConnectionStringBuilder() {
			defaults = new Dictionary<string, object>();
			defaults.Add(HostKey, DefaultHost);
			defaults.Add(PortKey, DefaultPort);
			defaults.Add(DatabaseKey, DefaultDatabase);
			defaults.Add(UserNameKey, DefaultUserName);
			defaults.Add(PasswordKey, DefaultPassword);
			defaults.Add(SchemaKey, DefaultSchema);
			defaults.Add(CreateKey, DefaultCreate);
			defaults.Add(BootOrCreateKey, DefaultBootOrCreate);
			defaults.Add(ParameterStyleKey, DefaultParameterStyle);
			defaults.Add(VerboseColumnNamesKey, DefaultVerboseColumnName);
			defaults.Add(PersistSecurityInfoKey, DefaultPersistSecurityInfo);
			defaults.Add(RowCacheSizeKey, DefaultRowCacheSize);
			defaults.Add(QueryTimeoutKey, DefaultQueryTimeout);

			keymaps = new Dictionary<string, string>();
			keymaps[HostKey.ToUpper()] = HostKey;
			keymaps["ADDRESS"] = HostKey;
			keymaps["SERVER"] = HostKey;
			keymaps[PortKey.ToUpper()] = PortKey;
			keymaps[DatabaseKey.ToUpper()] = DatabaseKey;
			keymaps["CATALOG"] = DatabaseKey;
			keymaps["INITIAL CATALOG"] = DatabaseKey;
			keymaps["DB"] = DatabaseKey;
			keymaps[SchemaKey.ToUpper()] = SchemaKey;
			keymaps["DEFAULT SCHEMA"] = SchemaKey;
			keymaps[CreateKey.ToUpper()] = CreateKey;
			keymaps[BootOrCreateKey] = BootOrCreateKey;
			keymaps["BOOT OR CREATE"] = BootOrCreateKey;
			keymaps["CREATE OR BOOT"] = BootOrCreateKey;
			keymaps["CREATEORBOOT"] = BootOrCreateKey;
			keymaps[UserNameKey.ToUpper()] = UserNameKey;
			keymaps["USER"] = UserNameKey;
			keymaps["USER NAME"] = UserNameKey;
			keymaps["USER ID"] = UserNameKey;
			keymaps["USERID"] = UserNameKey;
			keymaps["UID"] = UserNameKey;
			keymaps[PasswordKey.ToUpper()] = PasswordKey;
			keymaps["PASS"] = PasswordKey;
			keymaps["PWD"] = PasswordKey;
			keymaps["SECRET"] = PasswordKey;
			keymaps[ParameterStyleKey.ToUpper()] = ParameterStyleKey;
			keymaps["PARAMSTYLE"] = ParameterStyleKey;
			keymaps["PARAMETER STYLE"] = ParameterStyleKey;
			keymaps["USEPARMAMETER"] = ParameterStyleKey;
			keymaps["USE PARAMETER"] = ParameterStyleKey;
			keymaps[VerboseColumnNamesKey.ToUpper()] = VerboseColumnNamesKey;
			keymaps["VERBOSE COLUMNS"] = VerboseColumnNamesKey;
			keymaps["VERBOSE COLUMN NAMES"] = VerboseColumnNamesKey;
			keymaps["VERBOSECOLUMNS"] = VerboseColumnNamesKey;
			keymaps["COLUMNS VERBOSE"] = VerboseColumnNamesKey;
			keymaps[PersistSecurityInfoKey.ToUpper()] = PersistSecurityInfoKey;
			keymaps["PERSIST SECURITY INFO"] = PersistSecurityInfoKey;
			keymaps[RowCacheSizeKey.ToUpper()] = RowCacheSizeKey;
			keymaps["ROW CACHE SIZE"] = RowCacheSizeKey;
			keymaps[QueryTimeoutKey.ToUpper()] = QueryTimeoutKey;
			keymaps["QUERY TIMEOUT"] = QueryTimeoutKey;
		}

		private const string HostKey = "Host";
		private const string PortKey = "Port";
		private const string DatabaseKey = "Database";
		private const string UserNameKey = "UserName";
		private const string PasswordKey = "Password";
		private const string SchemaKey = "Schema";
		private const string CreateKey = "Create";
		private const string BootOrCreateKey = "BootOrCreate";
		private const string ParameterStyleKey = "ParameterStyle";
		private const string VerboseColumnNamesKey = "VerboseColumnNames";
		private const string PersistSecurityInfoKey = "PersistSecurityInfo";
		private const string RowCacheSizeKey = "RowCacheSize";
		private const string QueryTimeoutKey = "QueryTimeout";

		private const string DefaultHost = "localhost";
		private const int DefaultPort = 9157;
		private const string DefaultDatabase = "";
		private const string DefaultUserName = "";
		private const string DefaultPassword = "";
		private const string DefaultSchema = "";
		private const bool DefaultCreate = false;
		private const bool DefaultBootOrCreate = false;
		private const ParameterStyle DefaultParameterStyle = ParameterStyle.Marker;
		private const bool DefaultVerboseColumnName = false;
		private const bool DefaultPersistSecurityInfo = false;
		private const int DefaultRowCacheSize = 1024;
		private const int DefaultQueryTimeout = Int32.MaxValue;

		private static readonly Dictionary<string, object> defaults;
		private static readonly Dictionary<string, string> keymaps;

		private string host;
		private int port;
		private string database;
		private string userName;
		private string password;
		private string schema;
		private bool verboseColumnNames;
		private ParameterStyle paramStyle;
		private bool persistSecurityInfo;
		private int rowCacheSize;
		private int queryTimeout;

		private ArrayList keys;

		public override bool IsFixedSize {
			get { return true; }
		}

		public override object this[string keyword] {
			get {
				keyword = MappKey(keyword);
				if (base.ContainsKey(keyword))
					return base[keyword];
				return defaults[keyword];
			}
			set { SetValue(keyword, value); }
		}

		public override ICollection Keys {
			get {
				if (keys == null) {
					keys = new ArrayList();
					keys.Add(HostKey);
					keys.Add(PortKey);
					keys.Add(DatabaseKey);
					keys.Add(SchemaKey);
					keys.Add(UserNameKey);
					keys.Add(PasswordKey);
					keys.Add(PersistSecurityInfoKey);
					keys.Add(VerboseColumnNamesKey);
					keys.Add(ParameterStyleKey);
					keys.Add(RowCacheSizeKey);
					keys.Add(QueryTimeoutKey);
					keys = ArrayList.ReadOnly(keys);
				}
				return keys;
			}
		}

		public override ICollection Values {
			get {
				ArrayList list = new ArrayList();
				list.Add(host);
				list.Add(port);
				list.Add(database);
				list.Add(schema);
				list.Add(userName);
				list.Add(password);
				list.Add(persistSecurityInfo);
				list.Add(verboseColumnNames);
				list.Add(paramStyle);
				list.Add(rowCacheSize);
				keys.Add(queryTimeout);
				return list;
			}
		}

		[DisplayName("Data Source")]
		[RefreshProperties(RefreshProperties.All)]
		public string DataSource {
			get {
				string dataSource = host;
				if (port > 0)
					dataSource += ":" + port;
				return dataSource;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");

				base["Data Source"] = value;

				int index = value.IndexOf(':');
				if (index != -1) {
					Port = Int32.Parse(value.Substring(index));
					value = value.Substring(0, index);
				}

				Host = value;
			}
		}

		[DisplayName("Host Address")]
		[RefreshProperties(RefreshProperties.All)]
		public string Host {
			get { return host; }
			set {
				host = value;
				base[HostKey] = value;
			}
		}

		[DisplayName("Server Port")]
		[RefreshProperties(RefreshProperties.All)]
		public int Port {
			get { return port; }
			set {
				port = value;
				base[PortKey] = value;
			}
		}

		[DisplayName("Database Name")]
		[RefreshProperties(RefreshProperties.All)]
		public string Database {
			get { return database; }
			set {
				base[DatabaseKey] = value;
				database = value;
			}
		}

		[DisplayName("Schema")]
		[RefreshProperties(RefreshProperties.All)]
		public string Schema {
			get { return schema; }
			set {
				base[SchemaKey] = value;
				schema = value;
			}
		}

		[DisplayName("User Name")]
		[RefreshProperties(RefreshProperties.All)]
		public string UserName {
			get { return userName; }
			set {
				base[UserNameKey] = value;
				userName = value;
			}
		}

		[DisplayName("User Password")]
		[PasswordPropertyText(true)]
		[RefreshProperties(RefreshProperties.All)]
		public string Password {
			get { return password; }
			set {
				base[PasswordKey] = value;
				password = value;
			}
		}

		[DisplayName("Persist Security Info")]
		[RefreshProperties(RefreshProperties.All)]
		public bool PersistSecurityInfo {
			get { return persistSecurityInfo; }
			set {
				base[PersistSecurityInfoKey] = value;
				persistSecurityInfo = value;
			}
		}

		[DisplayName("Verbose Column Names")]
		[RefreshProperties(RefreshProperties.All)]
		public bool VerboseColumnNames {
			get { return verboseColumnNames; }
			set {
				base[VerboseColumnNamesKey] = value;
				verboseColumnNames = value;
			}
		}

		[DisplayName("Parameter Style")]
		[RefreshProperties(RefreshProperties.All)]
		public ParameterStyle ParameterStyle {
			get { return paramStyle; }
			set {
				base[ParameterStyleKey] = value;
				paramStyle = value;
			}
		}

		[DisplayName("Row Cache Size")]
		[RefreshProperties(RefreshProperties.All)]
		public int RowCacheSize {
			get { return rowCacheSize; }
			set {
				base[RowCacheSizeKey] = value;
				rowCacheSize = value;
			}
		}

		[DisplayName("Query Timeout")]
		[RefreshProperties(RefreshProperties.All)]
		public int QueryTimeout {
			get { return queryTimeout; }
			set {
				base[QueryTimeoutKey] = value;
				queryTimeout = value;
			}
		}

		private void InitToDefault() {
			host = DefaultHost;
			port = DefaultPort;
			database = DefaultDatabase;
			userName = DefaultUserName;
			password = DefaultPassword;
			schema = DefaultSchema;
			paramStyle = DefaultParameterStyle;
			verboseColumnNames = DefaultVerboseColumnName;
			persistSecurityInfo = DefaultPersistSecurityInfo;
			rowCacheSize = DefaultRowCacheSize;
			queryTimeout = DefaultQueryTimeout;
		}

		private void SetValue(string key, object value) {
			if (key == null)
				throw new ArgumentNullException("key");

			key = MappKey(key);

			switch(key) {
				case HostKey: {
					if (value == null) {
						host = DefaultHost;
						base.Remove(key);
					} else {
						Host = value.ToString();
					}
					break;
				}
				case PortKey:
					if (value == null) {
						port = DefaultPort;
						base.Remove(key);
					} else {
						Port = ToInt32(value);
					}
					break;
				case DatabaseKey:
					if (value == null) {
						database = DefaultDatabase;
						base.Remove(key);
					} else {
						Database = value.ToString();
					}
					break;
				case SchemaKey:
					if (value == null) {
						schema = DefaultSchema;
						base.Remove(key);
					} else {
						Schema = value.ToString();
					}
					break;
				case UserNameKey:
					if (value == null) {
						userName = DefaultUserName;
						base.Remove(key);
					} else {
						UserName = value.ToString();
					}
					break;
				case PasswordKey:
					if (value == null) {
						password = DefaultPassword;
						base.Remove(key);
					} else {
						Password = value.ToString();
					}
					break;
				case PersistSecurityInfoKey:
					if (value == null) {
						persistSecurityInfo = DefaultPersistSecurityInfo;
						base.Remove(key);
					} else {
						PersistSecurityInfo = ToBoolean(value);
					}
					break;
				case VerboseColumnNamesKey:
					if (value == null) {
						verboseColumnNames = DefaultVerboseColumnName;
						base.Remove(key);
					} else {
						VerboseColumnNames = ToBoolean(value);
					}
					break;
				case ParameterStyleKey:
					if (value == null) {
						paramStyle = DefaultParameterStyle;
						base.Remove(key);
					} else {
						ParameterStyle = (ParameterStyle) value;
					}
					break;
				case RowCacheSizeKey:
					if (value == null) {
						rowCacheSize = DefaultRowCacheSize;
						base.Remove(key);
					} else {
						RowCacheSize = ToInt32(value);
					}
					break;
				case QueryTimeoutKey:
					if (value == null) {
						queryTimeout = DefaultQueryTimeout;
						base.Remove(key);
					} else {
						QueryTimeout = ToInt32(value);
					}
					break;
				default:
					throw new ArgumentException();
			}
		}

		private string MappKey(string key) {
			key = key.ToUpper().Trim();
			if (!keymaps.ContainsKey(key))
				throw new ArgumentException("The connection string keyword is not supported: " + key);
			return keymaps[key];
		}
		
		private static bool ToBoolean(object value) {
			if (value == null)
				throw new ArgumentException();

			if (value is bool)
				return (bool) value;

			if (value is string) {
				string s = value.ToString().ToUpper();
				if (s == "YES" || s == "ENABLED" || 
					s == "TRUE" || s == "ON")
					return true;
				if (s == "NO" || s == "DISABLED" ||
					s == "FALSE" || s == "OFF")
					return false;
			}

			if (value is IConvertible)
				(value as IConvertible).ToBoolean(null);

			throw new ArgumentException();
		}

		private static int ToInt32(object value) {
			if (value == null)
				throw new ArgumentException();

			if (value is int)
				return (int) value;

			if (value is string) {
				string s = value.ToString();
				return Int32.Parse(s, CultureInfo.InvariantCulture);
			}

			if (value is IConvertible)
				return (value as IConvertible).ToInt32(null);

			throw new ArgumentException();
		}

		public override void Clear() {
			base.Clear();
			InitToDefault();
		}

		public override bool ContainsKey(string keyword) {
			keyword = keyword.ToUpper().Trim();
			if (!keymaps.ContainsKey(keyword))
				return false;
			return base.ContainsKey(keymaps[keyword]);
		}

		public override bool Remove(string keyword) {
			if (!ContainsKey(keyword))
				return false;

			this[keyword] = null;
			return true;
		}

		public override bool ShouldSerialize(string keyword) {
			if (!ContainsKey(keyword))
				return false;

			keyword = keyword.ToUpper().Trim();
			string key = keymaps[keyword];
			if (persistSecurityInfo && key == PasswordKey)
				return false;

			return base.ShouldSerialize(key);
		}

		public override bool TryGetValue(string keyword, out object value) {
			if (!ContainsKey(keyword)) {
				value = null;
				return false;
			}

			return base.TryGetValue(keymaps[keyword.ToUpper().Trim()], out value);
		}
	}
}