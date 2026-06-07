using Model.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.Bids;

public class BidNode : IEquatable<BidNode> {

    [JsonIgnore]
    public Guid Identifier { get; set; } = Guid.NewGuid();
    public int? Value { get; set; }
    public BidColor Color { get; set; }
    public BidType Type { get; set; }
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
    public List<BidNode> NextBids { get; set; } = [];
    public BidNode? Parent { get; set; } 

    [JsonIgnore]
    public string Path { get; set; } = "";


    public bool Matches(Hand hand, PlayerRole role) {
        if(role == PlayerRole.Opener && !OpenerBid) {
            return false;
        }

        if(role != PlayerRole.Opener && OpenerBid) {
            return false;
        }

        return hand.Matches(PointsRange, SpadesCardRange, HeartsCardRange, DiamondsCardRange, ClubsCardRange, Aces, Kings);
    }
    public bool Matches(Bid bid) {
        return Type == bid.BidType
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

    public static BidNode Pass() {
        return new BidNode {
            Type = BidType.Pass,
            Value = null,
            Color = BidColor.NoColor
        };
    }

    public Bid ToBid() {
        return new Bid {
            BidType = Type,
            Color = Color,
            Value = Value
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


    public bool EqualsByColorAndValue(BidNode? other) {
        if (other == null) {
            return false;
        }
        return Color == other.Color && Value == other.Value;
    }


    public override string ToString() {
        return ToBid().ToString();
    }



    //public Contract? ToContract() {
    //    if (Type != BidType.Submit || !Value.HasValue || Value < 1 || Value > 7 || Color == BidColor.NoColor) {
    //        return null;
    //    }

    //    return new Contract(Value.Value, Color);
    //}

    //public static BidNode FromContract(Contract contract, string condition, string path) {
    //    return new BidNode {
    //        Value = contract.Level,
    //        Color = contract.Color,
    //        Type = BidType.Submit,
    //        Condition = condition,
    //        Path = $"{path} > {contract}"
    //    };
    //}
}