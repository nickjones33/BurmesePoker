namespace BurmesePoker.Domain.Play;

/// <summary>
/// The identity of a player at the table, stable for the whole match.
/// </summary>
/// <remarks>
/// Introduced early, by P2, because ownership of a money card is recorded against a player
/// and outlives the hand that held it (RULES.md §4.4). The rest of <c>Play/</c> — seating,
/// hands, the turn engine — arrives in P7.
/// </remarks>
public readonly record struct PlayerId(int Value)
{
    public override string ToString() => $"P{Value}";
}
