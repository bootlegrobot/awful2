using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Awful.Common;

namespace Awful.Commands
{
    public class ToggleBookmarkCommand : BackgroundWorkerCommand<Data.ThreadDataSource>
    {
        private bool _isBookmarked;

        protected override bool PreCondition(Data.ThreadDataSource item)
        {
            return ConfirmToggle(item);
        }

        private bool ConfirmToggle(Data.ThreadDataSource thread)
        {
             _isBookmarked = Data.MainDataSource.Instance.Bookmarks.Contains(
                thread);

            string message = _isBookmarked ?
                "remove from bookmarks?" :
                "add to bookmarks?";

            var response = MessageBox.Show(message, ":O", MessageBoxButton.OKCancel);
            return response == MessageBoxResult.OK;
        }

        private bool ToggleBookmarksWork(bool isBookmarked, ThreadMetadata thread)
        {
            if (!isBookmarked)
                return Data.MainDataSource.Instance.CurrentUser.Metadata.AddToUserBookmarks(thread);
            else
                return Data.MainDataSource.Instance.CurrentUser.Metadata.RemoveFromUserBookmarks(thread);
        }

        private void PrintStatus(string message, bool success)
        {
            MessageBox.Show(message,
                success ? ":)" : ":(",
                MessageBoxButton.OK);
        }

        protected override object DoWork(Data.ThreadDataSource parameter)
        {
            return ToggleBookmarksWork(_isBookmarked, parameter.Data);
        }

        protected override void OnError(Exception ex)
        {
            PrintStatus(ex.Message, false);
        }

        protected override void OnCancel()
        {
            PrintStatus("request canceled.", true);
        }

        protected override void OnSuccess(object arg)
        {
            PrintStatus("request successful!", true); 
        }
    }

    public class QuotePostToClipboardCommand : BackgroundWorkerCommand<Data.ThreadPostSource>
    {
        protected override object DoWork(Data.ThreadPostSource parameter)
        {
            string quote = parameter.Data.Quote();
            return quote;
        }

        protected override void OnError(Exception ex)
        {
            MessageBox.Show("quote request failed.", ":(", MessageBoxButton.OK);
        }

        protected override void OnCancel() { }

        protected override void OnSuccess(object arg)
        {
            string quote = arg as string;
            Clipboard.SetText(quote);
            MessageBox.Show("quote added to clipboard.", ":)", MessageBoxButton.OK);
        }

        protected override bool PreCondition(Data.ThreadPostSource item)
        {
            return true;
        }
    }

    public class MarkPostAsReadCommand : BackgroundWorkerCommand<Data.ThreadPostSource>
    {
        protected override object DoWork(Data.ThreadPostSource parameter)
        {
            bool success = parameter.Data.MarkAsRead();
            return success;
        }

        protected override void OnError(Exception ex)
        {
            MessageBox.Show("mark post request failed.", ":(", MessageBoxButton.OK);
        }

        protected override void OnCancel()
        {
            
        }

        protected override void OnSuccess(object arg)
        {
            MessageBox.Show("The selected has been marked as read up to this post.", ":)", MessageBoxButton.OK);
        }

        protected override bool PreCondition(Data.ThreadPostSource item)
        {
            return true;
        }
    }

    public class ClearMarkedPostsCommand : BackgroundWorkerCommand<Data.ThreadDataSource>
    {
        protected override object DoWork(Data.ThreadDataSource parameter)
        {
            bool success = ThreadTasks.ClearMarkedPosts(parameter.Data);
            return success;
        }

        protected override void OnError(Exception ex)
        {
            this.IsRunning = false;
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
            MessageBox.Show("The request failed.", ":(", MessageBoxButton.OK);
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess(object arg)
        {
            this.IsRunning = false;
            MessageBox.Show("All read posts have been cleared.", ":)", MessageBoxButton.OK);
        }

        protected override bool PreCondition(Data.ThreadDataSource item)
        {
            return item.Data.ThreadID != null;
        }
    }


    public class EditPostCommand : BackgroundWorkerCommand<Data.ThreadPostSource>
    {
        public static event ThreadPostRequestEventHandler EditRequested;

        private static void OnEditRequested(EditPostCommand command, IThreadPostRequest request)
        {
            if (EditRequested != null)
                EditRequested(command, new ThreadPostRequestEventArgs(request));
        }

        public override bool CanExecute(object parameter)
        {
            bool valid = base.CanExecute(parameter);
            string username = Data.MainDataSource.Instance.CurrentUser.Metadata.Username;
            valid = valid && username.Equals((parameter as Data.ThreadPostSource).Data.Author);
            return valid;
        }

        protected override object DoWork(Data.ThreadPostSource parameter)
        {
            IThreadPostRequest request = parameter.Data.BeginEdit();
            return request;
        }

        protected override void OnError(Exception ex)
        {
            MessageBox.Show("The edit request failed.", ":(", MessageBoxButton.OK);
        }

        protected override void OnCancel()
        {
            
        }

        protected override void OnSuccess(object arg)
        {
            IThreadPostRequest request = arg as IThreadPostRequest;
            OnEditRequested(this, request);
        }

        protected override bool PreCondition(Data.ThreadPostSource item)
        {
            var response = MessageBox.Show("Are you sure you wish to edit this post? Any current in the reply window will be erased.", ":o", MessageBoxButton.OKCancel);
            return response == MessageBoxResult.OK;
        }
    }
}
