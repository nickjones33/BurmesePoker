using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;
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
    /// ✅ <b>P24.2 acceptance 4.</b> The computer's reasoning rides on <c>SeatPrompt</c> and never
    /// on a <c>TableEvent</c>.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>One seat's reasoning on the bus is precisely the leak §3.11 A1 exists to catch.</b>
    /// A rationale names cards of a hand and says what the computer would keep, so an event
    /// carrying one would hand every watcher a running commentary on somebody else's thirteen.
    /// A prompt is seat-private by construction; an event is public by construction.
    /// ⚠️ <b>Asserted over the type and not over one round</b>: what matters is that there is no
    /// event that <em>could</em> carry one, which a played round can only ever sample.
    /// </remarks>
    [Fact]
    public void NoTableEventCanCarryTheComputersReasoning()
    {
        var events = typeof(TableEvent).Assembly
            .GetTypes()
            .Where(type => typeof(TableEvent).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(events);

        foreach (var kind in events)
        {
            Assert.DoesNotContain(
                kind.GetProperties(),
                property => property.PropertyType == typeof(AdviceRationale));
        }

        // Non-vacuous: the type really is reachable from this layer, and the one thing that does
        // carry it is the seat-private prompt.
        Assert.Contains(
            typeof(SeatPrompt).GetProperties(),
            property => property.PropertyType == typeof(AdviceRationale));
    }

    /// <summary>
    /// ✅ <b>P37 acceptance 3 — the seating conversation is public, and it is asserted to be
    /// rather than passed over in silence</b> (RULES.md §3 step 2, §9 #45).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The first public question this table has ever asked.</b> Every other question is put
    /// to one seat through a seat-private <c>SeatPrompt</c>; this one is put to everybody, so the
    /// thing worth asserting is the opposite of the usual one — <b>that nothing is filtered</b>,
    /// and that there is nothing on the events that <em>could</em> be.
    /// </para>
    /// <para>
    /// ⚠️ <b>Asserted over the type, like the rationale above</b>: a seating event carries no
    /// card, no hand and no rationale, so a later change cannot smuggle a hand onto the one
    /// conversation the table hears in full.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheSeatingConversationIsPublicAndCarriesNoHand()
    {
        var seating = typeof(TableEvent).Assembly
            .GetTypes()
            .Where(type => typeof(TableEvent).IsAssignableFrom(type))
            .Where(type => type.Name.Contains("Seating", StringComparison.Ordinal))
            .ToArray();

        // Three: what a seat said, the agreement, and the refusal.
        Assert.Equal(3, seating.Length);

        foreach (var kind in seating)
        {
            Assert.DoesNotContain(
                kind.GetProperties(),
                property => property.PropertyType == typeof(Card)
                    || property.PropertyType == typeof(Card?)
                    || property.PropertyType == typeof(AdviceRationale)
                    || property.PropertyType == typeof(HandView)
                    || property.PropertyType == typeof(IReadOnlyList<Card>));
        }

        // And it really is broadcast: every connection at a table hears every word of it, the
        // watcher who holds no seat included.
        var table = TableSession.Open(
            TableSessionTests.TwoPeopleAndTwoBots(), TableSessionTests.Options(20260822));

        var watcher = table.Watch("The room");
        var nick = table.ConnectionFor(new PlayerId(1));
        var myaLay = table.ConnectionFor(new PlayerId(3));

        Assert.True(nick.SaysAboutTheSeating(SeatingOpinion.Ask));
        Assert.True(myaLay.SaysAboutTheSeating(SeatingOpinion.Refuse));

        foreach (var heard in (IReadOnlyList<SeatConnection>)[watcher, nick, myaLay])
        {
            Assert.Equal(
                [(new PlayerId(1), SeatingOpinion.Ask), (new PlayerId(3), SeatingOpinion.Refuse)],
                heard.Events.OfType<TableEvent.SeatingOpinionGiven>()
                    .Select(said => (said.Player, said.Opinion)));
        }
    }

    /// <summary>
    /// The form P13.1 made writable: build the seat's own view for every seat of a round and
    /// assert that no card reached two hands <b>except by way of the table</b>.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>This used to demand the <c>CardId</c> sets be pairwise disjoint outright, and P21
    /// showed that that was a fact about short rounds rather than about concealment.</b> Run the
    /// draw pile out and the discards are shuffled back into it (RULES.md §5), so a card one
    /// seat threw away is a card another seat may draw — public on the way out, private on the
    /// way back, and a breach at neither end. <see cref="PublicRelease"/> carries the reasoning
    /// and the round that found it.
    /// </remarks>
    [Fact]
    public void NoSeatIsEverShownACardFromAnotherSeatsHandExceptByWayOfTheTable()
    {
        var bySeat = round.Scripts.ToDictionary(entry => entry.Key, entry => Shown(entry.Value));
        var released = PublicRelease.In(PublicRelease.PerRound(round.Watcher.Events), round: 1);

        Assert.All(bySeat.Values, Assert.NotEmpty);

        foreach (var (seat, cards) in bySeat)
        {
            foreach (var (other, theirs) in bySeat)
            {
                if (seat != other)
                {
                    Assert.Empty(cards.Intersect(theirs).Except(released));
                }
            }
        }

        // ⚠️ And the allowance is not a hole to hide a leak in: a card only enters it by being
        // named in an event every seat and every watcher received.
        Assert.All(released, card => Assert.Contains(Mentioned(round.Watcher.Events), seen => seen == card));
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

    /// <summary>
    /// 🔥 <b>P13.5's new event, covered rather than merely still passing.</b> Whose turn it is
    /// is public; nothing else about a turn is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Broadcasting it was a deliberate widening of what the table says</b> (BUILD-PLAN
    /// P13.5), so the packet that widened it owes this test. A client with seats at positions
    /// wants a turn spotlight, and until P13.5 the nearest the public game had was
    /// <em>who moved last</em> — which points at the seat that has just finished.
    /// </para>
    /// <para>
    /// The argument for it is that everybody at a real table can see who is being waited on:
    /// the concealment is about what is <em>in a hand</em> (RULES.md §7.1). So the assertions
    /// are that the event names a seat and carries nothing else, that everybody hears the same
    /// one — including a watcher, who has no seat of their own to reason from — and that it is
    /// one per turn rather than one per question.
    /// </para>
    /// </remarks>
    [Fact]
    public void WhoseTurnItIsIsPublicAndIsAllThatIsPublicAboutATurn()
    {
        var watched = round.Watcher.Events.OfType<TableEvent.TurnBegan>().ToList();

        Assert.NotEmpty(watched);

        // Every seat at the table takes turns, and the watcher is told about all of them.
        Assert.Equal(
            round.Table.Players.OrderBy(player => player.Value),
            watched.Select(began => began.Player).Distinct().OrderBy(player => player.Value));

        // One per turn, whatever that turn asked. A turn asks between two and four questions.
        Assert.Equal(
            watched.Count,
            watched.Select(began => (began.Player, began.Round, began.Turn)).Distinct().Count());

        // Turn numbers count up from one and never go backwards within a round.
        Assert.Equal(watched.Select(began => began.Turn), watched.Select(began => began.Turn).Order());
        Assert.Equal(1, watched[0].Turn);

        // ⚠️ And it is the same event for everybody. A seat is told exactly what the room is
        // told — there is no per-listener variant to get wrong, which is what keeps the one
        // filtered event (the blind draw) the only one.
        foreach (var connection in round.Seats.Values)
        {
            Assert.Equal(
                watched.Select(began => (began.Player, began.Round, began.Turn)),
                connection.Events.OfType<TableEvent.TurnBegan>()
                    .Select(began => (began.Player, began.Round, began.Turn)));
        }
    }

    /// <remarks>
    /// The other half, and the one that would catch a future widening: the sweep in
    /// <see cref="EveryCardASeatIsSentIsOneItMaySee"/> is written over every event, so a turn
    /// event that ever started carrying a card would fail there. This says out loud that it
    /// carries none — a record with no <c>Card</c> on it cannot leak one.
    /// </remarks>
    [Fact]
    public void ATurnEventCarriesNoCardAtAll()
    {
        var carried = typeof(TableEvent.TurnBegan).GetProperties()
            .Where(property => property.PropertyType == typeof(Card) || property.PropertyType == typeof(Card?))
            .ToList();

        Assert.Empty(carried);
        Assert.Equal(
            ["Player", "Round", "Turn"],
            typeof(TableEvent.TurnBegan).GetProperties()
                .Where(property => property.DeclaringType == typeof(TableEvent.TurnBegan))
                .Select(property => property.Name)
                .Order());
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
                // ⚠️ Public, and the card it names is the one lying face up on the table
                // (RULES.md §4.5). What the refusal discloses is the objector's *rank*, which
                // they said out loud — no card of theirs is named.
                case TableEvent.ClaimRefused refused:
                    seen.Add(refused.Card.Id);
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
                // ⚠️ Public, and the card it names is the one lying face up on the table
                // (RULES.md §4.5). What the refusal discloses is the objector's *rank*, which
                // they said out loud — no card of theirs is named.
                case TableEvent.ClaimRefused refused:
                    seen.Add(refused.Card.Id);
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
