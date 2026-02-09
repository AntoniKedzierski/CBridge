using Model.Bidding;
using System;
using System.Collections.Generic;
using System.Text;

using Model.Enums;

namespace BiddingBrowser.BiddingTree.Validation.Constraints {
    internal sealed class PlayerCon {

        public NumberRange Pc { get; }
        public NumberRange Clubs { get; }
        public NumberRange Diamonds { get; }
        public NumberRange Hearts { get; }
        public NumberRange Spades { get; }

        public PlayerCon(NumberRange pc, NumberRange clubs, NumberRange diamonds, NumberRange hearts, NumberRange spades) {
            Pc = pc;
            Clubs = clubs;
            Diamonds = diamonds;
            Hearts = hearts;
            Spades = spades;
        }

        public static PlayerCon CreateInitial(int minPc, int maxPc, int minCards, int maxCards) {
            var anyPc = new NumberRange(minPc, maxPc);
            var anyLen = new NumberRange(minCards, maxCards);

            return new PlayerCon(anyPc, anyLen, anyLen, anyLen, anyLen);
        }

        public PlayerCon WithPc(NumberRange pc) {
            return new PlayerCon(pc, Clubs, Diamonds, Hearts, Spades);
        }

        public NumberRange GetSuitLength(BidColor suit) {
            return suit switch {
                BidColor.Clubs => Clubs,
                BidColor.Diamonds => Diamonds,
                BidColor.Hearts => Hearts,
                BidColor.Spades => Spades,
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, "Suit length is only valid for Clubs/Diamonds/Hearts/Spades.")
            };
        }

        public PlayerCon WithSuitLength(BidColor suit, NumberRange length) {
            return suit switch {
                BidColor.Clubs => new PlayerCon(Pc, length, Diamonds, Hearts, Spades),
                BidColor.Diamonds => new PlayerCon(Pc, Clubs, length, Hearts, Spades),
                BidColor.Hearts => new PlayerCon(Pc, Clubs, Diamonds, length, Spades),
                BidColor.Spades => new PlayerCon(Pc, Clubs, Diamonds, Hearts, length),
                _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, "Suit length is only valid for Clubs/Diamonds/Hearts/Spades.")
            };
        }



    }
}
