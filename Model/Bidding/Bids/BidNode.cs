using Model.Bidding.AI.Engine;
using Model.Enums;
using Model.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.Bids;

public class BidNode : Bid, IEquatable<BidNode>, IEqualityComparer<BidNode>, IComparable<BidNode> {

    [JsonIgnore]
    public Guid Identifier { get; set; } = Guid.NewGuid();
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public string? Convention { get; set; }
    public NumberRange? PointsRange { get; set; }
    public NumberRange? SpadesCardRange { get; set; }
    public NumberRange? HeartsCardRange { get; set; }
    public NumberRange? DiamondsCardRange { get; set; }
    public NumberRange? ClubsCardRange { get; set; }
    public int? Aces { get; set; }
    public int? Kings { get; set; }
    public bool OpenerBid { get; set; }
    public bool SignOff { get; set; }
    public bool OneRoundForcing { get; set; }
    public bool GameForcing { get; set; }
    public bool AutomaticResponse { get; set; }
    public bool GoToOpenings { get; set; }
    public List<BidNode> NextBids { get; set; } = [];
    public BidNode? Parent { get; set; } 

    public BiddingGoal RealizedGoal { get; set; }

    public string? AiSource { get; set; }

    [JsonIgnore]
    public string Path { get; set; } = "";


    public BidNode() : base() { }


    private BidNode(NumberRange pointsRange, NumberRange spadesRange, NumberRange heartsRange, NumberRange diamondsRange, NumberRange clubsRange) : base() {
        PointsRange = pointsRange;
        SpadesCardRange = spadesRange;
        HeartsCardRange = heartsRange;
        DiamondsCardRange = diamondsRange;
        ClubsCardRange = clubsRange;
        IsFromSystem = false;
    }


    public BidNode(BidType type, NumberRange pointsRange, NumberRange spadesRange, NumberRange heartsRange, NumberRange diamondsRange, NumberRange clubsRange)
        : this(pointsRange, spadesRange, heartsRange, diamondsRange, clubsRange) {
        Type = type;
    }


    public BidNode(int value, BidColor color, NumberRange pointsRange, NumberRange spadesRange, NumberRange heartsRange, NumberRange diamondsRange, NumberRange clubsRange)
        : this(BidType.Submit, pointsRange, spadesRange, heartsRange, diamondsRange, clubsRange) {
        Value = value;
        Color = color;
    }


    public bool Matches(Hand hand) {
        return hand.Matches(PointsRange, SpadesCardRange, HeartsCardRange, DiamondsCardRange, ClubsCardRange, Aces, Kings);
    }


    public bool Matches(Bid bid) => Type == bid.Type && Color == bid.Color && Value == bid.Value;

    public static BidNode Submit(int value, BidColor color) => new() {
        Type = BidType.Submit,
        Value = value,
        Color = color
    };


    public static BidNode SubmitOrPass(Auction auction, int value, BidColor color) {
        var lowestValue = auction.GetLowestLegalValue(color);
        if (value > lowestValue) {
            return Pass();
        }
        return Submit(value, color);
    }


    public static BidNode SubmitLowest(Auction auction, BidColor color, int? limit = null) {
        var lowestValue = auction.GetLowestLegalValue(color);
        if (limit != null && lowestValue > limit) {
            return Pass();
        }
        return Submit(lowestValue, color);
    }


    public static BidNode SubmitWithRaise(Auction auction, BidColor color) {
        var lowestValue = auction.GetLowestLegalValue(color);
        return Submit(lowestValue + 1, color);
    }


    public static BidNode Submit(int value, CardColor color) => new() {
        Type = BidType.Submit,
        Value = value,
        Color = color.ToBidColor()
    };


    public static BidNode SubmitGame(BidColor color) => color switch {
        BidColor.NoTrump => Submit(3, BidColor.NoTrump),
        BidColor.Spades => Submit(4, BidColor.Spades),
        BidColor.Hearts => Submit(4, BidColor.Hearts),
        BidColor.Diamonds => Submit(5, BidColor.Diamonds),
        BidColor.Clubs => Submit(5, BidColor.Clubs),
        _ => throw new Exception("Invalid color.")
    };


