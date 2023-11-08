namespace BurmesePoker;

internal class PlayersInOrder : LinkedList<Player>
{
    public PlayersInOrder(IOrderedEnumerable<Player> players)
    {
        foreach (Player player in players)
        {
            AddLast(player);
        }
    }

    internal Player PlayerBeforeIndex(int index)
    {
        if (Count < 2)
        {
            throw new InvalidOperationException("There are not enough players to get the player before the given index.");
        }
        else if (index == 0)
        {
            return Last!.Value;
        }
        else
        {
            return this.ElementAt(index - 1);
        }
    }
    internal Player PlayerAfterIndex(int index)
    {
        if (Count < 2)
        {
            throw new InvalidOperationException("There are not enough players to get the player after the given index.");
        }
        else if (index == Count - 1)
        {
            return First!.Value;
        }
        else
        {
            return this.ElementAt(index + 1);
        }
    }
}
