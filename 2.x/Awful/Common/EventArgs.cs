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

namespace Awful.Common
{
    public sealed class ThreadPostRequestEventArgs : EventArgs
    {
        public IThreadPostRequest Request { get; private set; }
        public ThreadPostRequestEventArgs(IThreadPostRequest request) { Request = request; }
    }

    public delegate void ThreadPostRequestEventHandler(object sender, ThreadPostRequestEventArgs args);

    public sealed class SAThreadViewEventArgs : EventArgs
    {
        public ViewModels.ThreadViewModel Viewmodel { get; private set; }
        public SAThreadViewEventArgs(ViewModels.ThreadViewModel viewmodel)
        {
            Viewmodel = viewmodel;
        }
    }

    public delegate void SAThreadViewEventHandler(object sender, SAThreadViewEventArgs args);

    public sealed class ThreadNavEventArgs : EventArgs
    {
        public Data.ThreadDataSource Thread { get; private set; }
        public int PageNumber { get; private set; }
        public ThreadNavEventArgs(Data.ThreadDataSource thread, int pagenumber) 
        {
            Thread = thread;
            PageNumber = pagenumber; 
        }
    }

    public delegate void ThreadNavEventHandler(object sender, ThreadNavEventArgs args);

    public sealed class ThreadReadyToBindEventArgs : EventArgs
    {
        public ThreadViewPageState State { get; private set; }
        public ThreadReadyToBindEventArgs(ThreadViewPageState state) { State = state; }
    }

    public delegate void ThreadReadyToBindEventHandler(object sender, ThreadReadyToBindEventArgs args);
}
