using Model.Bidding.AI.Eval;
using Model.Bidding.Bids;
using Model.Enums;
using Model.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
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
                return BidNode.Submit(1, longestColor);
            }
        }

        // Zgłaszanie BA
        if (hand.PointsNt >= 15 && hand.PointsNt <= 17) {
            return BidNode.Submit(1, BidColor.NoTrump);
        }

        // Silne 2 trefle
        if (hand.Points >= 18 && hand.PointsNt <= 20) {
            return BidNode.Submit(2, BidColor.Clubs);
        }

        if (hand.PointsNt >= 21 && hand.PointsNt <= 23) {
            return BidNode.Submit(2, BidColor.NoTrump);
        }

        if (hand.PointsNt >= 24) {
            return BidNode.Submit(3, BidColor.NoTrump);
        }

        // Słabe dwa
        if (hand.Points >= 7 && hand.Points <= 11 && hand.CountCards(longestColor) == 6) {
            return BidNode.Submit(2, longestColor);
        }

        if (hand.CountCards(longestColor) >= 7 && hand.PointsCount(longestColor) >= 3) {
            return BidNode.Submit(3, longestColor);
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
        var strongHand = PartnerOpened ? hand.Points >= 10 : hand.Points >= 16;
        var combinedHand = tableEvaluation.GetCombinedHandEvaluation(hand);

        if (combinedHand.Points.Lower < 19) {
            return BidNode.Pass();
        }

        var biddedColors = OwnBidsHistory.Select(e => e.Color).ToHashSet();
        var strongestColors = hand.GetStrongestColors();
        var strongestColor = strongestColors.First();

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

                if (lowestPossibleValue >= 2) {
                    return BidNode.Submit(2, BidColor.NoTrump);
                }

                return BidNode.Pass();
            }

            // Odpowiedź na 2 trefle.
            if (partnerBidNode.EqualsByColorAndValue(2, BidColor.Clubs)) {
                var longestColor = hand.GetLongestColor();
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

        // Sign-off.
        if (partnerBidNode.Type == BidType.Double || partnerBidNode.Type == BidType.Redouble) {
            return BidNode.Pass();
        }

        if (partnerBidNode.MakesGame()) {
            if (combinedHand.Points >= BiddingHelper.SmallSlamPointsRequirement(partnerBidNode.Color)) {
                return BidNode.Submit(6, partnerBidNode.Color);
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

        // Mamy fit, nie mamy partii.
        if (lastOwnBidColor == partnerBidNode.Color) {
            var partnerLowestPossibleBid = Auction.GetLowestLegalValue(partnerBidNode.Color, 2);

            var valueDiff = partnerBidNode.Value - partnerBidNode.Value;
            var possibleDiff = partnerBidNode.Value - partnerLowestPossibleBid;

            // Powiedział minimalnie jak mógł - strongHand i inwit.
            if (valueDiff == possibleDiff) {
                // Gramy końcówkę lub kontrę.
                return BidNode.SubmitLowestLegalGameOrDouble(Auction, lastOwnBidColor);
            }
            // Powiedział z przeskokiem, gramy partię tylko na dobrej ręce lub kontrujemy oponentów.
            else if (valueDiff == possibleDiff + 1) {
                if (strongHand || hand.CountCards(lastOwnBidColor.ToCardColor()) >= 6) {
                    return BidNode.SubmitLowestLegalGameOrDouble(Auction, lastOwnBidColor);
                }

                return BidNode.Pass();
            }
        }

        
    }


    /// <summary>
    /// Czy zgłoszenie koloru będzie podniesienim one-over-one?
    /// </summary>
    private bool IsOneOverOneSubmit(BidColor color) {
        return Auction.CanSubmit(1, color);
    }


    private bool CanSubmitTwoOverOne(BidColor color, BidNode partnerBidNode) {
        return Auction.CanSubmit(2, color) && (int)partnerBidNode.Color > (int)color;
    }


    private bool CanSubmitTwoOverOneJump(BidColor color, BidNode partnerBidNode) {
        return Auction.CanSubmit(2, color) && (int)partnerBidNode.Color < (int)color;
    }


    private BidNode TrueNaturalDefence(Hand hand) {

    }


    private BidNode TrueNaturalDefence(Hand hand, BidNode partnerBid, TableEvaluation tableEvaluation) {

    }


    private BidNode TrueNaturalGame(Hand hand, BidNode partnerBid, TableEvaluation tableEvaluation, bool gameForced = false) {

    }


    private BidNode ChaoticNaturalGame(Hand hand, BidNode partnerBid, TableEvaluation tableEvaluation, bool gameForced = false) {

    }


    private BidNode ChaoticNaturalDefence(Hand hand, BidNode partnerBid, TableEvaluation tableEvaluation) {

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
         * 
         * 
         * 
         * 
         * 
         */
        if (LastOwnBid != null) {

        }

        var chosenBids = new List<BidNode>();
        foreach (var branchHead in branches) {
            var chosenBid = ChooseBidByFreestyling(hand, branchHead.Value)?.AssertFreestyleIsntConfusing(branchHead.Key);
            if (chosenBid != null && chosenBid.IsBidLegal(Auction)) {
                chosenBids.Add(chosenBid);
            }
        }

        if (chosenBids.Count == 0) {
            if (!oneRoundForce) {
                return null;
            }

            return BidBestColor(hand);
        }

        var signOff = GetCommonBranchesValue(branches, e => e.SignOff);
        if (signOff) {
            return null;
        }

        var result = chosenBids.Min()!;
        result.IsFromSystem = false;
        return result;
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
