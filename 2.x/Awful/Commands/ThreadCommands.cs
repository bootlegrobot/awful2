using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Awful.Commands
{
    public class ToggleBookmarkCommand : BackgroundWorkerCommand<Data.ThreadDataSource>
    {
        private bool _isBookmarked;

        protected override void DoWork(DoWorkEventArgs e)
        {
            var item = e.Argument as Data.ThreadDataSource;
            e.Result = ToggleBookmarksWork(_isBookmarked, item.Data);
        }

        protected override void PostWork(RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                PrintStatus(e.Error.Message, false);
            
            else if (e.Cancelled)
                PrintStatus("request canceled.", true);
            
            else
            {
                PrintStatus("request successful!", true);
            }
        }

        protected override bool PreWork(Data.ThreadDataSource item)
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
    }

}
