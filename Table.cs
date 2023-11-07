namespace BurmesePoker;

internal class Table
{
    internal List<Player> Players { get; set; } = [];
    internal List<Card> Deck { get; set; } = [];
    internal List<Card> Discard { get; set; } = [];
    internal IEnumerable<Card> AllCards => Deck.Concat(Discard).Concat(Players.SelectMany(x => x.Hand));
}
