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
    public string BiddingSystemPath { get; set; } = @"..\..\..\..\BiddingBrowser\BiddingSystems\Wspólny Język.json";

    public PlayerPosition Position { get; private set; }
    public PlayerPosition LeftOpponentPosition => (PlayerPosition)(((int)Position + 1) % 4);
    public PlayerPosition PartnerPosition => (PlayerPosition)(((int)Position + 2) % 4);
    public PlayerPosition RightOpponentPosition => (PlayerPosition)(((int)Position + 3) % 4);
    public PlayerRole Role { get; private set; }

    public List<BidNode> OwnBidsHistory { get; private set; } = [];


    public BidEngine(Auction auction, PlayerPosition position) {
        Auction = auction;
        BiddingSystem = new BiddingSystem(BiddingSystemPath);   //hardcoded path for now
        Position = position;
        Role = PlayerRole.None;
    }


    public void Reset() {
        Role = PlayerRole.None;
        OwnBidsHistory.Clear();
    }


    public Bid Get(Hand hand, HandEvaluation partnersHand, HandEvaluation leftOpponentsHand, HandEvaluation rightOpponentsHand) {
        if (Role == PlayerRole.None) {
            Role = DetermineRole();
        }

        // Wywal gałęzie niepasujące do twojej poprzedniej odzywki.
        var lastOwnBid = OwnBidsHistory.LastOrDefault();
        var lastPartnerBid = Auction.GetLastPlayerBid(PartnerPosition);
        var partnersPath = new List<BidNode>();

        // Konwersja fizycznej odzywki partnera (Bid) na jego gałęzie drzewa.
        // Jeżeli ja nic nie mówiłem, a partner mówił, to to są wszystkie jego ścieżki.
        if (lastPartnerBid != null && lastPartnerBid.BidType != BidType.Pass) {
            partnersPath = lastOwnBid == null
                ? BiddingSystem.GetDescendants(lastPartnerBid).ToList()
                : BiddingSystem.GetDescendants(lastOwnBid, lastPartnerBid).ToList();
        }

        // Twój nic nie powiedział w ostatnim kółku lub spasował.
        if (partnersPath.Count == 0) {
            // Ale my coś już mówiliśmy
            BidNode? chosenBid = null;
            if (lastOwnBid != null) {
                chosenBid = FindBestBid(hand, partnersHand);
            }
            else {
                var openings = BiddingSystem.Openings() ?? throw new Exception("Openings not found");

                // Weź wszystko, co pasuje do ręki i jest legalne, i wybierz z tego systemową odzywkę.
                var bidCandidates = FindNodesByHand(hand, openings)
                    .Where(bid => IsBidLegal(bid))
                    .ToList();

                chosenBid = ChooseBidFromSystem(bidCandidates) ?? FindBestBid(hand, partnersHand);
            }

            if (chosenBid == null) {
                Role = PlayerRole.None;
                return BidNode.Pass().ToBid();
            }

            OwnBidsHistory.Add(chosenBid);
            return chosenBid.ToBid();
        }

        // Dostosowanie gałęzi, żeby obejmowały tylko odzywki zgodne z tym, co mówiłem wcześniej.
        var ownBranches = lastOwnBid == null
            ? partnersPath
            : partnersPath.Where(e => e.Parent == lastOwnBid);

        var chosenBids = new List<BidNode>();
        foreach (var branchHead in ownBranches) {
            // Weź wszystko, co pasuje do ręki i jest legalne, i wybierz z tego systemową odzywkę.
            var bidCandidates = FindNodesByHand(hand, branchHead)
                .Where(bid => IsBidLegal(bid))
                .ToList();

            var chosenBid = ChooseBidFromSystem(bidCandidates) ?? FindBestBid(hand, partnersHand);
            if (chosenBid != null) {
                chosenBids.Add(chosenBid);
            }
        }

        if (chosenBids.Count == 0) {
            Role = PlayerRole.None;
            return BidNode.Pass().ToBid();
        }

        var firstChosenBid = chosenBids[0];
        if (!chosenBids.All(e => e.EqualsByColorAndValue(firstChosenBid))) {
            throw new Exception("Multiple tree branches possible.");
        }

        OwnBidsHistory.Add(firstChosenBid);
        return firstChosenBid.ToBid();
    }


    public PlayerRole DetermineRole() {
        PlayerPosition starter = (PlayerPosition)(((int)(Auction.CurrentBidder) - (Auction.AuctionHistory.Count % 4) + 4) % 4);

        for (int i = 0; i < Auction.AuctionHistory.Count; i++) {
            Bid bid = Auction.AuctionHistory[i];
            PlayerPosition bidder = (PlayerPosition)(((int)starter + i) % 4);

            if (bidder != Position && bidder != PartnerPosition) { //bidder is NOT me nor my partner
                continue;
            }

            if (bid.BidType == BidType.Submit) {
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

    public List<BidNode> FindNodesByHand(Hand hand, BidNode head) {
        List<BidNode> foundBidNodes = new();
        foreach (BidNode bidNode in head.NextBids) {
            if (bidNode.Matches(hand, Role)) {
                foundBidNodes.Add(bidNode);
            }
        }

        return foundBidNodes;
    }


    public List<BidNode> FindNodesByHand(Hand hand, Root root) {
        List<BidNode> foundBidNodes = new();
        foreach (BidNode bidNode in root.Bids) {
            if (bidNode.Matches(hand, Role)) {
                foundBidNodes.Add(bidNode);
            }
        }

        return foundBidNodes;
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
        Bid? lastBid = Auction.GetLastSubmittedBid();

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

    public BidNode? ChooseBidFromSystem(List<BidNode> legalBids) {
        if (legalBids.Count == 0) {
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
            else if (bid.Value == lowestBid.Value && bid.Color < lowestBid.Color) {
                lowestBid = bid;
            }
        }

        return lowestBid;
    }


    public BidNode? FindBestBid(Hand hand, HandEvaluation partnersHand) {

        int minPairPoints = hand.Points + (partnersHand.Points.Lower ?? 0);
        int maxPairPoints = hand.Points + (partnersHand.Points.Upper ?? 0);
        int acesInPair = hand.OfValue(CardValue.Ace).Count() + (partnersHand.Aces ?? 0);
        int kingsInPair = hand.OfValue(CardValue.King).Count() + (partnersHand.Kings ?? 0);

        if (maxPairPoints < 24) { // Pass it is...
            return null;
        }

        SuitFit bestFit = FindFit(hand, partnersHand);

        if (minPairPoints >= 30) {
            // TODO: wielki szlem!

            if(bestFit.Length >= 8) {
                return BidNode.Submit(6, bestFit.Color);
            }
            else {
                return BidNode.Submit(6, BidColor.NoTrump);
            }
        }

        // Końcówka w starszym
        if (minPairPoints > 24 && bestFit.Length >= 8 && (bestFit.Color == BidColor.Spades || bestFit.Color == BidColor.Hearts)) {
            BidNode bidNode = BidNode.Submit(4, bestFit.Color);

            if (IsBidLegal(bidNode)) {
                return bidNode;
            }
        }

        // Końcówka BA
        if (minPairPoints > 25 && bestFit.Length < 9) { // bestFit.Length 8 na młodszym może być BA...
            BidNode bidNode = BidNode.Submit(3, BidColor.NoTrump);

            if (IsBidLegal(bidNode)) {
                return bidNode;
            }
        }

        // Końcówka w młodszym
        if (minPairPoints > 27 && bestFit.Length >= 8) { // Kolor już jest bez znaczenia, bo straszy był sprawdzony wcześniej 
            BidNode bidNode = BidNode.Submit(5, bestFit.Color);

            if (IsBidLegal(bidNode)) {
                return bidNode;
            }
        }

        // TODO: invit?
        if (minPairPoints > 20 && maxPairPoints >= 24) {
            Bid? currentBid = Auction.GetLastSubmittedBid();
            if(currentBid == null) {
                return null;
            }

            int? submitValue = currentBid.Value; // nigdy nie będzie null
            if (submitValue == null) {
                throw new Exception("Submit with null value.");
            }

            // Mój kolor jest niższy lub równy niż obecny
            if((int)bestFit.Color <= (int)currentBid.Color) {
                submitValue = currentBid.Value + 1;
            }

            if (bestFit.Length >= 8) { 
                return BidNode.Submit(submitValue.Value, bestFit.Color);
            }
            else {
                return BidNode.Submit(submitValue.Value, BidColor.NoTrump);
            }
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


    public void EvaluateHands(Bid bid, HandEvaluation PartnersHand, HandEvaluation LeftOpponentsHand, HandEvaluation RightOpponentsHand) {
        // Partner coś powiedział
        if (Auction.CurrentBidder == PartnerPosition) {
            var lastOwnBid = OwnBidsHistory.LastOrDefault();

            var partnersPath = lastOwnBid == null
                ? BiddingSystem.GetDescendants(bid).ToList()
                : BiddingSystem.GetDescendants(lastOwnBid, bid).ToList();

            BidNode? partnersPreviousBidNode = partnersPath.Count == 1
                ? partnersPath[0]
                : null;

            if(bid.BidType == BidType.Pass) {
                // TODO
                return;
            }

            if (bid.BidType == BidType.Double) {
                // TODO
                return;
            }

            if (bid.BidType == BidType.Redouble) {
                // TODO
                return;
            }

            // Wiele możliwości -> brak ewaluacij
            if(partnersPath.Count > 1) {
                return;
            }

            // Odzywka automatyczna - dokładnie wiadomo w jakiej gałęzi drzewa jesteśmy
            if (partnersPreviousBidNode.AutomaticResponse) {
                // Iteruje w tył po licytacji
                while (partnersPreviousBidNode != null) {
                    PartnersHand.Evaluate(partnersPreviousBidNode);
                    partnersPreviousBidNode = partnersPreviousBidNode.Parent?.Parent;
                }

                return;
            }

            // Nie ma pewności, w której gałęzi jesteśmy
            // Jest tylko jeden BidNode pasujący do odzywki partnera (na obecnym poziomie licytacji)
            while (partnersPreviousBidNode != null) {

                PartnersHand.Evaluate(partnersPreviousBidNode);

                // Przejdź do poprzedniego poziomu licytacji
                partnersPreviousBidNode = partnersPreviousBidNode.Parent?.Parent;
                if(partnersPreviousBidNode == null) {
                    return;
                }

                    partnersPath = partnersPreviousBidNode.Parent == null
                    ? BiddingSystem.GetDescendants(partnersPreviousBidNode.ToBid()).ToList()
                    : BiddingSystem.GetDescendants(partnersPreviousBidNode.Parent, partnersPreviousBidNode.ToBid()).ToList();

                // Wiele BidNode'ów pasuje do odzywki partnera (na poprzednim pozimie licytacji)
                if (partnersPath.Count() != 1) {
                    return;
                }

            }

            // Odzywka spoza wczytanego systemu
            // min PC > 20 && max PC >= 24
            // bid w kolor -> feat 8+, bid w BA -> feat < 8


        }
        // Oponenci coś powiedzieli
        else {

        }
            
    }

}
