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

namespace Awful.Helpers
{
	public class OddEvenTemplateSelector : DataTemplateSelector
	{
        private int _index = 1;
        private Dictionary<object, int> _indexTable;
		
		public OddEvenTemplateSelector() : base()
		{
			// Insert code required on object creation below this point.
            _indexTable = new Dictionary<object, int>();
		}
		
		public DataTemplate EvenTemplate { get; set; }
		public DataTemplate OddTemplate { get; set; }
		
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate result = null;
            int key = -1;

            if (_indexTable.ContainsKey(item))
                key = _indexTable[item];
            
            else
            {
                key = _index;
                _indexTable.Add(item, key);
                _index++;
            }

            result = key % 2 == 0 ?
                EvenTemplate :
                OddTemplate;

            return result;
        }
	}
}