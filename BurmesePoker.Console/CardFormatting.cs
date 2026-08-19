using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

using Spectre.Console;

namespace BurmesePoker.Console;

/// <summary>
/// How a card, a hand and a meld are drawn. The only place that turns domain values into
/// Spectre markup.
/// </summary>
/// <remarks>
/// <para>
/// Ordering and glyphs come from the salvaged <see cref="CardText"/>, so the console shows a
/// hand in the order the 2023 front end did — hearts, spades, clubs, diamonds, low to high,
/// jokers last.
/// </para>
/// <para>
/// <b>Money markers are computed, never stored</b> (BUILD-PLAN §3.3): a card's multiplier
/// comes from the round's <see cref="MoneyCardRegistry"/> every time it is drawn, so nothing
/// here can go stale.
/// </para>
/// </remarks>
public static class CardFormatting
{
    private static readonly Dictionary<Suit, int> SuitOrder = CardText.AllSuits
        .Select((suit, index) => (suit, index))
        .ToDictionary(entry => entry.suit, entry => entry.index);

    /// <summary>What the markers mean, for a footer under a hand. Defined in <see cref="Palette"/>.</summary>
    public const string Legend = Palette.Legend;

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
    public static string Of(Card card, MoneyCardRegistry money, bool owned = false)
    {
        var multiplier = money.Multiplier(card);

        var marker = multiplier switch
        {
            2 => $" [{Palette.Money}]($$)[/]",
            1 => $" [{Palette.Money}]($)[/]",
            _ => string.Empty
        };

        var star = owned && multiplier > 0 ? $" [{Palette.Good}]{Palette.OwnedMark}[/]" : string.Empty;

        return $"{Of(card)}{marker}{star}";
    }

    /// <summary>A hand in display order, with markers. Ownership is marked from the player's own view.</summary>
    public static string Hand(IReadOnlyList<Card> cards, MoneyCardRegistry money, Func<Card, bool>? owned = null) =>
        string.Join("  ", Sorted(cards).Select(card => Of(card, money, owned?.Invoke(card) ?? false)));

    /// <summary>Cards in display order: by suit as <see cref="CardText.AllSuits"/> lists them, then by rank.</summary>
    public static IEnumerable<Card> Sorted(IEnumerable<Card> cards) => cards
        .OrderBy(card => card.Suit is null ? CardText.AllSuits.Count : SuitOrder[card.Suit.Value])
        .ThenBy(card => CardText.Order(card.Rank))
        .ThenBy(card => card.Color)
        .ThenBy(card => card.Id.Value);

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
        var cards = meld.Slots.Select(slot =>
        {
            var face = money is null ? Of(slot.Card) : Of(slot.Card, money, owned?.Invoke(slot.Card) ?? false);

            return slot.IsSubstitute
                ? $"{face}[{Palette.Quiet}]({CardText.DisplayCode(slot.PlaysAs)}{CardText.DisplaySuit(slot.InSuit)})[/]"
                : face;
        });

        return $"[{Palette.Quiet}]{Label(meld.Kind)}[/] {string.Join("  ", cards)}";
    }

    /// <summary>A meld kind, padded to the width of the widest label so rows line up.</summary>
    public static string Label(MeldKind kind) => $"{kind.ToString().ToLowerInvariant(),-5}";

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
}
