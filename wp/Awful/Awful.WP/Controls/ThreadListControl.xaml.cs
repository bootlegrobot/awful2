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
using System.Windows.Input;
using Awful.Common;
using Awful.WP7;


namespace Awful.Controls
{
    public partial class ThreadListControl : UserControl, IThreadNavContext
    {
        #region Data virtualization mode

        public static readonly DependencyProperty VirtualizationModeProperty = DependencyProperty.Register(
            "VirtualizationMode", typeof(DataVirtualizationMode), typeof(ThreadListControl),
            new PropertyMetadata(DataVirtualizationMode.Automatic, (o, e) =>
                {
                    (o as ThreadListControl).OnVirtualizationModeChanged((DataVirtualizationMode)e.NewValue);
                }));

        private void OnVirtualizationModeChanged(DataVirtualizationMode dataVirtualizationMode)
        {
            this.ThreadListBox.DataVirtualizationMode = dataVirtualizationMode;
        }

        public DataVirtualizationMode VirtualizationMode
        {
            get { return (DataVirtualizationMode)GetValue(VirtualizationModeProperty); }
            set { SetValue(VirtualizationModeProperty, value); }
        }

        #endregion

        public ThreadDataSource SelectedThread { get; private set; }

        public ThreadListControl()
        {
            InitializeComponent();
            this.ThreadNav.DataContext = this;
            InteractionEffectManager.AllowedTypes.Add(typeof(RadDataBoundListBoxItem));
        }
   
        public void HideRefreshControl()
        {
            this.ThreadListBox.StopPullToRefreshLoading(true);
        }

        private void ThreadListBox_ItemTap(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var item = e.Item.AssociatedDataItem.Value as ThreadDataSource;
            NavigateToThreadView(item);
        }

        private void NavigateToThreadView(ThreadDataSource thread, int pageNumber = (int)ThreadPageType.NewPost)
        {
            if (this.ModalWindow.IsOpen)
                this.ModalWindow.IsOpen = false;

            var frame = App.Current.RootVisual as PhoneApplicationFrame;
            thread.NavigateToThreadView(frame.Navigate, pageNumber);
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

        private void ThreadListBox_DataRequested(object sender, EventArgs e)
        {
            if (!refreshRequested)
            {
                object context = (sender as FrameworkElement).DataContext;
                var listmodel = context as ViewModels.PagedListViewModel<ThreadDataSource>;
                if (listmodel != null)
                    listmodel.AppendNextPage();
            }
        }

        private ICommand _firstPageCommand;
        public System.Windows.Input.ICommand FirstPageCommand
        {
            get
            {
                if (_firstPageCommand == null)
                    _firstPageCommand = new ActionCommand((state) => { NavigateToThreadView(this.SelectedThread, 0); });
                
                return _firstPageCommand;
            }
            set
            {
                _firstPageCommand = value;
            }
        }

        private ICommand _lastPageCommand;
        public System.Windows.Input.ICommand LastPageCommand
        {
            get
            {
                if (_lastPageCommand == null)
                    _lastPageCommand = new ActionCommand((state) => { NavigateToThreadView(this.SelectedThread, (int)ThreadPageType.Last); });
                
                return _lastPageCommand;
            }
            set
            {
                _lastPageCommand = value;
            }
        }

        private ICommand _customPageCommand;
        public System.Windows.Input.ICommand CustomPageCommand
        {
            get
            {
                if (_customPageCommand == null)
                    _customPageCommand = new ActionCommand((state) => 
                    {
                        int page = -1;
                        if (!int.TryParse(state.ToString(), out page))
                            page = 1;

                        NavigateToThreadView(this.SelectedThread, page); 
                    });
                
                return _customPageCommand;
            }
            set
            {
                _customPageCommand = value;
            }
        }

        private void ThreadContextMenu_JumpToPageRequest(object sender, EventArgs e)
        {
            this.SelectedThread = sender as Data.ThreadDataSource;
            this.ThreadNav.ThreadSource = SelectedThread;
            this.ModalWindow.IsOpen = true;
        }
		
		private void RefreshEmptyList(object sender, System.Windows.Input.GestureEventArgs e)
        {
        	// TODO: Add event handler implementation here.
			object context = this.DataContext;
            var listmodel = context as ViewModels.ListViewModel<ThreadDataSource>;
            if (listmodel != null)
            {
                refreshRequested = true;
                this.BusyIndicator.Visibility = System.Windows.Visibility.Collapsed;
                listmodel.DataLoaded += OnDataLoaded;
                listmodel.Refresh();
            }
        }
    }
}
