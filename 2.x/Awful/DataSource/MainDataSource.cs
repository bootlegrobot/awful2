using Awful.Common;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Awful.Data
{
    public delegate bool NavigationDelegate(Uri uri);

    [DataContract]
    public class MainDataSource : Common.BindableBase
    {
        public static MainDataSource Instance { get; private set; }

        static MainDataSource() 
        { 
            Instance = new MainDataSource(); 
            AwfulWebClient.LoginRequired += AwfulWebClient_LoginRequired;
            AwfulLoginClient.LoginSuccessful += AwfulLoginClient_LoginSuccessful;
        }

        public MainDataSource()
        {
            LastUpdated = null;
        }

        private static void AwfulLoginClient_LoginSuccessful(object sender, LoginEventArgs e)
        {
            Instance.CurrentUser = new UserDataSource(e.User);
        }

        private static void AwfulWebClient_LoginRequired(object sender, LoginRequiredEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (Instance.CurrentUser.IsLoggedIn)
                {
                    e.SetCookiesAndProcced(Instance.CurrentUser.Metadata.Cookies);
                }

                else
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("You must login first before continuing.");
                    e.Cancel = true;
                }

                e.Signal.Set();
            });
        }

        #region properties

        [DataMember]
        public DateTime? LastUpdated { get; set; }

        [IgnoreDataMember]
        public bool IsActive { get { return this.LastUpdated.HasValue; } }

        private UserDataSource _currentUser;
        [DataMember]
        public UserDataSource CurrentUser
        {
            get
            {
                if (_currentUser == null)
                    _currentUser = new UserDataSource();

                return _currentUser;
            }

            set { this.SetProperty(ref _currentUser, value, "CurrentUser"); }
        }
        
        private ForumCollection _forums;
        [DataMember]
        public ForumCollection Forums
        {
            get 
            {
                if (this._forums == null)
                    this._forums = new ForumCollection();

                return this._forums; 
            }
            set { SetProperty(ref _forums, value, "Forums"); }
        }

        private UserBookmarks _bookmarks;
        [DataMember]
        public UserBookmarks Bookmarks
        {
            get 
            {
                if (this._bookmarks == null)
                    this._bookmarks = new UserBookmarks();

                return this._bookmarks; 
            }

            set { SetProperty(ref _bookmarks, value, "Bookmarks"); }
        }

        private ForumThreadsByID _threadTable;
        [DataMember]
        public ForumThreadsByID ThreadTable
        {
            get 
            {
                if (this._threadTable == null)
                    this._threadTable = new ForumThreadsByID();

                return _threadTable; 
            }
            set { SetProperty(ref _threadTable, value, "ForumThreads"); }
        }

        #endregion

        public ForumDataSource FindForumByID(string forumId)
        {
            return Forums.Where(f => f.ForumID.Equals(forumId)).FirstOrDefault();
        }

        public ForumThreadCollection FindForumThreadsByID(string forumId)
        {
            ForumThreadCollection threads = null;

            if (forumId == Bookmarks.ForumID)
                threads = Bookmarks;

            else if (ThreadTable.ContainsKey(forumId))
                threads = ThreadTable[forumId];

            else
            {
                threads = new ForumThreadCollection(forumId);
                ThreadTable.Add(forumId, threads);
            }

            return threads;
        }

        public ThreadDataSource FindThreadByID(string threadId, string forumId)
        {
            var threads = FindForumThreadsByID(forumId);
            var thread = threads.Where(t => t.ThreadID.Equals(threadId)).FirstOrDefault();
            return thread;
        }

        public void SaveToIsoStorage(string filename)
        {
            this.SaveToFile(filename);
        }

        public static void LoadInstanceFromIsoStorage(string filename)
        {
            var source = CoreExtensions.LoadFromFile<MainDataSource>(filename);
            if (source != null)
                Instance = source;
        }
    }

    [DataContract]
    public class UserDataSource : SampleDataCommon
    {
        public UserDataSource() : base() { }

        public UserDataSource(UserMetadata metadata)
        {
            SetMetadata(metadata);
        }

        private UserMetadata _metadata;
        [DataMember]
        public UserMetadata Metadata
        {
            get { return this._metadata; }
            set { this.SetProperty(ref _metadata, value, "Metadata"); }
        }

        public void SetMetadata(UserMetadata metadata)
        {
            this._metadata = metadata;
            this.Title = metadata.Username;
        }

        public bool IsLoggedIn { get { return this.Metadata != null && !this.Metadata.Cookies.IsNullOrEmpty(); } }
    }

    [DataContract]
    public class ForumDataSource : SampleDataCommon, ICommand
    {
        public ForumDataSource() : base() { }
        public ForumDataSource(ForumMetadata data)
            : base()
        {
            SetMetadata(data);
        }

        #region properties

        [IgnoreDataMember]
        public string ForumID
        {
            get { return this.UniqueId; }
            set { this.UniqueId = value; }
        }

        private ForumMetadata _data;
        [DataMember]
        public ForumMetadata Data
        {
            get { return this._data; }
            set
            {
                SetMetadata(value);
            }
        }

        private List<ForumDataSource> _subforums;
        [DataMember]
        public List<ForumDataSource> Subforums
        {
            get
            {
                if (this._subforums == null)
                    this._subforums = new List<ForumDataSource>();

                return this._subforums;
            }

            set { SetProperty(ref _subforums, value, "Items"); }
        }

        [IgnoreDataMember]
        public List<ForumDataSource> Items { get { return this.Subforums; } }

        private bool _showItems;
        [DataMember]
        public bool ShowItems
        {
            get { return _showItems; }
            set { SetProperty(ref _showItems, value, "ShowItems"); }
        }

        [DataMember]
        public ForumDataSource Parent { get; set; }
        [IgnoreDataMember]
        public bool IsRoot { get { return this.Data.LevelCount < 3; } }
        [IgnoreDataMember]
        public bool HasSubforums { get { return !this._subforums.IsNullOrEmpty(); } }
        [IgnoreDataMember]
        public string ItemsDescription
        {
            get
            {
                return string.Format("{0} {1}",
                  Subforums.Count,
                  Subforums.Count == 1 ? "subforum" : "subforums");
            }
        }
        [IgnoreDataMember]
        public bool HasNoItems { get { return !HasSubforums; } }
        [DataMember]
        public bool IsPinned { get; set; }
        [DataMember]
        public double Weight { get; set; }
        [IgnoreDataMember]
        public ICommand Command { get { return this; } }
        [IgnoreDataMember]
        public bool Handled { get; set; }

        #endregion

        public void SetMetadata(ForumMetadata data)
        {
            this._data = data;
            this.UniqueId = data.ForumID;
            this.Title = data.ForumName;
            this.Subtitle = data.ForumName;
            this.OnPropertyChanged("Data");
        }

        public event EventHandler CanExecuteChanged;
        
        public bool CanExecute(object parameter)
        {
            return this.HasSubforums;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                this.ShowItems = !this.ShowItems;
                this.Handled = true;
            }
        }

        private static string AbbreviateName(string forumName)
        {
            var tokens = forumName.Split(' ');
            var stringbuilder = new StringBuilder();
            foreach (var token in tokens)
                stringbuilder.Append(token.ToUpper());

            return stringbuilder.ToString();
        }

        public void NavigateToForum(NavigationDelegate navi)
        {
             navi(new Uri("/ForumViewPage.xaml?" + ForumViewPage.FORUMID_QUERY + "=" + this.Data.ForumID, UriKind.RelativeOrAbsolute));
        }
    }

    [CollectionDataContract]
    public class ForumCollection : ObservableCollection<ForumDataSource>
    {
        public ForumCollection() : base() { }
        public ForumCollection(IEnumerable<ForumDataSource> source) : base(source) { }

        public ForumCollection Refresh()
        {
            ForumCollection result = null;
            var forums = ForumTasks.FetchAllForums();
            if (forums != null)
            {
                result = new ForumCollection(CreateItems(forums));
            }

            return result;
        }

        private IEnumerable<ForumDataSource> CreateItems(IEnumerable<ForumMetadata> forums)
        {
            if (forums == null)
                throw new ArgumentNullException("Cannot create model items without metadata.");

            List<ForumDataSource> result = new List<ForumDataSource>(forums.Count());
            var enumerator = forums.GetEnumerator();
            OrganizeItems(enumerator, result, null, null, 10);
            CollapseSubforums(result);
            return result;
        }

        private void CollapseSubforums(List<ForumDataSource> result)
        {
            foreach (var forum in result)
            {
                var children = forum.Subforums.SelectMany(f => f.Subforums)
                    .Concat(forum.Subforums);

                var subforums = children.ToList();
                forum.Subforums.Clear();
                forum.Subforums.AddRange(subforums);
            }
        }

        private ForumDataSource OrganizeItems(IEnumerator<ForumMetadata> enumerator,
            IList<ForumDataSource> list,
            ForumDataSource parent,
            int? level,
            int weightStep)
        {
            ForumDataSource ancestor = null;

            while (enumerator.MoveNext())
            {
                var currentLevel = enumerator.Current.LevelCount;
                var item = new ForumDataSource(enumerator.Current);
                level = level.GetValueOrDefault(currentLevel);

                if (parent == null)
                    parent = item;

                // set the weight of the item here. this will be used for sorting later.
                item.Weight = parent.Weight + weightStep;

                // if the current item is a sibling, add to the list.
                if (level.Value == currentLevel)
                {
                    list.Add(item);
                    parent = item;
                }

                // if the item is a descendant of the previous item.
                else if (currentLevel > level)
                {
                    // add the item as a child of the parent.
                    parent.Subforums.Add(item);
                    //parent.AddAsSubforum(item);

                    // set child's group to that of the parent.
                    item.Data.ForumGroup = parent.Data.ForumGroup;

                    // add future children to this item until we reach a sibling.
                    // the weight of this items should fall within an order of 1.
                    var sibling = OrganizeItems(enumerator, parent.Subforums, item, currentLevel, 1);
                    list.Add(sibling);
                    parent = sibling;
                }

                // if the item is an ancestor
                else
                {
                    ancestor = item;
                    break;
                }
            }

            return ancestor;
        }
    }

    [CollectionDataContract]
    public class ForumThreadCollection : ObservableCollection<ThreadDataSource>
    {
        public ForumThreadCollection() : base() { }

        public ForumThreadCollection(string forumId)
            : this()
        {
            this.ForumID = forumId;
        }

        [DataMember]
        public virtual string ForumID { get; set; }

        protected IEnumerable<ThreadDataSource> LoadThreadsFromPage(ForumMetadata forum, int pageNumber)
        {
            List<ThreadDataSource> threads = null;
            var forumPageData = forum.FetchForumPage(pageNumber);
            if (forumPageData != null)
            {
                threads = new List<ThreadDataSource>(forumPageData.Threads.Count);
                foreach (var thread in forumPageData.Threads)
                {
                    var source = new ThreadDataSource(thread);
                    source.ForumID = this.ForumID;
                    threads.Add(source);
                }
            }
            return threads;
        }

        public virtual IEnumerable<ThreadDataSource> LoadThreadsFromPage(int pageNumber)
        {
            var forum = new ForumMetadata() { ForumID = ForumID };
            return LoadThreadsFromPage(forum, pageNumber);
        }

        public virtual void Update(IEnumerable<ThreadDataSource> threads)
        {
            foreach (var thread in threads)
            {
                int index = this.IndexOf(thread);
                if (index != -1)
                {
                    this.RemoveAt(index);
                    this.Insert(index, thread);
                }
            }
        }
    }

    [CollectionDataContract(
        ItemName    = "entry", 
        KeyName     = "forumId", 
        ValueName   = "threads"
        )]
    public class ForumThreadsByID : Dictionary<string, ForumThreadCollection> { }

    [CollectionDataContract]
    public class UserBookmarks : ForumThreadCollection
    {
        public UserBookmarks() : base() { ForumID = "-1"; }

        public override IEnumerable<ThreadDataSource> LoadThreadsFromPage(int pageNumber)
        {
            var bookmarks = new BookmarkMetadata();
            return LoadThreadsFromPage(bookmarks, 0);
        }
    }

    [DataContract]
    public class ThreadDataSource : SampleDataCommon
    {
        public ThreadDataSource(ThreadMetadata data)
            : base()
        {
            if (data != null)
                SetMetadata(data);
        }

        public ThreadDataSource() : this(null) { }

        #region Properties

        [IgnoreDataMember]
        public string ThreadID
        {
            get { return this.UniqueId; }
            set { this.UniqueId = value; }
        }

        private string _forumID;
        [DataMember]
        public string ForumID
        {
            get { return this._forumID; }
            set { SetProperty(ref _forumID, value, "ForumID"); }
        }

        private ThreadMetadata _data;
        [DataMember]
        public ThreadMetadata Data
        {
            get { return this._data; }
            set { SetMetadata(value); }
        }

        [DataMember]
        public string Tag { get; set; }
        [DataMember]
        public int Rating { get; set; }
        [DataMember]
        public string RatingColor { get; set; }
        [DataMember]
        public bool IsSticky { get; set; }
        [DataMember]
        public bool ShowPostCount { get; set; }
        
        private int _pageCount;
        [DataMember]
        public int PageCount
        {
            get { return this._pageCount; }
            set { SetProperty(ref _pageCount, value, "PageCount"); }
        }

        [DataMember]
        public bool ShowRating
        {
            get;
            set;
        }

        private double _itemOpacity;
        [IgnoreDataMember]
        public double ItemOpacity
        {
            get { return this._itemOpacity; }
            set
            {
                this._itemOpacity = value;
                this.OnPropertyChanged("ItemOpacity");
            }
        }

        private bool _hasBeenNavigatedTo;
        [DataMember]
        public bool HasBeenNavigatedTo
        {
            get { return this._hasBeenNavigatedTo; }
            set
            {
                if (this._hasBeenNavigatedTo != value)
                {
                    this._hasBeenNavigatedTo = value;
                    if (value)
                        this.ItemOpacity = Convert.ToDouble(Constants.THREAD_VIEWED_ITEM_OPACITY);
                    else
                        this.ItemOpacity = Convert.ToDouble(Constants.THREAD_DEFAULT_ITEM_OPACITY);
                }
            }
        }

        #endregion

        public void SetMetadata(ThreadMetadata data)
        {
            this._data = data;
            this.Title  = data.Title;
            this.Subtitle  = FormatSubtext1(data);
            this.Description  = FormatSubtext2(data);
            this.IsSticky = data.IsSticky;
            this.ThreadID = data.ThreadID;
            this.PageCount = data.PageCount;

            FormatRatingView(data);
            FormatImage(data.IconUri);
        }

        public void NavigateToThreadView(NavigationDelegate nav, int pageNumber = 0)
        {
            string uri = string.Format("/ThreadViewPage.xaml?ForumID={0}&ThreadID={1}&Page={2}",
                this.ForumID,
                this.ThreadID,
                pageNumber);

            this.HasBeenNavigatedTo = true;
            nav(new Uri(uri, UriKind.RelativeOrAbsolute));
        }

        private void FormatRatingView(ThreadMetadata metadata)
        {
            this.Rating = metadata.Rating;
            this.ShowRating = this.Rating != 0;

            switch (this.Rating)
            {
                // unrated case
                case 0:
                    this.ShowRating = false;
                    break;

                case 1:
                    this.RatingColor = App.THREAD_RATING_COLOR_1;
                    break;

                case 2:
                    this.RatingColor = App.THREAD_RATING_COLOR_2;
                    break;

                case 3:
                    this.RatingColor = App.THREAD_RATING_COLOR_3;
                    break;

                case 4:
                    this.RatingColor = App.THREAD_RATING_COLOR_4;
                    break;

                case 5:
                    this.RatingColor = App.THREAD_RATING_COLOR_5;
                    break;
            }
        }

        private string FormatSubtext2(ThreadMetadata metadata)
        {
            ShowPostCount = !metadata.IsNew;

            if (metadata.IsNew)
                return string.Empty;

            else if (metadata.NewPostCount == ThreadMetadata.NO_UNREAD_POSTS_POSTCOUNT)
                return "no new posts";
            
            else if (metadata.NewPostCount == 1)
                return "1 new post";
            
            else
                return string.Format("{0} new posts", metadata.NewPostCount);
        }

        private string FormatSubtext1(ThreadMetadata metadata)
        {
            return string.Format("by {0}", metadata.Author);
        }

        private void FormatImage(string iconUri)
        {
            if (iconUri != null)
            {
                // strip the '/'
                var tokens = iconUri.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string filename = tokens[tokens.Length - 1];
                tokens = filename.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                filename = tokens[0];
                this.Tag = filename;
                this.SetImage("Assets/ThreadIcon/" + filename + ".png");
            }
        }
    }

    public interface ThreadPageDataSource
    {
        string Title { get; set; }
        string ThreadID { get; set; }
        int PageNumber { get; set; }
        string Html { get; set; }
        ThreadPageMetadata Data { get; }
        List<ThreadPostSource> Posts { get; set; }
        IEnumerable<ThreadPostSource> Items { get; }
        ThreadPageDataSource Refresh();
        event EventHandler ThreadPageUpdating;
        event EventHandler ThreadPageUpdated;
    }

    [DataContract]
    public class ThreadPageDataObject : SampleDataCommon, ThreadPageDataSource
    {
        public ThreadPageDataObject(ThreadPageMetadata data)
            : base()
        {
            if (data != null)
                SetMetadata(data);
        }

        public ThreadPageDataObject() : this(null) { }

        #region properties

        private bool _isRunning;
        [IgnoreDataMember]
        public bool IsRunning
        {
            get { return _isRunning; }
            set { SetProperty(ref _isRunning, value, "IsRunning"); }
        }

        private ThreadPageMetadata _data;
        [DataMember]
        public ThreadPageMetadata Data
        {
            get { return this._data; }
            set { SetMetadata(value); }
        }

        [DataMember]
        public string ThreadID
        {
            get { return this.UniqueId; }
            set { this.UniqueId = value; }
        }

        [DataMember] public int PageNumber { get; set; }
        [DataMember] public string Html { get; set; }

        private List<ThreadPostSource> _posts;
        [DataMember]
        public List<ThreadPostSource> Posts
        {
            get 
            {
                if (_posts == null)
                    _posts = new List<ThreadPostSource>(40);

                return this._posts; 
            }
            set 
            { 
                SetProperty(ref _posts, value, "Posts");
                OnPropertyChanged("Items");
            }
        }

        [IgnoreDataMember]
        public IEnumerable<ThreadPostSource> Items
        {
            get { return this.Posts; }
        }

        #endregion

        public void SetMetadata(ThreadPageMetadata data)
        {
            this._data = data;
            this.ThreadID = data.ThreadID;
            this.PageNumber = data.PageNumber;

            if (!data.Posts.IsNullOrEmpty())
            {
                foreach (var post in data.Posts)
                    this.Posts.Add(new ThreadPostSource(post));

                this.Html = MetroStyler.Metrofy(data.Posts);
            }

            OnPropertyChanged("Data");
            OnPropertyChanged("Posts");
            OnPropertyChanged("Items");
        }

        public event EventHandler ThreadPageUpdating;
        public event EventHandler ThreadPageUpdated;

       

        public virtual ThreadPageDataSource Refresh()
        {
            ThreadPageDataSource result = null;
            ThreadPageMetadata metadata = Data != null ? Data : new ThreadPageMetadata() { ThreadID = ThreadID, PageNumber = PageNumber };

            var page = metadata.FetchThreadPage();
            
            if (page != null)
                result = new ThreadPageDataObject(page);

            return result;
        }
    }

    public sealed class ThreadPageDataProxy : Common.BindableBase, ThreadPageDataSource
    {
        private ThreadPageDataSource _page;

        public ThreadPageDataProxy(ThreadPageMetadata metadata)
        {
            _page = new ThreadPageDataObject(metadata);
        }

        public ThreadPageMetadata Data
        {
            get { return this._page.Data; }
        }

        public string Title
        {
            get
            {
                return _page.Title;
            }
            set
            {
                _page.Title = value;
                OnPropertyChanged("Title");
            }
        }

        public string ThreadID
        {
            get
            {
                return _page.ThreadID;
            }
            set
            {
                _page.ThreadID = value;
                OnPropertyChanged("ThreadID");
            }
        }

        public int PageNumber
        {
            get
            {
                return _page.PageNumber;
            }
            set
            {
                _page.PageNumber = value;
                OnPropertyChanged("PageNumber");
            }
        }

        public string Html
        {
            get
            {
                return _page.Html;
            }
            set
            {
                _page.Html = value;
                OnPropertyChanged("Html");
            }
        }

        public List<ThreadPostSource> Posts
        {
            get
            {
                return _page.Posts;
            }
            set
            {
                _page.Posts = value;
                OnPropertyChanged("Posts");
                OnPropertyChanged("Items");
            }
        }

        public IEnumerable<ThreadPostSource> Items
        {
            get { return _page.Items; }
        }

        public ThreadPageDataSource Refresh()
        {
            OnThreadPageUpdating(this);
            ThreadPool.QueueUserWorkItem(LoadPage, _page);
            return this;
        }

        private void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged("Title");
            OnPropertyChanged("Html");
            OnPropertyChanged("Posts");
            OnPropertyChanged("Items");
        }

        private void LoadPage(object source)
        {
            var page = (source as ThreadPageDataSource).Refresh();
            if (page != null)
                this._page = page;
            OnThreadPageUpdated(this);
        }

        public event EventHandler ThreadPageUpdating;
        public event EventHandler ThreadPageUpdated;

        private void OnThreadPageUpdating(ThreadPageDataSource source)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (ThreadPageUpdating != null)
                {
                    ThreadPageUpdating(source, EventArgs.Empty);
                }
            });
        }

        private void OnThreadPageUpdated(ThreadPageDataSource source)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (ThreadPageUpdated != null)
                {
                    ThreadPageUpdated(source, EventArgs.Empty);
                    NotifyAllPropertiesChanged();
                }
            });
        }
    }

    [DataContract]
    public class ThreadPostSource : SampleDataCommon
    {
        public ThreadPostSource(ThreadPostMetadata data)
            : base()
        {
            if (data != null)
                this.SetMetadata(data);
        }

        public ThreadPostSource() : this(null) { }

        private ThreadPostMetadata _data;
        [DataMember]
        public ThreadPostMetadata Data
        {
            get { return _data; }
            set { SetMetadata(value); }
        }

        [DataMember]
        public string Index { get; set; }
        [DataMember]
        public string IndexForeground { get; set; }
        [DataMember]
        public bool IsNew { get; set; }
        [DataMember]
        public string TitleForeground { get; set; }

        private string FormatDateTime(DateTime date)
        {
            return date.ToShortTimeString();
        }

        public void SetMetadata(ThreadPostMetadata data)
        {
            this._data = data;
            this.Title = data.Author;
            this.Subtitle = FormatDateTime(data.PostDate);
            this.Index = "#" + data.PageIndex.ToString();
            this.IsNew = data.IsNew;
            this.IndexForeground = data.IsNew ? App.POST_UNREAD_COLOR : App.POST_READ_COLOR;
            OnPropertyChanged("Data");
        }
    }
}
