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
/// <para>
/// <b>Everything it says, it also remembers</b> (BUILD-PLAN P11). A turn begins by clearing
/// the screen for concealment, so narration printed live is gone by the time the next player
/// looks; the same markup goes into a <see cref="RoundLog"/> that
/// <see cref="SpectrePlayerAgent"/> draws again after the clear. It is said <em>and</em> kept
/// rather than only kept, because a table of nothing but bots never clears the screen and
/// would otherwise watch a blank one.
/// </para>
/// </remarks>
public sealed class ConsoleObserver : IGameObserver
{
    private readonly IAnsiConsole _console;
    private readonly IReadOnlyDictionary<PlayerId, string> _names;
    private readonly RoundLog _log;

    /// <param name="console">
    /// The terminal to narrate to. Taken rather than reached for (BUILD-PLAN P13.1).
    /// </param>
    public ConsoleObserver(IAnsiConsole console, IReadOnlyDictionary<PlayerId, string> names, RoundLog log)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _names = names ?? throw new ArgumentNullException(nameof(names));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <remarks>
    /// ⚠️ <b>The seating is said every round because it changes every round</b> (RULES.md §3
    /// step 2). It used to be printed once at setup, which was true of a match that kept its
    /// seats and is a lie about one that re-draws them.
    /// </remarks>
    public void RoundStarted(int round, IReadOnlyList<PlayerId> seating, IReadOnlyList<Card> turnedUp)
    {
        _log.StartRound(round);

        _console.Write(new Rule($"Round {round}").LeftJustified());
        Say(
            $"[{Palette.Quiet}]Seats drawn:[/] "
            + string.Join(" → ", seating.Select(Who))
            + $"  [{Palette.Quiet}]— {Who(seating[0])} opens; the seats are re-drawn every round[/]");
        Say(
            "Turned up: " + string.Join("  ", turnedUp.Select(CardFormatting.Of))
            + $"  [{Palette.Quiet}]— both copies of each pay their owner; 7♦ and A♠ always do[/]");
        _console.WriteLine();
    }

    /// <remarks>
    /// <b>The card is not printed.</b> A blind draw is private — the whole table is told that
    /// somebody drew, and only they see what.
    /// </remarks>
    public void PlayerDrew(PlayerId player, Card card) =>
        Say($"{Who(player)} drew from the deck.");

    public void PlayerTookDiscard(PlayerId player, Card card) =>
        Say($"{Who(player)} took the discard {CardFormatting.Of(card)}.");

    public void MoneyCardClaimed(PlayerId player, Card card) =>
        Say(
            $"{Who(player)} claimed the turned-up {CardFormatting.Of(card)} off the table "
            + $"[{Palette.Quiet}](held, but owned by nobody)[/].");

    /// <remarks>
    /// Public and worth saying out loud: the pile everybody has been throwing into is now the
    /// deck, so a card somebody discarded ten turns ago can come back (RULES.md §5).
    /// </remarks>
    public void DiscardsReshuffled(int cards) =>
        Say(
            $"[{Palette.Money}]The draw pile ran out.[/] All {cards} discards were gathered and "
            + $"shuffled into a new one [{Palette.Quiet}](a money card still pays whoever the deck "
            + "gave it to first)[/].");

    /// <remarks>
    /// ⚠️ <b>Worth saying plainly, because it is the one thing a player tells the table about
    /// their own hand.</b> Only a holder may refuse (RULES.md §4.5), so the refusal itself says
    /// the objector is holding that rank.
    /// </remarks>
    public void ClaimRefused(PlayerId objector, PlayerId claimant, Card card) =>
        Say(
            $"{Who(objector)} refused {Who(claimant)} the turned-up {CardFormatting.Of(card)} "
            + $"[{Palette.Quiet}](which they may only by holding one — RULES.md §4.5)[/].");

    public void PlayerDiscarded(PlayerId player, Card card) =>
        Say($"{Who(player)} discarded {CardFormatting.Of(card)}.");

    public void PlayerDeclared(PlayerId player, IReadOnlyList<Meld> melds)
    {
        _console.WriteLine();
        _console.Write(new Rule($"{Who(player)} declares").LeftJustified());

        foreach (var meld in CardFormatting.Cover(melds))
        {
            _console.MarkupLine($"  {meld}");
        }

        _log.Add($"{Who(player)} [{Palette.Good}]declared[/] and went out.");
        _console.WriteLine();
    }

    /// <summary>Says it to the table now, and keeps it for whoever looks after the next clear.</summary>
    private void Say(string markup)
    {
        _console.MarkupLine(markup);
        _log.Add(markup);
    }

    private string Who(PlayerId player) => CardFormatting.Name(_names, player);
}
