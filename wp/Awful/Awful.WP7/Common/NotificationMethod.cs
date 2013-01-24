using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace System.Windows
{
    public enum NotificationMethod
    {
        Toast = 0,
        MessageBox
    }

    public class Notification : Awful.Common.BindableBase
    {
        private static Notification Instance { get; set; }

        public static void Show(NotificationMethod method, string message, string caption)
        {
            Instance.ShowMessage(method, message, caption);
        }

        public static void Show(string message, string caption)
        {
            Awful.AppDataModel model = Awful.WP7.App.Model;
            Instance.ShowMessage(model.DefaultNotification, message, caption);
        }

        public static void ShowError(string message, string caption)
        {
            Awful.AppDataModel model = Awful.WP7.App.Model;
            Instance.ShowMessage(model.DefaultNotification, message, caption);
        }

        static Notification() { Instance = new Notification(); }

        private Notification() { }

        private void ShowMessage(NotificationMethod method, string message, string caption)
        {
            switch (method)
            {
                // send toast, or message box if failure
                case NotificationMethod.Toast:
                    try { ShowToastMessage(message, caption); }
                    catch (Exception) { ShowMessageBox(message, caption); }
                    break;

                // send message box
                case NotificationMethod.MessageBox:
                    ShowMessageBox(message, caption);
                    break;
            }
        }

        private void ShowErrorMessage(NotificationMethod method, string message, string caption)
        {
            switch (method)
            {
                // send toast, or message box if failure
                case NotificationMethod.Toast:
                    try { ShowToastError(message, caption); }
                    catch (Exception) { ShowMessageBoxError(message, caption); }
                    break;

                // send message box
                case NotificationMethod.MessageBox:
                    ShowMessageBoxError(message, caption);
                    break;
            }
        }

        private void ShowMessageBox(string message, string caption)
        {
            MessageBox.Show(message, caption, MessageBoxButton.OK);
        }

        private void ShowToastMessage(string message, string caption)
        {
            CreateToast(message, caption).Show();
        }

        private void ShowToastError(string message, string caption)
        {
            var toast = CreateToast(message, caption);
            toast.Background = new System.Windows.Media.SolidColorBrush(Media.Color.FromArgb(255, 255, 0, 0));
            toast.Show();
        }

        private void ShowMessageBoxError(string message, string caption) 
        {
            caption = string.Format("Error: {0}", caption); 
            ShowMessageBox(message, caption); 
        }

        private Coding4Fun.Phone.Controls.ToastPrompt CreateToast(string message, string caption)
        {
            Coding4Fun.Phone.Controls.ToastPrompt toast = new Coding4Fun.Phone.Controls.ToastPrompt();
            toast.Title = caption;
            toast.TextWrapping = TextWrapping.Wrap;
            toast.Message = message;
            toast.TextOrientation = System.Windows.Controls.Orientation.Vertical;
            toast.MillisecondsUntilHidden = 3000;
			toast.SetValue(Windows.Controls.Canvas.ZIndexProperty, 255);
            toast.ImageSource = new BitmapImage(new Uri("Assets/Awful.png", UriKind.RelativeOrAbsolute));
            return toast;
        }

        public static void ShowError(NotificationMethod notificationMethod, string p1, string p2)
        {
            Instance.ShowErrorMessage(notificationMethod, p1, p2);
        }
    }
}
