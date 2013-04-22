using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Awful;
using Awful.Web;
using System.Linq;

namespace Awful.WP8.Core.Test
{
    [TestClass]
    public class AwfulWebClientTest
    {
        public static UserMetadata TestUser { get; set; }

        [TestInitialize]
        public void Intialize()
        {
            string username = "Awful!.dll";
            string password = "q2bddf";

            UserMetadata user = AwfulWebClient.LoginAsync(username, password).Result;
            var cookies = user.Cookies;
            Assert.IsNotNull(cookies);
            Assert.IsTrue(cookies.Count > 0);
            TestUser = user;
            Assert.IsTrue(AwfulWebClient.LoadSession(user));
        }

        [TestMethod]
        public void TestGetSmiliesAsync()
        {
            var smilies = AwfulWebClient.GetSmiliesAsync().Result;
            Assert.IsNotNull(smilies);
            Assert.IsTrue(smilies.ToList().Count > 0);
        }

        [TestMethod]
        public void TestGetForumListAsync()
        {
            var forums = AwfulWebClient.GetForumListAsync().Result;
            Assert.IsNotNull(forums);
            Assert.IsTrue(forums.ToList().Count > 0);
        }

        [TestMethod]
        public void TestGetForumIndexAsync()
        {
            ForumMetadata gbs = new ForumMetadata() { ForumID = "1" };
            var page = gbs.GetForumIndexAsync(1).Result;
            Assert.IsNotNull(page);
            Assert.IsTrue(page.Threads.Count > 0);
            Assert.IsTrue(page.ForumID.Equals("1"));
            Assert.IsTrue(page.PageCount.Equals(1));
        }

        [TestMethod]
        public void TestGetThreadPageAsync()
        {
            // http://forums.somethingawful.com/showthread.php?threadid=3460814
            ThreadMetadata thread = new ThreadMetadata() { ThreadID = "3460814" };
            var page = thread.GetThreadPageAsync(1).Result;
            Assert.IsNotNull(page);
            Assert.IsTrue(page.ThreadID.Equals("3460814"));
            Assert.IsTrue(page.Posts[0].Author.Equals("bootleg robot"));
            Assert.IsTrue(page.PageNumber.Equals(1));
        }
    }
}
