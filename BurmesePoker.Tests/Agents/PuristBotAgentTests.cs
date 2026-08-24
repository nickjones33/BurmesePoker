using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// <c>purist</c> — the rung that plays for the clean bonus (BUILD-PLAN P44).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The stated exchange rate is the whole design, and every test here is one side of
/// it.</b> The jokerless preference sits <em>between</em> <c>outs</c>' two keys: below the
/// melded cards — a meld is never paid for the bonus, which is <c>warden</c>'s lesson about
/// paying for a side idea in the currency wins are made of — and above the live outs, which are
/// the option value of the joker it sheds. Lexicographic rather than numeric, because a numeric
/// rate would need a win-probability estimate nothing here supplies, and a knob would make it a
/// family of rungs (P15).
/// </para>
/// <para>
/// ⚠️ <b>The contrast rung in every test is <c>outs</c></b>, the rung this is one change from:
/// where the two agree the change is priced out of the decision, and where they part the joker
/// is the only thing between them.
/// </para>
/// </remarks>
public class PuristBotAgentTests
{
    /// <summary>
    /// Thirteen cards that win clean — a five-card run, a four-card set, a four-card run — and
    /// a red joker as the fourteenth. Throwing the joker declares jokerless; throwing several
    /// ordinary cards also declares, with the joker melded in their place.
    /// </summary>
    private static readonly IReadOnlyList<Card> AWinningThirteenAndAJoker =
        Hands.Of("2H", "3H", "4H", "5H", "6H", "KC", "KH", "KS", "KD", "8D", "9D", "10D", "JD", "RJ");

    /// <summary>
    /// Deadwood that melds nothing and never can this turn — same-card duplicates, so no set is
    /// legal and no run is within reach, and the joker has no two partners to glue — plus a red
    /// joker. Every discard leaves zero melded, so the whole hand ties on the first key.
    /// </summary>
    private static readonly IReadOnlyList<Card> NothingButDeadwoodAndAJoker =
        Hands.Of("2C", "2C", "6C", "6C", "10C", "10C", "3D", "3D", "7D", "7D", "JD", "JD", "4H", "RJ");

    /// <summary>
    /// A hand whose joker is doing real work: it completes the 2♥ 3♥ 4♥ run as a fourth card,
    /// so throwing it costs a melded card and throwing any of the four loose cards costs none.
    /// </summary>
    private static readonly IReadOnlyList<Card> AJokerEarningItsPlace =
        Hands.Of("2H", "3H", "4H", "RJ", "KC", "KH", "KS", "8D", "9D", "10D", "QC", "QD", "4S", "9S");

    /// <remarks>
    /// 🔥 <b>The declaring corner, and it is the packet's whole reason in one hand.</b> Both
    /// rungs are looking at several discards that all leave thirteen melded; <c>outs</c> throws
    /// an ordinary card — a joker's <c>Potential</c> is <see cref="int.MaxValue"/>, so it loses
    /// every tie — and wins with the joker in the thirteen, forfeiting RULES.md §7.3's ×3.
    /// <c>purist</c> throws the joker and wins clean, for the same win.
    /// </remarks>
    [Fact]
    public void WhereACleanAndADirtyDeclarationTiePuristThrowsTheJoker()
    {
        var context = TurnContexts.Holding(AWinningThirteenAndAJoker);

        Assert.True(new PuristBotAgent().ChooseDiscard(context).IsJoker);
        Assert.False(new OutsBotAgent().ChooseDiscard(context).IsJoker);
    }

    /// <remarks>
    /// 🔥 <b>The half of the exchange rate that pays: outs are paid for cleanliness.</b> Every
    /// discard here leaves zero melded, so the first key ties across the hand — and the joker is
    /// worth a fistful of live outs (it will glue any partial the next draws form), so
    /// <c>outs</c> keeps it over anything. <c>purist</c> sheds it: cleanliness is priced above
    /// every out, and this is the hand where that is the whole difference.
    /// </remarks>
    [Fact]
    public void WhereTheMeldsTiePuristPaysOutsToShedItsJoker()
    {
        var context = TurnContexts.Holding(NothingButDeadwoodAndAJoker);

        Assert.True(new PuristBotAgent().ChooseDiscard(context).IsJoker);
        Assert.False(new OutsBotAgent().ChooseDiscard(context).IsJoker);
    }

