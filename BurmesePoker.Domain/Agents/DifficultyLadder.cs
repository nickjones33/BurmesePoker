using System.Globalization;

using BurmesePoker.Domain.Abstractions;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// One setting of the difficulty dial: a rung to play by, and how often to slip.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>A level is not a rung, and the difference is the whole of §3.12.</b> A rung is a
/// research instrument — it differs from its neighbour in exactly one decision, it is not
/// evenly spaced, and it is entitled to be incomplete. A level is a product: it has to be
/// monotone, fine-grained enough for somebody to ask for <em>a bit easier</em>, and to read as
/// a weaker player rather than as a stranger. The two are built from one mechanism
/// (<see cref="FallibleAgent"/>) and exposed as two lists, and <b>a menu with both in it would
/// be the mistake this design exists to avoid</b>.
/// </para>
/// <para>
/// ⚠️ <b>The rung is <see cref="BotCatalog.Hardest"/> for every level, on purpose.</b>
/// Difficulty is the strongest available rung with a mistake rate, so a rung that raises the
/// ceiling (P20–P22) raises every level at once and moves the calibration — and none of them
/// is <em>required</em> for a person to get a good opponent. That independence is the direct
/// lesson of P15, which spent a whole packet on a plausible rung worth +0.5 ± 0.55 points.
/// </para>
/// </remarks>
/// <param name="Name">
/// What a menu, a command line and a CSV row all call it. <b>Stable for ever</b>, for the
/// reason a rung's is (BUILD-PLAN §3.8 item 4): rename it and yesterday's calibration stops
/// being comparable. It may contain neither <see cref="BotRung.Reserved"/> nor
/// <see cref="Reserved"/>, and it may not be a rung's name — <c>--strategies</c> resolves both
/// lists and a name in both would be ambiguous.
/// </param>
/// <param name="Description">One line, written for somebody choosing how hard to make it.</param>
/// <param name="Rung">The way of playing the mistakes are made against.</param>
/// <param name="MistakeRate">
/// ε: how often it throws the second-best card instead of the best.
/// <para>
/// ⚠️ <b>Set by measurement, never by taste.</b> The values are what
/// <c>docs/strategy/measurements.csv</c> says separates the levels; the command that produced
/// them is in that file, and <c>docs/STRATEGY.md</c> quotes it. A level that measurement cannot
/// separate from its neighbour is deleted rather than shipped (§3.12 item 2).
/// </para>
/// </param>
public sealed record DifficultyLevel(string Name, string Description, BotRung Rung, double MistakeRate)
{
    /// <summary>What separates a rung from a rate in a probe's name (see <see cref="Probe"/>).</summary>
    public const char Reserved = '@';

    private readonly string _name = Checked(Name);

    /// <inheritdoc cref="DifficultyLevel" />
    /// <remarks>
    /// ⚠️ <b>Checked in the accessor, for the reason <see cref="BotRung.Name"/> is</b>: a
    /// record's <c>with</c> expression copies the backing fields and then sets what changed, so
    /// a property initialiser runs on construction only and would let a copy through.
    /// </remarks>
    public string Name
    {
        get => _name;
        init => _name = Checked(value);
    }

    /// <summary>
    /// Seats a fresh one for one seat of one game.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>The rung is handed the seat's own seed and the slips are drawn from a stream of
    /// their own.</b> The first half is what makes ε = 0 byte-identical to the undecorated rung
    /// (P19 acceptance 3) — at ε = 0 the decorator defers entirely and never draws — and the
    /// second is so that a rung which does decide something at random is not correlated with
    /// the mistakes laid over it.
    /// </para>
    /// <para>
    /// <b>Called per seat, never shared</b>, for the reason <see cref="BotRung.Create"/> is: an
    /// agent that remembered anything across games would make a run depend on the order its
    /// games happened to be scheduled in.
    /// </para>
    /// </remarks>
    public IPlayerAgent Create(int seed) => new FallibleAgent(Rung.Create(seed), MistakeRate, new Random(~seed));

