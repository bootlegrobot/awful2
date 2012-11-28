using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Diagnostics;
using Awful.Common;
using Telerik.Windows.Controls;
using Awful.Data;
using System.Text;

namespace Awful
{
    public class AppDataModel
    {
        static AppDataModel()
        {
           
        }

        private void AwfulLoginClient_LoginSuccessful(object sender, LoginEventArgs e)
        {
            var user = new UserDataSource(e.User);
            MainDataSource.Instance.CurrentUser = user;
        }
       
        public void Init()
        {
            LoadResourceFilesIntoIsoStore();
            LoadStateFromIsoStorage();
        }

        public void LoadStateFromIsoStorage()
        {
            if (!MainDataSource.Instance.IsActive)
                MainDataSource.LoadInstanceFromIsoStorage("main.xml");
        }

        public void SaveStateToIsoStorage()
        {
            MainDataSource.Instance.LastUpdated = DateTime.Now;
            MainDataSource.Instance.SaveToIsoStorage("main.xml");
        }

        private void LoadResourceFilesIntoIsoStore()
        {
            var dictionary = new Dictionary<string, string>();
            
            // add resource locations here.

            dictionary.Add(Constants.RESOURCE_PATH_CSS, Constants.FILE_PATH_CSS);
            dictionary.Add(Constants.RESOURCE_PATH_AWFUL_JAVASCRIPT,Constants.FILE_PATH_AWFUL_JAVASCRIPT);
            dictionary.Add(Constants.RESOURCE_PATH_JQUERY, Constants.FILE_PATH_JQUERY_JAVASCRIPT);
            dictionary.Add(Constants.RESOURCE_PATH_THREADVIEW, Constants.FILE_PATH_WWW);

            // copy them into storage
            foreach (var key in dictionary.Keys)
            {
               var success = CoreExtensions.CopyResourceFileToIsoStore(key, dictionary[key]);
               if (!success)
                   throw new Exception("Could not save resources to storage!");
            }
        }
    }
}
