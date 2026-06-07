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

    // IDEA: HandEvaluation MyHand -- for player to know what he told to others, might be useful in freestyle bidding
    public HandEvaluation PartnersHand { get; private set; }
    public HandEvaluation LeftOpponentsHand { get; private set; }
    public HandEvaluation RightOpponentsHand { get; private set; }
    public IBidInput BidInput { get; private set; }


    public Player(string name, PlayerPosition startingPosition, IBidInput BidInput) {
        Name = name;
        CurrentPosition = startingPosition;
        PartnersHand = new HandEvaluation();
        LeftOpponentsHand = new HandEvaluation();
        RightOpponentsHand = new HandEvaluation();
        this.BidInput = BidInput;
    }


    public void GiveHand(Hand hand) {
        Hand = hand;
        PartnersHand.Evaluate(hand);
        LeftOpponentsHand.Evaluate(hand);
        RightOpponentsHand.Evaluate(hand);
    }

    public virtual Bid MakeBid() {
        return BidInput.Get(Hand, PartnersHand, LeftOpponentsHand, RightOpponentsHand);
    }

    public void EvaluateHands(Bid bid) {
        // BidInput.EvaluateHands(bid, PartnersHand, LeftOpponentsHand, RightOpponentsHand);
    }

    public void Reset() {
        BidInput.Reset();
    }

}
