using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Awful.WP7.Controls
{
    public class ForumList : Control
    {
        public static readonly DependencyProperty ForumListSourceProperty = DependencyProperty.Register(
            "ForumListSource", typeof(IEnumerable<object>), typeof(ForumList), new PropertyMetadata(null));

        public IEnumerable<object> ForumListSource
        {
            get { return GetValue(ForumListSourceProperty) as IEnumerable<object>; }
            set { SetValue(ForumListSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
            "ItemTemplate", typeof(DataTemplate), typeof(ThreadList), new PropertyMetadata(null));

        public DataTemplate ItemTemplate
        {
            get { return GetValue(ItemTemplateProperty) as DataTemplate; }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public ForumList()
            : base()
        {
            this.DefaultStyleKey = typeof(ForumList);
        }
    }
}
