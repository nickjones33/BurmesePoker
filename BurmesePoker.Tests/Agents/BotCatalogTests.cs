using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Sim;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// ✅ <b>P18 — one catalog.</b> The ladder is named in one place, and everything that offers a
/// bot resolves the name rather than keeping a list of its own.
/// </summary>
/// <remarks>
/// <para>
/// <b>Four independent notions of <em>which bot</em> is what this replaced</b>: the harness's
/// own catalog, a private two-value enum in the console, two hard-coded <c>new</c>s in the
/// browser client and a default factory on the server's options — with the browser offering no
/// difficulty setting at all. The point of the packet is that a fifth rung is now one entry in
/// one list.
/// </para>
/// <para>
/// ⚠️ <b>The names are frozen and this is where that is said out loud</b> (BUILD-PLAN §3.8
/// item 4). A name is half of every row's join key in <c>docs/strategy/measurements.csv</c> and
/// the whole of a journal header's seat, so renaming a rung silently unjoins every number this
/// project has published.
/// </para>
/// </remarks>
public class BotCatalogTests
{
    /// <summary>
    /// The names, in ladder order, as every CSV and journal written so far spells them.
    /// </summary>
    /// <remarks>
    /// Written out rather than derived: a test that reads the catalog to check the catalog
    /// would agree with any rename at all.
    /// </remarks>
    private static readonly string[] Ladder =
        ["random", "simple", "greedy", "cautious", "counting", "outs", "warden", "prospector"];

    [Fact]
    public void TheLadderIsTheEightRungsInTheOrderTheyWereBuilt()
    {
        Assert.Equal(Ladder, BotCatalog.All.Select(rung => rung.Name));
    }

    /// <remarks>
    /// ✅ <b>P18 acceptance 3 and 5.</b> A P16 CSV and a P14 journal both name a strategy in
    /// text; both still join if — and only if — the names they carry still resolve.
    /// </remarks>
    [Theory]
    [InlineData("greedy")]
    [InlineData("simple")]
    [InlineData("random")]
    [InlineData("cautious")]
    [InlineData("counting")]
    [InlineData("outs")]
    [InlineData("warden")]
    [InlineData("prospector")]
    public void AnOlderFileNamingARungStillResolvesToOne(string written)
    {
        var rung = BotCatalog.Resolve(written);

        Assert.Equal(written, rung.Name);
        Assert.Equal(written, BotCatalog.Resolve(written.ToUpperInvariant()).Name);
        Assert.NotNull(BotCatalog.Find(written));
    }

    [Fact]
    public void AnUnknownNameIsARefusalAndNotAGuess()
    {
        var complaint = Assert.Throws<ArgumentException>(() => BotCatalog.Resolve("thoughtful"));

        // The list is in the complaint, because the only person who sees it is somebody who
        // has just typed a name into a command line.
        Assert.All(Ladder, name => Assert.Contains(name, complaint.Message, StringComparison.Ordinal));
        Assert.Null(BotCatalog.Find("thoughtful"));
        Assert.Null(BotCatalog.Find(null));
    }

    /// <summary>
    /// 🔥 <b>A mirror is a label on a copy, not a way of playing.</b>
    /// </summary>
    /// <remarks>
    /// The tournament's null cell seats one strategy twice under <c>"{name}#mirror"</c> so the
    /// harness can measure its own bias (P17). That is a thing a <see cref="Strategy"/> may
    /// wear and a <see cref="BotRung"/> may not: a front end offering <c>greedy#mirror</c> as
    /// an opponent would be absurd, and the suffix was chosen to be unusable precisely so this
    /// is cheap to hold.
    /// </remarks>
    [Fact]
    public void TheCatalogNeitherHoldsNorResolvesAMirror()
    {
        Assert.All(BotCatalog.All, rung => Assert.DoesNotContain(BotRung.Reserved, rung.Name));

        var mirrored = BotCatalog.Hardest.Name + Tournament.MirrorSuffix;

        Assert.Null(BotCatalog.Find(mirrored));
        Assert.Throws<ArgumentException>(() => BotCatalog.Resolve(mirrored));

        // And a rung cannot be given such a name in the first place, by construction or by
        // `with` — which is exactly how the tournament makes its copy.
        Assert.Throws<ArgumentException>(() => BotCatalog.Hardest with { Name = mirrored });
        Assert.Throws<ArgumentException>(
            () => new BotRung(mirrored, "A copy of a rung.", 2, _ => new GreedyBotAgent()));
    }

