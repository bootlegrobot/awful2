using System;
using System.Net;
using System.Windows;
using System.Text;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace Awful
{
    public class AwfulDebugger : Common.BindableBase, IDisposable
    {
        public enum Level
        {
            Critical = -1,
            Info,
            Debug,
            Verbose
        };

        public static Level DebugLevel { get; set; }
        private static readonly object LockObject = new object();

        private IsolatedStorageFileStream _logStream;
        private StreamWriter _logWriter;
        private readonly AutoResetEvent _signal;
        private readonly Queue<string> _messageQueue;

        private static AwfulDebugger _Instance;
        private static AwfulDebugger Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new AwfulDebugger();

                return _Instance;
            }
        }

        public static string CurrentFilePath { get; private set; }

        public static void SaveAndDispose()
        {
            Instance.Dispose();
        }

        public static void AddLog(object sender, Level level, string message)
        {
            Instance.AddMessage(sender, level, message);
        }

        public static void AddLog(object sender, Level level, Exception ex)
        {
            Instance.AddMessage(sender, level, ex);
        }

        public static string ViewLog(DateTime date)
        {
            return Instance.LoadLogFrom(date);
        }

        public static void StopLogging()
        {
            try
            {
                AwfulDebugger.SaveAndDispose();
            }

            catch (Exception) { }
        }

        private AwfulDebugger()
        {
            InitializeViewer();
            this._messageQueue = new Queue<string>();
            this._signal = new AutoResetEvent(false);
            ThreadPool.QueueUserWorkItem(ProcessMessages);
        }

        private void InitializeViewer()
        {
#if DEBUG
            Debug.WriteLine("AwfulDebugger: Getting User store...");
#endif
            var storageFile = IsolatedStorageFile.GetUserStoreForApplication();
            var filePath = string.Format("{0}/{1}.log", CoreConstants.LOG_DIRECTORY,
                DateTime.Now.ToString(CoreConstants.LOG_FILE_FORMAT));

            if (!storageFile.DirectoryExists(CoreConstants.LOG_DIRECTORY))
            {
                storageFile.CreateDirectory(CoreConstants.LOG_DIRECTORY);
#if DEBUG
                Debug.WriteLine("AwfulDebugger: Created log directory...");
#endif
            }

#if DEBUG
            Debug.WriteLine("AwfulDebugger: Opening logfile ...");
            Debug.WriteLine(string.Format("File is '{0}'", filePath));
#endif

            this._logStream = storageFile.OpenFile(filePath,
                FileMode.Append, FileAccess.Write, FileShare.Read);

            CurrentFilePath = filePath;

#if DEBUG
            Debug.WriteLine(string.Format("AwfulDebugger: log file size: {0}", _logStream.Length));
#endif

            this._logWriter = new StreamWriter(this._logStream);
        }

        private void ProcessMessages(object state)
        {
            while (true)
            {
                if (this._messageQueue.Count == 0)
                    this._signal.WaitOne();

                else    
                {
                    string message = this._messageQueue.Peek();

                    try
                    {
                        this._logWriter.WriteLine(message);
                        this._logWriter.Flush();
                        lock (LockObject) { message = this._messageQueue.Dequeue(); }
                    }

                    catch (ObjectDisposedException)
                    {
#if DEBUG
                        Debug.WriteLine("Stream was disposed, reinitializing...");
#endif
                        InitializeViewer();
                    }
                }
            }
        }

        private void AddMessage(object sender, Level level, string message)
        {
            if (level > AwfulDebugger.DebugLevel)
                return;

            string formatted = string.Format("[{0}][{1}]<{2}>: {3}",
                DateTime.Now.ToString(CoreConstants.LOG_TIMESTAMP_FORMAT),
                level.ToString(),
                sender.GetType().ToString(),
                message);

#if DEBUG
            Debug.WriteLine(formatted);
#endif

            lock (LockObject) { this._messageQueue.Enqueue(formatted); }
            _signal.Set();
        }

        private void AddMessage(object sender, Level level, Exception ex)
        {
            AddMessage(sender, level, "An Exception of type " + ex.GetType() + " was thrown.");
            AddMessage(sender, level, ex.Message == null ? string.Empty : ex.Message);
            AddMessage(sender, level, "Begin stack trace:");
            AddMessage(sender, level, ex.StackTrace == null ? string.Empty : ex.StackTrace);
            AddMessage(sender, level, "End stack trace.");
        }

        private string LoadLogFrom(DateTime date)
        {
            string filepath = string.Format("{0}/{1}.log",
                CoreConstants.LOG_DIRECTORY,
                date.ToString(CoreConstants.LOG_FILE_FORMAT));

            string fileText = string.Empty;
            var storageFile = IsolatedStorageFile.GetUserStoreForApplication();
            using (var filestream = storageFile.OpenFile(filepath,
                FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(filestream))
                {
                    fileText = reader.ReadToEnd();
                }
            }

            return fileText;
        }

        public void Dispose()
        {
            this._logWriter.Close();
            this._logStream.Close();

#if DEBUG
            Debug.WriteLine("AwfulDebugger: Log file disposed.");
#endif
        }
    }
}
