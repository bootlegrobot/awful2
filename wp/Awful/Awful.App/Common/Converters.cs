using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Awful.Common
{
    #region Visibility

    public abstract class VisibilityConverter<T> : IValueConverter
    {
        public abstract bool ShowCondition(T value);
       
        public virtual object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is T))
                return Visibility.Visible;
            else
            {
                bool isVisible = ShowCondition((T)value);
                if (isVisible)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CollapseOnFalse : VisibilityConverter<bool>
    {
        public override bool ShowCondition(bool value)
        {
            return value == true;
        }
    }

    public class CollapseOnTrue : VisibilityConverter<bool>
    {
        public override bool ShowCondition(bool value)
        {
            return value == false;
        }
    }

    #endregion

    public class ShadowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double factor = 0.0;

            if (parameter == null || !double.TryParse(parameter as string,
                System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out factor))
                factor = 1.0;

            // in case the value passed in is a double, but outside the range of allowed values
            else 
            { 
                factor = Math.Min(1.0, factor);
                factor = Math.Max(0.0, factor);
            }

            if (value is SolidColorBrush)
            {
                Color baseColor = (value as SolidColorBrush).Color;
                Color shadowColor = Color.FromArgb(
                    baseColor.A,
                    System.Convert.ToByte(baseColor.R * factor),
                    System.Convert.ToByte(baseColor.G * factor),
                    System.Convert.ToByte(baseColor.B * factor));

                Brush shadow = new SolidColorBrush() { Color = shadowColor };
                return shadow;
            }

            if (value is Color)
            {
                Color baseColor = (Color)value;
                Color shadowColor = Color.FromArgb(
                    baseColor.A,
                    System.Convert.ToByte(baseColor.R * factor),
                    System.Convert.ToByte(baseColor.G * factor),
                    System.Convert.ToByte(baseColor.B * factor));

                return shadowColor;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ContentFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            string text = value.ToString();
			string filtered = text;
			
			if (Helpers.ContentFilter.IsContentFilterEnabled)
            	filtered = Helpers.ContentFilter.Censor(text);
			
            return filtered;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LowercaseConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString().ToLower();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UppercaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString().ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AssetRetriever : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is string))
                return value;
            else
            {
                string key = value as string;
                object item = App.Current.Resources[key];
                if (item != null)
                    return item;
                else
                    throw new Exception("No such resource exists with key: " + key);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ForumLayoutRetriever : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty ThemeManagerProperty = DependencyProperty.Register(
            "ThemeManager", typeof(ThemeManager), typeof(ForumLayoutRetriever), new PropertyMetadata(null));

        public ThemeManager ThemeManager
        {
            get { return GetValue(ThemeManagerProperty) as ThemeManager; }
            set { SetValue(ThemeManagerProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (ThemeManager == null)
                throw new NullReferenceException("ThemeManager must not be null.");

            if (value is Data.ForumDataSource)
            {
                Data.ForumDataSource forum = value as Data.ForumDataSource;
                if (forum.Layout == null)
                {
                    var layout = ThemeManager.GetForumLayoutById(forum.ForumID);
                    forum.Layout = layout;
                }

                return forum.Layout;
            }

            else
                return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
