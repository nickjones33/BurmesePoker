using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// <c>opportunist</c> — the feeding ban at zero price (BUILD-PLAN P43).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Half of <c>warden</c>, and which half is the whole design.</b> P31 could not tell
/// <em>denial is worthless</em> from <em>`warden` overpaid for it</em>, because <c>warden</c>
/// buys locks with draws. This rung takes only what <c>outs</c> would have taken anyway — the
/// lock arms itself as a side effect — and then holds it, so its margin against <c>outs</c>
/// prices the hold alone.
/// </para>
/// <para>
/// ⚠️ <b>Every test here is one of the two halves</b>: the take is <c>outs</c>' (never a card it
/// does not want, whatever the lock would be worth), and the hold is <c>warden</c>'s, with §5.1's
/// own two escapes and no more.
/// </para>
/// </remarks>
public class OpportunistBotAgentTests
{
    /// <inheritdoc cref="WardenBotAgentTests"/>
    private static readonly IReadOnlyList<Card> NothingButDeadwood =
        Hands.Of("2C", "2C", "6C", "6C", "10C", "10C", "3D", "3D", "7D", "7D", "JD", "JD", "4H");

    /// <summary>
    /// A hand of four melds and two loose Queens — <b>12 of the 14 covered</b>, so the only card
    /// worth throwing is a Queen.
    /// </summary>
    private static readonly IReadOnlyList<Card> TwoLooseQueensAndFourMelds =
        Hands.Of("2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QD", "QC");

    /// <summary>
    /// One card from going out: throwing the Queen declares, and throwing anything else does not
    /// — the Queen a club rather than a diamond for <see cref="WardenBotAgentTests"/>' reason.
    /// </summary>
    private static readonly IReadOnlyList<Card> AWinningThirteenAndAQueen =
        Hands.Of("2H", "3H", "4H", "5H", "6H", "8D", "9D", "10D", "JD", "KC", "KH", "KS", "KD", "QC");

    /// <remarks>
    /// 🔥 <b>The zero price, asserted as the difference from <c>warden</c>.</b> The 9♠ melds
    /// nothing and never will; <c>warden</c> spends its draw on the lock and this rung refuses
    /// to — a card is taken because it improves the hand or not at all, which is what makes its
    /// margin against <c>outs</c> attribute to the hold rather than to the take.
    /// </remarks>
    [Fact]
    public void OpportunistNeverSpendsATurnToBuyALock()
    {
        var context = TurnContexts.Offered(NothingButDeadwood, Hands.Value("9S"), Stakes.Standard);

        Assert.Equal(TurnAction.TakeDiscard, new WardenBotAgent().ChooseAction(context));
        Assert.Equal(TurnAction.DrawFromDeck, new OpportunistBotAgent().ChooseAction(context));
        Assert.Equal(TurnAction.DrawFromDeck, new OutsBotAgent().ChooseAction(context));
    }

    /// <remarks>
    /// 🔥 <b>The hold, and it is <c>warden</c>'s exactly.</b> A Queen is what every other rung
    /// throws out of this hand — so a seat that has locked Queens by taking one in the open must
    /// not, or it has released for good the lock its take armed (§5.1, exception 1).
    /// </remarks>
    [Fact]
    public void OpportunistWillNotThrowBackARankItsTakeHasLocked()
    {
        var open = TurnContexts.Holding(TwoLooseQueensAndFourMelds);
        var locked = TurnContexts.Locking(TwoLooseQueensAndFourMelds, takenByYou: [Hands.Value("QS")]);

        Assert.Equal(Rank.Queen, new OpportunistBotAgent().ChooseDiscard(open).Rank);
        Assert.NotEqual(Rank.Queen, new OpportunistBotAgent().ChooseDiscard(locked).Rank);

        // And not merely at the head: a locked rank is not a card it is willing to throw at all,
        // which is what keeps a difficulty level built on this rung from releasing the lock as
        // its mistake (BUILD-PLAN P19).
        Assert.DoesNotContain(
            new OpportunistBotAgent().RankDiscards(locked), card => card.Rank == Rank.Queen);
    }

