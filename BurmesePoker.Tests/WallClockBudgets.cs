namespace BurmesePoker.Tests;

/// <summary>
/// The tests that assert how long something takes, and the tests heavy enough to make them
/// lie, kept out of each other's way.
/// </summary>
/// <remarks>
/// <para>
/// Two tests in this suite are wall-clock budgets — <c>HandEvaluatorTests.EvaluatingThirteen&#8203;CardsIsFast</c>
/// and <c>PartialCoverTests.ScoringThirteenCardsIsFast</c> — and they are the only two that can
/// fail for a reason other than a defect (<c>STATUS.md</c>). They allow a second for work that
/// takes single-digit milliseconds, which is three orders of magnitude of headroom and still
/// not enough when every core is busy.
/// </para>
/// <para>
/// <b>P15 is what made that theoretical.</b> Its ladder tests play whole simulations, xunit
/// runs classes in parallel, and the budget tests began failing on a quiet machine —
/// self-inflicted contention rather than a slow evaluator. Naming one collection puts the
/// heavy classes and the budgets in single file, which restores exactly the concurrency the
/// tree was green under. <b>Nothing about either budget was loosened</b>: a performance guard
/// is a decision for whoever owns it, not something to widen because a new test walked into it.
/// </para>
/// </remarks>
internal static class WallClockBudgets
{
    /// <summary>The collection name. Classes naming it never run beside one another.</summary>
    internal const string Collection = "wall-clock budgets";
}
