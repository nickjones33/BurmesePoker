using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Presentation;

/// <summary>
/// The hint a front end shows, and the view model built from a real turn (packet P13.1).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>The claim under test is that the advice is not a second implementation.</b> A hint
/// worked out beside the bots rather than by them is a different strategy wearing their name,
/// and it would start disagreeing with the per-card costs the moment either changed. So the
/// test is an equality against <see cref="GreedyBotAgent"/> on the very same
/// <see cref="TurnContext"/> — which is also why it has to be played through a real round: a
/// context is the engine's to build, and nothing else can make one.
/// </para>
/// </remarks>
public class ComputerAdviceTests
{
    private static readonly PlayerId[] FourPlayers = [.. Enumerable.Range(0, 4).Select(seat => new PlayerId(seat))];

    /// <summary>
    /// Twelve cards that partition and the Q♣, which joins nothing — the hand
    /// <c>GreedyBotAgentTests</c> reasons about, so the two agree or genuinely disagree.
    /// </summary>
    private static readonly string[] TwelveAndADeadQueen =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "QC"];

    [Fact]
    public void EveryAnswerIsTheBotsOwn()
    {
        var watcher = Play(TwelveAndADeadQueen);

        Assert.NotEmpty(watcher.Discards);
        Assert.All(watcher.Discards, pair => Assert.Equal(pair.Bot.Id, pair.Advice.Id));
        Assert.All(watcher.Actions, pair => Assert.Equal(pair.Bot, pair.Advice));
        Assert.All(watcher.Claims, pair => Assert.Equal(pair.Bot, pair.Advice));
    }

    /// <remarks>
    /// The whole reason the two must agree: the console puts the cost beside the arrow, and a
    /// hint pointing at a card the costs say is expensive would be the front end contradicting
    /// itself on screen.
    /// </remarks>
    [Fact]
    public void TheAdvisedThrowIsNeverDearerThanAnyOtherCard()
    {
        var watcher = Play(TwelveAndADeadQueen);

        Assert.All(watcher.Views, view =>
        {
            var advised = Assert.Single(view.Cards, card => card.IsSuggestedThrow);
            Assert.Equal(view.Cards.Min(card => card.CostOfThrowing), advised.CostOfThrowing);
        });
    }

    /// <remarks>
    /// The overload a front end actually calls: everything about the hand comes off the
    /// context, so a caller cannot pass the wrong player's ownership or last round's money
    /// cards by mistake.
    /// </remarks>
    [Fact]
    public void AViewBuiltFromATurnDescribesThatTurnsHand()
    {
        var watcher = Play(TwelveAndADeadQueen);

        Assert.All(watcher.Views, view =>
        {
            // Fourteen: the view is built at the discard, with the taken card still in hand.
            // ⚠️ It stays fourteen after the engine has discarded from the seat's own list,
            // which is the snapshot HandView takes rather than the alias it used to keep.
            Assert.Equal(14, view.Count);
            Assert.Equal(view.Count, view.Melds.Sum(meld => meld.Count) + view.Loose.Count);
            Assert.Equal(view.Covered, view.Melds.Sum(meld => meld.Count));
        });
    }

    /// <remarks>
    /// Ownership reaches the view model, and only the asking player's own — a
    /// <see cref="TurnContext"/> has no way to anybody else's, which is the concealment rule
    /// as a type (RULES.md §6.3).
    /// </remarks>
    [Fact]
    public void ADealtMoneyCardShowsAsOwnedInItsOwnersView()
    {
        // The Q♣ is turned up from the top, so both copies pay; seat one is dealt one of them.
        var watcher = Play(TwelveAndADeadQueen, turnedUpFromTop: "QC");
        var opening = watcher.Views[0];

        var queen = Assert.Single(opening.Cards, card => card.Card.SameValueAs(Hands.Value("QC")));
        Assert.True(queen.IsMoneyCard);
        Assert.True(queen.IsOwned);
        Assert.True(queen.State.HasFlag(CardDisplayState.Owned));
        Assert.True(queen.State.HasFlag(CardDisplayState.PaysOnce));
    }

    /// <summary>
    /// ✅ <b>R6(d) — the fifth question, put to the watcher for real.</b> The watcher used to sit
    /// at the opener's seat, the one seat the objection is never asked of, so its
    /// <c>Objections</c> list was asserted over while empty by construction. Here it sits
    /// upstream of a claiming opener, holding the rank, and the pair really is collected.
    /// </summary>
    [Fact]
    public void TheObjectionAdviceIsTheBotsOwnAndIsActuallyAsked()
    {
        var watcher = new AdviceWatcher();

        var engine = new RoundEngine(
            FourPlayers,
            FourPlayers.ToDictionary(
                player => player,
                IPlayerAgent (player) =>
                    player == FourPlayers[3] ? watcher
                    : player == FourPlayers[0] ? new ScriptedPlayerAgent(new ScriptedTurn { Claim = true })
                    : player == FourPlayers[1] ? new ScriptedPlayerAgent(new ScriptedTurn(), new ScriptedTurn { Declare = true })
                    : ScriptedPlayerAgent.Passive()),
            Stakes.Standard,
            DealBuilder.ForPlayers(4)
                .Give(1, "2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "KD")
                .Give(3, "3D")
                .TurnUpFromTop("3C")
                .TurnUpFromBottom("4C")
                .Build(),
            new Random(1));

        engine.Play();

        var pair = Assert.Single(watcher.Objections);

        // Every rung refuses whenever it may (P28), and the advice is the bot's own answer.
        Assert.True(pair.Bot);
        Assert.Equal(pair.Bot, pair.Advice);
    }

    private static AdviceWatcher Play(string[] hand, string turnedUpFromTop = "3C")
    {
        var watcher = new AdviceWatcher();

        var engine = new RoundEngine(
            FourPlayers,
            FourPlayers.ToDictionary(
                player => player,
                IPlayerAgent (player) => player == FourPlayers[0] ? watcher : new GreedyBotAgent()),
            Stakes.Standard,
            DealBuilder.ForPlayers(4)
                .Give(0, hand)
                .ThenDraw("JD")
                .TurnUpFromTop(turnedUpFromTop)
                .TurnUpFromBottom("4C")
                .Build(),
            new Random(1));

        engine.Play();
        return watcher;
    }

    /// <summary>
    /// A seat that plays as the computer does, and at every question also asks
    /// <see cref="ComputerAdvice"/> the same thing so the two answers can be compared.
    /// </summary>
    private sealed class AdviceWatcher : IPlayerAgent
    {
        private readonly GreedyBotAgent _bot = new();
        private readonly ComputerAdvice _advice = new();

        public List<(Card Bot, Card Advice)> Discards { get; } = [];

        public List<(TurnAction Bot, TurnAction Advice)> Actions { get; } = [];

        public List<(bool Bot, bool Advice)> Claims { get; } = [];

        public List<(bool Bot, bool Advice)> Objections { get; } = [];

        /// <summary>The view a front end would have drawn at each discard.</summary>
        public List<HandView> Views { get; } = [];

        public TurnAction ChooseAction(TurnContext context)
        {
            var chosen = _bot.ChooseAction(context);
            Actions.Add((chosen, _advice.Take(context)));
            return chosen;
        }

        public Card ChooseDiscard(TurnContext context)
        {
            var advised = _advice.Discard(context);
            var chosen = _bot.ChooseDiscard(context);

            Discards.Add((chosen, advised));
            Views.Add(HandView.Of(context, advised));
            return chosen;
        }

        public bool ClaimTurnedUpMoneyCard(TurnContext context)
        {
            var chosen = _bot.ClaimTurnedUpMoneyCard(context);
            Claims.Add((chosen, _advice.ClaimTurnedUpMoneyCard(context)));
            return chosen;
        }

        public bool ObjectToClaim(TurnContext context)
        {
            var chosen = _bot.ObjectToClaim(context);
            Objections.Add((chosen, _advice.ObjectToClaim(context)));
            return chosen;
        }

        public bool Declare(TurnContext context) => _bot.Declare(context);
    }
}
