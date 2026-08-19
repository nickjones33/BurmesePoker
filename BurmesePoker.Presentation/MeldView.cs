using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Presentation;

/// <summary>
/// One meld of the hand's best cover, with each card already carrying what the round says
/// about it.
/// </summary>
/// <remarks>
/// <b>The domain meld is kept rather than flattened.</b> A front end needs the slots to show a
/// joker as what it is standing in for — <c>🃏R(4♥)</c> — and the interpretation is display
/// only, never identity (see <see cref="MeldSlot"/>).
/// </remarks>
public sealed class MeldView
{
    internal MeldView(Meld meld, IReadOnlyList<CardView> cards)
    {
        Meld = meld;
        Cards = cards;
    }

    /// <summary>The meld itself, for the slots and their interpretations.</summary>
    public Meld Meld { get; }

    /// <summary>Its cards, in play order, each aligned with the slot at the same index.</summary>
    public IReadOnlyList<CardView> Cards { get; }

    /// <summary>A run or a set (RULES.md §6).</summary>
    public MeldKind Kind => Meld.Kind;

    /// <summary>How many cards it holds.</summary>
    public int Count => Meld.Count;

    /// <summary>What it is called: <c>run</c> or <c>set</c>. A non-colour token (§3.11 A2).</summary>
    public string Label => DisplayTokens.For(Kind);

    /// <summary>The slots, paired with the view of the card filling each one.</summary>
    public IEnumerable<(MeldSlot Slot, CardView Card)> Slots =>
        Meld.Slots.Select((slot, index) => (slot, Cards[index]));
}
