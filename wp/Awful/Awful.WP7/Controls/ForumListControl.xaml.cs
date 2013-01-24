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
using System.Windows.Media.Animation;
using Telerik.Windows.Data;
using Awful.WP7;

namespace Awful.Controls
{
    public partial class ForumListControl : UserControl
    {
        public bool IsUngrouped
        {
            get { return this.UngroupedForumItemSelector.Visibility == System.Windows.Visibility.Visible; }
            set
            {
                if (value)
                {
                    this.ForumItemSelector.Visibility = System.Windows.Visibility.Collapsed;
                    this.UngroupedForumItemSelector.Visibility = System.Windows.Visibility.Visible;
                    this.control = UngroupedForumItemSelector;
                }
                else
                {
                    this.UngroupedForumItemSelector.Visibility = System.Windows.Visibility.Collapsed;
                    this.ForumItemSelector.Visibility = System.Windows.Visibility.Visible;
                    this.control = ForumItemSelector;
                }
            }
        }

        public ForumListControl()
        {
            InitializeComponent();
            PrepareAnimations();
            InitializeJumpList();
            InteractionEffectManager.AllowedTypes.Add(typeof(RadDataBoundListBoxItem));
			this.IsUngrouped = false;
        }

        private void PrepareAnimations()
        {
            Func<RadMoveAndFadeAnimation> createAddAnimation = () =>
            {
                var duration = new TimeSpan(0, 0, 0, 0, 500);
                var moveAndFade = new RadMoveAndFadeAnimation();
                moveAndFade.MoveAnimation.StartPoint = new Point(0, -90);
                moveAndFade.MoveAnimation.EndPoint = new Point(0, 0);
                moveAndFade.FadeAnimation.StartOpacity = 0;
                moveAndFade.FadeAnimation.EndOpacity = 1;
                moveAndFade.Easing = new CubicEase();
                moveAndFade.Duration = new Duration(duration);
                return moveAndFade;
            };

            this.ForumItemSelector.ItemAddedAnimation = createAddAnimation();
            this.UngroupedForumItemSelector.ItemAddedAnimation = createAddAnimation();

            Func<RadMoveAndFadeAnimation> createRemoveAnimation = () =>
            {
                var duration = new TimeSpan(0, 0, 0, 0, 500);
                var moveAndFade = new RadMoveAndFadeAnimation();
                moveAndFade.MoveAnimation.StartPoint = new Point(0, 0);
                moveAndFade.MoveAnimation.EndPoint = new Point(0, -90);
                moveAndFade.FadeAnimation.StartOpacity = 1;
                moveAndFade.FadeAnimation.EndOpacity = 0;
                moveAndFade.Easing = new CubicEase() { EasingMode = EasingMode.EaseOut };
                moveAndFade.Duration = new Duration(duration);
                return moveAndFade;
            };

            this.ForumItemSelector.ItemRemovedAnimation = createRemoveAnimation();
            this.UngroupedForumItemSelector.ItemRemovedAnimation = createRemoveAnimation();
        }

        private void InitializeJumpList()
        {
            // grouping criteria
            List<DataDescriptor> groupSource = new List<DataDescriptor>();
            GenericGroupDescriptor<Data.ForumDataSource, object> groupByDataGroup =
                new GenericGroupDescriptor<Data.ForumDataSource, object>();

            groupByDataGroup.KeySelector = (Data.ForumDataSource item) => { return item.Data.ForumGroup; };
            groupSource.Add(groupByDataGroup);

            // sorting criteria
            List<DataDescriptor> sortSource = new List<DataDescriptor>()
            {
                new GenericSortDescriptor<Data.ForumDataSource, double>(item => item.Weight)
            };

            this.ForumItemSelector.GroupDescriptorsSource = groupSource;
            this.ForumItemSelector.SortDescriptorsSource = sortSource;
        }

        private void ForumItemSelector_ItemTap(object sender,
            Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var item = e.Item.AssociatedDataItem.Value as Data.ForumDataSource;
            if (!item.Handled)
            {
                var frame = App.Current.RootVisual as PhoneApplicationFrame;
                item.NavigateToForum(frame.Navigate);
            }
            else
                item.Handled = false;
        }

        bool refreshRequsted;
        RadVirtualizingDataControl control = null;
        private void OnRefreshRequested(object sender, EventArgs e)
        {
            var context = (sender as RadJumpList).DataContext;
            var viewmodel = context as ViewModels.ListViewModel<Data.ForumDataSource>;
            if (viewmodel != null)
            {
             	control = sender as RadVirtualizingDataControl;
             	control.IsHitTestVisible = false;
				Refresh(viewmodel);
            }
        }
		
		void Refresh(ViewModels.ListViewModel<Data.ForumDataSource> viewmodel)
		{
			 refreshRequsted = true;
             viewmodel.DataLoaded += OnViewmodelDataLoaded;
             viewmodel.Refresh();
		}

        void OnViewmodelDataLoaded(object sender, EventArgs e)
        {
            var viewmodel = sender as ViewModels.ListViewModel<Data.ForumDataSource>;
            viewmodel.DataLoaded -= OnViewmodelDataLoaded;
            if (refreshRequsted)
            {
                control.IsHitTestVisible = true;
				
				if (this.control is RadDataBoundListBox)
                	(this.control as RadDataBoundListBox).StopPullToRefreshLoading(true);
				else
					(this.control as RadJumpList).StopPullToRefreshLoading(true);
				
                refreshRequsted = false;
            }
        }

        private void RefreshEmptyList(object sender, System.Windows.Input.GestureEventArgs e)
        {
        	// TODO: Add event handler implementation here.
			var viewmodel = this.DataContext as ViewModels.ListViewModel<Data.ForumDataSource>;
			if (viewmodel != null)
			{
				control.IsHitTestVisible = false;
				Refresh(viewmodel);
			}
        }
    }
}
