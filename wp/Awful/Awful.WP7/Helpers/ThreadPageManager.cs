using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Collections.Generic;
using Awful.ViewModels;
using Awful.Data;
using Awful.Common;
using Awful.Controls;
using Awful.WP7;

namespace Awful
{
    public class ThreadPageManager : DependencyObject
    {
        public static ThreadPageManager Instance { get; private set; }

        #region IsPage Property

        public static readonly DependencyProperty IsPageProperty = DependencyProperty.RegisterAttached(
            "IsPage", typeof(bool), typeof(ThreadPageManager), new PropertyMetadata(false));

        public static bool GetIsPage(WebBrowser browser)
        {
            return Instance._browser != null &&
                Instance._browser.Equals(browser);
        }

        public static void SetIsPage(WebBrowser target, bool value)
        {
            if (value)
            {
                Instance.InitializeBrowser(target);
            }
        }

        #endregion

        #region ThreadMenu Property

        public static readonly DependencyProperty ThreadMenuProperty = DependencyProperty.RegisterAttached(
            "ThreadMenu", typeof(object), typeof(ThreadPageManager), new PropertyMetadata(null));

        public static object GetThreadMenu(UIElement menu)
        { 
            return Instance._threadMenu; 
        }

        public static void SetThreadMenu(UIElement target, object value)
        {
            Instance.InitializeThreadMenu(value as ThreadPageContextMenuProvider);
        }

        #endregion

        #region IsContainer Property

        public static readonly DependencyProperty IsContainerProperty = DependencyProperty.RegisterAttached(
            "IsContainer", typeof(bool), typeof(ThreadPageManager), new PropertyMetadata(false));

        public static bool GetIsContainer(ContentControl element)
        {
            WebBrowser browser = element.Content as WebBrowser;
            return browser != null && Instance._browser != null && Instance._browser.Equals(browser);
        }

        public static void SetIsContainer(ContentControl element, bool value)
        {
            if (value)
            {
              element.Content = Instance._browser;
            }
        }

        #endregion

        static ThreadPageManager() { Instance = new ThreadPageManager(); }

        public ThreadPageManager(WebBrowser browser, ThreadPageContextMenuProvider threadMenu) : this()
        {
            InitializeBrowser(browser);
            InitializeThreadMenu(threadMenu);
        }

        public ThreadPageManager() 
        { 
            InitializeEventManager();
            IsReady = false;
        }

        public void InitializeBrowser()
        {
            if (this._browser == null)
            {
                WebBrowser pageView = new WebBrowser();
                
                // set background to transparent color
                KollaSoft.KSWebBrowserHelper.SetBrowserBackground(pageView,
                    new SolidColorBrush(Color.FromArgb(0,255,255,255)));

                // disable double tap
                KollaSoft.KSWebBrowserHelper.SetSupressDoubleTap(pageView, true);
                // suppress pan and zoom
                KollaSoft.KSWebBrowserHelper.SetSuppressPanAndZoom(pageView, true);
                // attach events
                InitializeBrowser(pageView);
                InitializeBrowserForPageView(pageView);
            }
        }

        private void InitializeBrowser(WebBrowser browser)
        {
            // check if browser has been attached already
            if (browser != null && this._browser != browser)
                this._browser = AttachBrowser(browser);
        }

        private void InitializeThreadMenu(ThreadPageContextMenuProvider menu)
        {
            this._threadMenu = menu;
        }

        // javascript callback strings
        private const string JS_PAGE_LOADED_CALLBACK = "pageloaded";
        private const string JS_HTML_LOADED_CALLBACK = "htmlloaded";
        private const string JS_HTML_LOADING_CALLBACK = "htmlloading";
        private const string JS_PAGE_STYLE_SET_CALLBACK = "styleset";
        private const string JS_POST_SELECTED_CALLBACK = "post";
        private const string JS_ERROR_CALLBACK = "error";
        private const string JS_IMAGE_SELECTED_CALLBACK = "image";
        private const string JS_LINK_SELECTED_CALLBACK = "a";

        // javascript function names and constants
        private const string JS_CLEAR_HTML = "clearHtml";
        private const string JS_SET_FONTSIZE_FUNCTION = "set_fontsize";
        private const string JS_LOAD_HTML_FUNCTION = "loadHTML";
        private const string JS_SET_STYLES_FUNCTION = "set_styles";
        private const string JS_SCROLL_TO_FUNCTION = "scrollTo";
        private const string JS_ARG_DELIMITER = "##$$##";

        private delegate void ScriptEventDelegate(string response);
        private Dictionary<string, ScriptEventDelegate> _eventManager;
        private WebBrowser _browser;
        private ThreadPageContextMenuProvider _threadMenu;
        public bool IsReady { get; private set; }
        public bool IsLoaded { get; private set; }
        public int SelectedPostIndex { get; private set; }
        public string SelectedImageUrl { get; private set; }
        public string SelectedLinkUrl { get; private set; }

