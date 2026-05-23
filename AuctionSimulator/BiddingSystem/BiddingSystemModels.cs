using System.Text.Json.Serialization;

internal enum BidColor { NoColor, Clubs, Diamonds, Hearts, Spades, NoTrump }
internal enum BidType { Pass, Submit, Double, Redouble }

internal sealed class BiddingSystemModel
{
    public string SystemName { get; set; } = "";
    public List<RootNode> Roots { get; set; } = [];
}

internal sealed class RootNode
{
    public string Name { get; set; } = "";
    public List<BidNode> Bids { get; set; } = [];
}

internal sealed class BidNode
{
    public string? Identifier { get; set; }
    public int? Value { get; set; }
    public BidColor Color { get; set; }
    public BidType Type { get; set; }
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public string? Convention { get; set; }
    public NumberRange? PointsRange { get; set; }
    public NumberRange? SpadesCardRange { get; set; }
    public NumberRange? HeartsCardRange { get; set; }
    public NumberRange? DiamondsCardRange { get; set; }
    public NumberRange? ClubsCardRange { get; set; }
    public int? Aces { get; set; }
    public int? Kings { get; set; }
    public bool OpenerBid { get; set; }
    public bool SignOff { get; set; }
    public bool OneRoundForcing { get; set; }
    public bool GameForcing { get; set; }
    public bool AutomaticResponse { get; set; }
    public List<BidNode> NextBids { get; set; } = [];

    [JsonIgnore]
    public string Path { get; set; } = "";

    public string DisplayCall => Type switch
    {
        BidType.Pass => "Pass",
        BidType.Double => "X",
        BidType.Redouble => "XX",
        _ => $"{Value}{Color.Symbol()}"
    };

    public bool Matches(Hand hand)
    {
        return hand.Matches(PointsRange, SpadesCardRange, HeartsCardRange, DiamondsCardRange, ClubsCardRange, Aces, Kings);
    }

    public Contract? ToContract()
    {
        if (Type != BidType.Submit || !Value.HasValue || Value < 1 || Value > 7 || Color == BidColor.NoColor)
        {
            return null;
        }

        return new Contract(Value.Value, Color);
    }

    public static BidNode FromContract(Contract contract, string condition, string path)
    {
        return new BidNode
        {
            Value = contract.Level,
            Color = contract.Color,
            Type = BidType.Submit,
            Condition = condition,
            Path = $"{path} > {contract}"
        };
    }
}

internal sealed class NumberRange
{
    public int? Lower { get; set; }
    public int? Upper { get; set; }
}
