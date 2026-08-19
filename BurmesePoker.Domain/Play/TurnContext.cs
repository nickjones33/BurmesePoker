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
        int turnNumber,
        Card? availableDiscard,
        bool canClaimTurnedUpMoneyCard,
        Card? taken)
    {
        _table = table;
        _seat = seat;
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
    /// Whether this hand may be declared: all 13 cards partition into disjoint melds
    /// (RULES.md §7.1). <see cref="HandEvaluator"/> is the only authority on that, and the
    /// engine asks the same question before offering the choice.
    /// </summary>
    public bool CanDeclare => HandEvaluator.IsWinning(Hand);

    /// <summary>
    /// Whether the deck gave this card to <em>this</em> player, and so whether it pays them
    /// at settlement if it is a money card (RULES.md §4.4). A player knows what they were
    /// dealt and what they drew, so this reveals nothing; what other players own does not
    /// appear here at all.
    /// </summary>
    public bool YouOwn(Card card) => _table.Ownership.OwnerOf(card.Id) == Player;
}
