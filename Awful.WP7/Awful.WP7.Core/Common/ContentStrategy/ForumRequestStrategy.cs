using Awful;
using System;
using System.Collections.Generic;

namespace Awful
{
    public abstract class ForumRequestStrategy
    {
        public abstract Uri CreateForumListUri();
        public abstract Uri CreateForumPageUri(string forumId, int pageNumber);
        protected abstract Uri CreateUserBookmarksUri();

        public abstract ForumPageMetadata LoadUserBookmarks();
        public abstract ForumPageMetadata LoadForumPage(string forumId, int pageNumber);
        public abstract IList<ForumMetadata> LoadForumList();
    }
}