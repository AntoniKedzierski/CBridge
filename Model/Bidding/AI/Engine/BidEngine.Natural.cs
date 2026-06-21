using Model.Bidding.AI.Eval;
using Model.Bidding.Bids;
using Model.Enums;
using Model.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.AI.Engine;

public partial class BidEngine {

    /// <summary>
    /// Otwarcie, tylko w pierwszym kółku.
    /// </summary>
    private BidNode TrueNaturalOpening(Hand hand) {
        var longestColor = hand.GetLongestColor();
        var strongestColors = hand.GetStrongestColors();

        // Piątkę zawsze zgłaszamy.
        if (hand.Points >= 12 && hand.Points <= 17 && hand.CountCards(longestColor) >= 5) {
            // Chyba, że jest młodsza, bez figur.
            if (longestColor.IsMajor() || hand.PointsCount(longestColor) > 0) {
                return BidNode.SubmitLowest(Auction, longestColor.ToBidColor(), 2);
            }
        }

        // Zgłaszanie BA
        if (hand.PointsNt >= 15 && hand.PointsNt <= 17) {
            return BidNode.SubmitLowest(Auction, BidColor.NoTrump, 1);
        }

        // Silne 2 trefle
        if (hand.Points >= 18 && hand.PointsNt <= 20) {
            return BidNode.SubmitLowest(Auction, BidColor.Clubs, 2);
        }

        if (hand.PointsNt >= 21 && hand.PointsNt <= 23) {
            return Auction.GetLowestLegalValue(BidColor.NoTrump) >= 3
                ? BidNode.SubmitLowest(Auction, BidColor.NoTrump, 3)
                : BidNode.Submit(2, BidColor.NoTrump);
        }

        if (hand.PointsNt >= 24) {
            return BidNode.SubmitLowestLegalGameOrDouble(Auction, BidColor.NoTrump);
        }

        // Słabe dwa
        if (hand.Points >= 7 && hand.Points <= 11 && hand.CountCards(longestColor) == 6) {
            return BidNode.SubmitOrPass(Auction, 2, longestColor.ToBidColor());
        }

        if (hand.CountCards(longestColor) >= 7 && hand.PointsCount(longestColor) >= 3) {
            return BidNode.SubmitOrPass(Auction, 3, longestColor.ToBidColor());
        }

        return BidNode.Pass();
    }


    /// <summary>
    /// Odpowiedź, tylko w pierwszym kółku.
    /// </summary>
    private BidNode TrueNaturalResponse(Hand hand, Bid partnerBid) {
        var tableEvaluations = TranslateNaturalOpening(partnerBid).ToDictionary(
            e => e,
            e => Evaluator.FromPartner(e, hand)
        );

        var chosenBidNodes = new List<BidNode>();
        foreach (var branch in tableEvaluations) {
            var chosenBidNode = branch.Key.Type == BidType.Pass
                ? TrueNaturalOpening(hand)
                : TrueNaturalResponse(hand, branch.Key, branch.Value);

            if (chosenBidNode.Type != BidType.Pass) {
                chosenBidNodes.Add(chosenBidNode);
            }
        }

        return GetLowestSubmitOrPass(chosenBidNodes);
    }


