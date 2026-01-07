using Model.Enums;
using Model.Points;
using System.Diagnostics.CodeAnalysis;

namespace Model; 

public class Card : IPoints, IEqualityComparer<Card> {

    public CardValue Value { get; private set; }

    public CardColor Color { get; private set; }

    public Card(CardValue value, CardColor color) { 
        Value = value;
        Color = color;
    }


    public int CalculatePoints(bool noTrumpGame = false) {
        return Value switch {
            CardValue.Ace => 4,
            CardValue.King => 3,
            CardValue.Queen => 2,
            CardValue.Jack => 1,
            _ => 0,
        };
    }


    public override string ToString() {
        var colorChar = Color switch {
            CardColor.Clubs => "♣",
            CardColor.Diamonds => "♢",
            CardColor.Hearts => "♡",
            CardColor.Spades => "♠"
        };

        var cardValue = Value switch {
            CardValue.Two => "2",
            CardValue.Three => "3",
            CardValue.Four => "4",
            CardValue.Five => "5",
            CardValue.Six => "6",
            CardValue.Seven => "7",
            CardValue.Eight => "8",
            CardValue.Nine => "9",
            CardValue.Ten => "10",
            CardValue.Jack => "J",
            CardValue.Queen => "Q",
            CardValue.King => "K",
            CardValue.Ace => "A",
            _ => throw new NotImplementedException("Invalid card value.")
        };

        return $"{colorChar}{cardValue}";
    }

    
    public bool Equals(Card? x, Card? y) {
        return x != null && y != null && x.Color == y.Color && x.Value == y.Value;
    }


    public int GetHashCode([DisallowNull] Card obj) {
        return 100 * (int)obj.Color + (int)obj.Value;
    }
}
