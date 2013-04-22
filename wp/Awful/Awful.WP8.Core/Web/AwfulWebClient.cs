using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Awful;
using System.Net;
using KollaWP;
using System.IO;
using System.Net.Browser;

namespace Awful.Web
{
    public static class AwfulWebClient
    {
        public const int DefaultTimeoutInMilliseconds = 60000;
        private const string BASE_URL = "http://forums.somethingawful.com";
        private const string COOKIE_DOMAIN_URL = "http://fake.forums.somethingawful.com";
        private const string LOGIN_URL = "http://forums.somethingawful.com/account.php?";
        
        private static RestSharp.RestClient client;
        private static RestSharp.RestClient Client
        {
            get
            {
                if (client == null)
                    client = new RestSharp.RestClient(BASE_URL);

                return client;
            }
        }

        public static bool LoadSession(UserMetadata user)
        {
            if (user.Cookies.IsNullOrEmpty())
                return false;

            var domain = new Uri(BASE_URL, UriKind.Absolute);
            Client.CookieContainer = new CookieContainer();

            foreach (var cookie in user.Cookies)
            {
                Client.CookieContainer.Add(domain, cookie);
            }

            return true;
        }

        public static async Task<UserMetadata> LoginAsync(string username, string password)
        {
            /*
            RestSharp.RestRequest request = new RestSharp.RestRequest("account.php", RestSharp.Method.POST);
            request.AddParameter("action", "login");
            request.AddParameter("username", username);
            request.AddParameter("password", password);
            return await Client.ExecuteRequestAsync<UserMetadata>(request, response =>
                {
                    UserMetadata user = new UserMetadata();
                    user.Username = username;
                    user.Cookies = new List<System.Net.Cookie>();
                    foreach (var cookie in response.Cookies)
                        user.Cookies.Add(cookie.ToDotNETCookie());

                    return user;
                });
            */

            TaskCompletionSource<IEnumerable<Cookie>> tcs = new TaskCompletionSource<IEnumerable<Cookie>>();
            Awful.Deprecated.AwfulLoginClient login = new Deprecated.AwfulLoginClient();
            tcs.TrySetResult(login.Authenticate(username, password));
            var cookies = await tcs.Task;
            UserMetadata user = new UserMetadata();
            user.Username = username;
            user.Cookies = new List<Cookie>(cookies);
            return user;
        }

        public static async Task<IEnumerable<TagMetadata>> GetSmiliesAsync()
        {
            // http://forums.somethingawful.com/misc.php?action=showsmilies
            RestSharp.RestRequest request = new RestSharp.RestRequest("misc.php", RestSharp.Method.GET);
            request.AddParameter("action", "showsmilies");
            return await Client.ExecuteRequestAsync<IEnumerable<TagMetadata>>(request, response =>
            {
                HtmlAgilityPack.HtmlDocument doc = response.ToHtmlDocument();
                var smilies = SmileyParser.ParseSmiliesFromNode(doc);
                return smilies;
            });

        }

        public static async Task<IEnumerable<ForumMetadata>> GetForumListAsync()
        {
            // http://forums.somethingawful.com/forumdisplay.php?forumid=1
            RestSharp.RestRequest request = new RestSharp.RestRequest("forumdisplay.php", RestSharp.Method.GET);
            request.AddParameter("forumid", "1");
            return await Client.ExecuteRequestAsync<IEnumerable<ForumMetadata>>(request, response =>
                {
                    HtmlAgilityPack.HtmlDocument doc = response.ToHtmlDocument();
                    IEnumerable<ForumMetadata> forums = ForumParser.ParseForumList(doc);
                    return forums;
                });
        }

