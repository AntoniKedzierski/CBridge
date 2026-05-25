using Model.Bidding;
using Model.Bidding.AI;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Model;

public class Game {

    private readonly int _numberOfRounds;
    private readonly GameMode _gameMode;
    private Player[] _players;

    private int _currentRoundNumber;
    private PlayerPosition _dealer;

    private Auction _auction;
       
    public Game() {
        _players = new Player[4];
        _auction = new Auction();
    }

    public Game(int numberOfRounds, string[] names, IBidInput manualBidInput) : this() {
        _numberOfRounds = numberOfRounds;

        if (names.Length != 3) {
            throw new InvalidOperationException("Three players must participate in a game.");
        }

        _players[0] = new Player(names[0], PlayerPosition.North, manualBidInput);
        _players[1] = new Player(names[1], PlayerPosition.East, manualBidInput);
        _players[2] = new Player(names[2], PlayerPosition.South, manualBidInput);
        _players[2] = new Player("bot", PlayerPosition.West, new BidEngine(_auction));
        _gameMode = GameMode.ThreePlayers;
    }


    public Game(int numberOfRounds, string name, IBidInput manualBidInput) : this() {
        _numberOfRounds = numberOfRounds;

        _players[0] = new Player(name, PlayerPosition.North, manualBidInput);
        _players[1] = new Player("bot", PlayerPosition.East, new BidEngine(_auction));
        _players[2] = new Player("bot", PlayerPosition.South, new BidEngine(_auction));
        _players[3] = new Player("bot", PlayerPosition.West, new BidEngine(_auction));
        _gameMode = GameMode.OnePlayer;
    }


    public Game(int numberOfRounds) : this() {
        _numberOfRounds = numberOfRounds;

        _players[0] = new Player("bot", PlayerPosition.North, new BidEngine(_auction));
        _players[1] = new Player("bot", PlayerPosition.East, new BidEngine(_auction));
        _players[2] = new Player("bot", PlayerPosition.South, new BidEngine(_auction));
        _players[3] = new Player("bot", PlayerPosition.West, new BidEngine(_auction));
        _gameMode = GameMode.BotsOnly;
    }


    public Player GetPlayer(PlayerPosition position) => _players[(int)position];


    public bool NextRandomDeal() {
        if (_currentRoundNumber >= _numberOfRounds) {
            return false;
        }

        var deal = new Deal();
        for (int i = 0; i < 4; ++i) {
            _players[i].GiveHand(deal.Hands[(PlayerPosition)i]);
        }

        _dealer = (PlayerPosition)(_currentRoundNumber % 4);
        _currentRoundNumber++;
        return true;
    }


    public bool Play() {
        while (NextRandomDeal()) {
            while (!_auction.IsCompleted()) {
                var currentBid = GetPlayer(_auction.CurrentBidder).MakeBid();
                _auction.Submit(currentBid);
            }
        }

        return true;
    }
}
