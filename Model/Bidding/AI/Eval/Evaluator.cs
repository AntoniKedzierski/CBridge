using Model.Bidding.Bids;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.AI.Eval;

public static class Evaluator {

    public static TableEvaluation FromOwnHand(Hand hand) {
        return new() {
            Partner = new(hand),
            LeftOpponent = new(hand),
            RightOpponent = new(hand)
        };
    }


    public static TableEvaluation FromPartner(BidNode lastPartnerBidNode, Hand hand) {
        var result = new TableEvaluation {
            Partner = new(hand),
            LeftOpponent = new(hand),
            RightOpponent = new(hand)
        };

        result.Partner.Evaluate(lastPartnerBidNode);

        var combinedHandEvaluation = result.GetCombinedHandEvaluation(hand);
        result.LeftOpponent.Evaluate(combinedHandEvaluation);
        result.RightOpponent.Evaluate(combinedHandEvaluation);

        return result;
    }


    public static TableEvaluation FromPartner(BidNode lastParentBidNode, Hand hand, Auction auction, PlayerPosition position) {
        var result = new TableEvaluation {
            Partner = new(hand),
            LeftOpponent = new(hand),
            RightOpponent = new(hand)
        };

        if (lastParentBidNode.Type == BidType.Pass) {
            // TODO
        }

        if (lastParentBidNode.Type == BidType.Double) {
            // TODO
        }

        if (lastParentBidNode.Type == BidType.Redouble) {
            // TODO
        }

        var currentEvaluatedBidNode = lastParentBidNode;
        while (currentEvaluatedBidNode != null) {
            result.Partner.Evaluate(currentEvaluatedBidNode);
            currentEvaluatedBidNode = currentEvaluatedBidNode.Parent?.Parent;
        }

        var combinedHandEvaluation = result.GetCombinedHandEvaluation(hand);
        result.LeftOpponent.Evaluate(combinedHandEvaluation);
        result.RightOpponent.Evaluate(combinedHandEvaluation);

        return result;
    }
   
}
