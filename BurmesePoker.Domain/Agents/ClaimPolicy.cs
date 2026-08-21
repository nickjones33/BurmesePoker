using BurmesePoker.Domain.Abstractions;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// One arm of the claim-permission experiment: a rung, and what it answers when the seat after
/// it asks for the turned-up money card (RULES.md §4.5).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The same shape as <see cref="DifficultyLevel.Probe"/> and for the same reason.</b> A
/// probe is a way of naming an instrument on the command line so that a published figure carries
/// the command that made it (BUILD-PLAN §3.12). <c>outs/refuse</c> and <c>outs/allow</c> are two
/// players that differ in exactly one answer, which is what a head-to-head cell needs and what
/// P15 says a rung has to be.
/// </para>
/// <para>
/// ⚠️ <b>Not a level, not a rung, and never in a menu.</b> Its name carries
/// <see cref="Reserved"/> precisely so that <see cref="BotCatalog.Find"/> and
/// <see cref="DifficultyLadder.Find"/> can never return one.
/// </para>
/// </remarks>
/// <param name="Name">What a command line and a CSV row call it — <c>rung/refuse</c>.</param>
/// <param name="Description">One line, written for a reader of the measurement.</param>
/// <param name="Rung">The way of playing every other decision.</param>
/// <param name="Objects">Whether it refuses whenever the rule allows it to.</param>
public sealed record ClaimPolicy(string Name, string Description, BotRung Rung, bool Objects)
{
    /// <summary>What separates a rung from a policy in a probe's name.</summary>
    public const char Reserved = '/';

    /// <summary>The arm that refuses whenever §4.5 lets it — what every rung in the catalog does.</summary>
    public const string Refusing = "refuse";

    /// <summary>The arm that never refuses, and so never discloses that it holds the rank.</summary>
    public const string Allowing = "allow";

    /// <summary>Seats a fresh one for one seat of one game.</summary>
    /// <remarks>
    /// The rung is handed the seat's own seed, exactly as <see cref="DifficultyLevel.Create"/>
    /// hands it one — so <c>rung/refuse</c> is byte-identical to the bare rung, which is the
    /// control this cell is checked against.
    /// </remarks>
    public IPlayerAgent Create(int seed) => new ClaimPolicyAgent(Rung.Create(seed), Objects);

    /// <summary>The two arms of one rung, refusing first.</summary>
    public static IReadOnlyList<ClaimPolicy> Both(BotRung rung) => [Of(rung, true), Of(rung, false)];

    /// <summary>One arm of one rung.</summary>
    public static ClaimPolicy Of(BotRung rung, bool objects)
    {
        ArgumentNullException.ThrowIfNull(rung);

        return new ClaimPolicy(
            rung.Name + Reserved + (objects ? Refusing : Allowing),
            objects
                ? $"{rung.Name}, refusing the opener the turned-up money card whenever RULES.md §4.5 allows it."
                : $"{rung.Name}, never refusing it — an experiment's arm, not a way of playing.",
            rung,
            objects);
    }

    /// <summary>
    /// A policy arm by name — <c>outs/refuse</c> — or null if the name is not one.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Parsed here and nowhere else</b>, for the reason <see cref="DifficultyLevel.IsProbe"/>
    /// is: what an arm is called and what names are accepted cannot then drift apart.
    /// </remarks>
    public static ClaimPolicy? FindOrProbe(string? name)
    {
        if (name is null || name.IndexOf(Reserved, StringComparison.Ordinal) < 0)
        {
            return null;
        }

        var split = name.Split(Reserved);

        if (split.Length == 2 && BotCatalog.Find(split[0]) is { } rung)
        {
            if (string.Equals(split[1], Refusing, StringComparison.OrdinalIgnoreCase))
            {
                return Of(rung, true);
            }

            if (string.Equals(split[1], Allowing, StringComparison.OrdinalIgnoreCase))
            {
                return Of(rung, false);
            }
        }

        throw new ArgumentException(
            $"'{name}' is not a claim-permission arm. One reads rung{Reserved}{Refusing} or "
            + $"rung{Reserved}{Allowing} — e.g. {BotCatalog.Hardest.Name}{Reserved}{Refusing}.",
            nameof(name));
    }
}
