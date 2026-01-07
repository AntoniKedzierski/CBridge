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


    public Hand(IEnumerable<Card> cards) {
        Cards = [.. cards];
        SortHand();
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


    public override string ToString() {
        return string.Join(" ", Cards.Select(c => c.ToString())) + $"; Points: {CalculatePoints()}; NT Points: {CalculatePoints(true)}.";
    }

}