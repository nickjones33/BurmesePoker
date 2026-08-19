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

    /// <summary>What the markers mean, for a footer under a hand.</summary>
    public const string Legend =
        "[grey]($) pays once · ($$) pays double · ★ the deck gave it to you, so it pays you even after you throw it[/]";

    /// <summary>A card on its own — rank, suit glyph, and red or black.</summary>
    public static string Of(Card card)
    {
        var face = card.IsJoker
            ? $"{CardText.DisplayCode(card.Rank)}{(card.Color == CardColor.Red ? "R" : "B")}"
            : $"{CardText.DisplayCode(card.Rank)}{CardText.DisplaySuit(card.Suit)}";

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
            2 => " [yellow]($$)[/]",
            1 => " [yellow]($)[/]",
            _ => string.Empty
        };

        return $"{Of(card)}{marker}{(owned && multiplier > 0 ? " [green]★[/]" : string.Empty)}";
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
    public static string Meld(Meld meld)
    {
        var cards = meld.Slots.Select(slot => slot.IsSubstitute
            ? $"{Of(slot.Card)}[grey]({CardText.DisplayCode(slot.PlaysAs)}{CardText.DisplaySuit(slot.InSuit)})[/]"
            : Of(slot.Card));

        return $"[grey]{meld.Kind.ToString().ToLowerInvariant()}[/] {string.Join(" ", cards)}";
    }

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
        .Select(Meld);

    /// <summary>A player's name, escaped — names are typed by a human and may contain markup.</summary>
    public static string Name(IReadOnlyDictionary<PlayerId, string> names, PlayerId player) =>
        $"[bold]{Markup.Escape(names.TryGetValue(player, out var name) ? name : player.ToString())}[/]";
}
