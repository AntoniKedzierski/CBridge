using Model.Bidding;
using Model.Enums;
using Model.Helpers;
using Model.Points;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Model;

public class Hand : IPoints {

    public List<Card> Cards { get; private set; }
    public int Points {  get; private set; }
    public int PointsNt { get; private set; }

    public int SpadesCount { get; private set; }
    public int HeartsCount { get; private set; }
    public int DiamondsCount { get; private set; }
    public int ClubsCount { get; private set; }


    public Hand(IEnumerable<Card> cards) {
        Cards = [.. cards];
        SortHand();
        Points = CalculatePoints(false);
        PointsNt = CalculatePoints(true);
        SpadesCount = OfColor(CardColor.Spades).Count();
        HeartsCount = OfColor(CardColor.Hearts).Count();
        DiamondsCount = OfColor(CardColor.Diamonds).Count();
        ClubsCount = OfColor(CardColor.Clubs).Count();
    }


    public IEnumerable<Card> OfColor(CardColor color) => Cards.Where(c => c.Color == color);


    public IEnumerable<Card> OfValue(CardValue value) => Cards.Where(c => c.Value == value);

    public IEnumerable<CardColor> Colors => Cards.Select(e => e.Color).Distinct().ToList();


    public void SortHand() {
        Cards = Cards
            .OrderByDescending(e => e.Color)
            .ThenByDescending(e => e.Value)
            .ToList();
    }

