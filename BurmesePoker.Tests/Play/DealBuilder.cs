using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// Arranges the 108 cards into the order a round will draw them, so a scripted round deals
/// the hands the test asks for.
/// </summary>
/// <remarks>
/// <para>
/// The layout mirrors <c>RoundEngine</c>'s setup exactly: the first 13 × <em>n</em> cards are
/// dealt one at a time around the table, so seat <em>s</em> gets positions
/// <em>s</em>, <em>s+n</em>, <em>s+2n</em>…; the <b>last</b> card is turned up from the
/// bottom; and the card after the deal is turned up from the top. Everything between is the
/// draw pile, in draw order.
/// </para>
/// <para>
/// <b>Filler is never a money card.</b> Anything not named by the test is chosen so that its
/// value pays nothing — neither permanent designator (7♦, A♠) nor either turned-up value —
/// and the copies that would have paid are pushed to the bottom of the draw pile. So a deal
/// owns exactly the money the test asked for, and a settlement expectation can be worked out
/// by hand.
/// </para>
/// </remarks>
internal sealed class DealBuilder
{
    private static readonly string[] PermanentMoneyCards = ["7D", "AS"];

    private readonly int _playerCount;
    private readonly List<Card> _pool = [.. DeckBuilder.BuildTwoDecks()];
    private readonly Dictionary<int, Queue<Card>> _hands = [];
    private readonly List<Card> _draws = [];
    private Card? _turnedUpFromTop;
    private Card? _turnedUpFromBottom;

    private DealBuilder(int playerCount) => _playerCount = playerCount;

    public static DealBuilder ForPlayers(int count) => new(count);

    /// <summary>Deals the named cards, in order, to the given seat. Short hands are filled out.</summary>
    public DealBuilder Give(int seat, params string[] codes)
    {
        _hands[seat] = new Queue<Card>(codes.Select(Claim));
        return this;
    }

    /// <summary>The card turned up from the top of the deck — the claimable one.</summary>
    public DealBuilder TurnUpFromTop(string code)
    {
        _turnedUpFromTop = Claim(code);
        return this;
    }

    /// <summary>The card turned up from the bottom of the deck.</summary>
    public DealBuilder TurnUpFromBottom(string code)
    {
        _turnedUpFromBottom = Claim(code);
        return this;
    }

    /// <summary>The first cards of the draw pile, in the order they will be drawn.</summary>
    public DealBuilder ThenDraw(params string[] codes)
    {
        _draws.AddRange(codes.Select(Claim));
        return this;
    }

    public IReadOnlyList<Card> Build()
    {
        var top = _turnedUpFromTop
            ?? throw new InvalidOperationException("Say what is turned up from the top.");
        var bottom = _turnedUpFromBottom
            ?? throw new InvalidOperationException("Say what is turned up from the bottom.");

        var designators = PermanentMoneyCards.Select(Hands.Value).Append(top).Append(bottom).ToArray();
        bool PaysMoney(Card card) => Array.Exists(designators, card.SameValueAs);

        var filler = new Queue<Card>(_pool.Where(card => !PaysMoney(card)));
        var wouldPay = _pool.Where(PaysMoney).ToList();

        var order = new List<Card>(DeckBuilder.TotalCards);

        for (var dealt = 0; dealt < _playerCount * 13; dealt++)
        {
            var seat = dealt % _playerCount;

            order.Add(_hands.TryGetValue(seat, out var hand) && hand.Count > 0
                ? hand.Dequeue()
                : filler.Dequeue());
        }

        order.Add(top);
        order.AddRange(_draws);
        order.AddRange(filler);
        order.AddRange(wouldPay);
        order.Add(bottom);

        return order.Count == DeckBuilder.TotalCards
            ? order
            : throw new InvalidOperationException($"Built {order.Count} cards, not {DeckBuilder.TotalCards}.");
    }

    private Card Claim(string code)
    {
        var wanted = Hands.Value(code);
        var index = _pool.FindIndex(card => card.SameValueAs(wanted));

        return index < 0
            ? throw new InvalidOperationException($"The shoe has no {code} left.")
            : Pop(index);
    }

    private Card Pop(int index)
    {
        var card = _pool[index];
        _pool.RemoveAt(index);
        return card;
    }
}
