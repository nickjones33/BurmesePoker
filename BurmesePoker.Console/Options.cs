using System.Globalization;

using Spectre.Console;

namespace BurmesePoker.Console;

/// <summary>
/// What the command line asked for.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately tiny. Everything about the <em>table</em> — how many seats, how many people,
/// what the stakes are, how well the computer plays — is asked at the prompt, because that is
/// where a player already is. The flags are for the two things a prompt cannot do: replay a
/// match that has already happened, and change how the console itself behaves.
/// </para>
/// <para>
/// <b>The seed is never absent.</b> One is drawn if none is given, so the match just played can
/// always be asked for again (BUILD-PLAN P11) — an option that only sometimes reproduces a
/// match is worth much less than one that always does.
/// </para>
/// </remarks>
internal sealed record Options(int Seed, TimeSpan Pace, bool Hints, bool Help)
{
    /// <summary>How long the computer appears to think, when nothing says otherwise.</summary>
    public static readonly TimeSpan DefaultPace = TimeSpan.FromMilliseconds(450);

    public static bool TryParse(string[] args, out Options options, out string complaint)
    {
        var seed = Random.Shared.Next();
        var pace = DefaultPace;
        var hints = true;
        var help = false;

        complaint = string.Empty;
        options = new Options(seed, pace, hints, help);

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--seed":
                    if (!TryValue(args, ref index, out var seedText) || !int.TryParse(seedText, CultureInfo.InvariantCulture, out seed))
                    {
                        complaint = "--seed wants a whole number, e.g. --seed 1234.";
                        return false;
                    }

                    break;

                case "--pace":
                    if (!TryValue(args, ref index, out var paceText)
                        || !int.TryParse(paceText, CultureInfo.InvariantCulture, out var milliseconds)
                        || milliseconds < 0)
                    {
                        complaint = "--pace wants milliseconds, e.g. --pace 250. Zero means no pause at all.";
                        return false;
                    }

                    pace = TimeSpan.FromMilliseconds(milliseconds);
                    break;

                case "--no-hints":
                    hints = false;
                    break;

                case "--help" or "-h":
                    help = true;
                    break;

                default:
                    complaint = $"I do not know what {Markup.Escape(args[index])} means.";
                    return false;
            }
        }

        options = new Options(seed, pace, hints, help);
        return true;
    }

    public static void Usage()
    {
        var grid = new Grid().AddColumn().AddColumn();

        grid.AddRow("[bold]--seed <n>[/]", "Play a particular match. One is drawn and printed if you do not give one.");
        grid.AddRow("[bold]--pace <ms>[/]", $"How long a computer seat pauses before it moves. Default {DefaultPace.TotalMilliseconds:0}; 0 for none.");
        grid.AddRow("[bold]--no-hints[/]", "Stop the console telling you what the computer would do.");
        grid.AddRow("[bold]--help[/]", "This.");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(grid).Header("dotnet run --project BurmesePoker.Console --").BorderColor(Palette.Frame));
    }

    private static bool TryValue(string[] args, ref int index, out string value)
    {
        if (index + 1 >= args.Length)
        {
            value = string.Empty;
            return false;
        }

        value = args[++index];
        return true;
    }
}
