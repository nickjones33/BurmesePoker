namespace BurmesePoker.Tests.Web;

/// <summary>
/// ✅ <b>P42 build item 2: the ×5 jackpot is said out loud at both front ends.</b>
/// </summary>
/// <remarks>
/// <para>
/// The jackpot settled correctly from P26 on and <b>no screen ever explained it</b> — the
/// gap this fences shut. The domain carries the fact once
/// (<c>RoundResult.JackpotOwner</c>, asserted over a constructed round in
/// <c>RoundEngineTests</c>); what is asserted here is that each front end actually
/// <em>reads</em> it, at the highest layer the harness reaches for each. The test project
/// never references the console (its project is not in the graph), and the browser's razor
/// is markup rather than a rendered component — so both sentences are held by a source scan,
/// in the same spirit as <c>MarkupStandardsTests</c> and <c>LayeringTests</c>.
/// </para>
/// <para>
/// ⚠️ <b>Deliberately not a wording fence.</b> What must not regress is the read: a
/// settlement view that stops consulting <c>JackpotOwner</c> is a screen that has gone quiet
/// about the largest single swing in the game, whatever its other sentences say.
/// </para>
/// </remarks>
public class JackpotSpokenTests
{
    [Fact]
    public void TheBrowserSettlementSaysTheJackpotFromTheResultAlone()
    {
        var panel = Sources.Read("Components/Table/SettlementPanel.razor");

        // The fact comes off the result — a watcher cannot compute it (ownership is partly
        // private until settlement), so folding it client-side is not an option to drift back
        // to (BUILD-PLAN P42 build item 2).
        Assert.Contains("Result.JackpotOwner", panel, StringComparison.Ordinal);
        Assert.Contains("MoneyCardRegistry.Jackpot", panel, StringComparison.Ordinal);
    }

    [Fact]
    public void TheBrowserTableCentreNotesTheJackpotPairWhileItIsUp()
    {
        // The possibility is public from the deal: the board folds it at RoundStarted rather
        // than reading the live turn-up list, which loses a card if the top one is claimed
        // while the designation it made stands.
        Assert.Contains(
            "Board.JackpotPairUp",
            Sources.Read("Components/Table/TableCentre.razor"),
            StringComparison.Ordinal);
        Assert.Contains(
            "JackpotPairUp = MoneyCardRegistry.IsTheJackpotPair",
            Production("BurmesePoker.Web/TableBoard.cs"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void TheConsoleSettlementSaysTheJackpotAndTheDealSaysThePairIsUp()
    {
        // The console is outside the test project's reference graph on purpose (it is the
        // only project that prints), so its two sentences are held where LayeringTests holds
        // its constraints: at the source.
        Assert.Contains(
            "result.JackpotOwner",
            Production("BurmesePoker.Console/Program.cs"),
            StringComparison.Ordinal);
        Assert.Contains(
            "MoneyCardRegistry.IsTheJackpotPair",
            Production("BurmesePoker.Console/ConsoleObserver.cs"),
            StringComparison.Ordinal);
    }

    /// <summary>One production file's text, by its repository-relative path.</summary>
    private static string Production(string path) =>
        Sources.Production.Single(file => file.Path.Replace('\\', '/') == path).Text;
}
