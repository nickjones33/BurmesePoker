using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// <c>warden</c> — the first rung that plays RULES.md §5.1 at somebody rather than round it
/// (BUILD-PLAN P31).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Two claims, and they are one idea.</b> It <b>takes</b> a card it does not want when doing
/// so closes a rank against the seat that fed it, and it then <b>holds</b> that rank rather than
/// throwing it back — because the release is under the taker's control (§5.1, exception 1), so a
/// rung that locked and then released would have paid for nothing. Every test here is one half of
/// that or the seam between them.
/// </para>
/// <para>
/// ⚠️ <b>The restraint keeps the rule's own two escapes and no more.</b> It yields to the
/// declaring discard and it yields to having nothing else to throw — the same two the ban itself
/// yields to — so a lock can never cost this rung the round or deadlock its turn.
/// </para>
/// </remarks>
public class WardenBotAgentTests
{
    /// <summary>
    /// Thirteen cards no two of which are partners: two suits' worth of doubled singletons and a
    /// lone heart.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Built so that <em>every</em> card is deadwood</b>, which is what makes the take
    /// decision a clean question: nothing offered can improve this hand, so a rung that takes has
    /// taken for some other reason. Duplicate suits are forbidden in a set (§6.2), so a second 2♣
    /// is no nearer a meld than none.
    /// </remarks>
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
    /// (§7.1, §7.1.1).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The Queen is a club rather than a diamond, and that is the difference between a test
    /// and a coincidence.</b> A Q♦ extends 8♦9♦10♦J♦ into a five-run, which would leave the hand
    /// with <em>two</em> declaring discards — so a rung that threw the other one would still have
    /// gone out, and the assertion would pass without the exception existing. Found by writing it
    /// the wrong way round first.
    /// </remarks>
    private static readonly IReadOnlyList<Card> AWinningThirteenAndAQueen =
        Hands.Of("2H", "3H", "4H", "5H", "6H", "8D", "9D", "10D", "JD", "KC", "KH", "KS", "KD", "QC");

    /// <remarks>
    /// 🔥 <b>The whole rung in one assertion.</b> The 9♠ melds nothing and never will, so
    /// <c>outs</c> spends its turn on the deck instead — which is the right answer to the only
    /// question <c>outs</c> asks. <c>warden</c> asks a second one: the seat that threw this has
    /// just told the table it is shedding nines, seven of the eight are still unaccounted for, and
    /// taking it means it may never throw another (§5.1).
    /// </remarks>
    [Fact]
    public void WardenTakesACardItDoesNotWantToCloseTheRankAgainstTheSeatThatThrewIt()
    {
        var context = TurnContexts.Offered(NothingButDeadwood, Hands.Value("9S"), Stakes.Standard);

        Assert.Equal(TurnAction.DrawFromDeck, new OutsBotAgent().ChooseAction(context));
        Assert.Equal(TurnAction.TakeDiscard, new WardenBotAgent().ChooseAction(context));
    }

    /// <remarks>
    /// ⚠️ <b>A rank already closed buys nothing a second time</b>, so the rung is back to
    /// <c>outs</c>' answer — which matters because a take it does not need costs it the draw.
    /// </remarks>
    [Fact]
    public void ARankAlreadyClosedIsNotWorthTakingAgain()
    {
        var context = TurnContexts.Locking(
            NothingButDeadwood, Hands.Value("9S"), takenByYou: [Hands.Value("9H")]);

        Assert.Equal(TurnAction.DrawFromDeck, new WardenBotAgent().ChooseAction(context));
    }

    /// <remarks>
    /// 🔥 <b>§5.1, exception 1, read as a strategy rather than as a rule.</b> A rank this seat has
    /// thrown away can never close again however many more of it it picks up — so a take made to
    /// lock it would buy a card it does not want and nothing else.
    /// </remarks>
    [Fact]
    public void ARankThisSeatHasAlreadyThrownAwayCanNeverBeLockedAndIsNotWorthTaking()
    {
        var context = TurnContexts.Locking(
            NothingButDeadwood, Hands.Value("9S"), thrownByYou: [Hands.Value("9D")]);

        Assert.Equal(TurnAction.DrawFromDeck, new WardenBotAgent().ChooseAction(context));
    }

    /// <remarks>
    /// ⚠️ <b>Never a joker, and it is a rules decision rather than a strategy one.</b> Whether
    /// taking a joker closes the other jokers is a <c>PLAYER</c> house ruling (RULES.md §9 #27) —
    /// the rule says <em>the same rank</em> and a joker has none — so the one rung whose whole
    /// claim rests on the lock does not build on it. It takes a joker for the ordinary reason or
    /// not at all.
    /// </remarks>
    [Fact]
    public void AJokerIsNeverTakenForTheLockAlone()
    {
        // A joker that melds nothing here: two of a value is not a meld, so the black joker
        // improves no arrangement of a hand with no pair of same-suit neighbours in it.
        var context = TurnContexts.Offered(NothingButDeadwood, Hands.Value("BJ"), Stakes.Standard);

        Assert.Equal(
            new OutsBotAgent().ChooseAction(context),
            new WardenBotAgent().ChooseAction(context));
    }

