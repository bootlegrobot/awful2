using System;
using System.Linq;
using HtmlAgilityPack;
using System.Net;
using System.Collections.Generic;

namespace Awful
{
    public static class ThreadPageParser
    {
        public static ThreadPageMetadata ParseThreadPage(string threadID)
        {
            ThreadPageMetadata data = null;
            string uri = string.Format("http://forums.somethingawful.com/forumdisplay.php?threadid={0}&goto=newpost", threadID);
            return data;
        }

        public static ThreadPageMetadata ParseThreadPage(string threadID, int pageNumber)
        {
            ThreadPageMetadata data = null;
            string uri = string.Format("http://forums.somethingawful.com/forumdisplay.php?threadid={0}&pagenumber={1}", threadID,
                pageNumber);
            return data;
        }

        public static ThreadPageMetadata ParseThreadPage(HtmlDocument document)
        {
            return ProcessThreadPage(document.DocumentNode);
        }

        private static ThreadPageMetadata ProcessThreadPage(HtmlNode top)
        {
            /// Logger.AddEntry("AwfulThreadPage - Parsing HTML for posts...");

            // first, let's generate data about the thread
            ThreadPageMetadata page = ProcessParent(top);

            // now, let's parse page number
            int pageNumber = -1;
            var currentPageNode = top.Descendants("span")
                .Where(node => node.GetAttributeValue("class", "").Equals("curpage"))
                .FirstOrDefault();

            if (currentPageNode != null) { int.TryParse(currentPageNode.InnerText, out pageNumber); }

            // set page number
            page.PageNumber = pageNumber;


            // parse other thread page data
            var maxPagesNode = top.Descendants("div")
               .Where(node => node.GetAttributeValue("class", "").Equals("pages top"))
               .FirstOrDefault();

            if (maxPagesNode != null)
            {
                int totalPages = ParseMaxPagesNode(maxPagesNode);
                page.LastPage = totalPages;

                ///Logger.AddEntry(string.Format("AwfulThreadPage - maxPagesNode found: {0}", totalPages));
            }

            if (page.Posts == null) { page.Posts = new List<ThreadPostMetadata>(); }

            var postArray = top.Descendants("table")
                .Where(tables => tables.GetAttributeValue("class", "").Equals("post"))
                .ToArray();

            int index = 1;

            foreach (var postNode in postArray)
            {
                ThreadPostMetadata post = PostParser.ParsePost(postNode);
                post.PageIndex = index;
                page.Posts.Add(post);
                index++;
            }

            return page;
        }

        private static ThreadPageMetadata ProcessParent(HtmlNode top)
        {
            ThreadPageMetadata page = new ThreadPageMetadata();
            var threadNode = top.Descendants()
                .Where(node => node.GetAttributeValue("class", "").Equals("bclast"))
                .FirstOrDefault();

            if (threadNode != null)
            {
                string idString = threadNode.GetAttributeValue("href", "");
                idString = idString.Split('=')[1];
                string title = HttpUtility.HtmlDecode(threadNode.InnerText);

                page.ThreadTitle = title;
                page.ThreadID = idString;
            }

            return page;
        }

        private static int ParseMaxPagesNode(HtmlNode maxPagesNode)
        {
            // should look something like "Pages ([numbers])"...
            string text = maxPagesNode.InnerText;
            var tokens = text.Split(' ');

            // tokens should be ["Pages"], ["(<Last Page Number>)"], ...
            string pageToken = tokens[1];

            // remove garbage characters
            pageToken = pageToken.Replace("(", "");
            pageToken = pageToken.Replace(")", "");
            pageToken = pageToken.Replace(":", "");

            // extract page number
            int result = 1;
            Int32.TryParse(pageToken, out result);
            return result == 0 ? 1 : result;
        }
    }
}
