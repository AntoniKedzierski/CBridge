using Model.Enums;
using Newtonsoft.Json;
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

    [JsonIgnore]
    public string Path { get; set; } = "";

    public bool Matches(Hand hand) {
        return hand.Matches(PointsRange, SpadesCardRange, HeartsCardRange, DiamondsCardRange, ClubsCardRange, Aces, Kings);
    }


    public bool Matches(Bid bid) {
        return Type == bid.Type
            && Color == bid.Color
            && Value == bid.Value;
    }

    public static BidNode Submit(int value, BidColor color) { // Why static? How factory works??
        return new BidNode {
            Type = BidType.Submit,
            Value = value,
            Color = color
        };
    }

    public static BidNode Submit(int value, CardColor color) { // Why static? How factory works??
        var bidColor = color switch {
            CardColor.Spades => BidColor.Spades,
            CardColor.Hearts => BidColor.Hearts,
            CardColor.Diamonds => BidColor.Diamonds,
            CardColor.Clubs => BidColor.Clubs
        };
        return new BidNode {
            Type = BidType.Submit,
            Value = value,
            Color = bidColor
        };
    }

    public static BidNode Pass() {
        return new BidNode {
            Type = BidType.Pass,
            Value = null,
            Color = BidColor.NoColor
        };
    }

    public Bid ToBid(bool isFromSystem = true) {
        return new Bid {
            Type = Type,
            Color = Color,
            Value = Value,
            IsFromSystem = isFromSystem
        };
    }


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