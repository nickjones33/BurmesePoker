using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Web;

namespace BurmesePoker.Tests.Web;

/// <summary>
/// A hosted table played with <c>--journal</c> writes itself down as it plays (packet P24.1).
/// </summary>
/// <remarks>
/// <para>
/// <b>The file half of the claim.</b> That the record replays is the server's test
/// (<c>TableJournalTests</c>); what belongs to the Web layer is that a table which deals on its
/// own thread flushes the file after every settled round — so what is on disk survives the
/// process, and a browser round can be audited without closing the table (P30.2).
/// </para>
/// <para>
/// ⚠️ <b>Polled rather than awaited</b>, like everything about a table that deals on its own
/// thread (§3.6, <c>TwoPeopleTests</c>).
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class HostedTableJournalTests
{
    [Fact]
    public async Task ATableOfBotsWritesItsJournalAfterARound()
    {
        var path = Path.Combine(Path.GetTempPath(), $"burmese-poker-{Guid.NewGuid():N}.jsonl");

        try
        {
            // A long gap between rounds, so the file on disk is round 1's and stays still
            // while it is read (TwoPeopleTests' lesson: a settlement is not a resting state).
            await using var table = HostedTableTests.Open(
                seats: 4, people: 0, betweenSeconds: 60, journal: path);

            table.Arrive();

            var journal = await Written(path);

            table.Leave();

            Assert.True(journal.Header.Rounds >= 1);
            Assert.Equal(4, journal.Header.TableSize);
            Assert.NotEmpty(journal.Decisions);

            // Every seat is the computer's here, attributed to the level it plays.
            Assert.All(
                journal.Header.Seats,
                seat => Assert.Equal(DifficultyLadder.Default.Name, seat.Strategy));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>The journal, once the table has written one with a settled round in it.</summary>
    private static async Task<GameJournal> Written(string path)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path))
            {
                try
                {
                    var journal = JournalFormat.Read(File.ReadAllLines(path));

                    if (journal.Header.Rounds >= 1)
                    {
                        return journal;
                    }
                }
                catch (JournalException)
                {
                    // Caught the writer mid-file; the next poll reads a whole one.
                }
                catch (System.Text.Json.JsonException)
                {
                    // Likewise.
                }
            }

            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        Assert.Fail("The table never wrote a journal with a settled round in it.");
        return null!;
    }
}
