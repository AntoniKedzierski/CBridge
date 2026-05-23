using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;

var workspace = FindWorkspaceRoot(AppContext.BaseDirectory);
var projectDir = Path.Combine(workspace, "AuctionSimulator");
var savedDealsDir = Path.Combine(projectDir, "SavedDeals");
Directory.CreateDirectory(savedDealsDir);

var systemPath = args.FirstOrDefault(a => a.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    ?? Path.Combine(workspace, "BiddingBrowser", "BiddingSystems", "Wspólny Język.json");
var seedFromArgs = args.Select(TryParseSeed).FirstOrDefault(s => s.HasValue);

if (!File.Exists(systemPath))
{
    Console.Error.WriteLine($"Nie znaleziono pliku systemu: {systemPath}");
    return 2;
}

var system = JsonSerializer.Deserialize<BiddingSystemModel>(
    File.ReadAllText(systemPath, Encoding.UTF8),
    JsonSettings.Options);

if (system is null)
{
    Console.Error.WriteLine("Nie udało się odczytać systemu licytacyjnego.");
    return 2;
}

var selectedDeal = ChooseStartingDeal(savedDealsDir);
while (true)
{
    var seed = seedFromArgs ?? Random.Shared.Next();
    var dealer = new Dealer(seed);
    var auction = new Auction(system, dealer, selectedDeal);
    var result = auction.Run();

    var reportPath = Path.Combine(projectDir, "report.html");
    File.WriteAllText(reportPath, HtmlReport.Render(result, systemPath), Encoding.UTF8);

    PrintResult(system, result, reportPath);
    PromptSaveDeal(savedDealsDir, result.Deal);

    var nextRun = AskNextRun(savedDealsDir);
    if (nextRun.Exit)
    {
        break;
    }

    selectedDeal = nextRun.Deal;
}

return 0;

static void PrintResult(BiddingSystemModel system, SimulationResult result, string reportPath)
{
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
    Console.WriteLine($"Gałąź drzewa: {ConsoleFormat.FormatAuctionPath(result.Calls)}");
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

static Deal? ChooseStartingDeal(string savedDealsDir)
{
    Console.WriteLine("Wczytać zapisane rozdanie? [t/N]");
    if (!IsYes(Console.ReadLine()))
    {
        return null;
    }

    var deal = ChooseSavedDeal(savedDealsDir);
    if (deal is null)
    {
        Console.WriteLine("Generuję nowe rozdanie.");
    }

    return deal;
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

internal sealed record NextRunChoice(bool Exit, Deal? Deal);
