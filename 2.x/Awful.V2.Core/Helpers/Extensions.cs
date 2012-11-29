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

        public static string CreatePageUrl(this ThreadMetadata data, int page = -1)
        {
            StringBuilder url = new StringBuilder();
            url.AppendFormat("http://forums.somethingawful.com/displaythread.php?threadid={0}", data.ThreadID);
            if (page == -1)
                url.Append("&goto=newpost");
            else
                url.AppendFormat("&pagenumber={0}", page);

            return url.ToString();
        }

        public static ThreadPageMetadata FetchThreadPage(string threadId, int pageNumber)
        {
            // http://forums.somethingawful.com/showthread.php?noseen=0&threadid=3439182&pagenumber=69
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

        public static ThreadPageMetadata FetchThreadPage(this ThreadMetadata thread, int pageNumber)
        {
           return FetchThreadPage(thread.ThreadID, pageNumber);
        }

        public static ThreadPageMetadata FetchThreadPage(this ThreadPageMetadata page)
        {
            return FetchThreadPage(page.ThreadID, page.PageNumber);
        }

        public static ForumPageMetadata FetchForumPage(this ForumMetadata forum, int pageNumber)
        {
            var url = new StringBuilder();
            if (forum is BookmarkMetadata)
                url.Append((forum as BookmarkMetadata).Url);
            else
            {
                // http://forums.somethingawful.com/forumdisplay.php
                url.AppendFormat("{0}/{1}", CoreConstants.BASE_URL, CoreConstants.FORUM_PAGE_URI);
                // ?forumid=<FORUMID>
                url.AppendFormat("?forumid={0}", forum.ForumID);
                // &daysprune=30&perpage=30&posticon=0sortorder=desc&sortfield=lastpost
                url.Append("&daysprune=30&perpage=30&posticon=0sortorder=desc&sortfield=lastpost");
                // &pagenumber=<PAGENUMBER>
                url.AppendFormat("&pagenumber={0}", pageNumber);
            }
            var web = new AwfulWebClient();
            var doc = web.FetchHtml(url.ToString());
            var result = ForumParser.ParseForumPage(doc);
            return result;
        }
        
    }
}
