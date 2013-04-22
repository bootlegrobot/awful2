using System;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.DataForm;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Threading;
using Awful;

namespace Awful.ViewModels
{
    public class SettingsViewModel : Common.BindableBase
    {
        private AppDataModel appModel;
        private List<AwfulDebugger.Level> _levels = null;
        private List<AppDataModel.ThreadViewMode> _modes = null;
        private Awful.Commands.LogoutCommand _logout = new Commands.LogoutCommand();
      
        public SettingsViewModel()
        {
            appModel = App.Model;
        }

        public Awful.Commands.LogoutCommand LogoutCommand
        {
            get { return _logout; }
        }

        private string _quote;
        public string Quote
        {
            get { return _quote; }
            set { SetProperty(ref _quote, value, "Quote"); }
        }

        public List<AppDataModel.HomePageSection> HomePages
        {
            get
            {
                var list = new List<AppDataModel.HomePageSection>()
                {
                    AppDataModel.HomePageSection.Forums,
                    AppDataModel.HomePageSection.Pins,
                    AppDataModel.HomePageSection.Bookmarks
                };

                return list;
            }
        }

        public bool SmoothScrolling
        {
            get { return appModel.IsSmoothScrollEnabled; }
            set
            {
                appModel.IsSmoothScrollEnabled = value;
                OnPropertyChanged("SmoothScrolling");
            }
        }

        public AppDataModel.HomePageSection SelectedHomePage
        {
            get { return appModel.DefaultHomePage; }
            set { appModel.DefaultHomePage = value; OnPropertyChanged("SelectedHomePage"); }
        }

        public bool RefreshBookmarks
        {
            get { return appModel.AutoRefreshBookmarks; }
            set { appModel.AutoRefreshBookmarks = value; OnPropertyChanged("RefreshBookmarks"); }
        }

        public List<AwfulDebugger.Level> DebugItems
        {
            get
            {
                if (_levels == null)
                {
                    _levels = new List<AwfulDebugger.Level>()
                    {
                        AwfulDebugger.Level.Info,
                        AwfulDebugger.Level.Debug,
                        AwfulDebugger.Level.Verbose
                    };
                }

                return _levels;
            }
        }

        public AwfulDebugger.Level SelectedDebugLevel
        {
            get { return this.appModel.DebugLevel; }
            set
            {
                this.appModel.DebugLevel = value;
                OnPropertyChanged("SelectedDebugLevel");
            }
        }

        public List<AppDataModel.ThreadViewMode> ViewModes
        {
            get
            {
                if (_modes == null)
                {
                    _modes = new List<AppDataModel.ThreadViewMode>()
                    {
                        AppDataModel.ThreadViewMode.Header,
                        AppDataModel.ThreadViewMode.Fullscreen
                    };
                }

                return _modes;
            }
        }

        public AppDataModel.ThreadViewMode SelectedViewMode
        {
            get { return appModel.DefaultThreadView; }
            set
            {
                appModel.DefaultThreadView = value;
                OnPropertyChanged("SelectedViewMode");
            }
        }

        public bool ContentFilterEnabled
        {
            get { return appModel.IsContentFilterEnabled; }
            set
            {
                bool enabled = VerifyContentFilter(value);
                appModel.IsContentFilterEnabled = enabled;
                HideThreadTags = appModel.IsContentFilterEnabled;
                OnPropertyChanged("ModifyThreadTags");
            }
        }

        private bool VerifyContentFilter(bool value)
        {
            if (!value)
            {
                var response = MessageBox.Show("Content on the SomethingAwful Forums may contain content deemed " +
                    "unsuitable for minors. By clicking OK, you confirm that you " +
                    "are at least 18 years of age. If not, please click CANCEL to return " +
                    "back to the Settings menu.",
                    ":o", MessageBoxButton.OKCancel);
                bool keepEnabled = response == MessageBoxResult.Cancel;
                value = keepEnabled;
            }

            return value;
        }

        public bool ModifyThreadTags
        {
            get { return ContentFilterEnabled == false; }
        }

        public bool HideThreadTags
        {
            get { return appModel.HideThreadIcons; }
            set
            {
                appModel.HideThreadIcons = value;
                OnPropertyChanged("HideThreadTags");
            }
        }

        internal void Refresh()
        {
            OnPropertyChanged("HomePages");
            OnPropertyChanged("SelectedHomePage");
            OnPropertyChanged("DebugItems");
            OnPropertyChanged("SelectedDebugLevel");
            OnPropertyChanged("ViewModes");
            OnPropertyChanged("SelectedViewMode");
        }
    }
}
