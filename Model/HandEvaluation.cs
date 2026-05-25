using Model.Bidding;
using Model.Bidding.Bids;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model; 
public class HandEvaluation {
    public NumberRange Points { get; set; }
    public NumberRange Sapdes { get; set; }
    public NumberRange Hearts { get; set; }
    public NumberRange Diamonds { get; set; }
    public NumberRange Clubs { get; set; }
    public int? Aces { get; set; }
    public int? Kings { get; set; }

    public HandEvaluation() {
        Points = new NumberRange(0, 40);
        Sapdes = new NumberRange(0, 13);
        Hearts = new NumberRange(0, 13);
        Diamonds = new NumberRange(0, 13);
        Clubs = new NumberRange(0, 13);
    }

    // TODO
    public void Evaluate(BidNode bidNode) {

    }

    // TODO
    public void Evaluate(Hand hand) {

    }

    public float Precision() {
        return 1f 
            - 0.15f * (Points.Upper.Value - Points.Lower.Value) / 40f
            + 0.15f * (Sapdes.Upper.Value - Sapdes.Lower.Value) / 13f
            + 0.15f * (Hearts.Upper.Value - Hearts.Lower.Value) / 13f
            + 0.15f * (Diamonds.Upper.Value - Diamonds.Lower.Value) / 13f
            + 0.15f * (Clubs.Upper.Value - Clubs.Lower.Value) / 13f
            + 0.125f * (Aces.HasValue ? 1 : 0)
            + 0.125f * (Kings.HasValue ? 1 : 0);
    }
}
