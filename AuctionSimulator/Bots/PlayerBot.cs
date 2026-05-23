internal sealed class PlayerBot
{
    public PlayerBot(PlayerPosition position, Hand hand)
    {
        Position = position;
        Hand = hand;
    }

    public PlayerPosition Position { get; }
    public Hand Hand { get; }
    public Partnership Partnership => Position.Partnership();

    public BidNode? ChooseBid(
        IReadOnlyList<BidNode> options,
        Contract? lastContract,
        bool cannotPass,
        PartnershipTarget? target)
    {
        var matching = options
            .Where(b => b.Type == BidType.Submit)
            .Where(b => b.ToContract() is { } c && (lastContract is null || c.CompareTo(lastContract.Value) > 0))
            .Where(b => b.AutomaticResponse || b.Matches(Hand))
            .OrderByDescending(b => b.AutomaticResponse)
            .ThenByDescending(b => ScoresTowardTarget(b, target))
            .ThenBy(b => b.ToContract())
            .ToList();

        if (matching.Count > 0)
        {
            return matching[0];
        }

        if (!cannotPass)
        {
            return null;
        }

        return options
            .Where(b => b.Type == BidType.Submit)
            .Where(b => b.ToContract() is { } c && (lastContract is null || c.CompareTo(lastContract.Value) > 0))
            .OrderBy(b => b.ToContract())
            .FirstOrDefault();
    }

    private int ScoresTowardTarget(BidNode bid, PartnershipTarget? target)
    {
        if (target is null || target.Value.Partnership != Partnership)
        {
            return 0;
        }

        var contract = bid.ToContract();
        if (contract is null)
        {
            return 0;
        }

        var score = 0;
        if (contract.Value.Color == target.Value.Contract.Color)
        {
            score += 10;
        }

        if (contract.Value.Level >= target.Value.Contract.Level)
        {
            score += 3;
        }

        return score;
    }
}
