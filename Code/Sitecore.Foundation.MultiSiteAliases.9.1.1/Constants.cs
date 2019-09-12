using Sitecore.Data;

namespace Sitecore.Foundation.MultiSiteAliases
{
    internal class Constants
    {

        public class SiteProperties
        {
            public const string RootPath = "rootPath";
            public const string SiteLevelAliases = "SiteLevelAliases";
        }

        public class Template
        {
            public static ID AliasesTemplateId => new ID("{54BCFFB7-8F46-4948-AE74-DA5B6B5AFA86}");
        }

        public const string Args = "args";
        public const string AliasesNotActive = "Aliases are not active.";
        public const string ContextDatabaseNotFound = "There is no context database in AliasResover.";
        public const string ResolveAliases = "Resolve alias.";
        public const string InputFieldValueMissing = "Enter a value in the Add Input field.";

        public const string InvalidName = "The name contains invalid characters.";
        public const string MaxLength = "The name is too long, it cant Cross {0}";

        public const string HypenSplit = "-";
        public const char HypenSplitChar = '-';
        public const char SpaceChar = ' ';
        public const string ForwardSlash = "/";
        public const string I = "I";
        public const string Comma = ",";
        public const string Colon = ":";
        public const char ForwardSlashChar = '/';

        public const string SiteNotSelected = "Select the Site From Site List To Create the Aliases.";

        public const string AliasesAccessPath = "Content Editor/Ribbons/Chunks/Page Urls";
        public const string MediaPath = "/sitecore/media library";
        public const string ContentRootPath = "/sitecore/content/";
        public const string AliasesRootPath = "/sitecore/system/Aliases/";

        public const string AliasesNotSelected = "Select an alias from the list.";

        public const string RemoveAliases = "Remove alias: {0}";

        public const string ParentAliasesNotFound = "The parent alias '{0}' does not exist for Site {1}";
        public const string AliasesNotLinked = "An alias for {0} exists, but points to a non-existing item.";
        public const string AliasesAlreadyExists = "Aliases {0} Exists for the Site {1} Already";
        public const string HeaderFormat = "SiteName: {0} Aliases-> {1}";
        public const string AliasesItemLinkFieldValue = "<link linktype=\"internal\" url=\"{0}\" id=\"{1}\" />";
        public const string AliasesMediaLinkFieldValue = "<link linktype=\"media\" id=\"{0}\" />";
        public const string SiteName = "SiteName: {0}";

        internal class Fields
        {
            public const string LinkedField = "Linked item";
        }

        internal class SheerWindow
        {
            public const string RemoveAliases = "scRemoveAllAlias()";
            public const string CreateAliases = "scCreateAlias({0},{1},{2});";
        }

        internal class Log
        {
            public const string AliasesCreated = "Alias Created: {0}";
            public const string AliasesFailed = "Alias Creation Failed: ";
        }
    }
}
