using System;
using System.Linq;
using HtmlAgilityPack;
using System.Text;
using Kollasoft;

namespace Awful
{
    public abstract class AwfulPostWebParser
    {
        public abstract string Body { get; }

        public abstract string HandleText(string text);
        public abstract string HandleNode(HtmlNode node);
        public abstract string HandleSpoilers(HtmlNode node);
        public abstract string HandleQuotes(HtmlNode node);
        public abstract string HandleList(HtmlNode node);
        public abstract string HandleImages(HtmlNode node);
        public abstract string HandleLinks(HtmlNode node);
        public abstract string HandleObjects(HtmlNode node);
        
        protected virtual string ParseNode(HtmlNode node)
        {
            StringBuilder builder = new StringBuilder();
            while (node != null)
            {
                try { ProcessNode(node, builder); }
                catch (Exception ex)
                {
                    KSDebugger.AddLog(this,KSDebugger.Level.Critical, ex);
                }

                node = node.NextSibling;
            }
            return builder.ToString();
        }

        private void ProcessNode(HtmlNode node, StringBuilder builder)
        {
            switch (node.Name)
            {
                case "#text":
                    builder.Append(HandleText(node.InnerText));
                    break;

                case "span":
                    builder.Append(HandleSpoilers(node));
                    break;

                case "img":
                    builder.Append(HandleImages(node));
                    break;

                case "a":
                    builder.Append(HandleLinks(node));
                    break;

                case "div":
                    builder.Append(HandleQuotes(node));
                    break;

                case "iframe":
                    builder.Append(HandleObjects(node));
                    break;

                case "object":
                    builder.Append(HandleObjects(node));
                    break;

                default:
                    builder.Append(HandleNode(node));
                    break;
            }
        }
    }

    public class ForumContentParser : AwfulPostWebParser
    {
        private string body;
        public override string Body
        {
            get { return body; }
        }

        public ForumContentParser() { }

        public ForumContentParser(HtmlNode node)
        {
            body = ParseNode(node);
        }

        public override string HandleText(string text)
        {
            return Sanitize(text);
        }
        public override string HandleNode(HtmlNode node)
        {
            var innerNode = new ForumContentParser(node.FirstChild).Body;
            node.InnerHtml = innerNode;
            return node.OuterHtml;
        }
        public override string HandleSpoilers(HtmlNode node)
        {
            if (node.GetAttributeValue("class", "").Equals("bbc-spoiler"))
            {
                var spoilerContent = new SpoilerWebParser(node.FirstChild);
                return spoilerContent.Body;
            }
            return String.Empty;
        }
        public override string HandleQuotes(HtmlNode node)
        {
            if (node.GetAttributeValue("class", "").Equals("bbc-block"))
            {
                var quoteNode = node.Descendants("blockquote").First();
                var authorNode = node.Descendants("h4").FirstOrDefault();
                string author = null;
                if (authorNode != null)
                {
                    author = authorNode.InnerText.Replace(" posted:", "");
                }

                var parser = new QuoteWebParser(author, quoteNode.FirstChild);
                return parser.Body;
            }
            else
            {
                var body = new ForumContentParser(node.FirstChild).Body;
                return string.Format("<br/><blockquote>{0}</blockquote><br/>", body);
            }
            //return string.Empty;
        }
        public override string HandleList(HtmlNode node)
        {
            return node.OuterHtml;
        }
        public override string HandleImages(HtmlNode node)
        {
            if (node.Attributes.Contains("class"))
            {
                node.Attributes["class"].Value = "img";
            }
            else
            {
                node.Attributes.Add("class", "img");
            }
            string url = node.Attributes["src"].Value;
            
            string smiley = url.Replace("fi.somethingawful", "");
            smiley = url.Replace("i.somethingawful", "");

            if (url.Equals(smiley))
            {
                string html = new ImageWebParser(node).Body;
                return html;
            }
            return node.OuterHtml;
        }
        public override string HandleLinks(HtmlNode node)
        {
            string url = node.Attributes["href"].Value;

            node.Attributes["href"].Value = String.Format("javascript:navigate('a{0}{1}')",
                CoreConstants.DEMARC, url);

            var textNode = node.FirstChild as HtmlTextNode;
            
            if (textNode != null)
            {
                //if (textNode.Text.Length > 25)
                    //textNode.Text = String.Format("{0}&shy;", node.InnerText);
            }
            else
            {
                ForumContentParser main = new ForumContentParser(node.FirstChild);
                node.InnerHtml = main.Body;
            }
            return string.Format("{0}", node.OuterHtml);
        }

