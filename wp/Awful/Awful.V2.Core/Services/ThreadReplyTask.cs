using System;
using System.Linq;
using System.Net;
using System.ComponentModel;
using System.Threading;
using HtmlAgilityPack;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows;

namespace Awful
{
    public class ThreadReplyTask
    {
        private static readonly ThreadReplyTask instance = new ThreadReplyTask();

        private const int DEFAULT_TIMEOUT = 10000;

        public delegate byte[] FormDataDelegate();

        // responses
        private const string REPLY_ACTION_REQUEST = "postreply";
        private const string SUBMIT_REQUEST = "Submit Reply";
        private const string PARSEURL_REQUEST = "yes";
        private const string MAX_FILE_SIZE_REQUEST = "2097152";

        private const string EDIT_ACTION_REQUEST = "updatepost";
        private const string BOOKMARK_REQUEST = "yes";

        // http request form data
        private const string METHOD = "POST";
        private const string REPLY_BOUNDARY = "----WebKitFormBoundaryYRBJZZBPUZAdxj3S";
        private const string EDIT_BOUNDARY = "----WebKitFormBoundaryksMFcMGBHc3jdB0P";
        private const string REPLY_CONTENT_TYPE = "multipart/form-data; boundary=" + REPLY_BOUNDARY;
        private const string EDIT_CONTENT_TYPE = "multipart/form-data; boundary=" + EDIT_BOUNDARY;
        private const string USERAGENT = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.106 Safari/535.2";
        private readonly string REPLY_HEADER = String.Format("--{0}", REPLY_BOUNDARY);
        private readonly string REPLY_FOOTER = String.Format("--{0}--", REPLY_BOUNDARY);
        private readonly string EDIT_HEADER = String.Format("--{0}", EDIT_BOUNDARY);
        private readonly string EDIT_FOOTER = String.Format("--{0}--", EDIT_BOUNDARY);

        // web client
        private readonly Encoding encoding = Encoding.UTF8;

        struct ThreadReplyData
        {
            public string THREADID;
            public string TEXT;
            public string FORMKEY;
            public string FORMCOOKIE;
        }

        struct PostEditData
        {
            public string POSTID;
            public string TEXT;
        }
  
        public static string Quote(ThreadPostMetadata post) { return instance.QuotePost(post); }

        public static Uri Reply(ThreadMetadata thread, string text) { return instance.ReplyToThread(thread, text); }

        public static string FetchEditText(ThreadPostMetadata post) { return instance.GetEdit(post); }

        public static Uri Edit(ThreadPostMetadata post, string text) { return instance.SendEdit(post, text); }
        
        private string QuotePost(ThreadPostMetadata post)
        {
            var url = string.Format("http://forums.somethingawful.com/newreply.php?action=newreply&postid={0}", post.PostID);
            return BeginGetTextFromWebForm(url);
        }

        private Uri ReplyToThread(ThreadMetadata thread, string text)
        {
            var threadID = thread.ThreadID;
            ThreadReplyData? data = GetReplyData(threadID, text);

            if (data.HasValue)
            {
                /*
                Logger.AddEntry(string.Format("ThreadReplyService - Reply data: {0}|{1}|{2}{3}",
                    data.Value.THREADID,
                    data.Value.TEXT,
                    data.Value.FORMKEY,
                    data.Value.FORMCOOKIE));
                */
                return InitiateReply(data.Value);
            }

            else
            {
                //Logger.AddEntry("ThreadReplyService - ReplyAsync failed on null ThreadReplyData.");
                return null;
            }
        }

        private string GetEdit(ThreadPostMetadata post)
        {
            var url = string.Format("http://forums.somethingawful.com/editpost.php?action=editpost&postid={0}", post.PostID);
            return BeginGetTextFromWebForm(url);
        }

        private Uri SendEdit(ThreadPostMetadata post, string text)
        {
            PostEditData data = new PostEditData() { POSTID = post.PostID, TEXT = text };
            return InitiateEditRequest(data);
        }

        #region Post Replying Methods

        private ThreadReplyData? GetReplyData(string threadID, string text)
        {
            ThreadReplyData reply;
            string url = String.Format("http://forums.somethingawful.com/newreply.php?action=newreply&threadid={0}",
                threadID);

            var web = new AwfulWebClient();
            var doc = web.FetchHtml(url).ToHtmlDocument();

            reply = GetReplyFormInfo(threadID, doc);
            reply.TEXT = text;
            return reply;
        }

