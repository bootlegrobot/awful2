using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Awful.Common;
using Microsoft.Phone.Tasks;

namespace Awful.Commands
{
    public class ViewSAThreadCommand : BackgroundWorkerCommand<Uri>
    {
        public static event SAThreadViewEventHandler ViewThread;
        public static event SAThreadViewEventHandler ViewLoading;
        public static event SAThreadViewEventHandler ViewFailed;

        private static void FireEvent(object sender, ViewModels.ThreadViewModel viewmodel,
            SAThreadViewEventHandler handler)
        {
            if (handler != null)
                handler(sender, new SAThreadViewEventArgs(viewmodel));
        }

        protected override object DoWork(Uri parameter)
        {
            // try and parse a thread page from the html
            ThreadPageMetadata page = MetadataExtensions.ThreadPageFromUri(parameter);

            // since we have a page, create thread metadata from it
            ThreadMetadata thread = new ThreadMetadata()
            {
                ThreadID = page.ThreadID,
                Title = page.ThreadTitle,
                PageCount = page.LastPage
            };

            // create binding wrappers
            Data.ThreadDataSource threadSource = new Data.ThreadDataSource(thread);
            Data.ThreadPageDataObject pageSource = new Data.ThreadPageDataObject(page);

            // create viewmodel
            ViewModels.ThreadViewModel viewmodel = new ViewModels.ThreadViewModel();
            int pageIndex = pageSource.PageNumber - 1;
            viewmodel.UpdateModel(threadSource);
            viewmodel.Pages[pageIndex] = pageSource;

            // set the current page to page source
            viewmodel.SelectedItem = viewmodel.Pages[pageIndex];
            viewmodel.SelectedIndex = pageIndex;
            return viewmodel;
        }

        protected override void PreWork(Uri item)
        {
            FireEvent(this, null, ViewLoading);
        }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) &&
                (parameter as Uri).AbsoluteUri.Contains("forums.somethingawful.com");
        }

        protected override void OnError(Exception ex)
        {
            Notification.ShowError("Navigation failed.", "Hyperlink");
            FireEvent(this, null, ViewFailed);
        }

        protected override void OnCancel()
        {
            Notification.Show("Navigation cancelled.", "Hyperlink");
        }

        protected override void OnSuccess(object arg)
        {
            FireEvent(this, arg as ViewModels.ThreadViewModel, ViewThread);        
        }

        protected override bool PreCondition(Uri item)
        {
            return true;
        }
    }

    public class OpenWebBrowserCommand : BackgroundWorkerCommand<Uri>
    {
        protected override object DoWork(Uri parameter)
        {
            return parameter;
        }

        protected override void OnError(Exception ex)
        {
            Notification.ShowError("Link navigation failed.", "Hyperlink");
        }

        protected override void OnCancel()
        {
          
        }

        protected override void OnSuccess(object arg)
        {
            Uri uri = arg as Uri;
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = uri;
            try { task.Show(); }
            catch (Exception ex) { AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex); }
        }

        protected override bool PreCondition(Uri item)
        {
            return true;
        }
    }
}
