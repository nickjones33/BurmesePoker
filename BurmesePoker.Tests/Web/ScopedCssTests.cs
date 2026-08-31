using System.Text.RegularExpressions;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>A scoped stylesheet says what the browser will actually be given.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This class exists because a rule can be written, compiled, shipped, deployed and
/// discarded, and nothing anywhere notices.</b> P54 revealed the lobby's copy-link button with
/// <c>:global(.can-copy) .link .copy</c>. <c>:global()</c> is a <b>CSS-Modules</b> construct;
/// Blazor's CSS isolation has no such thing (its escape hatch is <c>::deep</c>), so the Razor
/// rewriter passed it through verbatim and every browser dropped the whole rule as an invalid
/// selector. The button was never once visible — from P54 until P61 — and the tree stayed green
/// throughout, because a stylesheet is not code and nothing here had ever read one as output.
/// </para>
/// <para>
/// ⚠️ <b>So these read the *generated* stylesheets under <c>obj/**/scopedcss/**</c>, not the
/// authored ones.</b> The rewrite is the step where a selector stops being what somebody typed
/// and becomes what a browser is asked to parse, and it is the only place the mistake is
/// visible. The configuration read is the one the test binaries were built in, so the file
/// examined belongs to the build that is running.
/// </para>
/// </remarks>
public class ScopedCssTests
{
    /// <summary>
    /// 🔥 <b>The class of mistake, not the one line.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b><c>:global()</c> reads like the obvious thing to write and is silently wrong here</b>
    /// — it is valid CSS Modules, it survives the build, and it fails only in the browser, where
    /// nothing in this project can see it. One assertion over every scoped stylesheet catches
    /// every future instance, which is worth more than a fence around the button that taught it.
    /// </remarks>
    [Fact]
    public void NoScopedStylesheetAsksTheBrowserToParseSomethingItWillDiscard()
    {
        var offenders = Generated()
            .Where(sheet => sheet.Text.Contains(":global(", StringComparison.Ordinal))
            .Select(sheet => sheet.Name)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            ":global() is CSS Modules, not Blazor CSS isolation — the rewriter emits it verbatim and the "
                + "browser discards the whole rule, so the declarations inside it never apply to anything. "
                + "Blazor's escape hatch is ::deep, and an ancestor outside the component needs no hatch at "
                + "all: only the last compound selector is scoped. It is in "
                + string.Join(", ", offenders) + ".");
    }

    /// <summary>
    /// The positive twin: the button the lobby draws is revealed by a rule that is really in the
    /// stylesheet the browser is served.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Read out of the bundle rather than out of the component's own file</b>, because the
    /// bundle is the artifact <c>App.razor</c> links and a browser downloads. And the reveal has
    /// to come <em>after</em> the <c>display: none</c> default: the two selectors weigh the same,
    /// so order is the whole of why one wins.
    /// </remarks>
    [Fact]
    public void TheCopyLinkIsRevealedByARuleTheBrowserWillKeep()
    {
        var bundle = Bundle();

        var hidden = Reveal(bundle, @"\.link\s+\.copy\[[^\]]+\]\s*\{[^}]*display:\s*none");
        var shown = Reveal(bundle, @"\.can-copy\s+\.link\s+\.copy\[[^\]]+\]\s*\{[^}]*display:\s*inline-block");

        Assert.True(
            hidden >= 0,
            "The copy button is hidden until the script that makes it work has run, and that default is gone.");

        Assert.True(
            shown >= 0,
            "Nothing in the served stylesheet reveals .copy under .can-copy, so the button the lobby draws "
                + "cannot ever appear. The <a class=\"url\"> beside it still works — which is exactly why a "
                + "broken reveal reads as nothing being wrong.");

        Assert.True(
            shown > hidden,
            "The reveal weighs the same as the default that hides it, so it only wins by being declared after "
                + $"it. It is at {shown} and the default is at {hidden}.");
    }

    /// <summary>Where in the bundle a rule matching this shape starts, or -1.</summary>
    private static int Reveal(string bundle, string pattern)
    {
        var match = Regex.Match(bundle, pattern, RegexOptions.Singleline);

        return match.Success ? match.Index : -1;
    }

    /// <summary>The stylesheet the browser is actually served, out of the running build.</summary>
    private static string Bundle()
    {
        var bundle = new FileInfo(Path.Combine(
            ScopedCss().FullName, "bundle", "BurmesePoker.Web.styles.css"));

        Assert.True(bundle.Exists, $"There is no scoped-CSS bundle at {bundle.FullName} to read.");

        return File.ReadAllText(bundle.FullName);
    }

    /// <summary>
    /// Every component's rewritten stylesheet, by component, with the commentary taken out.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The prose has to go before the scan reads it</b> — <c>Tables.razor.css</c> now
    /// explains, in a comment beside the rule, exactly which construct is not available here, and
    /// a scan that read the explanation would fail on the file that had learned the lesson.
    /// Sources.Markup's rule, arriving in a stylesheet.
    /// </remarks>
    private static IReadOnlyList<(string Name, string Text)> Generated()
    {
        var sheets = ScopedCss()
            .GetFiles("*.rz.scp.css", SearchOption.AllDirectories)
            .OrderBy(file => file.FullName, StringComparer.Ordinal)
            .Select(file => (file.Name, Sources.Markup(File.ReadAllText(file.FullName))))
            .ToArray();

        Assert.NotEmpty(sheets);

        return sheets;
    }

    /// <summary>
    /// The generated-stylesheet directory of the build these tests were compiled into.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The configuration is taken from where this assembly is running</b>, so a stale
    /// <c>obj/Release</c> left behind by an old <c>sim</c> run cannot fail a Debug build, and a
    /// Release test run reads Release.
    /// </remarks>
    private static DirectoryInfo ScopedCss()
    {
        var moniker = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = moniker.Parent
            ?? throw new InvalidOperationException($"{AppContext.BaseDirectory} is not inside a bin/<config>/<tfm>.");

        var scoped = new DirectoryInfo(Path.Combine(
            Sources.Web.FullName, "obj", configuration.Name, moniker.Name, "scopedcss"));

        Assert.True(
            scoped.Exists,
            $"No generated scoped CSS at {scoped.FullName}. These tests read the rewriter's output, which is "
                + "the only place a selector Blazor cannot express is visible.");

        return scoped;
    }
}
