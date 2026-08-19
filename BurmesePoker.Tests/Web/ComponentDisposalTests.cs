using System.Reflection;

using BurmesePoker.Server;
using BurmesePoker.Web;
using BurmesePoker.Web.Components.Table;

using Microsoft.AspNetCore.Components;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>§3.11 A5 — anything holding a subscription disposes it.</b>
/// </summary>
/// <remarks>
/// <para>
/// A table component subscribes to the observer stream, and a circuit that drops without
/// unhooking leaks a game: the dead component stays subscribed to a live table and is
/// re-rendered for ever. <b>The test reflects over the client's components</b> and asserts that
/// every one taking the table implements <see cref="IDisposable"/> or
/// <see cref="IAsyncDisposable"/>.
/// </para>
/// <para>
/// ⚠️ <b>Injected properties are private</b>, which is what <c>@inject</c> generates, so the
/// scan looks at non-public members too. A component that took the host through a
/// <c>[Parameter]</c> instead would be caught by the same sweep.
/// </para>
/// </remarks>
public class ComponentDisposalTests
{
    /// <summary>The types a component cannot hold without also being able to let go of it.</summary>
    private static readonly Type[] Subscribable =
        [typeof(HostedTable), typeof(SeatBoard), typeof(SeatConnection), typeof(TableSession)];

    private static IEnumerable<Type> Components => typeof(TableView).Assembly.GetTypes()
        .Where(type => type is { IsAbstract: false, IsClass: true })
        .Where(typeof(ComponentBase).IsAssignableFrom);

    [Fact]
    public void ThereAreComponentsToReflectOver()
    {
        Assert.NotEmpty(Components);
        Assert.Contains(typeof(TableView), Components);
    }

    [Fact]
    public void EveryComponentThatTakesTheTableCanLetGoOfIt()
    {
        var holders = Components.Where(Holds).ToList();

        Assert.NotEmpty(holders);

        foreach (var component in holders)
        {
            Assert.True(
                typeof(IDisposable).IsAssignableFrom(component)
                || typeof(IAsyncDisposable).IsAssignableFrom(component),
                $"{component.Name} takes the table and cannot let go of it (§3.11 A5).");
        }
    }

    /// <remarks>
    /// The other half of the same rule, and the reason the reflection is worth anything: the
    /// component that subscribes must actually unsubscribe. A <c>Dispose</c> that does nothing
    /// passes the test above and leaks exactly as much as no <c>Dispose</c> at all.
    /// </remarks>
    [Fact]
    public void TheTableUnhooksItselfFromTheStream()
    {
        var source = Sources.Read("Components/Table/TableView.razor");

        Assert.Contains("_table.Changed +=", source, StringComparison.Ordinal);
        Assert.Contains("_table.Changed -=", source, StringComparison.Ordinal);

        // 🔥 And the seat, which is the one that costs somebody a game (BUILD-PLAN P13.6): a
        // circuit that dropped while still holding a seat would leave the table waiting for
        // somebody who has gone, and a table does not deal while a seat is unaccounted for.
        Assert.Contains("_table.Arrive();", source, StringComparison.Ordinal);
        Assert.Contains("_table.Leave();", source, StringComparison.Ordinal);
        Assert.Contains("_table.SitDown(", source, StringComparison.Ordinal);
        Assert.Contains("_table.StandUp(_seat);", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// ✅ <b>§3.11 C13, and P13.6 gave it teeth.</b> Prerendering runs a component's
    /// initialisation on the request and again when the circuit starts.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Both of the things this component now does to a table are claims on it</b> — being
    /// counted as present, and taking a seat — and a claim made twice is a bug rather than a
    /// wasted call. So neither happens until the renderer says it is really interactive. A
    /// board can be drawn either time, and is.
    /// </remarks>
    [Fact]
    public void NothingIsClaimedWhileThePageIsOnlyBeingPrerendered()
    {
        var source = Sources.Read("Components/Table/TableView.razor");

        var guard = source.IndexOf("RendererInfo.IsInteractive", StringComparison.Ordinal);

        Assert.True(guard > 0, "TableView claims a seat without asking whether it is interactive (§3.11 C13).");

        foreach (var claim in new[] { "_table.Arrive();", "_table.SitDown(" })
        {
            Assert.True(
                source.IndexOf(claim, StringComparison.Ordinal) > guard,
                $"TableView runs `{claim}` before it knows it is interactive (§3.11 C13).");
        }
    }

    /// <remarks>
    /// The seat is the second subscription in the client (P13.4), and it is the one that would
    /// hurt: a dead circuit still holding a live seat is a player who cannot be replaced by the
    /// stand-in because somebody is still listening for their turn.
    /// </remarks>
    [Fact]
    public void TheSeatUnhooksItselfFromItsOwnConnection()
    {
        var source = Sources.Read("Components/Table/YourSeat.razor");

        Assert.Contains("Changed +=", source, StringComparison.Ordinal);
        Assert.Contains("Changed -=", source, StringComparison.Ordinal);
    }

    private static bool Holds(Type component) =>
        component.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.PropertyType)
            .Concat(component
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => field.FieldType))
            .Any(Subscribable.Contains);
}
