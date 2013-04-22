using System;
using System.Threading;
using System.Windows;
using System.Net;
using System.IO;
using Awful;

namespace Awful.Deprecated
{
    public interface ThreadService
    {
        bool AddBookmark(ThreadMetadata data);
        bool RemoveBookmark(ThreadMetadata data);
        bool Rate(ThreadMetadata data, int rating);
        bool Reply(ThreadMetadata data, string message);
        string Quote(ThreadPostMetadata data);
        bool RunURLTask(string url);
        bool RunURLTask(Uri uri);
    }

    public static class ThreadTasks
    {
        public static bool MarkAsLastRead(ThreadPostMetadata post)
        {
            return RunURLTask(post.MarkPostUri);
        }

        public static void MarkAsLastReadAsync(ThreadPostMetadata post, Action<bool> predicate)
        {
            ThreadPool.QueueUserWorkItem((markedPost) =>
                {
                    bool marked = MarkAsLastRead(markedPost as ThreadPostMetadata);
                    predicate(marked);
                }, post);
        }

        public static void QuoteAsync(ThreadPostMetadata post, Action<string> predicate)
        {
            ThreadPool.QueueUserWorkItem((markedPost) =>
            {
                string quote = Quote(markedPost as ThreadPostMetadata);
                predicate(quote);
            }, post);
        }

        public static bool AddBookmark(ThreadMetadata data){ return ThreadBookmarkTask.Bookmark(data, BookmarkAction.Add); }
        public static bool RemoveBookmark(ThreadMetadata data) { return ThreadBookmarkTask.Bookmark(data, BookmarkAction.Remove); }
        public static string FetchEditText(ThreadPostMetadata post) { return ThreadReplyTask.FetchEditText(post); }
        public static Uri Edit(ThreadPostMetadata post, string text) { return ThreadReplyTask.Edit(post, text); }
        public static Uri Reply(ThreadMetadata data, string message) { return ThreadReplyTask.Reply(data, message); }
        public static string Quote(ThreadPostMetadata post) { return ThreadReplyTask.Quote(post); }

        public static bool Rate(ThreadMetadata data, int rating)
        {
            var url = string.Format("http://forums.somethingawful.com/threadrate.php?vote={0}&threadid={1}",
                rating, data.ThreadID);

            return RunURLTask(url);
        }

        #region Clear Marked Posts

        public static bool ClearMarkedPosts(ThreadMetadata thread, int timeout = CoreConstants.DEFAULT_TIMEOUT_IN_MILLISECONDS)
        {
            // create request
            HttpWebRequest request = AwfulWebRequest.CreateFormDataPostRequest(
                "http://forums.somethingawful.com/showthread.php", 
                "application/x-www-form-urlencoded");
            
            // begin request stream creation and wait...
            var signal = new AutoResetEvent(false);
            var result = request.BeginGetRequestStream(callback =>
                SendClearMarkedPostRequest(callback, signal, thread), 
                request);

            signal.WaitOne();

            // begin response stream and wait...
            request = result.AsyncState as HttpWebRequest;
            result = request.BeginGetResponse(callback => { signal.Set(); }, request);
            signal.WaitOne(timeout);

            if (!result.IsCompleted)
                throw new TimeoutException();

            // process the response and return status
            bool success = ProcessClearMarkedPostResponse(result);
            return success;
        }

        private static void SendClearMarkedPostRequest(IAsyncResult result, AutoResetEvent signal, ThreadMetadata data)
        {
            HttpWebRequest request = result.AsyncState as HttpWebRequest;
            using (StreamWriter writer = new StreamWriter(request.EndGetRequestStream(result)))
            {
                string postData = string.Format("json=1&action=resetseen&threadid={0}", data.ThreadID);
                writer.Write(postData);
                writer.Close();
            }

            signal.Set();
        }

        private static bool ProcessClearMarkedPostResponse(IAsyncResult result)
        {
            var request = result.AsyncState as HttpWebRequest;
            var response = request.EndGetResponse(result) as HttpWebResponse;
            return response.StatusCode == HttpStatusCode.OK;
        }

        #endregion

        public static bool RunURLTask(string url)
        {
            var web = new AwfulWebClient();
            var doc = web.FetchHtml(url);
            return doc != null;
        }

        public static bool RunURLTask(Uri uri)
        {
            return RunURLTask(uri.AbsoluteUri);
        }
    }
}
