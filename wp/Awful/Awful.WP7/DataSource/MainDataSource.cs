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
using Awful.Helpers;
using Awful.WP7;

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
        }

        public MainDataSource()
        {
            LastUpdated = null;
            AwfulWebClient.LoginRequired += AwfulWebClient_LoginRequired;
            AwfulLoginClient.LoginSuccessful += AwfulLoginClient_LoginSuccessful;
        }

        private static void AwfulLoginClient_LoginSuccessful(object sender, LoginEventArgs e)
        {
            AwfulDebugger.AddLog(sender, AwfulDebugger.Level.Info, "Login was successful! Saving user cookies to iso storage...");
            var user = new UserDataSource(e.User);
            user.SaveToFile("user.xml");
            Instance.CurrentUser = user;
        }

        private static void AwfulWebClient_LoginRequired(object sender, LoginRequiredEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (Instance.CurrentUser.CanLogIn)
                {
                    AwfulDebugger.AddLog(sender, AwfulDebugger.Level.Info, "Sending credentials to the WebClient...");
                    e.SetUserAndProceed(Instance.CurrentUser.Metadata);
                }

                else
                {
                    // TODO: Create settings page that has a login interface -- no need to restart the app.
                    // Notify users at this point that they need to head to settings and login from there.

                    AwfulDebugger.AddLog(sender, AwfulDebugger.Level.Info, "User is unabled to login with these credentials.");
                    e.Cancel = true;
                }

                e.Signal.Set();
            });
        }

        #region properties

        [IgnoreDataMember]
        public bool AutoRefreshBookmarks { get; set; }

        [DataMember]
        public DateTime? LastUpdated { get; set; }

        [IgnoreDataMember]
        public bool IsActive { get { return this.LastUpdated.HasValue; } }

        private UserDataSource _currentUser;
        [IgnoreDataMember]
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
        [IgnoreDataMember]
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
        [IgnoreDataMember]
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

        private ThreadTable _threadTable;
        [IgnoreDataMember]
        public ThreadTable ThreadTable
        {
            get 
            {
                if (this._threadTable == null)
                    this._threadTable = new ThreadTable();

                return _threadTable; 
            }
            set { SetProperty(ref _threadTable, value, "ForumThreads"); }
        }

        private PinnedItemsCollection _pins;
        [IgnoreDataMember]
        public PinnedItemsCollection Pins
        {
            get
            {
                if (_pins == null)
                    _pins = new PinnedItemsCollection();
                return _pins;
            }

            set { SetProperty(ref _pins, value, "Pins"); }
        }

        #endregion

        public ForumDataSource FindForumByID(string forumId)
        {
            return Forums.Where(f => f.ForumID.Equals(forumId)).FirstOrDefault();
        }

        public ObservableSetWrapper<ThreadDataSource> FindForumThreadsByID(string forumId)
        {
            ObservableSetWrapper<ThreadDataSource> threads = null;

            if (forumId == Bookmarks.ForumID)
                threads = Bookmarks;

            else if (ThreadTable.ContainsKey(forumId))
                threads = ThreadTable[forumId];

            else
            {
                var forumThreads = new ForumThreadCollection(forumId);
                ThreadTable.Add(forumId, forumThreads);
                threads = forumThreads;
            }

            return threads;
        }

        public ThreadDataSource FindThreadByID(string threadId)
        {
            var threads = this.ThreadTable.Values
                .SelectMany(thread => thread)
                .Concat(this.Bookmarks);

            var selected = threads.Where(t => t.ThreadID.Equals(threadId)).FirstOrDefault();
            return selected;
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
    public class UserDataSource : CommonDataObject
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

        public bool CanLogIn { get { return this.Metadata != null && !this.Metadata.Cookies.IsNullOrEmpty(); } }
    }

    [DataContract]
    public class UserBookmarks : ObservableSetWrapper<ThreadDataSource>, ILastUpdated
    {
        public UserBookmarks() : base() { Items.CollectionChanged += UserBookmarks_CollectionChanged; }

        public UserBookmarks(IEnumerable<ThreadDataSource> collection) : base(collection) 
        { 
            Items.CollectionChanged += UserBookmarks_CollectionChanged; 
        }

        private void UserBookmarks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                    (item as ThreadDataSource).ForumID = this.ForumID;
            }
        }

        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public DateTime _lastUpdated;

        [IgnoreDataMember]
        public DateTime? LastUpdated
        {
            get
            {
                return _lastUpdated == default(DateTime) ?
                    new DateTime?() :
                    new DateTime?(_lastUpdated);
            }

            set { _lastUpdated = value.GetValueOrDefault(); }
        }

        [IgnoreDataMember]
        public string ForumID { get { return "-1"; } }
    }

    #region Forum related

    [DataContract]
    public class ForumDataSource : CommonDataObject, ICommand, IPinnable
    {
        public ForumDataSource() : base() { }
        public ForumDataSource(ForumMetadata data)
            : base()
        {
            SetMetadata(data);
        }

        #region properties

        private ForumLayout _layout;
        public ForumLayout Layout
        {
            get 
            {
                if (_layout == null)
                    _layout = ThemeManager.Instance.GetForumLayoutById(ForumID);

                return _layout; 
            }
            set { SetProperty(ref _layout, value, "Layout"); }
        }

        private bool _isPinned;
        [DataMember]
        public bool IsPinned
        {
            get { return this._isPinned; }
            set 
            { 
                SetProperty(ref _isPinned, value, "IsPinned"); 
            }
        }

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
        [IgnoreDataMember]
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

    [DataContract]
    public class ForumCollection : ObservableSetWrapper<ForumDataSource>
    {
        public ForumCollection() : base() { }
        public ForumCollection(IEnumerable<ForumDataSource> source) : base(source) { }

        internal static IEnumerable<ForumDataSource> GroupForums(IEnumerable<ForumDataSource> forums)
        {
            var list = new List<ForumDataSource>();
            var enumerator = forums.GetEnumerator();
            ForumDataSource parent = null;
            double weight = 0;

            while (enumerator.MoveNext())
            {
                ForumDataSource current = enumerator.Current;
                if (parent == null ||
                    parent.Data.LevelCount == current.Data.LevelCount)
                {
                    list.Add(current);
                    current.Subforums.Add(current);
                    parent = current;
                    weight = weight + 100;
                    current.Weight = weight;
                }

                else
                {
                    weight = weight + 1;
                    current.Weight = weight;
                    parent.Subforums.Add(current);
                }
               

            }
            return list;
        }
    }

    [DataContract]
    public class PinnedItemsCollection : ObservableSetWrapper<string>
    {
        public PinnedItemsCollection() : base() 
        {
            Items.CollectionChanged += PinnedItemsCollection_CollectionChanged;
        }

        public PinnedItemsCollection(IEnumerable<string> items)
            : base(items)
        {
            Items.CollectionChanged += PinnedItemsCollection_CollectionChanged;
        }

        private void PinnedItemsCollection_CollectionChanged(object sender, 
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

        }
    }

    [DataContract]
    public class ForumThreadCollection : ObservableSetWrapper<ThreadDataSource>, ILastUpdated
    {
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public DateTime _lastUpdated;

        [IgnoreDataMember]
        public DateTime? LastUpdated
        {
            get
            {
                return _lastUpdated == default(DateTime) ?
                    new DateTime?() :
                    new DateTime?(_lastUpdated);
            }

            set { _lastUpdated = value.GetValueOrDefault(); }
        }

        public static List<ThreadDataSource> CreateThreadSources(ForumPageMetadata page, ForumMetadata forum)
        {
            var threads = new List<ThreadDataSource>(page.Threads.Count);
            foreach (var thread in page.Threads)
            {
                var source = new ThreadDataSource(thread);
                source.ForumID = forum.ForumID;
                threads.Add(source);
            }
            return threads;
        }

        public ForumThreadCollection() : base() 
        {
            Items.HashCodeMethod = GetThreadHashCode;
            Items.CollectionChanged += ForumThreadCollection_CollectionChanged;
        }

        private int GetThreadHashCode(object obj)
        {
            ThreadDataSource thread = obj as ThreadDataSource;
            if (thread != null)
                return thread.ThreadID.GetHashCode();

            return obj.GetHashCode();
        }

        private void ForumThreadCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                    (item as ThreadDataSource).ForumID = this.ForumID;
            }
        }

        public ForumThreadCollection(string forumId)
            : this()
        {
            this.ForumID = forumId;
        }

        [DataMember]
        public virtual string ForumID { get; set; }

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
    public class ThreadTable : Dictionary<string, ForumThreadCollection> { }

    #endregion

    #region Thread related

    [DataContract]
    public class ThreadDataSource : CommonDataObject
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

        private int _rating;
        [DataMember]
        public int Rating
        {
            get { return _rating; }
            set { SetProperty(ref _rating, value, "Rating"); }
        }
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

        private double _itemOpacity = double.Parse(Constants.THREAD_DEFAULT_ITEM_OPACITY, System.Globalization.CultureInfo.InvariantCulture);
        [DataMember]
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
                    
                    double opacity = double.Parse(
                        value ? Constants.THREAD_VIEWED_ITEM_OPACITY : Constants.THREAD_DEFAULT_ITEM_OPACITY,
                        System.Globalization.CultureInfo.InvariantCulture);

                    this.ItemOpacity = opacity;
                }
            }
        }

        #endregion

        public void SetMetadata(ThreadMetadata data)
        {
            this._data = data;
            this.Title  = data.Title;
            this.IsSticky = data.IsSticky;
            this.ThreadID = data.ThreadID;
            this.PageCount = data.PageCount;
            this.Subtitle = FormatSubtext1(data);
            this.Description = FormatSubtext2(data);

            FormatRatingView(data);
            FormatImage(data.IconUri);
        }

        public void NavigateToThreadView(NavigationDelegate nav, int pageNumber = (int)ThreadPageType.NewPost)
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
            //ShowPostCount = !metadata.IsNew;
            ShowPostCount = true;

            if (metadata.IsNew)
            {
                return string.Format("{0} {1}",
                    PageCount,
                    PageCount == 1 ?
                    "page" :
                    "pages");
            }

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
                this.SetImage("/Assets/ThreadIcons/" + filename + ".png");
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
        event EventHandler ThreadPageFailed;
        void QuotePostAsync(int postIndex, Action<string> quote);
        void MarkPostAsReadAsync(int postIndex, Action<bool> success);
        void EditPostAsync(int postIndex, Action<IThreadPostRequest> request);
    }

    [DataContract]
    public class ThreadPageDataObject : CommonDataObject, ThreadPageDataSource
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
        public event EventHandler ThreadPageFailed;

        public virtual ThreadPageDataSource Refresh()
        {
            ThreadPageDataSource result = null;
            ThreadPageMetadata metadata = Data != null ? Data : new ThreadPageMetadata() { ThreadID = ThreadID, PageNumber = PageNumber };

            var page = metadata.Refresh();
            
            if (page != null)
                result = new ThreadPageDataObject(page);

            return result;
        }

        public void QuotePostAsync(int postIndex, Action<string> quote)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                var post = Posts[postIndex];
                var postQuote = post.Data.Quote();
                Deployment.Current.Dispatcher.BeginInvoke(() => { quote(postQuote); });
            }, null);
        }

        public void MarkPostAsReadAsync(int postIndex, Action<bool> success)
        {
            ThreadPool.QueueUserWorkItem((state) =>
                {
                    var post = Posts[postIndex];
                    var marked = post.Data.MarkAsRead();
                    Deployment.Current.Dispatcher.BeginInvoke(() => { success(marked); });
                });
        }

        public void EditPostAsync(int postIndex, Action<IThreadPostRequest> request)
        {
            ThreadPool.QueueUserWorkItem((state) =>
                {
                    var post = Posts[postIndex];
                    var postRequest = post.Data.BeginEdit();
                    Deployment.Current.Dispatcher.BeginInvoke(() => { request(postRequest); });
                });
        }
    }

    public sealed class ThreadPageDataProxy : Common.BindableBase, ThreadPageDataSource
    {
        private ThreadPageDataSource _page;
        public ThreadPageDataSource Page
        {
            get { return _page; }
            set { _page = value; }
        }

        public ThreadPageDataProxy(ThreadPageMetadata metadata)
        {
            _page = new ThreadPageDataObject(metadata);
        }

        public ThreadPageDataProxy(ThreadPageDataObject obj) { _page = obj; }

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
            ThreadPageDataSource page = null;
            try 
            { 
                page = (source as ThreadPageDataSource).Refresh();
                
                if (page != null)
                    this._page = page;

                OnThreadPageUpdated(this);

            }
            catch (Exception ex) 
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
                OnThreadPageUpdated(null);
            }
        }

        public event EventHandler ThreadPageUpdating;
        public event EventHandler ThreadPageUpdated;
        public event EventHandler ThreadPageFailed;

        private void OnThreadPageFailed(ThreadPageDataSource source)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (ThreadPageFailed != null)
                {
                    ThreadPageFailed(source, EventArgs.Empty);
                }
            });
        }

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

        public void QuotePostAsync(int postIndex, Action<string> quote)
        {
            _page.QuotePostAsync(postIndex, quote);
        }

        public void MarkPostAsReadAsync(int postIndex, Action<bool> success)
        {
            _page.MarkPostAsReadAsync(postIndex, success);
        }

        public void EditPostAsync(int postIndex, Action<IThreadPostRequest> request)
        {
            _page.EditPostAsync(postIndex, request);
        }
    }

    [DataContract]
    public class ThreadPostSource : CommonDataObject
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

    #endregion
}
