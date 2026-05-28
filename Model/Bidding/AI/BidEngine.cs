using Model.Bidding.Bids;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.AI;

public class BidEngine : IBidInput {
    public Auction Auction { get; private set; }
    public BiddingSystem BiddingSystem { get; private set; }
    public string BiddingSystemPath { get;  set; } = @"..\..\..\..\BiddingBrowser\BiddingSystems\Wspólny Język.json";

    public PlayerPosition Position {  get; private set; }
    public PlayerRole Role { get; private set; }

    public List<HandEvaluation> PartnersHandHypotheses = new List<HandEvaluation>();
    public List<HandEvaluation> LeftOpponentsHandHypotheses = new List<HandEvaluation>();
    public List<HandEvaluation> RightOpponentsHandHypotheses = new List<HandEvaluation>();


    public BidEngine(Auction auction, PlayerPosition position) {
        Auction = auction;
        BiddingSystem = new BiddingSystem(BiddingSystemPath);   //hardcoded path for now
        Position = position;
        Role = PlayerRole.None;
    }
    public Bid Get(Hand hand, HandEvaluation partnersHand, HandEvaluation leftOpponentsHand, HandEvaluation rightOpponentsHand) {
        if (Role == PlayerRole.None) {
            Role = DetermineRole();
        }

        List<BidNode> possibleBids = new List<BidNode>();
        foreach (Root root in BiddingSystem.Roots) {
            FindNodesByHandRecursive(hand, possibleBids, root.Bids);
        }

        possibleBids = FindLegalBids(possibleBids);

        if (possibleBids.Count == 1) {
            return possibleBids[0].ToBid();
        }

        throw new NotImplementedException();
    }


    public PlayerRole DetermineRole() {
        PlayerPosition starter = (PlayerPosition)(((int)(Auction.CurrentBidder) - (Auction.AuctionHistory.Count % 4) + 4) % 4);

        for(int i = 0; i < Auction.AuctionHistory.Count; i++) {
            Bid bid = Auction.AuctionHistory[i];
            PlayerPosition bidder = (PlayerPosition)(((int)starter + i) % 4);

            if (((int)(Position) + (int)(bidder)) % 2 != 0) { //bidder is NOT me nor my partner
                continue;
            }

            if(bid.BidType == BidType.Submit) {
                if (Position == bidder) { //bidder is me
                    return PlayerRole.Opener;
                }
                else {    //bidder is my partner
                    return PlayerRole.Responder;
                }
            }
        }

        return PlayerRole.Opener;
    }

    public List<BidNode> FindLegalBids(List<BidNode> possibleBids) {
        List<BidNode> legalBids = new List<BidNode>();
        foreach (BidNode bidNode in possibleBids) {
            if (IsBidLegal(bidNode)) {
                legalBids.Add(bidNode);
            }
        }
        return legalBids;
    }

    public bool IsBidLegal(BidNode bidNode) {
        Bid? lastBid = GetLastSubmittedBid();
        
        if (lastBid == null) {
            return true;    //if there are no previous bids, any bid is legal
        }

        if (bidNode.Value > lastBid.Value) {
            return true;
        } 
        
        if (bidNode.Value == lastBid.Value && (int)bidNode.Color > (int)lastBid.Color) {
            return true;
        }

        return false;
    }

    public Bid? GetLastSubmittedBid() {
        for (int i = Auction.AuctionHistory.Count - 1; i >= 0; i--) {
            if (Auction.AuctionHistory[i].BidType == BidType.Submit) {
                return Auction.AuctionHistory[i];
            }
        }
        return null;
    }

    //TODO
    public void EvaluateHands(Bid bid, HandEvaluation partnersHand, HandEvaluation LeftOpponentsHand, HandEvaluation RightOpponentsHand) {
        List<BidNode> possibleBids = new List<BidNode>();
        foreach(Root root in BiddingSystem.Roots) {
            FindNodesByBidRecursive(bid, possibleBids, root.Bids);
        }

        //partnersHand.Evaluate(bidNode);
        //LeftOpponentsHand.Evaluate(bidNode);
        //RightOpponentsHand.Evaluate(bidNode);
    }

    public List<BidNode> FindNodesByBid(Bid bid, List<BidNode> bidNodes) {
        var foundNodes = new List<BidNode>();
        FindNodesByBidRecursive(bid, bidNodes, foundNodes);
        return foundNodes;
    }

    public void FindNodesByBidRecursive(Bid bid, List<BidNode> foundBidNodes, List<BidNode> bidNodes) {

        foreach (BidNode bidNode in bidNodes) {
            if (bidNode.Matches(bid)) {
                foundBidNodes.Add(bidNode);
            }

            if (bidNode.NextBids is not null) {
                FindNodesByBidRecursive(bid, foundBidNodes, bidNode.NextBids);
            }
        }
    }

    public List<BidNode> FindNodesByHand(Hand hand, List<BidNode> bidNodes) {
        var foundNodes = new List<BidNode>();
        FindNodesByHandRecursive(hand, bidNodes, foundNodes);
        return foundNodes;
    }

    public void FindNodesByHandRecursive(Hand hand, List<BidNode> foundBidNodes, List<BidNode> bidNodes) {

        foreach (BidNode bidNode in bidNodes) {
            if (bidNode.Matches(hand, Role)) {
                foundBidNodes.Add(bidNode);
            }

            if (bidNode.NextBids is not null) {
                FindNodesByHandRecursive(hand, foundBidNodes, bidNode.NextBids);
            }
        }
    }




}