        public static async Task<ForumPageMetadata> GetForumIndexAsync(this ForumMetadata forum, int pageNumber)
        {
            // http://forums.somethingawful.com/forumdisplay.php?forumid=1&daysprune=15&perpage=40&posticon=0&sortorder=desc&sortfield=lastpost&pagenumber=1

            RestSharp.RestRequest request = new RestSharp.RestRequest("forumdisplay.php", RestSharp.Method.GET);
            
            request.AddParameter("forumid", forum.ForumID);
            request.AddParameter("daysprune", 15);
            request.AddParameter("perpage", 40);
            request.AddParameter("posticon", 0);
            request.AddParameter("sortorder", "desc");
            request.AddParameter("sortfield", "lastpost");
            request.AddParameter("pagenumber", pageNumber);
            return await Client.ExecuteRequestAsync<ForumPageMetadata>(request, response =>
                {
                    HtmlAgilityPack.HtmlDocument doc = response.ToHtmlDocument();
                    return ForumParser.ParseForumPage(doc);
                });
        }

        public static async Task<ThreadPageMetadata> GetThreadPageAsync(this ThreadMetadata thread, int pageNumber)
        {
            // http://forums.somethingawful.com/showthread.php?threadid=3545394&pagenumber=1

            RestSharp.RestRequest request = new RestSharp.RestRequest("showthread.php", RestSharp.Method.GET);
            request.AddParameter("threadid", thread.ThreadID);
            request.AddParameter("pagenumber", pageNumber);
            return await Client.ExecuteRequestAsync<ThreadPageMetadata>(request, response =>
            {
                HtmlAgilityPack.HtmlDocument doc = response.ToHtmlDocument();
                return ThreadPageParser.ParseThreadPage(doc);
            });
        }

        public static async Task<ThreadPageMetadata> GetLastPostAsync(this ThreadMetadata thread, int pageNumber)
        {
            // http://forums.somethingawful.com/showthread.php?threadid=3545394&goto=lastpost

            RestSharp.RestRequest request = new RestSharp.RestRequest("showthread.php", RestSharp.Method.GET);
            
            request.AddParameter("goto", "lastpost");
            return await Client.ExecuteRequestAsync<ThreadPageMetadata>(request, response =>
            {
                HtmlAgilityPack.HtmlDocument doc = response.ToHtmlDocument();
                return ThreadPageParser.ParseThreadPage(doc);
            });
        }

        public static async Task<string> QuoteAsync(this ThreadPostMetadata post)
        {
            // http://forums.somethingawful.com/newreply.php?action=newreply&postid=414146827

            RestSharp.RestRequest request = new RestSharp.RestRequest("newreply.php", RestSharp.Method.GET);

            request.AddParameter("action", "newreply");
            request.AddParameter("postid", post.PostID);
            return await Client.ExecuteRequestAsync<string>(request, response =>
            {
                HtmlAgilityPack.HtmlDocument doc = response.ToHtmlDocument();
                string body = doc.DocumentNode
                    .Descendants("textarea")
                    .FirstOrDefault()
                    .InnerText;

                return body;
            });
        }

        public static async Task<bool> MarkAsRead(this ThreadPostMetadata post)
        {
            // http://forums.somethingawful.com/showthread.php?action=setseen&threadid=3545394&index=23

            RestSharp.RestRequest request = new RestSharp.RestRequest("showthread.php", RestSharp.Method.GET);
            request.AddParameter("action", "setseen");
            request.AddParameter("threadid", post.ThreadID);
            request.AddParameter("index", post.ThreadIndex);

            return await Client.ExecuteRequestAsync<bool>(request, response =>
            {
                return response.StatusCode == HttpStatusCode.OK;
            });
        }

        public static async Task<ThreadPageMetadata> RefreshAsync(this ThreadPageMetadata page)
        {
            return await GetThreadPageAsync(new ThreadMetadata() { ThreadID = page.ThreadID },
                page.PageNumber);
        }

        public enum BookmarkOptions { Add, Remove }

