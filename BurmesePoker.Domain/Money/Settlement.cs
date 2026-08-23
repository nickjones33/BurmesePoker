using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Money;

/// <summary>
/// Turns a finished round into money movements (RULES.md §4.3, §7.2).
/// </summary>
/// <remarks>
/// <para>
/// Two independent payments, in this order:
/// </para>
/// <list type="number">
///   <item>the <b>round payment</b>: flat in what the loser <i>held</i>, with no per-card
///   penalty for unmelded cards (RULES.md §7.2), but multiplied by how and when the hand was
///   won and — on a third consecutive win — owed by one seat rather than by everybody. All of
///   that is <see cref="Win"/>, and the column it produces is
///   <see cref="RoundPayments"/> (RULES.md §7.3, §7.4, §7.5);</item>
///   <item>for each <b>owned</b> money card, its owner collects
///   <see cref="Stakes.MoneyCardValue"/> times the card's multiplier from <b>every other
///   player</b>. This settles regardless of who won, and the winner takes part in it both
///   ways (RULES.md §4.3).</item>
/// </list>
/// <para>
/// 🔥 <b>A multiplier is not a property of a card, and this is the only place that shows.</b>
/// One player owning <b>both</b> partners of a 7♦/A♠ turn-up is paid ×5 apiece where two
/// players holding one each are paid ×3 (RULES.md §4.1), so the registry is asked about a card
/// <i>and its owner</i> — under a configuration read from <see cref="CardOwnership"/> once,
/// before the walk below starts.
/// </para>
/// <para>
/// 🔥 <b>Since RULES.md rev 26 the round payment is not flat, and since rev 27 it is not even a
/// payment from everybody.</b> A <b>jokerless</b> declaration pays the winner
/// <see cref="TableRules.JokerlessMultiplier"/> times the round value (§7.3); a win from the
/// <b>initial deal</b> pays <see cref="Win.DealBonusMultiplier"/> times it (§7.4); and on a
/// <b>third consecutive win</b> the seat immediately above the winner pays the whole thing and
/// the rest pay nothing (§7.5). ⚠️ <b>The jokerless predicate is a property of the cards, not of
/// the partition</b> (<see cref="IsJokerless"/>): a jokerless hand is jokerless under every
/// cover of it, so nothing here asks an evaluator anything and <c>Meld.IsClean</c> — which is
/// §7.1.1's <i>required clean series</i>, a different rule sharing a word — is not consulted.
/// ⚠️ <b>All three reach step 1 only.</b> The money-card settlement is untouched by every one of
/// them, which is RULES.md §9 #36, #40 and #44's shared recorded default: the sayings name
/// <i>the winning prize</i> and none names the money.
/// </para>
/// <para>
/// <b>Settlement walks ownership records and holds no history.</b> Ownership is conferred only
/// by the deck and never transfers (RULES.md §4.4), so where a card now sits is irrelevant: a
/// money card its owner discarded still pays that owner, one an opponent picked up still pays
/// the player who drew it, and one nobody ever drew pays nobody. There is deliberately no table
/// or player-state parameter below. ⚠️ <b>And there is deliberately no <i>match</i> either</b>:
/// §7.5's streak is a property of a sequence of rounds, so this is <b>told</b> whether the win
/// was a third in a row rather than counting one — the same division P33 made when it passed the
/// declared thirteen in rather than deriving jokerlessness here. Counting streaks belongs to
/// whatever owns a sequence of rounds, which is <see cref="Play.MatchEngine"/>.
/// </para>
/// <para>
/// There is no score. Money is the only ledger (RULES.md §7.2).
/// </para>
/// </remarks>
public static class Settlement
{
    /// <summary>
    /// The money each player wins or loses over the round — positive to collect, negative to
    /// pay. Every player at the table appears, including those whose delta is zero, and the
    /// deltas always sum to zero.
    /// </summary>
    /// <param name="players">
    /// Everyone at the table this round. No duplicates. ⚠️ <b>The count is now load-bearing</b>:
    /// it is what <see cref="TableRules.For"/> is asked for the §7.3 multiplier, where before
    /// it only decided who paid whom.
    /// </param>
    /// <param name="winner">The player who went out. Must be one of <paramref name="players"/>.</param>
    /// <param name="stakes">The round and money card values (RULES.md §4.3).</param>
    /// <param name="moneyCards">Which card values pay this round, and how much.</param>
    /// <param name="ownership">Who the deck gave each card to. The whole basis of the side-bet.</param>
    /// <param name="shoe">
    /// The 108 cards of the shoe <b>in <see cref="DeckBuilder.BuildTwoDecks"/> order</b>, which
    /// resolves an owned <see cref="CardId"/> back to the <see cref="Card"/> the registry needs:
    /// ownership is recorded by instance, designation asks by value (BUILD-PLAN §3.1). That
    /// order is index-aligned — <c>shoe[i].Id.Value == i</c> — and is checked here, because
    /// <see cref="Deck.Cards"/> is <b>shuffled</b> and would otherwise settle the wrong cards
    /// in silence.
    /// </param>
    /// <param name="win">
    /// What this win was — jokerless (§7.3), from the initial deal (§7.4), a third consecutive
    /// win (§7.5). Required rather than optional on purpose: a default would pay flat, from
    /// everybody, in silence.
    /// </param>
    public static IReadOnlyDictionary<PlayerId, int> ForRound(
        IReadOnlyList<PlayerId> players,
        PlayerId winner,
        Stakes stakes,
        MoneyCardRegistry moneyCards,
        CardOwnership ownership,
        IReadOnlyList<Card> shoe,
        Win win)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(stakes);
        ArgumentNullException.ThrowIfNull(moneyCards);
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(shoe);
        ArgumentNullException.ThrowIfNull(win);
        RequireIndexAligned(shoe);

