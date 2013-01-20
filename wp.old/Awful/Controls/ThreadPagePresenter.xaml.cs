using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telerik.Windows.Controls;
using System.Collections;
using System.ComponentModel;
using Microsoft.Phone.Tasks;

namespace Awful.Controls
{
    public partial class ThreadPagePresenter : UserControl
    {
        public ThreadPagePresenter()
        {
            InitializeComponent();
            this.PageManager = new ThreadPageManager(this.ThreadPageView, this.MenuProvider);
            this.PageManager.Loading += new EventHandler(OnPageLoading);
            this.PageManager.Loaded += new EventHandler(OnPageLoaded);
            this.PageManager.ReadyForContent += new EventHandler(OnReadyForContent);
            this.PageManager.PostSelected += new EventHandler(OnPostSelected);
        }

        private RadAnimation FadeInAnimation
        {
            get { return this.Resources["FadeInAnimation"] as RadAnimation; }
        }

        private RadAnimation FadeOutAnimation
        {
            get { return this.Resources["FadeOutAnimation"] as RadAnimation; }
        }

        private bool _allowZoom;
        private ThreadPageManager PageManager;
        private Data.ThreadPageDataSource PageData;
		
		private ThreadPageContextMenuProvider MenuProvider
		{
			get { return this.Resources["ThreadContextMenu"] as ThreadPageContextMenuProvider; }	
		}
		
        #region PageItem DependencyProperty

        public object PageItem
		{
			get { return GetValue(PageItemProperty); }
			set { SetValue(PageItemProperty, value); }
		}
		
		public static readonly DependencyProperty PageItemProperty = DependencyProperty.Register(
			"PageItem", typeof(object), typeof(ThreadPagePresenter), new PropertyMetadata(null,
			(o, e) =>
		{
			if (!DesignerProperties.IsInDesignTool)
				(o as ThreadPagePresenter).OnPageItemChanged(e.NewValue);
		}));
		
		private void OnPageItemChanged(object item)
		{
            	if (item == null)
                	return;

            	else if (!(item is Data.ThreadPageDataSource))
                	throw new Exception("Item must be of type Data.ThreadPageDataSource!");

            	else
            	{
                    this.PageManager.ClearHtml();
                	PageData = AttachPageData(item as Data.ThreadPageDataSource);
                    if (string.IsNullOrEmpty(PageData.Html))
                    {
                        this.IsLoading = true;
                        PageData.Refresh();
                    }

                    else if (PageManager.IsReady)
                    {
                        this.ThreadPageLoadingBar.Opacity = 0;
                        PageManager.LoadHtml(PageData.Html);
                    }
            	}
			
		}

        private Data.ThreadPageDataSource AttachPageData(Data.ThreadPageDataSource data)
        {
            if (PageData != null)
            {
                PageData.ThreadPageUpdated += OnThreadPageUpdated;
                PageData.ThreadPageUpdating += OnThreadPageUpdating;    
            }

            data.ThreadPageUpdated += OnThreadPageUpdated;
            data.ThreadPageUpdating += OnThreadPageUpdating;
            return data;
        }

        private void OnThreadPageUpdating(object sender, EventArgs e)
        {
            this.IsLoading = true;
        }

        private void OnThreadPageUpdated(object sender, EventArgs e)
        {
            if (sender == null)
            {
                MessageBox.Show("Could not load the requested page. Please try again.", ":(", MessageBoxButton.OK);
                this.IsLoading = false;
            }

            if (PageManager.IsReady)
                PageManager.LoadHtml(PageData.Html);
        }

        #endregion

        #region IsLoading Dependency Property

        public event EventHandler PageLoading;
        public event EventHandler PageLoaded;

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(ThreadPagePresenter),
            new PropertyMetadata(false, (o, a) => { (o as ThreadPagePresenter).OnIsLoadingPropertyChanged((bool)a.NewValue); }));

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        private void OnPageLoading()
        {
            if (PageLoading != null)
                PageLoading(this, null);
        }

        private void OnPageLoaded()
        {
            if (PageLoaded != null)
                PageLoaded(this, null);
        }

        private void OnIsLoadingPropertyChanged(bool newValue)
        {
            // control is in pageLoading state
            if (newValue)
            {
                this.ThreadPageLoadingBar.IsIndeterminate = true;
                this.ThreadPageLoadingBar.Visibility = System.Windows.Visibility.Visible;
                RadAnimationManager.StopIfRunning(this.ThreadPageView, this.FadeInAnimation);
                RadAnimationManager.Play(this.ThreadPageView, this.FadeOutAnimation);
                this.OnPageLoading();
            }
            // control is in pageLoaded state
            else
            {
                this.ThreadPageLoadingBar.IsIndeterminate = false;
                this.ThreadPageLoadingBar.Visibility = System.Windows.Visibility.Collapsed;
                RadAnimationManager.StopIfRunning(this.ThreadPageView, this.FadeOutAnimation);
                RadAnimationManager.Play(this.ThreadPageView, this.FadeInAnimation);
                this.OnPageLoaded();
            }
        }

        #endregion

        public void ScrollToPost(Data.ThreadPostSource post)
        {
            this.PageManager.ScrollToPost(post);
        }

		public void ShowPostMenu(int postNumber)
		{
            int index = postNumber - 1;
            var post = this.PageData.Posts[index];
            this.MenuProvider.ShowPostMenu(post);
		}
	
