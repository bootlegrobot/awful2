using System;
using System.Linq;
using HtmlAgilityPack;
using Awful.Core.Models.Messaging.Interfaces;
using System.Collections.Generic;
using Awful.Core.Models;
using Awful.Core.Models.Messaging;

namespace Awful.Core.Web.Parsers
{
    public static class AwfulPrivateMessageParser
    {
        private const string NEW_MESSAGE_AUTHOR = "author";
        private const string NEW_MESSAGE_POSTBODY = "postbody";
        private const string NEW_MESSAGE_TOUSER = "touser";
        private const string NEW_MESSAGE_TITLE = "title";
        private const string NEW_MESSAGE_MESSAGE = "message";
        private const string NEW_MESSAGE_FOWARD = "forward";
        private const string NEW_MESSAGE_POSTICON = "posticon";
        private const string NEW_MESSAGE_INPUT_TAG = "input";
        private const string NEW_MESSAGE_ICON_TAG = "div";

        private const string NEW_MESSAGE_UNKNOWN_AUTHOR = "Mystery Goon?";
        private const string NEW_MESSAGE_PRIVATEMESSAGEID = "privatemessageid";

        private const string PRIVATE_MESSAGE_ICON_NEW = "newpm.gif";
        private const string PRIVATE_MESSAGE_ICON_READ = "pm.gif";
        private const string PRIVATE_MESSAGE_ICON_CANCELLED = "trashcan.gif";
        private const string PRIVATE_MESSAGE_ICON_REPLIED = "pmreplied.gif";
        private const string PRIVATE_MESSAGE_ICON_FORWARDED = "pmforwarded.gif";
        
        private const string PRIVATE_MESSAGE_UNKNOWN_AUTHOR = "Unknown Author";
        private const string PRIVATE_MESSAGE_UNKNOWN_TITLE = "Unknown Title";

        private const string PRIVATE_MESSAGE_EMPTY_FOLDER_TEXT = "There are no messages to display in this folder for this time period.";

        #region public methods

