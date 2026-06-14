using Model.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Helpers; 

public static class BiddingHelper {

    public static PlayerPosition GetPartner(this PlayerPosition player) {
        return (PlayerPosition)(((int)player + 2) % 4);
    }
    
    public static string ColorMark(this CardColor color) {
        return color switch {
            CardColor.Clubs => "♣",
            CardColor.Diamonds => "♢",
            CardColor.Hearts => "♡",
            CardColor.Spades => "♠",
            _ => ""
        };
    }


    public static string ColorMark(this BidColor color) {
        return color switch {
            BidColor.Clubs => "♣",
            BidColor.Diamonds => "♢",
            BidColor.Hearts => "♡",
            BidColor.Spades => "♠",
            BidColor.NoTrump => "NT",
            _ => ""
        };
    }


    public static BidColor ToBidColor(this CardColor color) {
        return color switch {
            CardColor.Clubs => BidColor.Clubs,
            CardColor.Diamonds => BidColor.Diamonds,
            CardColor.Hearts => BidColor.Hearts,
            CardColor.Spades => BidColor.Spades,
            _ => throw new Exception("Invalid color")
        };
    }


    public static Hand GetPlayerHand(this Player[] players, PlayerPosition playerPosition) {
        return players[(int)playerPosition].Hand;
    }
}