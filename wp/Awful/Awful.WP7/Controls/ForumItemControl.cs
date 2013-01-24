using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Awful.Controls
{
    public class ForumItemControl : ContentControl
    {
        private FrameworkElement stripeGlyph;
        private FrameworkElement mainGlyph;

        public ForumItemControl()
            : base()
        {
            this.DefaultStyleKey = typeof(ForumItemControl);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var element = this.GetTemplateChild("GraphicStack") as FrameworkElement;
            var render = new CompositeTransform();
            render.SkewX = this.SkewX;
            element.RenderTransform = render;

            /*
            element = this.GetTemplateChild("MainGlyph") as FrameworkElement;
            mainGlyph = element;

            element = this.GetTemplateChild("StripeGlyph") as FrameworkElement;
            element.SizeChanged += new SizeChangedEventHandler(StripeGlyphSizeChanged);
            */
        }

        public void StripeGlyphSizeChanged(object sender, SizeChangedEventArgs e)
        {
            mainGlyph.Height = e.NewSize.Height;
        }

        public static readonly DependencyProperty SkewXProperty = DependencyProperty.Register(
            "SkewX", typeof(double), typeof(ForumItemControl), new PropertyMetadata(13.0));

        public double SkewX
        {
            get { return (double)GetValue(SkewXProperty); }
            set { SetValue(SkewXProperty, value); }
        }

        public static readonly DependencyProperty GlyphHeightProperty = DependencyProperty.Register(
          "GlyphHeight", typeof(double), typeof(ForumItemControl), new PropertyMetadata(75.0));

        public double GlyphHeight
        {
            get { return (double)GetValue(GlyphHeightProperty); }
            set { SetValue(GlyphHeightProperty, value); }
        }

        public static readonly DependencyProperty SkewLeftProperty = DependencyProperty.Register(
            "SkewLeft", typeof(Thickness), typeof(ForumItemControl), new PropertyMetadata(new Thickness(9, 0, 0, 0)));

        public Thickness SkewLeft
        {
            get { return (Thickness)GetValue(SkewLeftProperty); }
            set { SetValue(SkewLeftProperty, value); }
        }

           public static readonly DependencyProperty SkewRightProperty = DependencyProperty.Register(
            "SkewRight", typeof(Thickness), typeof(ForumItemControl), new PropertyMetadata(new Thickness(9, 0, 0, 0)));

        public Thickness SkewRight
        {
            get { return (Thickness)GetValue(SkewRightProperty); }
            set { SetValue(SkewRightProperty, value); }
        }


        public static readonly DependencyProperty GlyphBrushProperty = DependencyProperty.Register(
         "GlyphBrush", typeof(Brush), typeof(ForumItemControl), new PropertyMetadata(null));

        public Brush GlyphBrush
        {
            get { return (Brush)GetValue(GlyphBrushProperty); }
            set { SetValue(GlyphBrushProperty, value); }
        }
    }
}
