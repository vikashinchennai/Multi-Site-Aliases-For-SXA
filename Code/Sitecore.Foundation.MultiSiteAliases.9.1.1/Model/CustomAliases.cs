namespace Sitecore.Foundation.MultiSiteAliases.Model
{
    using Sitecore.Data;
    using System.Collections.Generic;
    internal class CustomAliases
    {
        public CustomAliases()
        {
            AllSites = new List<Sites>();
            AddedSites = new List<Sites>();
        }
        public ID ItemId { get; set; }
        public List<Sites> AddedSites { get; set; }
        public List<Sites> AllSites { get; set; }
    }
}
