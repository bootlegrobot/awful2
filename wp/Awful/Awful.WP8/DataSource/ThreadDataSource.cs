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
using Awful;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using System.IO;

namespace Awful.Data
{
    [DataContract]
    public class ThreadDataSource : CommonDataObject,
        IEquatable<ThreadDataSource>, IUpdateable<ThreadDataSource>
    {
        public ThreadDataSource(ThreadMetadata data)
            : base()
        {
            if (data != null)
                SetMetadata(data);
        }

        public ThreadDataSource() : this(null) { }

        public static ThreadDataSource FromThreadId(string threadId)
        {
            ThreadMetadata data = new ThreadMetadata() { ThreadID = threadId };
            ThreadDataSource thread = new ThreadDataSource();
            thread._data = data;
            thread.ThreadID = data.ThreadID;
            return thread;
        }

        #region Properties

        [IgnoreDataMember]
        public string ThreadID
        {
            get { return this.UniqueId; }
            set { this.UniqueId = value; }
        }

        [IgnoreDataMember]
        public bool HasCategory
        {
            get
            {
                bool value = Data != null &&
                    Data.ColorCategory != BookmarkColorCategory.Unknown;

                return value;
            }
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

        private bool _showImage;
        [IgnoreDataMember]
        public bool ShowImage
        {
            get { return _showImage; }
            private set { SetProperty(ref _showImage, value, "ShowImage"); }
        }

        #endregion

        public void SetMetadata(ThreadMetadata data)
        {
            this._data = data;
            this.Title = data.Title;
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
            /*
            string uri = string.Format("/ThreadViewPage.xaml?ForumID={0}&ThreadID={1}&Page={2}",
                this.ForumID,
                this.ThreadID,
                pageNumber);
            */

            StringBuilder uriBuilder = new StringBuilder("/ThreadDetails.xaml?");

            if (pageNumber == (int)ThreadPageType.NewPost)
            {
                // only go to unread pages if the thread in question has been
                // read by the user. otherwise, load first page.
                uriBuilder.AppendFormat(!this.Data.IsNew
                    ? "id={0}&nav=unread"
                    : "id={0}&nav=page&pagenumber=1",
                    this.ThreadID);
            }

            else if (pageNumber == (int)ThreadPageType.Last)
                uriBuilder.AppendFormat("id={0}&nav=last", this.ThreadID);
            else
                uriBuilder.AppendFormat("id={0}&nav=page&pagenumber={1}", this.ThreadID, pageNumber);

            this.HasBeenNavigatedTo = true;
            nav(new Uri(uriBuilder.ToString(), UriKind.RelativeOrAbsolute));
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

        private string GetThreadTagFilename(string iconUri)
        {
            // strip the '/'
            var tokens = iconUri.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string filename = tokens[tokens.Length - 1];
            tokens = filename.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            filename = tokens[0];
            return filename;
        }

        private void FormatImage(string iconUri)
        {
            if (iconUri != null)
            {
                string filename = GetThreadTagFilename(iconUri);
                this.Tag = filename;
                this.CheckCacheForImage(filename);
            }
        }

        private void CheckCacheForImage(string filename)
        {
            var localFilePath = string.Format("/ThreadTags/{0}.jpg", filename);
            var remoteFilePath = string.Format("{0}/{1}.png", Constants.THREAD_TAG_REMOTE_SOURCE, filename);
            var storage = IsolatedStorageFile.GetUserStoreForApplication();

            if (!storage.FileExists(localFilePath))
                SetImage(remoteFilePath);
            else
                SetImage(localFilePath);
        }

        protected override void OnImageOpened(BitmapImage bitmap)
        {
            WriteableBitmap wb = new WriteableBitmap(bitmap);
            var path = string.Format("/ThreadTags/{0}.jpg", this.Tag);
            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            using (var file = storage.CreateFile(path))
                wb.SaveJpeg(file, wb.PixelWidth, wb.PixelHeight, 0, 85);

            this.ShowImage = true;
        }

        protected override void OnImageFailed(BitmapImage image)
        {
            var path = string.Format("/ThreadTags/{0}.jpg", this.Tag);
            image = new BitmapImage();

            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    using (var file = storage.OpenFile(path, FileMode.Open))
                        image.SetSource(file);

                    this.Image = image;
                    this.ShowImage = true;
                }
                catch (Exception) { storage.DeleteFile(path); }
            }

        }

        public bool Equals(ThreadDataSource other)
        {
            return this.ThreadID == other.ThreadID;
        }

        public override bool Equals(object obj)
        {
            if (obj is ThreadDataSource)
                return this.Equals((ThreadDataSource)obj);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int hash = -1;
            if (string.IsNullOrEmpty(ThreadID))
                hash = base.GetHashCode();
            else
                hash = ThreadID.GetHashCode();

            return hash;
        }

        public void Update(ThreadDataSource updated)
        {
            this.SetMetadata(updated.Data);
        }
    }

    public sealed class SortThreadsByScore : IComparer<ThreadDataSource>
    {
        public int Compare(ThreadDataSource x, ThreadDataSource y)
        {
            return Score(x).CompareTo(Score(y));
        }

        /// <summary>
        /// Scores threads based on custom criteria. From lowest to highest:
        /// 
        /// 1) old thread new post count
        /// 2) old thread no new posts
        /// 3) new threads
        /// 
        /// </summary>
        /// <param name="x">The thread to score.</param>
        /// <returns>integer score for the thread.</returns>
        private int Score(ThreadDataSource x)
        {
            int score = 0;

            // new threads
            if (x.Data.IsNew)
                score = int.MaxValue;

            // threads with no new posts
            else if (x.Data.NewPostCount == -1)
                score = int.MaxValue - 1;

            else
                score = score + x.Data.NewPostCount;
            
            return score;
        }
    }
}