using BiddingBrowser.BiddingTree.Bids;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BiddingBrowser.BiddingTree;
/// <summary>
/// Interaction logic for BiddingTree.xaml
/// </summary>
public partial class BiddingTree : UserControl {

    private BiddingTreeViewModel _viewModel;

    public BiddingTree() {
        InitializeComponent();
        DataContext = _viewModel = new();
    }


    private void BiddingTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        _viewModel.SelectedItem = e.NewValue;
    }


    private void NewElementButton_Click(object sender, RoutedEventArgs e) {
        if (DataTree.SelectedItem is Root root) {
            root.Bids.Add(new Bid() { Identifier = "NewBid" });
            return;
        }

        if (DataTree.SelectedItem is Bid bid) {
            bid.NextBids.Add(new Bid() { Identifier = "NewBid" });
        }
    }
}