        /// <summary>
        /// TODO: ADD SUMMARY.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static ICollection<IPrivateMessageFolder> ParseFolderList(HtmlDocument doc)
        {
            List<IPrivateMessageFolder> folders = new List<IPrivateMessageFolder>();
            // get select option nodes
            var top = doc.DocumentNode;
            var selectNode = top.Descendants("select")
                .Where(node => node.GetAttributeValue("name", "").Equals("folderid"))
                .FirstOrDefault();

            if (selectNode != null)
            {
                var optionNodes = selectNode.Descendants("option");
                foreach (var option in optionNodes)
                {
                    try
                    {
                        AwfulPrivateMessageFolder folder = new AwfulPrivateMessageFolder();
                        string name = option.NextSibling.InnerText.Trim();
                        string value = option.GetAttributeValue("value", "");
                        int id = 0;
                        if (int.TryParse(value, out id)) { folder.FolderID = id; }
                        folder.Name = name;
                        folders.Add(folder);
                    }
                    catch (Exception) { }
                }
            }
            return folders;
        }
        /// <summary>
        /// Parses the html from SA's new message web page for a collection of valid message tags.
        /// </summary>
        /// <param name="doc">The html document.</param>
        /// <returns>A collection of valid private message icons.</returns>
        public static ICollection<IPrivateMessageIcon> ParseNewPrivateMessageIcons(HtmlDocument doc)
        {
            var top = doc.DocumentNode;
            var iconArray = top.Descendants(NEW_MESSAGE_ICON_TAG)
                .Where(node => node.GetAttributeValue("class", "").Equals(NEW_MESSAGE_POSTICON))
                .ToArray();

            List<IPrivateMessageIcon> result = new List<IPrivateMessageIcon>();
            foreach (var icon in iconArray)
            {
                var inputNode = icon.Descendants(NEW_MESSAGE_INPUT_TAG).FirstOrDefault();
                var imgNode = icon.Descendants("img").FirstOrDefault();

                if (inputNode == null && imgNode == null)
                    result.Add(AwfulTag.NoIcon);

                else
                {
                    string iconValue = inputNode.GetAttributeValue("value", "");
                    string iconuri = string.Empty;
                    string title = string.Empty;

                    iconuri = imgNode.GetAttributeValue("src", "");
                    title = imgNode.GetAttributeValue("alt", "");
                    result.Add(new AwfulTag(title, iconValue, iconuri));
                }
            }

            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static IList<IPrivateMessage> ParseMessageList(HtmlDocument doc)
        {
            List<IPrivateMessage> messages = new List<IPrivateMessage>();
            var top = doc.DocumentNode;
            var messagesNode = top.Descendants("table").FirstOrDefault();
            var messageTable = messagesNode.Descendants("tr").ToArray();

            // remove last element from the table
            messageTable[messageTable.Length - 1] = null;

            foreach (var item in messageTable)
            {
               
                if (item != null && string.IsNullOrEmpty(item.GetAttributeValue("class", "")) == true)
                {
                    if (item.InnerText.Contains(PRIVATE_MESSAGE_EMPTY_FOLDER_TEXT)) break;
                    var message = new AwfulPrivateMessage();
                    var rows = item.Descendants("td").ToArray();
                    var statusNode = rows[0];
                    message.Status = GetMessageStatusFromNode(statusNode);
                    // skip the thread tag node, since I'm not using those yet
                    var titleNode = rows[2];
                    // title node has our subject and message id
                    message.Subject = GetMessageTitleFromNode(titleNode);
                    message.PrivateMessageID = GetMessageIDFromNode(titleNode);
                    // author node is next <td>
                    var authorNode = rows[3];
                    message.From = GetMessageAuthorFromNode(authorNode);

                    // postmark node is next <td>
                    var postmarkNode = rows[4];
                    message.Postmark = GetPostMarkFromNode(postmarkNode);
                    messages.Add(message);
                }
            }

            return messages;
        }
        /// <summary>
        /// Parses the html from SA's new message page and constructs a private message request
        /// object.
        /// </summary>
        /// <param name="doc">The html document.</param>
        /// <returns>A private message request, which can be sent or discarded.</returns>
        public static IPrivateMessageRequest ParseNewPrivateMessageFormDocument(HtmlDocument doc)
        {
            var pmRequest = new AwfulPrivateMessageRequest();
            var top = doc.DocumentNode;
            var inputArray = top.Descendants("input").ToArray();

            string toUser = GetInputValue(inputArray, NEW_MESSAGE_TOUSER);
            string title = GetInputValue(inputArray, NEW_MESSAGE_TITLE);
            string forward = GetInputValue(inputArray, NEW_MESSAGE_FOWARD);

            var messageNode = top.Descendants("textarea")
                .Where(node => node.GetAttributeValue("name", "").Equals(NEW_MESSAGE_MESSAGE))
                .SingleOrDefault();

            string message = messageNode == null ? string.Empty : messageNode.InnerText;

            pmRequest.Body = message;
            pmRequest.To = toUser;
            pmRequest.Subject = title;
            pmRequest.IsForward = forward != string.Empty;

            return pmRequest;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static IPrivateMessage ParsePrivateMessageDocument(HtmlDocument doc)
        {
            var pm = new AwfulPrivateMessage();
            var top = doc.DocumentNode;

            // **** PARSE BODY *****
            var postBodyNode = top.Descendants("td")
                .Where(node => node.GetAttributeValue("class", "").Equals(NEW_MESSAGE_POSTBODY))
                .SingleOrDefault();

            if (postBodyNode != null)
                pm.Body = new MainWebParser(postBodyNode).Body;

            // ***** PARSE AUTHOR *****
            var authorNode = top.Descendants("dt")
                .Where(node => node.GetAttributeValue("class", "").Equals(NEW_MESSAGE_AUTHOR))
                .FirstOrDefault();

            pm.From = authorNode == null ? NEW_MESSAGE_UNKNOWN_AUTHOR : authorNode.InnerText;

            // ***** PARSE MESSAGE ID *****
            var messageIdNode = top.Descendants(NEW_MESSAGE_INPUT_TAG)
                .Where(node => node.GetAttributeValue("name", "").Equals(NEW_MESSAGE_PRIVATEMESSAGEID))
                .FirstOrDefault();

            int id = 0;
            if (messageIdNode != null) 
            {
                string value = messageIdNode.GetAttributeValue("value", "");
                int.TryParse(value, out id); 
            }
            pm.PrivateMessageID = id;

            // ***** PARSE SUBJECT *****
            var subjectNode = top.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "").Equals("breadcrumbs"))
                .FirstOrDefault();

            if (subjectNode != null)
            {
                pm.Subject = subjectNode.ParseTitleFromBreadcrumbsNode();
            }

            // ***** PARSE POST MARK *****
            var postmarkNode = top.Descendants("td")
                .Where(node => node.GetAttributeValue("class", "")
                    .Equals("postdate"))
                .FirstOrDefault();

            if (postmarkNode != null) 
            { 
                string postmark = postmarkNode.InnerText;
                pm.Postmark = Convert.ToDateTime(postmark);
            }

            return pm;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlDocument"></param>
        /// <returns></returns>
        public static IPrivateMessageFolder ParsePrivateMessageFolder(HtmlDocument htmlDocument)
        {
            IPrivateMessageFolder folder = null;
            var top = htmlDocument.DocumentNode;
            var currentFolderNode = top.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "").Equals("breadcrumbs"))
                .FirstOrDefault();

            if (currentFolderNode != null)
            {
                int folderId = 0;
                // *************************************************************
                // THIS IS CURRENTLY BROKEN ON SA -- THE TITLE OF THE FOLDER IS *NOT* WHAT'S DISPLAYED ON THE BREADCRUMBS //
                string name = currentFolderNode.ParseTitleFromBreadcrumbsNode();
                // *************************************************************
                var idString = GetInputValue(top.Descendants("input").ToArray(), "thisfolder");
                
                if (int.TryParse(idString, out folderId))
                    folder = new AwfulPrivateMessageFolder() { FolderID = folderId };
                else
                    folder = new AwfulPrivateMessageFolder();

                var messages = ParseMessageList(htmlDocument);
                foreach (var message in messages)
                {
                    // I hate casting, but whatever...
                    (message as AwfulPrivateMessage).FolderID = folder.FolderID;
                    folder.Messages.Add(message);
                }
            }

