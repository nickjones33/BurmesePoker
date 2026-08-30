using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Web;

/// <summary>
/// A table somebody is asking the lobby for, as the open form has it.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>A class rather than markup, because this is where the form's decisions are</b>
/// (BUILD-PLAN P56). Nothing in this project renders a component in a test — the browser
/// client is checked by source scan and by playing — so a form whose clamping, defaults and
/// per-seat fill lived in a <c>.razor</c> file would be the one part of the lobby nothing could
/// assert. <b>The markup asks the questions; this answers them.</b>
/// </para>
/// <para>
/// ⚠️ <b>Every name arriving here is a name off a wire</b> (P18): resolved against the domain,
/// never trusted, and an unknown one falls back rather than throwing a site over.
/// </para>
/// </remarks>
public sealed class NewTable
{
    private List<string> _perSeat = [];

    /// <summary>What the table is called in the lobby.</summary>
    public string Title { get; set; } = "A table";

    /// <summary>
    /// How many seats it has.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b><see cref="RoundEngine.DefaultPlayers"/> and not <see cref="RoundEngine.MinimumPlayers"/></b>
    /// — P32's confusion, one layer up. Five is the table this game is played at and the table
    /// every published measurement is made at; four is merely the smallest legal one, and
    /// reading the floor as the default is what made the whole measurement set four-handed
    /// before P32 noticed.
    /// </remarks>
    public int Seats { get; set; } = RoundEngine.DefaultPlayers;

    /// <summary>
    /// How many of those seats are held for people.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>It is a quorum, and that is the fact the form has to teach</b> (BUILD-PLAN P56).
    /// <c>HostedTable.Ready</c> is <c>_attending &gt; 0 &amp;&amp; _table.IsFull</c>, and full
    /// means <em>every</em> person-seat claimed — so this is how many people must turn up
    /// before a card is dealt, not how many may. <b>0 is a room you can watch and never join;
    /// 5 deals nothing until the fifth friend arrives</b>, and a table waiting for somebody
    /// looks exactly like a broken one unless the lobby says which it is.
    /// </remarks>
    public int People { get; set; } = 1;

    /// <summary>
    /// The one opponent every computer seat plays, when they all play the same.
    /// </summary>
    /// <remarks>
    /// A level of <c>DifficultyLadder</c>, or — from the advanced control — one of
    /// <see cref="OpponentMenu.Advanced"/>'s rungs at ε = 0.
    /// </remarks>
    public string Difficulty { get; set; } = DifficultyLadder.Default.Name;

    /// <summary>How the computer's seats are filled: <see cref="SeatFill"/>.</summary>
    public string Fill { get; set; } = SeatFill.Same;

    /// <summary>Which <c>SeatingPolicy</c> this table holds its seats under (P36).</summary>
    public string Seating { get; set; } = SeatingPolicy.Default.Name;

    /// <summary>
    /// One opponent per computer seat, in seating order — the second step's answers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>Empty until the form has been posted once</b>, which is the whole of how the
    /// two-step post works: see <see cref="NeedsSeatChoices"/>.
    /// </para>
    /// <para>
    /// 🔥 <b>Defended in the accessor, because a post with none of these in it sets the
    /// property to null</b> — an initialiser runs on construction and the binder runs after it.
    /// The same shape as the <c>BL0008</c> lesson the lobby's model parameter carries, one
    /// level down, and <b>found by pressing the button</b>: it is a 500 on the first post of the
    /// first step and nothing in the tree could see it.
    /// </para>
    /// </remarks>
    public List<string> PerSeat
    {
        get => _perSeat;
        set => _perSeat = value ?? [];
    }

    /// <summary>The seat count this table will actually be opened with.</summary>
    public int WantedSeats => Math.Clamp(Seats, RoundEngine.MinimumPlayers, RoundEngine.MaximumPlayers);

    /// <summary>The number of people it will actually wait for.</summary>
    public int WantedPeople => Math.Clamp(People, 0, WantedSeats);

    /// <summary>How many seats the computer will play, which is the rest of them.</summary>
    public int ComputerSeats => WantedSeats - WantedPeople;

