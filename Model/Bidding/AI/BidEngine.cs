using Model.Bidding.Bids;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.AI;

public class BidEngine : IBidInput {
    public Auction Auction { get; private set; }
    public BidEngine(Auction auction) {
        Auction = auction;
    }
    public Bid Get(HandEvaluation partnersHand, HandEvaluation LeftOpponentsHand, HandEvaluation RightOpponentsHand) {
        throw new NotImplementedException();
    }

    public void EvaluateAllHands(BidNode bidNode, HandEvaluation partnersHand, HandEvaluation LeftOpponentsHand, HandEvaluation RightOpponentsHand) {
        partnersHand.Evaluate(bidNode);
        LeftOpponentsHand.Evaluate(bidNode);
        RightOpponentsHand.Evaluate(bidNode);
    }

    public BidNode? findBidNode(Bid bid) {
        return null;
    }
}
