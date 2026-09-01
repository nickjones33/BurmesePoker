using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// ✅ <b>P70: the two- and three-handed games are dealt, played and settled</b> — RULES.md §10
/// #7, the oldest ruling in that list and the only one no packet had ever owned.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Nothing here is new behaviour.</b> §2 has recorded <b>2 to 6</b> as Settled since rev 1,
/// §7.1.1's small-table win conditions have been implemented since P25 and
/// <see cref="TableRules.For"/> has been correct and tested the whole time — they were simply
/// unreachable from a dealt game, because <see cref="RoundEngine.MinimumPlayers"/> was 4. What
/// these tests fence is that lowering the floor <em>reaches</em> them.
/// </para>
/// <para>
/// ⚠️ <b>The win condition is asserted against <see cref="TableRules"/> and re-derived from the
/// melds, not read off the evaluator's verdict.</b> A hand that satisfies §7.1.1 by accident and
/// one the engine checked are indistinguishable from a boolean; the partition is the evidence.
/// </para>
/// <para>
/// 🔥 <b>Every two-handed round here is bounded, and that is a measurement rather than caution.</b>
/// P70 measured a two-handed table of <c>outs</c> abandoning about <b>three rounds in five</b> at
/// the harness cap, and ten times the cap bought five points of it — a stalled two-handed round is
/// stuck rather than slow (docs/STRATEGY.md §12). <see cref="RoundEngine"/> has no cap of its own,
/// so a test that dealt two-handed rounds unbounded would hang; these wrap each seat in
/// <see cref="SeatRecorder"/>, which is the cap the harness has used since P12, and skip a seed
/// whose round does not settle.
/// </para>
/// </remarks>
public class SmallTableTests
{
    /// <summary>
    /// Turns a two-handed round is given before it is treated as stuck.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Well under the harness's 400.</b> A two-handed round that settles at all settles in
    /// about 28 turns, and raising the cap to 4,000 moved the settled share from 36% to 42% — so
    /// sixty turns is generous for the existence proofs below and cheap for the ones that stall.
    /// </remarks>
    private const int Cap = 60;

    /// <summary>
    /// ✅ <b>The floor the document records is the floor the engine deals</b> (RULES.md §2, §2.1).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>This is the whole of §10 #7 as a single assertion.</b> <see cref="TableRules"/> has
    /// named two as the smallest table since P25; the engine named four until P70, and the two
    /// numbers disagreeing is exactly what made a Settled rule unreachable — with nothing red to
    /// show for it, because the constant had no assertion against the document at all.
    /// </remarks>
    [Fact]
    public void TheSmallestTableTheRulesDefineIsTheSmallestTableADealAccepts()
    {
        Assert.Equal(TableRules.SmallestTable, RoundEngine.MinimumPlayers);
        Assert.Equal(2, RoundEngine.MinimumPlayers);

        // And the floor is not the default: five is the table this game is played at (P32).
        Assert.NotEqual(RoundEngine.MinimumPlayers, RoundEngine.DefaultPlayers);
    }

    /// <summary>
    /// ✅ <b>The small-table win condition is the one §7.1.1 records</b>, at the sizes a dealt
    /// game can now reach.
    /// </summary>
    [Fact]
    public void TwoHandedBansSetsAndThreeHandedDemandsTwoCleanSeries()
    {
        var headToHead = TableRules.For(2);

        Assert.False(headToHead.SetsAllowed);
        Assert.Equal(0, headToHead.RequiredSeries);
        Assert.Equal(0, headToHead.RequiredCleanSeries);
        Assert.Equal(2, headToHead.JokerlessMultiplier);

        var threeHanded = TableRules.For(3);

        Assert.True(threeHanded.SetsAllowed);
        Assert.Equal(2, threeHanded.RequiredSeries);
        Assert.Equal(2, threeHanded.RequiredCleanSeries);
    }

