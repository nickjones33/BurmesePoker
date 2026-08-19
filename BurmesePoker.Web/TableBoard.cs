using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;

namespace BurmesePoker.Web;

/// <summary>
/// The public game as a watcher has it: the seats, the banks, what is on the table and
/// everything that has been said — folded out of the event stream and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Built from <see cref="TableEvent"/>s alone, and that is the whole point</b>
/// (BUILD-PLAN P13.3 acceptance 2). The <see cref="TableSession"/> is right there and could be
/// asked for its banks directly; a component that did so would be <em>a second route to the
/// table that bypasses the fan-out</em>, which is the thing this packet is supposed to refuse.
/// So the board knows exactly what a connection was told and not one card more, and a leak
/// would have to come through <c>TableFanOut</c> — where <c>ConcealmentTests</c> is already
/// standing.
/// </para>
/// <para>
/// <b>Immutable, and folded once per event by the host</b> rather than recomputed per render.
/// A Blazor component may render at any moment on any thread; handing it a value that cannot
/// change under it is the same rule P13.1 found the hard way about hands, one layer out.
/// </para>
/// <para>
/// ⚠️ <b>This is the observer stream, not <c>RoundLog</c>.</b> The console keeps a log because
/// it clears the screen between turns for concealment; a browser never loses its scrollback,
/// so what is ported is the stream and not the panel (BUILD-PLAN §3.11).
/// </para>
/// </remarks>
public sealed record TableBoard
{
    /// <summary>How many lines of narration are kept. A watcher scrolls; a match is unbounded.</summary>
    public const int LogKept = 240;

    private TableBoard(IReadOnlyList<PlayerId> seating, IReadOnlyDictionary<PlayerId, string> names)
    {
        Seating = seating;
        Names = names;
        Banks = seating.ToDictionary(player => player, _ => 0);
        LastDiscards = new Dictionary<PlayerId, Card?>();
        TurnedUp = [];
        Log = [];
        Declaration = [];
    }

    /// <summary>An empty table, before the first deal.</summary>
    public static TableBoard Of(
        IReadOnlyList<PlayerId> seating,
        IReadOnlyDictionary<PlayerId, string> names)
    {
        ArgumentNullException.ThrowIfNull(seating);
        ArgumentNullException.ThrowIfNull(names);

        return new TableBoard([.. seating], names.ToDictionary(entry => entry.Key, entry => entry.Value));
    }

    /// <summary>The seating order, which is also the turn order (RULES.md §3).</summary>
    public IReadOnlyList<PlayerId> Seating { get; private init; }

    /// <summary>What each seat is called.</summary>
    public IReadOnlyDictionary<PlayerId, string> Names { get; private init; }

    /// <summary>Which round is being played, or 0 before the first deal.</summary>
    public int Round { get; private init; }

    /// <summary>How many rounds have been settled.</summary>
    public int RoundsPlayed { get; private init; }

    /// <summary>The turned-up money cards still on the table (RULES.md §4.2, §4.5).</summary>
    public IReadOnlyList<Card> TurnedUp { get; private init; }

    /// <summary>Each seat's running total, accumulated from the settlements the table was told about.</summary>
    public IReadOnlyDictionary<PlayerId, int> Banks { get; private init; }

    /// <summary>The top of each seat's discard pile, as far as a watcher has seen.</summary>
    public IReadOnlyDictionary<PlayerId, Card?> LastDiscards { get; private init; }

    /// <summary>Whoever moved last — as close to "whose turn" as the public narration gets.</summary>
    public PlayerId? Acting { get; private init; }

    /// <summary>Who declared and ended the round, once somebody has.</summary>
    public PlayerId? Declarer { get; private init; }

    /// <summary>The melds they laid down (RULES.md §7.1).</summary>
    public IReadOnlyList<Meld> Declaration { get; private init; }

    /// <summary>The money worked out, once the round is over.</summary>
    public RoundResult? Settlement { get; private init; }

    /// <summary>The host gave up on the round — the people left, not the play (BUILD-PLAN P13.2).</summary>
    public bool Abandoned { get; private init; }

    /// <summary>Everything said at the table, oldest first, latest <see cref="LogKept"/> kept.</summary>
    public IReadOnlyList<LogLine> Log { get; private init; }

    /// <summary>How many lines have been said in all, trimmed ones included.</summary>
    public int Narrated { get; private init; }

