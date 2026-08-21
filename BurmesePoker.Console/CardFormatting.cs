using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;

using Spectre.Console;

namespace BurmesePoker.Console;

/// <summary>
/// How a card, a hand and a meld are drawn. The only place that turns domain values into
/// Spectre markup.
/// </summary>
/// <remarks>
/// <para>
/// Ordering and glyphs come from the presentation layer — <see cref="CardOrder"/> and
/// <see cref="DisplayTokens"/> (BUILD-PLAN P13.1) — which is where they moved so that a second
/// front end shows a hand in the same order with the same markers. The order itself is the
/// salvaged <see cref="CardText"/>'s, so the console shows a hand as the 2023 front end did:
/// hearts, spades, clubs, diamonds, low to high, jokers last.
/// </para>
/// <para>
/// <b>Money markers are computed, never stored</b> (BUILD-PLAN §3.3): a card's multiplier
/// comes from the round's <see cref="MoneyCardRegistry"/> every time it is drawn, so nothing
/// here can go stale.
/// </para>
/// <para>
/// <b>Colour is added on top of a token that already says it</b> (§3.11 A2). Every marker below
/// is a <see cref="DisplayTokens"/> string first and a colour second, so nothing here means
/// anything only by being yellow.
/// </para>
/// </remarks>
public static class CardFormatting
{
    /// <summary>What the markers mean, for a footer under a hand. Defined in <see cref="Palette"/>.</summary>
    public const string Legend = Palette.Legend;

    /// <summary>The deadwood row's label, padded to the width of the widest meld label.</summary>
    internal static readonly string LooseLabel = Pad(DisplayTokens.For(CardDisplayState.Loose));

    /// <summary>A card on its own — rank, suit glyph, and red or black.</summary>
    public static string Of(Card card)
    {
        var face = card.IsJoker
            ? $"{CardText.DisplayCode(card.Rank)}{(card.Color == CardColor.Red ? "R" : "B")}"
            : $"{CardText.DisplayCode(card.Rank)}{CardText.DisplaySuit(card.Suit)}";

        // A card's own colour, and nothing to do with the palette: red and black are a
        // property of the card (RULES.md §2.2), not a thing the console is saying about it.
        return card.Color == CardColor.Red ? $"[red]{face}[/]" : $"[silver]{face}[/]";
    }

    /// <summary>
    /// A card with its money marker, starred if it is a money card the deck gave this player.
    /// </summary>
    /// <remarks>
    /// <b>The star is only ever on a money card.</b> Every card dealt is owned, so starring
    /// ownership alone would mark all thirteen and say nothing; what a player actually needs
    /// to see is which of the cards that <em>pay</em> pay them (RULES.md §4.4) — a money card
    /// with no star was picked up from a discard pile or claimed off the table, and pays
    /// somebody else.
    /// </remarks>
    public static string Of(Card card, MoneyCardRegistry money, bool owned = false) =>
        Decorated(card, money.Multiplier(card), owned);

    /// <summary>
    /// A card the view model has already described — the same face, from state rather than
    /// from a second registry lookup.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A card whose rank is closed says so on its face</b> (RULES.md §5.1). It is not a hint
    /// and it does not go away with the hints: the card is not among the choices at all, and a
    /// player holding it has to be able to see why.
    /// </remarks>
    public static string Of(CardView card) =>
        Decorated(card.Card, card.Multiplier, card.IsOwned)
        + (card.CanBeThrown ? string.Empty : $" [{Palette.Quiet}]{DisplayTokens.Closed}[/]");

    /// <summary>A hand in display order, with markers. Ownership is marked from the player's own view.</summary>
    public static string Hand(IReadOnlyList<Card> cards, MoneyCardRegistry money, Func<Card, bool>? owned = null) =>
        string.Join("  ", Sorted(cards).Select(card => Of(card, money, owned?.Invoke(card) ?? false)));

    /// <summary>Cards in display order. The order is <see cref="CardOrder.Display"/>'s.</summary>
    public static IEnumerable<Card> Sorted(IEnumerable<Card> cards) => CardOrder.Display(cards);

