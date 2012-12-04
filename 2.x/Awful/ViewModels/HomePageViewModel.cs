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

namespace Awful.ViewModels
{
    public class HomePageViewModel : Common.BindableBase
    {
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
            var pinned = new PinnedSectionViewModel(Data.MainDataSource.Instance);
            var forums = new ForumSectionViewModel(Data.MainDataSource.Instance, pinned);
            var bookmarks = new BookmarkSectionViewModel(Data.MainDataSource.Instance);
           
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
        private PinnedSectionViewModel _pinned;

        public ForumSectionViewModel(Data.MainDataSource source, PinnedSectionViewModel pinned) : base() 
        { 
            _source = source;
            _pinned = pinned;
        }

        protected override void OnDataLoaded()
        {
            base.OnDataLoaded();
            
            if (_forums != null)
            {
                this._source.Forums = new Data.ForumCollection(_forums);
                this._pinned.UpdatePins(this._source.Forums);
            }
        }

        protected override IEnumerable<Data.ForumDataSource> LoadDataWork()
        {
            IEnumerable<Data.ForumDataSource> result = null;
            if (!_source.Forums.IsNullOrEmpty())
                _forums = _source.Forums;
            else
            {
                var user = _source.CurrentUser;
                _forums = Refresh(user);
            }

            result = Data.ForumCollection.FormatItems(_forums);
            return result;
        }

        private IEnumerable<Data.ForumDataSource> Refresh(Data.UserDataSource user)
        {
            var forums = user.Metadata.LoadForums();
            IEnumerable<Data.ForumDataSource> result = null;
            if (forums != null)
            {
                result = forums.Select(forum => new Data.ForumDataSource(forum));
            }
            return result;
        }
    }

    public class PinnedSectionViewModel : ListViewModel<Data.IPinnable>, IDataLoadable
    {
        private Data.MainDataSource _source;

        public PinnedSectionViewModel(Data.MainDataSource source) : base(source.Pins) 
        {
            _source = source;
            PinnedItemsManager.PinnedStatusChanged += PinnedItemsManager_PinnedStatusChanged;
        }

        private void PinnedItemsManager_PinnedStatusChanged(object sender, PinnedItemEventArgs e)
        {
            if (e.Item.IsPinned)
                _source.Pins.Add(e.Item);
            else
                _source.Pins.Remove(e.Item);
        }

        public void UpdatePins(IEnumerable<Data.ForumDataSource> pins)
        {
            foreach (var pin in pins)
            {
                _source.Pins.ReplaceItem(pin);
            }
        }

        protected override IEnumerable<Data.IPinnable> LoadDataWork()
        {
            return new List<Data.IPinnable>();
        }
    }

    public class BookmarkSectionViewModel : ListViewModel<Data.ThreadDataSource>, IDataLoadable
    {
        private Data.MainDataSource _source;

        public BookmarkSectionViewModel(Data.MainDataSource source) : base(source.Bookmarks) { _source = source; }

        protected override IEnumerable<Data.ThreadDataSource> LoadDataWork()
        {
            IEnumerable<Data.ThreadDataSource> threads = null;
            var user = _source.CurrentUser.Metadata;
            threads = Refresh(user);
            return threads;
        }

        private IEnumerable<Data.ThreadDataSource> Refresh(UserMetadata user)
        {
            var bookmarks = user.LoadBookmarks();
            var data = new BookmarkMetadata();
            return Data.ForumThreadCollection.CreateThreadSources(bookmarks, data);
        }
    }
}
