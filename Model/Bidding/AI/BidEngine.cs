using Model.Bidding.AI.Eval;
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

    public BiddingGoal PreviousGoal { get; private set; }


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


    public BiddingGoal DetermineGoal() {
        // Gdy w licytacji są same pasy - cel jeszcze nieokreślony.
        if (Auction.AuctionHistory.Count == 0 || Auction.AuctionHistory.All(e => e.Type == BidType.Pass)) {
            return BiddingGoal.Undefined;
        }

        var lastOwnBid = OwnBidsHistory.LastOrDefault();
        var lastPartnerBid = Auction.GetLastPlayerBid(PartnerPosition);

        if (PreviousGoal == BiddingGoal.Pass) {
            // Pewien obrót spraw może spowodować, że jednak coś zrobimy...
            // Gdy oponenci weszli za wysoko:
            // 1. PlayForPenalty (gdy mamy na kontrę)
            // 2. MinimizeLoss (gdy bardziej opłaca się wtopić)
        }

        if (PreviousGoal == BiddingGoal.SystemDefence) {
            // To zamienia się we wszystko.
        }


        // Gdy my otwieraliśmy, to dążymy do własnej gry.
        if (Auction.PlayerOpenedAuction(PartnerPosition) || Auction.PlayerOpenedAuction(Position)) {
           


            // Partner spasował na moją ostatnią odzywkę - to oznacza, że oponenci się wcieli!
            if (lastPartnerBid == null && lastOwnBid != null) {
                // Gdy wcieli się po odzywce robiącej partię, a naszym celem było jedynie zrobienie partii.
                if (lastOwnBid.MakesGame() && (PreviousGoal == BiddingGoal.Game || PreviousGoal == BiddingGoal.GameForcing)) {
                    // TODO: Policzy co się bardziej opłaca. Póki co stary cel.
                    return PreviousGoal;
                }

                // Gdy wcieli się po odzywce, która nie robi partii.
                if (!lastOwnBid.MakesGame()) {
                    if (PreviousGoal == BiddingGoal.GameForcing) {
                        return BiddingGoal.GameForcing;
                    }

                    if (PreviousGoal == BiddingGoal.Game) {
                        return BiddingGoal.PlayForPenalty;
                    }
                }
            }
        }

        return BiddingGoal.Undefined;
    }


    public Bid Get(Hand hand) {
        // Być może do usunięcia?
        if (Role == PlayerRole.None) {
            Role = DetermineRole();
        }

        // Wywal gałęzie niepasujące do twojej poprzedniej odzywki.
        var lastOwnBid = OwnBidsHistory.LastOrDefault();
        var lastPartnerBid = Auction.GetLastPlayerBid(PartnerPosition);
        var partnersPath = new Dictionary<BidNode, TableEvaluation>();
        var isFromSystem = true;

        // Jeżeli ja i partner licytowaliśmy - licytujemy dalej, czyli atakujemy.
        if (lastOwnBid != null && lastPartnerBid != null) {
            partnersPath = BiddingSystem
                .GetDescendants(lastOwnBid, lastPartnerBid)
                .ToDictionary(
                    e => e,
                    e => Evaluator.FromPartner(e, hand, Auction, Position)
                );

            // Partner mógł nie odzywać się według systemu. Wtedy nic nie pokaże się w śceiżkach partnera.
        }
        // Ja licytowałem, a partner pasował w poprzednim kółku.
        else if (lastOwnBid != null && lastPartnerBid == null) {
            // TODO - powtórzenie odzywki, przejście do obrony?
        }
        // Jeżeli ja nie licytowałem i partner otworzył licytację (zakładamy, że zawsze otworzył z systemu).
        else if (Auction.PlayerOpenedAuction(PartnerPosition) && lastPartnerBid != null) {
            partnersPath = BiddingSystem
                .GetDescendants(lastPartnerBid)
                .ToDictionary(
                    e => e,
                    e => Evaluator.FromPartner(e, hand, Auction, Position)
                );
        }
        // Jeżeli ja nie licytowałem i partner nie otworzył, bo otworzyli oponenci.
        else if (Auction.PlayerOpenedAuction(LeftOpponentPosition) || Auction.PlayerOpenedAuction(RightOpponentPosition)) {
            // TODO - ewaluacja stołu na podstawie odzywek oponentów.
        }
        // Nikt jeszcze nie licytował - wtedy nic.

        // Twój nic nie powiedział w ostatnim kółku lub spasował.
        if (partnersPath.Count == 0) {
            // Ale my coś już mówiliśmy
            BidNode? chosenBid = null;
            var tableEvaluation = Evaluator.FromOwnHand(hand);
            if (lastOwnBid != null) {
                chosenBid = ChooseBidByFreestyling(hand, tableEvaluation);
            }
            else {
                var openings = BiddingSystem.Openings() ?? throw new Exception("Openings not found");

                // Weź wszystko, co pasuje do ręki i jest legalne, i wybierz z tego systemową odzywkę.
                var bidCandidates = FindNodesByHand(hand, openings)
                    .Where(bid => IsBidLegal(bid))
                    .ToList();

                chosenBid = ChooseBidFromSystem(bidCandidates);
                if (chosenBid == null) {
                    chosenBid = ChooseBidByFreestyling(hand, tableEvaluation);
                    isFromSystem = false;
                }
            }

            Console.WriteLine("\t" + tableEvaluation.GetCombinedHandEvaluation(hand));

            if (chosenBid == null) {
                Role = PlayerRole.None;
                return BidNode.Pass().ToBid();
            }

            OwnBidsHistory.Add(chosenBid);
            return chosenBid.ToBid(isFromSystem);
        }

        // Dostosowanie gałęzi, żeby obejmowały tylko odzywki zgodne z tym, co mówiłem wcześniej.
        var ownBranches = lastOwnBid == null
            ? partnersPath
            : partnersPath.Where(e => e.Key.Parent == lastOwnBid);

        var chosenBids = new List<BidNode>();
        foreach (var branchHead in ownBranches) {
            // Weź wszystko, co pasuje do ręki i jest legalne, i wybierz z tego systemową odzywkę.
            var bidCandidates = FindNodesByHand(hand, branchHead.Key)
                .Where(bid => IsBidLegal(bid))
                .ToList();

            var chosenBid = ChooseBidFromSystem(bidCandidates);
            if (chosenBid == null) {
                chosenBid = ChooseBidByFreestyling(hand, branchHead.Value)?.AssertFreestyleIsntConfusing(branchHead.Key);
                isFromSystem = false;
            }

            if (chosenBid != null) {
                chosenBids.Add(chosenBid);
            }

            Console.WriteLine("\t" + branchHead.Value.GetCombinedHandEvaluation(hand));
        }

        if (chosenBids.Count == 0) {
            Role = PlayerRole.None;
            return BidNode.Pass().ToBid();
        }

        var firstChosenBid = chosenBids[0];
        if (!chosenBids.All(e => e.EqualsByColorAndValue(firstChosenBid))) {
            Console.WriteLine("Multiple tree branches possible: " + string.Join(", ", chosenBids.Distinct()));
            return BidNode.Pass().ToBid();
        }

        OwnBidsHistory.Add(firstChosenBid);
        return firstChosenBid.ToBid(isFromSystem);
    }


    public PlayerRole DetermineRole() {
        PlayerPosition starter = (PlayerPosition)(((int)(Auction.CurrentBidder) - (Auction.AuctionHistory.Count % 4) + 4) % 4);

        for (int i = 0; i < Auction.AuctionHistory.Count; i++) {
            Bid bid = Auction.AuctionHistory[i];
            PlayerPosition bidder = (PlayerPosition)(((int)starter + i) % 4);

            if (bidder != Position && bidder != PartnerPosition) { //bidder is NOT me nor my partner
                continue;
            }

            if (bid.Type == BidType.Submit) {
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


    public BidNode? ChooseBidByFreestyling(Hand hand, TableEvaluation tableEvaluation) {
        var combinedHandEvaluation = tableEvaluation.GetCombinedHandEvaluation(hand);

        if (combinedHandEvaluation.Points < 24) { // Pass it is...
            return null;
        }

        var bestFit = combinedHandEvaluation.FindFit();

        if (combinedHandEvaluation.Points >= 30) {
            // TODO: wielki szlem!k

            if (bestFit.Length >= 8) {
                return BidNode.Submit(6, bestFit.Color);
            }
            else {
                return BidNode.Submit(6, BidColor.NoTrump);
            }
        }

        // Końcówka w starszym
        if (combinedHandEvaluation.Points > 24 && bestFit.Length >= 8 && (bestFit.Color == BidColor.Spades || bestFit.Color == BidColor.Hearts)) {
            BidNode bidNode = BidNode.Submit(4, bestFit.Color);
            return IsBidLegal(bidNode) ? bidNode : null;
        }

        // Końcówka BA
        if (combinedHandEvaluation.Points > 25 && bestFit.Length < 9) { // bestFit.Length 8 na młodszym może być BA...
            BidNode bidNode = BidNode.Submit(3, BidColor.NoTrump);
            return IsBidLegal(bidNode) ? bidNode : null;
        }

        // Końcówka w młodszym
        if (combinedHandEvaluation.Points > 27 && bestFit.Length >= 8) { // Kolor już jest bez znaczenia, bo straszy był sprawdzony wcześniej 
            BidNode bidNode = BidNode.Submit(5, bestFit.Color);
            return IsBidLegal(bidNode) ? bidNode : null;
        }

        // TODO: invit?
        if (combinedHandEvaluation.Points.Lower > 20) {
            Bid? currentBid = Auction.GetLastSubmittedBid();
            if (currentBid == null) {
                return null;
            }

            int? submitValue = currentBid.Value; // nigdy nie będzie null
            if (submitValue == null) {
                throw new Exception("Submit with null value.");
            }

            // Mój kolor jest niższy lub równy niż obecny
            if ((int)bestFit.Color <= (int)currentBid.Color) {
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


    public bool IsOnMyTeam() {
        return Auction.CurrentBidder == Position || Auction.CurrentBidder == PartnerPosition;
    }


    public void EvaluateHands(Bid bid, HandEvaluation partnersHand, HandEvaluation leftOpponentsHand, HandEvaluation rightOpponentsHand) {
        // Partner coś powiedział
        if (Auction.CurrentBidder == PartnerPosition) {
            var lastOwnBid = OwnBidsHistory.LastOrDefault();

            var partnersPath = lastOwnBid == null
                ? BiddingSystem.GetDescendants(bid).ToList()
                : BiddingSystem.GetDescendants(lastOwnBid, bid).ToList();

            BidNode? partnersPreviousBidNode = partnersPath.Count == 1
                ? partnersPath[0]
                : null;

            if (bid.Type == BidType.Pass) {
                // TODO
                return;
            }

            if (bid.Type == BidType.Double) {
                // TODO
                return;
            }

            if (bid.Type == BidType.Redouble) {
                // TODO
                return;
            }

            // Wiele możliwości -> brak ewaluacij
            if (partnersPath.Count > 1) {
                return;
            }

            // Odzywka automatyczna - dokładnie wiadomo w jakiej gałęzi drzewa jesteśmy
            if (partnersPreviousBidNode?.AutomaticResponse == true) { // Może być null, jeżeli odzywka nie jest z systemu
                // Iteruje w tył po licytacji
                while (partnersPreviousBidNode != null) {
                    partnersHand.Evaluate(partnersPreviousBidNode);
                    partnersPreviousBidNode = partnersPreviousBidNode.Parent?.Parent;
                }

                return;
            }

            // Nie ma pewności, w której gałęzi jesteśmy
            // Jest tylko jeden BidNode pasujący do odzywki partnera (na obecnym poziomie licytacji)
            while (partnersPreviousBidNode != null) {

                partnersHand.Evaluate(partnersPreviousBidNode);

                // Przejdź do poprzedniego poziomu licytacji
                partnersPreviousBidNode = partnersPreviousBidNode.Parent?.Parent;
                if (partnersPreviousBidNode == null) {
                    return;
                }

                partnersPath = partnersPreviousBidNode.Parent == null
                    ? BiddingSystem.GetDescendants(partnersPreviousBidNode.ToBid()).ToList()
                    : BiddingSystem.GetDescendants(partnersPreviousBidNode.Parent, partnersPreviousBidNode.ToBid()).ToList();

                // Wiele BidNode'ów pasuje do odzywki partnera (na poprzednim pozimie licytacji)
                if (partnersPath.Count != 1) {
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
