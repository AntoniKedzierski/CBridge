using Bridgemate.Models;

namespace Bridgemate.Services;

public class NavigationStore {
    public List<DealResult> Deals { get; set; } = [];
    public AuctionEntry? SelectedEntry { get; set; }
}
