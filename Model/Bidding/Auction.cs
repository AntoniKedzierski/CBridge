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


    public void Start(PlayerPosition dealer) {
        CurrentBidder = dealer;
        AuctionHistory = [];
    }


    public void Clear() {
        AuctionHistory.Clear();
    }


    public void NextBidder() {
        CurrentBidder = (PlayerPosition)(((int)CurrentBidder + 1) % 4);
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
        NextBidder();
    }

    /// <summary>
    /// Returns the last bid made by the specified player, or null if the player has not made any bids.
    /// </summary>
    /// <param name="bidderPosition"></param>
    /// <returns></returns>
    public Bid? GetLastPlayerBid(PlayerPosition bidderPosition) {
        for (int i = AuctionHistory.Count - 1; i >= 0; i--) {
            if ((PlayerPosition)(((int)CurrentBidder - (AuctionHistory.Count - i) + 4) % 4) == bidderPosition) {
                return AuctionHistory[i];
            }
        }
        return null;
    }

    public Bid? GetLastSubmittedBid() {
        for (int i = AuctionHistory.Count - 1; i >= 0; i--) {
            if (AuctionHistory[i].BidType == BidType.Submit) {
                return AuctionHistory[i];
            }
        }
        return null;
    }
}
