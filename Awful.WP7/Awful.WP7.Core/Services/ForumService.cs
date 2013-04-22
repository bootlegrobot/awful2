using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Awful
{
    public class ForumTasks
    {
        private const string SMILEY_REQUEST_URI = "http://forums.somethingawful.com/misc.php?action=showsmilies";

        private static readonly ForumTasks Instance = new ForumTasks();

        private ForumTasks() { }

        public static IEnumerable<TagMetadata> FetchAllSmilies()
        {
            return Instance.Private_FetchAllSmilies();
        }

        private IEnumerable<TagMetadata> Private_FetchAllSmilies()
        {
            var client = new AwfulWebClient();
            var document = client.FetchHtml(SMILEY_REQUEST_URI).ToHtmlDocument();
            var smilies = SmileyParser.ParseSmiliesFromNode(document);
            return smilies;
        }

        public static IEnumerable<ForumMetadata> FetchAllForums() { return Instance.Private_FetchAllForums(); }
        public IEnumerable<ForumMetadata> Private_FetchAllForums()
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "START FetchAllForums()");
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Fetching Forums from SA...");
            
            string url = string.Format("{0}/{1}?forumid=1", CoreConstants.BASE_URL, CoreConstants.FORUM_PAGE_URI);
            var web = new AwfulWebClient();
            var doc = web.FetchHtml(url).ToHtmlDocument();

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Forum fetch complete. Parsing...");
            
            var result = ForumParser.ParseForumList(doc);

            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Info, "Parse complete.");

#if DEBUG
            Debug.WriteLine("Performing Sanitization Check...");
            bool clear = SanitizationCheck(result);
            Debug.WriteLine("Check Result: " + clear);
            foreach (var forum in result)
                Debug.WriteLine(forum);
#endif
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "END FetchAllForums()");
            return result;
        }

        private static bool SanitizationCheck(IEnumerable<ForumMetadata> forums)
        {
            bool clean = true;

            // DEBUGGING SANITIZATION CHECK
            foreach (var forum in forums)
            {
                var dupe = from f in forums
                           where f.ForumID.Equals(forum.ForumID)
                           select f;

                if (dupe.Count() > 1)
                {
                    Debug.WriteLine("Error: duplicates found!");
                    foreach (var item in dupe)
                        Debug.WriteLine(item.ToString());

                    clean = false;
                }
            }

            return clean;
        }
    }
}
