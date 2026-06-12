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

    public int Loop => AuctionHistory.Count / 4;

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
            if (AuctionHistory[^1].Type == BidType.Pass
                && AuctionHistory[^2].Type == BidType.Pass
                && AuctionHistory[^3].Type == BidType.Pass) {
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
    public Bid? GetLastPlayerBid(PlayerPosition bidderPosition, bool passAsNull = false) {
        for (int i = AuctionHistory.Count - 1; i >= 0; i--) {
            if ((PlayerPosition)(((int)CurrentBidder - (AuctionHistory.Count - i) + 4) % 4) == bidderPosition) {
                var bid = AuctionHistory[i];
                return bid.Type == BidType.Pass ? null : bid;
            }
        }
        return null;
    }

    public Bid? GetLastSubmittedBid() {
        for (int i = AuctionHistory.Count - 1; i >= 0; i--) {
            if (AuctionHistory[i].Type == BidType.Submit) {
                return AuctionHistory[i];
            }
        }
        return null;
    }

    public bool PlayerOpenedAuction(PlayerPosition bidderPosition) {
        for (int i = 0; i < AuctionHistory.Count; i++) {
            if ((PlayerPosition)(((int)CurrentBidder - (AuctionHistory.Count - i) + 4) % 4) == bidderPosition) {
                return AuctionHistory[i].Type == BidType.Submit;
            }
        }
        return false;
    }

    public Bid? FirstPlayerBid(PlayerPosition bidderPosition) {
        for (int i = 0; i < AuctionHistory.Count; i++) {
            if ((PlayerPosition)(((int)CurrentBidder - (AuctionHistory.Count - i) + 4) % 4) == bidderPosition) {
                return AuctionHistory[i];
            }
        }
        return null;
    }


    public List<Bid> GetPlayersSequence(PlayerPosition bidderPosition, out PlayerPosition? openingPlayer) {
        var playerBids = new List<Bid>();
        var partnerPosition = (PlayerPosition)(((int)bidderPosition + 2) % 4);

        openingPlayer = null;

        for (int i = 0; i < AuctionHistory.Count; i++) {
            var bidPlayer = (PlayerPosition)(((int)CurrentBidder - (AuctionHistory.Count - i) + 4) % 4);

            if (bidPlayer != bidderPosition && bidPlayer != partnerPosition) {
                continue;
            }

            if (openingPlayer == null && AuctionHistory[i].Type != BidType.Pass) {
                openingPlayer = bidPlayer;
            }

            playerBids.Add(AuctionHistory[i]);
        }

        return playerBids;
    }


    public bool NobodyBidsYet() => AuctionHistory.All(e => e.Type == BidType.Pass);


    public bool ReachedGameLevel() => GetLastSubmittedBid()?.MakesGame() ?? false;
}
