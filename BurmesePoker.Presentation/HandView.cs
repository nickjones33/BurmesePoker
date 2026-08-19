using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Presentation;

/// <summary>
/// A hand as the melds it nearly is, and what each card would cost to throw away — as data a
/// console, a browser or a test can read.
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
/// <para>
/// ⚠️ <b>Every cost is computed up front, and that is deliberate.</b> A view model is data —
/// something a Razor component can hold, iterate twice, and hand to a test — not a lazy
/// computation graph that quietly searches a hand when a renderer touches it. It costs one
/// <see cref="PartialCover.Best"/> per card, a few milliseconds a hand at the speeds P12
/// measured, and it is built once a turn per seat.
/// </para>
/// <para>
/// <b>Nothing here is a colour.</b> A card's state is a <see cref="CardDisplayState"/> with a
/// non-colour token behind it (§3.11 A2); what red means is the renderer's decision and always
/// was.
/// </para>
/// </remarks>
public sealed class HandView
{
    private readonly Dictionary<CardId, CardView> _byCard;

    private HandView(
        IReadOnlyList<Card> hand,
        PartialCover cover,
        IReadOnlyList<MeldView> melds,
        IReadOnlyList<CardView> loose,
        IReadOnlyList<CardView> cards)
    {
        Hand = hand;
        Covered = cover.CoveredCount;
        IsComplete = cover.IsComplete;
        Melds = melds;
        Loose = loose;
        Cards = cards;
        _byCard = cards.ToDictionary(view => view.Card.Id);
    }

    /// <summary>
    /// The player's own view of their own hand: what the round says about each card, and what
    /// the computer would throw if it were asked.
    /// </summary>
    /// <param name="context">The turn being decided. Only the asking player's own view of it is read.</param>
    /// <param name="suggestedThrow">
    /// The card the computer would throw, or null when a front end is not offering hints.
    /// ⚠️ <b>It is passed in rather than worked out here</b> (BUILD-PLAN P13.1): the hint is
    /// the computer's own answer, and a second implementation of it would be a different
    /// strategy wearing the first one's name. <see cref="ComputerAdvice"/> is where to get it.
    /// </param>
    public static HandView Of(TurnContext context, Card? suggestedThrow = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Of(context.Hand, context.MoneyCards, context.YouOwn, suggestedThrow);
    }

    /// <summary>
    /// The same view built from its parts, for a caller holding a hand rather than a turn.
    /// </summary>
    /// <param name="hand">The cards held — thirteen between turns, fourteen with one taken.</param>
    /// <param name="money">Which values pay this round, and how much (RULES.md §4.1, §4.2).</param>
    /// <param name="owned">
    /// Whether the deck gave a card to the player whose view this is. Only ever asked about
    /// that player's own cards, so it leaks nothing about anybody else's (RULES.md §4.4).
    /// </param>
    /// <param name="suggestedThrow">The computer's answer, or null for no hint.</param>
    public static HandView Of(
        IReadOnlyList<Card> hand,
        MoneyCardRegistry money,
        Func<Card, bool> owned,
        Card? suggestedThrow = null)
    {
        ArgumentNullException.ThrowIfNull(hand);
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(owned);

        // ⚠️ Copied, not aliased. TurnContext.Hand is the seat's own live list and the engine
        // discards from it the moment this decision is answered (RoundEngine step 2), so a
        // view that kept the reference would quietly describe thirteen cards it had reported
        // fourteen of. A view model is a snapshot of a moment; a browser component holding one
        // across a render is exactly the case this protects.
        hand = [.. hand];

        var cover = PartialCover.Best(hand);
        var melded = cover.Melds.SelectMany(meld => meld.CardIds).ToHashSet();

        var views = hand.ToDictionary(
            card => card.Id,
            card => new CardView(
                card,
                State(card, money, owned, melded, suggestedThrow),
                money.Multiplier(card),
                CostOfThrowing(card, hand, cover)));

        var melds = cover.Melds
            .OrderByDescending(meld => meld.Count)
            .ThenBy(meld => meld.Kind)
            .Select(meld => new MeldView(meld, [.. meld.Cards.Select(card => views[card.Id])]))
            .ToArray();

        return new HandView(
            hand,
            cover,
            melds,
            [.. CardOrder.Display(cover.Uncovered).Select(card => views[card.Id])],
            [.. CardOrder.Display(hand).Select(card => views[card.Id])]);
    }

    /// <summary>
    /// The cards held when the view was built, in the order the domain handed them over.
    /// A copy: nothing the engine does afterwards changes it.
    /// </summary>
    public IReadOnlyList<Card> Hand { get; }

    /// <summary>How many of the hand's cards meld.</summary>
    public int Covered { get; }

    /// <summary>How many cards are held.</summary>
    public int Count => Hand.Count;

    /// <summary>Whether every card melds — the hand wins, if it is the thirteen held between turns.</summary>
    public bool IsComplete { get; }

    /// <summary>The melds of the best cover, longest first, then runs before sets.</summary>
    public IReadOnlyList<MeldView> Melds { get; }

    /// <summary>The deadwood, in display order.</summary>
    public IReadOnlyList<CardView> Loose { get; }

    /// <summary>Every card held, in display order.</summary>
    public IReadOnlyList<CardView> Cards { get; }

    /// <summary>
    /// The view of one card of this hand.
    /// </summary>
    /// <remarks>
    /// By <see cref="CardId"/> — <b>instance identity, not value</b>: two decks mean the hand
    /// may hold the other copy of this very card, and it is a different card with a different
    /// cost (BUILD-PLAN §3.1).
    /// </remarks>
    /// <exception cref="ArgumentException">The card is not in this hand.</exception>
    public CardView Of(Card card) => _byCard.TryGetValue(card.Id, out var view)
        ? view
        : throw new ArgumentException($"{card} is not in this hand.", nameof(card));

    /// <summary>How many melded cards throwing this one gives up. Zero for deadwood.</summary>
    public int CostOfThrowing(Card card) => Of(card).CostOfThrowing;

    private static CardDisplayState State(
        Card card,
        MoneyCardRegistry money,
        Func<Card, bool> owned,
        HashSet<CardId> melded,
        Card? suggestedThrow)
    {
        var state = melded.Contains(card.Id) ? CardDisplayState.Melded : CardDisplayState.Loose;

        state |= money.Multiplier(card) switch
        {
            >= 2 => CardDisplayState.PaysDouble,
            1 => CardDisplayState.PaysOnce,
            _ => CardDisplayState.None
        };

        // The star only means anything on a card that pays: every card dealt is owned, so
        // marking ownership alone would mark the whole hand and say nothing (P11).
        if (money.Multiplier(card) > 0 && owned(card))
        {
            state |= CardDisplayState.Owned;
        }

        // Instance identity again: the computer named one physical card, not a value.
        if (suggestedThrow is { } advised && advised == card)
        {
            state |= CardDisplayState.SuggestedThrow;
        }

        return state;
    }

    private static int CostOfThrowing(Card card, IReadOnlyList<Card> hand, PartialCover cover)
    {
        var kept = new List<Card>(hand.Count - 1);
        foreach (var held in hand)
        {
            // Instance identity, not value: two decks mean the hand may hold the other copy
            // of this very card, and that one stays (BUILD-PLAN §3.1).
            if (held != card)
            {
                kept.Add(held);
            }
        }

        return Math.Max(0, cover.CoveredCount - PartialCover.Best(kept).CoveredCount);
    }
}
