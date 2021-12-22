using System.Collections.Generic;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Security;

namespace VIU.Plugin.SolrSearch.Infrastructure
{
	public class SolrPermissionProvider : IPermissionProvider
	{
		public static readonly PermissionRecord ManageSearch = new PermissionRecord { Name = "Configure Solr Search", SystemName = "Configure Search", Category = "Standard" };
		
		public IEnumerable<PermissionRecord> GetPermissions()
		{
			return new[]
			{
				ManageSearch
			};
		}

		public HashSet<(string systemRoleName, PermissionRecord[] permissions)> GetDefaultPermissions()
		{
            return new HashSet<(string, PermissionRecord[])>
            {
                (
                    NopCustomerDefaults.AdministratorsRoleName,
                    new[]
                    {
	                    ManageSearch
                    }
                )
            };
		}
	}
}