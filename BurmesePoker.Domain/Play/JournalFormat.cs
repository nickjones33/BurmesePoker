using System.Globalization;
using System.Text;
using System.Text.Json;

using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// A journal as lines of JSON, and back again (BUILD-PLAN P14).
/// </summary>
/// <remarks>
/// <para>
/// <b>One format, defined in one place.</b> Two consumers writing journals is expected; two
/// consumers deciding what a journal looks like is how a format quietly forks. Both front ends
/// call this and neither knows anything else about the shape.
/// </para>
/// <para>
/// <b>Lines, not a document</b> — exactly the choice <see cref="IEnumerable{T}"/> of strings
/// makes elsewhere in the tree (the harness's CSV): one JSON object per line, so a journal is
/// streamable, appendable, greppable, and readable by anything that reads JSON Lines. A file
/// holds any number of journals one after another; a <c>header</c> line starts each.
/// </para>
/// <para>
/// <b>Writing a file is the consumer's job</b>, not this class's. It returns lines and the
/// domain still contains no <c>File</c> (BUILD-PLAN §2).
/// </para>
/// </remarks>
public static class JournalFormat
{
    /// <summary>One journal, as its header line followed by a line per decision.</summary>
    public static IEnumerable<string> Lines(GameJournal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        yield return HeaderLine(journal.Header);

        foreach (var decision in journal.Decisions)
        {
            yield return DecisionLine(decision);
        }
    }

    /// <summary>Several journals in one file, each starting at its own header line.</summary>
    public static IEnumerable<string> Lines(IEnumerable<GameJournal> journals)
    {
        ArgumentNullException.ThrowIfNull(journals);

        return journals.SelectMany(Lines);
    }

    /// <summary>Reads a file that holds exactly one journal.</summary>
    /// <exception cref="JournalException">There is not exactly one.</exception>
    public static GameJournal Read(IEnumerable<string> lines)
    {
        var journals = ReadAll(lines);

        return journals.Count == 1
            ? journals[0]
            : throw new JournalException(
                $"Expected one journal, found {journals.Count}. Use {nameof(ReadAll)} for a file of several.");
    }

    /// <summary>Reads every journal in a file, in the order they were written.</summary>
    /// <exception cref="JournalException">
    /// A line is not readable, or a decision arrives before any header. <b>A corrupt journal
    /// fails here rather than replaying as something else</b> (BUILD-PLAN P14).
    /// </exception>
    public static IReadOnlyList<GameJournal> ReadAll(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var journals = new List<GameJournal>();
        JournalHeader? header = null;
        var decisions = new List<JournalDecision>();
        var number = 0;

        foreach (var line in lines)
        {
            number++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var document = Parse(line, number);
            var root = document.RootElement;
            var type = Text(root, "type", number);

            switch (type)
            {
                case "header":
                    if (header is not null)
                    {
                        journals.Add(new GameJournal(header, [.. decisions]));
                        decisions.Clear();
                    }

                    header = Guarded(() => ReadHeader(root, number), number);
                    break;

                case "decision":
                    if (header is null)
                    {
                        throw new JournalException(
                            $"Line {number} is a decision, but no journal has begun — a journal starts at its header line.");
                    }

                    decisions.Add(Guarded(() => ReadDecision(root, number), number));
                    break;

                default:
                    throw new JournalException($"Line {number} is of unknown type '{type}'.");
            }
        }

        if (header is not null)
        {
            journals.Add(new GameJournal(header, [.. decisions]));
        }

        return journals;
    }

    private static string HeaderLine(JournalHeader header)
    {
        var line = new StringBuilder(256);

        line.Append("{\"type\":\"header\"");
        Append(line, "rules", header.RulesRevision);
        Append(line, "fidelity", header.Fidelity == JournalFidelity.Rich ? "rich" : "thin");
        Append(line, "master_seed", header.MasterSeed);
        Append(line, "game", header.Game);
        Append(line, "seed", header.Seed);
        Append(line, "rounds", header.Rounds);
        Append(line, "abandoned", header.Abandoned);
        Append(line, "round_value", header.Stakes.RoundValue);
        Append(line, "money_card_value", header.Stakes.MoneyCardValue);

        line.Append(",\"seats\":[");

        for (var seat = 0; seat < header.Seats.Count; seat++)
        {
            if (seat > 0)
            {
                line.Append(',');
            }

            line.Append("{\"seat\":").Append(seat);
            Append(line, "player", header.Seats[seat].Player.Value);
            Append(line, "strategy", header.Seats[seat].Strategy);
            Append(line, "name", header.Seats[seat].Name);
            line.Append('}');
        }

        return line.Append("]}").ToString();
    }

    private static string DecisionLine(JournalDecision decision)
    {
        var line = new StringBuilder(128);

        line.Append("{\"type\":\"decision\"");
        Append(line, "round", decision.Round);
        Append(line, "turn", decision.Turn);
        Append(line, "player", decision.Player.Value);
        Append(line, "question", Name(decision.Question));
        Append(line, "answer", decision.Answer);

        if (decision.Snapshot is { } snapshot)
        {
            line.Append(",\"hand\":[");

            for (var card = 0; card < snapshot.Hand.Count; card++)
            {
                if (card > 0)
                {
                    line.Append(',');
                }

                line.Append(snapshot.Hand[card].Value);
            }

            line.Append(']');
            Append(line, "discard", snapshot.AvailableDiscard?.Value);
            Append(line, "taken", snapshot.Taken?.Value);
            Append(line, "draw_pile", snapshot.DrawPileCount);
        }

        return line.Append('}').ToString();
    }

