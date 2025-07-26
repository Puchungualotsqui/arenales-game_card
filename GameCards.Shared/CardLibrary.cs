namespace GameCards.Shared;

public static class CardLibrary
{
    public static readonly CardStruct Copper = new(
        Name: "Copper",
        Types: [CardType.Treasure],
        Description: "+1 Coin",
        Cost: 0,
        WinningPoints: 0,
        FrontCardImage: "/images/cards/copper.png",
        BackCardImage: "/images/cards/back.png"
    );
    
    public static readonly CardStruct Estate = new(
        Name: "Estate",
        Types: [ CardType.Victory ],
        Description: "Worth 1 Victory Point",
        Cost: 2,
        WinningPoints: 1,
        FrontCardImage: "/images/cards/estate.png",
        BackCardImage: "/images/cards/back.png"
    );

    public static readonly CardStruct Village = new(
        Name: "Village",
        Types: [ CardType.Action ],
        Description: "+1 Card, +2 Actions",
        Cost: 3,
        WinningPoints: 0,
        FrontCardImage: "/images/cards/village.png",
        BackCardImage: "/images/cards/back.png"
    );
}