using BiddingBrowser.BiddingTree.Bids;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace BiddingBrowser.Converters;

public class BidTypeConverter : IValueConverter {

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return value.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        return Enum.Parse<BidType>(value.ToString());
    }
}
