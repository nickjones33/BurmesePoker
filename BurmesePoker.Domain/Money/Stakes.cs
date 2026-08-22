namespace BurmesePoker.Domain.Money;

/// <summary>
/// The two amounts fixed at the start of a game and applied to every round
/// (RULES.md §4.3).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RoundValue"/> is what each losing player pays the winner — <b>flat</b>, with no
/// per-card penalty for unmelded cards (RULES.md §7.2). <see cref="MoneyCardValue"/> is what
/// each player pays, per money card, to that card's owner.
/// </para>
/// <para>
/// The 5:1 ratio of the standard stakes is load-bearing rather than incidental: under the
/// money rules as they are it makes the side-bet worth a measured 58% of the round prize —
/// significant without dominating (RULES.md §4.3).
/// </para>
/// </remarks>
public sealed record Stakes(int RoundValue, int MoneyCardValue)
{
    /// <summary>The stakes the game is normally played for: $5 a round, $1 a money card.</summary>
    public static Stakes Standard { get; } = new(5, 1);

    /// <inheritdoc cref="Stakes(int, int)"/>
    public int RoundValue { get; } = Positive(RoundValue, nameof(RoundValue));

    /// <inheritdoc cref="Stakes(int, int)"/>
    public int MoneyCardValue { get; } = Positive(MoneyCardValue, nameof(MoneyCardValue));

    /// <summary>
    /// Both stakes are amounts of money changing hands, so neither is meaningful at zero or
    /// below; a negative stake would quietly reverse the direction of every payment.
    /// </summary>
    private static int Positive(int value, string name) => value > 0
        ? value
        : throw new ArgumentOutOfRangeException(name, value, "A stake must be a positive amount.");
}