    /// <summary>
    /// Odpowiedź w dowolnym momencie, partner niespasował.
    /// </summary>
    private BidNode TrueNaturalResponse(Hand hand, BidNode partnerBidNode, TableEvaluation tableEvaluation) {
        if (partnerBidNode.Type == BidType.Double || partnerBidNode.Type == BidType.Redouble) {
            return BidNode.Pass();
        }

        var strongHand = PartnerOpened ? hand.Points >= 10 : hand.Points >= 15;
        var combinedHand = tableEvaluation.GetCombinedHandEvaluation(hand);

        if (combinedHand.Points.Lower < 19) {
            return BidNode.Pass();
        }

        var biddedColors = OwnBidsHistory.Select(e => e.Color).ToHashSet();
        var strongestColors = hand.GetStrongestColors();
        var strongestColor = strongestColors.First();
        var longestColor = hand.GetLongestColor();

        // Szukamy fitu z partnerem lub zgłaszamy własny.
        if (biddedColors.Count == 0) {
            // Jeżeli zgłosił BA, to proponujemy własny 5-kartowy kolor na poziomie dwóch.
            if (partnerBidNode.Color == BidColor.NoTrump && hand.CountCards(strongestColor) >= 5) {
                var lowestPossibleValue = Auction.GetLowestLegalValue(strongestColor.ToBidColor());

                // Albo strongHand albo maksymalnie jeden poziom różnicy.
                if (strongHand || lowestPossibleValue - partnerBidNode.Value!.Value == 1) {
                    return BidNode.Submit(lowestPossibleValue, strongestColor);
                }

                if (hand.HasEvenDistribution()) {
                    return BidNode.SubmitLowest(Auction, BidColor.NoTrump);
                }

                if (LastOpponentBid?.AtLevel(3) ?? false) {
                    return BidNode.Double();
                }

                return BidNode.Pass();
            }
            else if (partnerBidNode.Color == BidColor.NoTrump) {
                var lowestPossibleValue = Auction.GetLowestLegalValue(BidColor.NoTrump);

                if (LastOpponentBid?.AtLevel(3) ?? false) {
                    return BidNode.Double();
                }

                if (combinedHand.Points >= 32 && lowestPossibleValue >= 6) {
                    return BidNode.Submit(6, BidColor.NoTrump);
                }

                if (combinedHand.Points >= 26 && lowestPossibleValue >= 3) {
                    return BidNode.Submit(3, BidColor.NoTrump);
                }

                if (lowestPossibleValue == 2) {
                    return BidNode.Submit(2, BidColor.NoTrump);
                }

                return BidNode.Pass();
            }

            // Odpowiedź na 2 trefle.
            if (partnerBidNode.EqualsByColorAndValue(2, BidColor.Clubs)) {
                var lowestPossibleValue = Auction.GetLowestLegalValue(longestColor.ToBidColor());

                if (lowestPossibleValue == 3 || hand.Points <= 6) {
                    return BidNode.Pass();
                }

                return BidNode.Submit(lowestPossibleValue, longestColor);
            }

            // Partner zgłosił kolor.
            // Fitujemy z partnerem (3+ karty w jego kolorze).
            if (hand.Fits(partnerBidNode.Color)) {
                if (combinedHand.Points >= 24 && partnerBidNode.Color.IsMajor() || combinedHand.Points >= 27 && partnerBidNode.Color.IsMajor()) {
                    var gameMakingBid = BidNode.SubmitGame(partnerBidNode.Color);
                    if (gameMakingBid.IsBidLegal(Auction)) {
                        return gameMakingBid;
                    }

                    // Zabezpieczenie przed wcinką przeciwników.
                    if (LastOpponentBid != null) {
                        return BidNode.SubmitLowest(Auction, partnerBidNode.Color);
                    }

                    return BidNode.Pass();
                }

                return strongHand 
                    ? BidNode.SubmitLowest(Auction, partnerBidNode.Color)           // Inwit
                    : BidNode.SubmitWithRaise(Auction, partnerBidNode.Color);       // Sign-off
            }

            // Nie fituje.
            // Powinniśmy wejść swoim kolorem z przeskokiem i pokazać silną rękę.
            if (strongHand) {
                var lowestPossibleValue = Auction.GetLowestLegalValue(strongestColor.ToBidColor());

                // Nowy zgłoszony kolor z przeskokiem ma mieć 5 kart.
                if (hand.CountCards(strongestColor) >= 5) {
                    // Nie wchodzimy nowym kolorem na poziomie czterech!
                    if (lowestPossibleValue >= 3) {
                        return BidNode.Submit(lowestPossibleValue, strongestColor);
                    }

                    return BidNode.Submit(lowestPossibleValue + 1, strongestColor);
                }

                return BidNode.SubmitLowest(Auction, BidColor.NoTrump);
            }

            // Preferujemy zgłoszenie starszej czwórki na poziomie jeden.
            if (strongestColor.IsMajor() && hand.CountCards(strongestColor) >= 4) {
                var lowestPossibleValue = Auction.GetLowestLegalValue(strongestColor.ToBidColor());
                if (lowestPossibleValue == 1) {
                    return BidNode.Submit(1, strongestColor);
                }
            }

            // Jeżeli nie mogliśmy zgłosić swojego
            if (hand.HasEvenDistribution()) { 
                var lowestPossibleNoTrump = Auction.GetLowestLegalValue(BidColor.NoTrump);

                // Nie mamy strongHandu, nie wchodzimy na poziom 3. Wtedy preferujemy BA (1 lub 2)
                if (lowestPossibleNoTrump >= 3) {
                    return BidNode.Pass();
                }

                return BidNode.Submit(lowestPossibleNoTrump, BidColor.NoTrump);
            }

            return BidNode.Pass();
        }

        // Dalsza licytacja, partner nam odpowiedział.
        var lastOwnBidColor = LastOwnBid!.Color;

        // Gdy robimy grę, ale mamy punkty na szlemika w parze.
        if (partnerBidNode.MakesGame()) {
            if (combinedHand.Points >= BiddingHelper.SmallSlamPointsRequirement(partnerBidNode.Color)) {
                return BidNode.Submit(6, partnerBidNode.Color);
            }

            // Kontrujemy
            if (LastRightOpponentBid?.Type == BidType.Submit) {
                return BidNode.SubmitLowestLegalGameOrDouble(Auction, partnerBidNode.Color);
            }

            return BidNode.Pass();
        }

        // partnerPoints <= 18 - points (pass na pass), no chyba że kontra karna.
        if (partnerBidNode.Type == BidType.Pass) {
            if (LastOpponentBid?.AtLevel(3) ?? false) {
                return BidNode.Double();
            }
            return BidNode.Pass();
        }

        // Czy partner mówił coś z przeskokiem?
        var partnerLowestPossibleBid = Auction.GetLowestLegalValue(partnerBidNode.Color, 2);
        var valueDiff = partnerBidNode.Value - partnerBidNode.Value;
        var possibleDiff = partnerBidNode.Value - partnerLowestPossibleBid;
        var partnerRaised = possibleDiff < valueDiff;

        // Mamy fit, nie mamy partii.
        if (lastOwnBidColor == partnerBidNode.Color) {
            // Powiedział minimalnie jak mógł - strongHand i inwit.
            if (!partnerRaised) {
                // Gramy końcówkę lub kontrę.
                return BidNode.SubmitLowestLegalGameOrDouble(Auction, lastOwnBidColor);
            }
            // Powiedział z przeskokiem, gramy partię tylko na dobrej ręce lub kontrujemy oponentów.
            else if (valueDiff == possibleDiff + 1) {
                // StrongHand lub submit w color na 6+ kartach
                if (strongHand || lastOwnBidColor.IsColorGame() && hand.CountCards(lastOwnBidColor.ToCardColor()) >= 7) {
                    return BidNode.SubmitLowestLegalGameOrDouble(Auction, lastOwnBidColor);
                }

                // Jeżeli wychodzi gra na podstawie połączonej ręki.
                // 8+ w starszym, 24 PC
                // 8+ w młodszym, 27 PC
                if (lastOwnBidColor.IsColorGame() && combinedHand.GetSuit(lastOwnBidColor.ToCardColor()) >= 8 && combinedHand.Points >= lastOwnBidColor.GamePointsRequirement()) {
                    return BidNode.SubmitLowestLegalGameOrDouble(Auction, lastOwnBidColor);
                }

                // NoTrump - niedostępny na poziomie 3 lub niżej.
                // Pass na wszystko inne.
                return BidNode.Pass();
            }

            // Jakikolwiek większy przeskok.
            // Natychmiastowe zgłoszenie końcówki to sign-off.
            if (partnerBidNode.MakesGame()) {
                // Kontrujemy, gdy przeciwnik się wciął.
                if (LastRightOpponentBid != null) {
                    return BidNode.Double();
                }
                return BidNode.Pass();
            }

            // Fallback.
            return BidNode.Pass();
        }

        // Partner zgłosił inny kolor na nasz kolor (nie nasze BA).
        // W naturalnej licytacji wszystki super, ale w systemie nie ma preferencji zgłaszania fitu, np. 12+ PC, 5+ kart w innym kolorze, możliwy fit w kolorze otwarcia!
        if (lastOwnBidColor.IsColorGame() && partnerBidNode.Color.IsColorGame()) {
            // Inny najlepszy kolor
            // Potencjalny problem, partner mógł nie zgłosić fitu od razu, bo system priorytezuje wejście własnym kolorem przy 12+ PC (żeby było GF?)
            var bestFitColor = combinedHand.FindFit(lastOwnBidColor.ToCardColor());

            // One-over-one, słabe wejście, niezobowiązujące.
            if (LastOwnBid.GetLevel() == 1 && partnerBidNode.GetLevel() == 1) {
                // Jeżeli możemy go poprzeć w ten kolor, to próbujemy na najniższym możliwym poziomie, ale nie większym niż 3.
                if (hand.Fits(partnerBidNode.Color)) {
                    return BidNode.SubmitLowest(Auction, partnerBidNode.Color, 3);
                }

                // Pokazujemy drugi najlepszy kolor, o ile ma conajmniej 4 karty.
                // Maksymalnie na poziomie 2 (weakHand) lub 3 (strongHand).
                var secondBestColor = strongestColors[1];
                if (hand.CountCards(secondBestColor) >= 4) {
                    return BidNode.SubmitLowest(Auction, secondBestColor.ToBidColor(), strongHand ? 3 : 2);
                }

                // Sprawdzamy, czy nie wychodzi nam coś z ewaluacji stołu.
                // Pomijamy kolor, który partner zanegował. Możemy go zgłosić maksymalnie na poziomie dwóch.
                // To zwróci BA, jeżeli nie mamy pewnych 8-ek lub 9-ek (młodszy).
                return BidNode.SubmitLowest(Auction, bestFitColor, 2);
            }

            // Two-over-one, bez przeskoku, obiecujące solidną rękę.
            if (LastOwnBid.GetLevel() == 1 && partnerBidNode.GetLevel() == 2 && partnerLowestPossibleBid == partnerBidNode.Value) {
                // Jeżeli możemy go poprzeć w ten kolor, to próbujemy na najniższym możliwym poziomie, ale nie większym niż 4.
                if (hand.Fits(partnerBidNode.Color)) {
                    return BidNode.SubmitLowest(Auction, partnerBidNode.Color, 4);
                }

                // Pokazujemy drugi najlepszy kolor, o ile ma conajmniej 4 karty.
                // Maksymalnie na poziomie 3 (zawsze).
                var secondBestColor = strongestColors[1];
                if (hand.CountCards(secondBestColor) >= 4) {
                    return BidNode.SubmitLowest(Auction, secondBestColor.ToBidColor(), 3);
                }

                // Sprawdzamy, czy nie wychodzi nam coś z ewaluacji stołu.
                // Pomijamy kolor, który partner zanegował. Możemy go zgłosić maksymalnie na poziomie trzech.
                // To zwróci BA, jeżeli nie mamy pewnych 8-ek lub 9-ek (młodszy).
                return BidNode.SubmitLowest(Auction, bestFitColor, 3);
            }

            // Partner zgłosił własny kolor z przeskokiem.
            if (partnerRaised) {
                // Jeżeli możemy go poprzeć w ten kolor, to licytujemy końcówkę lub kontrę na oponentów.
                if (hand.Fits(partnerBidNode.Color)) {
                    return BidNode.SubmitLowestLegalGameOrDouble(Auction, partnerBidNode.Color);
                }

                // Sprawdzamy, czy nie wychodzi nam coś z ewaluacji stołu.
                // Pomijamy kolor, który partner zanegował. Możemy go zgłosić maksymalnie na poziomie trzech.                
                // Jeżeli to BA, to licytujemy grę.
                if (bestFitColor == BidColor.NoTrump) {
                    return BidNode.SubmitLowestLegalGameOrDouble(Auction, BidColor.NoTrump);
                }

                // wpp mamy ograniczenie na poziomie 4.
                return BidNode.SubmitLowest(Auction, bestFitColor, 4);
            }

            // Wszystkie inne przypadki misfita (poziom powyżej 2), patrzymy na ewaluację stołu.
            // Natychmiastowy pass z plażą.
            if (combinedHand.Points < 24) {
                return BidNode.Pass();
            }

            var lowestLegalBestFit = Auction.GetLowestLegalValue(bestFitColor);

            // Na silnej ręce obowiązują wyższe limity.
            return BidNode.SubmitLowest(Auction, bestFitColor, strongHand ? 3 : 2);
        }
        
        // Partner zgłosił BA na nasz kolor.
        if (lastOwnBidColor.IsColorGame() && partnerBidNode.Color.IsNoTrumpGame()) {
            // Z przeskokiem - partner jest mocny
            if (partnerRaised) {
                // Jeżeli możemy poprzeć jego BA:
                if (hand.HasEvenDistribution() || combinedHand.FitsNoTrumpForSure()) {
                    return BidNode.SubmitLowestLegalGameOrDouble(Auction, BidColor.NoTrump);
                }

                // Jeżeli nie jesteśmy pewni to dodatkowo patrzymy na punkty
                if (combinedHand.FitsNoTrump()) {
                    return strongHand
                        ? BidNode.SubmitLowestLegalGameOrDouble(Auction, BidColor.NoTrump)
                        : BidNode.SubmitOrPass(Auction, 3, BidColor.NoTrump);
                }

                // Nie pasuje nam BA (lub jeszcze o tym nie wiemy).
                // Nie próbujemy powtarzać koloru, zgłaszamy drugi najlepszy.
                var secondBestColor = strongestColors[1];
                if (hand.CountCards(secondBestColor) >= 4) {
                    // Limit poziomu - 3.
                    return BidNode.SubmitLowest(Auction, secondBestColor.ToBidColor(), 3);
                }

                // Fallback - zgłoszenie BA.
                return BidNode.SubmitLowest(Auction, BidColor.NoTrump, 3);
            }

            // BA na tym samym poziomie.
            // Jeżeli na to odpowiada i mamy weakHand, to pass.
            if (hand.HasEvenDistribution() && !strongHand) {
                return BidNode.Pass();
            }
            // Jeżeli stronghand, to szukamy gry BA.
            else if (hand.HasEvenDistribution()) {
                return BidNode.SubmitLowest(Auction, BidColor.NoTrump, 3);
            }

            // Jeżeli nasz najdłuższy kolor jest 6-kartowy, to go powtarzamy na poziomie trzech (strongHand) lub dwóch.
            if (hand.CountCards(longestColor) >= 6 && (Auction.GetLowestLegalValue(longestColor.ToBidColor()) == 3 && strongHand || Auction.GetLowestLegalValue(longestColor.ToBidColor()) == 2)) {
                return BidNode.SubmitLowest(Auction, longestColor.ToBidColor());
            }

            // Nie ma z czym grać.
            return BidNode.Pass();
        }

        // Kolor na bez atu.
        if (lastOwnBidColor.IsNoTrumpGame() && partnerBidNode.Color.IsColorGame()) {
            // Z przeskokiem - dobra ręka partnera.
            if (partnerRaised) {
                if (hand.Fits(partnerBidNode.Color)) {
                    return BidNode.SubmitLowestLegalGameOrDouble(Auction, partnerBidNode.Color);
                }

                // Gdy nie mamy fitu, to z silną ręką zgłaszamy końcówkę w BA.
                if (strongHand) {
                    return BidNode.SubmitLowestLegalGameOrDouble(Auction, BidColor.NoTrump);
                }

                // Ze słabą - najdłuższy kolor, o ile da się go zgłosić na tym samym poziomie.
                if (Auction.GetLowestLegalValue(longestColor.ToBidColor()) == partnerBidNode.GetLevel()) {
                    return BidNode.SubmitLowest(Auction, longestColor.ToBidColor());
                }

                // Jeżeli nie, to zgłaszamy BA na najniższym możliwym poziomie.
                return BidNode.SubmitLowest(Auction, BidColor.NoTrump, 4);
            }

            // Bez przeskoku, nie szarżujemy. 
            // Poparcie.
            if (hand.Fits(partnerBidNode.Color)) {
                return BidNode.SubmitLowest(Auction, partnerBidNode.Color, strongHand ? 4 : 3);
            }

            // Brak fitu - zgłaszamy najdłuższy kolor, max na poziomie 3.
            return BidNode.SubmitLowest(Auction, longestColor.ToBidColor(), 3);
        }

        return BidNode.Pass();
    }