        private string Sanitize(string text)
        {
            text = text.Replace("\r", "");
            text = text.Replace("\n", "");
            text = text.Replace("\t", "");

            text = text.Replace("&nbsp;", "");

            text = Helpers.ContentFilter.Censor(text);
            return text;
        }

        public override string HandleObjects(HtmlNode node)
        {
            /*
            var embedNode = node.Descendants("iframe")
                .Where(videoNode => videoNode.GetAttributeValue("class", string.Empty).Equals("youtube-player"))
                .FirstOrDefault();

            if (embedNode != null)
            {
                var src = embedNode.GetAttributeValue("src", "");
                src = src.Replace("-nocookie", "");
                src = src.Replace("/v/", "&");
                var tokens = src.Split('&');
                src = string.Format("{0}/watch?v={1}", tokens[0], tokens[1]);
                return string.Format("<a href=\"javascript:navigate('a{0}{1}')\">[click for video]</a>", CoreConstants.DEMARC, src);
            }
            */

            return new EmbeddedVideoWebParser(node, false).Body;
        }
    }

    public class QuoteWebParser : ForumContentParser
    {
        private string content;
        public override string Body { get { return content; } }

        private StringBuilder builder;

        public QuoteWebParser(string author, HtmlNode node) : base()
        {
            builder = new StringBuilder();
            builder.Append(@"<br/><div class=""bbc-block""><blockquote><div class=""quote-text"">");
            builder.Append(ParseNode(node));
            builder.Append("</div>");
            if (author != null)
            {
                /*
                    builder.AppendFormat(
                    @"<table style=""width: 100%""><tr><td class=""quote-author-grid"" colspan=""2"">
                      <pre class='quote-author'>- {0}</pre></td></tr></table>", author);
                */

                builder.AppendFormat("<div class='quote-author'>- {0}</div><br/><br/>", author);
            }
            builder.Append("</div><br/>");
            content = builder.ToString();
        }

        public override string HandleObjects(HtmlNode node)
        {
            return new EmbeddedVideoWebParser(node, true).Body;
        }

        public override string HandleImages(HtmlNode node)
        {
            if (node.Attributes.Contains("class"))
            {
                node.Attributes["class"].Value = "img";
            }
            else
            {
                node.Attributes.Add("class", "img");
            }
            string url = node.Attributes["src"].Value;

            string smiley = url.Replace(CoreConstants.SMILEY_PREFIX_1, "");
            smiley = url.Replace(CoreConstants.SMILEY_PREFIX_2, "");

            string html = String.Empty;
            if (url.Equals(smiley))
            {
                var parser = new ImageWebParser(node) { IsInQuote = true };
                html = parser.Body;
                return html;
            }
            return node.OuterHtml;
        }
    }

    public abstract class ShowHideWebParser : ForumContentParser
    {
        protected static Random idGenerator = new Random();

        public enum InlineStyle { Block, Inline }

        private readonly InlineStyle _inlineStyle;

        private string body;
        public override string Body
        {
            get
            {
                if (body == null)
                {
                    body = GetBody();
                }
                return body;
            }
        }

        public ShowHideWebParser(InlineStyle style)
            : base()
        {
            this._inlineStyle = style;
        }

        public ShowHideWebParser() : this(InlineStyle.Block) { }

