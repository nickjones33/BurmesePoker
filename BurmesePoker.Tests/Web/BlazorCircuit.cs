using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.TestHost;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// One real Blazor Server circuit, held from a test: the page's own component markers, a real
/// WebSocket, the framework's own protocol, and a socket that can be killed without a close
/// frame.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Why a circuit rather than a component</b> (P59). Everything this project has ever
/// asserted about a dropped connection has been asserted about objects on this side of the
/// wire — the seat, the channel, the table. <b>What holds a disconnected circuit for two
/// minutes is the framework, and nothing in the tree had ever asked it anything.</b> The
/// retention window is only observable through a circuit that really starts and a socket that
/// really dies.
/// </para>
/// <para>
/// ⚠️ <b>The kill is an abort, not a close.</b> A radio that goes away sends no close frame,
/// and a graceful shutdown is the one case a phone in a lift never produces — so
/// <see cref="KillTheSocket"/> aborts, and the server learns of it the way it learns of a lost
/// connection.
/// </para>
/// </remarks>
internal sealed partial class BlazorCircuit : IAsyncDisposable
{
    private readonly WebSocket _socket;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _waiting = new();
    private readonly CancellationTokenSource _stopped = new();
    private readonly Task _reading;
    private int _calls;

    private BlazorCircuit(WebSocket socket)
    {
        _socket = socket;
        _reading = Task.Run(ReadAsync);
    }

    /// <summary>The circuit id the server handed back — the name a reconnection asks for.</summary>
    internal string CircuitId { get; private set; } = string.Empty;

    /// <summary>
    /// Fetches a page, takes the component markers out of it, and starts a circuit for them —
    /// which is exactly what <c>blazor.web.js</c> does with the same markers.
    /// </summary>
    internal static async Task<BlazorCircuit> StartAsync(TestServer server, HttpClient client, string page)
    {
        var html = await client.GetStringAsync(page);
        var markers = ServerMarkers(html);

        Assert.NotEmpty(markers);

        var circuit = new BlazorCircuit(await OpenAsync(server));

        var baseUri = new Uri(client.BaseAddress!, "/").ToString();
        var uri = new Uri(client.BaseAddress!, page).ToString();

        circuit.CircuitId = await circuit.CallAsync(
            "StartCircuit", baseUri, uri, $"[{string.Join(',', markers)}]", string.Empty)
            as string ?? string.Empty;

        Assert.NotEqual(string.Empty, circuit.CircuitId);
        return circuit;
    }

    /// <summary>
    /// Opens a second socket and asks for a circuit back by name — the reconnection
    /// <c>blazor.web.js</c> attempts when a connection it lost comes back.
    /// </summary>
    /// <returns>
    /// False when the server no longer has it, which is the retention window having closed.
    /// </returns>
    /// <remarks>
    /// ⚠️ <b>The second socket is dropped again on the way out</b>, which puts a circuit it did
    /// reclaim straight back into the disconnected pool with a fresh window. That is harmless
    /// here — nothing asks twice — but a test that reconnected and then went on playing would
    /// have to hold this rather than ask it.
    /// </remarks>
    internal static async Task<bool> ReconnectsAsync(TestServer server, string circuitId)
    {
        await using var reconnection = new BlazorCircuit(await OpenAsync(server));
        return await reconnection.CallAsync("ConnectCircuit", circuitId) is true;
    }

    /// <summary>Kills the connection the way a lost radio does — no close frame, no warning.</summary>
    internal void KillTheSocket() => _socket.Abort();

    public async ValueTask DisposeAsync()
    {
        await _stopped.CancelAsync();
        _socket.Abort();
        _socket.Dispose();

        try
        {
            await _reading;
        }
        catch (Exception)
        {
            // A killed socket is how this object is meant to end; the read loop's complaint
            // about it is not a test failure.
        }

        _stopped.Dispose();
    }

    private static async Task<WebSocket> OpenAsync(TestServer server)
    {
        var websockets = server.CreateWebSocketClient();
        var socket = await websockets.ConnectAsync(
            new Uri(server.BaseAddress, "/_blazor"), CancellationToken.None);

        await socket.SendAsync(
            BlazorPack.Handshake(), WebSocketMessageType.Binary, endOfMessage: true, CancellationToken.None);

        return socket;
    }

