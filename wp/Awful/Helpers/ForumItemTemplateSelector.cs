using System;
using Telerik.Windows.Controls;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Navigation;
using Awful.Data;

namespace Awful.Helpers
{
    public class ForumItemTemplateSelector : DataTemplateSelector
    {
        private DataTemplate _MultiForumTemplate;
        public DataTemplate MultiForumTemplate
        {
            get { return this._MultiForumTemplate; }
            set { this._MultiForumTemplate = value; }
        }

        private DataTemplate _SingleForumTempalte;
        public DataTemplate SingleForumTemplate
        {
            get { return this._SingleForumTempalte; }
            set { this._SingleForumTempalte = value; }
        }

        public ForumItemTemplateSelector() : base() { }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var viewmodel = item as ForumDataSource;
            DataTemplate select = null;

            if (viewmodel == null)
                select = base.SelectTemplate(item, container);

            else if (viewmodel.HasNoItems)
                select = _SingleForumTempalte;

            else
                select = _MultiForumTemplate;
            
            return select;
        }
    }
}
