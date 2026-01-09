using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace BiddingBrowser.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class VisibilityConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not bool booleanValue) {
            throw new InvalidCastException();
        }

        var visibleIfTrue = true;
        if (parameter is string parameterText) {
            visibleIfTrue = parameterText.Equals("0");
        }

        if (visibleIfTrue) {
            return booleanValue ? Visibility.Visible : Visibility.Collapsed;
        }
        else {
            return booleanValue ? Visibility.Collapsed : Visibility.Visible;
        }
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}
