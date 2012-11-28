using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.ViewModels
{
    public delegate void RefreshFinishedDelegate();

    public class ForumViewPageViewModel : Common.BindableBase
    {
        private RefreshFinishedDelegate OnRefreshed;
        private ThreadListViewModel _threads;
        private ForumMetadata _data;
        public ForumMetadata Data { get { return this._data; } }

        public ForumViewPageViewModel(RefreshFinishedDelegate predicate = null)
        {
            OnRefreshed = predicate;
        }

        public void SetMetadata(ForumMetadata data)
        {
            if (data != null && !data.Equals(Data))
            {
                DetachThreadListModelEvents(this._threads);
                this._threads = new ThreadListViewModel(data);
                AttachThreadListModelEvents(this._threads);

                this._data = data;
                OnPropertyChanged("Title");
            }
        }

        public void SaveThreadListAsync()
        {
            this._threads.SaveThreadList();
        }

        public void RefreshAsync()
        {
            this._threads.RefreshAsync();
        }

        public void LoadDataAsync() { this._threads.LoadThreadsAsync(); }

        private void DetachThreadListModelEvents(ThreadListViewModel model)
        {
            if (model != null)
            {
                model.Items.CollectionChanged -= OnItemCollectionChanged;
                model.ThreadListAppended -= OnThreadListAppended;
                model.ThreadListAppending -= OnThreadListAppending;
                model.ThreadListFirstLoadStart -= OnThreadListFirstLoadStart;
                model.ThreadListFirstLoadFinish -= OnThreadListFirstLoadFinish;
                model.ThreadListRefreshing -= OnThreadListRefreshing;
                model.ThreadListRefreshed -= OnThreadListRefreshed;
                model.ThreadListFailed -= OnThreadListFailed;
            }
        }

      
        private void AttachThreadListModelEvents(ThreadListViewModel model)
        {
            if (model != null)
            {
                model.Items.CollectionChanged += OnItemCollectionChanged;
                model.ThreadListAppended += OnThreadListAppended;
                model.ThreadListAppending += OnThreadListAppending;
                model.ThreadListFirstLoadStart += OnThreadListFirstLoadStart;
                model.ThreadListFirstLoadFinish += OnThreadListFirstLoadFinish;
                model.ThreadListRefreshing += OnThreadListRefreshing;
                model.ThreadListRefreshed += OnThreadListRefreshed;
                model.ThreadListFailed += OnThreadListFailed;
            }
        }

        private void OnItemCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Items");
        }

        private void OnThreadListRefreshed(object sender, EventArgs e)
        {
            if (OnRefreshed != null)
                OnRefreshed();
        }

        private void OnThreadListRefreshing(object sender, EventArgs e)
        {
            
        }

        private void OnThreadListFirstLoadFinish(object sender, EventArgs e)
        {
            this.IsLoading = false;
        }

        private void OnThreadListFirstLoadStart(object sender, EventArgs e)
        {
            this.IsLoading = true;
        }

        private void OnThreadListAppending(object sender, EventArgs e)
        {
            this.IsLoading = true;
        }

        private void OnThreadListAppended(object sender, EventArgs e)
        {
            this.IsLoading = false;
        }

        void OnThreadListFailed(object sender, EventArgs e)
        {
            this.IsLoading = false;
            OnThreadListRefreshed(sender, e);
        }


        private bool _isLoading;
        public bool IsLoading
        {
            get { return this._isLoading; }
            private set { SetProperty(ref _isLoading, value, "IsLoading"); }
        }

        public string Title
        {
            get { return _data.ForumName; }
        }

        public object Items
        {
            get { return _threads.Items; }
        }

        public bool IsDataLoaded { get { return this._threads.IsDataLoaded; } }
    }
}
