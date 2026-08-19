using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;

namespace BurmesePoker.Tests.Server;

/// <summary>
/// One round, played by four connected seats with somebody watching, so the concealment can be
/// examined from every side of it at once.
/// </summary>
/// <remarks>
/// Played once and read four ways. The seats follow the computer's hint, so the round finishes
/// for the same reason a table of <c>GreedyBotAgent</c>s does.
/// </remarks>
public sealed class WatchedRound
{
    public WatchedRound()
    {
        Table = TableSession.Open(
            [
                TableSeat.Person(new PlayerId(1), "Nick"),
                TableSeat.Person(new PlayerId(2), "Mya Lay"),
                TableSeat.Person(new PlayerId(3), "Cobra"),
                TableSeat.Person(new PlayerId(4), "Su Htwe")
            ],
            TableSessionTests.Options(20260819));

        Watcher = Table.Watch();
        Seats = Table.Players.ToDictionary(player => player, player => Table.ConnectionFor(player));
        Scripts = Seats.ToDictionary(entry => entry.Key, entry => new ScriptedSeat(entry.Value));
        Played = Table.PlayRound();
    }

    public TableSession Table { get; }

    public SeatConnection Watcher { get; }

    public IReadOnlyDictionary<PlayerId, SeatConnection> Seats { get; }

    public IReadOnlyDictionary<PlayerId, ScriptedSeat> Scripts { get; }

    public RoundRecord Played { get; }
}

