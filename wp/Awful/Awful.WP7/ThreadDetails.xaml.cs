using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telerik.Windows.Controls;

namespace Awful
{
    public partial class ThreadDetails : PhoneApplicationPage
    {
        private ViewModels.ThreadDetailsViewModel DataSource 
        {
            get 
            { 
                return this.Resources["ThreadDetailsDataSource"] as ViewModels.ThreadDetailsViewModel; 
            } 
        }
        private PageOrientation CurrentOrientation { get; set; }
        private bool OrientationLocked { get; set; }

        private bool IsReplyViewActive
        {
            get { return this.ReplyViewPanel.Visibility == System.Windows.Visibility.Visible; }
            set
            {
                if (value)
                {
                    VisualStateManager.GoToState(this, "ReplyView", true);
                    try 
                    { 
                        ApplicationBar = threadReplyView.ReplyAppBar;
                        this.SupportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
                    }

                    catch (Exception) { }
                }

                else
                {
                    VisualStateManager.GoToState(this, "ThreadView", true);
                    try 
                    { 
                        ApplicationBar = DataSource.ThreadViewBar; 
                    }
                    catch (Exception) { }
                }
            }
        }

        public ThreadDetails()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
            OrientationChanged += OnOrientationChanged;
            ThreadPageManager.Instance.ReadyForContent += OnViewReadyForContent;
            Commands.EditPostCommand.EditRequested += OnPostEditRequested;
            this.threadReplyView.Loaded += OnThreadReplyViewLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            CurrentOrientation = this.Orientation;
            IsReplyViewActive = false;
        }

        private void OnOrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if (!IsReplyViewActive)
                CurrentOrientation = e.Orientation;
        }

        private void OnPostEditRequested(object sender, Common.ThreadPostRequestEventArgs args)
        {
            this.IsReplyViewActive = true;
        }

        private void OnThreadReplyViewLoaded(object sender, RoutedEventArgs e)
        {
            this.threadReplyView.ReplyClick = SendReply;
            this.threadReplyView.SaveDraftClick = SaveDraft;
            this.threadReplyView.CancelEditClick = CancelEdit;
        }

        private void OnViewReadyForContent(object sender, EventArgs e)
        {
            LoadFromQuery();
        }

        private void RestoreOrientation()
        {
            if (OrientationLocked)
            {
                bool portrait = CurrentOrientation.IsPortrait();
                if (portrait)
                    SupportedOrientations = SupportedPageOrientation.Portrait;
                else
                    SupportedOrientations = SupportedPageOrientation.Landscape;
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (IsReplyViewActive)
            {
                e.Cancel = true;
                IsReplyViewActive = false;
            }

            else if (threadSlideView.ControlViewModel.MoveToPreviousHistory())
                e.Cancel = true;

            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            // clear the saved draft if we're leaving of our own accord
            if (e.IsNavigationInitiator)
            {
                this.threadReplyView.ReplyViewModel.DeleteDraft();
            }
        }

        private void LoadFromQuery()
        {
            var query = NavigationContext.QueryString;

            if (query.ContainsKey("id") && query.ContainsKey("nav"))
            {
                string ThreadID = query["id"];
                ThreadMetadata thread = new ThreadMetadata() { ThreadID = ThreadID };

                switch (query["nav"])
                {
                    case "unread":
                        this.threadSlideView.ControlViewModel.LoadFirstUnreadPost(thread);
                        break;

                    case "last":
                        this.threadSlideView.ControlViewModel.LoadLastPost(thread);
                        break;

                    case "page":
                        int pageNumber = -1;
                        int.TryParse(query["pagenumber"], out pageNumber);
                        this.threadSlideView.ControlViewModel.LoadPageNumber(thread, pageNumber);
                        break;
                }
            }
        }

        private void ToggleBookmarks(object sender, System.EventArgs e)
        {
            string threadId = threadSlideView.ControlViewModel.CurrentThread.ThreadID;
            var threadModel = Data.ThreadDataSource.FromThreadId(threadId);
            DataSource.BookmarkCommand.Execute(threadModel);
        }

        private void ShowReplyView(object sender, System.EventArgs e)
        {
            // create request for reply content if necessary
            if (threadReplyView.ReplyViewModel.Request == null)
            {
                string threadId = threadSlideView.ControlViewModel.CurrentThread.ThreadID;
                this.threadReplyView.ReplyViewModel.CreateNew(threadId);
            }

            IsReplyViewActive = true;
        }

        private void RefreshThread(object sender, System.EventArgs e)
        {
            this.threadSlideView.ControlViewModel.RefreshCurrentPage();
        }

        private void ShowNavControl(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void SaveDraft(object sender, System.EventArgs e)
        {
            this.threadReplyView.ReplyViewModel.SaveCurrentState();
        }

        private void CancelEdit(object sender, System.EventArgs e)
        {
            string message = "Cancel edit? This will clear the reply window.";
            if (MessageBox.Show(message, "Cancel?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                string threadId = threadSlideView.ControlViewModel.CurrentThread.ThreadID;
                this.threadReplyView.ReplyViewModel.CreateNew(threadId);
            }
        }

        private void SendReply(object sender, System.EventArgs e)
        {
            this.threadReplyView.ReplyViewModel.SendThreadRequestAsync();
        }

        private void ToggleOrientation(object sender, System.EventArgs e)
        {
            OrientationLocked = !OrientationLocked;
        }

        private void ShowRatingPanel(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }
    }
}