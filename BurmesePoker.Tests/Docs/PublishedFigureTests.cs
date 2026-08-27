using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Docs;

/// <summary>
/// ✅ <b>P34 acceptance 3 — the numbers the prose quotes are checkable rather than proofread.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Three kinds of number go stale silently.</b> A figure transcribed out of
/// <c>docs/strategy/measurements.csv</c> into a table, the size of the tree, and the revision
/// of <c>RULES.md</c>. Each has been wrong at least once: a test count was written down as 709
/// for 715 and was caught by running rather than by reading, and a four-handed money figure
/// survived a re-measurement into a five-handed section.
/// </para>
/// <para>
/// ⚠️ <b>What makes the current figure findable is the form these documents are written in.</b>
/// They are newest-first, so the <em>first</em> count in a file is the current one and
/// everything after it is dated record. That is asserted rather than assumed — and it is what
/// lets the narrative keep every superseded number without any of them being checked as though
/// it were a claim about today.
/// </para>
/// </remarks>
public class PublishedFigureTests
{
    private static IReadOnlyDictionary<string, (double Mean, double Interval, string Verdict)> Published { get; } = Csv();

    /// <summary>
    /// ✅ <b>Every figure the ranking publishes is the figure in the file it was generated from.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b><c>docs/STRATEGY.md</c> §11 already forbids quoting a number from prose</b>; this
    /// makes the rule checkable instead of remembered. The three tables below are the whole
    /// transcription surface of the document — a 21-cell matrix, a ranking, and the difficulty
    /// dial's two tables — and a hand edit to any cell in them is a published number nobody
    /// measured.
    /// </para>
    /// <para>
    /// ⚠️ <b>The tolerance is derived from the printed precision</b>, never fixed: a cell
    /// written to one decimal is allowed half of one decimal, and one written to two is allowed
    /// half of two. A fixed tolerance would either reject honest rounding or accept a figure
    /// from the run before.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryFigureTheStrategyDocumentTabulatesIsTheFigureInTheCsv()
    {
        var checks = 0;

        // The head-to-head matrix: the row's win rate less the column's, in points.
        var matrix = Table("| | `random` |");
        var columns = Backticked(matrix[0]);

        foreach (var row in matrix.Skip(1))
        {
            var cells = Cells(row);
            var subject = Backticked(cells[0]).Single();

            foreach (var (cell, column) in cells.Skip(1).Zip(columns))
            {
                if (!cell.Contains('±'))
                {
                    continue;
                }

                var forward = Published.TryGetValue($"ladder.head-to-head.{subject}-over-{column}", out var found);
                var published = forward ? found : Published[$"ladder.head-to-head.{column}-over-{subject}"];
                var sign = forward ? 1 : -1;

                Agree(cell, sign * published.Mean * 100, published.Interval * 100, $"{subject} over {column}");
                checks++;
            }
        }

        // Every ordered pair of the field, both ways round and nothing on the diagonal.
        Assert.Equal(columns.Count * (columns.Count - 1), checks);

        // The ranking's free-for-all column, which is a different measurement of the same field.
        foreach (var row in Table("| # | strategy |").Skip(1))
        {
            var cells = Cells(row);
            var rung = Backticked(cells[1]).Single();
            var published = Published[$"ladder.free-for-all.{rung}"];

            Agree(cells[3], published.Mean * 100, published.Interval * 100, $"{rung} at the free-for-all");

            // The mean margin over the whole field, which carries no interval of its own.
            Compare(cells[2], Published[$"ladder.rank.{rung}"].Mean * 100, $"{rung}'s mean margin");
            checks += 2;
        }

        // The difficulty dial: the reference table, then the adjacent steps.
        foreach (var row in Table("| level | win % | step |").Skip(1))
        {
            var cells = Cells(row);
            var level = Backticked(cells[0]).Single();
            var published = Published[$"difficulty.reference-table.{level}"];

            Agree(cells[1], published.Mean * 100, published.Interval * 100, $"{level} at the reference table");
            checks++;
        }

        foreach (var row in Table("| step | margin | Holm |").Skip(1))
        {
            var cells = Cells(row);
            var step = Backticked(cells[0]);
            var published = Published[$"difficulty.step.{step[0]}-over-{step[1]}"];

            Agree(cells[1], published.Mean * 100, published.Interval * 100, $"the step {step[0]} over {step[1]}");
            checks++;
        }

        // A floor rather than a total: the field and the dial may both grow, and neither
        // should have to come back here — but a parse that silently found nothing must fail.
        Assert.True(checks > 50, $"only {checks} published figures were checked, which is not the document.");
    }

