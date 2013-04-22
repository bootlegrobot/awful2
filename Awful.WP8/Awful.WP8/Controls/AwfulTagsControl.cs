using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace Awful.Controls
{
    public class AwfulTagsControl : Control
    {
        #region ListBox

        private RadDataBoundListBox listBox;

        private static readonly DependencyProperty DataBoundListBoxProperty = DependencyProperty.RegisterAttached(
            "DataBoundListBox", typeof(RadDataBoundListBox), typeof(AwfulTagsControl), new PropertyMetadata(null));

        public static RadDataBoundListBox GetDataBoundListBox(UIElement element)
        {
            if (element is AwfulTagsControl)
                return (element as AwfulTagsControl).listBox;

            return null;
        }

        #endregion

        #region ItemTapCommand

        private static readonly DependencyProperty ItemTapCommandProperty = DependencyProperty.Register(
            "ItemTapCommand", typeof(ICommand), typeof(AwfulTagsControl), new PropertyMetadata(null));

        public ICommand ItemTapCommand
        {
            get { return GetValue(ItemTapCommandProperty) as ICommand; }
            set { SetValue(ItemTapCommandProperty, value); }
        }

        #endregion

        #region Colors

        private static readonly DependencyProperty TagForegroundProperty = DependencyProperty.Register(
            "TagForeground", typeof(Brush), typeof(AwfulTagsControl), new PropertyMetadata(null));

        public Brush TagForeground
        {
            get { return GetValue(TagForegroundProperty) as Brush; }
            set { SetValue(TagForegroundProperty, value); }
        }

        #endregion

        public AwfulTagsControl() : base() { DefaultStyleKey = typeof(AwfulTagsControl); }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            RadDataBoundListBox list = this.GetTemplateChild("listBox") as RadDataBoundListBox;
            if (list == null)
                throw new Exception("tag list control is null");

            listBox = list;
            list.ItemTap += list_ItemTap;
        }

        void list_ItemTap(object sender, ListBoxItemTapEventArgs e)
        {
            if (ItemTapCommand != null)
            {
                var item = e.Item.AssociatedDataItem.Value;
                ItemTapCommand.Execute(item);
            }
        }
    }
}
