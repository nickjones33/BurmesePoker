using Spectre.Console;

namespace BurmesePoker.Console;

/// <summary>
/// The console's colour and marker language, defined once.
/// </summary>
/// <remarks>
/// <para>
/// P8 invented its markers where it needed them — <c>($)</c>, <c>($$)</c> and <c>★</c> in
/// <see cref="CardFormatting"/>, greens and reds for money in <c>Program</c>, yellow for the
/// reshuffle in <see cref="ConsoleObserver"/>. That is three files agreeing by coincidence.
/// P11 keeps every one of those choices and moves them here, so a screen added later inherits
/// the language instead of inventing a fourth dialect (BUILD-PLAN P11).
/// </para>
/// <para>
/// <b>The rule the colours follow:</b> yellow is money the table is paying attention to,
/// green is <em>yours</em> or <em>good for you</em>, red is money leaving you, grey is context
/// you are not being asked to act on. A card's own red or silver is the card's colour and
/// means nothing else (RULES.md §2.2).
/// </para>
/// </remarks>
public static class Palette
{
    /// <summary>Money markers and anything the money layer is drawing attention to.</summary>
    public const string Money = "yellow";

    /// <summary>Yours, or in your favour: an owned money card, a bank in credit, the winner.</summary>
    public const string Good = "green";

    /// <summary>Money leaving you.</summary>
    public const string Bad = "red";

    /// <summary>Context: things that are true but are not being asked about.</summary>
    public const string Quiet = "grey";

    /// <summary>Panel and rule borders.</summary>
    public static readonly Color Frame = Color.Grey;

    /// <summary>Marks a money card the deck gave this player, so it pays them whoever holds it.</summary>
    public const string OwnedMark = "★";

    /// <summary>Marks the card the computer would throw away, when hints are on.</summary>
    public const string AdviceMark = "←";

    /// <summary>What the markers mean, for a footer under a hand.</summary>
    public const string Legend =
        $"[{Quiet}]($) pays once · ($$) pays double ·[/] [{Good}]{OwnedMark}[/][{Quiet}] the deck gave it "
        + "to you, so it pays you even after you throw it[/]";

    /// <summary>An amount of money, signed and coloured from the point of view of whoever holds it.</summary>
    public static string Amount(int money) => money switch
    {
        > 0 => $"[{Good}]+${money}[/]",
        < 0 => $"[{Bad}]-${-money}[/]",
        _ => $"[{Quiet}]$0[/]"
    };
}
