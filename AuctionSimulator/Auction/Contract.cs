internal readonly record struct Contract(int Level, BidColor Color) : IComparable<Contract>
{
    public int CompareTo(Contract other)
    {
        var level = Level.CompareTo(other.Level);
        return level != 0 ? level : Color.BidRank().CompareTo(other.Color.BidRank());
    }

    public bool IsGameOrSlamReached(Contract? current)
    {
        return current is not null && current.Value.Level >= Level && current.Value.Color == Color;
    }

    public override string ToString() => $"{Level}{Color.Symbol()}";
}

internal sealed record AuctionCall(int No, string Player, string Call, string Branch, string Condition, string Convention, string Reason)
{
    public string PlayerShort => Player switch
    {
        nameof(PlayerPosition.North) => "N",
        nameof(PlayerPosition.East) => "E",
        nameof(PlayerPosition.South) => "S",
        nameof(PlayerPosition.West) => "W",
        _ => Player
    };
}

internal sealed record SimulationResult(
    int Seed,
    int DealAttempts,
    string SystemName,
    Deal Deal,
    PartnershipTarget Target,
    PartnershipTarget? InferredTarget,
    IReadOnlyList<AuctionCall> Calls,
    string FinalContract,
    PairSummary NorthSouth,
    PairSummary EastWest);
