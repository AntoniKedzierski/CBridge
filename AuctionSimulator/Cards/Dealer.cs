internal sealed class Dealer
{
    public Dealer(int seed)
    {
        Seed = seed;
    }

    public int Seed { get; }

    public Deal Deal()
    {
        var deck = Enum.GetValues<Suit>()
            .SelectMany(s => Enum.GetValues<Rank>().Select(r => new Card(s, r)))
            .ToList();
        var rng = new Random(Seed);

        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        return new Deal
        {
            Hands = new Dictionary<PlayerPosition, Hand>
            {
                [PlayerPosition.North] = new(deck.Take(13)),
                [PlayerPosition.East] = new(deck.Skip(13).Take(13)),
                [PlayerPosition.South] = new(deck.Skip(26).Take(13)),
                [PlayerPosition.West] = new(deck.Skip(39).Take(13))
            }
        };
    }
}
