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
using Telerik.Windows.Controls;

namespace Awful.Common
{
	public class RadContextMenuProvider : Common.BindableBase
	{
		public RadContextMenuProvider()
		{
			// Insert code required on object creation below this point.
		}
		
		private RadContextMenu _menu;
		public RadContextMenu Menu
		{
			get { return this._menu; }
			set { this.SetProperty(ref _menu, value, "Menu"); }
		}
	}
}