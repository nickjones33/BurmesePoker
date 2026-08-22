using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// RULES.md §5.1: you may not discard a rank the next player has taken in the open.
/// </summary>
/// <remarks>
/// <para>
/// <b>The rule as a set of ranks, tested where it is decided.</b> Everything §5.1 turns on —
/// which ranks are closed, what re-opens one, and what is left throwable — is
/// <see cref="FeedingBan"/>, so most of this file needs no round around it. What does need a
/// round is the wiring: which takes arm it, which seat it binds, and that a reshuffle cannot
/// wipe a release. Those are in <c>RoundEngineTests</c> and at the bottom here.
/// </para>
/// <para>
/// ⚠️ <b>Four-handed throughout, because that is the only table a round may be dealt for</b>
/// (RULES.md §10 #7). The rule has no player-count branch (§9 #25), so a table size is never the
/// variable in any of this.
/// </para>
/// </remarks>
public class FeedingBanTests
{
    private static readonly TableRules FourHanded = TableRules.For(4);

    [Fact]
    public void NothingTakenInTheOpenLeavesTheWholeHandThrowable()
    {
        var hand = Hands.Of("2H", "3H", "4H", "QD", "QC", "KS", "7D", "9C", "10C", "JC", "AS", "5D", "6D", "RJ");

        // The ordinary turn, and it costs nothing at all: the hand itself comes back, not a copy
        // of it.
        Assert.Same(hand, new FeedingBan().LegalDiscards(hand, FourHanded));
    }

    [Fact]
    public void ATakeInTheOpenClosesEveryQueenAndOnlyTheQueens()
    {
        var ban = new FeedingBan();
        ban.TookInTheOpen(Hands.Value("QD"));

        // 🔥 Rank alone. This is the assertion that fails if somebody reaches for SameValueAs:
        // the Q♣ Mya Lay actually objected to is the second one here, and it is banned.
        Assert.True(ban.Closes(Hands.Value("QD")));
        Assert.True(ban.Closes(Hands.Value("QC")));
        Assert.True(ban.Closes(Hands.Value("QH")));
        Assert.False(ban.Closes(Hands.Value("KD")));
        Assert.False(ban.Closes(Hands.Value("JD")));
        Assert.False(ban.Closes(Hands.Value("RJ")));
    }

    [Fact]
    public void TheBanAccumulatesAndIsTrackedPerRank()
    {
        var ban = new FeedingBan();
        ban.TookInTheOpen(Hands.Value("QD"));
        ban.TookInTheOpen(Hands.Value("5S"));

        var hand = Hands.Of("2H", "3H", "4H", "QD", "QC", "5H", "5C", "9C", "10C", "JC", "AS", "KD", "6D", "RJ");
        var legal = ban.LegalDiscards(hand, FourHanded);

        Assert.Equal(2, ban.Count);
        Assert.DoesNotContain(legal, card => card.Rank is Rank.Queen or Rank.Five);
        Assert.Equal(10, legal.Count);

        // What is left is left in the hand's own order — the ordering every seed published before
        // this packet was dealt against (CoverScore.Ranking's stable sort).
        Assert.Equal(
            hand.Where(card => card.Rank is not (Rank.Queen or Rank.Five)).ToArray(),
            legal.ToArray());
    }

    /// <remarks>
    /// ⚠️ <b><c>PLAYER</c>, not <c>EXPERT</c></b> (RULES.md §9 #27, *"other jokers I'd assume"*). A
    /// joker has no rank for the rule to bite on, and this is the reading that changes least. If it
    /// is ever put to a player and comes back differently, it is a rule change and this test moves.
    /// </remarks>
    [Fact]
    public void TakingAJokerClosesTheOtherJokersAndNothingElse()
    {
        var ban = new FeedingBan();
        ban.TookInTheOpen(Hands.Value("RJ"));

        // Both colours: a red joker taken closes the black ones too — all four (§9 #27).
        Assert.True(ban.Closes(Hands.Value("RJ")));
        Assert.True(ban.Closes(Hands.Value("BJ")));
        Assert.False(ban.Closes(Hands.Value("QD")));
        Assert.False(ban.Closes(Hands.Value("AS")));
    }

    [Fact]
    public void ThrowingTheRankBackOpensItForTheRestOfTheRound()
    {
        var ban = new FeedingBan();
        ban.TookInTheOpen(Hands.Value("QD"));
        Assert.True(ban.Closes(Hands.Value("QC")));

        ban.ThrewAway(Hands.Value("QH"));

        Assert.False(ban.Closes(Hands.Value("QC")));
        Assert.True(ban.HasReleased(Hands.Value("QS")));
    }

