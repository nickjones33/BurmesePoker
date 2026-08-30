using System.Globalization;
using System.Text.RegularExpressions;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>§3.11 A18 — the table is playable at 360 px, and the felt is a ring only above one
/// stated width.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>There is exactly one breakpoint in this application and it was written down twice</b> —
/// <c>TableView.razor.css</c> stacks the felt at it and <c>SeatPanel.razor.css</c> reshapes the
/// panel at it — <em>with a comment asserting the two agree</em>. ⚠️ <b>A CSS custom property
/// cannot carry a breakpoint</b>: <c>@media</c> cannot read <c>var()</c>. So the two stay
/// ordinary CSS and this fences them against each other, which is P54's idiom exactly (the
/// patience and the circuit-retention window live in two files and are proved to agree rather
/// than merged into one).
/// </para>
/// <para>
/// 🔥 <b>And 360 px is arithmetic rather than taste.</b> The felt stacks into
/// <c>repeat(auto-fit, minmax(floor, 1fr))</c>, so one column of it plus the felt's own padding
/// plus the page's must fit the narrowest phone still in real use. Measured with the numbers in
/// the stylesheets rather than with a browser, so a padding raised in one file cannot quietly
/// push the table off the side of a phone.
/// </para>
/// </remarks>
public class ViewportTests
{
    /// <summary>The narrowest screen the table is required to be playable on, in CSS pixels.</summary>
    private const double NarrowestPhone = 360;

    /// <summary>CSS pixels to the rem, at the browser default this client never overrides.</summary>
    private const double Rem = 16;

    [Fact]
    public void TheFeltAndTheSeatPanelStackAtTheSameWidth()
    {
        var declared = Breakpoints();

        Assert.NotEmpty(declared);

        Assert.True(
            declared.Select(line => line.Width).Distinct(StringComparer.Ordinal).Count() == 1,
            "The client has one breakpoint. It is declared at "
                + string.Join(", ", declared.Select(line => $"{line.Width} in {line.File}"))
                + ", and @media cannot read a custom property, so agreeing is the only way they can be one decision.");

        Assert.Equal(
            ["Components/Table/SeatPanel.razor.css", "Components/Table/TableView.razor.css"],
            declared.Select(line => line.File).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void TheStackedFeltFitsTheNarrowestPhoneStillInUse()
    {
        var floor = Length(
            Regex.Match(Stacked(Sources.Read("Components/Table/TableView.razor.css")), @"minmax\(\s*([0-9.]+rem)\s*,").Groups[1].Value);

        var felt = Sides(Stacked(Sources.Read("Components/Table/TableView.razor.css")), ".felt");
        var page = Sides(Sources.Read("Components/Layout/MainLayout.razor.css"), "main");

        var needed = floor + felt + page;

        Assert.True(
            needed <= NarrowestPhone,
            $"One column of the stacked felt asks for {needed}px — {floor} of column, {felt} of felt padding "
                + $"and {page} of page padding — and the table has to be playable at {NarrowestPhone}px.");
    }

    /// <summary>
    /// 🔥 <b>The name is trimmed above the breakpoint and wraps below it, and the asymmetry is
    /// the standard.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>An ellipsis is only honest where the whole name is a hover away.</b> Above the line
    /// it is: the name carries <c>title=</c> and a person may type twenty-four characters into a
    /// seat. Below it the layout exists for a screen with no pointer on it, and a column of the
    /// stacked pack can be as narrow as <c>9.5rem</c> — measured at 412 px, and again from
    /// 600 px to 896 px, trimming the computer's <em>own</em> seat names. That is the defect the
    /// 56rem line was drawn to prevent, arriving underneath it.
    /// </remarks>
    [Fact]
    public void NoNameIsTrimmedOnceTheFeltHasStacked()
    {
        var panel = Sources.Read("Components/Table/SeatPanel.razor.css");

        Assert.Contains("white-space: nowrap", Rule(panel[..panel.IndexOf("@media", StringComparison.Ordinal)], ".name"), StringComparison.Ordinal);

        var stacked = Rule(Stacked(panel), ".name");

        Assert.True(
            stacked.Contains("white-space: normal", StringComparison.Ordinal),
            "Stacked, a seat's name must wrap rather than ellipse — there is no hover on the screens this "
                + $"layout is for, so a trimmed name is one nobody can read. The rule says: {stacked}");

        Assert.Contains("overflow: visible", stacked, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePageIsLaidOutForTheDeviceItIsOn()
    {
        Assert.Contains("width=device-width", Sources.Read("Components/App.razor"), StringComparison.Ordinal);
    }

    /// <summary>Every width breakpoint in the client's stylesheets, with the file that declares it.</summary>
    private static IReadOnlyList<(string File, string Width)> Breakpoints() =>
    [
        .. Sources.Web.GetFiles("*.css", SearchOption.AllDirectories)
            .Where(file => !file.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(file => file.FullName, StringComparer.Ordinal)
            .SelectMany(file => Regex
                .Matches(Sources.Markup(File.ReadAllText(file.FullName)), @"@media\s*\(\s*(?:max|min)-width:\s*([^)]+?)\s*\)")
                .Select(match => (
                    File: Path.GetRelativePath(Sources.Web.FullName, file.FullName).Replace(Path.DirectorySeparatorChar, '/'),
                    Width: match.Groups[1].Value)))
    ];

    /// <summary>The body of a stylesheet's width-breakpoint block.</summary>
    private static string Stacked(string css)
    {
        var start = css.IndexOf("@media", StringComparison.Ordinal);

        Assert.True(start >= 0, "There is no breakpoint in this stylesheet at all.");

        var depth = 0;

        for (var index = css.IndexOf('{', start); index < css.Length; index++)
        {
            depth += css[index] switch { '{' => 1, '}' => -1, _ => 0 };

            if (depth == 0)
            {
                return css[start..(index + 1)];
            }
        }

        throw new InvalidOperationException("The breakpoint block is never closed.");
    }

    /// <summary>One declaration block, by selector, out of a stretch of CSS.</summary>
    private static string Rule(string css, string selector)
    {
        var match = Regex.Match(css, Regex.Escape(selector) + @"\s*\{([^}]*)\}");

        Assert.True(match.Success, $"No {selector} rule here, and this test is about what it says.");

        return match.Groups[1].Value;
    }

    /// <summary>What a selector's <c>padding</c> costs on the two sides together, in CSS pixels.</summary>
    private static double Sides(string css, string selector)
    {
        var padding = Regex.Match(Rule(css, selector), @"padding:\s*([^;]+);");

        Assert.True(padding.Success, $"{selector} declares no padding, and the arithmetic needs it.");

        var parts = padding.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return 2 * Length(parts.Length > 1 ? parts[1] : parts[0]);
    }

    /// <summary>A CSS length in <c>rem</c> or <c>px</c>, as CSS pixels.</summary>
    private static double Length(string value)
    {
        Assert.True(value.Length > 0, "An empty length is not a length.");

        var number = double.Parse(
            value.TrimEnd('r', 'e', 'm', 'p', 'x'),
            CultureInfo.InvariantCulture);

        return value.EndsWith("rem", StringComparison.Ordinal) ? number * Rem : number;
    }
}
