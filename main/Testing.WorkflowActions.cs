using System;
using System.Net;
using System.Linq;
using System.Net.Mail;
using System.Configuration;
using System.Collections.Generic;
using System.Collections.Specialized;

using Sitecore;
using System.Web;
using Sitecore.SecurityModel;
using Sitecore.Collections;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Workflows.Simple;
using Sitecore.Security.Accounts;
using Sitecore.Security.AccessControl;


namespace Testing.WorkflowActions
{
    public class NotifyNextStepUser
    {

        public void Process(WorkflowPipelineArgs args)
        {
            Item contentItem = args.DataItem;
            var contentWorkflow = contentItem.Database.WorkflowProvider.GetWorkflow(contentItem);
            var contentHistory = contentWorkflow.GetHistory(contentItem);
            string oldName, emailTemplateName, emailTemplateBody, workflowComment, userRolesSetting, previewUrl;
            CheckboxField dontSend2AuthorField;
            oldName = userRolesSetting = workflowComment = String.Empty;

            List<User> emailUserList = new List<User>();

            try
            {

                using (new SecurityDisabler())
                {

                    Item processorItem = args.ProcessorItem.InnerItem;  // Current workflow state
                    Item nextStateItem = HelperClass.GetNextState(args); // Next workflow state
                    User submittingUser = null;
                    bool hasPresentation = false;


                    Sitecore.Diagnostics.Log.Info("============Work Flow Notification Start==================", this);

                    emailTemplateName = processorItem.Fields["Email template"].Value;
                    dontSend2AuthorField = processorItem.Fields["Dont Send to author"];
                    workflowComment = (!String.IsNullOrEmpty(args.Comments)) ? args.Comments : "---";
                    hasPresentation = HelperClass.DoesItemHasPresentationDetails(contentItem.ID.Guid.ToString());

                    // Generate preview link
                    if (hasPresentation)
                    {

                        previewUrl =
                        string.Format("{0}://{1}/?sc_itemid=%7b{2}%7d&sc_lang={3}&sc_mode=preview",
                        HttpContext.Current.Request.Url.Scheme,
                        HttpContext.Current.Request.Url.Host,
                        contentItem.ID.Guid.ToString().ToUpper(),
                        contentItem.Language.Name);
                    }
                    else
                    {

                        previewUrl =
                        string.Format("{0}://{1}/sitecore/shell/Applications/Content Editor.aspx?la={3}&fo={2}",
                        HttpContext.Current.Request.Url.Scheme,
                        HttpContext.Current.Request.Url.Host,
                        contentItem.ID.Guid.ToString().ToUpper(),
                        contentItem.Language.Name);
                    }


                    Item emailTemplateItem = HelperClass.GetItemByFieldName("Title", "/WhereEverYourEmailTemplateIs/Email Template/Workflow/" + emailTemplateName);

                    if (emailTemplateItem != null)
                    {

                        emailTemplateBody = WebUtility.HtmlDecode(emailTemplateItem.Fields["Text"].Value);

                        // Get all workflow action under next workflow state
                        IEnumerable<Item> items = nextStateItem.Children;
                        foreach (Item singleItem in items)
                        {

                            // Get all rule by access rule, find it's user rules. Finally concat all user roles.
                            AccessRuleCollection accessRules = singleItem.Security.GetAccessRules();
                            if (accessRules != null)
                            {
                                userRolesSetting = accessRules2Email(accessRules, userRolesSetting);
                            }
                        }

                        // Get all email addresses by user roles
                        if (userRolesSetting.Length > 0)
                        {
                            emailUserList = HelperClass.GetRecipientsToMail(userRolesSetting.Substring(0, userRolesSetting.Length - 1));
                        }

                        // Get authoer's email address of current content
                        if (contentHistory.Length > 0)
                        {

                            var firstUser = contentHistory.First().User;
                            submittingUser = User.FromName(firstUser, false);
                            if (!String.IsNullOrEmpty(submittingUser.Profile.Email))
                            {
                                emailUserList.Add(submittingUser);
                                Sitecore.Diagnostics.Log.Info("Added Author mail: " + submittingUser.Profile.Email, this);
                            }
                            else
                            {
                                Sitecore.Diagnostics.Log.Info("Author has no mail! ", this);
                            }
                        }

                        // Preparing to send out emails based on a email teamplate from eamil user list.
                        if (emailUserList.Count > 0)
                        {

                            foreach (User singleEmailUser in emailUserList)
                            {

                                try
                                {
                                    string tmpReceiverName = (!String.IsNullOrEmpty(singleEmailUser.Profile.FullName)) ? singleEmailUser.Profile.FullName : singleEmailUser.Profile.UserName;


                                    MailMessage tempEmailMessage = new MailMessage
                                        {
                                            IsBodyHtml = true,
                                            From = new MailAddress(ConfigurationManager.AppSettings["EmailReminder.FromAddress"]),
                                            Subject = "Workflow Notification: " + contentItem.Name,
                                            Body = HelperClass.replacePlaceHodler(
                                                            emailTemplateBody,
                                                            new Dictionary<string, string> { 
                                                            { "[ItemName]", contentItem.Name } ,
                                                            { "[ItemURL]", "<a href='" + previewUrl + "' target='_blank'>Preview Item Page</a>" } , 
                                                            { "[WorkflowName]", processorItem.Parent.DisplayName + " Item" } ,
                                                            { "[NextWorkflowName]", nextStateItem.DisplayName } ,
                                                            { "[SubmitComment]", workflowComment} ,
                                                            { "[Receiver]", tmpReceiverName },
                                                            { "[CurrentActionUser]", Context.User.Name }
                                                        }
                                                )
                                        };

                                    tempEmailMessage.To.Add(singleEmailUser.Profile.Email);

                                    if (tempEmailMessage.To.Count > 0)
                                    {
                                        Sitecore.MainUtil.SendMail(tempEmailMessage);
                                        Sitecore.Diagnostics.Log.Info("Sending Mail to: " + tempEmailMessage.To, this);
                                    }
                                }
                                catch (Exception ex)
                                {

                                    Sitecore.Diagnostics.Log.Error("Sending Mail Error:" + ex.StackTrace, this);
                                }
                            }
                        }
                    }

                    Sitecore.Diagnostics.Log.Info("============Work Flow notification End==================", this);
                }
            }
            catch (Exception ex)
            {

                Sitecore.Diagnostics.Log.Error("NotifyNextStepUser:" + ex, this);
            }
        }

        protected string accessRules2Email(AccessRuleCollection accessRules, string oldUserRolesSetting)
        {

            foreach (AccessRule accessRule in accessRules)
            {
                string name = accessRule.Account.Name;

                if (oldUserRolesSetting.Contains(name) || !accessRule.Account.Domain.Name.ToLower().Contains("TheDomainOfUsersYouWannaSent"))
                {
                    continue;
                }
                string comment = accessRule.AccessRight.Comment;
                var permiss = accessRule.SecurityPermission;

                oldUserRolesSetting += name + "|";
                Sitecore.Diagnostics.Log.Info("Name: " + name + "\\ Comment: " + comment, this);
            }

            return oldUserRolesSetting;
        }

    }
}