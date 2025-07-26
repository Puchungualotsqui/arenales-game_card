namespace GameCards.Shared;

public record CardStruct(
    string Name,
    List<CardType> Types,    // Supports multi-type cards
    string Description,
    int Cost,
    int WinningPoints,
    string FrontCardImage,
    string BackCardImage
);

public enum CardType
{
    Action,
    Treasure,
    Victory,
    Curse,
    Attack,
    Defense
}