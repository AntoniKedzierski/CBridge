internal sealed class Auction
{
    private readonly BiddingSystemModel _system;
    private readonly Dealer _dealer;
    private readonly Deal? _providedDeal;
    private readonly Dictionary<string, BidNode> _conventions;

    public Auction(BiddingSystemModel system, Dealer dealer, Deal? providedDeal = null)
    {
        _system = system;
        _dealer = dealer;
        _providedDeal = providedDeal;
        _conventions = BuildConventionIndex(system);
    }

    public SimulationResult Run()
    {
        var deal = _providedDeal ?? _dealer.Deal();
        var bots = Enum.GetValues<PlayerPosition>()
            .ToDictionary(p => p, p => new PlayerBot(p, deal.Hands[p]));
        var target = PartnershipTarget.Find(deal) ?? PartnershipTarget.Fallback(deal);

        var calls = new List<AuctionCall>();
        var passCount = 0;
        var openingRoot = _system.Roots.FirstOrDefault(r => SameText(r.Name, "Otwarcia")) ?? _system.Roots.First();
        IReadOnlyList<BidNode> currentOptions = openingRoot.Bids;
        PlayerPosition? opener = null;
        var gameForcingSide = Partnership.None;
        var oneRoundForceUntil = -1;
        var signOffSide = Partnership.None;
        BidNode? lastTreeBid = null;
        Contract? lastContract = null;
        var inference = new AuctionInference();
        PartnershipTarget? inferredAuctionTarget = null;

        for (var turn = 0; turn < 60; turn++)
        {
            var player = (PlayerPosition)(turn % 4);
            var bot = bots[player];
            var side = player.Partnership();
            var branchBefore = lastTreeBid is null ? openingRoot.Name : lastTreeBid.Path;
            var expectedSystemActor = GetExpectedActor(opener, lastTreeBid);
            var mustPassBySignOff = signOffSide == side;
            var shouldUseTree = !mustPassBySignOff && (opener is null || player == expectedSystemActor);
            BidNode? chosen = null;
            var reason = "";

            if (shouldUseTree)
            {
                AssignPaths(currentOptions, "");
                var inferredTarget = opener is null ? null : inference.GetTarget(side, opener.Value);
                var cannotPass = turn <= oneRoundForceUntil
                    || (gameForcingSide == side && (inferredTarget is null || !inferredTarget.Value.Contract.IsGameOrSlamReached(lastContract)));

                chosen = bot.ChooseBid(currentOptions, lastContract, cannotPass, inferredTarget);

                if (chosen is null && inferredTarget is not null)
                {
                    if (lastContract is null || inferredTarget.Value.Contract.CompareTo(lastContract.Value) > 0)
                    {
                        chosen = BidNode.FromContract(inferredTarget.Value.Contract, inferredTarget.Value.Reason, branchBefore);
                        reason = "Brak pasującej gałęzi; kontrakt wynika z zakresów ujawnionych licytacją.";
                    }
                    else
                    {
                        reason = "Kontrakt wynikający z licytacji został już osiągnięty; bot pasuje.";
                    }
                }
                else if (chosen is null && cannotPass)
                {
                    reason = "Brak legalnej odzywki w gałęzi mimo forsingu; bot pasuje.";
                }
            }

            var contract = chosen?.ToContract();
            var callText = chosen is null ? "Pass" : chosen.DisplayCall;
            if (contract is not null)
            {
                var actualChosen = chosen!;
                lastContract = contract;
                passCount = 0;
                opener ??= player;
                inference.Apply(actualChosen, opener.Value);
                inferredAuctionTarget = inference.GetTarget(side, opener.Value) ?? inferredAuctionTarget;
                lastTreeBid = actualChosen;
                currentOptions = ResolveNextOptions(actualChosen);

                if (actualChosen.SignOff)
                {
                    signOffSide = side;
                }

                if (actualChosen.GameForcing)
                {
                    gameForcingSide = side;
                }

                if (actualChosen.OneRoundForcing)
                {
                    oneRoundForceUntil = turn + 4;
                }
            }
            else
            {
                passCount++;
                if (opener is null)
                {
                    currentOptions = openingRoot.Bids;
                }
            }

            calls.Add(new AuctionCall(
                calls.Count + 1,
                player.ToString(),
                callText,
                branchBefore,
                chosen?.Condition ?? "",
                chosen?.Convention ?? "",
                reason));

            if (lastContract is not null && passCount >= 3)
            {
                break;
            }
        }

        return new SimulationResult(
            _dealer.Seed,
            _providedDeal is null ? 1 : 0,
            _system.SystemName,
            deal,
            target,
            inferredAuctionTarget,
            calls,
            lastContract?.ToString() ?? "Pass out",
            PairSummary.FromDeal(deal, Partnership.NS),
            PairSummary.FromDeal(deal, Partnership.EW));
    }

    private static Dictionary<string, BidNode> BuildConventionIndex(BiddingSystemModel system)
    {
        var index = new Dictionary<string, BidNode>(StringComparer.OrdinalIgnoreCase);
        var conventions = system.Roots.FirstOrDefault(r => SameText(r.Name, "Konwencje"));
        if (conventions is null)
        {
            return index;
        }

        foreach (var bid in conventions.Bids)
        {
            IndexConventionNode(index, bid, conventions.Name);
        }

        return index;
    }

    private static void IndexConventionNode(Dictionary<string, BidNode> index, BidNode bid, string path)
    {
        bid.Path = $"{path} > {bid.DisplayCall}";
        foreach (var key in new[] { bid.Identifier, bid.Convention, bid.Condition, bid.Description })
        {
            if (!string.IsNullOrWhiteSpace(key) && !index.ContainsKey(key))
            {
                index[key] = bid;
            }
        }

        foreach (var child in bid.NextBids)
        {
            IndexConventionNode(index, child, bid.Path);
        }
    }

    private IReadOnlyList<BidNode> ResolveNextOptions(BidNode chosen)
    {
        if (chosen.NextBids.Count > 0)
        {
            AssignPaths(chosen.NextBids, chosen.Path);
            return chosen.NextBids;
        }

        if (!string.IsNullOrWhiteSpace(chosen.Convention) && TryFindConvention(chosen.Convention, out var convention))
        {
            AssignPaths(convention.NextBids, convention.Path);
            return convention.NextBids;
        }

        return Array.Empty<BidNode>();
    }

    private bool TryFindConvention(string name, out BidNode convention)
    {
        if (_conventions.TryGetValue(name, out convention!))
        {
            return true;
        }

        var match = _conventions.FirstOrDefault(kvp =>
            kvp.Key.Contains(name, StringComparison.OrdinalIgnoreCase)
            || name.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase));
        convention = match.Value!;
        return convention is not null;
    }

    private static PlayerPosition? GetExpectedActor(PlayerPosition? opener, BidNode? lastTreeBid)
    {
        if (opener is null || lastTreeBid is null)
        {
            return null;
        }

        return lastTreeBid.OpenerBid ? opener.Value.Partner() : opener.Value;
    }

    private static void AssignPaths(IEnumerable<BidNode> bids, string parentPath)
    {
        foreach (var bid in bids)
        {
            bid.Path = string.IsNullOrWhiteSpace(parentPath) ? bid.DisplayCall : $"{parentPath} > {bid.DisplayCall}";
            AssignPaths(bid.NextBids, bid.Path);
        }
    }

    private static bool SameText(string? left, string right)
    {
        return string.Equals(left?.Trim(), right, StringComparison.OrdinalIgnoreCase);
    }
}
