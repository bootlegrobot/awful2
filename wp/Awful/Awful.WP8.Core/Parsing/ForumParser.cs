using System;
using System.Linq;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Net;
using Awful.Web;

namespace Awful
{
    public static class ForumParser
    {
        #region Forum Parsing

        public static bool UseWhitelist { get; set; }

        private static readonly List<string> ForumBlacklist = new List<string>()
        {
            "Main",
            "Discussion",
            "The Finer Arts",
            "The Community",
            "Archives",
            "The Crackhead Clubhouse",
            "Retarded Forum for Assholes",
        };

        private static readonly List<string> ForumWhitelist = new List<string>()
        {
            "Pet Island"
        };

        public static IEnumerable<ForumMetadata> ParseForumList(HtmlDocument doc)
        {
            List<ForumMetadata> forums = new List<ForumMetadata>(100);
            
            if (doc == null)
                return forums;

            var parent = doc.DocumentNode;
           
            var selectNode = parent.Descendants("select")
                .Where(node => node.GetAttributeValue("name", "").Equals("forumid"))
                .FirstOrDefault();

            if (selectNode != null)
            {
                var forumNodes = selectNode.Descendants("option").ToArray();

                foreach (var node in forumNodes)
                {
                    var forum = CreateForumMetadata(node);
                    if (forum != null)
                        forums.Add(forum);
                }
            }

            return forums;        
        }

        private static ForumMetadata CreateForumMetadata(HtmlNode node)
        {
            ForumMetadata result = null;

            var value = node.Attributes["value"].Value;
            int id = -1;

            // instantiate only if the forum has a valid id
            if (int.TryParse(value, out id) && id > -1)
            {
                result = new ForumMetadata() { ForumID = value }
                    .ParseForumName(node)
                    .ParseForumLevelCount(node)
                    .ParseForumGroup();
            }

            return result;
        }

        private static ForumMetadata ParseForumGroup(this ForumMetadata data)
        {
            if (data != null)
                data.ForumGroup = data.GetGroup();

            return data;
        }

        private static ForumMetadata ParseForumName(this ForumMetadata data, HtmlNode node)
        {
            if (data != null)
            {
                string name = node.NextSibling.InnerText;
                name = HttpUtility.HtmlDecode(name);
                if (name != String.Empty)
                {
                    name = name.Replace("-", "");
                    name = name.Trim();
                    data.ForumName = name;
                }

                if (UseWhitelist && !ForumWhitelist.Contains(name))
                    data = null;

                else if (ForumBlacklist.Contains(name))
                    data = null;
            }

            return data;
        }

        private static ForumMetadata ParseForumLevelCount(this ForumMetadata data, HtmlNode node)
        {
            if (data != null)
            {
                string name = node.NextSibling.InnerText;
                name = HttpUtility.HtmlDecode(name);
                if (name != String.Empty)
                {
                    var tokens = name.Split(' ');
                    var countToken = tokens[0];
                    int count = countToken.Length;
                    data.LevelCount = count;
                }
            }
           
            return data;
        }

        #endregion

        public static ForumPageMetadata ParseForumPage(HtmlDocument doc)
        {
            var top = doc.DocumentNode;
            var page = new ForumPageMetadata();
            int pageNumber = -1;

            // first, let's find the forum id
            var formNode = top.Descendants("form")
                .Where(node => node.GetAttributeValue("id", "").Equals("ac_timemachine"))
                .FirstOrDefault();

            if (formNode != null)
            {
                string idString = formNode.GetAttributeValue("action", "");
                // strip undesiriable stuff off
                idString = idString.Replace("/forumdisplay.php?", "");
                idString = idString.Split('=').Last();
                page.ForumID = idString;
            }

            // then, let's find the page number
            var pageNumberNode = top.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "").Equals("curpage"))
                .FirstOrDefault();

            if (pageNumberNode != null)
            {
                var pageNumberText = pageNumberNode.InnerText;
                if (!int.TryParse(pageNumberText, out pageNumber)) { pageNumber = -1; }
            }

            page.PageNumber = pageNumber;

            HandleMaxPages(page, top);
            HandleThreads(page, top);
            return page;
        }

        private static void HandleMaxPages(ForumPageMetadata page, HtmlNode node)
        {
            var maxPagesNode = node.Descendants("div")
                .Where(n => n.GetAttributeValue("class", "").Equals("pages"))
                .FirstOrDefault();

            if (maxPagesNode == null)
            {
                //Logger.AddEntry("AwfulForumPage - Could not parse maxPagesNode.");
                page.PageCount = 1;
            }
            else
            {
                page.PageCount = ExtractMaxForumPages(maxPagesNode);
                //Logger.AddEntry(string.Format("AwfulForumPage - maxPagesNode parsed. Value: {0}", page.Parent.TotalPages));
            }
        }

        private static int ExtractMaxForumPages(HtmlNode node)
        {
            var text = node.InnerHtml.Sanitize();
            var tokens = text.Split(' ');

            if (tokens.Length == 1)
                return 1;

            var number = tokens[1];
            number = number.Replace("(", "");
            number = number.Replace("):", "");

            int result;
            Int32.TryParse(number, out result);
            return result == 0 ? 1 : result;
        }

        private static void HandleThreads(ForumPageMetadata page, HtmlNode node)
        {
            var forumThreadsTable = node.Descendants("table")
                   .Where(n => n.Id.Equals("forum"))
                   .First();

            var threadList = forumThreadsTable.Descendants("tbody").First();
            var threadsInfo = threadList.Descendants("tr");

            page.Threads = GenerateThreadData(page, threadsInfo);
        }

        // TODO: Remember to sort thread data by new posts
        private static IList<ThreadMetadata> GenerateThreadData(ForumPageMetadata page, IEnumerable<HtmlNode> threadsInfo)
        {
            //Logger.AddEntry("AwfulForumPage - Generating thread data...");

            List<ThreadMetadata> data = new List<ThreadMetadata>();
            foreach (var node in threadsInfo)
            {
                var thread = ThreadParser.ParseThread(node);
                data.Add(thread);
            }

            return data;
        }
    }
}