    private BidNode TrueNaturalDefence(Hand hand) {
        return BidNode.Pass();
    }


    private BidNode TrueNaturalDefence(Hand hand, BidNode partnerBid, TableEvaluation tableEvaluation) {
        return BidNode.Pass();
    }


    private BidNode TrueNaturalGame(Hand hand, BidNode partnerBid, TableEvaluation tableEvaluation, bool gameForced = false) {
        return BidNode.Pass();
    }


    private BidNode ChaoticNaturalGame(Hand hand, BidNode partnerBid, TableEvaluation tableEvaluation, bool gameForced = false) {
        return BidNode.Pass();
    }


    private BidNode ChaoticNaturalDefence(Hand hand, BidNode partnerBid, TableEvaluation tableEvaluation) {
        return BidNode.Pass();
    }


    private BidNode? GetNaturalBid(Hand hand, Bid lastPartnerBid, bool oneRoundForce = false) {
        /*  1. Jeżeli nic nie licytowałem:
         *      1.1. Partner nic nie licytował:
         *          1.1.1. Oponenci nic nie licytowali:
         *              TRUE NATURAL OPENING
         *          1.1.2. Oponenci coś licytowali:
         *              TRUE NATURAL OPENING lub TRUE NATURAL DEFENCE
         *      1.2. Partner coś licytował:
         *          1.2.1. Oponenci nic nie licytowali (tylko w pierwszym kółku):
         *              TRUE NATURAL RESPONSE
         *          1.2.2. Oponenci coś licytowali:
         *              TRUE NATURAL RESPONSE lub TRUE NATURAL DEFENCE
         *  2. Jeżeli coś licytowałem:
         *      2.1. Partner nic nie licytował:
         *          2.1.1. Oponenci coś licytowali:
         *              TRUE NATURAL DEFENCE
         *      2.2. Partner coś licytował:
         *          2.2.1. Oponenci nic nie licytowali:
         *              TRUE NATURAL GAME
         *          2.2.2. Oponenci coś licytowali:
         *              2.2.2.1. Pierwsze kółko:
         *                  TRUE NATURAL RESPONSE lub TRUE NATURAL DEFENCE
         *              2.2.2.1. Następne kółka:
         *                  CHAOTIC NATURAL GAME lub CHAOTIC NATURAL DEFENCE
         */
        return BidNode.Pass();
    }

