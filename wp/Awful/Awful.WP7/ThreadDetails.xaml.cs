using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Awful
{
    public partial class ThreadDetails : PhoneApplicationPage
    {
        private ViewModels.ThreadDetailsViewModel DataSource { get { return this.Resources["ThreadDetailsDataSource"] as ViewModels.ThreadDetailsViewModel; } }
        public ThreadDetails()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.IsNavigationInitiator)
                LoadFromQuery(e);
        }

        private void LoadFromQuery(NavigationEventArgs e)
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
        	// TODO: Add event handler implementation here.
        }

        private void ShowReplyView(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void RefreshThread(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void ShowNavControl(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void SaveDraft(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void SendReply(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void ToggleOrientation(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }

        private void ShowRatingPanel(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
        }
    }
}