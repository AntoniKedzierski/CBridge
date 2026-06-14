using Model.Bidding.Bids;
using Model.Enums;
using Model.Helpers;
using Newtonsoft.Json.Converters;
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
        if (!bid.IsBidLegal(this)) {
            throw new Exception("Illegal bid!");
        }
        AuctionHistory.Add(bid);
        NextBidder();
    }


    public PlayerPosition GetBidder(int i) {
        var number = ((int)CurrentBidder - AuctionHistory.Count - i) % 4;
        if (number < 0) {
            number += 4;
        }

        return (PlayerPosition)number;
    }

    /// <summary>
    /// Returns the last bid made by the specified player, or null if the player has not made any bids.
    /// </summary>
    /// <param name="bidderPosition"></param>
    /// <returns></returns>
    public Bid? GetLastPlayerBid(PlayerPosition bidderPosition, bool passAsNull = false) {
        for (int i = AuctionHistory.Count - 1; i >= 0; i--) {
            if (GetBidder(i) == bidderPosition) {
                var bid = AuctionHistory[i];
                return bid.Type == BidType.Pass ? null : bid;
            }
        }
        return null;
    }

    public Bid? GetLastSubmittedBid(PlayerPosition bidderPosition) {
        for (int i = AuctionHistory.Count - 1; i >= 0; i--) {
            var bidder = GetBidder(i);
            if (bidder == bidderPosition) {
                if (AuctionHistory[i].Type == BidType.Submit) {
                    return AuctionHistory[i];
                }
            }
        }
        return null;
    }

    public Bid? GetLastSubmittedBid(bool onlySubmitions = false) {
        for (int i = AuctionHistory.Count - 1; i >= 0; i--) {
            if (onlySubmitions && AuctionHistory[i].Type == BidType.Submit) {
                return AuctionHistory[i];
            }
            if (!onlySubmitions && AuctionHistory[i].Type != BidType.Pass) {
                return AuctionHistory[i];
            }
        }
        return null;
    }

    public Bid? GetLastSubmittedBid(out PlayerPosition? bidderPosition) {
        bidderPosition = null;
        for (int i = AuctionHistory.Count - 1; i >= 0; i--) {
            if (AuctionHistory[i].Type == BidType.Submit) {
                bidderPosition = GetBidder(i);
                return AuctionHistory[i];
            }
        }
        return null;
    }

    public bool PlayerOpenedAuction(PlayerPosition bidderPosition) {
        for (int i = 0; i < AuctionHistory.Count; i++) {
            if (GetBidder(i) == bidderPosition) {
                return AuctionHistory[i].Type == BidType.Submit;
            }
        }
        return false;
    }

    public Bid? FirstPlayerBid(PlayerPosition bidderPosition) {
        for (int i = 0; i < AuctionHistory.Count; i++) {
            if (GetBidder(i) == bidderPosition) {
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
            var bidPlayer = GetBidder(i);

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


    public IEnumerable<Bid> GetBidSequence(bool includePass = false) {
        foreach (var bid in AuctionHistory) {
            if (includePass && bid.Type == BidType.Pass) {
                yield return bid;
            }
            else if (bid.Type != BidType.Pass) {
                yield return bid;
            }
        }
    }


    public PlayerPosition GetAuctionWinner(PlayerPosition onePlayerFromPlayingPair, BidColor color) {
        for (int i = 0; i < AuctionHistory.Count; ++i) {
            var bidder = GetBidder(i);
            if (bidder == onePlayerFromPlayingPair && AuctionHistory[i].Color == color) {
                return bidder;
            }
            if (bidder.GetPartner() == onePlayerFromPlayingPair && AuctionHistory[i].Color == color) {
                return bidder;
            }
        }

        throw new Exception("Invalid bidder sequence.");
    }


    public Contract GetContract(Player[] players) {
        var lastSubmit = GetLastSubmittedBid(out var bidderPosition);
        var lastBid = GetLastSubmittedBid();

        if (NobodyBidsYet() || lastSubmit == null || lastBid == null || bidderPosition == null) {
            return new() {
                Passed = true
            };
        }

        return new() {
            Value = lastSubmit.Value!.Value,
            Color = lastSubmit.Color,
            IsDoubled = lastBid.Type == BidType.Double,
            IsRedoubled = lastBid.Type == BidType.Redouble,
            Player = GetAuctionWinner(bidderPosition.Value, lastSubmit.Color)
        };
    }


    public bool NobodyBidsYet() => AuctionHistory.All(e => e.Type == BidType.Pass);


    public bool ReachedGameLevel() => GetLastSubmittedBid()?.MakesGame() ?? false;
}
