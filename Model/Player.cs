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


    public Player(string name, PlayerPosition startingPosition) {
        Name = name;
        CurrentPosition = startingPosition;
    }


    public void GiveHand(Hand hand) => Hand = hand;
}
