using Nop.Core;

namespace VIU.Plugin.SolrSearch
{
    /// <summary>
    /// Represents plugin constants
    /// </summary>
    public static class SolrSearchPluginDefaults
    {
        /// <summary>
        /// Gets a plugin system name
        /// </summary>
        public static string SystemName => "SolrSearch";

        /// <summary>
        /// Gets a user agent used to request Sendinblue services
        /// </summary>
        public static string UserAgent => $"nopCommerce-{NopVersion.CURRENT_VERSION}";

        /// <summary>
        /// Gets a name of the synchronization schedule task
        /// </summary>
        public static string BuildIndexTaskName => "Rebuild Solr Index";

        /// <summary>
        /// Gets a type of the synchronization schedule task
        /// </summary>
        public static string BuildIndexTask => "VIU.Plugin.SolrSearch.Tasks.IndexTask";

        /// <summary>
        /// Gets a default synchronization period in hours
        /// </summary>
        public static int DefaultReindexPeriod => 24;
    }
}