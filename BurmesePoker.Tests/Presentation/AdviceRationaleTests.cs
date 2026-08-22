using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Presentation;

/// <summary>
/// The computer's reasoning, said out loud — packet P24.2.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The claim under test is that the sentence is true, not that it is present.</b> An
/// explanation is the one feature in this project whose failure mode is to look right: a
/// paragraph that justified the wrong card, or that talked about a rule this table does not
/// have, reads exactly as well as a correct one. So most of what is asserted here is about
/// <em>what the sentence may not say</em>.
/// </para>
/// </remarks>
public class AdviceRationaleTests
{
    /// <summary>
    /// Twelve cards that partition and a dead queen — the hand <c>ComputerAdviceTests</c> and
    /// <c>GreedyBotAgentTests</c> both reason about, so a disagreement here is a real one.
    /// </summary>
    private static readonly string[] TwelveAndADeadQueen =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC", "JD"];

    /// <summary>
    /// ✅ <b>Acceptance 1.</b> The sentence names the key that separated the chosen card from
    /// the next best, and the card it names is the card the arrow points at.
    /// </summary>
    [Fact]
    public void TheSentenceNamesTheKeyThatSeparatedTheWinnerFromTheRunnerUp()
    {
        var advice = new ComputerAdvice();
        var context = TurnContexts.Holding(Hands.Of(TwelveAndADeadQueen));

        var why = advice.WhyThrow(context);

        Assert.Equal(advice.Discard(context).Id, why.Advised!.Value.Id);
        Assert.NotNull(why.RunnerUp);
        Assert.False(why.NoRunnerUp);

        // Either a key separated them or nothing did — and whichever it was, the sentence says so
        // rather than leaving a reader to guess.
        if (why.SeparatedBy is { } key)
        {
            Assert.False(why.NothingSeparatedThem);
            Assert.Contains(key.Name, why.Sentence, StringComparison.Ordinal);
        }
        else
        {
            Assert.True(why.NothingSeparatedThem);
            Assert.Contains("Nothing separated them", why.Sentence, StringComparison.Ordinal);
        }

        Assert.Contains(why.Advised!.Value.ToString(), why.Sentence, StringComparison.Ordinal);
    }

