using BiddingBrowser.BiddingTree.Bids;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace BiddingBrowser.Converters {

    public class BidColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            // Null check
            if (value == null) return null;
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) return Binding.DoNothing;

            // Try to parse safely
            if (Enum.TryParse(typeof(BidColor), value.ToString(), out var result)) {
                return result;
            }

            // Prevent exceptions
            return Binding.DoNothing;
        }
    }
}
