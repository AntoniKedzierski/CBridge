using Model.Bidding;
using Model.Bidding.Bids;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.AI.Eval; 

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

    public HandEvaluation(Hand hand) {
        Points = new NumberRange(0, 40);
        Spades = new NumberRange(0, 13);
        Hearts = new NumberRange(0, 13);
        Diamonds = new NumberRange(0, 13);
        Clubs = new NumberRange(0, 13);
        Evaluate(hand);
    }


    public static HandEvaluation OnOwnHand(Hand hand) {
        return new() {
            Points = new NumberRange(hand.Points, hand.Points),
            Spades = new NumberRange(hand.SpadesCount, hand.SpadesCount),
            Hearts = new NumberRange(hand.HeartsCount, hand.HeartsCount),
            Diamonds = new NumberRange(hand.DiamondsCount, hand.DiamondsCount),
            Clubs = new NumberRange(hand.ClubsCount, hand.ClubsCount),
            Aces = hand.OfValue(CardValue.Ace).Count(),
            Kings = hand.OfValue(CardValue.King).Count()
        };
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


    public void Evaluate(HandEvaluation otherHandEvaluation) {
        Points.Narrow(new(null, 40 - otherHandEvaluation.Points.Lower));
        Spades.Narrow(new(null, 13 - otherHandEvaluation.Spades.Lower));
        Hearts.Narrow(new(null, 13 - otherHandEvaluation.Hearts.Lower));
        Diamonds.Narrow(new(null, 13 - otherHandEvaluation.Diamonds.Lower));
        Clubs.Narrow(new(null, 13 -otherHandEvaluation.Clubs.Lower));

        if (otherHandEvaluation.Aces != null) {
            Aces = 4 - otherHandEvaluation.Aces;
        }

        if (otherHandEvaluation.Kings != null) {
            Kings = 4 - otherHandEvaluation.Kings;
        }
    }


    public void Evaluate(Hand hand) {
        Points.Upper -= hand.PointsNt;
        Spades.Upper -= hand.OfColor(CardColor.Spades).Count();
        Hearts.Upper -= hand.OfColor(CardColor.Hearts).Count();
        Diamonds.Upper -= hand.OfColor(CardColor.Diamonds).Count();
        Clubs.Upper -= hand.OfColor(CardColor.Clubs).Count();
    }

    /// <summary>
    /// Ma zastosowanie tylko dla ewaluacji siły połączonych rąk.
    /// </summary>
    /// <returns></returns>
    public SuitFit FindFit() {
        SuitFit bestFit = new() {
            Color = BidColor.NoColor,
            Length = 0
        };

        foreach (CardColor color in Enum.GetValues(typeof(CardColor))) {
            var suitColor = GetSuit(color);
            if (suitColor >= bestFit.Length) { // Major colors are preferred
                bestFit.Color = (BidColor)((int)color + 1); // CardColor doesnt have NoColor, while BidColor does
                bestFit.Length = suitColor.Lower ?? 0;
            }
        }
        return bestFit;
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

    public NumberRange GetSuit(CardColor color) {
        return color switch {
            CardColor.Spades => Spades,
            CardColor.Hearts => Hearts,
            CardColor.Diamonds => Diamonds,
            CardColor.Clubs => Clubs,
            _ => throw new InvalidOperationException()
        };
    }


    public override string ToString() {
        return $"P: {Points}; S: {Spades}; H: {Hearts}; D: {Diamonds}; C: {Clubs}; A: {(Aces.HasValue ? Aces.ToString() : "unknown")}; K: {(Kings.HasValue ? Kings.ToString() : "unknown")}";
    }

} 