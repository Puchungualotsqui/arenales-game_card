using GameCards.Shared;

namespace GameCards.Server.Models;

public class PlayerState
{
    public string PlayerId { get; }
    public string Name { get; }
    public List<CardStruct> Deck { get; private set; } = new();
    public List<CardStruct> DiscardPile { get; private set; } = new();
    public List<CardStruct> Hand { get; private set; } = new();

    public PlayerState(string playerId, string name)
    {
        PlayerId = playerId;
        Name = name;
    }

    public void GainStartingDeck()
    {
        // Example: 7 Copper + 3 Estates
        Deck.AddRange(Enumerable.Repeat(CardLibrary.Copper, 7));
        Deck.AddRange(Enumerable.Repeat(CardLibrary.Estate, 3));
        ShuffleDeck();
        DrawCards(5);
    }
    
    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (Deck.Count == 0)
                ReshuffleDiscardIntoDeck();
            
            if (Deck.Count == 0) break;
            
            var top = Deck[0];
            Deck.RemoveAt(0);
            Hand.Add(top);
        }
    }

    private void ReshuffleDiscardIntoDeck()
    {
        if (DiscardPile.Count == 0) return;
        Deck.AddRange(DiscardPile);
        DiscardPile.Clear();
        
        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        var random = new Random();
        int n = Deck.Count;

        for (int i = n - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (Deck[i], Deck[j]) = (Deck[j], Deck[i]);
        }
    }
    
    public PlayerPublicView ToPublicView() =>
        new PlayerPublicView(PlayerId, Name, DiscardPile, Deck, Hand);
}