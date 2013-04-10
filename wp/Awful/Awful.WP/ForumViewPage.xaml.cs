using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace Awful
{
    public partial class ForumViewPage : PhoneApplicationPage
    {
        public const string FORUMID_QUERY = "ForumId";

        private ViewModels.ForumPageViewModel viewmodel;

        public ForumViewPage()
        {
            InitializeComponent();
            Loaded += ForumViewPage_Loaded;
            viewmodel = new ViewModels.ForumPageViewModel();
            this.DataContext = viewmodel;
        }

        private void ForumViewPage_Loaded(object sender, RoutedEventArgs e)
        {
          
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (NavigationContext.QueryString.ContainsKey(FORUMID_QUERY))
            {
                string id = NavigationContext.QueryString[FORUMID_QUERY];
                viewmodel.UpdateModel(id, 0); 
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }

        /*
        private void OnDataLoaded(object sender, EventArgs e)
        {
            HideRefreshControl();
        }
          
        private void HideRefreshControl()
        {
            this.ThreadListBox.StopPullToRefreshLoading(true);
        }

        private void ThreadListBox_ItemTap(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var item = e.Item.AssociatedDataItem.Value as Data.ThreadDataSource;
            item.NavigateToThreadView(NavigationService.Navigate);
        }

        private void ThreadListBox_RefreshRequested(object sender, EventArgs e)
        {
            viewmodel.Refresh();
        }

        private void ThreadListBox_DataRequested(object sender, EventArgs e)
        {
            viewmodel.AppendNextPage();
        }
        */
    }
}