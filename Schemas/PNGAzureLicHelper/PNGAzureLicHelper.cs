using System.Data;
using Terrasoft.Common;
using Terrasoft.Core;
using Terrasoft.Core.DB;

namespace Terrasoft.Configuration
{
	public class AzureLicHelper
	{

		private readonly UserConnection _userConnection;
		private const int MaxCallCount = 200;

		public AzureLicHelper(UserConnection userConnection) {
			_userConnection = userConnection;
		}

		private Query GetCheckLicQuery(string apiKey) {
			var select = new Select(_userConnection)
				.Column(Func.Count("Id")).As("Count")
				.From("SysAzureCogInfo")
				.Where("ApiKey").IsEqual(Column.Const(apiKey));
			select.InitializeParameters();
			return select;
		}

		private Insert GetInsertQuery(string apiKey) {
			var insert = new Insert(_userConnection)
				.Into("SysAzureCogInfo")
				.Set("ApiKey", Column.Parameter(apiKey))
				.Set("CreatedById", Column.Parameter(_userConnection.CurrentUser.Id));
			insert.InitializeParameters();
			return insert;
		}

		protected void AddInfo(string apiKey) {
			using (DBExecutor executor = _userConnection.EnsureDBConnection()) {
				GetInsertQuery(apiKey).Execute(executor);
			}
		}

		public bool CheckLic(string apiKey) {
			using (DBExecutor dbExec = _userConnection.EnsureDBConnection()) {
				using (IDataReader reader = dbExec.ExecuteReader(GetCheckLicQuery(apiKey).GetSqlText())) {
					if (reader.Read()) {
						AddInfo(apiKey);
						var count = reader.GetColumnValue<int>("Count");
						return count < MaxCallCount;
					}
				}
			}
			return false;
		}

	}
}