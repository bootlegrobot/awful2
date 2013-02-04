using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Awful.Commands
{
    public abstract class BackgroundWorkerCommand<T> : Common.BindableBase, ICommand
    {
        private bool _isRunning;
        public bool IsRunning
        {
            get { return _isRunning; }
            protected set { SetProperty(ref _isRunning, value, "IsRunning"); }
        }

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
            OnCanExecuteChanged();

            if (e.Cancelled)
                OnCancel();

            else if (e.Error != null)
                OnError(e.Error);
            else
                OnSuccess(e.Result);
        }

        private void OnWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            T parameter = (T)e.Argument;
            e.Result = DoWork(parameter);

            if (Worker.CancellationPending)
                e.Cancel = true;
        }

        protected abstract object DoWork(T parameter);
        protected abstract void OnError(Exception ex);
        protected abstract void OnCancel();
        protected abstract void OnSuccess(object arg);

        protected abstract bool PreCondition(T item);

        protected virtual void PreWork(T item) { }

        protected virtual BackgroundWorker CreateBackgroundWorker()
        {
            return new BackgroundWorker();
        }

        public virtual bool CanExecute(object parameter)
        {
            return parameter is T && !this.Worker.IsBusy;
        }

        public void Reset() { if (!this.Worker.IsBusy) { this.IsRunning = false; } }

        public event EventHandler CanExecuteChanged;
        protected virtual void OnCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter)
                && PreCondition((T)parameter))
            {
                this.IsRunning = true;
                PreWork((T)parameter);
                Worker.RunWorkerAsync(parameter);
                OnCanExecuteChanged();
            }
        }
    }
}