    /// <summary>
    /// A rung at an arbitrary mistake rate, named for the pair — <c>greedy@0.35</c>.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>The instrument the levels were calibrated with, kept rather than deleted</b>
    /// (§3.12's closing rule: a published figure carries the command that made it). ε is a dial
    /// that can be turned to any value, and the sweep that found the shipped values has to be
    /// re-runnable by somebody who doubts them — <c>tournament --strategies
    /// greedy@0,greedy@0.3,greedy@0.6</c> is that command. ⚠️ <b>It is not a level and never
    /// appears in a menu</b>: its name carries <see cref="Reserved"/> precisely so that
    /// <see cref="Find"/> can never return one.
    /// </remarks>
    public static DifficultyLevel Probe(BotRung rung, double mistakeRate)
    {
        ArgumentNullException.ThrowIfNull(rung);
        ArgumentOutOfRangeException.ThrowIfNegative(mistakeRate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mistakeRate, 1);

        var rate = Rate(mistakeRate);

        return new DifficultyLevel(
            rung.Name + Reserved + rate,
            $"{rung.Name}, throwing the second-best card {rate} of the time — a calibration probe, not a level.",
            rung,
            mistakeRate);
    }

    /// <summary>Whether a name is the one shape carrying <see cref="Reserved"/> that is allowed.</summary>
    /// <remarks>
    /// ⚠️ <b>The probe form is parsed here and nowhere else</b>, so that "what a probe is called"
    /// and "what a name may be" cannot drift apart: the accessor lets a name through exactly
    /// when <see cref="DifficultyLadder.FindOrProbe"/> would build one from it.
    /// </remarks>
    internal static bool IsProbe(string name, out BotRung? rung, out double mistakeRate)
    {
        rung = null;
        mistakeRate = 0;

        var split = name.Split(Reserved);

        if (split.Length != 2
            || BotCatalog.Find(split[0]) is not { } found
            || !double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var rate)
            || rate is < 0 or > 1
            || !string.Equals(split[1], Rate(rate), StringComparison.Ordinal))
        {
            return false;
        }

        rung = found;
        mistakeRate = rate;
        return true;
    }

    /// <summary>How a rate is spelled in a probe's name — one spelling, so a name round-trips.</summary>
    private static string Rate(double mistakeRate) => mistakeRate.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Checked(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Contains(BotRung.Reserved, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"'{name}' is not a name a difficulty level may have: '{BotRung.Reserved}' labels a copy the "
                + "harness made of a rung (Tournament.MirrorSuffix), which is not a way of playing.",
                nameof(name));
        }

        return !name.Contains(Reserved, StringComparison.Ordinal) || IsProbe(name, out _, out _)
            ? name
            : throw new ArgumentException(
                $"'{name}' is not a name a difficulty level may have: '{Reserved}' names a calibration probe, "
                + $"which reads rung{Reserved}rate — e.g. {BotCatalog.Hardest.Name}{Reserved}0.35.",
                nameof(name));
    }
}

