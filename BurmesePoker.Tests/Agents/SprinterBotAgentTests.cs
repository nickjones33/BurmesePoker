using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// <c>sprinter</c> — the endgame played as a race (BUILD-PLAN P46).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The one change is the last-resort discard key, and it only bites within one card of
/// covering.</b> Off the endgame no discard leaves a hand a single draw would win, so the winning
/// key is zero for every candidate and the rung is <c>outs</c> card for card. Near the line the two
/// objectives — the most improvable thirteen and the most <em>winnable</em> thirteen — come apart,
/// and this rung keeps the winnable one, cover count never sacrificed to do it.
/// </para>
/// <para>
/// ⚠️ <b>How well it plays is not here</b> — that is a measurement with an interval, from
/// <c>docs/strategy/measurements.csv</c>. What a test holds is the rule: that the race fires only
/// in the endgame, that it never trades a melded card for a faster fuse, and that when it deviates
/// from <c>outs</c> it does so to keep more winning draws and for no other reason.
/// </para>
/// </remarks>
public class SprinterBotAgentTests
{
    /// <summary>
    /// Ten covered — two runs and a four-set — with a near-run (2♠3♠) one card from a win and a
    /// lone 7♦ to throw. A 4♠ or an A♠ completes the run and declares.
    /// </summary>
    private static readonly IReadOnlyList<Card> OneCardFromWinning =
        Hands.Of("4H", "5H", "6H", "8C", "9C", "10C", "QD", "QS", "QH", "QC", "2S", "3S", "7D");

    /// <summary>
    /// Deadwood: every card one of a same-card pair, none within run reach of another, so no
    /// single draw completes anything — a hand that is not near a win by any road.
    /// </summary>
    private static readonly IReadOnlyList<Card> NowhereNearAWin =
        Hands.Of("2C", "2C", "6C", "6C", "10C", "10C", "3D", "3D", "7D", "7D", "JD", "JD", "4H");

    /// <remarks>
    /// ✅ <b>The trigger is existence, not a threshold</b>: a hand a single draw would declare is
    /// within one card of covering; a hand no draw could is not. This is the fact
    /// <see cref="SeatRecorder"/> counts to say how often the race could fire at all.
    /// </remarks>
    [Fact]
    public void WithinOneCardOfCoveringIsWhetherSomeDrawWouldDeclare()
    {
        Assert.True(Endgame.WithinOneCardOfCovering(OneCardFromWinning));
        Assert.False(Endgame.WithinOneCardOfCovering(NowhereNearAWin));
    }

    /// <remarks>
    /// ✅ <b>Winning draws are the copies-weighted values that <em>declare</em>, not merely
    /// improve</b>, checked the long way through <see cref="PartialCover.Best"/>: a value counts
    /// when thirteen of the fourteen it leaves meld, weighed by its loose copies. It is a subset of
    /// the live outs, so it can never exceed them.
    /// </remarks>
    [Fact]
    public void WinningDrawsCountsOnlyTheDrawsThatDeclare()
    {
        foreach (var kept in EndgameLeaningHands(200))
        {
            var covered = PartialCover.Best(kept).CoveredCount;

            Assert.Equal(WinningDrawsTheLongWay(kept), LiveOuts.WinningDraws(kept, covered));
            Assert.True(LiveOuts.WinningDraws(kept, covered) <= LiveOuts.CardCount(kept, covered));
        }

        // And the crafted near-win really has some — a hand a draw would declare, counted both ways.
        var winningCover = PartialCover.Best(OneCardFromWinning).CoveredCount;
        Assert.Equal(WinningDrawsTheLongWay(OneCardFromWinning), LiveOuts.WinningDraws(OneCardFromWinning, winningCover));
        Assert.True(LiveOuts.WinningDraws(OneCardFromWinning, winningCover) > 0);
    }

    /// <remarks>
    /// 🔥 <b>Off the endgame the rung is <c>outs</c>, card for card</b> — no candidate discard
    /// leaves a hand a draw would win, so the winning key is zero everywhere and the tie falls to
    /// the out-count, which is <c>outs</c>' own key. The deadwood hand plus a spare is nowhere near
    /// a win whichever card is thrown.
    /// </remarks>
    [Fact]
    public void OffTheEndgameTheDiscardIsOutsCardForCard()
    {
        var context = TurnContexts.Holding([.. NowhereNearAWin, Hands.Value("9S")]);

        Assert.Equal(
            new OutsBotAgent().ChooseDiscard(context).Id,
            new SprinterBotAgent().ChooseDiscard(context).Id);

        Assert.Equal(
            new OutsBotAgent().RankDiscards(context).Select(card => card.Id),
            new SprinterBotAgent().RankDiscards(context).Select(card => card.Id));
    }

    /// <remarks>
    /// ⚠️ <b>The take, the claim, the objection and the declaration are all <c>outs</c>'</b> — the
    /// change is the discard alone (P15), so a difference in results is attributable to it and to
    /// nothing else. Asserted over the same hands the discard is.
    /// </remarks>
    [Fact]
    public void EverythingButTheDiscardIsOutsCardForCard()
    {
        foreach (var hand in EndgameLeaningHands(120, cards: 14))
        {
            var context = TurnContexts.Holding(hand);

            Assert.Equal(new OutsBotAgent().ChooseAction(Offer(hand)), new SprinterBotAgent().ChooseAction(Offer(hand)));
            Assert.Equal(new OutsBotAgent().Declare(context), new SprinterBotAgent().Declare(context));
        }
    }

