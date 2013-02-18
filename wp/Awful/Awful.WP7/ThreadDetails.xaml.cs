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
    }
}