        public static async Task<bool> SetBookmarkAsync(this ThreadMetadata thread, BookmarkOptions options)
        {
            RestSharp.RestRequest request = new RestSharp.RestRequest(CoreConstants.BOOKMARK_THREAD_URI, RestSharp.Method.GET);
            request.AddParameter("json", "1");
            request.AddParameter("action", options == BookmarkOptions.Add
                ? "cat_toggle"
                : "remove");
            request.AddParameter("threadid", thread.ThreadID);

            return await Client.ExecuteRequestAsync<bool>(request, response =>
            {
                return response.StatusCode == HttpStatusCode.OK;
            });
        }

        public static async Task<ForumPageMetadata> RefreshAsync(this ForumPageMetadata page)
        {
            return await GetForumIndexAsync(new ForumMetadata() { ForumID = page.ForumID },
                page.PageNumber);
        }

        public static async Task<PrivateMessageMetadata> RefreshAsync(this PrivateMessageMetadata message)
        {
            // http://forums.somethingawful.com/private.php?action=show&privatemessageid=4823295
            RestSharp.RestRequest request = new RestSharp.RestRequest("private.php", RestSharp.Method.GET);
            request.AddParameter("action", "show");
            request.AddParameter("privatemessageid", message.PrivateMessageId);
            return await Client.ExecuteRequestAsync<PrivateMessageMetadata>(request, response =>
                {
                    var doc = response.ToHtmlDocument();
                    return PrivateMessageParser.ParsePrivateMessageDocument(doc);
                });
        }

        public static async Task<IPrivateMessageRequest> GetNewRequestAsync()
        {
            //http://forums.somethingawful.com/private.php?action=newmessage&privatemessageid=4823295
            RestSharp.RestRequest request = new RestSharp.RestRequest("private.php", RestSharp.Method.GET);
            request.AddParameter("action", "newmessage");
            return await Client.ExecuteRequestAsync<IPrivateMessageRequest>(request, response =>
            {
                var doc = response.ToHtmlDocument();
                return PrivateMessageParser.ParseNewPrivateMessageFormDocument(doc);
            });
        }

        public static async Task<IPrivateMessageRequest> GetReplyRequestAsync(this PrivateMessageMetadata message)
        {
            //http://forums.somethingawful.com/private.php?action=newmessage&privatemessageid=4823295
            RestSharp.RestRequest request = new RestSharp.RestRequest("private.php", RestSharp.Method.GET);
            request.AddParameter("action", "newmessage");
            request.AddParameter("privatemessageid", message.PrivateMessageId);
            return await Client.ExecuteRequestAsync<IPrivateMessageRequest>(request, response =>
            {
                var doc = response.ToHtmlDocument();
                return PrivateMessageParser.ParseNewPrivateMessageFormDocument(doc);
            });
        }

        public static async Task<IPrivateMessageRequest> GetForwardRequestAsync(this PrivateMessageMetadata message)
        {
            // http://forums.somethingawful.com/private.php?action=newmessage&forward=true&privatemessageid=4823295
            RestSharp.RestRequest request = new RestSharp.RestRequest("private.php", RestSharp.Method.GET);
            request.AddParameter("action", "newmessage");
            request.AddParameter("forward", "true");
            request.AddParameter("privatemessageid", message.PrivateMessageId);
            return await Client.ExecuteRequestAsync<IPrivateMessageRequest>(request, response =>
            {
                var doc = response.ToHtmlDocument();
                return PrivateMessageParser.ParseNewPrivateMessageFormDocument(doc);
            });
        }

        public static async Task<IEnumerable<PrivateMessageFolderMetadata>> GetFolderListAsync()
        {
            // http://forums.somethingawful.com/private.php?
            RestSharp.RestRequest request = new RestSharp.RestRequest("private.php", RestSharp.Method.GET);
            return await Client.ExecuteRequestAsync<IEnumerable<PrivateMessageFolderMetadata>>(request, response =>
            {
                var doc = response.ToHtmlDocument();
                return PrivateMessageParser.ParseFolderList(doc);
            });

        }

