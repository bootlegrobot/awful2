using System.Linq;
using System.Collections.Generic;

namespace Awful
{
    public enum ForumGroup
    {
        MAIN=0,
        DISCUSSION,
        ARTS,
        COMMUNITY,
        ARCHIVES,
        OTHER
    }

    public static class ForumHiearchy
    {
        // key: group | element: list of forums
        private static readonly Dictionary<ForumGroup, IList<string>> Grouping = new Dictionary<ForumGroup, IList<string>>();

        static ForumHiearchy() { Init(); }

        private static void Init()
        {
            Grouping.Add(ForumGroup.MAIN, new List<string>() 
            { 
                "1", "26", "155", "214", "154" 
            });

            Grouping.Add(ForumGroup.DISCUSSION, new List<string>() 
            { 
                "192", "190", "158", "200", "46", "162", "22", "170", 
                "202", "219", "167", "236", "124", "132", "218", "211",
                 "44", "145", "93", "234", "191", "256", "146", "250", 
                 "103",  "122", "181", "175", "177", "139", "91", "248",
                 "179", "183", "244", "161", "242"
            });
            
            Grouping.Add(ForumGroup.ARTS, new List<string>() 
            { 
                "31", "210", "247", "151", "133", "182", 
                "150", "104", "130", "144", "27", "215", 
                "255" 
            });
            
            Grouping.Add(ForumGroup.COMMUNITY, new List<string>() 
            { 
                "61", "77", "85", "43", "241", "188", "251", 
                "186", "201" 
            });
            
            Grouping.Add(ForumGroup.ARCHIVES, new List<string>() 
            { 
                "21", "115", "222", "229", "25", "204" 
            });
        }

        public static ForumGroup GetGroup(this ForumMetadata forum)
        {
            string forumID = forum.ForumID;
            ForumGroup? group = Grouping.Keys.Where(key => Grouping[key].Contains(forumID)).SingleOrDefault();
            return !group.HasValue ? ForumGroup.OTHER : group.Value;
        }
    }
}
