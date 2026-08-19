using Spectre.Console;
using Spectre.Console.Rendering;

namespace BurmesePoker.Console;

/// <summary>
/// What has happened this round, remembered so it can be drawn again after the screen is
/// cleared.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because concealment and narration are in direct conflict at one terminal.</b>
/// Play is fully concealed (RULES.md §6.3) and every seat is at the same keyboard, so a turn
/// begins by clearing the screen — which throws away every public thing that was said since
/// the player last looked. Before the bots arrived that cost a player three turns of
/// narration; with bots those three turns happen in milliseconds, so the only trace of them
/// was the one discard the table panel happened to show (BUILD-PLAN P11).
/// </para>
/// <para>
/// <b>It remembers markup, not events.</b> The alternative — keeping the events and
/// re-rendering them — would put a second copy of the narration wording next to
/// <see cref="ConsoleObserver"/>'s, and the two would drift. The observer says a thing once
/// and hands the same string here.
/// </para>
/// <para>
/// <b>Nothing survives the round.</b> <see cref="MatchEngine"/> deliberately keeps no
/// per-round history (BUILD-PLAN §3.8) and neither does this: what carries between rounds is
/// the standings, which <c>Program</c> remembers for itself.
/// </para>
/// </remarks>
public sealed class RoundLog
{
    /// <summary>How many lines the panel shows by default — about a turn each for a full table.</summary>
    public const int DefaultLines = 10;

    private readonly List<string> _lines = [];

    /// <summary>The round these lines belong to. Zero before the first one starts.</summary>
    public int Round { get; private set; }

    /// <summary>Forgets the last round and begins another.</summary>
    public void StartRound(int round)
    {
        Round = round;
        _lines.Clear();
    }

    /// <summary>Remembers one line of public narration, exactly as it was printed.</summary>
    public void Add(string markup) => _lines.Add(markup);

    /// <summary>The most recent lines, oldest first.</summary>
    public IReadOnlyList<string> Recent(int lines) =>
        lines >= _lines.Count ? _lines : _lines[^lines..];

    /// <summary>
    /// The log as a panel to sit above a hand: what everybody at the table saw happen since
    /// this player last looked.
    /// </summary>
    public IRenderable AsPanel(int lines = DefaultLines)
    {
        var recent = Recent(lines);

        var rows = new Rows(recent.Count == 0
            ? [new Markup("[grey]Nothing yet — you open.[/]")]
            : recent.Select(IRenderable (line) => new Markup(line)).ToArray());

        var panel = new Panel(rows)
            .Header($"Round {Round} so far")
            .BorderColor(Palette.Frame);

        return _lines.Count > recent.Count
            ? panel.Header($"Round {Round} so far [grey](last {recent.Count} of {_lines.Count})[/]")
            : panel;
    }
}