        // The ownership configuration, once a round rather than once a card: what a value
        // pays is a function of the turn-up, but the ×5 is a function of who owns what
        // (RULES.md §4.1). Nothing is stored on a card either way (BUILD-PLAN §3.3).
        var configuration = moneyCards.ConfigurationOf(ownership, shoe);

        var deltas = new Dictionary<PlayerId, int>(players.Count);

        foreach (var player in players)
        {
            if (!deltas.TryAdd(player, 0))
            {
                throw new ArgumentException($"{player} is at the table twice.", nameof(players));
            }
        }

        if (deltas.Count == 0)
        {
            throw new ArgumentException("There is nobody at the table.", nameof(players));
        }

        if (!deltas.ContainsKey(winner))
        {
            throw new ArgumentException($"The winner {winner} is not at the table.", nameof(winner));
        }

        // 1. The round payment: flat in what the losers held, multiplied by how and when the
        //    winner won, and — on a third consecutive win — owed by one seat rather than by
        //    everybody (RULES.md §7.2, §7.3, §7.4, §7.5). Worked out where every consumer that
        //    has to display the split can ask for it too.
        foreach (var (player, amount) in RoundPayments(players, winner, stakes, win))
        {
            deltas[player] += amount;
        }

        // 2. The money cards: pairwise, by owner, independently of who won.
        foreach (var (card, owner) in ownership.Records)
        {
            if (!deltas.ContainsKey(owner))
            {
                throw new ArgumentException(
                    $"Card {card} is owned by {owner}, who is not at the table.", nameof(ownership));
            }

            var multiplier = moneyCards.Multiplier(Resolve(shoe, card), owner, configuration);

            if (multiplier == 0)
            {
                continue;
            }

            foreach (var player in players)
            {
                if (player != owner)
                {
                    Pay(deltas, from: player, to: owner, multiplier * stakes.MoneyCardValue);
                }
            }
        }

