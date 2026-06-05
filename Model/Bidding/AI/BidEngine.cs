using Model.Bidding.Bids;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
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
    public PlayerPosition LeftOpponentPosition => (PlayerPosition)(((int)Position + 1) % 4);
    public PlayerPosition PartnerPosition => (PlayerPosition)(((int)Position + 2) % 4);
    public PlayerPosition RightOpponentPosition => (PlayerPosition)(((int)Position + 3) % 4);
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

        List<BidNode> possibleBidNodes = FindNodesByHand(hand);

        possibleBidNodes = FindLegalBids(possibleBidNodes);

        BidNode? chosenBidNode = ChooseBidFromSystem(possibleBidNodes, hand, partnersHand, leftOpponentsHand, rightOpponentsHand);

        if (chosenBidNode == null) {
            chosenBidNode = FindBestBid(hand, partnersHand, leftOpponentsHand, rightOpponentsHand);

        }

        if (chosenBidNode != null) {
            UpdatePossiblePaths(chosenBidNode);
            return chosenBidNode.ToBid();
        }

        Role = PlayerRole.None; // Didnt submit, isnt an opener

        return BidNode.Pass().ToBid();
    }


    public PlayerRole DetermineRole() {
        PlayerPosition starter = (PlayerPosition)(((int)(Auction.CurrentBidder) - (Auction.AuctionHistory.Count % 4) + 4) % 4);

        for(int i = 0; i < Auction.AuctionHistory.Count; i++) {
            Bid bid = Auction.AuctionHistory[i];
            PlayerPosition bidder = (PlayerPosition)(((int)starter + i) % 4);

            if (bidder != Position && bidder != PartnerPosition) { //bidder is NOT me nor my partner
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

    public BidNode? ChooseBidFromSystem(List<BidNode> legalBids, Hand hand, HandEvaluation partnersHand, HandEvaluation leftOpponentsHand, HandEvaluation rightOpponentsHand) {
        if(legalBids.Count == 0) {
            return null;
        }

        List<BidNode> conventionBids = new();

        foreach (BidNode bid in legalBids) {
            if (!string.IsNullOrWhiteSpace(bid.Convention)) {
                conventionBids.Add(bid);
            }
        }

        List<BidNode> bidsToChoose;

        if (conventionBids.Count == 1) {
            return conventionBids[0];
        }
        else if (conventionBids.Count > 1) { // Covention bids are preferred
            bidsToChoose = conventionBids;
        }
        else {
            bidsToChoose = legalBids;
        }

        BidNode lowestBid = bidsToChoose[0];

        foreach (BidNode bid in bidsToChoose) { // Lower bids are preferred
            if (bid.Value < lowestBid.Value) {
                lowestBid = bid;
            }
            else if (bid.Value == lowestBid.Value &&
                     bid.Color < lowestBid.Color) {
                lowestBid = bid;
            }
        }

        return lowestBid;


    }


    public BidNode? FindBestBid(Hand hand, HandEvaluation partnersHand, HandEvaluation leftOpponentsHand, HandEvaluation rightOpponentsHand) {

        int minPairPoints = hand.Points + (partnersHand.Points.Lower ?? 0);
        int maxPairPoints = hand.Points + (partnersHand.Points.Upper ?? 0);

        if (maxPairPoints < 24) { // Pass it is...
            return null;
        }
        
        SuitFit bestFit = FindFit(hand, partnersHand);

        if (minPairPoints >= 30) {
            // TODO: slam bidding
        }

        // Ending in major suit
        if(minPairPoints > 24 && bestFit.Length >= 8 && (bestFit.Color == BidColor.Spades || bestFit.Color == BidColor.Hearts)) {
            BidNode bidNode = BidNode.Submit(4, bestFit.Color);

            if (IsBidLegal(bidNode)) {
                return bidNode;
            }
        }

        // Ending in no trump
        if (minPairPoints > 25 && bestFit.Length < 9) { // Not sure about bestFit.Length reqiurement
            BidNode bidNode = BidNode.Submit(3, BidColor.NoTrump);

            if (IsBidLegal(bidNode)) {
                return bidNode;
            }
        }

        // Ending in minor suit
        if (minPairPoints > 27 && bestFit.Length >= 8) { // Color doenst matter
            BidNode bidNode = BidNode.Submit(5, bestFit.Color);

            if (IsBidLegal(bidNode)) {
                return bidNode;
            }
        }

        if (maxPairPoints >= 24) {
            // TODO: invitational bidding
        }

        return null;

    }

    public SuitFit FindFit(Hand hand, HandEvaluation partnersHand) {

        SuitFit bestFit = new SuitFit {
            Color = BidColor.NoColor,
            Length = 0
        };

        foreach (CardColor color in Enum.GetValues(typeof(CardColor))) {

            int pairSuitLength = hand.OfColor(color).Count() + (partnersHand.GetSuit(color).Lower ?? 0);

            if (pairSuitLength >= bestFit.Length) { // Major colors are preferred
                bestFit.Color = (BidColor)((int)color + 1); // CardColor doesnt have NoColor, while BidColor does
                bestFit.Length = pairSuitLength;
            }
        }
        return bestFit;
    }

    public bool IsOnMyTeam() {
        return Auction.CurrentBidder == Position || Auction.CurrentBidder == PartnerPosition;
    }

    public void UpdatePossiblePaths(Bid bid, List<List<BidNode>> possiblePaths) {
        if (bid.BidType == BidType.Pass) {
            return;
        }

        List<List<BidNode>> newPossiblePaths = new();

        if (possiblePaths.Count == 0) {
            List<BidNode> rootLevel = BiddingSystem.Roots.SelectMany(root => root.Bids).ToList();

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

    public void UpdatePossiblePaths(BidNode bidNode, List<List<BidNode>> possiblePaths) {
        if (bidNode.Type == BidType.Pass) {
            return;
        }

        List<List<BidNode>> newPossiblePaths = new();

        if (possiblePaths.Count == 0) {
            newPossiblePaths.Add(new List<BidNode> { bidNode });
        }
        else {
            foreach (List<BidNode> path in possiblePaths) {
                BidNode lastNode = path[^1];

                if(lastNode.NextBids.Contains(bidNode)) {
                    List<BidNode> extendedPath = new(path);
                    extendedPath.Add(bidNode);
                    newPossiblePaths.Add(extendedPath);
                }
            }
        }

        possiblePaths.Clear();
        possiblePaths.AddRange(newPossiblePaths);
    }

    public void UpdatePossiblePaths(BidNode bidNode) {
        if (IsOnMyTeam()) {
            UpdatePossiblePaths(bidNode, PartnershipPossiblePaths);
        }
        else {
            UpdatePossiblePaths(bidNode, OpponentsPossiblePaths);
        }
    }


    //TODO: when to evaluate who?, pass means oposite range of sum of all submits ranges (currently pass means nothing), what if there r more possibilities? -> PartnershipPossiblePaths.Count > 1, 
    public void EvaluateHands(Bid bid, HandEvaluation partnersHand, HandEvaluation LeftOpponentsHand, HandEvaluation RightOpponentsHand) {
        
        UpdatePossiblePaths(bid);

        if (PartnershipPossiblePaths.Count == 1 && Auction.CurrentBidder == PartnerPosition) {
            List<BidNode> path = PartnershipPossiblePaths[0];
            int startIndex = (Role == PlayerRole.Opener) ? 1 : 0;

            for (int i = startIndex; i < path.Count; i += 2) {
                partnersHand.Evaluate(path[i]);
            }
        }

        // How to determine which opponent made the bid?
        if (OpponentsPossiblePaths.Count == 1 && (Auction.CurrentBidder == LeftOpponentPosition || Auction.CurrentBidder == RightOpponentPosition)) {
            BidNode lastNode = OpponentsPossiblePaths[0][^1];
            LeftOpponentsHand.Evaluate(lastNode);
            RightOpponentsHand.Evaluate(lastNode);
        }
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
