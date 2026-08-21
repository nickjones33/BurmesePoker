using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// ✅ <b>RULES.md §4.5 — claiming the turned-up money card needs the permission of the seat that
/// plays before you, and that seat may refuse only by holding the rank</b> (packet P28).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The permission and the feeding ban are one mechanism, and these tests are written to
/// show it rather than to describe it.</b> A claim is a public take, so it closes that rank
/// against whoever discards to the claimer (§5.1); the veto exists because the claim spends the
/// upstream seat's hand. So the two facts worth pinning are that an <em>allowed</em> claim closes
/// the rank and a <em>refused</em> one does not — the one line where this packet touches P27's
/// code.
/// </para>
/// <para>
/// ⚠️ <b>The objection is asked of a seat that is not on turn</b>, which nothing else in the game
/// does. Alice opens, so the seat asked is Dan — the last in the order, and the one whose discard
/// Alice will be offered all round.
/// </para>
/// </remarks>
public class ClaimPermissionTests
{
    private static readonly PlayerId Alice = new(0);
    private static readonly PlayerId Bob = new(1);
    private static readonly PlayerId Carol = new(2);
    private static readonly PlayerId Dan = new(3);
    private static readonly PlayerId[] FourPlayers = [Alice, Bob, Carol, Dan];

    /// <summary>Thirteen cards that partition exactly: 2♥–7♥, 8♦9♦10♦, and all four kings.</summary>
    private static readonly string[] WinningHand =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "KD"];

    /// <summary>Thirteen cards with no three of any suit, for the seat that may not object.</summary>
    private static readonly string[] NoThrees =
        ["2D", "4D", "5D", "6D", "7S", "8S", "9S", "10S", "JS", "QS", "JD", "QD", "2S"];

    /// <summary>
    /// ✅ <b>Only a holder is asked at all.</b> A question with one possible answer is not a
    /// question, and an objection is a disclosure — so a seat that could not refuse never hears
    /// about the claim (RULES.md §4.5, §10 #18).
    /// </summary>
    [Fact]
    public void ASeatThatCouldNotRefuseIsNeverAsked()
    {
        var dan = Passive();
        var round = Play(Claiming(), dan, Deal().Give(3, NoThrees));

        Assert.Empty(dan.Objections);
        Assert.True(round.Table.TurnedUpFromTopClaimed);
    }

    /// <summary>
    /// ✅ <b>A holder is asked, and the holding test is the rank alone</b> (RULES.md §9 #30). Dan
    /// holds the 3♦ and the card on the table is the 3♣: different card, different suit, same
    /// lock — because §5.1 closes a rank.
    /// </summary>
    [Fact]
    public void AHolderOfTheRankIsAskedEvenThoughItIsAnotherSuit()
    {
        var dan = Passive();
        var round = Play(Claiming(), dan, Deal().Give(3, "3D"));

        var asked = Assert.Single(dan.Objections);
        Assert.Equal(Alice, asked.Claimant);
        Assert.Equal(round.Table.TurnedUpFromTop.Id, asked.Card.Id);
    }

    /// <summary>
    /// ✅ <b>A refusal stops the claim: the card stays on the table and the opener draws blind.</b>
    /// There is no discard to take on the opening turn, so the only other way to take a card is
    /// the deck (RULES.md §5).
    /// </summary>
    [Fact]
    public void ARefusedClaimLeavesTheCardOnTheTableAndTheOpenerDrawsBlind()
    {
        var observer = new RecordingObserver();
        var round = Play(Claiming(), Refusing(), Deal().Give(3, "3D"), observer);

        Assert.False(round.Table.TurnedUpFromTopClaimed);
        Assert.Equal(2, round.Table.TurnedUpOnTable.Count);
        Assert.Contains(round.Table.TurnedUpOnTable, card => card.Id == round.Table.TurnedUpFromTop.Id);
        Assert.Contains(observer.Events, entry => entry.StartsWith($"{Alice} drew"));

        // The deck gave the opener its card, so the blind draw is owned — which a claim never is
        // (RULES.md §4.4, §4.5).
        Assert.DoesNotContain(observer.Events, entry => entry.Contains("claimed"));
    }

    /// <summary>
    /// 🔥 <b>The one line where this packet touches P27's code: a refused claim must not arm the
    /// feeding ban.</b> Nothing was taken in the open, so the rank stays open to the seat that
    /// refused — which is the whole of what the veto was for (RULES.md §4.5, §5.1).
    /// </summary>
    [Fact]
    public void ARefusedClaimDoesNotCloseTheRankAgainstTheSeatThatRefusedIt()
    {
        var round = Play(Claiming(), Refusing(), Deal().Give(3, "3D"));
        var ban = round.Table.SeatOf(Alice).MayNotBeFed;

        Assert.True(ban.IsEmpty);
        Assert.False(ban.Closes(Hands.Value("3D")));
    }

    /// <summary>
    /// ✅ <b>And an allowed claim does arm it</b>, which is the reason the permission is asked in
    /// the first place: Dan may not throw a three to Alice for the rest of the round (RULES.md
    /// §4.5, §5.1).
    /// </summary>
    [Fact]
    public void AnAllowedClaimClosesTheRankAgainstTheSeatThatAllowedIt()
    {
        var round = Play(Claiming(), Passive(), Deal().Give(3, "3D"));
        var ban = round.Table.SeatOf(Alice).MayNotBeFed;

        Assert.True(round.Table.TurnedUpFromTopClaimed);
        Assert.True(ban.Closes(Hands.Value("3D")));
    }

    /// <summary>
    /// 🔥 <b>The refusal is public, and it is the first thing a player tells the table about
    /// their own hand.</b> Only a holder may refuse, so the narration says Dan is holding a three
    /// — disclosed by the player rather than leaked by the engine (RULES.md §4.5, §10 #18).
    /// </summary>
    [Fact]
    public void ARefusalIsNarratedToTheWholeTable()
    {
        var observer = new RecordingObserver();
        var round = Play(Claiming(), Refusing(), Deal().Give(3, "3D"), observer);

        var refusal = Assert.Single(observer.Refusals);

        Assert.Equal(Dan, refusal.Objector);
        Assert.Equal(Alice, refusal.Claimant);
        Assert.Equal(round.Table.TurnedUpFromTop.Id, refusal.Card.Id);
    }

    /// <summary>
    /// ✅ <b>Nobody is asked when nobody claims.</b> The permission belongs to the claim and not
    /// to the deal (RULES.md §4.5).
    /// </summary>
    [Fact]
    public void NobodyIsAskedWhenTheOpenerDoesNotWantTheCard()
    {
        var dan = Passive();
        Play(Passive(), dan, Deal().Give(3, "3D"));

        Assert.Empty(dan.Objections);
    }

    /// <summary>
    /// ✅ <b>The objection test is <c>Card.SameRankAs</c> and it is one method, not two</b>
    /// (RULES.md §9 #30). Rank alone: another suit locks, another value does not, and a joker
    /// answers for the other jokers because a joker's rank is null.
    /// </summary>
    [Theory]
    [InlineData("3C", "3D", true)]
    [InlineData("3C", "3C", true)]
    [InlineData("3C", "4C", false)]
    [InlineData("3C", "RJ", false)]
    [InlineData("RJ", "BJ", true)]
    [InlineData("RJ", "3C", false)]
    public void OnlyAHolderOfThatRankMayRefuse(string claimed, string held, bool mayRefuse)
    {
        var request = new ClaimRequest(Alice, Hands.Value(claimed));

        Assert.Equal(mayRefuse, request.MayBeRefusedBy([Hands.Value(held)]));
    }

    /// <summary>An empty hand refuses nothing, which is the degenerate case of the same rule.</summary>
    [Fact]
    public void AHandHoldingNothingRefusesNothing()
    {
        Assert.False(new ClaimRequest(Alice, Hands.Value("3C")).MayBeRefusedBy([]));
    }

    /// <summary>Alice takes the turned-up card if she is allowed to, and throws her spare 2♣.</summary>
    private static ScriptedPlayerAgent Claiming() =>
        new(new ScriptedTurn { Claim = true, Discard = "2C" });

    /// <summary>Dan, refusing whatever he is asked to permit.</summary>
    private static ScriptedPlayerAgent Refusing() => new() { Objects = true };

    private static ScriptedPlayerAgent Passive() => ScriptedPlayerAgent.Passive();

    /// <summary>
    /// 🔥 <b>Bob ends the round, and that is what makes these tests comparable.</b> He is dealt
    /// thirteen cards that already win, so on turn 2 he draws, throws back what he drew and goes
    /// out — <b>whichever way the permission went</b>. A round whose ending depended on Alice's
    /// claim could not be run twice and compared, because the claim is the thing under test.
    /// </summary>
    private static ScriptedPlayerAgent EndsTheRound() =>
        new(new ScriptedTurn(), new ScriptedTurn { Declare = true });

    /// <summary>Alice with one nameable card to throw; Bob with the hand that ends the round.</summary>
    private static DealBuilder Deal() =>
        DealBuilder.ForPlayers(4).Give(0, "2C").Give(1, WinningHand);

    private static RoundEngine Play(
        IPlayerAgent alice,
        IPlayerAgent dan,
        DealBuilder deal,
        IGameObserver? observer = null)
    {
        var agents = new Dictionary<PlayerId, IPlayerAgent>
        {
            [Alice] = alice,
            [Bob] = EndsTheRound(),
            [Carol] = ScriptedPlayerAgent.Passive(),
            [Dan] = dan
        };

        var engine = new RoundEngine(
            FourPlayers,
            agents,
            Stakes.Standard,
            deal.TurnUpFromTop("3C").TurnUpFromBottom("4C").Build(),
            new Random(1),
            observer: observer);

        engine.Play();
        return engine;
    }
}
