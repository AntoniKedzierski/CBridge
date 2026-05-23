internal static class BridgeExtensions
{
    public static Partnership Partnership(this PlayerPosition player)
    {
        return player is PlayerPosition.North or PlayerPosition.South
            ? global::Partnership.NS
            : global::Partnership.EW;
    }

    public static PlayerPosition Partner(this PlayerPosition player)
    {
        return player switch
        {
            PlayerPosition.North => PlayerPosition.South,
            PlayerPosition.East => PlayerPosition.West,
            PlayerPosition.South => PlayerPosition.North,
            _ => PlayerPosition.East
        };
    }

    public static string ShortName(this PlayerPosition player)
    {
        return player switch
        {
            PlayerPosition.North => "N",
            PlayerPosition.East => "E",
            PlayerPosition.South => "S",
            PlayerPosition.West => "W",
            _ => player.ToString()
        };
    }

    public static string Symbol(this Suit suit)
    {
        return suit switch
        {
            Suit.Clubs => "♣",
            Suit.Diamonds => "♦",
            Suit.Hearts => "♥",
            Suit.Spades => "♠",
            _ => ""
        };
    }

    public static string Code(this Suit suit)
    {
        return suit switch
        {
            Suit.Clubs => "C",
            Suit.Diamonds => "D",
            Suit.Hearts => "H",
            Suit.Spades => "S",
            _ => ""
        };
    }

    public static string Symbol(this Rank rank)
    {
        return rank switch
        {
            Rank.Two => "2",
            Rank.Three => "3",
            Rank.Four => "4",
            Rank.Five => "5",
            Rank.Six => "6",
            Rank.Seven => "7",
            Rank.Eight => "8",
            Rank.Nine => "9",
            Rank.Ten => "10",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            Rank.Ace => "A",
            _ => ""
        };
    }

    public static string ConsoleSymbol(this Rank rank)
    {
        return rank == Rank.Queen ? "D" : rank.Symbol();
    }

    public static string Symbol(this BidColor color)
    {
        return color switch
        {
            BidColor.Clubs => "♣",
            BidColor.Diamonds => "♦",
            BidColor.Hearts => "♥",
            BidColor.Spades => "♠",
            BidColor.NoTrump => "BA",
            _ => ""
        };
    }

    public static int BidRank(this BidColor color)
    {
        return color switch
        {
            BidColor.Clubs => 1,
            BidColor.Diamonds => 2,
            BidColor.Hearts => 3,
            BidColor.Spades => 4,
            BidColor.NoTrump => 5,
            _ => 0
        };
    }
}
