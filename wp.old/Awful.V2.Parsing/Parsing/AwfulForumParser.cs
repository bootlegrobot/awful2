using System;
using System.Linq;
using System.Collections.Generic;
using Awful.Core.Models;
using HtmlAgilityPack;
using System.Net;
using Awful.Core.Event;
using KollaSoft;

namespace Awful.Core.Web.Parsers
{
    public static class AwfulForumParser
    {
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

        public static IEnumerable<ForumData> ParseForumList(HtmlDocument doc)
        {
            List<ForumData> forums = new List<ForumData>(100);
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
                    var value = node.Attributes["value"].Value;
                    int id = 0;
                    if (Int32.TryParse(value, out id) && id > 0)
                    {
                        string name = node.NextSibling.InnerText;
                        name = HttpUtility.HtmlDecode(name);
                        if (name != String.Empty)
                        {
                            name = name.Replace("-", "");
                            name = name.Trim();
                            if (!ForumBlacklist.Contains(name))
                            {
                                var forum = new AwfulForum() { ID = id, ForumName = name };
                                AwfulSubforum.SetDefaultSubforum(forum);
                                forums.Add(forum);
                            }
                        }
                    }
                }
            }

            return forums;        
        }
        
        public static ForumPageData ParseForumPage(HtmlDocument doc)
        {
            var top = doc.DocumentNode;
            AwfulForum forum = new AwfulForum();
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
                int id = -1;
                if (int.TryParse(idString, out id))
                {
                    forum.ID = id;
                }
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

            var page = new AwfulForumPage(forum, pageNumber);
            HandleMaxPages(page, top);
            HandleThreads(page, top);
            return page;
        }

        private static void HandleMaxPages(AwfulForumPage page, HtmlNode node)
        {
            var maxPagesNode = node.Descendants("div")
                .Where(n => n.GetAttributeValue("class", "").Equals("pages"))
                .FirstOrDefault();

            if (maxPagesNode == null)
            {
                Logger.AddEntry("AwfulForumPage - Could not parse maxPagesNode.");
                page.Parent.TotalPages = 1;
            }
            else
            {
                page.Parent.TotalPages = ExtractMaxForumPages(maxPagesNode);
                Logger.AddEntry(string.Format("AwfulForumPage - maxPagesNode parsed. Value: {0}", page.Parent.TotalPages));
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

        private static void HandleThreads(AwfulForumPage page, HtmlNode node)
        {
            var forumThreadsTable = node.Descendants("table")
                   .Where(n => n.Id.Equals("forum"))
                   .First();

            var threadList = forumThreadsTable.Descendants("tbody").First();
            var threadsInfo = threadList.Descendants("tr");

            page.Threads = GenerateThreadData(page, threadsInfo);
        }

        // TODO: Remember to sort thread data by new posts
        private static IList<AwfulThread> GenerateThreadData(AwfulForumPage page, IEnumerable<HtmlNode> threadsInfo)
        {
            Logger.AddEntry("AwfulForumPage - Generating thread data...");

            List<AwfulThread> data = new List<AwfulThread>();
            foreach (var node in threadsInfo)
            {
                var thread = AwfulThreadParser.ParseFromNode(page.ForumID, node);
                data.Add(thread);
            }
            return data;
        }
    }
}
