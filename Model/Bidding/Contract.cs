using Model.Enums;
using Model.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding; 

public class Contract {

    public int Value { get; set; }

    public BidColor Color { get; set; }

    public bool IsDoubled { get; set; }

    public bool IsRedoubled { get; set; }

    public PlayerPosition Player { get; set; }

    public bool Passed { get; set; }


    public string Decribe(Player[] players) {
        var playerHand = players.GetPlayerHand(Player);
        var dummyHand = players.GetPlayerHand(Player.GetPartner());
        var totalPoints = playerHand.PointsNt + dummyHand.PointsNt;
        var distribution = $"{playerHand.SpadesCount}-{playerHand.HeartsCount}-{playerHand.DiamondsCount}-{playerHand.ClubsCount}";
        var dummyDistribution = $"{dummyHand.SpadesCount}-{dummyHand.HeartsCount}-{dummyHand.DiamondsCount}-{dummyHand.ClubsCount}";

        if (Passed) {
            return ToString();
        }

        return ToString() + $" on {totalPoints} PC with {distribution} and dummy {dummyDistribution}";
    }


    public override string ToString() {
        if (Passed) {
            return "Passed";
        }

        var result = $"{Player} playes {Value}{Color.ColorMark()}";
        if (IsDoubled) {
            result += " (doubled)";
        }

        if (IsRedoubled) {
            result += " (redoubled)";
        }

        return result;
    }
}
