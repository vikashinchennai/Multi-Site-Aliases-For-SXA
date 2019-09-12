namespace Sitecore.Foundation.MultiSiteAliases.SheerWindow
{
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Foundation.MultiSiteAliases.Model;
    using Sitecore.SecurityModel;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Collections;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class NextGenAliasesForm : DialogForm
    {
        /// <summary>The list.</summary>
        protected Listbox NewAliases;
        protected Listbox ExistingAliases;
        /// <summary></summary>
        protected Edit NewAlias;

        protected void Add_Click()
        {
            string str = this.NewAlias.Value;
            if (str.Length == 0)
            {
                SheerResponse.Alert(MultiSiteAliases.Constants.InputFieldValueMissing);
            }
            else
            {
                AliasInfo aliasInfo = new AliasInfo(str);

                foreach (string input in aliasInfo.AscendersAndName)
                {
                    if (!Regex.IsMatch(input, Settings.ItemNameValidation, RegexOptions.ECMAScript))
                    {
                        SheerResponse.Alert(MultiSiteAliases.Constants.InvalidName);
                        return;
                    }
                    if (input.Length > Settings.MaxItemNameLength)
                    {
                        SheerResponse.Alert(MultiSiteAliases.Constants.MaxLength, Settings.MaxItemNameLength.ToString());
                        return;
                    }
                }
                Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
                Error.AssertItemFound(itemFromQueryString);

                ArrayList arrayList = new ArrayList();
                foreach (ListItem control in this.NewAliases.Selected)
                {
                    var test = control.Value.Split(MultiSiteAliases.Constants.HypenSplitChar);
                    string path = ShortID.Decode(StringUtil.Mid(test.LastOrDefault(), 0));
                    Item obj = itemFromQueryString.Database.GetItem(path);
                    if (obj != null)
                    {
                        arrayList.Add((object)new Aliases()
                        {
                            SiteName = string.Join(MultiSiteAliases.Constants.HypenSplit, test.Take(test.Count() - 1).ToArray()),
                            Item = obj
                        });
                    }
                }
                if (arrayList.Count == 0)
                {
                    SheerResponse.Alert(MultiSiteAliases.Constants.SiteNotSelected);
                    return;
                }

                foreach (Aliases eachItem in arrayList)
                {
                    this.CreateAlias(aliasInfo, itemFromQueryString, eachItem.Item, eachItem.SiteName);
                }

                this.NewAlias.Value = string.Empty;
                SheerResponse.SetModified(false);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.CanRunApplication(MultiSiteAliases.Constants.AliasesAccessPath);
            Assert.ArgumentNotNull((object)e, nameof(e));
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
                this.RefreshList();
        }

        protected void Remove_Click()
        {
            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
            Error.AssertItemFound(itemFromQueryString);
            ArrayList arrayList = new ArrayList();
            foreach (System.Web.UI.Control control in this.ExistingAliases.Selected)
            {
                string path = ShortID.Decode(StringUtil.Mid(control.ID.Split(MultiSiteAliases.Constants.HypenSplitChar).LastOrDefault(), 0));
                Item obj = itemFromQueryString.Database.GetItem(path);
                if (obj != null)
                {
                    arrayList.Add((object)obj);
                }
            }
            if (arrayList.Count == 0)
            {
                SheerResponse.Alert(MultiSiteAliases.Constants.AliasesNotSelected);
            }
            else
            {
                foreach (Item obj in arrayList)
                {
                    obj.Delete();
                    Log.Audit((object)this, MultiSiteAliases.Constants.RemoveAliases, AuditFormatter.FormatItem(obj));
                }
                RefreshPostDeletion(itemFromQueryString);
            }
        }

        private void CreateAlias(AliasInfo aliasInfo, Item targetItemToLink, Item destinationFolder, string siteName)
        {
            Assert.ArgumentNotNull((object)targetItemToLink, nameof(targetItemToLink));
            Assert.ArgumentNotNull((object)destinationFolder, nameof(destinationFolder));
            Assert.ArgumentNotNull((object)siteName, nameof(siteName));
            Error.AssertItemFound(destinationFolder);
            bool status = false;
            ID destinationItemId = null;
            ID destinationFolderId = destinationFolder.ID;//To Add to the List View and Log

            foreach (string ascender in aliasInfo.Ascenders)
            {
                destinationFolder = destinationFolder.Children[ascender];
                if (destinationFolder == null)
                {
                    SheerResponse.Alert(string.Format(MultiSiteAliases.Constants.ParentAliasesNotFound, (object)ascender, siteName));
                    return;
                }
            }
            var customAliases = aliasInfo.Name;

            if (destinationFolder.Children[customAliases] != null)
            {
                SheerResponse.Alert(MultiSiteAliases.Constants.AliasesAlreadyExists, customAliases, siteName);
                return;
            }
            status = CreateNewAliases(targetItemToLink, customAliases, destinationFolder, out destinationItemId);

            if (status)
            {
                var id = destinationFolderId.ToShortID() + MultiSiteAliases.Constants.HypenSplit + destinationItemId.ToShortID();
                //Site Name Aliases apth
                var header = string.Format(MultiSiteAliases.Constants.HeaderFormat, siteName, aliasInfo.AliasesNameWithPath);
                InsertAddedAliasesInUi(header, id);
            }
        }

        private bool CreateNewAliases(Item target, string customAliases, Item destinationFolder, out ID createdItemId)
        {
            var customAliasesTemplateId = Context.ContentDatabase.GetTemplate(MultiSiteAliases.Constants.Template.AliasesTemplateId);

            using (new SecurityDisabler())
            {
                Item newItem = null;
                try
                {
                    newItem = destinationFolder.Add(customAliases, customAliasesTemplateId);

                    if (newItem != null)
                    {
                        newItem.Editing.BeginEdit();
                        var linkValue = (target.Paths.IsMediaItem) ?
                                    string.Format(MultiSiteAliases.Constants.AliasesMediaLinkFieldValue, (object)target.ID)
                                    : string.Format(MultiSiteAliases.Constants.AliasesItemLinkFieldValue, target.Paths.ContentPath, (object)target.ID);
                        
                        newItem[MultiSiteAliases.Constants.Fields.LinkedField] = linkValue;
                        newItem.Editing.EndEdit();
                        Log.Audit((object)this, MultiSiteAliases.Constants.Log.AliasesCreated, AuditFormatter.FormatItem(newItem));
                        createdItemId = newItem.ID;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    newItem?.Editing?.CancelEdit();
                    Log.Error(MultiSiteAliases.Constants.Log.AliasesFailed + AuditFormatter.FormatItem(newItem), ex, this);
                }
            }
            createdItemId = null;
            return false;
        }

        private void RefreshPostDeletion(Item itemFromQueryString)
        {
            SheerResponse.Eval(MultiSiteAliases.Constants.SheerWindow.RemoveAliases);

            var aliasesInfo = new AliasInfo().AliasesWindow(itemFromQueryString);

            if (aliasesInfo != null && aliasesInfo.AddedSites != null)
            {
                using (new SecurityDisabler())
                {
                    foreach (var eachSite in aliasesInfo.AddedSites.Distinct().GroupBy(f => f.AliasesItemId))
                    {
                        var siteName = string.Join(MultiSiteAliases.Constants.Comma, eachSite.Select(f => f.SiteName));
                        var pathIds = string.Join(MultiSiteAliases.Constants.Colon, eachSite.Select(s => s.AliasesPathId.ToShortID()).Distinct());

                        var _item = eachSite.FirstOrDefault();
                        var header = string.Format(MultiSiteAliases.Constants.HeaderFormat, siteName, _item.AliasesName);
                        var id = pathIds + MultiSiteAliases.Constants.HypenSplit + eachSite.Key.ToShortID();

                        InsertAddedAliasesInUi(header, id);
                    }
                }
            }
        }

        private void InsertAddedAliasesInUi(string header, string id)
        {
            SheerResponse.Eval(string.Format(MultiSiteAliases.Constants.SheerWindow.CreateAliases
                                                , StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(id))
                                                , StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(header))
                                                , StringUtil.EscapeJavascriptString(StringUtil.EscapeQuote(MultiSiteAliases.Constants.I + id))));
        }

        private void RefreshList()
        {
            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Context.ContentDatabase);
            Error.AssertItemFound(itemFromQueryString);

            var aliasesInfo = new AliasInfo().AliasesWindow(itemFromQueryString);
            using (new SecurityDisabler())
            {
                this.NewAliases.Controls.Clear();
                this.ExistingAliases.Controls.Clear();
                if (aliasesInfo != null)
                {
                    if (aliasesInfo.AllSites != null)
                    {
                        foreach (var item in aliasesInfo.AllSites.GroupBy(f => f.AliasesPathId))
                        {
                            var siteNames = string.Join(MultiSiteAliases.Constants.Comma, item.Select(sm => sm.SiteName));

                            ListItem listItem = new ListItem();

                            this.NewAliases.Controls.Add((System.Web.UI.Control)listItem);
                            listItem.ID = MultiSiteAliases.Constants.I + item.Key.ToShortID();
                            //Site Name
                            listItem.Header = string.Format(MultiSiteAliases.Constants.SiteName, siteNames);
                            listItem.Value = siteNames.Replace(MultiSiteAliases.Constants.SpaceChar, MultiSiteAliases.Constants.HypenSplitChar) + MultiSiteAliases.Constants.HypenSplit + item.Key.ToShortID();
                            listItem.Selected = false;
                        }
                    }
                    if (aliasesInfo.AddedSites != null)
                    {
                        foreach (var eachSite in aliasesInfo.AddedSites.Distinct().GroupBy(f => f.AliasesItemId))
                        {
                            ListItem listItem = new ListItem();
                            var siteName = string.Join(MultiSiteAliases.Constants.Comma, eachSite.Select(f => f.SiteName));
                            var pathIds = string.Join(MultiSiteAliases.Constants.Colon, eachSite.Select(s => s.AliasesPathId.ToShortID()).Distinct());

                            var _item = eachSite.FirstOrDefault();

                            this.ExistingAliases.Controls.Add((System.Web.UI.Control)listItem);
                            listItem.ID = MultiSiteAliases.Constants.I + pathIds + MultiSiteAliases.Constants.HypenSplit + eachSite.Key.ToShortID();
                            //Site Name Aliases apth
                            listItem.Header = string.Format(MultiSiteAliases.Constants.HeaderFormat, siteName, _item.AliasesName);
                            listItem.Value = pathIds + MultiSiteAliases.Constants.HypenSplit + eachSite.Key.ToShortID();
                            listItem.Selected = false;
                        }
                    }
                }
            }
        }

    }
}
