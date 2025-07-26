using GameCards.Server.Models;
using GameCards.Shared;

namespace GameCards.Server.Services;

public class ArenalesGame
{
    public Guid GameId { get; } = Guid.NewGuid();
    public string OwnerPlayerId { get; set; } = string.Empty;
    public List<PlayerState> Players { get; private set; } = [];
    public string PlayerPlayingId = string.Empty;
    public int CurrentTurnIndex { get; private set; } = 0;
    public int TurnNumber { get; private set; } = 0;
    public bool IsPublic { get; set; }
    public int MaxPlayers { get; set; }
    public GamePhase CurrentGamePhase { get; private set; } = GamePhase.WaitingForPlayers;
    public TurnPhase CurrentTurnPhase { get; private set; } = TurnPhase.Action;
    
    public bool IsStarted => CurrentGamePhase != GamePhase.WaitingForPlayers;
    
    public void AddPlayer(string playerId, string name)
    {
        if (IsStarted || Players.Count >= MaxPlayers)
            throw new InvalidOperationException("Cannot join, game already started or max players!");
        
        Players.Add(new PlayerState(playerId, name));
    }

    public void StartGame()
    {
        if (Players.Count < 2)
            throw new InvalidOperationException("Cannot start, need at least 2 players!");
        
        CurrentGamePhase = GamePhase.Setup;
        
        // TODO: Initialize decks, shuffle, deal starting cards
        SetupInitialDecks();
        
        TurnNumber = 1;

        CurrentTurnIndex = 0;
        PlayerPlayingId = Players[CurrentTurnIndex].PlayerId;

        CurrentTurnPhase = TurnPhase.Action;

        CurrentGamePhase = GamePhase.InProgress;
    }
    
    public void AdvanceTurnPhase()
    {
        switch (CurrentTurnPhase)
        {
            case TurnPhase.Action:
                CurrentTurnPhase = TurnPhase.Buy;
                break;
            
            case TurnPhase.Buy:
                CurrentTurnPhase = TurnPhase.Cleanup;
                break;
            
            case TurnPhase.Cleanup:
                if (Players.Count > 0)
                    EndOfTurnCleanup();
                
                MoveToNextValidPlayer();

                if (Players.Count == 0)
                {
                    CurrentGamePhase = GamePhase.GameOver;
                    PlayerPlayingId = string.Empty;
                    return;
                }
                
                CurrentTurnPhase = TurnPhase.Action;
                break;
        }
        
        if (Players.Count > 0 && CurrentTurnIndex < Players.Count)
            PlayerPlayingId = Players[CurrentTurnIndex].PlayerId;
        else
            PlayerPlayingId = string.Empty; // fallback if no players
    }
    
    private void EndOfTurnCleanup()
    {
        var currentPlayer = Players[CurrentTurnIndex];

        // Discard all cards in hand
        currentPlayer.DiscardPile.AddRange(currentPlayer.Hand);
        currentPlayer.Hand.Clear();

        // Draw 5 new cards for next turn
        currentPlayer.DrawCards(5); 
    }

    
    private void MoveToNextValidPlayer()
    {
        if (Players.Count == 0)
            return; // game ends

        // Move index to next
        CurrentTurnIndex++;

        // If beyond last, wrap around
        if (CurrentTurnIndex >= Players.Count)
        {
            CurrentTurnIndex = 0;
            TurnNumber++; // completed a round
        }

        // Extra safety: if the current index is invalid, wrap to 0
        if (CurrentTurnIndex >= Players.Count)
            CurrentTurnIndex = 0;
    }


    private void SetupInitialDecks()
    {
        foreach (var player in Players)
        {
            player.GainStartingDeck();
        }
    }
    
    public GamePublicState GetPublicState()
    {
        return new GamePublicState(
            GameId,
            OwnerPlayerId,
            CurrentGamePhase,
            CurrentTurnPhase,
            PlayerPlayingId,
            Players.Select(p => p.ToPublicView()).ToList(),
            CurrentTurnIndex,
            TurnNumber
        );
    }
}