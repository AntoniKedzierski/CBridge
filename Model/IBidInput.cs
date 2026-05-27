using Model.Bidding.Bids;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model {
    public interface IBidInput {
        public Bid Get(HandEvaluation partnersHand, HandEvaluation LeftOpponentsHand, HandEvaluation RightOpponentsHand);
        public void EvaluateHands(Bid bid, HandEvaluation partnersHand, HandEvaluation leftOpponentsHand, HandEvaluation rightOpponentsHand);
    }
}
