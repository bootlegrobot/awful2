using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Awful;
using Awful.Web;
using System.Linq;

namespace Awful.Core.Test
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

        [TestMethod]
        public void TestGetLastPostAsync()
        {
            ThreadMetadata thread = new ThreadMetadata() { ThreadID = "3460814" };
            var page = thread.GetLastPostAsync().Result;
            Assert.IsNotNull(page);
            Assert.IsTrue(page.ThreadID.Equals("3460814"));
            Assert.IsFalse(page.PageNumber.Equals(1));
        }

        [TestMethod]
        public void TestQuoteAsync()
        {
            // http://forums.somethingawful.com/newreply.php?action=newreply&postid=399607488
            ThreadPostMetadata post = new ThreadPostMetadata() { PostID = "399607488" };
            var quote = post.QuoteAsync().Result;
            Assert.IsTrue(quote.Contains("This thread is for the discussion of the app"));
        }

        [TestMethod]
        public void TestMarkAsReadAsync()
        {
            // load up a thread, the OP should be old
            ThreadMetadata thread = new ThreadMetadata { ThreadID = "3460814" };
            var page = thread.GetThreadPageAsync(1).Result;
            ThreadPostMetadata post = page.Posts.First();
            Assert.IsFalse(post.IsNew);
            
            // now lets mark it read
            bool marked = post.MarkAsReadAsync().Result;
            Assert.IsTrue(marked);

            // ensure that the post below it is brand new after refresh
            page = thread.GetThreadPageAsync(1).Result;
            post = page.Posts[1];
            Assert.IsTrue(post.IsNew);
        }

        [TestMethod]
        public void TestThreadPageRefreshAsync()
        {
            ThreadPageMetadata page = new ThreadPageMetadata() { ThreadID = "3460814", PageNumber = 1 };
            var refreshed = page.RefreshAsync().Result;
            Assert.IsTrue(page.ThreadID.Equals(refreshed.ThreadID));
            Assert.IsTrue(page.PageNumber.Equals(refreshed.PageNumber));
            Assert.IsNull(page.ThreadTitle);
            Assert.IsNotNull(refreshed.ThreadTitle);
        }

        [TestMethod]
        public void TestThreadBookmarkAsync()
        {
            ThreadMetadata thread = new ThreadMetadata() { ThreadID = "3460814" };
            Assert.Fail("Not Implemented");
        }

        [TestMethod]
        public void TestForumPageRefreshAsync()
        {
            ForumPageMetadata forum = new ForumPageMetadata() { ForumID = "1" };
            Assert.IsNull(forum.Threads);
            var refreshed = forum.RefreshAsync().Result;
            Assert.IsNotNull(refreshed);
            Assert.IsNotNull(refreshed.Threads);
            Assert.IsTrue(refreshed.ForumID.Equals("1"));
        }

        [TestMethod]
        public void TestPrivateMessageRefreshAsync()
        {
            Assert.Fail("Not Implemented");
        }

        [TestMethod]
        public void TestGetNewMessageRequestAsync()
        {
            Assert.Fail("Not Implemented");
        }

        [TestMethod]
        public void TestGetMessageReplyRequestAsync()
        {
            Assert.Fail("Not Implemented");
        }

        [TestMethod]
        public void TestGetMessageForwardRequestAsync()
        {
            Assert.Fail("Not Implemented");
        }

        [TestMethod]
        public void TestGetMessageFolderListAsync()
        {
            Assert.Fail("Not Implemented");
        }

        [TestMethod]
        public void TestGetMessageFolderRefreshAsync()
        {
            Assert.Fail("Not Implemented");
        }

        [TestMethod]
        public void TestReplyAsync()
        {
            Assert.Fail("Not Implemented");
        }

        [TestMethod]
        public void TestEditRequestAsync()
        {
            Assert.Fail("Not Implemented");
        }

        [TestMethod]
        public void TestSendEditAsync()
        {
            Assert.Fail("Not Implemented");
        }
    }
}