    /// <summary>
    /// Whether this post is the first half of a two-step open: the shape is settled and the
    /// seats have still to be chosen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Two steps rather than one interactive form, and §3.11 C12 is the reason</b>
    /// (BUILD-PLAN P56). The lobby is static SSR on purpose — every control on it is a real
    /// one and it works with no circuit at all — and a static page cannot grow a control per
    /// seat as the seat count is typed. That is why <em>a mixed table</em> was a checkbox. So
    /// the shape is posted first and the seats are chosen on the page that comes back, which
    /// keeps the property that made the lobby cheap and costs one round trip.
    /// </para>
    /// <para>
    /// ⚠️ <b>It is a count check rather than a flag, so it self-corrects</b>: change the shape
    /// on the second step and the seats are asked again, because the answers no longer fit the
    /// table. A table with no computer seats in it never asks at all.
    /// </para>
    /// </remarks>
    public bool NeedsSeatChoices =>
        string.Equals(Fill, SeatFill.Each, StringComparison.Ordinal)
        && ComputerSeats > 0
        && PerSeat.Count != ComputerSeats;

    /// <summary>Fills the second step in with the mix a person would otherwise have got.</summary>
    /// <remarks>
    /// <b>The spread is the starting point rather than a blank form</b>: somebody who wanted a
    /// different level in each seat is nearly always starting from the dial, and a form that
    /// opens on the answer they nearly wanted is one they can correct in a control or two.
    /// </remarks>
    public void AskForSeats()
    {
        var spread = DifficultyLadder.Spread(ComputerSeats);

        PerSeat = [.. spread.Select(level => level.Name)];
    }

    /// <summary>What to open, out of what was asked for.</summary>
    /// <remarks>
    /// ⚠️ <b>The plan is built from the site's own opening plan</b>, so the pace, the patience
    /// and the journal a table was started with are inherited rather than re-invented by a
    /// form that has never heard of them.
    /// </remarks>
    public TablePlan Wanted(TablePlan opening, int seed)
    {
        ArgumentNullException.ThrowIfNull(opening);

        return opening with
        {
            Title = string.IsNullOrWhiteSpace(Title) ? "A table" : Title.Trim(),
            Seats = WantedSeats,
            People = WantedPeople,
            Difficulty = Offered(Difficulty),
            Difficulties = Fill switch
            {
                // ⚠️ The spread is strongest first and cycles, so it fits whatever the table's
                // shape turns out to be — HostedTable takes as many of it as it has computer
                // seats (P19).
                SeatFill.Mixed => [.. DifficultyLadder.Spread(WantedSeats).Select(level => level.Name)],
                SeatFill.Each when PerSeat.Count > 0 => [.. PerSeat.Select(Offered)],
                _ => null
            },
            Seating = SeatingPolicy.Resolve(Seating).Name,
            Seed = seed
        };
    }

    /// <summary>An opponent the menu actually offered, in the menu's own spelling.</summary>
    private static string Offered(string? name) =>
        OpponentMenu.Offers(name) && DifficultyLadder.FindOrProbe(name) is { } chosen
            ? chosen.Name
            : DifficultyLadder.Default.Name;
}

/// <summary>How the computer's seats at a new table are filled.</summary>
/// <remarks>
/// ⚠️ <b>Three answers rather than P19's checkbox</b>, because there are three things a person
/// means: the same opponent everywhere, the dial spread across the seats, or a table they
/// choose seat by seat. The third is the one that needs a second step
/// (<see cref="NewTable.NeedsSeatChoices"/>).
/// </remarks>
public static class SeatFill
{
    /// <summary>The one chosen opponent at every computer seat.</summary>
    public const string Same = "same";

    /// <summary>The dial spread across them, strongest first (P19).</summary>
    public const string Mixed = "mixed";

    /// <summary>One choice per seat, made on the form that comes back.</summary>
    public const string Each = "each";

    /// <summary>What the form offers, in the order it offers it.</summary>
    public static IReadOnlyList<(string Value, string Label)> Offered { get; } =
    [
        (Same, "all play the same"),
        (Mixed, "a mixed table — a different level in each seat"),
        (Each, "let me choose each seat")
    ];
}
