using BurmesePoker.Domain.Cards;
using BurmesePoker.Server;

namespace BurmesePoker.Tests.Server;

/// <summary>
/// Which cards the table itself put in front of everybody, round by round.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>It exists because of a false alarm, and the false alarm is worth recording</b>
/// (BUILD-PLAN P21). Two concealment tests written in P13.2 and P13.4 asserted that the hands
/// of four seats, taken over a whole round, are <b>pairwise disjoint by <c>CardId</c></b>. That
/// held for two years of rounds and then stopped, the day a stronger stand-in rung made a round
/// long enough to run the draw pile out.
/// </para>
/// <para>
/// ⚠️ <b>A card can reach a second hand without anything leaking, and the rules say so.</b> When
/// the draw pile is exhausted the discards are shuffled and become the new one (RULES.md §5) —
/// <c>TableEvent.DiscardsReshuffled</c> — so a king one seat threw away on turn nine is a king
/// another seat may draw on turn fifty. It was <em>public</em> when it was discarded and
/// <em>private</em> again once drawn, and neither of those is a breach. The round that found
/// this ran <b>67 turns and reshuffled 54 cards</b>.
/// </para>
/// <para>
/// <b>So disjointness was never the property; it was a coincidence of short rounds.</b> The
/// property is that a card in two seats' hands passed through the table on the way — and that
/// is what this makes assertable. The stricter statement of the same rule lives in
/// <c>ConcealmentTests.EveryCardASeatIsSentIsOneItMaySee</c>, which asks what the fan-out
/// <em>sent</em> rather than what a hand happened to hold, and which never needed relaxing.
/// </para>
/// </remarks>
internal static class PublicRelease
{
    /// <summary>
    /// The cards the table made public in each round, keyed by round number: the two turned up
    /// at the deal, every discard, every taken discard and every claimed money card
    /// (RULES.md §3, §4.5, §6.3).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Round by round, and that is not tidiness.</b> A <c>CardId</c> names a card in one
    /// round's shoe, which is rebuilt at every deal (P13.4), so a set carried across a deal
    /// would forgive a genuine leak next round on the strength of a discard in this one.
    /// </remarks>
    internal static IReadOnlyDictionary<int, HashSet<CardId>> PerRound(IEnumerable<TableEvent> events)
    {
        var released = new Dictionary<int, HashSet<CardId>>();
        var round = 0;

        foreach (var moment in events)
        {
            if (moment is TableEvent.RoundStarted started)
            {
                round = started.Round;
                Cards(round).UnionWith(started.TurnedUp.Select(card => card.Id));
                continue;
            }

            switch (moment)
            {
                case TableEvent.Discarded discarded:
                    Cards(round).Add(discarded.Card.Id);
                    break;
                case TableEvent.TookDiscard took:
                    Cards(round).Add(took.Card.Id);
                    break;
                case TableEvent.MoneyCardClaimed claimed:
                    Cards(round).Add(claimed.Card.Id);
                    break;
            }
        }

        return released;

        HashSet<CardId> Cards(int at) =>
            released.TryGetValue(at, out var cards) ? cards : released[at] = [];
    }

    /// <summary>What this round put in front of everybody, or nothing if the round never began.</summary>
    internal static HashSet<CardId> In(IReadOnlyDictionary<int, HashSet<CardId>> released, int round) =>
        released.TryGetValue(round, out var cards) ? cards : [];
}
