namespace Sitecore.Foundation.MultiSiteAliases.Pipeline
{
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore;
    using Sitecore.Abstractions;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.DependencyInjection;
    using Sitecore.Diagnostics;
    using Sitecore.Pipelines.HttpRequest;
    using Sitecore.Pipelines.HttpRequest.HandlerMapping;
    using Sitecore.Web;
    public class AliasResolver : HttpRequestProcessor
    {

        public AliasResolver()
        { }
        private string AliasItemPath { get; set; }
        public override void Process(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, MultiSiteAliases.Constants.Args);
            if (!Settings.AliasesActive)
            {
                Tracer.Warning(MultiSiteAliases.Constants.AliasesNotActive);
                return;
            }
            Database database = Context.Database;
            if (database == null)
            {
                Tracer.Warning(MultiSiteAliases.Constants.ContextDatabaseNotFound);
                return;
            }

            Profiler.StartOperation(MultiSiteAliases.Constants.ResolveAliases);

            if (ID.TryParse(Context.Site?.Properties[MultiSiteAliases.Constants.SiteProperties.SiteLevelAliases], out ID aliasesPathId))
            {
                var aliasesPathItem = database.GetItem(aliasesPathId);
                if (aliasesPathItem == null)
                    return;

                var aliaseFullPath = aliasesPathItem?.Paths?.FullPath;
                if (aliaseFullPath == null || !aliaseFullPath.StartsWith(MultiSiteAliases.Constants.AliasesRootPath))
                    return;


                AliasItemPath = aliaseFullPath.Replace(MultiSiteAliases.Constants.AliasesRootPath, "") + '/' + args.LocalPath;
                if (database.Aliases.Exists(AliasItemPath))
                {
                    if (this.ProcessItem(args))
                    {
                        Item obj = Context.Item;
                        if (obj != null && obj.Paths != null && obj.Paths.IsMediaItem && obj.TemplateID != TemplateIDs.MediaFolder)
                        {
                            var mediaManager = ServiceLocator.ServiceProvider.GetRequiredService<BaseMediaManager>();
                            string mediaUrl = mediaManager?.GetMediaUrl((MediaItem)obj);
                            if (!string.IsNullOrEmpty(mediaUrl))
                            {
                                var handlerMapper = (HandlerMapper)new DirectHandlerMapper(ServiceLocator.ServiceProvider.GetRequiredService<BaseFactory>()?.GetCustomHandlers());
                                if (handlerMapper != null && handlerMapper.TryFindMatchingHandler(mediaUrl, out CustomHandler matchingHandler) && matchingHandler != null)
                                {
                                    Context.Data.RawUrl = mediaUrl;
                                    args.HttpContext.RewritePath(matchingHandler.Handler, mediaUrl, args.Url.QueryString, true);
                                    args.AbortPipeline();
                                }
                            }
                        }
                    }
                    //To Redirect External Site
                    else if (this.ProcessExternalUrl(args))
                    {
                        string path = Context.Page?.FilePath;
                        if (!string.IsNullOrEmpty(path) && WebUtil.IsExternalUrl(path))
                            WebUtil.Redirect(path);
                    }

                }
            }
            Profiler.EndOperation();
        }

        private bool ProcessExternalUrl(HttpRequestArgs args)
        {
            string targetUrl = Context.Database.Aliases.GetTargetUrl(AliasItemPath);
            if (targetUrl.Length > 0)
                return this.ProcessExternalUrl(targetUrl);
            return false;
        }
        private bool ProcessExternalUrl(string path)
        {
            if (Context.Page.FilePath.Length > 0)
                return false;
            Context.Page.FilePath = path;
            return true;
        }

        private bool ProcessItem(HttpRequestArgs args)
        {
            // Use the AliasItemPath property to find the site specific aliases
            ID targetID = Context.Database.Aliases.GetTargetID(AliasItemPath);
            if (!targetID.IsNull)
            {
                Item item = args.GetItem(targetID);
                if (item != null && Context.Item == null)
                {
                    Context.Item = item;
                }
                return true;
            }
            // Use the AliasItemPath property to output porper information
            Tracer.Error(string.Format(MultiSiteAliases.Constants.AliasesNotLinked, AliasItemPath));
            return false;
        }
    }
}