    /// <remarks>
    /// 🔥 <b>The half of the exchange rate that refuses: a melded card is never paid.</b> The
    /// joker here is the fourth card of a run, so shedding it loses a melded card — it is out of
    /// the tie at the top before the jokerless preference is ever asked, and the rung is
    /// <c>outs</c> card for card, the whole ranking included.
    /// </remarks>
    [Fact]
    public void PuristNeverGivesUpAMeldedCardForTheBonus()
    {
        var context = TurnContexts.Holding(AJokerEarningItsPlace);

        var purist = new PuristBotAgent().ChooseDiscard(context);

        Assert.False(purist.IsJoker);
        Assert.Equal(new OutsBotAgent().ChooseDiscard(context).Id, purist.Id);
        Assert.Equal(
            new OutsBotAgent().RankDiscards(context).Select(card => card.Id),
            new PuristBotAgent().RankDiscards(context).Select(card => card.Id));
    }

    /// <remarks>
    /// ⚠️ <b>Cleanliness needs zero jokers, so the preference is monotone in the count.</b> With
    /// two jokers and everything else melded, every discard leaves a winning thirteen — a joker
    /// appends to any run — and <c>purist</c> starts shedding towards clean even though this
    /// win, if it comes, is still dirty.
    /// </remarks>
    [Fact]
    public void WithTwoJokersPuristPrefersTheThrowThatLeavesFewer()
    {
        var twoJokersAndTwelveMelded = Hands.Of(
            "2H", "3H", "4H", "5H", "KC", "KH", "KS", "KD", "8D", "9D", "10D", "JD", "RJ", "BJ");

        var context = TurnContexts.Holding(twoJokersAndTwelveMelded);

        Assert.True(new PuristBotAgent().ChooseDiscard(context).IsJoker);
        Assert.False(new OutsBotAgent().ChooseDiscard(context).IsJoker);
    }

    /// <remarks>
    /// ⚠️ <b>One change means one change</b> (P15): the take and the claim are <c>outs</c>' —
    /// including taking a joker that improves the hand, because a taken joker is not a
    /// forfeited bonus while the ranking can shed it again — and the counterfactual instrument
    /// keeps the rung's own preference (BUILD-PLAN P31 item 3).
    /// </remarks>
    [Fact]
    public void EverythingButTheDiscardRankingIsOutsCardForCard()
    {
        var improving = TurnContexts.Offered(
            Hands.Of("2H", "3H", "5C", "7D", "9S", "JC", "QD", "KH", "2S", "6D", "8C", "10H", "AC"),
            Hands.Value("4H"),
            Stakes.Standard);

        var useless = TurnContexts.Offered(
            Hands.Of("2C", "2C", "6C", "6C", "10C", "10C", "3D", "3D", "7D", "7D", "JD", "JD", "4H"),
            Hands.Value("9S"),
            Stakes.Standard);

        Assert.Equal(new OutsBotAgent().ChooseAction(improving), new PuristBotAgent().ChooseAction(improving));
        Assert.Equal(new OutsBotAgent().ChooseAction(useless), new PuristBotAgent().ChooseAction(useless));

        // And the ranking asked over an arbitrary candidate set — the counterfactual instrument
        // — is the rung's own ordering, jokerless preference included.
        var deadwood = TurnContexts.Holding(NothingButDeadwoodAndAJoker);

        Assert.Equal(
            new PuristBotAgent().RankDiscards(deadwood).Select(card => card.Id),
            new PuristBotAgent().RankDiscards(deadwood, deadwood.Hand).Select(card => card.Id));
    }

    /// <remarks>
    /// ⚠️ <b>The catalog facts the dial and the sweep rest on</b>: money-ranked — a rung built
    /// to trade rounds for a multiplied prize has no honest win-rate ranking — level with
    /// <c>outs</c>, and <b>not</b> the hardest, so no stand-in seat, hint or difficulty level
    /// moved when it landed.
    /// </remarks>
    [Fact]
    public void PuristIsMoneyRankedLevelWithOutsAndNotTheHardest()
    {
        var purist = BotCatalog.Resolve("purist");

        Assert.Equal(RankedOn.Money, purist.Ranked);
        Assert.Equal(BotCatalog.Resolve("outs").Strength, purist.Strength);
        Assert.Equal("outs", BotCatalog.Hardest.Name);
        Assert.Contains(purist, BotCatalog.StakesSensitive);
        Assert.DoesNotContain(purist, BotCatalog.Ladder);
    }
}
