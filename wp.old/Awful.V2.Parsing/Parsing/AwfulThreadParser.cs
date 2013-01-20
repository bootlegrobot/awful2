// -----------------------------------------------------------------------
// <copyright file="AwfulThreadParser.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Awful.Core.Models;
using HtmlAgilityPack;
using Awful.Core.Models.Factory;

namespace Awful.Core.Web.Parsers
{
    public static class AwfulThreadParser
    {
        public static ThreadPageData ParseFromThreadPage(HtmlDocument doc)
        {
            AwfulThreadPage page = null;
            page = AwfulThreadPageFactory.Build(doc);
            if (page != null)
            {
                page.Html = AwfulThreadPageHtmlFactory.Metrofy(page);
            }
            return page;
        }

        public static AwfulThread ParseFromNode(int forumID, HtmlNode top)
        {
            AwfulThread thread = null;
            thread = AwfulThreadFactory.Build(top, forumID);
            return thread;
        }
    }
}