    /// <summary>
    /// ✅ <b>The guide written for a player quotes the same file the strategy document does.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>This fence moved home with the figures it holds</b> (P39). It was written against
    /// <c>docs/PLAYING.md</c>, whose <em>Playing better</em> section carried four transcribed
    /// figures — and went on quoting a <b>four-handed</b> reference table and a headline pair
    /// from a run two measurements old, while the table it describes deals <b>five</b>. Nothing
    /// noticed, because prose does not have a column to disagree with. Those figures live in
    /// <c>docs/HOW-TO-PLAY-WELL.md</c> now, with every other number a player is given.
    /// </para>
    /// <para>
    /// ⚠️ <b>Only the transcribed figures are fenced, never the prose around them</b> — each
    /// against the row of <c>measurements.csv</c> it was printed from, at the scale it is
    /// printed at (points of win rate, or dollars a round). And where the guide's sentence is a
    /// <em>verdict</em> — this is a null, that separates — the verdict is fenced too, because a
    /// margin can drift back inside its interval without the number moving much at all.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheFiguresThePlayersGuideQuotesAreTheFiguresInTheCsv()
    {
        var guide = Documentation.Text("docs/HOW-TO-PLAY-WELL.md");

        // The difficulty dial's reference table, weakest first.
        var dial = Regex.Match(guide, @"win\s+\*\*([\d.]+)% / ([\d.]+)% / ([\d.]+)% / ([\d.]+)%\*\*");

        Assert.True(dial.Success, "docs/HOW-TO-PLAY-WELL.md no longer says what the four settings win.");

        foreach (var (level, printed) in new[] { "easy", "medium", "hard", "expert" }
            .Zip(dial.Groups.Values.Skip(1).Select(group => group.Value)))
        {
            Compare(printed, Published[$"difficulty.reference-table.{level}"].Mean * 100, $"{level}'s share");
        }

        // The headline: what the tie-break is worth.
        var headline = Regex.Match(guide, @"\*\*([\d.]+)% of rounds against ([\d.]+)%\*\*");

        Assert.True(headline.Success, "docs/HOW-TO-PLAY-WELL.md no longer says what a tie-break is worth.");

        Compare(headline.Groups[1].Value, Published["headline.balanced.5-handed.greedy"].Mean * 100, "the thinking bot");
        Compare(headline.Groups[2].Value, Published["headline.balanced.5-handed.simple"].Mean * 100, "the simple bot");

        // Every margin the guide quotes: the sentence's anchor, the row it must agree with,
        // and the scale the fraction is printed at. ⚠️ The sign the sentence prints is part of
        // the anchor and the scale carries it, so a margin that flips direction fails here
        // rather than reading as its own opposite.
        (string Anchor, string Id, double Scale)[] margins =
        [
            (@"worth \*\*\+([\d.]+) ± ([\d.]+) points\*\*", "ladder.head-to-head.greedy-over-outs", -100),
            (@"measures\s+\*\*-\$([\d.]+) ± \$([\d.]+) a round\*\*", "money.net-per-round.5-1.prospector", -1),
            (@"banks \*\*\+\$([\d.]+) ± \$([\d.]+)\*\* a round", "money.net-per-round.5-20.prospector", 1),
            (@"\$5/\$40 \*\*\+\$([\d.]+) ± \$([\d.]+)\*\*", "money.net-per-round.5-40.prospector", 1),
            (@"\*\*-([\d.]+) ± ([\d.]+) points\*\* of win rate", "claim.permission.refuse-over-allow", -100),
            (@"\*\*-\$([\d.]+) ± \$([\d.]+)\*\* a round between", "claim.permission.money.refuse-over-allow", -1),
            (@"\*\*\+([\d.]+) ± ([\d.]+) points\*\* \*behind\*", "ladder.head-to-head.greedy-over-counting", 100),
            (@"measures \*\*\+([\d.]+) ± ([\d.]+) points\*\*", "ladder.head-to-head.greedy-over-cautious", 100),
            (@"gives up \*\*([\d.]+) ± ([\d.]+) points\*\*", "ladder.head-to-head.outs-over-warden", 100),
        ];

        foreach (var (anchor, id, scale) in margins)
        {
            var quoted = Regex.Match(guide, anchor);

            Assert.True(quoted.Success, $"docs/HOW-TO-PLAY-WELL.md no longer quotes {id}.");

            Compare(quoted.Groups[1].Value, Published[id].Mean * scale, $"{id}'s margin");
            Compare(quoted.Groups[2].Value, Published[id].Interval * Math.Abs(scale), $"{id}'s interval");
        }

        // Two figures printed without an interval: what chasing the money costs in rounds, and
        // how often the clean bonus is collected by players who are not trying.
        var slower = Regex.Match(guide, @"winning \*\*([\d.]+) points\*\* fewer rounds");

        Assert.True(slower.Success, "docs/HOW-TO-PLAY-WELL.md no longer says what chasing the money costs.");

        Compare(slower.Groups[1].Value, Published["money.win-rate.5-20.prospector"].Mean * -100, "what chasing costs in rounds");

        var jokerless = Regex.Match(guide, @"\*\*([\d.]+)%\*\* of rounds, purely by");

        Assert.True(jokerless.Success, "docs/HOW-TO-PLAY-WELL.md no longer says the accidental jokerless rate.");

        Compare(jokerless.Groups[1].Value, Published["bonus.jokerless-rate.ladder"].Mean * 100, "the accidental clean rate");

        // The verdicts the prose asserts: what the guide calls nothing must still be inside its
        // interval, and what it calls the one thing that works must still be separated.
        foreach (var (id, verdict) in new[]
        {
            ("ladder.head-to-head.greedy-over-cautious", "inside the interval"),
            ("ladder.head-to-head.greedy-over-counting", "inside the interval"),
            ("money.net-per-round.5-1.prospector", "inside the interval"),
            ("claim.permission.refuse-over-allow", "inside the interval"),
            ("ladder.head-to-head.greedy-over-outs", "separated (Holm)"),
            ("ladder.head-to-head.outs-over-warden", "separated (Holm)"),
            ("money.net-per-round.5-20.prospector", "separated (Holm)"),
            ("money.net-per-round.5-40.prospector", "separated (Holm)"),
        })
        {
            Assert.True(
                Published[id].Verdict == verdict,
                $"{id} is `{Published[id].Verdict}` and docs/HOW-TO-PLAY-WELL.md tells a player it is `{verdict}`.");
        }
    }

