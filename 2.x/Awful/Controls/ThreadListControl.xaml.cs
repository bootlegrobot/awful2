using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Awful.Data;
using Awful.ViewModels;
using Telerik.Windows.Controls;

namespace Awful.Controls
{
    public partial class ThreadListControl : UserControl
    {
        public ThreadListControl()
        {
            InitializeComponent();
        }
   
        public void HideRefreshControl()
        {
            this.ThreadListBox.StopPullToRefreshLoading(true);
        }

        private void ThreadListBox_ItemTap(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var item = e.Item.AssociatedDataItem.Value as ThreadDataSource;
            var frame = App.Current.RootVisual as PhoneApplicationFrame;
            item.NavigateToThreadView(frame.Navigate);
        }

        bool refreshRequested;
        private void ThreadListBox_RefreshRequested(object sender, EventArgs e)
        {
            object context = (sender as FrameworkElement).DataContext;
            var listmodel = context as ViewModels.ListViewModel<ThreadDataSource>;
            if (listmodel != null)
            {
                refreshRequested = true;
                this.BusyIndicator.Visibility = System.Windows.Visibility.Collapsed;
                listmodel.DataLoaded += OnDataLoaded;
                listmodel.Refresh();
            }
        }

        void OnDataLoaded(object sender, EventArgs e)
        {
            var listmodel = sender as ViewModels.ListViewModel<ThreadDataSource>;
            listmodel.DataLoaded -= OnDataLoaded;
            if (refreshRequested)
            {
                refreshRequested = false;
                this.ThreadListBox.StopPullToRefreshLoading(true);
                this.BusyIndicator.Visibility = System.Windows.Visibility.Visible;
            }
        }
    }
}
