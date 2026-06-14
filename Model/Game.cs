using Model.Bidding;
using Model.Bidding.AI;
using Model.Bidding.Bids;
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

    public Auction Auction { get; private set; }
       
    public Game() {
        _players = new Player[4];
        _dealer = PlayerPosition.North;
        Auction = new Auction();
    }

    public Game(int numberOfRounds, string[] names, IBidInput manualBidInput) : this() {
        _numberOfRounds = numberOfRounds;

        if (names.Length != 3) {
            throw new InvalidOperationException("Three players must participate in a game.");
        }

        _players[0] = new Player(names[0], PlayerPosition.North, manualBidInput);
        _players[1] = new Player(names[1], PlayerPosition.East, manualBidInput);
        _players[2] = new Player(names[2], PlayerPosition.South, manualBidInput);
        _players[2] = new Player("bot", PlayerPosition.West, new BidEngine(Auction, PlayerPosition.West));
        _gameMode = GameMode.ThreePlayers;
    }


    public Game(int numberOfRounds, string name, IBidInput manualBidInput) : this() {
        _numberOfRounds = numberOfRounds;

        _players[0] = new Player(name, PlayerPosition.North, manualBidInput);
        _players[1] = new Player("bot", PlayerPosition.East, new BidEngine(Auction, PlayerPosition.East));
        _players[2] = new Player("bot", PlayerPosition.South, new BidEngine(Auction, PlayerPosition.South));
        _players[3] = new Player("bot", PlayerPosition.West, new BidEngine(Auction, PlayerPosition.West));
        _gameMode = GameMode.OnePlayer;
    }


    public Game(int numberOfRounds) : this() {
        _numberOfRounds = numberOfRounds;

        _players[0] = new Player("bot", PlayerPosition.North, new BidEngine(Auction, PlayerPosition.North));
        _players[1] = new Player("bot", PlayerPosition.East, new BidEngine(Auction, PlayerPosition.East));
        _players[2] = new Player("bot", PlayerPosition.South, new BidEngine(Auction, PlayerPosition.South));
        _players[3] = new Player("bot", PlayerPosition.West, new BidEngine(Auction, PlayerPosition.West));
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
            var nsPoints = _players[0].Hand.PointsNt + _players[2].Hand.PointsNt;
            var ewPoints = _players[1].Hand.PointsNt + _players[3].Hand.PointsNt;

            Console.WriteLine($"--------------------------------------");
            Console.WriteLine($"Round {_currentRoundNumber}    Dealer: {_dealer}    NS: {nsPoints}    EW: {ewPoints}");
            foreach (var player in _players) {
                Console.WriteLine(player.CurrentPosition.ToString() + " " + player.Hand);
            }
            Console.WriteLine();

            Auction.Start(_dealer);
            while (!Auction.IsCompleted()) {
                var currentBid = GetPlayer(Auction.CurrentBidder).MakeBid();

                // Evaluate hand strength for all players based on the current bid, except for the player who made the bid
                foreach (var player in _players) {
                    if (player.CurrentPosition == Auction.CurrentBidder) {
                        continue;
                    }
                }
                Auction.Submit(currentBid);
            }

            var contract = Auction.GetContract(_players);
            Console.WriteLine();
            Console.WriteLine(contract.Decribe(_players));

            // Rozgrywka
            Auction.Clear();
            Console.WriteLine();
            foreach (var player in _players) {
                player.Reset();
            }
        }

        return true;
    }
}