    /// <summary>
    /// ✅ <b>P70 acceptance 1</b> — a three-handed match played end to end at several seeds:
    /// dealt, played, settled, banks conserved, and won on §7.1.1's terms.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Three-handed needs no cap</b>: 200 rounds of <c>outs</c> settled all 200 in 24.8
    /// turns, which is the four-handed figure. The two-handed half is the test below, because
    /// there the round is not guaranteed to end at all.
    /// </remarks>
    [Theory]
    [InlineData(20260901)]
    [InlineData(20260902)]
    [InlineData(15)]
    public void AThreeHandedMatchIsPlayedEndToEnd(int seed)
    {
        var players = Players(3);

        var match = new MatchEngine(
            players,
            players.ToDictionary(
                player => player,
                player => BotCatalog.Hardest.Create(seed + player.Value)),
            Stakes.Standard,
            new Random(seed));

        var rounds = Enumerable.Range(0, 5).Select(_ => match.PlayRound().Result).ToList();

        Assert.Equal(5, match.RoundsPlayed);
        Assert.Equal(0, match.Banks.Values.Sum());
        Assert.All(rounds, round => TheRoundWasWonOnTheTermsOfItsTable(round, players, 3));
    }

    /// <summary>
    /// ✅ <b>P70 acceptance 1, two-handed</b> — dealt, played, settled, banks conserved, and won
    /// on §7.1.1's terms: <b>runs and nothing else</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Seeds are searched rather than named</b>, because a two-handed round is not certain
    /// to end (see the class remark and docs/STRATEGY.md §12). A seed whose first round stalls is
    /// skipped whole — a match cannot carry on through an abandoned round, since the round has no
    /// winner and no settlement — and the assertion is that several seeds do play a whole match.
    /// </remarks>
    [Fact]
    public void ATwoHandedMatchIsPlayedEndToEnd()
    {
        var players = Players(2);
        var played = 0;
        var stalled = 0;

        for (var seed = 20260901; seed < 20260901 + 60 && played < 3; seed++)
        {
            var match = new MatchEngine(
                players,
                players.ToDictionary(
                    player => player,
                    player => Capped(BotCatalog.Hardest.Create(seed + player.Value))),
                Stakes.Standard,
                new Random(seed));

            List<RoundResult> rounds;

            try
            {
                rounds = [.. Enumerable.Range(0, 2).Select(_ => match.PlayRound().Result)];
            }
            catch (RoundAbandonedException)
            {
                stalled++;
                continue;
            }

            Assert.Equal(2, match.RoundsPlayed);
            Assert.Equal(0, match.Banks.Values.Sum());
            Assert.All(rounds, round => TheRoundWasWonOnTheTermsOfItsTable(round, players, 2));

            played++;
        }

        Assert.True(played == 3, $"only {played} two-handed matches of 60 seeds played out.");

        // Non-vacuity of the search itself: if nothing ever stalled, the cap would be doing
        // nothing and the measurement this test is built on would be wrong.
        Assert.True(stalled > 0, "no two-handed round stalled, which contradicts the P70 measurement.");
    }

