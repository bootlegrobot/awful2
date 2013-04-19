using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Awful.ViewModels;

namespace Awful.Controls
{
    public class AwfulSmileyControl : Control
    {
        private RadDataBoundListBox listBox;
        private RadAutoCompleteBox SmileyAutoComplete;
        private SmileyListViewModel SmileyViewModel;

        private static readonly DependencyProperty DataBoundListBoxProperty = DependencyProperty.RegisterAttached(
            "DataBoundListBox", typeof(RadDataBoundListBox), typeof(AwfulSmileyControl), new PropertyMetadata(null));

        public static RadDataBoundListBox GetDataBoundListBox(UIElement element)
        {
            if (element is AwfulSmileyControl)
                return (element as AwfulSmileyControl).listBox;

            return null;
        }

        private static readonly DependencyProperty ItemTapCommandProperty = DependencyProperty.Register(
            "ItemTapCommand", typeof(ICommand), typeof(AwfulSmileyControl), new PropertyMetadata(null));

        public ICommand ItemTapCommand
        {
            get { return GetValue(ItemTapCommandProperty) as ICommand; }
            set { SetValue(ItemTapCommandProperty, value); }
        }

        public AwfulSmileyControl()
            : base()
        {
            this.DefaultStyleKey = typeof(AwfulSmileyControl);
            this.Loaded += AwfulSmileyControl_Loaded;
        }

        private void AwfulSmileyControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!SmileyViewModel.IsDataLoaded)
                SmileyViewModel.LoadData();
        }

        private void InitializeSmilieyAutoComplete()
        {
            SmileyAutoComplete.Text = string.Empty;
            SmileyAutoComplete.FilterKeyProvider = (object item) =>
            {
                var smiley = item as Data.SmileyDataModel;
                return smiley.Code;
            };
        }
        private void SmileyDataRequested(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
            var listBox = sender as Telerik.Windows.Controls.RadDataBoundListBox;
            listBox.DataVirtualizationMode = Telerik.Windows.Controls.DataVirtualizationMode.OnDemandManual;
            SmileyViewModel.AppendNextPage();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Grid root = this.GetTemplateChild("LayoutRoot") as Grid;
            RadAutoCompleteBox racb = this.GetTemplateChild("autoComplete") as RadAutoCompleteBox;
            RadDataBoundListBox list = this.GetTemplateChild("listBox") as RadDataBoundListBox;
            if (list == null)
                throw new InvalidOperationException("smiley control list is null");

            this.listBox = list;
            list.ItemTap += list_ItemTap;
            list.DataRequested += SmileyDataRequested;

            SmileyViewModel = new SmileyListViewModel();
            racb.DataContext = SmileyViewModel;
            list.DataContext = SmileyViewModel;
            SmileyAutoComplete = racb;
            InitializeSmilieyAutoComplete();
        }

        private void list_ItemTap(object sender, ListBoxItemTapEventArgs e)
        {
            if (ItemTapCommand != null)
            {
                var item = e.Item.AssociatedDataItem.Value;
                ItemTapCommand.Execute(item);
            }
        }
    }
}