        public event EventHandler ReadyForContent;
        public event EventHandler Loading;
        public event EventHandler Loaded;
        public event EventHandler PostListRequested;
        public event EventHandler PostSelected;

        public void LoadHtml(string html)
        {
            if (html != null)
            {
                this._browser.InvokeScript(JS_LOAD_HTML_FUNCTION, html);
            }
        }

        public void ClearHtml()
        {
            if (this.IsReady)
                this._browser.InvokeScript(JS_CLEAR_HTML);
        }

        public void ScrollToPost(ThreadPostSource post)
        {
            if (post == null) return;

            string smooth;
            bool smoothScroll = false;

            if (smoothScroll)
            {
                smooth = "true";
                this._browser.InvokeScript("scrollTo", "postlink" + post.Data.PageIndex, smooth);
            }
            else
            {
                smooth = "false";
                this._browser.InvokeScript("scrollTo", "post_" + post.Data.PageIndex, smooth);
            }
        }

        public void SetPageStyle(Color foreground, Color background, Color accentColor, Color seenColor, int fontSize)
        {
            string font = fontSize.ToString();
            string fg = ToHtmlColor(foreground);
            string accent = ToHtmlColor(accentColor);
            string bg = ToHtmlColor(background);
            string seen = ToHtmlColor(seenColor);

            // hard coded chrome color for the html background -- makes for a cool look
            // when at the top and bottom of the page
            Color chromeColor = (Color)Application.Current.Resources["PhoneChromeColor"];
            string chrome = ToHtmlColor(chromeColor);

            this._browser.InvokeScript(JS_SET_STYLES_FUNCTION, fg, bg, accent, seen, chrome, font);
            App.Model.ContentFontSize = fontSize;
        }

        private string ToHtmlColor(Color color)
        {
            return string.Format("#{0}", color.ToString().Substring(3));
        }

        private void InitializeEventManager()
        {
            this._eventManager = new Dictionary<string, ScriptEventDelegate>();
            this._eventManager.Add(JS_HTML_LOADING_CALLBACK, OnPageContentLoading);
            this._eventManager.Add(JS_HTML_LOADED_CALLBACK, OnPageContentLoaded);
            this._eventManager.Add(JS_PAGE_LOADED_CALLBACK, OnDOMLoaded);
            this._eventManager.Add(JS_PAGE_STYLE_SET_CALLBACK, OnPageStyleSet);
            this._eventManager.Add(JS_POST_SELECTED_CALLBACK, OnPostSelected);
            this._eventManager.Add(JS_IMAGE_SELECTED_CALLBACK, OnImageSelected);
            this._eventManager.Add(JS_LINK_SELECTED_CALLBACK, OnLinkSelected);
            this._eventManager.Add(JS_ERROR_CALLBACK, OnErrorGenerated);
        }

        private WebBrowser AttachBrowser(WebBrowser browser)
        {
            if (browser == null)
                throw new ArgumentNullException("browser is null.");

            browser.Base = Constants.THREAD_VIEWER_WEB_FOLDER;
            browser.IsScriptEnabled = true;
            browser.IsGeolocationEnabled = false;
            browser.Loaded += new RoutedEventHandler(PageBrowser_Loaded);
            browser.Navigated += new EventHandler<System.Windows.Navigation.NavigationEventArgs>(PageBrowser_Navigated);
            browser.Navigating += new EventHandler<NavigatingEventArgs>(PageBrowser_Navigating);
            browser.NavigationFailed += new System.Windows.Navigation.NavigationFailedEventHandler(PageBrowser_NavigationFailed);
            browser.ScriptNotify += new EventHandler<NotifyEventArgs>(PageBrowser_ScriptNotify);
            return browser;
        }

        private WebBrowser DetachBrowser(WebBrowser browser)
        {
            if (browser == null)
                throw new ArgumentNullException("browser is null.");

            browser.Loaded -= new RoutedEventHandler(PageBrowser_Loaded);
            browser.ScriptNotify -= new EventHandler<NotifyEventArgs>(PageBrowser_ScriptNotify);
            browser.Navigated -= new EventHandler<System.Windows.Navigation.NavigationEventArgs>(PageBrowser_Navigated);
            browser.Navigating -= new EventHandler<NavigatingEventArgs>(PageBrowser_Navigating);
            browser.NavigationFailed -= new System.Windows.Navigation.NavigationFailedEventHandler(PageBrowser_NavigationFailed);
            return browser;
        }


        private void OnPostSelected()
        {
            if (PostSelected != null)
                PostSelected(this, EventArgs.Empty);
        }

        private void OnPostListRequested()
        {
            if (PostListRequested != null)
                PostListRequested(this, null);
        }

