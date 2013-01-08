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
using System.Windows.Threading;

namespace Awful
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public ViewModels.SettingsViewModel SettingsDataSource
        {
            get { return LayoutRoot.Resources["SettingsDataSource"] as ViewModels.SettingsViewModel; }
        }

        public ViewModels.LoggingViewModel LoggingDataSource
        {
            get { return LayoutRoot.Resources["LoggingDataSource"] as ViewModels.LoggingViewModel; }
        }

        public SettingsPage()
        {
            InitializeComponent();
            MainPivot.SelectionChanged += new SelectionChangedEventHandler(MainPivot_SelectionChanged);
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += (o, e) => { this.ContentFilterSwitch.IsChecked = SettingsDataSource.ContentFilterEnabled; };
            timer.Start();
        }

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPivot.SelectedItem.Equals(LoggingPivot))
            {
                if (!LoggingDataSource.IsDataLoaded)
                    LoggingDataSource.LoadData();
            }
        }

        private void LogFilePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var collection = LoggingDataSource.SelectedItem.LoadTextAsync();
            this.LogFileViewer.ItemsSource = collection;
        }

        private void ViewLogContent(object content)
        {
            
        }

        private void LockPivot(object sender, ManipulationStartedEventArgs e)
        {
            this.MainPivot.IsLocked = true;
        }

        private void UnlockPivot(object sender, ManipulationCompletedEventArgs e)
        {
            this.MainPivot.IsLocked = false;
        }
    }
}
