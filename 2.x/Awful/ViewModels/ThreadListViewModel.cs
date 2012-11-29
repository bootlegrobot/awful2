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
using Telerik.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Awful.Data;

namespace Awful.ViewModels
{
    /// <summary>
    /// The ThreadListViewModel ties the logic behind forum page retrieval and thread list presentation on the UI.
    /// ThreadListControl expects a data context of type ThreadListViewModel!
    /// </summary>
    public class ThreadListViewModel : Common.BindableBase
    {
        /// <summary>
        /// Creates a new ThreadListViewModel.
        /// </summary>
        /// <param name="metadata">The metadata which listed threads will come from.</param>
        public ThreadListViewModel(ForumMetadata metadata)
        {
            // if no metadata is supplied, go to jail, collect $200...
            if (metadata == null)
                throw new ArgumentNullException("Forum metadata must not be null!");

            // Set the metadata
            this._metadata = metadata;
            // Set the first run to be true
            this._firstRun = true;

            // Initialize the background thread
            this._worker = new BackgroundWorker();
            this._worker.WorkerReportsProgress = true;
            this._worker.WorkerSupportsCancellation = true;
            this._worker.DoWork += new DoWorkEventHandler(OnWorkerDoWork);
            this._worker.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
            this._worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnWorkerWorkCompleted);

            // Initialize current page to the first page of the forum.
            this._currentPage = -1;
        }

        //private bool _suppressThreadsUpdateNotification;
        
        private ForumMetadata _metadata;    // Current forum metadata. Threads from this forum will be listed on the ui.
        private int _currentPage;           // Current page of the forum. Data requests will increment this variable.
        private BackgroundWorker _worker;   // Background thread for fetching metadata from the web.
        private bool _firstRun;             // first run flag (for automatic data loading)
        private delegate void ThreadListUpdater(IEnumerable<ThreadDataModel> threads);  // delegate for work involving UI updates.
        private delegate IEnumerable<ThreadDataModel> ThreadFetchAction(ForumMetadata metadata); // delegate for work involving thread list retrieval.

        /// <summary>
        /// Helper struct for wrapping object necessary for carrying out a thread list task.
        /// </summary>
        private class ThreadListEventProcessor
        {
            public IEnumerable<ThreadDataModel> Threads { get; private set; }
            public ThreadListUpdater Updater { get; set; }
            public ThreadFetchAction Fetcher { get; set; }
            public void SetThreads(IEnumerable<ThreadDataModel> threads) { this.Threads = threads; }
        }

        #region Properties

        public bool IsDataLoaded
        {
            get { return this._firstRun == false; }
        }

