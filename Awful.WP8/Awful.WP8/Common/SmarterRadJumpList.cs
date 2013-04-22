using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.Windows.Controls;

namespace Awful.Common
{
    /// <summary>
    /// A RadJumpList that clears the ContentTemplate on recycled item containers. The ContentTemplate will
    /// be set accordingly once the container is prepared for override. This allievates the issue where
    /// recycled items would not update their layouts.
    /// </summary>
    public class SmarterRadJumpList : RadJumpList
    {
        public SmarterRadJumpList() : base() { }

        protected override void ClearContainerForItemOverride(RadVirtualizingDataControlItem element, Telerik.Windows.Data.IDataSourceItem item)
        {
            base.ClearContainerForItemOverride(element, item);
            element.ContentTemplate = null;
        }
    }
}
