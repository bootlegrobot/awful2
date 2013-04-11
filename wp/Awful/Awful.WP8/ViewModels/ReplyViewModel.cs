using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows;

namespace Awful.ViewModels
{
    [DataContract]
    public sealed class ReplyViewModelState
    {
        [DataMember]
        public string ThreadId { get; set; }

        [DataMember]
        public string PostId { get; set; }

        [DataMember]
        public bool IsEditRequest { get; set; }

        [DataMember]
        public string Content { get; set; }
    }

    public class ReplyViewModel : Commands.SendThreadResponseCommand
    {
        public ReplyViewModel() : base()
        {
            this.Success += OnRequestSuccess;
            this.Failure += OnRequestFailure;
			this.StateChanged += new System.EventHandler(ReplyViewModel_StateChanged);
            this.LoadFromState();
        }

        private void OnRequestFailure(object sender, EventArgs e)
        {
           
        }

        private void OnRequestSuccess(object sender, EventArgs e)
        {
            Request = null;
        }

        private string _requestType = "reply";
        public string RequestType
        {
            get { return _requestType.ToLower(); }
            set { SetProperty(ref _requestType, value, "RequestType"); }
        }

        private string _text = string.Empty;
        public string Text 
        {
            get { return _text; } 
            set 
            { 
                SetProperty(ref _text, value, "Text"); 
                OnPropertyChanged("Count");
            } 
        }

        public bool IsEnabled
        {
            get { return Request != null && !this.IsRunning; }
        }

        public int Count { get { return _text.Length; } }

        private IThreadPostRequest _request;
        public IThreadPostRequest Request
        {
            get { return _request; }
            set
            {
                if (_request != value)
                {
                    this._request = value;
                    Text = value == null ? string.Empty : value.Content;
                    RequestType = value == null ? string.Empty : value.RequestType.ToString();
                    OnPropertyChanged("IsEnabled");
                }
            }
        }

        public System.Windows.Input.ICommand SendRequestCommand
        {
            get { return this; }
        }

        public void SendThreadRequestAsync()
        {
            if (Request != null && !IsRunning)
            {
                
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Sending thread request.");
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "RequestType: " + RequestType);
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "----------------------BeginMessage--------------------");
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, Text);
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "-----------------------EndMessage---------------------");

                Request.Content = Text;
                SendRequestCommand.Execute(Request);
            }
        }

        /// <summary>
        /// Now deprecated.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="notify"></param>
        public static void SendRequestAsync(IThreadPostRequest request, Action<Uri> notify)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                Uri success = request.Send();
                Deployment.Current.Dispatcher.BeginInvoke(() => notify(success));
            }, null);
        }

        public void CreateNew(string threadId)
        {
            ThreadMetadata thread = new ThreadMetadata() { ThreadID = threadId };
            this.Request = thread.CreateReplyRequest();
        }

        public void SaveCurrentState()
        {
            try
            {
                ReplyViewModelState state = new ReplyViewModelState();
                state.Content = this.Text;
                state.IsEditRequest = this.Request.RequestType == PostRequestType.Edit;
                if (state.IsEditRequest)
                    state.PostId = (this.Request as ThreadPostEditRequest).Post.PostID;
                else
                    state.ThreadId = (this.Request as ThreadReplyRequest).Thread.ThreadID;

                state.SaveToFile("reply.xml");

                Notification.Show("Draft saved.", "Save Draft");
            }

            catch (Exception ex) { AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex); }
        }

        private void LoadFromState()
        {
            ReplyViewModelState state = CoreExtensions.LoadFromFile<ReplyViewModelState>("reply.xml");
            if (state != null)
            {
                IThreadPostRequest request = state.IsEditRequest
                    ? new ThreadPostMetadata() { PostID = state.PostId }.BeginEdit()
                    : new ThreadMetadata() { ThreadID = state.ThreadId }.CreateReplyRequest();

                request.Content = state.Content;
                this.Request = request;
            }
        }

        public void DeleteDraft()
        {
            CoreExtensions.DeleteFileFromStorage("reply.xml");
        }

        private void ReplyViewModel_StateChanged(object sender, System.EventArgs e)
        {
        	// TODO: Add event handler implementation here.
			OnPropertyChanged("IsEnabled");
        }

#if WP7
        /// <summary>
        /// Not supported in Windows Phone OS versions &lt; 8.0.
        /// </summary>
        /// <param name="obj"></param>
        internal void AttachImage(object obj) { }
#endif

#if WP8
        internal async void AttachImage(Microsoft.Phone.Tasks.PhotoResult e)
        {
            this.IsRunning = true;

            try
            {
                List<ImgurUploader.DataUploadItem> items = new List<ImgurUploader.DataUploadItem>();
                items.Add(new DataUploadItem(e));
                var request = ImgurUploader.ImgurUploadRequest.CreateUploadRequest(items, ImgurUploader.ImgurLinkType.Normal);
                var response = await request.UploadAsync();
                var result = response.CreateResult();

                if (result.ToClipboard())
                    Notification.Show("Image link copied to clipboard.", "Success!");

                else
                    Notification.ShowError("Image upload failed.", "Error");
            }

            catch (Exception ex)
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
                Notification.ShowError("Image upload failed.", "Error");
            }

            this.IsRunning = false;
        }
#endif

    }
}
