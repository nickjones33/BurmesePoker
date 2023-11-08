namespace BurmesePoker;

internal class Card
{
    //standard card constructor
    internal Card(CardRank rank, CardSuit suit)
    {
        Rank = rank;
        Suit = suit;
        Color = suit == CardSuit.Hearts || suit == CardSuit.Diamonds ? CardColor.Red : CardColor.Black;

        MoneyCardStatus = (rank == CardRank.Seven && suit == CardSuit.Diamonds) || (rank == CardRank.Ace && suit == CardSuit.Spades) ?
            MoneyCardStatus.MoneyCard :
            MoneyCardStatus.NotMoneyCard;
    }
    //joker constructor (suitless)
    internal Card(CardRank rank, CardColor color)
    {
        if (rank != CardRank.Joker)
        {
            throw new IndexOutOfRangeException(rank.ToString());
        }

        Rank = rank;
        Color = color;
        Suit = CardSuit.Joker;
    }

    internal Guid Id { get; } = Guid.NewGuid();

    internal CardSuit Suit { get; }
    internal CardRank Rank { get; }
    internal CardColor Color { get; }
    internal MoneyCardStatus MoneyCardStatus { get; set; } = MoneyCardStatus.NotMoneyCard;
    internal Player? MoneyCardOwner { get; set; } = null;
    internal bool IsMoneyCard => MoneyCardStatus != MoneyCardStatus.NotMoneyCard;
    private string MoneyCardDisplay => MoneyCardStatus switch
    {
        MoneyCardStatus.NotMoneyCard => "",
        MoneyCardStatus.MoneyCard => "($)",
        MoneyCardStatus.DoubleMoneyCard => "($$)",
        _ => throw new IndexOutOfRangeException(MoneyCardStatus.ToString())
    };

    internal string DisplayValue => Rank != CardRank.Joker ?
        $"{Common.DisplayCode(Rank)}{Common.DisplaySuit(Suit)}{MoneyCardDisplay}" :
        $"{Common.DisplayCode(Rank)}({Color}){MoneyCardDisplay}";
    internal int RankOrder => Common.CardRankOrder(Rank);

    internal bool ValueEqualTo(Card card) => Suit == card.Suit && Rank == card.Rank && Color == card.Color;
}