    /// <summary>
    /// ✅ <b>Acceptance 1, the case that must not be hidden.</b> Where every key ties, the
    /// computer is <em>indifferent</em> — and an expert will not be, which is one of the more
    /// valuable turns this instrument can catch. It is said out loud rather than dressed up as a
    /// decision.
    /// </summary>
    [Fact]
    public void WhereNothingSeparatedThemTheSentenceSaysSo()
    {
        // Four kings and four queens: throwing any one of the eight leaves the same partition and
        // the same partnership, so the ordering falls through to the hand's own order.
        var context = TurnContexts.Holding(
            Hands.Of("KC", "KH", "KS", "KD", "QC", "QH", "QS", "QD", "2H", "3H", "4H", "5H", "6H", "7H"));

        var why = new ComputerAdvice().WhyThrow(context);

        Assert.True(why.NothingSeparatedThem || why.SeparatedBy is not null);

        if (why.NothingSeparatedThem)
        {
            Assert.Null(why.SeparatedBy);
            Assert.Contains("no real preference", why.Sentence, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// ✅ <b>Acceptance 1, the other case.</b> The ranking dedupes by value, so a hand of pairs
    /// can offer one distinct move — and then there is nothing to compare with, which is a
    /// sentence rather than a blank.
    /// </summary>
    [Fact]
    public void WhereThereIsNoRunnerUpItSaysThatInstead()
    {
        // ⚠️ Fourteen cards of exactly two values, and duplicate suits cannot make a set (§6.2),
        // so nothing melds and nothing is near going out. The ranking dedupes by value, the ban
        // closes one of the two, and one distinct move is left.
        var hand = Hands.Of("KC", "KC", "KC", "KC", "KC", "KC", "KC", "QC", "QC", "QC", "QC", "QC", "QC", "QC");
        var context = TurnContexts.Fed(hand, Hands.Value("QC"));

        var why = new ComputerAdvice().WhyThrow(context);

        Assert.True(why.NoRunnerUp);
        Assert.Null(why.RunnerUp);
        Assert.Contains("nothing to compare", why.Sentence, StringComparison.Ordinal);
    }

    /// <summary>
    /// ✅ <b>Acceptance 2.</b> The arrow, the sentence and the journal's second opinion are
    /// <b>one</b> ranking between them — asserted rather than assumed.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The instrument is <see cref="ComputerAdvice.RankingsBought"/> and not a clock.</b>
    /// What P24.2 promises is that an explanation costs no extra <c>PartialCover.Best</c> calls
    /// over the hint a front end already draws, and the ranking is where every one of those calls
    /// is made.
    /// </remarks>
    [Fact]
    public void TheArrowTheSentenceAndTheJournalCostOneRankingBetweenThem()
    {
        var advice = new ComputerAdvice();
        var context = TurnContexts.Holding(Hands.Of(TwelveAndADeadQueen));

        Assert.Equal(0, advice.RankingsBought);

        var why = advice.WhyThrow(context);
        var arrow = advice.Discard(context);
        var opinion = advice.OnDiscard(context);

        Assert.Equal(1, advice.RankingsBought);
        Assert.Equal(arrow.Id, why.Advised!.Value.Id);
        Assert.Equal(arrow.Id, opinion!.Card);

        // Non-vacuous: a different turn really does buy a new answer, so the memo is a memo and
        // not a cache that has quietly stopped asking.
        advice.WhyThrow(TurnContexts.Holding(Hands.Of(TwelveAndADeadQueen)));
        Assert.Equal(2, advice.RankingsBought);
    }

    /// <summary>
    /// ✅ <b>Acceptance 6 — the trap P19 created.</b> A difficulty level is the strongest rung
    /// wrapped in a mistake rate, and the mistake <em>is</em> the runner-up of the very ranking
    /// this feature renders. Explaining through a level would confidently justify a move the
    /// computer chose <b>because it was second best</b>, which is the worst failure this packet
    /// can have.
    /// </summary>
    [Fact]
    public void TheExplanationIsTheBareRungsAndNeverALevels()
    {
        var advice = new ComputerAdvice();
        var bare = BotCatalog.Hardest.Create(0);
        var slips = 0;

        for (var seed = 0; seed < 12; seed++)
        {
            var context = TurnContexts.Holding(Hands.Of(TwelveAndADeadQueen));
            var why = advice.WhyThrow(context);

            Assert.Equal(bare.ChooseDiscard(context).Id, why.Advised!.Value.Id);

            foreach (var level in DifficultyLadder.All)
            {
                if (level.Create(seed).ChooseDiscard(context).Id != why.Advised!.Value.Id)
                {
                    slips++;
                }
            }
        }

        // Non-vacuous: the easy level really does throw something else, so the equality above is a
        // statement about which agent was asked rather than a coincidence of one hand.
        Assert.True(slips > 0, "no difficulty level ever slipped, so this test proves nothing.");
    }

    /// <summary>
    /// ✅ <b>Acceptance 7.</b> A card the feeding ban took out of the choice is explained as a
    /// <b>rule</b> and not as a judgement.
    /// </summary>
    [Fact]
    public void ABannedCardIsExplainedAsARuleAndNotAsAJudgement()
    {
        // ⚠️ Not TwelveAndADeadQueen: with the J♦ that hand goes out by throwing the Q♣, and §5.1
        // exception 2 then makes the banned card legal after all — which is the rule working and
        // not the ban failing. The 2♠ breaks the diamond run, so nothing here can declare.
        var hand = Hands.Of(
            "2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC", "2S");
        var context = TurnContexts.Fed(hand, Hands.Value("QC"));

        Assert.True(
            context.LegalDiscards.Count < context.Hand.Count,
            $"nothing was actually banned: hand {context.Hand.Count}, legal {context.LegalDiscards.Count}.");

        var why = new ComputerAdvice().WhyThrow(context);

        Assert.Contains("not in the choice at all", why.Sentence, StringComparison.Ordinal);
        Assert.Contains("a rule and not the computer's opinion", why.Sentence, StringComparison.Ordinal);
    }

    /// <summary>
    /// ✅ <b>Acceptance 7, the rarest turn there is.</b> Under RULES.md §5.1's floor every card
    /// is legal again, and an explanation that has been saying <em>"not throwable"</em> all round
    /// must stop. ⚠️ A rationale computed from a stale ban is confidently wrong in the one
    /// situation nobody has seen.
    /// </summary>
    [Fact]
    public void WhereTheBansFloorYieldsTheExplanationStopsSayingIt()
    {
        // Every rank the seat holds has been taken in the open by the player it feeds, so the ban
        // would empty the choice — and yields for the turn instead (§5.1, The floor).
        var hand = Hands.Of("KC", "KH", "KS", "KD", "QC", "QH", "QS", "QD", "JC", "JH", "JS", "JD", "2H", "3H");
        var context = TurnContexts.Fed(
            hand,
            Hands.Value("KC"),
            Hands.Value("QC"),
            Hands.Value("JC"),
            Hands.Value("2H"),
            Hands.Value("3H"));

        Assert.Equal(context.Hand.Count, context.LegalDiscards.Count);

        var why = new ComputerAdvice().WhyThrow(context);

        Assert.DoesNotContain("not in the choice at all", why.Sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("may not throw", why.Sentence, StringComparison.Ordinal);
    }

    /// <summary>
    /// ✅ <b>Acceptance 8 — the newest thing in the packet.</b> The sentence is true <b>at the
    /// table it is played at</b>: four-handed a declaration owes one joker-free run, five-handed
    /// it owes no series at all (RULES.md §7.1.1), so an explanation phrased in runs is false at
    /// the table the browser now deals (P32).
    /// </summary>
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void TheSentenceIsTrueAtTheTableItIsPlayedAt(int seats)
    {
        var context = TurnContexts.Holding(Hands.Of(TwelveAndADeadQueen), seats);

        var why = new ComputerAdvice().WhyThrow(context);

        Assert.Contains($"At {seats}", why.Sentence, StringComparison.Ordinal);

        if (seats == 4)
        {
            Assert.Contains("must be a run", why.Sentence, StringComparison.Ordinal);
        }
        else
        {
            // ⚠️ Not merely "says something different": at five seats there is no series
            // requirement, so the word must not be there at all.
            Assert.DoesNotContain("must be a run", why.Sentence, StringComparison.Ordinal);
            Assert.Contains("any thirteen that all meld win", why.Sentence, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// ✅ <b>Acceptance 9.</b> No explanation implies the computer is playing for §7.3's clean
    /// bonus. It is not: <c>CoverScore.Potential</c> returns <see cref="int.MaxValue"/> for a
    /// joker, so the true sentence is that it will never throw one, and every clean win it
    /// collects is an accident.
    /// </summary>
    [Fact]
    public void NoExplanationImpliesTheComputerIsPlayingForTheCleanBonus()
    {
        var context = TurnContexts.Holding(
            Hands.Of("RJ", "BJ", "2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "QC"));

        var why = new ComputerAdvice().WhyThrow(context);

        Assert.Contains("never throw a joker", why.Sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("bonus", why.Sentence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("jokerless", why.Sentence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("×2", why.Sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("×3", why.Sentence, StringComparison.Ordinal);
    }

    /// <summary>
    /// 🔥 <b>The keys are packed for sorting, not for reading</b>, and this is the assertion that
    /// keeps that off a screen. <c>outs</c> stores its second key negated and a joker's
    /// partnership is <see cref="int.MaxValue"/>.
    /// </summary>
    [Fact]
    public void NoPackedNumberEverReachesTheSentence()
    {
        var context = TurnContexts.Holding(
            Hands.Of("RJ", "2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC"));

        var why = new ComputerAdvice().WhyThrow(context);

        Assert.DoesNotContain("2147483647", why.Sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("-", why.Sentence, StringComparison.Ordinal);
    }

    /// <summary>
    /// The other four questions carry a rationale too, and the declaration's says plainly that
    /// there is no judgement in it rather than dressing one up.
    /// </summary>
    [Fact]
    public void TheOtherQuestionsAreExplainedAsAGainOrAsNothingAtAll()
    {
        var advice = new ComputerAdvice();
        var hand = Hands.Of("2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC");
        var context = TurnContexts.Offered(hand, Hands.Value("JD"), BurmesePoker.Domain.Money.Stakes.Standard);

        var take = advice.WhyTake(context);
        Assert.Equal(Hands.Value("JD").Rank, take.Advised!.Value.Rank);

        // The J♦ closes 8♦9♦10♦ into a run of four, so the sentence is the gain and says both
        // counts — <em>"from 12 to 13"</em> — rather than merely that it is worth taking.
        Assert.Contains("raises the cards of your hand that meld from 12 to 13", take.Sentence, StringComparison.Ordinal);

        Assert.Contains("worth nothing either way", advice.WhyObject(context).Sentence, StringComparison.Ordinal);
        Assert.Contains("no judgement here", advice.WhyDeclare(context).Sentence, StringComparison.Ordinal);
        Assert.NotEmpty(advice.WhyClaim(context).Sentence);
    }
}
