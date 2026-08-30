using System.Globalization;

using BurmesePoker.Domain.Agents;

namespace BurmesePoker.Web;

/// <summary>
/// What the lobby may offer as an opponent: the dial, and — behind an advanced control — the
/// measured ladder itself, each rung with the price of choosing it.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is a deliberate amendment to §3.12 and to P19, settled by Nick on 2026-08-29.</b>
/// Both front ends offered <em>levels only</em>, and <see cref="DifficultyLadder"/> said in as
/// many words that a menu with rungs in it as well would be the mistake the design existed to
/// avoid. <b>The standing rule is now: levels are the menu; rungs are an advanced disclosure
/// that states its price.</b>
/// </para>
/// <para>
/// ⚠️ <b>The thing §3.12 was protecting against was selling a measured-worse opponent as a
/// matter of taste</b> — <c>warden</c> is seven points of win rate worse than the rung every
/// level is built on, and <c>random</c> is a joke. <b>Showing the margin beside the name is
/// what pays that bill</b>, so <see cref="Opponent.Margin"/> is not decoration and not
/// optional: it is read from <c>docs/strategy/measurements.csv</c> and fenced by
/// <c>PublishedFigureTests</c> exactly as every other published figure is, and
/// <b>a rung with no published row is not offerable at all</b>.
/// </para>
/// <para>
/// ✅ <b>Nothing new was needed below the form.</b> <see cref="DifficultyLevel.Probe"/> mints
/// <c>sprinter@0</c> and <see cref="DifficultyLadder.FindOrProbe"/> already resolves it — the
/// harness has been resolving both lists since P19 — so an advanced choice travels as a name
/// like every other choice on this page. ⚠️ <b><see cref="DifficultyLadder.Find"/> stays
/// <c>Find</c> wherever a <em>level</em> is meant</b> (the site's own <c>--difficulty</c>
/// shorthand), or a typo there would quietly open a table against a research rung.
/// </para>
/// </remarks>
public static class OpponentMenu
{
    /// <summary>
    /// The published margins, in points of win rate, against <see cref="Reference"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>Transcribed, and therefore fenced</b> — the project's rule for every published
    /// figure (§3.12's closing rule, P34, P39, P50). Each row is
    /// <c>ladder.head-to-head.*</c> in <c>docs/strategy/measurements.csv</c> with its sign
    /// turned round to read <em>this rung against the reference</em>, and
    /// <c>PublishedFigureTests</c> fails the build if a digit here stops agreeing with that
    /// file — or if a measured rung is missing from this list.
    /// </para>
    /// <para>
    /// 🔥 <b>The bad ones are here on purpose.</b> Stating the price is the whole justification
    /// for offering the ladder at all, so hiding <c>random</c> and <c>warden</c> would keep the
    /// menu tidy by giving up the only thing that makes it honest.
    /// </para>
    /// </remarks>
    private static readonly (string Rung, double Margin, double Interval, bool Separated)[] Published =
    [
        ("random", -39.8, 0.3, true),
        ("simple", -10.8, 0.8, true),
        ("greedy", -2.7, 0.8, true),
        ("cautious", -2.9, 0.8, true),
        ("counting", -2.9, 0.8, true),
        ("outs", 0.0, 0.0, false),
        ("warden", -7.3, 0.8, true),
        ("opportunist", +0.1, 0.8, false),
        ("angler", +0.6, 0.8, false),
        ("sprinter", +1.2, 0.8, true)
    ];

    /// <summary>The rung every margin is stated against — the strongest one there is.</summary>
    public static BotRung Reference => BotCatalog.Hardest;

    /// <summary>The dial, strongest first: what a menu of opponents is, and the default.</summary>
    public static IReadOnlyList<DifficultyLevel> Levels => DifficultyLadder.ByStrength;

    /// <summary>
    /// The ladder, in ladder order, each rung with what choosing it costs or buys.
    /// </summary>
    public static IReadOnlyList<Opponent> Advanced { get; } =
    [
        .. Published
            .Where(row => BotCatalog.Find(row.Rung) is not null)
            .Select(row => new Opponent(
                BotCatalog.Resolve(row.Rung),
                row.Margin,
                row.Interval,
                row.Separated))
    ];

    /// <summary>Whether a name off the form is one this menu actually offered.</summary>
    /// <remarks>
    /// ⚠️ <b>The whole of what the lobby will accept</b>: a level by name, or one of the rungs
    /// above at ε = 0. Anything else — including a probe at some other mistake rate, which is a
    /// calibration instrument rather than an opponent — is not offered here and falls back.
    /// </remarks>
    public static bool Offers(string? name) =>
        DifficultyLadder.Find(name) is not null
        || Advanced.Any(opponent => string.Equals(opponent.Value, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// What a level is called where a person reads it — on a seat, and in the lobby's list.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A rung chosen at ε = 0 is called by the rung's name, and the journal is not.</b>
    /// A seat reading <c>Mya Lay (sprinter@0)</c> would be showing a person the machinery; a
    /// journal column reading <c>sprinter</c> would be losing the mistake rate a replay needs.
    /// So the two are deliberately different, and this is the half a person sees.
    /// </remarks>
    public static string Called(DifficultyLevel level)
    {
        ArgumentNullException.ThrowIfNull(level);

        return level.MistakeRate == 0 && level.Name.Contains(DifficultyLevel.Reserved, StringComparison.Ordinal)
            ? level.Rung.Name
            : level.Name;
    }

    /// <summary>One rung offered as an opponent, with the measurement that prices it.</summary>
    /// <param name="Rung">The way of playing.</param>
    /// <param name="Margin">
    /// Points of win rate against <see cref="Reference"/>, published and fenced.
    /// </param>
    /// <param name="Interval">The 95% half-width on that margin, in points.</param>
    /// <param name="Separated">
    /// Whether the measurement separates it from the reference at all — Holm-corrected over the
    /// ladder's own family. ⚠️ <b>A margin without this reads as a difference somebody measured
    /// when it is a difference nobody could find.</b>
    /// </param>
    public sealed record Opponent(BotRung Rung, double Margin, double Interval, bool Separated)
    {
        /// <summary>What this choice travels as — a probe at ε = 0, which is the rung itself.</summary>
        public string Value { get; } = DifficultyLevel.Probe(Rung, 0).Name;

        /// <summary>Whether this is the rung every other margin is stated against.</summary>
        public bool IsReference => Rung == Reference;

        /// <summary>The price, in the form a person choosing an opponent can act on.</summary>
        public string Price => IsReference
            ? "the strongest way of playing there is — what every difficulty level is built on"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{Margin:+0.0;-0.0} ± {Interval:0.0} points of win rate against {Reference.Name} — {Verdict}");

        private string Verdict => (Separated, Margin > 0) switch
        {
            (true, true) => "measurably stronger",
            (true, false) => "measurably weaker",
            _ => "no measurable difference"
        };
    }
}
