namespace BurmesePoker.Presentation;

/// <summary>
/// What a front end is being told about a card — what it <em>is</em>, never what colour to
/// draw it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A state, not a colour</b> (BUILD-PLAN P13.1). The console draws money yellow and an
/// owned card green; a browser will not necessarily do either, and neither of them may say a
/// thing with colour alone (§3.11 A2, WCAG 1.4.1). So the view model names the state and
/// <see cref="DisplayTokens"/> gives every one of them a non-colour token; how it is coloured
/// on top of that is the renderer's business.
/// </para>
/// <para>
/// <b>Flags, because a card is usually several of these at once</b> — an owned money card
/// inside a meld is three. <see cref="Melded"/> and <see cref="Loose"/> are the one pair that
/// is exclusive: a card is in the best cover or it is not.
/// </para>
/// <para>
/// A card's <em>own</em> red or black is not here. That is a property of the card
/// (RULES.md §2.2), reachable from <c>Card.Color</c>, and not something the presentation
/// layer is saying about it.
/// </para>
/// </remarks>
[Flags]
public enum CardDisplayState
{
    /// <summary>Nothing to say about it.</summary>
    None = 0,

    /// <summary>Part of the best cover of the hand — one of the melds it nearly is.</summary>
    Melded = 1 << 0,

    /// <summary>Left over by the best cover. The deadwood, and the only cards worth throwing.</summary>
    Loose = 1 << 1,

    /// <summary>A money card paying once this round (RULES.md §4.1, §4.2).</summary>
    PaysOnce = 1 << 2,

    /// <summary>A money card paying double — designated twice over (RULES.md §4.3).</summary>
    PaysDouble = 1 << 3,

    /// <summary>
    /// The deck gave this card to the player whose view this is, so it pays them at settlement
    /// whether they still hold it or threw it away (RULES.md §4.4).
    /// </summary>
    Owned = 1 << 4,

    /// <summary>The card the computer would throw away, when a front end is offering hints.</summary>
    SuggestedThrow = 1 << 5
}
