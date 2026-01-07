using Model;
using Model.Enums;
using System.Text;

namespace ConsoleUI;

public class Program {

    public static void Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;

        var game = new Game(8);

        while (game.NextRandomDeal()) {
            game.GetPlayer(PlayerPosition.North).Hand.DisplayHand();

            
        }
    }
}