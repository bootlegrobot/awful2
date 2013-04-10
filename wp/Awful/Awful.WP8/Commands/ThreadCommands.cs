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

            var response = MessageBox.Show(message, "Bookmarks", MessageBoxButton.OKCancel);
            return response == MessageBoxResult.OK;
        }

        private bool ToggleBookmarksWork(bool isBookmarked, ThreadMetadata thread)
        {
            if (!isBookmarked)
                return Data.MainDataSource.Instance.CurrentUser.Metadata.AddToUserBookmarks(thread);
            else
                return Data.MainDataSource.Instance.CurrentUser.Metadata.RemoveFromUserBookmarks(thread);
        }

        private void PrintStatus(string message, bool success, string caption)
        {
            if (success)
                Notification.Show(message, caption);
            else
                Notification.ShowError(message, caption);
        }

        protected override object DoWork(Data.ThreadDataSource parameter)
        {
            return ToggleBookmarksWork(_isBookmarked, parameter.Data);
        }

        protected override void OnError(Exception ex)
        {
            PrintStatus(ex.Message, false, "Bookmark Toggle");
        }

        protected override void OnCancel()
        {
            PrintStatus("request canceled.", true, "Bookmark Toggle");
        }

        protected override void OnSuccess(object arg)
        {
            PrintStatus("request successful!", true, "Bookmark Toggle"); 
        }
    }

    public class QuotePostToClipboardCommand : BackgroundWorkerCommand<Data.ThreadPostSource>
    {
        public static event EventHandler ShowQuote;
        public string CurrentQuote { get; private set; }

        private static void OnShowQuote(QuotePostToClipboardCommand command)
        {
            if (ShowQuote != null)
                ShowQuote(command, EventArgs.Empty);
        }

        private void OnShowQuote()
        {
            QuotePostToClipboardCommand.OnShowQuote(this);
        }

        public QuotePostToClipboardCommand()
            : base()
        {
            CurrentQuote = string.Empty;
        }

        protected override object DoWork(Data.ThreadPostSource parameter)
        {
            string quote = parameter.Data.Quote();
            return quote;
        }

        protected override void OnError(Exception ex)
        {
            CurrentQuote = string.Empty;
            Notification.ShowError("Quote request failed.", "Quote");
        }

        protected override void OnCancel() { }

        protected override void OnSuccess(object arg)
        {
            string quote = arg as string;
            Clipboard.SetText(quote);
            CurrentQuote = quote;
            Notification.Show(App.Model.DefaultNotification,
                "Quote added to clipboard. Tap to view.", 
                "Quote",
                OnShowQuote);
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
            Notification.ShowError("mark post request failed.", "Mark Posts");
        }

        protected override void OnCancel()
        {
            
        }

        protected override void OnSuccess(object arg)
        {
            Notification.Show("The selected has been marked as read up to this post.", "Mark Posts");
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
            Notification.ShowError("The request failed.", "Unmark Thread");
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess(object arg)
        {
            this.IsRunning = false;
            Notification.Show("All read posts have been cleared.", "Unmark Thread");
        }

        protected override bool PreCondition(Data.ThreadDataSource item)
        {
            return item.Data.ThreadID != null;
        }
    }

    public class RateThreadCommand : BackgroundWorkerCommand<int>
    {
        public string ThreadId { get; set; }
        
        protected override object DoWork(int parameter)
        {
            var thread = new ThreadMetadata() { ThreadID = ThreadId };
            return thread.Rate(parameter);
        }

        protected override void OnError(Exception ex)
        {
            Notification.ShowError("Request failed.", "Rate Thread");
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess(object arg)
        {
            int rating = (int)arg;
            Notification.Show(string.Format("You rated this thread '{0}'! Go hog wild!", rating), "Rate Thread");
        }

        protected override bool PreCondition(int item)
        {
            return item > 0 && item < 6;
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
            if (valid)
            {
                //string username = Data.MainDataSource.Instance.CurrentUser.Metadata.Username;
                //valid = valid && username.Equals((parameter as Data.ThreadPostSource).Data.Author);

                valid = valid && (parameter as Data.ThreadPostSource).Data.IsEditable;
            }
            return valid;
        }

        protected override object DoWork(Data.ThreadPostSource parameter)
        {
            IThreadPostRequest request = parameter.Data.BeginEdit();
            return request;
        }

        protected override void OnError(Exception ex)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
            Notification.ShowError("The edit request failed.", "Edit Post");
        }

        protected override void OnCancel()
        {
            
        }

        protected override void OnSuccess(object arg)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Edit Request was successful!");

            IThreadPostRequest request = arg as IThreadPostRequest;
            OnEditRequested(this, request);
        }

        protected override bool PreCondition(Data.ThreadPostSource item)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Edit Request Made, Confirming...");

            var response = MessageBox.Show("Are you sure you wish to edit this post? Any current text in the reply window will be erased.", ":o", MessageBoxButton.OKCancel);
            bool proceed = response == MessageBoxResult.OK;

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, string.Format("Edit Request {0}.", proceed ? "Confirmed" : "Cancelled"));

            return proceed;
        }
    }

    public class SendThreadResponseCommand : BackgroundWorkerCommand<IThreadPostRequest>
    {
        public event EventHandler Success;
        public event EventHandler Failure;

        public static void FireEvent(object sender, EventHandler handler)
        {
            if (handler != null)
                handler(sender, EventArgs.Empty);
        }
    
        protected override object DoWork(IThreadPostRequest parameter)
        {
            Uri redirect = null;

            if (parameter != null)
            {
                UpdateStatus("Sending post...");
                redirect = parameter.Send();
                if (redirect == null)
                    throw new Exception("Post failed to send.");
            }

            return redirect;
        }

        protected override void OnError(Exception ex)
        {
            string message = "Request failed.";
            Notification.ShowError(message, "Post failed.");
            UpdateStatus(string.Empty);
            FireEvent(this, Failure);
        }

        private void RedirectToUri(Uri uri)
        {
            Common.RedirectionListener.Notify(uri);
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess(object arg)
        {
            UpdateStatus(string.Empty);
            var request = arg as Uri;
            string message = "Tap here to view.";

            Notification.Show(
                Awful.App.Model.DefaultNotification,
                message, 
                "Post was successful!",
                () => { RedirectToUri(request); });

            FireEvent(this, Success);
        }

        protected override bool PreCondition(IThreadPostRequest item)
        {
            return ConfirmSend(item);
        }

        private bool ConfirmSend(IThreadPostRequest Request)
        {
            string message = Request.RequestType == PostRequestType.Edit
                ? "Send edit?"
                : "Send reply?";

            return MessageBox.Show(message, "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK;
        }
    }
}
