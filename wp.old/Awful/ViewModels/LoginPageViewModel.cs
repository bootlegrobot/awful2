using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Awful.ViewModels
{
    public class LoginPageViewModel : Common.BindableBase, ICommand
    {
        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            private set 
            { 
                SetProperty(ref _isBusy, value, "IsBusy");
                OnPropertyChanged("IsNotBusy");
            }
        }

        public bool IsNotBusy { get { return !IsBusy; } }

        private string _username = string.Empty;
        public string Username
        {
            get { return _username; }
            set
            {
                SetProperty(ref _username, value, "Username");
                OnCanExecuteChanged();
            }
        }

        private string _password = string.Empty;
        public string Password
        {
            get { return _password; }
            set
            {
                SetProperty(ref _password, value, "Password");
                OnCanExecuteChanged();
            }
        }

        public ICommand LoginCommand { get { return this; } }

        private readonly BackgroundWorker _worker;

        public event EventHandler LoginFailed;
        public event EventHandler LoginSuccess;

        public LoginPageViewModel()
        {
            _worker = new BackgroundWorker();
            _worker.WorkerSupportsCancellation = false;
            _worker.WorkerReportsProgress = true;
            _worker.DoWork += OnDoWork;
            _worker.RunWorkerCompleted += OnWorkCompleted;
        }

        public void LoginAsync()
        {
            if (!_worker.IsBusy)
            {
                this.IsBusy = true;
                _worker.RunWorkerAsync();
            }
        }

        private void OnWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.IsBusy = false;

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, ":(", MessageBoxButton.OK);
                OnLoginFinished(false);
            }

            else if (!e.Cancelled)
            {
                bool success = (bool)e.Result;
                OnLoginFinished(success);
            }
        }

        private void OnLoginFinished(bool success)
        {
            if (success && LoginSuccess != null)
                LoginSuccess(this, EventArgs.Empty);
            
            else if (LoginFailed != null)
                LoginFailed(this, EventArgs.Empty);
        }

        private void OnDoWork(object sender, DoWorkEventArgs e)
        {
            UserMetadata user = new UserMetadata();
            user.Username = this.Username;
            e.Result = user.Login(this.Password);
        }


        public bool CanExecute(object parameter)
        {
            bool valid = Username != null && Username != string.Empty;
            valid = valid && Password != null && Password != string.Empty;
            return valid && !IsBusy;
        }

        public event EventHandler CanExecuteChanged;
        private void OnCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
                LoginAsync();
        }
    }

}
