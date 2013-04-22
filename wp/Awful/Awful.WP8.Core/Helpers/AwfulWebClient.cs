using System;
using System.ComponentModel;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using System.Threading;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using KollaWP;


namespace Awful.Deprecated
{
    public class AwfulWebClient : KSBindableBase
    {
        public const int DefaultTimeoutInMilliseconds = 60000;

        /// <summary>
        /// Fires an event anytime the web client needs authentication information to proceed.
        /// </summary>
        public static event EventHandler<LoginRequiredEventArgs> LoginRequired;

        /// <summary>
        /// User's username on the something awful forums.
        /// </summary>
        public static string Username { get; private set; }
        /// <summary>
        /// User's password on the something awful forums.
        /// </summary>
        public static string Password { get; private set; }

        /// <summary>
        /// Will simulate timeouts (i.e. throw exceptions on html requests) according
        /// to the the frequency specified by SimulateTimeoutChance.
        /// </summary>
        public static bool SimulateTimeout { get; set; }

        /// <summary>
        /// The probability, as a percentage, that web requests will throw a timeout
        /// exception; only works when SimulateTimeout is true.
        /// </summary>
        public static int SimulateTimeoutChance { get; set; }

        private delegate string FetchHtmlDelegate(string url, int timeout);

        private static LoginRequiredEventArgs OnLoginRequired(AwfulWebClient client)
        {
            LoginRequiredEventArgs args = null;
            if (LoginRequired != null)
            {
                args = new LoginRequiredEventArgs();
                LoginRequired(client, args);
            }

            return args;
        }

        private void Authenticate()
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "START Authenticate");
            // first, check iso storage for cookies.
            // TODO: the above.

            // else, login using the supplied username and password.
            var login = new AwfulLoginClient();
            int tries = 0;
            while (!AwfulWebRequest.CanAuthenticate && tries < 3)
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Authentication failed, attempting to try again...");

                var loginArgs = OnLoginRequired(this);
                if (loginArgs == null)
                    throw new Exception("You need to attach an event listener for login required!");

                loginArgs.Signal.WaitOne();

                if (loginArgs.Ignore)
                {
                    AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Ignoring authentication request. Moving on...");
                    return;
                }

                else if (!loginArgs.Cancel)
                {
                    AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "User has input credentials; retrying...");

                    Username = loginArgs.Username;
                    Password = loginArgs.Password;
                    AwfulWebRequest.SetCookieJar(login.Authenticate(Username, Password));
                    tries++;
                }

                // automatically fail on cancel
                else
                    throw new Exception("User cancelled authentication.");
            }

            // after too many tries throw the white flag.
            if (tries > 3)
                throw new Exception("User failed to authenticate.");

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "END Authenticate()");
        }

        #region FetchHtmlState

        class FetchHtmlState
        {
            public HttpWebRequest Request { get; set; }
            public Action<string> HtmlAction { get; set; }
            public Action<FetchHtmlState> Callback { get; set; }
            public Exception Error { get; set; }
            public string Html { get; set; }
            public bool IgnoreTimeout { get; set; }
            private Timer Timer { get; set; }
            public int TimeoutInMilliseconds { get; private set; }

            public FetchHtmlState(int timeoutInMilliseconds)
            {
                IgnoreTimeout = false;
                TimeoutInMilliseconds = timeoutInMilliseconds;
            }

            public void StartTimer()
            {
                // new timer, fire callback according to timeoutInMilliseconds, don't repeat
                Timer = new Timer(new TimerCallback(ThrowTimeoutException),
                    null,
                    TimeoutInMilliseconds,
                    System.Threading.Timeout.Infinite);
            }

            private void ThrowTimeoutException(object state)
            {
                if (!IgnoreTimeout)
                {
                    // create new exception to be thrown
                    Error = new TimeoutException("fetch html timeout reached.");
                    Callback(this);
                }
            }
        }

        #endregion

        /// <summary>
        /// Fetches raw html from the specified url.
        /// </summary>
        /// <param name="url">The location of the target html.</param>
        /// <param name="callback">Callback function when html response is received from the server.</param>
        /// <param name="timeout">The timeout value, in milliseconds. If the timeout is reached, a TimeoutException is thrown.
        /// If no value is supplied, a default value of 10 seconds is used.</param>
        public void FetchHtmlAsync(string url, Action<string> callback, int timeout = DefaultTimeoutInMilliseconds)
        {
            FetchHtmlStateAsync(url, state =>
                {
                    if (state.Error != null)
                        throw state.Error;

                    callback(state.Html);
                });
        }

        private void FetchHtmlStateAsync(string url, Action<FetchHtmlState> callback, int timeout = DefaultTimeoutInMilliseconds)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, string.Format("START FetchHtml({0}, {1})", url, timeout));
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Performing authentication check...");

            Authenticate();

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Authentication check complete.");
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, string.Format("Requesting html from url '{0}'...", url));

            if (!NetworkInterface.GetIsNetworkAvailable())
                throw new Exception("The network is unavailable. Check your network settings and please try again.");

            if (SimulateTimeout)
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "SimulateTimeout = true.");
                Random random = new Random();
                int value = random.Next(1, 101);
                if (value <= SimulateTimeoutChance)
                {
                    AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Timeout generated.");
                    throw new TimeoutException("Artificial timeout generated!");
                }
            }

            FetchHtmlState state = new FetchHtmlState(timeout);
            state.Callback = callback;
            state.Request = AwfulWebRequest.CreateGetRequest(url);
            state.Request.BeginGetResponse(FetchHtmlAsyncResponse, state);
            state.StartTimer();
        }

        private void FetchHtmlAsyncResponse(IAsyncResult result)
        {
            FetchHtmlState state = result.AsyncState as FetchHtmlState;
            state.IgnoreTimeout = true;

            try
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "START ProcessResponse()");

                using (var response = state.Request.EndGetResponse(result))
                using (var stream = new StreamReader(response.GetResponseStream(),
                    Encoding.GetEncoding(CoreConstants.WEB_RESPONSE_ENCODING)))
                {
                    state.Html = stream.ReadToEnd();
                }

                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "END ProcessResponse()");

                if (string.IsNullOrEmpty(state.Html))
                    throw new NullReferenceException("fetch html result is null or empty");
            }

            catch (Exception ex) { state.Error = ex; }

            state.Callback(state);
        }

        /// <summary>
        /// Fetches raw html from the specified url.
        /// </summary>
        /// <param name="url">The location of the target html.</param>
        /// <param name="timeout">The timeout value, in milliseconds, before cancelling the request.
        /// If no value is supplied, a default value of 10 seconds is used.</param>
        /// <returns>A string representing the raw html.</returns>
        public string FetchHtml(string url, int timeout = DefaultTimeoutInMilliseconds)
        {
            AutoResetEvent signal = new AutoResetEvent(false);
            FetchHtmlState state = null;

            FetchHtmlStateAsync(url, callback =>
            {
                state = callback;
                signal.Set();

            }, timeout);
            
          
            signal.WaitOne();

            if (state.Error != null)
                throw state.Error;

            return state.Html;
        }
    }

    public class LoginRequiredEventArgs : EventArgs
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Cancel { get; set; }
        public bool Ignore { get; set; }
        public AutoResetEvent Signal { get; private set; }
        internal LoginRequiredEventArgs() 
        {
            Signal = new AutoResetEvent(false);
        }

        public void SetUserAndProceed(UserMetadata user)
        {
            AwfulWebRequest.SetCredentials(user);
            this.Ignore = true;
        }
    }
}
