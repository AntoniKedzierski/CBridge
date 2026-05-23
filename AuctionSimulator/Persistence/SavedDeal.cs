using System.Text;
using System.Text.Json;

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
