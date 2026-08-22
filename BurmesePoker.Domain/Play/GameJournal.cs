using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// A played game written down completely enough to be played again — a header, and every
/// decision every seat made (BUILD-PLAN §3.9, P14).
/// </summary>
/// <remarks>
/// <para>
/// <b>A seed is a pointer; a journal is the artifact.</b> A seed replays a bot game only
/// against the code that produced it, and it cannot replay a person at all. A journal records
/// the answers themselves, so it survives a strategy being edited and it records a human seat
/// exactly as it records a computer one.
/// </para>
/// <para>
/// <b>Pure data, and no I/O anywhere near it.</b> Turning one into lines is
/// <see cref="JournalFormat"/>'s job and writing those lines to a file is the consumer's
/// (BUILD-PLAN §2) — the domain still contains no <c>File</c>.
/// </para>
/// <para>
/// <b>Nothing in the engine knows this type exists.</b> A journal is written by a decorator
/// over <see cref="IPlayerAgent"/> (<see cref="Agents.JournalingAgent"/>) and read by an agent
/// that answers from it (<see cref="Agents.JournalPlayerAgent"/>), so replaying a game is
/// playing it with different seats. <see cref="RoundEngine"/> and <see cref="MatchEngine"/>
/// are untouched by the whole feature.
/// </para>
/// </remarks>
/// <param name="Header">The table this was played at, and the seed it was dealt from.</param>
/// <param name="Decisions">
/// Every answer given, in the order they were asked. One game is played one turn at a time, so
/// this is chronological across seats as well as within them.
/// </param>
public sealed record GameJournal(JournalHeader Header, IReadOnlyList<JournalDecision> Decisions);

/// <summary>How much of each decision is written down.</summary>
/// <remarks>
/// <b>The expensive one is opt-in and must stay that way</b> (BUILD-PLAN §3.9). Recording the
/// answers is a few bytes a turn; recording the hand alongside them copies thirteen cards
/// about fifty times a round, and §3.7 measured this work to be allocation-bound rather than
/// compute-bound. A throughput run takes <see cref="Thin"/>.
/// </remarks>
public enum JournalFidelity
{
    /// <summary>The answers alone. Enough to replay, and cheap enough to leave on.</summary>
    Thin,

    /// <summary>
    /// The answers plus what the seat could see when it gave them, which is what makes a
    /// journal analysable without replaying it.
    /// </summary>
    Rich
}

/// <summary>
/// The five questions an <see cref="IPlayerAgent"/> is ever asked. Exactly its five methods.
/// </summary>
public enum JournalQuestion
{
    /// <summary>Take the previous player's discard, or draw blind (RULES.md §5).</summary>
    Action,

    /// <summary>Which of the fourteen to throw away (RULES.md §5).</summary>
    Discard,

    /// <summary>Take the turned-up money card instead of drawing (RULES.md §4.5).</summary>
    Claim,

    /// <summary>
    /// Refuse the seat after you the turned-up money card (RULES.md §4.5) — the one decision
    /// recorded against a seat that is not on turn.
    /// </summary>
    Objection,

    /// <summary>Lay all thirteen down and end the round (RULES.md §7.1).</summary>
    Declare
}

/// <summary>One seat of the table this game was played at.</summary>
/// <param name="Player">Who sat there. The seat's position in the header's list is the turn order.</param>
/// <param name="Strategy">
/// What the results are attributed to — <c>greedy</c>, <c>simple</c>, <c>human</c>. Half of
/// every CSV row's join key (BUILD-PLAN §3.8 item 4), so a journal joins to the run that
/// produced it.
/// </param>
/// <param name="Name">What the seat was called at the table, when it was called anything.</param>
public sealed record JournalSeat(PlayerId Player, string Strategy, string? Name = null);

