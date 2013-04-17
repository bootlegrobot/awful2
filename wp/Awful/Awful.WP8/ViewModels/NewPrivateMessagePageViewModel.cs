using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Awful.Data;
using System.Windows;

namespace Awful.ViewModels
{
    public delegate void InitCallback(bool success);

    public class NewPrivateMessagePageViewModel : Common.BindableBase
    {
        private const string INIT = "INIT";
        private const string SEND_MESSAGE = "SEND";

        public InitCallback InitCallback { get; set; }

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

        public void LoadMessage(PrivateMessageDataSource message)
        {
            var thread = Common.BackgroundThread<PrivateMessageDataSource>.RunAsync(message,
                LoadTagOptions,
                OnSuccess,
                OnTagLoadError);
        }

        private object LoadTagOptions(PrivateMessageDataSource state)
        {
            PrivateMessageDataSource source = state as PrivateMessageDataSource;
            
            if (source == null)
            {
                // TODO: CREATE NEW PRIVATE MESSAGE
            }

            else
                this._request = IsForward ? source.Metadata.BeginForward() : source.Metadata.BeginReply();
            
            this._tagOptions = this._request.TagOptions.Select(tag => new IconTagDataModel() { Metadata = tag }).ToList();
            this._body = this._request.Body;
            this._subject = this._request.Subject;
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
    }
}
