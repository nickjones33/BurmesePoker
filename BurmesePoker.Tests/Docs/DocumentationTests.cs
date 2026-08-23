using System.Text.RegularExpressions;

using BurmesePoker.Tests.Web;

namespace BurmesePoker.Tests.Docs;

/// <summary>
/// ✅ <b>P34 acceptance 3 — the anti-staleness habit as tests, in this project's own idiom.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This project has twice turned a habit into a default and then into an assertion</b> —
/// <em>a rung cannot be added without being measured</em> (P18 → P20 → P23) and <em>a Settled
/// rule cannot be recorded without being checked</em> (P30.2). <b>The documentation set is the
/// third.</b> A document nobody lists is a document nobody maintains; a command that no longer
/// runs is stale in the most expensive way, because a visitor types it; and a fence that names
/// a test which has been renamed away is <em>worse</em> than no fence, because the document
/// goes on promising it.
/// </para>
/// <para>
/// ⚠️ <b>What none of this may do is flatten the narrative.</b> These files keep superseded
/// reasoning on purpose. Every check here is a join between a document and something that can
/// be re-derived — never a rule about how a document should read.
/// </para>
/// </remarks>
public class DocumentationTests
{
    /// <summary>
    /// ✅ <b>The documentation map is complete both ways.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>An orphan document is one nobody maintains</b>, and this repository had two when
    /// the check was written — a closed code review and the dial's off-table evidence, neither
    /// of them listed anywhere. The other direction matters as much: a map entry for a file
    /// that has been renamed or deleted sends a reader somewhere that does not exist.
    /// <c>CLAUDE.md</c> is excluded because it is the file the map lives in.
    /// </remarks>
    [Fact]
    public void EveryDocumentIsInTheMapAndEveryEntryInTheMapExists()
    {
        var listed = MappedPaths();

        Assert.NotEmpty(listed);

        var missing = Documentation.Mapped.Where(path => !listed.Contains(path)).ToList();

        Assert.True(
            missing.Count == 0,
            "Not in CLAUDE.md's documentation map: " + string.Join(", ", missing)
            + ". A document nobody lists is a document nobody maintains — add a row, or delete the file.");

        var phantom = listed.Where(path => !File.Exists(Path.Combine(Documentation.Root.FullName, path))).ToList();

        Assert.True(
            phantom.Count == 0,
            "The documentation map names file(s) that do not exist: " + string.Join(", ", phantom));
    }

    /// <summary>
    /// ✅ <b>P34 acceptance 2 — every historical document says so in its first three lines.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>The reader cannot tell current fact from historical record without knowing the
    /// packet history</b>, and three of these documents are wholly historical: a superseded
    /// plan, a specification of code that was deleted, and a closed review. Each carries a
    /// banner above the fold saying what it was for and what replaced it.
    /// </para>
    /// <para>
    /// 🔥 <b>Asserted both ways.</b> The three must carry the banner — a banner quietly deleted
    /// is the failure this exists for — and no other document may, because a current document
    /// flagged historical is the same lie the other way round.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryHistoricalDocumentSaysSoAboveTheFoldAndNoCurrentOneDoes()
    {
        string[] historical =
        [
            "docs/RECONCILIATION-PLAN.md",
            "docs/RULES-TECHNICAL.md",
            "docs/REVIEW-2026-08.md"
        ];

        foreach (var document in Documentation.All)
        {
            var opening = string.Join(
                '\n',
                Documentation.Text(document)
                    .Split('\n')
                    .Where(line => line.Trim().Length > 0)
                    .Take(3));

            var flagged = opening.Contains("HISTORICAL", StringComparison.Ordinal)
                || opening.Contains("SUPERSEDED", StringComparison.Ordinal)
                || opening.Contains("Superseded", StringComparison.Ordinal);

            Assert.True(
                flagged == historical.Contains(document),
                flagged
                    ? $"{document} is flagged historical above the fold and is not in this test's list. "
                        + "Either it really is historical — add it — or the banner is wrong."
                    : $"{document} is historical and its first three lines do not say so. A reader cannot "
                        + "tell a record from a fact without the packet history in their head (BUILD-PLAN P34).");
        }
    }

