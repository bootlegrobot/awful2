using System;
using HtmlAgilityPack;

namespace Awful.Parsing
{
    public static class Parser
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
            return null;
        }
    }
}
