using Model.Bidding;
using Model.Bidding.Bids;
using Model.Enums;
using Model.Helpers;

namespace Bridgemate.Models;

public class AuctionEntry {
    public PlayerPosition Position { get; set; }
    public required Bid Bid { get; init; }
    public BidNode? Node { get; set; }

    public string DisplayText => Bid.Type switch {
        BidType.Pass => "Pass",
        BidType.Double => "X",
        BidType.Redouble => "XX",
        BidType.Submit => $"{Bid.Value}{Bid.Color.ColorMark()}",
        _ => "?"
    };
}
