using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Awful.Commands
{
    public class LogoutCommand : ICommand
    {
       
        public bool CanExecute(object parameter)
        {
            var storage = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();
            bool exists = storage.FileExists("user.xml");
            return exists;
        }

        public event EventHandler CanExecuteChanged;
        private void OnCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }

        public void Execute(object parameter)
        {
            if (System.Windows.MessageBox.Show(
                "Are you sure?", "Logout", System.Windows.MessageBoxButton.OKCancel) == 
                System.Windows.MessageBoxResult.OK)
            {
                bool success = CoreExtensions.DeleteFileFromStorage("user.xml");
                string message = success ?
                    "Logout successful."
                    : "Logout failed.";

                System.Windows.Notification.Show(message, "Logout");
            }

            OnCanExecuteChanged();
        }
    }
}
