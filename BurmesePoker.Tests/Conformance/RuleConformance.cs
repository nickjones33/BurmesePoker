using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Conformance;

/// <summary>
/// An <see cref="IGameObserver"/> that audits a round as it is played, asserting every Settled
/// rule of <c>RULES.md</c> it can see from the narration and the table (packet P30.2).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Everything else in this suite proves a rule <em>can</em> hold; this proves it
/// <em>does</em>.</b> Every rules test before this packet was a scripted fixture — a deal built
/// card by card to put one situation in front of the engine — or a unit test on a helper. What
/// no test asserted is that ordinary rounds, dealt at random and played by the rungs a person
/// actually meets, never once break anything. P21's concealment tests were asserting a property
/// of round length without knowing it, and P28's <c>JournalFormat.Name</c> mistranslated a
/// question for as long as the format had a default arm — both the shape of defect only an
/// audit of ordinary play can catch.
/// </para>
/// <para>
/// <b>The checks are deliberately independent re-derivations, not calls into the code under
/// audit.</b> The feeding ban is mirrored here from public events alone (a take closes, a
/// throw-back releases) rather than read off <see cref="PlayerState.MayNotBeFed"/>; the
/// settlement is recomputed from ownership and the turn-up without <see cref="Settlement"/> or
/// <see cref="MoneyCardRegistry"/>; a meld is validated against §6 directly rather than by
/// asking the generators. An audit that asked the engine whether the engine is right would be
/// vacuous — and <see cref="RuleConformanceTests"/> proves each family of checks can go red.
/// </para>
/// <para>
/// <b>Cheap by construction</b> (BUILD-PLAN §3.8 item 3): integers, ids and set lookups only —
/// no formatting, no hand copies beyond the per-event id sweep the invariant demands.
/// </para>
/// <para>
/// Usage: construct, pass as the engine's observer, call <see cref="Watch"/> with the engine's
/// table (the deal is audited there), <c>Play()</c>, then <see cref="RoundIsSettled"/>.
/// </para>
/// </remarks>
internal sealed class RuleConformance : IGameObserver
{
    private TableState? _table;
    private IReadOnlyList<PlayerId> _seating = [];
    private IReadOnlyList<Card> _turnedUp = [];

    /// <summary>The ownership already checked: write-once, so nothing here may ever change.</summary>
    private readonly Dictionary<CardId, PlayerId> _ownershipSeen = [];

    /// <summary>The ban, mirrored per protected seat from public events alone (RULES.md §5.1).</summary>
    private readonly Dictionary<PlayerId, HashSet<Rank?>> _closed = [];
    private readonly Dictionary<PlayerId, HashSet<Rank?>> _released = [];

    private int _takes;
    private int _discards;
    private bool _claimed;
    private bool _claimRefused;
    private PlayerId? _declaredBy;
    private RoundResult? _settled;

    /// <summary>A closed-rank discard waiting for the declaration that licenses it (§5.1 exc. 2).</summary>
    private (PlayerId Player, Card Card, bool FloorApplied)? _pendingClosedDiscard;

    /// <summary>How many turns the audit saw completed.</summary>
    public int Turns => _discards;

    /// <summary>Starts auditing the given table. Call straight after the engine is constructed.</summary>
    public void Watch(TableState table)
    {
        _table = table;

        // §4.4: the deal confers ownership — every dealt card is owned by the seat holding it,
        // and nothing else is owned yet (the turned-up cards are owned by nobody).
        var records = table.Ownership.Records;
        Assert.Equal(table.Seats.Sum(seat => seat.Hand.Count), records.Count);

        foreach (var seat in table.Seats)
        {
            Assert.Equal(RoundEngine.HandSize, seat.Hand.Count);
            Assert.All(seat.Hand, card => Assert.Equal(seat.Id, records[card.Id]));
        }

        foreach (var (card, owner) in records)
        {
            _ownershipSeen[card] = owner;
        }

        Assert.All(_turnedUp, card => Assert.False(records.ContainsKey(card.Id)));
        EverythingHolds(allowedNewOwnership: null);
    }

    public void RoundStarted(int round, IReadOnlyList<PlayerId> seating, IReadOnlyList<Card> turnedUp)
    {
        _seating = [.. seating];
        _turnedUp = [.. turnedUp];

        // §3 step 4: two money cards are turned up. §2: between two and six play — the engine's
        // own floor of four is the recorded divergence (§10 #7), so the audit takes the wider rule.
        Assert.Equal(2, turnedUp.Count);
        Assert.InRange(seating.Count, TableRules.SmallestTable, RoundEngine.MaximumPlayers);
        Assert.Equal(seating.Count, seating.Distinct().Count());

        foreach (var player in seating)
        {
            _closed[player] = [];
            _released[player] = [];
        }
    }

