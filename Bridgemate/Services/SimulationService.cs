using Bridgemate.Models;
using Model;
using Model.Bidding;
using Model.Bidding.AI.Engine;
using Model.Bidding.Bids;
using Model.Enums;

namespace Bridgemate.Services;

public class SimulationService {

    public List<DealResult> Simulate(int numberOfDeals) {
        var results = new List<DealResult>();
        var auction = new Auction();

        var engines = Enum.GetValues<PlayerPosition>()
            .ToDictionary(pos => pos, pos => new BidEngine(auction, pos));

        var players = Enum.GetValues<PlayerPosition>()
            .Select(pos => new Player("bot", pos, engines[pos]))
            .ToArray();

        for (int dealIndex = 0; dealIndex < numberOfDeals; dealIndex++) {
            var deal = new Deal();
            foreach (var player in players) {
                player.GiveHand(deal.Hands[player.CurrentPosition]);
            }

            var dealer = (PlayerPosition)(dealIndex % 4);
            auction.Start(dealer);

            try {
                while (!auction.IsCompleted()) {
                    var currentPlayer = players.First(p => p.CurrentPosition == auction.CurrentBidder);
                    var bid = currentPlayer.MakeBid();
                    auction.Submit(bid);
                }
            } catch (Exception) {
                // Safety catch for unexpected bidding errors; still record the deal
            }

            var auctionEntries = CaptureAuction(auction, engines);
            var contract = auction.GetContract(players);

            results.Add(new DealResult {
                DealNumber = dealIndex + 1,
                Dealer = dealer,
                Hands = deal.Hands,
                AuctionHistory = auctionEntries,
                Contract = contract
            });

            auction.Clear();
            foreach (var player in players) {
                player.Reset();
            }
        }

        return results;
    }


    private static List<AuctionEntry> CaptureAuction(Auction auction, Dictionary<PlayerPosition, BidEngine> engines) {
        var entries = new List<AuctionEntry>();
        var playerBidIndexes = Enum.GetValues<PlayerPosition>().ToDictionary(p => p, _ => 0);

        for (int i = 0; i < auction.AuctionHistory.Count; i++) {
            var bid = auction.AuctionHistory[i];
            var pos = auction.GetBidder(i);
            var engine = engines[pos];

            BidNode? node = null;
            if (bid.IsFromSystem) {
                var idx = playerBidIndexes[pos];
                if (idx < engine.OwnBidsHistory.Count) {
                    node = engine.OwnBidsHistory[idx];
                }
                playerBidIndexes[pos]++;
            }

            entries.Add(new AuctionEntry { Position = pos, Bid = bid, Node = node });
        }

        return entries;
    }
}
