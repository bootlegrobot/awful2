using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace Awful
{
    public class AwfulLoginClient : Common.BindableBase
    {
        public const int DefaultTimeoutInMilliseconds = 10000;
        private const string COOKIE_DOMAIN_URL = "http://fake.forums.somethingawful.com";
        private const string LOGIN_URL = "http://forums.somethingawful.com/account.php?";

        private string _status;
        public string Status
        {
            get { return this._status; }
            set { this._status = value; this.OnPropertyChanged("Status"); }
        }

        public static event EventHandler<LoginEventArgs> LoginSuccessful;

        private static void OnLoginSuccessful(AwfulLoginClient sender, string username, List<Cookie> cookie)
        {
            if (LoginSuccessful != null)
            {
                var user = new UserMetadata() { Username = username, Cookies = cookie };
                LoginSuccessful(sender, new LoginEventArgs(user));
            }
        }

        public List<Cookie> Authenticate(string username, string password, int timeout = DefaultTimeoutInMilliseconds)
        {
            var request = AwfulWebRequest.CreatePostRequest(LOGIN_URL);

            var signal = new AutoResetEvent(false);
            var result = request.BeginGetRequestStream(callback => { signal.Set(); },
                request);

            signal.WaitOne(DefaultTimeoutInMilliseconds);
            if (!result.IsCompleted)
                throw new TimeoutReachedException();

            bool uploaded = UploadLoginData(result, username, password);
            if (!uploaded)
                throw new LoginFailedException();

            List<Cookie> success = ProcessLoginResults(result.AsyncState as HttpWebRequest,
                timeout);

            if (success != null)
                OnLoginSuccessful(this, username, success);

            return success;
        }

        private List<Cookie> ProcessLoginResults(HttpWebRequest httpWebRequest, int timeout)
        {
            AutoResetEvent signal = new AutoResetEvent(false);
            var result = httpWebRequest.BeginGetResponse(callback => 
                ProcessLoginResponse(callback, signal), httpWebRequest);

            signal.WaitOne(timeout);
            return ParseLoginResponse(result);
        }

        private bool UploadLoginData(IAsyncResult result, 
            string username, string password) 
        {
            bool success = false;

            try
            {
                HttpWebRequest request = result.AsyncState as HttpWebRequest;
                using (StreamWriter writer = new StreamWriter(request.EndGetRequestStream(result)))
                {
                    var postData = String.Format("action=login&username={0}&password={1}",
                        username.Replace(" ", "+"),
                        HttpUtility.UrlEncode(password));

                    writer.Write(postData);
                    success = true;
                }
            }

            catch (Exception ex) { success = false; }
            return success;
        }

        private void ProcessLoginResponse(IAsyncResult callback, AutoResetEvent signal) { signal.Set(); }

        private List<Cookie> ParseLoginResponse(IAsyncResult callback)
        {
            var request = callback.AsyncState as HttpWebRequest;
            WebResponse response = request.EndGetResponse(callback);
            if (response.ResponseUri.AbsoluteUri != "http://forums.somethingawful.com/account.php?")
            {
                // connecting to a wireless network without authenticating first will get you here
                throw new Exception("Authenticate failed. Check your internet connection settings and try again.");
            }

            string html = null;
            using (var reader = new StreamReader(response.GetResponseStream()))
                html = reader.ReadToEnd();
                
            //Logger.AddEntry(html);

            bool success = request.CookieContainer.Count >= 2;
            if (!success)
                throw new LoginFailedException();

            var collection = request.CookieContainer.GetCookies(
                new Uri(COOKIE_DOMAIN_URL));

            return ManageCookies(collection);            
        }

        private List<Cookie> ManageCookies(CookieCollection collection)
        {
            var cookies = collection;
            List<Cookie> cookieList = new List<Cookie>(cookies.Count);
            var container = new CookieContainer();
            foreach (Cookie cookie in cookies)
            {
                var awfulCookie = new Cookie(
                    cookie.Name,
                    cookie.Value,
                    "/",
                    ".somethingawful.com");
                cookieList.Add(awfulCookie);
            }

            return cookieList;
        }
    }

    public class LoginFailedException : Exception { }

    public class LoginEventArgs : EventArgs
    {
        public UserMetadata User { get; private set; }

        internal LoginEventArgs(UserMetadata metadata) { this.User = metadata; }
    }
}