    // TODO: niedoliczanie punktów za figury w krótkościach, jak już doliczamy za te krótkości
    public int CalculatePoints(bool noTrumpGame = false) {
        var totalPoints = 0;
        foreach (var color in Enum.GetValues(typeof(CardColor)).Cast<CardColor>()) {
            var suit = OfColor(color).ToList();
            var suitLengthColor = 0;
            if(suit.Count == 2) {
                // Drugi walet
                if (suit.Any(e => e.Value == CardValue.Jack)) {
                    suitLengthColor -= 1;
                }
                // Druga dama
                else if(suit.Any(e => e.Value == CardValue.Queen)){
                    suitLengthColor -= 1;
                }

                //inne
                suitLengthColor += Math.Max(0, 3 - suit.Count);
            }
            else if(suit.Count == 1) {
                // Walet singiel
                if (suit.Any(e => e.Value == CardValue.Jack)) {
                    suitLengthColor -= 1;
                }
                // Singlowa dama
                else if (suit.Any(e => e.Value == CardValue.Queen)) {
                    suitLengthColor -= 2;
                }
                // Król singiel
                else if (suit.Any(e => e.Value == CardValue.King)) {
                    suitLengthColor -= 2;
                }

                //inne
                suitLengthColor += Math.Max(0, 3 - suit.Count);

            }
            else {
                suitLengthColor = Math.Max(0, 3 - suit.Count);
            }

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


    public double StopCount(CardColor ofColor) {
        var cards = OfColor(ofColor).ToList();
        if (cards.Count == 0) {
            return 0;
        }

        var stops = 0.0;
        if (cards.Any(e => e.Value == CardValue.Ace)) {
            stops++;
        }

        if (cards.Any(e => e.Value == CardValue.King)) {
            stops += cards.Count == 1 ? 0.5 : 1;
        }

        if (cards.Any(e => e.Value == CardValue.Queen) && cards.Count >= 2) {
            stops += cards.Count == 2 ? 0.5 : 1;
        }

        return stops;
    }


    public int CountCards(CardColor ofColor) => OfColor(ofColor).Count();


    public int PointsCount(CardColor ofColor) => OfColor(ofColor).Sum(e => e.CalculatePoints());


    public bool HasEvenDistribution() {
        var colorCounts = Cards.GroupBy(e => e.Color).ToDictionary(e => e.Key, e => e.Count());
        var minCount = colorCounts.Values.Min();
        var maxCount = colorCounts.Values.Max();

        return maxCount <= 5 && minCount >= 2;
    }


    public CardColor GetLongestColor() {
        var colorCounts = Cards.GroupBy(e => e.Color).ToDictionary(e => e.Key, e => e.Count());
        return colorCounts.OrderByDescending(e => e.Value).ThenByDescending(e => (int)e.Key).First().Key;
    }


    public List<CardColor> GetStrongestColors() {
        var colorCounts = Cards.GroupBy(e => e.Color).ToDictionary(e => e.Key, e => e.Count());
        var stopCounts = Colors.ToDictionary(e => e, StopCount);
        var pointsCount = Colors.ToDictionary(e => e, PointsCount);

        var keys = colorCounts.Keys.ToList();
        var result = new List<CardColor>();

        do {
            var strongestColor = GetStrongestColor(colorCounts, stopCounts, pointsCount);
            result.Add(strongestColor);

            // Usuwamy najmocniejszy kolor.
            colorCounts.Remove(strongestColor);
            stopCounts.Remove(strongestColor);
            pointsCount.Remove(strongestColor);
            keys.Remove(strongestColor);
        } while (keys.Count > 0);

        return result;
    }


    public bool HasMajorFour(out CardColor? color) {
        if (SpadesCount == 4) {
            color = CardColor.Spades;
            return true;
        }

        if (HeartsCount == 4) {
            color = CardColor.Hearts;
            return true;
        }

        color = null;
        return false;
    }


    public bool Fits(BidColor color) => CountCards(color.ToCardColor()) >= 3;


    public static CardColor GetStrongestColor(Dictionary<CardColor, int> colorCounts, Dictionary<CardColor, double> stopCounts, Dictionary<CardColor, int> pointsCount) {
        if (colorCounts.Count == 1) {
            return colorCounts.First().Key;
        }

        var maxCount = colorCounts.Max(e => e.Value);
        var maxStops = stopCounts.Max(e => e.Value);
        var totalPoinst = pointsCount.Values.Sum();
        var colors = colorCounts.Keys;

        // Zwracamy najdłuższy (najstarszy w przypadku remisów).
        if (totalPoinst == 0) {
            foreach (var color in colors.OrderByDescending(e => (int)e)) {
                if (colorCounts[color] == maxCount) {
                    return color;
                }
            }
        }

        // Szukamy najdłuższego koloru z największą liczbą zatrzymań.
        foreach (var color in colors) {
            if (colorCounts[color] == maxCount && stopCounts[color] == maxStops) {
                return color;
            }
        }

        // 4441, 4432, 4333
        if (maxCount == 4) {
            if (colorCounts.Count(e => e.Value == 4) == 1) {
                var fourCardColor = colorCounts.FirstOrDefault(e => e.Value == 4).Key;

                // Gdy nie ma zera punktów
                if (pointsCount[fourCardColor] != 0) {
                    return fourCardColor;
                }

                // wpp, kolor z najlepszymi punktami
                return pointsCount.OrderByDescending(e => e.Value).First().Key;
            }

            // Wybieramy tylko z tych czwórek, kolor z najwyższymi punktami, jeżeli remisy, to starszy.
            return colorCounts
                .Where(e => e.Value == 4)
                .OrderByDescending(e => pointsCount[e.Key])
                .ThenByDescending(e => (int)e.Key)
                .First()
                .Key;
        }
        // 5530, 5521, 5431, 5422, 5440, 5332
        else if (maxCount == 5) {
            // 5332 - zawsze piątka
            if (colorCounts.Count(e => e.Value == 5) == 1 && !colorCounts.Any(e => e.Value == 4)) {
                return colorCounts.First(e => e.Value == 5).Key;
            }

            return colorCounts
                .Where(e => e.Value >= 4)
                .OrderByDescending(e => pointsCount[e.Key])
                .ThenByDescending(e => (int)e.Key)
                .First()
                .Key;
        }
        // 6610, 6520, 6511, 6430, 6421, 6331, 6322
        else if (maxCount == 6) {
            // 6430, 6421, 6331, 6322 - zawsze szóstka
            if (colorCounts.Any(e => e.Value == 4 || e.Value == 3)) {
                return colorCounts.First(e => e.Value == 6).Key;
            }

            return colorCounts
                .Where(e => e.Value >= 5)
                .OrderByDescending(e => pointsCount[e.Key])
                .ThenByDescending(e => (int)e.Key)
                .First()
                .Key;
        }
        // 7600, 7510, 7420, 7411, 7330, 7321
        else if (maxCount == 7) {
            // 7420, 7411, 7330, 7321 - zawsze siódemka
            if (colorCounts.Any(e => e.Value == 4 || e.Value == 3)) {
                return colorCounts.First(e => e.Value == 7).Key;
            }

            return colorCounts
                .Where(e => e.Value >= 5)
                .OrderByDescending(e => pointsCount[e.Key])
                .ThenByDescending(e => (int)e.Key)
                .First()
                .Key;
        }
        // Jeżeli jakiś kolor ma 8 kart (xd)
        else if (maxCount >= 8) {
            // Jeżeli istnieje inny 5 kartowy kolor, to musi mieć 3 stopy.
            if (colorCounts.Any(e => e.Value == 5)) {
                var fiveCardColor = colorCounts.FirstOrDefault(e => e.Value == 5).Key;
                if (stopCounts[fiveCardColor] == 3) {
                    return fiveCardColor;
                }
            }

            // Jeżeli nie, to zwracamy ten 8+ kartowy
            return colorCounts.FirstOrDefault(e => e.Value >= 8).Key;
        }

        // Fallback - najdłuższy kolor
        return colorCounts
            .OrderByDescending(e => e.Value)
            .ThenBy(e => pointsCount[e.Key])
            .ThenByDescending(e => (int)e.Key)
            .First()
            .Key;
    }


    public Card? GetStrongestCardInColor(CardColor color) {
        return Cards.Where(card => card.Color == color).MaxBy(card => card.Value);
    }
    public Card? GetWeakestCardInColor(CardColor color) {
        return Cards.Where(card => card.Color == color).MinBy(card => card.Value);
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