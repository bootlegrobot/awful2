using System;
using System.ComponentModel;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using System.Threading;
using System.Text;
using System.Windows;
using System.Collections.Generic;


namespace Awful
{
    public class AwfulWebClient : Common.BindableBase
    {
        public const int DefaultTimeoutInMilliseconds = 10000;

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

        private delegate HtmlDocument FetchHtmlDelegate(string url, int timeout);

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

		/// <summary>
        /// Fetches raw html from the specified url.
        /// </summary>
        /// <param name="url">The location of the target html.</param>
        /// <param name="timeout">The timeout value, in milliseconds, before cancelling the request.
        /// If no value is supplied, a default value of 10 seconds is used.</param>
        /// <returns>A HtmlDocument representing the raw html.</returns>
        public HtmlDocument FetchHtml(string url, int timeout = DefaultTimeoutInMilliseconds)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, string.Format("START FetchHtml({0}, {1})", url, timeout));
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Performing authentication check...");
            
            Authenticate();
            
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Authentication check complete.");
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, string.Format("Requesting html from url '{0}'...", url));

            if (SimulateTimeout)
            {
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "SimulateTimeout = true.");
                Random random = new Random();
                int value = random.Next(1, 100);
                if (value <= SimulateTimeoutChance)
                {
                    AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Timeout generated.");
                    throw new TimeoutException("Artificial timeout generated!");
                }
            }

            var request = AwfulWebRequest.CreateGetRequest(url);
            var signal = new AutoResetEvent(false);
            var result = request.BeginGetResponse(callback => ProcessResponse(callback, signal), request);
            
            signal.WaitOne(timeout);
            if (!result.IsCompleted)
                throw new TimeoutException();
            
            string html = this.ProcessResponse(request.EndGetResponse(result));

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "Html result:");
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, html);
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "End Html.");
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info,  "Request complete.");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, string.Format("END FetchHtml({0}, {1})", url, timeout));

            return doc;
        }

		/// <summary>
        /// Asyncronously begins an html fetch request; Call EndFetchHtml to retrieve the fetch results.
        /// </summary>
        /// <param name="url">The location of the target html.</param>
        /// <param name="callback">The callback delegate to execute after this method is run.</param>
        /// <param name="timeout">The timeout value, in milliseconds, before the request is cancelled.
        /// If no value is supplied, a default value of 10 seconds is used.</param>
        /// <returns></returns>
        public IAsyncResult BeginFetchHtml(string url, AsyncCallback callback, 
            int timeout = DefaultTimeoutInMilliseconds)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, string.Format("START BeginFetchHtml({0}, {1})", url, timeout));
            FetchHtmlDelegate fetch = FetchHtml;
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, string.Format("END BeginFetchHtml({0}, {1})", url, timeout));

            return fetch.BeginInvoke(url, timeout, callback, this);
        }

        public HtmlDocument EndFetchHtml(IAsyncResult result)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "START EndFetchHtml()");
            
            FetchHtmlDelegate fetch = (result.AsyncState as AwfulWebClient).FetchHtml;
            var doc = fetch.EndInvoke(result);

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "END EndFetchHtml()");
            return doc;
        }

        private void ProcessResponse(IAsyncResult result, AutoResetEvent signal) { signal.Set(); }

        private string ProcessResponse(WebResponse response)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "START ProcessResponse()");

            string html = string.Empty;
            using (var stream = new StreamReader(response.GetResponseStream(), 
                Encoding.GetEncoding(CoreConstants.WEB_RESPONSE_ENCODING)))
            {
                html = stream.ReadToEnd();
            }

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "END ProcessResponse()");

            return html;
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