        private Uri InitiateReply(ThreadReplyData data, int timeout = DEFAULT_TIMEOUT)
        {
            // create request
            string url = "http://forums.somethingawful.com/newreply.php";
            var request = AwfulWebRequest.CreateFormDataPostRequest(url, REPLY_CONTENT_TYPE);
            if (request == null)
                throw new NullReferenceException("request is not an http request.");

            request.AllowAutoRedirect = true;

            // begin request stream
            var signal = new AutoResetEvent(false);
            var result = request.BeginGetRequestStream(callback => { signal.Set(); }, request);
            signal.WaitOne();

            // process request

            FormDataDelegate replyFormData = () => { return GetReplyMultipartFormData(data); };
            var success = ProcessReplyRequest(result, replyFormData);

            // begin response
            request = result.AsyncState as HttpWebRequest;
            result = request.BeginGetResponse(callback => { signal.Set(); }, request);
            signal.WaitOne(timeout);

            if (!result.IsCompleted)
                throw new TimeoutException();

            // process response and return status value
            Uri redirect = HandleGetResponse(result);
            return redirect;
        }

        private bool ProcessReplyRequest(IAsyncResult asyncResult, FormDataDelegate data)
        {
            byte[] formData = data();
            return HandleGetRequest(asyncResult, formData);
        }

        #region DON'T TOUCH THIS!
        private byte[] GetReplyMultipartFormData(ThreadReplyData data)
        {
            Stream dataStream = new MemoryStream();
            StringBuilder sb = new StringBuilder();

            AddFormDataString(sb, "action", REPLY_ACTION_REQUEST, REPLY_HEADER);
            AddFormDataString(sb, "threadid", data.THREADID, REPLY_HEADER);
            AddFormDataString(sb, "formkey", data.FORMKEY, REPLY_HEADER);
            AddFormDataString(sb, "form_cookie", data.FORMCOOKIE, REPLY_HEADER);
            AddFormDataString(sb, "message", data.TEXT, REPLY_HEADER);
            AddFormDataString(sb, "parseurl", PARSEURL_REQUEST, REPLY_HEADER);
            AddFormDataString(sb, "submit", SUBMIT_REQUEST, REPLY_HEADER);
            sb.AppendLine(REPLY_FOOTER);

            string content = sb.ToString();

            dataStream.Write(encoding.GetBytes(content), 0, content.Length);

            dataStream.Position = 0;
            byte[] formData = new byte[dataStream.Length];
            dataStream.Read(formData, 0, formData.Length);
            dataStream.Close();

            return formData;
        }
        #endregion

        private ThreadReplyData GetReplyFormInfo(string threadID, HtmlDocument doc)
        {
            ThreadReplyData data = new ThreadReplyData();
            data.THREADID = threadID;

            var formNodes = doc.DocumentNode.Descendants("input").ToArray();

            var formKeyNode = formNodes
                .Where(node => node.GetAttributeValue("name", "").Equals("formkey"))
                .FirstOrDefault();

            var formCookieNode = formNodes
                .Where(node => node.GetAttributeValue("name", "").Equals("form_cookie"))
                .FirstOrDefault();

            try
            {
                data.FORMKEY = formKeyNode.GetAttributeValue("value", "");
                data.FORMCOOKIE = formCookieNode.GetAttributeValue("value", "");
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Could not parse newReply form data.");
            }

            return data;
        }

        #endregion

        #region Post Editing Methods

        private Uri InitiateEditRequest(PostEditData data, int timeout = CoreConstants.DEFAULT_TIMEOUT_IN_MILLISECONDS)
        {
            // Logger.AddEntry("ThreadReplyService - EditRequest initiated.");

            string url = "http://forums.somethingawful.com/editpost.php";
            var request = AwfulWebRequest.CreateFormDataPostRequest(url, EDIT_CONTENT_TYPE);
            if (request == null)
                throw new NullReferenceException("request is not an http request.");

            request.AllowAutoRedirect = true;

            // begin request stream
            var signal = new AutoResetEvent(false);
            var result = request.BeginGetRequestStream(callback => { signal.Set(); }, request);
            signal.WaitOne();

            // process request
            FormDataDelegate editFormData = () => { return GetEditMultipartFormData(data); };
            var success = ProcessReplyRequest(result, editFormData);

            // begin response
            request = result.AsyncState as HttpWebRequest;
            result = request.BeginGetResponse(callback => { signal.Set(); }, request);
            signal.WaitOne(timeout);

            if (!result.IsCompleted)
                throw new TimeoutException();

            // process response and return status value
            Uri redirect = HandleGetResponse(result);
            return redirect;
        }