    [Fact]
    public void AReleasedRankNeverClosesAgainEvenIfAnotherIsTakenInTheOpen()
    {
        var ban = new FeedingBan();

        // Discarding a Queen is a public statement that you are not collecting Queens, and it is
        // not retracted by a later pickup (RULES.md §5.1, exception 1).
        ban.ThrewAway(Hands.Value("QH"));
        ban.TookInTheOpen(Hands.Value("QD"));

        Assert.False(ban.Closes(Hands.Value("QD")));
        Assert.True(ban.IsEmpty);
    }

    /// <remarks>
    /// The floor (RULES.md §5.1, <c>PLAYER</c>, Settled). It needs all fourteen cards to be of
    /// banned ranks, and <b>two ranks are enough</b> — two decks put eight copies of a rank in the
    /// shoe — so it is vanishingly rare and not impossible.
    /// </remarks>
    [Fact]
    public void TheBanYieldsWhereItWouldLeaveNoLegalDiscardAtAll()
    {
        var ban = new FeedingBan();
        ban.TookInTheOpen(Hands.Value("QD"));
        ban.TookInTheOpen(Hands.Value("5S"));

        var hand = Hands.Of("QD", "QC", "QH", "QS", "QD", "QC", "QH", "5S", "5H", "5C", "5D", "5S", "5H", "5C");
        var legal = ban.LegalDiscards(hand, FourHanded);

        // Every card is banned, so the ban gives way rather than the turn: the discard is
        // mandatory (§7.1) and the ban is not.
        Assert.Equal(hand.Count, legal.Count);
        Assert.Same(hand, legal);
    }

    [Fact]
    public void TheYieldIsForThatTurnOnlyAndReleasesNothing()
    {
        var ban = new FeedingBan();
        ban.TookInTheOpen(Hands.Value("QD"));

        var unthrowable = Hands.Of("QD", "QC", "QH", "QS", "QD", "QC", "QH", "QS", "QD", "QC", "QH", "QS", "QD", "QC");
        Assert.Same(unthrowable, ban.LegalDiscards(unthrowable, FourHanded));

        // Draw a legal card next turn and the closed rank is closed again.
        var ordinary = Hands.Of("QD", "QC", "QH", "QS", "QD", "QC", "QH", "QS", "QD", "QC", "QH", "QS", "QD", "2H");
        Assert.Equal([Hands.Value("2H")], ban.LegalDiscards(ordinary, FourHanded).Select(card => card).ToArray(), ByValue);
        Assert.True(ban.Closes(Hands.Value("QC")));
        Assert.False(ban.HasReleased(Hands.Value("QC")));
    }

