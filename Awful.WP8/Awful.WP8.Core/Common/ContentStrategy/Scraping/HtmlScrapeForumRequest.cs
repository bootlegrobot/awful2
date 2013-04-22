using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.Deprecated
{
    internal class HtmlScrapeForumRequest : ForumRequestStrategy
    {
        public override Uri CreateForumListUri()
        {
            return new Uri("http://forums.somethingawful.com/forumdisplay.php?forumid=1");
        }

        public override Uri CreateForumPageUri(string forumId, int pageNumber)
        {
            return new Uri(
                string.Format("http://forums.somethingawful.com/forumdisplay.php?forumid={0}&pagenumber={1}",
                forumId, pageNumber));
        }

        protected override Uri CreateUserBookmarksUri()
        {
            return new Uri("http://forums.somethingawful.com/usercp.php");
        }

        public override ForumPageMetadata LoadUserBookmarks()
        {
            string url = CreateUserBookmarksUri().AbsoluteUri;
            var doc = new AwfulWebClient().FetchHtml(url).ToHtmlDocument();
            return ForumParser.ParseForumPage(doc);
        }

        public override ForumPageMetadata LoadForumPage(string forumId, int pageNumber)
        {
            string url = CreateForumPageUri(forumId, pageNumber).AbsoluteUri;
            var doc = new AwfulWebClient().FetchHtml(url).ToHtmlDocument();
            return ForumParser.ParseForumPage(doc);
        }

        public override IList<ForumMetadata> LoadForumList()
        {
            return new List<ForumMetadata>(ForumTasks.FetchAllForums());
        }
    }
}
