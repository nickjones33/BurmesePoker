namespace BurmesePoker;

public class Card
{
    //standard card constructor
    public Card(CardRank rank, CardSuit suit)
    {
        Rank = rank;
        Suit = suit;
        Color = suit == CardSuit.Hearts || suit == CardSuit.Diamonds ? CardColor.Red : CardColor.Black;

        IsMoneyCard = (rank == CardRank.Seven && suit == CardSuit.Diamonds) || (rank == CardRank.Ace && suit == CardSuit.Spades);
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

    public Guid Id { get; } = Guid.NewGuid();

    public CardSuit Suit { get; }
    public CardRank Rank { get; }
    public CardColor Color { get; }
    public bool IsMoneyCard { get; set; } = false;

    public string DisplayValue => Rank != CardRank.Joker ?
        $"{Common.DisplayCode(Rank)}{Common.DisplaySuit(Suit)}{(IsMoneyCard ? "($)" : "")}" :
        $"{Common.DisplayCode(Rank)}({Color}){(IsMoneyCard ? "($)" : "")}";
    public int RankOrder => Common.CardRankOrder(Rank);

    public bool ValueEqualTo(Card card) => Suit == card.Suit && Rank == card.Rank && Color == card.Color;
}
