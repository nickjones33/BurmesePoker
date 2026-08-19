namespace BurmesePoker.Sim;

/// <summary>
/// One master seed in, a seed per game out — the whole reproducibility story (BUILD-PLAN §3.7).
/// </summary>
/// <remarks>
/// <para>
/// Games must not draw their seeds from a shared generator: that would make a game's seed
/// depend on how many games were dealt out before it, and so on the scheduling of a parallel
/// run. Deriving each seed from <c>(master, game)</c> instead makes game 417 the same game
/// whether it ran first, last, or alone.
/// </para>
/// <para>
/// The mixing function is SplitMix64's finaliser over the two numbers packed into one word,
/// which is injective before the mix and well spread after it — adjacent master seeds give
/// unrelated runs rather than shifted ones.
/// </para>
/// </remarks>
public static class SeedSequence
{
    /// <summary>The seed for one game of a run. Non-negative, and stable for ever.</summary>
    public static int GameSeed(int masterSeed, int game)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(game);

        return Mix(masterSeed, game);
    }

    /// <summary>
    /// The seed for one seat of one game — what a strategy that needs randomness is handed
    /// (BUILD-PLAN P15).
    /// </summary>
    /// <remarks>
    /// <b>Derived, for the same reason a game's seed is.</b> A strategy that reached for
    /// <see cref="Random.Shared"/> would make its own play depend on how many other seats were
    /// running beside it, and take every other seat's reproducibility down with it (§3.7 item
    /// 1). Deriving from <c>(game seed, seat)</c> instead makes seat 2 of game 417 draw the
    /// same numbers whether the run was parallel, serial, or a single game on its own — and
    /// it separates the seats, so two random bots at one table do not play in lockstep.
    /// </remarks>
    public static int SeatSeed(int gameSeed, int seat)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seat);

        return Mix(gameSeed, seat);
    }

    private static int Mix(int high, int low)
    {
        var z = ((ulong)(uint)high << 32) | (uint)low;

        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;

        return (int)(z & int.MaxValue);
    }
}
