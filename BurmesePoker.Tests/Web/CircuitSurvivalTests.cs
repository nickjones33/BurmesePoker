using BurmesePoker.Domain.Play;
using BurmesePoker.Web;

using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>What a browser table does when the connection vanishes and comes back</b> (P59).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The gap these tests close.</b> P53 watched an idle circuit for seven and a half
/// minutes <em>with keepalives flowing</em> and nothing closed it. <b>Nobody had ever measured
/// a client that stops answering and then returns</b> — which is the question a screen lock, an
/// app switch and a wifi-to-cellular handover all ask. And P54 joined the table's patience to
/// <c>CircuitOptions.DisconnectedCircuitRetentionPeriod</c> on a claim — <em>inside that window
/// the framework is deliberately hiding a dropped connection from the player, so a shorter
/// patience would have the computer play the turn of somebody the framework is still expecting
/// back</em> — which until now was fenced <b>only against itself</b>: one number read from
/// <c>Program.cs</c>, one off the real <c>Lobby</c>, each asserted to agree with the other.
/// </para>
/// <para>
/// 🔥 <b>The claim is exercised by running the same disconnection under both orderings.</b>
/// With patience longer than retention a turn survives every drop the framework hides; with the
/// two numbers swapped, the very same drop costs the player their turn while the framework is
/// still holding their circuit. ⚠️ <b>That is what makes P54's pairing load-bearing rather than
/// decorative</b>, and it is the failure the fence in <c>ContainerTests</c> can only assert the
/// absence of.
/// </para>
/// <para>
/// ⚠️ <b>Everything is asserted off the table, never off the transport</b> (P59 build item 3).
/// The question is whether the <em>player</em> lost anything: the seat is read from
/// <see cref="HostedTable.WaitingFor"/> and the turn from
/// <see cref="TableBoard.StoodInFor"/> — the public board's own word for <em>the computer
/// played this turn because nobody answered in time</em> — and the socket only ever appears as
/// the thing that was killed.
/// </para>
/// <para>
/// ⚠️ <b><see cref="TableBoard.Turn"/> is the wrong instrument for this and measuring found
/// it.</b> A seat is asked <em>twice</em> in one turn — whether to take, and then what to
/// throw — so the turn stays on a seat for two patiences, and a turn the computer has already
/// played still reads as that seat's. <see cref="TableBoard.StoodInFor"/> is the fact
/// itself.
/// </para>
/// <para>
/// ✅ <b>Proved able to fail the way the packet asked for.</b> With the patience shortened
/// below the retention period —
/// <see cref="Patience"/> set to one second against a three-second window — the
/// <em>inside the window</em> test goes red: the computer has already played the turn of
/// somebody the framework has not even stopped holding.
/// </para>
/// <para>
/// ⚠️ <b>What is deliberately not here: cellular conditions.</b> The packet costed a
/// <c>tc netem</c> arm at ~150 ms RTT with loss and jitter. There is no socket to shape —
/// this is <c>TestServer</c>, whose WebSocket is a pair of in-memory pipes — and shaping a
/// real one needs root on an interface, which is not a thing a test may take. <b>Latency is
/// P60's, on a real network.</b>
/// </para>
/// <para>
/// ⚠️ <b>The two numbers are seconds here, not minutes.</b> A test cannot wait out the shipped
/// window, so the site is opened with a retention and a patience scaled down together — the
/// <em>ordering</em> is the thing under test, and the shipped ordering is fenced separately by
/// <c>ContainerTests</c>.
/// </para>
/// </remarks>
public class CircuitSurvivalTests
{
    /// <summary>Long enough for a circuit to start and a socket to die inside it.</summary>
    private static readonly TimeSpan Retention = TimeSpan.FromSeconds(3);

