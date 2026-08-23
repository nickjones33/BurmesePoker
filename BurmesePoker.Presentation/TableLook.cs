using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Presentation;

/// <summary>
/// What the rules make public at the table, folded from the public events and from nothing
/// else: every seat's discard pile, and every seat's face-up cards — the ones taken in the
/// open, lying in front of their taker for as long as they stay in the hand (RULES.md §5,
/// §5.2).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The engine plays these rules already; this is the one place they are shown</b>
/// (P41). An open take and a discard are public events by <c>CardId</c>, so the face-up set
/// and every pile are pure folds over them — no engine state, no journal change, and every
/// front end that draws either reads this rather than re-deriving it. The console's observer,
/// the server's fan-out and the browser's board all hold one of these and feed it the same
/// events they were already narrating.
/// </para>
/// <para>
/// ⚠️ <b>The blind draw has no method here, and that is the concealment rule</b> (RULES.md
/// §6.3, §5.2): a card the deck gives unseen is never face up, so there is deliberately
/// nothing a narrator could call to make it so. <c>ConcealmentTests</c> holds the fan-out to
/// this from the outside.
/// </para>
/// <para>
/// <b>Immutable, like <c>TableBoard</c> and for the same reason</b>: a browser component holds
/// a fold across a render, so every event hands back a new value and nothing changes under a
/// reader. A narrator that folds in place holds the latest one in a field.
/// </para>
/// <para>
/// <b>Instance identity throughout</b> (BUILD-PLAN §3.1). Two decks mean a seat can hold the
/// face-up Q♣ and a concealed Q♣ at once, and which copy leaves when a Q♣ is thrown is exactly
/// what the face-up card settles (§9 #50): the engine discards by <c>CardId</c>, so the
/// face-up copy stays unless that very card was thrown.
/// </para>
/// </remarks>
public sealed record TableLook
{
    private static readonly IReadOnlyList<Card> NoCards = [];

    private TableLook(
        IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> piles,
        IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> faceUp)
    {
        Piles = piles;
        FaceUp = faceUp;
    }

    /// <summary>A table before anything has been thrown or taken.</summary>
    public static TableLook Empty { get; } = new(
        new Dictionary<PlayerId, IReadOnlyList<Card>>(),
        new Dictionary<PlayerId, IReadOnlyList<Card>>());

    /// <summary>
    /// Each seat's own discard pile, oldest first — all of it, because every card of every pile
    /// may be looked through (RULES.md §5).
    /// </summary>
    public IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> Piles { get; private init; }

    /// <summary>
    /// Each seat's face-up cards in the order they were taken: taken in the open, still in the
    /// hand, visible to everybody (RULES.md §5.2).
    /// </summary>
    public IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> FaceUp { get; private init; }

    /// <summary>One seat's discard pile, oldest first. Empty before it has thrown anything.</summary>
    public IReadOnlyList<Card> PileOf(PlayerId player) =>
        Piles.TryGetValue(player, out var pile) ? pile : NoCards;

    /// <summary>One seat's face-up cards, in the order they were taken.</summary>
    public IReadOnlyList<Card> FaceUpOf(PlayerId player) =>
        FaceUp.TryGetValue(player, out var cards) ? cards : NoCards;

    /// <summary>Whether anybody is holding this very card face up.</summary>
    public bool IsFaceUp(CardId card) =>
        FaceUp.Values.Any(cards => cards.Any(held => held.Id == card));

    /// <summary>A new deal: every pile is fresh and nobody holds anything yet (RULES.md §3).</summary>
    public TableLook RoundStarted() => Empty;

    /// <summary>
    /// A seat threw a card: it goes on top of their own pile, and if it was one of their
    /// face-up cards, that copy has left their hand and stops being face up (RULES.md §5.2).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>By <c>CardId</c>, which is §9 #50's recorded default doing its work</b>: a seat
    /// holding the face-up Q♣ and a concealed Q♣ that throws the concealed one leaves the
    /// face-up card exactly where it lies.
    /// </remarks>
    public TableLook Discarded(PlayerId player, Card card) => this with
    {
        Piles = Appended(Piles, player, card),
        FaceUp = Removed(FaceUp, player, card)
    };

    /// <summary>
    /// A seat took the previous seat's top discard: it comes off whichever pile it was lying
    /// on, and it lies face up in front of its taker (RULES.md §5, §5.2).
    /// </summary>
    public TableLook TookDiscard(PlayerId player, Card card) => this with
    {
        Piles = Lifted(Piles, card),
        FaceUp = Appended(FaceUp, player, card)
    };

    /// <summary>
    /// The opener claimed the turned-up money card (RULES.md §4.5): the most public take there
    /// is, so it lies face up too — §9 #49's recorded default.
    /// </summary>
    /// <remarks>
    /// The card came off the table rather than off a pile, so no pile moves.
    /// </remarks>
    public TableLook MoneyCardClaimed(PlayerId player, Card card) => this with
    {
        FaceUp = Appended(FaceUp, player, card)
    };

    /// <summary>
    /// The draw pile ran out and every pile was gathered into a new one (RULES.md §5). The
    /// face-up cards are in hands, not on piles, so they stay exactly where they are.
    /// </summary>
    public TableLook DiscardsReshuffled() => this with
    {
        Piles = new Dictionary<PlayerId, IReadOnlyList<Card>>()
    };

    private static IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> Appended(
        IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> held,
        PlayerId player,
        Card card)
    {
        var copy = new Dictionary<PlayerId, IReadOnlyList<Card>>(held);
        copy[player] = held.TryGetValue(player, out var had) ? [.. had, card] : [card];
        return copy;
    }

    private static IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> Removed(
        IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> held,
        PlayerId player,
        Card card)
    {
        if (!held.TryGetValue(player, out var had) || !had.Any(kept => kept.Id == card.Id))
        {
            return held;
        }

        var copy = new Dictionary<PlayerId, IReadOnlyList<Card>>(held);
        copy[player] = [.. had.Where(kept => kept.Id != card.Id)];
        return copy;
    }

    /// <summary>The piles with one card lifted off whichever of them it was on top of.</summary>
    private static IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> Lifted(
        IReadOnlyDictionary<PlayerId, IReadOnlyList<Card>> piles,
        Card taken)
    {
        foreach (var (player, pile) in piles)
        {
            if (pile.Count > 0 && pile[^1].Id == taken.Id)
            {
                var copy = new Dictionary<PlayerId, IReadOnlyList<Card>>(piles);
                copy[player] = [.. pile.Take(pile.Count - 1)];
                return copy;
            }
        }

        return piles;
    }
}
