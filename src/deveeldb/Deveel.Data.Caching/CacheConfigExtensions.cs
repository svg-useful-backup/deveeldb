﻿// 
//  Copyright 2010-2015 Deveel
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

using Deveel.Data.Configuration;

namespace Deveel.Data.Caching {
	public static class CacheConfigExtensions {
		public static int DataCacheSize(this IConfiguration config) {
			return config.GetValue<int>(CacheConfigKeys.DataCacheSize);
		}

		public static void DataCacheSize(this IConfiguration config, int value) {
			config.SetValue(CacheConfigKeys.DataCacheSize, value);
		}
	}
}