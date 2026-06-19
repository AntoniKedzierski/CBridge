using Model;
using Model.Bidding;
using Model.Enums;

namespace Bridgemate.Models;

public class DealResult {
    public int DealNumber { get; set; }
    public PlayerPosition Dealer { get; set; }
    public Dictionary<PlayerPosition, Hand> Hands { get; set; } = [];
    public List<AuctionEntry> AuctionHistory { get; set; } = [];
    public Contract Contract { get; set; } = new();

    public int NsPoints => Hands[PlayerPosition.North].PointsNt + Hands[PlayerPosition.South].PointsNt;
    public int EwPoints => Hands[PlayerPosition.East].PointsNt + Hands[PlayerPosition.West].PointsNt;

    public string Summary {
        get {
            var contract = Contract.Passed ? "Passed out" : Contract.ToString();
            return $"Deal #{DealNumber}  •  {contract}";
        }
    }

    public string PointsSummary => $"NS: {NsPoints} PC  |  EW: {EwPoints} PC";
}
