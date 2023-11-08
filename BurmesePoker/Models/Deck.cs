namespace BurmesePoker;

internal class Deck : List<Card>
{
    internal Deck() : base() { }
    internal Deck(IEnumerable<Card> collection) : base(collection) { }

    internal Deck Shuffle()
    {
        Random random = new();
        return new(this.OrderBy(x => random.Next()));
    }

    internal Card DrawFromTop()
    {
        Card topCard = this.First();
        this.RemoveAt(0);
        return topCard;
    }

    internal Card DrawFromBottom()
    {
        Card bottomCard = this.Last();
        this.RemoveAt(this.Count - 1);
        return bottomCard;
    }
}