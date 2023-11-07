namespace BurmesePoker;

public class Card
{
    //standard card constructor
    public Card(CardRank rank, CardSuit suit)
    {
        Rank = rank;
        Suit = suit;
        Color = suit == CardSuit.Hearts || suit == CardSuit.Diamonds ? CardColor.Red : CardColor.Black;
    }
    //joker constructor (suitless)
    public Card(CardRank rank, CardColor color)
    {
        if (rank != CardRank.Joker)
        {
            throw new IndexOutOfRangeException(rank.ToString());
        }

        Rank = rank;
        Color = color;
        Suit = CardSuit.Joker;
    }

    public CardSuit Suit { get; }
    public CardRank Rank { get; }
    public CardColor Color { get; }

    public string DisplayValue => Rank != CardRank.Joker ? $"{Common.DisplayCode(Rank)}{Common.DisplaySuit(Suit)}" : $"{Common.DisplayCode(Rank)}({Color})";
    public int RankOrder => Common.CardRankOrder(Rank);
}
