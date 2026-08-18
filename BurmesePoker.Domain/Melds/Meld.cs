using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// A candidate meld: an ordered list of <see cref="MeldSlot"/>s, each naming the card that
/// fills it and the rank and suit that card plays as.
/// </summary>
/// <remarks>
/// <para>
/// A meld is a <b>candidate</b>, not a commitment. Generators deliberately emit overlapping
/// melds — the same joker instance offered to several suits, every sub-run of a longer run
/// (BUILD-PLAN §3.4). Disjointness is the exact-cover search's job, and it enforces it by
/// <see cref="CardIds"/>.
/// </para>
/// <para>
/// A meld's <b>identity is its set of <see cref="CardId"/>s</b>, never its display value.
/// Two melds holding the same cards are the same cover element even if their jokers are
/// interpreted differently, so generators de-duplicate on <see cref="CardIds"/> and keep the
/// first interpretation they saw.
/// </para>
/// <para>
/// This type validates nothing. Whether the slots form a legal run or set is the generator's
/// responsibility (RULES.md §6).
/// </para>
/// </remarks>
public sealed class Meld
{
    private readonly MeldSlot[] _slots;
    private readonly HashSet<CardId> _cardIds;

    public Meld(MeldKind kind, IEnumerable<MeldSlot> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);

        Kind = kind;
        // Store a copy. The retired code aliased a list that backtracking then mutated
        // (BUILD-PLAN §3.4).
        _slots = [.. slots];
        if (_slots.Length < 3)
        {
            throw new ArgumentException("A meld holds at least three cards.", nameof(slots));
        }

        _cardIds = [.. _slots.Select(slot => slot.Card.Id)];
        if (_cardIds.Count != _slots.Length)
        {
            throw new ArgumentException("A meld cannot use the same card twice.", nameof(slots));
        }
    }

    /// <summary>Whether this meld is a run or a set (RULES.md §6).</summary>
    public MeldKind Kind { get; }

    /// <summary>The positions of the meld, in play order.</summary>
    public IReadOnlyList<MeldSlot> Slots => _slots;

    /// <summary>The physical cards the meld consumes, in play order.</summary>
    public IEnumerable<Card> Cards => _slots.Select(slot => slot.Card);

    /// <summary>
    /// The meld's identity: the instances it consumes. This is what de-duplication and the
    /// exact-cover search compare — never the display value.
    /// </summary>
    public IReadOnlySet<CardId> CardIds => _cardIds;

    /// <summary>How many cards the meld holds.</summary>
    public int Count => _slots.Length;

    /// <summary>How many of the positions are filled by a joker standing in for a card.</summary>
    public int JokerCount => _slots.Count(slot => slot.IsSubstitute);

    /// <summary>True when the two melds share a physical card, and so cannot both be played.</summary>
    public bool Overlaps(Meld other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return _cardIds.Overlaps(other._cardIds);
    }

    /// <summary>
    /// The set of <see cref="CardId"/>s a de-duplicating collection should key on. Exposed
    /// as the mutable-typed instance so callers can use
    /// <see cref="HashSet{T}.CreateSetComparer"/> without copying.
    /// </summary>
    internal HashSet<CardId> IdentityKey => _cardIds;

    public override string ToString() => $"{Kind}: {string.Join(" ", _slots.AsEnumerable())}";
}
