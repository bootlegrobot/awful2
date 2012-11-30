using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Awful
{
    public static class ForumTasks
    {
        public static IEnumerable<ForumMetadata> FetchAllForums()
        {
            string url = string.Format("{0}/{1}?forumid=1", CoreConstants.BASE_URL, CoreConstants.FORUM_PAGE_URI);
            var web = new AwfulWebClient();
            var doc = web.FetchHtml(url);
            var result = ForumParser.ParseForumList(doc);
#if DEBUG
            Debug.WriteLine("Performing Sanitization Check...");
            bool clear = SanitizationCheck(result);
            Debug.WriteLine("Check Result: " + clear);
#endif
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