    public void PlayerDrew(PlayerId player, Card card)
    {
        ResolvePendingClosedDiscard(declaredBy: null);
        BeginTake(player);

        // §4.4: a blind draw confers ownership — unless the card left the deck once before,
        // in which case first acquisition wins (§5) and no record moves.
        EverythingHolds(allowedNewOwnership: (card.Id, player));
        AfterTake(player);
    }

    public void PlayerTookDiscard(PlayerId player, Card card)
    {
        ResolvePendingClosedDiscard(declaredBy: null);
        BeginTake(player);

        // §5.1: a public take closes that rank against the seat that discards to the taker —
        // unless the taker has already thrown that rank back, which released it for good.
        TookInTheOpen(player, card);

        // §4.4: a take confers no ownership, ever.
        EverythingHolds(allowedNewOwnership: null);
        AfterTake(player);
    }

    public void MoneyCardClaimed(PlayerId player, Card card)
    {
        ResolvePendingClosedDiscard(declaredBy: null);
        BeginTake(player);

        // §4.5: only on the opening turn, only by the opener, only once, and only the card
        // turned up from the top — and never after a refusal.
        Assert.Equal(0, _discards);
        Assert.Equal(_seating[0], player);
        Assert.False(_claimed, "The turned-up money card was claimed twice.");
        Assert.False(_claimRefused, "A refused claim was taken anyway (RULES.md §4.5).");
        Assert.Equal(Table.TurnedUpFromTop.Id, card.Id);
        _claimed = true;

        // The physical card leaves the table (RULES.md §4.5) — one designator remains.
        Assert.Single(Table.TurnedUpOnTable);

        // §5.1 + §4.5: a claim is a public take and arms the ban like one.
        TookInTheOpen(player, card);

        // §4.4: a claimed card is held but never owned.
        EverythingHolds(allowedNewOwnership: null);
        AfterTake(player);
    }

    public void ClaimRefused(PlayerId objector, PlayerId claimant, Card card)
    {
        ResolvePendingClosedDiscard(declaredBy: null);

        // §4.5: the veto belongs to the seat that plays before the claimant — the last seat of
        // the round — is exercised at most once, on the opening turn, before anything is taken.
        Assert.Equal(0, _takes);
        Assert.Equal(0, _discards);
        Assert.Equal(_seating[0], claimant);
        Assert.Equal(_seating[^1], objector);
        Assert.False(_claimed);
        Assert.False(_claimRefused);
        Assert.Equal(Table.TurnedUpFromTop.Id, card.Id);
        _claimRefused = true;

        // §4.5, §10 #18: only a holder of that rank may refuse — the objection disclosed it.
        Assert.Contains(Table.SeatOf(objector).Hand, held => held.Rank == card.Rank);

        // A refused claim leaves both cards on the table and arms nothing (asserted by the
        // ban mirror simply not closing the rank here).
        Assert.Equal(2, Table.TurnedUpOnTable.Count);
        EverythingHolds(allowedNewOwnership: null);
    }

    public void PlayerDiscarded(PlayerId player, Card card)
    {
        ResolvePendingClosedDiscard(declaredBy: null);

        // §5: one taken, one discarded, every turn — and by the seat whose turn it is.
        Assert.Equal(_discards + 1, _takes);
        Assert.Equal(_seating[_discards % _seating.Count], player);
        _discards++;

        // §5.1: a discard is never a closed rank unless the floor applied — the whole hand was
        // closed — or the round ended on it (exception 2). The declaration, if it is coming,
        // is the next event; judgment is deferred until it either arrives or does not.
        var protectedSeat = FeedsInto(player);

        if (_closed[protectedSeat].Contains(card.Rank))
        {
            // The floor fires only where the whole fourteen was closed; the thrown card is
            // closed by the branch above, so the thirteen left behind settle it.
            var floorApplied = Table.SeatOf(player).Hand
                .All(held => _closed[protectedSeat].Contains(held.Rank));
            _pendingClosedDiscard = (player, card, floorApplied);
        }

        // §5.1 exception 1: the protected player throwing a rank releases it, permanently.
        _released[player].Add(card.Rank);
        _closed[player].Remove(card.Rank);

        // §4.4: discarding moves no ownership.
        EverythingHolds(allowedNewOwnership: null);

        // Between turns every seat is back to thirteen (§5).
        Assert.All(Table.Seats, seat => Assert.Equal(RoundEngine.HandSize, seat.Hand.Count));
    }

