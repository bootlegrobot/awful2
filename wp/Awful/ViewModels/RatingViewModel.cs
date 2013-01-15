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
using System.ComponentModel;

namespace Awful.ViewModels
{
    public class RatingViewModel : Commands.BackgroundWorkerCommand<object>
    {
        public delegate void PostRatingAction(bool success);

        private Data.ThreadDataSource _thread;
        public Data.ThreadDataSource Thread
        {
            get { return _thread; }
            set { SetProperty(ref _thread, value, "Thread"); }
        }

        public ICommand RatingCommand { get { return this; } }
        
        private PostRatingAction PostAction { get; set; }
        public void SetPostRatingAction(PostRatingAction action)
        {
            this.PostAction = action;
        }
      
        protected override object DoWork(object parameter)
        {
            int value = -1;
            bool success = false;
            if (int.TryParse(parameter.ToString(), out value))
            {
                success = ThreadTasks.Rate(this.Thread.Data, value);
                
                if (!success)
                    throw new Exception("Rating request failed.");
            }

            return value;
        }

        protected override void OnError(Exception ex)
        {
            this.IsRunning = false;
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
            MessageBox.Show("Rating request failed.", ":(", MessageBoxButton.OK);
            PostAction(false);
        }

        protected override void OnCancel()
        {
            
        }

        protected override void OnSuccess(object arg)
        {
            this.IsRunning = false;
            string value = arg.ToString();
            MessageBox.Show(string.Format("You rated this thread '{0}'! Good job, go hog wild!", value),
                ":)", MessageBoxButton.OK);
            PostAction(true);
        }

        protected override bool PreCondition(object item)
        {
            int value = -1;
            return int.TryParse(item.ToString(), out value);
        }
    }
}
