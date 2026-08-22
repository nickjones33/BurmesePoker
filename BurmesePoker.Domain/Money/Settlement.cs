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
///   <item>every player except the winner pays the <see cref="Stakes.RoundValue"/> to the
///   winner — flat in what the loser <i>held</i>, with no per-card penalty for unmelded cards
///   (RULES.md §7.2), but <b>multiplied when the declared thirteen hold no joker</b>: ×2 at
///   two, three or four seats and ×3 at five or more (RULES.md §7.3);</item>
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
/// 🔥 <b>Since RULES.md rev 26 the round payment is not flat, and it is not seat-count
/// independent either.</b> A <b>jokerless</b> declaration pays the winner
/// <see cref="TableRules.JokerlessMultiplier"/> times the round value from every loser
/// (RULES.md §7.3) — the first thing in this game that pays for <i>how</i> the winner won
/// rather than <i>that</i> they won. ⚠️ <b>The predicate is a property of the cards, not of the
/// partition</b> (<see cref="IsJokerless"/>): a jokerless hand is jokerless under every cover
/// of it, so nothing here asks an evaluator anything and <c>Meld.IsClean</c> — which is
/// §7.1.1's <i>required clean series</i>, a different rule sharing a word — is not consulted.
/// ⚠️ <b>The multiplier reaches step 1 only.</b> The money-card settlement is untouched, which
/// is RULES.md §9 #36's recorded default: three separate sayings name <i>the winning prize</i>
/// and none names the money.
/// </para>
/// <para>
/// <b>Settlement walks ownership records and looks at exactly one hand.</b> Ownership is
/// conferred only by the deck and never transfers (RULES.md §4.4), so where a card now sits
/// is irrelevant: a money card its owner discarded still pays that owner, one an opponent
/// picked up still pays the player who drew it, and one nobody ever drew pays nobody. There
/// is deliberately no table or player-state parameter below — and the one hand that is passed
/// is the <b>winner's declared thirteen</b>, which step 1 needs and step 2 never reads.
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
    /// <param name="declaredHand">
    /// The winner's declared thirteen, which decides the §7.3 bonus and nothing else. Required
    /// rather than optional on purpose: a default would pay flat in silence, which is precisely
    /// the bug this packet exists to remove.
    /// </param>
    public static IReadOnlyDictionary<PlayerId, int> ForRound(
        IReadOnlyList<PlayerId> players,
        PlayerId winner,
        Stakes stakes,
        MoneyCardRegistry moneyCards,
        CardOwnership ownership,
        IReadOnlyList<Card> shoe,
        IReadOnlyList<Card> declaredHand)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(stakes);
        ArgumentNullException.ThrowIfNull(moneyCards);
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(shoe);
        ArgumentNullException.ThrowIfNull(declaredHand);
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

        // 1. The round payment, from every loser to the winner: flat in what they held, and
        //    doubled or tripled when the winner declared without a joker (RULES.md §7.2, §7.3).
        var payment = RoundPayment(stakes, TableRules.For(players.Count), IsJokerless(declaredHand));

        foreach (var player in players)
        {
            if (player != winner)
            {
                Pay(deltas, from: player, to: winner, payment);
            }
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
    /// What one loser pays the winner under RULES.md §7.2 step 1, as amended by §7.3 — the
    /// round value, times <see cref="TableRules.JokerlessMultiplier"/> on a jokerless
    /// declaration.
    /// </summary>
    /// <remarks>
    /// ⚠️ Exposed because a consumer that splits a net delta into <i>the round</i> and <i>the
    /// side bet</i> — the console's settlement panel, the harness's per-seat CSV row — has to
    /// split it at the same place the domain settled it, or the bonus silently lands in the
    /// side-bet column.
    /// </remarks>
    public static int RoundPayment(Stakes stakes, TableRules rules, bool jokerless)
    {
        ArgumentNullException.ThrowIfNull(stakes);
        return stakes.RoundValue * (jokerless ? rules.JokerlessMultiplier : 1);
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
