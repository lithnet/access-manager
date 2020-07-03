using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Lithnet.AccessManager.Server.UI
{
    public class EnumToDisplayConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            var enumValue = value as Enum;
            return enumValue == null ? DependencyProperty.UnsetValue : enumValue.GetEnumDescription();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            return value;
        }
    }
}
