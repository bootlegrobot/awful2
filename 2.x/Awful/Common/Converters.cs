using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

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

}
