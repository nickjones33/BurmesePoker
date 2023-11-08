using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BurmesePoker.Tests")]
namespace BurmesePoker;

internal class WinConditionObserver
{
    private readonly IEnumerable<Card> Cards;
    internal WinConditionObserver(IEnumerable<Card> cards) => Cards = cards;

    public bool IsWinningHand()
    {
        return false;
    }
}

