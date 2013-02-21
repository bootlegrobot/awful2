using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Awful.Data;
using System.Collections.ObjectModel;
using KollaSoft;
using System.Windows.Input;

namespace Awful.ViewModels
{
    #region Helper Classes

    public delegate ThreadPageMetadata LoadPageDelegate(object state);
    
    public sealed class LoadPageCommandArgs
    {
        public LoadPageDelegate LoadPage { get; set; }
        public object State { get; set; }
    }

    public sealed class ThreadPageCache : Dictionary<string, Dictionary<int, ThreadPageMetadata>>
    {
        public void AddPage(ThreadPageMetadata page)
        {
            if (!this.ContainsKey(page.ThreadID))
                this.Add(page.ThreadID, new Dictionary<int, ThreadPageMetadata>());

            else if (!this[page.ThreadID].ContainsKey(page.PageNumber))
                this[page.ThreadID].Add(page.PageNumber, null);

            this[page.ThreadID][page.PageNumber] = page;
        }

        public ThreadPageMetadata GetPage(ThreadMetadata thread, int pageNumber)
        {
            ThreadPageMetadata page = null;
            string threadId = thread.ThreadID;

            if (this.ContainsKey(threadId) && this[threadId].ContainsKey(pageNumber))
                page = this[threadId][pageNumber];

            return page;
        }
    }

