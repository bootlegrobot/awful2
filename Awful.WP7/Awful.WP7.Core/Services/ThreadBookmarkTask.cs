using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Windows;

namespace Awful
{
    public enum BookmarkAction { Add, Remove }
    
    public class ThreadBookmarkTask
    {
        private static ThreadBookmarkTask instance = new ThreadBookmarkTask();

        private ThreadBookmarkTask() { }

        public static bool Bookmark(ThreadMetadata thread, BookmarkAction action) { return instance.ToggleBookmark(thread, action); }

        private bool ToggleBookmark(ThreadMetadata thread, BookmarkAction action, int timeout = CoreConstants.DEFAULT_TIMEOUT_IN_MILLISECONDS)
        {
            //Logger.AddEntry(string.Format("ToggleBookmarkAsync - ThreadID: {0}, Action: {1}", thread.ID, action));
            
            string url = String.Format("{0}/{1}", CoreConstants.BASE_URL, CoreConstants.BOOKMARK_THREAD_URI);

            //Logger.AddEntry(string.Format("ToggleBookmarkAsync - Bookmark url: {0}", url));
            
            // create request and request data
            var request = InitializePostRequest(url);
            var signal = new AutoResetEvent(false);
            var result = request.BeginGetRequestStream(callback =>
                ProcessToggleBookmarkAsyncGetRequest(callback, signal, action, thread),
                request);

            // wait for processing
            signal.WaitOne();

            // begin the response process
            request = result.AsyncState as HttpWebRequest;
            result = request.BeginGetResponse(callback => { signal.Set(); }, request);
            signal.WaitOne(timeout);

            // process response and return success status
            bool success = ProcessToggleBookmarkAsyncGetResponse(result);
            return success;
        }

        private void ProcessToggleBookmarkAsyncGetRequest(IAsyncResult asyncResult, AutoResetEvent signal,
            BookmarkAction action, ThreadMetadata data)
        {
            //Logger.AddEntry("ToggleBookmarkAsync - Initializingweb request...");

            HttpWebRequest request = asyncResult.AsyncState as HttpWebRequest;
            StreamWriter writer = new StreamWriter(request.EndGetRequestStream(asyncResult));
            var postData = String.Format("{0}&{1}={2}",
                action == BookmarkAction.Add ? CoreConstants.BOOKMARK_ADD : CoreConstants.BOOKMARK_REMOVE,
                CoreConstants.THREAD_ID,
                data.ThreadID);

            //Logger.AddEntry(string.Format("ToggleBookmarkAsync - PostData: {0}", postData));

            writer.Write(postData);
            writer.Close();

            signal.Set();
        }

        private bool ProcessToggleBookmarkAsyncGetResponse(IAsyncResult result)
        {
            var request = result.AsyncState as HttpWebRequest;
            var response = request.EndGetResponse(result) as HttpWebResponse;

            //Logger.AddEntry("ToggleBookmarkAsync - Start Get Response successful.");
            //Logger.AddEntry(string.Format("ToggleBookmarkAsync - Repsonse url: {0}", response.ResponseUri));

            return response.StatusCode == HttpStatusCode.OK;
        }

        private HttpWebRequest InitializePostRequest(string url)
        {
            HttpWebRequest request = AwfulWebRequest.CreateFormDataPostRequest(url, "application/x-www-form-urlencoded");
            return request;
        }
    }
}
