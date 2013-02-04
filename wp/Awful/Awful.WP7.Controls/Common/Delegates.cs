using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful
{
   public delegate void IndexSelectedDelegate(object sender, int index);
   public delegate void LoadMoreItemsDelegate(object sender);
   public delegate void RefreshDelegate(object sender);
   
   public delegate void NavigateToThreadPageDelegate(object sender, int index, NavigateToThreadPageArgs args);
   public sealed class NavigateToThreadPageArgs
   {
       public int Page { get; private set; }

       public NavigateToThreadPageArgs(int page) { Page = page; }

       public static readonly NavigateToThreadPageArgs First;
       public static readonly NavigateToThreadPageArgs Last;
       static NavigateToThreadPageArgs()
       {
           First = new NavigateToThreadPageArgs(0);
           Last = new NavigateToThreadPageArgs(-1);
       }
   }
}
