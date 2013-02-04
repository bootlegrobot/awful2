using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Awful.WP7.Controls
{
    public class RatingColorItem
    {
        public int Rating { get; set; }
        public SolidColorBrush Color { get; set; }
    }

    public class RatingColorModel : List<RatingColorItem>
    {
        public RatingColorModel() : base() { }
        public RatingColorModel(IEnumerable<RatingColorItem> items) : base(items) { }
        public RatingColorModel(int capacity) : base(capacity) { }
    }
}
