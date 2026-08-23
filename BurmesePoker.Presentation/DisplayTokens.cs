using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Presentation;

/// <summary>
/// A short piece of text for every display state, so that nothing a front end shows depends on
/// colour to be understood.
/// </summary>
/// <remarks>
/// <para>
/// <b>WCAG 1.4.1, taken as a standard before there is a second front end to retrofit</b>
/// (BUILD-PLAN §3.11 A2). The rule is that colour never carries meaning on its own. The
/// console already obeys it — <c>($)</c>, <c>($$$)</c>, <c>★</c>, <c>←</c> and the
/// <c>run</c>/<c>set</c>/<c>loose</c> row labels are all glyphs that survive a greyscale
/// screen — so this is where those markers move to, not where they are invented.
/// </para>
/// <para>
/// <b>Every single flag of <see cref="CardDisplayState"/> has one, and a test says so.</b>
/// That is the point of putting them here rather than beside the renderer: a state added later
/// without a token fails the build's tests rather than quietly shipping a colour-only marker.
/// </para>
/// <para>
/// Padding, brackets and colour are the renderer's. These are the words.
/// </para>
/// </remarks>
public static class DisplayTokens
{
    /// <summary>Marks a money card the deck gave this player.</summary>
    public const string Owned = "★";

    /// <summary>Marks the card the computer would throw away.</summary>
    public const string SuggestedThrow = "←";

    /// <summary>
    /// Marks a card whose rank is closed to you this turn (RULES.md §5.1) — a word rather than a
    /// glyph, because the thing it names is a rule and has to survive being read aloud.
    /// </summary>
    public const string Closed = "closed";

    /// <summary>
    /// Marks a card lying face up: taken in the open, visible to every player for as long as
    /// it stays in the hand (RULES.md §5.2).
    /// </summary>
    public const string FaceUp = "▲";

    /// <summary>
    /// The token for a single display state.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="state"/> is <see cref="CardDisplayState.None"/>, or more than one flag
    /// at once — a token names one thing, and a card wearing several shows several.
    /// </exception>
    public static string For(CardDisplayState state) => state switch
    {
        CardDisplayState.Melded => "meld",
        CardDisplayState.Loose => "loose",
        CardDisplayState.PaysOnce => "($)",
        CardDisplayState.PaysTriple => "($$$)",
        CardDisplayState.Owned => Owned,
        CardDisplayState.SuggestedThrow => SuggestedThrow,
        CardDisplayState.Unthrowable => Closed,
        CardDisplayState.FaceUp => FaceUp,
        _ => throw new ArgumentException(
            $"{state} is not a single display state, so it has no one token.", nameof(state))
    };

    /// <summary>
    /// The tokens for everything a card is, in flag order.
    /// </summary>
    public static IEnumerable<string> All(CardDisplayState state) => States
        .Where(flag => state.HasFlag(flag))
        .Select(For);

    /// <summary>
    /// What a meld is called: <c>run</c> or <c>set</c> (RULES.md §6). The label a melded card
    /// sits under, and the non-colour token that says which kind of meld it is in.
    /// </summary>
    public static string For(MeldKind kind) => kind.ToString().ToLowerInvariant();

    /// <summary>
    /// Every state that is one thing — <see cref="CardDisplayState.None"/> is not one. What a
    /// legend iterates, and what the §3.11 A2 test asserts over so that a state added later
    /// cannot ship without a token.
    /// </summary>
    public static readonly IReadOnlyList<CardDisplayState> States =
    [
        .. Enum.GetValues<CardDisplayState>().Where(state => state != CardDisplayState.None)
    ];
}
