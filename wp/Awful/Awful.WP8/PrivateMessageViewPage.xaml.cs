using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Awful.ViewModels;

namespace Awful
{
    public partial class PrivateMessageViewPage : PhoneApplicationPage
    {
        public PrivateMessageViewPage()
        {
            InitializeComponent();
            Loaded += PrivateMessageViewPage_Loaded;
        }

        private PrivateMessagesPageViewModel Viewmodel { get { return this.Resources["dataSource"] as PrivateMessagesPageViewModel; } }

        private void PrivateMessageViewPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Viewmodel.IsDataLoaded)
                Viewmodel.LoadData();
        }
    }
}