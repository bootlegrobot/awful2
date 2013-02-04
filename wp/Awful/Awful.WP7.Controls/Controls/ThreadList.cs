using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace Awful.WP7.Controls
{
    public class ThreadList : Control
    {
        public static readonly DependencyProperty ThreadListSourceProperty = DependencyProperty.Register(
            "ThreadListSource", typeof(IEnumerable<object>), typeof(ThreadList), new PropertyMetadata(null));

        public IEnumerable<object> ThreadListSource
        {
            get { return GetValue(ThreadListSourceProperty) as IEnumerable<object>; }
            set { SetValue(ThreadListSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            "ItemTemplate", typeof(DataTemplate), typeof(ThreadList), new PropertyMetadata(null));

        public DataTemplate ItemTemplate
        {
            get { return GetValue(ItemTemplateProperty) as DataTemplate; }
            set { SetValue(ItemTemplateProperty, value); }
        }
		
		public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
			"Status", typeof(string), typeof(ThreadList), new PropertyMetadata(null));
		
		public string Status
		{
			get { return GetValue(StatusProperty) as string; }
			set { SetValue(StatusProperty, value); }
		}

        public ThreadList()
            : base()
        {
            this.DefaultStyleKey = typeof(ThreadList);
        }
		
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			RadDataBoundListBox threadItemsPresenter = this.GetTemplateChild("threadItemsPresenter") as RadDataBoundListBox;
			if (threadItemsPresenter != null)
				AttachListBoxEvents(threadItemsPresenter);
		}
		
		private void AttachListBoxEvents(RadDataBoundListBox listBox)
		{
            listBox.DataRequested += listBox_DataRequested;
            listBox.ItemTap += listBox_ItemTap;
		}

        void listBox_ItemTap(object sender, ListBoxItemTapEventArgs e)
        {
            throw new NotImplementedException();
        }

        void listBox_DataRequested(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