    /// <remarks>
    /// Exception 2 (RULES.md §5.1). <b>The ban never stands between a player and a win</b>, and it
    /// costs nothing: the round ends on that discard, so the protected player never gets a turn in
    /// which to take the card.
    /// </remarks>
    [Fact]
    public void ABannedRankMayBeThrownWhenItIsTheDeclaringDiscard()
    {
        var ban = new FeedingBan();
        ban.TookInTheOpen(Hands.Value("QD"));

        // Thirteen that partition exactly — 2♥–7♥, 8♦9♦10♦, four kings — plus one Queen, which is
        // banned and is also the only card that can be thrown to go out.
        var hand = Hands.Of(
            "2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "KD", "QC");

        var legal = ban.LegalDiscards(hand, FourHanded);

        Assert.Contains(legal, card => card.Rank == Rank.Queen);
        Assert.Equal(hand.Count, legal.Count);
    }

    [Fact]
    public void ABannedRankThatIsNotTheDeclaringDiscardStaysBannedEvenOnAWinningHand()
    {
        var ban = new FeedingBan();
        ban.TookInTheOpen(Hands.Value("KD"));

        // The same near-win, but the closed rank is inside the melds: throwing a King breaks the
        // set and leaves twelve melding, so exception 2 does not reach it and the Q♣ is the throw.
        var hand = Hands.Of(
            "2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "KD", "QC");

        var legal = ban.LegalDiscards(hand, FourHanded);

        Assert.DoesNotContain(legal, card => card.Rank == Rank.King);
        Assert.Equal(10, legal.Count);
    }

    /// <remarks>
    /// ✅ <b>R7 — exception 2 asks the table's own win condition, not a win condition</b>
    /// (RULES.md §5.1, §7.1.1). The two tests above use a hand that wins at four seats and at
    /// five alike, so a <c>LegalDiscards</c> that hard-coded <c>TableRules.For(5)</c> would have
    /// passed them both — while at a real four-handed table it would offer a banned card whose
    /// throw leaves thirteen that cover but hold no clean series, which is not a declaring
    /// discard there and so is not legal at all.
    /// </remarks>
    [Fact]
    public void TheDeclaringDiscardExceptionAsksTheTablesOwnWinCondition()
    {
        var ban = new FeedingBan();
        ban.TookInTheOpen(Hands.Value("JD"));

        // Throwing the banned J♣ leaves thirteen that partition into sets alone: a winning
        // hand at five seats, where §7.1.1 requires no series, and a losing one at four,
        // where the one required series must exist and be clean.
        var hand = Hands.Of(
            "KC", "KH", "KS", "QC", "QH", "QS", "5C", "5H", "5S", "9C", "9H", "9S", "9D", "JC");

        Assert.DoesNotContain(ban.LegalDiscards(hand, FourHanded), card => card.Rank == Rank.Jack);
        Assert.Contains(ban.LegalDiscards(hand, TableRules.For(5)), card => card.Rank == Rank.Jack);
    }

    /// <remarks>
    /// ✅ RULES.md §9 #13: a player may throw back the card they just took *"as long as you aren't
    /// violating any other discard rules"* — and §5.1 is the only other discard rule there is. So
    /// the just-taken card is filtered like any other and <b>needs no case of its own anywhere</b>.
    /// </remarks>
    [Fact]
    public void TheCardJustTakenIsFilteredLikeAnyOther()
    {
        var hand = Hands.Of("2H", "3H", "4H", "8D", "9D", "10D", "KC", "KH", "KS", "5C", "6C", "7C", "9H", "QC");
        var justTaken = hand[^1];

        var context = TurnContexts.Fed(hand, Hands.Value("QD"));

        Assert.Equal(justTaken, context.Taken);
        Assert.DoesNotContain(context.LegalDiscards, card => card.Id == justTaken.Id);

        // …and with nothing closed, the very same card is throwable. There is no special case in
        // either direction.
        Assert.Contains(TurnContexts.Holding(hand).LegalDiscards, card => card.Id == justTaken.Id);
    }

    [Fact]
    public void ATurnIsBoundByTheSeatItFeedsAndNotByTheSeatThatFeedsIt()
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(seat => new PlayerId(seat))];
        var shoe = Deck.TwoDecks();
        var table = new TableState(
            players, Stakes.Standard, shoe, shoe.Cards, Hands.Value("3C"), Hands.Value("4C"));

        // Seat 0 discards to seat 1, so only seat 1's takes bind seat 0 (RULES.md §9 #16).
        Assert.Equal(players[1], table.SeatFedBy(players[0]).Id);
        Assert.Equal(players[0], table.SeatFedBy(players[3]).Id);

        table.SeatOf(players[3]).TookInTheOpen(Hands.Value("QD"));

        Assert.True(table.SeatFedBy(players[2]).MayNotBeFed.Closes(Hands.Value("QC")));
        Assert.False(table.SeatFedBy(players[0]).MayNotBeFed.Closes(Hands.Value("QC")));
    }

    /// <remarks>
    /// ⚠️ <b>Two-handed the seat that feeds you is the seat you feed</b>, and the mutual lock that
    /// produces is a legal state of the game rather than a defect (RULES.md §5.1, Two-handed;
    /// §9 #25). The rule has no player-count branch, and this asserts that there is none in the
    /// code either — even though <c>RoundEngine.MinimumPlayers</c> is still 4, so no dealt game
    /// reaches it (§10 #7).
    /// </remarks>
    [Fact]
    public void TwoHandedEachSeatIsBothTheFeederAndTheFed()
    {
        var players = (IReadOnlyList<PlayerId>)[new PlayerId(0), new PlayerId(1)];
        var shoe = Deck.TwoDecks();
        var table = new TableState(
            players, Stakes.Standard, shoe, shoe.Cards, Hands.Value("3C"), Hands.Value("4C"));

        Assert.Equal(players[1], table.SeatFedBy(players[0]).Id);
        Assert.Equal(players[0], table.SeatFedBy(players[1]).Id);

        // A throws a Queen, B takes it → Queens closed to A. Later B throws one, A takes it →
        // Queens closed to B. Two bans, and neither release can happen.
        table.SeatOf(players[1]).TookInTheOpen(Hands.Value("QD"));
        table.SeatOf(players[0]).TookInTheOpen(Hands.Value("QH"));

        Assert.True(table.SeatFedBy(players[0]).MayNotBeFed.Closes(Hands.Value("QS")));
        Assert.True(table.SeatFedBy(players[1]).MayNotBeFed.Closes(Hands.Value("QS")));
    }

    /// <summary>Value equality, so a test can name a card without naming its instance.</summary>
    private static readonly IEqualityComparer<Card> ByValue = new SameValue();

    private sealed class SameValue : IEqualityComparer<Card>
    {
        public bool Equals(Card one, Card other) => one.SameValueAs(other);

        public int GetHashCode(Card card) => HashCode.Combine(card.Rank, card.Suit, card.Color);
    }
}
