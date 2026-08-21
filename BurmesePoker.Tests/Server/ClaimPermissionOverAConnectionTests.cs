using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;

namespace BurmesePoker.Tests.Server;

/// <summary>
/// A hosted table where the opener always wants the turned-up card, played until the seat above
/// it is holding that rank and has to answer for it (packet P28).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The first question in this game asked of a seat that is not on turn</b> (RULES.md
/// §4.5). Everything the server does around a question assumes the seat being asked is the one
/// the table is waiting on — the spotlight, the takeover announcement, the pacing beat — so the
/// point of playing it through a real <c>TableSession</c> is to find out which of those
/// assumptions were load-bearing.
/// </para>
/// <para>
/// ⚠️ <b>Rounds are played until one produces a permission question rather than one round being
/// assumed to.</b> The upstream seat has to be holding the rank, which is a fact about the deal;
/// the seed is fixed, so the round it happens on is fixed too.
/// </para>
/// </remarks>
public sealed class ClaimedTable
{
    /// <summary>The upstream seat: last in the order, and the one that discards to the opener.</summary>
    public static readonly PlayerId Objector = new(4);

    /// <summary>The opener, who claims whenever there is anything to claim.</summary>
    public static readonly PlayerId Opener = new(1);

    public ClaimedTable()
    {
        Table = TableSession.Open(
            [
                TableSeat.Computer(Opener, "Grabby", new AlwaysClaims()),
                TableSeat.Computer(new PlayerId(2), "Mya Lay", new GreedyBotAgent()),
                TableSeat.Computer(new PlayerId(3), "Cobra", new GreedyBotAgent()),
                TableSeat.Person(Objector, "Nick")
            ],
            TableSessionTests.Options(20260821));

        Watcher = Table.Watch();
        Nick = new ScriptedSeat(Table.ConnectionFor(Objector));

        // ⚠️ The seats are drawn again every round (RULES.md §3 step 2), so "the opener" is only
        // the opener while the first seating stands — which is why one round is played here and
        // the properties below are about that round.
        Played = Table.PlayRound();
    }

    public TableSession Table { get; }

    public SeatConnection Watcher { get; }

    public ScriptedSeat Nick { get; }

    public RoundRecord Played { get; }

    /// <summary>Every question this seat was asked about somebody else's claim.</summary>
    public IReadOnlyList<SeatPrompt> Permissions =>
        [.. Nick.Prompts.Where(prompt => prompt.Question == SeatQuestion.ObjectToClaim)];

    /// <summary>A seat that takes the turned-up card whenever the table offers it (RULES.md §4.5).</summary>
    private sealed class AlwaysClaims : IPlayerAgent
    {
        private readonly GreedyBotAgent _inner = new();

        public TurnAction ChooseAction(TurnContext context) => _inner.ChooseAction(context);

        public Card ChooseDiscard(TurnContext context) => _inner.ChooseDiscard(context);

        public bool ClaimTurnedUpMoneyCard(TurnContext context) => context.TurnedUpMoneyCards.Count > 0;

        public bool ObjectToClaim(TurnContext context) => _inner.ObjectToClaim(context);

        public bool Declare(TurnContext context) => true;
    }
}

/// <summary>
/// ✅ <b>P28 — the claim's permission over a connection.</b>
/// </summary>
[Collection(WallClockBudgets.Collection)]
public class ClaimPermissionOverAConnectionTests(ClaimedTable table) : IClassFixture<ClaimedTable>
{
    /// <summary>
    /// ✅ <b>A seat that is not on turn is asked, and it is asked about somebody else's move.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The turn number is the opener's</b>, which is the whole oddity: this arrives during
    /// turn 1 and the seat answering it is the last one in the order. A prompt is still that
    /// seat's own view — its own hand, the public table — so nothing about the concealment
    /// changes.
    /// </remarks>
    [Fact]
    public void TheSeatAboveIsAskedWhileItIsNotItsTurn()
    {
        var asked = Assert.Single(table.Permissions);

        Assert.Equal(ClaimedTable.Objector, asked.Player);
        Assert.Equal(1, asked.TurnNumber);
        Assert.Equal(RoundEngine.HandSize, asked.Hand.Count);

        var permission = Assert.IsType<ClaimRequest>(asked.PermissionAsked);

        Assert.Equal(ClaimedTable.Opener, permission.Claimant);

        // ✅ Only a holder is ever asked (RULES.md §4.5), and the prompt can say so without
        // asking the table anything: it reads its own hand.
        Assert.True(asked.MayObject);
        Assert.Contains(asked.Hand.Hand, held => held.SameRankAs(permission.Card));
    }

    /// <summary>
    /// 🔥 <b>The spotlight does not move.</b> The question carries the opener's turn number, so
    /// a table that announced it the way it announces every other question would light up the
    /// wrong seat in the middle of somebody else's turn (<c>BoundedAgent</c>).
    /// </summary>
    [Fact]
    public void BeingAskedForPermissionIsNotBeingOnTurn()
    {
        var turns = table.Watcher.Events
            .OfType<TableEvent.TurnBegan>()
            .Where(began => began.Turn == 1)
            .ToList();

        var announced = Assert.Single(turns);

        Assert.Equal(ClaimedTable.Opener, announced.Player);
    }

    /// <summary>
    /// ✅ <b>The refusal reaches every connection, seated or watching.</b> Only a holder may
    /// refuse, so it is a disclosure — <b>made by the player, not leaked by the server</b>
    /// (RULES.md §4.5, §10 #18).
    /// </summary>
    [Fact]
    public void ARefusalIsToldToTheWholeTable()
    {
        var refusal = Assert.Single(table.Watcher.Events.OfType<TableEvent.ClaimRefused>());

        Assert.Equal(ClaimedTable.Objector, refusal.Objector);
        Assert.Equal(ClaimedTable.Opener, refusal.Claimant);

        foreach (var connection in table.Table.Connections)
        {
            Assert.Contains(connection.Events.OfType<TableEvent.ClaimRefused>(), told => told == refusal);
        }
    }

    /// <summary>
    /// ✅ <b>And the claim really was refused</b>: the card is still on the table and nobody was
    /// told it was claimed.
    /// </summary>
    [Fact]
    public void ARefusedClaimLeavesTheCardWhereItWas()
    {
        Assert.Empty(table.Watcher.Events.OfType<TableEvent.MoneyCardClaimed>());
        Assert.Equal(2, table.Played.Table.TurnedUpOnTable.Count);
        Assert.False(table.Played.Table.TurnedUpFromTopClaimed);
    }

    /// <summary>
    /// ✅ <b>An answer that does not fit is refused rather than played</b> (P13.2), and the new
    /// question is no exception: a client sending an objection to a question about a discard is
    /// answering something it was not asked.
    /// </summary>
    [Fact]
    public void AnObjectionOnlyFitsThePermissionQuestion()
    {
        var permission = table.Permissions[0];
        var somethingElse = permission with { Question = SeatQuestion.Discard };

        Assert.True(new SeatAnswer.Objection(true).Fits(permission));
        Assert.True(new SeatAnswer.Objection(false).Fits(permission));
        Assert.False(new SeatAnswer.Objection(true).Fits(somethingElse));
        Assert.False(new SeatAnswer.Claim(true).Fits(permission));
    }
}
