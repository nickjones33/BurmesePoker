using System.Globalization;
using System.Text.RegularExpressions;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>§3.11 A3 — contrast is computed, never eyeballed.</b>
/// </summary>
/// <remarks>
/// <para>
/// WCAG 1.4.3 and 1.4.11: at least <b>4.5:1</b> for body text and at least <b>3:1</b> for the
/// boundaries of interactive components. <em>"Tedious by hand, trivial in code, and the single
/// most commonly skipped item in the standard"</em> — so it is a test, and it runs in
/// <b>both themes</b>, because a palette that passes in the light one and fails in the dark
/// one is a palette that fails.
/// </para>
/// <para>
/// 🔥 <b>It reads <c>wwwroot/theme.css</c>, which is the file the browser loads.</b> The
/// alternative — a palette declared in C# and mirrored into CSS — passes this test while the
/// page shows something else. The naming convention is what makes the pairs discoverable:
/// <c>--on-x</c> is drawn on <c>--x</c> and <c>--edge-x</c> is a line on it, and a token's
/// base is the longest declared name its own name begins with. So <b>a token added later is
/// measured without anybody remembering to list it</b>, and one whose base does not exist
/// fails rather than being skipped.
/// </para>
/// </remarks>
public class PaletteContrastTests
{
    private const double Text = 4.5;
    private const double Boundary = 3.0;

    public static TheoryData<string> Themes => new("light", "dark");

    [Theory]
    [MemberData(nameof(Themes))]
    public void EveryForegroundReachesItsBackground(string theme)
    {
        var palette = Palette(theme);

        Assert.NotEmpty(palette);

        foreach (var (token, colour) in palette)
        {
            var (prefix, need) = token switch
            {
                _ when token.StartsWith("on-", StringComparison.Ordinal) => ("on-", Text),
                _ when token.StartsWith("edge-", StringComparison.Ordinal) => ("edge-", Boundary),
                _ => (string.Empty, 0d)
            };

            if (prefix.Length == 0)
            {
                continue;
            }

            var background = BaseOf(palette, token[prefix.Length..]);

            Assert.True(
                background is not null,
                $"--{token} is drawn on something, and no --{token[prefix.Length..]} is declared to say what.");

            var ratio = Contrast(colour, palette[background!]);

            Assert.True(
                ratio >= need,
                $"{theme}: --{token} ({colour}) on --{background} ({palette[background!]}) is "
                + $"{ratio.ToString("0.00", CultureInfo.InvariantCulture)}:1, and needs "
                + $"{need.ToString("0.0", CultureInfo.InvariantCulture)}:1.");
        }
    }

    /// <remarks>
    /// A guard on the guard: the two themes must not be the same file read twice, and every
    /// token declared in one must be declared in the other. A dark theme that forgot a token
    /// inherits the light one's, which is how a dark page ends up with black text on it.
    /// </remarks>
    [Fact]
    public void BothThemesAreCompleteAndDifferent()
    {
        var light = Palette("light");
        var dark = Palette("dark");

        Assert.Equal(light.Keys.Order(StringComparer.Ordinal), dark.Keys.Order(StringComparer.Ordinal));
        Assert.All(light, entry => Assert.NotEqual(entry.Value, dark[entry.Key]));
    }

    /// <remarks>
    /// The page is drawn out of these and not out of anything else: every <c>var(--…)</c> in
    /// the client's stylesheets names a token this palette declares. A typo in a custom
    /// property is silent in CSS — the declaration is simply dropped — so it is caught here.
    /// </remarks>
    [Fact]
    public void EveryColourTheClientUsesIsDeclared()
    {
        var declared = Palette("light");
        var used = new List<(string File, string Token)>();

        foreach (var file in Sources.Web.GetFiles("*.css", SearchOption.AllDirectories))
        {
            if (file.Name == "theme.css")
            {
                continue;
            }

            used.AddRange(
                Regex.Matches(File.ReadAllText(file.FullName), @"var\(\s*--([a-z0-9-]+)\s*\)")
                    .Select(match => (file.Name, match.Groups[1].Value)));
        }

        Assert.NotEmpty(used);
        Assert.All(used, use => Assert.Contains(use.Token, declared.Keys));
    }

    /// <summary>
    /// The declared colours of one theme: the light block, with the dark block's overrides
    /// applied on top of it exactly as the cascade applies them.
    /// </summary>
    private static Dictionary<string, string> Palette(string theme)
    {
        var css = Sources.Read("wwwroot/theme.css");
        var dark = css.IndexOf("@media (prefers-color-scheme: dark)", StringComparison.Ordinal);

        Assert.True(dark > 0, "theme.css declares no dark theme, and §3.11 B10 asks for one.");

        var colours = Declarations(css[..dark]);

        if (theme == "dark")
        {
            foreach (var (token, colour) in Declarations(css[dark..]))
            {
                colours[token] = colour;
            }
        }

        return colours;
    }

    private static Dictionary<string, string> Declarations(string css) =>
        Regex.Matches(css, @"--([a-z0-9-]+)\s*:\s*(#[0-9a-fA-F]{6})\s*;")
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value.ToLowerInvariant());

    /// <summary>
    /// The background a foreground token names: the longest declared token its own name begins
    /// with, so <c>--on-raised-muted</c> is measured against <c>--raised</c>.
    /// </summary>
    private static string? BaseOf(Dictionary<string, string> palette, string rest) => palette.Keys
        .Where(token => token == rest || rest.StartsWith(token + "-", StringComparison.Ordinal))
        .OrderByDescending(token => token.Length)
        .FirstOrDefault();

    /// <summary>The WCAG contrast ratio of two sRGB colours.</summary>
    private static double Contrast(string first, string second)
    {
        var (a, b) = (Luminance(first), Luminance(second));
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>Relative luminance, WCAG 2.x's definition exactly.</summary>
    private static double Luminance(string hex)
    {
        var channels = new[] { 1, 3, 5 }
            .Select(at => int.Parse(hex.AsSpan(at, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d)
            .Select(value => value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4))
            .ToArray();

        return (0.2126 * channels[0]) + (0.7152 * channels[1]) + (0.0722 * channels[2]);
    }
}