    /// <summary>
    /// Zwraca naturalną odpowiedź, na podstawie możliwych gałęzi partnera
    /// </summary>
    /// <returns></returns>
    private BidNode GetNaturalBid(Hand hand, Dictionary<BidNode, TableEvaluation> partnerBranches, bool isForced = false) {
        var chosenBidNodes = new List<BidNode>();
        foreach (var branch in partnerBranches) {
            var chosenBidNode = TrueNaturalResponse(hand, branch.Key, branch.Value).AssertFreestyleIsntConfusing(branch.Key);
            //  Jak oponenci się wetną (czyli bardzo często), to powyższa funkcja często nie zwórci nic, bo będzie confusing, ale jeżeli jest GF, to i tak później się coś zwróci i będzie confusing tak czy siak!!!!

            if (chosenBidNode != null && chosenBidNode.Type != BidType.Pass) {
                chosenBidNodes.Add(chosenBidNode);
            }
        }

        var BidNodeToSubmit = GetLowestSubmitOrPass(chosenBidNodes);
        if(isForced && BidNodeToSubmit.Type == BidType.Pass) {
            return ForcedToBid(hand);
        }

        return BidNodeToSubmit;
    }

    /// <summary>
    /// Należy wywołać tą funckję tylko jako ostateczność! Zakłada, że nie ma fitu, bo wtedy coś wcześniej zwróciłoby odpowiednią odzywkę?
    /// TableEvaluation lub freestyle nie działa, trafiamy kiedy są punkty i fit!!
    /// Zwraca pass tylko i wyłącznie, jeżeli poprzednia odzywka partnera jest rekontrą lub robi partię.
    /// </summary>
    private BidNode ForcedToBid(Hand hand) {
        if(Auction.GetLastPlayerBid(PartnerPosition, passAsNull: false)!.Type == BidType.Redouble || Auction.GetLastPlayerBid(PartnerPosition, passAsNull: false)!.MakesGame()) {
            return BidNode.Pass();
        }

        // Najsilniejszy kolor, którego jeszcze nie zgłaszaliśmy i ma on więcej niz 3 karty
        BidNode? resultInColor = null;
        foreach(CardColor color in hand.GetStrongestColors()) {
            if (OwnBidsHistory.All(e => e.Color != color.ToBidColor()) && hand.OfColor(color).Count() > 3) {
                resultInColor = BidNode.SubmitLowest(Auction, color.ToBidColor());
            }
        }

        // Lepiej zgłości 3 BA niż NOWY kolor na wysokości 4
        if(resultInColor?.Value > 3) {
            if(Auction.GetLowestLegalValue(BidColor.NoTrump) == 3) {
                return BidNode.Submit(3, BidColor.NoTrump);
            }
            else if(resultInColor != null){
                return resultInColor;
            }
        }

        // Nie ma fitu z partnerem, zgłosiliśmy już swoje dobre kolory, więc mozliwie najniższe BA i niech on decyduje
        return BidNode.SubmitLowest(Auction, BidColor.NoTrump);
    }

