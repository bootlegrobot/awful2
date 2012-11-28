using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            var forums = new ForumSectionViewModel(Data.MainDataSource.Instance.Forums);
            var bookmarks = new BookmarkSectionViewModel(Data.MainDataSource.Instance.Bookmarks);

            _items = new List<HomePageSection>()
            {
                new HomePageSection("forums") { Content = forums, Command = forums },
                new HomePageSection("bookmarks") { Content = bookmarks, Command = bookmarks }
            };

            return _items;
        }
    }

    public class HomePageSection : Data.SampleDataCommon, IDataLoadable
    {
        private static DataTemplate GlobalContentTemplate;

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

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ForumSectionViewModel)
                return ForumListTemplate;
            else if (item is BookmarkSectionViewModel)
                return BookmarkListTemplate;

            else
                return base.SelectTemplate(item, container);
        }
    }


    public class ForumSectionViewModel : ListViewModel<Data.ForumDataSource>, IDataLoadable
    {
        private Data.ForumCollection _forums;

        public ForumSectionViewModel(Data.ForumCollection forums) : base(forums) { _forums = forums; }

        protected override IEnumerable<Data.ForumDataSource> LoadDataWork()
        {
            Data.ForumCollection forums = null;
            forums = _forums.Refresh();
            return forums;
        }
    }

    public class BookmarkSectionViewModel : ListViewModel<Data.ThreadDataSource>, IDataLoadable
    {
        private Data.UserBookmarks _bookmarks;

        public BookmarkSectionViewModel(Data.UserBookmarks bookmarks) : base(bookmarks) { _bookmarks = bookmarks; }

        protected override IEnumerable<Data.ThreadDataSource> LoadDataWork()
        {
            IEnumerable<Data.ThreadDataSource> threads = null;
            threads = _bookmarks.LoadThreadsFromPage(0);
            return threads;
        }
    }
}
