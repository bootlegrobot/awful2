using System;
using System.Linq;
using System.IO.IsolatedStorage;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Threading;
using System.Net;
using System.Windows.Controls;
using System.Windows.Shapes;
using Telerik.Windows.Data;
using Microsoft.Phone.Shell;

namespace Awful.ViewModels
{
    public class LoggingViewModel : ListViewModel<LogViewModel>
    {
        private int _index = -1;
        private LogViewModel _item = null;

        private IApplicationBar _appBar;
        public IApplicationBar AppBar
        {
            get { return _appBar; }
            set { _appBar = value; }
        }
       
        public LoggingViewModel() : base() 
        {
            LogViewModel.Directory = CoreConstants.LOG_DIRECTORY;
        }

        public void ScrollCurrentLogToTop()
        {
            var current = SelectedItem;
            if (current != null && current.Content != null)
                current.SelectedItem = current.Content.First();
        }

        public void ScrollCurrentLogToBottom()
        {
            var current = SelectedItem;
            if (current != null && current.Content != null)
                current.SelectedItem = current.Content.Last();
        }

        public bool DeleteCurrentLog()
        {
            var current = SelectedItem;
            if (SelectedItem != null)
                return SelectedItem.DeleteLog();

            return false;
        }

        public override void Refresh()
        {
            this.SelectedItem = null;
            this.SelectedItemIndex = -1;
            this.LoadDataAsync();
        }

        protected override void OnError(Exception exception)
        {
            //throw new NotImplementedException();
        }

        protected override void OnCancel()
        {
            //throw new NotImplementedException();
        }

        protected override System.Collections.Generic.IEnumerable<LogViewModel> LoadDataInBackground()
        {
            // stop debugger to read current log file
            AwfulDebugger.StopLogging();

            // initialize
            List<LogViewModel> items = new List<LogViewModel>();
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            var files = storage.GetFileNames(CoreConstants.LOG_DIRECTORY + "/*");
            foreach (var file in files)
                items.Add(new LogViewModel{ Filename = file });

            return items;
        }

        public int SelectedItemIndex
        {
            get { return _index; }
            set
            {
                SetProperty(ref _index, value, "SelectedItemIndex");
                if (value != -1 || Items.IsNullOrEmpty())
                {
                    try { SelectedItem = Items[value]; }
                    catch (ArgumentOutOfRangeException)
                    {
                        SelectedItem = null;
                    }
                }
            }
        }

        public LogViewModel SelectedItem
        {
            get
            {
                return _item;
            }
            set { SetProperty(ref _item, value, "SelectedItem"); }
        }

        protected override void OnSuccess()
        {
           
        }
    }

    public sealed class LoggingItem : Common.BindableBase
    {
        private string _filename = string.Empty;
        private string _directory = string.Empty;
        private readonly object _lockObject = new object();
        private readonly VirtualizingDataCollection _collection;
        private bool _isLoaded;
        private static IsolatedStorageFile storage;
        private List<string> _textBuffer;

        public string Filename { 
            get { return this._filename; }
            set { SetProperty(ref _filename, value, "Filename"); }
        }

        public bool IsLoaded
        {
            get { return _isLoaded; }
            private set { SetProperty(ref _isLoaded, value, "IsLoaded"); }
        }

        public string Directory
        {
            get { return this._directory; }
        }

        static LoggingItem() { storage = IsolatedStorageFile.GetUserStoreForApplication(); }

        public LoggingItem(string directory)
        {
            _directory = directory;
            _collection = new VirtualizingDataCollection(200, 10);
            _collection.ItemsLoading += new EventHandler<VirtualizingDataCollectionItemsLoadingEventArgs>(OnItemsLoading);
            _textBuffer = new List<string>(200);
        }

        void OnItemsLoading(object sender, VirtualizingDataCollectionItemsLoadingEventArgs e)
        {
            int startIndex = e.StartIndex;
            int count = e.Count;
            LoadItems(startIndex, count);
        }

        private void LoadItems(int startIndex, int count)
        {
            List<string> list = new List<string>(count);
            int index = startIndex;
            int bufferLength = _textBuffer.Count;
            while (index < bufferLength)
            {
                string line = _textBuffer[index];
                if (line != null)
                    list.Add(line);

                index++;
            }

            _collection.LoadItems(startIndex, list);
        }

        public VirtualizingDataCollection LoadTextAsync()
        {
            var path = string.Format("{0}/{1}", this.Directory, this.Filename);
            if (!IsLoaded)
            {
                ThreadPool.QueueUserWorkItem(state =>
                    {
                        try { ReadTextFromFile(state as string); }
                        catch (IsolatedStorageException)
                        {
                            AwfulDebugger.SaveAndDispose();
                            ReadTextFromFile(state as string);
                        }
                    }, path);
            }

            return this._collection;
        }

        private void ReadTextFromFile(object path)
        {
            string filepath = path as string;
            using (var stream = storage.OpenFile(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    string line = null;
                    do
                    {
                        line = reader.ReadLine();
                        lock (_lockObject) { _textBuffer.Add(line); }
                    }
                    while (line != null);
                    this.IsLoaded = true;
                    Deployment.Current.Dispatcher.BeginInvoke(() => { this._collection.Count = _textBuffer.Count; });
                }
            }
        }

        public override string ToString()
        {
            return Filename;
        }
    }

    public sealed class LogViewModel : Commands.BackgroundWorkerCommand<string>
    {
        private string _filename = string.Empty;
        private static string directory = string.Empty;
        private List<string> _content;
        private string _selectedItem = null;

        public LogViewModel() : base() {  }

        public static string Directory
        {
            get { return directory; }
            set { directory = value; }
        }

        public string Filename
        {
            get { return _filename; }
            set { _filename = value; }
        }

        public string FilePath
        {
            get { return Directory + "/" + Filename; }
        }

        public List<string> Content
        {
            get
            {
                // if content is null (file hasn't been loaded)
                // begin loading thread
                if (_content == null)
                    this.Execute(this.FilePath);

                return _content;
            }
            set { SetProperty(ref _content, value, "Content"); }
        }

        public string SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value, "SelectedItem"); }
        }

        public bool DeleteLog()
        {
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            bool value = false;
            try 
            { 
                storage.DeleteFile(this.FilePath);
                value = !storage.FileExists(this.FilePath);
            }

            catch (Exception) { }

            return value;
        }

        public override string ToString()
        {
            return this.Filename;
        }

        protected override object DoWork(string parameter)
        {
            UpdateStatus("Loading file...");

            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            var list = new List<string>(10000);
            using (Stream fs = storage.OpenFile(parameter, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (StreamReader fsReader = new StreamReader(fs))
                {
                    string text = string.Empty;
                    while ((text = fsReader.ReadLine()) != null)
                        list.Add(text);
                }
            }

            return list;
        }

        protected override void OnError(Exception ex)
        {
            UpdateStatus("Load failed.");

            // notify the user that loading the file failed.
            Notification.ShowError(NotificationMethod.MessageBox,
                string.Format("Could not open the log file '{0}'.", this.Filename),
                "Error");
        }

        protected override void OnCancel()
        {
            UpdateStatus(string.Empty);

            // empty content buffer for reloading on next binding query
            this._content = null;
        }

        protected override void OnSuccess(object arg)
        {
            var list = arg as List<string>;
            if (list != null)
                this.Content = list;
        }

        protected override bool PreCondition(string item)
        {
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            return storage.FileExists(this.FilePath);
        }
    }
}
