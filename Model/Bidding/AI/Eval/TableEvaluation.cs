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
        if (result.Points.Upper > 40) {
            result.Points.Upper = 40;
        }

        result.Spades += Partner.Spades;
        if (result.Spades.Upper > 13) {
            result.Spades.Upper = 13;
        }

        result.Hearts += Partner.Hearts;
        if (result.Hearts.Upper > 13) {
            result.Hearts.Upper = 13;
        }

        result.Diamonds += Partner.Diamonds;
        if (result.Diamonds.Upper > 13) {
            result.Diamonds.Upper = 13;
        }

        result.Clubs += Partner.Clubs;
        if (result.Clubs.Upper > 13) {
            result.Clubs.Upper = 13;
        }

        result.Aces += Partner.Aces;
        if (result.Aces > 4) {
            result.Aces = 4;
        }

        result.Kings += Partner.Kings;
        if (result.Kings > 4) {
            result.Kings = 4;
        }

        return result;
    }

    public override string ToString() {
        return $"\tPartner: {Partner}\n\tLeft Opponent: {LeftOpponent}\n\tRight Opponent: {RightOpponent}";
    }   

}