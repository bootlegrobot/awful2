using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private ObservableCollection<Data.ThreadDataSource> _threads;

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
                this._threads = threads as Data.ForumThreadCollection;
                this.Items = threads;
            }

            this.CurrentIndex = currentPage - 1;
        }

        protected override IEnumerable<Data.ThreadDataSource> LoadDataPage(int index)
        {
            IEnumerable<Data.ThreadDataSource> threads = null;
            var pageSource = _threads as Data.ForumThreadCollection;

            
            if (pageSource != null)
            {
                pageSource.ForumID = Forum.ForumID;
                var page = LoadThreadsFromPage(Forum.ForumID, index + 1);
                if (page != null)
                    threads = page;
            }

            return threads;
        }

        private IEnumerable<Data.ThreadDataSource> LoadThreadsFromPage(ForumMetadata forum, int pageNumber)
        {
            List<Data.ThreadDataSource> threads = null;
            var forumPageData = forum.LoadPage(pageNumber);
            if (forumPageData != null)
            {
                threads = Data.ForumThreadCollection.CreateThreadSources(forumPageData, forum);
            }
            return threads;
        }

        public virtual IEnumerable<Data.ThreadDataSource> LoadThreadsFromPage(string forumId, int pageNumber)
        {
            var forum = new ForumMetadata() { ForumID = forumId };
            return LoadThreadsFromPage(forum, pageNumber);
        }
    }
}