    private async Task<object?> CallAsync(string target, params object?[] arguments)
    {
        var id = Interlocked.Increment(ref _calls).ToString();
        var answer = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting[id] = answer;

        await SendAsync(BlazorPack.Invocation(id, target, arguments));

        return await answer.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private async Task SendAsync(byte[] message)
    {
        await _socket.SendAsync(
            message, WebSocketMessageType.Binary, endOfMessage: true, _stopped.Token);
    }

    private async Task ReadAsync()
    {
        var buffer = new byte[64 * 1024];
        var pending = new List<byte>();
        var handshook = false;

        while (!_stopped.IsCancellationRequested)
        {
            var received = await _socket.ReceiveAsync(buffer, _stopped.Token);

            if (received.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            pending.AddRange(buffer.AsSpan(0, received.Count));

            if (!received.EndOfMessage)
            {
                continue;
            }

            var body = pending.ToArray();
            pending.Clear();
            var at = 0;

            // The handshake's answer is JSON and terminated, not framed.
            if (!handshook)
            {
                var terminator = Array.IndexOf(body, BlazorPack.HandshakeTerminator);
                var reply = Encoding.UTF8.GetString(body, 0, terminator);
                Assert.DoesNotContain("error", reply, StringComparison.Ordinal);
                handshook = true;
                at = terminator + 1;
            }

            while (at < body.Length)
            {
                var length = 0;

                for (var shift = 0; ; shift += 7)
                {
                    var chunk = body[at++];
                    length |= (chunk & 0x7f) << shift;

                    if ((chunk & 0x80) == 0)
                    {
                        break;
                    }
                }

                Dispatch(body.AsSpan(at, length));
                at += length;
            }
        }
    }

    private void Dispatch(ReadOnlySpan<byte> message)
    {
        var cursor = new BlazorPack.Cursor(message);
        var items = cursor.ReadArrayHeader();
        var kind = cursor.ReadInteger();

        switch (kind)
        {
            case 1 when items >= 5:
            {
                cursor.Skip();                              // headers
                cursor.Skip();                              // no invocation id: nothing to answer
                var target = cursor.ReadStringOrNull();
                var arguments = cursor.ReadArrayHeader();

                // ⚠️ A render batch must be acknowledged or the renderer stops after ten of
                // them (P59). Nothing here draws the batch — what a circuit *renders* is not
                // this packet's question; that it is alive and holding a seat is.
                if (target == "JS.RenderBatch" && arguments >= 1)
                {
                    var batch = cursor.ReadInteger();
                    _ = SendAsync(BlazorPack.Invocation(null, "OnRenderCompleted", batch, null));
                }

                return;
            }

            case 3:
            {
                cursor.Skip();                              // headers
                var id = cursor.ReadStringOrNull();
                var result = cursor.ReadInteger();

                if (id is null || !_waiting.TryRemove(id, out var answer))
                {
                    return;
                }

                switch (result)
                {
                    case 1:
                        answer.TrySetException(new InvalidOperationException(cursor.ReadStringOrNull()));
                        return;
                    case 2:
                        answer.TrySetResult(null);
                        return;
                    default:
                        answer.TrySetResult(Value(ref cursor));
                        return;
                }
            }

            default:
                return;
        }
    }

    /// <summary>The two answer shapes this packet asks for: a circuit id, and a yes or no.</summary>
    private static object? Value(ref BlazorPack.Cursor cursor)
    {
        var copy = cursor;

        try
        {
            return cursor.ReadStringOrNull();
        }
        catch (InvalidOperationException)
        {
            cursor = copy;
            return cursor.ReadBoolean();
        }
    }

    private static IReadOnlyList<string> ServerMarkers(string html) =>
        [.. Marker().Matches(html)
            .Select(match => match.Groups[1].Value)
            .Where(json => json.Contains("\"type\":\"server\"", StringComparison.Ordinal))];

    [GeneratedRegex("<!--Blazor:(.*?)-->", RegexOptions.Singleline)]
    private static partial Regex Marker();
}