        private void OnErrorGenerated(string response)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, response);
        }

        private void OnPageContentLoaded(string response)
        {
            // Set styles
            var theme = ThemeManager.Instance.CurrentTheme;

            SetPageStyle(theme.ThreadPageForegroundColor,
                theme.ThreadPageBackgroundColor,
                theme.AccentColor,
                theme.ThreadPageOldPostColor,
                App.Model.ContentFontSize);
        }

        private void OnPageContentLoading(string response)
        {
            if (Loading != null)
            {
                this.IsLoaded = false;
                Loading(this, null);
            }
        }

        private void OnDOMLoaded(string response)
        {
            this.IsReady = true;
            if (ReadyForContent != null)
                ReadyForContent(this, null);
        }

        private void OnPageStyleSet(string response)
        {
            if (Loaded != null)
            {
                this.IsLoaded = true;
                Loaded(this, null);
            }
        }

        private void OnLinkSelected(string response)
        {
            // expecting a format of 'link##$$##<link url>'
            // example: link##$$##http://www.contoso.com
            // example #2 link##$$##http://forums.somethingawful.com/showthread.php?threadid=3030303

            var delim = new string[] { JS_ARG_DELIMITER };
            string url = response.Split(delim, StringSplitOptions.RemoveEmptyEntries).Last();

            this.SelectedLinkUrl = url;
            _threadMenu.ShowLinkMenu(url);
        }

        private void OnImageSelected(string response)
        {
            // expecting a format of 'image##$$##<image url>'
            // example: image##$$##http://www.contoso.com/image.jpg

            var delim = JS_ARG_DELIMITER.ToCharArray();
            string index = response.Split(delim, StringSplitOptions.RemoveEmptyEntries).Last();
            
            this.SelectedImageUrl = index;
            _threadMenu.ShowImageMenu(this.SelectedImageUrl);
        }

        private void OnPostSelected(string response)
        {
            // expecting a format of 'post##$$##<post index>'
            // example: post##$$##14

            var delim = JS_ARG_DELIMITER.ToCharArray();
            string index = response.Split(delim, StringSplitOptions.RemoveEmptyEntries).Last();
            
            this.SelectedPostIndex = Convert.ToInt32(index);
            OnPostSelected();
        }

        private void ManageScriptAction(string action)
        {
            if (IgnoreInteractions)
                return;

            if (this._eventManager.ContainsKey(action))
                this._eventManager[action](null);
            else
            {
                // check for delimiter in action
                var delim = JS_ARG_DELIMITER.ToCharArray();
                var tokens = action.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                if (this._eventManager.ContainsKey(tokens[0]))
                    this._eventManager[tokens[0]](action);
                else
                {
                    AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, action);
#if DEBUG
                    MessageBox.Show(action, "debug: script action", MessageBoxButton.OK);
#endif
                }
            }
        }

        #region Web Browser events

        private void InitializeBrowserForPageView(WebBrowser browser)
        {
            browser.Navigate(
             new Uri(Constants.THREAD_VIEWER_FILENAME, UriKind.RelativeOrAbsolute));
        }

        private void PageBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitializeBrowserForPageView(sender as WebBrowser);
        }

        private void PageBrowser_ScriptNotify(object sender, NotifyEventArgs e)
        {
            this.ManageScriptAction(e.Value);
        }

        private void PageBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "Event: PageBrowser_Navigated");
        }

        void PageBrowser_NavigationFailed(object sender, System.Windows.Navigation.NavigationFailedEventArgs e)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "Event: PageBrowser_NavigationFailed");

            try
            {
                // ignore javascript uri navigation failures
                if (e.Uri.AbsoluteUri.Contains("javascript"))
                    return;

                else if (e.Uri.OriginalString.Contains(Constants.THREAD_VIEWER_FILENAME))
                {
                    AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, "Thread View Initialization failed.");
                    this.InitializeBrowserForPageView(sender as WebBrowser);
                }

                else
                    this.IsReady = false;
            }

            catch (Exception ex) { this.IsReady = false; }
        }

        void PageBrowser_Navigating(object sender, NavigatingEventArgs e)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "Event: PageBrowser_Navigating");
            if (e.Uri.OriginalString.Contains(Constants.THREAD_VIEWER_FILENAME))
                AwfulDebugger.AddLog(this, AwfulDebugger.Level.Debug, "Loading View Html in browser...");
        }

        #endregion

        internal void ZoomOut()
        {
            if (IsReady)
            {
                int fontSize = App.Model.ContentFontSize - 2;
                // zoom out
                this._browser.InvokeScript(JS_SET_FONTSIZE_FUNCTION, fontSize.ToString());
                App.Model.ContentFontSize = fontSize;
            }
        }

        internal void ZoomIn()
        {
            if (IsReady)
            {
                int fontSize = App.Model.ContentFontSize + 2;
                // zoom in
                this._browser.InvokeScript(JS_SET_FONTSIZE_FUNCTION, fontSize.ToString());
                App.Model.ContentFontSize = fontSize;
            }
        }

        public bool IgnoreInteractions { get; set; }
    }
}