        private ObservableCollection<ThreadDataModel> _threads;
        /// <summary>
        /// The main thread list. Users interact with this list on the UI.
        /// </summary>
        public ObservableCollection<ThreadDataModel> Items
        {
            get
            {
                if (this._threads == null)
                {
                    this._threads = new ObservableCollection<ThreadDataModel>();
                    this._threads.CollectionChanged += 
                        new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnThreadsCollectionChanged);
                }

                return this._threads;
            }
        }

        /// <summary>
        /// Updates the UI when the collection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnThreadsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged("Items");
        }

        #endregion 

        #region Events and Event Handlers

        /// <summary>
        /// Fired when a Refresh request has been acknowledged.
        /// </summary>
        public event EventHandler ThreadListRefreshing;
        /// <summary>
        /// Fired when the Refresh request has completed, failure or success.
        /// </summary>
        public event EventHandler ThreadListRefreshed;
        /// <summary>
        /// Fireed when the request for additional threads has been acknowledged.
        /// </summary>
        public event EventHandler ThreadListAppending;
        /// <summary>
        /// Fired when the request for additional threads has been completed, failure or success.
        /// </summary>
        public event EventHandler ThreadListAppended;
        /// <summary>
        /// Fired when threads are pulled for the first time. Listen to this event if you want to
        /// let users know that for some reason.
        /// </summary>
        public event EventHandler ThreadListFirstLoadStart;
        /// <summary>
        /// Fired when the thread list is populated for the first time.
        /// </summary>
        public event EventHandler ThreadListFirstLoadFinish;
        /// <summary>
        /// Fired when the thread list fails a fetch.
        /// </summary>
        public event EventHandler ThreadListFailed;

        /// <summary>
        /// Fires the ThreadListFirstLoadStart event.
        /// </summary>
        private void OnThreadListFirstLoadStart()
        {
            if (ThreadListFirstLoadStart != null)
                ThreadListFirstLoadStart(this, null);
        }

        /// <summary>
        /// Appends specified threads to the thread list, and notifies users. 
        /// Only fired on first load.
        /// </summary>
        /// <param name="threads">the list of threads to append.</param>
        private void OnThreadListFirstLoadFinish(IEnumerable<ThreadDataModel> threads)
        {
            if (threads != null)
            {
                // mark the first run as complete
                this._firstRun = false;
                // append the threads
                foreach (var item in threads)
                    this.Items.Add(item);
            }

            else
            {
                // TODO: Add message of failure here.
            }

            // Notify listeners
            if (ThreadListFirstLoadFinish != null)
                ThreadListFirstLoadFinish(this, null);
        }

        /// <summary>
        /// Pretty straightforward.
        /// </summary>
        private void OnThreadListRefreshing()
        {
            if (ThreadListRefreshing != null)
                ThreadListRefreshing(this, null);
        }

        /// <summary>
        /// Pretty straightforward.
        /// </summary>
        private void OnThreadListFailed()
        {
            if (ThreadListFailed != null)
                ThreadListFailed(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clears the Thread list and refreshes with new lists from the forum's first page.
        /// MUST BE RUN ON UI THREAD. 
        /// TODO: Notify user when load fails.
        /// </summary>
        /// <param name="threads">List of thread view models which represent threads.</param>
        private void OnThreadListRefreshed(IEnumerable<ThreadDataModel> threads)
        {
            if (threads != null)
            {
                // first, clear the list
                this.Items.Clear();
                // then add the new threads to the thread list
                foreach (var thread in threads)
                    this.Items.Add(thread);
            }

            // last, notify any listeners that the thread list has been refreshed
            if (ThreadListRefreshed != null)
                ThreadListRefreshed(this, null);
        }

        /// <summary>
        /// Fires the ThreadListAppending event.
        /// </summary>
        private void OnThreadListAppending()
        {
            if (ThreadListAppending != null)
                ThreadListAppending(this, null);
        }

        /// <summary>
        /// Appends supplied thread items to the main thread list and
        /// notifies listeners on completion.
        /// </summary>
        /// <param name="threads">The threads to be appended.</param>
        private void OnThreadListAppended(IEnumerable<ThreadDataModel> threads)
        {
            if (threads != null)
            {
                // append the threads in...threads to the main thread list
                foreach (var item in threads)
                {
                    int index = this.Items.IndexOf(item);
                    if (index == -1)
                        this.Items.Add(item);
                    else
                    {
                        this.Items.RemoveAt(index);
                        this.Items.Insert(index, item);
                    }
                }
            }

            else
            {
                // TODO: Notify user that the append failed.
            }

            // Notify UI that the task is complete
            if (ThreadListAppended != null)
                ThreadListAppended(this, null);
        }

        #endregion

        /// <summary>
        /// Creates viewmodels from a collection of thread metadata.
        /// </summary>
        /// <param name="threads">a collection of metadata representing forum threads.</param>
        /// <returns>A list of thread item viewmodels for presenting to the ui; 
        /// null if the collection is null or empty.</returns>
        private IEnumerable<ThreadDataModel> CreateItemViewModels(IEnumerable<ThreadMetadata> threads)
        {
            if (threads == null)
                return null;

            var list = new List<ThreadDataModel>(100);
            foreach (var item in threads)
                list.Add(new ThreadDataModel(item));

            return list.IsNullOrEmpty() ? null : list;
        }

        /// <summary>
        /// Creates viewmodels from forum page metadata.
        /// </summary>
        /// <param name="metadata">metadata representing the target forum page.</param>
        /// <returns>A list of thread item viewmodels for presenting to the ui; null if the metadata is also null.</returns>
        private IEnumerable<ThreadDataModel> CreateItemViewModels(ForumPageMetadata metadata)
        {
            if (metadata == null) return null;
            return CreateItemViewModels(metadata.Threads);
        }

        /// <summary>
        /// Retrieves threads from the first page of the forum. If successful, the current page
        /// is set to the first page.
        /// </summary>
        /// <param name="metadata">Metadata from the target forum.</param>
        /// <returns>A list of thread item viewmodels from the result forum page metadata; null on failure.</returns>
        private IEnumerable<ThreadDataModel> FetchThreadsOnFirstPage(ForumMetadata metadata)
        {
            // try and get the page metadata from the web.
            // Best to run this in the background, lest you freeze the UI thread.
            // TODO : develop a check for total pages and add to forum metadata
            var pageMetadata = metadata.LoadPage(1);

            if (pageMetadata != null)
                this._currentPage = 1;

            return CreateItemViewModels(pageMetadata);
        }

        /// <summary>
        /// Retrieves threads from storage, or the first page of the forum. If successful, the current page
        /// is set:
        /// - to -1 on storage load,
        /// - to  1 on first page load.
        /// </summary>
        /// <param name="metadata">Metadata from the target forum.</param>
        /// <returns>A list of thread item viewmodels from the result forum page metadata; null on failure.</returns>
        private IEnumerable<ThreadDataModel> FetchThreadsOnFirstLoad(ForumMetadata metadata)
        {
            if (this._firstRun)
            {
                // check our iso store for existing threads
                var threads = LoadFromIsoStore(metadata);
                this._currentPage = -1;

                // if that fails, just load the first page
                if (threads == null)
                    threads = FetchThreadsOnFirstPage(metadata);

                return threads;
            }

            throw new Exception("First load should not run more than once!");
        }

        /// <summary>
        /// Retrieves threads from the next page of the forum. If successful, 
        /// the current page is set to the loaded page.
        /// </summary>
        /// <param name="metadata">Metadata from the target forum.</param>
        /// <returns>A list of thread item viewmodels from the resulting forum page metadata; null on failure.</returns>
        private IEnumerable<ThreadDataModel> FetchThreadsOnNextPage(ForumMetadata metadata)
        {
            // THIS IS SLOPPY, SO I WANT TO FIX IT SOMEHOW...
            if (metadata is BookmarkMetadata)
                return null;

            // increment the new page
            int newPage = this._currentPage + 1;
            // try and get the page metadata from the web
            // Best to run this in the background, lest you freeze the UI thread.
            // TODO : develop a check for total pages and add to forum metadata
            var pageMetadata = metadata.LoadPage(newPage);

            if (pageMetadata != null)
                this._currentPage = newPage;

            return CreateItemViewModels(pageMetadata);
        }

        /// <summary>
        /// Retrieves a thread listing from SA Servers. The list is generated from 
        /// ForumPage metadata.
        /// </summary>
        private void FetchDataFromSA(ThreadFetchAction fetchTask, ThreadListUpdater updateTask)
        {
            if (!this._worker.IsBusy)
            {
                // Let the background worker do it.
                var task = new ThreadListEventProcessor();
                task.Fetcher = fetchTask;
                task.Updater = updateTask;

                this._worker.RunWorkerAsync(task);
            }

            else
            {
                // TODO: Notify users that a task is running, and wait for it to finish.
            }
        }

        /// <summary>
        /// Clears the thread list and pulls threads from the first page of the forum.
        /// </summary>
        public void RefreshAsync()
        {
            // Notify listeners that the thread list is refreshing
            OnThreadListRefreshing();
            // Fetch the data from SA (asynchronous command)
            FetchDataFromSA(FetchThreadsOnFirstPage, OnThreadListRefreshed);
        }

        /// <summary>
        /// Appends threads from the next forum page to the thread list.
        /// </summary>
        public void LoadThreadsAsync()
        {
            // If this is the first load...
            if (!this.IsDataLoaded)
            {
                // Notify.
                OnThreadListFirstLoadStart();
                // Work.
                FetchDataFromSA(FetchThreadsOnFirstLoad, OnThreadListFirstLoadFinish);
            }
            else
            {
                // Notify listeners that we are about to append to the list
                OnThreadListAppending();
                // Do some work!
                FetchDataFromSA(FetchThreadsOnNextPage, OnThreadListAppended);
            }
        }

        public void SaveThreadList()
        {
            var list = new List<ThreadMetadata>(this.Items.Count);
            foreach (var item in this.Items)
                list.Add(item.Metadata);

            ThreadPool.QueueUserWorkItem(SaveToIsoStore, list);
        }

        private void SaveToIsoStore(object state)
        {
            var list = state as IEnumerable<ThreadMetadata>;
            list.SaveToFile(string.Format("{0}.xml", this._metadata.ForumID));
        }

        private IEnumerable<ThreadDataModel> LoadFromIsoStore(ForumMetadata metadata)
        {
            string file = string.Format("{0}.xml", metadata.ForumID);
            IEnumerable<ThreadMetadata> list = CoreExtensions.LoadFromFile<IEnumerable<ThreadMetadata>>(file);
            this._currentPage = 0;
            return CreateItemViewModels(list);
        }

        /// <summary>
        /// Cancels any current job running. Currently not in use.
        /// </summary>
        /// <param name="task">Current task running. Lets the task update the UI as required.</param>
        private void CancelJob(ThreadListEventProcessor task)
        {
            // do it on the UI thread
            if (this._worker.CancellationPending)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => { task.Updater(null); });
            }
        }

        #region Background Worker methods

        /// <summary>
        /// Callback when the background worker finishes its work.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWorkerWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // first, check if the task was cancelled or not
            if (e.Cancelled)
            {
                // TODO: Add a message here maybe.
            }

            // print the error message out, if there is one
            else if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
                OnThreadListFailed();
            }

            else
            {
                // do whatever post processing needed to be done here.
                ThreadListEventProcessor task = (ThreadListEventProcessor)e.Result;
                if (task != null)
                    task.Updater(task.Threads);
            }
        }

        /// <summary>
        /// Callback when updates to progress are requested in the background thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // TODO: Maybe add some status message so user isn't kept in the dark.
        }

        /// <summary>
        /// Callback when work is initiated on the UI thread. This method runs on a separate thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            ThreadListEventProcessor task = (ThreadListEventProcessor)e.Argument;
            if (task != null)
            {
                IEnumerable<ThreadDataModel> threads = null;
                
                // fetch the threads
                threads = task.Fetcher(this._metadata);
              
              
                // if the job was cancelled along the way, report it
                if (this._worker.CancellationPending)
                {
                    e.Cancel = true;
                    this.CancelJob(task);
                }
                else
                {
                    // or set the threads for processing in the ui thread
                    task.SetThreads(threads);
                    e.Result = task;
                }
            }
        }

        #endregion
    }
}
