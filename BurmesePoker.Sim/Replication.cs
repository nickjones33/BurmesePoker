using System.Globalization;

namespace BurmesePoker.Sim;

/// <summary>
/// One comparison: a margin measured at the published seed, and the same margin at a second one.
/// </summary>
/// <param name="Id">The row's key, matching the <c>ladder.head-to-head.*</c> / <c>difficulty.step.*</c> ids.</param>
/// <param name="Subject">Who was compared.</param>
/// <param name="A">The margin at the published master seed.</param>
/// <param name="VerdictA">Its Holm reading — separated, raw only, or inside the interval.</param>
/// <param name="B">The same margin at the replication seed.</param>
/// <param name="VerdictB">Its Holm reading.</param>
/// <remarks>
/// ⚠️ <b>Determinism is not replication.</b> Every published figure descends from one master seed
/// (review F5), so two runs of it agree to the last bit and say nothing about whether a second
/// draw of the world would find the same thing. This row is the first statement in the project
/// that is statistics rather than arithmetic: <see cref="MarginInside"/> asks whether the second
/// seed's estimate lands inside the first's interval, and <see cref="VerdictHolds"/> whether a
/// separation survived being drawn again.
/// </remarks>
public sealed record ReplicationRow(
    string Id,
    string Subject,
    Measurement A,
    string VerdictA,
    Measurement B,
    string VerdictB)
{
    /// <summary>Whether the replication estimate lands inside the published interval.</summary>
    public bool MarginInside => Math.Abs(B.Mean - A.Mean) <= A.Interval;

    /// <summary>
    /// Whether a separation held: a margin that survived Holm at the published seed still does at
    /// the replication seed. A margin that did not survive makes no claim to hold, so it passes.
    /// </summary>
    public bool VerdictHolds => VerdictA != "separated (Holm)" || VerdictB == "separated (Holm)";

    /// <summary>A one-word reading of the pair for the CSV verdict column.</summary>
    public string Reading =>
        !VerdictHolds ? "VERDICT FELL"
        : !MarginInside ? "outside the interval"
        : "reproduces";
}

/// <summary>What the fresh-seed replication found.</summary>
/// <param name="SeedA">The published master seed.</param>
/// <param name="SeedB">The replication seed.</param>
/// <param name="Seats">The table it was run at.</param>
/// <param name="GamesPerCell">Games a cell, both seeds.</param>
/// <param name="Rows">Every head-to-head margin and dial step, at both seeds.</param>
/// <param name="Elapsed">Wall clock.</param>
public sealed record ReplicationReport(
    int SeedA,
    int SeedB,
    int Seats,
    int GamesPerCell,
    IReadOnlyList<ReplicationRow> Rows,
    TimeSpan Elapsed)
{
    /// <summary>Whether every separation survived being drawn again — the packet's prediction.</summary>
    public bool EveryVerdictHolds => Rows.All(row => row.VerdictHolds);

    /// <summary>Whether every replication estimate landed inside its published interval.</summary>
    public bool EveryMarginInside => Rows.All(row => row.MarginInside);

    /// <summary>One row by id, or a complaint.</summary>
    public ReplicationRow Row(string id) =>
        Rows.FirstOrDefault(row => row.Id == id)
        ?? throw new ArgumentException($"No replication row '{id}'.", nameof(id));
}

/// <summary>
/// The fresh-seed replication: §3's head-to-head matrix and the dial's adjacent steps, run at a
/// second master seed and set beside the published one — 🔥 <b>P48's reproducibility statement</b>
/// (review F5).
/// </summary>
/// <remarks>
/// <para>
/// <b>Both seeds are run here, not read from the file.</b> The comparison is only apples to apples
/// if the two margins were computed by the same code the same way, so this re-derives the
/// published seed's matrix rather than parsing <c>measurements.csv</c> — a byte-for-byte match of
/// the published seed's rows is a free check that nothing drifted, and the replication seed's rows
/// are the new information.
/// </para>
/// <para>
/// <b>The load-bearing case is <c>sprinter</c> over <c>outs</c></b> (BUILD-PLAN P46): a
/// <c>+1.2 ± 0.8</c> that survives Holm but is a dead heat at the crossed table, the tightest
/// separation in the document. If any verdict is going to fall it is this one, so it is named in
/// the report rather than left to be found.
/// </para>
/// </remarks>
public static class Replication
{
    /// <summary>Where the replication writes unless told otherwise.</summary>
    public const string DefaultPath = "docs/strategy/replication.csv";

    /// <summary>The seed the replication draws against, distinct from the published <c>20260819</c>.</summary>
    public const int DefaultReplicationSeed = 20260826;

