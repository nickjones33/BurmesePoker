namespace BurmesePoker;

internal class Player(string name, int money)
{
    public string Name { get; } = name;
    public List<Card> Hand { get; set; } = [];
    public int Score { get; set; }
    public int Money { get; set; } = money;
}
