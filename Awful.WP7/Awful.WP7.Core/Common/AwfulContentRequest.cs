using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful
{
    public class AwfulContentRequest
    {
        public static ThreadRequestStrategy Threads { get; private set; }
        public static ForumRequestStrategy Forums { get; private set; }
        public static PrivateMessageRequestStrategy Messaging { get; private set; }
        public static SmileyRequestStrategy Smilies { get; private set; }

        static AwfulContentRequest()
        {
            Threads = new Common.HtmlScrapeThreadRequest();
            Forums = new Common.HtmlScrapeForumRequest();
            Messaging = new Common.PrivateMessageHttpRequest();
            Smilies = new Common.HtmlScrapeSmileyRequest();
        }
    }
}
