namespace Sitecore.Foundation.MultiSiteAliases.Model
{
    using Sitecore.Data;
    internal class Sites
    {
        public string SiteName { get; set; }
        public ID AliasesPathId { get; set; }
        public string AliasesName { get; set; }
        public ID AliasesItemId { get; set; }
        public string RootPath { get; set; }
    }
}
