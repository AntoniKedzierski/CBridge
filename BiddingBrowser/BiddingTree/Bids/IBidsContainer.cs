using System;
using System.Collections.Generic;
using System.Text;

namespace BiddingBrowser.BiddingTree.Bids; 

public interface IBidsContainer {

    public void AddBid(Bid bid);

    public void RemoveBid(Bid bid);

    public void MoveUp(Bid bid);

    public void MoveDown(Bid bid);
}