    /// <summary>Whether a round is under way — dealt, and not yet settled or given up on.</summary>
    public bool InPlay => Round > 0 && Settlement is null && !Abandoned;

    /// <summary>The seats in seating order, each with everything a watcher may show about it.</summary>
    public IEnumerable<SeatLine> Seats => Seating.Select(player => new SeatLine(
        player,
        Names.TryGetValue(player, out var name) ? name : $"Seat {player.Value}",
        Banks.TryGetValue(player, out var bank) ? bank : 0,
        LastDiscards.TryGetValue(player, out var discard) ? discard : null,
        IsActing: Acting == player && InPlay,
        HasDeclared: Declarer == player,
        Won: Settlement?.Winner == player));

    /// <summary>What one seat took home this round, or null before it is settled.</summary>
    public int? PayoutOf(PlayerId player) =>
        Settlement is { } settled && settled.Payouts.TryGetValue(player, out var money) ? money : null;

    /// <summary>
    /// The board after one more thing happened.
    /// </summary>
    /// <remarks>
    /// Exhaustive over <see cref="TableEvent"/>, which is a closed hierarchy — so an event
    /// added later stops compiling here rather than being silently ignored by the one thing
    /// that draws the table.
    /// </remarks>
    public TableBoard After(TableEvent moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        return moment switch
        {
            TableEvent.RoundStarted started => (this with
            {
                Round = started.Round,
                TurnedUp = [.. started.TurnedUp],
                LastDiscards = new Dictionary<PlayerId, Card?>(),
                Acting = null,
                Declarer = null,
                Declaration = [],
                Settlement = null,
                Abandoned = false
            }).Say(
                started.Round,
                "The deal is done. Turned up on the table:",
                started.TurnedUp,
                LogTone.Money),

            // A watcher is told that somebody drew and never what (TableFanOut). The card is
            // non-null only for the seat that drew it, which this page never is — it is read
            // here rather than assumed away, because P13.4 seats a player at the same board.
            TableEvent.Drew drew => Moved(drew.Player).Say(
                Round,
                drew.Card is null
                    ? $"{Who(drew.Player)} drew from the deck."
                    : $"{Who(drew.Player)} drew from the deck:",
                drew.Card is { } seen ? [seen] : [],
                LogTone.Normal),

            TableEvent.TookDiscard took => Moved(took.Player).Say(
                Round,
                $"{Who(took.Player)} took the discard",
                [took.Card],
                LogTone.Normal),

            // RULES.md §4.5: the opening player may take the top turned-up card, which leaves
            // the table. Held, but owned by nobody.
            TableEvent.MoneyCardClaimed claimed => (Moved(claimed.Player) with
            {
                TurnedUp = Without(TurnedUp, claimed.Card)
            }).Say(
                Round,
                $"{Who(claimed.Player)} claimed the turned-up card off the table — held, but owned by nobody:",
                [claimed.Card],
                LogTone.Money),

            TableEvent.Discarded discarded => (Moved(discarded.Player) with
            {
                LastDiscards = With(LastDiscards, discarded.Player, discarded.Card)
            }).Say(
                Round,
                $"{Who(discarded.Player)} discarded",
                [discarded.Card],
                LogTone.Normal),

            // RULES.md §5: the discards become the new draw pile, so a card thrown ten turns
            // ago can come back. Worth saying out loud.
            TableEvent.DiscardsReshuffled reshuffled => Say(
                Round,
                $"The draw pile ran out. All {reshuffled.Cards} discards were gathered and shuffled "
                + "into a new one — a money card still pays whoever the deck gave it to first.",
                [],
                LogTone.Money),

            TableEvent.Declared declared => (Moved(declared.Player) with
            {
                Declarer = declared.Player,
                Declaration = [.. declared.Melds]
            }).Say(
                Round,
                $"{Who(declared.Player)} declared and went out.",
                [],
                LogTone.Good),

            TableEvent.Settled settled => (this with
            {
                Settlement = settled.Result,
                RoundsPlayed = settled.Result.Round,
                Banks = Banked(Banks, settled.Result),
                Acting = null
            }).Say(
                Round,
                $"The money is settled. {Who(settled.Result.Winner)} won the round in "
                + $"{settled.Result.Turns} turns.",
                [],
                LogTone.Good),

            TableEvent.SeatPlayedByTheComputer taken => Say(
                taken.Round,
                $"{Who(taken.Player)} is being played by the computer.",
                [],
                LogTone.Quiet),

            TableEvent.TableAbandoned abandoned => (this with
            {
                Abandoned = true,
                Acting = null
            }).Say(
                abandoned.Round,
                $"Round {abandoned.Round} was given up on after {abandoned.Limit:g} — only a "
                + "declaration ends a round, so a table nobody is left at never finishes.",
                [],
                LogTone.Bad),

            _ => this
        };
    }

