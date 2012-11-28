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

namespace Awful.Controls
{
    public partial class ForumListControl : UserControl
    {
        public ForumListControl()
        {
            InitializeComponent();
            PrepareAnimations();
            InitializeJumpList();
        }

        private void PrepareAnimations()
        {
            //InteractionEffectManager.AllowedTypes.Add(typeof(RadDataBoundListBoxItem));

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
        private void OnRefreshRequested(object sender, EventArgs e)
        {
            var context = (sender as RadJumpList).DataContext;
            var viewmodel = context as ViewModels.ListViewModel<Data.ForumDataSource>;
            if (viewmodel != null)
            {
                refreshRequsted = true;
                viewmodel.DataLoaded += OnViewmodelDataLoaded;
                viewmodel.Refresh();
            }

        }

        void OnViewmodelDataLoaded(object sender, EventArgs e)
        {
            var viewmodel = sender as ViewModels.ListViewModel<Data.ForumDataSource>;
            viewmodel.DataLoaded -= OnViewmodelDataLoaded;
            if (refreshRequsted)
            {
                this.ForumItemSelector.StopPullToRefreshLoading(true);
                refreshRequsted = false;
            }
        }
    }
}
