using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
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
    /// ✅ <b>P36 — the lobby form offers the setting, out of the domain's own list</b>.
    /// </summary>
    /// <remarks>
    /// <b>A source scan, in the P18/P19 idiom</b>: the difficulty menu is a <c>foreach</c> over
    /// <c>DifficultyLadder</c> rather than a hand-typed list, and this is the same. A form that
    /// spelled the policies itself would be a second place a policy is named, and the third one
    /// added would reach the browser only when somebody remembered this file.
    /// </remarks>
    [Fact]
    public void TheLobbyFormOffersTheSeatingOutOfTheDomainsOwnList()
    {
        var form = Sources.Read("Components/Pages/Tables.razor");

        Assert.Contains("SeatingPolicy.Offered", form, StringComparison.Ordinal);
        Assert.Contains("@bind-Value=\"Wanted!.Seating\"", form, StringComparison.Ordinal);
        Assert.Contains("<label for=\"new-seating\"", form, StringComparison.Ordinal);
        // ⚠️ The resolution moved out of the markup in P56, along with the rest of the form's
        // decisions — a form whose clamping and fill live in a .razor file is the one part of
        // the lobby nothing can assert. It still has to happen, and it still has to be the
        // domain's own.
        Assert.Contains(
            "SeatingPolicy.Resolve(Seating)",
            Sources.Read("NewTable.cs"),
            StringComparison.Ordinal);

        // Not a hand-typed menu: no policy is spelled out in the markup.
        foreach (var policy in SeatingPolicy.Offered)
        {
            Assert.DoesNotContain($"value=\"{policy.Name}\"", form, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// ✅ <b>P36 — <c>--seating</c> names how long the seats hold, and the default is the rule</b>
    /// (RULES.md §3 step 2, rev 28).
    /// </summary>
    /// <remarks>
    /// The difficulty's rule, applied to the setting beside it (P18): a name off a command line is
    /// resolved through the domain and never trusted, so <c>--seating rubbish</c> opens the house
    /// table on <c>held</c> rather than failing to boot. ⚠️ <b>A number chosen here is a house
    /// arrangement and not the players agreeing</b> (§9 #45) — that is packet P37.
    /// </remarks>
    [Fact]
    public async Task TheCommandLineNamesHowLongTheSeatsHold()
    {
        await using var shuffled = Open(("seating", "every-round"));
        await using var occasional = Open(("seating", "every-5-rounds"));
        await using var strange = Open(("seating", "musical-chairs"));
        await using var silent = Open();

        Assert.Equal("every-round", shuffled.Opening.Seating);
        Assert.Equal("every-5-rounds", occasional.Opening.Seating);
        Assert.Equal(SeatingPolicy.Default.Name, strange.Opening.Seating);
        Assert.Equal(SeatingPolicy.Default.Name, silent.Opening.Seating);

        // …and the name is resolved once, where the table is built.
        Assert.Equal(SeatingPolicy.EveryRound, shuffled.OpenTheHouseTable().Seating);
        Assert.Equal(SeatingPolicy.Held, silent.OpenTheHouseTable().Seating);
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

    /// <summary>
    /// ✅ <b>P54 — a site left up for weeks behaves, because something closes the tables.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Nothing in this client closed a table before this packet</b>: <c>Close</c> existed
    /// and only these tests called it, so a hosted site accumulated a table per form press,
    /// reached <see cref="Lobby.MostTables"/>, and from then on answered every <em>Open it</em>
    /// with an error — weeks after the deploy, and reading as a broken form rather than a full
    /// site (<c>HOSTING.md</c> §8).
    /// ⚠️ <b>A table is idle from the moment it is opened</b>, because a table opened by a press
    /// nobody followed up has never had a viewer to lose and is exactly the leak.
    /// </remarks>
    [Fact]
    public async Task ATableNobodyHasBeenAtIsClosed()
    {
        var clock = new StoppedClock(new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero));
        await using var lobby = Open(clock);

        var house = lobby.OpenTheHouseTable();
        var opened = lobby.Open(lobby.Opening with { Title = "Somebody's", Seed = lobby.NextSeed() });

        Assert.Same(house, lobby.House);
        Assert.Equal(clock.GetUtcNow(), opened.IdleSince);

        // Not yet: a table is kept for as long as the site says it is.
        clock.Now += Lobby.IdleTablesAreClosedAfter - TimeSpan.FromSeconds(1);
        Assert.Empty(await lobby.ReapIdleTables());
        Assert.Equal(2, lobby.Tables.Count);

        clock.Now += TimeSpan.FromSeconds(2);

        // ⚠️ The house table is spared, whatever its clock says: `dotnet run` is meant to be a
        // game rather than an empty room, and the deployed site's own URL is that table.
        Assert.Equal([opened.Id], await lobby.ReapIdleTables());
        Assert.Same(house, Assert.Single(lobby.Tables));
    }

    /// <remarks>
    /// ⚠️ <b>The clock starts on the last viewer leaving and is cleared by the next arrival</b> —
    /// a table two people are watching is not idle because one of them left.
    /// </remarks>
    [Fact]
    public async Task ATableSomebodyIsAtIsNotIdleAtAll()
    {
        var clock = new StoppedClock(new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero));
        await using var lobby = Open(clock);

        // ⚠️ A seat still waiting for a person, deliberately: a table whose seats are all the
        // computer's would start dealing the moment somebody arrived, and this test is about
        // the clock rather than about a round.
        var table = lobby.Open(lobby.Opening with { People = 1, Seed = lobby.NextSeed() });

        table.Arrive();
        table.Arrive();
        Assert.Null(table.IdleSince);

        clock.Now += TimeSpan.FromHours(1);
        Assert.Empty(await lobby.ReapIdleTables());

        table.Leave();
        Assert.Null(table.IdleSince);

        table.Leave();
        Assert.Equal(clock.GetUtcNow(), table.IdleSince);

        // …and coming back stops the clock again, rather than merely resetting it.
        table.Arrive();
        Assert.Null(table.IdleSince);
        Assert.Empty(await lobby.ReapIdleTables());

        table.Leave();
        clock.Now += Lobby.IdleTablesAreClosedAfter + TimeSpan.FromSeconds(1);
        Assert.Equal([table.Id], await lobby.ReapIdleTables());
        Assert.Empty(lobby.Tables);
    }

    /// <remarks>
    /// <b>The point of reaping, stated as the thing it buys</b>: the site can go on opening
    /// tables for ever, rather than for twelve.
    /// </remarks>
    [Fact]
    public async Task ClosingTheIdleOnesMakesRoomForNewTables()
    {
        var clock = new StoppedClock(new DateTimeOffset(2026, 8, 29, 12, 0, 0, TimeSpan.Zero));
        await using var lobby = Open(clock);

        lobby.OpenTheHouseTable();

        for (var opened = lobby.Tables.Count; opened < Lobby.MostTables; opened++)
        {
            lobby.Open(lobby.Opening with { Seed = lobby.NextSeed() });
        }

        Assert.Throws<InvalidOperationException>(() => lobby.Open(lobby.Opening));

        clock.Now += Lobby.IdleTablesAreClosedAfter + TimeSpan.FromSeconds(1);

        Assert.Equal(Lobby.MostTables - 1, (await lobby.ReapIdleTables()).Count);
        Assert.Same(lobby.House, Assert.Single(lobby.Tables));

        lobby.Open(lobby.Opening with { Seed = lobby.NextSeed() });
        Assert.Equal(2, lobby.Tables.Count);
    }

    /// <summary>A clock that only moves when a test moves it.</summary>
    /// <remarks>
    /// ⚠️ <b>Written here rather than taken from a package</b>: what these tests need of a
    /// <c>TimeProvider</c> is <c>GetUtcNow</c>, and the alternative to four lines is a test
    /// that sleeps for the interval it is asserting about.
    /// </remarks>
    private sealed class StoppedClock(DateTimeOffset now) : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    internal static Lobby Open(TimeProvider clock, params (string Key, string Value)[] settings) => new(
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(
                setting => setting.Key,
                setting => (string?)setting.Value))
            .Build(),
        NullLoggerFactory.Instance,
        clock);

    internal static Lobby Open(params (string Key, string Value)[] settings) => new(
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(
                setting => setting.Key,
                setting => (string?)setting.Value))
            .Build(),
        NullLoggerFactory.Instance);
}
