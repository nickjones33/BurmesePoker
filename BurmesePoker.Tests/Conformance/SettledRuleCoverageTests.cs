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
            + "the deal, turned-up cards owned by nobody). ✅ STEP 2 AS REV 28 CORRECTS IT — a "
            + "seating is drawn once and HELD: RuleConformanceTests.TheSeatingIsDrawnOnceAndHeld"
            + "UnlessThePolicySaysOtherwise at 4, 5 and 6 seats, with the same match asked to "
            + "re-seat so the check is about the rule rather than about a missing feature; the "
            + "policy itself: SeatingPolicyTests; the engine: MatchEngineTests.TheSeatsAreDrawn"
            + "OnceAndHeld. ✅ AND ITS OTHER HALF — re-drawn when the players AGREE (§9 #45): "
            + "RuleConformanceTests.TheSeatingChangesWhenTheTableAgreesAndNotOtherwise at 4, 5 "
            + "and 6 seats, so both readings are checked rather than only the one the code "
            + "happens to implement; the agreement itself: SeatingAgreementTests. 🔥 This entry "
            + "read 'NOT CHECKED' between rev 28 and P36 — the conformance test asserted the "
            + "every-round reading P28 built and the document had withdrawn (§10 #22), and it "
            + "was left standing rather than deleted so the seating was not unasserted "
            + "altogether. P36 inverted it and P37 discharged §10 #23. ⚠️ §9 #47 (everybody or "
            + "most?) is still open and proceeding on its recorded default — unanimous among "
            + "the people — fenced by SeatingAgreementTests.AgreementIsUnanimousUntilTheExpert"
            + "SaysOtherwise. ✅ No published measurement is affected: every experiment runs one "
            + "round a game, so no seat is ever asked at all."),
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
            "Round value from each loser (flat in what they held; see §7.3 for the multiplier "
            + "on how they won), money settled separately by owner, no deadwood penalty, zero "
            + "sum: RuleConformance.TheSettlementIsTheRules and its deadwood mutant. 'Nothing "
            + "ends the session' is a property of MatchEngine having no end condition — "
            + "asserted by its own tests, not observable in one round."),
        ["7.3"] = Checked(
            "The clean bonus, re-derived from the seat count and the cards laid down: "
            + "RuleConformance.TheSettlementIsTheRules over every settled round — a jokerless "
            + "declaration pays ×2 at 2–4 seats and ×3 at 5+, and the mutants that pay a clean "
            + "hand flat, pay a jokered one the bonus, or use the wrong side of the 4/5 seam "
            + "are all caught: RuleConformanceTests.MutantTheCleanBonusIsCaughtBothWaysRound"
            + "AndBothWaysWrong. The rule itself: SettlementTests (the multiplier by table "
            + "size, the joker-in-a-set case, four sets and no series at five seats); the "
            + "table: TableRulesTests. ⚠️ Two details are still open (§9 #36 the money "
            + "settlement, #37 six-plus seats), both proceeding on their recorded defaults and "
            + "both fenced by a test that says so. 🔥 This entry was an Exempt(...) between "
            + "rev 26 and P33 — the only exemption this registry has carried because the code "
            + "was missing rather than because no ordinary-play check could exist. P33 built "
            + "the rule and converted it, which is the alarm being answered rather than "
            + "silenced."),
        ["7.4"] = Checked(
            "The deal bonus, re-derived from the shape of the round: a declaration that follows "
            + "no discard is a win from the initial deal and pays ×2 on top of §7.3, and the "
            + "mutants that pay it flat, pay it to a winner who played a turn, or replace §7.3's "
            + "multiplier instead of multiplying with it are all caught: "
            + "RuleConformanceTests.MutantTheDealBonusIsCaughtBothWaysRound; the engine path "
            + "itself: RoundEngineTests.AThirteenThatAlreadyWinsIsLaidDownBeforeAnybodyDrawsAnd"
            + "PaysDouble; the arithmetic: SettlementTests.AWinFromTheInitialDealPaysDouble. "
            + "🔥 This entry was an Exempt(...) between rev 27 and P35, because the engine had "
            + "no path that could offer a declaration before the first turn — a round began by "
            + "asking seat 0 to take a card. P35 built the path and converted it. ⚠️ Three "
            + "details are open and all three proceed on their recorded defaults, each fenced by "
            + "a test named for the question: §9 #38 (the dealt thirteen alone, not the winner's "
            + "first turn) — RoundEngineTests.TheDealBonusIsTheDealtThirteenAloneUntilTheExpert"
            + "SaysOtherwise; §9 #39 (the two bonuses multiply) — SettlementTests.TheTwoBonuses"
            + "MultiplyUntilTheExpertSaysOtherwise; §9 #40 (round payment only, never the money "
            + "cards) — SettlementTests.TheDealBonusDoesNotReachTheMoneyCards. ⚠️ And §9 #48, "
            + "opened by P35: two seats dealt a winning thirteen at once, where the earlier in "
            + "turn order takes it — RoundEngineTests.WhenTwoSeatsAreDealtAWinningThirteenThe"
            + "EarlierInTurnOrderTakesIt."),
        ["7.5"] = Checked(
            "The feeding blame, re-derived over a SEQUENCE of rounds: "
            + "RuleConformanceTests.AStreakOfWinsBreaksNoSettledRuleAndIsBilledToTheSeatAbove at "
            + "4 and 5 seats keeps its own count of consecutive wins, hands it to a fresh audit "
            + "each round, and asserts that a run really occurred — so a lucky run of 120 rounds "
            + "containing no streak fails rather than passing vacuously. RuleConformance then "
            + "re-derives the blamed seat from THAT round's seating and checks the engine's own "
            + "account of the win agrees. The mutants that spread the payment over the table, "
            + "bill the seat below instead of the seat above, or bill one loser's share instead "
            + "of the winner's whole payment are caught: "
            + "RuleConformanceTests.MutantTheFeedingBlameIsCaughtWhoeverIsBilled. The counting: "
            + "MatchEngineTests.AThirdConsecutiveWinIsPaidEntirelyByTheSeatAboveTheWinner and "
            + "TheStreakIsCountedHereBecauseARoundCannotKnowItAboutItself; the arithmetic: "
            + "SettlementTests.AThirdConsecutiveWinIsPaidEntirelyByTheSeatAboveTheWinner. "
            + "🔥 This entry was an Exempt(...) between rev 27 and P35 that said this rule could "
            + "never be audited by this harness, because RuleConformance watches ordinary rounds "
            + "and a three-round streak is not a property of a round. 🔥 That was half right: the "
            + "audit still watches one round, but it can be TOLD what the rounds before it did — "
            + "the count kept by the driver rather than by the engine — and re-derive the "
            + "consequence. ⚠️ Two details are open on their recorded defaults, each fenced: §9 "
            + "#41 (the streak keeps firing at a fourth win) — MatchEngineTests.TheStreakKeeps"
            + "FiringUntilTheExpertSaysOtherwise; §9 #46 (the seating of the round being settled "
            + "decides who is blamed, now that the seats can move mid-streak) — "
            + "RoundEngineTests.TheSeatBlamedIsTakenFromTheSeatingOfTheRoundBeingSettled. §9 #44 "
            + "(the substitution never reaches the money cards): "
            + "SettlementTests.TheStreakSubstitutionDoesNotReachTheMoneyCards. ✅ Whether §7.5 is "
            + "in the standing measurement set is answered in docs/STRATEGY.md §11: it is not, "
            + "and cannot be — every experiment plays one round a game."),
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
    /// <para>
    /// Two kinds live here: <b>whole</b> exemptions (<c>EXEMPT — …</c>), where nothing can check
    /// the rule, and <b>partial</b> ones marked ⚠️ inline, where most of a section is checked and
    /// some corner is not. Each must say why, and there must stay few enough to read in one
    /// sitting.
    /// </para>
    /// <para>
    /// ⚠️ <b>The ceiling moved from 5 to 7 on 2026-08-22, and the reason is recorded rather than
    /// waved through.</b> Three rules landed in <c>RULES.md</c> in one day that nothing
    /// implements — §7.4 and §7.5 (rev 27, unbuilt, §10 #20 and #21) and §3's corrected seating
    /// (rev 28, which the engine now contradicts, §10 #22). 🔥 <b>A count is a proxy for the
    /// property that actually matters, which is that no exemption is permanent</b>, so the
    /// ceiling was raised and a stronger assertion put beside it: <b>every whole exemption must
    /// name the packet that will discharge it.</b> An exemption nobody owns is the thing this
    /// test exists to prevent, and a bare number never caught that.
    /// </para>
    /// <para>
    /// ✅ <b>It came back down on 2026-08-22, and to 6 rather than to 5.</b> P36 made §3's entry
    /// true again and P35 converted §7.4 and §7.5 — but each of those two arrived carrying open
    /// §9 questions, so they moved from <b>whole</b> exemptions to <b>partial</b> ones and the
    /// total did not budge. 🔥 <b>What did change is the kind: there are no whole exemptions at
    /// all, for the first time since this test was written.</b> Every Settled rule in
    /// <c>RULES.md</c> is checked by something; what is left is six sections with a corner
    /// proceeding on a recorded default, each fenced by a test named for the question.
    /// <b>If this ceiling has to move up again, that is a finding about the backlog and not a
    /// licence.</b>
    /// </para>
    /// </remarks>
    [Fact]
    public void TheExemptionListIsShortAndEveryEntrySaysWhy()
    {
        var whole = Registry
            .Where(entry => entry.Value.StartsWith("EXEMPT", StringComparison.Ordinal))
            .ToList();

        var partial = Registry.Where(entry =>
            !entry.Value.StartsWith("EXEMPT", StringComparison.Ordinal) && entry.Value.Contains('⚠'));

        var exemptions = whole.Concat(partial).ToList();

        Assert.InRange(exemptions.Count, 1, 6);

        Assert.All(exemptions, entry => Assert.True(
            entry.Value.Length > 60, $"§{entry.Key}'s exemption says nothing a reader can re-check."));

        // 🔥 The assertion the count was standing in for: a rule nothing checks has to be
        // somebody's job. A whole exemption that names no packet is a rule quietly abandoned.
        Assert.All(whole, entry => Assert.True(
            Regex.IsMatch(entry.Value, @"\bP\d+(\.\d+)?\b"),
            $"§{entry.Key} is wholly exempt and names no packet to discharge it. "
            + "An exemption nobody owns is a rule quietly abandoned."));
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
