using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Telerik.Windows.Controls.Primitives;

namespace Awful.Controls
{
	public class LoadMoreControl : RadContentControl
	{
		public LoadMoreControl()
        {
            this.DefaultStyleKey = typeof(LoadMoreControl);
        }
	}
}