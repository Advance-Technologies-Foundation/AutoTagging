using System;
using System.Collections.Generic;
using System.Linq;
using TagExtractor;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Core.Store;

namespace Terrasoft.Configuration
{
	public class AutoTagger
	{
		private const string AutoTaggedEntitiesCacheGroupName = "AutoTaggedEntitiesStoreCache";
		private const string AutoTaggedEntitiesCacheItemName = "AutoTaggedEntitiesStoreCacheItem";
		private const string AzureServiceUrlSysSettingCode = "AutoTaggingAPIUrl";
		private const string AzureServiceKeySysSettingCode = "AutoTaggingAPIKey";

		private UserConnection _userConnection;
		protected UserConnection UserConnection {
			get {
				return _userConnection;
			}
		}

		private EntityCollection _autoTaggedEntities;
		protected EntityCollection AutoTaggedEntities {
			get {
				return _autoTaggedEntities ?? (_autoTaggedEntities = GetAutoTaggedEntities());
			}
		}

		private string _azureServiceUrl;
		protected string AzureServiceUrl {
			get {
				return _azureServiceUrl ?? (_azureServiceUrl = (string)GetSysSettingValue(AzureServiceUrlSysSettingCode));
			}
		}

		private string _azureServiceKey;
		protected string AzureServiceKey {
			get {
				return _azureServiceKey ?? (_azureServiceKey = (string)GetSysSettingValue(AzureServiceKeySysSettingCode));
			}
		}

		public AutoTagger(UserConnection userConnection) {
			_userConnection = userConnection;
		}

		private object GetSysSettingValue(string sysSettingsName) {
			object settingsValue;
			if (Core.Configuration.SysSettings.TryGetValue(UserConnection, sysSettingsName, out settingsValue)) {
				return settingsValue;
			}
			return null;
		}

		private EntityCollection GetAutoTaggedEntities() {
			EntitySchemaQuery esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "PNGAutoTaggedSection");
			esq.Cache = UserConnection.SessionCache.WithLocalCaching(AutoTaggedEntitiesCacheGroupName);
			esq.CacheItemName = AutoTaggedEntitiesCacheItemName;
			esq.IsDistinct = true;
			esq.UseAdminRights = false;
			EntitySchemaQueryColumn entitySchemaNameColumn = esq.AddColumn("[SysModule:Id:PNGSection].[SysModuleEntity:Id:SysModuleEntity].[SysSchema:UId:SysEntitySchemaUId].Name");
			entitySchemaNameColumn.Name = "EntitySchemaName";
			esq.AddColumn("PNGColumns");
			return esq.GetEntityCollection(UserConnection);
		}

		private void ExecuteStoreProcedureWithParameters(string schemaName, List<string> tags, Guid recordId) {
			if (!tags.Any()) {
				return;
			}
			var procedureName = "tsp_AssignTags";
			var storedProcedure = new StoredProcedure(UserConnection, procedureName);
			storedProcedure.PackageName = "tspkg_AutoTagger";
			var strTags = String.Join("; ", tags);
			storedProcedure.WithParameter("tags", strTags);
			storedProcedure.WithParameter("schemaName", schemaName);
			storedProcedure.WithParameter("recordId", recordId);
			storedProcedure.Execute();
		}

		private List<string> GetEntitySchemaExistColumns(string entitySchemaName) {
			Entity lookupItem = AutoTaggedEntities.Where(e => e.GetTypedColumnValue<string>("EntitySchemaName") == entitySchemaName).FirstOrDefault();
			string[] lookupItemColumns = lookupItem?.GetTypedColumnValue<string>("PNGColumns").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (lookupItemColumns == null || lookupItemColumns.Count() == 0) {
				return null;
			}
			EntitySchema entitySchema = UserConnection.EntitySchemaManager.GetInstanceByName(entitySchemaName);
			return entitySchema?.Columns.GetByNames(lookupItemColumns)?.Select(c => c.Name)?.ToList();
		}

		private string GetEntityText(Guid entityRecordId, string entitySchemaName) {
			var entityText = "";
			List<string> entitySchemaExistColumns = GetEntitySchemaExistColumns(entitySchemaName);
			EntitySchemaQuery esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, entitySchemaName);
			if (entitySchemaExistColumns != null) {
				foreach (string columnName in entitySchemaExistColumns) {
					esq.AddColumn(columnName);
				}
			} else if (esq.RootSchema.PrimaryDisplayColumn != null) {
				esq.AddColumn(esq.RootSchema.PrimaryDisplayColumn.Name);
			} else {
				return entityText;
			}
			Entity entity = esq.GetEntity(UserConnection, entityRecordId);
			if (entity != null) {
				if (entitySchemaExistColumns != null) {
					foreach (string columnName in entitySchemaExistColumns) {
						entityText += " " + entity.GetTypedColumnValue<string>(columnName);
					}
				} else {
					entityText = entity.PrimaryDisplayColumnValue;
				}
			}
			return entityText;
		}

		public bool IsEntityAutoTagged(string entitySchemaName) {
			return AutoTaggedEntities.Any(e => e.GetTypedColumnValue<string>("EntitySchemaName") == entitySchemaName);
		}

		public void Assign(Guid entityRecordId, string entitySchemaName) {
			if (!IsEntityAutoTagged(entitySchemaName)) {
				return;
			}

			string text = GetEntityText(entityRecordId, entitySchemaName);
			Assign(entityRecordId, entitySchemaName, text);
		}

		public void Assign(Guid entityRecordId, string entitySchemaName, string text) {
			if (string.IsNullOrEmpty(text) || AzureServiceUrl == null || AzureServiceKey == null || !IsEntityAutoTagged(entitySchemaName)) {
				return;
			}
			var licHelper = new AzureLicHelper(UserConnection);
			var tExtractor = new TagExtractorExecuter(AzureServiceUrl, AzureServiceKey, licHelper.CheckLic);
			var tags = tExtractor.GetTags(entityRecordId, text);
			ExecuteStoreProcedureWithParameters(entitySchemaName, tags, entityRecordId);
		}
	}
}