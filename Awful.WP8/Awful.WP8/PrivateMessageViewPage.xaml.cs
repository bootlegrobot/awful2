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
            Telerik.Windows.Controls.InteractionEffectManager.SetIsInteractionEnabled(this.LayoutRoot, true);
            Telerik.Windows.Controls.InteractionEffectManager.AllowedTypes.Add(typeof(Telerik.Windows.Controls.RadDataBoundListBoxItem));
        }

        private PrivateMessagesPageViewModel Viewmodel { get { return App.Current.Resources["PMFoldersSource"] as PrivateMessagesPageViewModel; } }
        private PrivateMessageDetailsViewModel Details { get { return App.Current.Resources["PMDetailsSource"] as PrivateMessageDetailsViewModel; } }
        private NewPrivateMessagePageViewModel NewMessage { get { return App.Current.Resources["PMNewSource"] as NewPrivateMessagePageViewModel; } }

        private void PrivateMessageViewPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Viewmodel.IsDataLoaded)
                Viewmodel.LoadData();
        }

        private void AppBarSelectButton_Click(object sender, EventArgs e)
        {
            Viewmodel.IsSelectModeActive = !Viewmodel.IsSelectModeActive;
        }

        private void groupList_ItemTap(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var item = e.Item.AssociatedDataItem.Value as Data.PrivateMessageDataSource;

            if (item is Data.PrivateMessageDataGroup)
                return;

            Details.CurrentFolder = Viewmodel.SelectedFolder;
            Details.Items = Viewmodel.SelectedFolder.GetMessages();
            Details.SelectedItem = item;
            NavigationService.Navigate(new Uri("/PrivateMessageDetailsPage.xaml", UriKind.Relative));
        }

        private void AppbarNewButton_Click(object sender, EventArgs e)
        {
            NewMessage.InitCallback = NavigateToNewMessagePage;
            NewMessage.IsForward = false;
            NewMessage.CreateNewMessage();
        }

        private void NavigateToNewMessagePage(bool success)
        {
            if (success)
                NavigationService.Navigate(new Uri("/NewPrivateMessagePage.xaml", UriKind.Relative));
        }

        private void AppbarRefreshButton_Click(object sender, EventArgs e)
        {
            Viewmodel.Refresh();
        }

        private void AppbarSeachButton_Click(object sender, EventArgs e)
        {

        }

        private void AppbarFoldersMenu_Click(object sender, EventArgs e)
        {
            folderListWindow.IsOpen = true;
        }
    }
}