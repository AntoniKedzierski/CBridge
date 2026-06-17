using Model.Bidding.AI.Eval;
using Model.Bidding.Bids;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.AI.Engine;

public partial class BidEngine {

    private BidNode? GetBidFromSystemBranches(Hand hand, BidNode branchHead) {
        return GetBidFromSystemBranches(hand, [branchHead]);
    }


    private BidNode? GetBidFromSystemBranches(Hand hand, IEnumerable<BidNode> branches) {
        var chosenBids = new List<BidNode>();
        foreach (var branchHead in branches) {
            // Weź wszystko, co pasuje do ręki i jest legalne, i wybierz z tego systemową odzywkę.
            var bidCandidates = FindNodesByHand(hand, branchHead)
                .Where(e => e.IsBidLegal(Auction))
                .ToList();

            var chosenBid = ChooseBidFromSystem(bidCandidates);
            if (chosenBid != null) {
                chosenBids.Add(chosenBid);
            }
        }

        if (chosenBids.Count == 0) {
            return null;
        }

        var firstChosenBid = chosenBids[0];
        if (!chosenBids.All(e => e.EqualsByColorAndValue(firstChosenBid))) {
            Console.WriteLine("Multiple tree branches possible: " + string.Join(", ", chosenBids.Distinct()));
            return null;
        }

        return firstChosenBid;
    }


    private TResult? GetCommonBranchesValue<TResult>(Dictionary<BidNode, TableEvaluation> branches, Func<BidNode, TResult> property) {
        TResult? currentValue = default;
        var valueFound = false;

        foreach (var branchHead in branches.Keys) {
            if (!valueFound) {
                currentValue = property.Invoke(branchHead);
                continue;
            }

            if (valueFound && !currentValue.Equals(property.Invoke(branchHead))) {
                Console.WriteLine($"Property mismatch across branches. Current value was {currentValue}, but {branchHead} has different value {property.Invoke(branchHead)}.");
            }
        }

        return currentValue;
    }


    public List<BidNode> FindNodesByHand(Hand hand, BidNode head) {
        List<BidNode> foundBidNodes = new();
        foreach (BidNode bidNode in head.NextBids) {
            if (bidNode.Matches(hand)) {
                foundBidNodes.Add(bidNode);
            }
        }

        return foundBidNodes;
    }


    public List<BidNode> FindNodesByHand(Hand hand, Root root) {
        List<BidNode> foundBidNodes = new();
        foreach (BidNode bidNode in root.Bids) {
            if (bidNode.Matches(hand)) {
                foundBidNodes.Add(bidNode);
            }
        }

        return foundBidNodes;
    }


    public static BidNode? ChooseBidFromSystem(List<BidNode> legalBids, bool preferConventions = false) {
        if (legalBids.Count == 0) {
            return null;
        }

        if (preferConventions) {
            foreach (var bid in legalBids) {
                if (bid.Convention != null) {
                    return bid;
                }
            }
        }

        // Lower bids are preferred
        var lowestBid = legalBids[0];
        foreach (var bid in legalBids) {
            if (bid.Value < lowestBid.Value) {
                lowestBid = bid;
            }
            else if (bid.Value == lowestBid.Value && bid.Color < lowestBid.Color) {
                lowestBid = bid;
            }
        }

        lowestBid.IsFromSystem = true;
        return lowestBid;
    }
}
