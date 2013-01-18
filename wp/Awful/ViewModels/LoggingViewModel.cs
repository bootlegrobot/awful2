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

namespace Awful.ViewModels
{
    public class LoggingViewModel : ListViewModel<LoggingItem>
    {
        private int _index = -1;
        private LoggingItem _item = null;
       
        public LoggingViewModel() : base() 
        {
            
        }
       
        protected override void OnError(Exception exception)
        {
            //throw new NotImplementedException();
        }

        protected override void OnCancel()
        {
            //throw new NotImplementedException();
        }

        protected override System.Collections.Generic.IEnumerable<LoggingItem> LoadDataInBackground()
        {
            List<LoggingItem> items = new List<LoggingItem>();
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            var files = storage.GetFileNames(CoreConstants.LOG_DIRECTORY + "/*");
            foreach (var file in files)
                items.Add(new LoggingItem(CoreConstants.LOG_DIRECTORY) { Filename = file });

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
                    SelectedItem = Items[value];
                }
            }
        }

        public LoggingItem SelectedItem
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

    public sealed class LoggingItem1 : Common.BindableBase
    {
        public delegate void LogContentDelegate(object content);
        private string _filename = string.Empty;
        private object _content = null;
        private string _dir = string.Empty;
        private BackgroundWorker _worker = new BackgroundWorker();

        public LoggingItem1(string directory) 
        { 
            _dir = directory;
            _worker.DoWork += new DoWorkEventHandler(OnDoWork);
            _worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnWorkCompleted);
        }

        public void LoadContentAsync(LogContentDelegate view)
        {
            if (this.IsContentLoaded)
                view(this.Content);

            else if (!this._worker.IsBusy)
            {
                this._worker.RunWorkerAsync(view);
            }
        }

        void OnWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, ":(", MessageBoxButton.OK);
            }

            else if (!e.Cancelled && e.Error == null)
            {
                LogContentDelegate ViewContent = e.Result as LogContentDelegate;
                ViewContent(this.Content);
            }
        }

        void OnDoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = e.Argument as LogContentDelegate;
            var path = string.Format("{0}/{1}", this.Directory, this.Filename);
            try
            {
                ReadTextFromFile(path);
            }
            catch (IsolatedStorageException ex)
            {
                AwfulDebugger.SaveAndDispose();
                ReadTextFromFile(path);
            }

            catch (Exception)
            {
                throw new Exception("Could not load the target file.");
            }
        }

        private void ReadTextFromFile(string path)
        {
            var store = IsolatedStorageFile.GetUserStoreForApplication();
            using (var stream = store.OpenFile(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    StackPanel panel = new StackPanel();
                    string line;
                    do
                    {
                        line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line))
                        {
                            panel.Children.Add(new Rectangle { Height = 20.0 });
                        }
                        else
                        {
                            var textBlock = new TextBlock
                            {
                                TextWrapping = TextWrapping.Wrap,
                                Text = line,
                                FontSize = 22,
                            };
                            panel.Children.Add(textBlock);
                        }
                    } while (line != null);

                    this._content = panel;
                  
                    // Gives the UI a bit of breathing room.
                    Thread.Sleep(500);
                }
            }
        }

        public string Directory { get { return this._dir; } }

        public string Filename
        {
            get { return _filename; }
            set
            {
                SetProperty(ref _filename, value, "Filename");
            }
        }

        public object Content
        {
            get 
            {
                return _content;
            }
            set
            {
                SetProperty(ref _content, value, "Content");
            }
        }

        public bool IsContentLoaded
        {
            get { return this._content != null; }
        }

        public override string ToString()
        {
            return Filename;
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
}
