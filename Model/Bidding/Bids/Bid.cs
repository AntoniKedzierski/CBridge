using Model.Enums;
using Model.Helpers;

namespace Model.Bidding.Bids;

public class Bid : IEquatable<Bid> {

    public BidType Type { get; set; }

    public BidColor Color { get; set; }

    public int? Value { get; set; }

    public bool IsFromSystem { get; set; }

    public Bid() { }

    public Bid(int value, BidColor color) {
        Value = value;
        Color = color;
    }

    public bool MakesGame() {
        if (Type != BidType.Submit) {
            return false;
        }

        return Color == BidColor.NoTrump && Value >= 3
            || Color == BidColor.Spades && Value >= 4
            || Color == BidColor.Hearts && Value >= 4
            || Color == BidColor.Diamonds && Value >= 5
            || Color == BidColor.Clubs && Value >= 5;
    }



    public bool IsBidLegal(Auction auction) {
        if (Type == BidType.Pass) {
            return true;
        }

        Bid? lastBid = auction.GetLastSubmittedBid();

        if (lastBid == null) {
            return true;    //if there are no previous bids, any bid is legal
        }

        if ((lastBid.Type == BidType.Pass || lastBid.Type == BidType.Submit) && Type == BidType.Double) {
            return true;
        }

        if ((lastBid.Type == BidType.Pass || lastBid.Type == BidType.Double) && Type == BidType.Redouble) {
            return true;
        }

        var lastSubmitBid = auction.GetLastSubmittedBid(true)!;
        if (Value > lastSubmitBid.Value) {
            return true;
        }

        if (Value == lastSubmitBid.Value && (int)Color > (int)lastBid.Color) {
            return true;
        }

        return false;
    }


    public override string ToString() {
        if (Type == BidType.Pass) {
            return "Pass";
        }

        if (Type == BidType.Double) {
            return "X";
        }

        if (Type == BidType.Redouble) {
            return "XX";
        }

        return $"{Value}{Color.ColorMark()}" + (IsFromSystem ? "" : " F");
    }


    public virtual bool Equals(Bid? other) {
        if (other == null) {
            return false;
        }

        return other.Color == Color && other.Type == Type && (other.Value?.Equals(Value) ?? true);
    }

    public static Bid Pass() {
        return new Bid { Type = BidType.Pass };
    }
}

