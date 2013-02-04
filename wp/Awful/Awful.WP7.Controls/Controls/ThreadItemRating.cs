using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Awful.WP7.Controls
{
    public class ThreadItemRating : Control
    {
        public static readonly DependencyProperty RatingColorsProperty = DependencyProperty.Register(
            "RatingColors", typeof(RatingColorModel), typeof(ThreadItemRating), new PropertyMetadata(null, (o, e) =>
                {
                    var control = o as ThreadItemRating;
                    control.OnRatingChanged(control.Rating);

                }));

        public RatingColorModel RatingColors
        {
            get { return GetValue(RatingColorsProperty) as RatingColorModel; }
            set { SetValue(RatingColorsProperty, value); }
        }

        public static readonly DependencyProperty RatingProperty = DependencyProperty.Register(
            "Rating", typeof(int), typeof(ThreadItemRating), new PropertyMetadata(5, (o, e) =>
                {
                    (o as ThreadItemRating).OnRatingChanged((int)e.NewValue);
                }));

        private void OnRatingChanged(int p)
        {
            if (RatingColors != null)
            {
                var foreground = RatingColors
                    .Where(model => model.Rating == p)
                    .Select(model => model.Color)
                    .Single();

                this.Foreground = foreground;
                this.Background = Darken(foreground, DARK_FACTOR);
            }
        }

        public int Rating
        {
            get { return (int)GetValue(RatingProperty); }
            set { SetValue(RatingProperty, value); }
        }

        private const double DARK_FACTOR = 0.7;
        private static SolidColorBrush Darken(SolidColorBrush brush, double factor)
        {
            var color = brush.Color;
            var darkened = Color.FromArgb(
                color.A,
                Convert.ToByte(color.R - (color.R * factor)),
                Convert.ToByte(color.G - (color.G * factor)),
                Convert.ToByte(color.B - (color.B * factor)));

            return new SolidColorBrush(darkened);
        }

        public ThreadItemRating()
            : base()
        {
            this.DefaultStyleKey = typeof(ThreadItemRating);
        }

    }
}