    private BidNode GetLowestSubmitOrPass(IEnumerable<BidNode> bidCandidates) {
        return bidCandidates
            .Where(e => e.Type == BidType.Submit)
            .OrderBy(e => e.Value)
            .ThenByDescending(e => (int)e.Color)
            .FirstOrDefault()
            ?? BidNode.Pass();
    }




    private static IEnumerable<BidNode> TranslateNaturalOpening(Bid partnerBid) {
        if (partnerBid.Type == BidType.Pass) {
            yield return new BidNode(BidType.Pass, new(12, 14), new(0, 4), new(0, 4), new(0, 4), new(0, 4));
            yield return new BidNode(BidType.Pass, new(0, 11), new(0, 5), new(0, 5), new(0, 5), new(0, 5));
            yield return new BidNode(BidType.Pass, new(0, 6), new(0, 6), new(0, 6), new(0, 6), new(0, 6));
            yield return new BidNode(BidType.Pass, new(0, 3), new(0, 7), new(0, 7), new(0, 7), new(0, 7));
            yield break;
        }

        if (partnerBid.Value == 1) {
            switch (partnerBid.Color) {
                case BidColor.NoTrump:
                    yield return new BidNode(1, BidColor.NoTrump, new(15, 18), new(2, 4), new(2, 4), new(2, 5), new(2, 5));
                    break;
                case BidColor.Spades:
                    yield return new BidNode(1, BidColor.Spades, new(12, 17), new(5, null), new(), new(), new());
                    break;
                case BidColor.Hearts:
                    yield return new BidNode(1, BidColor.Hearts, new(12, 17), new(), new(5, null), new(), new());
                    break;
                case BidColor.Diamonds:
                    yield return new BidNode(1, BidColor.Diamonds, new(12, 17), new(), new(), new(5, null), new());
                    break;
                case BidColor.Clubs:
                    yield return new BidNode(1, BidColor.Clubs, new(12, 17), new(), new(), new(), new(5, null));
                    break;
            }
            yield break;
        }

        if (partnerBid.Value == 2) {
            switch (partnerBid.Color) {
                case BidColor.NoTrump:
                    yield return new BidNode(1, BidColor.NoTrump, new(19, 23), new(2, 4), new(2, 4), new(2, 5), new(2, 5));
                    break;
                case BidColor.Spades:
                    yield return new BidNode(2, BidColor.Spades, new(7, 11), new(6, 6), new(), new(), new());
                    break;
                case BidColor.Hearts:
                    yield return new BidNode(2, BidColor.Hearts, new(7, 11), new(), new(6, 6), new(), new());
                    break;
                case BidColor.Diamonds:
                    yield return new BidNode(2, BidColor.Diamonds, new(7, 11), new(), new(), new(6, 6), new());
                    break;
                case BidColor.Clubs:
                    yield return new BidNode(2, BidColor.Clubs, new(18, 20), new(), new(), new(), new(6, 6));
                    break;
            }
            yield break;
        }

        if (partnerBid.Value == 3) {
            switch (partnerBid.Color) {
                case BidColor.NoTrump:
                    yield return new BidNode(1, BidColor.NoTrump, new(24, null), new(2, 4), new(2, 4), new(2, 5), new(2, 5));
                    break;
                case BidColor.Spades:
                    yield return new BidNode(3, BidColor.Spades, new(3, 11), new(7, null), new(), new(), new());
                    break;
                case BidColor.Hearts:
                    yield return new BidNode(3, BidColor.Hearts, new(3, 11), new(), new(7, null), new(), new());
                    break;
                case BidColor.Diamonds:
                    yield return new BidNode(3, BidColor.Diamonds, new(3, 11), new(), new(), new(7, null), new());
                    break;
                case BidColor.Clubs:
                    yield return new BidNode(3, BidColor.Clubs, new(3, 11), new(), new(), new(), new(7, null));
                    break;
            }
            yield break;
        }
    }


    public IEnumerable<BidNode> TranslateNaturalResponse(Hand hand, BidNode lastOwnBidNode, Bid partnerBid) {
        // partnerPoints <= 18 - points 
        if (partnerBid.Type == BidType.Pass) {
            yield break;
        }

        // Kontry są karne, nic nie tłumaczą.
        if (partnerBid.Type != BidType.Submit) {
            yield break;
        }

        // Sign-off.
        if (partnerBid.MakesGame()) {
            yield break;
        }

        // Mamy fit, nie mamy partii.
        if (lastOwnBidNode.Color == partnerBid.Color) {
            var partnerLowestPossibleBid = Auction.GetLowestLegalValue(partnerBid.Color, 2);

            var valueDiff = partnerBid.Value - lastOwnBidNode.Value;
            var possibleDiff = partnerBid.Value - partnerLowestPossibleBid;

            // Powiedział minimalnie jak mógł - strongHand!
            if (valueDiff == possibleDiff) {

            }
        }
    }
}
