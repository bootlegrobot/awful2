using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Awful.ViewModels
{
    public interface IDataLoadable
    {
        void LoadData();
        bool IsDataLoaded { get; }
    }

    public enum ListViewModelCommands
    {
        Refresh = 0,
        NextPage,
        Clear
    }

    public abstract class ListViewModel<T> : Common.BindableBase, ICommand, IDataLoadable
    {
        public ListViewModel(ObservableCollection<T> collection)
        {
            this._items = collection;
            collection.CollectionChanged += NotifyCollectionChanged;
        }

        public ListViewModel() : this(new ObservableCollection<T>()) { }

        public event EventHandler DataLoaded;

        #region properties

        public bool IsDataLoaded
        {
            get { return !this.Items.IsNullOrEmpty(); }
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
            set { UpdateItems(value); }
        }

        #endregion

        protected virtual void OnDataLoaded()
        {
            if (DataLoaded != null)
                DataLoaded(this, EventArgs.Empty);
        }

        private void NotifyCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Items");
        }

        private void UpdateItems(ObservableCollection<T> items)
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
        public virtual void LoadData()
        {
            this.IsRunning = true;
            LoadDataAsync(ProcessData);
        }

        public virtual void Refresh()
        {
            this.LoadData();
        }

        protected abstract IEnumerable<T> LoadDataWork();

        protected virtual void LoadDataAsync(Action<IEnumerable<T>> completed)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                var forums = LoadDataWork();
                Deployment.Current.Dispatcher.BeginInvoke(() => { completed(forums); });

            }, null);
        }

        protected virtual void ProcessData(IEnumerable<T> items)
        {
            this.Items.Clear();

            foreach (var item in items)
                this.Items.Add(item);

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
                var param = (ListViewModelCommands)parameter;
                switch (param)
                {
                    case ListViewModelCommands.Refresh:
                        this.Refresh();
                        break;

                    case ListViewModelCommands.Clear:
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

        protected override IEnumerable<T> LoadDataWork() { return LoadDataWork(0); }

        protected abstract IEnumerable<T> LoadDataWork(int index);

        public void LoadFirstPage()
        {
            this.LoadData();
        }

        public virtual void AppendNextPage()
        {
            this.IsRunning = true;
            LoadDataAsync(CurrentIndex + 1, AppendData);
        }

        protected override void LoadDataAsync(Action<IEnumerable<T>> completed)
        {
            LoadDataAsync(0, completed);
        }

        protected virtual void LoadDataAsync(int index, Action<IEnumerable<T>> completed)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                var forums = LoadDataWork(index);
                if (forums != null)
                    _currentIndex = index;

                Deployment.Current.Dispatcher.BeginInvoke(() => { completed(forums); });

            }, null);
        }

        protected virtual void AppendData(IEnumerable<T> items)
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