    private string Who(PlayerId player) =>
        Names.TryGetValue(player, out var name) ? name : $"Seat {player.Value}";

    private TableBoard Moved(PlayerId player) => this with { Acting = player };

    /// <summary>
    /// The board with one more line of narration on it.
    /// </summary>
    /// <remarks>
    /// <see cref="Narrated"/> counts every line ever said and <see cref="LogLine.Sequence"/> is
    /// taken from it, <b>not from the length of the kept log</b> — the log is trimmed, and a
    /// key that repeats is a key that makes Blazor reuse the wrong DOM node (§3.11 C14).
    /// </remarks>
    private TableBoard Say(int round, string text, IReadOnlyList<Card> cards, LogTone tone)
    {
        var line = new LogLine(Narrated, round, text, [.. cards], tone);
        var kept = Log.Count + 1 > LogKept ? Log.Skip(Log.Count + 1 - LogKept) : Log;

        return this with { Narrated = Narrated + 1, Log = [.. kept, line] };
    }

    private static IReadOnlyList<Card> Without(IReadOnlyList<Card> cards, Card gone) =>
        // Instance identity: the other copy of the same value stays on the table (§3.1).
        [.. cards.Where(card => card.Id != gone.Id)];

    private static IReadOnlyDictionary<PlayerId, Card?> With(
        IReadOnlyDictionary<PlayerId, Card?> discards,
        PlayerId player,
        Card card) =>
        new Dictionary<PlayerId, Card?>(discards) { [player] = card };

    private static IReadOnlyDictionary<PlayerId, int> Banked(
        IReadOnlyDictionary<PlayerId, int> banks,
        RoundResult result)
    {
        var copy = new Dictionary<PlayerId, int>(banks);

        foreach (var (player, money) in result.Payouts)
        {
            copy[player] = (copy.TryGetValue(player, out var had) ? had : 0) + money;
        }

        return copy;
    }
}

/// <summary>One seat as a watcher may show it.</summary>
/// <param name="Player">Which seat.</param>
/// <param name="Name">What to call whoever is in it.</param>
/// <param name="Bank">Their running total across the rounds this page has watched.</param>
/// <param name="LastDiscard">The top of their discard pile, or null before they have thrown one.</param>
/// <param name="IsActing">Whether theirs was the last move — the nearest the public game gets to "whose turn".</param>
/// <param name="HasDeclared">Whether they laid down all thirteen and ended the round.</param>
/// <param name="Won">Whether the settlement names them the winner.</param>
public sealed record SeatLine(
    PlayerId Player,
    string Name,
    int Bank,
    Card? LastDiscard,
    bool IsActing,
    bool HasDeclared,
    bool Won);

/// <summary>
/// What a line of narration is about, as a name rather than a colour.
/// </summary>
/// <remarks>
/// The same rule as <c>CardDisplayState</c> one layer out (§3.11 A2): the projection says what
/// a line <em>is</em> and the stylesheet decides what that looks like. Nothing here is
/// understood by colour alone — every line is a sentence.
/// </remarks>
public enum LogTone
{
    /// <summary>A move: a draw, a discard, a card taken.</summary>
    Normal,

    /// <summary>The money layer doing something — a deal, a claim, a reshuffle.</summary>
    Money,

    /// <summary>A declaration or a settlement.</summary>
    Good,

    /// <summary>Context nobody is being asked to act on.</summary>
    Quiet,

    /// <summary>The round did not finish.</summary>
    Bad
}

/// <summary>
/// One thing said at the table.
/// </summary>
/// <param name="Sequence">Its place in the narration — a stable <c>@key</c> for the log list.</param>
/// <param name="Round">Which round said it.</param>
/// <param name="Text">The sentence. Complete on its own, with no card in it.</param>
/// <param name="Cards">The cards it is about, drawn after the sentence. Public cards only.</param>
/// <param name="Tone">What kind of thing happened.</param>
public sealed record LogLine(
    int Sequence,
    int Round,
    string Text,
    IReadOnlyList<Card> Cards,
    LogTone Tone);
