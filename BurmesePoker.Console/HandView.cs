using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;

using Spectre.Console;
using Spectre.Console.Rendering;

namespace BurmesePoker.Console;

/// <summary>
/// A hand drawn as the melds it nearly is, and what each card would cost to throw away.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thirteen sorted cards is the wrong picture.</b> What a player is actually holding is
/// some melds and some deadwood, and the only question a turn asks is which of the deadwood to
/// let go of. <see cref="PartialCover.Best"/> (BUILD-PLAN P10) hands back exactly that split,
/// so this is a reading of one domain call rather than any new judgement about the hand.
/// </para>
/// <para>
/// <b>The arrangement shown is <em>a</em> best one, not the tidiest one.</b> The cover search
/// returns the first partition it finds at the maximum count, so thirteen cards of one suit in
/// sequence come back as four melds rather than one, and two equally good arrangements are
/// decided by search order. Presentation sorts what it is given and goes no further — tidying
/// it would be solving a different problem (BUILD-PLAN P8).
/// </para>
/// <para>
/// <b>The cost of a card is measured, not guessed:</b> the cover of the twelve that would be
/// left, against the cover of the thirteen. A loose card costs nothing; a card a meld needs
/// costs the whole meld. That is the same arithmetic <c>GreedyBotAgent</c> discards on, which
/// is why the number and the computer's advice never contradict each other.
/// </para>
/// </remarks>
public sealed class HandView
{
    private readonly IReadOnlyList<Card> _hand;
    private readonly PartialCover _cover;
    private readonly Dictionary<CardId, int> _costs = [];

    private HandView(IReadOnlyList<Card> hand, PartialCover cover)
    {
        _hand = hand;
        _cover = cover;
    }

    /// <summary>The best cover of this hand, ready to draw.</summary>
    public static HandView Of(IReadOnlyList<Card> hand) => new(hand, PartialCover.Best(hand));

    /// <summary>How many of the hand's cards meld.</summary>
    public int Covered => _cover.CoveredCount;

    /// <summary>How many cards are held.</summary>
    public int Count => _hand.Count;

    /// <summary>
    /// How many melded cards throwing this one gives up. Zero for deadwood.
    /// </summary>
    public int CostOfThrowing(Card card)
    {
        if (_costs.TryGetValue(card.Id, out var known))
        {
            return known;
        }

        var kept = new List<Card>(_hand.Count - 1);
        foreach (var held in _hand)
        {
            // Instance identity, not value: two decks mean the hand may hold the other copy
            // of this very card, and that one stays (BUILD-PLAN §3.1).
            if (held != card)
            {
                kept.Add(held);
            }
        }

        var cost = Math.Max(0, _cover.CoveredCount - PartialCover.Best(kept).CoveredCount);
        _costs[card.Id] = cost;
        return cost;
    }

    /// <summary>
    /// The hand as a panel: one line per meld, then the loose cards, headed by the score.
    /// </summary>
    public IRenderable AsPanel(MoneyCardRegistry money, Func<Card, bool> owned)
    {
        var rows = new List<IRenderable>();

        foreach (var meld in _cover.Melds.OrderByDescending(meld => meld.Count).ThenBy(meld => meld.Kind))
        {
            rows.Add(new Markup(CardFormatting.Meld(meld, money, owned)));
        }

        if (_cover.Uncovered.Count > 0)
        {
            rows.Add(new Markup(Loose(_cover.Uncovered, money, owned)));
        }

        var header = _cover.IsComplete
            ? $"Your hand — [{Palette.Good}]all {_hand.Count} meld[/]"
            : $"Your hand — {_cover.CoveredCount} of {_hand.Count} meld";

        return new Panel(new Rows(rows)).Header(header).BorderColor(Palette.Frame);
    }

    /// <summary>The deadwood, labelled to line up with the meld rows above it.</summary>
    private static string Loose(
        IReadOnlyList<Card> cards,
        MoneyCardRegistry money,
        Func<Card, bool> owned)
    {
        var drawn = CardFormatting.Sorted(cards)
            .Select(card => CardFormatting.Of(card, money, owned(card)));

        return $"[{Palette.Quiet}]{"loose",-5}[/] {string.Join("  ", drawn)}";
    }
}