        #region DON'T TOUCH THIS! 
        private byte[] GetEditMultipartFormData(PostEditData data)
        {
            Stream dataStream = new MemoryStream();
            StringBuilder sb = new StringBuilder();

            AddFormDataString(sb, "action", EDIT_ACTION_REQUEST, EDIT_HEADER);
            AddFormDataString(sb, "postid", data.POSTID, EDIT_HEADER);
            AddFormDataString(sb, "message", data.TEXT, EDIT_HEADER);
            AddFormDataString(sb, "parseurl", PARSEURL_REQUEST, EDIT_HEADER);
            AddFormDataString(sb, "bookmark", BOOKMARK_REQUEST, EDIT_HEADER);
            AddFormDataString(sb, "submit", SUBMIT_REQUEST, EDIT_HEADER);
            sb.AppendLine(EDIT_FOOTER);

            string content = sb.ToString();

            dataStream.Write(encoding.GetBytes(content), 0, content.Length);

            dataStream.Position = 0;
            byte[] formData = new byte[dataStream.Length];
            dataStream.Read(formData, 0, formData.Length);
            dataStream.Close();

            return formData;
        }
        #endregion

        #endregion

        private string BeginGetTextFromWebForm(string webUrl)
        {
            var web = new AwfulWebClient();
            var url = webUrl;
            var doc = web.FetchHtml(url).ToHtmlDocument();

            //Logger.AddEntry(string.Format("ThreadReplyServer - Retrieving text from '{0}'.", url));

            string bodyText = GetFormText(doc.DocumentNode);
            return HttpUtility.HtmlDecode(bodyText);
        }

        private string GetFormText(HtmlNode node)
        {
            var textArea = node.Descendants("textarea").FirstOrDefault();
            if (textArea == null) return String.Empty;

            var text = textArea.InnerText;
            return text;
        }

        private bool HandleGetRequest(IAsyncResult result, byte[] formData)
        {
            // Logger.AddEntry("ThreadReplyService - GetRequest initiated.");
            HttpWebRequest webRequest = result.AsyncState as HttpWebRequest;
            using (Stream writer = webRequest.EndGetRequestStream(result))
            {
                writer.Write(formData, 0, formData.Length);
            }

            // Logger.AddEntry("ThreadReplyService - GetRequest successful.");

            return true;
        }

        private Uri HandleGetResponse(IAsyncResult result)
        {
            //Logger.AddEntry("ThreadReplyService - GetResponse initiated.");
            HttpWebRequest webRequest = result.AsyncState as HttpWebRequest;
            HttpWebResponse response = webRequest.EndGetResponse(result) as HttpWebResponse;
            string html = string.Empty;

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string text = reader.ReadToEnd();
                html = text;
                
#if DEBUG
                Debug.WriteLine("Reply Text Response: " + text);
#endif
            }

            // Check if response was successful - Look for redirect data.
            HtmlDocument responseHtml = new HtmlDocument();
            responseHtml.LoadHtml(html);

            HtmlNode responseRedirectMeta = responseHtml.DocumentNode.Descendants("meta")
                .Where(meta => meta.GetAttributeValue("http-equiv", "").Equals("Refresh"))
                .SingleOrDefault();

            // try to parse redirect url
            Uri redirect = null;

            try
            {
                string delim = "URL=";
                string contentValue = responseRedirectMeta.GetAttributeValue("content", "");
                var tokens = contentValue.Split(new string[] { delim }, StringSplitOptions.RemoveEmptyEntries);
                string redirectValue = tokens[1];
                redirectValue = HttpUtility.HtmlDecode(redirectValue);
                redirect = new Uri(string.Format("{0}/{1}", CoreConstants.BASE_URL, redirectValue));
            }

            catch (Exception ex)
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
            }

            return redirect;
        }

        private void AddFormDataString(StringBuilder sb, string name, string data, string header)
        {
            sb.AppendLine(header);
            sb.AppendLine(String.Format("Content-Disposition: form-data; name=\"{0}\"", name));
            sb.AppendLine();
            sb.AppendLine(data);            
        }
    }
}