/// <summary>
/// The difficulty dial: every setting a person can be offered, weakest first.
/// </summary>
/// <remarks>
/// <para>
/// <b>What a front end offers.</b> <see cref="BotCatalog"/> is the ladder and is what the
/// harness ranks; this is the product, and the console's prompt and the lobby's form show this
/// list and only this list (§3.12).
/// </para>
/// <para>
/// 🔥 <b>Three levels rather than five, and that is a result rather than a shortage of
/// imagination.</b> ε is a dial that can be turned to any value, so the temptation is a long
/// menu — but at 8,008 games a cell the 95% half-width on a margin between two thinking players
/// is about a point (P17), and <b>a level that is not separated from its neighbour is a lie
/// told to everybody who reads the menu</b> (§3.12 item 2). The shipped values are spaced so
/// that the adjacent margins clear that floor with room, and the measurement is in
/// <c>docs/strategy/measurements.csv</c> under <c>difficulty.*</c>.
/// </para>
/// <para>
/// ⚠️ <b>Nothing here is the stand-in or the hint.</b> A seat the computer takes over for
/// somebody who stopped answering, and the suggestion your own seat is shown, are
/// <see cref="BotCatalog.Hardest"/> whatever the table is set to — a hint that got worse as you
/// lowered the difficulty would be absurd. Said where they are used, because it looks like an
/// oversight in all three places.
/// </para>
/// </remarks>
public static class DifficultyLadder
{
    /// <summary>
    /// Every setting, <b>weakest first</b> — the dial read from the bottom up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>The ε values are measured, and the spacing is the measurement rather than the
    /// dial</b> (see <see cref="DifficultyLevel.MistakeRate"/>). ε is very far from linear in
    /// what it costs — the sweep
    /// <c>BurmesePoker.Sim -- --strategies outs@0,outs@0.1,outs@0.2,outs@0.35,outs@0.5,outs@0.75,outs@1
    /// --seating balanced --games 4802</c> put ε = 0 to ε = 0.5 at nine and a half points of win
    /// rate and ε = 0.5 to ε = 1 at sixteen and a half — so these four are placed to be
    /// <b>evenly spaced in results</b>, about seven and a half points apart at the reference
    /// table, and not evenly spaced in ε.
    /// <c>BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent
    /// --games 8000</c> is the check; the numbers are in <c>docs/strategy/measurements.csv</c>
    /// under <c>difficulty.*</c> and are quoted by <c>docs/STRATEGY.md</c>.
    /// </para>
    /// <para>
    /// 🔥 <b>Re-fitted in P23 against <c>outs</c>, and only one value moved.</b> P19 placed these
    /// against <c>greedy</c>; P21 promoted <c>outs</c> and re-based every level onto it without
    /// re-spacing, which left the reference table at steps of <b>8.2 / 4.3 / 10.3</b> points —
    /// ordered, and visibly not a dial. The re-fit moved <c>hard</c> from 0.5 to <b>0.4</b> and
    /// left the other three alone, giving <b>7.9 / 6.7 / 7.7</b>. ⚠️ <b>That one value is the
    /// finding: ε's curve has very nearly the same shape on a rung that looks ahead as on one
    /// that does not</b>, so a mistake rate is close to being a property of the mistake rather
    /// than of the rung it is made against — which is why the next rung to raise the ceiling
    /// should expect to re-check this and not to re-derive it.
    /// </para>
    /// <para>
    /// 🔥 <b>Re-fitted again in P32 when the standing table moved to five seats, and this time
    /// <em>nothing</em> moved.</b> The sweep
    /// <c>BurmesePoker.Sim -- --strategies outs@0,outs@0.1,outs@0.2,outs@0.35,outs@0.5,outs@0.75,outs@1
    /// --seating balanced --seats 5 --games 16807</c> puts the curve at
    /// <b>27.5 / 26.3 / 24.7 / 22.9 / 19.4 / 13.1 / 6.0</b> points of win rate, and fitting four
    /// evenly-spaced levels on it asks for <c>hard</c> ≈ 0.42 and <c>medium</c> ≈ 0.67 — the
    /// shipped 0.4 and 0.7 inside the rounding. ⚠️ <b>The prediction written down before that run
    /// was that at least one value would have to move</b>, because a five-handed table's base win
    /// rate is 20% rather than 25% and the steps should compress with it. <b>They did not</b>: the
    /// reference table reads 9.9 / 15.9 / 23.8 / 30.4 for steps of <b>6.0 / 7.9 / 6.6</b> against
    /// four-handed's 7.1 / 7.9 / 8.0, and all three adjacent margins still survive Holm.
    /// 🔥 <b>So ε is close to being a property of the mistake rather than of the rung <em>or of
    /// the table</em></b> — P23's finding, holding across a second axis it was never tested on.
    /// </para>
    /// <para>
    /// ✅ <b>One dial, not one dial per table size</b> (BUILD-PLAN P32's first open decision).
    /// A level ought to mean the same thing wherever you sit, and a per-size dial would be three
    /// calibrations to keep true and three ways for the menu to lie. The values are fitted at
    /// five seats, and the dial is <b>checked monotone and separated at four, five and six</b> —
    /// which is only affordable as a decision because the re-fit turned out to be a no-op.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<DifficultyLevel> All { get; } =
    [
        new(
            "easy",
            "Plays the right idea and gets it wrong nearly every time — it will feed you cards you want.",
            BotCatalog.Hardest,
            MistakeRate: 0.9),
        new(
            "medium",
            "Knows what to keep, and throws the wrong one of two good cards more often than not.",
            BotCatalog.Hardest,
            MistakeRate: 0.7),
        new(
            "hard",
            "Plays well, and slips about two times in five on the cards it is choosing between.",
            BotCatalog.Hardest,
            MistakeRate: 0.4),
        new(
            "expert",
            "Throws the best card it can see, every single turn.",
            BotCatalog.Hardest,
            MistakeRate: 0.0)
    ];

