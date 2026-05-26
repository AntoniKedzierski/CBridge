using Model.Bidding;
using Model.Bidding.Bids;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model; 
public class HandEvaluation {
    public NumberRange Points { get; set; }
    public NumberRange Spades { get; set; }
    public NumberRange Hearts { get; set; }
    public NumberRange Diamonds { get; set; }
    public NumberRange Clubs { get; set; }
    public int? Aces { get; set; }
    public int? Kings { get; set; }

    public HandEvaluation() {
        Points = new NumberRange(0, 40);
        Spades = new NumberRange(0, 13);
        Hearts = new NumberRange(0, 13);
        Diamonds = new NumberRange(0, 13);
        Clubs = new NumberRange(0, 13);
    }

    // TODO: getting information from stops and exact distributions in bid
    public void Evaluate(BidNode bidNode) {
        Points.Narrow(bidNode.PointsRange);

        Spades.Narrow(bidNode.SpadesCardRange);
        Hearts.Narrow(bidNode.HeartsCardRange);
        Diamonds.Narrow(bidNode.DiamondsCardRange);
        Clubs.Narrow(bidNode.ClubsCardRange);

        Aces = bidNode.Aces ?? Aces;
        Kings = bidNode.Kings ?? Kings;
    }

    public void Evaluate(Hand hand) {
        Points.Upper -= hand.PointsNt;
        Spades.Upper -= hand.OfColor(CardColor.Spades).Count();
        Hearts.Upper -= hand.OfColor(CardColor.Hearts).Count();
        Diamonds.Upper -= hand.OfColor(CardColor.Diamonds).Count();
        Clubs.Upper -= hand.OfColor(CardColor.Clubs).Count();
    }

    public float Precision() {
        return 1f 
            - 0.15f * (Points.Upper.Value - Points.Lower.Value) / 40f
            + 0.15f * (Spades.Upper.Value - Spades.Lower.Value) / 13f
            + 0.15f * (Hearts.Upper.Value - Hearts.Lower.Value) / 13f
            + 0.15f * (Diamonds.Upper.Value - Diamonds.Lower.Value) / 13f
            + 0.15f * (Clubs.Upper.Value - Clubs.Lower.Value) / 13f
            + 0.125f * (Aces.HasValue ? 1 : 0)
            + 0.125f * (Kings.HasValue ? 1 : 0);
    }
}
