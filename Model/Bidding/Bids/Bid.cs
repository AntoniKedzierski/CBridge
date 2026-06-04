using Model.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.Bids;

public class Bid {
    public BidType BidType { get; set; }
    public BidColor Color { get; set; }
    public int? Value { get; set; }


    public override string ToString() {
        
        if (BidType == BidType.Pass) {
            return "Pass";
        }

        var colorChar = Color switch {
            BidColor.Clubs => "♣",
            BidColor.Diamonds => "♢",
            BidColor.Hearts => "♡",
            BidColor.Spades => "♠",
            BidColor.NoTrump => "NT"
        };

        return $"{Value}{Color}";
    }

}