/// <summary>
/// The table a journal was played at: enough to set the same game up again.
/// </summary>
/// <remarks>
/// <b>The deal is not recorded, and does not need to be.</b> <see cref="Seed"/> is what the
/// match's <see cref="Random"/> was constructed with, and that alone reproduces every shuffle
/// and every reshuffle (BUILD-PLAN §3.7) — so a replay deals the same cards and then answers
/// from the file. ⚠️ It follows that a consumer must not draw anything else from the match's
/// generator before the first round, or a replay would deal a different game; the console
/// seats its table from a separate one for exactly this reason.
/// </remarks>
/// <param name="Seed">What the match's random number generator was seeded with.</param>
/// <param name="Seats">The seating, in turn order. The first seat opens every round (RULES.md §3).</param>
/// <param name="Stakes">What every round was played for.</param>
/// <param name="Rounds">
/// How many rounds settled. A replay plays this many — a game abandoned mid-round leaves its
/// unfinished decisions in the file, which are data rather than something to play back.
/// </param>
/// <param name="Fidelity">Which of the two levels was recorded.</param>
/// <param name="MasterSeed">The run's master seed, when there was a run. A join key.</param>
/// <param name="Game">The game's index in that run. A join key.</param>
/// <param name="Abandoned">Whether a round was given up on, which is why <paramref name="Rounds"/> stops where it does.</param>
/// <param name="RulesRevision">
/// Which revision of <c>RULES.md</c> the game was played under. A journal outlives the code
/// that wrote it, and a rules change is the one thing that would make an old one replay
/// differently without anybody noticing.
/// </param>
public sealed record JournalHeader(
    int Seed,
    IReadOnlyList<JournalSeat> Seats,
    Stakes Stakes,
    int Rounds,
    JournalFidelity Fidelity = JournalFidelity.Thin,
    int? MasterSeed = null,
    int? Game = null,
    bool Abandoned = false,
    int RulesRevision = JournalHeader.CurrentRulesRevision)
{
    /// <summary>
    /// The revision of <c>RULES.md</c> this build plays. ⚠️ Bump it when the rules document
    /// changes in a way that changes play.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It is bound to the document, not merely aspirationally beside it.</b> This constant
    /// sat at 13 through four play-changing revisions (P25–P28) because nothing compared the two
    /// (review R2). <c>GameJournalTests.TheRevisionStampedIsTheRevisionRulesMdIsAt</c> now parses
    /// the rev out of <c>docs/RULES.md</c>'s header and fails the build when they disagree — the
    /// P23 idiom — and the <c>/poker</c> skill's Phase 6 names the bump.
    /// ⚠️ <b>The binding is unconditional, so this moves with <em>every</em> revision and not only
    /// with the play-changing ones.</b> Rev 25 is the first that did not change play — three
    /// expert confirmations and one new settlement rule (§7.3) that nothing implements yet — and
    /// the stamp still moves, because a journal is a record of which document a build was reading.
    /// Rev 26 is the second: it corrected §7.3 (the bonus is <b>jokerless</b>, and it is ×2 at two,
    /// three or four seats and ×3 at five or more) and §7.3 is still unbuilt.
    /// </remarks>
    public const int CurrentRulesRevision = 26;

    /// <summary>How many seats were at the table.</summary>
    public int TableSize => Seats.Count;

    /// <summary>The seating as the engine wants it — players in turn order.</summary>
    public IReadOnlyList<PlayerId> Players => [.. Seats.Select(seat => seat.Player)];
}

/// <summary>
/// One answer, and what it was an answer to.
/// </summary>
/// <remarks>
/// <b>Answers are recorded by <see cref="CardId"/>, never by value</b> (BUILD-PLAN §3.1). Two
/// decks are in play, so there are two 5♥; a replay that threw the other copy would be a
/// different game from the one that was written down.
/// </remarks>
/// <param name="Round">Which round of the match, counting from 1.</param>
/// <param name="Turn">Which turn of that round, counting from 1.</param>
/// <param name="Player">Who was asked.</param>
/// <param name="Question">Which of the five.</param>
/// <param name="Answer">
/// The answer, as its canonical token — <c>take</c>/<c>draw</c>, a card id, or <c>yes</c>/<c>no</c>.
/// Read it with <see cref="AsAction"/>, <see cref="AsCardId"/> or <see cref="AsBoolean"/>
/// rather than by hand: the question decides which is meaningful, and those three say so.
/// </param>
/// <param name="Snapshot">What the seat could see, under <see cref="JournalFidelity.Rich"/> only.</param>
public sealed record JournalDecision(
    int Round,
    int Turn,
    PlayerId Player,
    JournalQuestion Question,
    string Answer,
    DecisionSnapshot? Snapshot = null)
{
    /// <summary>Writes down how a card was taken.</summary>
    public static JournalDecision Of(int round, int turn, PlayerId player, TurnAction action, DecisionSnapshot? snapshot = null) =>
        new(round, turn, player, JournalQuestion.Action, action == TurnAction.TakeDiscard ? "take" : "draw", snapshot);

    /// <summary>Writes down which card was thrown, by identity.</summary>
    public static JournalDecision Of(int round, int turn, PlayerId player, Card discard, DecisionSnapshot? snapshot = null) =>
        new(round, turn, player, JournalQuestion.Discard, discard.Id.Value.ToString(), snapshot);

    /// <summary>Writes down a yes-or-no answer.</summary>
    public static JournalDecision Of(int round, int turn, PlayerId player, JournalQuestion question, bool answer, DecisionSnapshot? snapshot = null) =>
        new(round, turn, player, question, answer ? "yes" : "no", snapshot);

    /// <summary>Reads the answer as a way of taking a card.</summary>
    public TurnAction AsAction() => Answer switch
    {
        "take" => TurnAction.TakeDiscard,
        "draw" => TurnAction.DrawFromDeck,
        _ => throw Unreadable("take or draw")
    };

    /// <summary>Reads the answer as the identity of a card.</summary>
    public CardId AsCardId() => int.TryParse(Answer, out var id) && id >= 0
        ? new CardId(id)
        : throw Unreadable("a card id");

    /// <summary>Reads the answer as a yes or a no.</summary>
    public bool AsBoolean() => Answer switch
    {
        "yes" => true,
        "no" => false,
        _ => throw Unreadable("yes or no")
    };

    private JournalException Unreadable(string wanted) => new(
        $"Round {Round} turn {Turn}, {Player}: the {Question} answer reads '{Answer}', which is not {wanted}.");
}

