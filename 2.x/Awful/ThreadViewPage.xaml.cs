using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Awful.Common;
using System.Runtime.Serialization;

namespace Awful
{
    public partial class ThreadViewPage : PhoneApplicationPage
    {
        public ThreadViewPage()
        {
            InitializeComponent();
            Loaded += ThreadViewPage_Loaded;
            viewmodel = new ViewModels.ThreadViewModel();
        }

        private ViewModels.ThreadViewModel viewmodel;
        private Controls.ThreadPagePresenter pagePresenter;
        private ThreadViewPageState state;

        private IApplicationBar PageNavBar
        {
            get { return this.Resources["DefaultAppBar"] as IApplicationBar; }
        }

        private IApplicationBar ReplyBar
        {
            get { return this.Resources["ReplyAppBar"] as IApplicationBar; }
        }

        private bool IsReplyViewActive
        {
            get { return this.ThreadReplyPanel.Visibility == System.Windows.Visibility.Visible; }
        }

        private bool IsPostListViewActive
        {
            get { return this.ThreadViewPostListPanel.Visibility == System.Windows.Visibility.Visible; }
        }

        public void GoToIndex(int index = -1)
        {
            // if index equals -1, load last read page
            if (index < 0)
            {
                int lastRead = this.viewmodel.Thread.Data.GetLastReadPage();
                lastRead = Math.Min(lastRead, this.viewmodel.Items.Count);
                index = lastRead - 1;
            }

            var item = this.viewmodel.Items[index];
            GoToPage(item);
        }

        private void GoToPage(Data.ThreadPageDataSource item)
        {
            if (item != this.viewmodel.SelectedItem)
            {
                this.viewmodel.SelectedItem = item;
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (this.IsPostListViewActive)
            {
                this.ClosePostList(null, null);
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (!e.IsNavigationInitiator)
                BuildStateFromIsoStore();
            else
                BuildStateFromNavigationQuery();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (!e.IsNavigationInitiator)
            {
                state.Save(this.viewmodel.SelectedItem);
            }

            else
                state = null;
        }

        private void BuildStateFromIsoStore()
        {
            state = ThreadViewPageState.Load();
            if (state == null)
            {
                BuildStateFromNavigationQuery();
            }

            else
            {
                bool refresh = viewmodel.Thread == null || viewmodel.Thread.ThreadID != state.ThreadID;
                refresh = refresh && (viewmodel.SelectedItem == null || viewmodel.SelectedItem.PageNumber != state.PageNumber);

                if (refresh)
                {
                    this.viewmodel.UpdateModel(state);
                    this.DataContext = viewmodel;
                    this.GoToIndex(state.PageNumber);
                }
            }
        }

        private void BuildStateFromNavigationQuery()
        {
            int currentPage = -1;
            string threadId = null;
            string forumId = null;

            // get page number from navigation query
            if (NavigationContext.QueryString.ContainsKey("ForumID"))
                forumId = NavigationContext.QueryString["ForumID"];

            if (NavigationContext.QueryString.ContainsKey("ThreadID"))
                threadId = NavigationContext.QueryString["ThreadID"];

            // get page number from navigation query
            if (NavigationContext.QueryString.ContainsKey("Page"))
            {
                int pageNumber;
                int.TryParse(NavigationContext.QueryString["Page"], out pageNumber);
                currentPage = pageNumber;
            }

            this.state = new ThreadViewPageState();
            state.ThreadID = threadId;
            state.ForumID = forumId;
            state.PageNumber = currentPage;

            this.viewmodel.UpdateModel(threadId, forumId, currentPage);
            this.DataContext = viewmodel;
            this.GoToIndex(currentPage);
        }

        void ThreadViewPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsReplyViewActive)
                this.ApplicationBar = ReplyBar;
            else
                this.ApplicationBar = PageNavBar;
        }

        private void ClosePostList(object sender, System.Windows.RoutedEventArgs e)
        {
        	// TODO: Add event handler implementation here.
            this.ThreadViewPostListPanel.Visibility = System.Windows.Visibility.Collapsed;
            if (this.ApplicationBar != null)
                this.ApplicationBar.IsVisible = true;
        }

        private void UpdateTextCount(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }
            
        private void HideThreadReplyPanel(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
            if (this.IsReplyViewActive)
            {
                this.ApplicationBar = this.PageNavBar;
                this.ThreadReplyPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ThreadViewPanel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ShowThreadReplyPanel(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
            if (!this.IsReplyViewActive)
            {
                this.ThreadViewPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ThreadReplyPanel.Visibility = System.Windows.Visibility.Visible;
                this.ApplicationBar = this.ReplyBar;
            }

        }

        private void ShowTagWindow(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void ShowSmiliesWindow(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void SendReply(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void ClearAllReplyText(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void TogglePageOrientationLock(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void ShowPostList(object sender, System.Windows.Input.GestureEventArgs e)
        {
        	// TODO: Add event handler implementation here.
            this.ThreadViewPostListPanel.Visibility = System.Windows.Visibility.Visible;
            if (this.ApplicationBar != null)
                this.ApplicationBar.IsVisible = false;
        }

        private void ToggleThreadBookmark(object sender, System.Windows.RoutedEventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void GoToLastPage(object sender, EventArgs e)
        {
            this.GoToIndex(this.viewmodel.Items.Count - 1);
        }

        private void GoToFirstPage(object sender, EventArgs e)
        {
            this.GoToIndex(0);
        }

        private void ScrollToPost(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var item = e.Item.AssociatedDataItem.Value as Data.ThreadPostSource;
            if (item != null)
                this.pagePresenter.ScrollToPost(item);

            this.ClosePostList(null, null);
        }

        private void ThreadPagePresenter_Loaded(object sender, RoutedEventArgs e)
        {
            this.pagePresenter = sender as Controls.ThreadPagePresenter;
        }
    }

    [DataContract]
    public class ThreadViewPageState
    {
        private const string SAVE_FILE = "ThreadPageViewState.xml";
        [DataMember]
        public string ForumID { get; set; }
        [DataMember] 
        public string ThreadID { get; set; }
        [DataMember] 
        public int PageNumber { get; set; }
        [DataMember]
        public string Html { get; set; }
        [DataMember]
        public List<Data.ThreadPostSource> Posts { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public int PageCount { get; set; }

        public void Save(Data.ThreadPageDataSource source)
        {
            var data = source.Data;

            PageCount = data.LastPage;
            Title = source.Title;
            ThreadID = source.ThreadID;
            PageNumber = source.PageNumber;
            Html = source.Html;
            Posts = source.Posts;
            bool saved = this.SaveToFile(SAVE_FILE);

            MessageBox.Show("state saved: " + saved);
        }

        public static ThreadViewPageState Load()
        {
            var state = CoreExtensions.LoadFromFile<ThreadViewPageState>(SAVE_FILE);
            return state;
        }

       
    }
}