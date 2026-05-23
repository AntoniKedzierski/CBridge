internal static class ConsoleFormat
{
    public static string FormatAuctionPath(IEnumerable<AuctionCall> calls)
    {
        var bids = calls
            .Where(c => !string.Equals(c.Call, "Pass", StringComparison.OrdinalIgnoreCase))
            .Select(c => string.IsNullOrWhiteSpace(c.Convention) ? c.Call : $"{c.Call} ({c.Convention})")
            .ToList();

        return bids.Count == 0 ? "brak odzywek" : string.Join(" > ", bids);
    }
}
