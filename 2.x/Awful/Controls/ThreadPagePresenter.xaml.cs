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
        }

        private RadAnimation FadeInAnimation
        {
            get { return this.Resources["FadeInAnimation"] as RadAnimation; }
        }

        private RadAnimation FadeOutAnimation
        {
            get { return this.Resources["FadeOutAnimation"] as RadAnimation; }
        }

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
                    if (PageData.Html == null)
                        PageData.Refresh();
                    else if (PageManager.IsReady)
                        PageManager.LoadHtml(PageData.Html);
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
            if (PageManager.IsReady)
                PageManager.LoadHtml(PageData.Html);
        }

        #endregion

        #region IsLoading Dependency Property

        public event EventHandler PageLoading;
        public event EventHandler PageLoaded;

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(ThreadPagePresenter),
            new PropertyMetadata(true, (o, a) => { (o as ThreadPagePresenter).OnIsLoadingPropertyChanged((bool)a.NewValue); }));

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
                RadAnimationManager.StopIfRunning(this.ThreadPageView, this.FadeInAnimation);
                RadAnimationManager.Play(this.ThreadPageView, this.FadeOutAnimation);
                this.OnPageLoading();
            }
            // control is in pageLoaded state
            else
            {
                this.ThreadPageLoadingBar.IsIndeterminate = false;
                
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

		public void ShowPostMenu(int postIndex)
		{
            this.MenuProvider.ShowPostMenu();
		}
		
		public void ShowImageMenu(string imageUrl)
		{
            this.MenuProvider.ShowImageMenu();
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
                this.PageManager.LoadHtml(PageData.Html);
        }

        #region Context Menu Handlers

        private void EditSelectedPost(object sender, ContextMenuItemSelectedEventArgs e)
        {

        }

        private void QuoteSelectedPost(object sender, ContextMenuItemSelectedEventArgs e)
        {
            /*
            PageData.QuoteAsync(PageManager.SelectedPostIndex, quote =>
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (quote != null)
                        {
                            Clipboard.SetText(quote);
                            MessageBox.Show("Quote added to clipboard.", ":)", MessageBoxButton.OK);
                        }
                        else
                            MessageBox.Show("Quote failed.", ":(", MessageBoxButton.OK);
                    });
                });
             */
        }

        private void MarkSelectedPostAsRead(object sender, ContextMenuItemSelectedEventArgs e)
        {
            /*
            PageData.MarkPostAsReadAsync(PageManager.SelectedPostIndex, marked =>
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (marked)
                            MessageBox.Show("Mark successful.", ":)", MessageBoxButton.OK);
                        else
                            MessageBox.Show("Mark failed.", ":(", MessageBoxButton.OK);
                    });
                });
             */
        }

        private void ShowOnlyPostsFromSelectedAuthor(object sender, ContextMenuItemSelectedEventArgs e)
        {

        }

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
            // ratio = start / finish
            var ratio = e.DistanceRatio;
            
            // if pinching in
            if (ratio > 1)
                PageManager.ZoomIn();
            else
                PageManager.ZoomOut();
        }
    }

    public class ThreadPageContextMenuProvider : Common.RadContextMenuProvider
    {
        private RadContextMenu _post;
        public RadContextMenu PostMenu
        {
            get { return _post; }
            set { SetProperty(ref _post, value, "PostMenu"); }
        }

        private RadContextMenu _image;
        public RadContextMenu ImageMenu
        {
            get { return _image; }
            set { SetProperty(ref _image, value, "ImageMenu"); }
        }

        public void ShowPostMenu()
        {
            if (this.Menu != null)
                this.Menu.IsOpen = false;

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

    }
}