            return folder;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlDocument"></param>
        /// <returns></returns>
        public static IPrivateMessageFolderRequest ParseEditFolderPage(HtmlDocument htmlDocument)
        {
            AwfulEditFolderRequest request = null;
            var top = htmlDocument.DocumentNode;
            var inputArray = top.Descendants("input");
            if (inputArray != null)
            {
                request = new AwfulEditFolderRequest();
                int highest = 0;
                var highestNode = inputArray
                    .Where(node => node.GetAttributeValue("name", "").Equals("highest"))
                    .FirstOrDefault();

                if (highestNode != null) { int.TryParse(highestNode.GetAttributeValue("value", ""), out highest); }
                request.HighestIndex = highest;
                
                var folderArray = inputArray.Where(node => node.GetAttributeValue("name", "").Contains("folderlist"));
                if (folderArray != null)
                {
                    int maxFolderListIndex = 0;
                    foreach (var item in folderArray)
                    {
                        string keyString = item.GetAttributeValue("name", "");
                        string value = item.GetAttributeValue("value", "");
                        keyString = keyString.Replace("folderlist", "");
                        keyString = keyString.Replace("[", "");
                        keyString = keyString.Replace("]", "");

                        int key = 0;
                        int.TryParse(keyString, out key);
                        request.FolderTable.Add(key, value);
                        maxFolderListIndex = Math.Max(maxFolderListIndex, key);
                    }

                    request.NewFolderFieldIndex = maxFolderListIndex;
                }
            }

            return request;
        }

        #endregion

        #region private methods

        private static string GetInputValue(IList<HtmlNode> nodes, string name)
        {
            string value = null;
            var valueNode = nodes.Where(node => node.GetAttributeValue("name", "").Equals(name))
                .SingleOrDefault();

            value = valueNode == null ? string.Empty : valueNode.GetAttributeValue("value", "");
            return value;
        }

        private static DateTime? GetPostMarkFromNode(HtmlNode node)
        {
            if (node != null)
            {
                // expecting something like 'Apr 23, 2009 at 18:43'
                string nodetext = node.InnerText;
                // so let's remove the ' at '
                nodetext = nodetext.Replace(" at ", " ");
                DateTime value;
                DateTime.TryParse(nodetext, out value);
                return new DateTime?(value);
            }

            else { return null; }
        }

        private static string GetMessageAuthorFromNode(HtmlNode node)
        {
            string result = PRIVATE_MESSAGE_UNKNOWN_AUTHOR;
            if (node != null) { result = node.InnerText; }
            return result;
        }

        private static string GetMessageTitleFromNode(HtmlNode node)
        {
            string result = string.Empty;
            try
            {
                var linkNode = node.Descendants("a").FirstOrDefault();
                if (linkNode != null) { result = linkNode.InnerText; }
                
            }
            catch (Exception) { result = PRIVATE_MESSAGE_UNKNOWN_TITLE; }
            return result;
        }

        private static int GetMessageIDFromNode(HtmlNode node)
        {
            int id = 0;
            try
            {
                var idNode = node.Descendants("a").FirstOrDefault();
                if (idNode != null)
                {
                    string link = idNode.GetAttributeValue("href", "");
                    string idString = link.Split('=').LastOrDefault();
                    int.TryParse(idString, out id);
                }
            }
            catch (Exception) { id = -1; }
            return id;
        }

        private static PrivateMessageStatus GetMessageStatusFromNode(HtmlNode node)
        {
            var status = PrivateMessageStatus.UNKNOWN;
            try
            {
                // assuming there is a <tr> with a <td><img></td> ...

                var imgNode = node.Descendants("img").FirstOrDefault();
                status = imgNode == null
                    ? PrivateMessageStatus.UNKNOWN
                    : GetMessageStatusFromImage(imgNode.GetAttributeValue("src", ""));

            }
            catch (Exception) { }
            return status;
        }

        private static PrivateMessageStatus GetMessageStatusFromImage(string src)
        {
            var tokens = src.Split('/');
            var result = PrivateMessageStatus.UNKNOWN;
            var fileName = tokens.LastOrDefault();
            if (fileName != null)
            {
                switch (fileName)
                {
                    case PRIVATE_MESSAGE_ICON_NEW:
                        result = PrivateMessageStatus.NEW;
                        break;

                    case PRIVATE_MESSAGE_ICON_READ:
                        result = PrivateMessageStatus.READ;
                        break;

                    case PRIVATE_MESSAGE_ICON_REPLIED:
                        result = PrivateMessageStatus.REPLIED;
                        break;

                    case PRIVATE_MESSAGE_ICON_FORWARDED:
                        result = PrivateMessageStatus.FORWARDED;
                        break;

                    case PRIVATE_MESSAGE_ICON_CANCELLED:
                        result = PrivateMessageStatus.CANCELED;
                        break;
                }
            }

            return result;
        }

        #endregion     
    }
}
