using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.Common
{
    internal sealed class HtmlScrapeThreadRequest : ThreadRequestStrategy
    {
        protected override Uri CreateThreadPageUri(string threadId, int pageNumber)
        {
            return new Uri(string.Format(
                "http://forums.somethingawful.com/showthread.php?threadid={0}&pagenumber={1}",
                threadId,
                pageNumber));
        }

        protected override Uri CreateThreadNewPostPageUri(string threadId)
        {
            return new Uri(string.Format(
                "http://forums.somethingawful.com/showthread.php?threadid={0}&goto=newpost",
                threadId));
        }

        protected override Uri CreateThreadLastPostPageUri(string threadId)
        {
            return new Uri(string.Format(
                "http://forums.somethingawful.com/showthread.php?threadid={0}&goto=lastpost",
                threadId));
        }

        protected override Uri CreateThreadPageByUserUri(string threadId, string userId)
        {
            return new Uri(string.Format(
                "http://forums.somethingawful.com/showthread.php?threadid={0}&userid={1}",
                threadId,
                userId));
        }

        public override ThreadPageMetadata LoadThreadPage(Uri uri)
        {
            string url = uri.AbsoluteUri;
            var doc = new AwfulWebClient().FetchHtml(url).ToHtmlDocument();
            return ThreadPageParser.ParseThreadPage(doc);
        }

        public override ThreadPageMetadata LoadThreadPage(string threadId, int pageNumber)
        {
            return LoadThreadPage(CreateThreadPageUri(threadId, pageNumber));
        }

        public override ThreadPageMetadata LoadThreadUnreadPostPage(string threadId)
        {
            return LoadThreadPage(CreateThreadNewPostPageUri(threadId));
        }

        public override ThreadPageMetadata LoadThreadLastPostPage(string threadId)
        {
            return LoadThreadPage(CreateThreadLastPostPageUri(threadId));
        }

        public override IThreadPostRequest BeginReplyToThread(string threadId)
        {
            return new ThreadReplyRequest(threadId);
        }

        public override string QuotePost(string postId)
        {
            var post = new ThreadPostMetadata() { PostID = postId };
            return ThreadTasks.Quote(post);
        }

        public override bool MarkPostAsRead(string postId)
        {
            return ThreadTasks.MarkAsLastRead(new ThreadPostMetadata() { PostID = postId });
        }

        public override IThreadPostRequest BeginPostEdit(string postId)
        {
            return new ThreadPostEditRequest(postId);
        }
    }
}