    /// <summary>
    /// ✅ <b>Every command the documentation prints resolves against the tree it is printed in.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The case this is written from actually happened</b>: <c>--seat</c> became
    /// <c>--people</c> when the browser grew a lobby, and prose elsewhere went on saying
    /// <c>--seat</c> — a command a reader would type and watch do nothing, since the
    /// command-line configuration provider records an unknown switch rather than refusing it.
    /// <b>The negative control below asserts exactly that name still does not resolve</b>, so
    /// this test cannot pass by accepting everything.
    /// </para>
    /// <para>
    /// ⚠️ <b>A project's options are read off the source that parses them</b>, one recogniser
    /// per front end, because there is no shared parser: the harness has its own
    /// <c>Arguments</c>, the console switches on the flag, and the browser reads
    /// <c>IConfiguration</c>. A copy of the option list in this file would be a second thing to
    /// keep true, which is the defect the whole packet is about.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryCommandTheDocumentationPrintsResolves()
    {
        Assert.NotEmpty(Documentation.Commands);

        var checkedOptions = 0;

        foreach (var (document, command) in Documentation.Commands)
        {
            var tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var where = $"{document}: `{command}`";

            switch (tokens[0])
            {
                case "cmp":
                    continue;

                case "python3":
                    checkedOptions += CheckScript(tokens, where);
                    continue;

                case "dotnet":
                    checkedOptions += CheckDotnet(tokens, where);
                    continue;

                default:
                    Assert.Fail($"{where}: nothing in this test knows what `{tokens[0]}` is.");
                    break;
            }
        }

        // The scan is alive: it really did resolve options against project sources rather than
        // waving every command through as an unrecognised shape.
        Assert.True(checkedOptions > 30, $"only {checkedOptions} options were resolved, which is too few to be the set.");

        // 🔥 The negative control. `--seat` is the flag that drifted; if this resolves, the
        // recogniser is accepting anything and every assertion above is vacuous.
        Assert.DoesNotContain("seat", Options("BurmesePoker.Web"));
        Assert.Contains("people", Options("BurmesePoker.Web"));
    }

