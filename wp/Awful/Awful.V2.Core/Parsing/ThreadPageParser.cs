using System;
using System.Linq;
using HtmlAgilityPack;
using System.Net;
using System.Collections.Generic;

namespace Awful
{
    public static class ThreadPageParser
    {
        private const string THREAD_POST_HTML_ELEMENT = "table";
        private const string THREAD_POST_HTML_ATTRIBUTE = "class";
        private const string THREAD_POST_HTML_VALUE = "post ";

        private const string THREAD_PAGE_NUMBER_ELEMENT_1 = "select";
        private const string THREAD_PAGE_NUMBER_ATTRIBUTE_1 = "data-url";
        private const string THREAD_PAGE_NUMBER_VALUE_1 = "showthread.php";
        private const string THREAD_PAGE_NUMBER_ELEMENT_2 = "option";
        private const string THREAD_PAGE_NUMBER_ATTRIBUTE_2 = "selected";
        private const string THREAD_PAGE_NUMBER_VALUE_2 = "selected";
        private const string THREAD_PAGE_NUMBER_ATTRIBUTE_3 = "value";

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

        internal static ThreadPageMetadata ParseThreadPage(HtmlDocument document)
        {
            return ProcessThreadPage(document.DocumentNode);
        }

        public static ThreadPageMetadata ParseThreadPage(Uri threadPageUri)
        {
            var client = new AwfulWebClient();
            var htmlDoc = client.FetchHtml(threadPageUri.AbsoluteUri);
            return ParseThreadPage(htmlDoc);
        }

        private static ThreadPageMetadata ProcessThreadPage(HtmlNode top)
        {
            AwfulDebugger.AddLog(top,  AwfulDebugger.Level.Debug, "Parsing HTML for posts...");

            // first, let's generate data about the thread
            ThreadPageMetadata page = new ThreadPageMetadata()
                .ProcessParent(top)
                .ParsePageNumberAndMaxPages(top)
                .ParsePostTable(top);
         
            return page;
        }

        private static ThreadPageMetadata ParsePostTable(this ThreadPageMetadata page, HtmlNode top)
        {
            if (page.Posts == null) { page.Posts = new List<ThreadPostMetadata>(); }

            AwfulDebugger.AddLog(top, AwfulDebugger.Level.Debug, "Parsing post data...");

            var postArray = top.Descendants(THREAD_POST_HTML_ELEMENT)
                .Where(tables => tables.GetAttributeValue(THREAD_POST_HTML_ATTRIBUTE, "").Equals(THREAD_POST_HTML_VALUE))
                .ToArray();

            int index = 1;

            foreach (var postNode in postArray)
            {
                ThreadPostMetadata post = PostParser.ParsePost(postNode);
                post.PageIndex = index;
                page.Posts.Add(post);
                index++;
            }

            // check if there is at least one post on the page. If not, there was a parsing error.
            if (page.Posts.Count == 0)
                throw new Exception("Parse Error: Could not parse the posts on this page.");

            AwfulDebugger.AddLog(top, AwfulDebugger.Level.Debug, "Thread page parsing complete.");

            return page;
        }

        private static ThreadPageMetadata ParsePageNumberAndMaxPages(this ThreadPageMetadata page, HtmlNode top)
        {
            // now, let's parse page number
            AwfulDebugger.AddLog(top, AwfulDebugger.Level.Debug, "Parsing page number...");

            int pageNumber = -1;
            int lastPage = -1;

            var currentPageNode = top.Descendants(THREAD_PAGE_NUMBER_ELEMENT_1)
                .Where(node => node.GetAttributeValue(THREAD_PAGE_NUMBER_ATTRIBUTE_1, "").Contains(THREAD_PAGE_NUMBER_VALUE_1))
                .FirstOrDefault();

            if (currentPageNode != null)
            {
                var currentPageOptions = currentPageNode.Descendants(THREAD_PAGE_NUMBER_ELEMENT_2);

                var currentPageOption = currentPageOptions
                    .Where(node => node.GetAttributeValue(THREAD_PAGE_NUMBER_ATTRIBUTE_2, "").Equals(THREAD_PAGE_NUMBER_VALUE_2))
                    .FirstOrDefault();

                AwfulDebugger.AddLog(top, AwfulDebugger.Level.Debug, "Parsing total number of pages...");

                var lastPageOption = currentPageOptions.LastOrDefault();

                if (currentPageOption != null)
                    int.TryParse(currentPageOption.GetAttributeValue(THREAD_PAGE_NUMBER_ATTRIBUTE_3, ""), out pageNumber);

                if (lastPageOption != null)
                    int.TryParse(lastPageOption.GetAttributeValue(THREAD_PAGE_NUMBER_ATTRIBUTE_3, ""), out lastPage);

                page.PageNumber = pageNumber;
                page.LastPage = lastPage;
            }

            else
            {
                AwfulDebugger.AddLog(top, AwfulDebugger.Level.Debug, "Page number parsing failed.");
                // set page number
                page.PageNumber = 1;
                page.LastPage = 1;
            }

           

            return page;
        }

        private static ThreadPageMetadata ProcessParent(this ThreadPageMetadata page, HtmlNode top)
        {
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
    }
}
