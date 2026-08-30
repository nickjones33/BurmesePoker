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
/// <b>a rung with no published row is not offerable at all</b>. ⚠️ <b>P57 added a second
/// exclusion beside that one</b> — a rung that cannot be asked for its second-best move is not
/// offerable either, because a level is built out of exactly that question (P19); see
/// <see cref="CanBeAskedForItsSecondBestMove"/>.
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
    /// for offering the ladder at all, so hiding <c>warden</c> — seven points of win rate worse
    /// than the reference — would keep the menu tidy by giving up the only thing that makes it
    /// honest.
    /// </para>
    /// <para>
    /// ⚠️ <b><c>random</c> has a published row and is deliberately not in this list</b>, which is
    /// the one exclusion that is not about the measurement: see
    /// <see cref="CanBeAskedForItsSecondBestMove"/>. <b>A row here that cannot pass that test is
    /// dead data</b>, so the row was removed rather than left to be filtered.
    /// </para>
    /// </remarks>
    private static readonly (string Rung, double Margin, double Interval, bool Separated)[] Published =
    [
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
            .Select(row => (Rung: BotCatalog.Find(row.Rung), row.Margin, row.Interval, row.Separated))
            .Where(row => row.Rung is not null && CanBeAskedForItsSecondBestMove(row.Rung))
            .Select(row => new Opponent(
                row.Rung!,
                row.Margin,
                row.Interval,
                row.Separated))
    ];

    /// <summary>
    /// Whether this rung can be asked which card it would throw <em>instead</em> — the question
    /// P19 built a difficulty level out of, and the second ground on which this menu excludes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>P57, and it was a live 500 before it was a rule.</b> The menu offered
    /// <c>random@0</c>, <see cref="DifficultyLadder.FindOrProbe"/> resolved it, and
    /// <see cref="DifficultyLevel.Create"/> threw: a level is <em>always</em> a rung wrapped in a
    /// <c>FallibleAgent</c>, and that wrapper demands <see cref="IRanksDiscards"/> because a
    /// mistake is the rung's own second choice. <see cref="RandomBotAgent"/> has no second choice
    /// to name, so it cannot be a level — <b>and it therefore cannot be an opponent this lobby
    /// offers, whatever the CSV measured about it</b>.
    /// </para>
    /// <para>
    /// ⚠️ <b>So the menu now excludes on two grounds, and they are different in kind.</b>
    /// <em>No published row → not offerable</em> is about honesty: a rung whose price cannot be
    /// stated must not be sold. <em>Cannot be asked for its second-best move → not offerable</em>
    /// is about P19's invariant: the lobby must not advertise a seat the engine has never been
    /// able to build. ⚠️ <b>The fix is the menu rather than
    /// <see cref="DifficultyLevel.Create"/></b> — unwrapping at ε = 0 would put
    /// <c>BurmesePoker.Domain</c> and every published measurement in the blast radius for a defect
    /// that lives here (Nick's decision, 2026-08-30).
    /// </para>
    /// <para>
    /// ⚠️ <b>Asked of the agent rather than declared on the rung</b>: the constructor that throws
    /// asks the same question of the same object, so anything shorter would be a second opinion
    /// able to drift away from the one that matters. The seed is irrelevant — no rung decides
    /// which interfaces it implements by it — and this runs once, at type initialisation.
    /// </para>
    /// </remarks>
    public static bool CanBeAskedForItsSecondBestMove(BotRung rung)
    {
        ArgumentNullException.ThrowIfNull(rung);

        return rung.Create(0) is IRanksDiscards;
    }

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
