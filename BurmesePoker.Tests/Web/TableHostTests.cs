using BurmesePoker.Domain.Play;
using BurmesePoker.Web;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// How the site's one table is opened (packet P13.4): who is in it, and which connection your
/// half of the page is drawn from.
/// </summary>
/// <remarks>
/// ⚠️ <b>The seat's connection is your own, never the watcher's.</b> A watcher is sent the public
/// game and no hand at all (P13.2), so a page that drew a hand from one would draw nothing —
/// and a page that drew the public board from a <em>seat</em> would put your blind draws in
/// front of anyone else looking at it. The two are separate here and stay separate.
/// </remarks>
public class TableHostTests
{
    [Fact]
    public async Task ASeatIsYoursAndItIsYourOwnConnection()
    {
        await using var host = Open(("seat", "1"), ("name", "Nick"));

        Assert.Equal(1, host.SeatNumber);
        Assert.NotNull(host.Yours);
        Assert.Equal(new PlayerId(1), host.Yours!.Player);
        Assert.Equal("Nick", host.Yours.Name);

        // The public board is the watcher's fold and knows the seating; your own half knows a
        // hand, and nothing has been dealt yet.
        Assert.Equal(4, host.Board.Seating.Count);
        Assert.Equal("Nick", host.Board.Names[new PlayerId(1)]);
        Assert.Null(host.Yours.Hand);
    }

    /// <remarks>
    /// P13.3's table, still there: <c>--seat 0</c> is a room with nobody in it, which is the
    /// strictest case for concealment and the one the leak test was written against.
    /// </remarks>
    [Fact]
    public async Task NobodyHasToBePlaying()
    {
        await using var host = Open(("seat", "0"));

        Assert.Equal(0, host.SeatNumber);
        Assert.Null(host.Yours);
        Assert.All(host.Board.Names.Values, name => Assert.EndsWith("(bot)", name, StringComparison.Ordinal));
    }

    [Fact]
    public async Task EverySeatButYoursIsTheComputers()
    {
        await using var host = Open(("seat", "3"), ("name", "Nick"), ("seats", "5"));

        Assert.Equal(5, host.Seats);
        Assert.Equal(new PlayerId(3), host.Yours!.Player);
        Assert.Equal("Nick", host.Board.Names[new PlayerId(3)]);
        Assert.Equal(4, host.Board.Names.Count(name => name.Value.EndsWith("(bot)", StringComparison.Ordinal)));
    }

    [Fact]
    public void ASeatThatIsNotAtTheTableIsRefusedWhenTheTableOpens()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Open(("seat", "9")));
        Assert.Throws<ArgumentOutOfRangeException>(() => Open(("seat", "-1")));
    }

    /// <remarks>
    /// The same seed deals the same table, which is how a browser match is reproduced at all
    /// (P11, P14) — and the hints toggle starts where the configuration put it.
    /// </remarks>
    [Fact]
    public async Task TheTableCarriesItsSeedAndItsSettings()
    {
        await using var host = Open(("seed", "20260819"), ("hints", "false"), ("pace", "10"));

        Assert.Equal(20260819, host.Seed);
        Assert.False(host.Hints);
        Assert.Equal(TimeSpan.FromMilliseconds(10), host.Pace);
        Assert.False(host.IsDealing);
    }

    private static TableHost Open(params (string Key, string Value)[] settings) => new(
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(
                setting => setting.Key,
                setting => (string?)setting.Value))
            .Build(),
        NullLogger<TableHost>.Instance);
}
