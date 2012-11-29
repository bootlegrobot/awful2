using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Windows;
using HtmlAgilityPack;

namespace Awful
{
    public static class CoreExtensions
    {
        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            bool isNull = collection == null;
            bool isEmpty = !isNull && collection.Count == 0;
            return isNull || isEmpty;
        }

        public static bool IsStringUrlHttpOrHttps(this string value)
        {
            return value.Contains("://") && (value.Contains("http"));
        }

        private static bool BuildPath(this IsolatedStorageFile storage, string path)
        {
            bool success = false;
            try
            {
                var folders = path.Split('/');
                for (int i = 0; i < folders.Length - 1; i++)
                {
                    if (!storage.DirectoryExists(folders[i]))
                        storage.CreateDirectory(folders[i]);
                }
                success = true;
            }
            catch (Exception ex) { success = false; }
            return success;

        }

        public static bool CopyResourceFileToIsoStore(string source, string destination)
        {
            bool success = false;
            try
            {
                var bytes = ReadFileFromResourceStore(source);
                success = bytes.SaveBinaryToIsoStore(destination);
            }
            catch (Exception ex) { success = false; }
            return success;
        }

        public static byte[] ReadFileFromResourceStore(string filePath)
        {
            byte[] result = null;
            try
            {
                Stream stream = Application.GetResourceStream(new Uri(filePath, UriKind.RelativeOrAbsolute)).Stream;
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    result = reader.ReadBytes((int)stream.Length);
                }
            }
            catch (Exception ex) { }
            return result;
        }

        public static bool SaveBinaryToIsoStore(this byte[] bytes, string filePath, 
            IsolatedStorageFile storage = null)
        {
            bool success = false;
            try
            {
                if (storage == null)
                    storage = IsolatedStorageFile.GetUserStoreForApplication();

                if (BuildPath(storage, filePath))
                {
                    using (BinaryWriter writer = new BinaryWriter(
                        new IsolatedStorageFileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, storage)))
                    {
                        writer.Write(bytes);
                        success = true;
                    }
                }
            }
            catch (Exception ex) { success = false; }
            return success;
        }

        public static bool SaveToFile<T>(this T item, string filePath)
        {
            bool result = false;
            try
            {
                var storage = IsolatedStorageFile.GetUserStoreForApplication();
                if (storage.BuildPath(filePath))
                {
                    DataContractSerializer dcs = new DataContractSerializer(typeof(T));
                    using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(filePath,
                        System.IO.FileMode.Create, System.IO.FileAccess.Write, storage))
                    {
                        dcs.WriteObject(fileStream, item);
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#endif

                Debug.WriteLine(ex.Message);
                result = false;
            }

            return result;
        }

        public static T LoadFromFile<T>(string file)
        {
            T result = default(T);
            try
            {
                var storage = IsolatedStorageFile.GetUserStoreForApplication();
                if (!storage.FileExists(file))
                    return result;

                DataContractSerializer dcs = new DataContractSerializer(typeof(T));
                using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(file,
                    System.IO.FileMode.Open, System.IO.FileAccess.Read, storage))
                {
                    result = (T)dcs.ReadObject(fileStream);        
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                result = default(T);
            }

            return result;
        }
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

        internal static string ParseTitleFromBreadcrumbsNode(this HtmlNode node)
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

    public static class MetadataExtensions
    {
        #region Thread Extensions

        public static int GetLastReadPage(this ThreadMetadata thread)
        {
            if (thread.IsNew)
                return 1;

            int page = -1;
            int postsPerPage = 40;
            int totalPostCount = thread.ReplyCount;
            int newPostCount = thread.NewPostCount;
            int readPostCount = totalPostCount - newPostCount;

            // get the exact number of pages read, including fractions of a page
            double readPages = (double)readPostCount / postsPerPage;
            // get the base number of pages read
            double basePage = Math.Floor(readPages);

            // the fractional remainder
            double remainder = readPages - basePage;

            // if the remainder = 0.975, then the entire page was read (quirky!)
            if (Math.Round(remainder, 3) == 0.975)
                // jump two pages from the base page
                page = (int)basePage + 2;

            else
                // jump one page from the base page
                page = (int)basePage + 1;

            return page;
        }

        public static string ConvertToMetroStyle(this ThreadPageMetadata page)
        {
            return MetroStyler.Metrofy(page.Posts);
        }

        public static string PageUrl(this ThreadMetadata data, int page = -1)
        {
            StringBuilder url = new StringBuilder();
            url.AppendFormat("http://forums.somethingawful.com/displaythread.php?threadid={0}", data.ThreadID);
            if (page == -1)
                url.Append("&goto=newpost");
            else
                url.AppendFormat("&pagenumber={0}", page);

            return url.ToString();
        }

        private static ThreadPageMetadata FetchThreadPage(string threadId, int pageNumber)
        {
            // example string: http://forums.somethingawful.com/showthread.php?noseen=0&threadid=3439182&pagenumber=69
            var url = new StringBuilder();
            // http://forums.somethingawful.com/showthread.php
            url.AppendFormat("{0}/{1}", CoreConstants.BASE_URL, CoreConstants.THREAD_PAGE_URI);
            // noseen=0&threadid=<THREADID>&pagenumber=<PAGENUMBER>
            url.AppendFormat("?noseen=0&threadid={0}&pagenumber={1}", threadId, pageNumber);

            var web = new AwfulWebClient();
            var doc = web.FetchHtml(url.ToString());
            var result = ThreadPageParser.ParseThreadPage(doc);
            return result;
        }

        public static ThreadPageMetadata LoadPage(this ThreadMetadata thread, int pageNumber)
        {
           return FetchThreadPage(thread.ThreadID, pageNumber);
        }

        public static ThreadPageMetadata Refresh(this ThreadPageMetadata page)
        {
            return FetchThreadPage(page.ThreadID, page.PageNumber);
        }

        #endregion

        #region Forum Extensions

        private static ForumPageMetadata LoadPageFromUrl(string forumPageUrl)
        {
            var web = new AwfulWebClient();
            var doc = web.FetchHtml(forumPageUrl);
            var result = ForumParser.ParseForumPage(doc);
            return result;
        }

        private static ForumPageMetadata LoadPageFromForumId(string forumId, int pageNumber)
        {
            var url = new StringBuilder();
            // http://forums.somethingawful.com/forumdisplay.php
            url.AppendFormat("{0}/{1}", CoreConstants.BASE_URL, CoreConstants.FORUM_PAGE_URI);
            // ?forumid=<FORUMID>
            url.AppendFormat("?forumid={0}", forumId);
            // &daysprune=30&perpage=30&posticon=0sortorder=desc&sortfield=lastpost
            url.Append("&daysprune=30&perpage=30&posticon=0sortorder=desc&sortfield=lastpost");
            // &pagenumber=<PAGENUMBER>
            url.AppendFormat("&pagenumber={0}", pageNumber);

            return LoadPageFromUrl(url.ToString());
        }

        public static ForumPageMetadata LoadPage(this ForumMetadata forum, int pageNumber)
        {
            ForumPageMetadata page = null;
            if (forum is BookmarkMetadata)
            {
                string url = null;
                url = (forum as BookmarkMetadata).Url;
                page = LoadPageFromUrl(url);
            }
            else
            {
                page = LoadPageFromForumId(forum.ForumID, pageNumber);
            }
            return page;
        }

        public static ForumPageMetadata Refresh(this ForumPageMetadata page)
        {
            return LoadPageFromForumId(page.ForumID, page.PageNumber);
        }

        #endregion

        #region Private Message Extensions

        public static PrivateMessageMetadata Refresh(this PrivateMessageMetadata metadata)
        {
            int id = -1;
            if (int.TryParse(metadata.PrivateMessageId, out id))
                return PrivateMessageService.Service.FetchMessage(id);
            else
                return null;
        }

        public static IPrivateMessageRequest BeginForward(this PrivateMessageMetadata metadata)
        {
            IPrivateMessageRequest request = null;
            int id = -1;
            if (int.TryParse(metadata.PrivateMessageId, out id))
                request = PrivateMessageService.Service.BeginForwardToMessageRequest(id);

            return request;
        }

        public static IPrivateMessageRequest BeginReply(this PrivateMessageMetadata metadata)
        {
            IPrivateMessageRequest request = null;
            int id = -1;
            if (int.TryParse(metadata.PrivateMessageId, out id))
                request = PrivateMessageService.Service.BeginReplyToMessageRequest(id);

            return request;
        }

        public static bool Send(this IPrivateMessageRequest request)
        {
            return PrivateMessageService.Service.SendMessage(request);
        }

        public static bool Delete(this PrivateMessageMetadata metadata)
        {
            int id = -1;
            int folderId = -1;
            bool success = false;
            if (int.TryParse(metadata.PrivateMessageId, out id) &&
                int.TryParse(metadata.FolderId, out folderId))
            {
                success = PrivateMessageService.Service.DeleteMessage(id, folderId, folderId);
            }

            return success;
        }

        public static bool MoveTo(this PrivateMessageMetadata metadata, PrivateMessageFolderMetadata folder)
        {
            int id = int.Parse(metadata.PrivateMessageId);
            int srcId = int.Parse(metadata.FolderId);
            int destId = int.Parse(folder.FolderId);
            return PrivateMessageService.Service.MoveMessage(id, srcId, destId);
        }

        public static bool MoveTo(this IEnumerable<PrivateMessageMetadata> messages, PrivateMessageFolderMetadata folder)
        {
            var idList = new List<int>(messages.Count());
            string folderId = messages.First().FolderId;
            foreach (var message in messages)
            {
                if (folderId.Equals(message.FolderId))
                    throw new Exception("All messages must belong to the same folder.");

                int messageId = int.Parse(message.PrivateMessageId);
                idList.Add(messageId);
            }

            int srcId = int.Parse(folderId);
            int destId = int.Parse(folder.FolderId);
            return PrivateMessageService.Service.MoveMessages(idList, srcId, destId);
        }

        public static PrivateMessageFolderMetadata Refresh(this PrivateMessageFolderMetadata metadata)
        {
            int id = -1;
            PrivateMessageFolderMetadata folder = null;
            if (int.TryParse(metadata.FolderId, out id))
                folder = PrivateMessageService.Service.FetchFolder(id);

            return folder;
        }

        public static bool RenameTo(this PrivateMessageFolderMetadata metadata, string name)
        {
            bool success = false;
            var editor = PrivateMessageService.Service.BeginEditFolderRequest();
            editor.RenameFolder(metadata, name);
            success = editor.SendRequest();
            return success;
        }

        public static bool Delete(this PrivateMessageFolderMetadata metadata)
        {
            bool success = false;
            var editor = PrivateMessageService.Service.BeginEditFolderRequest();
            editor.DeleteFolder(metadata);
            success = editor.SendRequest();
            return success;
        }

        public static bool DeleteAllMessages(this PrivateMessageFolderMetadata metadata)
        {
            int folderId = int.Parse(metadata.FolderId);
            var messageIds = metadata.Messages.Select(message => int.Parse(message.PrivateMessageId)).ToList();
            return PrivateMessageService.Service.DeleteMessages(messageIds, folderId, folderId);
        }

        public static bool CreateNew(this PrivateMessageFolderMetadata metadata)
        {
            if (metadata.Name == null || metadata.Name == string.Empty)
                throw new Exception("Cannot create a new folder with no name!");

            bool success = false;
            var editor = PrivateMessageService.Service.BeginEditFolderRequest();
            editor.CreateFolder(metadata.Name);
            success = editor.SendRequest();
            return success;
        }

        #endregion

        #region User Extensions

        public static bool Login(this UserMetadata user, string password)
        {
            var login = new AwfulLoginClient();
            var cookies = login.Authenticate(user.Username, password);
            bool success = false;
            if (cookies != null)
            {
                success = true;
                user.Cookies = cookies;
                AwfulWebRequest.SetCredentials(user);
            }

            return success;
        }

        public static void MakeActive(this UserMetadata user)
        {
            if (!user.IsActive())
                AwfulWebRequest.SetCredentials(user);
        }

        public static bool IsActive(this UserMetadata user)
        {
            return AwfulWebRequest.ActiveUser.Equals(user) &&
                AwfulWebRequest.CanAuthenticate;
        }

        public static void Logout(this UserMetadata user)
        {
            if (user.IsActive())
            {
                AwfulWebRequest.ClearCredentials();
            }
        }
    
        public static IEnumerable<ForumMetadata> LoadForums(this UserMetadata user)
        {
            var forums = ForumTasks.FetchAllForums();
            return forums;
        }

        public static ForumPageMetadata LoadBookmarks(this UserMetadata user)
        {
            BookmarkMetadata data = new BookmarkMetadata();
            return data.LoadPage(0);
        }

        public static IEnumerable<PrivateMessageFolderMetadata> LoadPrivateMessageFolders(this UserMetadata user)
        {
            var messages = PrivateMessageService.Service.FetchFolders();
            return messages;
        }

        #endregion
    }
}
