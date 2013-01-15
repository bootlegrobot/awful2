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
using Awful.ViewModels;

namespace Awful
{
    public partial class HomePage : PhoneApplicationPage
    {
        public HomePage()
        {
            InitializeComponent();
            Datasource = new HomePageViewModel();
            MainContent.DataContext = Datasource;
        }

        private HomePageViewModel Datasource
        {
            get;
            set;
        }

        private void LoadContent(object sender, SelectionChangedEventArgs e)
        {
            var index = (sender as Pivot).SelectedIndex;
            var item = Datasource.Items[index];
            if (!item.IsDataLoaded)
                item.LoadData();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // if we got here from the login page...
            if (e.IsNavigationInitiator && e.NavigationMode == System.Windows.Navigation.NavigationMode.New)
            {
                NavigationService.RemoveBackEntry();
            }
        }

        private void LoadContentInitial(object sender, RoutedEventArgs e)
        {
            /*
            //When we initially load the panorama, get items to put into the data source.
            var item = Datasource.Items[0];
            if (!item.IsDataLoaded)
                item.LoadData();
            */
        }

        private void NavigateToSettings(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.RelativeOrAbsolute));
        }
    }
}