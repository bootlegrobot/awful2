using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful
{
    public enum PostRequestType
    {
        Reply, Edit
    }

    public interface IThreadPostRequest
    {
        PostRequestType RequestType { get; }
        string Content { get; set; }
        bool Send();
    }

    internal class ThreadReplyRequest : IThreadPostRequest
    {
        public ThreadReplyRequest(ThreadMetadata thread) { this.Thread = thread; }

        public ThreadReplyRequest(string threadId) : this(new ThreadMetadata() { ThreadID = threadId }) { }

        public ThreadMetadata Thread
        {
            get; private set;
        }

        public PostRequestType RequestType
        {
            get { return PostRequestType.Reply; }
        }

        private string _content;
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

        public bool Send()
        {
            return ThreadTasks.Reply(this.Thread, Content);
        }
    }

    internal class ThreadPostEditRequest : IThreadPostRequest
    {
        public ThreadPostEditRequest(ThreadPostMetadata post)
        {
            this.Post = post;
            Content = ThreadTasks.FetchEditText(post);
        }

        public ThreadPostEditRequest(string postId) : this(new ThreadPostMetadata() { PostID = postId }) { }

        public PostRequestType RequestType
        {
            get { return PostRequestType.Edit; }
        }

        public ThreadPostMetadata Post
        {
            get;
            private set;
        }

        private string _content;
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

        public bool Send()
        {
            return ThreadTasks.Edit(Post, Content);
        }
    }
}