    /// <summary>
    /// The same settings, <b>strongest first</b> — the order a menu is drawn in.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>The order is the default, and P18 is why that sentence is here.</b> A Spectre
    /// <c>SelectionPrompt&lt;T&gt;</c> opens on <c>default(T)</c> when that value is one of the
    /// choices and otherwise on the first entry drawn — so whichever level heads this list is
    /// what everybody who presses return gets. It is the hardest, which is what the console and
    /// the browser have both handed out since P10, and <b>a dial that quietly defaulted to its
    /// bottom is the exact bug P18 found</b>.
    /// </remarks>
    public static IReadOnlyList<DifficultyLevel> ByStrength { get; } = [.. All.Reverse()];

    /// <summary>The setting that plays best.</summary>
    public static DifficultyLevel Hardest => ByStrength[0];

    /// <summary>What a table is opened at unless somebody says otherwise.</summary>
    public static DifficultyLevel Default => Hardest;

    /// <summary>Looks a level up by name, case-insensitively, or null if there is none.</summary>
    /// <remarks>
    /// ⚠️ <b>Named levels only.</b> A probe is not a setting, and its name carries
    /// <see cref="DifficultyLevel.Reserved"/> so that this can never return one — a form field
    /// or a query string reaching this method cannot conjure an uncalibrated opponent.
    /// </remarks>
    public static DifficultyLevel? Find(string? name) =>
        All.FirstOrDefault(level => string.Equals(level.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Looks a level up by name.</summary>
    /// <exception cref="ArgumentException">No level is called that.</exception>
    public static DifficultyLevel Resolve(string name) =>
        Find(name)
        ?? throw new ArgumentException(
            $"No difficulty called '{name}'. Known: {string.Join(", ", All.Select(level => level.Name))}.",
            nameof(name));

    /// <summary>
    /// A level or a calibration probe, by name: <c>medium</c>, or <c>greedy@0.35</c>.
    /// </summary>
    /// <remarks>
    /// <b>For the harness and nothing else</b> — see <see cref="DifficultyLevel.Probe"/> for why
    /// the probe form is kept. A front end calls <see cref="Find"/>.
    /// </remarks>
    public static DifficultyLevel? FindOrProbe(string? name)
    {
        if (name is null || name.IndexOf(DifficultyLevel.Reserved, StringComparison.Ordinal) < 0)
        {
            return Find(name);
        }

        return DifficultyLevel.IsProbe(name, out var rung, out var rate)
            ? DifficultyLevel.Probe(rung!, rate)
            : throw new ArgumentException(
                $"'{name}' is not a calibration probe. One reads rung{DifficultyLevel.Reserved}rate, with a "
                + $"rate between 0 and 1 — e.g. {BotCatalog.Hardest.Name}{DifficultyLevel.Reserved}0.35.",
                nameof(name));
    }

    /// <summary>
    /// A level for each of <paramref name="seats"/> computer seats, spread across the dial.
    /// </summary>
    /// <remarks>
    /// <b>What "a mixed table" means</b> (P19: difficulty is per seat). A table of four
    /// identical bots is the least interesting table in the game, so a spread deals the levels
    /// out <b>strongest first</b> and cycles if there are more seats than settings — the same
    /// list a menu is drawn from, so a person who asked for a mix gets the levels they were
    /// shown.
    /// </remarks>
    public static IReadOnlyList<DifficultyLevel> Spread(int seats)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seats);

        return [.. Enumerable.Range(0, seats).Select(index => ByStrength[index % ByStrength.Count])];
    }
}