/// <summary>
/// ✅ <b>Acceptance 2, and the most important test in packet P13.2.</b> Nothing that must stay
/// concealed crosses the fan-out.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>It belongs here rather than in the UI packet, because by the UI packet it would be too
/// late.</b> A hand is fully concealed until a declaration (RULES.md §7.1) and there is money
/// on the table, so over a connection this stops being a courtesy the front end extends and
/// becomes a property somebody could exploit (BUILD-PLAN §3.10, §3.11 A1). <b>Hiding a card in
/// the markup is a leak</b> — the question is what was <em>sent</em>.
/// </para>
/// <para>
/// <b>The type already forbids most of it.</b> A <c>TurnContext</c> has no route to another
/// seat's hand (P7) and a <c>HandView</c> is built from one (P13.1), so what these tests guard
/// is everything the server <em>adds</em> around it: the narration. The engine narrates
/// everything, deliberately, including the card behind a blind draw — <c>TableFanOut</c> is the
/// single place that decides who hears what, and these are the assertions that hold it to it.
/// </para>
/// <para>
/// <b>A watcher is the strictest case there is</b>, because there is no hand it may
/// legitimately be shown: anything private reaching one is unambiguously a leak, with no
/// "except their own" to reason about. That is also exactly what P13.3 renders.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class ConcealmentTests(WatchedRound round) : IClassFixture<WatchedRound>
{
    /// <summary>
    /// The form P13.1 made writable: build the seat's own view for every seat of a round and
    /// assert the <c>CardId</c> sets are pairwise disjoint.
    /// </summary>
    [Fact]
    public void NoSeatIsEverShownACardFromAnotherSeatsHand()
    {
        var bySeat = round.Scripts.ToDictionary(entry => entry.Key, entry => Shown(entry.Value));

        Assert.All(bySeat.Values, Assert.NotEmpty);

        foreach (var (seat, cards) in bySeat)
        {
            foreach (var (other, theirs) in bySeat)
            {
                if (seat != other)
                {
                    Assert.Empty(cards.Intersect(theirs));
                }
            }
        }
    }

    /// <summary>The one event that differs by listener, and the only one that could leak.</summary>
    [Fact]
    public void NoSeatIsToldWhatAnybodyElseDrewBlind()
    {
        var draws = round.Watcher.Events.OfType<TableEvent.Drew>().ToList();
        Assert.NotEmpty(draws);

        // A watcher hears that a card was drawn, and never which.
        Assert.All(draws, drew => Assert.Null(drew.Card));

        foreach (var (seat, connection) in round.Seats)
        {
            var told = connection.Events.OfType<TableEvent.Drew>().ToList();

            Assert.All(told, drew => Assert.True(
                drew.Card is null || drew.Player == seat,
                $"{seat} was told {drew.Player} drew {drew.Card}"));

            // …and does hear its own, which is what stops this passing by telling nobody
            // anything.
            Assert.Contains(told, drew => drew.Player == seat && drew.Card is not null);
        }
    }

    /// <summary>
    /// The sweep: every card mentioned in anything a seat was sent is either public by the
    /// rules or one of that seat's own.
    /// </summary>
    /// <remarks>
    /// Deliberately written over <em>every</em> event rather than over the one that filters, so
    /// a future event carrying a card it should not fails here without anybody remembering to
    /// come back and extend the test.
    /// </remarks>
    [Fact]
    public void EveryCardASeatIsSentIsOneItMaySee()
    {
        foreach (var (seat, connection) in round.Seats)
        {
            var events = connection.Events;
            var permitted = new HashSet<CardId>(PublicCards(events));
            permitted.UnionWith(Shown(round.Scripts[seat]));

            foreach (var drew in events.OfType<TableEvent.Drew>())
            {
                if (drew is { Player: var who, Card: { } card } && who == seat)
                {
                    permitted.Add(card.Id);
                }
            }

            var sent = Mentioned(events);
            Assert.NotEmpty(sent);
            Assert.Empty(sent.Except(permitted));
        }
    }

    /// <summary>The strictest case: a watcher may see the public game and nothing else.</summary>
    [Fact]
    public void AWatcherIsSentNothingButThePublicGame()
    {
        var events = round.Watcher.Events;
        var mentioned = Mentioned(events);

        Assert.NotEmpty(mentioned);
        Assert.Empty(mentioned.Except(PublicCards(events)));
    }

    /// <remarks>
    /// The round really was played through the connections — a fan-out that sent nothing would
    /// pass every assertion above.
    /// </remarks>
    [Fact]
    public void TheRoundWasActuallyPlayedThroughTheConnections()
    {
        Assert.Contains(round.Played.Result.Winner, round.Table.Players);
        Assert.All(round.Scripts.Values, seat => Assert.Equal(seat.Prompts.Count, seat.Answered));
        Assert.All(round.Seats.Values, connection => Assert.NotEmpty(connection.Events));
        Assert.Empty(round.Watcher.Events.OfType<TableEvent.SeatPlayedByTheComputer>());
    }

    /// <summary>Every card this seat was shown in a prompt of its own.</summary>
    private static HashSet<CardId> Shown(ScriptedSeat seat) =>
        [.. seat.Prompts.SelectMany(prompt => prompt.Hand.Hand).Select(card => card.Id)];

    /// <summary>
    /// Every card named by an event that is public by the rules: the turned-up cards, every
    /// discard, a taken discard, a claimed money card, and the winning hand once it is laid
    /// down (RULES.md §3, §4.5, §5, §6.3, §7.1).
    /// </summary>
    private static HashSet<CardId> PublicCards(IEnumerable<TableEvent> events)
    {
        var seen = new HashSet<CardId>();

        foreach (var moment in events)
        {
            switch (moment)
            {
                case TableEvent.RoundStarted started:
                    seen.UnionWith(started.TurnedUp.Select(card => card.Id));
                    break;
                case TableEvent.TookDiscard took:
                    seen.Add(took.Card.Id);
                    break;
                case TableEvent.MoneyCardClaimed claimed:
                    seen.Add(claimed.Card.Id);
                    break;
                case TableEvent.Discarded discarded:
                    seen.Add(discarded.Card.Id);
                    break;
                case TableEvent.Declared declared:
                    seen.UnionWith(declared.Melds.SelectMany(meld => meld.CardIds));
                    break;
                case TableEvent.Settled settled:
                    seen.UnionWith(settled.Result.Melds.SelectMany(meld => meld.CardIds));
                    break;
            }
        }

        return seen;
    }

    /// <summary>Every card named by an event, whatever the event is.</summary>
    private static HashSet<CardId> Mentioned(IEnumerable<TableEvent> events)
    {
        var seen = new HashSet<CardId>();

        foreach (var moment in events)
        {
            switch (moment)
            {
                case TableEvent.RoundStarted started:
                    seen.UnionWith(started.TurnedUp.Select(card => card.Id));
                    break;
                case TableEvent.Drew { Card: { } drawn }:
                    seen.Add(drawn.Id);
                    break;
                case TableEvent.TookDiscard took:
                    seen.Add(took.Card.Id);
                    break;
                case TableEvent.MoneyCardClaimed claimed:
                    seen.Add(claimed.Card.Id);
                    break;
                case TableEvent.Discarded discarded:
                    seen.Add(discarded.Card.Id);
                    break;
                case TableEvent.Declared declared:
                    seen.UnionWith(declared.Melds.SelectMany(meld => meld.CardIds));
                    break;
                case TableEvent.Settled settled:
                    seen.UnionWith(settled.Result.Melds.SelectMany(meld => meld.CardIds));
                    break;
            }
        }

        return seen;
    }
}
