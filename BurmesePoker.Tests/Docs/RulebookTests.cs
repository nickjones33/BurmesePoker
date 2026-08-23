using System.Globalization;
using System.Text.RegularExpressions;

using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Docs;

/// <summary>
/// ✅ <b>P38 — the rulebook cannot fall behind the rules without something turning red.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>A rulebook is the highest-consequence stale document this project could own</b>: it is
/// the one a person plays from, and it is the furthest from anything a build would break — no
/// command in it to fail, no figure with a CSV column to disagree with. So every joint it has
/// is asserted here: the revision it was derived from, the worked round it prints, the house
/// readings it promises, and the voice it must keep.
/// </para>
/// <para>
/// ⚠️ <b><c>RULES.md</c> stays the sole rules authority and the rulebook decides nothing.</b>
/// Nothing here checks that a rule is <i>stated correctly</i> — prose cannot be joined to
/// prose. What can be joined is re-derivable: a revision number, a seeded round, a list of
/// open-question rows.
/// </para>
/// </remarks>
public class RulebookTests
{
    private static string Rulebook => Documentation.Text("docs/RULEBOOK.md");

    /// <summary>
    /// ✅ <b>The revision the rulebook stamps is the revision the rules are at.</b>
    /// </summary>
    /// <remarks>
    /// The <see cref="JournalHeader.CurrentRulesRevision"/> idiom, applied to a document: that
    /// constant is already bound to <c>RULES.md</c>'s own header by
    /// <c>GameJournalTests.TheRevisionStampedIsTheRevisionRulesMdIsAt</c>, so binding the
    /// rulebook to the constant binds it to the document. <b>A play-changing revision bumps the
    /// constant, and this goes red until somebody re-reads the rulebook against what changed</b>
    /// — which is the whole point: re-reading is the maintenance, and this is what compels it.
    /// </remarks>
    [Fact]
    public void TheRevisionTheRulebookWasDerivedFromIsTheRevisionTheRulesAreAt()
    {
        var stamped = Regex.Match(Rulebook, @"\brev\s*\**\s*(\d+)");

        Assert.True(stamped.Success, "docs/RULEBOOK.md no longer says which revision it was derived from.");

        Assert.Equal(
            JournalHeader.CurrentRulesRevision,
            int.Parse(stamped.Groups[1].Value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// ✅ <b>The worked round is generated, not invented: replaying its printed seed reproduces
    /// every figure it teaches from.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>An invented example in a rulebook is a bug that teaches itself to every reader</b>
    /// (BUILD-PLAN P38 build item 2). So the example is a real dealt round, and this test is
    /// the generator run again: same seed, same construction, and every fact the prose quotes —
    /// the turn-up, who the deck gave money to, the winner, the turn count, and all fifteen
    /// cells of the settlement table — asserted against what the engine actually does.
    /// </para>
    /// <para>
    /// ⚠️ <b>The construction below is the example's definition</b>: five of the strongest
    /// catalog rung, seat seeds <c>seed × 100 + seat</c>. A rules change that moves any of
    /// this round's numbers goes red here, which is exactly what a derived document wants.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheWorkedRoundIsTheRoundItsSeedActuallyPlays()
    {
        var text = Rulebook;
        var seedMatch = Regex.Match(text, @"seed \*\*(\d+)\*\*");

        Assert.True(seedMatch.Success, "docs/RULEBOOK.md's worked round no longer prints its seed.");

        var seed = int.Parse(seedMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 5).Select(id => new PlayerId(id))];
        var rung = BotCatalog.Resolve("outs");
        var agents = players.ToDictionary(
            player => player,
            player => rung.Create((seed * 100) + player.Value));

        var engine = RoundEngine.Shuffled(players, agents, Stakes.Standard, new Random(seed));
        var table = engine.Table;

        // The five hands as dealt, written down before a card moves.
        var dealt = table.Seats.Select(seat => string.Join(" ", seat.Hand)).ToList();

        var result = engine.Play();

        foreach (var hand in dealt)
        {
            Assert.Contains(hand, text);
        }

        // The two turned-up cards, exactly as dealt.
        var turnUp = Regex.Match(text, @"the \*\*(\S+)\*\* from the bottom of the deck and the \*\*(\S+)\*\* from the top");

        Assert.True(turnUp.Success, "the worked round no longer says what was turned up.");
        Assert.Equal(table.TurnedUpFromBottom.ToString(), turnUp.Groups[1].Value);
        Assert.Equal(table.TurnedUpFromTop.ToString(), turnUp.Groups[2].Value);

        // Who the deck gave money to, card by card, with each card's multiplier.
        var configuration = table.MoneyCards.ConfigurationOf(table.Ownership, table.Shoe);
        var owned = table.Ownership.Records
            .Select(record => (Card: table.Shoe[record.Key.Value], Owner: record.Value))
            .Select(entry => (entry.Card, entry.Owner,
                Multiplier: table.MoneyCards.Multiplier(entry.Card, entry.Owner, configuration)))
            .Where(entry => entry.Multiplier > 0)
            .ToList();

        var printed = Regex.Matches(text, @"\|\s*([^(|]+?) \(×(\d)\)\s*\|\s*([A-Z][a-z]+)\s*\|\s*\$(\d+) × 4 = \*\*\$(\d+)\*\*")
            .Select(match => match.Groups)
            .ToList();

        Assert.Equal(owned.Count, printed.Count);

        foreach (var groups in printed)
        {
            var card = groups[1].Value.Trim();
            var multiplier = int.Parse(groups[2].Value, CultureInfo.InvariantCulture);
            var owner = Seat(groups[3].Value);
            var each = int.Parse(groups[4].Value, CultureInfo.InvariantCulture);
            var collects = int.Parse(groups[5].Value, CultureInfo.InvariantCulture);

            Assert.Contains(owned, entry =>
                Name(entry.Card) == card && entry.Owner == owner && entry.Multiplier == multiplier);
            Assert.Equal(multiplier * Stakes.Standard.MoneyCardValue, each);
            Assert.Equal(each * (players.Count - 1), collects);
        }

        // The end of the round: who won, in how many turns, laying down what.
        var ending = Regex.Match(text, @"ran \*\*(\d+) turns\*\* before \*\*([A-Z][a-z]+)\*\* went out");

        Assert.True(ending.Success, "the worked round no longer says who won or how long it ran.");
        Assert.Equal(result.Turns, int.Parse(ending.Groups[1].Value, CultureInfo.InvariantCulture));
        Assert.Equal(result.Winner, Seat(ending.Groups[2].Value));

        foreach (var meld in result.Melds)
        {
            Assert.Contains($"{string.Join(" ", meld.Slots)}", text);
        }

        // The settlement table, all fifteen cells, and the split the prose teaches from:
        // round payment and money cards are separate ledgers that sum to the net.
        var rounds = Settlement.RoundPayments(players, result.Winner, Stakes.Standard, result.Win);
        var rows = Regex.Matches(
                text, @"\|\s*([A-Z][a-z]+)\s*\|\s*([+-]?\$\d+)\s*\|\s*([+-]?\$\d+)\s*\|\s*\*\*([+-]?\$\d+)\*\*")
            .Select(match => match.Groups)
            .ToList();

        Assert.Equal(players.Count, rows.Count);

        foreach (var groups in rows)
        {
            var player = Seat(groups[1].Value);
            var net = result.Payouts[player];

            Assert.Equal(rounds[player], Money(groups[2].Value));
            Assert.Equal(net - rounds[player], Money(groups[3].Value));
            Assert.Equal(net, Money(groups[4].Value));
        }
    }

    /// <summary>
    /// ✅ <b>The house readings cover every question the rules are playing on a recorded
    /// default — and cite nothing else.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>A rulebook must state one answer, which silently promotes a default to a rule for
    /// the reader</b> (BUILD-PLAN P38 build item 4) — so the honest form is a closing section
    /// naming each point the table had to choose. The set is derived from <c>RULES.md</c> §9
    /// itself: the un-struck numbered rows of its open-question tables, told apart from the
    /// closed tables by their five-column shape. <b>A question closing or opening moves that
    /// set, and this test is how the rulebook finds out.</b>
    /// </para>
    /// <para>
    /// ⚠️ <b>Both directions</b>: an open row the appendix does not name is a house choice the
    /// reader was not told about, and a citation of a row that is no longer open is the
    /// rulebook promising a question that has been answered.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheHouseReadingsAreExactlyTheQuestionsTheRulesPlayOnADefault()
    {
        var open = Regex.Matches(Documentation.Text("docs/RULES.md"), @"^\|\s*(\d+)\s*\|.*$", RegexOptions.Multiline)
            .Where(match => match.Value.Trim().Trim('|').Split('|').Length >= 5)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(open);

        var appendix = Rulebook[Rulebook.IndexOf("## How our table reads", StringComparison.Ordinal)..];
        var cited = Regex.Matches(appendix, @"#(\d+)")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        var untold = open.Where(row => !cited.Contains(row)).Order().ToList();
        var stale = cited.Where(row => !open.Contains(row)).Order().ToList();

        Assert.True(
            untold.Count == 0,
            "RULES.md §9 plays these on a recorded default and the rulebook's house readings do not "
            + "mention them: #" + string.Join(", #", untold));

        Assert.True(
            stale.Count == 0,
            "The rulebook's house readings cite §9 rows that are no longer open questions: #"
            + string.Join(", #", stale) + ". The answer arrived; fold it into the body.");
    }

    /// <summary>
    /// ✅ <b>The rulebook's voice is a rulebook's: it says what happens, never who remembered
    /// it or which packet built it.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>These are the packet's own "must nots"</b> (BUILD-PLAN P38): no provenance tags,
    /// no packet numbers, no confidence scale, and not the word the rest of the documentation
    /// set uses for what <c>RULES.md</c> is — a reader being taught a game must not be taught
    /// that the game is uncertain of itself. The one deliberate exception is the closing
    /// pointer at <c>RULES.md</c> §9, which is where the house readings send a curious reader.
    /// </remarks>
    [Fact]
    public void TheRulebookSpeaksWithARulebooksVoice()
    {
        var text = Rulebook;

        foreach (var banned in new[] { "reconstruction", "EXPERT", "PLAYER", "DERIVED", "Settled", "Probable", "Tentative" })
        {
            Assert.False(
                text.Contains(banned, StringComparison.Ordinal),
                $"docs/RULEBOOK.md says \"{banned}\", which is the project's voice and not a rulebook's.");
        }

        Assert.False(
            Regex.IsMatch(text, @"\bP\d{1,2}(?:\.\d)?\b"),
            "docs/RULEBOOK.md names a work packet. A rulebook has no build history in it.");
    }

    /// <summary>The seats of the worked round, in turn order, as the prose names them.</summary>
    private static readonly string[] Seats = ["Aye", "Bo", "Cho", "Da", "Ei"];

    private static PlayerId Seat(string name) =>
        new(Array.IndexOf(Seats, name) is var seat && seat >= 0
            ? seat
            : throw new ArgumentException($"The worked round has no seat called {name}.", nameof(name)));

    private static int Money(string cell) =>
        int.Parse(cell.Replace("$", string.Empty), CultureInfo.InvariantCulture);

    /// <summary>A card as the rulebook prints it — a joker by its colour, anything else as itself.</summary>
    private static string Name(Domain.Cards.Card card) =>
        card.IsJoker
            ? $"{card.Color.ToString().ToLowerInvariant()} joker"
            : card.ToString();
}
