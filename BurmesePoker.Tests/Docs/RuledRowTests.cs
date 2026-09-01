using System.Reflection;
using System.Text.RegularExpressions;

namespace BurmesePoker.Tests.Docs;

/// <summary>
/// 🔥 <b>A ruling by Nick is a <c>PLAYER</c> ruling, and a <c>PLAYER</c> ruling must not quietly
/// become unfenced</b> (BUILD-PLAN P69 acceptance 4).
/// </summary>
/// <remarks>
/// <para>
/// On 2026-09-01 Nick ruled eleven of <c>RULES.md</c> §9's open rows. Nine of them close on the
/// default already in force, so <b>no code changed at all</b> — which is exactly the shape in
/// which a fence rots unnoticed: the row stops reading like a question, somebody renames the
/// test, and the day an <c>EXPERT</c> overturns the ruling nothing goes red. <b>Provenance ranks:
/// <c>EXPERT</c> outranks <c>PLAYER</c></b>, so every one of these rows is still a question and
/// every one of them still names the test that answers it.
/// </para>
/// <para>
/// ⚠️ <b>Both halves are checked</b>: the row must name a test, and the test must exist. Naming a
/// test that has been renamed away is the failure this exists to catch, and it cannot be seen by
/// reading either the document or the tree alone.
/// </para>
/// </remarks>
public class RuledRowTests
{
    /// <summary>
    /// Each row Nick ruled, and the test that keeps its reading true. The names are checked
    /// against the assembly, so a rename is a red build rather than a stale sentence.
    /// </summary>
    public static TheoryData<int, string, string> Ruled =>
        new()
        {
            { 33, "FeedingBanTests", "ABannedRankThatIsNotTheDeclaringDiscardStaysBannedEvenOnAWinningHand" },
            { 36, "SettlementTests", "TheBonusDoesNotReachTheMoneyCardSettlement" },
            { 37, "TableRulesTests", "TheTwoRulesThatMoveWithTheTableSizeSplitAtTheSameSeam" },
            { 38, "RoundEngineTests", "TheDealBonusIsTheDealtThirteenAloneUntilTheExpertSaysOtherwise" },
            { 40, "SettlementTests", "TheDealBonusDoesNotReachTheMoneyCards" },
            { 41, "MatchEngineTests", "TheStreakResetsWhenItPaysUntilTheExpertSaysOtherwise" },
            { 44, "SettlementTests", "TheStreakSubstitutionDoesNotReachTheMoneyCards" },
            { 47, "SeatingAgreementTests", "AgreementIsAMajorityUntilTheExpertSaysOtherwise" },
            { 48, "RoundEngineTests", "WhenTwoSeatsAreDealtAWinningThirteenTheEarlierInTurnOrderTakesIt" },
            { 49, "TableLookTests", "TheClaimedTurnUpLiesFaceUpUntilTheExpertSaysOtherwise" },
            { 50, "TableLookTests", "TheFaceUpCopyStaysWhenTheConcealedDuplicateIsThrownUntilTheExpertSaysOtherwise" }
        };

    [Theory]
    [MemberData(nameof(Ruled))]
    public void EveryRowNickRuledStillNamesATestThatExists(int row, string fixture, string fence)
    {
        var type = typeof(RuledRowTests).Assembly.GetTypes()
            .SingleOrDefault(candidate => candidate.Name == fixture);

        Assert.True(type is not null, $"RULES.md §9 #{row} names {fixture}, which no longer exists.");

        Assert.True(
            type!.GetMethod(fence, BindingFlags.Public | BindingFlags.Instance) is not null,
            $"RULES.md §9 #{row} names {fixture}.{fence}, which has been renamed or removed. "
            + "A row ruled by a player rather than by an expert is still a question, and it must "
            + "keep the fence that fails the day the expert answers it.");

        var rules = Documentation.Text("docs/RULES.md");
        var stated = Regex.Matches(rules, @"^\|\s*(?:~~)?" + row + @"(?:~~)?\s*\|.*$", RegexOptions.Multiline);

        Assert.True(stated.Count > 0, $"RULES.md §9 has no row #{row} at all.");

        Assert.Contains(
            stated,
            match => match.Value.Contains(fence, StringComparison.Ordinal));
    }

    /// <summary>
    /// ✅ <b>Every one of the ruled rows is a <c>PLAYER</c> ruling dated the day it was given</b>,
    /// said in the row itself — so a reader of §9 can see at a glance that an expert has not
    /// spoken, whatever the code does.
    /// </summary>
    [Theory]
    [MemberData(nameof(Ruled))]
    public void EveryRowNickRuledSaysWhoRuledItAndWhen(int row, string fixture, string fence)
    {
        _ = fixture;
        _ = fence;

        var rules = Documentation.Text("docs/RULES.md");
        var stated = Regex.Matches(rules, @"^\|\s*(?:~~)?" + row + @"(?:~~)?\s*\|.*$", RegexOptions.Multiline)
            .Select(match => match.Value)
            .Where(text => text.Contains("Ruled by Nick", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            stated.Count == 1,
            $"RULES.md §9 #{row} does not say it was ruled by Nick, so its reading reads as a "
            + "recommendation the code happens to follow.");

        Assert.Contains("2026-09-01", stated[0], StringComparison.Ordinal);
    }
}
