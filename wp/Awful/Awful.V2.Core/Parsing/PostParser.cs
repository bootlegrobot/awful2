using System;
using System.Linq;
using HtmlAgilityPack;
using System.Net;

namespace Awful
{
    public static class PostParser
    {
        private const string POST_ID_ATTRIBUTE = "id";

        public static ThreadPostMetadata ParsePost(HtmlNode postNode)
        {
            ThreadPostMetadata post = new ThreadPostMetadata()
                .ParseIcon(postNode)
                .ParseAuthor(postNode)
                .ParseContent(postNode)
                .ParsePostDate(postNode)
                .ParsePostID(postNode)
                .ParseUserID(postNode)
                .ParsePostThreadIndexAndMarkUrl(postNode)
                .ParseHasSeen(postNode);

            return post;
        }

        private static ThreadPostMetadata ParseContent(this ThreadPostMetadata post, HtmlNode postNode)
        {
            var content = postNode.Descendants("td")
                .Where(node => node.GetAttributeValue("class", "")
                    .Equals("postbody"))
                .FirstOrDefault();

            if (content == null)
                throw new ArgumentException("Content should not be null");

            post.PostBody = content;
            return post;
        }

        private static ThreadPostMetadata ParseIcon(this ThreadPostMetadata post, HtmlNode postNode)
        {
            try
            {
                var uriString = postNode.Descendants()
                   .Where(node => node.GetAttributeValue("class", "").Equals("title"))
                   .First()
                   .Descendants("img")
                   .First()
                   .GetAttributeValue("src", "");

                post.PostIconUri = new Uri(uriString, UriKind.Absolute);
                post.ShowIcon = true;
            }

            catch (Exception)
            {
                post.PostIconUri = null;
                post.ShowIcon = false;
            }

            return post;
        }

        private static ThreadPostMetadata ParseUserID(this ThreadPostMetadata post, HtmlNode postNode)
        {
            var userIDNode = postNode.Descendants()
                .Where(node => node.GetAttributeValue("class", "").Contains("userid"))
                .FirstOrDefault();

            if (userIDNode != null)
            {
                string value = userIDNode.GetAttributeValue("class", "");
                value = value.Replace("userinfo userid-", "");
                post.UserID = value;
            }

            return post;
        }

        private static ThreadPostMetadata ParsePostDate(this ThreadPostMetadata post, HtmlNode postNode)
        {
            var postDateNode = postNode.Descendants()
              .Where(node => node.GetAttributeValue("class", "").Equals("postdate"))
              .FirstOrDefault();

            var postDateString = postDateNode == null ? string.Empty : postDateNode.InnerText;

            try
            {
                post.PostDate = postDateNode == null ? default(DateTime) :
                    Convert.ToDateTime(postDateString.SanitizeDateTimeHTML());
            }

            catch (Exception)
            {
                post.PostDate = DateTime.Parse(postDateString.SanitizeDateTimeHTML(), System.Globalization.CultureInfo.InvariantCulture);
            }

            return post;
        }

        private static ThreadPostMetadata ParseHasSeen(this ThreadPostMetadata post, HtmlNode postNode)
        {
            var hasSeenMarker = postNode.Descendants("tr")
                .Where(node => node.GetAttributeValue("class", "").Contains(CoreConstants.LASTREAD_FLAG))
                .FirstOrDefault();

            var hasNotSeenMarker = postNode.Descendants("img")
            .Where(node => node.GetAttributeValue("src", "")
                .Equals(CoreConstants.NEWPOST_GIF_URL)).FirstOrDefault();

            bool firstGuess = hasSeenMarker != null;
            bool secondGuess = hasNotSeenMarker == null;

            post.IsNew = !(firstGuess || secondGuess);
            return post;
        }

        private static ThreadPostMetadata ParseAuthor(this ThreadPostMetadata post, HtmlNode postNode)
        {
            var authorNode = postNode.Descendants()
              .Where(node =>
                  (node.GetAttributeValue("class", "").Equals("author")) ||
                  (node.GetAttributeValue("class", "").Equals("author op")) ||
                  (node.GetAttributeValue("title", "").Equals("Administrator")) ||
                  (node.GetAttributeValue("title", "").Equals("Moderator")))
              .FirstOrDefault();

            if (authorNode != null)
            {
                var type = authorNode.GetAttributeValue("title", "");
                switch (type)
                {
                    case "Administrator":
                        post.AuthorType = ThreadPostMetadata.PostType.Administrator;
                        break;

                    case "Moderator":
                        post.AuthorType = ThreadPostMetadata.PostType.Moderator;
                        break;

                    default:
                        post.AuthorType = ThreadPostMetadata.PostType.Standard;
                        break;
                }

                post.Author = authorNode.InnerText;
            }

            else
            {
                post.Author = "AwfulPoster";
                post.AuthorType = ThreadPostMetadata.PostType.Standard;
            }

            return post;
        }

        private static ThreadPostMetadata ParsePostThreadIndexAndMarkUrl(this ThreadPostMetadata post, HtmlNode postNode)
        {
            var seenUrlNode = postNode.Descendants("a")
                .Where(node => node.GetAttributeValue("class", "").Contains(MARK_THREAD_CLASS_ID))
                .FirstOrDefault();

            if (seenUrlNode == null)
            {
                post.ThreadIndex = -1;
            }

            else
            {
                // make sure the string is in the right format so the uri class can parse correctly.
                var nodeValue = seenUrlNode.GetAttributeValue("href", "");
                post.MarkPostUri = new Uri(string.Format("http://forums.somethingawful.com{0}", 
                    HttpUtility.HtmlDecode(nodeValue)), UriKind.Absolute);
                int index = -1;
                string indexValue = nodeValue.Split('&').LastOrDefault();
                if (indexValue != null)
                {
                    indexValue = indexValue.Split('=').Last();
                    post.ThreadIndex = int.TryParse(indexValue, out index)
                        ? index
                        : -1;
                }
            }

            return post;
        }

        private static ThreadPostMetadata ParsePostID(this ThreadPostMetadata post, HtmlNode postNode)
        {
            string idValue = postNode.GetAttributeValue(POST_ID_ATTRIBUTE, "");

            string result = null;

            if (idValue != null)
            {
                string postID = idValue.Replace("post", "");
                result = postID;
            }

            post.PostID = result;
            return post;
        }

        public const string MARK_THREAD_CLASS_ID = "lastseen_icon";
    }
}