    public sealed class ThreadPageSlideViewItem : Common.BindableBase, 
        IEquatable<ThreadPageSlideViewItem>
    {
        public int Index { get; set; }

        public string Preview 
        { 
            get 
            { 
                return string.Format("Page {0}", Index + 1); 
            } 
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value, "Name"); }
        }

        public bool Equals(ThreadPageSlideViewItem other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            if (obj is ThreadPageSlideViewItem)
                return this.Equals((ThreadPageSlideViewItem)obj);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.Index.GetHashCode();
        }
    }

    public sealed class ThreadPageSlideViewList : KSVirtualizedList<ThreadPageSlideViewItem>
    {

        public override bool Contains(ThreadPageSlideViewItem item)
        {
            return this.Count > item.Index;
        }

        private int _count;

        public ThreadPageSlideViewList(int count)
        {
            // TODO: Complete member initialization
            this.Count = count;
        }
        public override int Count
        {
            get
            {
                return _count;
            }
            protected set
            {
                _count = value;
                OnPropertyChanged("Count");
            }
        }

        protected override ThreadPageSlideViewItem GetItem(int index)
        {
            return new ThreadPageSlideViewItem() { Index = index };
        }

        public override int IndexOf(ThreadPageSlideViewItem item)
        {
            return item.Index;
        }

        protected override void SetItem(ThreadPageSlideViewItem item, int index)
        {
            item.Index = index;
        }
    }

    #endregion

    public class ThreadPageSlideViewModel : Commands.BackgroundWorkerCommand<LoadPageCommandArgs>
    {
        public ThreadPageSlideViewModel() : base()
        {
            if (!PopulateForDesignTool())
            {
                this._items = new List<ThreadPageSlideViewItem>() { new ThreadPageSlideViewItem() };
            }
            
            this._firstPageCommand = new Common.ActionCommand(LoadFirstPageAction);
            this._lastPageCommand = new Common.ActionCommand(LoadLasPageAction);
            this._customPageCommand = new Common.ActionCommand(LoadPageNumberAction);
            Commands.ViewSAThreadCommand.ViewThread += OnPageFromLinkAvailable;
        }

        public enum ViewStates
        {
            New,
            Switching,
            Loading,
            Ready
        }

        public event EventHandler ViewStateChanged;

        private readonly ThreadPageCache _pageCache = new ThreadPageCache();

        #region Properties

        private IList<ThreadPageSlideViewItem> _items;
        public IList<ThreadPageSlideViewItem> Items
        {
            get
            {
                return _items; 
            }

            set { SetProperty(ref _items, value, "Items"); }
        }

        private ViewStates _currentState = ViewStates.New;
        public ViewStates CurrentState
        {
            get { return _currentState; }
            private set
            {
                this._currentState = value;
                if (ViewStateChanged != null)
                    ViewStateChanged(this, null);
            }
        }

        private string _info = string.Empty;
        public string Info
        {
            get 
            { 
                _info = string.Format("Page {0} of {1}", CurrentPage, TotalPages);
                return _info;
            }
        }

        private ThreadMetadata _currentThread = null;
        public ThreadMetadata CurrentThread
        {
            get { return _currentThread; }
            set { SetProperty(ref _currentThread, value, "CurrentThread"); }
        }

        private ThreadPageDataSource _currentThreadPage = null;
        public ThreadPageDataSource CurrentThreadPage
        {
            get { return _currentThreadPage; }
            set { SetProperty(ref _currentThreadPage, value, "CurrentThreadPage"); }
        }

        private int _currentPage = -1;
        public int CurrentPage
        {
            get { return _currentPage; }
            set 
            { 
                SetProperty(ref _currentPage, value, "CurrentPage");
                PrevPage = value - 1;
                NextPage = value + 1;
                OnPropertyChanged("Info");
            }
        }

        private int _prevPage = 0;
        public int PrevPage
        {
            get { return _prevPage; }
            private set { SetProperty(ref _prevPage, value, "PrevPage"); }
        }

        private int _nextPage = 0;
        public int NextPage
        {
            get { return _prevPage; }
            private set { SetProperty(ref _nextPage, value, "NextPage"); }
        }

        private int _totalPages = 0;
        public int TotalPages
        {
            get { return _totalPages; }
            set 
            { 
                SetProperty(ref _totalPages, value, "TotalPages");
                OnPropertyChanged("Info");
            }
        }

        private ThreadPageSlideViewItem _selectedItem = null;
        public ThreadPageSlideViewItem SelectedItem
        {
            get { return _selectedItem;  }
            set 
            { 
                SetProperty(ref _selectedItem, value, "SelectedItem");

                if (CurrentState == ViewStates.Ready)
                {
                    CurrentState = ViewStates.Switching;
                    LoadPageNumber(CurrentThread, value.Index + 1);
                }
            }
        }

        private readonly Commands.RateThreadCommand _ratingCommand = new Commands.RateThreadCommand();
        public ICommand RatingCommand
        {
            get { return _ratingCommand; }
        }

        private readonly Common.ActionCommand _firstPageCommand;
        public ICommand FirstPageCommand
        {
            get { return _firstPageCommand; }
        }

        private readonly Common.ActionCommand _lastPageCommand;
        public ICommand LastPageCommand
        {
            get { return _lastPageCommand; }
        }

        private readonly Common.ActionCommand _customPageCommand;
        public ICommand CustomPageCommand
        {
            get { return _customPageCommand; }
        }

        #endregion

        private void OnPageFromLinkAvailable(object sender, Common.SAThreadViewEventArgs args)
        {
            Uri pageUri = args.PageUri;
            LoadPageFromUri(pageUri);
        }

        private bool PopulateForDesignTool()
        {
            if (System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                this.CurrentThread = new ThreadMetadata()
                {
                    ThreadID = "-1",
                    Title = "Sample thread title that is really really long for some reason!"
                };

                this.CurrentThreadPage = ThreadPageDataObject.CreateSampleObject();
                this.CurrentPage = this.CurrentThreadPage.PageNumber;
                this.TotalPages = this.CurrentThreadPage.Data.LastPage;
                this.Status = "Loading...";
                this.IsRunning = true;
                this.Items = new ThreadPageSlideViewList(this.TotalPages);
                return true;
            }

            return false;
        }

        public void RefreshCurrentPage()
        {
            LoadPageCommandArgs args = new LoadPageCommandArgs();
            args.LoadPage = RefreshCurrentPage;
            args.State = CurrentThreadPage;
            Execute(args);
        }

        public void LoadPageNumber(ThreadMetadata thread, int pageNumber)
        {
            this.CurrentThread = thread;
            LoadPageCommandArgs args = new LoadPageCommandArgs();
            args.LoadPage = LoadPageNumber;
            args.State = pageNumber;
            Execute(args);
        }

        #region Command Actions

        private void LoadPageNumberAction(object state) { LoadPageNumber(CurrentThread, (int)state); }
        private void LoadFirstPageAction(object state) { LoadPageNumber(CurrentThread, 1); }
        private void LoadLasPageAction(object state) { LoadLastPost(CurrentThread); }

        #endregion

        public void LoadPageFromUri(Uri uri)
        {
            LoadPageCommandArgs args = new LoadPageCommandArgs();
            args.LoadPage = LoadPageFromUri;
            args.State = uri;
            Execute(args);
        }

        public void LoadFirstUnreadPost(ThreadMetadata thread)
        {
            LoadPageCommandArgs args = new LoadPageCommandArgs();
            args.LoadPage = LoadFirstUnreadPost;
            args.State = thread;
            Execute(args);
        }

        public void LoadLastPost(ThreadMetadata thread)
        {
            LoadPageCommandArgs args = new LoadPageCommandArgs();
            args.LoadPage = LoadLastPost;
            args.State = thread;
            Execute(args);
        }

        protected override void OnStateChanged()
        {
            if (IsRunning)
                this.CurrentState = ViewStates.Loading;
            
            base.OnStateChanged();
        }

        #region LoadPage delegates

        private ThreadPageMetadata LoadFirstUnreadPost(object state)
        {
            var thread = state as ThreadMetadata;
            if (thread == null)
                throw new Exception("Expected type ThreadMetadata from state.");

            UpdateStatus("Loading Thread...");
            return thread.FirstUnreadPost();
        }

        private ThreadPageMetadata LoadLastPost(object state)
        {
            var thread = state as ThreadMetadata;
            if (thread == null)
                throw new Exception("Expected type ThreadMetadata from state.");

            return thread.LastPage();
        }

        private ThreadPageMetadata LoadPageNumber(object state)
        {
            try
            {
                int pageNumber = (int)state;
                var page = _pageCache.GetPage(CurrentThread, pageNumber);

                if (page == null)
                {
                    UpdateStatus("Loading Page...");
                    page = CurrentThread.Page(pageNumber);
                }

                return page;
            }
            catch (InvalidCastException)
            {
                throw new Exception("Expected type int from state.");
            }
        }

        private ThreadPageMetadata LoadPageFromUri(object state)
        {
            var uri = state as Uri;
            if (uri == null)
                throw new Exception("Expected state to be of type Uri.");

            UpdateStatus("Loading Thread...");
            var page = MetadataExtensions.ThreadPageFromUri(uri);
            return page;
        }

        private ThreadPageMetadata RefreshCurrentPage(object state)
        {
            var page = state as ThreadPageMetadata;
            if (page == null)
                throw new Exception("Expected state to be of type ThreadPageMetadata.");

            UpdateStatus("Refreshing Page...");
            page = page.Refresh();            
            return page;
        }

        #endregion

        #region BackroundWorkerCommand Members

        protected override object DoWork(LoadPageCommandArgs value)
        {
            // load the page
            ThreadPageMetadata page = value.LoadPage(value.State);

            if (page == null)
                throw new Exception("There was an error loading the requested page.");

            else
            {
                this._pageCache.AddPage(page);
            }

            UpdateStatus("Rendering...");

            ThreadPageDataObject dataObject = new ThreadPageDataObject(page);
            this._currentThread = MetadataExtensions.FromPageMetadata(page);
            this._currentPage = page.PageNumber;
            this._currentThreadPage = dataObject;

            if (TotalPages != page.LastPage)
                _items = new ThreadPageSlideViewList(page.LastPage);

            return dataObject;
        }

        protected override void OnError(Exception ex)
        {
            Notification.ShowError(NotificationMethod.MessageBox, ex.Message, "View Page");
            this.CurrentState = ViewStates.Ready;
            this.Status = string.Empty;
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess(object arg)
        {
            // set current 
            ThreadPageDataSource page = arg as ThreadPageDataSource;
            OnPropertyChanged("CurrentThread");
            OnPropertyChanged("CurrentPage");
            OnPropertyChanged("CurrentThreadPage");
            Status = string.Empty;

            if (TotalPages != page.Data.LastPage)
            {
                // ignore the selected item change when we bind new items to view
                TotalPages = page.Data.LastPage;
                CurrentState = ViewStates.New;
                OnPropertyChanged("Items");
                SelectedItem = Items[CurrentPage - 1];
            }

            CurrentState = ViewStates.Ready;
        }

        protected override bool PreCondition(LoadPageCommandArgs item)
        {
           return item.LoadPage != null;
        }

        #endregion
    }
}
