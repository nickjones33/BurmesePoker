using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// ✅ <b>P19 acceptance 1 and 2 — the dial is calibrated, and a test says the calibration has
/// not silently inverted.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The published calibration is not this file.</b> Separating four levels takes tens of
/// thousands of games — at 8,008 games a cell the 95% half-width on a margin between two
/// thinking players is about a point (P17) — and it lives in
/// <c>docs/strategy/measurements.csv</c>, which <c>sim suite</c> regenerates and
/// <c>docs/STRATEGY.md</c> quotes. What a test can afford is the ordering, at the reference
/// table, from a fixed seed.
/// </para>
/// <para>
/// ⚠️ <b>So a failure here is a real one.</b> The run is deterministic, so this cannot fail for
/// being unlucky: it fails when a level's ε has been moved, when a rung underneath the dial has
/// changed what it plays, or when the decorator has stopped meaning what it meant.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class DifficultyCalibrationTests
{
    /// <summary>
    /// The reference table: every level at one table, fully crossed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>Balanced and not rotated</b> (P16): a rotation seats one pattern, so every level
    /// would sit downstream of the same neighbour every game — the honest answer to "what
    /// happens at that table" and not to "which is harder to beat". 256 assignments at four
    /// seats, and the games are exactly one pass through them, so seat balance is arithmetic
    /// rather than luck.
    /// </para>
    /// <para>
    /// ⚠️ <b>One pass and no more, because this class is the most expensive in the suite.</b>
    /// The published gaps are five to nine points and the 95% half-width at 256 games is about
    /// five, so the ordering is what a run this size can hold — the <em>sizes</em> are the
    /// suite's job and are quoted from <c>docs/strategy/measurements.csv</c>.
    /// </para>
    /// </remarks>
    private static SimulationReport ReferenceTable()
    {
        var levels = StrategyCatalog.Levels;

        return Simulator.Run(new SimulationOptions
        {
            Strategies = levels,
            Assignments = SeatingPlan.Balanced(levels, RoundEngine.MinimumPlayers),
            Seats = RoundEngine.MinimumPlayers,
            Games = 256,
            RoundsPerGame = 1,
            MasterSeed = 20260819,
            Parallel = true
        }.Validated());
    }

    [Fact]
    public void TheDialIsOrderedAtTheReferenceTable()
    {
        var series = StrategySeries.Of(ReferenceTable());

        var byDial = DifficultyLadder.All
            .Select(level => Measurement.Of(series.Single(one => one.Name == level.Name).WinRate).Mean)
            .ToList();

        // Weakest first, so every level wins more often than the one below it. ⚠️ A strict
        // comparison and not a tolerance: the ladder may not silently invert, and at this run
        // size the published gaps of about seven points have room to spare.
        Assert.Equal(byDial.Order(), byDial);
        Assert.Equal(byDial.Distinct().Count(), byDial.Count);

        // And the whole dial spans something a person could feel — not four names for one
        // opponent (BUILD-PLAN §3.12 item 2).
        Assert.True(
            byDial[^1] - byDial[0] > 0.15,
            $"the dial spans {(byDial[^1] - byDial[0]) * 100:0.0} points, which is not a difficulty setting.");
    }

    /// <summary>
    /// ✅ <b>P19 acceptance 4 — a level is deterministic, journals, and replays.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A decorator that drew from <see cref="Random.Shared"/> would pass every other test
    /// in this suite</b> and quietly cost the whole harness its reproducibility (BUILD-PLAN
    /// §3.7 item 1) — and this one sits at three or four seats of every table. A journal is the
    /// stricter half: it records the decision every seat made, so a replay that agrees row for
    /// row is a statement about the mistakes themselves and not only about the totals (§3.9).
    /// </remarks>
    [Fact]
    public void ADialRunJournalsAndReplaysToTheSameRows()
    {
        var run = new SimulationOptions
        {
            Strategies = StrategyCatalog.Levels,
            Seats = RoundEngine.MinimumPlayers,
            Games = 8,
            RoundsPerGame = 2,
            MasterSeed = 20260819,
            Journal = JournalFidelity.Thin,
            Parallel = false
        }.Validated();

        var played = Simulator.Run(run);
        var rows = CsvReport.Rows(played).ToList();

        Assert.Equal(rows, CsvReport.Rows(Replay.Run([.. played.Games.Select(game => game.Journal!)])).ToList());
        Assert.Equal(rows, CsvReport.Rows(Simulator.Run(run with { Parallel = true })).ToList());
        Assert.NotEqual(rows, CsvReport.Rows(Simulator.Run(run with { MasterSeed = 7 })).ToList());

        // ⚠️ The header names the seat's level, which is what makes a journal re-joinable to a
        // published number (P18): a run of the dial is attributable to the dial.
        Assert.Equal(
            StrategyCatalog.Levels.Select(level => level.Name).Order(),
            played.Games[0].Journal!.Header.Seats.Select(seat => seat.Strategy).Distinct().Order());
    }

    /// <summary>
    /// ✅ <b>P19 acceptance 1b — the family is the adjacent steps, not the round-robin.</b>
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Only "does <em>n+1</em> beat <em>n</em>" is a claim the menu makes.</b> Correcting
    /// over all k(k−1)/2 pairs is a needless power loss — and it costs the cells too: four
    /// levels are three head-to-head runs rather than six.
    /// </remarks>
    [Fact]
    public void OnlyTheStepsOfTheDialAreComparedAndCorrected()
    {
        var report = Tournament.Run(new TournamentOptions
        {
            Strategies = StrategyCatalog.Levels,
            Pairs = PairFamily.Adjacent,
            Seats = RoundEngine.MinimumPlayers,
            GamesPerCell = 14,
            MasterSeed = 20260819,
            Parallel = false
        });

        Assert.Equal(StrategyCatalog.Levels.Count - 1, report.Pairs.Count);
        Assert.Equal(report.Pairs.Count, report.Comparisons);
        Assert.Equal(report.Pairs.Count, report.Verdicts.Count);

        for (var step = 0; step < report.Pairs.Count; step++)
        {
            Assert.Equal(StrategyCatalog.Levels[step].Name, report.Pairs[step].Row);
            Assert.Equal(StrategyCatalog.Levels[step + 1].Name, report.Pairs[step].Column);
            Assert.True(report.Met(report.Pairs[step].Row, report.Pairs[step].Column));
        }

        // ⚠️ Two levels apart never met, so the matrix has a hole in it rather than a dead heat
        // — which is why a report has to ask before it reads a cell.
        Assert.False(report.Met("easy", "hard"));
        Assert.False(report.Met("easy", "expert"));

        // A round-robin over the same field is the other question, and still answerable.
        var everyone = Tournament.Run(new TournamentOptions
        {
            Strategies = StrategyCatalog.Levels,
            Seats = RoundEngine.MinimumPlayers,
            GamesPerCell = 14,
            MasterSeed = 20260819,
            Parallel = false
        });

        Assert.Equal(
            StrategyCatalog.Levels.Count * (StrategyCatalog.Levels.Count - 1) / 2,
            everyone.Pairs.Count);
        Assert.True(everyone.Met("easy", "expert"));
    }
}
