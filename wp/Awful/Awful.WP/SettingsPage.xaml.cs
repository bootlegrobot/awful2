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

        private readonly DispatcherTimer _timer;

        public SettingsPage()
        {
            InitializeComponent();
            MainPivot.SelectionChanged += new SelectionChangedEventHandler(MainPivot_SelectionChanged);
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += (o, e) => { this.ContentFilterSwitch.IsChecked = SettingsDataSource.ContentFilterEnabled; };
            
            Loaded += SettingsPage_Loaded;
            Unloaded += SettingsPage_Unloaded;
        }

        void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }

        void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _timer.Start();
        }

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPivot.SelectedItem.Equals(LoggingPivot))
            {
                if (!LoggingDataSource.IsDataLoaded)
                    LoggingDataSource.LoadData();

                this.ApplicationBar = LoggingDataSource.AppBar;
            }

            else
                this.ApplicationBar = null;

            if (MainPivot.SelectedItem.Equals(SettingsPivot))
            {
                SettingsDataSource.Refresh();
            }
        }

        private void LogFilePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void ScrollLogToTop(object sender, EventArgs e)
        {
            LoggingDataSource.ScrollCurrentLogToTop();
            LogFileViewer.BringIntoView(LogFileViewer.SelectedItem);
        }

        private void ScrollLogToBottom(object sender, EventArgs e)
        {
            LoggingDataSource.ScrollCurrentLogToBottom();
            LogFileViewer.BringIntoView(LogFileViewer.SelectedItem);
        }

        private void RefreshLogFileList(object sender, EventArgs e)
        {
            LoggingDataSource.Refresh();
        }

        private void DeleteSelectedLogFile(object sender, EventArgs e)
        {
            var response = 
                MessageBox.Show("Are you sure you want to delete the selected log file?", 
                ":o", 
                MessageBoxButton.OKCancel);

            if (response == MessageBoxResult.OK)
            {
                bool deleted = LoggingDataSource.DeleteCurrentLog();
                if (deleted)
                    LoggingDataSource.Refresh();
            }
        }
    }
}