    /// <summary>
    /// The efficient replication: <b>read the published seed's matrix from the file the suite just
    /// wrote, and run only the second seed</b> (BUILD-PLAN P48, Nick's call).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The published margins were themselves produced by <see cref="Tournament.Run"/> at the same
    /// games, so reading them and computing the second seed the same way is still apples to apples
    /// — it saves recomputing a matrix the suite already spent hours on. The one thing
    /// <see cref="Run"/> gives that this does not is a byte-identical re-derivation of the
    /// published seed as a free integrity check; that is what <c>--recompute-a</c> is for.
    /// </para>
    /// <para>
    /// ⚠️ <b>The published file must be current.</b> A seed-A margin read from a stale
    /// <c>measurements.csv</c> would be compared against a fresh seed-B one, and the comparison
    /// would be measuring the file's age rather than the world's noise — so this is run after the
    /// suite, against the file it just wrote.
    /// </para>
    /// </remarks>
    public static ReplicationReport AgainstPublished(
        string publishedPath,
        IReadOnlyList<Strategy> ladder,
        IReadOnlyList<Strategy> dial,
        int seats,
        int gamesPerCell,
        int seedA = 20260819,
        int seedB = DefaultReplicationSeed,
        bool parallel = true)
    {
        ArgumentNullException.ThrowIfNull(publishedPath);
        ArgumentNullException.ThrowIfNull(ladder);
        ArgumentNullException.ThrowIfNull(dial);

        var published = ReadPublished(publishedPath);

        // 🔥 Pre-flight the ids against the file BEFORE the multi-hour seed-B runs (P48): a dial
        // listed in the wrong order or a rung missing from the published set is a config error, and
        // it must fail in seconds rather than after the ladder has been recomputed. The expected
        // ids are a pure function of the input lists, so they can be checked without running.
        var expected = new List<string>();

        for (var row = 0; row < ladder.Count; row++)
        {
            for (var column = row + 1; column < ladder.Count; column++)
            {
                expected.Add($"ladder.head-to-head.{ladder[row].Name}-over-{ladder[column].Name}");
            }
        }

        for (var index = 0; index + 1 < dial.Count; index++)
        {
            expected.Add($"difficulty.step.{dial[index + 1].Name}-over-{dial[index].Name}");
        }

        var missing = expected.Where(id => !published.ContainsKey(id)).ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"{publishedPath} is missing {missing.Count} id(s) the replication needs, e.g. "
                + $"'{missing[0]}'. Regenerate the suite, or list the ladder/dial in its order.");
        }

        var clock = System.Diagnostics.Stopwatch.StartNew();

        var ladderB = Ladder(ladder, seats, gamesPerCell, seedB, parallel);
        var dialB = Dial(dial, seats, gamesPerCell, seedB, parallel);

        var rows = new List<ReplicationRow>();

        for (var index = 0; index < ladderB.Pairs.Count; index++)
        {
            var cell = ladderB.Pairs[index];
            var id = $"ladder.head-to-head.{cell.Row}-over-{cell.Column}";

            if (!published.TryGetValue(id, out var a))
            {
                throw new InvalidOperationException(
                    $"{publishedPath} has no '{id}'. Regenerate the suite before replicating against it.");
            }

            rows.Add(new ReplicationRow(
                id, cell.Label, a.Margin, a.Verdict, cell.Margin, Reading(ladderB.Verdicts[index])));
        }

        for (var index = 0; index < dialB.Pairs.Count; index++)
        {
            var cell = dialB.Pairs[index];
            var id = $"difficulty.step.{cell.Column}-over-{cell.Row}";

            if (!published.TryGetValue(id, out var a))
            {
                throw new InvalidOperationException(
                    $"{publishedPath} has no '{id}'. Regenerate the suite before replicating against it.");
            }

            rows.Add(new ReplicationRow(
                id,
                $"{cell.Column} over {cell.Row}",
                a.Margin,
                a.Verdict,
                cell.Margin with { Mean = -cell.Margin.Mean },
                Reading(dialB.Verdicts[index])));
        }

        clock.Stop();

        return new ReplicationReport(seedA, seedB, seats, gamesPerCell, rows, clock.Elapsed);
    }

    /// <summary>
    /// The published head-to-head and dial-step margins, by id: mean and interval rebuilt into a
    /// <see cref="Measurement"/> and the verdict string read straight off.
    /// </summary>
    private static IReadOnlyDictionary<string, (Measurement Margin, string Verdict)> ReadPublished(string path)
    {
        var rows = new Dictionary<string, (Measurement, string)>(StringComparer.Ordinal);

        foreach (var line in File.ReadLines(path).Skip(1))
        {
            if (line.Length == 0)
            {
                continue;
            }

            var fields = Fields(line);
            var id = fields[0];

            if (!id.StartsWith("ladder.head-to-head.", StringComparison.Ordinal)
                && !id.StartsWith("difficulty.step.", StringComparison.Ordinal))
            {
                continue;
            }

            var games = int.Parse(fields[3], CultureInfo.InvariantCulture);
            var mean = double.Parse(fields[4], CultureInfo.InvariantCulture);
            var interval = fields[6].Length == 0 ? 0 : double.Parse(fields[6], CultureInfo.InvariantCulture);

            // The interval column is 1.959963985 × the standard error; recover the SE it was made
            // from so the rebuilt measurement carries the interval the file published.
            rows[id] = (new Measurement(games, mean, interval / 1.959963985), fields[7]);
        }

        return rows;
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

    /// <summary>Runs both matrices at both seeds and lines the margins up.</summary>
    public static ReplicationReport Run(
        IReadOnlyList<Strategy> ladder,
        IReadOnlyList<Strategy> dial,
        int seats,
        int gamesPerCell,
        int seedA = 20260819,
        int seedB = DefaultReplicationSeed,
        bool parallel = true)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        ArgumentNullException.ThrowIfNull(dial);

        var clock = System.Diagnostics.Stopwatch.StartNew();

        var ladderA = Ladder(ladder, seats, gamesPerCell, seedA, parallel);
        var ladderB = Ladder(ladder, seats, gamesPerCell, seedB, parallel);
        var dialA = Dial(dial, seats, gamesPerCell, seedA, parallel);
        var dialB = Dial(dial, seats, gamesPerCell, seedB, parallel);

        var rows = new List<ReplicationRow>();

        for (var index = 0; index < ladderA.Pairs.Count; index++)
        {
            var cell = ladderA.Pairs[index];

            rows.Add(new ReplicationRow(
                $"ladder.head-to-head.{cell.Row}-over-{cell.Column}",
                cell.Label,
                cell.Margin,
                Reading(ladderA.Verdicts[index]),
                ladderB.Pairs[index].Margin,
                Reading(ladderB.Verdicts[index])));
        }

        for (var index = 0; index < dialA.Pairs.Count; index++)
        {
            var cell = dialA.Pairs[index];
            var other = dialB.Pairs[index];

            rows.Add(new ReplicationRow(
                $"difficulty.step.{cell.Column}-over-{cell.Row}",
                $"{cell.Column} over {cell.Row}",
                cell.Margin with { Mean = -cell.Margin.Mean },
                Reading(dialA.Verdicts[index]),
                other.Margin with { Mean = -other.Margin.Mean },
                Reading(dialB.Verdicts[index])));
        }

        clock.Stop();

        return new ReplicationReport(seedA, seedB, seats, gamesPerCell, rows, clock.Elapsed);
    }

    private static TournamentReport Ladder(
        IReadOnlyList<Strategy> field, int seats, int games, int seed, bool parallel) =>
        Tournament.Run(new TournamentOptions
        {
            Strategies = field,
            Seats = seats,
            GamesPerCell = games,
            MasterSeed = seed,
            Parallel = parallel
        });

    private static TournamentReport Dial(
        IReadOnlyList<Strategy> field, int seats, int games, int seed, bool parallel) =>
        Tournament.Run(new TournamentOptions
        {
            Strategies = field,
            Pairs = PairFamily.Adjacent,
            Seats = seats,
            GamesPerCell = games,
            MasterSeed = seed,
            Parallel = parallel
        });

    private static string Reading(HolmVerdict verdict) =>
        verdict.Survives ? "separated (Holm)" : verdict.Separated ? "raw only" : "inside the interval";
}