    [Fact]
    public void EveryRungSaysWhatItPlaysLikeInWordsAPersonChoosingAnOpponentCouldUse()
    {
        Assert.All(BotCatalog.All, rung =>
        {
            Assert.False(string.IsNullOrWhiteSpace(rung.Description));

            // A description that is the name again, or that names the class behind it, is a
            // menu entry that helps nobody.
            Assert.NotEqual(rung.Name, rung.Description, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("Agent", rung.Description, StringComparison.Ordinal);
        });
    }

    /// <remarks>
    /// ⚠️ <b>Per seat, never shared</b> (§3.7): an agent that remembered anything across games
    /// would make a run depend on the order its games were scheduled in.
    /// </remarks>
    [Fact]
    public void ARungMakesAFreshAgentEveryTimeItIsAsked()
    {
        Assert.All(BotCatalog.All, rung =>
        {
            var first = rung.Create(1);
            var second = rung.Create(1);

            Assert.NotNull(first);
            Assert.IsAssignableFrom<IPlayerAgent>(first);
            Assert.NotSame(first, second);
        });
    }

    /// <summary>
    /// ⚠️ <b>Two orders, answering two different questions.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="BotCatalog.All"/> is the ladder — ordered by which decision was added, which
    /// is what a research instrument wants and what every report is ordered by.
    /// <see cref="BotCatalog.ByStrength"/> is what a person choosing an opponent is shown.
    /// Neither is derivable from the other, so the menu is checked for being the same set.
    /// </remarks>
    [Fact]
    public void TheMenuOrderIsStrongestFirstAndHoldsEveryRung()
    {
        Assert.Equal(
            [.. BotCatalog.All.OrderBy(rung => rung.Name, StringComparer.Ordinal)],
            BotCatalog.ByStrength.OrderBy(rung => rung.Name, StringComparer.Ordinal));

        var strengths = BotCatalog.ByStrength.Select(rung => rung.Strength).ToList();

        Assert.Equal(strengths.OrderByDescending(strength => strength), strengths);
        Assert.Same(BotCatalog.ByStrength[0], BotCatalog.Hardest);
        Assert.Equal(BotCatalog.All.Max(rung => rung.Strength), BotCatalog.Hardest.Strength);
    }

    /// <summary>
    /// 🔥 <b>Rungs nothing can separate share a level, and the menu keeps ladder order between
    /// them.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>greedy</c>, <c>cautious</c> and <c>counting</c> are mutually inside the interval
    /// (<c>docs/strategy/measurements.csv</c>, P17 and P20). Giving them different levels would
    /// put a difference in front of a person that nobody can feel, because it is not there
    /// (§3.12 item 2) — and it would quietly change what the console's default opponent is.
    /// </para>
    /// <para>
    /// 🔥 <b>And the other half of the same rule: a rung that <em>is</em> separated must not
    /// share their number.</b> P21 measured <c>outs</c> at <c>+3.1 ± 1.0</c> over <c>greedy</c>,
    /// surviving Holm, so it is a level of its own and it is <see cref="BotCatalog.Hardest"/> —
    /// which is what every difficulty level, the browser's hint and a stand-in seat are built
    /// on. <b>This assertion is what would have caught leaving the strength behind when the
    /// measurement moved.</b>
    /// </para>
    /// </remarks>
    [Fact]
    public void TheRungsMeasurementCannotSplitAreOneLevelAndTheOneItCanIsNot()
    {
        Assert.Equal(BotCatalog.Resolve("greedy").Strength, BotCatalog.Resolve("cautious").Strength);
        Assert.Equal(BotCatalog.Resolve("greedy").Strength, BotCatalog.Resolve("counting").Strength);

        Assert.True(BotCatalog.Resolve("outs").Strength > BotCatalog.Resolve("greedy").Strength);
        Assert.Equal("outs", BotCatalog.Hardest.Name);

        // 🔥 And the case P22 added: two rungs that play the *same cards* at the stakes the game
        // is actually played for share a level by construction rather than by measurement.
        // `prospector` differs from `outs` only in what it does when the side bet is worth more
        // than a melded card, which at $5/$1 it never is (docs/STRATEGY.md §10), so the two are
        // one player at this table and the menu must not pretend otherwise. Ladder order breaks
        // the tie, which is why `outs` is still what a stand-in seat and a hint are built on.
        Assert.Equal(BotCatalog.Resolve("outs").Strength, BotCatalog.Resolve("prospector").Strength);
    }

    /// <remarks>
    /// ⚠️ <b>The harness's catalog is an adapter over this one</b> (P18), and the order is the
    /// ladder's in both — a report ordered by anything else would renumber every column of
    /// every CSV published so far.
    /// </remarks>
    [Fact]
    public void TheHarnessNamesTheSameRungsInTheSameOrder()
    {
        Assert.Equal(
            BotCatalog.All.Select(rung => rung.Name),
            StrategyCatalog.All.Select(strategy => strategy.Name));
    }
}
