namespace Terrasoft.Configuration
{
	using System;
	using System.Collections.Generic;
	using Terrasoft.Core;
	using Terrasoft.Core.DB;
	using Terrasoft.Core.Scheduler;
	
	public class AutoTaggerJob : IJobExecutor
	{

		public void Execute(UserConnection userConnection, IDictionary<string, object> parameters) {
			AutoTagger autoTagger = new AutoTagger(userConnection);
			autoTagger.Assign((Guid)parameters["entityRecordId"], (string)parameters["entitySchemaName"]);
		}
	
	}

}
