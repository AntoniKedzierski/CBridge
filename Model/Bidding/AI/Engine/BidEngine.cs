using Model.Bidding.AI.Eval;
using Model.Bidding.Bids;
using Model.Enums;
using Model.Helpers;

namespace Model.Bidding.AI.Engine;

public partial class BidEngine : IBidInput {

    public Auction Auction { get; private set; }

    public BiddingSystem BiddingSystem { get; private set; }

    public string BiddingSystemPath { get; set; } = @"..\..\..\..\BiddingBrowser\BiddingSystems\Wspólny Język.json";

    public PlayerPosition Position { get; private set; }

    public PlayerPosition LeftOpponentPosition => Position.GetLeftOpponent();

    public PlayerPosition PartnerPosition => Position.GetPartner();

    public PlayerPosition RightOpponentPosition => Position.GetRightOpponent();

    public List<BidNode> OwnBidsHistory { get; private set; } = [];

    public BidNode? LastOwnBid => OwnBidsHistory.LastOrDefault();

    public Bid? LastOpponentBid => Auction.GetLastPlayerBid(RightOpponentPosition, passAsNull: true) ?? Auction.GetLastPlayerBid(LeftOpponentPosition, passAsNull: true);

    public Bid? LastRightOpponentBid => Auction.GetLastPlayerBid(RightOpponentPosition, passAsNull: true);

    public BiddingGoal Goal { get; private set; }

    public bool PartnerOpened { get; private set; } = false;


    public BidEngine(Auction auction, PlayerPosition position) {
        Auction = auction;
        BiddingSystem = new BiddingSystem(BiddingSystemPath);   // hardcoded path for now
        Position = position;
        Goal = BiddingGoal.None;
    }


    public void Reset() {
        OwnBidsHistory.Clear();
        Goal = BiddingGoal.None;
    }


    public void DetermineGoal() {
        // Pierwsze określenie celu, po pierwszym okrążeniu licytacji.
        if (Goal == BiddingGoal.None) {
            var ourSequence = Auction.GetPlayersSequence(Position, out var openingPlayer);
            var opponentsSequence = Auction.GetPlayersSequence(LeftOpponentPosition, out var openingOpponent);

            // Zliczanie pasów? Chyba nienajgorsza metoda...
            var oursPassCount = ourSequence.Count(e => e.Type == BidType.Pass);
            var theirsPassCount = opponentsSequence.Count(e => e.Type == BidType.Pass);

            if (oursPassCount == theirsPassCount) {
                Goal = BiddingGoal.None;
                return;
            }

            // Finalnie, ten kto mniej pasował, ma grać.
            Goal = theirsPassCount < oursPassCount ? BiddingGoal.Pass : BiddingGoal.Game;
            return;
        }

        // Gdy wcześniej mieliśmy pasować, a przeciwnicy jeszcze nie doszli do partii.
        if (Goal == BiddingGoal.Pass) {
            if (!Auction.ReachedGameLevel()) {
                Goal = BiddingGoal.Pass;
                return;
            }

            // Jeżeli doszli do partii, to przeliczamy, czy opłaca się samemu zgłaszać kontrakt w celu zminimalizowania strat.
            Goal = BiddingGoal.MinLoss;
        }

        // Goal pozostaje niezmieniony.
    }


    // TODO
    public BidNode? PlayInDefence(Hand hand, Bid lastOpponentBid) {
        // Gałąź z konwencjami obronnymi na ostatnią odzywkę przeciwników.
        var defences = BiddingSystem.Defences() ?? throw new Exception("Defences not found.");
        var defenceBranch = defences.Bids.FirstOrDefault(e => e.EqualsByColorAndValue(lastOpponentBid));

        if (defenceBranch == null) {
            return null;
        }

        var result = GetBidFromSystemBranches(hand, defenceBranch);
        if (result?.GoToOpenings == true) {
            return PlayInOffence(hand);
        }

        // Próbujemy otwarcia naturalnego
        return result ?? TrueNaturalOpening(hand);
    }


    public BidNode? PlayInDefence(Hand hand, Bid lastOpponentBid, Bid lastPartnerBid) {
        var defences = BiddingSystem.Defences() ?? throw new Exception("Defences not found.");
        var defenceBranch = defences.Bids.FirstOrDefault(e => e.EqualsByColorAndValue(lastOpponentBid));

        if (defenceBranch == null) {
            return null;
        }

        var partnerDefences = defenceBranch.NextBids.FirstOrDefault(e => e.EqualsByColorAndValue(lastPartnerBid));
        if (partnerDefences == null) {
            return null;
        }

        var result = GetBidFromSystemBranches(hand, partnerDefences);
        if (result == null && partnerDefences.OneRoundForcing) {
            result = TrueNaturalResponse(hand, partnerDefences, Evaluator.FromPartner(partnerDefences, hand, Auction, Position));
        }

        return result;
    }


