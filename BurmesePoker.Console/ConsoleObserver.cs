using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

using Spectre.Console;

namespace BurmesePoker.Console;

/// <summary>
/// Narrates the round to the table. Draws what everybody may see, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// <b>The domain narrates private information too, and filtering it is this class's job</b>
/// (BUILD-PLAN §3.5): <see cref="PlayerDrew"/> is told which card came off the deck, and
/// deliberately does not print it. What is public is what a player at the table could see —
/// a card taken from a discard pile, a claim off the table, a discard, and the declaration
/// (RULES.md §6.3).
/// </para>
/// <para>
/// The money that moved is <em>not</em> drawn here. Settlement returns net deltas only, and
/// splitting them into the round payment and the money-card side bet needs the ownership
/// record, which is on the table rather than in the result — so <c>Program</c> reports it
/// once the round is over (BUILD-PLAN P8).
/// </para>
/// </remarks>
public sealed class ConsoleObserver : IGameObserver
{
    private readonly IReadOnlyDictionary<PlayerId, string> _names;

    public ConsoleObserver(IReadOnlyDictionary<PlayerId, string> names) =>
        _names = names ?? throw new ArgumentNullException(nameof(names));

    public void RoundStarted(int round, IReadOnlyList<Card> turnedUp)
    {
        AnsiConsole.Write(new Rule($"Round {round}").LeftJustified());
        AnsiConsole.MarkupLine(
            "Turned up: " + string.Join("  ", turnedUp.Select(CardFormatting.Of))
            + "  [grey]— both copies of each pay their owner; 7♦ and A♠ always do[/]");
        AnsiConsole.WriteLine();
    }

    /// <remarks>
    /// <b>The card is not printed.</b> A blind draw is private — the whole table is told that
    /// somebody drew, and only they see what.
    /// </remarks>
    public void PlayerDrew(PlayerId player, Card card) =>
        AnsiConsole.MarkupLine($"{Who(player)} drew from the deck.");

    public void PlayerTookDiscard(PlayerId player, Card card) =>
        AnsiConsole.MarkupLine($"{Who(player)} took the discard {CardFormatting.Of(card)}.");

    public void MoneyCardClaimed(PlayerId player, Card card) =>
        AnsiConsole.MarkupLine(
            $"{Who(player)} claimed the turned-up {CardFormatting.Of(card)} off the table "
            + "[grey](held, but owned by nobody)[/].");

    /// <remarks>
    /// Public and worth saying out loud: the pile everybody has been throwing into is now the
    /// deck, so a card somebody discarded ten turns ago can come back (RULES.md §5).
    /// </remarks>
    public void DiscardsReshuffled(int cards) =>
        AnsiConsole.MarkupLine(
            $"[yellow]The draw pile ran out.[/] All {cards} discards were gathered and shuffled "
            + "into a new one [grey](a money card still pays whoever the deck gave it to first)[/].");

    public void PlayerDiscarded(PlayerId player, Card card) =>
        AnsiConsole.MarkupLine($"{Who(player)} discarded {CardFormatting.Of(card)}.");

    public void PlayerDeclared(PlayerId player, IReadOnlyList<Meld> melds)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"{Who(player)} declares").LeftJustified());

        foreach (var meld in CardFormatting.Cover(melds))
        {
            AnsiConsole.MarkupLine($"  {meld}");
        }

        AnsiConsole.WriteLine();
    }

    private string Who(PlayerId player) => CardFormatting.Name(_names, player);
}
