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
    public partial class LoginPage : PhoneApplicationPage
    {
        private const string LOGIN_KEY = "Login";
        private ViewModels.LoginPageViewModel viewmodel;

        public LoginPage()
        {
            InitializeComponent();
            viewmodel = new ViewModels.LoginPageViewModel();
            viewmodel.LoginSuccess += viewmodel_LoginSuccess;
            viewmodel.LoginFailed += viewmodel_LoginFailed;
            this.DataContext = viewmodel;
        }

        private void viewmodel_LoginFailed(object sender, EventArgs e)
        {
            // Maybe throw a login failed message here.
            //MessageBox.Show("Login failed. Please try again.", ":(", MessageBoxButton.OK);
        }

        private void viewmodel_LoginSuccess(object sender, EventArgs e)
        {
            MessageBox.Show("Login successful! Welcome to the forums.", ":D", MessageBoxButton.OK);
            NavigateToHomePage();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // check if user has already logged in
            if (Data.MainDataSource.Instance.CurrentUser.CanLogIn)
                NavigateToHomePage();
            else
                ShowLoginScreen();
        }

        private void ShowLoginScreen()
        {
            this.LoginPanel.Visibility = System.Windows.Visibility.Visible;
        }

        private void NavigateToHomePage()
        {
            // Let the Home Page know we came from the login page. We want to remove this page from the stack.
            NavigationService.Navigate(new Uri("/HomePage.xaml?Source=" + LOGIN_KEY, UriKind.RelativeOrAbsolute));
        }
    }
}