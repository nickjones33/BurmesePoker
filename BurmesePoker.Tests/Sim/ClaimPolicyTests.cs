using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Money;

using BurmesePoker.Sim;

using BurmesePoker.Tests.Agents;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// ✅ <b>P29 — the one decision in the engine that was taken rather than measured.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Every rung refuses the opener the turned-up money card whenever RULES.md §4.5 allows
/// it</b>, which P28 took from the rule's own reasoning: the claim is a public take, so letting
/// it through closes that rank against this seat for the rest of the round (§5.1). ⚠️ <b>What
/// nothing prices is the disclosure</b> — only a holder may refuse, so refusing announces that
/// this seat holds the rank. <see cref="ClaimPolicy"/> is the pair of arms that lets the harness
/// say which is worth more; <c>docs/STRATEGY.md</c> carries the number.
/// </para>
/// <para>
/// ⚠️ <b>No assertion here is about a measured value.</b> These cells are fourteen games; the
/// published figure is 8,008 and lives in <c>docs/strategy/measurements.csv</c>. What belongs
/// here is that the two arms differ in exactly one answer, that the refusing arm is the shipped
/// player, and that the difference is reachable at all.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class ClaimPolicyTests
{
    /// <summary>
    /// ✅ <b>The refusing arm is the shipped player, and this is an identity rather than a
    /// measurement.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>The control the whole cell rests on.</b> Every rung in <see cref="BotCatalog"/>
    /// answers the permission the same way the refusing arm does, so a table of
    /// <c>greedy/refuse</c> must play the same rounds card for card as a table of <c>greedy</c>
    /// — otherwise the arm has changed something besides the policy and the margin measures two
    /// things. ⚠️ <b>Two homogeneous tables and never a head-to-head cell</b>: two labels of one
    /// player sit in different seats there, so their aggregates differ by seat luck however
    /// identical they are (P22 learned this the hard way).
    /// </remarks>
    [Fact]
    public void RefusingIsWhatEveryRungAlreadyDoesAndAllowingIsNot()
    {
        Assert.Equal(Rounds(WholeTableOf("greedy")), Rounds(WholeTableOf("greedy/refuse")));

        // And the other arm really does reach the engine, or the cell would be an elaborate
        // null: a table that never refuses plays a different round.
        Assert.NotEqual(Rounds(WholeTableOf("greedy")), Rounds(WholeTableOf("greedy/allow")));
    }

    /// <summary>
    /// ✅ <b>A refusal is counted, and only where there is one to count</b> (BUILD-PLAN P29).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The anti-vacuity half is the second assertion.</b> A counter that never incremented
    /// would pass the "allowing refuses nothing" claim perfectly, so the refusing table has to
    /// be shown actually stopping claims — which is also the only evidence in the tree that the
    /// branch P28 decided is reached at all in ordinary play.
    /// </remarks>
    [Fact]
    public void TheHarnessCountsTheClaimsThatWereActuallyStopped()
    {
        Assert.True(Refusals(WholeTableOf("greedy/refuse")) > 0);
        Assert.Equal(0, Refusals(WholeTableOf("greedy/allow")));

        // A refusal is at most one a round, because only the opening turn may claim
        // (RULES.md §4.5).
        Assert.All(
            Simulator.Run(WholeTableOf("greedy/refuse")).Games.SelectMany(game => game.Rounds),
            round => Assert.InRange(round.ClaimsRefused, 0, 1));
    }

    /// <summary>
    /// ✅ <b>A claim counted is a claim <em>asked for</em>, and P28 is what made those two
    /// different.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>The seat decorator sees the answer the agent gave and the engine sees whether it
    /// stood</b>, so <c>claims − claims_refused</c> is what left the table. A run in which the
    /// two are equal is a run in which nothing was refused, and this asserts the arithmetic
    /// rather than the rate.
    /// </remarks>
    [Fact]
    public void WhatWasAskedForIsNotWhatWasGot()
    {
        var report = Simulator.Run(WholeTableOf("greedy/refuse"));

        var asked = report.Games.SelectMany(game => game.Rounds).Sum(round => round.Seats.Sum(seat => seat.Claims));
        var stopped = report.Games.SelectMany(game => game.Rounds).Sum(round => round.ClaimsRefused);

        Assert.True(asked > 0);
        Assert.True(stopped > 0);
        Assert.True(stopped <= asked);
    }

    /// <summary>
    /// ✅ <b>The two arms are named, resolvable, and neither is a rung or a level.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Its name carries <see cref="ClaimPolicy.Reserved"/> for the reason a calibration
    /// probe's carries <c>@</c></b>: a form field or a menu reaching
    /// <see cref="BotCatalog.Find"/> or <see cref="DifficultyLadder.Find"/> must not be able to
    /// conjure an experiment's arm as an opponent.
    /// </remarks>
    [Fact]
    public void AnArmIsAnInstrumentAndNeverAnOpponent()
    {
        var arms = ClaimPolicy.Both(BotCatalog.Hardest);

        Assert.Equal([true, false], arms.Select(arm => arm.Objects));
        Assert.Equal($"{BotCatalog.Hardest.Name}/refuse", arms[0].Name);
        Assert.Equal($"{BotCatalog.Hardest.Name}/allow", arms[1].Name);

        Assert.All(arms, arm => Assert.Null(BotCatalog.Find(arm.Name)));
        Assert.All(arms, arm => Assert.Null(DifficultyLadder.Find(arm.Name)));
        Assert.All(arms, arm => Assert.Equal(arm.Name, StrategyCatalog.Resolve(arm.Name).Name));

        // An ordinary name is not an arm, and a malformed one is refused rather than guessed at.
        Assert.Null(ClaimPolicy.FindOrProbe("outs"));
        Assert.Throws<ArgumentException>(() => ClaimPolicy.FindOrProbe("outs/maybe"));
        Assert.Throws<ArgumentException>(() => ClaimPolicy.FindOrProbe("nosuchrung/refuse"));
    }

    /// <summary>
    /// ✅ <b>The arm answers the policy and asks the rung nothing.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Consulting the inner rung and then overriding would make the two arms differ in a
    /// random draw as well as in the policy</b>, wherever a rung decides anything at random.
    /// The decorator is deliberately not a filter.
    /// </remarks>
    [Fact]
    public void TheArmDecidesThePermissionAndTheRungDecidesEverythingElse()
    {
        var context = TurnContexts.Holding(
            Hands.Of("2C", "3C", "4C", "5C", "6C", "7C", "8H", "9S", "10D", "JC", "QH", "KS", "AD", "2D"));

        var inner = new RecordingAgent(new GreedyBotAgent());
        var refusing = new ClaimPolicyAgent(inner, objects: true);
        var allowing = new ClaimPolicyAgent(inner, objects: false);

        Assert.True(refusing.ObjectToClaim(context));
        Assert.False(allowing.ObjectToClaim(context));
        Assert.Empty(inner.Objections);

        // Everything else is the rung's, unchanged.
        Assert.Equal(new GreedyBotAgent().ChooseDiscard(context), refusing.ChooseDiscard(context));
    }

    /// <summary>One arm at all four seats, so a difference can only be the policy.</summary>
    private static SimulationOptions WholeTableOf(string name) => new SimulationOptions
    {
        Strategies = [StrategyCatalog.Resolve(name)],
        Seats = 4,
        Games = 24,
        RoundsPerGame = 1,
        MasterSeed = 20260819,
        Stakes = Stakes.Standard,
        Parallel = false
    }.Validated();

    private static int Refusals(SimulationOptions options) =>
        Simulator.Run(options).Games.SelectMany(game => game.Rounds).Sum(round => round.ClaimsRefused);

    /// <summary>Every round of a run as text, with the strategy's name taken out of it.</summary>
    private static IReadOnlyList<string> Rounds(SimulationOptions options) =>
    [
        .. Simulator.Run(options).Games.SelectMany(game => game.Rounds.Select(round =>
            $"{round.Turns}:{round.Reshuffles}:"
            + string.Join(
                "|",
                round.Seats.Select(seat =>
                    $"{seat.Seat},{seat.Won},{seat.Covered},{seat.Takes},{seat.Draws},{seat.Claims}"))))
    ];
}