/// <summary>
/// What one seat could see when it was asked something — <see cref="JournalFidelity.Rich"/> only.
/// </summary>
/// <remarks>
/// <b>Cards by identity again</b>, and by identity alone: the shoe is index-aligned to
/// <see cref="CardId"/> (<see cref="TableState.Shoe"/>), so an analysis resolves a value
/// without the journal having to carry one. It is also what keeps the rich form to a list of
/// small integers rather than a list of objects.
/// </remarks>
/// <param name="Hand">The hand held at that moment — thirteen cards, or fourteen after the take.</param>
/// <param name="AvailableDiscard">What the previous player had left takeable, if anything.</param>
/// <param name="Taken">The card taken this turn, once it had been taken.</param>
/// <param name="DrawPileCount">How much deck was left.</param>
public sealed record DecisionSnapshot(
    IReadOnlyList<CardId> Hand,
    CardId? AvailableDiscard,
    CardId? Taken,
    int DrawPileCount)
{
    /// <summary>Takes a snapshot of what a context is showing.</summary>
    public static DecisionSnapshot Of(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new DecisionSnapshot(
            [.. context.Hand.Select(card => card.Id)],
            context.AvailableDiscard?.Id,
            context.Taken?.Id,
            context.DrawPileCount);
    }
}

/// <summary>
/// A journal and a game disagree, or a journal cannot be read.
/// </summary>
/// <remarks>
/// <b>Divergence must be loud</b> (BUILD-PLAN P14). A replay that quietly carried on after
/// being asked something the file does not answer would look successful while being a
/// different game, which is worse than no replay at all.
/// </remarks>
public sealed class JournalException(string message) : Exception(message);

/// <summary>
/// Collects a game's decisions as they are made. The mutable twin of <see cref="GameJournal"/>.
/// </summary>
/// <remarks>
/// One of these per game, shared by that game's seats. <b>Not thread-safe, and does not need
/// to be</b>: a table plays one turn at a time (BUILD-PLAN §3.6), and a parallel run gives each
/// game its own everything (P12).
/// </remarks>
public sealed class GameJournalBuilder(JournalFidelity fidelity = JournalFidelity.Thin)
{
    private readonly List<JournalDecision> _decisions = [];

    /// <summary>Which of the two levels the agents wrapped by this builder should record.</summary>
    public JournalFidelity Fidelity { get; } = fidelity;

    /// <summary>What has been written down so far.</summary>
    public IReadOnlyList<JournalDecision> Decisions => _decisions;

    /// <summary>Appends one answer.</summary>
    public void Append(JournalDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        _decisions.Add(decision);
    }

    /// <summary>Takes a snapshot if this builder is recording them, and null if it is not.</summary>
    public DecisionSnapshot? SnapshotOf(TurnContext context) =>
        Fidelity == JournalFidelity.Rich ? DecisionSnapshot.Of(context) : null;

    /// <summary>Seals what has been collected into a journal.</summary>
    public GameJournal Build(JournalHeader header) => new(header, [.. _decisions]);
}
