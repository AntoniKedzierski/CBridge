using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BiddingBrowser.BiddingTree.Bids;

public class Root : BindableBase, IBidsContainer {

    public required string Name { get; set => SetProperty(ref field, value); }

    public ObservableCollection<Bid> Bids { get; set => SetProperty(ref field, value); } = new();

    public void AddBid(Bid bid) {
        Bids.Add(bid);
        bid.Parent = this;
    }

    public void RemoveBid(Bid bid) {
        Bids.Remove(bid);
    }


    public void MoveUp(Bid bid) {
        var index = Bids.IndexOf(bid);
        if (index < 1) {
            return;
        }

        Bids.Remove(bid);
        Bids.Insert(index - 1, bid);
    }


    public void MoveDown(Bid bid) {
        var index = Bids.IndexOf(bid);
        if (index >= Bids.Count - 1) {
            return;
        }

        Bids.Remove(bid);
        Bids.Insert(index + 1, bid);
    }
}
