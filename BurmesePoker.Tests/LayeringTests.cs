using System.Reflection;

using BurmesePoker.Domain.Cards;
using BurmesePoker.Sim;

namespace BurmesePoker.Tests;

/// <summary>
/// The one mechanical thing P11 can check about a console packet.
/// </summary>
/// <remarks>
/// <para>
/// A console UX pass is verified by playing it, and nothing in <c>BurmesePoker.Console</c> is
/// reachable from here by construction — the test project references Domain and Sim and never
/// the front end (BUILD-PLAN §2). What <em>is</em> checkable is the direction of the
/// dependency: presentation may reach into the domain, and the domain may not reach back.
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
    /// A guard on the guards above: <see cref="Assembly.GetReferencedAssemblies"/> lists only
    /// what the compiler kept, so a test that passed because the list was empty would be
    /// worthless. It is not empty.
    /// </remarks>
    [Fact]
    public void TheReferenceListsAreNotEmpty()
    {
        Assert.NotEmpty(typeof(Card).Assembly.GetReferencedAssemblies());
        Assert.NotEmpty(typeof(Simulator).Assembly.GetReferencedAssemblies());
    }
}
