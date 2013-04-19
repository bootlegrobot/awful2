using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Awful
{
    public partial class NewPrivateMessagePage : PhoneApplicationPage
    {
        public NewPrivateMessagePage()
        {
            InitializeComponent();
        }

        private ViewModels.NewPrivateMessagePageViewModel ViewModel { get { return App.Current.Resources["PMNewSource"] as ViewModels.NewPrivateMessagePageViewModel; } }

        private void upload_Click(object sender, EventArgs e)
        {
            PhotoChooserTask pct = new PhotoChooserTask();
            pct.ShowCamera = true;
            pct.Completed += pct_Completed;
            pct.Show();
        }

        private void pct_Completed(object sender, PhotoResult e)
        {
            PhotoChooserTask pct = sender as PhotoChooserTask;
            pct.Completed -= pct_Completed;

            if (e.TaskResult == TaskResult.OK)
            {
                var response = MessageBox.Show("Upload image to Imgur? This cannot be undone once started.", "Upload", MessageBoxButton.OKCancel);
                if (response == MessageBoxResult.OK)
                    ViewModel.AttachImage(e);
            }
        }

        private void AppBarUploadButton_Click(object sender, EventArgs e)
        {
            upload_Click(sender, e);
        }

        private void AppBarSendButton_Click(object sender, EventArgs e)
        {
            ViewModel.SendMessage();
        }
    }
}