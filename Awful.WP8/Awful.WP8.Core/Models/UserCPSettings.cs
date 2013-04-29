using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kollasoft;
using HtmlAgilityPack;
using RestSharp;

namespace Awful
{
    public interface IUserSettings
    {
        bool InvisbleMode { get; set; }
        bool AutomaticLogin { get; set; }
        bool AllowEmail { get; set; }
        bool BookmarkRepliedThreads { get; set; }
        bool MarkOldPostsInColor { get; set; }
        bool ColorizeThreadsByBookmark { get; set; }
        bool ShowMarkAsReadIcon { get; set; }
        bool ShowBookmarkStarButtons { get; set; }
        bool AllowEmailFromModAdmin { get; set; }
        bool EnableMessaging { get; set; }
        bool NewMessageEmailNotify { get; set; }
        bool NewMessageEmailPopup { get; set; }
        bool HightlightThreadOP { get; set; }
        bool AdjustPagePositionToRequestedPost { get; set; }
        bool ShowUserSignature { get; set; }
        bool ShowUserAvatar { get; set; }
        bool ShowImages { get; set; }
        bool ShowVideo { get; set; }
        bool ShowSmilies { get; set; }
        bool DisableAdvancedPostingFeatures { get; set; }
    }

    internal class UserSettings : IUserSettings, IPreparePostRequest
    {
        #region Input Names

        private const string INVISIBLE_MODE = "invisible";
        private const string COOKIE_USER = "cookieuser";
        private const string SHOW_EMAIL = "showemail";
        private const string BOOKMARK_OWN_POSTS = "bookmark_own_posts";
        private const string COLOR_SEEN = "color_seen";
        private const string THREADS_HIGHLIGHT_SEEN = "threads_highlight_seen";
        private const string THREADS_COLORIZE_BOOKMARKS = "threads_colorize_bookmarks";
        private const string SHOW_SEEN_ICON = "show_seen_icon";
        private const string THREADS_STAR_BUTTONS = "threads_star_buttons";
        private const string ALLOW_MAIL = "allowmail";
        private const string RECEIVE_PM = "receivepm";
        private const string EMAIL_ON_PM = "emailonpm";
        private const string PM_POPUP = "pmpopup";
        private const string THREADS_HIGHLIGHT_OP = "threads_highlight_op";
        private const string JS_ONLOAD_POSTJUMP = "js_onload_postjump";
        private const string SHOW_SIGNATURES = "showsignatures";
        private const string SHOW_AVATARS = "showavatars";
        private const string SHOW_IMAGES = "showimages";
        private const string SHOW_VIDEO = "showvideo";
        private const string SHOW_SMILIES = "showsmilies";
        private const string ADV_POST_DISABLED = "adv_post_disabled";

        #endregion

        private const string SUBMIT = "Submit Modifications";

        private readonly Dictionary<string, bool> userCPTable = new Dictionary<string, bool>();

        private Dictionary<string, bool> SettingsTable { get { return userCPTable; } }
       
        public static UserSettings FromHtmlDocument(HtmlDocument doc)
        {
            UserSettings cpSettings = new UserSettings();

            // grab list of all radio type nodes that are checked
            var options = doc.DocumentNode.Descendants("input")
                .Where(node => node.GetAttributeValue("type", "").Equals("radio") &&
                    node.Attributes.Contains("checked"))
                .ToList();

            // set initiali values
            foreach (var option in options)
            {
                string name = option.GetAttributeValue("name", "");
                string value = option.GetAttributeValue("value", "");
                cpSettings.SettingsTable.AddOrUpdateValue(name, ResponseToBool(value));
            }

            return cpSettings;
        }

        private static string BoolToResponse(bool value)
        {
            return value ? "yes" : "no";
        }

        private static bool ResponseToBool(string value)
        {
            return value == "yes" ? true : false;
        }

        public IRestRequest PreparePostRequest(IRestRequest request)
        {
            request.Method = Method.POST;
            foreach (var key in SettingsTable.Keys)
                request.AddParameter(key, BoolToResponse(SettingsTable[key]));

            request.AddParameter("submit", SUBMIT);

            return request;
        }

