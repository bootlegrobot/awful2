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
using Awful;

namespace Awful
{
    public partial class ThreadViewPage : PhoneApplicationPage
    {
        public ThreadViewPage()
        {
            InitializeComponent();

            viewmodel = new ViewModels.ThreadViewModel();
            SetThreadNavCommands(viewmodel);

            _threadStack = new Stack<ViewModels.ThreadViewModel>();
            
            Commands.EditPostCommand.EditRequested += new ThreadPostRequestEventHandler(OpenEditWindow);
            Commands.ViewSAThreadCommand.ViewThread += OnSAUriLinkSelected;
            Commands.ViewSAThreadCommand.ViewLoading += ViewSAThreadCommand_ViewLoading;
            Commands.ViewSAThreadCommand.ViewFailed += ViewSAThreadCommand_ViewFailed;

            viewmodel.ReadyToBind += OnViewModelReadyToBind;
            viewmodel.UpdateFailed += OnViewModelUpdateFailed;
            Loaded += ThreadViewPage_Loaded;
            threadReplyControl.Loaded += WireThreadReplyControl;
        }

        void ViewSAThreadCommand_ViewFailed(object sender, SAThreadViewEventArgs args)
        {
            VisualStateManager.GoToState(this, "Reading", true);
        }

        void ViewSAThreadCommand_ViewLoading(object sender, SAThreadViewEventArgs args)
        {
            VisualStateManager.GoToState(this, "Loading", true);
        }

        void OnSAUriLinkSelected(object sender, SAThreadViewEventArgs args)
        {
            this._threadStack.Push(args.Viewmodel);
            this.LoadViewmodel(args.Viewmodel);
        }

        void WireThreadReplyControl(object sender, RoutedEventArgs e)
        {
            var control = sender as Controls.ThreadReplyControl;
            control.ReplyClick = this.SendReply;
            control.SaveDraftClick = this.SaveReplyDraft;
            control.CancelEditClick = this.CancelEditRequest;
        }

        void OnViewModelUpdateFailed(object sender, EventArgs e)
        {
            ThreadInitializationPane.Visibility = Visibility.Collapsed;
        }

        private readonly Stack<ViewModels.ThreadViewModel> _threadStack;

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
                    _currentRequest = viewmodel.Thread.Data.CreateReplyRequest();

                return _currentRequest;
            }
        }

        private IApplicationBar PageNavBar
        {
            get { return this.Resources["DefaultAppBar"] as IApplicationBar; }
        }

        private IApplicationBar ReplyBar
        {
            get { return this.threadReplyControl.Resources["ReplyAppBar"] as IApplicationBar; }
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

            else if (this._threadStack.Count > 0)
            {
                this._threadStack.Pop();
                if (this._threadStack.Count > 0)
                {
                    this.LoadViewmodel(this._threadStack.Peek());
                    e.Cancel = true;
                }
            }

            base.OnBackKeyPress(e);
        }

        private void LoadViewmodel(ViewModels.ThreadViewModel threadViewModel)
        {
            this.pagePresenter.ClearHtml();
            this.SetThreadNavCommands(threadViewModel);
            this.DataContext = threadViewModel;

            VisualStateManager.GoToState(this, "Reading", true);
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
                this.state.Save(this);
            }

            else
            {
                this.state = null;
                this._threadStack.Clear();
            }
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
            this.state = args.State;
            this._threadStack.Push(viewmodel);
            this.DataContext = viewmodel;
            this.ThreadInitializationPane.Visibility = System.Windows.Visibility.Collapsed;
            this.GoToIndex(args.State.PageNumber - 1);

            VisualStateManager.GoToState(this, "Reading", true);
        }

        private void BuildStateFromNavigationQuery()
        {
            VisualStateManager.GoToState(this, "Loading", true);

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
                this.threadReplyControl.ReplyViewModel.Text = request.Content;
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
                VisualStateManager.GoToState(this, "Reading", true);
                /*
                this.ThreadReplyPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ThreadViewPanel.Visibility = System.Windows.Visibility.Visible;
                this.PostJumpButtonPanel.Visibility = System.Windows.Visibility.Visible;
                */
                this.IsFullscreenActive = this.IsFullscreenActive;
            }
        }

        private void ShowThreadReplyPanel(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
            if (!this.IsReplyViewActive)
            {
                VisualStateManager.GoToState(this, "Replying", true);
                /*
                this.ThreadViewPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ThreadReplyPanel.Visibility = System.Windows.Visibility.Visible;
                this.PostJumpButtonPanel.Visibility = System.Windows.Visibility.Collapsed;
                */

                // cancel edit option
                var menu = this.ReplyBar.MenuItems[1] as IApplicationBarMenuItem;
                if (CurrentRequest.RequestType == PostRequestType.Edit)
                    menu.IsEnabled = true;
                else
                    menu.IsEnabled = false;

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
                this.CurrentRequest.Content = this.threadReplyControl.ReplyTextBox.Text.Trim();
                SendReplyOrEdit(this.CurrentRequest);
            }

            CancelEditRequest(null, null);
        }

        private void SendReplyOrEdit(IThreadPostRequest request)
        {
            if (request != null)
                ViewModels.ReplyViewModel.SendRequestAsync(request, success =>
                    NotifyUserOfResult(success));
        }

        private void NotifyUserOfResult(Uri success)
        {
            string message = success == null? "Request successful!" : "request failed.";
            string caption = "Thread Post";
            if (success != null)
            {
                Notification.Show(App.Model.DefaultNotification,
                    message,
                    caption);
            }

            else
                Notification.ShowError(message, caption);
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
                Notification.Show("view locked.", "Toggle View");
            else
                Notification.Show("view unlocked.", "Toggle View");
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

        private void SaveReplyDraft(object sender, EventArgs e)
        {
            this.SavedDraft = this.threadReplyControl.ReplyTextBox.Text.Trim();
            Notification.Show("Message saved to drafts.", "Save Draft");
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

        private void ThreadPageSlideView_SlideAnimationStarted_1(object sender, EventArgs e)
        {
            MessageBox.Show("Slide Started!");
        }

        private void ThreadPageSlideView_SlideAnimationCompleted_1(object sender, EventArgs e)
        {
            MessageBox.Show("Slide Completed");
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