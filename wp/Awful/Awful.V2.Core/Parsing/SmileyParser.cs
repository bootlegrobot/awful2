﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful
{
    internal static class SmileyParser
    {
        private const string SMILEY_NODE_PARENT_ELEMENT = "li";
        private const string SMILEY_NODE_ATTRIBUTE_VALUE = "smilie";
        private const string SMILEY_TEXT_ATTRIBUTE_VALUE = "text";
        private const string SMILEY_URI_ATTRIBUTE = "src";
        private const string SMILEY_TITLE_ATTRIBUTE = "title";

        public static List<TagMetadata> ParseSmiliesFromNode(HtmlDocument doc)
        {
            List<TagMetadata> list = null;
            try
            {
                list = new List<TagMetadata>();
                var nodes = doc.DocumentNode.Descendants(SMILEY_NODE_PARENT_ELEMENT)
                    .Where(node => node.GetAttributeValue("class", "").Equals(SMILEY_NODE_ATTRIBUTE_VALUE));

                foreach (var node in nodes)
                {
                    TagMetadata data = new TagMetadata()
                        .ParseValue(node)
                        .ParseTitleAndUri(node);

                    list.Add(data);
                }
            }
            catch (Exception) { }
            return list;
        }

        private static TagMetadata ParseValue(this TagMetadata data, HtmlNode parent)
        {
            var node = parent.Descendants("div")
                .Where(n => n.GetAttributeValue("class", "").Equals(SMILEY_TEXT_ATTRIBUTE_VALUE))
                .FirstOrDefault();

            string value = string.Empty;
            if (node != null) { value = node.InnerText; }
            data.Value = value;
            return data;
        }

        private static TagMetadata ParseTitleAndUri(this TagMetadata data, HtmlNode parent)
        {
            var node = parent.Descendants("img")
                .FirstOrDefault();

            if (node != null)
            {
                string uri = node.GetAttributeValue(SMILEY_URI_ATTRIBUTE, "");
                string title = node.GetAttributeValue(SMILEY_TITLE_ATTRIBUTE, "");
                data.TagUri = uri;
                data.Title = title;
            }

            return data;
        }
    }
}
