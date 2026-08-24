using System.Globalization;

using BurmesePoker.Domain.Agents;

using BurmesePoker.Sim;

using BurmesePoker.Tests.Web;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// ✅ <b>P23 — the standing answer, held to the code by a test rather than by proofreading.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Two things drift, and both of them drift silently.</b> A rung added to
/// <see cref="BotCatalog"/> can fail to reach the standing suite, in which case
/// <c>docs/STRATEGY.md</c> is a document about a ladder that no longer exists; and a difficulty
/// level's ε can be moved without the published calibration moving with it, in which case the
/// menu offers four settings the document says something else about. <b>Neither has an
/// exception to throw and neither makes a test red on its own.</b> This file is what makes both
/// of them red.
/// </para>
/// <para>
/// ⚠️ <b>The second half reads <c>docs/strategy/measurements.csv</c> from the tree</b>, which is
/// unusual for a unit test and is the point: the file is what the documentation quotes, so the
/// only join worth asserting is between the file and the menu. Regenerating it is
/// <c>sim suite</c>, and a run that changes a level's ε without re-running is exactly the state
/// this refuses to let pass.
/// </para>
/// </remarks>
public class StandingAnswerTests
{
    /// <summary>The published data, one row to an id.</summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Published { get; } = Read();

    /// <summary>
    /// ✅ <b>P23 acceptance 2 — the levels published are the levels offered.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>ε is the whole of the calibration</b> (BUILD-PLAN §3.12), so a level whose rate has
    /// moved since the suite last ran is a level nobody has measured — and it would still be
    /// monotone, still pass <c>DifficultyCalibrationTests</c>, and still be offered in both
    /// front ends. The mistake rate is in the file precisely so that this comparison exists.
    /// </remarks>
    [Fact]
    public void EveryLevelTheMenuOffersIsALevelTheDocumentPublished()
    {
        foreach (var level in DifficultyLadder.All)
        {
            var rate = Row($"difficulty.mistake-rate.{level.Name}");

            Assert.Equal(
                level.MistakeRate,
                double.Parse(rate[Column.Mean], CultureInfo.InvariantCulture),
                6);

            // And it was actually played at that rate, not merely listed at it.
            Assert.True(
                Published.ContainsKey($"difficulty.reference-table.{level.Name}"),
                $"{level.Name} carries a published mistake rate but was never at the reference table.");
        }

        // The other direction, which is the one a deleted level breaks: the file may not
        // publish a setting nobody is offered (§3.12 item 2 — a level that cannot be separated
        // from its neighbour is deleted rather than shipped, and the document goes with it).
        Assert.Equal(
            DifficultyLadder.All.Select(level => level.Name).Order(),
            Published.Keys
                .Where(id => id.StartsWith("difficulty.mistake-rate.", StringComparison.Ordinal))
                .Select(id => id["difficulty.mistake-rate.".Length..])
                .Order());
    }

    /// <summary>
    /// ✅ <b>P23 acceptance 2, the half that is the dial's actual claim.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A monotone dial claims exactly k−1 things</b>, so the published steps have to be
    /// the adjacent pairs of the shipped list in the shipped order — and every one of them has
    /// to have survived the correction. A step that came back <em>inside the interval</em> and
    /// was shipped anyway is the lie §3.12 item 2 is about.
    /// </remarks>
    [Fact]
    public void EveryStepTheDialClaimsWasPublishedAndSeparated()
    {
        var steps = DifficultyLadder.All
            .Zip(DifficultyLadder.All.Skip(1), (below, above) => $"difficulty.step.{above.Name}-over-{below.Name}")
            .ToList();

        Assert.Equal(DifficultyLadder.All.Count - 1, steps.Count);

        foreach (var step in steps)
        {
            var row = Row(step);

            Assert.Equal("separated (Holm)", row[Column.Verdict]);

            // Stated the strong way round: the step is positive and clears its own interval.
            var mean = double.Parse(row[Column.Mean], CultureInfo.InvariantCulture);
            var interval = double.Parse(row[Column.Interval], CultureInfo.InvariantCulture);

            Assert.True(mean > interval, $"{step} is {mean:0.0000} ± {interval:0.0000}, which is not a step.");
        }

        Assert.Equal(
            steps.Order(),
            Published.Keys.Where(id => id.StartsWith("difficulty.step.", StringComparison.Ordinal)).Order());
    }

