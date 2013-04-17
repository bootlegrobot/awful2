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
using System.Windows.Media;

namespace Awful
{
    public partial class PrivateMessageDetailsPage : PhoneApplicationPage
    {
        public PrivateMessageDetailsPage()
        {
            InitializeComponent();
            this.PageManager = new ThreadPageManager(this.PrivateMessageWebView, null,
                this.TitleText.Foreground as SolidColorBrush,
                this.LayoutRoot.Background as SolidColorBrush);

            this.PageManager.Loading += PageManager_Loading;
            this.PageManager.Loaded += PageManager_Loaded;
            this.PageManager.ReadyForContent += PageManager_ReadyForContent;

            Loaded += PrivateMessageDetailsPage_Loaded;
        }

        void PrivateMessageDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.PrevButton.IsEnabled = ViewModel.HasPrev;
            this.NextButton.IsEnabled = ViewModel.HasNext;
        }

        void PageManager_Loading(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        void PageManager_Loaded(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            if (!ViewModel.ShowWebView)
                ViewModel.ShowWebView = true;
        }

        void PageManager_ReadyForContent(object sender, EventArgs e)
        {
            ShowCurrentMessage();
        }

        private readonly ThreadPageManager PageManager;
        private NewPrivateMessagePageViewModel NewMessage { get { return App.Current.Resources["PMNewSource"] as NewPrivateMessagePageViewModel; } }
        private PrivateMessageDetailsViewModel ViewModel { get { return App.Current.Resources["PMDetailsSource"] as PrivateMessageDetailsViewModel; } }
        private IApplicationBarIconButton PrevButton
        {
            get
            {
                return this.ApplicationBar.Buttons[2] as IApplicationBarIconButton; 
            }
        }

        private IApplicationBarIconButton NextButton
        {
            get
            {
                return this.ApplicationBar.Buttons[3] as IApplicationBarIconButton;
            }
        }

        private void ShowCurrentMessage()
        {
            if (PageManager.IsReady)
            {
                PageManager.ClearHtml();
                ViewModel.SelectedItem.GetFormattedMessageAsync(html => PageManager.LoadHtml(html));
            }
        }

        private void AppBarPrevButton_Click(object sender, EventArgs e)
        {
            ViewModel.ShowPrevItem();
            ShowCurrentMessage();
            PrevButton.IsEnabled = ViewModel.HasPrev;
            NextButton.IsEnabled = ViewModel.HasNext;
        }

        private void AppBarNextButton_Click(object sender, EventArgs e)
        {
            ViewModel.ShowNextItem();
            ShowCurrentMessage();
            NextButton.IsEnabled = ViewModel.HasNext;
            PrevButton.IsEnabled = ViewModel.HasPrev;
        }

        private void AppBarReplyButton_Click(object sender, EventArgs e)
        {
            RespondOptionsWindow.IsOpen = !RespondOptionsWindow.IsOpen;
        }

        private void ForwardText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NewMessage.IsForward = true;
            NewMessage.InitCallback = NavigateToNewMessagePage;
            NewMessage.LoadMessage(ViewModel.SelectedItem);
        }

        private void ReplyText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NewMessage.IsForward = false;
            NewMessage.InitCallback = NavigateToNewMessagePage;
            NewMessage.LoadMessage(ViewModel.SelectedItem);
        }

        private void NavigateToNewMessagePage(bool success)
        {
            if (success)
                NavigationService.Navigate(new Uri("/NewPrivateMessagePage.xaml", UriKind.Relative));
        }
    }
}