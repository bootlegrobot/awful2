using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Awful
{
    public abstract class CustomTemplateSelector : ContentControl
    {
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            
            DataTemplate contentTemplate = SelectDataTemplate(newContent, this);
            if (contentTemplate != null)
                this.ContentTemplate = ContentTemplate;
        }

        protected abstract DataTemplate SelectDataTemplate(object content, DependencyObject container);
    }

    public interface IDataTemplateProvider
    {
        DataTemplate ContentTemplate { get; set; }
    }

    public class VariableContentControl : CustomTemplateSelector
    {
        protected override DataTemplate SelectDataTemplate(object content, DependencyObject container)
        {
            if (content is IDataTemplateProvider)
                return (content as IDataTemplateProvider).ContentTemplate;

            return null;
        }
    }

    
}
