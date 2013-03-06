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

        public enum CommandType { Viewmodel, Uri };
        private delegate SAThreadViewEventArgs CommandTypeDelgate(Uri parameter);

        private CommandTypeDelgate GenerateThreadView
        {
            get
            {
                if (ResultType == CommandType.Uri)
                    return new CommandTypeDelgate(RouteUri);
                else
                    return new CommandTypeDelgate(CreateViewmodel);
            }
        }

        private CommandType _resultType = CommandType.Uri;
        public CommandType ResultType
        {
            get { return _resultType; }
            set { _resultType = value; }
        }
        
        private static void FireEvent(object sender, 
            SAThreadViewEventHandler handler,
            SAThreadViewEventArgs args)
        {
            if (handler != null)
                handler(sender, args);
        }

        private SAThreadViewEventArgs CreateViewmodel(Uri parameter)
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
            return new SAThreadViewEventArgs(viewmodel);
        }

        private SAThreadViewEventArgs RouteUri(Uri parameter)
        {
            return new SAThreadViewEventArgs(parameter);
        }

        protected override object DoWork(Uri parameter)
        {
            return GenerateThreadView(parameter);
        }

        protected override void PreWork(Uri item)
        {
            FireEvent(this, ViewLoading, null);
        }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) &&
                (parameter as Uri).AbsoluteUri.Contains("forums.somethingawful.com");
        }

        protected override void OnError(Exception ex)
        {
            Notification.ShowError("Navigation failed.", "Hyperlink");
            FireEvent(this, ViewFailed, null);
        }

        protected override void OnCancel()
        {
            Notification.Show("Navigation cancelled.", "Hyperlink");
        }

        protected override void OnSuccess(object arg)
        {
            var eventArgs = arg as SAThreadViewEventArgs;
            FireEvent(this, ViewThread, eventArgs);  

            // linking to other pages isn't a redirect so don't use this.
            // RedirectionListener.Notify(eventArgs.PageUri);
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
