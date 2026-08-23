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
    Declare,

    /// <summary>
    /// Shall we change seats (RULES.md §3 step 2, §9 #45)? 🔥 <b>The one decision recorded
    /// <em>between</em> rounds</b> — its turn is 0, because there is no turn it belongs to — and
    /// the only one whose answer is neither a card nor a yes-or-no.
    /// </summary>
    Seating
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
    int RulesRevision = JournalHeader.CurrentRulesRevision,
    int RoundsBetweenSeatings = 0)
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
    /// Rev 27 is the third: §7.4 (a win from the initial deal pays ×2) and §7.5 (a third
    /// consecutive win is paid entirely by the seat above the winner), both from Aung Aung, both
    /// settlement, and both unbuilt — see packet P35.
    /// Rev 28 is the first that changes play since rev 23 and the first to <em>withdraw</em> a
    /// reading this project built: §3 step 2 no longer re-draws the seating before every deal —
    /// a seating is held until somebody asks to change it — so <c>MatchEngine</c> contradicts the
    /// document until P36 lands (§10 #22).
    /// Rev 29 rules §9 #45: a re-seating happens when the players <em>agree</em>, not when one of
    /// them asks — <c>PLAYER</c>, and the work splits into P36 (hold the seating) and P37 (the
    /// agreeing).
    /// </remarks>
    public const int CurrentRulesRevision = 29;

    /// <summary>
    /// How long the seating held (RULES.md §3 step 2), as the policy this match played under.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A journal has to record this or a replay deals different seats</b> (P36). The field
    /// is the number rather than the policy because a header is a line of JSON and
    /// <b>0 — absent — is <c>held</c></b>, which is both the default and the rule; a journal
    /// written before P36 therefore reads back as the policy the engine will now play it under.
    /// ⚠️ <b>A journal written between P28 and P36 is the one exception and cannot say so</b>: it
    /// was written by an engine that re-drew every round and carries no field to prove it, which
    /// is what <c>CurrentRulesRevision</c> 28 is for.
    /// </remarks>
    public SeatingPolicy Seating => SeatingPolicy.Of(RoundsBetweenSeatings);

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
/// <param name="Advice">
/// What the computer would have answered, when a seat was shown its opinion — null everywhere
/// else, which is every bot seat and every seat at a table nobody asked for advice at (P24.2).
/// </param>
public sealed record JournalDecision(
    int Round,
    int Turn,
    PlayerId Player,
    JournalQuestion Question,
    string Answer,
    DecisionSnapshot? Snapshot = null,
    JournalAdvice? Advice = null)
{
    /// <summary>
    /// Whether the seat and the computer chose differently — <b>the query this whole field
    /// exists for</b> (BUILD-PLAN P24.2). False when nothing advised.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>By <see cref="CardId"/>, exactly as <see cref="Answer"/> is written</b> (§3.1). Two
    /// decks hold two 5♥, and a comparison that said <em>"she agreed"</em> because the values
    /// matched would be wrong on precisely the hands worth studying.
    /// </remarks>
    public bool DisagreedWithTheComputer =>
        Advice is { } advice && Question == JournalQuestion.Discard && AsCardId() != advice.Card;

    /// <summary>Writes down how a card was taken.</summary>
    public static JournalDecision Of(int round, int turn, PlayerId player, TurnAction action, DecisionSnapshot? snapshot = null) =>
        new(round, turn, player, JournalQuestion.Action, action == TurnAction.TakeDiscard ? "take" : "draw", snapshot);

    /// <summary>Writes down which card was thrown, by identity.</summary>
    public static JournalDecision Of(int round, int turn, PlayerId player, Card discard, DecisionSnapshot? snapshot = null) =>
        new(round, turn, player, JournalQuestion.Discard, discard.Id.Value.ToString(), snapshot);

    /// <summary>Writes down a yes-or-no answer.</summary>
    public static JournalDecision Of(int round, int turn, PlayerId player, JournalQuestion question, bool answer, DecisionSnapshot? snapshot = null) =>
        new(round, turn, player, question, answer ? "yes" : "no", snapshot);

    /// <summary>
    /// Writes down what a seat thought about changing the seating (RULES.md §3 step 2).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Three tokens and never <c>yes</c>/<c>no</c>.</b> Consent is neither: a table of
    /// consenting seats changes nothing, and a two-state record would lose exactly the state that
    /// makes that true. ⚠️ <b>No snapshot</b> — the question is asked between rounds, when no seat
    /// is holding a turn's fourteen and there is nothing a hand could mean.
    /// </remarks>
    public static JournalDecision Of(int round, int turn, PlayerId player, SeatingOpinion opinion) =>
        new(round, turn, player, JournalQuestion.Seating, opinion switch
        {
            SeatingOpinion.Ask => "ask",
            SeatingOpinion.Refuse => "refuse",
            SeatingOpinion.Consent => "consent",
            _ => throw new ArgumentOutOfRangeException(nameof(opinion), opinion, "Not an opinion about the seating.")
        });

    /// <summary>Reads the answer as an opinion about the seating (RULES.md §3 step 2).</summary>
    public SeatingOpinion AsSeatingOpinion() => Answer switch
    {
        "consent" => SeatingOpinion.Consent,
        "ask" => SeatingOpinion.Ask,
        "refuse" => SeatingOpinion.Refuse,
        _ => throw Unreadable("consent, ask or refuse")
    };

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
/// The computer's opinion, written down <b>beside</b> a seat's answer rather than instead of it
/// (BUILD-PLAN P24.2).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>It is an opinion beside an answer, not a rationale on a decision.</b>
/// <see cref="Agents.JournalingAgent"/> records the answer <em>the seat gave</em>, and at a table
/// with an expert in it that answer is hers. This is a different agent's opinion about the same
/// moment — so <em>where an expert disagreed with the computer</em> becomes a query
/// (<see cref="JournalDecision.DisagreedWithTheComputer"/>) rather than something somebody has to
/// notice and write down. That is the artifact P24.2 exists to produce.
/// </para>
/// <para>
/// ⚠️ <b>The card is a <see cref="CardId"/> for §3.1's reason</b>, and the rung is named because
/// an opinion is only worth anything if you know whose it was — the adviser is
/// <c>BotCatalog.Hardest</c> at ε = 0 and never the table's difficulty setting.
/// </para>
/// </remarks>
/// <param name="Card">The card the adviser would have thrown.</param>
/// <param name="Rung">Which way of playing said so — <c>outs</c>.</param>
/// <param name="Why">The sentence it gave, as the seat was shown it.</param>
public sealed record JournalAdvice(CardId Card, string Rung, string Why);

/// <summary>
/// Somebody's opinion about a decision, for recording beside the answer.
/// </summary>
/// <remarks>
/// <para>
/// <b>The seam and nothing else.</b> The domain cannot reach <c>ComputerAdvice</c> — that lives
/// in Presentation, one layer out — so this is what a journal asks and what Presentation fills
/// in. It is the same shape as every other decorator seam in the tree (BUILD-PLAN §3.8 item 2).
/// </para>
/// <para>
/// ⚠️ <b>The discard alone.</b> The other four questions have no ranking behind them, and a
/// disagreement about which card to throw is the one a person actually argues with.
/// </para>
/// </remarks>
public interface ISecondOpinion
{
    /// <summary>What the adviser would throw here, and why — or null if it has no opinion.</summary>
    JournalAdvice? OnDiscard(TurnContext context);
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
