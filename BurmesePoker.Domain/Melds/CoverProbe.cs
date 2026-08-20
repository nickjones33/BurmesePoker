using System.Numerics;
using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// One hand, asked the same question about a great many possible fourteenth cards: <em>if I
/// drew this, could I meld more of what I am holding?</em>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>It exists because of one measurement</b> (BUILD-PLAN P21, §3.7 item 4). A rung that
/// counts live outs asks that question about thirty-odd values for each of seven-odd candidate
/// discards, and the profile of a single answer is <b>three quarters candidate generation and
/// one quarter search</b> — so the thing to remove is not the search but the thirty-four
/// rebuilds of an index over thirteen cards that did not change.
/// </para>
/// <para>
/// <b>What is safe to reuse, and why.</b> The melds of thirteen cards plus a fourteenth split
/// cleanly in two: those that use the new card, and those that do not. The second group is
/// exactly the melds of the thirteen, which do not depend on the fourteenth card at all — so
/// they are built once. The first group is generated per probe, but only from the cards that
/// could possibly be in a meld with it: a run through the probe lies in the probe's suit and a
/// set on it is the probe's rank, and either may borrow a joker (RULES.md §6.1, §6.2). That is
/// a handful of cards rather than thirteen.
/// </para>
/// <para>
/// ⚠️ <b>A joker probe is the exception and is not shortcut.</b> A joker fits anywhere, so
/// "the cards that could be in a meld with it" is the whole hand and there is nothing to save;
/// it takes the ordinary path. There is one joker probe to every fifty-two ranked ones.
/// </para>
/// <para>
/// ⚠️ <b>Nothing here is on the path of a rule.</b> <see cref="HandEvaluator"/> is the win
/// authority and does not know this type exists, and <see cref="PartialCover.Best"/> — which
/// every front end's hint and every other rung's score come from — is untouched (BUILD-PLAN
/// §3.4). What makes this trustworthy is <c>TheProbeAgreesWithTheFullSearch</c>, which plays it
/// against <see cref="PartialCover.Best"/> over real hands rather than reasoning about it.
/// </para>
/// </remarks>
internal sealed class CoverProbe
{
    private readonly IReadOnlyList<Card> _kept;
    private readonly MeldIndex _index;
    private readonly Dictionary<CardId, int> _positions;
    private readonly List<ulong>[] _byLowestCard;

    /// <summary>Which lists the probe just now being asked about was filed under.</summary>
    private readonly List<int> _touched = [];

    /// <param name="kept">The thirteen a discard would leave behind. Not copied; do not mutate it.</param>
    internal CoverProbe(IReadOnlyList<Card> kept)
    {
        _kept = kept;
        _index = MeldIndex.Build(kept);
        _positions = new Dictionary<CardId, int>(_index.Count);

        for (var position = 0; position < _index.Count; position++)
        {
            _positions[_index.Cards[position].Id] = position;
        }

        // Masks only: which cards a meld consumes is the whole of what a covered count needs,
        // and the melds themselves are what make an index expensive to carry about.
        _byLowestCard = new List<ulong>[_index.Count + 1];

        for (var position = 0; position <= _index.Count; position++)
        {
            _byLowestCard[position] = [];
        }

        for (var position = 0; position < _index.Count; position++)
        {
            foreach (var (_, mask) in _index.ByLowestCard[position])
            {
                _byLowestCard[position].Add(mask);
            }
        }
    }

    /// <summary>
    /// Could the hand and this fourteenth card meld <paramref name="target"/> cards between
    /// them?
    /// </summary>
    /// <remarks>
    /// The probe takes the last bit position, which is where a card of a higher
    /// <see cref="CardId"/> than anything in the shoe belongs anyway — the search wants the
    /// cards in a fixed order and nothing more.
    /// </remarks>
    internal bool CoversAtLeastWith(Card probe, int target)
    {
        if (target <= 0)
        {
            return true;
        }

        if (probe.IsJoker || target > _index.Count + 1)
        {
            // Nothing to reuse: every card in the hand could be in a meld with a joker.
            return PartialCover.CoversAtLeast([.. _kept, probe], target);
        }

        var probePosition = _index.Count;

        try
        {
            foreach (var meld in MeldsUsing(probe))
            {
                var mask = 1UL << probePosition;
                var lowest = probePosition;

                foreach (var id in meld.CardIds)
                {
                    if (id == probe.Id)
                    {
                        continue;
                    }

                    var position = _positions[id];

                    mask |= 1UL << position;
                    lowest = Math.Min(lowest, position);
                }

                _byLowestCard[lowest].Add(mask);
                _touched.Add(lowest);
            }

            return Search(0, 0UL, target, []);
        }
        finally
        {
            // The probe's melds are filed under whichever card they start at, so every list
            // they were added to has to be cut back — a mask left behind would be a card of
            // this hand melding with a card that is not in it.
            foreach (var position in _touched)
            {
                _byLowestCard[position].RemoveAt(_byLowestCard[position].Count - 1);
            }

            _touched.Clear();
        }
    }

    /// <summary>Every meld the probe could be part of, out of the cards this hand holds.</summary>
    /// <remarks>
    /// A run through the probe lies in the probe's suit; a set on it is the probe's rank; and
    /// either may use a joker in place of a card (RULES.md §6.1, §6.2). Nothing else in the
    /// hand can appear, so nothing else is offered to the generator.
    /// </remarks>
    private IEnumerable<Meld> MeldsUsing(Card probe)
    {
        var relevant = new List<Card>(_index.Count + 1);

        foreach (var card in _kept)
        {
            if (card.IsJoker || card.Suit == probe.Suit || card.Rank == probe.Rank)
            {
                relevant.Add(card);
            }
        }

        relevant.Add(probe);

        return MeldCandidates.For(relevant).Where(meld => meld.CardIds.Contains(probe.Id));
    }

    /// <summary>
    /// <see cref="PartialCover.CoversAtLeast"/>'s walk, over the index this type keeps: stop at
    /// the first arrangement that clears the bar, and abandon any branch that cannot.
    /// </summary>
    private bool Search(int position, ulong covered, int target, HashSet<(int, ulong)> hopeless)
    {
        var have = BitOperations.PopCount(covered);
        var count = _index.Count + 1;

        if (have >= target)
        {
            return true;
        }

        if (position == count || have + count - position < target)
        {
            return false;
        }

        if ((covered & (1UL << position)) != 0)
        {
            return Search(position + 1, covered, target, hopeless);
        }

        if (hopeless.Contains((position, covered)))
        {
            return false;
        }

        if (Search(position + 1, covered, target, hopeless))
        {
            return true;
        }

        foreach (var mask in _byLowestCard[position])
        {
            if ((covered & mask) == 0 && Search(position + 1, covered | mask, target, hopeless))
            {
                return true;
            }
        }

        hopeless.Add((position, covered));

        return false;
    }
}
