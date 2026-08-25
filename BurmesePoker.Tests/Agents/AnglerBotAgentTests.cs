using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// <c>angler</c> — a draw priced in cards (BUILD-PLAN P45).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The one change is the take, and every test here is one side of its price.</b> A card
/// off the pile costs this turn's blind draw; the rung takes it only when the hand it leaves is
/// worth more than the draw's expectation — live out-cards over cards unseen. So a card that
/// melds nothing is taken when it opens enough doors (the enrichment take, which no rung before
/// this had), an inert card is left where <c>outs</c> leaves it, and an improving card is taken
/// exactly as <c>outs</c> takes it, because a certain melded card outprices any draw a real
/// hand can expect.
/// </para>
/// <para>
/// ⚠️ <b>How well it plays is not here</b> — that is a measurement with an interval, published
/// from <c>docs/strategy/measurements.csv</c>. What belongs in a test is the rule itself, and
/// that the card-weighted count the price reads is the count the definition asks for.
/// </para>
/// </remarks>
public class AnglerBotAgentTests
{
    /// <summary>
    /// Thirteen with <b>no live outs at all</b>: every card is one of a same-card pair or the
    /// lone 4♥, no two distinct cards share a rank or sit within run reach, so no single draw
    /// completes any meld — the hand a blind draw is worth nothing to.
    /// </summary>
    private static readonly IReadOnlyList<Card> NothingButDeadwood =
        Hands.Of("2C", "2C", "6C", "6C", "10C", "10C", "3D", "3D", "7D", "7D", "JD", "JD", "4H");

    /// <summary>
    /// Four melds and a loose Q♦ — twelve of thirteen covered, waiting on plenty
    /// (2♥–7♥, 8♦9♦10♦, K♣K♥K♠).
    /// </summary>
    private static readonly IReadOnlyList<Card> TwelveCoveredAndALooseQueen =
        Hands.Of("2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QD");

    /// <remarks>
    /// 🔥 <b>The enrichment take — the one decision no rung before this makes.</b> The deadwood
    /// hand has zero live out-cards, so a blind draw is expected to bring nothing; the 5♣ melds
    /// nothing today but sits beside the 6♣s, and keeping it opens the 4♣s, the 7♣s and the
    /// jokers as outs. A hand with doors beats a hand without, and the draw it forfeits was
    /// worthless — so the card is taken, where <c>outs</c> and everything below it draws blind.
    /// </remarks>
    [Fact]
    public void ACardThatMeldsNothingIsTakenWhenItOpensMoreDoorsThanTheDeckWould()
    {
        var context = TurnContexts.Offered(NothingButDeadwood, Hands.Value("5C"), Stakes.Standard);

        Assert.Equal(TurnAction.DrawFromDeck, new OutsBotAgent().ChooseAction(context));
        Assert.Equal(TurnAction.TakeDiscard, new AnglerBotAgent().ChooseAction(context));
    }

    /// <remarks>
    /// ⚠️ <b>And the same card is refused when the price is wrong.</b> The 9♠ joins nothing in
    /// this hand — taking it opens no doors at all — so the comparison is a tie of nothings and
    /// the tie goes to the deck, whose cards are the ones that pay (RULES.md §4.4). This is what
    /// keeps the enrichment take from being <c>warden</c>'s paid lock under another name.
    /// </remarks>
    [Fact]
    public void AnInertCardIsLeftOnThePile()
    {
        var context = TurnContexts.Offered(NothingButDeadwood, Hands.Value("9S"), Stakes.Standard);

        Assert.Equal(TurnAction.DrawFromDeck, new AnglerBotAgent().ChooseAction(context));
        Assert.Equal(new OutsBotAgent().ChooseAction(context), new AnglerBotAgent().ChooseAction(context));
    }

    /// <remarks>
    /// ✅ <b>An improving card is still taken, and the arithmetic says why it always will be</b>:
    /// a certain melded card prices at the whole unseen pool — ninety-odd card-equivalents —
    /// against live out-counts that real hands keep far below half of it. The J♦ completes the
    /// diamond run; <c>angler</c> takes it exactly as <c>outs</c> does.
    /// </remarks>
    [Fact]
    public void AnImprovingCardIsTakenExactlyAsOutsTakesIt()
    {
        var context = TurnContexts.Offered(TwelveCoveredAndALooseQueen, Hands.Value("JD"), Stakes.Standard);

        Assert.Equal(TurnAction.TakeDiscard, new OutsBotAgent().ChooseAction(context));
        Assert.Equal(TurnAction.TakeDiscard, new AnglerBotAgent().ChooseAction(context));
    }

