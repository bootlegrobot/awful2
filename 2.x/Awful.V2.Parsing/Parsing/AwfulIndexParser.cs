using System;
using System.Linq;
using HtmlAgilityPack;
using System.Net;
using Awful.Core.Event;

namespace Awful.Core.Web
{
    public static class AwfulIndexParser
    {
        private const string USERSESSION_NAME_LINK = "member.php?";
        public static string ParseUserSessionName(HtmlDocument doc)
        {
            var parent = doc.DocumentNode;
            var usernameNode = parent.Descendants("a")
                .Where(node => node.GetAttributeValue("href", "").Contains(USERSESSION_NAME_LINK))
                .FirstOrDefault();

            string result = string.Empty;
            if (usernameNode != null)
            {
                result = HttpUtility.HtmlDecode(usernameNode.InnerText);
            }
            else
            {
                Logger.AddEntry("Could not parse username from index page. Lowtax!!!!");
                result = "Unknown User_" + DateTime.Now.Date.Ticks;
            }

            return result;    
        }

        public static string ParseUserSessionName(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            return ParseUserSessionName(doc);
        }
    }
}
