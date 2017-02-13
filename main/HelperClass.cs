    /*************************************************************************************************/
    /*********************There are some helper static methods used in Workflow***********************/
    /*********It is not a completed class. Please add those functions to anywhere you want************/
    /*************************************************************************************************/

        public static Item GetNextState(WorkflowPipelineArgs args)
        {
            Item command = args.ProcessorItem.InnerItem.Parent;
            string nextStateID = command["Next State"];

            if (nextStateID.Length == 0)
            {
                return null;
            }

            Item nextState = args.DataItem.Database.Items[ID.Parse(nextStateID)];

            if (nextState != null)
            {
                return nextState;
            }

            return null;
        }




        public static bool DoesItemHasPresentationDetails(string itemId)
        {
            if (Sitecore.Data.ID.IsID(itemId))
            {
                Item item = Sitecore.Context.Database.GetItem(Sitecore.Data.ID.Parse(itemId));
                if (item != null)
                {
                    return item.Fields[Sitecore.FieldIDs.LayoutField] != null
                           && !String.IsNullOrEmpty(item.Fields[Sitecore.FieldIDs.LayoutField].Value);
                }
            }
            return false;
        }


        public static Item GetItemByFieldName(string fieldName, string itemEndwithPath)
        {
            var searchIndex = ContentSearchManager.GetIndex("sitecore_master_index"); // sub your index name
            using (var context = searchIndex.CreateSearchContext())
            {
                var searchResultItems =
                    context.GetQueryable<SearchResultItem>()
                    .Where(item => item.Path.EndsWith(itemEndwithPath))
                    .FirstOrDefault();

                return searchResultItems == null ? null : searchResultItems.GetItem();
            }
        }




        public static List<User> GetRecipientsToMail(string recipientsFromConfig)
        {
            List<User> emailUserList = new List<User>(); //Just Use to make sure there is no duplicated mail.

            var rgxEmail = new Regex(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                    @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                    @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");

            var userRoleNames = recipientsFromConfig.Split('|');
            foreach (var userRoleNameOrMailAddress in userRoleNames)
            {


                if (Role.Exists(userRoleNameOrMailAddress))
                {

                    var role = Role.FromName(userRoleNameOrMailAddress);
                    var users = RolesInRolesManager.GetUsersInRole(role, true);

                    foreach (var user in users.Where(x => x.IsInRole(role)).Where(user => !string.IsNullOrEmpty(user.Profile.Email)))
                    {
                        //MargeCustomLog.Log.Info("User Mail: " + user.Profile.Email);
                        if (!(emailUserList.Exists(x => x.Profile.Email == user.Profile.Email)))
                        {
                            emailUserList.Add(user);
                        }
                    }
                }

            }

            return emailUserList;
        }


        public static string replacePlaceHodler(string replacingString, Dictionary<string, string> replacePair)
        {
            foreach (KeyValuePair<string, string> entry in replacePair)
            {
                replacingString = replacingString.Replace(entry.Key, entry.Value ?? "---");
            }
            return replacingString;
        }