﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Awful.Commands
{
    public abstract class BackgroundWorkerCommand<T> : Common.BindableBase, ICommand
    {
        private bool _isRunning = false;
        public bool IsRunning
        {
            get { return _isRunning; }
            protected set 
            { 
                SetProperty(ref _isRunning, value, "IsRunning");
                OnStateChanged();
            }
        }

        public bool IsBusy { get { return this.Worker.IsBusy; } }

        private string _status = string.Empty;
        public string Status
        {
            get { return _status; }
            protected set { SetProperty(ref _status, value, "Status"); }
        }

        private BackgroundWorker _worker = null;
        private BackgroundWorker Worker
        {
            get
            {
                if (_worker == null)
                {
                    _worker = CreateBackgroundWorker();
                    _worker.DoWork += OnWorkerDoWork;
                    _worker.RunWorkerCompleted += OnRunWorkerCompleted;
                    _worker.ProgressChanged += OnProgressChanged;
                }

                return _worker;
            }
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string value = e.UserState as string;
            Status = value;
        }

        protected void UpdateStatus(string value)
        {
            if (Worker.IsBusy)
                Worker.ReportProgress(0, value);
            else
                Status = value;
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

            this.IsRunning = false;
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
            return new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
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

        public event EventHandler StateChanged;
        protected virtual void OnStateChanged()
        {
            if (StateChanged != null)
                StateChanged(this, EventArgs.Empty);
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