    /// <summary>
    /// ✅ <b>P23 — a rung cannot be added without being measured, and now that is a test.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>P20 made the suite's field default to the catalog; a default is not a guarantee.</b>
    /// Nothing failed if a rung was added and the document was never regenerated, and nothing
    /// failed if a future front end wrote the field out again — which is the defect P18 and P20
    /// each removed one layer apart. This is the layer above both: <b>every rung in the catalog
    /// is the subject of a published measurement</b>, whichever instrument settled it.
    /// </para>
    /// <para>
    /// ⚠️ <b>It reads the file rather than running the suite</b>, deliberately. Running it would
    /// assert that the code measures the catalog, which is true by construction and worth
    /// nothing; reading it asserts that somebody <em>ran</em> it, which is the thing that
    /// actually goes stale.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryRungInTheCatalogIsMeasuredBySomethingInTheDocument()
    {
        var subjects = Published.Values.Select(row => row[Column.Subject]).ToList();

        Assert.All(BotCatalog.All, rung =>
            Assert.True(
                subjects.Any(subject => Names(subject).Contains(rung.Name, StringComparer.Ordinal)),
                $"'{rung.Name}' is in BotCatalog and nothing in docs/strategy/measurements.csv is about it. "
                + "A rung reaches the ladder tournament or the money sweep by what it declares in "
                + "BotRung.Ranked (BUILD-PLAN P23) — and the file is regenerated by `sim suite`."));
    }

    /// <summary>
    /// 🔥 <b>The two instruments partition the catalog: every rung, exactly one of them.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The promise the ladder field's shortening rests on.</b> Dropping
    /// <c>prospector</c> from the round-robin is only honest because the money sweep picks it
    /// up; a third value of <see cref="RankedOn"/> that nothing ran would quietly drop a rung
    /// out of the programme altogether.
    /// </remarks>
    [Fact]
    public void TheLadderAndTheSweepBetweenThemAreTheWholeCatalog()
    {
        Assert.Equal(
            BotCatalog.All.Select(rung => rung.Name).Order(),
            BotCatalog.Ladder.Concat(BotCatalog.StakesSensitive).Select(rung => rung.Name).Order());

        Assert.Empty(BotCatalog.Ladder.Intersect(BotCatalog.StakesSensitive));
        Assert.NotEmpty(BotCatalog.StakesSensitive);

        // ⚠️ Both are in *ladder* order, which is what every report and every CSV is ordered by
        // — a filter that reordered the field would renumber columns published since P12.
        Assert.Equal([.. BotCatalog.All.Where(BotCatalog.Ladder.Contains)], BotCatalog.Ladder);

        // 🔥 The invariant SuiteOptions.MoneyReference rests on: the strongest rung there is, is
        // one the ladder tournament ranks — so the side bet is swept against a rung that has
        // actually been ranked against a field.
        // ⚠️ It read `Assert.Same(Hardest, Ladder[^1])` until P31, and warden is what took the
        // last entry away from it: two rungs now hang off `outs`, so the ladder is a tree and its
        // tail is whichever branch was written last. That was never a claim about strength — it
        // was true by coincidence for six rungs running, which is exactly how a coincidence gets
        // asserted as a law.
        Assert.Contains(BotCatalog.Hardest, BotCatalog.Ladder);

        var options = new SuiteOptions();

        Assert.Equal(BotCatalog.Hardest.Name, options.MoneyReference);

        // ⚠️ Every money-ranked rung, not the last one written (P44): a single challenger
        // defaulting to StakesSensitive[^1] would have dropped prospector from the programme
        // the day purist landed, in complete silence.
        Assert.Equal(
            BotCatalog.StakesSensitive.Select(rung => rung.Name),
            options.MoneyChallengers);
        Assert.Equal(BotCatalog.Ladder.Select(rung => rung.Name), options.Strategies.Select(one => one.Name));
    }

