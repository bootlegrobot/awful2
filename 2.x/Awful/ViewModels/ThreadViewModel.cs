using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Awful.ViewModels
{
    public class ThreadViewModel : Common.BindableBase
    {
        private Data.ThreadDataSource _thread;
        public Data.ThreadDataSource Thread
        {
            get { return this._thread; }
            set { SetProperty(ref _thread, value, "Thread"); }
        }

        private ICommand _threadBookmarkCommand;
        public ICommand ThreadBookmarkCommand
        {
            get
            {
                if (_threadBookmarkCommand == null)
                    _threadBookmarkCommand = new Commands.ToggleBookmarkCommand();

                return _threadBookmarkCommand;
            }
        }

        private Data.ThreadPageDataProxy _proxy;
        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                SetProperty(ref _selectedIndex, value, "SelectedIndex"); 
            }
        }

        public Data.ThreadPageDataSource _selectedItem;
        public Data.ThreadPageDataSource SelectedItem
        {
            get { return this._selectedItem; }
            set { SetProperty(ref _selectedItem, value, "SelectedItem"); }
        }

        public string Title
        {
            get
            {
                if (this.Thread != null)
                    return this.Thread.Title;

                return "Unknown Thread Title";
            }
        }

        private IList<Data.ThreadPageDataSource> _pages;
        public IList<Data.ThreadPageDataSource> Pages
        {
            get
            {
                if (Thread == null)
                    return null;

                if (_pages == null)
                    SetPages(Thread.PageCount);

                return _pages;
            }
            set { SetProperty(ref _pages, value, "Items"); }
        }

        public IList<Data.ThreadPageDataSource> Items { get { return Pages; } }

        private void SetPages(int value)
        {
            this._pages = new ThreadPageProxy(Thread);
            OnPropertyChanged("Items");
        }

        public void UpdateModel(string threadId, string forumId, int pageNumber)
        {
            var thread = Data.MainDataSource.Instance.FindThreadByID(threadId, forumId);
            if (thread != null)
            {
                this.Thread = thread;
                this._pages = null;
            }
        }

        public void UpdateModel(ThreadViewPageState state)
        {
            var thread = Data.MainDataSource.Instance.FindThreadByID(state.ThreadID, state.ForumID);
            if (thread == null)
            {
                ThreadMetadata metadata = new ThreadMetadata();
                metadata.ThreadID = state.ThreadID;
                metadata.Title = state.Title;
                metadata.PageCount = state.PageCount;
                thread = new Data.ThreadDataSource(metadata);
            }

            this.Thread = thread;
            int index = state.PageNumber - 1;
            var page = this.Pages[index];
            page.Html = state.Html;
            page.Posts = state.Posts;

        }
    }

    public class ThreadPageProxy : KollaSoft.KSVirtualizedList<Data.ThreadPageDataSource>
    {
        private const int MAX_CAPACITY = 20;
        private int _count;
        private Queue<Data.ThreadPageDataSource> _cache;
        private Data.ThreadDataSource _source;

        public ThreadPageProxy(Data.ThreadDataSource source) : base()
        {
            this._source = source;
            this._count = source.PageCount;
            this._cache = new Queue<Data.ThreadPageDataSource>(MAX_CAPACITY);
        }

        public override int IndexOf(Data.ThreadPageDataSource item)
        {
            int index = item.PageNumber - 1;
            return index;
        }

        protected override Data.ThreadPageDataSource GetItem(int index)
        {
            var pageNumber = index + 1;
            var page = this._cache.Where(p => p.PageNumber == pageNumber).FirstOrDefault();
            if (page == null)
            {
                var metadata = new ThreadPageMetadata() { ThreadID = _source.ThreadID, PageNumber = pageNumber };
                page = new Data.ThreadPageDataProxy(metadata) { };
                UpdateCache(page);
            }

            return page;
        }

        private void UpdateCache(Data.ThreadPageDataSource page)
        {
            this._cache.Enqueue(page);
            if (this._cache.Count > MAX_CAPACITY)
                this._cache.Dequeue();
        }

        protected override void SetItem(Data.ThreadPageDataSource item, int index)
        {
            item.PageNumber = index + 1;
        }

        public override bool Contains(Data.ThreadPageDataSource item)
        {
            int index = item.PageNumber - 1;
            return index >= 0 && index < _count;
        }

        public override int Count
        {
            get
            {
                return _count;
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }
    }

}
