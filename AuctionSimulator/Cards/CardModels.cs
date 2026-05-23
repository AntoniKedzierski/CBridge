internal enum PlayerPosition { North, East, South, West }
internal enum Partnership { None, NS, EW }
internal enum Suit { Clubs, Diamonds, Hearts, Spades }
internal enum Rank { Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

internal readonly record struct Card(Suit Suit, Rank Rank)
{
    public int Hcp => Rank switch
    {
        Rank.Ace => 4,
        Rank.King => 3,
        Rank.Queen => 2,
        Rank.Jack => 1,
        _ => 0
    };

    public bool IsFigure => Rank is Rank.Ace or Rank.King or Rank.Queen or Rank.Jack;

    public string Code() => $"{Suit.Code()}{Rank.Symbol()}";

    public static Card Parse(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length < 2)
        {
            throw new FormatException($"Niepoprawny kod karty: {code}");
        }

        var suit = code[0] switch
        {
            'S' => Suit.Spades,
            'H' => Suit.Hearts,
            'D' => Suit.Diamonds,
            'C' => Suit.Clubs,
            _ => throw new FormatException($"Niepoprawny kolor karty: {code}")
        };

        var rank = code[1..] switch
        {
            "2" => Rank.Two,
            "3" => Rank.Three,
            "4" => Rank.Four,
            "5" => Rank.Five,
            "6" => Rank.Six,
            "7" => Rank.Seven,
            "8" => Rank.Eight,
            "9" => Rank.Nine,
            "10" => Rank.Ten,
            "J" => Rank.Jack,
            "Q" => Rank.Queen,
            "K" => Rank.King,
            "A" => Rank.Ace,
            _ => throw new FormatException($"Niepoprawna figura karty: {code}")
        };

        return new Card(suit, rank);
    }

    public override string ToString() => $"{Suit.Symbol()}{Rank.Symbol()}";
}

internal sealed class Hand
{
    public Hand(IEnumerable<Card> cards)
    {
        Cards = cards.OrderByDescending(c => c.Suit).ThenByDescending(c => c.Rank).ToArray();
    }

    public IReadOnlyList<Card> Cards { get; }
    public int Hcp => Cards.Sum(c => c.Hcp);
    public int Aces => Cards.Count(c => c.Rank == Rank.Ace);
    public int Kings => Cards.Count(c => c.Rank == Rank.King);

    public int Count(Suit suit) => Cards.Count(c => c.Suit == suit);

    public bool Matches(NumberRange? points, NumberRange? spades, NumberRange? hearts, NumberRange? diamonds, NumberRange? clubs, int? aces, int? kings)
    {
        return InRange(Hcp, points)
            && InRange(Count(Suit.Spades), spades)
            && InRange(Count(Suit.Hearts), hearts)
            && InRange(Count(Suit.Diamonds), diamonds)
            && InRange(Count(Suit.Clubs), clubs)
            && (!aces.HasValue || Aces == aces.Value)
            && (!kings.HasValue || Kings == kings.Value);
    }

    public string RenderSuit(Suit suit)
    {
        var ranks = Cards.Where(c => c.Suit == suit).Select(c => c.Rank.Symbol()).ToList();
        return ranks.Count == 0 ? "-" : string.Join(" ", ranks);
    }

    public string RenderSuitForConsole(Suit suit)
    {
        var ranks = Cards.Where(c => c.Suit == suit).Select(c => c.Rank.ConsoleSymbol()).ToList();
        return ranks.Count == 0 ? "-" : string.Join(" ", ranks);
    }

    private static bool InRange(int value, NumberRange? range)
    {
        if (range is null)
        {
            return true;
        }

        return (!range.Lower.HasValue || value >= range.Lower.Value)
            && (!range.Upper.HasValue || value <= range.Upper.Value);
    }
}

internal sealed class Deal
{
    public required Dictionary<PlayerPosition, Hand> Hands { get; init; }
}
