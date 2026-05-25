using Model.Bidding;
using Model.Enums;
using Model.Points;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model;

public class Hand : IPoints {

    public Card[] Cards { get; private set; }
    public int Points {  get; private set; }
    public int PointsNt { get; private set; }


    public Hand(IEnumerable<Card> cards) {
        Cards = [.. cards];
        SortHand();
        Points = CalculatePoints(false);
        PointsNt = CalculatePoints(true);
    }


    public IEnumerable<Card> OfColor(CardColor color) => Cards.Where(c => c.Color == color);


    public IEnumerable<Card> OfValue(CardValue value) => Cards.Where(c => c.Value == value);


    public void SortHand() {
        Cards = Cards.OrderByDescending(e => e.Color).ThenByDescending(e => e.Value).ToArray();
    }


    public int CalculatePoints(bool noTrumpGame = false) {
        var totalPoints = 0;
        foreach (var color in Enum.GetValues(typeof(CardColor)).Cast<CardColor>()) {
            var suit = OfColor(color).ToList();
            var suitLengthColor = Math.Max(0, 3 - suit.Count);
            totalPoints += suit.Sum(e => e.CalculatePoints(noTrumpGame));
            totalPoints += noTrumpGame ? 0 : suitLengthColor;
        }

        return totalPoints;
    }

    // TODO: Zatrzymania, układ kart, itp.
    public bool Matches(NumberRange? points, NumberRange? spades, NumberRange? hearts, NumberRange? diamonds, NumberRange? clubs, int? aces, int? kings) {
        return InRange(Points, points)
            && InRange(OfColor(CardColor.Spades).Count(), spades)
            && InRange(OfColor(CardColor.Hearts).Count(), hearts)
            && InRange(OfColor(CardColor.Diamonds).Count(), diamonds)
            && InRange(OfColor(CardColor.Clubs).Count(), clubs)
            && (!aces.HasValue || OfValue(CardValue.Ace).Count() == aces.Value)
            && (!kings.HasValue || OfValue(CardValue.King).Count() == kings.Value);
    }

    private static bool InRange(int value, NumberRange? range) {
        if (range is null) {
            return true;
        }

        return (!range.Lower.HasValue || value >= range.Lower.Value)
            && (!range.Upper.HasValue || value <= range.Upper.Value);
    }


    public override string ToString() {
        return string.Join(" ", Cards.Select(c => c.ToString())) + $"; Points: {CalculatePoints()}; NT Points: {CalculatePoints(true)}.";
    }

}