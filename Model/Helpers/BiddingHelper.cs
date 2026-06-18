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


    public static PlayerPosition GetLeftOpponent(this PlayerPosition player) {
        return (PlayerPosition)(((int)player + 1) % 4);
    }


    public static PlayerPosition GetRightOpponent(this PlayerPosition player) {
        return (PlayerPosition)(((int)player + 3) % 4);
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


    public static CardColor ToCardColor(this BidColor color) {
        return color switch {
            BidColor.Clubs => CardColor.Clubs,
            BidColor.Diamonds => CardColor.Diamonds,
            BidColor.Hearts => CardColor.Hearts,
            BidColor.Spades => CardColor.Spades,
            _ => throw new Exception("Invalid color")
        };
    }


    public static bool IsMajor(this CardColor color) {
        return color == CardColor.Spades || color == CardColor.Hearts;
    }


    public static bool IsMajor(this BidColor color) {
        return color == BidColor.Spades || color == BidColor.Hearts;
    }


    public static Hand GetPlayerHand(this Player[] players, PlayerPosition playerPosition) {
        return players[(int)playerPosition].Hand;
    }


    public static int SmallSlamPointsRequirement(this BidColor color) {
        if (color == BidColor.NoTrump) {
            return 32;
        }
        return 30;
    }


    public static int GamePointsRequirement(this BidColor color) {
        if (color == BidColor.NoTrump) {
            return 25;
        }
        return color.IsMajor() ? 24 : 27;
    }


    public static bool IsColorGame(this BidColor color) => color != BidColor.NoTrump && color != BidColor.NoColor;

    public static bool IsNoTrumpGame(this BidColor color) => color == BidColor.NoTrump;
}