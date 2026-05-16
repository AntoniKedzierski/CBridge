using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.OutputEncoding = Encoding.UTF8;

var baseDir = AppContext.BaseDirectory;
var workspace = FindWorkspaceRoot(baseDir);
var systemPath = args.FirstOrDefault(a => a.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    ?? Path.Combine(workspace, "BiddingBrowser", "BiddingSystems", "Wspólny Język.json");
var savedDealsDir = Path.Combine(workspace, "BridgeAuctionSimulator_HB", "SavedDeals");
Directory.CreateDirectory(savedDealsDir);
var seedFromArgs = args.Select(TryParseSeed).FirstOrDefault(s => s.HasValue);

if (!File.Exists(systemPath))
{
    Console.Error.WriteLine($"Nie znaleziono pliku systemu: {systemPath}");
    return 2;
}

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
};

var system = JsonSerializer.Deserialize<BiddingSystem>(File.ReadAllText(systemPath, Encoding.UTF8), jsonOptions);
if (system is null)
{
    Console.Error.WriteLine("Nie udało się odczytać systemu licytacyjnego.");
    return 2;
}

var selectedDeal = ChooseStartingDeal(savedDealsDir);
while (true)
{
    var seed = seedFromArgs ?? Random.Shared.Next();
    var simulator = new AuctionSimulator(system, seed, selectedDeal);
    var result = simulator.Run();
    var reportPath = Path.Combine(workspace, "BridgeAuctionSimulator_HB", "report.html");
    File.WriteAllText(reportPath, HtmlReport.Render(result, systemPath), Encoding.UTF8);

    Console.WriteLine();
    Console.WriteLine($"System: {system.SystemName}");
    Console.WriteLine();
    Console.WriteLine("Karty:");
    PrintHands(result.Deal);
    Console.WriteLine();
    Console.WriteLine($"Seed: {result.Seed}");
    Console.WriteLine($"Cel z kart (kontrolnie): {result.Target}");
    Console.WriteLine($"Cel z licytacji: {result.InferredTarget?.ToString() ?? "brak"}");
    Console.WriteLine($"Kontrakt po 3 pasach: {result.FinalContract}");
    Console.WriteLine($"Raport HTML: {reportPath}");
    Console.WriteLine();
    Console.WriteLine("Licytacja:");
    foreach (var call in result.Calls)
    {
        Console.WriteLine($"{call.No,2}. {call.PlayerShort}: {call.Call}");
    }
    Console.WriteLine();
    Console.WriteLine($"Gałąź drzewa: {FormatAuctionPath(result.Calls)}");

    PromptSaveDeal(savedDealsDir, result.Deal);

    var nextRun = AskNextRun(savedDealsDir);
    if (nextRun.Exit)
    {
        break;
    }

    selectedDeal = nextRun.Deal;
}

return 0;

static int? TryParseSeed(string value)
{
    return int.TryParse(value, out var seed) ? seed : null;
}

static string FindWorkspaceRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Four.slnx")) || Directory.Exists(Path.Combine(dir.FullName, "BiddingBrowser")))
        {
            return dir.FullName;
        }

        dir = dir.Parent;
    }

    return Directory.GetCurrentDirectory();
}

static Deal? ChooseStartingDeal(string savedDealsDir)
{
    Console.WriteLine("Wczytać zapisane rozdanie? [t/N]");
    if (!IsYes(Console.ReadLine()))
    {
        return null;
    }

    var files = Directory.GetFiles(savedDealsDir, "*.json").OrderBy(Path.GetFileName).ToArray();
    if (files.Length == 0)
    {
        Console.WriteLine("Brak zapisanych rozdań. Generuję nowe rozdanie.");
        return null;
    }

    Console.WriteLine("Zapisane rozdania:");
    for (var i = 0; i < files.Length; i++)
    {
        Console.WriteLine($"{i + 1}. {Path.GetFileNameWithoutExtension(files[i])}");
    }

    Console.WriteLine("Podaj numer rozdania do wczytania albo Enter, żeby wygenerować nowe:");
    var selected = Console.ReadLine();
    if (!int.TryParse(selected, out var index) || index < 1 || index > files.Length)
    {
        Console.WriteLine("Nie wybrano zapisu. Generuję nowe rozdanie.");
        return null;
    }

    try
    {
        return SavedDeal.Load(files[index - 1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Nie udało się wczytać rozdania: {ex.Message}");
        Console.WriteLine("Generuję nowe rozdanie.");
        return null;
    }
}

static void PromptSaveDeal(string savedDealsDir, Deal deal)
{
    Console.WriteLine();
    Console.WriteLine("Zapisać to rozdanie do późniejszego odtworzenia? [t/N]");
    if (!IsYes(Console.ReadLine()))
    {
        return;
    }

    Console.WriteLine("Nazwa zapisu (Enter = data i czas):");
    var name = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(name))
    {
        name = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    foreach (var invalid in Path.GetInvalidFileNameChars())
    {
        name = name.Replace(invalid, '_');
    }

    var path = Path.Combine(savedDealsDir, $"{name}.json");
    SavedDeal.Save(path, deal);
    Console.WriteLine($"Zapisano rozdanie: {path}");
}

static NextRunChoice AskNextRun(string savedDealsDir)
{
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("Co dalej?");
        Console.WriteLine("1 - nowe losowe rozdanie od początku");
        Console.WriteLine("2 - wczytaj zapisane rozdanie i uruchom od początku");
        Console.WriteLine("Enter - zakończ");
        var answer = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(answer))
        {
            return new NextRunChoice(true, null);
        }

        if (answer.Trim() == "1")
        {
            return new NextRunChoice(false, null);
        }

        if (answer.Trim() == "2")
        {
            var deal = ChooseSavedDeal(savedDealsDir);
            if (deal is not null)
            {
                return new NextRunChoice(false, deal);
            }
        }

        Console.WriteLine("Nie rozumiem wyboru.");
    }
}