/// <summary>The replication as a file, quotable by <c>docs/STRATEGY.md</c>.</summary>
public static class ReplicationCsv
{
    /// <summary>Every comparison, one to a row.</summary>
    public static IEnumerable<string> Rows(ReplicationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        yield return
            "id,subject,mean_a,interval_a,verdict_a,mean_b,interval_b,verdict_b,margin_inside,verdict";

        foreach (var row in report.Rows)
        {
            yield return string.Create(CultureInfo.InvariantCulture,
                $"{Quote(row.Id)},{Quote(row.Subject)},"
                + $"{Number(row.A.Mean)},{Number(row.A.Interval)},{Quote(row.VerdictA)},"
                + $"{Number(row.B.Mean)},{Number(row.B.Interval)},{Quote(row.VerdictB)},"
                + $"{(row.MarginInside ? "yes" : "no")},{Quote(row.Reading)}");
        }
    }

    /// <summary>Writes them, making the directory if it is not there.</summary>
    public static void WriteTo(string path, ReplicationReport report)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, Rows(report));
    }

    private static string Quote(string text) =>
        text.Contains(',', StringComparison.Ordinal) || text.Contains('"', StringComparison.Ordinal)
            ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : text;

    private static string Number(double value) =>
        double.IsFinite(value)
            ? value.ToString("0.000000", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}
