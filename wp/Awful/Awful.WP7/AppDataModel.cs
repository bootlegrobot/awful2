using System;
using System.Linq;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Diagnostics;
using Awful.Common;
using Telerik.Windows.Controls;
using Awful.Data;
using System.Text;
using Awful.Helpers;
using System.Windows;

namespace Awful
{
    public class AppDataModel : KollaSoft.Settings
    {
        public void Init()
        {

#if DEBUG
            AwfulDebugger.DebugLevel = AwfulDebugger.Level.Debug;
            AwfulWebClient.SimulateTimeout = true;
            AwfulWebClient.SimulateTimeoutChance = 1;  // chance to timeout on requests
#endif
            LoadResourceFilesIntoIsoStore();
            LoadStateFromIsoStorage();
        }

        public void LoadStateFromIsoStorage()
        {
            if (!MainDataSource.Instance.IsActive)
            {
                UserDataSource user = CoreExtensions.LoadFromFile<UserDataSource>("user.xml");
                PinnedItemsCollection pins = CoreExtensions.LoadFromFile<PinnedItemsCollection>("pins.xml");
                ForumCollection forums = CoreExtensions.LoadFromFile<ForumCollection>("forums.xml");
                UserBookmarks bookmarks = CoreExtensions.LoadFromFile<UserBookmarks>("bookmarks.xml");
                ThreadTable threads = CoreExtensions.LoadFromFile<ThreadTable>("threads.xml");
                
                MainDataSource.Instance.CurrentUser = user;
                MainDataSource.Instance.Pins = pins;
                MainDataSource.Instance.Forums = forums;
                MainDataSource.Instance.Bookmarks = bookmarks;
                MainDataSource.Instance.ThreadTable = threads;
            }
        }

        public void SaveStateToIsoStorage()
        {
            try
            {
                MainDataSource.Instance.Pins.SaveToFile("pins.xml");
                MainDataSource.Instance.Bookmarks.SaveToFile("bookmarks.xml");
                MainDataSource.Instance.ThreadTable.SaveToFile("threads.xml");
                MainDataSource.Instance.Forums.SaveToFile("forums.xml");
            }

            catch (Exception ex) { AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex); }
            finally
            {
                AwfulDebugger.SaveAndDispose();
            }
        }

        private void LoadResourceFilesIntoIsoStore()
        {
            var dictionary = new Dictionary<string, string>();
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            
            // add resource locations here.

            dictionary.Add(Constants.RESOURCE_PATH_CSS, Constants.FILE_PATH_CSS);
            dictionary.Add(Constants.RESOURCE_PATH_AWFUL_JAVASCRIPT,Constants.FILE_PATH_AWFUL_JAVASCRIPT);
            dictionary.Add(Constants.RESOURCE_PATH_JQUERY, Constants.FILE_PATH_JQUERY_JAVASCRIPT);
            dictionary.Add(Constants.RESOURCE_PATH_THREADVIEW, Constants.FILE_PATH_WWW);

            // copy them into storage
            foreach (var key in dictionary.Keys)
            {
                var path = dictionary[key];
               var success = CoreExtensions.CopyResourceFileToIsoStore(key, path);
               success = success && storage.FileExists(path);
             
#if DEBUG
               if (success)
               {
                   using (var reader = new System.IO.StreamReader(
                       storage.OpenFile(path, 
                       System.IO.FileMode.Open, 
                       System.IO.FileAccess.Read)))
                   {
                       string text = reader.ReadToEnd();
                       Debug.WriteLine(text);
                   }
               }

#endif

               if (!success)
                   throw new Exception("Could not save resources to storage!");
            }
        }

        public override void LoadSettings()
        {
            AwfulDebugger.DebugLevel = this.DebugLevel;
            ContentFilter.IsContentFilterEnabled = this.IsContentFilterEnabled;
            ThemeManager.Instance.SetCurrentTheme(this.CurrentTheme);
            MainDataSource.Instance.AutoRefreshBookmarks = this.AutoRefreshBookmarks;
        }

        private const int NOTIFICATION_DEFAULT = 0;
        public const string NOTIFICATION_KEY = "DefaultNotification";
        public NotificationMethod DefaultNotification
        {
            get { return (NotificationMethod)GetValueOrDefault<int>(NOTIFICATION_KEY, NOTIFICATION_DEFAULT); }
            set
            {
                NotificationMethod? method = (NotificationMethod)value;
                AddOrUpdateValue(NOTIFICATION_KEY, method.GetValueOrDefault());
                OnPropertyChanged("DefaultNotification");
            }
        }

