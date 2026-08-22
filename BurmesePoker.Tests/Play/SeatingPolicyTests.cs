using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// ✅ <b>Packet P36: how long a seating holds</b> (RULES.md §3 step 2, rev 28; §10 #22).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is a revert-shaped packet that is not a revert.</b> Before P28 a seating was drawn
/// once and held for a whole match and could never change; P28 re-drew it before every deal on
/// rev 19's reading; rev 28 withdrew that reading on the expert's own words. What the rules want
/// is a seating that is <em>held</em> and can be <em>changed</em>, which is neither of the two
/// behaviours this engine has had.
/// </para>
/// <para>
/// ⚠️ <b>The policy is the mechanism and not the rule.</b> §3 says the seats change when the
/// players <em>agree</em> (§9 #45, ruled by Nick on 2026-08-22), and a number chosen before
/// anybody sits down is not people agreeing. The two tests at the bottom of this file are named
/// for §9 #45 and §9 #47 so that neither can be quietly assumed by a policy that only counts
/// rounds.
/// </para>
/// </remarks>
public class SeatingPolicyTests
{
    /// <summary>
    /// ✅ <b>The rule, and the default</b>: a seating is drawn once and kept, however many rounds
    /// are played.
    /// </summary>
    [Fact]
    public void AHeldSeatingIsNeverDrawnAgain()
    {
        Assert.Equal(SeatingPolicy.Held, SeatingPolicy.Default);
        Assert.Equal(0, SeatingPolicy.Held.RoundsBetweenSeatings);

        for (var played = 0; played < 50; played++)
        {
            Assert.False(SeatingPolicy.Held.ReseatsBefore(played));
        }
    }

    /// <summary>
    /// ⚠️ <b>The reading rev 28 withdrew, kept as a choice a table may make</b> — and the shape
    /// this engine had between P28 and P36, so a journal from that window still replays.
    /// </summary>
    [Fact]
    public void EveryRoundDrawsAgainBeforeEveryDealButTheFirst()
    {
        Assert.False(SeatingPolicy.EveryRound.ReseatsBefore(0));

        for (var played = 1; played < 20; played++)
        {
            Assert.True(SeatingPolicy.EveryRound.ReseatsBefore(played));
        }
    }

    /// <summary>
    /// <b>N rounds between seatings means the seats hold for N rounds</b> — and the first round
    /// is never re-drawn, because whoever opened the table has already seated it.
    /// </summary>
    [Fact]
    public void EveryNRoundsHoldsForNRoundsAtATime()
    {
        var policy = SeatingPolicy.Every(5);

        Assert.Equal(5, policy.RoundsBetweenSeatings);
        Assert.Equal([5, 10, 15, 20], Enumerable.Range(0, 21).Where(policy.ReseatsBefore).ToArray());
    }

