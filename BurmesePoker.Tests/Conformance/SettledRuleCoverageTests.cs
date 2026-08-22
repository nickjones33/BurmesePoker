using System.Text.RegularExpressions;
using BurmesePoker.Tests.Web;

namespace BurmesePoker.Tests.Conformance;

/// <summary>
/// ✅ <b>P30.2 acceptance 2, in the P23 idiom: every section of <c>RULES.md</c> that records a
/// Settled rule is either named by a conformance check or carries a written exemption — and a
/// rule added to the document and to nothing else fails the build.</b>
/// </summary>
/// <remarks>
/// <para>
/// The Settled sections are parsed out of the document itself, the way
/// <c>StandingAnswerTests</c> parses <c>measurements.csv</c>: a section counts as Settled when
/// a rule table in it closes a row with <c>Settled</c>, when its own heading says so, or when
/// its prose declares a provenance tag Settled. The registry below then has to account for
/// every one of them.
/// </para>
/// <para>
/// ⚠️ <b>The registry is a claim ledger, not a test runner</b> — each entry names where the
/// check lives, so a reader can re-check the claim, and the exemptions are short enough to
/// read and each says why. Without this test, "every single rule is checked" is a sentence in
/// a status file; with it, it is a build.
/// </para>
/// </remarks>
public class SettledRuleCoverageTests
{
    /// <summary>
    /// Every Settled section, accounted for: <c>Checked</c> names the checks; <c>Exempt</c>
    /// says why no ordinary-play check can exist.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> Registry = new Dictionary<string, string>
    {
        ["2"] = Checked(
            "108 cards wherever they sit, at every event: RuleConformance.EverythingHolds; "
            + "13 dealt a seat: RuleConformance.Watch; table sizes 4–6 played: "
            + "RuleConformanceTests.OrdinaryRoundsBreakNoSettledRule. ⚠️ The two-player floor "
            + "is Settled and unreachable — RoundEngine.MinimumPlayers is 4, the recorded "
            + "divergence (§10 #7), so 2–3-handed conformance waits on the packet that discharges it."),
        ["3"] = Checked(
            "Deal order and the two turn-ups: RuleConformance.RoundStarted/Watch (ownership at "
            + "the deal, turned-up cards owned by nobody); seats re-drawn every round: "
            + "RuleConformanceTests.TheSeatingIsRedrawnEveryRoundAfterTheFirst."),
        ["4.1"] = Checked(
            "Multipliers re-derived independently — permanent values, ×3 on a designated "
            + "permanent value, ×5 only on the 7♦/A♠ pair: RuleConformance.TheSettlementIsTheRules "
            + "over every settled round; the §9 #32 fence: MoneyCardRegistryTests."),
        ["4.2"] = Checked(
            "Designation by exact value (rank, suit, colour): the same re-derivation matches "
            + "turn-ups by SameValueAs and the audited settlement agrees with the engine's."),
        ["4.3"] = Checked(
            "Per-card, per-opponent, owner collects, winner participates: "
            + "RuleConformance.TheSettlementIsTheRules; the mutant that overpays is caught: "
            + "RuleConformanceTests.MutantADeadwoodPenaltyIsCaught."),
        ["4.4"] = Checked(
            "Ownership conferred only by the deal or a blind draw, write-once, never "
            + "transferred: RuleConformance.EverythingHolds on every event; a take or claim "
            + "conferring none: PlayerTookDiscard/MoneyCardClaimed allow no new record."),
        ["4.5"] = Checked(
            "Claim only on turn 1, only the opener, only once, physical card leaves the table; "
            + "objection only from the upstream seat and only from a holder: "
            + "RuleConformance.MoneyCardClaimed/ClaimRefused; the edges: ClaimPermissionTests."),
        ["5"] = Checked(
            "One taken, one discarded, every turn, in seating order; 13 between turns and 14 "
            + "during one; reshuffle gathers every pile and leaves the turn-ups: "
            + "RuleConformance.BeginTake/AfterTake/PlayerDiscarded/DiscardsReshuffled."),
        ["5.1"] = Checked(
            "The ban mirrored from public events alone: a discard is never a closed rank unless "
            + "the floor applied or the round ended on it (exception 2 bound to the declaration "
            + "— review R1): RuleConformance.PlayerDiscarded/ResolvePendingClosedDiscard; the "
            + "§7.1.1-sensitive edge: FeedingBanTests.TheDeclaringDiscardExceptionAsksTheTablesOwnWinCondition."),
        ["6.1"] = Checked(
            "Runs one suit, consecutive, ace never wrapping: RuleConformance.AValidRun on every "
            + "declared hand; the wrap mutant: MutantAnIllegalDeclarationIsCaughtEachWayItCanBeIllegal."),
        ["6.2"] = Checked(
            "Sets one rank, no suit twice, so four at most: RuleConformance.AValidSet; the "
            + "9♥ 9♥ 9♠ mutant likewise."),
        ["6.3"] = Checked(
            "The declared melds partition exactly the thirteen held, disjoint by CardId: "
            + "RuleConformance.ADeclaredHandSatisfiesTheTable. Concealment until the "
            + "declaration is the server's boundary and is audited by ConcealmentTests and "
            + "BrowserRoundTests, not here — the engine has no event that could lay a meld down early."),
        ["7.1"] = Checked(
            "All thirteen melded; the discard first and the reveal after it; the mandatory "
            + "discard with no exception: RuleConformance.PlayerDeclared (declarer is the seat "
            + "that just discarded, holding thirteen)."),
        ["7.1.1"] = Checked(
            "The series and clean-series counts the table size requires, at 4, 5 and 6 seats: "
            + "RuleConformance.ADeclaredHandSatisfiesTheTable against TableRules.For; the "
            + "four-versus-five mutant; the table itself: HandEvaluatorTests. ⚠️ 2–3-handed is "
            + "correct and unreachable (§10 #7), as §2's entry records."),
        ["7.2"] = Checked(
            "Flat round value from each loser, money settled separately by owner, no deadwood "
            + "penalty, zero sum: RuleConformance.TheSettlementIsTheRules and its deadwood "
            + "mutant. 'Nothing ends the session' is a property of MatchEngine having no end "
            + "condition — asserted by its own tests, not observable in one round."),
    };

