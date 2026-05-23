internal sealed class AuctionInference
{
    private readonly Dictionary<PlayerPosition, InferredHand> _hands = Enum.GetValues<PlayerPosition>()
        .ToDictionary(p => p, _ => InferredHand.Any());

    public void Apply(BidNode bid, PlayerPosition opener)
    {
        var actor = bid.OpenerBid ? opener : opener.Partner();
        _hands[actor].Apply(bid);
    }

    public PartnershipTarget? GetTarget(Partnership side, PlayerPosition opener)
    {
        if (side != opener.Partnership())
        {
            return null;
        }

        var openerHand = _hands[opener];
        var responderHand = _hands[opener.Partner()];
        var points = openerHand.Points + responderHand.Points;
        var spades = openerHand.Spades + responderHand.Spades;
        var hearts = openerHand.Hearts + responderHand.Hearts;
        var diamonds = openerHand.Diamonds + responderHand.Diamonds;
        var clubs = openerHand.Clubs + responderHand.Clubs;
        var slamLevel = points.Lower >= 37 ? 7 : points.Lower >= 30 ? 6 : 0;

        if (points.Lower >= 24 && spades.Lower >= 8)
        {
            return Target(side, slamLevel > 0 ? slamLevel : 4, BidColor.Spades, points, spades, "pików");
        }

        if (points.Lower >= 24 && hearts.Lower >= 8)
        {
            return Target(side, slamLevel > 0 ? slamLevel : 4, BidColor.Hearts, points, hearts, "kierów");
        }

        if (points.Lower >= 25 && spades.Upper < 8 && hearts.Upper < 8)
        {
            var level = slamLevel > 0 ? slamLevel : 3;
            return new PartnershipTarget(side, new Contract(level, BidColor.NoTrump), $"{points.Lower}+ PC z licytacji, z licytacji brak 8 kart w starszym.");
        }

        if (points.Lower >= 27 && diamonds.Lower >= 8)
        {
            return Target(side, slamLevel > 0 ? slamLevel : 5, BidColor.Diamonds, points, diamonds, "kar");
        }

        if (points.Lower >= 27 && clubs.Lower >= 8)
        {
            return Target(side, slamLevel > 0 ? slamLevel : 5, BidColor.Clubs, points, clubs, "trefli");
        }

        return null;
    }

    private static PartnershipTarget Target(Partnership side, int level, BidColor color, InferredRange points, InferredRange suit, string suitName)
    {
        return new PartnershipTarget(side, new Contract(Math.Min(level, 7), color), $"{points.Lower}+ PC z licytacji i {suit.Lower}+ {suitName} z licytacji.");
    }
}

internal sealed class InferredHand
{
    public InferredRange Points { get; private set; } = new(0, 40);
    public InferredRange Spades { get; private set; } = new(0, 13);
    public InferredRange Hearts { get; private set; } = new(0, 13);
    public InferredRange Diamonds { get; private set; } = new(0, 13);
    public InferredRange Clubs { get; private set; } = new(0, 13);

    public static InferredHand Any() => new();

    public void Apply(BidNode bid)
    {
        Points = Points.Intersect(bid.PointsRange, 0, 40);
        Spades = Spades.Intersect(bid.SpadesCardRange, 0, 13);
        Hearts = Hearts.Intersect(bid.HeartsCardRange, 0, 13);
        Diamonds = Diamonds.Intersect(bid.DiamondsCardRange, 0, 13);
        Clubs = Clubs.Intersect(bid.ClubsCardRange, 0, 13);
    }
}

internal readonly record struct InferredRange(int Lower, int Upper)
{
    public static InferredRange operator +(InferredRange left, InferredRange right)
    {
        return new InferredRange(left.Lower + right.Lower, left.Upper + right.Upper);
    }

    public InferredRange Intersect(NumberRange? range, int min, int max)
    {
        if (range is null)
        {
            return this;
        }

        var lower = Math.Max(Lower, range.Lower ?? min);
        var upper = Math.Min(Upper, range.Upper ?? max);
        return lower > upper ? this : new InferredRange(lower, upper);
    }
}