    /// <summary>The shipped ordering, scaled: a seat waits longer than the framework holds.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The instrument, proved before it is used: a circuit that really starts really takes the
    /// seat.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Nothing in this tree had ever started one.</b> The seat is claimed in
    /// <c>TableView.OnInitialized</c> and only when the component is <em>really</em> interactive
    /// (§3.11 C13), so a table whose one person-seat stops waiting is proof that a circuit
    /// started, rendered, and ran the component — not that a page was fetched.
    /// </remarks>
    [Fact]
    public async Task ARealCircuitSitsDownInTheSeat()
    {
        await using var site = new Site(Patience, Retention);
        var (table, seat) = site.Table();

        Assert.Contains(seat, table.WaitingFor);

        await using var circuit = await site.SitDownAsync(table, "Nick");

        Assert.DoesNotContain(seat, table.WaitingFor);
        Assert.NotEqual(string.Empty, circuit.CircuitId);
    }

    /// <summary>
    /// ✅ <b>Answer one, well inside the window: nothing was lost.</b>
    /// </summary>
    [Fact]
    public async Task AConnectionThatComesBackInsideTheWindowFindsItsOwnSeatAndItsOwnTurn()
    {
        await using var site = new Site(Patience, Retention);
        var (table, seat) = site.Table();
        await using var circuit = await site.SitDownAsync(table, "Nick");

        await Site.WaitUntil(() => table.Board.Turn == seat, "it was never this seat's turn");

        circuit.KillTheSocket();
        await Task.Delay(Retention / 3);

        Assert.True(
            await BlazorCircuit.ReconnectsAsync(site.Server, circuit.CircuitId),
            "the framework should still have been holding the circuit.");

        Assert.DoesNotContain(seat, table.WaitingFor);
        Assert.Equal(seat, table.Board.Turn);
        Assert.DoesNotContain(seat, table.Board.StoodInFor);
    }

    /// <summary>
    /// ✅ <b>Answer two, past the window but inside the patience: the seat goes, the turn
    /// stays.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>This is the answer that was worth measuring.</b> Losing the circuit is not losing
    /// the game: the framework gives the seat up — <c>TableView.Dispose</c> stands the player
    /// up, which is what stops a table waiting for somebody who has gone (§3.11 A5) — while the
    /// engine is still blocked in <c>SeatChannel.Ask</c> holding the question. ⚠️ <b>So a seat
    /// and a turn are recovered by different mechanisms</b>: the seat by sitting down again
    /// under the same name (P13.6), the turn by the patience not having run out.
    /// </remarks>
    [Fact]
    public async Task AConnectionThatComesBackPastTheWindowHasLostTheSeatAndNotTheTurn()
    {
        await using var site = new Site(Patience, Retention);
        var (table, seat) = site.Table();
        await using var circuit = await site.SitDownAsync(table, "Nick");

        await Site.WaitUntil(() => table.Board.Turn == seat, "it was never this seat's turn");

        circuit.KillTheSocket();
        await Task.Delay(Retention);

        Assert.False(
            await BlazorCircuit.ReconnectsAsync(site.Server, circuit.CircuitId),
            "the framework should have given the circuit up.");

        await Site.WaitUntil(() => table.WaitingFor.Contains(seat), "the seat was never given up");

        // The turn is still standing, which is the whole of what the patience buys.
        Assert.Equal(seat, table.Board.Turn);
        Assert.DoesNotContain(seat, table.Board.StoodInFor);

        // And the seat comes back by name, which is the other half of the recovery (P13.6).
        Assert.NotNull(table.SitDown("Nick"));
        Assert.DoesNotContain(seat, table.WaitingFor);
        Assert.DoesNotContain(seat, table.Board.StoodInFor);
    }

    /// <summary>
    /// ✅ <b>Answer three, past both: the computer played the turn.</b>
    /// </summary>
    [Fact]
    public async Task AConnectionThatStaysAwayPastThePatienceHasLostTheTurnToTheComputer()
    {
        await using var site = new Site(Patience, Retention);
        var (table, seat) = site.Table();
        await using var circuit = await site.SitDownAsync(table, "Nick");

        await Site.WaitUntil(() => table.Board.Turn == seat, "it was never this seat's turn");

        circuit.KillTheSocket();

        await Site.WaitUntil(
            () => table.Board.StoodInFor.Contains(seat),
            "the computer never played the turn of a player who had gone",
            Patience * 3);
    }

