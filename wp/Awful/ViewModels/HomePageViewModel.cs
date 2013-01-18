using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Awful.Common;

namespace Awful.ViewModels
{
    public class HomePageViewModel : Common.BindableBase
    {
        private string _welcome;
        public string Welcome
        {
            get
            {
                if (_welcome == null)
                    _welcome = FormatWelcomeMessage();

                return _welcome;
            }
        }

        private string FormatWelcomeMessage()
        {
            var user = Data.MainDataSource.Instance.CurrentUser;
            return string.Format("Welcome, {0}!", user.Metadata.Username);
        }

        private List<HomePageSection> _items;
        public List<HomePageSection> Items
        {
            get
            {
                if (_items == null)
                    _items = CreateItems();

                return _items;
            }
        }

        private List<HomePageSection> CreateItems()
        {
           
            var forums = new ForumSectionViewModel(Data.MainDataSource.Instance);
            var bookmarks = new BookmarkSectionViewModel(Data.MainDataSource.Instance);
            var pinned = new PinnedSectionViewModel(Data.MainDataSource.Instance, forums);
           
            _items = new List<HomePageSection>()
            {
                new HomePageSection("forums") { Content = forums, Command = forums },
                new HomePageSection("bookmarks") { Content = bookmarks, Command = bookmarks },
                new HomePageSection("pinned") { Content = pinned, Command = pinned }
            };

            return _items;
        }
    }

    public class HomePageSection : Data.CommonDataObject, IDataLoadable
    {
        public HomePageSection() : base() { }
        public HomePageSection(string title)
            : this()
        {
            this.Title = title;
        }

        private IDataLoadable _content;
        public IDataLoadable Content
        {
            get { return _content; }
            set { SetProperty(ref _content, value, "Content"); }
        }

        private ICommand _command;
        public ICommand Command
        {
            get { return _command; }
            set { SetProperty(ref _command, value, "Command"); }
        }

        public void LoadData()
        {
            Content.LoadData();
        }

        public bool IsDataLoaded { get { return Content.IsDataLoaded; } }

        public override string ToString()
        {
            return this.Title;
        }
    }