        protected virtual string GetBody()
        {
            int id = idGenerator.Next();
            string trigger = String.Format("spoiler-trigger-{0}", id);
            string spoiler = String.Format("spoiler-{0}", id);

            string content = GetContent();
            return string.Format(@"
                <a href=""javascript:;"" id=""{0}"" class=""spoiler-trigger"" onclick=""show_spoiler('{0}', '{1}', '{4}');"">{3}</a>
                <span id=""{1}"" style=""display:none"">{2}</span>",
                trigger,
                spoiler,
                content,
                GetHideDisplay(),
                CoreConstants.DEMARC);
        }
        protected abstract string GetContent();
        protected abstract string GetHideDisplay();
    }

    public class SpoilerWebParser : ShowHideWebParser
    {
        private HtmlNode node;
        public SpoilerWebParser(HtmlNode node) : base(InlineStyle.Inline)
        {
            this.node = node;
        }

        protected override string GetContent()
        {
            return ParseNode(node).Trim();
        }

        protected override string GetHideDisplay()
        {
            return "[tap for spoiler]";
        }
    }

    public class EmbeddedVideoWebParser : ShowHideWebParser
    {
        private HtmlNode node;
        private bool isInQuoteBlock;

        public EmbeddedVideoWebParser(HtmlNode node, bool isInQuoteBlock)
            : base(InlineStyle.Block)
        {
            this.isInQuoteBlock = isInQuoteBlock;
            this.node = node;
        }

        public override string HandleObjects(HtmlNode node)
        {
            if (node != null)
            {
                // shrink window to 75% of page; 50% if in quotes
                node.SetAttributeValue("width", isInQuoteBlock ? "160" : "240");
                node.SetAttributeValue("height", isInQuoteBlock ? "120" : "180");
            }

            return node.OuterHtml;
        }

        protected override string GetContent()
        {
            return ParseNode(node);
        }

        protected override string GetHideDisplay()
        {
            return "[tap for video]";
        }
    }

    public class ImageWebParser : ShowHideWebParser
    {
        private HtmlNode node;
        public bool IsInQuote { get; set; }

        public ImageWebParser(HtmlNode node)
            : base()
        {
            this.node = node;
        }

        protected override string GetBody()
        {
            int id = idGenerator.Next();
            string triggerID = String.Format("img-trigger-{0}", id);
            string imageID = String.Format("img-{0}", id);
            string url = GetContent();
            string result = string.Empty;

            if (!IsInQuote)
            {
                result = string.Format(@"
                <a href=""javascript:show_image('{0}','{1}','{2}','{4}')"" id=""{0}"" style=""color: red"">{3}</a>
                <div><img id=""{1}"" onclick=""javascript:showImageMenu('{2}','{4}');"" class=""img"" border=""1"" style=""display:none""/></div>",
                    triggerID,
                    imageID,
                    url,
                    GetHideDisplay(),
                    CoreConstants.DEMARC);
            }
            else
            {
                result = string.Format(@"
                <a href=""javascript:show_quoted_image('{0}','{1}','{2}','{4}')"" id=""{0}"" style=""color: red"">{3}</a>
                <div><img id=""{1}"" onclick=""javascript:showImageMenu('{2}','{4}');"" class=""img"" border=""1"" style=""display:none""/></div>",
                  triggerID,
                  imageID,
                  url,
                  GetHideDisplay(),
                  CoreConstants.DEMARC);
            }

            return result;
        }

        protected override string GetContent()
        {
            string url = String.Empty;
            try 
            { 
                url = node.Attributes["src"].Value;
                if (url.Contains(CoreConstants.ATTACHMENT_URI))
                {
                    url = String.Format("{0}/{1}", CoreConstants.BASE_URL, url);
                }
            }
            catch (Exception) { }
            return url;
        }

        protected override string GetHideDisplay()
        {
            return CoreConstants.HIDE_IMAGE_TEXT;
        }
    }
}
