using Awful;
using System;

namespace Awful
{
    public abstract class ThreadRequestStrategy
    {
        protected abstract Uri CreateThreadPageUri(string threadId, int pageNumber);
        protected abstract Uri CreateThreadNewPostPageUri(string threadId);
        protected abstract Uri CreateThreadLastPostPageUri(string threadId);
        protected abstract Uri CreateThreadPageByUserUri(string threadId, string userId);

        public abstract ThreadPageMetadata LoadThreadPage(string threadId, int pageNumber);
        public abstract ThreadPageMetadata LoadThreadUnreadPostPage(string threadId);
        public abstract ThreadPageMetadata LoadThreadLastPostPage(string threadId);
        public abstract IThreadPostRequest BeginReplyToThread(string threadId);

        public abstract string QuotePost(string postId);
        public abstract bool MarkPostAsRead(string postId);
        public abstract IThreadPostRequest BeginPostEdit(string postId);
    }
}