    public class HomePageSectionTemplateSelector : KollaSoft.KSDataTemplateSelector
    {
        public DataTemplate ForumListTemplate { get; set; }
        public DataTemplate BookmarkListTemplate { get; set; }
        public DataTemplate PinnedListTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ForumSectionViewModel)
                return ForumListTemplate;
            else if (item is BookmarkSectionViewModel)
                return BookmarkListTemplate;
            else if (item is PinnedSectionViewModel)
                return PinnedListTemplate;
            else
                return base.SelectTemplate(item, container);
        }
    }

    public class ForumSectionViewModel : ListViewModel<Data.ForumDataSource>, IDataLoadable
    {
        private Data.MainDataSource _source;
        private IEnumerable<Data.ForumDataSource> _forums;
        private const string EMPTY_STATUS = "Tap here to refresh the forums list.";

        private IEnumerable<Group<Data.ForumDataSource>> _groups;
        public IEnumerable<Group<Data.ForumDataSource>> Groups
        {
            get { return _groups; }
            set { SetProperty(ref _groups, value, "Groups"); }
        }

        public ForumSectionViewModel(Data.MainDataSource source) : base() 
        { 
            _source = source;
            if (!_source.Forums.IsNullOrEmpty())
            {
                _forums = source.Forums.Items;
            }

            UpdateStatus(EMPTY_STATUS);
        }

        protected override IEnumerable<Data.ForumDataSource> LoadDataInBackground()
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Loading forum body...");

            IEnumerable<Data.ForumDataSource> result = null;
            if (this._forums != null)
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Forum body found in iso storage!");
                
                // Removing this cache, so future refreshes will pull from forums
                UpdateStatus("Formatting...");
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Formatting Data...");
                result = Data.ForumCollection.GroupForums(_forums);
                this._forums = null;
            }

            else
            {
                var user = _source.CurrentUser;
                var forums = Refresh(user);
                this._source.Forums = new Data.ForumCollection(forums);
                UpdateStatus("Formatting...");
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Formatting Data...");
                result = Data.ForumCollection.GroupForums(forums);
            }

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Format Complete.");
            return result;
        }

        private IEnumerable<Data.ForumDataSource> Refresh(Data.UserDataSource user)
        {
            UpdateStatus("Loading forums...");
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Forum body missing. Pulling body from SA...");

            var forums = user.Metadata.LoadForums();
            IEnumerable<Data.ForumDataSource> result = null;
            if (forums != null)
            {
                result = forums.Select(forum => new Data.ForumDataSource(forum));
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Forum body request complete!");
            }
            return result;
        }

        protected override void UpdateItems(IEnumerable<Data.ForumDataSource> items)
        {
            base.UpdateItems(items);

            /* Experimenting with MSToolkit's LongListSelector...
            var group = items.GroupBy(forum => forum.Data.ForumGroup)
                .OrderBy(item => item.Key)
                .Select(item => new Group<Data.ForumDataSource>(item.Key.ToString(), item));

            this.Groups = group;
            */
        }

        protected override void OnError(Exception exception)
        {
            this.IsRunning = false;
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, exception);
            MessageBox.Show("Could not retrieve the forums from SA. Please try again.", ":(", MessageBoxButton.OK);
            UpdateStatus(EMPTY_STATUS);
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess()
        {
           // do nothing
        }
    }

    public class PinnedSectionViewModel : ListViewModel<Data.ForumDataSource>, IDataLoadable
    {
        private Data.MainDataSource _source;
        private ForumSectionViewModel _viewmodel;

        public PinnedSectionViewModel(Data.MainDataSource source,
            ForumSectionViewModel forumViewModel) : base() 
        {
            _source = source;
            _viewmodel = forumViewModel;
            _viewmodel.DataLoaded += new EventHandler(OnForumDataLoaded);

            PinnedItemsManager.PinnedStatusChanged += PinnedItemsManager_PinnedStatusChanged;
            this.UpdateStatus("Tap and hold your favorite forums to pin them here.");
        }

        private void OnForumDataLoaded(object sender, EventArgs e)
        {
            Items.Clear();

            var pinned = _source.Forums
                .Where(forum => _source.Pins.Contains(forum.ForumID));

            foreach (var pin in pinned)
            {
                pin.IsPinned = true;
                Items.Add(pin);
            }
        }

        private void PinnedItemsManager_PinnedStatusChanged(object sender, PinnedItemEventArgs e)
        {
            if (e.Item.IsPinned)
            {
                _source.Pins.Add(e.Item.UniqueId);
                Items.Add(e.Item as Data.ForumDataSource);
            }
            else
            {
                _source.Pins.Remove(e.Item.UniqueId);
                Items.Remove(e.Item as Data.ForumDataSource);
            }
        }

        protected override IEnumerable<Data.ForumDataSource> LoadDataInBackground()
        {
            return new List<Data.ForumDataSource>();
        }

        protected override void OnError(Exception exception)
        {
            this.IsRunning = false;
            MessageBox.Show("Could not retrieve pinned items.", ":(", MessageBoxButton.OK);
            UpdateStatus("Load failed.");
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess()
        {
           // do nothing
        }
    }

    public class BookmarkSectionViewModel : ListViewModel<Data.ThreadDataSource>, IDataLoadable
    {
        private Data.MainDataSource _source;
        private const string EMPTY_STATUS = "Tap here to refresh your bookmarks.";

        public BookmarkSectionViewModel(Data.MainDataSource source) : base(source.Bookmarks.Items) 
        { 
            _source = source;
            UpdateStatus(EMPTY_STATUS);
            UpdateLastUpdated(source.Bookmarks.LastUpdated);
        }

        protected override IEnumerable<Data.ThreadDataSource> LoadDataInBackground()
        {
            IEnumerable<Data.ThreadDataSource> threads = null;
            var user = _source.CurrentUser.Metadata;
            threads = Refresh(user);
            return threads;
        }

        protected override bool OnIsDataLoaded()
        {
            bool ready = base.OnIsDataLoaded();
            ready = ready && !this._source.AutoRefreshBookmarks;
            return ready;
        }

        private IEnumerable<Data.ThreadDataSource> Refresh(UserMetadata user)
        {
            UpdateStatus("Loading bookmarks...");
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Loading bookmarks from SA...");
            var bookmarks = user.LoadBookmarks();
            var data = new BookmarkMetadata();
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Load completed. Formatting...");
            UpdateStatus("Formatting...");
            var result = Data.ForumThreadCollection.CreateThreadSources(bookmarks, data);
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Format complete.");

            this._source.Bookmarks.LastUpdated = DateTime.Now;

            return result;
        }

        protected override void OnError(Exception exception)
        {
            this.IsRunning = false;
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, exception);
            MessageBox.Show("Could not retrieve bookmarks from SA. Please try again.", ":(", MessageBoxButton.OK);
            UpdateStatus(EMPTY_STATUS);
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess()
        {
            this._source.AutoRefreshBookmarks = false;
            UpdateLastUpdated(this._source.Bookmarks.LastUpdated);
        }
    }
}
