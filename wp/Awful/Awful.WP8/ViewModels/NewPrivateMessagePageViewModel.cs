using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awful.Data;
using System.Windows;
using System.Windows.Input;

namespace Awful.ViewModels
{
    public delegate void InitCallback(bool success);

    public class NewPrivateMessagePageViewModel : Common.BindableBase
    {
        private const string INIT = "INIT";
        private const string SEND_MESSAGE = "SEND";

        public NewPrivateMessagePageViewModel()
            : base()
        {
            SelectSmileyCommand = new Commands.RelayCommand(SelectSmiley);
            SelectTagCommand = new Commands.RelayCommand(SelectTag);
        }

        public InitCallback InitCallback { get; set; }

        public ICommand SelectSmileyCommand { get; private set; }
        public ICommand SelectTagCommand { get; private set; }

        private IPrivateMessageRequest _request;
        public IPrivateMessageRequest Request
        {
            get { return _request; }
            set { SetProperty(ref _request, value, "Request"); }
        }

        private string _subject;
        public string Subject
        {
            get { return _subject; }
            set { SetProperty(ref _subject, value, "Subject"); }
        }

        private string _body;
        public string Body
        {
            get { return _body; }
            set { SetProperty(ref _body, value, "Body"); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value, "Title"); }
        }

        private bool _isForward;
        public bool IsForward
        {
            get { return _isForward; }
            set { SetProperty(ref _isForward, value); }
        }

        private List<IconTagDataModel> _tagOptions;
        public List<IconTagDataModel> TagOptions
        {
            get { return _tagOptions; }
            set { SetProperty(ref _tagOptions, value, "TagOptions"); }
        }

        private IconTagDataModel _selectedTag = null;
        public IconTagDataModel SelectedTag
        {
            get { return _selectedTag; }
            set 
            {
                if (SetProperty(ref _selectedTag, value, "SelectedTag"))
                    OnPropertyChanged("SelectedTagLabel");
            }
        }

        public string SelectedTagLabel
        {
            get
            {
                return _selectedTag == null
                    ? "No Tag Selected"
                    : _selectedTag.Tag;
            }
        }

        private int _selectedPivotIndex = 0;
        public int SelectedPivotIndex
        {
            get { return _selectedPivotIndex; }
            set { SetProperty(ref _selectedPivotIndex, value, "SelectedPivotIndex"); }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get { return _isRunning; }
            private set { SetProperty(ref _isRunning, value, "IsRunning"); }
        }

        private string _to;
        public string To
        {
            get { return _to; }
            set 
            { 
                SetProperty(ref _to, value, "To"); 
            }
        }

        /// <summary>
        /// Send the selected smiley to the clipboard and turn back to the message pivot.
        /// </summary>
        /// <param name="state"></param>
        private void SelectSmiley(object state)
        {
            SmileyDataModel item = state as SmileyDataModel;
            Clipboard.SetText(item.Code);
            SelectedPivotIndex = 0;
        }

        /// <summary>
        /// Send the selected code tag to the clipboard and turn back to the message pivot.
        /// </summary>
        /// <param name="state"></param>
        private void SelectTag(object state)
        {
            CodeTagDataModel item = state as CodeTagDataModel;
            Clipboard.SetText(item.Code);
            SelectedPivotIndex = 0;
        }

        public void LoadMessage(PrivateMessageDataSource message)
        {
            var thread = Common.BackgroundThread<PrivateMessageDataSource>.RunAsync(message,
                LoadTagOptions,
                OnSuccess,
                OnTagLoadError);
        }

        public void CreateNewMessage()
        {
            LoadMessage(null);
        }

        private object LoadTagOptions(PrivateMessageDataSource state)
        {
            PrivateMessageDataSource source = state as PrivateMessageDataSource;
            if (source == null)
            {
                // TODO: CREATE NEW PRIVATE MESSAGE
                var request = MainDataSource.Instance.CurrentUser.Metadata.CreateNewPrivateMessage();
                this._request = request;
            }

            else
                this._request = IsForward ? source.Metadata.BeginForward() : source.Metadata.BeginReply();
            
            this._tagOptions = this._request.TagOptions.Select(tag => new IconTagDataModel() { Metadata = tag }).ToList();
            this._tagOptions.Insert(0, IconTagDataModel.NoIcon);
            this._body = this._request.Body;
            this._subject = this._request.Subject;
            this._to = this._request.To;
            return true;
        }

        private void OnSuccess(object arg)
        {
            OnPropertyChanged("TagOptions");
            OnPropertyChanged("Subject");
            OnPropertyChanged("Body");
            InitCallback(true);
        }

        private void OnTagLoadError(Exception ex)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
            MessageBox.Show("Could not load message data; please try again.", "Error", MessageBoxButton.OK);
            InitCallback(false);
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
                items.Add(new ImgurUploader.DataUploadItem(e));
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

        internal void SendMessage()
        {
            if (IsRunning)
                return;

            var response = MessageBox.Show("Send message?", "Confirm", MessageBoxButton.OKCancel);
            if (response == MessageBoxResult.OK)
            {
                IsRunning = true;
                Common.BackgroundThread<IPrivateMessageRequest>.RunAsync(this.Request,
                    SendMessageAsync,
                    OnSendMessageAsyncSuccess,
                    OnSendMessageAsyncFailure);
            }
        }

        private object SendMessageAsync(IPrivateMessageRequest request)
        {
            request.To = this.To;
            request.Subject = this.Subject;
            request.Body = this.Body;
            request.SelectedTag = this.SelectedTag == null ?
                TagMetadata.NoTag :
                this.SelectedTag.Metadata;

            return request.Send();
        }

        private void OnSendMessageAsyncFailure(Exception ex)
        {
            IsRunning = false;
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
            Notification.ShowError("Send failed.", "Failure");
        }

        private void OnSendMessageAsyncSuccess(object arg)
        {
            IsRunning = false;
            bool success = (bool)arg;
            if (success)
                Notification.Show("Message sent.", "Success!");
            else
                Notification.ShowError("Send failed.", "Failure");
        }
    }
}
