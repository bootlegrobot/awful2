using System;
using System.Net;
using System.Net.Browser;
using System.Collections.Generic;

namespace Awful
{
    /// <summary>
    /// A set of helper functions to make proper web requests to the SA forums.
    /// </summary>
    public static class AwfulWebRequest
    {
        private const string ACCEPT = "text/html, application/xhtml+xml, */*";
        private const string USERAGENT = "Mozilla/5.0 (compatible; MSIE 9.0; Windows Phone OS 7.5; Trident/5.0; IEMobile/9.0; Microsoft; XDeviceEmulator)";
        private const string POST_CONTENT_TYPE = "application/x-www-form-urlencoded";

        private static List<Cookie> _cookieJar = new List<Cookie>();
        public static IEnumerable<Cookie> CookieJar { get { return _cookieJar; } }

        /// <summary>
        /// Sets the global cookie container for all future web requests. If this container is not set,
        /// certain requests may fail. This method is typically called after authentication cookies are
        /// obtained (see AwfulLoginRequest).
        /// </summary>
        /// <param name="jar">The container of cookies used to authenticate requests.</param>
        public static void SetCookieJar(IEnumerable<Cookie> jar)
        {
            _cookieJar.Clear();
            _cookieJar.AddRange(jar);
        }

        /// <summary>
        /// Gets the status of whether or not web requests can be properly authenticated.
        /// </summary>
        public static bool CanAuthenticate { get { return !_cookieJar.IsNullOrEmpty(); } }

        private static CookieContainer GetCookiesForUri(Uri uri)
        {
            var container = new CookieContainer();
            var cookies = CookieJar;
            foreach (Cookie cookie in cookies) { container.Add(uri, cookie); }
            return container;
        }

        /// <summary>
        /// Creates a typical GET request to web pages on the sa forums.
        /// </summary>
        /// <param name="url">The url of the requested page.</param>
        /// <returns>A preconfigured HttpWebRequest object to propertly fulfill the request.</returns>
        public static HttpWebRequest CreateGetRequest(string url)
        {
            var uri = new Uri(url);
            var request = (HttpWebRequest)WebRequestCreator.ClientHttp.Create(uri);
            request.Accept = ACCEPT;
            request.UserAgent = USERAGENT;
            request.CookieContainer = GetCookiesForUri(uri);
            request.Method = "GET";
            request.UseDefaultCredentials = false;
            return request;
        }

        /// <summary>
        /// Creates a typical POST request to web pages on the sa forums.
        /// </summary>
        /// <param name="url">The url of the requested page.</param>
        /// <returns>A preconfigured HttpWebRequest object to propertly fulfill the request.</returns>
        public static HttpWebRequest CreatePostRequest(string url)
        {
            var uri = new Uri(url);
            var request = (HttpWebRequest)WebRequestCreator.ClientHttp.Create(uri);
            request.Accept = ACCEPT;
            request.UserAgent = USERAGENT;
            request.CookieContainer = GetCookiesForUri(uri);
            request.Method = "POST";
            request.ContentType = POST_CONTENT_TYPE;
            request.UseDefaultCredentials = false;
            return request;
        }

        /// <summary>
        /// Creates a special POST request upload multipart form data to SA's servers.
        /// </summary>
        /// <param name="url">The url of the requested page.</param>
        /// <returns>A preconfigured HttpWebRequest object to propertly fulfill the request.</returns>
        public static HttpWebRequest CreateFormDataPostRequest(string url, string contentType)
        {
            var uri = new Uri(url);
            var request = (HttpWebRequest)WebRequestCreator.ClientHttp.Create(uri);
            request.Accept = ACCEPT;
            request.UserAgent = USERAGENT;
            request.CookieContainer = GetCookiesForUri(uri);
            request.Method = "POST";
            request.ContentType = contentType;
            request.UseDefaultCredentials = false;
            return request;
        }
    }
}
