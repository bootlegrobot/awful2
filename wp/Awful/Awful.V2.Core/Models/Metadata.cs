using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using HtmlAgilityPack;
using System.Runtime.Serialization;
using System.Net;

namespace Awful
{
    [DataContract]
    public class ForumMetadata
    {
        [DataMember]
        public string ForumName { get; set; }
        [DataMember]
        public string ForumID { get; set; }
        [DataMember]
        public ForumGroup ForumGroup { get; set; }
        [DataMember]
        public int LevelCount { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[");
            builder.AppendLine("[" + ForumName + "]");
            builder.AppendLine("[ForumId: " + ForumID + "]");
            builder.AppendLine("[ForumGroup: " + ForumGroup + "]");
            builder.AppendLine("[LevelCount: " + LevelCount + "]");
            builder.AppendLine("]");
            return builder.ToString();
        }
    }

    public class UserBookmarksMetadata : ForumMetadata
    {
        public string Url { get { return CoreConstants.USERCP_URI; } }
        public UserBookmarksMetadata()
        {
            ForumName = "bookmarks";
            ForumID = "-1";
        }
    }

    public class ForumPageMetadata
    {
        public string ForumID { get; set; }
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
        public IList<ThreadMetadata> Threads { get; set; }
    }

    public enum BookmarkColorCategory
    {
        Unknown = 0,
        Category0,
        Category1,
        Category2,
    }

    [DataContract]
    public class ThreadMetadata : IEquatable<ThreadMetadata>
    {
        public const int NO_UNREAD_POSTS_POSTCOUNT = -1;

        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string ThreadID { get; set; }
        [DataMember]
        public string Author { get; set; }
        [DataMember]
        public int ReplyCount { get; set; }
        [DataMember]
        public int PageCount { get; set; }
        [DataMember]
        public int NewPostCount { get; set; }
        [DataMember]
        public bool ShowPostCount { get; set; }
        [DataMember]
        public bool IsNew { get; set; }
        [DataMember]
        public bool IsSticky { get; set; }
        [DataMember]
        public int Rating { get; set; }
        [DataMember]
        public DateTime? LastUpdated { get; set; }
        [DataMember]
        public string IconUri { get; set; }
        [DataMember]
        public BookmarkColorCategory ColorCategory { get; set; }
        
        public bool Equals(ThreadMetadata other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;
            return this.ThreadID.Equals(other.ThreadID);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ThreadMetadata))
                return false;

            return Equals(obj as ThreadMetadata);
        }

        public override int GetHashCode()
        {
            return this.ThreadID.GetHashCode();
        }
    }

    public enum ThreadPageType
    {
        Last = -1,
        NewPost = 0
    }

    public class ThreadPageMetadata
    {
        public string ThreadTitle { get; set; }
        public string ThreadID { get; set; }
        public int LastPage { get; set; }
        public int PageNumber { get; set; }
        public string RawHtml { get; set; }
        public IList<ThreadPostMetadata> Posts { get; set; }
    }

    [DataContract]
    public class ThreadPostMetadata
    {
        [DataMember]
        public DateTime PostDate { get; set; }
        [DataMember]
        public Uri PostIconUri { get; set; }
        [DataMember]
        public bool ShowIcon { get; set; }
        [DataMember]
        public string UserID { get; set; }
        [DataMember]
        public string PostID { get; set; }
        [DataMember]
        public string Author { get; set; }
        [DataMember]
        public PostType AuthorType { get; set; }
        [IgnoreDataMember]
        public HtmlNode PostBody { get; set; }
        [DataMember]
        public int PageIndex { get; set; }
        [DataMember]
        public int ThreadIndex { get; set; }
        [DataMember]
        public bool IsNew { get; set; }
        [DataMember]
        public Uri MarkPostUri { get; set; }

        public enum PostType
        {
            Standard,
            Administrator,
            Moderator
        }
    }

    [DataContract]
    public class UserMetadata
    {
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public List<Cookie> Cookies { get; set; }
    }

    [DataContract]
    public class TagMetadata
    {
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Value { get; set; }
        [DataMember]
        public string TagUri { get; set; }

        public static readonly TagMetadata NoTag;
        static TagMetadata() { NoTag = new TagMetadata() { Title = "No Icon", Value = "0", TagUri = string.Empty }; }

        public static IEnumerable<TagMetadata> LoadSmileyList()
        {
            return AwfulContentRequest.Smilies.LoadSmilies();
        }
    }

    [DataContract]
    public class PrivateMessageMetadata
    {
        public enum MessageStatus
        {
            Unknown=0,
            New,
            Read,
            Cancelled,
            Replied,
            Forwarded
        };

        [DataMember]
        public string Subject { get; set; }
        [DataMember]
        public string To { get; set; }
        [DataMember]
        public string From { get; set; }
        [DataMember]
        public string Body { get; set; }
        [DataMember]
        public string PrivateMessageId { get; set; }
        [DataMember]
        public MessageStatus Status { get; set; }
        [DataMember]
        public DateTime? PostDate { get; set; }
        [DataMember]
        public string FolderId { get; set; }
    }

    [DataContract]
    public class PrivateMessageFolderMetadata
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string FolderId { get; set; }
        [DataMember]
        public ICollection<PrivateMessageMetadata> Messages { get; set; }

        public static bool CreateNew(string name)
        {
            return AwfulContentRequest.Messaging.CreateNewFolder(name);
        }
    }
}
