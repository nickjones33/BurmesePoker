using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// What one seat has actually been shown this round, and the supply estimate that follows from
/// it — <em>how many copies of that value are still out there</em>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Extracted from <see cref="CountingBotAgent"/> by P31, unchanged.</b> It moved for the
/// reason <see cref="ThreatScore"/> moved out of <see cref="CautiousBotAgent"/> at P20: a second
/// rung needed it, and two copies of a memory would be two places for the answer to drift.
/// 🔥 <b>P20 measured this memory as worth nothing as a tie-break</b> (<c>counting</c> is
/// <c>+0.3 ± 1.0</c> the <em>wrong</em> way against <c>greedy</c>); <see cref="WardenBotAgent"/>
/// is the first decision in the programme where it feeds a rule that might matter, which is a
/// result about the machinery either way (BUILD-PLAN P31 item 2).
/// </para>
/// <para>
/// ⚠️ <b>The information set is the cautious default and is deliberately narrow.</b> This counts
/// <b>only what the seat has been shown</b>: its own hand at each decision, everything that has
/// passed through it, and the one discard offered to it each turn whether or not it took it.
/// RULES.md §9 #15 has since been closed the permissive way — the piles are public and may be
/// read through — so this is <em>weaker</em> than the rules allow rather than stronger, which is
/// the direction to be wrong in.
/// </para>
/// <para>
/// ⚠️ <b>The turned-up money cards are public, plainly visible, and deliberately not counted.</b>
/// Reading them would make which card designates money an input to a discard, which RULES.md §4.4
/// forbids — and <c>MoneyCardsDoNotChangeWhatABotThrowsAway</c> is the test that says so.
/// </para>
/// <para>
/// 🔥 <b>The round boundary is the hazard.</b> A <see cref="CardId"/> names a card in a
/// <em>round's</em> shoe, which is rebuilt at every deal (P13.4), so a memory that survived a deal
/// would not be stale — it would be wrong. <see cref="Observe"/> is driven by
/// <see cref="TurnContext.Round"/> and forgets before it counts anything.
/// </para>
/// </remarks>
public sealed class ShoeMemory
{
    private const int SuitCount = 4;
    private const int RankCount = 13;

    /// <summary>The physical cards this seat has been shown, so that one is never counted twice.</summary>
    private readonly HashSet<CardId> _seen = [];

    /// <summary>How many copies of each value that comes to, indexed by <see cref="Slot"/>.</summary>
    private readonly int[] _copies = new int[RankCount * SuitCount];

    /// <summary>Which round the memory belongs to. Zero before the first turn of the match.</summary>
    private int _round;

    /// <summary>
    /// The physical cards this seat has been shown this round.
    /// </summary>
    /// <remarks>
    /// ✅ <b>Public because it is P20's claim</b> (P20 acceptance 2): what a counting seat knows
    /// beyond its own hand has to be a subset of what a watcher at the table can see, and a claim
    /// nothing can read is a claim nothing can check. It is a copy, so reading it cannot change it.
    /// </remarks>
    public IReadOnlySet<CardId> Remembered => new HashSet<CardId>(_seen);

    /// <summary>
    /// How many copies of one value this seat believes are still out there: two, less the ones it
    /// has been shown.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Never negative.</b> Two decks hold two copies of everything, so it cannot be — but the
    /// clamp is what stops a miscount turning a threat into a negative number and inverting a
    /// tie-break rather than merely getting it wrong.
    /// </remarks>
    public int Available(Rank rank, Suit suit) =>
        Math.Max(0, ThreatScore.CopiesInTheShoe - _copies[Slot(rank, suit)]);

    /// <summary>
    /// How many copies of one <b>rank</b>, in any suit, this seat believes are still out there —
    /// eight less what it has seen.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Rank alone, because RULES.md §5.1 is about rank alone</b> (see
    /// <see cref="Card.SameRankAs"/>). It is the same arithmetic <see cref="Available"/> does,
    /// summed over the four suits, and it is the only question <see cref="WardenBotAgent"/> asks of
    /// a memory — <em>if I close this rank against the seat above me, how much of it is still
    /// loose for them to be holding?</em>
    /// </remarks>
    public int LooseCopiesOf(Rank rank)
    {
        var loose = 0;

        foreach (var suit in Suits)
        {
            loose += Available(rank, suit);
        }

        return loose;
    }

    /// <summary>
    /// Takes in everything this seat is being shown right now — and nothing else.
    /// </summary>
    /// <remarks>
    /// <b>Call it at every on-turn decision, not only the one that reads the memory.</b> The engine
    /// builds a fresh context for each question in a turn (<c>RoundEngine.TakeTurn</c>), so the
    /// round reset has to have happened before whichever of them comes first.
    /// </remarks>
    public void Observe(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Forget(context.Round);

        foreach (var card in context.Hand)
        {
            Remember(card);
        }

        if (context.AvailableDiscard is { } offered)
        {
            Remember(offered);
        }

        // context.TurnedUpMoneyCards is public and is not read here, on purpose. See the remarks
        // on this class: RULES.md §4.4.
    }

    /// <summary>The supply estimate, in the shape <see cref="ThreatScore"/> asks for.</summary>
    /// <remarks>
    /// ⚠️ <b>Internal because <see cref="ThreatScore"/> is</b> — the scoring is the ladder's own
    /// arithmetic and not something a front end may reach. <see cref="Available"/> is the same
    /// answer in a shape anybody may ask for.
    /// </remarks>
    internal ThreatScore.Supply Supply => Available;

    private static readonly Suit[] Suits = [Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades];

    /// <summary>Adds one physical card to the memory, if it is not there and is not a joker.</summary>
    /// <remarks>
    /// A joker has neither rank nor suit (<see cref="Card.IsJoker"/>), and the supply estimate is
    /// only ever asked about a ranked value, so counting one would have nowhere to go.
    /// </remarks>
    private void Remember(Card card)
    {
        if (!card.IsJoker && _seen.Add(card.Id))
        {
            _copies[Slot(card.Rank!.Value, card.Suit!.Value)]++;
        }
    }

    /// <summary>Drops the whole memory when the deal moves on.</summary>
    private void Forget(int round)
    {
        if (round == _round)
        {
            return;
        }

        _round = round;
        _seen.Clear();
        Array.Clear(_copies);
    }

    private static int Slot(Rank rank, Suit suit) =>
        ((int)rank - (int)Rank.Two) * SuitCount + (int)suit;
}
