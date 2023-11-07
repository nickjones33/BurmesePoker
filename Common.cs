namespace BurmesePoker;

public static class Common
{
    public static IEnumerable<CardSuit> CardSuits_All() {
        IEnumerable<CardSuit> noJokers = CardSuits_NoJokers();
        return noJokers.Append(CardSuit.Joker);
    }
    public static IEnumerable<CardSuit> CardSuits_NoJokers() => new List<CardSuit>
    {
        CardSuit.Hearts,
        CardSuit.Spades,
        CardSuit.Clubs,
        CardSuit.Diamonds
    };
    public static IEnumerable<CardRank> CardRankCodes_All() {
        IEnumerable<CardRank> noJokers = CardRankCodes_NoJokers();
        return noJokers.Append(CardRank.Joker);
    }
    public static IEnumerable<CardRank> CardRankCodes_NoJokers() => new List<CardRank>
    {
        CardRank.Two,
        CardRank.Three,
        CardRank.Four,
        CardRank.Five,
        CardRank.Six,
        CardRank.Seven,
        CardRank.Eight,
        CardRank.Nine,
        CardRank.Ten,
        CardRank.Jack,
        CardRank.Queen,
        CardRank.King,
        CardRank.Ace
    };
    public static int CardRankOrder(CardRank input) => input switch
    {
        CardRank.Two => 0,
        CardRank.Three => 1,
        CardRank.Four => 2,
        CardRank.Five => 3,
        CardRank.Six => 4,
        CardRank.Seven => 5,
        CardRank.Eight => 6,
        CardRank.Nine => 7,
        CardRank.Ten => 8,
        CardRank.Jack => 9,
        CardRank.Queen => 10,
        CardRank.King => 11,
        CardRank.Ace => 12,
        _ => throw new IndexOutOfRangeException(input.ToString()),
    };
    public static string DisplayCode(CardRank input) => input switch
    {
        CardRank.Two => "2",
        CardRank.Three => "3",
        CardRank.Four => "4",
        CardRank.Five => "5",
        CardRank.Six => "6",
        CardRank.Seven => "7",
        CardRank.Eight => "8",
        CardRank.Nine => "9",
        CardRank.Ten => "10",
        CardRank.Jack => "J",
        CardRank.Queen => "Q",
        CardRank.King => "K",
        CardRank.Ace => "A",
        CardRank.Joker => "Joker",
        _ => throw new IndexOutOfRangeException(input.ToString()),
    };
    public static string DisplaySuit(CardSuit input) => input switch
    {
        CardSuit.Joker => "",
        CardSuit.Hearts => "♥",
        CardSuit.Spades => "♠",
        CardSuit.Clubs => "♣",
        CardSuit.Diamonds => "♦",
        _ => throw new IndexOutOfRangeException(input.ToString())
    };
}

public enum CardRank
{
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace,
    Joker
}
public enum CardSuit
{
    Joker,
    Hearts,
    Spades,
    Clubs,
    Diamonds
}
public enum CardColor {
    Red,
    Black
}