using BiddingBrowser.BiddingTree.Validation.Constraints;
using Model.Bidding;
using Model.Enums;

using System;
using System.Collections.Generic;
using System.Text;

namespace BiddingBrowser.BiddingTree.Validation {
    internal sealed class BiddingCon {

        public PlayerCon Opener { get; }
        public PlayerCon Responder { get; }

        public BiddingCon(PlayerCon opener, PlayerCon responder) {
            Opener = opener;
            Responder = responder;
        }

        public static BiddingCon CreateInitial(int minPc, int maxPc, int minCards, int maxCards) {
            return new BiddingCon(
                PlayerCon.CreateInitial(minPc, maxPc, minCards, maxCards),
                PlayerCon.CreateInitial(minPc, maxPc, minCards, maxCards)
            );
        }

        public BiddingCon WithOpenerPc(NumberRange pc) {
            return new BiddingCon(Opener.WithPc(pc), Responder);
        }

        public BiddingCon WithResponderPc(NumberRange pc) {
            return new BiddingCon(Opener, Responder.WithPc(pc));
        }

        public NumberRange GetSuitLength(bool opener, BidColor suit) {
            return opener ? Opener.GetSuitLength(suit) : Responder.GetSuitLength(suit);
        }

        public BiddingCon WithSuitLength(bool opener, BidColor suit, NumberRange length) {
            if (opener) {
                return new BiddingCon(Opener.WithSuitLength(suit, length), Responder);
            }
            return new BiddingCon(Opener, Responder.WithSuitLength(suit, length));
        }



    }
}