        return deltas;
    }

    /// <summary>
    /// RULES.md §7.3's qualifying test: <b>no joker anywhere in the declared thirteen</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A joker in a <i>set</i> forfeits the bonus exactly as one in a run does.</b> Rev 25
    /// recorded the condition as <i>"all series clean"</i> and the expert corrected it the same
    /// day — <i>"not just series, if you want to win with jokerless"</i>. The narrow reading and
    /// this one agree only two-handed, where every meld is a series (§7.1.1), and separate the
    /// moment a set is legal.
    /// <para>
    /// 🔥 <b>It takes cards and not melds on purpose.</b> A hand can be covered more than one
    /// way, so a partition-shaped predicate would have to say <i>which</i> cover the winner is
    /// paid on; a joker in the thirteen is there under every cover, so the question does not
    /// arise. Passing the melds' cards instead of the hand is equivalent and equally correct —
    /// they are the same thirteen (§6.3).
    /// </para>
    /// </remarks>
    public static bool IsJokerless(IEnumerable<Card> declaredHand)
    {
        ArgumentNullException.ThrowIfNull(declaredHand);
        return !declaredHand.Any(card => card.IsJoker);
    }

    /// <summary>
    /// What <b>one</b> payer owes the winner under RULES.md §7.2 step 1, as amended by §7.3 and
    /// §7.4 — the round value, times what <see cref="Win.Multiplier"/> makes of how and when the
    /// hand was won.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>This is not what the winner collects, and since RULES.md rev 27 the difference is
    /// not just the seat count.</b> On a third consecutive win one seat owes this and the rest
    /// owe nothing (§7.5), so a consumer wanting the round column wants
    /// <see cref="RoundPayments"/> and not this.
    /// </remarks>
    public static int RoundPayment(Stakes stakes, TableRules rules, Win win)
    {
        ArgumentNullException.ThrowIfNull(stakes);
        ArgumentNullException.ThrowIfNull(win);
        return stakes.RoundValue * win.Multiplier(rules);
    }

    /// <summary>
    /// RULES.md §7.2 step 1 alone, per player: what the <b>round payment</b> moves, positive to
    /// collect, before the money cards settle on top of it. Everyone appears and the values sum
    /// to zero.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Exposed because two consumers outside the domain draw this column, and they used to
    /// re-derive it.</b> The console's settlement panel and the harness's per-seat CSV row both
    /// split a net delta into <i>the round</i> and <i>the side bet</i>, and both did it by
    /// assuming every loser paid the same amount — which stopped being true the day §7.5 was
    /// recorded. Splitting a net at the wrong place posts the difference to the side-bet column,
    /// where every money measurement reads it (BUILD-PLAN P35 build item 4).
    /// </para>
    /// <para>
    /// ⚠️ <b><paramref name="seating"/> is the round's turn order and not a set.</b> §7.5 names
    /// <em>the seat immediately above the winner</em>, so the order is load-bearing here in a way
    /// it never was before: the same players in a different order name a different payer.
    /// </para>
    /// </remarks>
    /// <param name="seating">The table in turn order (RULES.md §3 step 2).</param>
    /// <param name="winner">Who went out. Must be seated.</param>
    /// <param name="stakes">What the round was played for.</param>
    /// <param name="win">What the win was.</param>
    public static IReadOnlyDictionary<PlayerId, int> RoundPayments(
        IReadOnlyList<PlayerId> seating,
        PlayerId winner,
        Stakes stakes,
        Win win)
    {
        ArgumentNullException.ThrowIfNull(seating);
        ArgumentNullException.ThrowIfNull(stakes);
        ArgumentNullException.ThrowIfNull(win);

        var payment = RoundPayment(stakes, TableRules.For(seating.Count), win);
        var payments = seating.ToDictionary(player => player, _ => 0);

        if (!payments.ContainsKey(winner))
        {
            throw new ArgumentException($"The winner {winner} is not at the table.", nameof(winner));
        }

        // §7.5: on a third consecutive win the seat above pays the winner's WHOLE round payment
        // and nobody else pays anything — blamed for feeding the streak. ⚠️ <b>The winner
        // collects the same either way</b>: the rule substitutes who owes it, not how much is
        // owed, so the one seat pays what all of them would have paid between them. (RULES.md
        // §5.1 names the same edge of the table, and §4.5 a third time.)
        if (win.PaidByTheSeatAboveAlone)
        {
            var blamed = SeatAbove(seating, winner);

            payments[blamed] -= payment * (seating.Count - 1);
            payments[winner] += payment * (seating.Count - 1);
        }
        else
        {
            foreach (var payer in seating.Where(player => player != winner))
            {
                payments[payer] -= payment;
                payments[winner] += payment;
            }
        }

        return payments;
    }

    /// <summary>
    /// The seat immediately <b>above</b> <paramref name="player"/> in turn order — the one that
    /// discards to them, and so the only one that can feed them (RULES.md §5, §5.1).
    /// </summary>
    /// <remarks>
    /// 🔥 <b>The third rule to single out this edge of the table.</b> §5.1 bans you from feeding
    /// the seat below you, §4.5 makes a claim need the permission of the seat above you, and §7.5
    /// blames the seat above you for a streak — three sayings, from two people, all naming the
    /// same relationship. ⚠️ <b>Two-handed it is the same seat both ways</b>, which is the rule
    /// working rather than a case to special-case (§9 #25).
    /// </remarks>
    public static PlayerId SeatAbove(IReadOnlyList<PlayerId> seating, PlayerId player)
    {
        ArgumentNullException.ThrowIfNull(seating);

        for (var seat = 0; seat < seating.Count; seat++)
        {
            if (seating[seat] == player)
            {
                return seating[(seat + seating.Count - 1) % seating.Count];
            }
        }

        throw new ArgumentException($"{player} is not in this seating.", nameof(player));
    }

    private static void Pay(Dictionary<PlayerId, int> deltas, PlayerId from, PlayerId to, int amount)
    {
        deltas[from] -= amount;
        deltas[to] += amount;
    }

    private static Card Resolve(IReadOnlyList<Card> shoe, CardId card) =>
        card.Value >= 0 && card.Value < shoe.Count
            ? shoe[card.Value]
            : throw new ArgumentException($"Card {card} is not in the shoe.", nameof(shoe));

    private static void RequireIndexAligned(IReadOnlyList<Card> shoe)
    {
        for (var index = 0; index < shoe.Count; index++)
        {
            if (shoe[index].Id.Value != index)
            {
                throw new ArgumentException(
                    "The shoe must be in DeckBuilder.BuildTwoDecks order, where a card's id is " +
                    $"its index; card {shoe[index]} at index {index} has id {shoe[index].Id}. " +
                    "Deck.Cards is shuffled and cannot be passed here.", nameof(shoe));
            }
        }
    }
}
