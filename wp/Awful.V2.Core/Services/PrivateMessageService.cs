using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections.Generic;

namespace Awful
{
    /// <summary>
    /// TODO: Add summary here.
    /// </summary>
    public class PrivateMessageService
    {
        private bool HasStarted { get; set; }
        private PrivateMessageService() { this.HasStarted = false; }

        #region fields
        private const int SENT_MESSAGE_FOLDERID = -1;
        private const int INBOX_MESSAGE_FOLDERID = 0;
        private const string SEND_MESSAGE_ACTION_VALUE = "dosend";
        private const string NEW_MESSAGE_ACTION_VALUE = "newmessage";
        private const string MOVE_MESSAGE_ACTION_VALUE = "dostuff";
        private const string SEND_MESSAGE_SUBMIT_VALUE = "Send Message";
        private const string MOVE_MESSAGE_SUBMIT_VALUE = "Move";
        private const string DELETE_MESSAGE_SUBMIT_ACTION = "Delete";
        #endregion

        #region static
        internal static PrivateMessageService Service { get; private set; }
        static PrivateMessageService() { Service = new PrivateMessageService(); }
        #endregion

        #region public methods
        public PrivateMessageMetadata FetchMessage(int privateMessageId)
        {
            PrivateMessageMetadata message = null;
            var web = new AwfulWebClient();
            var url = string.Format("{0}/{1}?action=show&privatemessageid={2}", CoreConstants.BASE_URL,
                CoreConstants.PRIVATE_MESSAGE_URI,
                privateMessageId);

            var doc = web.FetchHtml(url);
            if (doc != null)
                message = PrivateMessageParser.ParsePrivateMessageDocument(doc);

            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        public bool SendMessage(IPrivateMessageRequest request)
        {
            string url = string.Format("{0}/{1}", CoreConstants.BASE_URL, CoreConstants.PRIVATE_MESSAGE_URI);
            string postdata = string.Format("prevmessageid={0}&action={1}&forward={2}&touser={3}&title={4}&iconid={5}&message={6}&parseurl=yes&savecopy=yes&submit={7}",
                request.PrivateMessageId,
                SEND_MESSAGE_ACTION_VALUE,
                request.IsForward ? "yes" : string.Empty,
                HttpUtility.UrlEncode(request.To),
                HttpUtility.UrlEncode(request.Subject),
                request.SelectedTag.Value,
                HttpUtility.UrlEncode(request.Body),
                HttpUtility.UrlEncode(SEND_MESSAGE_SUBMIT_VALUE));

            return this.SendPost(postdata, url);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="privateMessageID"></param>
        /// <param name="thisFolderID"></param>
        /// <param name="folderID"></param>
        /// <param name="result"></param>
        public bool MoveMessage(int privateMessageID, int thisFolderID, int folderID)
        {
            List<int> ids = new List<int>();
            ids.Add(privateMessageID);
            return this.MoveMessages(ids, thisFolderID, folderID);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="privateMessageID"></param>
        /// <param name="thisFolderID"></param>
        /// <param name="folderID"></param>
        /// <param name="result"></param>
        public bool DeleteMessage(int privateMessageID, int thisFolderID, int folderID)
        {
            List<int> list = new List<int>();
            list.Add(privateMessageID);
            return this.DeleteMessages(list, thisFolderID, folderID);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="privateMessageIDs"></param>
        /// <param name="thisFolderID"></param>
        /// <param name="folderID"></param>
        /// <param name="result"></param>
        public bool DeleteMessages(IList<int> privateMessageIDs, int thisFolderID, int folderID)
        {
            string deleteAction = string.Format("&delete={0}", DELETE_MESSAGE_SUBMIT_ACTION);
            return this.HandleMessages(privateMessageIDs, thisFolderID, folderID, deleteAction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public ICollection<PrivateMessageFolderMetadata> FetchFolders()
        {
            // pull inbox html from server
            string url = string.Format("{0}/{1}?folderid={2}", CoreConstants.BASE_URL, CoreConstants.PRIVATE_MESSAGE_URI, INBOX_MESSAGE_FOLDERID);
            var web = new AwfulWebClient();
            var doc = web.FetchHtml(url);

            List<PrivateMessageFolderMetadata> folders = null;
            if (doc != null)
            {
                folders = new List<PrivateMessageFolderMetadata>();
                folders.AddRange(PrivateMessageParser.ParseFolderList(doc));
                if (folders.Count > 0)
                {
                    // let's cheat a little bit by parsing the inbox as well...
                    var messages = PrivateMessageParser.ParseMessageList(doc);
                    foreach (var message in messages)
                    {
                        folders[0].Messages.Add(message);
                    }
                }
            }

            return folders;
        }

        /// <summary>
        /// TODO: Add summary here.
        /// </summary>
        /// <param name="result"></param>
        public PrivateMessageFolderMetadata FetchInbox()
        {
            return FetchFolder(INBOX_MESSAGE_FOLDERID);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public PrivateMessageFolderMetadata FetchSentItemsAsync()
        {
           return  FetchFolder(SENT_MESSAGE_FOLDERID);
        }

        /// <summary>
        /// TODO: Add summary here.
        /// </summary>
        /// <param name="folderID"></param>
        /// <param name="result"></param>
        public PrivateMessageFolderMetadata FetchFolder(int folderID)
        {
            string url = string.Format("{0}/{1}?folderid={2}", CoreConstants.BASE_URL, CoreConstants.PRIVATE_MESSAGE_URI, folderID);
            var web = new AwfulWebClient();
            var doc = web.FetchHtml(url);
            PrivateMessageFolderMetadata folder = null;
            if (doc != null)
            {
                folder = PrivateMessageParser.ParsePrivateMessageFolder(doc);
            }

            return folder;
        }

        /// <summary>
        /// TODO : Add summary here.
        /// </summary>
        /// <param name="result"></param>
        public PrivateMessageFolderEditor BeginEditFolderRequest()
        {
            var web = new AwfulWebClient();
            var url = string.Format("{0}/{1}?action=editfolders", CoreConstants.BASE_URL, CoreConstants.PRIVATE_MESSAGE_URI);
            var doc = web.FetchHtml(url);
            PrivateMessageFolderEditor editor = null;
            if (doc != null)
            {
                editor = PrivateMessageParser.ParseEditFolderPage(doc);
            }

            return editor;
        }

        /// <summary>
        /// TODO : Add summary here.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="result"></param>
        public bool SendEditFolderRequest(PrivateMessageFolderEditor request)
        {
            string url = string.Format("{0}/{1}", CoreConstants.BASE_URL, CoreConstants.PRIVATE_MESSAGE_URI);
            string postData = request.GenerateRequestData();
            return this.SendPost(postData, url);
        }

        /// <summary>
        /// TODO: Add summary here. 
        /// </summary>
        /// <param name="result"></param>
        public IPrivateMessageRequest BeginNewMessageRequestAsync()
        {
            var url = string.Format("{0}/{1}?action={2}", CoreConstants.BASE_URL, CoreConstants.PRIVATE_MESSAGE_URI, NEW_MESSAGE_ACTION_VALUE);
            return this.GetMessageRequest(url);
        }

        /// <summary>
        /// TODO: Add summary here.
        /// </summary>
        /// <param name="privateMessageID"></param>
        /// <param name="result"></param>
        public IPrivateMessageRequest BeginReplyToMessageRequest(int privateMessageID)
        {
            var url = string.Format("{0}/{1}?action={2}&privatemessageid={3}",
                CoreConstants.BASE_URL, CoreConstants.PRIVATE_MESSAGE_URI, NEW_MESSAGE_ACTION_VALUE, privateMessageID);
            return this.GetMessageRequest(url);
        }

        /// <summary>
        /// TODO: Add summary here.
        /// </summary>
        /// <param name="privateMessageID"></param>
        /// <param name="result"></param>
        public IPrivateMessageRequest BeginForwardToMessageRequest(int privateMessageID)
        {
            var url = string.Format("{0}/{1}?action={2}&forward=true&privatemessageid={3}",
                CoreConstants.BASE_URL, CoreConstants.PRIVATE_MESSAGE_URI, NEW_MESSAGE_ACTION_VALUE, privateMessageID);
            
            return this.GetMessageRequest(url);
        }

        /// <summary>
        /// TODO: Add summary here.
        /// </summary>
        /// <param name="privateMessageIDs"></param>
        /// <param name="thisFolderID"></param>
        /// <param name="folderID"></param>
        /// <param name="result"></param>
        public bool MoveMessages(IList<int> privateMessageIDs, int thisFolderID, int folderID)
        {
            string submitMove = string.Format("&move={0}", MOVE_MESSAGE_SUBMIT_VALUE);
            return this.HandleMessages(privateMessageIDs, thisFolderID, folderID, submitMove);
        }

        /// <summary>
        /// TODO: Add summary here.
        /// </summary>
        /// <param name="context"></param>
        public void StartService(ApplicationServiceContext context)
        {
            if (!this.HasStarted) { this.HasStarted = true; }
        }

        /// <summary>
        /// TODO: Add summary here.
        /// </summary>
        public void StopService()
        {
            if (this.HasStarted) { this.HasStarted = false; }
        }
        #endregion

        #region private methods

        private bool HandleMessages(IList<int> privateMessageIDs, int thisFolderID, int folderID,
            string action)
        {

            StringBuilder urlBuilder = new StringBuilder();
            // first, create private message id string (privatemessage[id])
            var iterator = privateMessageIDs.GetEnumerator();
            if (iterator.MoveNext() != null)
            {
                urlBuilder.Append(this.FormatPrivateMessageIDUrlString(iterator.Current));
                urlBuilder.Append("=yes");
            }
            while (iterator.MoveNext())
            {
                urlBuilder.Append("&");
                urlBuilder.Append(this.FormatPrivateMessageIDUrlString(iterator.Current));
                urlBuilder.Append("yes");
                iterator.MoveNext();
            }

            // next, add 'dostuff' action
            urlBuilder.AppendFormat("&action={0}", MOVE_MESSAGE_ACTION_VALUE);
            // then this folder id
            urlBuilder.AppendFormat("&thisfolder={0}", thisFolderID);
            // then to folder id
            urlBuilder.AppendFormat("&folderid={0}", folderID);
            // then submit command
            urlBuilder.Append(action);

            string url = string.Format("{0}/{1}", CoreConstants.BASE_URL, CoreConstants.PRIVATE_MESSAGE_URI);
            string postdata = urlBuilder.ToString();

            // submit to something Awful.Core...
            return this.SendPost(postdata, url);
        }

        private bool SendPost(string postData, string url)
        {
            bool result = false;
            var signal = new AutoResetEvent(false);
            var httpRequest = AwfulWebRequest.CreatePostRequest(url);

            //Logger.AddEntry("Send Post start...");

            // Synchronous Post Request!
            var state = httpRequest.BeginGetRequestStream(callback => { signal.Set(); }, httpRequest);
            signal.WaitOne();
            BeginPostRequest(postData, state);

            // Synchronous Post Response!
            var request = state.AsyncState as WebRequest;
            state = request.BeginGetResponse( callback => { signal.Set(); }, request);
            signal.WaitOne();

            result = ProcessPostResponse(state);
            return result;
        }

        private void BeginPostRequest(string postData, IAsyncResult asyncResult)
        {
                //Logger.AddEntry("post data = " + postData);
                HttpWebRequest getRequest = asyncResult.AsyncState as HttpWebRequest;
                using (StreamWriter writer = new StreamWriter(getRequest.EndGetRequestStream(asyncResult)))
                {
                    var postBody = postData;
                    writer.Write(postData);
                    writer.Close();
                }
        }

        private bool ProcessPostResponse(IAsyncResult callback)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)callback.AsyncState;
                HttpWebResponse response = request.EndGetResponse(callback) as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.OK)
                    return true;
                else
                    return false;
            }

            catch (Exception)
            {
                return false;
            }
        }

        private IPrivateMessageRequest GetMessageRequest(string url)
        {
            var web = new AwfulWebClient();
            var doc = web.FetchHtml(url);

            PrivateMessageRequest request = PrivateMessageParser.ParseNewPrivateMessageFormDocument(doc);
            return request;
        }

        private string FormatPrivateMessageIDUrlString(int id)
        {
            return HttpUtility.UrlEncode(string.Format("privatemessage[{0}]", id));
        }

        #endregion
    }
}
