using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Awful.Commands
{
    public abstract class BackgroundWorkerCommand<T> : ICommand
    {
        private BackgroundWorker _worker;
        private BackgroundWorker Worker
        {
            get
            {
                if (_worker == null)
                {
                    _worker = CreateBackgroundWorker();
                    _worker.DoWork += OnWorkerDoWork;
                    _worker.RunWorkerCompleted += OnRunWorkerCompleted;
                }

                return _worker;
            }
        }

        private void OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            PostWork(e);
        }

        private void OnWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            DoWork(e);
        }

        protected abstract void DoWork(DoWorkEventArgs e);
        protected abstract void PostWork(RunWorkerCompletedEventArgs e);
        protected abstract bool PreWork(T item);

        protected virtual BackgroundWorker CreateBackgroundWorker()
        {
            return new BackgroundWorker();
        }

        public virtual bool CanExecute(object parameter)
        {
            return parameter is T;
        }

        public event EventHandler CanExecuteChanged;
        public void Execute(object parameter)
        {
            if (CanExecute(parameter) && PreWork((T)parameter) && !this.Worker.IsBusy)
                Worker.RunWorkerAsync(parameter);
        }
    }
}
