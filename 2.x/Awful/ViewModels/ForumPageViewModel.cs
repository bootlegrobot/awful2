using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Awful.ViewModels
{
    public class ForumPageViewModel : PagedListViewModel<Data.ThreadDataSource>
    {
        private const string EMPTY_STATUS = "Tap here to refresh the thread list.";

        public ForumPageViewModel() : base() { UpdateStatus(EMPTY_STATUS); }

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

        protected override IEnumerable<Data.ThreadDataSource> LoadPageInBackground(int index)
        {
            UpdateStatus("Loading thread list...");

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
                UpdateStatus("Formatting...");
                threads = Data.ForumThreadCollection.CreateThreadSources(forumPageData, forum);
            }
            return threads;
        }

        public virtual IEnumerable<Data.ThreadDataSource> LoadThreadsFromPage(string forumId, int pageNumber)
        {
            var forum = new ForumMetadata() { ForumID = forumId };
            return LoadThreadsFromPage(forum, pageNumber);
        }

        protected override void OnError(Exception exception)
        {
            this.IsRunning = false;
            MessageBox.Show("Could not load the requested page.", ":(", MessageBoxButton.OK);
            UpdateStatus(EMPTY_STATUS);
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }
    }
}