    /// <summary>
    /// ✅ <b>P39 acceptance 4 — a figure has one home.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b><c>docs/PLAYING.md</c>'s <em>Playing better</em> figures went two whole
    /// measurements stale before P34 fenced them, because a figure with two homes has none</b> —
    /// the suite was re-run, <c>docs/STRATEGY.md</c> was regenerated, and nobody thought of the
    /// second copy. The section is a pointer now and the figures live in
    /// <c>docs/HOW-TO-PLAY-WELL.md</c> alone.
    /// </para>
    /// <para>
    /// ⚠️ <b>Asserted as an absence, which is the only shape this fence can have</b>: the
    /// player-facing page carries the pointer, and carries no margin, no interval and no
    /// reference-table row of its own. A figure added back to <c>PLAYING.md</c> is this test
    /// red, whatever the figure says.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheFiguresHaveOneHomeAndThePlayingGuidePointsAtIt()
    {
        var playing = Documentation.Text("docs/PLAYING.md");

        Assert.Contains("HOW-TO-PLAY-WELL.md", playing, StringComparison.Ordinal);

        Assert.True(
            !playing.Contains('±'),
            "docs/PLAYING.md quotes a measured margin again. Those figures have one home now — "
            + "docs/HOW-TO-PLAY-WELL.md — and a second copy is how they went stale last time.");

        Assert.False(
            Regex.IsMatch(playing, @"\*\*[\d.]+% / [\d.]+% / [\d.]+% / [\d.]+%\*\*"),
            "docs/PLAYING.md quotes the difficulty reference table again; it lives in docs/HOW-TO-PLAY-WELL.md.");

        Assert.False(
            Regex.IsMatch(playing, @"% of rounds against"),
            "docs/PLAYING.md quotes the tie-break headline again; it lives in docs/HOW-TO-PLAY-WELL.md.");
    }

