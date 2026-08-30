using System.Text.RegularExpressions;

using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>P56 — opening a table you actually want</b>: the shape, the quorum, and who is in the
/// seats you did not keep for people.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The form's decisions are in <see cref="NewTable"/> rather than in the markup, and that
/// is what makes them assertable.</b> Nothing in this project renders a component in a test —
/// the browser client is held by source scan and by playing — so clamping, defaults and the
/// two-step post would be the one part of the lobby nothing could check if they lived in a
/// <c>.razor</c> file.
/// </para>
/// <para>
/// ⚠️ <b>The per-seat acceptance is asserted on <see cref="HostedTable.Difficulties"/> and not
/// on the form</b>: what a person asked for has to reach the seat it was asked for, and a model
/// that merely holds the right strings has proved nothing.
/// </para>
/// </remarks>
public class OpeningATableTests
{
    /// <summary>What a site was started with — the plan a form's answers are laid over.</summary>
    private static TablePlan Opening { get; } = new()
    {
        Title = "The house table",
        Seats = RoundEngine.DefaultPlayers,
        People = 1,
        Seed = 20260819,
        Pace = TimeSpan.Zero,
        BetweenRounds = TimeSpan.FromMilliseconds(1),
        Patience = TimeSpan.Zero
    };

    /// <summary>
    /// ✅ <b>P56 item 4 — the form opens on the table this game is played at.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>P32's exact confusion, one layer up.</b> The house table and every published
    /// measurement are <see cref="RoundEngine.DefaultPlayers"/>; the open form defaulted to
    /// <see cref="RoundEngine.MinimumPlayers"/>, which is the smallest <em>legal</em> table and
    /// not the table — the same mis-reading that made the whole measurement set four-handed.
    /// </remarks>
    [Fact]
    public void TheFormOpensOnTheTableThisGameIsPlayedAtRatherThanTheSmallestLegalOne()
    {
        var fresh = new NewTable();

        Assert.Equal(RoundEngine.DefaultPlayers, fresh.Seats);
        Assert.NotEqual(RoundEngine.MinimumPlayers, fresh.Seats);

        // And one person, so that the table a visitor opens is one they can sit down at alone.
        Assert.Equal(1, fresh.People);
        Assert.Equal(RoundEngine.DefaultPlayers - 1, fresh.ComputerSeats);
    }

    /// <summary>
    /// ✅ <b>The shape is clamped, and the people are a quorum inside it.</b>
    /// </summary>
    [Fact]
    public void ATableIsOpenedOnAShapeItCanActuallyHave()
    {
        var vast = new NewTable { Seats = 99, People = 99 };

        Assert.Equal(RoundEngine.MaximumPlayers, vast.WantedSeats);
        Assert.Equal(RoundEngine.MaximumPlayers, vast.WantedPeople);
        Assert.Equal(0, vast.ComputerSeats);

        var tiny = new NewTable { Seats = 1, People = -4 };

        Assert.Equal(RoundEngine.MinimumPlayers, tiny.WantedSeats);
        Assert.Equal(0, tiny.WantedPeople);
        Assert.Equal(RoundEngine.MinimumPlayers, tiny.ComputerSeats);
    }

    /// <summary>
    /// ✅ <b>P56 item 3 — choosing every seat takes two posts, and the model is what decides
    /// which post this is.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>§3.11 C12 is the obstacle, not <c>TablePlan</c>.</b> The lobby is static SSR on
    /// purpose and cannot grow a control per seat as the seat count is typed — which is why
    /// <em>a mixed table</em> was a checkbox in P19. ⚠️ <b>A count check rather than a flag</b>,
    /// so changing the shape on the second step asks the seats again: answers that no longer
    /// fit the table are not answers.
    /// </remarks>
    [Fact]
    public void ChoosingEverySeatIsAskedOnTheFormThatComesBack()
    {
        var wanted = new NewTable { Seats = 5, People = 1, Fill = SeatFill.Each };

        Assert.True(wanted.NeedsSeatChoices);

        wanted.AskForSeats();

        // Opened on the mix a person would otherwise have got, which is a form they can correct
        // rather than a blank one they have to fill in.
        Assert.Equal([.. DifficultyLadder.Spread(4).Select(level => level.Name)], wanted.PerSeat);
        Assert.False(wanted.NeedsSeatChoices);

        // A shape changed on the second step is a second step asked again.
        wanted.Seats = 6;
        Assert.True(wanted.NeedsSeatChoices);

        // Neither of the other two ways of filling the seats asks anything…
        Assert.False(new NewTable { Fill = SeatFill.Same }.NeedsSeatChoices);
        Assert.False(new NewTable { Fill = SeatFill.Mixed }.NeedsSeatChoices);

        // …and neither does a table with no computer seats to choose for.
        Assert.False(new NewTable { Seats = 4, People = 4, Fill = SeatFill.Each }.NeedsSeatChoices);
    }

