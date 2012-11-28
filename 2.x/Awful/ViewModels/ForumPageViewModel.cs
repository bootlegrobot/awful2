using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.ViewModels
{
    public class ForumPageViewModel : PagedListViewModel<Data.ThreadDataSource>
    {
        public ForumPageViewModel() : base() { }

        private Data.ForumDataSource _forum;
        public Data.ForumDataSource Forum
        {
            get { return _forum; }
            private set 
            { 
                SetProperty(ref _forum, value, "Forum");
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            private set { SetProperty(ref _title, value, "Title"); }
        }

        private Data.ForumThreadCollection _threads;

        public void UpdateModel(string forumId, int currentPage)
        {
            var forum = Data.MainDataSource.Instance.FindForumByID(forumId);
            if (forum != null)
            {
                this.Forum = forum;
                this.Title = forum.Title;
            }

            var threads = Data.MainDataSource.Instance.FindForumThreadsByID(forumId);
            if (threads != null)
            {
                this._threads = threads;
                this.Items = threads;
            }

            this.CurrentIndex = currentPage - 1;
        }

        protected override IEnumerable<Data.ThreadDataSource> LoadDataWork(int index)
        {
            IEnumerable<Data.ThreadDataSource> threads = null;
            var page = _threads.LoadThreadsFromPage(index + 1);
            if (page != null)
                threads = page;

            return page;
        }
    }
}
