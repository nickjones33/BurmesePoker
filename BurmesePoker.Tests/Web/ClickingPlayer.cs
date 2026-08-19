using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;
using BurmesePoker.Server;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// A person at the browser, scripted: it presses whatever the page would let it press, the
/// moment the seat says it is being asked something.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The point of driving <see cref="SeatBoard"/> rather than the connection under it</b>
/// (which <c>ScriptedSeat</c> already does) is that this is the thing the component holds. Every
/// card it sees is a card the page would draw, so what it writes down is what a real page would
/// have on screen — and P13.4's acceptance is about the page.
/// </para>
/// <para>
/// It answers inside <see cref="SeatBoard.Changed"/>, which is raised on the round's own thread
/// before the seat begins waiting (P13.2), so a whole match runs deterministically with nothing
/// sleeping, polling or racing. A real circuit answers later, from another thread, which
/// <c>TableSessionTests.AnAnswerFromAnotherThreadIsWaitedFor</c> covers.
/// </para>
/// <para>
/// <b>What it plays:</b> it draws from the deck, declines the turned-up card, throws whichever
/// card the computer marked, and declares as soon as it may — the seat's hint end to end, which
/// is also why a table of these finishes for the same reason a table of bots does.
/// </para>
/// </remarks>
public sealed class ClickingPlayer
{
    private readonly SeatBoard _seat;
    private int _round;

    public ClickingPlayer(SeatBoard seat)
    {
        _seat = seat;
        seat.Changed += OnDrawn;
    }

    /// <summary>Every question it was shown, in order.</summary>
    public List<SeatPrompt> Asked { get; } = [];

    /// <summary>
    /// Every hand the page held while it played, including the resting ones — <b>with the round
    /// it belonged to</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A <c>CardId</c> identifies a card in a round's shoe, and the shoe is rebuilt every
    /// round</b> (<c>MatchEngine</c>), so the same id is a different physical card next round.
    /// Anything comparing hands across seats has to compare them a round at a time.
    /// </remarks>
    public List<(int Round, HandView Hand)> Hands { get; } = [];

    /// <summary>The hand left on screen the moment after each card was thrown.</summary>
    public List<(Card Thrown, HandView Kept)> Threw { get; } = [];

    private void OnDrawn()
    {
        if (_seat.Asking is { } asked)
        {
            _round = asked.Round;
        }

        if (_seat.Hand is { } showing && (Hands.Count == 0 || !ReferenceEquals(Hands[^1].Hand, showing)))
        {
            Hands.Add((_round, showing));
        }

        if (_seat.Asking is not { } asking)
        {
            return;
        }

        Asked.Add(asking);

        switch (asking.Question)
        {
            case SeatQuestion.Take:
                _seat.Take(TurnAction.DrawFromDeck);
                break;

            case SeatQuestion.ClaimMoneyCard:
                _seat.Claim(false);
                break;

            case SeatQuestion.Discard:
                var throwing = Suggested(asking.Hand);
                if (_seat.Throw(throwing))
                {
                    Threw.Add((throwing, _seat.Hand!));
                }

                break;

            default:
                _seat.Declare(true);
                break;
        }
    }

    /// <summary>The card the computer marked, or the first loose one if it is not offering hints.</summary>
    private static Card Suggested(HandView hand)
    {
        foreach (var card in hand.Cards)
        {
            if (card.IsSuggestedThrow)
            {
                return card.Card;
            }
        }

        return hand.Loose.Count > 0 ? hand.Loose[0].Card : hand.Cards[0].Card;
    }
}
