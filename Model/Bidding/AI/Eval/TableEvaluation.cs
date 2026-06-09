using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.AI.Eval; 

public class TableEvaluation {

    public required HandEvaluation Partner { get; set; }

    public required HandEvaluation LeftOpponent { get; set; }

    public required HandEvaluation RightOpponent { get; set; }

    public HandEvaluation GetCombinedHandEvaluation(Hand ownHand) {
        var result = HandEvaluation.OnOwnHand(ownHand);
        result.Points += Partner.Points;
        result.Spades += Partner.Spades;
        result.Hearts += Partner.Hearts;
        result.Diamonds += Partner.Diamonds;
        result.Clubs += Partner.Clubs;
        result.Aces += Partner.Aces;
        result.Kings += Partner.Kings;
        return result;
    }

}