using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kollasoft;

namespace Awful.Deprecated
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

        protected override Uri CreatePostMarkAsReadUri(string threadId, string index)
        {
            return new Uri(string.Format(
                "http://forums.somethingawful.com/showthread.php?setseen&threadid={0}&index={1}",
                threadId,
                index));
        }

        public override ThreadPageMetadata LoadThreadPage(Uri uri)
        {
            string url = uri.AbsoluteUri;
            var doc = new AwfulWebClient().FetchHtml(url).ToHtmlDocument();
            var page = ThreadPageParser.ParseThreadPage(doc);

            // check for post id
            Kollasoft.KSUrlParser query = uri.Parse();

            if (page != null && query.Query.ContainsKey("postid"))
            {
                string id = query.Query["postid"];
                var targetPost = page.Posts.Where(post => post.PostID.Equals(id)).SingleOrDefault();
                if (targetPost != null)
                {
                    page.TargetPostIndex = page.Posts.IndexOf(targetPost);
                }
            }

            return page;
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

        public override bool MarkPostAsRead(string threadId, string index)
        {
            var markUri = CreatePostMarkAsReadUri(threadId, index);
            return ThreadTasks.MarkAsLastRead(new ThreadPostMetadata() { MarkPostUri = markUri });
        }

        public override bool MarkPostAsRead(ThreadPostMetadata post)
        {
            return ThreadTasks.MarkAsLastRead(post);
        }

        public override IThreadPostRequest BeginPostEdit(string postId)
        {
            return new ThreadPostEditRequest(postId);
        }
    }
}
