using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using Awful.Common;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Awful.Data
{
    public class ThreadDataModel : SampleDataItem, IEquatable<ThreadDataModel>
    {
        private string _threadID;
        private ThreadMetadata _metadata;

        public ThreadDataModel(ThreadMetadata metadata) : base()
        {
            this._metadata = metadata;
            this._threadID  = metadata.ThreadID;

            this.Title  = metadata.Title;
            this.Subtitle  = FormatSubtext1(metadata);
            this.Description  = FormatSubtext2(metadata);
            this.IsSticky = metadata.IsSticky;

            FormatRatingView(metadata);
            FormatImage(metadata.IconUri);
        }

        public void NavigateToThreadView(NavigationService nav, int pageNumber = 0)
        {
            PhoneApplicationService.Current.State[Constants.STATE_CURRENT_THREAD_KEY] = this.Metadata;

            if (pageNumber != 0)
                nav.Navigate(new Uri("/ThreadViewPage.xaml?Page=" + 1, UriKind.RelativeOrAbsolute));
            else
                nav.Navigate(new Uri("/ThreadViewPage.xaml", UriKind.RelativeOrAbsolute));

            this.HasBeenNavigatedTo = true;
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

        #region Properties

        public ThreadMetadata Metadata { get { return this._metadata; } }

        public string Tag { get; private set; }

        public int Rating
        {
            get;
            private set;
        }

        public string RatingColor { get; private set; }

        public bool ShowRating
        {
            get;
            private set;
        }

        private double _itemOpacity;
        public double ItemOpacity
        {
            get { return this._itemOpacity; }
            private set
            {
                this._itemOpacity = value;
                this.OnPropertyChanged("ItemOpacity");
            }
        }



        public bool IsSticky { get; private set; }
        public bool ShowPostCount { get; private set; }

        private bool _hasBeenNavigatedTo;
        private bool HasBeenNavigatedTo
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

        public bool Equals(ThreadDataModel other)
        {
            return this.Metadata.ThreadID.Equals(other.Metadata.ThreadID);
        }

        public override bool Equals(object obj)
        {
            if (obj is ThreadDataModel)
                return this.Equals(obj as ThreadDataModel);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.Metadata.ThreadID.GetHashCode();
        }
    }

    public class ThreadPagePostDataModel : Data.SampleDataItem
    {
        public ThreadPagePostDataModel(ThreadPostMetadata metadata)
        {
            this._metadata = metadata;
            // TODO: Initialize properties.

            this.Title = metadata.Author;
            this.Subtitle = FormatDateTime(metadata.PostDate);
            this.Index = "#" + metadata.PageIndex.ToString();
            this.IndexColor = metadata.IsNew ? App.POST_UNREAD_COLOR : App.POST_READ_COLOR;
        }

        private ThreadPostMetadata _metadata;
        public ThreadPostMetadata Data { get { return this._metadata; } }

        public object Index { get; private set; }
        public object IndexColor { get; private set; }

        private string FormatDateTime(DateTime date)
        {
            return date.ToShortTimeString();
        }
    }

    public class ThreadPageDataModel : SampleDataItem
    {
        public ThreadPageDataModel(ThreadMetadata data, int index)
        {
            this._parent = data;
            this.Index = index;
        }

        private ThreadPageMetadata _page;
        public ThreadPageMetadata Metadata { get { return this._page; } }

        private ThreadMetadata _parent;
        private BackgroundWorker _worker;

        private delegate ThreadPageItemTask Fetcher(ThreadMetadata metadata, ThreadPageItemTask task);
        private delegate void Updater(string html, IEnumerable<ThreadPagePostDataModel> posts);

        public event EventHandler ThreadPageUpdated;
        public event EventHandler ThreadPageUpdating;

        private class ThreadPageItemTask
        {
            public Fetcher Fetch { get; set; }
            public Updater Update { get; set; }
            public string Html { get; set; }
            public IEnumerable<ThreadPagePostDataModel> Posts { get; set; }
        }

        #region Properties

        #region Worker & Events

        /// <summary>
        /// Gets the background worker to support tasks.
        /// </summary>
        private BackgroundWorker Worker
        {
            get
            {
                // I think it's best to lazy load this object,
                // in the event that enumerating through hundreds of these
                // all at once won't kill the phone.
                if (this._worker == null)
                {
                    this._worker = new BackgroundWorker();
                    this._worker.WorkerReportsProgress = true;
                    this._worker.WorkerSupportsCancellation = true;
                    this._worker.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                    this._worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnWorkerWorkCompleted);
                    this._worker.DoWork += new DoWorkEventHandler(OnWorkerDoWork);
                }

                return this._worker;
            }
        }

        private void OnWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // TODO: do something here...
        }

        /// <summary>
        /// Callback when background work is finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWorkerWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // TODO: notify users if the work was cancelled
            if (e.Cancelled)
            {
                // notify!
            }
            else if (e.Error != null)
            {
                // print the error message on screen.
                // TODO: handle this better.
                MessageBox.Show(e.Error.Message);
                UpdatePage(string.Empty, null);
            }
            else
            {
                // update the UI
                var task = e.Result as ThreadPageItemTask;
                if (task != null)
                {
                    task.Update(task.Html, task.Posts);
                    OnThreadPageUpdated();
                }
            }
        }

        private void OnWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            var task = e.Argument as ThreadPageItemTask;
            if (task != null)
            {
                task = task.Fetch(this._parent, task);
                if (this.Worker.CancellationPending)
                    e.Cancel = true;
                else
                    e.Result = task;
            }
        }

        #endregion

        public int Index { get; private set; }
        public int PageNumber { get { return this.Index + 1; } }

        private ObservableCollection<ThreadPagePostDataModel> _posts;
        public ObservableCollection<ThreadPagePostDataModel> Items
        {
            get
            {
                if (this._posts == null)
                {
                    this._posts = new ObservableCollection<ThreadPagePostDataModel>();
                }

                return this._posts;
            }
        }

        #endregion

        public void QuoteAsync(int postIndex, Action<string> predicate)
        {
            ThreadPagePostDataModel selectedPost = Items[postIndex];
            ThreadTasks.QuoteAsync(selectedPost.Data, quote => { predicate(quote); });
        }

        public void MarkPostAsReadAsync(int postIndex, Action<bool> predicate)
        {
            var confirm = MessageBox.Show("Mark this post as last read?", ":o", MessageBoxButton.OKCancel);
            if (confirm == MessageBoxResult.Cancel)
                return;

            else
            {
                Data.ThreadPagePostDataModel selectedPost = Items[postIndex];
                ThreadTasks.MarkAsLastReadAsync(selectedPost.Data, marked => { predicate(marked); });
            }
        }

        private IEnumerable<ThreadPagePostDataModel> CreatePostItems(IEnumerable<ThreadPostMetadata> posts)
        {
            // set initial capacity to 40. pretty safe.
            var list = new List<ThreadPagePostDataModel>(40);
            foreach (var item in posts)
                list.Add(new ThreadPagePostDataModel(item));

            // return null if the list is empty.
            return list.IsNullOrEmpty() ? null : list;
        }

        private IEnumerable<ThreadPagePostDataModel> CreatePostItems(ThreadPageMetadata metadata)
        {
            return CreatePostItems(metadata.Posts);
        }

        public void LoadContentAsync()
        {
            if (!this.Worker.IsBusy)
            {
                var task = new ThreadPageItemTask()
                {
                    Update = UpdatePage,
                    Fetch = FetchDataFromCache
                };

                OnThreadPageUpdating();
                this.Worker.RunWorkerAsync(task);
            }
        }

        public void RefreshContentAsync()
        {
            if (!this.Worker.IsBusy)
            {
                var task = new ThreadPageItemTask()
                {
                    Update = UpdatePage,
                    Fetch = FetchDataFromWebOnly
                };

                OnThreadPageUpdating();
                this.Worker.RunWorkerAsync(task);
            }
        }

        private void UpdatePage(string html, IEnumerable<ThreadPagePostDataModel> posts)
        {
            // update UI with native (hopefully!) style HTML
            this.Content = html;
            this.Items.Clear();

            if (posts != null)
            {
                // update post jump list
                foreach (var item in posts)
                    this.Items.Add(item);
            }

            this.OnPropertyChanged("Items");
        }

        private void OnThreadPageUpdating()
        {
            if (ThreadPageUpdating != null)
                ThreadPageUpdating(this, EventArgs.Empty);
        }

        private void OnThreadPageUpdated()
        {
            if (ThreadPageUpdated != null)
                ThreadPageUpdated(this, null);
        }

        /// <summary>
        /// Fetches metadata from SA's servers.
        /// </summary>
        /// <param name="metadata">Thread metadata which all pages will stem from.</param>
        /// <param name="task">Thread list task performing work.</param>
        /// <param name="checkCache">Flag to check persistent storage for content first, if true.</param>
        /// <returns>An updated ThreadPageItemTask, if successful. If not, Html and Posts values will be null.</returns>
        private ThreadPageItemTask FetchData(ThreadMetadata metadata, ThreadPageItemTask task, bool checkCache)
        {
            // first fetch persisted native html, if available
            if (checkCache)
                task = FetchDataFromIsoStorage(metadata, task);

            if (task.Html == null || task.Posts == null)
            {
                ThreadPageMetadata pageData = metadata.FetchThreadPage(this.PageNumber);
                if (pageData != null)
                {
                    task.Html = pageData.ConvertToMetroStyle();
                }

                this._page = pageData;
                task.Posts = CreatePostItems(pageData);
            }

            return task;
        }

        private ThreadPageItemTask FetchDataFromWebOnly(ThreadMetadata metadata, ThreadPageItemTask task)
        {
            return FetchData(metadata, task, false);
        }

        private ThreadPageItemTask FetchDataFromCache(ThreadMetadata metadata, ThreadPageItemTask task)
        {
            return FetchData(metadata, task, true);
        }

        /// <summary>
        /// Fetches metadata from persistent storage.
        /// </summary>
        /// <param name="metadata">Thread metadata which all pages will stem from.</param>
        /// <param name="task">Thread list task performing work.</param>
        /// <returns>An updated ThreadPageItemTask, if successful. If not, Html and Posts values will be null.</returns>
        private ThreadPageItemTask FetchDataFromIsoStorage(ThreadMetadata metadata, ThreadPageItemTask task)
        {
            // implement me!!
            return task;
        }

    }
}
