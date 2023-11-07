namespace BurmesePoker;

public static class Common
{
    public static string DisplayCode(CardRankCode input) => input switch
    {
        CardRankCode.Two => "2",
        CardRankCode.Three => "3",
        CardRankCode.Four => "4",
        CardRankCode.Five => "5",
        CardRankCode.Six => "6",
        CardRankCode.Seven => "7",
        CardRankCode.Eight => "8",
        CardRankCode.Nine => "9",
        CardRankCode.Ten => "10",
        CardRankCode.Jack => "J",
        CardRankCode.Queen => "Q",
        CardRankCode.King => "K",
        CardRankCode.Joker => "Joker",
        _ => throw new IndexOutOfRangeException(input.ToString()),
    };
}

public enum CardRankCode
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
    None,
    Hearts,
    Spades,
    Clubs,
    Diamonds
}