        #region Properties

        public bool InvisbleMode
        {
            get
            {
                return SettingsTable.GetValueOrDefault(INVISIBLE_MODE);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(INVISIBLE_MODE, value);
            }
        }

        public bool AutomaticLogin
        {
            get
            {
                return SettingsTable.GetValueOrDefault(COOKIE_USER);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(COOKIE_USER, value);
            }
        }

        public bool AllowEmail
        {
            get
            {
                return SettingsTable.GetValueOrDefault(SHOW_EMAIL);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(SHOW_EMAIL, value);
            }
        }

        public bool BookmarkRepliedThreads
        {
            get
            {
                return SettingsTable.GetValueOrDefault(BOOKMARK_OWN_POSTS);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(BOOKMARK_OWN_POSTS, value);
            }
        }

        public bool MarkOldPostsInColor
        {
            get
            {
                return SettingsTable.GetValueOrDefault(COLOR_SEEN);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(COLOR_SEEN, value);
            }
        }

        public bool ColorizeThreadsByBookmark
        {
            get
            {
                return SettingsTable.GetValueOrDefault(THREADS_COLORIZE_BOOKMARKS);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(THREADS_COLORIZE_BOOKMARKS, value);
            }
        }

        public bool ShowMarkAsReadIcon
        {
            get
            {
                return SettingsTable.GetValueOrDefault(SHOW_SEEN_ICON);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(SHOW_SEEN_ICON, value);
            }
        }

        public bool ShowBookmarkStarButtons
        {
            get
            {
                return SettingsTable.GetValueOrDefault(THREADS_STAR_BUTTONS);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(THREADS_STAR_BUTTONS, value);
            }
        }

        public bool AllowEmailFromModAdmin
        {
            get
            {
                return SettingsTable.GetValueOrDefault(ALLOW_MAIL);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(ALLOW_MAIL, value);
            }
        }

        public bool EnableMessaging
        {
            get
            {
                return SettingsTable.GetValueOrDefault(RECEIVE_PM);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(RECEIVE_PM, value);
            }
        }

        public bool NewMessageEmailNotify
        {
            get
            {
                return SettingsTable.GetValueOrDefault(EMAIL_ON_PM);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(EMAIL_ON_PM, value);
            }
        }

        public bool NewMessageEmailPopup
        {
            get
            {
                return SettingsTable.GetValueOrDefault(PM_POPUP);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(PM_POPUP, value);
            }
        }

        public bool HightlightThreadOP
        {
            get
            {
                return SettingsTable.GetValueOrDefault(THREADS_HIGHLIGHT_OP);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(THREADS_HIGHLIGHT_OP, value);
            }
        }

        public bool AdjustPagePositionToRequestedPost
        {
            get
            {
                return SettingsTable.GetValueOrDefault(JS_ONLOAD_POSTJUMP);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(JS_ONLOAD_POSTJUMP, value);
            }
        }

        public bool ShowUserSignature
        {
            get
            {
                return SettingsTable.GetValueOrDefault(SHOW_SIGNATURES);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(SHOW_SIGNATURES, value);
            }
        }

        public bool ShowUserAvatar
        {
            get
            {
                return SettingsTable.GetValueOrDefault(SHOW_AVATARS);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(SHOW_AVATARS, value);
            }
        }

        public bool ShowImages
        {
            get
            {
                return SettingsTable.GetValueOrDefault(SHOW_IMAGES);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(SHOW_IMAGES, value);
            }
        }

        public bool ShowVideo
        {
            get
            {
                return SettingsTable.GetValueOrDefault(SHOW_VIDEO);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(SHOW_VIDEO, value);
            }
        }

        public bool ShowSmilies
        {
            get
            {
                return SettingsTable.GetValueOrDefault(SHOW_SMILIES);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(SHOW_SMILIES, value);
            }
        }

        public bool DisableAdvancedPostingFeatures
        {
            get
            {
                return SettingsTable.GetValueOrDefault(ADV_POST_DISABLED);
            }
            set
            {
                SettingsTable.AddOrUpdateValue(ADV_POST_DISABLED, value);
            }
        }

        #endregion
    }
}
