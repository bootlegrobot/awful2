using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;

namespace Awful.ViewModels
{
    public interface IDataLoadable
    {
        void LoadData();
        bool IsDataLoaded { get; }
    }

    public enum ListCommands
    {
        Refresh = 0,
        NextPage,
        Clear
    }

    public delegate void ListItemsDelegate<T>(IEnumerable<T> items);

    public abstract class ListViewModel<T> : Common.BindableBase, ICommand, IDataLoadable
    {
        public ListViewModel(ObservableCollection<T> collection)
        {
            this._items = collection;
            collection.CollectionChanged += NotifyCollectionChanged;
            ProcessItems = UpdateItems;
        }

        public ListViewModel() : this(new ObservableCollection<T>()) { }

        public event EventHandler DataLoaded;

        #region properties

        private DateTime? _lastUpdated;
        public string LastUpdated
        {
            get
            {
                return string.Format("Last Updated: {0}",
                    _lastUpdated.HasValue ?
                    _lastUpdated.Value.ToString("MM/dd/yyyy") + " " + _lastUpdated.Value.ToShortTimeString() :
                    "never");
            }
        }

        protected ListItemsDelegate<T> ProcessItems
        {
            get;
            set;
        }

        private BackgroundWorker _worker;
        protected BackgroundWorker Worker
        {
            get
            {
                if (_worker == null)
                    _worker = CreateBackgroundWorker();

                return _worker;
            }
        }

        public bool IsDataLoaded
        {
            get { return OnIsDataLoaded(); }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get { return this._isRunning; }
            protected set
            {
                this._isRunning = value;
                this.OnPropertyChanged("IsRunning");
            }
        }

        private ObservableCollection<T> _items;
        public ObservableCollection<T> Items
        {
            get { return _items; }
            set { UpdateItemCollection(value); }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            private set { SetProperty(ref _status, value, "Status"); }
        }

        #endregion

        protected virtual BackgroundWorker CreateBackgroundWorker()
        {
            var worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(OnDoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(OnProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnWorkCompleted);
            return worker;
        }

        private void OnWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                OnCancel();
                OnDataLoaded();
            }

            else if (e.Error != null)
            {
                OnError(e.Error);
                OnDataLoaded();
            }

            else
            {
                IEnumerable<T> items = e.Result as IEnumerable<T>;
                ProcessItems(items);
                OnSuccess();
            }
        }

        protected virtual bool OnIsDataLoaded()
        {
            return !Items.IsNullOrEmpty();
        }

        protected abstract void OnError(Exception exception);
        protected abstract void OnCancel();
        protected abstract void OnSuccess();
        
        protected void UpdateStatus(string message)
        {
            try 
            {
                if (this.Worker.IsBusy)
                    Worker.ReportProgress(-1, message);
                else
                    this.Status = message;
            }
            catch (InvalidOperationException) { }
        }

        protected void UpdateLastUpdated(DateTime? date)
        {
            this._lastUpdated = date;
            UpdateStatus(LastUpdated);
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string message = e.UserState as string;
            this.Status = message;
        }

        protected virtual void OnDoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = LoadDataInBackground();
            if (Worker.CancellationPending)
                e.Cancel = true;
        }

        protected virtual void OnDataLoaded()
        {
            if (DataLoaded != null)
                DataLoaded(this, EventArgs.Empty);
        }

        private void NotifyCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Items");
        }

        private void UpdateItemCollection(ObservableCollection<T> items)
        {
            if (this._items != null)
                this._items.CollectionChanged -= NotifyCollectionChanged;

            items.CollectionChanged += NotifyCollectionChanged;
            this._items = items;
            OnPropertyChanged("Items");
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData()
        {
            if (!Worker.IsBusy)
            {
                this.IsRunning = true;
                this.ProcessItems = this.UpdateItems;
                LoadDataAsync();
            }
        }

        public virtual void Refresh()
        {
            this.LoadData();
        }

        protected abstract IEnumerable<T> LoadDataInBackground();

        protected virtual void LoadDataAsync()
        {
            Worker.RunWorkerAsync();
        }

        protected virtual void UpdateItems(IEnumerable<T> items)
        {
            if (items != null)
            {
                this.Items.Clear();

                foreach (var item in items)
                    this.Items.Add(item);
            }

            this.IsRunning = false;
            OnDataLoaded();
        }

        public bool CanExecute(object parameter)
        {
            return parameter != null;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                var param = (ListCommands)parameter;
                switch (param)
                {
                    case ListCommands.Refresh:
                        this.Refresh();
                        break;

                    case ListCommands.Clear:
                        this.Items.Clear();
                        break;
                }
            }
        }
    }

    public abstract class PagedListViewModel<T> : ListViewModel<T>
    {
        private int _currentIndex;
        public int CurrentIndex
        {
            get { return this._currentIndex; }
            set { SetProperty(ref _currentIndex, value, "CurrentIndex"); }
        }

        protected override IEnumerable<T> LoadDataInBackground() { return LoadPageInBackground(0); }
        protected abstract IEnumerable<T> LoadPageInBackground(int index);

        public void LoadFirstPage()
        {
            this.Refresh();
        }

        public virtual void AppendNextPage()
        {
            if (!Worker.IsBusy)
            {
                this.IsRunning = true;
                ProcessItems = AppendItems;
                LoadPageAsync(CurrentIndex + 1);
            }
        }

        protected override void LoadDataAsync()
        {
            LoadPageAsync(0);
        }

        protected override void OnDoWork(object sender, DoWorkEventArgs e)
        {
            int index = (int)e.Argument;
            var page = LoadPageInBackground(index);
            if (page != null)
                _currentIndex = index;

            e.Result = page;
        }

        protected void LoadPageAsync(int index)
        {
            Worker.RunWorkerAsync(index);
        }

        protected virtual void AppendItems(IEnumerable<T> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    int index = Items.IndexOf(item);
                    if (index != -1)
                    {
                        Items.RemoveAt(index);
                        Items.Insert(index, item);
                    }
                    else
                        Items.Add(item);
                }
            }

            this.IsRunning = false;
        }
    }
}
