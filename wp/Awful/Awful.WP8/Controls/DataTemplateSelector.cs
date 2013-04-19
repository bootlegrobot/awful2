using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Awful.Controls
{
    public abstract class DataTemplateSelector : ContentControl
    {
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            ContentTemplate = SelectTemplate(newContent, this);
        }

        protected virtual System.Windows.DataTemplate SelectTemplate(object content, DependencyObject container)
        {
            return this.ContentTemplate;
        }
    }
}
