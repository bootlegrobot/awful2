using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Awful.Common
{
    public delegate object DoWorkCallback<T>(T parameter);
    public delegate void ErrorCallback(Exception ex);
    public delegate void SuccessCallback(object arg);

    public class BackgroundThread<T> : BindableBase
    {
        private string _status;
        public string Status
        {
            get { return _status; }
            private set { SetProperty(ref _status, value, "Status"); }
        }

        public void UpdateStatus(string value)
        {
            if (this._worker.IsBusy)
                this._worker.ReportProgress(0, value);

            else
                this.Status = value;
        }

        private BackgroundWorker _worker;
        private DoWorkCallback<T> _doWork;
        private SuccessCallback _success;
        private ErrorCallback _error;

        public static BackgroundThread<T> RunAsync(T parameter,
            DoWorkCallback<T> doWork,
            SuccessCallback success,
            ErrorCallback error)
        {
            BackgroundThread<T> thread = new BackgroundThread<T>();
            thread._doWork = doWork;
            thread._success = success;
            thread._error = error;
            thread._worker.RunWorkerAsync(parameter);
            return thread;
        }

        private BackgroundThread()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += _worker_DoWork;
            _worker.RunWorkerCompleted += _worker_RunWorkerCompleted;
            _worker.ProgressChanged += _worker_ProgressChanged;
            _worker.WorkerReportsProgress = true;
        }

        void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string value = e.UserState as string;
            Status = value;
        }

        void _worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
                this._success(e.Result);
        }

        void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var result = this._doWork((T)e.Argument);
            e.Result = result;
        }
    }
}
