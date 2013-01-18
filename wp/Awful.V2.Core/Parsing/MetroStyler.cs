using System;
using System.Text;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace Awful
{
    public static class MetroStyler
    {
        public static string Metrofy(ICollection<ThreadPostMetadata> posts)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<table>");

            int postCount = posts.Count;

            foreach (var item in posts)
            {
                try
                {
                    var post = item;

                    html.Append("<tr><td>");
                    html.AppendFormat("<a href='#post_{0}' id='postlink{0}' style='display:none'>This should be hidden.</a>", post.PageIndex);
                    html.AppendFormat("<div class='post_header' id='post_{0}' onclick=\"javascript:openPostMenu('{0}')\">", post.PageIndex);

                    if (post.ShowIcon)
                    {
                        Uri iconUri = post.PostIconUri;
                        html.AppendFormat("<img class='post_icon' alt='' src='{0}' />", iconUri.AbsoluteUri);
                        html.AppendFormat("<div class='post_header_text' >", post.PageIndex);
                        html.Append(AppendPostAuthor(post));
                        html.AppendFormat("<span class='text_subtlestyle'>#{0}/{1}, {2}</span>",
                            post.PageIndex,
                            postCount,
                            post.PostDate);
                    }
                    else
                    {
                        html.Append("<div class='post_header_text_noicon'>");
                        html.Append(AppendPostAuthor(post));
                        html.AppendFormat("<span class='text_subtlestyle'>#{0}/{1}, {2}</span>",
                            post.PageIndex,
                            postCount,
                            post.PostDate.ToString("MM/dd/yyyy, h:mm tt"));
                    }

                    var node = new HtmlNode(HtmlNodeType.Element, post.PostBody.OwnerDocument, -1)
                    {
                        InnerHtml = post.PostBody.InnerHtml,
                        Name = post.PostBody.Name
                    };

                    var content = new ForumContentParser(node.FirstChild).Body;

                    html.AppendFormat("</div></div><div class='{0}'><pre>{1}</pre></div></pre></td></tr>",
                        !post.IsNew ? "post_content_seen" : "post_content",
                        content);
                }

                catch (Exception ex)
                {
                    string error = string.Format("There was an error while merging posts. [{0}] {1}", ex.Message, ex.StackTrace);
                }
            }

            html.Append("</table>");
            string result = html.ToString();
            return result;
        }

        private static string AppendPostAuthor(ThreadPostMetadata post)
        {
            string style = string.Empty;
            switch (post.AuthorType)
            {
                case ThreadPostMetadata.PostType.Administrator:
                    style = "admin_post";
                    break;

                case ThreadPostMetadata.PostType.Moderator:
                    style = "mod_post";
                    break;

                case ThreadPostMetadata.PostType.Standard:
                    style = "user_post";
                    break;
            }

            return string.Format("<span class='text_title3style'><span class='{0}'>{1}</span></span><br/>", style, post.Author);
        }
    }
}
