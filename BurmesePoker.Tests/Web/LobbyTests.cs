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
    /// ✅ <b>P18 — <c>--difficulty</c>, which the browser did not have at all; P19 — it names a
    /// level of the dial rather than a rung of the ladder.</b>
    /// </summary>
    /// <remarks>
    /// The site's own command line names how hard the house table is, exactly as
    /// <c>--people</c> names how many seats are waiting. A name nobody knows opens the table
    /// anyway, on the default level: a site that refused to boot over a typo in a difficulty
    /// would be a poor trade. ⚠️ <b>A rung's name is one the dial does not know</b> (§3.12).
    /// </remarks>
    [Fact]
    public async Task TheCommandLineNamesHowHardTheComputerIs()
    {
        await using var gentle = Open(("difficulty", "easy"));
        await using var strange = Open(("difficulty", "thoughtful"));
        await using var rung = Open(("difficulty", BotCatalog.Hardest.Name));
        await using var silent = Open();

        Assert.Equal("easy", gentle.Opening.Difficulty);
        Assert.Equal("easy", gentle.OpenTheHouseTable().Difficulty.Name);
        Assert.Equal(DifficultyLadder.Default.Name, strange.Opening.Difficulty);
        Assert.Equal(DifficultyLadder.Default.Name, rung.Opening.Difficulty);
        Assert.Equal(DifficultyLadder.Default.Name, silent.Opening.Difficulty);
    }

    /// <summary>
    /// ✅ <b>P19 — <c>--mixed</c> spreads the dial across the computer's seats.</b>
    /// </summary>
    /// <remarks>
    /// The spread is longer than any table, and <c>HostedTable</c> takes as many of it as it
    /// has computer seats — so one flag is right whatever <c>--seats</c> and <c>--people</c>
    /// turn out to be.
    /// </remarks>
    [Fact]
    public async Task TheCommandLineCanAskForAMixedTable()
    {
        await using var mixed = Open(("mixed", "true"), ("people", "0"));
        await using var plain = Open(("people", "0"));

        Assert.True(mixed.OpenTheHouseTable().IsMixed);
        Assert.False(plain.OpenTheHouseTable().IsMixed);
        Assert.Null(plain.Opening.Difficulties);
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
