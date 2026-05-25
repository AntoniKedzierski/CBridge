using Model.Bidding;
using Model.Bidding.Bids;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model;

public class Player {

    public PlayerPosition CurrentPosition { get; private set; }

    public int Score { get; private set; }

    public string Name { get; private set; }

    public Hand Hand { get; private set; }
    public HandEvaluation PartnersHand { get; private set; }
    public HandEvaluation LeftOpponentsHand { get; private set; }
    public HandEvaluation RightOpponentsHand { get; private set; }
    public IBidInput BidInput { get; private set; }


    public Player(string name, PlayerPosition startingPosition, IBidInput manualBidInput) {
        Name = name;
        CurrentPosition = startingPosition;
        PartnersHand = new HandEvaluation();
        LeftOpponentsHand = new HandEvaluation();
        RightOpponentsHand = new HandEvaluation();
        BidInput = manualBidInput;
    }


    public void GiveHand(Hand hand) {
        Hand = hand;
        PartnersHand.Evaluate(hand);
        LeftOpponentsHand.Evaluate(hand);
        RightOpponentsHand.Evaluate(hand);
    }

    public virtual Bid MakeBid() {
        return BidInput.Get(PartnersHand, LeftOpponentsHand, RightOpponentsHand);
    }
}
