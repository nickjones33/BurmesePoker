namespace BurmesePoker;

internal class Player(string name, int money)
{
    internal string Name { get; } = name;
    internal List<Card> Hand { get; set; } = [];
    internal List<Card> Discard { get; set; } = [];
    internal int Score { get; set; }
    internal int Money { get; set; } = money;
    internal string PrintOrderedHand => string.Join(", ", Hand.OrderBy(x => x.RankOrder).Select(x => x.DisplayValue));
}