        public static async Task<PrivateMessageFolderMetadata> RefreshAsync(this PrivateMessageFolderMetadata folder)
        {
            // http://forums.somethingawful.com/private.php?folderid=4&daysprune=9999
            RestSharp.RestRequest request = new RestSharp.RestRequest("private.php", RestSharp.Method.GET);
            request.AddParameter("folderid", folder.FolderId);
            request.AddParameter("daysprune", "9999");
            return await Client.ExecuteRequestAsync<PrivateMessageFolderMetadata>(request, response =>
            {
                var doc = response.ToHtmlDocument();
                return PrivateMessageParser.ParsePrivateMessageFolder(doc);
            });
        }

        #region Replying to Threads

        private const string REPLY_ACTION_REQUEST = "postreply";
        private const string SUBMIT_REQUEST = "Submit Reply";
        private const string PARSEURL_REQUEST = "yes";
        private const string MAX_FILE_SIZE_REQUEST = "2097152";

        public struct ReplyResponse
        {
            public bool Success { get; set; }
            public Uri Redirect { get; set; }
        }

        public static async Task<ReplyResponse> ReplyAsync(this ThreadMetadata thread,
            string message)
        {
            // http://forums.somethingawful.com/newreply.php?action=newreply&threadid=3545394

            // STEP 1: LOAD NEW POST FORM

            string formKey = null;
            string formCookie = null;
            RestSharp.RestRequest request = new RestSharp.RestRequest("newreply.php", RestSharp.Method.GET);
            
            request.AddParameter("threadid", thread.ThreadID);

            // STEP 2: EXTRACT DATA FOR POST

            await Client.ExecuteRequestAsync<HtmlAgilityPack.HtmlDocument>(request, response => 
            { 
                var doc = response.ToHtmlDocument();

                var formNodes = doc.DocumentNode.Descendants("input").ToArray();

                formKey = formNodes
                    .Where(node => node.GetAttributeValue("name", "").Equals("formkey"))
                    .FirstOrDefault()
                    .GetAttributeValue("value", string.Empty);

                formCookie = formNodes
                    .Where(node => node.GetAttributeValue("name", "").Equals("form_cookie"))
                    .FirstOrDefault()
                    .GetAttributeValue("value", string.Empty);

                return doc;
            });
            
            // STEP 3: SEND POST

            request = new RestSharp.RestRequest("newreply.php", RestSharp.Method.POST);
            request.AddParameter("action", REPLY_ACTION_REQUEST);
            request.AddParameter("threadid", thread.ThreadID);
            request.AddParameter("formkey", formKey);
            request.AddParameter("form_cookie", formCookie);
            request.AddParameter("message", message);
            request.AddParameter("parseurl", PARSEURL_REQUEST);
            request.AddParameter("submit", SUBMIT_REQUEST);

            return await Client.ExecuteRequestAsync<ReplyResponse>(request, response =>
                {
                    ReplyResponse reply = new ReplyResponse();
                    reply.Redirect = response.ResponseUri;
                    reply.Success = response.StatusCode == System.Net.HttpStatusCode.OK;
                    return reply;
                });
        }

        #endregion

        #region Editing Posts

        private const string EDIT_ACTION_REQUEST = "updatepost";
        private const string BOOKMARK_REQUEST = "yes";

        public class EditPostRequest
        {
            public string PostId { get; private set; }
            public string FormKey { get; private set; }
            public string FormCookie { get; private set; }
            public string Message { get; set; }

            public EditPostRequest(string key, string cookie, string postId)
            {
                FormKey = key;
                FormCookie = cookie;
                PostId = postId;
            }
        }

