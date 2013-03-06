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
using Awful.ViewModels;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace Awful.Controls
{
    public partial class ThreadPageSlideView : UserControl
    {
        private RadAnimation FadeInAnimation { get { return this.Resources["fadeInAnimation"] as RadAnimation; } }
        private RadAnimation FadeOutAnimation { get { return this.Resources["fadeOutAnimation"] as RadAnimation; } }
        public ThreadPageSlideViewModel ControlViewModel { get { return this.Resources["PageDataSource"] as ThreadPageSlideViewModel; } }
        private ThreadPageContextMenuProvider MenuProvider { get { return this.Resources["ThreadContextMenu"] as ThreadPageContextMenuProvider; } }

        public bool IsPostJumpListVisible
        {
            get { return this.PostJumpListPanel.Visibility == System.Windows.Visibility.Visible; }
            set
            {
                if (value)
                    ShowPostJumpList(this, null);
                else
                    HidePostJumpList(this, null);
            }
        }

        public ThreadPageManager PageManager { get { return ThreadPageManager.Instance; } }
        private bool _measurePinch;
        private readonly DispatcherTimer _hideTitleTimer;

        public ThreadPageSlideView()
        {
            InitializeComponent();

            this._hideTitleTimer = new DispatcherTimer();
            this._hideTitleTimer.Interval = TimeSpan.FromSeconds(2);
            this._hideTitleTimer.Tick += (o, te) =>
            {
                (o as DispatcherTimer).Stop();
                VisualStateManager.GoToState(this, "HideTitle", true);
            };

            VisualStateManager.GoToState(this, "HideTitle", false);
            VisualStateManager.GoToState(this, "Loading", false);

            var gestures = GestureService.GetGestureListener(this.LayoutRoot);
            gestures.DoubleTap += ShowThreadTitle;

            gestures.PinchStarted += OnLayoutPinchStarted;
            gestures.PinchDelta += OnLayoutPinchDelta;
            gestures.PinchCompleted += OnLayoutPinchCompleted;

            Loaded += OnLoad;
            Unloaded += OnUnload;
        }

        #region Pinch Events

        void OnLayoutPinchCompleted(object sender, PinchGestureEventArgs e)
        {
            if (this._measurePinch)
            {
                this._measurePinch = false;
                PageManager.IgnoreInteractions = false;

                if (e.DistanceRatio < 0.5)
                    ShowPostJumpList(this, null);

                else
                {
                    var transform = PageViewPanel.RenderTransform as CompositeTransform;
                    double startX = transform.ScaleX;
                    double startY = transform.ScaleY;
                    double endX = 1.0;
                    double endY = 1.0;

                    DoubleAnimation scaleX = new DoubleAnimation() { From = startX, To = endX, Duration = TimeSpan.FromMilliseconds(250), FillBehavior = FillBehavior.HoldEnd };
                    DoubleAnimation scaleY = new DoubleAnimation() { From = startY, To = endY, Duration = TimeSpan.FromMilliseconds(250), FillBehavior = FillBehavior.HoldEnd };

                    Storyboard scale = new Storyboard();
                    Storyboard.SetTarget(scaleX, transform);
                    Storyboard.SetTargetProperty(scaleX, new PropertyPath("ScaleX"));
                    Storyboard.SetTarget(scaleY, transform);
                    Storyboard.SetTargetProperty(scaleY, new PropertyPath("ScaleY"));
                    scale.Children.Add(scaleX);
                    scale.Children.Add(scaleY);
                    scale.Begin();
                }
            }
        }

        void OnLayoutPinchDelta(object sender, PinchGestureEventArgs e)
        {
            if (_measurePinch)
            {
                double scale = Math.Min(1.0, e.DistanceRatio);
                scale = Math.Max(0.5, scale);

                var transform = PageViewPanel.RenderTransform as CompositeTransform;

                transform.ScaleX = scale;
                transform.ScaleY = scale;
            }
        }

        private void OnLayoutPinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            this._measurePinch = true;
            PageManager.IgnoreInteractions = true;
        }

        #endregion

        private void OnUnload(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "ShowPage", false);
            DetachPageManager(this.PageManager);
            DetachViewModel(this.ControlViewModel);
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            AttachPageManager(this.PageManager);
            AttachViewModel(this.ControlViewModel);
        }

        private void AttachViewModel(ThreadPageSlideViewModel model)
        {
            model.ViewStateChanged += OnModelViewStateChanged;
        }

        private void DetachViewModel(ThreadPageSlideViewModel model)
        {
            model.ViewStateChanged -= OnModelViewStateChanged;
        }

        private void AttachPageManager(ThreadPageManager manager)
        {
            manager.Loading += new EventHandler(OnPageLoading);
            manager.Loaded += new EventHandler(OnPageLoaded);
            manager.ReadyForContent += new EventHandler(OnReadyForContent);
            manager.PostSelected += new EventHandler(OnPostSelected);
        }

        private void DetachPageManager(ThreadPageManager manager)
        {
            manager.Loading -= new EventHandler(OnPageLoading);
            manager.Loaded -= new EventHandler(OnPageLoaded);
            manager.ReadyForContent -= new EventHandler(OnReadyForContent);
            manager.PostSelected -= new EventHandler(OnPostSelected);
        }

        #region ViewModel Events

        private void OnModelViewStateChanged(object sender, EventArgs e)
        {
            var model = sender as ThreadPageSlideViewModel;
            switch (model.CurrentState)
            {
                case ThreadPageSlideViewModel.ViewStates.Switching:
                    if (PageManager.IsReady)
                        PageManager.ClearHtml();
                    break;

                case ThreadPageSlideViewModel.ViewStates.Loading:
                    VisualStateManager.GoToState(this, "Loading", true);
                    break;

                case ThreadPageSlideViewModel.ViewStates.Ready:
                    this.PageManager.LoadHtml(this.ControlViewModel.CurrentThreadPage.Html);
                    break;
            }
        }

        #endregion

        #region PageManager Events

        private void OnPageLoaded(object sender, EventArgs e)
        {
            // process posts - scroll to first unread post or last read post
            int targetPostIndex = ControlViewModel.CurrentThreadPage.Data.TargetPostIndex;
            var targetPost = ControlViewModel.CurrentThreadPage.Posts[targetPostIndex];

            if (targetPost != null)
                this.PageManager.ScrollToPost(targetPost);
            else
                this.PageManager.ScrollToPost(ControlViewModel.CurrentThreadPage.Posts.First());

            VisualStateManager.GoToState(this, "ShowPage", true);
           
        }

        private void OnPageLoading(object sender, EventArgs e) { }

        private void OnReadyForContent(object sender, EventArgs e)
        {
            if (this.PageManager.IsReady)
            {
                // process state
                // PageManager.ClearHtml();
                // this.pageSlideControl.PageProvider.CurrentIndex = ControlViewModel.CurrentPage - 1;
            }
        }

        private void OnPostSelected(object sender, EventArgs e)
        {
            // open thread context post menu; wire up selected post by index
            MenuProvider.ShowPostMenu(ControlViewModel.CurrentThreadPage.Posts[PageManager.SelectedPostIndex - 1]);
        }

        #endregion

        #region UI Events

        private void HightlightSwipeGlyph(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null)
            {
                Telerik.Windows.Controls.RadAnimationManager.Play(element, FadeInAnimation);
            }
        }

        private void HideSwipeGlyph(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null)
                Telerik.Windows.Controls.RadAnimationManager.Play(element, FadeOutAnimation);
        }

        private void SwipeComplete(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            // handle animations
            HideSwipeGlyph(sender, e);
        }

        private void PrimeLeftSwipe(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            // handle animations
            HightlightSwipeGlyph(sender, e);
        }

        private void PrimeRightSwipe(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            // handle animations
            HightlightSwipeGlyph(sender, e);
        }

        private void SlideAnimationStarted(object sender, EventArgs e)
        {

        }

        private void SlideAnimationCompleted(object sender, EventArgs e)
        {
          
        }

        private void MonitorSwipeDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {

        }

        private void ScrollToPost(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
        	// TODO: Add event handler implementation here.
            VisualStateManager.GoToState(this, "ShowPage", true);
            this.PageManager.ScrollToPost(e.Item.AssociatedDataItem.Value as Data.ThreadPostSource);
        }

        private void HidePostJumpList(object sender, System.Windows.RoutedEventArgs e)
        {
        	// TODO: Add event handler implementation here.
            VisualStateManager.GoToState(this, "ShowPage", true);
        }

        private void ShowPostJumpList(object sender, System.Windows.RoutedEventArgs e)
        {
        	// TODO: Add event handler implementation here.
            VisualStateManager.GoToState(this, "ShowJumpList", true);
        }

        private void ShowThreadTitle(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            VisualStateManager.GoToState(this, "ShowTitle", true);
            this._hideTitleTimer.Start();
        }

        #endregion

        private void OnCustomPageNavTextLostFocus(object sender, RoutedEventArgs e)
        {
            // set the timer to 2 seconds again and start
            this._hideTitleTimer.Interval = TimeSpan.FromSeconds(2);
            this._hideTitleTimer.Start();
        }

        private void OnCustomPageNavTextGotFocus(object sender, RoutedEventArgs e)
        {
            this._hideTitleTimer.Stop();
        }

        private void ScrollToTop(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ThreadPageManager.Instance.ScrollToPost(ControlViewModel.CurrentThreadPage.Posts.First());
            HidePostJumpList(sender, null);
        }

        private void ScrollToBottom(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ThreadPageManager.Instance.ScrollToPost(ControlViewModel.CurrentThreadPage.Posts.Last());
            HidePostJumpList(sender, null);
        }
    }
}
