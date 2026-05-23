internal sealed record PairSummary(Partnership Partnership, int Hcp, int Spades, int Hearts, int Diamonds, int Clubs, bool HasFigureInEverySuit)
{
    public static PairSummary FromDeal(Deal deal, Partnership partnership)
    {
        var players = partnership == Partnership.NS
            ? new[] { PlayerPosition.North, PlayerPosition.South }
            : new[] { PlayerPosition.East, PlayerPosition.West };
        var cards = players.SelectMany(p => deal.Hands[p].Cards).ToList();

        return new PairSummary(
            partnership,
            cards.Sum(c => c.Hcp),
            cards.Count(c => c.Suit == Suit.Spades),
            cards.Count(c => c.Suit == Suit.Hearts),
            cards.Count(c => c.Suit == Suit.Diamonds),
            cards.Count(c => c.Suit == Suit.Clubs),
            Enum.GetValues<Suit>().All(s => cards.Any(c => c.Suit == s && c.IsFigure)));
    }
}

internal readonly record struct PartnershipTarget(Partnership Partnership, Contract Contract, string Reason)
{
    public static PartnershipTarget? Find(Deal deal)
    {
        var candidates = new[] { Partnership.NS, Partnership.EW }
            .Select(p => TryForPair(deal, p))
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .OrderByDescending(t => t.Contract.Level)
            .ThenBy(t => t.Contract.Color)
            .ToList();

        return candidates.Count == 0 ? null : candidates[0];
    }

    public static PartnershipTarget Fallback(Deal deal)
    {
        var ns = PairSummary.FromDeal(deal, Partnership.NS);
        var ew = PairSummary.FromDeal(deal, Partnership.EW);
        var pair = ns.Hcp >= ew.Hcp ? ns : ew;
        return new PartnershipTarget(pair.Partnership, new Contract(1, BidColor.NoTrump), "Brak pełnej końcówki w losowym rozdaniu; wybrano parę z większą liczbą PC.");
    }

    private static PartnershipTarget? TryForPair(Deal deal, Partnership partnership)
    {
        var summary = PairSummary.FromDeal(deal, partnership);
        var slamLevel = summary.Hcp >= 37 ? 7 : summary.Hcp >= 30 ? 6 : 0;

        if (summary.Hcp >= 24 && summary.Spades >= 8)
        {
            return Target(partnership, slamLevel > 0 ? slamLevel : 4, BidColor.Spades, $"{summary.Hcp} PC i {summary.Spades} pików w parze.");
        }

        if (summary.Hcp >= 24 && summary.Hearts >= 8)
        {
            return Target(partnership, slamLevel > 0 ? slamLevel : 4, BidColor.Hearts, $"{summary.Hcp} PC i {summary.Hearts} kierów w parze.");
        }

        if (summary.Hcp >= 25 && summary.Spades < 8 && summary.Hearts < 8 && summary.HasFigureInEverySuit)
        {
            return Target(partnership, slamLevel > 0 ? slamLevel : 3, BidColor.NoTrump, $"{summary.Hcp} PC, brak 8 kart w starszym i figura w każdym kolorze.");
        }

        if (summary.Hcp >= 27 && summary.Diamonds >= 8)
        {
            return Target(partnership, slamLevel > 0 ? slamLevel : 5, BidColor.Diamonds, $"{summary.Hcp} PC i {summary.Diamonds} kar w parze.");
        }

        if (summary.Hcp >= 27 && summary.Clubs >= 8)
        {
            return Target(partnership, slamLevel > 0 ? slamLevel : 5, BidColor.Clubs, $"{summary.Hcp} PC i {summary.Clubs} trefli w parze.");
        }

        return null;
    }

    private static PartnershipTarget Target(Partnership partnership, int level, BidColor color, string reason)
    {
        return new PartnershipTarget(partnership, new Contract(Math.Min(level, 7), color), reason);
    }

    public override string ToString()
    {
        return $"{Partnership}: {Contract} ({Reason})";
    }
}
