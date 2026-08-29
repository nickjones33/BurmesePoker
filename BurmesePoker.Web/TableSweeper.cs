namespace BurmesePoker.Web;

/// <summary>
/// The thing that makes a site left up for weeks behave: it asks the lobby, every so often, to
/// close the tables nobody is at.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Nothing closed a table before this packet</b> (BUILD-PLAN P54, <c>HOSTING.md</c> §8).
/// <c>Lobby.Close</c> existed and only the tests called it, so a hosted site accumulated a table
/// per form press for ever, hit <see cref="Lobby.MostTables"/>, and from then on answered every
/// <em>Open it</em> with an error — weeks after the deploy, and looking like a broken form
/// rather than a full site.
/// </para>
/// <para>
/// ⚠️ <b>A hosted service and not a timer on the lobby</b>, so that the sweep has somewhere to
/// be cancelled from and the lobby stays a thing that answers questions. It is the only place in
/// this client that does something nobody asked for.
/// </para>
/// <para>
/// ⚠️ <b>It survives its own failures.</b> A sweep that threw would otherwise take the hosted
/// service down and leave the site with no reaper at all — which is the state this class exists
/// to end, arrived at silently.
/// </para>
/// </remarks>
public sealed class TableSweeper(Lobby lobby, TimeProvider clock, ILogger<TableSweeper> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var ticks = new PeriodicTimer(Lobby.SweepsForIdleTablesEvery, clock);

        while (await Safely(ticks, stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await lobby.ReapIdleTables().ConfigureAwait(false);
            }
            catch (Exception problem)
            {
                log.LogError(problem, "The sweep for idle tables failed; the site keeps sweeping.");
            }
        }
    }

    /// <summary>Waits for the next tick, and reports a shutdown as "no more ticks".</summary>
    private static async ValueTask<bool> Safely(PeriodicTimer ticks, CancellationToken stopping)
    {
        try
        {
            return await ticks.WaitForNextTickAsync(stopping).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
