using Model.Bidding.Bids;
using Model.Enums;

namespace Model;

public class Player {

    public PlayerPosition CurrentPosition { get; private set; }

    public int Score { get; private set; }

    public string Name { get; private set; }

    public Hand Hand { get; private set; }

    public IBidInput BidInput { get; private set; }


    public Player(string name, PlayerPosition startingPosition, IBidInput BidInput) {
        Name = name;
        CurrentPosition = startingPosition;
        this.BidInput = BidInput;
    }


    public void GiveHand(Hand hand) {
        Hand = hand;
    }


    public virtual Bid MakeBid() {
        return BidInput.Get(Hand);
    }


    public void Reset() {
        BidInput.Reset();
    }

}