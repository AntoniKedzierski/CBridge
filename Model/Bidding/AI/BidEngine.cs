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
    public List<List<BidNode>> PartnershipPossiblePaths { get; private set; } = new();
    public List<List<BidNode>> OpponentsPossiblePaths { get; private set; } = new();

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

        List<BidNode> possibleBids = FindNodesByHand(hand);

        possibleBids = FindLegalBids(possibleBids);

        if (possibleBids.Count == 1) {
            Bid chosenBid = possibleBids[0].ToBid();
            UpdatePossiblePaths(chosenBid);

            return chosenBid;
        }

        return new Bid {
            BidType = BidType.Pass,
            Color = BidColor.NoColor,
            Value = null
        };

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

    public List<BidNode> FindNodesByHand(Hand hand) {
        List<BidNode> foundBidNodes = new();

        List<BidNode> bidNodes = GetCurrentBidLevel(PartnershipPossiblePaths);

        foreach (BidNode bidNode in bidNodes) {
            if (bidNode.Matches(hand, Role)) {
                foundBidNodes.Add(bidNode);
            }
        }

        return foundBidNodes;
    }

    public List<BidNode> GetCurrentBidLevel(List<List<BidNode>> possiblePaths) {
        if (possiblePaths.Count == 0) {
            return BiddingSystem.Roots.SelectMany(root => root.Bids).ToList();
        }

        return possiblePaths.SelectMany(path => path[^1].NextBids).ToList();
    }

    public bool IsOnMyTeam() {
        return ((int)Position + (int)Auction.CurrentBidder) % 2 == 0;
    }

    public void UpdatePossiblePaths(Bid bid, List<List<BidNode>> possiblePaths) {
        if (bid.BidType == BidType.Pass) {
            return;
        }

        List<List<BidNode>> newPossiblePaths = new();

        if (possiblePaths.Count == 0) {
            List<BidNode> rootLevel = BiddingSystem.Roots
                .SelectMany(root => root.Bids)
                .ToList();

            foreach (BidNode bidNode in rootLevel) {
                if (bidNode.Matches(bid)) {
                    newPossiblePaths.Add(new List<BidNode> { bidNode });
                }
            }
        }
        else {
            foreach (List<BidNode> path in possiblePaths) {
                BidNode lastNode = path[^1];

                foreach (BidNode nextNode in lastNode.NextBids) {
                    if (nextNode.Matches(bid)) {
                        List<BidNode> extendedPath = new(path);
                        extendedPath.Add(nextNode);

                        newPossiblePaths.Add(extendedPath);
                    }
                }
            }
        }

        possiblePaths.Clear();
        possiblePaths.AddRange(newPossiblePaths);
    }

    public void UpdatePossiblePaths(Bid bid) {
        if (IsOnMyTeam()) {
            UpdatePossiblePaths(bid, PartnershipPossiblePaths);
        }
        else {
            UpdatePossiblePaths(bid, OpponentsPossiblePaths);
        }
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

        UpdatePossiblePaths(bid);

        //partnersHand.Evaluate(bidNode);
        //LeftOpponentsHand.Evaluate(bidNode);
        //RightOpponentsHand.Evaluate(bidNode);
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
