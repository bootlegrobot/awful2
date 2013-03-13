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

        public static IEnumerable<TSource> Page<TSource>(this IEnumerable<TSource> source, int page, int pageSize)
        {
            return source.Skip((page - 1) * pageSize).Take(pageSize);
        }

        public static bool DeleteFileFromStorage(string filepath)
        {
            bool deleted = false;
            try
            {
                IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
               
                if (storage.FileExists(filepath))
                {
                    storage.DeleteFile(filepath);
                    deleted = true;
                }
            }
            catch (Exception ex) { AwfulDebugger.AddLog(new object(), AwfulDebugger.Level.Critical, ex); }
            return deleted;
        }

        public static void SafelyCreateDirectory(this IsolatedStorageFile storage, string dir)
        {
            if (!storage.DirectoryExists(dir))
                storage.CreateDirectory(dir);
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

                if (storage.FileExists(filePath))
                    storage.DeleteFile(filePath);

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
            AwfulDebugger.AddLog(item, AwfulDebugger.Level.Debug, "START SaveToFile()");
            AwfulDebugger.AddLog(item, AwfulDebugger.Level.Debug, "Filepath: " + filePath);

            bool result = false;
            try
            {
                AwfulDebugger.AddLog(item, AwfulDebugger.Level.Debug, "Getting application user store...");
                var storage = IsolatedStorageFile.GetUserStoreForApplication();
                if (storage.BuildPath(filePath))
                {
                    AwfulDebugger.AddLog(item, AwfulDebugger.Level.Debug, "Creating DataContractSerializer...");
                    DataContractSerializer dcs = new DataContractSerializer(item.GetType());
                    using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(filePath,
                        System.IO.FileMode.Create, System.IO.FileAccess.Write, storage))
                    {
                        AwfulDebugger.AddLog(item, AwfulDebugger.Level.Debug, "Writing serlialized object to filestream...");
                        dcs.WriteObject(fileStream, item);
                        result = true;
                        AwfulDebugger.AddLog(item, AwfulDebugger.Level.Debug, "Write complete.");
                        AwfulDebugger.AddLog(item, AwfulDebugger.Level.Info, "Save successful!");
                    }
                }
            }
            catch (Exception ex)
            {
                AwfulDebugger.AddLog(item, AwfulDebugger.Level.Info, "Save failed.");
                AwfulDebugger.AddLog(item, AwfulDebugger.Level.Critical, ex);
                result = false;
#if DEBUG
                throw ex;
#endif
            }

            AwfulDebugger.AddLog(item, AwfulDebugger.Level.Debug, "END SaveToFile()");
            return result;
        }

        public static T LoadFromFile<T>(string file)
        {
            AwfulDebugger.AddLog(file, AwfulDebugger.Level.Debug, "START LoadFromFile()");
            AwfulDebugger.AddLog(file, AwfulDebugger.Level.Info, string.Format("Loading file '{0}' of type '{1}' from storage...", file, typeof(T)));
 
            T result = default(T);
            try
            {
                AwfulDebugger.AddLog(file, AwfulDebugger.Level.Debug, "Getting application user store...");
                var storage = IsolatedStorageFile.GetUserStoreForApplication();
                if (!storage.FileExists(file))
                    return result;

                DataContractSerializer dcs = new DataContractSerializer(typeof(T));
                using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(file,
                    System.IO.FileMode.Open, System.IO.FileAccess.Read, storage))
                {
                    AwfulDebugger.AddLog(file, AwfulDebugger.Level.Debug, "Reading serlialized object from filestream...");
                    result = (T)dcs.ReadObject(fileStream);
                    AwfulDebugger.AddLog(file, AwfulDebugger.Level.Debug, "Read complete.");
                    AwfulDebugger.AddLog(file, AwfulDebugger.Level.Info, "Load successful!");
                }
            }
            catch (Exception ex)
            {
                AwfulDebugger.AddLog(file, AwfulDebugger.Level.Critical, ex);
                AwfulDebugger.AddLog(file, AwfulDebugger.Level.Info, "Load failed.");
                result = default(T);
            }

            AwfulDebugger.AddLog(file, AwfulDebugger.Level.Debug, "END LoadFile()");
            return result;
        }
    }

    public static class HtmlExtensions
    {
        internal static HtmlDocument ToHtmlDocument(this string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }

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

        public static ThreadMetadata AsSample(this ThreadMetadata data)
        {
            data.ThreadID = "1";
            data.Title = "Sample Thread Title";
            data.IsNew = true;
            data.IsSticky = true;
            data.Rating = 5;
            data.LastUpdated = DateTime.Now;
            data.ColorCategory = BookmarkColorCategory.Category0;
            data.Author = "Sample Thread Author";
            data.PageCount = 10;
            return data;
        }

        public static ThreadPostMetadata AsSample(this ThreadPostMetadata data)
        {
            var random = new Random();
            data.Author = "Sample Author";
            data.AuthorType = ThreadPostMetadata.PostType.Moderator;
            data.IsNew = true;
            data.PostID = random.Next().ToString();
            data.ThreadIndex = random.Next(0, 100);
            data.PostDate = DateTime.Now;
            data.PostBody = HtmlTextNode.CreateNode("<p>Sample Post Body.</p>");
            return data;
        }

        public static ThreadPageMetadata AsSample(this ThreadPageMetadata data)
        {
            data.ThreadTitle = "Sample Thread Title";
            data.ThreadID = "1";
            data.LastPage = 10;
            data.PageNumber = 1;
            data.RawHtml = "<html><head/><body>Sample Page Html</body></html>";
            data.Posts = new List<ThreadPostMetadata>(10);
            for (int i = 0; i < 10; i++)
            {
                var post = new ThreadPostMetadata().AsSample();
                post.PageIndex = i + 1;
                data.Posts.Add(post);
            }
            return data;
        }

        public static ThreadMetadata FromPageMetadata(ThreadPageMetadata page)
        {
            var data = new ThreadMetadata();
            data.ThreadID = page.ThreadID;
            data.Title = page.ThreadTitle;
            data.PageCount = page.LastPage;
            return data;
        }

        public static int Rate(this ThreadMetadata thread, int rating)
        {
             bool success = ThreadTasks.Rate(thread, rating);
             return success ? rating : -1;
        }

        public static string ToMetroStyle(this ThreadPageMetadata page)
        {
            return MetroStyler.Metrofy(page.Posts);
        }

        public static ThreadPageMetadata Page(this ThreadMetadata thread, int pageNumber)
        {
            return AwfulContentRequest.Threads.LoadThreadPage(thread.ThreadID, pageNumber);
        }

        public static ThreadPageMetadata ThreadPageFromUri(Uri uri)
        {
            return AwfulContentRequest.Threads.LoadThreadPage(uri);
        }

        public static ThreadPageMetadata FirstUnreadPost(this ThreadMetadata thread)
        {
            // adding some special logic here.
            // if the thread is new, then using 'goto=newpost' actually loads the last page.
            // in this case, users typically want the first unread post,
            // and for new threads, that would be the first page.

            return thread.IsNew ?
                AwfulContentRequest.Threads.LoadThreadPage(thread.ThreadID, 1) :
                AwfulContentRequest.Threads.LoadThreadUnreadPostPage(thread.ThreadID);
        }

        public static ThreadPageMetadata LastPage(this ThreadMetadata thread)
        {
            return AwfulContentRequest.Threads.LoadThreadLastPostPage(thread.ThreadID);
        }

        public static ThreadPageMetadata Refresh(this ThreadPageMetadata page)
        {
            return AwfulContentRequest.Threads.LoadThreadPage(page.ThreadID, page.PageNumber);
        }

        public static string Quote(this ThreadPostMetadata post)
        {
            return AwfulContentRequest.Threads.QuotePost(post.PostID);
        }

        public static bool MarkAsRead(this ThreadPostMetadata post)
        {
            return AwfulContentRequest.Threads.MarkPostAsRead(post);
        }

        public static IThreadPostRequest CreateReplyRequest(this ThreadMetadata thread)
        {
            return AwfulContentRequest.Threads.BeginReplyToThread(thread.ThreadID);
        }

        public static IThreadPostRequest BeginEdit(this ThreadPostMetadata post)
        {
            return AwfulContentRequest.Threads.BeginPostEdit(post.PostID);
        }

        #endregion

        #region Forum Extensions

        public static ForumMetadata AsSample(this ForumMetadata forum)
        {
            forum.ForumName = "Sample Forum";
            forum.ForumID = "1";
            forum.ForumGroup = ForumGroup.MAIN;
            forum.LevelCount = 1;
            return forum;
        }

        public static ForumPageMetadata Page(this ForumMetadata forum, int pageNumber)
        {
            ForumPageMetadata page = null;
            if (forum is UserBookmarksMetadata)
            {
                page = AwfulContentRequest.Forums.LoadUserBookmarks();
            }
            else
            {
                page = AwfulContentRequest.Forums.LoadForumPage(forum.ForumID, pageNumber);
            }
            return page;
        }

        public static ForumPageMetadata Refresh(this ForumPageMetadata page)
        {
            return AwfulContentRequest.Forums.LoadForumPage(page.ForumID, page.PageNumber);
        }

        #endregion

        #region Private Message Extensions

        public static PrivateMessageMetadata Refresh(this PrivateMessageMetadata metadata)
        {
            return AwfulContentRequest.Messaging.LoadMessage(metadata.PrivateMessageId);
        }

        public static IPrivateMessageRequest BeginForward(this PrivateMessageMetadata metadata)
        {
            var request = AwfulContentRequest.Messaging.ForwardMessage(metadata.PrivateMessageId);
            return request;
        }

        public static IPrivateMessageRequest BeginReply(this PrivateMessageMetadata metadata)
        {
            var request = AwfulContentRequest.Messaging.BeginReplyToMessage(metadata.PrivateMessageId);
            return request;
        }

        public static bool Send(this IPrivateMessageRequest request)
        {
            return AwfulContentRequest.Messaging.SendMessageRequest(request);
            
        }

        public static bool Delete(this PrivateMessageMetadata metadata)
        {
            bool success = AwfulContentRequest.Messaging.DeleteMessage(metadata.FolderId, metadata.PrivateMessageId);
            return success;
        }

        public static bool MoveTo(this PrivateMessageMetadata metadata, PrivateMessageFolderMetadata folder)
        {
            
            return AwfulContentRequest.Messaging.MoveMessage(
                metadata.FolderId,
                folder.FolderId,
                metadata.PrivateMessageId);
        }

        public static bool MoveTo(this IEnumerable<PrivateMessageMetadata> messages, PrivateMessageFolderMetadata folder)
        {
            var idList = new List<string>(messages.Count());

            string folderId = messages.First().FolderId;
            foreach (var message in messages)
            {
                if (folderId.Equals(message.FolderId))
                    throw new Exception("All messages must belong to the same folder.");

                idList.Add(message.PrivateMessageId);
            }

            return AwfulContentRequest.Messaging.MoveMessages(folderId, folder.FolderId, idList);
        }

        public static PrivateMessageFolderMetadata Refresh(this PrivateMessageFolderMetadata metadata)
        {
           
            return AwfulContentRequest.Messaging.LoadFolder(metadata.FolderId);
        }

        public static bool RenameTo(this PrivateMessageFolderMetadata metadata, string name)
        {
            bool success = AwfulContentRequest.Messaging.RenameFolder(metadata.FolderId, name);
            return success;
        }

        public static bool Delete(this PrivateMessageFolderMetadata metadata)
        {
            return AwfulContentRequest.Messaging.DeleteFolder(metadata.FolderId);
        }

        public static bool DeleteAllMessages(this PrivateMessageFolderMetadata metadata)
        {
            return AwfulContentRequest.Messaging.DeleteAllMessages(metadata.FolderId,
                metadata.Messages.Select(messsage => messsage.PrivateMessageId));
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

        public static bool AddToUserBookmarks(this UserMetadata user, ThreadMetadata thread)
        {
            return ThreadTasks.AddBookmark(thread);
        }

        public static bool RemoveFromUserBookmarks(this UserMetadata user, ThreadMetadata thread)
        {
            return ThreadTasks.RemoveBookmark(thread);
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
            UserBookmarksMetadata data = new UserBookmarksMetadata();
            return data.Page(0);
        }

        public static IEnumerable<PrivateMessageFolderMetadata> LoadPrivateMessageFolders(this UserMetadata user)
        {
            var messages = PrivateMessageService.Service.FetchFolders();
            return messages;
        }

        #endregion
    }
}