    /// <summary>
    /// ✅ <b>P49 acceptance — the document that teaches the instrument carries no figure of its own.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b><c>docs/SIMULATIONS.md</c> explains <em>how</em> the numbers are measured and points
    /// at the two documents that hold them</b> — <c>STRATEGY.md</c> and
    /// <c>HOW-TO-PLAY-WELL.md</c>. It is P39's one-home rule applied in advance: a document that
    /// carried a figure would be a second place that figure could go stale, which is the exact
    /// staleness class F10 caught living in <c>STRATEGY.md</c>'s prose. So the guarantee is that
    /// there is no figure here to rot.
    /// </para>
    /// <para>
    /// ⚠️ <b>Asserted as an absence, the same shape as the <c>PLAYING.md</c> fence above</b>: no
    /// interval marker and no percentage claim. The two pointers are asserted to still exist,
    /// because a teaching document that stopped pointing at the figures would send a reader
    /// nowhere. Commands the document prints are checked separately, by
    /// <c>DocumentationTests.EveryCommandTheDocumentationPrintsResolves</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheSimulationsGuideTeachesTheInstrumentAndCarriesNoFigureOfItsOwn()
    {
        var sims = Documentation.Text("docs/SIMULATIONS.md");

        Assert.Contains("STRATEGY.md", sims, StringComparison.Ordinal);
        Assert.Contains("HOW-TO-PLAY-WELL.md", sims, StringComparison.Ordinal);

        Assert.True(
            !sims.Contains('±'),
            "docs/SIMULATIONS.md quotes a measured margin. It teaches the instrument and has no "
            + "figures of its own — those live in STRATEGY.md and HOW-TO-PLAY-WELL.md, so it cannot "
            + "go stale the way F10's prose did.");

        var percentage = Regex.Match(sims, @"\d\s*%");

        Assert.False(
            percentage.Success,
            $"docs/SIMULATIONS.md states a percentage figure (`{percentage.Value}`). It is digit-free "
            + "by rule; say it in words and point at the file that carries the number.");
    }

    /// <summary>
    /// ✅ <b>The one measurement this project ships as prose <em>in the product</em> is still
    /// the measurement it claims to be.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Refusing a claim has been measured and is worth nothing either way</b>, and the
    /// browser says so on the felt when it explains the objection — a null makes an explanation
    /// more interesting rather than less, because it is a thing this project knows and no player
    /// does. ⚠️ <b>It deliberately carries no number</b>, so it cannot rot into a wrong figure
    /// the way a quoted interval would. <b>But it is still a measurement, and it is the only one
    /// outside <c>docs/</c>.</b>
    /// </para>
    /// <para>
    /// ⚠️ <b>So the fence is on the claim rather than on the wording</b>: if that cell ever
    /// separates, in either currency, the sentence on the table is wrong and this goes red.
    /// The sentence itself is asserted to still exist, because a fence on a sentence that has
    /// been rewritten away is no fence at all.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheOnlyMeasuredClaimTheProductSpeaksAloudIsStillANull()
    {
        var rationale = Documentation.Text("BurmesePoker.Presentation/AdviceRationale.cs");

        Assert.Contains("refusing is worth nothing either way", rationale, StringComparison.Ordinal);

        var rate = Published["claim.permission.refuse-over-allow"];
        var money = Published["claim.permission.money.refuse-over-allow"];

        Assert.Equal("inside the interval", rate.Verdict);

        Assert.True(
            Math.Abs(money.Mean) <= money.Interval,
            $"the claim's permission is now worth {money.Mean:+0.00;-0.00} ± {money.Interval:0.00} a round, "
            + "so AdviceRationale tells a player at the table something that has stopped being true.");
    }

