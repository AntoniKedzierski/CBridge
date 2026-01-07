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
    

    public Game(int numberOfRounds, string[] names) {
        _numberOfRounds = numberOfRounds;
        _players = new Player[4];

        if (names.Length != 3) {
            throw new InvalidOperationException("Three players must participate in a game.");
        }

        _players[0] = new Player(names[0], PlayerPosition.North);
        _players[1] = new Player(names[1], PlayerPosition.East);
        _players[2] = new Player(names[2], PlayerPosition.South);
        _players[3] = new Bot(PlayerPosition.West);
        _gameMode = GameMode.ThreePlayers;
    }


    public Game(int numberOfRounds) {
        _numberOfRounds = numberOfRounds;
        _players = new Player[4];

        _players[0] = new Player("Player", PlayerPosition.North);
        _players[1] = new Bot(PlayerPosition.East);
        _players[2] = new Bot(PlayerPosition.South);
        _players[3] = new Bot(PlayerPosition.West);
        _gameMode = GameMode.OnePlayer;
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
        if (!NextRandomDeal()) {
            return false;
        }

        return true;
    }
}
