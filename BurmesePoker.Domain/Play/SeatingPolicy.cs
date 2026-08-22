using System.Globalization;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// How long a seating holds (RULES.md §3 step 2).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>A seating is drawn once and held.</b> Rev 19 read §3 step 2 as <em>a step of the
/// deal</em> — the seats re-drawn before every round — and rev 28 withdrew that reading on the
/// expert's own words: <em>"in real games you don't shuffle seats every round, only when people
/// ask for it."</em> So the default here is <see cref="Held"/>, and this type exists because
/// <em>when</em> a re-draw happens is a <b>policy</b>, which is the seam between packet P36 and
/// packet P37.
/// </para>
/// <para>
/// ⚠️ <b>This is the mechanism and not the rule.</b> §3 says the seats change <em>when the
/// players agree</em>, and a number fixed when a table opens is not people agreeing (§9 #45,
/// ruled by Nick on 2026-08-22). A table that re-seats every <em>N</em> rounds with <em>N</em>
/// chosen at the start is a legitimate house arrangement and is the only shape that works
/// before the agreeing exists — <b>P37 puts the table's answer where the number is</b>, and
/// nothing here should be read as saying what the rule is.
/// </para>
/// <para>
/// 🔥 <b>One condition, in one place.</b> <see cref="ReseatsBefore"/> is the whole of the
/// decision and <see cref="MatchEngine"/> is the only thing entitled to ask it —
/// <c>LayeringTests.NothingOutsideTheSeatingPolicyDecidesWhenTheSeatsAreDrawnAgain</c> fails
/// the build on a second copy. The temptation is a flag beside a number, which is two states
/// too many: <b>zero rounds between seatings <em>is</em> "never"</b>.
/// </para>
/// </remarks>
public sealed record SeatingPolicy
{
    private SeatingPolicy(int roundsBetweenSeatings) =>
        RoundsBetweenSeatings = roundsBetweenSeatings;

    /// <summary>
    /// Drawn once and kept — RULES.md §3 step 2 as rev 28 corrects it, and the default.
    /// </summary>
    public static SeatingPolicy Held { get; } = new(0);

    /// <summary>Re-drawn before every deal — the reading rev 28 withdrew, kept as a choice.</summary>
    /// <remarks>
    /// ⚠️ <b>It is offered because it is a table somebody may want, not because it is the
    /// rule.</b> It is also what this engine did between P28 and P36, so a journal written in
    /// that window replays under this and under nothing else.
    /// </remarks>
    public static SeatingPolicy EveryRound { get; } = new(1);

    /// <summary>What a table gets when nobody says otherwise.</summary>
    public static SeatingPolicy Default => Held;

    /// <summary>
    /// The policies a front end offers, in the order it should offer them.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>One list, resolved through the domain, never re-typed</b> (BUILD-PLAN P18/P19: the
    /// difficulty dial's own discipline). The third entry is a house arrangement rather than a
    /// magic number — the seats do change, but not under your feet every deal — and it is here
    /// so that <see cref="Every"/>'s arm is reachable from a menu rather than only from code.
    /// </remarks>
    public static IReadOnlyList<SeatingPolicy> Offered { get; } = [Held, EveryRound, Every(5)];

    /// <summary>How many rounds a seating holds for; <b>0 means it is never re-drawn</b>.</summary>
    public int RoundsBetweenSeatings { get; }

    /// <summary>What this policy is called on a command line, in a form and in a journal.</summary>
    public string Name => RoundsBetweenSeatings switch
    {
        0 => "held",
        1 => "every-round",
        var rounds => string.Create(CultureInfo.InvariantCulture, $"every-{rounds}-rounds")
    };

    /// <summary>One line for a menu.</summary>
    public string Description => RoundsBetweenSeatings switch
    {
        0 => "drawn once and kept, until the players agree to change it",
        1 => "re-drawn before every deal",
        var rounds => string.Create(CultureInfo.InvariantCulture, $"re-drawn every {rounds} rounds")
    };

    /// <summary>A seating re-drawn every <paramref name="rounds"/> rounds.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Fewer than one round between seatings.</exception>
    public static SeatingPolicy Every(int rounds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rounds, 1);

        return new SeatingPolicy(rounds);
    }

    /// <summary>
    /// The policy a number names: <b>0 or less is <see cref="Held"/></b>, and anything else is
    /// that many rounds between seatings.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The forgiving door, for values that arrive from outside</b> — a journal header, a
    /// command line, a form. <see cref="Every"/> is the strict one, for code that means it.
    /// </remarks>
    public static SeatingPolicy Of(int roundsBetweenSeatings) =>
        roundsBetweenSeatings <= 0 ? Held : new SeatingPolicy(roundsBetweenSeatings);

    /// <summary>The policy that name means, or null if it names none.</summary>
    /// <remarks>
    /// ⚠️ <b>Case-insensitive, and it knows the whole family rather than only <see cref="Offered"/></b>:
    /// <c>every-7-rounds</c> resolves even though no menu shows it, because a journal written by
    /// a table that was asked for one has to read back.
    /// </remarks>
    public static SeatingPolicy? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var wanted = name.Trim();

        foreach (var policy in (SeatingPolicy[])[Held, EveryRound])
        {
            if (string.Equals(policy.Name, wanted, StringComparison.OrdinalIgnoreCase))
            {
                return policy;
            }
        }

        // ⚠️ Lower-cased before the shape is matched: `Held` and `EveryRound` are compared
        // case-insensitively above, and a family arm that was not would resolve `every-5-rounds`
        // and refuse `EVERY-5-ROUNDS`.
        var parts = wanted.ToLowerInvariant().Split('-');

        return parts is ["every", var middle, "rounds"]
            && int.TryParse(middle, NumberStyles.None, CultureInfo.InvariantCulture, out var rounds)
            && rounds >= 1
                ? new SeatingPolicy(rounds)
                : null;
    }

    /// <summary>That name's policy, or <see cref="Default"/> — never a throw.</summary>
    /// <remarks>
    /// The difficulty dial's rule, for the same reason (P18): a name off a form or a command
    /// line opens the table on the default rather than failing to boot.
    /// </remarks>
    public static SeatingPolicy Resolve(string? name) => Find(name) ?? Default;

    /// <summary>
    /// Whether the round about to be dealt gets a fresh draw, given how many have been played.
    /// </summary>
    /// <remarks>
    /// <b>The first round never re-draws</b>: whoever opened the table has already seated it —
    /// a lobby, a console, a harness assigning strategies to seats on purpose — and drawing over
    /// the top of that would take the caller's arrangement away without asking.
    /// </remarks>
    public bool ReseatsBefore(int roundsPlayed) =>
        RoundsBetweenSeatings > 0 && roundsPlayed > 0 && roundsPlayed % RoundsBetweenSeatings == 0;
}