    /// <summary>
    /// ✅ <b>The count these documents quote is the size of the tree, and the revision they
    /// quote is the revision of the rules.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Counted by discovery rather than by trust</b>: every method carrying a
    /// <c>[Fact]</c>, plus one for each row of data a <c>[Theory]</c> is given. That is the
    /// number a run reports, so a packet that adds tests and leaves the prose alone is a red
    /// build rather than a document quietly a packet behind.
    /// </para>
    /// <para>
    /// ⚠️ <b>Only the first count in each document is checked, and that is not a hedge.</b>
    /// These files are newest-first and keep every earlier figure on purpose — the session log
    /// records the tree at 677, 697, 715 and 795, and every one of those was true when it was
    /// written. <b>A check that demanded they all agree would be asking the project to delete
    /// its own history.</b>
    /// </para>
    /// </remarks>
    [Fact]
    public void TheFirstCountAndRevisionEachDocumentQuotesAreTheCurrentOnes()
    {
        var tests = TestCases();

        foreach (var document in new[] { "CLAUDE.md", "docs/STATUS.md" })
        {
            var text = Documentation.Text(document);
            var quoted = Regex.Match(text, @"green at \**([\d,]+)");

            Assert.True(quoted.Success, $"{document} no longer says how big a green tree is.");

            Assert.Equal(
                tests,
                int.Parse(quoted.Groups[1].Value.Replace(",", string.Empty), CultureInfo.InvariantCulture));
        }

        foreach (var document in new[] { "CLAUDE.md", "docs/STATUS.md", "docs/RULES.md" })
        {
            var quoted = Regex.Match(Documentation.Text(document), @"\brev(?:ision)?\s*\**\s*(\d+)");

            Assert.True(quoted.Success, $"{document} quotes no rules revision at all.");

            Assert.Equal(
                JournalHeader.CurrentRulesRevision,
                int.Parse(quoted.Groups[1].Value, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// The number of test cases in this assembly: a fact is one, a theory is one a data row.
    /// </summary>
    private static int TestCases()
    {
        var total = 0;

        foreach (var type in typeof(PublishedFigureTests).Assembly.GetTypes())
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(inherit: true);

                if (!attributes.Any(one => one is FactAttribute))
                {
                    continue;
                }

                var rows = 0;

                foreach (var data in attributes.OfType<Xunit.v3.IDataAttribute>())
                {
                    rows += data switch
                    {
                        InlineDataAttribute => 1,
                        MemberDataAttribute member => Rows(member, type),
                        _ => throw new InvalidOperationException(
                            $"{type.Name}.{method.Name} is fed by {data.GetType().Name}, which this count "
                            + "does not know how to size. Teach it, or the published test count is a guess.")
                    };
                }

                total += Math.Max(rows, 1);
            }
        }

        return total;
    }

    private static int Rows(MemberDataAttribute member, Type declaring)
    {
        var owner = member.MemberType ?? declaring;
        const BindingFlags Where = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        var value =
            owner.GetProperty(member.MemberName, Where)?.GetValue(null)
            ?? owner.GetMethod(member.MemberName, Where)?.Invoke(null, null)
            ?? owner.GetField(member.MemberName, Where)?.GetValue(null)
            ?? throw new InvalidOperationException($"{owner.Name} has no member {member.MemberName}.");

        return ((System.Collections.IEnumerable)value).Cast<object>().Count();
    }

    /// <summary>A published figure and its interval, in points, against what the prose printed.</summary>
    private static void Agree(string cell, double mean, double interval, string what)
    {
        var printed = Regex.Match(cell, @"([+-]?\d+(?:\.\d+)?)\s*±\s*(\d+(?:\.\d+)?)");

        Assert.True(printed.Success, $"{what}: `{cell}` is not a figure with an interval.");

        Compare(printed.Groups[1].Value, mean, $"{what}'s margin");
        Compare(printed.Groups[2].Value, interval, $"{what}'s interval");
    }

    private static void Compare(string printed, double measured, string what)
    {
        var value = double.Parse(printed, CultureInfo.InvariantCulture);
        var decimals = printed.Contains('.') ? printed.Length - printed.IndexOf('.') - 1 : 0;
        var slack = (0.5 * Math.Pow(10, -decimals)) + 1e-9;

        Assert.True(
            Math.Abs(value - measured) <= slack,
            $"{what} is published as {printed} and measurements.csv says {measured:0.0000}. "
            + "docs/STRATEGY.md is generated from that file and never transcribed (§11).");
    }

    private static IReadOnlyList<string> Table(string header)
    {
        var lines = Documentation.Text("docs/STRATEGY.md").Split('\n');
        var start = Array.FindIndex(lines, line => line.StartsWith(header, StringComparison.Ordinal));

        Assert.True(start >= 0, $"docs/STRATEGY.md no longer has a table beginning `{header}`.");

        var rows = new List<string> { lines[start] };

        // The row after the header is the alignment row, and the table ends at the first line
        // that is not a row.
        for (var index = start + 2; index < lines.Length && lines[index].TrimStart().StartsWith('|'); index++)
        {
            rows.Add(lines[index]);
        }

        Assert.True(rows.Count > 1, $"the table beginning `{header}` has no rows in it.");

        return rows;
    }

    private static string[] Cells(string row) =>
        row.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();

    private static IReadOnlyList<string> Backticked(string text) =>
        [.. Regex.Matches(text, "`([^`]+)`").Select(match => match.Groups[1].Value)];

    private static IReadOnlyDictionary<string, (double, double, string)> Csv()
    {
        var rows = new Dictionary<string, (double, double, string)>(StringComparer.Ordinal);

        foreach (var line in File.ReadAllLines(
            Path.Combine(Documentation.Root.FullName, "docs", "strategy", "measurements.csv")).Skip(1))
        {
            // Only the columns before the quoted question and command, which is every column
            // this file reads.
            var cells = line.Split(',');

            // ⚠️ Not every row carries an interval — a rank is a mean over a field and has
            // none — so an absent one is read as absent rather than as a zero.
            rows[cells[0]] = (Figure(cells[4]), Figure(cells[6]), cells[7]);
        }

        return rows;
    }

    private static double Figure(string cell) =>
        cell.Length == 0 ? double.NaN : double.Parse(cell, CultureInfo.InvariantCulture);
}