    public static BidNode SubmitLowestLegalGameOrDouble(Auction auction, BidColor color) {
        var lowestValue = auction.GetLowestLegalValue(color);
        if (lowestValue >= 5) {
            return Double();
        }

        return color switch {
            BidColor.NoTrump => Submit(Math.Max(3, lowestValue), BidColor.NoTrump),
            BidColor.Spades => Submit(Math.Max(4, lowestValue), BidColor.Spades),
            BidColor.Hearts => Submit(Math.Min(4, lowestValue), BidColor.Hearts),
            BidColor.Diamonds => Submit(Math.Min(5, lowestValue), BidColor.Diamonds),
            BidColor.Clubs => Submit(Math.Max(5, lowestValue), BidColor.Clubs),
            _ => throw new Exception("Invalid color.")
        };
    }


    public static new BidNode Pass() => new() {
        Type = BidType.Pass,
        Value = null,
        Color = BidColor.NoColor
    };


    public static BidNode Double() => new() {
        Type = BidType.Double,
        Value = null,
        Color = BidColor.NoColor
    };


    public static BidNode Redouble() => new() {
        Type = BidType.Redouble,
        Value = null,
        Color = BidColor.NoColor
    };


    public Bid ToBid(bool isFromSystem = true) => new() {
        Type = Type,
        Color = Color,
        Value = Value,
        IsFromSystem = isFromSystem,
        StackTrace = StackTrace
    };


    public void AssignParent(BidNode? parent) {
        Parent = parent;
        foreach (var child in NextBids) {
            child.AssignParent(this);
        }
    }


    public bool Equals(BidNode? other) {
        if (other == null) {
            return false;
        }

        return Identifier.Equals(other.Identifier);
    }


    public bool EqualsByColorAndValue(Bid? other) {
        if (other == null) {
            return false;
        }

        if (Type == BidType.Pass && other.Type == Type) {
            return true;
        }

        return Color == other.Color && Value == other.Value;
    }


    public bool EqualsByColorAndValue(int? value, BidColor color) {
        return Color == color && Value == value;
    }

    /// <summary>
    /// Sprawdzenie, czy nowa odzywka z freestylu nie równa się niczemu wśród rodzeństwa.
    /// </summary>
    /// <param name="branchHead"></param>
    /// <returns></returns>
    public BidNode? AssertFreestyleIsntConfusing(BidNode branchHead) {
        foreach (var node in branchHead.NextBids) {
            if (EqualsByColorAndValue(node)) {
                return null;
            }
        }

        return this;
    }


    public bool IsGameForcing() {
        if (GameForcing) {
            return true;
        }

        var parent = Parent;
        while (parent != null) {
            if (parent.GameForcing) {
                return true;
            }
            parent = parent.Parent;
        }

        return false;
    }


    public bool Equals(BidNode? x, BidNode? y) {
        return x?.Equals(y) ?? true;
    }


    public int GetHashCode([DisallowNull] BidNode obj) {
        return obj.Identifier.GetHashCode();
    }


    public int CompareTo(BidNode? other) {
        if (other == null) {
            return 1;
        }

        // Najpierw porównujemy Value (poziom odzywki: 1-7)
        int valueComparison = Nullable.Compare(Value, other.Value);
        if (valueComparison != 0) {
            return valueComparison;
        }

        // Jeśli Value są równe, porównujemy Color
        // Porządek: ♣ < ♦ < ♥ < ♠ < NoTrump
        return GetColorOrder(Color).CompareTo(GetColorOrder(other.Color));
    }

    private static int GetColorOrder(BidColor color) {
        return color switch {
            BidColor.Clubs => 0,
            BidColor.Diamonds => 1,
            BidColor.Hearts => 2,
            BidColor.Spades => 3,
            _ => 4 // NoColor/NoTrump
        };
    }
}