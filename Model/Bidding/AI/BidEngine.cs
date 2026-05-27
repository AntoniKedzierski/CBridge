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
    public BiddingSystem BiddingSystem { get; private set; }
    public string BiddingSystemPath { get; private set; } = @"BiddingBrowser\BiddingSystems\Wspólny Język.json";
    public BidEngine(Auction auction) {
        Auction = auction;
        BiddingSystem = new BiddingSystem(BiddingSystemPath);   //hardcoded path for now
    }
    public Bid Get(HandEvaluation partnersHand, HandEvaluation LeftOpponentsHand, HandEvaluation RightOpponentsHand) {
        throw new NotImplementedException();
    }

    public void EvaluateHands(Bid bid, HandEvaluation partnersHand, HandEvaluation LeftOpponentsHand, HandEvaluation RightOpponentsHand) {
        List<BidNode> possibleBids = new List<BidNode>();

        partnersHand.Evaluate(bidNode);
        LeftOpponentsHand.Evaluate(bidNode);
        RightOpponentsHand.Evaluate(bidNode);
    }

    public void findBidNode(Bid bid, List<BidNode> foundBidNodes, List<BidNode> bidNodes) {

        foreach (BidNode bidNode in bidNodes) {
            if (bidNode.Type == bid.BidType && bidNode.Color == bid.Color && bidNode.Value == bid.Level) {
                foundBidNodes.Add(bidNode);
            }

            findBidNode(bid, foundBidNodes, bidNode.NextBids);
        }
    }
}