    /// <summary>
    /// ✅ <b>P34's fifth check, added by P35: every test <c>RULES.md</c> names as a fence exists.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Five packets have now shipped rules built on open questions</b>, and the discipline
    /// each time was the same: proceed on the recorded default, and fence it with a test named
    /// for the question, so that the day an expert answers, <b>the failing test is the change
    /// list</b>. ⚠️ <b>A fence that has been renamed or deleted is worse than no fence</b>,
    /// because §9 goes on promising it and nobody finds out until the answer arrives.
    /// </para>
    /// <para>
    /// ⚠️ <b>The parse is deliberately wider than <c>#9</c>'s <em>"built on this default"</em>
    /// rows</b> — any test <c>RULES.md</c> names at all has to exist, wherever it names it,
    /// because a rule document citing a test that has gone is the same defect whichever section
    /// it is in.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryTestTheRulesDocumentNamesAsAFenceExists()
    {
        var named = Regex
            .Matches(Documentation.Text("docs/RULES.md"), @"`([A-Za-z]+Tests)\.([A-Za-z]+)`")
            .Select(match => (Class: match.Groups[1].Value, Method: match.Groups[2].Value))
            .Distinct()
            .ToList();

        Assert.NotEmpty(named);

        var tests = typeof(DocumentationTests).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods().Select(method => $"{type.Name}.{method.Name}"))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (owner, method) in named)
        {
            Assert.True(
                tests.Contains($"{owner}.{method}"),
                $"RULES.md names `{owner}.{method}` and no such test exists. A fence that has been "
                + "renamed away is worse than no fence: the document goes on promising it (BUILD-PLAN P34).");
        }

        // ⚠️ And every §9 row that says a default was built on has to name one at all. A row
        // that proceeds on a default and fences nothing is the state this check exists to stop.
        foreach (var row in Documentation.Text("docs/RULES.md")
            .Split('\n')
            .Where(line => line.Contains("Built on this default", StringComparison.Ordinal)))
        {
            Assert.True(
                Regex.IsMatch(row, @"[Ff]enced by \*{0,2}`[A-Za-z]+Tests\.[A-Za-z]+`"),
                "A §9 row records a default that was built on and names no test to fence it: "
                + row[..Math.Min(160, row.Length)]);
        }
    }

    private static int CheckDotnet(string[] tokens, string where)
    {
        switch (tokens.ElementAtOrDefault(1))
        {
            case "build":
                return 0;

            case "test":
                foreach (var option in tokens.Skip(2).Where(token => token.StartsWith("--", StringComparison.Ordinal)))
                {
                    // ⚠️ MTP, not VSTest: `--filter "FullyQualifiedName~…"` is rejected outright.
                    Assert.True(
                        option is "--filter-method" or "--filter-class",
                        $"{where}: `dotnet test` here runs on Microsoft.Testing.Platform, which takes "
                        + $"--filter-method and --filter-class and not `{option}`.");
                }

                return 0;

            case "run":
                break;

            default:
                Assert.Fail($"{where}: nothing in this test knows what `dotnet {tokens.ElementAtOrDefault(1)}` is.");
                break;
        }

        var separator = Array.IndexOf(tokens, "--");
        var project = tokens[Array.IndexOf(tokens, "--project") + 1];

        Assert.True(
            Directory.Exists(Path.Combine(Documentation.Root.FullName, project))
                && Directory.GetFiles(Path.Combine(Documentation.Root.FullName, project), "*.csproj").Length == 1,
            $"{where}: there is no project called {project}.");

        if (separator < 0)
        {
            return 0;
        }

        var arguments = tokens[(separator + 1)..];
        var recognised = Options(project);
        var found = 0;

        if (arguments.Length > 0 && !arguments[0].StartsWith("--", StringComparison.Ordinal))
        {
            Assert.Contains($"args[0] == \"{arguments[0]}\"", Program(project), StringComparison.Ordinal);
            found++;
        }

        foreach (var option in arguments
            .Where(token => token.StartsWith("--", StringComparison.Ordinal))
            .Select(token => token[2..]))
        {
            Assert.True(
                recognised.Contains(option),
                $"{where}: {project} does not read `--{option}`. Nothing rejects an unknown switch at "
                + "run time, so a drifted flag is a command that silently does something else.");

            found++;
        }

        return found;
    }

    private static int CheckScript(string[] tokens, string where)
    {
        var script = tokens[1];

        Assert.True(
            File.Exists(Path.Combine(Documentation.Root.FullName, script)),
            $"{where}: there is no script at {script}.");

        var recognised = Regex
            .Matches(Documentation.Text(script), @"add_argument\(""--([a-z-]+)""")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        var found = 0;

        foreach (var option in tokens.Skip(2)
            .Where(token => token.StartsWith("--", StringComparison.Ordinal))
            .Select(token => token[2..]))
        {
            Assert.True(recognised.Contains(option), $"{where}: {script} takes no `--{option}`.");
            found++;
        }

        return found;
    }

    /// <summary>
    /// The switches a project actually reads, taken off the source that parses them.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>One recogniser per front end and no shared list</b>: the harness parses its own
    /// <c>Arguments</c>, the console switches on the flag text, and the browser reads
    /// <c>IConfiguration</c> by key. A list copied into this file would drift exactly the way
    /// the prose did.
    /// </remarks>
    private static IReadOnlySet<string> Options(string project)
    {
        var source = string.Join(
            '\n',
            Sources.Production.Where(file => file.Path.StartsWith(project, StringComparison.Ordinal))
                .Select(file => file.Text));

        var patterns = new[]
        {
            @"(?:Value|Number|Flag|Has)\(""([a-z-]+)""",   // the harness
            @"case ""--([a-z-]+)""",                        // the console
            @"GetValue<[^>]*>\(""([a-z-]+)""\)"             // the browser
        };

        return patterns
            .SelectMany(pattern => Regex.Matches(source, pattern).Select(match => match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string Program(string project) =>
        Sources.Production
            .Single(file => file.Path == Path.Combine(project, "Program.cs"))
            .Text;

    /// <summary>The first column of <c>CLAUDE.md</c>'s documentation map.</summary>
    private static IReadOnlySet<string> MappedPaths()
    {
        var map = Documentation.Text("CLAUDE.md");
        var start = map.IndexOf("## Documentation map", StringComparison.Ordinal);

        Assert.True(start >= 0, "CLAUDE.md has no documentation map any more.");

        return Regex
            .Matches(map[start..], @"^\|\s*`([^`]+)`\s*\|", RegexOptions.Multiline)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }
}
