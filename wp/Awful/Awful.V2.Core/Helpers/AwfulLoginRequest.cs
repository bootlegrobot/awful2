using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Net.NetworkInformation;

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
            if (!NetworkInterface.GetIsNetworkAvailable())
                throw new LoginFailedException("The network is unavailable. Check your network settings and please try again.");

            var request = AwfulWebRequest.CreatePostRequest(LOGIN_URL);

            var signal = new AutoResetEvent(false);
            var result = request.BeginGetRequestStream(callback => { signal.Set(); },
                request);

            signal.WaitOne();

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
            var result = httpWebRequest.BeginGetResponse(callback => { signal.Set(); }, httpWebRequest);

#if DEBUG
            signal.WaitOne();
#else
            signal.WaitOne(timeout);
#endif
            if (!result.IsCompleted)
                throw new TimeoutReachedException();

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

        private List<Cookie> ParseLoginResponse(IAsyncResult callback)
        {
            bool success = false;
            var request = callback.AsyncState as HttpWebRequest;
            HttpWebResponse response = request.EndGetResponse(callback) as HttpWebResponse;

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new LoginFailedException("Your username and/or password is incorrect. Please try again.");

                case HttpStatusCode.OK:
                    success = request.CookieContainer.Count >= 2;
                    break;
            }

            if (!success)
                throw new LoginFailedException();

            string html = null;

            using (var reader = new StreamReader(response.GetResponseStream()))
                html = reader.ReadToEnd();
                
          
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

    public class LoginFailedException : Exception {

        public LoginFailedException() : base() { }

        public LoginFailedException(string p) : base(p) { }
    }

    public class LoginEventArgs : EventArgs
    {
        public UserMetadata User { get; private set; }

        internal LoginEventArgs(UserMetadata metadata) { this.User = metadata; }
    }
}
