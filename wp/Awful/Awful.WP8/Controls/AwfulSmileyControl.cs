using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Awful.Controls
{
    public class AwfulSmileyControl : Control
    {
        private RadDataBoundListBox listBox;

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
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            RadDataBoundListBox list = this.GetTemplateChild("listBox") as RadDataBoundListBox;
            if (list == null)
                throw new InvalidOperationException("smiley control list is null");

            this.listBox = list;
            list.ItemTap += list_ItemTap;
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
