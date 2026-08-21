using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// What a player may see when they are asked to decide something — their own hand, the
/// public information, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// <b>The type is the concealment rule.</b> Play is fully concealed: the only public
/// information is the discards (RULES.md §6.3). So there is no way from here to another
/// player's hand, no way to the table, and no way to the ownership record — which would
/// leak which money cards an opponent was dealt.
/// </para>
/// <para>
/// A fresh context is built for each decision in a turn, so <see cref="Taken"/> is null
/// while the player is still choosing how to take a card, and set from the discard onwards.
/// </para>
/// </remarks>
public sealed class TurnContext
{
    private readonly TableState _table;
    private readonly PlayerState _seat;

    internal TurnContext(
        TableState table,
        PlayerState seat,
        int round,
        int turnNumber,
        Card? availableDiscard,
        bool canClaimTurnedUpMoneyCard,
        Card? taken)
    {
        _table = table;
        _seat = seat;
        Round = round;
        TurnNumber = turnNumber;
        AvailableDiscard = availableDiscard;
        CanClaimTurnedUpMoneyCard = canClaimTurnedUpMoneyCard;
        Taken = taken;
    }

    /// <summary>The player being asked.</summary>
    public PlayerId Player => _seat.Id;

    /// <summary>Their hand — 13 cards, or 14 once they have taken one.</summary>
    public IReadOnlyList<Card> Hand => _seat.Hand;

    /// <summary>Everyone at the table, in turn order.</summary>
    public IReadOnlyList<PlayerId> Players => _table.Players;

    /// <summary>
    /// Which round of the match this is, counting from 1. Public information — everybody at
    /// the table knows — and the only thing that tells a turn apart from the same-numbered
    /// turn of the round before, which a front end that changes hands per turn depends on.
    /// </summary>
    public int Round { get; }

    /// <summary>Which turn of the round this is, counting from 1.</summary>
    public int TurnNumber { get; }

    /// <summary>
    /// The previous player's most recent discard, which this player may take instead of
    /// drawing — or null on the opening turn, when nobody has discarded yet.
    /// </summary>
    public Card? AvailableDiscard { get; }

    /// <summary>
    /// Whether the top money card may be claimed instead of drawing. True only on the
    /// opening turn (RULES.md §4.5).
    /// </summary>
    public bool CanClaimTurnedUpMoneyCard { get; }

    /// <summary>The card taken this turn, or null before it has been taken.</summary>
    public Card? Taken { get; }

    /// <summary>How many cards are left to draw.</summary>
    public int DrawPileCount => _table.DrawPileCount;

    /// <summary>The turned-up money cards still on the table.</summary>
    public IReadOnlyList<Card> TurnedUpMoneyCards => _table.TurnedUpOnTable;

    /// <summary>Which card values pay this round, and how much.</summary>
    public MoneyCardRegistry MoneyCards => _table.MoneyCards;

    /// <summary>The stakes in play.</summary>
    public Stakes Stakes => _table.Stakes;

    /// <summary>
    /// What a declared hand must contain at this table (RULES.md §7.1.1) — how many series,
    /// how many of them joker-free, and whether sets are legal melds at all. Public
    /// information: it is a function of how many are sitting down.
    /// </summary>
    public TableRules Rules => _table.Rules;

    /// <summary>
    /// The ranks this player may not throw, because the seat they discard to has taken them in the
    /// open and not thrown them back (RULES.md §5.1).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It costs the concealment model nothing, and the reason is worth being exact about.</b>
    /// The ban is computed from things that happened in the open, never from reading a hand: the
    /// seat below <em>took</em> its card in view of the table, and every discard that would release
    /// a rank was <em>thrown</em> in view of it. So a player at a real table knows the closed ranks
    /// by having watched, and this tells them nothing they could not have seen (§5.1, Enforcement,
    /// point 1).
    /// </remarks>
    public FeedingBan ClosedToYou => _table.SeatFedBy(Player).MayNotBeFed;

    /// <summary>
    /// Which of the held cards may actually be thrown — <b>the whole of the choice this turn
    /// presents</b> (RULES.md §5.1, §10 #13).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>A banned discard is not an infraction but an impossible move.</b> It is never offered
    /// and cannot be chosen, so there is no penalty and nothing to retract — which is why every
    /// player, bot or person, chooses from this list and not from <see cref="Hand"/>. It is
    /// <b>never empty</b>: where the ban would leave nothing throwable it yields for the turn, so a
    /// turn cannot deadlock (§5.1, The floor).
    /// </para>
    /// <para>
    /// ⚠️ <b>Computed, not cached.</b> <see cref="Hand"/> is the seat's own live list and the engine
    /// discards from it the moment this decision is answered, so an answer kept from before the
    /// discard would describe cards that have gone (P13.1, P19). It is free on an ordinary turn —
    /// nothing closed means the hand itself, with no work and no allocation.
    /// </para>
    /// </remarks>
    public IReadOnlyList<Card> LegalDiscards => ClosedToYou.LegalDiscards(Hand, Rules);

    /// <summary>
    /// Whether this hand may be declared: all 13 cards partition into disjoint melds
    /// (RULES.md §7.1) and the partition satisfies <see cref="Rules"/> (§7.1.1).
    /// <see cref="HandEvaluator"/> is the only authority on that, and the engine asks the
    /// same question before offering the choice.
    /// </summary>
    public bool CanDeclare => HandEvaluator.IsWinning(Hand, Rules);

    /// <summary>
    /// Whether the deck gave this card to <em>this</em> player, and so whether it pays them
    /// at settlement if it is a money card (RULES.md §4.4). A player knows what they were
    /// dealt and what they drew, so this reveals nothing; what other players own does not
    /// appear here at all.
    /// </summary>
    public bool YouOwn(Card card) => _table.Ownership.OwnerOf(card.Id) == Player;
}