    /// <remarks>
    /// ⚠️ <b>The discard is <c>outs</c>' card for card</b> — take and ranking are the only
    /// places the price is read, which is P15's one-decision discipline asserted rather than
    /// claimed.
    /// </remarks>
    [Fact]
    public void TheDiscardIsOutsCardForCard()
    {
        foreach (var hand in RandomHands(12, cards: 14))
        {
            var context = TurnContexts.Holding(hand);

            Assert.Equal(
                new OutsBotAgent().ChooseDiscard(context).Id,
                new AnglerBotAgent().ChooseDiscard(context).Id);

            Assert.Equal(
                new OutsBotAgent().RankDiscards(context).Select(card => card.Id),
                new AnglerBotAgent().RankDiscards(context).Select(card => card.Id));
        }
    }

    /// <remarks>
    /// 🔥 <b>The same purchase at the other place it arises</b>
    /// (<see cref="ProspectorBotAgent"/>'s rule): a claimed turn-up is a known card bought with
    /// this turn's blind draw, so the enrichment that takes the 5♣ off the pile claims it off
    /// the table — and the inert 9♠ is refused in both places. <c>outs</c> claims neither,
    /// because neither improves the hand today.
    /// </remarks>
    [Fact]
    public void TheClaimPaysTheSameTollAsTheTake()
    {
        var enriching = TurnContexts.Offered(
            NothingButDeadwood,
            Hands.Value("8S"),
            Stakes.Standard,
            designating:
            [
                Card.Ranked(new CardId(1_003), Rank.Nine, Suit.Spades),
                Card.Ranked(new CardId(1_004), Rank.Five, Suit.Clubs)
            ]);

        var inert = TurnContexts.Offered(
            NothingButDeadwood,
            Hands.Value("8S"),
            Stakes.Standard,
            designating:
            [
                Card.Ranked(new CardId(1_003), Rank.Five, Suit.Clubs),
                Card.Ranked(new CardId(1_004), Rank.Nine, Suit.Spades)
            ]);

        Assert.True(new AnglerBotAgent().ClaimTurnedUpMoneyCard(enriching));
        Assert.False(new AnglerBotAgent().ClaimTurnedUpMoneyCard(inert));

        Assert.False(new OutsBotAgent().ClaimTurnedUpMoneyCard(enriching));
        Assert.False(new OutsBotAgent().ClaimTurnedUpMoneyCard(inert));
    }

    /// <remarks>
    /// ✅ <b>The card-weighted count is the count the definition asks for</b>, checked the long
    /// way — every value the shoe still holds, through <see cref="PartialCover.Best"/>, each
    /// improving one weighed by its loose copies and the jokers by however many of the four the
    /// hand does not hold. The probes are <see cref="LiveOuts.Count"/>'s probes exactly, so this
    /// is the same fence <c>ThePruneNeverThrowsAwayARealOut</c> is, for the other number.
    /// </remarks>
    [Fact]
    public void TheCardWeightedCountWeighsEachOutByItsLooseCopies()
    {
        foreach (var kept in RandomHands(20, cards: 13))
        {
            var covered = PartialCover.Best(kept).CoveredCount;

            Assert.Equal(CardCountTheLongWay(kept, covered), LiveOuts.CardCount(kept, covered));

            // And a door is never worth less than one card nor more than its copies: the
            // value count brackets the card count from below.
            Assert.True(LiveOuts.CardCount(kept, covered) >= LiveOuts.Count(kept, covered));
        }
    }

    /// <summary>The card-weighted outs of a hand, counted without a prune, a bar or a cache.</summary>
    private static int CardCountTheLongWay(IReadOnlyList<Card> kept, int covered)
    {
        var total = 0;

        foreach (var drawn in EveryValue())
        {
            var loose = drawn.IsJoker
                ? 4 - kept.Count(held => held.IsJoker)
                : 2 - kept.Count(held => held.SameValueAs(drawn));

            if (loose > 0 && PartialCover.Best([.. kept, drawn]).CoveredCount > covered)
            {
                total += loose;
            }
        }

        return total;
    }

    /// <inheritdoc cref="OutsBotAgentTests"/>
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

    /// <summary>Hands dealt off a shuffled shoe, so the shapes are the ones play produces.</summary>
    private static IEnumerable<IReadOnlyList<Card>> RandomHands(int count, int cards, int seed = 20260824)
    {
        var random = new Random(seed);

        for (var hand = 0; hand < count; hand++)
        {
            var deck = Deck.TwoDecks();
            deck.Shuffle(random);

            yield return [.. deck.Cards.Take(cards)];
        }
    }
}
