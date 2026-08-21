using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Money;

/// <summary>
/// The one fact about a round's ownership that changes what a card pays (RULES.md §4.1,
/// the ×5).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This type exists because rev 21 made a multiplier stop being a property of a card.</b>
/// Everything else in the money layer is a pure function of the two turned-up cards, so
/// <see cref="MoneyCardRegistry"/> could answer about a card in isolation. The jackpot cannot
/// be: two players holding one tripled partner each are paid ×3 apiece, and one player holding
/// <b>both</b> is paid ×5 apiece, from exactly the same designation. The inputs widen; the
/// principle does not move — money status is still <b>computed and never stored on a card</b>
/// (BUILD-PLAN §3.3), and this is computed once a round rather than once a card.
/// </para>
/// <para>
/// ⚠️ <b>Narrow on purpose.</b> The rule was stated of the 7♦ and the A♠, and rev 21 also made
/// jokers permanent — so a turn-up can now produce two tripled values other ways (a 7♦ and a
/// joker, two jokers of opposite colours) and <b>nobody has been asked whether those pay ×5</b>
/// (RULES.md §9 #32). <see cref="MoneyCardRegistry.ConfigurationOf"/> recognises the 7♦/A♠ pair
/// and nothing else, so widening it later is a visible change rather than a silent one.
/// </para>
/// </remarks>
/// <param name="OwnsBothPartners">
/// The player the deck gave <b>both</b> partners of a 7♦/A♠ turn-up to, or <c>null</c> — which
/// is the ordinary case, and covers every turn-up that is not that pair.
/// </param>
public readonly record struct MoneyOwnership(PlayerId? OwnsBothPartners)
{
    /// <summary>No jackpot: every money card pays what its value alone says it pays.</summary>
    /// <remarks>
    /// The default, so a caller that has no ownership to hand — a hint, a card drawn on a
    /// panel, an odds estimate over the shoe — asks the ordinary question and gets the
    /// ordinary answer.
    /// </remarks>
    public static MoneyOwnership None => default;
}
