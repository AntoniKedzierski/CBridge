using Model.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.Bids;

public class Bid {

    public BidType Type { get; set; }

    public BidColor Color { get; set; }

    public int? Value { get; set; }

    public bool IsFromSystem { get; set; }

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

    public override string ToString() {
        if (Type == BidType.Pass) {
            return "Pass";
        }

        var colorChar = Color switch {
            BidColor.Clubs => "♣",
            BidColor.Diamonds => "♢",
            BidColor.Hearts => "♡",
            BidColor.Spades => "♠",
            BidColor.NoTrump => "NT"
        };

        return $"{Value}{Color}" + (IsFromSystem ? " S" : " F");
    }

}