        public enum HomePageSection
        {
            Forums = 0,
            Bookmarks,
            Pins
        };

        private const int HOMEPAGESECTION_DEFAULT = 0;
        public const string HOMEPAGESECTION_KEY = "DefaultHomePage";
        public HomePageSection DefaultHomePage
        {
            get
            {
                int value = GetValueOrDefault<int>(HOMEPAGESECTION_KEY, HOMEPAGESECTION_DEFAULT);
                return (HomePageSection)value;
            }

            set
            {
                int key = (int)value;
                AddOrUpdateValue(HOMEPAGESECTION_KEY, key);
                NotifyPropertyChanged("DefaultHomePage");
            }
        }

        private const bool AUTOREFRESH_BOOKMARKS_DEFAULT = false;
        public const string AUTOREFRESH_BOOKMARKS_KEY = "AutoRefreshBookmarks";
        public bool AutoRefreshBookmarks
        {
            get { return GetValueOrDefault<bool>(AUTOREFRESH_BOOKMARKS_KEY, AUTOREFRESH_BOOKMARKS_DEFAULT); }
            set { AddOrUpdateValue(AUTOREFRESH_BOOKMARKS_KEY, value); NotifyPropertyChanged("AutoRefreshBookmarks"); }
        }

        public enum ThreadViewMode
        {
            Header = 0,
            Fullscreen
        };

        private const int THREADVIEW_DEFAULT = 0;
        public const string THREADVIEW_KEY = "DefaultThreadView";
        public ThreadViewMode DefaultThreadView
        {
            get 
            {
                int value = GetValueOrDefault<int>(THREADVIEW_KEY, THREADVIEW_DEFAULT);
                return (ThreadViewMode)value;
            }

            set
            {
                int toInt = (int)value;
                AddOrUpdateValue(THREADVIEW_KEY, value);
                NotifyPropertyChanged("DefaultThreadView");
            }
        }

        private const int FONTSIZE_DEFAULT = 12;
        private const string FONTSIZE_KEY = "ContentFontSize";
        public int ContentFontSize
        {
            get { return GetValueOrDefault<int>(FONTSIZE_KEY, FONTSIZE_DEFAULT); }
            set
            {
                AddOrUpdateValue(FONTSIZE_KEY, value);
                NotifyPropertyChanged("ContentFontSize");
            }
        }

        private const bool HIDETHREADICON_DEFAULT = true;
        private const string HIDETHREADICON_KEY = "HideThreadIcons";
        public bool HideThreadIcons
        {
            get { return GetValueOrDefault<bool>(HIDETHREADICON_KEY, HIDETHREADICON_DEFAULT); }
            set
            {
                AddOrUpdateValue(HIDETHREADICON_KEY, value);
                NotifyPropertyChanged("HideThreadIcons");
            }
        }

        private const bool CONTENTFILTERENABLED_DEFAULT = true;
        private const string CONTENTFILTERENABLED_KEY = "IsContentFilterEnabled";
        public bool IsContentFilterEnabled
        {
            get { return GetValueOrDefault<bool>(CONTENTFILTERENABLED_KEY, CONTENTFILTERENABLED_DEFAULT); }
            set
            {
                AddOrUpdateValue(CONTENTFILTERENABLED_KEY, value);
                Helpers.ContentFilter.IsContentFilterEnabled = value;
                NotifyPropertyChanged("IsContentFilterEnabled");
            }
        }

        private const AwfulDebugger.Level DEBUGLEVEL_DEFAULT = AwfulDebugger.Level.Info;
        private const string DEBUGLEVEL_KEY = "DebugLevel";
        public AwfulDebugger.Level DebugLevel
        {
            get { return GetValueOrDefault<AwfulDebugger.Level>(DEBUGLEVEL_KEY, DEBUGLEVEL_DEFAULT); }
            set
            {
                AddOrUpdateValue(DEBUGLEVEL_KEY, value);
                AwfulDebugger.DebugLevel = value;
                NotifyPropertyChanged("DebugLevel");
            }
        }

        private const string CURRENTTHEME_DEFAULT = "accent";
        private const string CURRENTTHEME_KEY = "CurrentTheme";
        public string CurrentTheme
        {
            get { return GetValueOrDefault<string>(CURRENTTHEME_KEY, CURRENTTHEME_DEFAULT); }
            set
            {
                AddOrUpdateValue(CURRENTTHEME_KEY, value);
                ThemeManager.Instance.SetCurrentTheme(value);
                NotifyPropertyChanged("CurrentTheme");
            }
        }
    }
}