    /// <summary>
    /// ⚠️ <b>Zero is "never", and that is the whole of the state.</b> The temptation is a flag
    /// beside a number, which is two states too many and admits <em>"re-seating is on, every 0
    /// rounds"</em>.
    /// </summary>
    [Fact]
    public void ZeroOrAbsentRoundsBetweenSeatingsMeansNever()
    {
        Assert.Equal(SeatingPolicy.Held, SeatingPolicy.Of(0));
        Assert.Equal(SeatingPolicy.Held, SeatingPolicy.Of(-4));
        Assert.Equal(SeatingPolicy.EveryRound, SeatingPolicy.Of(1));

        // The strict door refuses what the forgiving one accepts, so code that means it says so.
        Assert.Throws<ArgumentOutOfRangeException>(() => SeatingPolicy.Every(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SeatingPolicy.Every(-1));
    }

    /// <summary>
    /// ✅ <b>A name resolves through the domain, and an unknown one opens the default table
    /// rather than throwing a site over</b> — P18's rule for the difficulty dial, applied to the
    /// setting P36 adds beside it.
    /// </summary>
    [Fact]
    public void EveryPolicyRoundTripsThroughItsOwnName()
    {
        foreach (var policy in (SeatingPolicy[])[..SeatingPolicy.Offered, SeatingPolicy.Every(7)])
        {
            Assert.Equal(policy, SeatingPolicy.Find(policy.Name));
            Assert.Equal(policy, SeatingPolicy.Resolve(policy.Name.ToUpperInvariant()));
        }

        Assert.Equal("held", SeatingPolicy.Held.Name);
        Assert.Equal("every-round", SeatingPolicy.EveryRound.Name);
        Assert.Equal("every-5-rounds", SeatingPolicy.Every(5).Name);

        Assert.Null(SeatingPolicy.Find("rubbish"));
        Assert.Null(SeatingPolicy.Find("every-0-rounds"));
        Assert.Null(SeatingPolicy.Find(null));
        Assert.Equal(SeatingPolicy.Default, SeatingPolicy.Resolve("rubbish"));
        Assert.Equal(SeatingPolicy.Default, SeatingPolicy.Resolve(null));
    }

    /// <summary>
    /// <b>What a front end offers is what the domain lists</b>, held first because it is the rule.
    /// </summary>
    [Fact]
    public void TheOfferedListLeadsWithTheRule()
    {
        Assert.Equal(SeatingPolicy.Held, SeatingPolicy.Offered[0]);
        Assert.Contains(SeatingPolicy.EveryRound, SeatingPolicy.Offered);
        Assert.Equal(SeatingPolicy.Offered.Count, SeatingPolicy.Offered.Distinct().Count());
        Assert.All(SeatingPolicy.Offered, policy => Assert.NotEmpty(policy.Description));
    }

    /// <summary>
    /// ✅ <b>P36 acceptance 4, at the root: no published figure can move</b>, because the harness
    /// plays one round a game and a policy cannot reach a round that has no predecessor.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Asserted rather than argued</b> (the packet's own words). The default is checked
    /// here as well as the identity, because the argument is only as good as the 1.
    /// </remarks>
    [Fact]
    public void NoPolicyCanReachTheFirstRoundOfAGame()
    {
        Assert.Equal(1, new SimulationOptions { Strategies = StrategyCatalog.Ladder }.RoundsPerGame);

        foreach (var policy in (SeatingPolicy[])[..SeatingPolicy.Offered, SeatingPolicy.Every(2)])
        {
            Assert.False(policy.ReseatsBefore(0));
        }
    }

    /// <summary>
    /// ⚠️ <b>§9 #45 — a re-seating happens when the players <em>agree</em>, and nothing here asks
    /// them.</b>
    /// </summary>
    /// <remarks>
    /// <b>A fence, so P37's rule cannot be quietly assumed by a policy that only counts rounds.</b>
    /// The decision is a pure function of how many rounds have been played: it takes no player, no
    /// request and no answer, and <see cref="IPlayerAgent"/> still asks the five questions it asked
    /// before this packet. A sixth — <em>shall we change seats</em> — is packet P37's, and it is
    /// public rather than seat-private, which is why it is not smuggled in here.
    /// </remarks>
    [Fact]
    public void NobodyIsAskedWhetherToChangeSeats()
    {
        var asked = typeof(IPlayerAgent).GetMethods().Select(method => method.Name).ToArray();

        Assert.Equal(5, asked.Length);
        Assert.DoesNotContain(asked, name => name.Contains("Seat", StringComparison.OrdinalIgnoreCase));

        var decide = typeof(SeatingPolicy).GetMethod(nameof(SeatingPolicy.ReseatsBefore))!;

        Assert.Equal([typeof(int)], decide.GetParameters().Select(parameter => parameter.ParameterType));
    }

    /// <summary>
    /// ⚠️ <b>§9 #47 — whether <em>agree</em> means everybody or most of them is not decided
    /// here.</b>
    /// </summary>
    /// <remarks>
    /// <b>The second fence.</b> A policy that counted votes would have answered #47 by accident;
    /// this one cannot count anything but rounds, so the recommendation standing in <c>RULES.md</c>
    /// (unanimous among the people at the table) is still P37's to take or to overturn. What is
    /// asserted is that two tables agree on when to re-seat whenever they agree on the number —
    /// no seat, no majority and no table size is anywhere in the answer.
    /// </remarks>
    [Fact]
    public void WhatAgreementMeansIsNotDecidedByCountingRounds()
    {
        var four = SeatingPolicy.Every(3);
        var six = SeatingPolicy.Of(3);

        Assert.Equal(four, six);
        Assert.Equal(
            Enumerable.Range(0, 12).Select(four.ReseatsBefore),
            Enumerable.Range(0, 12).Select(six.ReseatsBefore));
    }
}
