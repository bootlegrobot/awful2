using System;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.DataForm;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Awful.ViewModels
{
    public class SettingsViewModel : Common.BindableBase
    {
        private AppDataModel appModel;
        private List<AwfulDebugger.Level> _levels = null;
      
        public SettingsViewModel()
        {
            appModel = App.Model;
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
                        AwfulDebugger.Level.Critical,
                        AwfulDebugger.Level.Debug
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


        public bool ContentFilterEnabled
        {
            get { return appModel.IsContentFilterEnabled; }
            set
            {
                bool enabled = VerifyContentFilter(value);
                appModel.IsContentFilterEnabled = enabled;
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

        public bool HideThreadTags
        {
            get { return appModel.HideThreadIcons; }
            set
            {
                appModel.HideThreadIcons = value;
                OnPropertyChanged("HideThreadTags");
            }
        }
    }
}
