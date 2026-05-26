using Model.Bidding.Bids;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding; 
public class Auction {
    public PlayerPosition CurrentBidder { get; private set; }
    public List<Bid> AuctionHistory { get; private set; }

    public Auction(PlayerPosition dealer) {
        CurrentBidder = dealer;
        AuctionHistory = new List<Bid>();
    }

    public bool IsCompleted() { 
        if (AuctionHistory.Count >= 4) {    // edge case: 3 passes at the beginning of the auction
            if (AuctionHistory[^1].BidType == BidType.Pass
                && AuctionHistory[^2].BidType == BidType.Pass
                && AuctionHistory[^3].BidType == BidType.Pass) {
                return true;
            }
        }
        
        return false;
    }

    public void Submit(Bid bid) { 
        AuctionHistory.Add(bid);
        CurrentBidder = (PlayerPosition)(((int)CurrentBidder + 1) % 4); // why cant i do math on enums in c# :(
    }

}
