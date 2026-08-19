using BurmesePoker.Presentation;

using Spectre.Console;
using Spectre.Console.Rendering;

namespace BurmesePoker.Console;

/// <summary>
/// A <see cref="HandView"/> drawn as a panel: one line per meld, then the loose cards, headed
/// by the score.
/// </summary>
/// <remarks>
/// <para>
/// <b>This file draws and decides nothing</b> (BUILD-PLAN P13.1). Which cards group into which
/// near-meld, what each one costs to throw and what state each is in are all
/// <see cref="HandView"/>'s answers, computed in a project that has never heard of Spectre; a
/// browser asks the same questions and gets the same answers. What is left here — panels,
/// markup, the width the labels pad to — is the part a Razor client shares none of, which is
/// exactly why it stayed behind.
/// </para>
/// <para>
/// It replaces the console's own <c>HandView</c>, which answered both halves in one breath.
/// The rendering is unchanged to the byte.
/// </para>
/// </remarks>
internal static class HandPanel
{
    /// <summary>The hand as a panel.</summary>
    public static IRenderable Of(HandView view)
    {
        var rows = new List<IRenderable>();

        foreach (var meld in view.Melds)
        {
            rows.Add(new Markup(CardFormatting.Meld(meld)));
        }

        if (view.Loose.Count > 0)
        {
            rows.Add(new Markup(Loose(view.Loose)));
        }

        var header = view.IsComplete
            ? $"Your hand — [{Palette.Good}]all {view.Count} meld[/]"
            : $"Your hand — {view.Covered} of {view.Count} meld";

        return new Panel(new Rows(rows)).Header(header).BorderColor(Palette.Frame);
    }

    /// <summary>The deadwood, labelled to line up with the meld rows above it.</summary>
    private static string Loose(IReadOnlyList<CardView> cards)
    {
        var drawn = cards.Select(CardFormatting.Of);

        return $"[{Palette.Quiet}]{CardFormatting.LooseLabel}[/] {string.Join("  ", drawn)}";
    }
}
