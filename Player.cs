namespace BurmesePoker;

internal class Player(string name)
{
    public string Name { get; } = name;
    public IEnumerable<Card> Hand { get; set; } = [];
    public int Score { get; set; }
}