    public void DiscardsReshuffled(int cards)
    {
        ResolvePendingClosedDiscard(declaredBy: null);

        // §5: the gathered discards are the new draw pile, whole; §9 #4: the turned-up cards
        // stay where they are.
        Assert.True(cards > 0);
        Assert.Equal(cards, Table.DrawPileCount);
        Assert.Equal(_claimed ? 1 : 2, Table.TurnedUpOnTable.Count);
        Assert.All(Table.Seats, seat => Assert.Empty(seat.Discards));
        EverythingHolds(allowedNewOwnership: null);
    }

    public void PlayerDeclared(PlayerId player, IReadOnlyList<Meld> melds)
    {
        ResolvePendingClosedDiscard(declaredBy: player);

        // §7.1: the discard comes first and the reveal follows it — so the declarer is the seat
        // that has just completed its turn, holding thirteen again.
        Assert.Equal(_takes, _discards);
        Assert.Equal(_seating[(_discards - 1) % _seating.Count], player);
        Assert.Null(_declaredBy);
        _declaredBy = player;

        ADeclaredHandSatisfiesTheTable(melds, Table.SeatOf(player).Hand, TableRules.For(_seating.Count));
        EverythingHolds(allowedNewOwnership: null);
    }

    public void RoundSettled(RoundResult result)
    {
        Assert.NotNull(_declaredBy);
        Assert.Equal(_declaredBy, result.Winner);
        Assert.Equal(_discards, result.Turns);
        _settled = result;

        TheSettlementIsTheRules(
            result.Payouts, result.Winner, _seating, Table.Stakes, _turnedUp,
            Table.Ownership.Records, Table.Shoe);
        EverythingHolds(allowedNewOwnership: null);
    }

    /// <summary>The audit's own end: the round really settled and nothing is left pending.</summary>
    public void RoundIsSettled()
    {
        Assert.NotNull(_settled);
        Assert.Null(_pendingClosedDiscard);
    }

    /// <summary>
    /// Re-checks the standing invariants right now, outside any event — how the non-vacuity
    /// tests show that conservation and write-once ownership can actually go red.
    /// </summary>
    public void AuditNow() => EverythingHolds(allowedNewOwnership: null);

    private TableState Table => _table ?? throw new InvalidOperationException(
        "Watch(table) was never called — the audit has nothing to audit.");

    private void BeginTake(PlayerId player)
    {
        // §5: the previous turn must be complete, and it is this seat's turn.
        Assert.Equal(_discards, _takes);
        Assert.Equal(_seating[_discards % _seating.Count], player);
        _takes++;
    }

    private void AfterTake(PlayerId player)
    {
        // §5: fourteen during the turn, thirteen everywhere else.
        Assert.Equal(RoundEngine.HandSize + 1, Table.SeatOf(player).Hand.Count);
    }

    private void TookInTheOpen(PlayerId player, Card card)
    {
        if (!_released[player].Contains(card.Rank))
        {
            _closed[player].Add(card.Rank);
        }
    }

    /// <summary>The seat <paramref name="player"/> discards to — the next round the table (§5.1).</summary>
    private PlayerId FeedsInto(PlayerId player)
    {
        for (var seat = 0; seat < _seating.Count; seat++)
        {
            if (_seating[seat] == player)
            {
                return _seating[(seat + 1) % _seating.Count];
            }
        }

        throw new InvalidOperationException($"{player} is not in the seating.");
    }

    /// <summary>
    /// A closed-rank discard is judged once the next event says whether the round ended on it.
    /// </summary>
    private void ResolvePendingClosedDiscard(PlayerId? declaredBy)
    {
        if (_pendingClosedDiscard is not { } pending)
        {
            return;
        }

        _pendingClosedDiscard = null;

        if (declaredBy == pending.Player)
        {
            return; // §5.1 exception 2: the round ended on it.
        }

        Assert.True(
            pending.FloorApplied,
            $"{pending.Player} threw {pending.Card}, a rank closed against them, without declaring "
            + "and without the floor applying (RULES.md §5.1).");
    }

