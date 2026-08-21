using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Presentation;

/// <summary>
/// One card as a front end needs it: the card itself, everything the round says about it, and
/// what throwing it would cost.
/// </summary>
/// <remarks>
/// <para>
/// <b>Data, not markup.</b> Everything here is a value a Spectre console, a Razor component or
/// a test can read; nothing here knows how any of them draws it (BUILD-PLAN P13.1).
/// </para>
/// <para>
/// <b>The multiplier is carried, not the fact that it is a money card</b>, because a front end
/// wants to say <em>which</em>: one designation pays once, two pay double (RULES.md §4.3).
/// <see cref="State"/> says the same thing as a flag, and the two cannot disagree — both are
/// built in one place from one registry lookup.
/// </para>
/// </remarks>
/// <param name="Card">The physical card. Instance identity, so two copies of a value are two views.</param>
/// <param name="State">Everything true of this card in this hand, as flags.</param>
/// <param name="Multiplier">
/// How many times this card's <em>value</em> pays this round: 0, 1 or 3 (RULES.md §4.1).
/// ⚠️ Never 5 — the jackpot needs the round's ownership and is settled, not drawn.
/// </param>
/// <param name="CostOfThrowing">
/// How many melded cards throwing this one gives up — the cover of the twelve that would be
/// left against the cover of the thirteen. Zero for deadwood.
/// </param>
public readonly record struct CardView(
    Card Card,
    CardDisplayState State,
    int Multiplier,
    int CostOfThrowing)
{
    /// <summary>Whether this card's value pays anybody this round.</summary>
    public bool IsMoneyCard => Multiplier > 0;

    /// <summary>Whether the deck gave it to the player whose view this is (RULES.md §4.4).</summary>
    public bool IsOwned => State.HasFlag(CardDisplayState.Owned);

    /// <summary>Whether the best cover of the hand uses it.</summary>
    public bool IsMelded => State.HasFlag(CardDisplayState.Melded);

    /// <summary>Whether the computer would throw this one away.</summary>
    public bool IsSuggestedThrow => State.HasFlag(CardDisplayState.SuggestedThrow);

    /// <summary>Every non-colour token this card carries, in flag order (§3.11 A2).</summary>
    public IEnumerable<string> Tokens => DisplayTokens.All(State);
}