    public BidNode? PlayInOffence(Hand hand) {
        var openings = BiddingSystem.Openings() ?? throw new Exception("Openings not found");
        var bidCandidates = FindNodesByHand(hand, openings).Where(e => e.IsBidLegal(Auction)).ToList();
        var chosenBid = ChooseBidFromSystem(bidCandidates, preferConventions: true);
        return chosenBid;
    }


    public BidNode? PlayInOffence(Hand hand, Bid lastPartnerBid, BidNode? lastOwnBid = null, bool elevateSystem = false) {
        var descendants = lastOwnBid == null
            ? BiddingSystem.GetOpenings(lastPartnerBid)
            : BiddingSystem.GetDescendants(lastOwnBid, lastPartnerBid);

        var branches = descendants
            .ToDictionary(
                e => e,
                e => Evaluator.FromPartner(e, hand, Auction, Position)
            );

        // Potencjalne przeście na GF
        var gameForcing = branches.Keys.All(e => e.IsGameForcing());
        var anyNotGameForcing = branches.Keys.Any(e => !e.IsGameForcing());

        if (gameForcing) {
            Goal = BiddingGoal.Gf;
        }

        var systemBid = GetBidFromSystemBranches(hand, branches.Keys);
        if (systemBid != null || Goal == BiddingGoal.None) {
            return systemBid;
        }

        // Tutaj celem może być jedynie: Game, GameForcing, PremiumContract.
        return GetNaturalBid(hand, branches);
    }


    public Bid Get(Hand hand) {
        var selectedBidNode = SelectOptimalBid(hand);
        Console.Write($"{Position}: {selectedBidNode}");

        var partnerBid = Auction.GetLastPlayerBid(PartnerPosition, passAsNull: true);

        if (selectedBidNode?.IsBidLegal(Auction) == false) {
            Console.WriteLine("Illegal bid.");
        }

        if (selectedBidNode == null) {
            Console.WriteLine();
            return Bid.Pass();
        }

        OwnBidsHistory.Add(selectedBidNode);
        Console.WriteLine();
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
            PartnerOpened = true;
            return PlayInDefence(hand, lastOpponentsBid, lastPartnersBid) ?? PlayInOffence(hand, lastPartnersBid);
        }

        // Tutaj licytacja na pewno trwała dłużej niż jedno kółko.
        DetermineGoal();

        // Sprawdzamy drzewka obronne, na wszelki wypadek (szczególnie pod kątem dwukolorówek Michaelsa).
        if (Goal == BiddingGoal.Pass && lastOpponentsBid != null) {
            return PlayInDefence(hand, lastOpponentsBid);
        }

        // TODO
        if (Goal == BiddingGoal.MinLoss || Goal == BiddingGoal.Penalty) {
            return null;
        }

        // TODO
        if (Goal == BiddingGoal.Premium) {
            return null;
        }

        // Wszystko inne wymaga ofensywnego grania systemem lub naturalnie.
        // Gdy partner ostatnio spasował, a doszło do nas, to znaczy, że musimy odpowiedzieć oponentom, którzy się wcieli.
        if (lastPartnersBid == null) {
            // TODO
            // 1. Czy nie opłaca się im dać kontry karnej?
            // 2. Czy opłaca się ponownie zgłaszać ustalony kontrakt?
            return null;
        }

        // Odpowiedź wg systemu na odzywkę partnera.
        var lastOwnBid = OwnBidsHistory.LastOrDefault();

        // Jeżeli w drugim kółku wciąż nie wiadomo, kto jest grającym, to szukamy najpierw odpowiedzi w drzewku obron.
        if (Goal == BiddingGoal.None && lastOpponentsBid != null && lastPartnersBid != null) {
            return PlayInDefence(hand, lastOpponentsBid, lastPartnersBid) ?? PlayInOffence(hand, lastPartnersBid, lastOwnBid);
        }

        var bidSequence = Auction.GetBidSequence().ToArray();
        var lastOpponentSubmition = Auction.GetLastSubmittedBid(RightOpponentPosition) ?? Auction.GetLastSubmittedBid(LeftOpponentPosition);

        var result = PlayInOffence(hand, lastPartnersBid, lastOwnBid);
        if (result == null && lastOpponentSubmition != null) {
            var defenceResult = PlayInDefence(hand, lastOpponentSubmition, lastPartnersBid);
            if (defenceResult != null) {
                Goal = BiddingGoal.Game;
                result = defenceResult;
            }
        }

        if (bidSequence.Length >= 2 && bidSequence.Last().Type == BidType.Double && lastPartnersBid.Type == BidType.Double && result == null && bidSequence.First().Color != BidColor.Diamonds) {
            Console.WriteLine("Coś się zjebało.");
        }

        if (result != null) {
            result.RealizedGoal = Goal;
        }
        return result;
    }


}
