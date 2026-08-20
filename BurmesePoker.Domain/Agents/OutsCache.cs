using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// Whether a hand can meld a given number of its cards, remembered by the <b>values</b> it was
/// asked about — the third of P21's three prunes, and the cheapest of them, because an answer
/// already bought costs a dictionary lookup.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a value multiset is the right key, and is not an approximation.</b>
/// <see cref="PartialCover"/> reads a <see cref="CardId"/> for one purpose only: to keep
/// the melds of an arrangement disjoint (BUILD-PLAN §3.1, §3.4). Two hands holding the same
/// values differ by a relabelling of ids, and a relabelling maps arrangements onto arrangements
/// one for one — so the number of cards covered is the same for both. Which melds come back
/// would not be, and this remembers only the count.
/// </para>
/// <para>
/// ⚠️ <b>It is worth about a tenth, and that is measured rather than hoped for.</b> A seat asks
/// on the order of two hundred questions a turn and about <b>9%</b> of them have been asked
/// before — mostly across the turn boundary, where the thirteen a seat kept last turn are
/// thirteen it must consider keeping again the moment it draws. <b>It is here because it costs a
/// dictionary lookup, not because it is the reason the rung is affordable</b>; the three prunes
/// that are the reason are in <see cref="LiveOuts"/> and <see cref="CoverProbe"/>.
/// </para>
/// <para>
/// ⚠️ <b>Any cache over the win authority has to be transparent, and this one is asserted to
/// be</b> (P21 acceptance 3): it answers <see cref="PartialCover.CoversAtLeast"/> and nothing
/// else, it is never consulted by <see cref="HandEvaluator"/>, and
/// <c>TheCacheAnswersExactlyWhatTheSearchWouldHave</c> plays it against the search over real
/// hands.
/// </para>
/// <para>
/// ⚠️ <b>It is bounded, because a match is unbounded.</b> An agent lives for one seat of one
/// game (<see cref="BotRung.Create"/>) and a game may be any number of rounds, so the table is
/// emptied whole once it passes <see cref="Capacity"/> — deterministically, since what is
/// asked and in what order is a function of the cards. A cache that grew for ever would cost
/// the harness more in memory than the searches it saves.
/// </para>
/// </remarks>
internal sealed class OutsCache
{
    /// <summary>
    /// Entries kept before the table is emptied. A seat asks a few thousand distinct questions
    /// a round, so this holds several rounds' worth and costs well under a megabyte — which
    /// matters, because a run holds one of these per seat per game in flight.
    /// </summary>
    internal const int Capacity = 60_000;

    /// <summary>Values in the shoe: 52 ranked, and the joker, which has no rank or suit.</summary>
    private const int Values = 53;

    /// <summary>The joker's slot, and the fact that all four of them share it.</summary>
    private const int JokerSlot = 52;

    /// <summary>
    /// Bits per value in a key: a hand holds at most two copies and the probe may make a
    /// third, so two bits count every hand this can be asked about.
    /// </summary>
    private const int BitsPerValue = 2;

    private readonly Dictionary<UInt128, bool> _answers = [];

    /// <summary>Could these fourteen cards meld <paramref name="target"/> of themselves?</summary>
    internal bool CoversAtLeast(CoverProbe search, IReadOnlyList<Card> kept, Card probe, int target)
    {
        var key = KeyOf(kept, probe) + ((UInt128)target << (Values * BitsPerValue));

        if (_answers.TryGetValue(key, out var known))
        {
            return known;
        }

        var reaches = search.CoversAtLeastWith(probe, target);

        if (_answers.Count >= Capacity)
        {
            _answers.Clear();
        }

        _answers[key] = reaches;

        return reaches;
    }

    /// <summary>How many entries are being held. For the tests that say this is bounded.</summary>
    internal int Count => _answers.Count;

    /// <summary>
    /// The hand as a count per value, packed two bits each — the bar it is being asked about
    /// goes above them, since the same fourteen cards get asked about different bars.
    /// </summary>
    /// <remarks>
    /// Order-independent by construction — adding a card is an increment of its own two bits —
    /// so no sort is needed, which matters because this runs once per search avoided as well as
    /// once per search made.
    /// </remarks>
    private static UInt128 KeyOf(IReadOnlyList<Card> kept, Card probe)
    {
        var key = UInt128.Zero;

        foreach (var card in kept)
        {
            key += UInt128.One << (SlotOf(card) * BitsPerValue);
        }

        return key + (UInt128.One << (SlotOf(probe) * BitsPerValue));
    }

    /// <summary>
    /// Which of the <see cref="Values"/> a card is. <b>Both jokers of both decks are one
    /// value</b>, because a joker's colour tells two physical cards apart and changes no meld
    /// there is (BUILD-PLAN §3.2).
    /// </summary>
    private static int SlotOf(Card card) =>
        card.IsJoker
            ? JokerSlot
            : (((int)card.Rank!.Value - (int)Rank.Two) * CardText.AllSuits.Count) + (int)card.Suit!.Value;
}
