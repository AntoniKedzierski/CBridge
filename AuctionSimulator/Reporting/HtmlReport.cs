using System.Net;
using System.Text;

internal static class HtmlReport
{
    public static string Render(SimulationResult result, string systemPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"pl\"><head><meta charset=\"utf-8\"><title>Symulacja licytacji</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f5f7fb;color:#162033}.wrap{max-width:1180px;margin:0 auto;padding:24px}h1{font-size:26px;margin:0 0 8px}.meta{color:#56616f;margin-bottom:20px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:14px}.card,.panel{background:#fff;border:1px solid #dfe5ee;border-radius:8px;padding:14px}.hand h2,.panel h2{font-size:18px;margin:0 0 10px}.suit{display:grid;grid-template-columns:34px 1fr;gap:8px;margin:5px 0}.hcp{font-weight:700;margin-top:8px}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #dfe5ee;padding:8px;text-align:left;vertical-align:top}th{background:#eef2f7}.ok{color:#0b6b3a;font-weight:700}.warn{color:#8a4f00;font-weight:700}</style></head><body><main class=\"wrap\">");
        sb.AppendLine("<h1>Symulacja licytacji brydżowej</h1>");
        sb.AppendLine($"<div class=\"meta\">System: {E(result.SystemName)} | Plik: {E(systemPath)} | Seed: {result.Seed}</div>");
        sb.AppendLine("<section class=\"grid\">");

        foreach (var player in Enum.GetValues<PlayerPosition>())
        {
            var hand = result.Deal.Hands[player];
            sb.AppendLine("<article class=\"card hand\">");
            sb.AppendLine($"<h2>{player.ShortName()} - {hand.Hcp} PC</h2>");
            foreach (var suit in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
            {
                sb.AppendLine($"<div class=\"suit\"><strong>{suit.Symbol()}</strong><span>{E(hand.RenderSuit(suit))}</span></div>");
            }
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
        sb.AppendLine($"<section class=\"panel\" style=\"margin-top:18px\"><h2>Gałąź drzewa</h2><p>{E(ConsoleFormat.FormatAuctionPath(result.Calls))}</p></section></main></body></html>");
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
}
