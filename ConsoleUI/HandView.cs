using Model;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI;

public static class HandView {

    private static string CardToString(Card card) {
        return card.Value switch {
            CardValue.Two => "2",
            CardValue.Three => "3",
            CardValue.Four => "4",
            CardValue.Five => "5",
            CardValue.Six => "6",
            CardValue.Seven => "7",
            CardValue.Eight => "8",
            CardValue.Nine => "9",
            CardValue.Ten => "10",
            CardValue.Jack => "J",
            CardValue.Queen => "Q",
            CardValue.King => "K",
            CardValue.Ace => "A",
            _ => throw new NotImplementedException("Invalid card value.")
        };
    }

    public static void DisplayHand(this Hand hand) {
        Console.WriteLine($"Points: {hand.CalculatePoints()} PC.");
        Console.WriteLine($"NT points: {hand.CalculatePoints(true)} PC.");
        Console.WriteLine($"♠: " + string.Join(" ", hand.OfColor(CardColor.Spades).Select(CardToString)));
        Console.WriteLine($"♡: " + string.Join(" ", hand.OfColor(CardColor.Hearts).Select(CardToString)));
        Console.WriteLine($"♢: " + string.Join(" ", hand.OfColor(CardColor.Diamonds).Select(CardToString)));
        Console.WriteLine($"♣: " + string.Join(" ", hand.OfColor(CardColor.Clubs).Select(CardToString)));
        Console.WriteLine();
    }
}
