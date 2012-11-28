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
using Telerik.Windows.Data;
using System.Windows.Media.Animation;

namespace Awful
{
    public partial class ForumListPage : PhoneApplicationPage
    {
        public ForumListPage()
        {
            InitializeComponent();
            InitializeJumpList();
            PrepareAnimations();
        }

        private ViewModels.ForumNavViewModel viewmodel;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);


            if (viewmodel == null)
                viewmodel = new ViewModels.ForumNavViewModel();

            if (!viewmodel.IsDataLoaded)
                viewmodel.LoadData();

            this.DataContext = viewmodel;
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
            GenericGroupDescriptor<Data.ForumDataModel, object> groupByDataGroup =
                new GenericGroupDescriptor<Data.ForumDataModel, object>();

            groupByDataGroup.KeySelector = (Data.ForumDataModel item) => { return item.Data.ForumGroup; };
            groupSource.Add(groupByDataGroup);

            // sorting criteria
            List<DataDescriptor> sortSource = new List<DataDescriptor>()
            {
                new GenericSortDescriptor<Data.ForumDataModel, double>(item => item.Weight)
            };

            this.ForumItemSelector.GroupDescriptorsSource = groupSource;
            this.ForumItemSelector.SortDescriptorsSource = sortSource;
        }

        private void ForumItemSelector_ItemTap(object sender,
            Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
            var item = e.Item.AssociatedDataItem.Value as Data.ForumDataModel;
            if (!item.Handled)
                item.NavigateToForum(NavigationService);
            else
                item.Handled = false;
        }

        private void RadExpanderControl_ExpandedStateChanging(object sender, 
            
            Telerik.Windows.Controls.ExpandedStateChangingEventArgs e)
        {
          
        }
    }
}