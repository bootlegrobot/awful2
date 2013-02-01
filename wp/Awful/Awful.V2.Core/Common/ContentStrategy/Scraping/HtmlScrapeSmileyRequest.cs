using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.Common
{
    internal sealed class HtmlScrapeSmileyRequest : SmileyRequestStrategy
    {
        protected override Uri GetSmileyPageUri()
        {
            return new Uri("http://forums.somethingawful.com/misc.php?action=showsmilies");
        }

        public override IEnumerable<TagMetadata> LoadSmilies()
        {
            var url = GetSmileyPageUri().AbsoluteUri;
            var doc = new AwfulWebClient().FetchHtml(url);
            return SmileyParser.ParseSmiliesFromNode(doc);
        }
    }
}
