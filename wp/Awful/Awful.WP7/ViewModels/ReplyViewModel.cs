using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace Awful.ViewModels
{
    public class ReplyViewModel : Common.BindableBase
    {
        private string _text = string.Empty;
        public string Text 
        {
            get { return _text; } 
            set { SetProperty(ref _text, value, "Text"); OnPropertyChanged("Count");} }

        public int Count { get { return _text.Length; } }

        public static void SendRequestAsync(IThreadPostRequest request, Action<bool> notify)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                bool success = request.Send();
                Deployment.Current.Dispatcher.BeginInvoke(() => notify(success));
            }, null);
        }
    }
}
