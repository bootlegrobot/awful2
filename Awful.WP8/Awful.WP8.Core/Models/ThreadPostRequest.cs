using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.Deprecated
{
    public class ThreadReplyRequest : IThreadPostRequest
    {
        internal ThreadReplyRequest(ThreadMetadata thread) { this.Thread = thread; }

        internal ThreadReplyRequest(string threadId) : this(new ThreadMetadata() { ThreadID = threadId }) { }

        public ThreadMetadata Thread
        {
            get; private set;
        }

        public PostRequestType RequestType
        {
            get { return PostRequestType.Reply; }
        }

        private string _content = string.Empty;
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
            }
        }
    }

    public class ThreadPostEditRequest : IThreadPostRequest
    {
        internal ThreadPostEditRequest(ThreadPostMetadata post)
        {
            this.Post = post;
            Content = ThreadTasks.FetchEditText(post);
        }

        internal ThreadPostEditRequest(string postId) : this(new ThreadPostMetadata() { PostID = postId }) { }

        public PostRequestType RequestType
        {
            get { return PostRequestType.Edit; }
        }

        public ThreadPostMetadata Post
        {
            get;
            private set;
        }

        private string _content = string.Empty;
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
            }
        }

        public Uri Send()
        {
            return ThreadTasks.Edit(Post, Content);
        }
    }
}
