using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model;

public class Deal {

    public Dictionary<PlayerPosition, Hand> Hands { get; private set; } = new();

    public Deal() {
        var draftDeck = CreateDraftDeck();
        Random.Shared.Shuffle(draftDeck);

        foreach (var position in Enum.GetValues<PlayerPosition>()) {
            Hands[position] = new(draftDeck[((int)position * 13)..(((int)position + 1) * 13 - 1)]);
        }
    }


    /// <summary>
    /// Calculates missing hand. 
    /// </summary>
    /// <param name="hands">Three hands.</param>
    public Deal(Dictionary<PlayerPosition, Hand> hands) {
        var draftDeck = CreateDraftDeck().ToHashSet();

        // Determine which player is missing (bot plays there).
        var missingPlayerPosition = Enum.GetValues<PlayerPosition>()
            .Except(hands.Keys)
            ?.FirstOrDefault() 
            ?? throw new InvalidOperationException("All hands have been given.");

        // Copy all given hands.
        Hands = hands;

        // Get missing cards.
        var givenCards = hands.SelectMany(e => e.Value.Cards);
        var missingCards = draftDeck.Except(givenCards);
        Hands[missingPlayerPosition] = new Hand(missingCards);

        // Sort all hands, just in case.
        foreach (var hand in Hands.Values) {
            hand.SortHand();
        }
    }


    private static Card[] CreateDraftDeck() {
        var draftDeck = new List<Card>();
        foreach (var color in Enum.GetValues(typeof(CardColor)).Cast<CardColor>()) {
            foreach (var value in Enum.GetValues(typeof(CardValue)).Cast<CardValue>()) {
                draftDeck.Add(new Card(value, color));
            }
        }
        return draftDeck.ToArray();
    }

}