    /// <summary>
    /// 🔥 <b>P70 acceptance 2 — §5.1 in ordinary two-handed play, and the mutual lock is not in
    /// it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two players alternate, so each is both the feeder and the fed. <b>The ban bites</b>: most
    /// two-handed rounds end with a rank closed against somebody, and many with both seats
    /// holding one. ⚠️ <b>What does not happen is the <em>mutual</em> lock</b> — the same rank
    /// closed in both directions, which RULES.md §5.1 (<i>Two-handed</i>) records as a legal
    /// state that can quietly remove a player from contention.
    /// </para>
    /// <para>
    /// 🔥 <b>Measured, not assumed: nought in 400 rounds at a 60-turn cap and nought in 120 at
    /// 400</b>, against 315 of those 400 rounds ending with some rank closed. <b>The mechanism is
    /// the rungs' own</b>: rank R closes against A when B takes R out of A's pile, and it closes
    /// against B only if A then takes R out of <em>B's</em> pile — which needs B to throw back the
    /// very rank it took because it wanted it. A cover-maximising rung practically never does.
    /// ⚠️ <b>So the lock is a legal state the rules permit and this field does not reach</b>; the
    /// rule itself is asserted where it can be constructed, at the table state, by
    /// <c>FeedingBanTests.TwoHandedEachSeatIsBothTheFeederAndTheFed</c>. This test asserts what
    /// ordinary play does produce, and is deliberately not a fence on the zero — a rung that threw
    /// ranks back would reach the lock, and that would be a finding rather than a failure.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheFeedingBanBitesInOrdinaryTwoHandedPlayAndALockedRoundIsStillWon()
    {
        var players = Players(2);
        var shoe = Deck.TwoDecks().Cards;
        var closedAgainstSomebody = 0;
        var closedAgainstBoth = 0;
        var closedAndWon = 0;

        for (var game = 0; game < 120; game++)
        {
            var seed = 20260901 + game;

            var engine = RoundEngine.Shuffled(
                players,
                players.ToDictionary(
                    player => player,
                    player => Capped(BotCatalog.Hardest.Create(seed + player.Value))),
                Stakes.Standard,
                new Random(seed));

            RoundResult? result = null;

            try
            {
                result = engine.Play();
            }
            catch (RoundAbandonedException)
            {
                // Stuck, which is this table's ordinary outcome — see the class remark.
            }

            // ⚠️ Read off the table the round finished on, settled or not. A rank is released
            // only by the protected seat throwing it back, so what stands at the end is what the
            // round accumulated.
            var table = engine.Table;

            var againstOne = shoe.Any(card => table.SeatFedBy(players[0]).MayNotBeFed.Closes(card));
            var againstTheOther = shoe.Any(card => table.SeatFedBy(players[1]).MayNotBeFed.Closes(card));

            if (!againstOne && !againstTheOther)
            {
                continue;
            }

            closedAgainstSomebody++;

            if (againstOne && againstTheOther)
            {
                closedAgainstBoth++;
            }

            if (result is { } settled)
            {
                closedAndWon++;
                TheRoundWasWonOnTheTermsOfItsTable(settled, players, 2);
            }
        }

        // Non-vacuity: a run in which the ban never armed would have checked nothing at all.
        Assert.True(closedAgainstSomebody > 0, "the feeding ban never armed in 120 two-handed rounds.");
        Assert.True(closedAgainstBoth > 0, "no two-handed round closed a rank against both seats.");

        // And a locked table is not a stopped one: §5.1's floor keeps a turn legal, so a round
        // carrying closed ranks can still be played out and won.
        Assert.True(
            closedAndWon > 0,
            $"{closedAgainstSomebody} rounds armed the ban and none of them was won.");
    }

    /// <summary>
    /// RULES.md §7.1.1, re-derived from the partition the winner declared on, plus §7.2's zero
    /// sum and §6.3's thirteen.
    /// </summary>
    private static void TheRoundWasWonOnTheTermsOfItsTable(
        RoundResult round, IReadOnlyList<PlayerId> players, int seats)
    {
        var rules = TableRules.For(seats);

        Assert.Contains(round.Winner, players);
        Assert.Equal(0, round.Payouts.Values.Sum());
        Assert.Equal(RoundEngine.HandSize, round.Melds.Sum(meld => meld.Count));

        if (!rules.SetsAllowed)
        {
            Assert.All(round.Melds, meld => Assert.Equal(MeldKind.Run, meld.Kind));
        }

        Assert.True(
            round.Melds.Count(meld => meld.Kind == MeldKind.Run) >= rules.RequiredSeries,
            $"{seats}-handed: {round.Melds.Count(meld => meld.Kind == MeldKind.Run)} series "
            + $"where §7.1.1 requires {rules.RequiredSeries}.");

        Assert.True(
            round.Melds.Count(meld => meld.IsClean) >= rules.RequiredCleanSeries,
            $"{seats}-handed: {round.Melds.Count(meld => meld.IsClean)} clean series "
            + $"where §7.1.1 requires {rules.RequiredCleanSeries}.");
    }

    /// <summary>The harness's own cap, which is the only bound on a round there is.</summary>
    private static IPlayerAgent Capped(IPlayerAgent agent) => new SeatRecorder(agent, Cap);

    private static IReadOnlyList<PlayerId> Players(int seats) =>
        [.. Enumerable.Range(0, seats).Select(seat => new PlayerId(seat))];
}
