namespace BurmesePoker;

internal static class Common
{
    internal static (CardRank rank, CardColor color, CardSuit suit) DetermineCardRankSuitFromString(string input)
    {
        if (input.Length < 2) throw new ArgumentException("Input must be at least 2 characters long.");
        if (input.Length == 2)
        {
            CardRank rank = CardRankFromString(input.ToUpper()[0].ToString());
            CardSuit cardSuit = CardSuitFromChar(input.ToUpper()[1].ToString());            
            CardColor cardColor = CardColorFromSuit(cardSuit);
            return (rank, cardColor, cardSuit);
        }
        else if (input.Length == 3)
        {
            if (input[..2] == "10") //has to be 10 or fall through to joker
            {
                CardSuit suit = CardSuitFromChar(input.ToUpper()[2].ToString());
                return (CardRank.Ten, CardColorFromSuit(suit), suit);
            }
        }
        
        return (CardRank.Joker, CardColorFromString(input.ToUpper()[^0]), CardSuit.Joker); //joker
    }
    internal static CardColor CardColorFromSuit(CardSuit suit) => suit switch
    {
        CardSuit.Hearts => CardColor.Red,
        CardSuit.Diamonds => CardColor.Red,
        CardSuit.Spades => CardColor.Black,
        CardSuit.Clubs => CardColor.Black,
        _ => throw new ArgumentException("Input must be a valid card suit."),
    };
    internal static CardSuit CardSuitFromChar(string input) => input switch
    {
        "H" => CardSuit.Hearts,
        "D" => CardSuit.Diamonds,
        "S" => CardSuit.Spades,
        "C" => CardSuit.Clubs,
        "" => CardSuit.Joker,
        "♥" => CardSuit.Hearts,
        "♠" => CardSuit.Spades,
        "♣" => CardSuit.Clubs,
        "♦" => CardSuit.Diamonds,
        _ => throw new ArgumentException("Input must be a valid card suit."),
    };
    internal static CardRank CardRankFromString(string input) => input switch
    {
        "2" => CardRank.Two,
        "3" => CardRank.Three,
        "4" => CardRank.Four,
        "5" => CardRank.Five,
        "6" => CardRank.Six,
        "7" => CardRank.Seven,
        "8" => CardRank.Eight,
        "9" => CardRank.Nine,
        "10" => CardRank.Ten,
        "T" => CardRank.Ten,
        "J" => CardRank.Jack,
        "Q" => CardRank.Queen,
        "K" => CardRank.King,
        "A" => CardRank.Ace,
        "🃏" => CardRank.Joker,
        _ => throw new ArgumentException("Input must be a valid card rank."),
    };
    internal static CardColor CardColorFromString(char input) => input switch
    {
        'R' => CardColor.Red,
        'B' => CardColor.Black,
        _ => throw new ArgumentException("Input must be a valid card color."),
    };
    internal static IEnumerable<CardSuit> CardSuits_All()
    {
        IEnumerable<CardSuit> noJokers = CardSuits_NoJokers();
        return noJokers.Append(CardSuit.Joker);
    }
    internal static IEnumerable<CardSuit> CardSuits_NoJokers() => new List<CardSuit>
    {
        CardSuit.Hearts,
        CardSuit.Spades,
        CardSuit.Clubs,
        CardSuit.Diamonds
    };
    internal static IEnumerable<CardRank> CardRankCodes_All()
    {
        IEnumerable<CardRank> noJokers = CardRankCodes_NoJokers();
        return noJokers.Append(CardRank.Joker);
    }
    internal static IEnumerable<CardRank> CardRankCodes_NoJokers() => new List<CardRank>
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
    internal static int CardRankOrder(CardRank input) => input switch
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
        CardRank.Joker => 13,
        _ => throw new IndexOutOfRangeException(input.ToString()),
    };
    internal static string DisplayCode(CardRank input) => input switch
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
        CardRank.Joker => "🃏",
        _ => throw new IndexOutOfRangeException(input.ToString()),
    };
    internal static string DisplaySuit(CardSuit input) => input switch
    {
        CardSuit.Joker => "",
        CardSuit.Hearts => "♥",
        CardSuit.Spades => "♠",
        CardSuit.Clubs => "♣",
        CardSuit.Diamonds => "♦",
        _ => throw new IndexOutOfRangeException(input.ToString())
    };
}

internal enum PlayerAction
{
    Draw,
    Pickup
}
internal enum MoneyCardStatus
{
    NotMoneyCard,
    MoneyCard,
    DoubleMoneyCard
}
internal enum CardRank
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
internal enum CardSuit
{
    Joker,
    Hearts,
    Spades,
    Clubs,
    Diamonds
}
internal enum CardColor
{
    Red,
    Black
}