using Microsoft.AspNetCore.Components.Server.Circuits;

namespace BurmesePoker.Web;

/// <summary>
/// Whether the circuit drawing this page is connected, told to the seat it is playing (P64).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The only thing in this client that knows a socket exists</b>, and it exists because
/// nothing else could learn what it knows. Blazor Server holds a dropped circuit for
/// <c>CircuitOptions.DisconnectedCircuitRetentionPeriod</c> and says nothing to the components
/// inside it — they are not disposed, they simply stop being drawn for — so a seat's standing
/// question went on spending its patience on somebody who could not answer it. P63 measured
/// that failing on a phone: a tab backgrounded on Android lost its circuit in <b>5.6 s</b>, and
/// the computer played the seat's turn while the framework was still holding the circuit.
/// </para>
/// <para>
/// ⚠️ <b>Scoped, because a circuit is a scope.</b> One of these per circuit, holding whichever
/// seat that circuit's <c>TableView</c> sat down in — null for a watcher, and null again the
/// moment the view stands up.
/// </para>
/// <para>
/// ⚠️ <b><see cref="OnConnectionUpAsync"/> also runs on the first connection</b>, which is not a
/// return; <c>SeatBoard.ConnectionBack</c> says nothing unless it was told the connection had
/// gone.
/// </para>
/// </remarks>
public sealed class SeatPresence : CircuitHandler
{
    private SeatBoard? _seat;

    /// <summary>The seat this circuit is playing, or null when it is only watching.</summary>
    public void Holds(SeatBoard? seat) => _seat = seat;

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _seat?.ConnectionLost();
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _seat?.ConnectionBack();
        return Task.CompletedTask;
    }
}
