using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Server;

/// <summary>
/// What a remote seat is allowed to answer, and what the table simply does not accept.
/// </summary>
/// <remarks>
/// 🔥 <b>An answer that does not fit is refused, not obeyed</b> (P13.4). The connection drops it,
/// the question stays standing, and the stand-in plays if nobody answers properly — which is why
/// the feeding ban belongs here as well as in the engine: a browser naming a closed card is
/// answering a question it was never asked, and a hosted table must not fall over for it.
/// </remarks>
public class SeatAnswerTests
{
    private static readonly IReadOnlyList<Card> ThreeQueensAndSomeDeadwood =
        Hands.Of("QD", "QC", "QH", "2H", "3H", "4H", "5D", "7C", "9S", "10S", "JS", "AC", "6H", "8D");

    [Fact]
    public void ADiscardTheSeatIsHoldingAndMayThrowFits()
    {
        var prompt = Prompt(TurnContexts.Holding(ThreeQueensAndSomeDeadwood));

        Assert.True(new SeatAnswer.Discard(ThreeQueensAndSomeDeadwood[0]).Fits(prompt));
        Assert.True(prompt.MayThrow(ThreeQueensAndSomeDeadwood[0]));
    }

    [Fact]
    public void ADiscardOfARankTheSeatBelowTookInTheOpenDoesNotFit()
    {
        var closed = Prompt(TurnContexts.Fed(ThreeQueensAndSomeDeadwood, Hands.Value("QS")));
        var queen = ThreeQueensAndSomeDeadwood.First(card => card.Rank == Rank.Queen);
        var deadwood = ThreeQueensAndSomeDeadwood.First(card => card.Rank == Rank.Seven);

        // Held, and still not a move (RULES.md §5.1).
        Assert.True(closed.Holds(queen));
        Assert.False(closed.MayThrow(queen));
        Assert.False(new SeatAnswer.Discard(queen).Fits(closed));

        Assert.True(new SeatAnswer.Discard(deadwood).Fits(closed));
    }

    private static SeatPrompt Prompt(TurnContext context) =>
        SeatPrompt.For(context, SeatQuestion.Discard);
}