    /// <summary>
    /// ✅ <b>P23 — the menu is drawn from the dial, in both front ends.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A source scan, in the spirit of <c>LayeringTests</c></b>, and for the same reason: the
    /// rule is about where a sentence is <em>written down</em>. A level's description is the
    /// honest one-line explanation a person chooses by, and a copy of it pasted into a prompt or
    /// a <c>&lt;select&gt;</c> would be a second place for it to drift — with the drifted one on
    /// the screen.
    /// </para>
    /// <para>
    /// ⚠️ <b>The rung descriptions are covered too</b>, and they are the case that has actually
    /// happened: the console offered <see cref="BotCatalog"/> as a difficulty menu until P19.
    /// </para>
    /// </remarks>
    [Fact]
    public void NoFrontEndWritesOutWhatALevelIsCalledOrWhatItPlaysLike()
    {
        var dial = Path.Combine("BurmesePoker.Domain", "Agents") + Path.DirectorySeparatorChar;

        var sentences = DifficultyLadder.All.Select(level => level.Description)
            .Concat(BotCatalog.All.Select(rung => rung.Description))
            .ToList();

        var found = 0;

        foreach (var (path, text) in Sources.Production)
        {
            foreach (var sentence in sentences.Where(one => text.Contains(one, StringComparison.Ordinal)))
            {
                found++;

                Assert.True(
                    path.StartsWith(dial, StringComparison.Ordinal),
                    $"{path}: writes out \"{sentence}\". A level and a rung say what they are like in one "
                    + "place, and every menu asks (BUILD-PLAN P23) — a copy is a second thing to keep true.");
            }
        }

        // A guard on the guard: each sentence is written exactly once, in the catalog or the dial.
        Assert.Equal(sentences.Count, found);

        // And both menus really are the dial, drawn strongest first — the order is the default,
        // which is the P18 bug in one line.
        Assert.Contains("DifficultyLadder.ByStrength", Sources.Production
            .Single(file => file.Path.EndsWith("Program.cs", StringComparison.Ordinal)
                && file.Path.StartsWith("BurmesePoker.Console", StringComparison.Ordinal)).Text,
            StringComparison.Ordinal);

        Assert.Contains("DifficultyLadder.ByStrength", Sources.Read("Components/Pages/Tables.razor"),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// ✅ <b>P29 — the two things the programme played for a year and never wrote down.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Round length and the abandoned count became load-bearing at P27.</b> Every rung's
    /// cover count used to be monotone — throwing back the card just taken restores the hand
    /// exactly — which is <c>GreedyBotAgent</c>'s own stated reason a table of bots reaches a
    /// declaration at all. RULES.md §5.1 takes the just-taken card out of the choice, so
    /// convergence is no longer guaranteed by construction and what stands behind it is a turn
    /// cap. <b>A document that publishes win rates and not whether the rounds finished is
    /// publishing a conditional probability without its condition.</b>
    /// </para>
    /// <para>
    /// ⚠️ <b>And the refusal rate is the size of a branch nobody measured.</b> Every rung refuses
    /// the opener the turned-up money card whenever RULES.md §4.5 allows it, which P28 decided
    /// from the rule's reasoning; this asserts the file says how often that decision is even
    /// reached, not that the answer is any particular number.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheDocumentSaysHowLongARoundRunsAndWhetherTheRoundsFinished()
    {
        foreach (var scope in new[] { "ladder", "difficulty", "claim-permission" })
        {
            var turns = double.Parse(Row($"play.turns-per-round.{scope}")[Column.Mean], CultureInfo.InvariantCulture);
            var abandoned = Row($"play.abandoned.{scope}");

            // A round is at least one turn a seat and shorter than the cap that would end it.
            Assert.InRange(turns, 4, 400);

            // ⚠️ Not asserted to be zero. A table that does not converge is a result and the
            // document has to be able to say so — what is asserted is that the verdict column
            // is honest about which case this run was.
            var rate = double.Parse(abandoned[Column.Mean], CultureInfo.InvariantCulture);

            Assert.Equal(rate == 0 ? "every game settled" : "SOME GAMES DID NOT SETTLE", abandoned[Column.Verdict]);

            Assert.True(Published.ContainsKey($"claim.refusal-rate.{scope}"));
            Assert.True(Published.ContainsKey($"claim.attempt-rate.{scope}"));
        }

        // And the decision P28 took has a measured price beside it, in both currencies.
        var arms = ClaimPolicy.Both(BotCatalog.Resolve(new SuiteOptions().ClaimRung));

        Assert.Equal(
            $"{arms[0].Name} over {arms[1].Name}",
            Row("claim.permission.refuse-over-allow")[Column.Subject]);

        Assert.True(Published.ContainsKey("claim.permission.money.refuse-over-allow"));
    }

    /// <summary>
    /// ✅ <b>P33, in P23's and P30.2's idiom: a rule that changes what a win is worth cannot be
    /// built without being measured.</b> RULES.md §7.3 pays a jokerless declaration ×2 or ×3, so
    /// the document has to say how often that is actually collected — at every scope it reports
    /// a round length for.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The rate is asserted to be a rate and not to be any particular number</b>, exactly
    /// as the abandoned count is. What it must not be is <b>absent</b>: a bonus nobody measured
    /// is a scoring rule the document cannot price.
    /// <para>
    /// ⚠️ <b>And what it measures is a floor.</b> No rung in the field knows the bonus exists —
    /// <c>CoverScore.Potential</c> returns <c>int.MaxValue</c> for a joker — so every jokerless
    /// declaration counted here came out clean by accident. A rung that played for the bonus
    /// would be a new rung under P15's discipline and would arrive with its own row.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheDocumentSaysHowOftenTheCleanBonusIsActuallyCollected()
    {
        foreach (var scope in new[] { "ladder", "difficulty", "claim-permission" })
        {
            var row = Row($"bonus.jokerless-rate.{scope}");
            var rate = double.Parse(row[Column.Mean], CultureInfo.InvariantCulture);

            Assert.InRange(rate, 0, 1);
            Assert.Equal("jokerless rate", row[Column.Metric]);
        }
    }

    /// <summary>
    /// ✅ <b>P35 — the document says how often a hand wins before anybody plays it</b>
    /// (<c>RULES.md</c> §7.4, <c>docs/STRATEGY.md</c> §15).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The row that would explain a money figure moving.</b> §7.4 is the one rule packet P35
    /// built that a one-round-per-game harness can observe at all, so if a published dollar figure
    /// shifts under it, this is the column that says why. ⚠️ <b>It is expected to be very small</b>
    /// — about one deal in 3,700 at P35's run — and the assertion is that it is a rate and that it
    /// is <b>published</b>, never that it is any particular number.
    /// </para>
    /// <para>
    /// ⚠️ <b>Its sibling §7.5 has no row and cannot have one</b>, which <c>docs/STRATEGY.md</c> §11
    /// states out loud: a third consecutive win cannot occur while every experiment plays one
    /// round a game (BUILD-PLAN §3.8). <b>An absent measurement with a written reason is a
    /// different thing from an absent measurement.</b>
    /// </para>
    /// </remarks>
    [Fact]
    public void TheDocumentSaysHowOftenAHandWinsBeforeAnybodyPlaysIt()
    {
        foreach (var scope in new[] { "ladder", "difficulty", "claim-permission" })
        {
            var row = Row($"bonus.deal-rate.{scope}");
            var rate = double.Parse(row[Column.Mean], CultureInfo.InvariantCulture);

            Assert.InRange(rate, 0, 1);
            Assert.Equal("deal-bonus rate", row[Column.Metric]);
        }
    }

    /// <summary>
    /// ✅ <b>P32 — the document is about the table this game is actually played at.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>This is the drift P32 existed to fix, and it drifted in complete silence for
    /// twenty packets.</b> <c>SuiteOptions.Seats</c> read <c>RoundEngine.MinimumPlayers</c>,
    /// so the whole standing set was four-handed because four is the smallest legal table and
    /// not because anybody chose it — and by <c>RULES.md</c> §7.1.1 and §7.3 the four-handed
    /// game is a <em>different game</em> from the five-handed one. Nothing was red the whole
    /// time.
    /// </para>
    /// <para>
    /// ⚠️ <b>The join is the file's own <c>seats</c> column against the constant</b>, so moving
    /// the default without re-running <c>sim suite</c> is a red build rather than a document
    /// quietly about the wrong table.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryPublishedRowWasPlayedAtTheTableTheSuiteIsAbout()
    {
        var seats = SuiteOptions.DefaultSeats.ToString(CultureInfo.InvariantCulture);

        Assert.All(
            Published.Values,
            row => Assert.Equal(seats, row[Column.Seats]));
    }

    /// <summary>
    /// ✅ <b>P32 item 4 — the longest-running measurement in the project keeps its continuity.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>P12's headline is the one figure this project has already had to correct</b> (P16
    /// re-measured it under a balanced seating and it moved a point), and it has been
    /// reproduced by P23, P29 and P33 since. Moving the standing set to five seats would have
    /// <em>ended</em> that series rather than continued it, so both sizes are published and the
    /// seat count is part of the id. ⚠️ <b>The pair is also the cheapest statement the file
    /// makes about which findings belong to the game and which belonged to four seats</b>: a
    /// four-handed declaration owes a joker-free series and a five-handed one owes nothing.
    /// </remarks>
    [Fact]
    public void TheHeadlineIsPublishedAtFourSeatsAndAtTheDefaultTable()
    {
        foreach (var seats in new[] { 4, SuiteOptions.DefaultSeats }.Distinct())
        {
            foreach (var plan in new[] { "rotate", "balanced" })
            {
                foreach (var rung in new[] { "greedy", "simple" })
                {
                    var row = Row($"headline.{plan}.{seats}-handed.{rung}");

                    Assert.Equal("win rate", row[Column.Metric]);
                    Assert.InRange(
                        double.Parse(row[Column.Mean], CultureInfo.InvariantCulture), 0, 1);

                    // ⚠️ The command has to name the table it was played at, not the table the
                    // rest of the file was: these two rows differ in exactly that.
                    Assert.Contains($"--seats {seats}", row[Column.Command], StringComparison.Ordinal);
                }
            }
        }
    }

    /// <summary>
    /// ✅ <b>P32 — the four-handed game is kept beside the five-handed one, not deleted.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Four-handed is a different game and not an old version of this one.</b> By
    /// <c>RULES.md</c> §7.1.1 a four-handed declaration owes a joker-free series and a
    /// five-handed one owes no series at all, and by §7.3 a jokerless hand is paid ×2 at four
    /// and ×3 at five — so P25 through P33's whole published set measures something that is
    /// still true, of a table this project no longer defaults to. <b>Deleting it would have
    /// thrown away the only controlled comparison anybody will ever have across that seam.</b>
    /// </para>
    /// <para>
    /// ⚠️ <b>It is frozen and it is not regenerated.</b> <c>sim suite</c> writes
    /// <c>measurements.csv</c> only; the archive is P33's run, kept as it stood, and this
    /// asserts what it claims to be — every row of it played at four seats. Re-making it is
    /// <c>sim suite --seats 4</c>, and that is a decision rather than a chore.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheFourHandedGameIsKeptAsANamedSecondTable()
    {
        var archive = Read("measurements-4-handed.csv");

        Assert.NotEmpty(archive);
        Assert.All(archive.Values, row => Assert.Equal("4", row[Column.Seats]));

        // 🔥 And it is a whole standing set rather than a souvenir: the ladder's ranking, the
        // dial's reference table and the money sweep's verdict are all in it, so a question
        // about the four-handed game can be answered from the file instead of from a session
        // log (§11's own rule, applied to the archive).
        Assert.Contains(archive.Keys, id => id.StartsWith("ladder.rank.", StringComparison.Ordinal));
        Assert.Contains(archive.Keys, id => id.StartsWith("difficulty.reference-table.", StringComparison.Ordinal));
        Assert.Contains(archive.Keys, id => id.StartsWith("money.net-per-round.", StringComparison.Ordinal));
    }

    /// <summary>Which column of a row is which. The header names them; this is the order.</summary>
    private static class Column
    {
        internal const int Subject = 1;
        internal const int Metric = 2;
        internal const int Mean = 4;
        internal const int Interval = 6;
        internal const int Verdict = 7;
        internal const int Seats = 9;
        internal const int Command = 11;
    }

    /// <summary>One published row, or a complaint naming the command that would create it.</summary>
    private static IReadOnlyList<string> Row(string id) =>
        Published.TryGetValue(id, out var row)
            ? row
            : throw new InvalidOperationException(
                $"docs/strategy/measurements.csv has no '{id}'. Regenerate it: "
                + "dotnet run -c Release --project BurmesePoker.Sim -- suite --games 8000 --seed 20260819");

    /// <summary>The words of a subject, so that a name is matched whole and not inside another.</summary>
    private static IReadOnlyList<string> Names(string subject) =>
        [.. subject.Split([' ', ',', '(', ')', '$', '/'], StringSplitOptions.RemoveEmptyEntries)];

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Read(
        string file = "measurements.csv")
    {
        var path = Path.Combine(Sources.Root.FullName, "docs", "strategy", file);
        var lines = File.ReadAllLines(path);

        return lines.Skip(1)
            .Where(line => line.Length > 0)
            .Select(Fields)
            .ToDictionary(row => row[0], row => row, StringComparer.Ordinal);
    }

    /// <summary>Splits a CSV line, respecting the quoting <c>SuiteCsv</c> writes.</summary>
    private static IReadOnlyList<string> Fields(string line)
    {
        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }
        }

        fields.Add(field.ToString());
        return fields;
    }
}