static Deal? ChooseSavedDeal(string savedDealsDir)
{
    var files = Directory.GetFiles(savedDealsDir, "*.json").OrderBy(Path.GetFileName).ToArray();
    if (files.Length == 0)
    {
        Console.WriteLine("Brak zapisanych rozdań.");
        return null;
    }

    Console.WriteLine("Zapisane rozdania:");
    for (var i = 0; i < files.Length; i++)
    {
        Console.WriteLine($"{i + 1}. {Path.GetFileNameWithoutExtension(files[i])}");
    }

    Console.WriteLine("Podaj numer rozdania do wczytania albo Enter, żeby wrócić:");
    var selected = Console.ReadLine();
    if (!int.TryParse(selected, out var index) || index < 1 || index > files.Length)
    {
        return null;
    }

    try
    {
        return SavedDeal.Load(files[index - 1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Nie udało się wczytać rozdania: {ex.Message}");
        return null;
    }
}

static bool IsYes(string? value)
{
    return value is not null
        && (value.Equals("t", StringComparison.OrdinalIgnoreCase)
            || value.Equals("tak", StringComparison.OrdinalIgnoreCase)
            || value.Equals("y", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
}

static void PrintHands(Deal deal)
{
    foreach (var player in Enum.GetValues<PlayerPosition>())
    {
        var hand = deal.Hands[player];
        Console.WriteLine($"{player.ShortName()}: {hand.Hcp} PC");
        foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
        {
            Console.WriteLine($"{suit.Symbol()} {hand.RenderSuitForConsole(suit)}");
        }
    }
}

static string FormatAuctionPath(IEnumerable<AuctionCall> calls)
{
    var bids = calls
        .Where(c => !string.Equals(c.Call, "Pass", StringComparison.OrdinalIgnoreCase))
        .Select(c => string.IsNullOrWhiteSpace(c.Convention) ? c.Call : $"{c.Call} ({c.Convention})")
        .ToList();

    return bids.Count == 0 ? "brak odzywek" : string.Join(" > ", bids);
}

internal sealed record NextRunChoice(bool Exit, Deal? Deal);

internal sealed class AuctionSimulator
{
    private readonly BiddingSystem _system;
    private readonly int _seed;
    private readonly Deal? _providedDeal;
    private readonly Dictionary<string, BidNode> _conventions;

    public AuctionSimulator(BiddingSystem system, int seed, Deal? providedDeal)
    {
        _system = system;
        _seed = seed;
        _providedDeal = providedDeal;
        _conventions = BuildConventionIndex(system);
    }

    public SimulationResult Run()
    {
        var deal = _providedDeal ?? Deal.Create(_seed);
        var target = PartnershipTarget.Find(deal) ?? PartnershipTarget.Fallback(deal);

        var calls = new List<AuctionCall>();
        var passCount = 0;
        var openingRoot = _system.Roots.FirstOrDefault(r => SameText(r.Name, "Otwarcia")) ?? _system.Roots.First();
        IReadOnlyList<BidNode> currentOptions = openingRoot.Bids;
        var openingSide = Partnership.None;
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
            var hand = deal.Hands[player];
            var side = player.Partnership();
            var branchBefore = lastTreeBid is null ? openingRoot.Name : lastTreeBid.Path;
            var expectedSystemActor = GetExpectedActor(opener, lastTreeBid);
            var mustPassBySignOff = signOffSide == side;
            var shouldUseTree = !mustPassBySignOff && (opener is null || player == expectedSystemActor);
            BidNode? chosen = null;
            var reason = "";

            if (shouldUseTree)
            {
                var inferredTarget = opener is null ? null : inference.GetTarget(side, opener.Value);
                var cannotPass = turn <= oneRoundForceUntil
                    || (gameForcingSide == side && (inferredTarget is null || !inferredTarget.Value.Contract.IsGameOrSlamReached(lastContract)));
                chosen = ChooseBid(currentOptions, hand, lastContract, cannotPass, inferredTarget, side);
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
                openingSide = opener.Value.Partnership();
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
            _seed,
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

    private static Dictionary<string, BidNode> BuildConventionIndex(BiddingSystem system)
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

    private static BidNode? ChooseBid(IReadOnlyList<BidNode> options, Hand hand, Contract? lastContract, bool cannotPass, PartnershipTarget? target, Partnership side)
    {
        AssignPaths(options, "");

        var matching = options
            .Where(b => b.Type == BidType.Submit)
            .Where(b => b.ToContract() is { } c && (lastContract is null || c.CompareTo(lastContract.Value) > 0))
            .Where(b => b.AutomaticResponse || b.Matches(hand))
            .OrderByDescending(b => b.AutomaticResponse)
            .ThenByDescending(b => ScoresTowardTarget(b, target, side))
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

    private static int ScoresTowardTarget(BidNode bid, PartnershipTarget? target, Partnership side)
    {
        if (target is null || target.Value.Partnership != side)
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
            return new PartnershipTarget(
                side,
                new Contract(level, BidColor.NoTrump),
                $"{points.Lower}+ PC z licytacji, z licytacji brak 8 kart w starszym.");
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
        return new PartnershipTarget(
            side,
            new Contract(Math.Min(level, 7), color),
            $"{points.Lower}+ PC z licytacji i {suit.Lower}+ {suitName} z licytacji.");
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
        if (lower > upper)
        {
            return this;
        }

        return new InferredRange(lower, upper);
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

internal sealed class Deal
{
    public required Dictionary<PlayerPosition, Hand> Hands { get; init; }

    public static Deal Create(int seed)
    {
        var deck = Enum.GetValues<Suit>()
            .SelectMany(s => Enum.GetValues<Rank>().Select(r => new Card(s, r)))
            .ToList();
        var rng = new Random(seed);

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

internal sealed class SavedDeal
{
    public string CreatedAt { get; set; } = "";
    public Dictionary<string, List<string>> Hands { get; set; } = [];

    public static void Save(string path, Deal deal)
    {
        var saved = new SavedDeal
        {
            CreatedAt = DateTimeOffset.Now.ToString("O"),
            Hands = Enum.GetValues<PlayerPosition>()
                .ToDictionary(p => p.ShortName(), p => deal.Hands[p].Cards.Select(c => c.Code()).ToList())
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(saved, options), Encoding.UTF8);
    }

    public static Deal Load(string path)
    {
        var saved = JsonSerializer.Deserialize<SavedDeal>(File.ReadAllText(path, Encoding.UTF8))
            ?? throw new InvalidOperationException("Pusty plik zapisu.");

        var hands = new Dictionary<PlayerPosition, Hand>();
        foreach (var player in Enum.GetValues<PlayerPosition>())
        {
            if (!saved.Hands.TryGetValue(player.ShortName(), out var cardCodes))
            {
                throw new InvalidOperationException($"Brak ręki dla gracza {player.ShortName()}.");
            }

            hands[player] = new Hand(cardCodes.Select(Card.Parse));
        }

        var allCards = hands.Values.SelectMany(h => h.Cards).ToList();
        if (allCards.Count != 52 || allCards.Distinct().Count() != 52)
        {
            throw new InvalidOperationException("Zapisane rozdanie nie zawiera dokładnie 52 różnych kart.");
        }

        return new Deal { Hands = hands };
    }
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

    public string ToCardList()
    {
        return string.Join(" ", Cards.Select(c => c.ToString()));
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
        var rankText = code[1..];
        var rank = rankText switch
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

internal sealed class BiddingSystem
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

internal static class HtmlReport
{
    public static string Render(SimulationResult result, string systemPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"pl\"><head><meta charset=\"utf-8\"><title>Symulacja licytacji HB</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f5f7fb;color:#162033}.wrap{max-width:1180px;margin:0 auto;padding:24px}h1{font-size:26px;margin:0 0 8px}.meta{color:#56616f;margin-bottom:20px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:14px}.card,.panel{background:#fff;border:1px solid #dfe5ee;border-radius:8px;padding:14px}.hand h2,.panel h2{font-size:18px;margin:0 0 10px}.suit{display:grid;grid-template-columns:34px 1fr;gap:8px;margin:5px 0}.hcp{font-weight:700;margin-top:8px}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #dfe5ee;padding:8px;text-align:left;vertical-align:top}th{background:#eef2f7}.ok{color:#0b6b3a;font-weight:700}.warn{color:#8a4f00;font-weight:700}</style></head><body><main class=\"wrap\">");
        sb.AppendLine("<h1>Symulacja licytacji brydżowej HB</h1>");
        sb.AppendLine($"<div class=\"meta\">System: {E(result.SystemName)} | Plik: {E(systemPath)} | Seed: {result.Seed}</div>");
        sb.AppendLine("<section class=\"grid\">");
        foreach (var player in Enum.GetValues<PlayerPosition>())
        {
            var hand = result.Deal.Hands[player];
            sb.AppendLine("<article class=\"card hand\">");
            sb.AppendLine($"<h2>{player}</h2>");
            foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
            {
                sb.AppendLine($"<div class=\"suit\"><strong>{suit.Symbol()}</strong><span>{E(hand.RenderSuit(suit))}</span></div>");
            }
            sb.AppendLine($"<div class=\"hcp\">PC: {hand.Hcp}</div>");
            sb.AppendLine("</article>");
        }
        sb.AppendLine("</section>");

        sb.AppendLine("<section class=\"grid\" style=\"margin-top:14px\">");
        AppendPair(sb, result.NorthSouth);
        AppendPair(sb, result.EastWest);
        sb.AppendLine("<article class=\"panel\">");
        sb.AppendLine("<h2>Wynik</h2>");
        sb.AppendLine($"<p>Cel z kart (kontrolnie): <span class=\"ok\">{E(result.Target.ToString())}</span></p>");
        sb.AppendLine($"<p>Cel z licytacji: <span class=\"ok\">{E(result.InferredTarget?.ToString() ?? "brak")}</span></p>");
        sb.AppendLine($"<p>Kontrakt po trzech pasach: <span class=\"ok\">{E(result.FinalContract)}</span></p>");
        sb.AppendLine("</article></section>");

        sb.AppendLine("<section style=\"margin-top:18px\"><h2>Licytacja</h2><table><thead><tr><th>#</th><th>Gracz</th><th>Odzywka</th><th>Warunek / konwencja</th><th>Uwagi</th></tr></thead><tbody>");
        foreach (var call in result.Calls)
        {
            var condition = string.Join("<br>", new[] { call.Condition, string.IsNullOrWhiteSpace(call.Convention) ? "" : $"Konwencja: {call.Convention}" }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(E));
            sb.AppendLine($"<tr><td>{call.No}</td><td>{E(call.PlayerShort)}</td><td><strong>{E(call.Call)}</strong></td><td>{condition}</td><td>{E(call.Reason)}</td></tr>");
        }
        sb.AppendLine("</tbody></table></section>");
        sb.AppendLine($"<section class=\"panel\" style=\"margin-top:18px\"><h2>Gałąź drzewa</h2><p>{E(FormatPath(result.Calls))}</p></section></main></body></html>");
        return sb.ToString();
    }

    private static void AppendPair(StringBuilder sb, PairSummary pair)
    {
        sb.AppendLine("<article class=\"panel\">");
        sb.AppendLine($"<h2>{pair.Partnership}</h2>");
        sb.AppendLine($"<p>PC w parze: <strong>{pair.Hcp}</strong></p>");
        sb.AppendLine($"<p>♠ {pair.Spades}, ♥ {pair.Hearts}, ♦ {pair.Diamonds}, ♣ {pair.Clubs}</p>");
        sb.AppendLine($"<p>Figura w każdym kolorze: {(pair.HasFigureInEverySuit ? "<span class=\"ok\">tak</span>" : "<span class=\"warn\">nie</span>")}</p>");
        sb.AppendLine("</article>");
    }

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

    private static string FormatPath(IEnumerable<AuctionCall> calls)
    {
        var bids = calls
            .Where(c => !string.Equals(c.Call, "Pass", StringComparison.OrdinalIgnoreCase))
            .Select(c => string.IsNullOrWhiteSpace(c.Convention) ? c.Call : $"{c.Call} ({c.Convention})")
            .ToList();

        return bids.Count == 0 ? "brak odzywek" : string.Join(" > ", bids);
    }
}

internal enum PlayerPosition { North, East, South, West }
internal enum Partnership { None, NS, EW }
internal enum Suit { Clubs, Diamonds, Hearts, Spades }
internal enum Rank { Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
internal enum BidColor { NoColor, Clubs, Diamonds, Hearts, Spades, NoTrump }
internal enum BidType { Pass, Submit, Double, Redouble }

internal static class BridgeExtensions
{
    public static Partnership Partnership(this PlayerPosition player)
    {
        return player is PlayerPosition.North or PlayerPosition.South ? global::Partnership.NS : global::Partnership.EW;
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
