namespace Sitecore.Foundation.MultiSiteAliases.Model
{
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore;
    using Sitecore.Abstractions;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.DependencyInjection;
    using Sitecore.Diagnostics;
    using Sitecore.Text;
    using Sitecore.Web;
    using Sitecore.XA.Foundation.Multisite;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class AliasInfo
    {
        public AliasInfo() : base()
        {

        }
        public readonly ListString path;
        public AliasInfo(string value)
        {
            Assert.ArgumentNotNullOrEmpty(value, nameof(value));
            value = StringUtil.RemovePrefix(MultiSiteAliases.Constants.ForwardSlash, value);
            value = StringUtil.RemovePostfix(MultiSiteAliases.Constants.ForwardSlash, value);
            this.path = new ListString(value, MultiSiteAliases.Constants.ForwardSlashChar);
        }
        public IEnumerable<string> Ascenders
        {
            get
            {
                if (this.path.Count > 1)
                {
                    for (int i = 0; i < this.path.Count - 1; ++i)
                        yield return this.path[i];
                }
            }
        }
        public IEnumerable<string> AscendersAndName
        {
            get
            {
                return (IEnumerable<string>)this.path.Items;
            }
        }
        public string AliasesNameWithPath
        {
            get
            {
                return string.Join(MultiSiteAliases.Constants.ForwardSlash, AscendersAndName);
            }
        }
        public string Name
        {
            get
            {
                return this.path[this.path.Count - 1];
            }
        }
        private List<Sites> Sites()
        {
            var service = ServiceLocator.ServiceProvider.GetRequiredService<ISiteInfoResolver>();
            List<Sites> sitesInfo = new List<Sites>();

            foreach (var site in service.Sites)
            {
                GetSiteInfo(sitesInfo, 
                            site.Name, 
                            site?.Properties[MultiSiteAliases.Constants.SiteProperties.SiteLevelAliases],
                            site?.Properties[MultiSiteAliases.Constants.SiteProperties.RootPath]);
            }
            foreach (var site in Sitecore.Sites.SiteManager.GetSites())
            {
                GetSiteInfo(sitesInfo, 
                            site.Name, 
                            site?.Properties[MultiSiteAliases.Constants.SiteProperties.SiteLevelAliases],
                            site?.Properties[MultiSiteAliases.Constants.SiteProperties.RootPath]);
            }
            return sitesInfo;
        }



        private static void GetSiteInfo(List<Sites> sitesInfo, string siteName, string aliasesVal, string rootPath)
        {
            if (string.IsNullOrEmpty(siteName) || string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(aliasesVal) || sitesInfo.Any(s => s.SiteName == siteName))
                return;
            if (ID.TryParse(aliasesVal, out ID aliasesId) && !aliasesId.IsNull && aliasesId.Guid != Guid.Empty)
                sitesInfo.Add(new Sites() { SiteName = siteName, AliasesPathId = aliasesId, RootPath = rootPath });
        }

        public CustomAliases AliasesWindow(Item item)
        {
            var customAliases = new CustomAliases();
            customAliases.ItemId = item.ID;
            var selectedItemFullPath = item.Paths.FullPath;

            foreach (var site in Sites())
            {
                if (selectedItemFullPath.StartsWith(site.RootPath) || selectedItemFullPath.StartsWith(MultiSiteAliases.Constants.MediaPath))
                    ProcessEachSite(item, customAliases, site.SiteName, site.AliasesPathId);
            }
            

            return customAliases;
        }

        private static void ProcessEachSite(Item item, CustomAliases customAliases, string siteName, ID id)
        {
            var customAliasesFolderItem = Context.ContentDatabase.GetItem(id);
            if (customAliasesFolderItem != null)
            {
                //To Show in Multi List for user
                customAliases.AllSites.Add(new Sites() { SiteName = siteName, AliasesPathId = id });

                //To Show Aliases as same Url pattern
                var roothFullPath = customAliasesFolderItem.Paths.FullPath + MultiSiteAliases.Constants.ForwardSlash;

                var itemId = item.ID;
                foreach (var eachAliases in customAliasesFolderItem
                                            .Axes.GetDescendants()
                                            .Where(f => f.TemplateID == MultiSiteAliases.Constants.Template.AliasesTemplateId))
                {
                    LinkField lnkField = eachAliases.Fields[MultiSiteAliases.Constants.Fields.LinkedField];
                    if (lnkField?.TargetID == itemId)
                    {
                        var path = eachAliases.Paths.FullPath.Replace(roothFullPath, string.Empty);
                        //To Show as already Exists Entry
                        customAliases.AddedSites.Add(new Sites()
                        {
                            SiteName = siteName,
                            AliasesPathId = id,
                            AliasesName = path,
                            AliasesItemId = eachAliases.ID
                        });
                    }
                }
            }
        }
    }
}