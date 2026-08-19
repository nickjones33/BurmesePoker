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
        var handlers = 0;

        foreach (var (path, text) in Sources.Components)
        {
            foreach (Match handler in Regex.Matches(text, @"@on[a-z]+\s*=" ))
            {
                handlers++;

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

        // ⚠️ Until P13.4 there was not one control on the table and this passed vacuously, which
        // is a test that says nothing. The seat asks four questions; the smallest of them puts
        // two buttons on the page.
        Assert.True(handlers >= 6, $"only {handlers} handlers scanned, which is fewer than the seat has.");
    }

    /// <summary>
    /// ✅ <b>P13.4 acceptance 3 — the component does not widen the concealment.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The leak test guards what is <em>sent</em>; this guards that the page has nothing
    /// else to draw.</b> Everything on screen came through a <c>SeatConnection</c> — the public
    /// board out of the watcher's events, your hand out of the <c>SeatPrompt</c> your own seat
    /// was asked. A component that reached the <c>TableSession</c>, or a <c>TableState</c>, or
    /// a <c>TurnContext</c>, would be a second route round the fan-out, and
    /// <c>ConcealmentTests</c> would stop standing in front of the page.
    /// </para>
    /// <para>
    /// ⚠️ <b>It also forbids working a hand out again.</b> <c>PartialCover</c> and
    /// <c>HandEvaluator</c> are how a <c>HandView</c> is built, and the client is given one
    /// rather than building one (P13.1).
    /// </para>
    /// </remarks>
    [Fact]
    public void NoComponentFindsASecondRouteToTheTable()
    {
        string[] forbidden =
            ["TableSession", "MatchEngine", "TableState", "TurnContext", "PartialCover", "HandEvaluator", "ConnectionFor"];

        foreach (var (path, text) in Sources.Components)
        {
            foreach (var route in forbidden)
            {
                Assert.False(
                    text.Contains(route, StringComparison.Ordinal),
                    $"{path}: names {route}, which is a way round the fan-out (BUILD-PLAN P13.4).");
            }
        }
    }

    /// <remarks>
    /// And the other half of it, stated positively: the seat's own components draw the hand from
    /// the prompt they were handed and from the view model in it.
    /// </remarks>
    [Fact]
    public void TheSeatDrawsTheHandItWasSent()
    {
        Assert.Contains("SeatPrompt", Sources.Read("Components/Table/TurnPrompt.razor"), StringComparison.Ordinal);
        Assert.Contains("HandView", Sources.Read("Components/Table/HandPanel.razor"), StringComparison.Ordinal);

        // 🔥 The seat is handed down rather than fetched (BUILD-PLAN P13.6): a SeatBoard belongs
        // to the viewer who sat down, so the island that sat down owns it and this draws
        // whichever one it was given. It used to be injected, which is one seat for everybody.
        var seat = Sources.Read("Components/Table/YourSeat.razor");

        Assert.Contains("public SeatBoard? Seat { get; set; }", seat, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject", seat, StringComparison.Ordinal);

        // The hand is passed down, never fetched: neither leaf component injects anything.
        Assert.DoesNotContain("@inject", Sources.Read("Components/Table/HandPanel.razor"), StringComparison.Ordinal);
        Assert.DoesNotContain("@inject", Sources.Read("Components/Table/TurnPrompt.razor"), StringComparison.Ordinal);

        // The one thing the island is injected with is the lobby, which is how it finds the
        // table its URL names — and it is the only component in the client that knows there is
        // more than one table (BUILD-PLAN P13.6).
        var lobbied = Sources.Components
            .Where(file => file.Text.Contains("@inject Lobby", StringComparison.Ordinal))
            .Select(file => file.Path)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(2, lobbied.Count);
        Assert.EndsWith("Tables.razor", lobbied[0].Replace('\\', '/'), StringComparison.Ordinal);
        Assert.EndsWith("TableView.razor", lobbied[1].Replace('\\', '/'), StringComparison.Ordinal);
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
        Assert.EndsWith("Table.razor", interactive[0], StringComparison.Ordinal);
        Assert.Contains("<TableView @rendermode=\"InteractiveServer\"", Sources.Read("Components/Pages/Table.razor"));

        // ⚠️ And what crosses the boundary is serialisable (BUILD-PLAN P13.6): a parameter
        // handed to an interactive root component is serialised to the circuit, so the page
        // passes the table's *id* and the island looks the table up.
        Assert.DoesNotContain("HostedTable", Sources.Read("Components/Pages/Table.razor"), StringComparison.Ordinal);
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
    /// ✅ <b>P13.5 — the felt is a table, not a document.</b> No visible paragraph on the table
    /// is longer than <see cref="ProseBudget"/> characters unless it is inside a disclosure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>"Minimize prose" is a mood until something counts it.</b> The table used to carry
    /// fourteen copies of <em>"melds nothing"</em>, a paragraph explaining the money cards under
    /// the turned-up cards, another under the hand, a third under the settlement, a three
    /// sentence lede and a line about the seed. Every one of them was true. The rule this test
    /// states is not that they were wrong but that <b>the felt is not where they live</b>.
    /// </para>
    /// <para>
    /// ⚠️ <b>Prose leaves the screen; it does not leave the page.</b> So the budget is measured
    /// on <em>visible</em> text only, and there are exactly two exemptions, which are the two
    /// destinations: a <c>&lt;details&gt;</c> the player can open, and a
    /// <c>&lt;span class="said"&gt;</c> — the accessible name of the thing the glyph replaced.
    /// <b>Text moved into <c>.said</c> costs nothing here</b>, and that is deliberate: an icon
    /// with no accessible name is worse than the prose it replaced, and a budget that punished
    /// the fix would produce exactly that.
    /// </para>
    /// <para>
    /// The third destination — the round log — is a different component and is measured by
    /// nothing, because narration is what it is for.
    /// </para>
    /// <para>
    /// ⚠️ <b>It measures the longest run of literal text, not the length of the element.</b> A
    /// paragraph is full of Razor, and <c>@@if (asking.Taken is { } taken)</c> is not prose; a
    /// sentence is one unbroken run and an expression is not.
    /// </para>
    /// </remarks>
    [Fact]
    public void NoParagraphOnTheTableIsAWallOfText()
    {
        var measured = 0;
        var longest = 0;

        foreach (var (path, text) in Felt)
        {
            // The two places prose is allowed to live.
            var visible = Regex.Replace(text, "<span class=\"said\">.*?</span>", " ", RegexOptions.Singleline);
            var disclosures = Regex.Matches(visible, "<details.*?</details>", RegexOptions.Singleline);

            foreach (Match paragraph in Regex.Matches(visible, "<p[ >].*?</p>", RegexOptions.Singleline))
            {
                if (disclosures.Any(open => paragraph.Index > open.Index && paragraph.Index < open.Index + open.Length))
                {
                    continue;
                }

                measured++;

                var run = LongestRunOfProse(paragraph.Value);
                longest = Math.Max(longest, run.Length);

                Assert.True(
                    run.Length <= ProseBudget,
                    $"{path}: {run.Length} characters of prose on the felt — \"{run}\". "
                    + $"The budget is {ProseBudget}; put it in a <details>, in the round log, or "
                    + "in the accessible name of the thing it describes (BUILD-PLAN P13.5).");
            }
        }

        // Non-vacuous: the table really does have paragraphs, and one of them really is near the
        // budget, so this is a ceiling somebody is standing under rather than one nobody is near.
        Assert.True(measured >= 8, $"only {measured} paragraphs measured, which is fewer than the table has.");
        Assert.True(longest >= 30, $"the longest run measured was {longest}, so nothing here is being tested.");
    }

    /// <remarks>
    /// The other half of the same rule, stated positively: <b>the sentences that left the felt
    /// are still on the page.</b> A budget nothing balances is a licence to delete.
    /// </remarks>
    [Fact]
    public void TheProseThatLeftTheFeltIsStillOnThePage()
    {
        var legend = Sources.Read("Components/Table/TableLegend.razor");
        var about = Sources.Read("Components/Table/AboutTable.razor");

        // The legend explains every display state there is, and it iterates the enum rather than
        // listing them — so a state added later cannot ship with nothing beside it (§3.11 A2).
        Assert.Contains("DisplayTokens.States", legend, StringComparison.Ordinal);
        Assert.Contains("<details", legend, StringComparison.Ordinal);
        Assert.Contains("pays its owner", legend, StringComparison.Ordinal);

        // The seed is how a match gets reported (§3.9). It left the felt; it did not leave.
        Assert.Contains("<details", about, StringComparison.Ordinal);
        Assert.Contains("Seed", about, StringComparison.Ordinal);
        Assert.Contains("concealed", about, StringComparison.Ordinal);

        // And the per-card cost — the biggest block of text there was — is in the accessible
        // name of the button that throws the card.
        var hand = Sources.Read("Components/Table/HandPanel.razor");
        Assert.Contains("gives up", hand, StringComparison.Ordinal);
        Assert.DoesNotContain("melds nothing", hand, StringComparison.Ordinal);
    }

    /// <summary>
    /// 🔥 <b>P13.5's worst trap: a hidden live region announces nothing.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The round log became a panel you open in the packet that also made the felt carry more
    /// and the page carry less. Putting the list behind <c>display:none</c>, or inside a closed
    /// <c>&lt;details&gt;</c>, would take the entire narration away from the people relying on
    /// it most — in the packet that increased the reliance (WCAG 4.1.3).
    /// </para>
    /// <para>
    /// So the region is rendered whether the panel is open or shut, and closing it clips it the
    /// way <c>.said</c> is clipped: off screen, still laid out, still announcing. <b>The one
    /// thing that may depend on being open is whether the box is a tab stop</b> — a focus stop
    /// on a box nobody can see is a keyboard trap in everything but name.
    /// </para>
    /// </remarks>
    [Fact]
    public void ClosingTheLogDoesNotSilenceIt()
    {
        var markup = Sources.Read("Components/Table/RoundLogPanel.razor");
        var css = Sources.Read("Components/Table/RoundLogPanel.razor.css");

        var from = markup.IndexOf("id=\"log-lines\"", StringComparison.Ordinal);
        var to = markup.IndexOf("</ol>", StringComparison.Ordinal);

        Assert.True(from > 0 && to > from, "the log's live region is not where this test expects it.");

        var region = markup[from..to];

        Assert.Contains("aria-live=\"polite\"", region, StringComparison.Ordinal);

        // Nothing in here is conditional.
        Assert.DoesNotContain("@if", region, StringComparison.Ordinal);

        // ⚠️ And nothing in here is hidden. `hidden` takes the element out of the accessibility
        // tree exactly as display:none does; aria-hidden on a glyph inside a line is fine and is
        // why this looks for the attribute rather than for the word.
        Assert.DoesNotMatch(new Regex(@"(?<![a-zA-Z-])hidden\s*="), region);

        // The one thing that may depend on being open is whether the box is a tab stop —
        // checked per attribute, not per line, because a second attribute on the same line is
        // exactly how a mutation gets past this.
        foreach (Match attribute in Regex.Matches(region, @"([a-zA-Z][a-zA-Z0-9-]*)\s*=\s*""([^""]*)"""))
        {
            if (!attribute.Groups[2].Value.Contains("_open", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.Equal("tabindex", attribute.Groups[1].Value);
        }

        // …and nowhere else in the region at all, attribute or not.
        Assert.Equal(
            Regex.Matches(region, "_open").Count,
            Regex.Matches(region, @"tabindex\s*=\s*""[^""]*_open[^""]*""").Count);

        // And the closed state clips rather than hides. Either of these two declarations takes
        // the element out of the accessibility tree, and with it every line of the game.
        Assert.DoesNotContain("display: none", css, StringComparison.Ordinal);
        Assert.DoesNotContain("visibility: hidden", css, StringComparison.Ordinal);
        Assert.Contains("clip-path", css, StringComparison.Ordinal);
    }

    /// <summary>
    /// 🔥 <b>P13.5 — moving focus must never be able to kill the circuit.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// §3.11 B7 asks for focus to move when the turn does, and the client does that through
    /// <c>ElementReference.FocusAsync</c>. ⚠️ <b>An <c>ElementReference</c> keeps its id after
    /// the element it names has gone</b>, so the call can land on nothing — and Blazor turns an
    /// unhandled interop exception into a <em>torn-down circuit</em>. The page then looks
    /// completely correct and does nothing at all, which is the single hardest failure in this
    /// client to recognise (P13.3 found the same shape in a 404).
    /// </para>
    /// <para>
    /// It is a race a person can win: answer a question quickly enough and the control is
    /// replaced between the render that decided to focus it and the call reaching the browser.
    /// <b>Found by playing 1,429 turns and reading the server log</b> — the browser showed a
    /// hand, a prompt and a spotlight, and every card refused every press in silence.
    /// </para>
    /// </remarks>
    [Fact]
    public void NoFocusCallCanTakeTheCircuitDown()
    {
        var guarded = 0;

        // Only the components that actually hold an element reference can make this mistake —
        // everything else is calling a component method, and the guard belongs where the
        // reference lives rather than at every call site of every wrapper.
        foreach (var (path, text) in Sources.Components.Where(file =>
            file.Text.Contains("ElementReference", StringComparison.Ordinal)))
        {
            foreach (Match focus in Regex.Matches(text, @"_\w+\.FocusAsync\(\)"))
            {
                guarded++;

                // The whole method it sits in, near enough: from the previous blank line-ish
                // boundary to the end of the block it is in.
                var from = Math.Max(0, focus.Index - 600);
                var around = text[from..Math.Min(text.Length, focus.Index + 400)];

                Assert.True(
                    around.Contains("catch (JSException)", StringComparison.Ordinal),
                    $"{path}: FocusAsync is called without catching JSException. An element "
                    + "reference outlives its element, and an unhandled interop failure tears "
                    + "the circuit down (BUILD-PLAN P13.5).");

                Assert.True(
                    around.Contains("catch (JSDisconnectedException)", StringComparison.Ordinal),
                    $"{path}: FocusAsync is called without catching JSDisconnectedException.");
            }
        }

        // Non-vacuous: the seat really does move focus, in both of the places §3.11 B7 names.
        Assert.True(guarded >= 2, $"only {guarded} focus calls scanned, which is fewer than the seat has.");
    }

    /// <summary>How many characters of visible prose one paragraph on the felt may carry.</summary>
    private const int ProseBudget = 80;

    /// <summary>The table itself, and the page that is nothing but the table.</summary>
    private static IEnumerable<(string Path, string Text)> Felt => Sources.Components
        .Where(file => file.Path.Contains("Components/Table/", StringComparison.Ordinal)
            || file.Path.Contains("Components\\Table\\", StringComparison.Ordinal)
            || file.Path.EndsWith("Pages/Table.razor", StringComparison.Ordinal)
            || file.Path.EndsWith("Pages\\Table.razor", StringComparison.Ordinal));

    /// <summary>
    /// The longest unbroken run of literal text in a fragment of markup — tags, Razor
    /// expressions and code fragments all break a run, because none of them is prose.
    /// </summary>
    private static string LongestRunOfProse(string markup) =>
        Regex.Replace(markup, "<[^>]*>", "\u0000")
            .Split(['\u0000', '@', '{', '}'])
            .Select(run => Regex.Replace(run, @"\s+", " ").Trim())
            .OrderByDescending(run => run.Length)
            .First();

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
