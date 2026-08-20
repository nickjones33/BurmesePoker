using System.Reflection;
using System.Text.RegularExpressions;

using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Presentation;
using BurmesePoker.Server;
using BurmesePoker.Sim;
using BurmesePoker.Tests.Web;
using BurmesePoker.Web;

namespace BurmesePoker.Tests;

/// <summary>
/// The one mechanical thing P11 can check about a console packet.
/// </summary>
/// <remarks>
/// <para>
/// A console UX pass is verified by playing it, and nothing in <c>BurmesePoker.Console</c> is
/// reachable from here by construction — the test project references Domain, Presentation,
/// Server and Sim, and never the front end (BUILD-PLAN §2). What <em>is</em> checkable is the direction of
/// the dependency: presentation may reach into the domain, and the domain may not reach back.
/// </para>
/// <para>
/// ⚠️ <b>P13.1 added the row that matters most for what comes next.</b>
/// <c>BurmesePoker.Presentation</c> is the project both front ends share, and it earns that
/// only by knowing about no rendering technology at all — not Spectre, not ASP.NET, not
/// <c>System.Console</c>. A reference to any of them would make it a third front end wearing a
/// shared name, and the drift the extraction exists to prevent would be back.
/// </para>
/// <para>
/// <b>This is not decoration.</b> P11 put a deliberate pause between computer turns, and the
/// obvious place to put it was <c>GreedyBotAgent</c> — where it would have sat inside the
/// simulation harness's hot loop and quietly ruined P12. It went into a decorator in the
/// console instead. A layering test is what makes that kind of mistake fail loudly rather
/// than slowly.
/// </para>
/// </remarks>
public class LayeringTests
{
    [Fact]
    public void TheDomainDoesNotReferenceSpectre()
    {
        Assert.DoesNotContain(
            typeof(Card).Assembly.GetReferencedAssemblies(),
            reference => reference.Name?.StartsWith("Spectre", StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <remarks>
    /// The domain does no I/O at all (BUILD-PLAN §2), which is a stronger claim than "does not
    /// print with Spectre" and the reason a simulation can run two thousand of these in
    /// parallel without a console anywhere.
    /// </remarks>
    [Fact]
    public void TheDomainDoesNotWriteToAConsoleEither()
    {
        Assert.DoesNotContain(
            typeof(Card).Assembly.GetReferencedAssemblies(),
            reference => reference.Name is "System.Console");
    }

    /// <remarks>
    /// The harness prints — it is a command-line tool — but it prints with the base library.
    /// Spectre belongs to the one project that is a user interface.
    /// </remarks>
    [Fact]
    public void TheSimulationHarnessDoesNotReferenceSpectre()
    {
        Assert.DoesNotContain(
            typeof(Simulator).Assembly.GetReferencedAssemblies(),
            reference => reference.Name?.StartsWith("Spectre", StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <remarks>
    /// <b>The one new rule of P13.1</b> (BUILD-PLAN §2): a view model that referenced a
    /// renderer would stop being one. Spectre is the console's, ASP.NET is the browser's, and
    /// neither belongs to the project that answers what a hand looks like.
    /// </remarks>
    [Fact]
    public void ThePresentationLayerReferencesNoRenderingTechnology()
    {
        var referenced = typeof(HandView).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToList();

        Assert.NotEmpty(referenced);

        foreach (var forbidden in new[] { "Spectre", "Microsoft.AspNetCore", "System.Console" })
        {
            Assert.DoesNotContain(
                referenced,
                name => name.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <remarks>
    /// <para>
    /// <b>The one new rule of P13.2</b> (BUILD-PLAN §2, §3.10). The table server is a seat and
    /// a fan-out: Blazor Server supplies the transport, so there is no protocol in there and
    /// nothing in there that knows a socket exists. A reference to ASP.NET would mean the
    /// server had grown a wire of its own — at which point a test could no longer hold the very
    /// connection a browser holds, which is the whole of why P13.2 has a mechanical acceptance
    /// criterion.
    /// </para>
    /// <para>
    /// Spectre and <c>System.Console</c> are forbidden for the older reason: a table nobody is
    /// drawing must not be able to print.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheTableServerReferencesNoTransportAndNoRenderingTechnology()
    {
        var referenced = typeof(TableSession).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToList();

        Assert.NotEmpty(referenced);

        foreach (var forbidden in new[] { "Spectre", "Microsoft.AspNetCore", "System.Console", "System.Net" })
        {
            Assert.DoesNotContain(
                referenced,
                name => name.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <remarks>
    /// <para>
    /// <b>The one new rule of P13.3</b> (BUILD-PLAN §2, P13.3). The browser client is the
    /// second project that may draw, and the one thing it may not do is draw with the first
    /// one's paint: <c>BurmesePoker.Console</c> is Spectre markup, none of which survives a
    /// wire, and the whole of P13.1 was extracting the half that does into
    /// <c>BurmesePoker.Presentation</c>.
    /// </para>
    /// <para>
    /// ⚠️ <b>This is the row that closes P13.2's leftover.</b> The pacing decorator a bot seat
    /// needs lived in the console, and the cheap way to reach it from here would have been a
    /// project reference. It moved instead — and this is what says so out loud.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheBrowserClientDoesNotReachForTheConsole()
    {
        var referenced = typeof(HostedTable).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToList();

        Assert.NotEmpty(referenced);
        Assert.DoesNotContain(referenced, name => name is "BurmesePoker.Console");
        Assert.DoesNotContain(
            referenced,
            name => name.StartsWith("Spectre", StringComparison.OrdinalIgnoreCase));

        // And it does reach for the three it is built on, so the list above is not empty by
        // accident.
        Assert.Contains(referenced, name => name is "BurmesePoker.Domain");
        Assert.Contains(referenced, name => name is "BurmesePoker.Presentation");
        Assert.Contains(referenced, name => name is "BurmesePoker.Server");
    }

    /// <remarks>
    /// The direction, stated the other way round: the domain does not know the presentation
    /// layer exists. Rules first, a view of them second — the same shape as
    /// <see cref="TheDomainDoesNotReferenceSpectre"/> one layer out.
    /// </remarks>
    [Fact]
    public void TheDomainDoesNotReferenceThePresentationLayer()
    {
        Assert.DoesNotContain(
            typeof(Card).Assembly.GetReferencedAssemblies(),
            reference => reference.Name is "BurmesePoker.Presentation");

        Assert.DoesNotContain(
            typeof(Card).Assembly.GetReferencedAssemblies(),
            reference => reference.Name is "BurmesePoker.Server");
    }

    /// <summary>
    /// ✅ <b>P18 acceptance 4 — a bot is constructed in one place.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A source scan, in the same spirit as the reference checks above</b>, and for the same
    /// reason: the rule is about where a decision is <em>written down</em>, which no compiled
    /// assembly records. Before this packet the console, the browser client and the table
    /// server each named a bot for themselves — four independent notions of which one — and the
    /// browser had no difficulty setting at all as a direct result.
    /// </para>
    /// <para>
    /// ⚠️ <b>It bans constructing a <em>rung</em>, not an agent.</b> The types are asked of
    /// <see cref="BotCatalog"/> rather than listed here, so a fifth rung is covered the day it
    /// is added. Decorators and stand-ins — the pacing wrapper, the journalling wrapper, the
    /// replay seat, the remote seat — are not ways of playing and are not in the catalog, so
    /// they are untouched by this.
    /// </para>
    /// <para>
    /// The test project is not scanned, deliberately: a test that wants a particular agent in a
    /// particular seat is entitled to say so, and several do.
    /// </para>
    /// </remarks>
    [Fact]
    public void NothingOutsideTheCatalogBuildsABot()
    {
        var rungs = BotCatalog.All.Select(rung => rung.Create(0).GetType().Name).Distinct().ToList();

        Assert.Equal(BotCatalog.All.Count, rungs.Count);

        var catalog = Path.Combine("BurmesePoker.Domain", "Agents") + Path.DirectorySeparatorChar;
        var scanned = 0;
        var found = 0;

        foreach (var (path, text) in Sources.Production)
        {
            scanned++;

            foreach (var rung in rungs)
            {
                foreach (Match built in Regex.Matches(text, $@"\bnew\s+{rung}\s*\("))
                {
                    found++;

                    Assert.True(
                        path.StartsWith(catalog, StringComparison.Ordinal),
                        $"{path}: builds a {rung} directly. A bot is named once, in BotCatalog, and "
                        + "resolved by name everywhere else (BUILD-PLAN P18) — otherwise a new rung "
                        + $"reaches this file only when somebody remembers it exists. Found: {built.Value}");
                }
            }
        }

        // A guard on the guard: a scan that matched nothing would pass whatever the tree said.
        Assert.True(scanned > 40, $"only {scanned} files scanned, which is less than this solution has.");
        Assert.Equal(BotCatalog.All.Count, found);
    }

    /// <remarks>
    /// A guard on the guards above: <see cref="Assembly.GetReferencedAssemblies"/> lists only
    /// what the compiler kept, so a test that passed because the list was empty would be
    /// worthless. It is not empty.
    /// </remarks>
    [Fact]
    public void TheReferenceListsAreNotEmpty()
    {
        Assert.NotEmpty(typeof(Card).Assembly.GetReferencedAssemblies());
        Assert.NotEmpty(typeof(HandView).Assembly.GetReferencedAssemblies());
        Assert.NotEmpty(typeof(Simulator).Assembly.GetReferencedAssemblies());
        Assert.NotEmpty(typeof(TableSession).Assembly.GetReferencedAssemblies());
        Assert.NotEmpty(typeof(HostedTable).Assembly.GetReferencedAssemblies());
    }
}
