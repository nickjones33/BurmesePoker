using System.Buffers.Binary;
using System.Text;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// Just enough of the wire format a Blazor Server circuit is driven over to hold one from a
/// test: SignalR's length-prefixed framing, and the slice of MessagePack the hub protocol
/// actually puts on it.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Why this exists rather than a client library</b> (P59). A circuit is reached over
/// SignalR, and <c>AddInteractiveServerComponents</c> narrows that hub to one protocol —
/// <c>blazorpack</c>, which is MessagePack with a small type set and which no NuGet client
/// speaks. The alternative to writing it down here is to test something that is <em>not</em> a
/// circuit, which is the exact substitution this project has a scar from (<c>--no-restore</c>,
/// P52).
/// </para>
/// <para>
/// ⚠️ <b>It is deliberately partial.</b> Only the messages a circuit's opening needs are
/// written; everything else on the wire is skipped by shape rather than understood. A test
/// that needs a message this cannot write should add it here, not work around it.
/// </para>
/// </remarks>
internal static class BlazorPack
{
    /// <summary>The one protocol the component hub accepts.</summary>
    internal const string Protocol = "blazorpack";

    /// <summary>The record separator SignalR terminates its JSON handshake with.</summary>
    internal const byte HandshakeTerminator = 0x1e;

    /// <summary>The handshake a client opens with, terminator included.</summary>
    internal static byte[] Handshake() =>
        [.. Encoding.UTF8.GetBytes($"{{\"protocol\":\"{Protocol}\",\"version\":1}}"), HandshakeTerminator];

    /// <summary>An invocation of a hub method, framed and ready to send.</summary>
    /// <param name="invocationId">Null for a call whose result is not waited for.</param>
    internal static byte[] Invocation(string? invocationId, string target, params object?[] arguments)
    {
        var body = new List<byte>();

        WriteArrayHeader(body, 6);
        WriteInteger(body, 1);          // an invocation
        WriteMapHeader(body, 0);        // no headers
        WriteStringOrNil(body, invocationId);
        WriteStringOrNil(body, target);
        WriteArrayHeader(body, arguments.Length);

        foreach (var argument in arguments)
        {
            WriteValue(body, argument);
        }

        WriteArrayHeader(body, 0);      // no streams

        return Frame(body);
    }

    /// <summary>Length-prefixes a message body the way SignalR's binary transports do.</summary>
    private static byte[] Frame(List<byte> body)
    {
        var framed = new List<byte>(body.Count + 5);

        for (var length = body.Count; ;)
        {
            var chunk = (byte)(length & 0x7f);
            length >>= 7;
            framed.Add(length == 0 ? chunk : (byte)(chunk | 0x80));

            if (length == 0)
            {
                break;
            }
        }

        framed.AddRange(body);
        return [.. framed];
    }

    private static void WriteValue(List<byte> to, object? value)
    {
        switch (value)
        {
            case null:
                to.Add(0xc0);
                break;
            case string text:
                WriteStringOrNil(to, text);
                break;
            case bool flag:
                to.Add(flag ? (byte)0xc3 : (byte)0xc2);
                break;
            case long number:
                WriteInteger(to, number);
                break;
            case int number:
                WriteInteger(to, number);
                break;
            default:
                throw new NotSupportedException($"{value.GetType()} is not on this wire.");
        }
    }

    internal static void WriteArrayHeader(List<byte> to, int count)
    {
        if (count < 16)
        {
            to.Add((byte)(0x90 | count));
            return;
        }

        to.Add(0xdc);
        WriteBigEndian(to, (ushort)count);
    }

    private static void WriteMapHeader(List<byte> to, int count) => to.Add((byte)(0x80 | count));

    private static void WriteInteger(List<byte> to, long value)
    {
        if (value is >= 0 and < 128)
        {
            to.Add((byte)value);
            return;
        }

        to.Add(0xd3);
        Span<byte> eight = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(eight, value);
        to.AddRange(eight);
    }

