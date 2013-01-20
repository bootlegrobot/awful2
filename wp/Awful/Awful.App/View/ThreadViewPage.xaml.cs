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
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace Awful
{
    public partial class ThreadViewPage : PhoneApplicationPage
    {
        public ThreadViewPage()
        {
            InitializeComponent();
            viewmodel = new ViewModels.ThreadViewModel();
            viewmodel.ReadyToBind += OnViewModelReadyToBind;
            viewmodel.UpdateFailed += OnViewModelUpdateFailed;
            SetThreadNavCommands(viewmodel);
            Commands.EditPostCommand.EditRequested += new ThreadPostRequestEventHandler(OpenEditWindow);
            Loaded += ThreadViewPage_Loaded;
        }

        void OnViewModelUpdateFailed(object sender, EventArgs e)
        {
            ThreadInitializationPane.Visibility = Visibility.Collapsed;
        }

        private ViewModels.ThreadViewModel viewmodel;
        public ViewModels.ThreadViewModel Viewmodel { get { return viewmodel; } }

        private ViewModels.RatingViewModel RatingViewModel
        {
            get { return this.Resources["ratingViewModel"] as ViewModels.RatingViewModel; }
        }

        public string SavedDraft { get; set; }

        private Controls.ThreadPagePresenter pagePresenter;
        private ThreadViewPageState state;
        private bool _pageLocked = true;
        private PageOrientation _currentOrientation;

        private IThreadPostRequest _currentRequest;
        private IThreadPostRequest CurrentRequest
        {
            get
            {
                if (_currentRequest == null)
                    _currentRequest = viewmodel.Thread.Data.BeginReply();

                return _currentRequest;
            }
        }

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
		
		private bool _isFullscreenActive = false;
		private bool IsFullscreenActive
		{
			get { return this._isFullscreenActive; }
			set
			{
				_isFullscreenActive = value;
				if (value)
				{
                    this.PostJumpButtonPanel.Opacity = 0.3;
					this.TitlePanel.Visibility = System.Windows.Visibility.Collapsed;
					//this.TitleText.Visibility = System.Windows.Visibility.Collapsed;
				}
				else
				{
                    this.PostJumpButtonPanel.Opacity = 1.0;
					this.TitlePanel.Visibility = System.Windows.Visibility.Visible;
					//this.TitleText.Visibility = System.Windows.Visibility.Visible;
				}
			}
		}

        private void SetThreadNavCommands(ViewModels.ThreadViewModel viewmodel)
        {
            viewmodel.FirstPageCommand = new ActionCommand((state) =>
            {
                GoToFirstPage(null, null);
                this.ModalWindow.IsOpen = false;
            });

            viewmodel.LastPageCommand = new ActionCommand((state) =>
            {
                GoToLastPage(null, null);
                this.ModalWindow.IsOpen = false;
            });

            viewmodel.CustomPageCommand = new ActionCommand((state) =>
            {
                int page = -1;
                if (!int.TryParse(state.ToString(), out page))
                    GoToIndex(0);
                else
                    GoToIndex(page - 1);
                this.ModalWindow.IsOpen = false;
            });
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if (this._pageLocked)
            {
                var portrait = this._currentOrientation.IsPortrait();
                this.SupportedOrientations = portrait ?
                    SupportedPageOrientation.Portrait :
                    SupportedPageOrientation.Landscape;
            }

            else
            {
                this.SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
                this._currentOrientation = e.Orientation;
                base.OnOrientationChanged(e);
            }
        }

        public void GoToIndex(int index = -1)
        {
            var pageProvider = this.ThreadViewPagination.PageProvider;
            if (pageProvider != null)
                pageProvider.CurrentIndex = index;
            else
            {
                var item = this.viewmodel.Items[index];
                GoToPage(item);
            }
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

            else if (this.IsReplyViewActive)
            {
                this.HideThreadReplyPanel(null, null);
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!e.IsNavigationInitiator)
            {
                // reset the rating viewmodel -- make sure we have no lingering loading bars
                RatingViewModel.Reset();
                // load state from iso store
                BuildStateFromIsoStore();
            }
            else
                BuildStateFromNavigationQuery();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (!e.IsNavigationInitiator)
            {
                state.Save(this);
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

        private void OnViewModelReadyToBind(object sender, Common.ThreadReadyToBindEventArgs args)
        {
            ThreadInitializationPane.Visibility = Visibility.Collapsed;
            this.state = args.State;
            this.DataContext = viewmodel;
            this.GoToIndex(args.State.PageNumber - 1);
        }

        private void BuildStateFromNavigationQuery()
        {
            ThreadInitializationPane.Visibility = Visibility.Visible;

            int pageNumber = int.MinValue;
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
                int.TryParse(NavigationContext.QueryString["Page"], out pageNumber);
            }

            this.viewmodel.UpdateModel(threadId, pageNumber);
        }

        void ThreadViewPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsReplyViewActive)
                this.ApplicationBar = ReplyBar;
            else
                this.ApplicationBar = PageNavBar;

            this._currentOrientation = this.Orientation;

            if (!this.IsFullscreenActive)
                this.IsFullscreenActive = App.Model.DefaultThreadView == AppDataModel.ThreadViewMode.Fullscreen;
        }

        private void OpenEditWindow(object sender, ThreadPostRequestEventArgs e)
        {
            var request = e.Request;
            if (request != null)
            {
                this.ThreadReplyTextBox.Text = request.Content;
                this._currentRequest = request;
                this.ShowThreadReplyPanel(null, null);
            }
        }

        private void ClosePostList(object sender, System.Windows.RoutedEventArgs e)
        {
        	// TODO: Add event handler implementation here.
            this.ThreadViewPostListPanel.Visibility = System.Windows.Visibility.Collapsed;
            if (this.ApplicationBar != null)
                this.ApplicationBar.IsVisible = true;
        }
       
        private void HideThreadReplyPanel(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
            if (this.IsReplyViewActive)
            {
                this.ApplicationBar = this.PageNavBar;
                this.ThreadReplyPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ThreadViewPanel.Visibility = System.Windows.Visibility.Visible;
                this.PostJumpButtonPanel.Visibility = System.Windows.Visibility.Visible;
                this.IsFullscreenActive = this.IsFullscreenActive;
            }
        }

        private void ShowThreadReplyPanel(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
            if (!this.IsReplyViewActive)
            {
                this.ThreadViewPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ThreadReplyPanel.Visibility = System.Windows.Visibility.Visible;
                this.PostJumpButtonPanel.Visibility = System.Windows.Visibility.Collapsed;

                // cancel edit option
                var menu = this.ReplyBar.MenuItems[2] as IApplicationBarMenuItem;
                if (CurrentRequest.RequestType == PostRequestType.Edit)
                    menu.IsEnabled = false;
                else
                    menu.IsEnabled = true;

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
            bool sendEdit = this.CurrentRequest.RequestType == PostRequestType.Edit;
            string confirmMessage = null;
            
            if (sendEdit)
            {
                confirmMessage = "edit post?";
            }

            else
                confirmMessage = "send reply?";

            var response = MessageBox.Show(confirmMessage, ":o", MessageBoxButton.OKCancel);
            if (response == MessageBoxResult.OK)
            {
                this.CurrentRequest.Content = this.ThreadReplyTextBox.Text.Trim();
                SendReplyOrEdit(this.CurrentRequest);
            }

            CancelEditRequest(null, null);
        }

        private void SendReplyOrEdit(IThreadPostRequest request)
        {
            if (request != null)
                ViewModels.ThreadViewModel.SendRequestAsync(request, success =>
                    NotifyUserOfResult(success));
        }

        private void NotifyUserOfResult(bool success)
        {
            string message = success ? "request successful!" : "request failed.";
            string caption = success ? ":)" : ":(";
            MessageBox.Show(message, caption, MessageBoxButton.OK);
        }

        private void ClearAllReplyText(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
            this.ThreadReplyTextBox.Text = string.Empty;
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
		
		private void ShowRatingControl(object sender, System.EventArgs e)
        {
            // Ensure Page nav is hidden
            this.ThreadNavControl.Visibility = System.Windows.Visibility.Collapsed;
            // Ensure Rating control is visible
            this.ThreadRatingControl.Visibility = System.Windows.Visibility.Visible;
            // Set viewmodel thread property to current thread
            this.RatingViewModel.Thread = this.viewmodel.Thread;
            // Set viewmodel post action to closing the modal window
            this.RatingViewModel.SetPostRatingAction(success => { this.ModalWindow.IsOpen = false; });

            // Show modal window
            this.ModalWindow.IsOpen = true;
        }

        private void GoToLastPage(object sender, EventArgs e)
        {
            this.GoToIndex(this.viewmodel.Items.Count - 1);
        }

        private void GoToFirstPage(object sender, EventArgs e)
        {
            this.GoToIndex(0);
		}
		
		private void ToggleBookmark(object sender, EventArgs e)
        {
            this.viewmodel.ThreadBookmarkCommand.Execute(this.viewmodel.Thread);
        }
		
		private void TogglePageLock(object sender, EventArgs e)
        {
            this._pageLocked = !this._pageLocked;
            if (this._pageLocked)
                MessageBox.Show("view locked.", ":)", MessageBoxButton.OK);
            else
                MessageBox.Show("view unlocked.", ":)", MessageBoxButton.OK);
        }
		
		private void ToggleFullscreen(object sender, EventArgs e)
        {
            this.IsFullscreenActive = !this.IsFullscreenActive;
        }
		
		private void ShowPageNav(object sender, EventArgs e)
        {
            // Ensure Rating control is hidden
            this.ThreadRatingControl.Visibility = System.Windows.Visibility.Collapsed;
            // Ensure Nav control is visible
            this.ThreadNavControl.Visibility = System.Windows.Visibility.Visible;

            // Set Commands
            this.ThreadNavControl.FirstPageCommand = viewmodel.FirstPageCommand;
            this.ThreadNavControl.LastPageCommand = viewmodel.LastPageCommand;
            this.ThreadNavControl.CustomPageCommand = viewmodel.CustomPageCommand;
            this.ThreadNavControl.ThreadSource = viewmodel.Thread;

            this.ModalWindow.IsOpen = true;
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

        private void RefreshCurrentPage(object sender, EventArgs e)
        {
            if (this.viewmodel.IsReady)
                this.pagePresenter.Refresh();
            else
            {
                BuildStateFromNavigationQuery();
            }
        }

        private void CancelEditRequest(object sender, EventArgs e)
        {
            // I'll let this be a user choice (users must clear the text box manually)...
            // this.ThreadReplyTextBox.Text = string.Empty;
            
            this._currentRequest = null;
            this.HideThreadReplyPanel(null, null);
        }

        private void UpdateTextCount(object sender, TextChangedEventArgs e)
        {
            this.ReplyTextCount.Text = this.ThreadReplyTextBox.Text.Length.ToString();
        }

        private void SaveReplyDraft(object sender, EventArgs e)
        {
            this.SavedDraft = this.ThreadReplyTextBox.Text.Trim();
            MessageBox.Show("Message saved to drafts.", ":)", MessageBoxButton.OK);
        }

        private RadAnimation FadeInAnimation
        {
            get { return this.Resources["fadeInAnimation"] as RadAnimation; }
        }

        private RadAnimation FadeOutAnimation
        {
            get { return this.Resources["fadeOutAnimation"] as RadAnimation; }
        }

        private void HightlightSwipeGlyph(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
        	// TODO: Add event handler implementation here.
            var element = sender as FrameworkElement;
            if (element != null)
                Telerik.Windows.Controls.RadAnimationManager.Play(element, FadeInAnimation);
        }

        private void HideSwipeGlyph(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null)
                Telerik.Windows.Controls.RadAnimationManager.Play(element, FadeOutAnimation);
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
        [DataMember]
        public string Draft { get; set; }

        public void Save(ThreadViewPage page)
        {
            var source = page.Viewmodel.SelectedItem;
            var data = source.Data;

            PageCount = data.LastPage;
            Title = source.Title;
            ThreadID = source.ThreadID;
            PageNumber = source.PageNumber;
            Html = source.Html;
            Posts = source.Posts;

            Draft = page.SavedDraft;

            bool saved = this.SaveToFile(SAVE_FILE);

#if DEBUG
            MessageBox.Show("state saved: " + saved);
#endif
        }

        public static ThreadViewPageState Load()
        {
            var state = CoreExtensions.LoadFromFile<ThreadViewPageState>(SAVE_FILE);
            return state;
        }

        public static void Save(ThreadViewPageState state) { state.SaveToFile(SAVE_FILE); }

        public ThreadViewPageState() { }

        public ThreadViewPageState(Data.ThreadDataSource thread, Data.ThreadPageDataSource page)
        {
            ThreadID = thread.ThreadID;
            Title = thread.Title;
            PageCount = thread.PageCount;
            Html = page.Html;
            Posts = page.Posts;
            PageNumber = page.PageNumber;
        }
    }
}