        public static async Task<EditPostRequest> GetEditPostRequestAsync(this ThreadPostMetadata post)
        {
            RestSharp.RestRequest request = new RestSharp.RestRequest("editpost.php", RestSharp.Method.GET);
            
            request.AddParameter("action", "editpost");
            request.AddParameter("postid", post.PostID);

            return await Client.ExecuteRequestAsync<EditPostRequest>(request, response =>
            {
                var doc = response.ToHtmlDocument();

                string body = doc.DocumentNode
                    .Descendants("textarea")
                    .FirstOrDefault()
                    .InnerText;
                
                var formNodes = doc.DocumentNode.Descendants("input").ToArray();

                string formKey = formNodes
                    .Where(node => node.GetAttributeValue("name", "").Equals("formkey"))
                    .FirstOrDefault()
                    .GetAttributeValue("value", string.Empty);

                string formCookie = formNodes
                    .Where(node => node.GetAttributeValue("name", "").Equals("form_cookie"))
                    .FirstOrDefault()
                    .GetAttributeValue("value", string.Empty);

                EditPostRequest postRequest = new EditPostRequest(formKey, formCookie, post.PostID);
                postRequest.Message = body;
                return postRequest;
            });
            
        }

        public static async Task<ReplyResponse> SendRequestAsync(this EditPostRequest epr)
        {
            RestSharp.RestRequest request = new RestSharp.RestRequest("editpost.php", RestSharp.Method.POST);
            request.AddParameter("action", EDIT_ACTION_REQUEST);
            request.AddParameter("postid", epr.PostId);
            request.AddParameter("message", epr.Message);
            request.AddParameter("parseurl", PARSEURL_REQUEST);
            request.AddParameter("bookmark", BOOKMARK_REQUEST);
            request.AddParameter("submit", SUBMIT_REQUEST);

            return await Client.ExecuteRequestAsync<ReplyResponse>(request, response =>
                {
                    ReplyResponse reply = new ReplyResponse();
                    reply.Redirect = response.ResponseUri;
                    reply.Success = response.StatusCode == System.Net.HttpStatusCode.OK;
                    return reply;
                });
        }

        #endregion
    }

    public static class HtmlExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Sanitize(this string text)
        {
            text = text.Replace("\r", String.Empty);
            text = text.Replace("\n", String.Empty);
            text = text.Replace("\t", String.Empty);

            text = HttpUtility.HtmlDecode(text);
            return text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="innerText"></param>
        /// <returns></returns>
        public static string SanitizeDateTimeHTML(this string innerText)
        {
            string value = innerText.Replace("\t", "");
            value = value.Replace("&iquest;", "");
            value = value.Replace("\n", "");
            value = value.Replace("#", "");
            value = value.Replace("?", "");
            return value;
        }

        public static string ParseTitleFromBreadcrumbsNode(this HtmlAgilityPack.HtmlNode node)
        {
            string value = string.Empty;
            try
            {
                var breadcrumbs = node.InnerText.Replace("&gt;", "|").Split('|');
                value = breadcrumbs.Last().Trim();
            }
            catch (Exception) { value = "Unknown Value"; }
            return value;
        }
    }

    public static class RestSharpExtensions
    {
        public static async Task<T> ExecuteRequestAsync<T>(this RestSharp.IRestClient client, 
            RestSharp.IRestRequest request,
            Func<RestSharp.IRestResponse, T> callback)
        {
            var tcs = new TaskCompletionSource<T>();

            client.ExecuteAsync(request, (response, resp) =>
            {
                try
                {
                    if (response.ErrorException != null)
                        tcs.TrySetException(response.ErrorException);
                    else
                        tcs.TrySetResult(callback(response));
                }
                catch (Exception ex) { tcs.TrySetException(ex); }
            });

            return await tcs.Task;
        }

        public static System.Net.Cookie ToDotNETCookie(this RestSharp.RestResponseCookie cookie)
        {
            return new System.Net.Cookie(cookie.Name,
                cookie.Value,
                cookie.Path,
                cookie.Domain);
        }

        public static RestSharp.HttpCookie ToRestSharpCookie(this System.Net.Cookie cookie)
        {
            return new RestSharp.HttpCookie()
            {
                Name = cookie.Name,
                Value = cookie.Value,
                Domain = cookie.Domain,
                Path = cookie.Path
            };
        }

        public static HtmlAgilityPack.HtmlDocument ToHtmlDocument(this RestSharp.IRestResponse response)
        {
            string html = response.Content;
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);
            return document;
        }
    }
}
