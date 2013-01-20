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
        public event SAThreadViewEventHandler ViewThread;

        private void OnViewThread(ViewModels.ThreadViewModel viewmodel)
        {
            if (ViewThread != null)
                ViewThread(this, new SAThreadViewEventArgs(viewmodel));
        }

        protected override object DoWork(Uri parameter)
        {
            // first, obtain the html at the location specified by the uri
            var client = new AwfulWebClient();
            var html = client.FetchHtml(parameter.AbsoluteUri);

            // next, try and parse a thread page from the html
            var page = ThreadPageParser.ParseThreadPage(html);

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
            return viewmodel;
        }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter) &&
                (parameter as Uri).AbsoluteUri.Contains("forums.somethingawful.com");
        }

        protected override void OnError(Exception ex)
        {
            MessageBox.Show("Navigation failed.", ":(", MessageBoxButton.OK);
        }

        protected override void OnCancel()
        {
            MessageBox.Show("Navigation cancelled.", ":|", MessageBoxButton.OK);
        }

        protected override void OnSuccess(object arg)
        {
            OnViewThread(arg as ViewModels.ThreadViewModel);
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
            MessageBox.Show("Link navigation failed.", ":(", MessageBoxButton.OK);
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
            catch (Exception ex) { }
        }

        protected override bool PreCondition(Uri item)
        {
            return true;
        }
    }
}