    /// <remarks>
    /// 🔥 <b>The whole of the rung, asserted as an invariant rather than on a hand-picked case</b>:
    /// wherever <c>sprinter</c> throws a different card than <c>outs</c>, the two thirteens it is
    /// choosing between are tied on cover — the race never gives up a meld — and <c>sprinter</c>'s
    /// keeps <em>strictly more winning draws</em>. That is the only reason it ever deviates, and
    /// the sweep of endgame-leaning hands finds the deviation, so the test is not vacuous.
    /// </remarks>
    [Fact]
    public void WhereItDivergesFromOutsItKeepsMoreWinningDrawsAtEqualCover()
    {
        var diverged = 0;

        foreach (var hand in EndgameLeaningHands(4000, cards: 14))
        {
            var context = TurnContexts.Holding(hand);
            var byOuts = new OutsBotAgent().ChooseDiscard(context);
            var bySprinter = new SprinterBotAgent().ChooseDiscard(context);

            if (byOuts.Id == bySprinter.Id)
            {
                continue;
            }

            diverged++;

            var keptByOuts = Without(hand, byOuts);
            var keptBySprinter = Without(hand, bySprinter);

            var coveredByOuts = PartialCover.Best(keptByOuts).CoveredCount;
            var coveredBySprinter = PartialCover.Best(keptBySprinter).CoveredCount;

            // Cover is never sacrificed: the race only reorders discards already tied at the top.
            Assert.Equal(coveredByOuts, coveredBySprinter);

            // And it deviated for exactly one reason — a faster fuse.
            Assert.True(
                LiveOuts.WinningDraws(keptBySprinter, coveredBySprinter)
                > LiveOuts.WinningDraws(keptByOuts, coveredByOuts));
        }

        Assert.True(diverged > 0, "no endgame-leaning hand made sprinter and outs disagree; the sweep proved nothing");
    }

    /// <summary>An offer of the last card of a hand, so the take can be asked the same way for both.</summary>
    private static TurnContext Offer(IReadOnlyList<Card> hand) =>
        TurnContexts.Offered([.. hand.SkipLast(1)], Hands.Value("5C"), Stakes.Standard);

    /// <summary>The hand without that exact card — instance identity, as the rung's own removal is.</summary>
    private static IReadOnlyList<Card> Without(IReadOnlyList<Card> hand, Card card) =>
        [.. hand.Where(held => held != card)];

    /// <summary>The winning draws of a hand, counted without a prune, a bar or a cache.</summary>
    private static int WinningDrawsTheLongWay(IReadOnlyList<Card> kept)
    {
        var total = 0;

        foreach (var drawn in EveryValue())
        {
            var loose = drawn.IsJoker
                ? 4 - kept.Count(held => held.IsJoker)
                : 2 - kept.Count(held => held.SameValueAs(drawn));

            if (loose > 0 && PartialCover.Best([.. kept, drawn]).CoveredCount >= kept.Count)
            {
                total += loose;
            }
        }

        return total;
    }

    private static IEnumerable<Card> EveryValue()
    {
        yield return Card.Joker(new CardId(1_000), CardColor.Red);

        foreach (var suit in CardText.AllSuits)
        {
            foreach (var rank in CardText.AllRanks)
            {
                yield return Card.Ranked(new CardId(1_000), rank, suit);
            }
        }
    }

    /// <summary>
    /// Hands built to reach the endgame far more often than a shuffle would: a random spine, then
    /// two near-melds bolted on, so a fair share of them sit within a card or two of covering —
    /// which is where the one thing this rung does can be seen at all.
    /// </summary>
    private static IEnumerable<IReadOnlyList<Card>> EndgameLeaningHands(int count, int cards = 13, int seed = 20260825)
    {
        var random = new Random(seed);

        for (var hand = 0; hand < count; hand++)
        {
            var deck = Deck.TwoDecks();
            deck.Shuffle(random);

            // Two random adjacent-in-a-suit pairs and a random set-pair, then filled from the top.
            var spine = new List<Card>();
            var pool = deck.Cards.ToList();

            AddNearMeld(pool, spine, random);
            AddNearMeld(pool, spine, random);

            foreach (var card in pool)
            {
                if (spine.Count == cards)
                {
                    break;
                }

                if (!spine.Exists(held => held.Id == card.Id))
                {
                    spine.Add(card);
                }
            }

            yield return spine;
        }
    }

    /// <summary>Pulls two cards that could sit in one meld out of the pool and onto the spine.</summary>
    private static void AddNearMeld(List<Card> pool, List<Card> spine, Random random)
    {
        var anchorIndex = random.Next(pool.Count);
        var anchor = pool[anchorIndex];
        pool.RemoveAt(anchorIndex);
        spine.Add(anchor);

        // FindIndex, not FirstOrDefault: Card is a struct, so a missing partner would come back
        // as default(Card) and slip a duplicate CardId 0 into the hand.
        var partnerIndex = pool.FindIndex(card =>
            !card.IsJoker && !anchor.IsJoker
            && ((card.Rank == anchor.Rank && card.Suit != anchor.Suit)
                || (card.Suit == anchor.Suit && CoverScoreRunReach(card.Rank!.Value, anchor.Rank!.Value))));

        if (partnerIndex >= 0)
        {
            spine.Add(pool[partnerIndex]);
            pool.RemoveAt(partnerIndex);
        }
    }

    /// <summary>Within a run of one another, and not the same rank — a near-run companion.</summary>
    private static bool CoverScoreRunReach(Rank one, Rank other) =>
        one != other && Math.Abs((int)one - (int)other) <= 2;
}