    /// <remarks>
    /// 🔥 <b>The trap the packet named in advance, and the test that says it was avoided.</b> The
    /// release is under the taker's control, so <c>outs</c> would happily throw back the card it
    /// took for a reason <c>outs</c> knows nothing about: here a Queen is the only card in the hand
    /// that costs no melded card, so it is <em>exactly</em> what every other rung throws. A seat
    /// that has locked Queens must not.
    /// </remarks>
    [Fact]
    public void WardenWillNotThrowBackARankItHasLocked()
    {
        var open = TurnContexts.Holding(TwoLooseQueensAndFourMelds);
        var locked = TurnContexts.Locking(TwoLooseQueensAndFourMelds, takenByYou: [Hands.Value("QS")]);

        Assert.Equal(Rank.Queen, new OutsBotAgent().ChooseDiscard(open).Rank);
        Assert.Equal(Rank.Queen, new OutsBotAgent().ChooseDiscard(locked).Rank);

        Assert.Equal(Rank.Queen, new WardenBotAgent().ChooseDiscard(open).Rank);
        Assert.NotEqual(Rank.Queen, new WardenBotAgent().ChooseDiscard(locked).Rank);

        // And not merely at the head: a locked rank is not a card it is willing to throw at all,
        // which is what keeps a difficulty level built on this rung from releasing the lock as
        // its mistake (BUILD-PLAN P19).
        Assert.DoesNotContain(new WardenBotAgent().RankDiscards(locked), card => card.Rank == Rank.Queen);
    }

    /// <remarks>
    /// ⚠️ <b>Going out outranks the lock, for the reason it outranks the ban</b> (§5.1, exception
    /// 2): the round ends on that discard, so the seat below never gets a turn in which to take the
    /// card and the gift is never delivered.
    /// </remarks>
    [Fact]
    public void TheLockYieldsToTheDeclaringDiscard()
    {
        var locked = TurnContexts.Locking(AWinningThirteenAndAQueen, takenByYou: [Hands.Value("QS")]);

        Assert.Equal(Rank.Queen, new WardenBotAgent().ChooseDiscard(locked).Rank);
    }

    /// <remarks>
    /// ⚠️ <b>And a floor, for the reason the ban has one</b>: the discard is mandatory (§7.1) and a
    /// self-imposed restraint least of all outranks it. Eight Queens and six Kings, both ranks
    /// locked, and the turn still has to be completed.
    /// </remarks>
    [Fact]
    public void TheLockYieldsWhenHoldingEveryLockedRankWouldLeaveNothingToThrow()
    {
        var everythingLocked = Hands.Of(
            "QC", "QD", "QH", "QS", "QC", "QD", "QH", "QS", "KC", "KD", "KH", "KS", "KC", "KD");

        var locked = TurnContexts.Locking(
            everythingLocked, takenByYou: [Hands.Value("QS"), Hands.Value("KS")]);

        var thrown = new WardenBotAgent().ChooseDiscard(locked);

        Assert.Contains(locked.LegalDiscards, card => card.Id == thrown.Id);
        Assert.NotEmpty(new WardenBotAgent().RankDiscards(locked));
    }

    /// <remarks>
    /// ✅ <b>The counterfactual is an instrument and never a move</b> (BUILD-PLAN P31 item 3). Asked
    /// over the whole hand, a seat the ban has restricted says what it <em>would</em> have thrown —
    /// and the two answers differing is the whole of the mechanism variable. ⚠️ The rung's own
    /// restraint stays in force in both, so the difference is §5.1 and not <c>warden</c>.
    /// </remarks>
    [Fact]
    public void TheCounterfactualRankingAnswersOverTheChoiceItIsHandedAndKeepsTheRungsOwnRestraint()
    {
        // Queens are closed *to* this seat by the player it feeds, and it has locked Kings against
        // the player that feeds it. Both hold, and only the first is §5.1 acting on this turn.
        var context = TurnContexts.Locking(TwoLooseQueensAndFourMelds, takenByYou: [Hands.Value("KD")]);

        var warden = new WardenBotAgent();
        var overTheLegalSet = warden.RankDiscards(context);
        var overTheWholeHand = warden.RankDiscards(context, context.Hand);

        // Nothing is closed to it here, so the two are the same question and must agree.
        Assert.Equal(context.Hand.Count, context.LegalDiscards.Count);
        Assert.Equal(overTheLegalSet[0].Id, overTheWholeHand[0].Id);

        // And the restraint is in both: a King is not a card it is willing to throw either way.
        Assert.DoesNotContain(overTheWholeHand, card => card.Rank == Rank.King);
    }

    /// <remarks>
    /// 🔥 <b>The counterfactual asked of a seat the ban has actually restricted</b>: <c>outs</c>
    /// would throw a Queen out of this hand and §5.1 will not let it, so the two heads differ —
    /// which is a bitten lock, counted.
    /// </remarks>
    [Fact]
    public void ARestrictedSeatSaysWhatTheBanTookFromIt()
    {
        var fed = TurnContexts.Fed(TwoLooseQueensAndFourMelds, Hands.Value("QS"));

        var outs = new OutsBotAgent();

        Assert.NotEqual(fed.Hand.Count, fed.LegalDiscards.Count);
        Assert.NotEqual(Rank.Queen, outs.RankDiscards(fed)[0].Rank);
        Assert.Equal(Rank.Queen, outs.RankDiscards(fed, fed.Hand)[0].Rank);
    }
}
