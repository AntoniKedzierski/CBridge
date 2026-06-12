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
        BiddingSystem = new BiddingSystem(BiddingSystemPath);   // hardcoded path for now
        Position = position;
        Role = PlayerRole.None;
    }


    public void Reset() {
        Role = PlayerRole.None;
        OwnBidsHistory.Clear();
    }




    public BiddingGoal DetermineGoal(Hand hand) {
        // Pierwsze określenie celu, po pierwszym okrążeniu licytacji.
        if (PreviousGoal == BiddingGoal.Undefined) {
            var ourSequence = Auction.GetPlayersSequence(Position, out var openingPlayer);
            var opponentsSequence = Auction.GetPlayersSequence(LeftOpponentPosition, out var openingOpponent);

            // Zliczanie pasów? Chyba nienajgorsza metoda...
            var oursPassCount = ourSequence.Count(e => e.Type == BidType.Pass);
            var theirsPassCount = opponentsSequence.Count(e => e.Type == BidType.Pass);

            if (oursPassCount == theirsPassCount) {
                return BiddingGoal.Undefined;
            }

            // Finalnie, ten kto mniej pasował, ma grać.
            return theirsPassCount < oursPassCount ? BiddingGoal.Pass : BiddingGoal.Game;
        }

        // Gdy wcześniej mieliśmy pasować, a przeciwnicy jeszcze nie doszli do partii.
        if (PreviousGoal == BiddingGoal.Pass) {
            if (!Auction.ReachedGameLevel()) {
                return BiddingGoal.Pass;
            }

            // Jeżeli doszli do partii, to przeliczamy, czy opłaca się samemu zgłaszać kontrakt w celu zminimalizowania strat.
            return BiddingGoal.MinimizeLoss;
        }

        return PreviousGoal;
    }


    // TODO
    public BidNode? PlayInDefence(Hand hand, Bid lastOpponentBid) {
        // Gałąź z konwencjami obronnymi na ostatnią odzywkę przeciwników.
        var defences = BiddingSystem.Defences() ?? throw new Exception("Defences not found.");
        var defenceBranch = defences.Bids.FirstOrDefault(e => e.EqualsByColorAndValue(lastOpponentBid.Value, lastOpponentBid.Color));
        return null;
    }


    public BidNode? PlayInOffence(Hand hand) {
        var openings = BiddingSystem.Openings() ?? throw new Exception("Openings not found");
        var bidCandidates = FindNodesByHand(hand, openings).ToList();
        var chosenBid = ChooseBidFromSystem(bidCandidates);
        return chosenBid;
    }


    public BidNode? PlayInOffence(Hand hand, Bid lastPartnerBid, BidNode? lastOwnBid = null, bool elevateSystem = false) {
        var descendants = lastOwnBid == null
            ? BiddingSystem.GetDescendants(lastPartnerBid)
            : BiddingSystem.GetDescendants(lastOwnBid, lastPartnerBid);

        var branches = descendants
            .ToDictionary(
                e => e,
                e => Evaluator.FromPartner(e, hand, Auction, Position)
            );

        return GetBidFromBranches(hand, branches);
    }


    private BidNode? GetBidFromBranches(Hand hand, Dictionary<BidNode, TableEvaluation> branches) {
        var chosenBids = new List<BidNode>();
        foreach (var branchHead in branches) {
            // Weź wszystko, co pasuje do ręki i jest legalne, i wybierz z tego systemową odzywkę.
            var bidCandidates = FindNodesByHand(hand, branchHead.Key)
                .Where(IsBidLegal)
                .ToList();

            var chosenBid = ChooseBidFromSystem(bidCandidates);
            if (chosenBid == null) {
                chosenBid = ChooseBidByFreestyling(hand, branchHead.Value)?.AssertFreestyleIsntConfusing(branchHead.Key);
            }

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


    public Bid Get(Hand hand) {
        var selectedBidNode = SelectOptimalBid(hand);
        if (selectedBidNode == null) {
            return Bid.Pass();
        }

        OwnBidsHistory.Add(selectedBidNode);
        return selectedBidNode.ToBid();
    }


    private BidNode? SelectOptimalBid(Hand hand) {
        // Zagranie systemem:
        //  1. W pierwszym kółku licytacji, zależnie od tego, kto się odzywał:
        //      1.1. NIKT NIC NIE MÓWIŁ -> Próbujemy otworzyć z systemem.
        //      1.2. PARTNER PASOWAŁ -> Sprawdzamy konwencje obronne, potem sprawdzamy system pod kątem otwarcia.
        //      1.3. PARTNER NIE PASOWAŁ
        //          1.3.1. OPONENCI SIĘ NIE WCIELI -> Odpowiadamy systemem.
        //          1.3.2. OPONENCI SIĘ WCIELI -> Sprawdzamy konwencje obronne, potem sprawdzamy system, potem sprawdzamy podniesiony system, potem licytację naturalną.
        //  2. W kolejnych kółkach zaczynamy od określenia celu.
        //      2.1. Pass -> pasujemy.
        //      2.2. SystemDefence -> szukamy komunikacji odnośnie wistu w konwencjach obronnych (dwukolorówki Michaelsa). Nie ma tu kontry wywoławczej ani wejścia kolorem przeciwników (bo to konwencje pierwszego kółka).
        //      2.3. Game -> licytacja systemem, dążąca do partii.
        //      2.4. GameForcing -> licytacja systemem, która nie może zakończyć się przed zrobieniem partii.
        //      2.5. PremiumContract -> licytacja konwencjami szlemowymi w celu osiągnięcia kontraktu premiowego.
        //      2.6. MinimizeLoss -> TODO
        //      2.7. PlayForPenalty -> TODO.

        // Najpierw ten po prawej, potem po lewej.
        var lastOpponentsBid = Auction.GetLastPlayerBid(RightOpponentPosition, passAsNull: true) ?? Auction.GetLastPlayerBid(LeftOpponentPosition, passAsNull: true);
        var lastPartnersBid = Auction.GetLastPlayerBid(PartnerPosition, passAsNull: true);

        // Osobne traktowanie licytacji w pierwszym kółku.
        if (Auction.Loop == 0) {
            // Oponenci się nie wcinali
            if (lastOpponentsBid == null) {
                return lastPartnersBid == null ? PlayInOffence(hand) : PlayInOffence(hand, lastPartnersBid);
            }

            // Oponenci się wcinali, partner nic nie mówił
            if (lastPartnersBid == null) {
                return PlayInDefence(hand, lastOpponentsBid);
            }

            // Oponenci i partner coś mówili!
            return PlayInDefence(hand, lastOpponentsBid) ?? PlayInOffence(hand, lastPartnersBid, elevateSystem: true);
        }

        // Tutaj licytacja na pewno trwała dłużej niż jedno kółko.
        var goal = DetermineGoal(hand);

        // Sprawdzamy drzewka obronne, na wszelki wypadek.
        if (goal == BiddingGoal.Pass) {
            return PlayInDefence(hand, lastOpponentsBid!);
        }

        // TODO
        if (goal == BiddingGoal.MinimizeLoss || goal == BiddingGoal.PlayForPenalty) {
            return null;
        }

        // TODO
        if (goal == BiddingGoal.PremiumContract) {
            return null;
        }

        // Wszystko inne wymaga ofensywnego grania systemem lub naturalnie.
        // Gdy partner ostatnio spasował, a doszło do nas, to znaczy, że musimy odpowiedzieć oponentom, którzy się wcieli.
        if (lastPartnersBid == null) {
            // TODO
            return null;
        }

        // Odpowiedź wg systemu na odzywkę partnera.
        var lastOwnBid = OwnBidsHistory.LastOrDefault();
        return PlayInOffence(hand, lastPartnersBid, lastOwnBid);
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


    public static BidNode? ChooseBidFromSystem(List<BidNode> legalBids) {
        if (legalBids.Count == 0) {
            return null;
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