    /// <remarks>
    /// ⚠️ <b>Going out outranks the hold</b> (§5.1, exception 2): the round ends on the declaring
    /// discard, so the protected seat never gets a turn in which to take the card.
    /// </remarks>
    [Fact]
    public void TheHoldYieldsToTheDeclaringDiscard()
    {
        var locked = TurnContexts.Locking(AWinningThirteenAndAQueen, takenByYou: [Hands.Value("QS")]);

        Assert.Equal(Rank.Queen, new OpportunistBotAgent().ChooseDiscard(locked).Rank);
    }

    /// <remarks>
    /// ⚠️ <b>And a floor, for the reason the ban has one</b>: the discard is mandatory (§7.1) and
    /// a self-imposed restraint least of all outranks it.
    /// </remarks>
    [Fact]
    public void TheHoldYieldsWhenHoldingEveryLockedRankWouldLeaveNothingToThrow()
    {
        var everythingLocked = Hands.Of(
            "QC", "QD", "QH", "QS", "QC", "QD", "QH", "QS", "KC", "KD", "KH", "KS", "KC", "KD");

        var locked = TurnContexts.Locking(
            everythingLocked, takenByYou: [Hands.Value("QS"), Hands.Value("KS")]);

        var thrown = new OpportunistBotAgent().ChooseDiscard(locked);

        Assert.Contains(locked.LegalDiscards, card => card.Id == thrown.Id);
        Assert.NotEmpty(new OpportunistBotAgent().RankDiscards(locked));
    }

    /// <remarks>
    /// ✅ <b>The counterfactual is an instrument and never a move</b> (BUILD-PLAN P31 item 3), and
    /// the rung's own restraint stays in force in it, so what §13's mechanism variable measures
    /// through this rung is §5.1 and not the rung's own holds.
    /// </remarks>
    [Fact]
    public void TheCounterfactualRankingKeepsTheRungsOwnRestraint()
    {
        var context = TurnContexts.Locking(TwoLooseQueensAndFourMelds, takenByYou: [Hands.Value("KD")]);

        var opportunist = new OpportunistBotAgent();
        var overTheLegalSet = opportunist.RankDiscards(context);
        var overTheWholeHand = opportunist.RankDiscards(context, context.Hand);

        // Nothing is closed to it here, so the two are the same question and must agree.
        Assert.Equal(context.Hand.Count, context.LegalDiscards.Count);
        Assert.Equal(overTheLegalSet[0].Id, overTheWholeHand[0].Id);

        // And the restraint is in both: a King is not a card it is willing to throw either way.
        Assert.DoesNotContain(overTheWholeHand, card => card.Rank == Rank.King);
    }

    /// <remarks>
    /// ⚠️ <b>Where no lock is in play the rung is <c>outs</c>, card for card</b> — take and
    /// discard both — which is P15's one-decision discipline asserted rather than claimed.
    /// </remarks>
    [Fact]
    public void WithNothingLockedOpportunistIsOutsCardForCard()
    {
        var holding = TurnContexts.Holding(TwoLooseQueensAndFourMelds);
        var offered = TurnContexts.Offered(NothingButDeadwood, Hands.Value("2H"), Stakes.Standard);

        Assert.Equal(
            new OutsBotAgent().ChooseAction(offered),
            new OpportunistBotAgent().ChooseAction(offered));

        Assert.Equal(
            new OutsBotAgent().ChooseDiscard(holding).Id,
            new OpportunistBotAgent().ChooseDiscard(holding).Id);

        Assert.Equal(
            new OutsBotAgent().RankDiscards(holding).Select(card => card.Id),
            new OpportunistBotAgent().RankDiscards(holding).Select(card => card.Id));
    }
}