    /// <summary>
    /// 🔥 <b>P54's claim, exercised — and this is the test that can go red.</b>
    /// </summary>
    /// <remarks>
    /// The identical disconnection, with the patience put <em>below</em> the retention period.
    /// The framework is still holding the circuit — the reconnection succeeds — and the player
    /// nevertheless comes back to a table that has played their turn without them. ⚠️ <b>A
    /// player cannot see this happen</b>: from the browser the connection simply returns, with
    /// no indication that anything was decided in the gap. That is why the two numbers must not
    /// be moved independently.
    /// </remarks>
    [Fact]
    public async Task WithThePatienceShorterThanTheWindowTheComputerPlaysTheTurnOfSomebodyTheFrameworkIsStillHolding()
    {
        var shortPatience = TimeSpan.FromSeconds(2);
        var longRetention = TimeSpan.FromSeconds(20);

        await using var site = new Site(shortPatience, longRetention);
        var (table, seat) = site.Table();
        await using var circuit = await site.SitDownAsync(table, "Nick");

        await Site.WaitUntil(() => table.Board.Turn == seat, "it was never this seat's turn");

        circuit.KillTheSocket();

        await Site.WaitUntil(
            () => table.Board.StoodInFor.Contains(seat),
            "the shorter patience should have run out while the circuit was still held",
            shortPatience * 6);

        Assert.True(
            await BlazorCircuit.ReconnectsAsync(site.Server, circuit.CircuitId),
            "the framework should still have been holding the circuit — which is the point.");

        Assert.DoesNotContain(seat, table.WaitingFor);
    }

    /// <summary>
    /// The site under test: the real <c>Program</c>, opened on a table with one person-seat and
    /// the two numbers this packet is about set explicitly.
    /// </summary>
    private sealed class Site(TimeSpan patience, TimeSpan retention) : WebApplicationFactory<Lobby>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // The house table's own command line (P11, P14) — one seat for a person, the rest
            // the computer's, and a pace that does not make a test wait to watch bots think.
            builder.UseSetting("people", "1");
            builder.UseSetting("seed", "20260830");
            builder.UseSetting("pace", "1");
            builder.UseSetting("between", "1");
            builder.UseSetting("patience", ((int)patience.TotalSeconds).ToString());

            // ⚠️ Registered after the app's own, so this is the one that takes effect.
            builder.ConfigureServices(services => services.Configure<CircuitOptions>(
                circuits => circuits.DisconnectedCircuitRetentionPeriod = retention));
        }

        /// <summary>The one client this site is asked for pages through.</summary>
        private HttpClient Client { get; set; } = null!;

        /// <summary>The house table, and the one seat it is waiting for a person to take.</summary>
        internal (HostedTable Table, PlayerId Seat) Table()
        {
            // Asking for a client is what starts the host, and so what makes Server and the
            // lobby exist at all.
            Client = CreateClient();

            var table = Services.GetRequiredService<Lobby>().OpenTheHouseTable();
            return (table, Assert.Single(table.WaitingFor));
        }

        internal Task<BlazorCircuit> SitDownAsync(HostedTable table, string you) =>
            BlazorCircuit.StartAsync(Server, Client, $"/table/{table.Id}?you={you}");

        /// <summary>Polls the table until it says something, rather than sleeping for a guess.</summary>
        internal static async Task WaitUntil(Func<bool> said, string complaint, TimeSpan? within = null)
        {
            var deadline = DateTime.UtcNow + (within ?? TimeSpan.FromSeconds(30));

            while (DateTime.UtcNow < deadline)
            {
                if (said())
                {
                    return;
                }

                await Task.Delay(25);
            }

            Assert.Fail(complaint);
        }
    }
}
