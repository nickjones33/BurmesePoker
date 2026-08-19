using System.Text.RegularExpressions;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>§3.11 A4, C12, C14 and C15 — the standards that live in the markup.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A source scan, "in the same spirit as <c>LayeringTests</c>"</b> (BUILD-PLAN §3.11 A4).
/// These are not style preferences: each one is a decision that costs a rewrite of every
/// component if it is discovered late, which is precisely why the list was written down before
/// the first component existed.
/// </para>
/// <para>
/// A scan is coarser than a parser and deliberately so — it fails on the *shape* of the
/// mistake. What it cannot catch, playing it catches (the §3.11 B list).
/// </para>
/// </remarks>
public class MarkupStandardsTests
{
    /// <summary>Elements that are already operable by keyboard and named to assistive technology.</summary>
    private static readonly string[] RealControls =
        ["button", "a", "input", "select", "textarea", "summary", "details", "label", "form"];

    [Fact]
    public void ThereAreComponentsToCheck()
    {
        Assert.NotEmpty(Sources.Components);
        Assert.Contains(Sources.Components, file => file.Path.EndsWith("TableView.razor", StringComparison.Ordinal));
    }

    /// <summary>
    /// ✅ <b>A4 — every action is a real control.</b> No <c>&lt;div @onclick&gt;</c>: a button
    /// is a <c>&lt;button&gt;</c> and navigation is an <c>&lt;a&gt;</c>, so keyboard, screen
    /// readers and Blazor's own enhanced navigation all work without special cases.
    /// </summary>
    [Fact]
    public void EveryHandlerIsOnARealControl()
    {
        foreach (var (path, text) in Sources.Components)
        {
            foreach (Match handler in Regex.Matches(text, @"@on[a-z]+\s*=" ))
            {
                var open = text.LastIndexOf('<', handler.Index);

                Assert.True(open >= 0, $"{path}: an event handler outside any element.");

                var tag = Regex.Match(text[open..], @"^<\s*([A-Za-z][A-Za-z0-9]*)").Groups[1].Value;

                // A capitalised tag is another Razor component, which is checked where it is
                // defined rather than where it is used.
                Assert.True(
                    char.IsUpper(tag[0]) || RealControls.Contains(tag, StringComparer.OrdinalIgnoreCase),
                    $"{path}: <{tag}> takes a handler, and a handler belongs on a real control (§3.11 A4).");
            }
        }
    }

    /// <summary>
    /// ✅ <b>C12 — static SSR is the default and interactivity is opted into per component.</b>
    /// <em>"The one decision on the list that cannot be walked back cheaply."</em>
    /// </summary>
    [Fact]
    public void TheRootIsNotInteractive()
    {
        foreach (var root in new[] { "Components/App.razor", "Components/Routes.razor" })
        {
            Assert.DoesNotContain("@rendermode", Sources.Read(root), StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <remarks>
    /// And the opting-in really is per component: the render mode is named where a component is
    /// <em>used</em>, and the table is the only thing in the client that asks for one.
    /// </remarks>
    [Fact]
    public void OnlyTheTableIsInteractive()
    {
        var interactive = Sources.Components
            .Where(file => file.Text.Contains("@rendermode", StringComparison.OrdinalIgnoreCase))
            .Select(file => file.Path)
            .ToList();

        Assert.Single(interactive);
        Assert.EndsWith("Watch.razor", interactive[0], StringComparison.Ordinal);
        Assert.Contains("<TableView @rendermode=\"InteractiveServer\" />", Sources.Read("Components/Pages/Watch.razor"));
    }

    /// <summary>
    /// ✅ <b>C14 — <c>@key</c> on every card and every seat.</b> Without it the diff reorders
    /// DOM nodes and drags focus with them, and it shows up exactly when a hand is re-sorted
    /// after a draw — which this game does constantly.
    /// </summary>
    [Fact]
    public void EveryLoopKeysWhatItRepeats()
    {
        var loops = 0;

        foreach (var (path, text) in Sources.Components)
        {
            foreach (Match loop in Regex.Matches(text, @"@foreach\s*\("))
            {
                loops++;

                // The element the loop repeats is the first one it opens — checked precisely
                // rather than by looking nearby, because a nested loop's own @key would
                // otherwise cover for an outer element that has none.
                var body = text.IndexOf('{', loop.Index);
                var open = text.IndexOf('<', body);
                var close = text.IndexOf('>', open);

                Assert.True(body > 0 && open > 0 && close > open, $"{path}: a @foreach repeating no element.");

                var element = text[open..close];

                Assert.True(
                    element.Contains("@key", StringComparison.Ordinal),
                    $"{path}: `{Summarise(element)}` is repeated without an @key (§3.11 C14).");
            }
        }

        Assert.True(loops >= 4, $"only {loops} loops scanned, which is fewer than the table has.");
    }

    private static string Summarise(string element) =>
        string.Join(' ', element.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Take(3));

    /// <summary>
    /// ✅ <b>A5's warning — never <c>StateHasChanged</c> from <c>Dispose</c>.</b> Disposal can
    /// run during renderer teardown, where requesting a render is explicitly unsupported.
    /// </summary>
    [Fact]
    public void NothingRendersFromItsOwnDisposal()
    {
        foreach (var (path, text) in Sources.Components)
        {
            foreach (Match disposal in Regex.Matches(text, @"(public|protected)[^\n]*\bDispose(Async)?\s*\("))
            {
                var body = text[disposal.Index..Math.Min(text.Length, disposal.Index + 400)];

                Assert.DoesNotContain("StateHasChanged", body, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// ✅ <b>C15 — the observer stream re-renders through <c>InvokeAsync</c>.</b> Every move at
    /// the table arrives on the round's thread, not the renderer's.
    /// </summary>
    [Fact]
    public void TheTableRendersTheStreamThroughInvokeAsync()
    {
        var table = Sources.Read("Components/Table/TableView.razor");

        Assert.Contains("InvokeAsync(StateHasChanged)", table, StringComparison.Ordinal);

        // Every StateHasChanged in the client is inside an InvokeAsync — there is no other
        // safe way to render what the table said.
        foreach (var (path, text) in Sources.Components)
        {
            foreach (Match render in Regex.Matches(text, @"StateHasChanged"))
            {
                var before = text[Math.Max(0, render.Index - 20)..render.Index];

                Assert.True(
                    before.Contains("InvokeAsync(", StringComparison.Ordinal),
                    $"{path}: StateHasChanged called outside InvokeAsync (§3.11 C15).");
            }
        }
    }

    /// <summary>
    /// ✅ <b>B8 — the round log is a polite live region, and the only one.</b> A bot table says
    /// something every second or so; <c>assertive</c> would interrupt a screen reader
    /// continuously. The hand and the seats are not live regions.
    /// </summary>
    [Fact]
    public void TheLogIsTheOnlyLiveRegionAndItIsPolite()
    {
        var live = Sources.Components
            .Where(file => file.Text.Contains("aria-live", StringComparison.Ordinal))
            .ToList();

        Assert.Single(live);
        Assert.EndsWith("RoundLogPanel.razor", live[0].Path, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", live[0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("assertive", live[0].Text, StringComparison.Ordinal);
    }
}