    private static void WriteStringOrNil(List<byte> to, string? text)
    {
        if (text is null)
        {
            to.Add(0xc0);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(text);

        switch (bytes.Length)
        {
            case < 32:
                to.Add((byte)(0xa0 | bytes.Length));
                break;
            case < 256:
                to.Add(0xd9);
                to.Add((byte)bytes.Length);
                break;
            case < 65536:
                to.Add(0xda);
                WriteBigEndian(to, (ushort)bytes.Length);
                break;
            default:
                to.Add(0xdb);
                WriteBigEndian(to, (uint)bytes.Length);
                break;
        }

        to.AddRange(bytes);
    }

    private static void WriteBigEndian(List<byte> to, ushort value)
    {
        to.Add((byte)(value >> 8));
        to.Add((byte)value);
    }

    private static void WriteBigEndian(List<byte> to, uint value)
    {
        to.Add((byte)(value >> 24));
        to.Add((byte)(value >> 16));
        to.Add((byte)(value >> 8));
        to.Add((byte)value);
    }

    /// <summary>A cursor over one MessagePack message body.</summary>
    /// <remarks>
    /// ⚠️ <b><see cref="Skip"/> is what makes the partial reader honest</b>: anything not
    /// understood is stepped over by its own encoded shape rather than guessed at, so an
    /// unexpected message cannot desynchronise the stream.
    /// </remarks>
    internal ref struct Cursor(ReadOnlySpan<byte> body)
    {
        private readonly ReadOnlySpan<byte> _body = body;
        private int _at;

        internal int ReadArrayHeader()
        {
            var header = _body[_at++];

            return header switch
            {
                >= 0x90 and <= 0x9f => header & 0x0f,
                0xdc => ReadBigEndian16(),
                0xdd => (int)ReadBigEndian32(),
                _ => throw new InvalidOperationException($"0x{header:x2} is not an array."),
            };
        }

        internal long ReadInteger()
        {
            var header = _body[_at++];

            switch (header)
            {
                case <= 0x7f: return header;
                case >= 0xe0: return (sbyte)header;
                case 0xcc: return _body[_at++];
                case 0xcd: return ReadBigEndian16();
                case 0xce: return ReadBigEndian32();
                case 0xcf: return (long)ReadBigEndian64();
                case 0xd0: return (sbyte)_body[_at++];
                case 0xd1: return (short)ReadBigEndian16();
                case 0xd2: return (int)ReadBigEndian32();
                case 0xd3: return (long)ReadBigEndian64();
                default: throw new InvalidOperationException($"0x{header:x2} is not a number.");
            }
        }

        internal bool ReadBoolean() => _body[_at++] switch
        {
            0xc3 => true,
            0xc2 => false,
            var other => throw new InvalidOperationException($"0x{other:x2} is not a boolean."),
        };

        internal string? ReadStringOrNull()
        {
            var header = _body[_at++];

            int length;

            switch (header)
            {
                case 0xc0: return null;
                case >= 0xa0 and <= 0xbf: length = header & 0x1f; break;
                case 0xd9: length = _body[_at++]; break;
                case 0xda: length = ReadBigEndian16(); break;
                case 0xdb: length = (int)ReadBigEndian32(); break;
                default: throw new InvalidOperationException($"0x{header:x2} is not a string.");
            }

            var text = Encoding.UTF8.GetString(_body.Slice(_at, length));
            _at += length;
            return text;
        }

        /// <summary>Steps over one value of any shape, however deep.</summary>
        internal void Skip()
        {
            var header = _body[_at++];

            switch (header)
            {
                case <= 0x7f or >= 0xe0 or 0xc0 or 0xc2 or 0xc3:
                    return;
                case >= 0x80 and <= 0x8f:
                    SkipMany((header & 0x0f) * 2);
                    return;
                case >= 0x90 and <= 0x9f:
                    SkipMany(header & 0x0f);
                    return;
                case >= 0xa0 and <= 0xbf:
                    _at += header & 0x1f;
                    return;
                case 0xc4: _at += _body[_at] + 1; return;
                case 0xc5: _at += ReadBigEndian16(); return;
                case 0xc6: _at += (int)ReadBigEndian32(); return;
                case 0xca: _at += 4; return;
                case 0xcb: _at += 8; return;
                case 0xcc or 0xd0: _at += 1; return;
                case 0xcd or 0xd1: _at += 2; return;
                case 0xce or 0xd2: _at += 4; return;
                case 0xcf or 0xd3: _at += 8; return;
                case 0xd9: _at += _body[_at] + 1; return;
                case 0xda: _at += ReadBigEndian16(); return;
                case 0xdb: _at += (int)ReadBigEndian32(); return;
                case 0xdc: SkipMany(ReadBigEndian16()); return;
                case 0xdd: SkipMany((int)ReadBigEndian32()); return;
                case 0xde: SkipMany(ReadBigEndian16() * 2); return;
                case 0xdf: SkipMany((int)ReadBigEndian32() * 2); return;
                default: throw new InvalidOperationException($"0x{header:x2} has no known shape.");
            }
        }

        private void SkipMany(int values)
        {
            for (var each = 0; each < values; each++)
            {
                Skip();
            }
        }

        private ushort ReadBigEndian16()
        {
            var value = BinaryPrimitives.ReadUInt16BigEndian(_body[_at..]);
            _at += 2;
            return value;
        }

        private uint ReadBigEndian32()
        {
            var value = BinaryPrimitives.ReadUInt32BigEndian(_body[_at..]);
            _at += 4;
            return value;
        }

        private ulong ReadBigEndian64()
        {
            var value = BinaryPrimitives.ReadUInt64BigEndian(_body[_at..]);
            _at += 8;
            return value;
        }
    }
}