    private static JournalHeader ReadHeader(JsonElement root, int number)
    {
        var seats = new List<JournalSeat>();

        if (!root.TryGetProperty("seats", out var seated) || seated.ValueKind != JsonValueKind.Array)
        {
            throw new JournalException($"Line {number}: a header needs a 'seats' array.");
        }

        foreach (var seat in seated.EnumerateArray())
        {
            seats.Add(new JournalSeat(
                new PlayerId(Number(seat, "player", number)),
                Text(seat, "strategy", number),
                OptionalText(seat, "name")));
        }

        if (seats.Count == 0)
        {
            throw new JournalException($"Line {number}: a header names no seats.");
        }

        return new JournalHeader(
            Seed: Number(root, "seed", number),
            Seats: seats,
            Stakes: new Stakes(Number(root, "round_value", number), Number(root, "money_card_value", number)),
            Rounds: Number(root, "rounds", number),
            Fidelity: Text(root, "fidelity", number) == "rich" ? JournalFidelity.Rich : JournalFidelity.Thin,
            MasterSeed: OptionalNumber(root, "master_seed"),
            Game: OptionalNumber(root, "game"),
            Abandoned: root.TryGetProperty("abandoned", out var abandoned) && abandoned.ValueKind == JsonValueKind.True,
            RulesRevision: Number(root, "rules", number));
    }

    private static JournalDecision ReadDecision(JsonElement root, int number)
    {
        var question = Text(root, "question", number) switch
        {
            "action" => JournalQuestion.Action,
            "discard" => JournalQuestion.Discard,
            "claim" => JournalQuestion.Claim,
            "objection" => JournalQuestion.Objection,
            "declare" => JournalQuestion.Declare,
            var unknown => throw new JournalException($"Line {number}: '{unknown}' is not one of the five questions.")
        };

        DecisionSnapshot? snapshot = null;

        if (root.TryGetProperty("hand", out var hand) && hand.ValueKind == JsonValueKind.Array)
        {
            snapshot = new DecisionSnapshot(
                [.. hand.EnumerateArray().Select(card => new CardId(card.GetInt32()))],
                OptionalNumber(root, "discard") is { } discard ? new CardId(discard) : null,
                OptionalNumber(root, "taken") is { } taken ? new CardId(taken) : null,
                OptionalNumber(root, "draw_pile") ?? 0);
        }

        return new JournalDecision(
            Number(root, "round", number),
            Number(root, "turn", number),
            new PlayerId(Number(root, "player", number)),
            question,
            Text(root, "answer", number),
            snapshot);
    }

    /// <remarks>
    /// 🔥 <b>Every case is named and there is no catch-all, which is a correction rather than a
    /// style.</b> This ended <c>_ =&gt; "declare"</c>, so when P28 added a fifth question the
    /// writer silently wrote every objection down as a declaration — <b>a journal that read back
    /// as a different game</b>, and one the in-memory replay could not see because it never
    /// crosses this method. A serializer's default arm is a mistranslation waiting for the next
    /// case.
    /// </remarks>
    private static string Name(JournalQuestion question) => question switch
    {
        JournalQuestion.Action => "action",
        JournalQuestion.Discard => "discard",
        JournalQuestion.Claim => "claim",
        JournalQuestion.Objection => "objection",
        JournalQuestion.Declare => "declare",
        _ => throw new JournalException($"There is no name for the {question} question.")
    };

    /// <summary>
    /// Turns anything a malformed line can throw into a <see cref="JournalException"/> that
    /// names the line. <b>A corrupt journal is a journal problem</b>, not an
    /// <see cref="InvalidOperationException"/> out of a JSON reader.
    /// </summary>
    private static T Guarded<T>(Func<T> read, int number)
    {
        try
        {
            return read();
        }
        catch (Exception problem) when (problem is InvalidOperationException or FormatException or ArgumentException)
        {
            throw new JournalException($"Line {number} is not a readable journal line: {problem.Message}");
        }
    }

    private static JsonDocument Parse(string line, int number)
    {
        try
        {
            return JsonDocument.Parse(line);
        }
        catch (JsonException problem)
        {
            throw new JournalException($"Line {number} is not readable JSON: {problem.Message}");
        }
    }

    private static string Text(JsonElement element, string property, int number) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new JournalException($"Line {number} has no '{property}'.");

    private static string? OptionalText(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int Number(JsonElement element, string property, int number) =>
        OptionalNumber(element, property)
            ?? throw new JournalException($"Line {number} has no whole number '{property}'.");

    /// <remarks>
    /// The kind is checked before the value is read: <see cref="JsonElement.TryGetInt32"/>
    /// <em>throws</em> rather than returning false when the element is a string or a null, and
    /// a corrupt journal must fail as a <see cref="JournalException"/> naming its line.
    /// </remarks>
    private static int? OptionalNumber(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var read)
            ? read
            : null;

    private static void Append(StringBuilder line, string property, int value) =>
        line.Append(",\"").Append(property).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));

    private static void Append(StringBuilder line, string property, int? value)
    {
        line.Append(",\"").Append(property).Append("\":");

        if (value is { } number)
        {
            line.Append(number.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            line.Append("null");
        }
    }

    private static void Append(StringBuilder line, string property, bool value) =>
        line.Append(",\"").Append(property).Append("\":").Append(value ? "true" : "false");

    private static void Append(StringBuilder line, string property, string? value)
    {
        line.Append(",\"").Append(property).Append("\":");

        if (value is null)
        {
            line.Append("null");
        }
        else
        {
            // Escaped by the JSON writer's own encoder rather than by hand: a player types
            // their own name at the console, and a quote in it must not break the file.
            line.Append('"').Append(JsonEncodedText.Encode(value).ToString()).Append('"');
        }
    }
}
