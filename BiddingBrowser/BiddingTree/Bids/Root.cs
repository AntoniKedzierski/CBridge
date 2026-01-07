using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BiddingBrowser.BiddingTree.Bids;

public class Root : BindableBase {

    public required string Name { get; set => SetProperty(ref field, value); }

    public ObservableCollection<Bid> Bids { get; set => SetProperty(ref field, value); } = new();

}
