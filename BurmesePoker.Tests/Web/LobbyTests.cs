using BurmesePoker.Domain.Agents;
using BurmesePoker.Web;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// The lobby (packet P13.6): what is open, how a URL names one of them, and what a site opened
/// from the command line looks like.
/// </summary>
/// <remarks>
/// ⚠️ <b>It replaced a singleton with one table in it.</b> P13.3 opened one table from
/// configuration, which is the whole of what a page that watched needed; a second table is a
/// dictionary of them and a route that names one. Nothing below the lobby knows it exists.
/// </remarks>
public class LobbyTests
{
    [Fact]
    public async Task TheSiteOpensOneTableFromItsOwnCommandLine()
    {
        await using var lobby = Open(("seed", "20260819"), ("seats", "5"), ("people", "2"), ("table", "The kitchen"));

        Assert.Empty(lobby.Tables);

        var table = lobby.OpenTheHouseTable();

        Assert.Equal("The kitchen", table.Title);
        Assert.Equal(5, table.Seats);
        Assert.Equal(2, table.Plan.People);
        Assert.Equal(20260819, table.Seed);
        Assert.Same(table, Assert.Single(lobby.Tables));

        // Idempotent: every request that reached the site would otherwise open another one.
        Assert.Same(table, lobby.OpenTheHouseTable());
    }

    /// <summary>
    /// ✅ <b>P18 — <c>--difficulty</c>, which the browser did not have at all.</b>
    /// </summary>
    /// <remarks>
    /// The site's own command line names the rung the house table plays on, exactly as
    /// <c>--people</c> names how many seats are waiting. A name nobody knows opens the table
    /// anyway, on the hardest rung: a site that refused to boot over a typo in a difficulty
    /// would be a poor trade.
    /// </remarks>
    [Fact]
    public async Task TheCommandLineNamesHowWellTheComputerPlays()
    {
        await using var easy = Open(("difficulty", "simple"));
        await using var strange = Open(("difficulty", "thoughtful"));
        await using var silent = Open();

        Assert.Equal("simple", easy.Opening.Difficulty);
        Assert.Equal("simple", easy.OpenTheHouseTable().Difficulty.Name);
        Assert.Equal(BotCatalog.Hardest.Name, strange.Opening.Difficulty);
        Assert.Equal(BotCatalog.Hardest.Name, silent.Opening.Difficulty);
    }

    [Fact]
    public async Task ATableIsFoundByTheIdItsUrlCarries()
    {
        await using var lobby = Open();

        var first = lobby.OpenTheHouseTable();
        var second = lobby.Open(lobby.Opening with { Title = "Another", Seed = lobby.NextSeed() });

        Assert.NotEqual(first.Id, second.Id);
        Assert.Same(first, lobby.Find(first.Id));
        Assert.Same(second, lobby.Find(second.Id));
        Assert.Null(lobby.Find("no-such-table"));
        Assert.Null(lobby.Find(null));
        Assert.Equal(2, lobby.Tables.Count);
    }

    /// <remarks>
    /// A table is a parked thread and a paced bot loop (§3.6), and the page that opens one is a
    /// form anybody can press.
    /// </remarks>
    [Fact]
    public async Task TheSiteHoldsOnlySoManyTables()
    {
        await using var lobby = Open();

        for (var opened = 0; opened < Lobby.MostTables; opened++)
        {
            lobby.Open(lobby.Opening with { Seed = lobby.NextSeed() });
        }

        Assert.Throws<InvalidOperationException>(() => lobby.Open(lobby.Opening));

        // …and closing one makes room again.
        Assert.True(await lobby.Close(lobby.Tables[0].Id));
        Assert.False(await lobby.Close("no-such-table"));

        lobby.Open(lobby.Opening with { Seed = lobby.NextSeed() });
        Assert.Equal(Lobby.MostTables, lobby.Tables.Count);
    }

    /// <remarks>
    /// <b>A seed is a pointer</b> (§3.9): the whole site is reproducible from one number, so a
    /// second table's seed comes out of the first one's sequence rather than out of the clock.
    /// </remarks>
    [Fact]
    public async Task ASeededSiteDealsTheSameTablesEveryTime()
    {
        await using var one = Open(("seed", "20260819"));
        await using var two = Open(("seed", "20260819"));

        Assert.Equal(one.Opening.Seed, two.Opening.Seed);
        Assert.Equal(
            new[] { one.NextSeed(), one.NextSeed(), one.NextSeed() },
            new[] { two.NextSeed(), two.NextSeed(), two.NextSeed() });
    }

    [Fact]
    public async Task ATableOfMorePeopleThanSeatsIsRefused()
    {
        await using var lobby = Open();

        Assert.Throws<ArgumentOutOfRangeException>(() => lobby.Open(lobby.Opening with { Seats = 4, People = 5 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => lobby.Open(lobby.Opening with { Seats = 4, People = -1 }));
    }

    internal static Lobby Open(params (string Key, string Value)[] settings) => new(
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(
                setting => setting.Key,
                setting => (string?)setting.Value))
            .Build(),
        NullLoggerFactory.Instance);
}