    // §9 (the question ledger) and §10 (the divergence ledger) are deliberately absent: they
    // record questions and rulings about the code, not rules of the game, and the parser below
    // correctly finds no Settled rule row in either. Every settled answer they mention is
    // promoted into a rule section above, where the check lives.

    [Fact]
    public void EverySettledRuleIsCheckedOrNamesWhyItCannotBe()
    {
        var settled = SettledSections();

        Assert.NotEmpty(settled);

        // The parse is alive: the sections the audit was actually built for must be among the
        // flagged ones, or the document's format has drifted out from under the regexes.
        Assert.Contains("5.1", settled);
        Assert.Contains("7.1.1", settled);
        Assert.Contains("4.4", settled);

        var unaccounted = settled.Where(section => !Registry.ContainsKey(section)).ToList();

        Assert.True(
            unaccounted.Count == 0,
            "RULES.md records Settled rules in section(s) "
            + string.Join(", ", unaccounted.Select(section => $"§{section}"))
            + " that no conformance registry entry checks or exempts. Add the check — or the "
            + "written exemption — to SettledRuleCoverageTests.Registry.");

        // …and the registry cannot quietly outlive the document: an entry for a section that
        // no longer records anything Settled is stale the other way.
        var stale = Registry.Keys.Where(section => !settled.Contains(section)).ToList();

        Assert.True(
            stale.Count == 0,
            "The conformance registry names section(s) "
            + string.Join(", ", stale.Select(section => $"§{section}"))
            + " that RULES.md no longer records as Settled. Remove or renumber the entries.");
    }

    /// <summary>The exemption list stays short enough to read (P30.2 acceptance 2).</summary>
    /// <remarks>
    /// Today nothing is wholly exempt; what exists is <b>partial</b> exemptions, marked ⚠️
    /// inline — rules Settled for table sizes the engine cannot yet deal (2–3-handed, §10 #7).
    /// Each must say why, and there must stay few enough of them to read in one sitting.
    /// </remarks>
    [Fact]
    public void TheExemptionListIsShortAndEveryEntrySaysWhy()
    {
        var whole = Registry.Where(entry => entry.Value.StartsWith("EXEMPT", StringComparison.Ordinal));
        var partial = Registry.Where(entry =>
            !entry.Value.StartsWith("EXEMPT", StringComparison.Ordinal) && entry.Value.Contains('⚠'));
        var exemptions = whole.Concat(partial).ToList();

        Assert.InRange(exemptions.Count, 1, 5);
        Assert.All(exemptions, entry => Assert.True(
            entry.Value.Length > 60, $"§{entry.Key}'s exemption says nothing a reader can re-check."));
    }

    /// <summary>
    /// The sections of <c>RULES.md</c> that record at least one Settled rule, by number.
    /// </summary>
    private static IReadOnlySet<string> SettledSections()
    {
        var lines = File.ReadAllLines(Path.Combine(Sources.Root.FullName, "docs", "RULES.md"));
        var settled = new HashSet<string>();
        var section = "";
        var inTheLegend = false;

        foreach (var line in lines)
        {
            var heading = Regex.Match(line, @"^#{2,4}\s+(\d+(?:\.\d+)*)");

            if (heading.Success)
            {
                section = heading.Groups[1].Value;
                inTheLegend = false;
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                // An unnumbered top-level section — the preamble's legend explains the word
                // Settled and must not count as using it.
                section = "";
                inTheLegend = true;
            }

            if (section.Length == 0 || inTheLegend)
            {
                continue;
            }

            // A rule table's confidence column: the last cell of a row, starting with Settled.
            if (line.TrimStart().StartsWith('|'))
            {
                var cells = line.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (cells.Length > 1 && cells[^1].StartsWith("Settled", StringComparison.Ordinal))
                {
                    settled.Add(section);
                }

                continue;
            }

            // A heading or prose that declares its provenance Settled: a tag in backticks,
            // then the word — the shape "`EXPERT`, Settled" takes everywhere it appears.
            if (Regex.IsMatch(line, @"`[A-Z]+`(\s+`[A-Z]+`)*\s*,?\s+Settled\b"))
            {
                settled.Add(section);
            }
        }

        return settled;
    }

    private static string Checked(string where) => "CHECKED — " + where;

    private static string Exempt(string why) => "EXEMPT — " + why;
}