    /// <summary>
    /// One meld, with each joker showing what it stands in for — <c>🃏R(4♥)</c>.
    /// </summary>
    /// <remarks>
    /// The money arguments are optional because a meld is drawn in two places that want
    /// different things: a declaration is public and shows the cards as the table sees them,
    /// while a player's own hand wants the markers that say which of them pay <em>them</em>.
    /// </remarks>
    public static string Meld(Meld meld, MoneyCardRegistry? money = null, Func<Card, bool>? owned = null)
    {
        ArgumentNullException.ThrowIfNull(meld);

        var cards = meld.Slots.Select(slot => Slot(
            slot,
            money is null ? Of(slot.Card) : Of(slot.Card, money, owned?.Invoke(slot.Card) ?? false)));

        return Row(meld.Kind, cards);
    }

    /// <summary>One meld of a hand the view model has already described.</summary>
    public static string Meld(MeldView meld)
    {
        ArgumentNullException.ThrowIfNull(meld);

        return Row(meld.Kind, meld.Slots.Select(pair => Slot(pair.Slot, Of(pair.Card))));
    }

    /// <summary>A meld kind, padded to the width of the widest label so rows line up.</summary>
    public static string Label(MeldKind kind) => Pad(DisplayTokens.For(kind));

    /// <summary>
    /// A whole cover, longest meld first.
    /// </summary>
    /// <remarks>
    /// <b>This only sorts what the evaluator found; it does not re-cover the hand.</b>
    /// <see cref="HandEvaluator.TryFindCover"/> returns <em>a</em> partition, not the tidiest
    /// one — thirteen cards of one suit in sequence come back as four melds rather than one
    /// (BUILD-PLAN P8). Ordering by length puts the substantial melds first, which is as far
    /// as presentation can go without solving a different problem.
    /// </remarks>
    public static IEnumerable<string> Cover(IReadOnlyList<Meld> melds) => melds
        .OrderByDescending(meld => meld.Count)
        .ThenBy(meld => meld.Kind)
        .Select(meld => Meld(meld));

    /// <summary>A player's name, escaped — names are typed by a human and may contain markup.</summary>
    public static string Name(IReadOnlyDictionary<PlayerId, string> names, PlayerId player) =>
        $"[bold]{Markup.Escape(Plain(names, player))}[/]";

    /// <summary>A player's name unescaped and undecorated, for a narrow column.</summary>
    public static string Plain(IReadOnlyDictionary<PlayerId, string> names, PlayerId player) =>
        names.TryGetValue(player, out var name) ? name : player.ToString();

    /// <summary>The face, its money marker and its ownership star — the three in one place.</summary>
    private static string Decorated(Card card, int multiplier, bool owned)
    {
        var marker = multiplier switch
        {
            >= 2 => $" [{Palette.Money}]{DisplayTokens.For(CardDisplayState.PaysTriple)}[/]",
            1 => $" [{Palette.Money}]{DisplayTokens.For(CardDisplayState.PaysOnce)}[/]",
            _ => string.Empty
        };

        var star = owned && multiplier > 0 ? $" [{Palette.Good}]{Palette.OwnedMark}[/]" : string.Empty;

        return $"{Of(card)}{marker}{star}";
    }

    /// <summary>A slot's face, with what a joker is standing in for written after it.</summary>
    private static string Slot(MeldSlot slot, string face) => slot.IsSubstitute
        ? $"{face}[{Palette.Quiet}]({CardText.DisplayCode(slot.PlaysAs)}{CardText.DisplaySuit(slot.InSuit)})[/]"
        : face;

    private static string Row(MeldKind kind, IEnumerable<string> cards) =>
        $"[{Palette.Quiet}]{Label(kind)}[/] {string.Join("  ", cards)}";

    /// <summary>Labels are padded to a common width so the rows of a hand line up.</summary>
    private static string Pad(string label) => $"{label,-5}";
}
