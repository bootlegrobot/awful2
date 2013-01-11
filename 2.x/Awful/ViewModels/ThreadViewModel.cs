using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Awful.Data;

namespace Awful.ViewModels
{
    public class ThreadViewModel : Commands.BackgroundWorkerCommand<int>, Controls.IThreadNavContext
    {
        public event Common.ThreadReadyToBindEventHandler ReadyToBind;
        public event EventHandler UpdateFailed;

        private bool _isReady = false;
        public bool IsReady
        {
            get { return _isReady; }
            private set { _isReady = value; }
        }

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

        private ICommand _firstPageCommand;
        public ICommand FirstPageCommand
        {
            get { return _firstPageCommand; }
            set { SetProperty(ref _firstPageCommand, value, "FirstPageCommand"); }
        }

        private ICommand _lastPageCommand;
        public ICommand LastPageCommand
        {
            get { return _lastPageCommand; }
            set { SetProperty(ref _lastPageCommand, value, "LastPageCommand"); }
        }

        private ICommand _customPageCommand;
        public ICommand CustomPageCommand
        {
            get { return _customPageCommand; }
            set { SetProperty(ref _customPageCommand, value, "CustomPageCommand"); }
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

        public void UpdateModel(ThreadDataSource thread)
        {
            if (thread != null)
            {
                this.Thread = thread;
                this._pages = null;
            }

            this.Execute((int)ThreadPageType.NewPost);
        }

        public void UpdateModel(string threadId, int pageNumber)
        {
            var thread = Data.MainDataSource.Instance.FindThreadByID(threadId);
            if (thread != null)
            {
                this.Thread = thread;
                this._pages = null;
            }

            this.Execute(pageNumber);
        }

        public void UpdateModel(ThreadViewPageState state)
        {
            var thread = Data.MainDataSource.Instance.FindThreadByID(state.ThreadID);
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
            
            this.IsReady = true;
        }

        private void OnReadyToBind(ThreadViewPageState state)
        {
            var eventHandler = ReadyToBind;
            if (eventHandler != null)
                eventHandler(this, new Common.ThreadReadyToBindEventArgs(state));
        }

        internal static void SendRequestAsync(IThreadPostRequest request, Action<bool> notify)
        {
            ThreadPool.QueueUserWorkItem(state =>
                {
                    bool success = request.Send();
                    Deployment.Current.Dispatcher.BeginInvoke(() => notify(success));
                }, null);
        }

        protected override void OnError(Exception ex)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
            MessageBox.Show("Could not load the requested page. Please try again.", ":(", MessageBoxButton.OK);
            IsReady = false;
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess(object arg)
        {
            ThreadViewPageState state = arg as ThreadViewPageState;

            if (state != null)
            {
                // we know for sure it was successful
                IsReady = true;
                OnReadyToBind(state);
            }
        }

        protected override object DoWork(int parameter)
        {
            ThreadViewPageState state = null;
            ThreadPageMetadata page = LoadPageFromWeb(parameter);

            if (page == null)
                throw new NullReferenceException("Thread page must not be null.");

            else
            {
                var pageData = new Data.ThreadPageDataObject(page);

                // mine data about the thread from the page
                Thread.Title = page.ThreadTitle;
                Thread.PageCount = page.LastPage;
                
                // set the selected index to that of the loaded page
                SelectedIndex = page.PageNumber - 1;
                
                // create page state and save immeadiately so we don't have to load this again
                state = new ThreadViewPageState(Thread, pageData);

                try { ThreadViewPageState.Save(state); }
                catch (Exception) { }
                
                // setup page proxy and insert page at proper index
                SetPages(Thread.PageCount);
                Pages[SelectedIndex] = pageData;
            }

            return state;
        }

        private ThreadPageMetadata LoadPageFromWeb(int pageNumber)
        {
            ThreadPageMetadata page = null;

            if (pageNumber == (int)ThreadPageType.NewPost)
                page = Thread.Data.LoadNewPostPage();

            else if (pageNumber == (int)ThreadPageType.Last)
                page = Thread.Data.LoadLastPage();

            else
                page = Thread.Data.LoadPage(pageNumber);

            return page;
        }

        protected override bool PreCondition(int item)
        {
            // thread must not be null
            // number must not:
            // - represent new post or last page
            // - be nonzero otherwise

            // not gonna check for upper bound -- if that happens then there's a problem elsewhere.

            bool valid = this.Thread != null;

            valid = valid &&
                item == (int)ThreadPageType.NewPost ||
                item == (int)ThreadPageType.Last ||
                item >= 1;

            return valid;
        }
    }

    public class ThreadPageProxy : KollaSoft.KSVirtualizedList<Data.ThreadPageDataSource>
    {
        private const int MAX_CAPACITY = 20;
        private int _count;
        //private Queue<Data.ThreadPageDataSource> _cache;
        private Data.ThreadDataSource _source;
        private Dictionary<int, Data.ThreadPageDataSource> _cacheTable;

        public ThreadPageProxy(Data.ThreadDataSource source) : base()
        {
            this._source = source;
            this._count = source.PageCount;

            //this._cache = new Queue<Data.ThreadPageDataSource>(MAX_CAPACITY);
            this._cacheTable = new Dictionary<int, ThreadPageDataSource>(source.PageCount);
        }

        public override int IndexOf(Data.ThreadPageDataSource item)
        {
            int index = item.PageNumber - 1;
            return index;
        }

        protected override Data.ThreadPageDataSource GetItem(int index)
        {
            var pageNumber = index + 1;
            Data.ThreadPageDataSource page = null;

            // var page = this._cache.Where(p => p.PageNumber == pageNumber).FirstOrDefault();
            if (_cacheTable.ContainsKey(pageNumber))
                page = _cacheTable[pageNumber];

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
            if (_cacheTable.ContainsKey(page.PageNumber))
                _cacheTable[page.PageNumber] = page;
            else
                _cacheTable.Add(page.PageNumber, page);

            /*
            this._cache.Enqueue(page);
            if (this._cache.Count > MAX_CAPACITY)
                this._cache.Dequeue();
            */
        }

        protected override void SetItem(Data.ThreadPageDataSource item, int index)
        {
            item.PageNumber = index + 1;
            if (item is ThreadPageDataObject)
            {
                var proxy = new ThreadPageDataProxy(item as ThreadPageDataObject);
                UpdateCache(proxy);
            }
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
