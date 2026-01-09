using BiddingBrowser.BiddingTree.Bids;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BiddingBrowser.Converters;

public class BidToBrushConverter : IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
        if (values.Length < 2)
            return GetBrush("AlabasterGreyBrush");

        if (values[0] is not BidType bidType ||
            values[1] is not BidColor bidColor) {
            return GetBrush("AlabasterGreyBrush");
        }

        if (bidType != BidType.Submit)
            return GetBrush("AlabasterGreyBrush");

        return bidColor switch {
            BidColor.NoTrump => GetBrush("NoTrumpBrush"),
            BidColor.Spades => GetBrush("SpadesBrush"),
            BidColor.Hearts => GetBrush("HeartsBrush"),
            BidColor.Diamonds => GetBrush("DiamondsBrush"),
            BidColor.Clubs => GetBrush("ClubsBrush"),
            _ => GetBrush("AlabasterGreyBrush")
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }

    private static Brush GetBrush(string key) {
        return Application.Current.TryFindResource(key) as Brush ?? Brushes.Transparent;
    }

}