        private void ProcessPagePosts(IEnumerable<Data.ThreadPostSource> source)
        {
            try
            {
                var unread = source
                    .Where(post => post.Data.IsNew)
                    .FirstOrDefault();

                if (unread == null)
                    PageManager.ScrollToPost(source.First());
                else
                    PageManager.ScrollToPost(unread);
            }

            catch (Exception ex) { }
            finally { this.IsLoading = false; }
        }

        private void OnPageLoaded(object sender, EventArgs e)
        {
            // load the first unread post
            ProcessPagePosts(this.PageData.Posts);
        }

        private void OnPageLoading(object sender, EventArgs e){ }

        private void OnReadyForContent(object sender, EventArgs e)
        {
            if (this.PageManager.IsReady && PageData.Html != null)
            {
                this.ThreadPageLoadingBar.Opacity = 0;
                this.PageManager.LoadHtml(PageData.Html);
            }
        }

        private void OnPostSelected(object sender, EventArgs e)
        {
            this.ShowPostMenu(this.PageManager.SelectedPostIndex);
        }

        #region Context Menu Handlers

        private void OpenSelectedImageInWebBrowser(object sender, ContextMenuItemSelectedEventArgs e)
        {
            string url = this.PageManager.SelectedImageUrl;
            if (url.IsStringUrlHttpOrHttps())
            {
                    var imageUri = new Uri(url);
                    try 
                    {
                        var webtask = new WebBrowserTask();
                        webtask.Uri = imageUri;
                        webtask.Show();
                    }
                    catch (Exception) { }
            }
        }

        #endregion

        private void ZoomText(object sender, PinchGestureEventArgs e)
        {
            if (!_allowZoom)
                return;

            // ratio = start / finish
            var ratio = e.DistanceRatio;
            
            // if pinching in
            if (ratio > 1)
                PageManager.ZoomIn();
            else
                PageManager.ZoomOut();
        }

        private void BeginTextZoom(object sender, PinchStartedGestureEventArgs e)
        {
            // allow only horizontal pinches
            var angle = Math.Abs(e.Angle);
            _allowZoom = ((angle >= 80) && (angle <= 100)) ||
                ((angle <= 290) && (angle >= 250));
        }

        public void Refresh()
        {
            this.PageData.Refresh();
        }

    }

    public class ThreadPageContextMenuProvider : Common.RadContextMenuProvider
    {
        private RadContextMenu _post;
        public RadContextMenu PostMenu
        {
            get
            {
                if (_post == null)
                    _post = ConfigurePostMenu(new RadContextMenu());

                return _post;
            }
        }

        private RadContextMenu _image;
        public RadContextMenu ImageMenu
        {
            get { return _image; }
            set { SetProperty(ref _image, value, "ImageMenu"); }
        }

        private RadContextMenu _link;
        public RadContextMenu LinkMenu
        {
            get
            {
                if (_link == null)
                    _link = CreateLinkMenu(new RadContextMenu());

                return _link;
            }
        }

        private RadContextMenuItem viewOnSA;
        private RadContextMenuItem viewOnWeb;

        private RadContextMenu CreateLinkMenu(RadContextMenu radContextMenu)
        {
            var saCommand = new Commands.ViewSAThreadCommand();
            var webCommand = new Commands.OpenWebBrowserCommand();

            viewOnSA = new RadContextMenuItem() { Content = "open in Awful!", Command = saCommand };
            viewOnWeb = new RadContextMenuItem() { Content = "open in web browser", Command = webCommand };

            radContextMenu.Items.Add(viewOnSA);
            radContextMenu.Items.Add(viewOnWeb);

            return radContextMenu;
        }

        public void ShowLinkMenu(string url)
        {
            try
            {
                var uri = new Uri(url, UriKind.Absolute);
                if (this.Menu != null)
                    this.Menu.IsOpen = false;

                foreach (var item in LinkMenu.Items)
                    (item as RadContextMenuItem).CommandParameter = uri;

                this.Menu = LinkMenu;
                this.Menu.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Could not navigate to '{0}'", url), ":(", MessageBoxButton.OK);
            }
        }

        public void ShowPostMenu(Data.ThreadPostSource post)
        {
            if (this.Menu != null)
                this.Menu.IsOpen = false;

            foreach (var item in PostMenu.Items)
                (item as RadContextMenuItem).CommandParameter = post;

            this.Menu = PostMenu;
            this.Menu.IsOpen = true;
        }

        public void ShowImageMenu()
        {
            if (this.Menu != null)
                this.Menu.IsOpen = false;

            this.Menu = ImageMenu;
            this.Menu.IsOpen = true;
        }

        private RadContextMenuItem quoteMenu;
        private RadContextMenuItem editMenu;
        private RadContextMenuItem markMenu;

        private RadContextMenu ConfigurePostMenu(RadContextMenu postMenu)
        {
            var quote = new Commands.QuotePostToClipboardCommand();
            var edit = new Commands.EditPostCommand();
            var mark = new Commands.MarkPostAsReadCommand();

            quoteMenu = new RadContextMenuItem() { Content = "quote", Command = quote };
            editMenu = new RadContextMenuItem() { Content = "edit", Command = edit };
            markMenu = new RadContextMenuItem() { Content = "mark as read", Command = mark };

            postMenu.Items.Add(editMenu);
            postMenu.Items.Add(quoteMenu);
            postMenu.Items.Add(markMenu);

            postMenu.IsFadeEnabled = false;
            postMenu.IsZoomEnabled = false;
            return postMenu;
        }
    }
}