    /// <summary>
    /// The invariants that hold at every moment an observer can look: 108 distinct cards
    /// wherever they sit (§2), and ownership write-once, conferred only by the deck (§4.4).
    /// </summary>
    private void EverythingHolds((CardId Card, PlayerId Owner)? allowedNewOwnership)
    {
        var seen = 0;
        var distinct = new HashSet<CardId>();

        foreach (var card in Table.AllCards)
        {
            seen++;
            distinct.Add(card.Id);
        }

        Assert.Equal(DeckBuilder.TotalCards, seen);
        Assert.Equal(DeckBuilder.TotalCards, distinct.Count);

        var records = Table.Ownership.Records;

        foreach (var (card, owner) in records)
        {
            if (_ownershipSeen.TryGetValue(card, out var recorded))
            {
                Assert.Equal(recorded, owner);
            }
            else
            {
                Assert.True(
                    allowedNewOwnership is { } allowed && allowed.Card == card && allowed.Owner == owner,
                    $"Ownership of card {card} appeared on an event that confers none (RULES.md §4.4).");
                _ownershipSeen[card] = owner;
            }
        }
    }

    /// <summary>
    /// §6 and §7.1.1, re-derived: the melds partition exactly the thirteen held, disjoint by
    /// <see cref="CardId"/>; every run is one suit and consecutive with the ace never wrapped;
    /// every set is one rank with no suit twice; and the partition holds the series — and the
    /// clean series — the table size requires.
    /// </summary>
    internal static void ADeclaredHandSatisfiesTheTable(
        IReadOnlyList<Meld> melds, IReadOnlyList<Card> held, TableRules rules)
    {
        var used = new HashSet<CardId>();

        foreach (var meld in melds)
        {
            Assert.True(meld.Count >= 3, "A meld holds at least three cards (RULES.md §6).");
            Assert.All(meld.Slots, slot => Assert.True(used.Add(slot.Card.Id), "Melds overlap (§6.3)."));

            switch (meld.Kind)
            {
                case MeldKind.Run:
                    AValidRun(meld);
                    break;

                case MeldKind.Set:
                    Assert.True(rules.SetsAllowed, "A set is not a legal meld at this table (§7.1.1).");
                    AValidSet(meld);
                    break;

                default:
                    Assert.Fail($"A meld of unknown kind: {meld.Kind}.");
                    break;
            }
        }

        // §7.1: all thirteen, and exactly the thirteen the declarer is holding.
        Assert.Equal(RoundEngine.HandSize, used.Count);
        Assert.Equal(RoundEngine.HandSize, held.Count);
        Assert.True(used.SetEquals(held.Select(card => card.Id)), "The melds are not the hand held (§7.1).");

        // §7.1.1: the series the table size requires, of which the required number clean —
        // clean meaning a run with no joker standing in anywhere (§9 #29).
        var series = melds.Count(meld => meld.Kind == MeldKind.Run);
        var clean = melds.Count(meld => meld.Kind == MeldKind.Run && meld.Cards.All(card => !card.IsJoker));

        Assert.True(
            series >= rules.RequiredSeries,
            $"{series} series declared where {rules} requires {rules.RequiredSeries}.");
        Assert.True(
            clean >= rules.RequiredCleanSeries,
            $"{clean} clean series declared where {rules} requires {rules.RequiredCleanSeries}.");
    }

    /// <summary>§6.1 re-derived: one suit, consecutive, and an ace is high or low, never both.</summary>
    private static void AValidRun(Meld run)
    {
        var suits = run.Slots.Select(slot => slot.InSuit).Distinct().ToList();
        Assert.True(suits.Count == 1, $"A run holds one suit, not {suits.Count} ({run}).");
        Assert.All(run.Slots, slot => Assert.True(
            slot.IsSubstitute || (slot.Card.Rank == slot.PlaysAs && slot.Card.Suit == slot.InSuit),
            $"A ranked card plays as itself ({run})."));

        // Consecutive under exactly one reading of the ace: high (14), or low (1, at the start
        // of an ascending window). K-A-2 fits neither and fails both (§6.1).
        var played = run.Slots.Select(slot => (int)slot.PlaysAs).ToList();

        if (played[0] > played[^1])
        {
            played.Reverse();
        }

        Assert.True(
            Consecutive(played) || Consecutive([.. played.Select(rank => rank == (int)Rank.Ace ? 1 : rank)]),
            $"A run must be consecutive with the ace never wrapping ({run}).");
    }

