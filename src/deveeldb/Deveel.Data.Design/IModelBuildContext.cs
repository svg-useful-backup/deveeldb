﻿using System;

namespace Deveel.Data.Design {
	public interface IModelBuildContext {
		void OnBuildModel(DbModelBuilder builder);
	}
}
