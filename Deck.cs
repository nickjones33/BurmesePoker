namespace BurmesePoker;

internal class Deck : List<Card>
{
    internal Deck() : base() { }
    internal Deck(IEnumerable<Card> collection) : base(collection) { }

    internal void Shuffle()
    {
        Random random = new();
        this.OrderBy(x => random.Next());
    }

    internal Card DrawFromTop()
    {
        var topCard = this.First();
        this.RemoveAt(0);
        return topCard;
    }

    internal Card DrawFromBottom()
    {
        var bottomCard = this.Last();
        this.RemoveAt(this.Count - 1);
        return bottomCard;
    }
}