    private static bool Consecutive(List<int> values)
    {
        // An ace played low sorts to the front under the substitution above.
        values.Sort();

        for (var at = 1; at < values.Count; at++)
        {
            if (values[at] != values[at - 1] + 1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>§6.2 re-derived: one rank, no suit twice — so at most four cards.</summary>
    private static void AValidSet(Meld set)
    {
        Assert.True(
            set.Slots.Select(slot => slot.PlaysAs).Distinct().Count() == 1,
            $"A set holds one rank ({set}).");
        Assert.All(set.Slots, slot => Assert.True(
            slot.IsSubstitute || (slot.Card.Rank == slot.PlaysAs && slot.Card.Suit == slot.InSuit),
            $"A ranked card plays as itself ({set})."));
        Assert.True(
            set.Slots.Select(slot => slot.InSuit).Distinct().Count() == set.Count,
            $"A set may not hold a suit twice ({set}), so four cards is its ceiling (§6.2).");
    }

    /// <summary>
    /// §4.1, §4.3 and §7.2, re-derived without <see cref="Settlement"/> or
    /// <see cref="MoneyCardRegistry"/>: every loser pays the winner the flat round value and
    /// nothing more — no deadwood penalty however many cards a hand held — and every owned
    /// money card pays its owner per opponent, at ×1, ×3 on a designated permanent value, or
    /// ×5 where one player owns both partners of a 7♦/A♠ turn-up.
    /// </summary>
    internal static void TheSettlementIsTheRules(
        IReadOnlyDictionary<PlayerId, int> payouts,
        PlayerId winner,
        IReadOnlyList<PlayerId> players,
        Stakes stakes,
        IReadOnlyList<Card> turnedUp,
        IReadOnlyDictionary<CardId, PlayerId> ownership,
        IReadOnlyList<Card> shoe)
    {
        var expected = players.ToDictionary(player => player, _ => 0);

        // §7.2 step 1: the flat round payment. No per-card penalty exists, so this is the whole
        // of what losing costs.
        foreach (var player in players)
        {
            if (player != winner)
            {
                expected[player] -= stakes.RoundValue;
                expected[winner] += stakes.RoundValue;
            }
        }

        // §4.1's jackpot, independently: the turn-up is the 7♦/A♠ pair and one player owns both
        // partners — the partner being the copy of that value not lying on the table.
        var jackpotHolder = JackpotHolder(turnedUp, ownership, shoe);

        // §7.2 step 2: each owned money card pays its owner from every other player (§4.3).
        foreach (var (id, owner) in ownership)
        {
            var card = shoe[id.Value];
            var multiplier = MultiplierOf(card, turnedUp);

            if (multiplier == 3 && jackpotHolder == owner)
            {
                multiplier = 5;
            }

            foreach (var player in players)
            {
                if (player != owner)
                {
                    expected[player] -= multiplier * stakes.MoneyCardValue;
                    expected[owner] += multiplier * stakes.MoneyCardValue;
                }
            }
        }

        Assert.Equal(0, payouts.Values.Sum());
        Assert.Equal(players.Count, payouts.Count);

        foreach (var player in players)
        {
            Assert.Equal(expected[player], payouts[player]);
        }
    }

    /// <summary>
    /// §4.1 re-derived: a permanent value (7♦, A♠, any joker) pays 1; a designated value pays
    /// 1; a designation landing on a permanent value pays 3. A turned-up card itself is owned
    /// by nobody, so it never reaches this method through an ownership record.
    /// </summary>
    private static int MultiplierOf(Card card, IReadOnlyList<Card> turnedUp)
    {
        var permanent = card.IsJoker
            || (card.Rank == Rank.Seven && card.Suit == Suit.Diamonds)
            || (card.Rank == Rank.Ace && card.Suit == Suit.Spades);
        var designated = turnedUp.Any(card.SameValueAs);

        return (permanent, designated) switch
        {
            (true, true) => 3,
            (false, false) => 0,
            _ => 1
        };
    }

    private static PlayerId? JackpotHolder(
        IReadOnlyList<Card> turnedUp,
        IReadOnlyDictionary<CardId, PlayerId> ownership,
        IReadOnlyList<Card> shoe)
    {
        if (turnedUp.Count != 2)
        {
            return null;
        }

        var sevenUp = turnedUp.FirstOrDefault(up => up.Rank == Rank.Seven && up.Suit == Suit.Diamonds);
        var aceUp = turnedUp.FirstOrDefault(up => up.Rank == Rank.Ace && up.Suit == Suit.Spades);

        if (sevenUp == default || aceUp == default)
        {
            return null;
        }

        PlayerId? holder = null;

        foreach (var up in new[] { sevenUp, aceUp })
        {
            var partner = shoe.Single(card => card.SameValueAs(up) && card.Id != up.Id);

            if (!ownership.TryGetValue(partner.Id, out var owner)
                || (holder is not null && holder != owner))
            {
                return null;
            }

            holder = owner;
        }

        return holder;
    }
}