    /// <summary>
    /// ✅ <b>P56 acceptance — a per-seat choice reaches the seat that was chosen for it.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Including an advanced rung, which is the whole of what P56 needed below the
    /// form</b>: <c>sprinter@0</c> is a probe, <c>DifficultyLadder.FindOrProbe</c> has resolved
    /// one since P19, and the only change was <c>Find</c> → <c>FindOrProbe</c> where a
    /// <em>seat</em> is resolved. ⚠️ <b>And the seat reads as the rung</b> — a person seeing
    /// <c>Mya Lay (sprinter@0)</c> is being shown the machinery.
    /// </remarks>
    [Fact]
    public async Task APerSeatChoiceReachesTheSeatItWasChosenFor()
    {
        var wanted = new NewTable
        {
            Title = "  Mixed feelings  ",
            Seats = 5,
            People = 1,
            Fill = SeatFill.Each,
            PerSeat = ["easy", "sprinter@0", "expert", "warden@0"]
        };

        var plan = wanted.Wanted(Opening, seed: 20260829);

        Assert.Equal("Mixed feelings", plan.Title);
        Assert.Equal(["easy", "sprinter@0", "expert", "warden@0"], plan.Difficulties);

        await using var table = new HostedTable(
            "test", plan with { Pace = TimeSpan.Zero, Patience = TimeSpan.Zero },
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        Assert.Equal(
            ["easy", "sprinter@0", "expert", "warden@0"],
            table.Difficulties.Select(level => level.Name));

        Assert.Equal(
            [BotCatalog.Hardest, BotCatalog.Resolve("sprinter"), BotCatalog.Hardest, BotCatalog.Resolve("warden")],
            table.Difficulties.Select(level => level.Rung));

        // A rung is seated at no mistake rate at all, which is the rung itself.
        Assert.Equal(0, table.Difficulties[1].MistakeRate);

        // ⚠️ What a person reads is the rung's own name; what a replay reads is the level's.
        Assert.Contains(table.Board.Names.Values, name => name.EndsWith("(sprinter)", StringComparison.Ordinal));
        Assert.DoesNotContain(table.Board.Names.Values, name => name.Contains('@', StringComparison.Ordinal));
    }

    /// <summary>
    /// ✅ <b>An opponent the menu never offered is not one a form can ask for.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>This is how "a rung with no published row is not offerable" is enforced rather than
    /// intended</b> (Nick's answer, 2026-08-29). <c>prospector</c> and <c>purist</c> are ranked
    /// on money and have no head-to-head row against the reference, so no margin can be shown
    /// beside them — and a name for one, arriving on a post, opens a table on the default level
    /// instead. ⚠️ <b>So is a probe at some other mistake rate</b>: a calibration instrument is
    /// not an opponent, whatever it is called.
    /// </remarks>
    [Fact]
    public void AnOpponentTheMenuNeverOfferedIsNotOneAPostCanAskFor()
    {
        foreach (var money in BotCatalog.StakesSensitive)
        {
            Assert.DoesNotContain(OpponentMenu.Advanced, opponent => opponent.Rung == money);
            Assert.False(OpponentMenu.Offers(DifficultyLevel.Probe(money, 0).Name));
        }

        Assert.False(OpponentMenu.Offers("outs@0.35"));
        Assert.False(OpponentMenu.Offers("rubbish"));
        Assert.False(OpponentMenu.Offers("outs"));
        Assert.False(OpponentMenu.Offers(null));

        var wanted = new NewTable
        {
            Seats = 4,
            People = 0,
            Fill = SeatFill.Each,
            PerSeat = ["prospector@0", "outs@0.35", "rubbish", "sprinter@0"]
        };

        var plan = wanted.Wanted(Opening, seed: 1);

        Assert.Equal(
            [DifficultyLadder.Default.Name, DifficultyLadder.Default.Name, DifficultyLadder.Default.Name, "sprinter@0"],
            plan.Difficulties);
    }

    /// <summary>
    /// ✅ <b>Every advanced opponent is a rung at no mistake rate, and none of them is a level.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The two lists stay two lists</b> (§3.12, as amended): a name in both would be
    /// ambiguous, and the advanced group is the ladder rather than a longer dial.
    /// </remarks>
    [Fact]
    public void TheAdvancedGroupIsTheLadderAndNotALongerDial()
    {
        Assert.NotEmpty(OpponentMenu.Advanced);
        Assert.Equal(BotCatalog.Hardest, OpponentMenu.Reference);

        foreach (var opponent in OpponentMenu.Advanced)
        {
            var level = DifficultyLadder.FindOrProbe(opponent.Value);

            Assert.NotNull(level);
            Assert.Equal(opponent.Rung, level!.Rung);
            Assert.Equal(0, level.MistakeRate);
            Assert.Equal(opponent.Rung.Name, OpponentMenu.Called(level));

            // Not a level: the dial cannot resolve it, and the dial's own names are unchanged.
            Assert.Null(DifficultyLadder.Find(opponent.Value));
            Assert.DoesNotContain(DifficultyLadder.All, dial => dial.Name == opponent.Value);
        }

        // A level is still called by its own name wherever a person reads one.
        Assert.All(DifficultyLadder.All, level => Assert.Equal(level.Name, OpponentMenu.Called(level)));
    }

    /// <summary>
    /// 🔥 <b>P57 — every opponent the lobby offers can be resolved <em>and built</em>.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>This is the test that would have caught the defect, and the distinction it draws is
    /// the whole of it.</b> The menu offered <c>random@0</c>;
    /// <see cref="DifficultyLadder.FindOrProbe"/> resolved it perfectly — resolution is exactly
    /// the step that succeeded — and <see cref="DifficultyLevel.Create"/> then threw
    /// <c>ArgumentException</c> inside <c>HostedTable.Fill</c>, so a person choosing that seat got
    /// <b>500</b> on a green tree. ⚠️ <b>A test that stops at resolution is the test this project
    /// already had.</b>
    /// </para>
    /// <para>
    /// ⚠️ <b>Both lists, because the dial goes through the same constructor</b> — every level is
    /// <c>BotCatalog.Hardest</c> wrapped in a <c>FallibleAgent</c>, so a future rung promoted to
    /// <c>Hardest</c> without <c>IRanksDiscards</c> would break all four levels at once and not
    /// merely the advanced group.
    /// </para>
    /// <para>
    /// ✅ <b>Proved able to fail</b> by putting <c>random</c> back into the menu: the offering is
    /// asserted here to be seatable, and <c>random@0</c> throws on construction rather than
    /// returning something useless.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryOpponentTheLobbyOffersCanActuallyBeBuilt()
    {
        foreach (var name in OpponentMenu.Advanced
            .Select(opponent => opponent.Value)
            .Concat(OpponentMenu.Levels.Select(level => level.Name)))
        {
            Assert.True(OpponentMenu.Offers(name), $"{name} is offered by the lobby's own form.");

            var level = DifficultyLadder.FindOrProbe(name);

            Assert.NotNull(level);

            // 🔥 Resolution is not construction. This is the line the packet exists for.
            Assert.NotNull(level!.Create(seed: 1));
        }

        // ⚠️ And the excluded one really would have thrown, so the exclusion is not superstition.
        var joke = DifficultyLevel.Probe(BotCatalog.Resolve("random"), 0);

        Assert.False(OpponentMenu.Offers(joke.Name));
        Assert.Throws<ArgumentException>(() => joke.Create(seed: 1));
    }

    /// <summary>
    /// ✅ <b>The lobby offers what this packet built, and quotes no figure of its own.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A margin typed into a <c>.razor</c> file is the one thing this project has a test
    /// for</b> (P34, P39, P50): prose has no column to disagree with. The prices are drawn from
    /// <see cref="OpponentMenu"/>, which <c>PublishedFigureTests</c> holds to
    /// <c>measurements.csv</c> — so the markup must contain no interval at all.
    /// </remarks>
    [Fact]
    public void TheLobbyOffersTheLadderWithItsPriceAndSaysWhatATableIsWaitingFor()
    {
        var lobby = Sources.Components
            .Single(file => file.Path.EndsWith("Tables.razor", StringComparison.Ordinal))
            .Text;

        // The dial at the head of the menu, the ladder behind an advanced group, and the price.
        Assert.Contains("OpponentMenu.Levels", lobby, StringComparison.Ordinal);
        Assert.Contains("OpponentMenu.Advanced", lobby, StringComparison.Ordinal);
        Assert.Contains("<optgroup", lobby, StringComparison.Ordinal);
        Assert.Contains("opponent.Price", lobby, StringComparison.Ordinal);

        // ⚠️ And no figure of its own: every margin on this page came out of the CSV.
        Assert.DoesNotContain('±', lobby);

        // The quorum, said before the button is pressed and again in the list afterwards.
        Assert.Contains("the first card is dealt when every seat", lobby, StringComparison.Ordinal);
        Assert.Contains("it deals when they are all here", lobby, StringComparison.Ordinal);

        // The seat count reads the default rather than the floor (P56 item 4, P32's mistake).
        Assert.False(
            Regex.IsMatch(lobby, @"Seats\s*=\s*RoundEngine\.MinimumPlayers"),
            "the open form is defaulting to the smallest legal table again (P32, P56 item 4).");
    }
}
