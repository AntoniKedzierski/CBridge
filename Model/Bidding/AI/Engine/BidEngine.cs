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
            result = PlayInOffence(hand);
            if (result != null) {
                result.AiSource = "Defence - PlayInOffence";
            }
            return result;
        }

        if (result != null) {
            result.AiSource = "PlayInDefence";
        }
        return result;
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
            var tableEvaluation = new Dictionary<BidNode, TableEvaluation>() {
                { partnerDefences, Evaluator.FromPartner(partnerDefences, hand, Auction, Position) }
            };
            result = GetNaturalBid(hand, tableEvaluation, oneRoundForce: true);

            if (result != null) {
                result.AiSource = "Defence - GetNaturalBid";
            }
            return result;
        }

        if (result != null) {
            result.AiSource = "PlayInDefence";
        }
        return result;
    }


    public BidNode? PlayInOffence(Hand hand) {
        var openings = BiddingSystem.Openings() ?? throw new Exception("Openings not found");
        var bidCandidates = FindNodesByHand(hand, openings).Where(e => e.IsBidLegal(Auction)).ToList();
        var chosenBid = ChooseBidFromSystem(bidCandidates, preferConventions: true);
        if (chosenBid != null) {
            chosenBid.AiSource = "PlayInOffence - opening";
        }
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
            if (systemBid != null) {
                systemBid.AiSource = "PlayInOffence - response";
            }
            return systemBid;
        }

        // Tutaj celem może być jedynie: Game, GameForcing, PremiumContract.
        var result = GetNaturalBid(hand, branches);
        if (result != null) {
            result.AiSource = "GetNaturalBid";
        }

        return result;
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


    private bool CanBidGame(HandEvaluation combinedHandEvaluation, BidColor color) {
        return color switch {
            BidColor.Clubs => combinedHandEvaluation.Clubs >= 8 && combinedHandEvaluation.Points >= 27,
            BidColor.Diamonds => combinedHandEvaluation.Diamonds >= 8 && combinedHandEvaluation.Points >= 27,
            BidColor.Hearts => combinedHandEvaluation.Hearts >= 8 && combinedHandEvaluation.Points >= 24,
            BidColor.Spades => combinedHandEvaluation.Spades >= 8 && combinedHandEvaluation.Points >= 24,
            BidColor.NoTrump => combinedHandEvaluation.Points >= 25,
            _ => false
        };
    }


    private BidNode SubmitGame(BidColor color) {
        return color switch {
            BidColor.Clubs => BidNode.Submit(5, BidColor.Clubs),
            BidColor.Diamonds => BidNode.Submit(5, BidColor.Diamonds),
            BidColor.Hearts => BidNode.Submit(4, BidColor.Hearts),
            BidColor.Spades => BidNode.Submit(4, BidColor.Spades),
            BidColor.NoTrump => BidNode.Submit(3, BidColor.NoTrump),
            _ => throw new Exception("Invalid color.")
        };
    }


    public BidNode? ChooseBidByFreestyling(Hand hand, TableEvaluation tableEvaluation) {
        var combinedHandEvaluation = tableEvaluation.GetCombinedHandEvaluation(hand);
        var bestFit = combinedHandEvaluation.FindFit();

        if (Goal == BiddingGoal.Gf) {
            var lastPartnersBid = Auction.GetLastPlayerBid(PartnerPosition)!;
            if (lastPartnersBid.MakesGame()) {
                // TODO: Wejście w grę premiową
                return null;
            }

            var canBidGame = CanBidGame(combinedHandEvaluation, bestFit.Color);
            if (canBidGame) {
                return SubmitGame(bestFit.Color);
            }

            // Inwit... (o jeden mniej, niż gra).
            var lastBid = Auction.GetLastSubmittedBid(onlySubmitions: true)!;

            // Mój kolor jest niższy lub równy niż obecny
            var submitValue = lastBid.Value!;
            if ((int)bestFit.Color <= (int)lastBid.Color) {
                submitValue = lastBid.Value + 1;
            }

            return BidNode.Submit(submitValue!.Value, bestFit.Color);
        }

        if (combinedHandEvaluation.Points < 24) { // Pass it is...
            return null;
        }

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
            return bidNode.IsBidLegal(Auction) ? bidNode : null;
        }

        // Końcówka BA
        if (combinedHandEvaluation.Points > 25 && bestFit.Length < 9) { // bestFit.Length 8 na młodszym może być BA...
            BidNode bidNode = BidNode.Submit(3, BidColor.NoTrump);
            return bidNode.IsBidLegal(Auction) ? bidNode : null;
        }

        // Końcówka w młodszym
        if (combinedHandEvaluation.Points > 27 && bestFit.Length >= 8) { // Kolor już jest bez znaczenia, bo straszy był sprawdzony wcześniej 
            BidNode bidNode = BidNode.Submit(5, bestFit.Color);
            return bidNode.IsBidLegal(Auction) ? bidNode : null;
        }

        // TODO: invit?
        if (combinedHandEvaluation.Points.Lower > 20) {
            Bid? currentBid = Auction.GetLastSubmittedBid(true);
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


    public BidNode BidBestColor(Hand hand) {
        var colorLengths = hand.Cards.GroupBy(e => e.Color).ToDictionary(e => e.Key, e => e?.Count() ?? 0);
        var longestColorCount = colorLengths.Max(e => e.Value);

        // Pierwszy najdłuższy kolor.
        var longestColor = colorLengths.First(e => e.Value == longestColorCount).Key;

        // TODO
        Bid currentBid = Auction.GetLastSubmittedBid(true)!;

        int? submitValue = currentBid.Value; // nigdy nie będzie null
        if (submitValue == null) {
            throw new Exception("Submit with null value.");
        }

        // Mój kolor jest niższy lub równy niż obecny
        if ((int)longestColor <= (int)currentBid.Color) {
            submitValue = currentBid.Value + 1;
        }

        return BidNode.Submit(submitValue.Value, longestColor);
    }

}
