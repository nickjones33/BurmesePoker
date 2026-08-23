using System.Globalization;
using System.Diagnostics;

using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>What the standing suite is run with.</summary>
public sealed record SuiteOptions
{
    /// <summary>
    /// The ladder, weakest first — <b>the rungs settled on win rate</b>.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A filter of the catalog and never a list</b> (BUILD-PLAN P18, P20, P23). The
    /// default used to be every rung there is, which made <c>prospector</c> six head-to-head
    /// cells that reproduce an identity a unit test already asserts — three quarters of an hour
    /// of run time, and six more comparisons in the Holm family that every real verdict is
    /// corrected against. It is measured by <see cref="Ratios"/>' sweep instead, which is what
    /// <see cref="RankedOn"/> records and what <c>StandingAnswerTests</c> holds the two halves
    /// of together.
    /// </remarks>
    public IReadOnlyList<Strategy> Strategies { get; init; } = StrategyCatalog.Ladder;

    /// <summary>
    /// The difficulty dial, weakest first (BUILD-PLAN P19).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A second tournament and not more rows in the first one.</b> Every level is the
    /// hardest rung with a handicap, so <c>expert</c> and <c>greedy</c> are the same player
    /// under two names — put in one field they would be a null cell dressed up as a
    /// comparison, and the free-for-all that gives each level its reference win rate would be
    /// diluted by four rungs nobody is offered.
    /// </remarks>
    public IReadOnlyList<Strategy> Levels { get; init; } = StrategyCatalog.Levels;

    /// <summary>
    /// The money sweep's stakes ratios, cheapest side bet first (BUILD-PLAN P22).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A third run and not more rows in either of the other two.</b> Every other cell in
    /// this suite is played at <see cref="Stakes.Standard"/>, and the whole point of this one is
    /// that the stakes are the independent variable — a ratio mixed into a field would be a
    /// different game seated beside the one being ranked.
    /// </remarks>
    public IReadOnlyList<Stakes> Ratios { get; init; } = MoneySweep.DefaultRatios;

    /// <summary>
    /// The rung whose take decision reads the side bet, and the rung it is one change from.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Read off the catalog rather than typed</b> (BUILD-PLAN P23). Both were literals
    /// until P23, which is the same defect one layer up from the one P18 and P20 each removed: a
    /// second stakes-reading rung would have been added to <see cref="BotCatalog"/> and measured
    /// by nothing. The challenger is the stakes-sensitive rung; the reference is the strongest
    /// rung there is.
    /// ⚠️ <b>It read <c>Ladder[^1]</c> until P31 and now reads <see cref="BotCatalog.Hardest"/></b>
    /// — the same rung, said properly. Adding <c>warden</c> put a second branch on <c>outs</c>, so
    /// the ladder's last entry is the branch written last rather than the strongest thing in it,
    /// and the old expression would have measured the side bet against a rung that had never been
    /// ranked above anything.
    /// </remarks>
    public string MoneyChallenger { get; init; } = BotCatalog.StakesSensitive[^1].Name;

    /// <inheritdoc cref="MoneyChallenger"/>
    public string MoneyReference { get; init; } = BotCatalog.Hardest.Name;

    /// <summary>
    /// The rung the two claim-permission arms are built on (BUILD-PLAN P29).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Read off the catalog rather than typed</b>, for the reason
    /// <see cref="MoneyChallenger"/> is: the question is what refusing is worth to the best
    /// player there is, so it re-bases itself when a rung raises the ceiling. The two arms are
    /// <c>{rung}/refuse</c> and <c>{rung}/allow</c>; see <see cref="ClaimPolicy"/>.
    /// </remarks>
    public string ClaimRung { get; init; } = BotCatalog.Hardest.Name;

    /// <summary>
    /// The table this project's standing answer is about — <b>five seats</b> (BUILD-PLAN P32).
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A decision, spelled as a number.</b> It was <c>RoundEngine.MinimumPlayers</c> until
    /// P32, which is how the whole published set came to be four-handed <em>by accident</em>:
    /// nobody ever chose four, it was simply the smallest legal table and the field inherited
    /// the constant. ⚠️ <b>The two are not interchangeable</b> — by <c>RULES.md</c> §7.1.1 a
    /// four-handed declaration owes a joker-free series and a five-handed one owes no series at
    /// all, and by §7.3 a jokerless hand is paid ×2 at four and ×3 at five, so "the minimum
    /// table" and "the default table" are two different games. Changing this constant changes
    /// which game the documentation is about, and nothing else in the suite should have to be
    /// touched to do it.
    /// </remarks>
    public const int DefaultSeats = RoundEngine.DefaultPlayers;

    /// <summary>Seats at the table (RULES.md §2.1).</summary>
    /// <inheritdoc cref="DefaultSeats" path="/remarks"/>
    public int Seats { get; init; } = DefaultSeats;

    /// <summary>Games wanted in each cell, rounded up to a whole number of passes.</summary>
    public int GamesPerCell { get; init; } = 2000;

    /// <summary>The one number the whole suite reproduces from.</summary>
    public int MasterSeed { get; init; } = 20260819;

    /// <summary>Turns before a stalled round is given up on.</summary>
    public int TurnCap { get; init; } = 400;

    /// <summary>What the rounds are played for.</summary>
    public Stakes Stakes { get; init; } = Stakes.Standard;

    /// <summary>Whether the games inside a cell run concurrently. Results are identical either way.</summary>
    public bool Parallel { get; init; } = true;
}

/// <summary>
/// One published number, with everything needed to re-examine it.
/// </summary>
/// <param name="Id">A stable key. <b>The documentation quotes this</b>, so it may not be renamed lightly.</param>
/// <param name="Question">What it answers, in a sentence.</param>
/// <param name="Command">The command that produced it (BUILD-PLAN §3.12).</param>
/// <param name="Subject">Who or what it is about.</param>
/// <param name="Metric">What was measured.</param>
/// <param name="Value">The estimate, with its interval.</param>
/// <param name="Verdict">A word, where there is one to say. Empty otherwise.</param>
public sealed record SuiteMeasurement(
    string Id,
    string Question,
    string Command,
    string Subject,
    string Metric,
    Measurement Value,
    string Verdict);

/// <summary>What the suite measured, and the tournaments it mostly came from.</summary>
/// <param name="Options">What it was run with.</param>
/// <param name="Measurements">Every published number.</param>
/// <param name="Tournament">The ladder ranked — the research instrument.</param>
/// <param name="Difficulty">The dial calibrated — the product (BUILD-PLAN P19).</param>
/// <param name="Money">The side bet swept — the one axis that is not rummy (BUILD-PLAN P22).</param>
/// <param name="Claim">
/// The claim's permission, refusing against allowing — the one decision in the engine that was
/// taken rather than measured (BUILD-PLAN P28 item 5, P29).
/// </param>
/// <param name="Elapsed">How long it took.</param>
public sealed record SuiteReport(
    SuiteOptions Options,
    IReadOnlyList<SuiteMeasurement> Measurements,
    TournamentReport Tournament,
    TournamentReport Difficulty,
    MoneyReport Money,
    TournamentCell Claim,
    TimeSpan Elapsed)
{
    /// <summary>
    /// Whether every step of the dial beat the one below it, correction and all.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A level that is not separated from its neighbour is a lie told to everybody who
    /// reads the menu</b> (BUILD-PLAN §3.12 item 2), so this is checked on every run rather
    /// than at the packet that set the values. It is the same standing as the null test: the
    /// suite exits non-zero if it ever stops holding.
    /// </remarks>
    public bool DialIsSeparated =>
        Difficulty.Verdicts.Count == Difficulty.Options.Strategies.Count - 1
        && Difficulty.Verdicts.All(verdict => verdict.Survives)
        && Difficulty.Pairs.All(cell => cell.Margin.Mean < 0);
}

/// <summary>
/// The standing set of measurements the documentation quotes (BUILD-PLAN P17).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The numbers in the documentation stop being transcribed and start being generated.</b>
/// <c>RULES.md</c> tags every rule with its provenance because a rule without a source cannot be
/// re-examined; a measurement without its origin is worse, because it looks like a fact. Every
/// row here carries the command that made it and the games it came from, and
/// <c>docs/STRATEGY.md</c> quotes the file rather than a session's console.
/// </para>
/// <para>
/// <b>It is deliberately small.</b> A suite that measured everything measurable would never be
/// re-run, and a stale suite is worse than none. What is in it is what the docs actually claim:
/// the ladder at one table, the ladder head to head, the harness's own null test, and P12's
/// headline under both seatings — the one figure this project has already had to correct once.
/// </para>
/// </remarks>
public static class Suite
{
    /// <summary>Where the suite writes unless told otherwise.</summary>
    public const string DefaultPath = "docs/strategy/measurements.csv";

    /// <summary>Plays the standing set.</summary>
    public static SuiteReport Run(SuiteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var clock = Stopwatch.StartNew();

        var tournament = Tournament.Run(new TournamentOptions
        {
            Strategies = options.Strategies,
            Seats = options.Seats,
            GamesPerCell = options.GamesPerCell,
            MasterSeed = options.MasterSeed,
            TurnCap = options.TurnCap,
            Stakes = options.Stakes,
            CountLockBites = true,
            Parallel = options.Parallel
        });

        // ⚠️ Only the adjacent pairs, because only "n+1 beats n" is a claim the menu makes
        // (BUILD-PLAN P19 acceptance 1b). Correcting a dial over the whole round-robin is a
        // power loss paid for comparisons nobody is making.
        var difficulty = Tournament.Run(new TournamentOptions
        {
            Strategies = options.Levels,
            Pairs = PairFamily.Adjacent,
            Seats = options.Seats,
            GamesPerCell = options.GamesPerCell,
            MasterSeed = options.MasterSeed,
            TurnCap = options.TurnCap,
            Stakes = options.Stakes,
            CountLockBites = true,
            Parallel = options.Parallel
        });

        // ⚠️ The stakes are the independent variable here and are Stakes.Standard everywhere
        // else in this suite, which is why it is a third run rather than more cells in the
        // first (BUILD-PLAN P22).
        var money = MoneySweep.Run(new MoneySweepOptions
        {
            Challenger = StrategyCatalog.Resolve(options.MoneyChallenger),
            Reference = StrategyCatalog.Resolve(options.MoneyReference),
            Ratios = options.Ratios,
            Seats = options.Seats,
            GamesPerCell = options.GamesPerCell,
            MasterSeed = options.MasterSeed,
            TurnCap = options.TurnCap,
            Parallel = options.Parallel
        });

        // 🔥 One cell, and no correction, because one comparison is its own family. Every rung
        // refuses whenever RULES.md §4.5 allows it, which P28 decided from the rule's own
        // reasoning and did not measure; these two arms differ in that answer and in nothing
        // else (BUILD-PLAN P29).
        var claimRung = BotCatalog.Resolve(options.ClaimRung);
        var arms = ClaimPolicy.Both(claimRung).Select(Strategy.Of).ToList();

        var claim = Tournament.HeadToHead(
            new TournamentOptions
            {
                Strategies = arms,
                Seats = options.Seats,
                GamesPerCell = options.GamesPerCell,
                MasterSeed = options.MasterSeed,
                TurnCap = options.TurnCap,
                Stakes = options.Stakes,
                Parallel = options.Parallel
            },
            arms[0],
            arms[1]);

        var measurements = new List<SuiteMeasurement>();
        var names = string.Join(",", options.Strategies.Select(strategy => strategy.Name));

        var tournamentCommand = string.Create(CultureInfo.InvariantCulture,
            $"BurmesePoker.Sim -- tournament --strategies {names} --seats {options.Seats} "
            + $"--games {options.GamesPerCell} --seed {options.MasterSeed}");

        foreach (var player in tournament.FreeForAll.Players)
        {
            measurements.Add(new SuiteMeasurement(
                $"ladder.free-for-all.{player.Name}",
                "How often does this rung declare with the whole ladder at one table, fully crossed?",
                tournamentCommand,
                player.Name,
                "win rate",
                player.WinRate,
                string.Empty));
        }

        for (var index = 0; index < tournament.Pairs.Count; index++)
        {
            var cell = tournament.Pairs[index];
            var verdict = tournament.Verdicts[index];

            measurements.Add(new SuiteMeasurement(
                $"ladder.head-to-head.{cell.Row}-over-{cell.Column}",
                "Head to head at every seating in which both are at the table, how many points "
                + "of win rate separate them?",
                tournamentCommand,
                cell.Label,
                "win rate margin",
                cell.Margin,
                verdict.Survives ? "separated (Holm)" : verdict.Separated ? "raw only" : "inside the interval"));
        }

        foreach (var standing in tournament.Ranking)
        {
            measurements.Add(new SuiteMeasurement(
                $"ladder.rank.{standing.Name}",
                "Averaged over the field, how many points does this rung beat an opponent by?",
                tournamentCommand,
                standing.Name,
                "mean head-to-head margin",
                new Measurement(tournament.Comparisons, standing.MeanMargin, 0),
                $"{standing.Beaten}-{standing.LostTo}-{standing.Undecided} beaten/lost/undecided"));
        }

        foreach (var player in tournament.NullCell.Players)
        {
            measurements.Add(new SuiteMeasurement(
                $"harness.null-test.{player.Name}",
                $"A strategy against a copy of itself must win {1.0 / options.Seats:0.0%} of the "
                + "games. Anything else is the apparatus, not the strategy.",
                tournamentCommand,
                player.Name,
                "win rate",
                player.WinRate,
                tournament.NullTestHolds ? "holds" : "FAILED"));
        }

        foreach (var check in tournament.Pairing)
        {
            measurements.Add(new SuiteMeasurement(
                $"pairing.{check.Scope}.{check.Label}",
                "Paired standard error over independent, on the same data. Below one is variance "
                + "reduction; above one means the independent formula was understating it.",
                tournamentCommand,
                check.Label,
                "standard error ratio",
                new Measurement(check.Paired.Count, check.Ratio, 0),
                check.Ratio < 1 ? "pairing narrows" : "pairing widens"));
        }

        // 🔥 Two table sizes and not one (BUILD-PLAN P32 item 4). This is the longest-running
        // measurement in the project — P12 published it, P16 corrected it and P23, P29 and P33
        // each reproduced it — and moving the standing set to five seats would have ended that
        // series rather than continued it. ⚠️ The two rows are *not* one measurement said twice:
        // by RULES.md §7.1.1 a four-handed declaration owes a joker-free series and a five-handed
        // one owes nothing at all, so the pair is the cheapest statement this file makes about
        // which of its findings belong to the game and which belonged to four seats.
        foreach (var seats in HeadlineSeats(options))
        {
            foreach (var (plan, balanced) in new[] { ("rotate", false), ("balanced", true) })
            {
                var headline = Headline(options, seats, balanced);

                var command = string.Create(CultureInfo.InvariantCulture,
                    $"BurmesePoker.Sim -- --strategies greedy,simple --seating {plan} --seats {seats} "
                    + $"--games {headline.Games} --seed {options.MasterSeed}");

                foreach (var series in headline.Series)
                {
                    measurements.Add(new SuiteMeasurement(
                        $"headline.{plan}.{seats}-handed.{series.Name}",
                        $"P12's headline: greedy against simple at {seats} seats. ⚠️ The rotation "
                        + "seats [g,s,g,s,…], in which every greedy sits downstream of a simple — the "
                        + "honest strategy-vs-strategy figure is the balanced one (P16). ⚠️ The seat "
                        + "count is part of the id since P32: four-handed and five-handed are different "
                        + "games (RULES.md §7.1.1) and both are published.",
                        command,
                        series.Name,
                        "win rate",
                        Measurement.Of(series.WinRate),
                        string.Empty));
                }
            }
        }

        var levels = string.Join(",", options.Levels.Select(level => level.Name));

        var difficultyCommand = string.Create(CultureInfo.InvariantCulture,
            $"BurmesePoker.Sim -- tournament --strategies {levels} --pairs adjacent "
            + $"--seats {options.Seats} --games {options.GamesPerCell} --seed {options.MasterSeed}");

        foreach (var player in difficulty.FreeForAll.Players)
        {
            measurements.Add(new SuiteMeasurement(
                $"difficulty.reference-table.{player.Name}",
                "The reference table: every difficulty level at one table, fully crossed. This is "
                + "the ordering the menu claims, and it is what a person actually sits down to.",
                difficultyCommand,
                player.Name,
                "win rate",
                player.WinRate,
                string.Empty));
        }

        for (var index = 0; index < difficulty.Pairs.Count; index++)
        {
            var cell = difficulty.Pairs[index];
            var verdict = difficulty.Verdicts[index];

            measurements.Add(new SuiteMeasurement(
                $"difficulty.step.{cell.Column}-over-{cell.Row}",
                "One step of the dial, head to head. ⚠️ The family is the k−1 adjacent steps and "
                + "not the round-robin, because 'n+1 beats n' is the only claim a monotone dial "
                + "makes (BUILD-PLAN P19).",
                difficultyCommand,
                $"{cell.Column} over {cell.Row}",
                "win rate margin",
                cell.Margin with { Mean = -cell.Margin.Mean },
                verdict.Survives ? "separated (Holm)" : verdict.Separated ? "raw only" : "inside the interval"));
        }

        foreach (var level in options.Levels)
        {
            measurements.Add(new SuiteMeasurement(
                $"difficulty.mistake-rate.{level.Name}",
                "ε — how often this level throws the second-best card off its own ranking rather "
                + "than the best. Placed so the levels are evenly spaced in results, which is not "
                + "evenly spaced in ε (BUILD-PLAN P19).",
                difficultyCommand,
                level.Name,
                "mistake rate",
                new Measurement(0, Rate(level.Name), 0),
                string.Empty));
        }

        var ratios = string.Join(
            ",", options.Ratios.Select(stakes => $"{stakes.RoundValue}:{stakes.MoneyCardValue}"));

        var moneyCommand = string.Create(CultureInfo.InvariantCulture,
            $"BurmesePoker.Sim -- money --challenger {options.MoneyChallenger} "
            + $"--reference {options.MoneyReference} --ratios {ratios} --seats {options.Seats} "
            + $"--games {options.GamesPerCell} --seed {options.MasterSeed}");

        for (var index = 0; index < money.Cells.Count; index++)
        {
            var cell = money.Cells[index];
            var verdict = money.Verdicts[index];

            var reading = verdict.Survives ? "separated (Holm)" : verdict.Separated ? "raw only" : "inside the interval";

            measurements.Add(new SuiteMeasurement(
                $"money.net-per-round.{cell.Id}",
                "Should you draw blind for the money? A blind draw confers ownership of a money "
                + "card and a take never does (RULES.md §4.4). ⚠️ This is the verdict figure and "
                + "it is money, not win rate.",
                moneyCommand,
                $"{money.Options.Challenger.Name} over {money.Options.Reference.Name} at {cell.Label}",
                "net per round margin",
                cell.NetMargin,
                reading));

            measurements.Add(new SuiteMeasurement(
                $"money.win-rate.{cell.Id}",
                "The same margin in win rate, reported beside the money because this is the "
                + "first packet where the two can come apart (BUILD-PLAN P22 acceptance 2).",
                moneyCommand,
                $"{money.Options.Challenger.Name} over {money.Options.Reference.Name} at {cell.Label}",
                "win rate margin",
                cell.WinMargin,
                cell.MoneyAndWinRateDisagree ? "points the other way to the money" : string.Empty));

            measurements.Add(new SuiteMeasurement(
                $"money.take-rate.{cell.Id}.{money.Options.Challenger.Name}",
                "The mechanism. The rule can only act by refusing takes, so a cell in which this "
                + "has not moved from the reference's is a cell in which nothing happened.",
                moneyCommand,
                $"{money.Options.Challenger.Name} at {cell.Label}",
                "take rate",
                cell.Player(money.Options.Challenger.Name).TakeRate,
                string.Empty));

            measurements.Add(new SuiteMeasurement(
                $"money.side-margin.{cell.Id}",
                "The money-card half of the net margin alone — where the mechanism has to show "
                + "up if the rule is doing anything. ⚠️ Computed on every run since P22 and "
                + "never published (review R13): it reached bytes only through money.csv, which "
                + "sim suite does not regenerate, so §10's mechanism argument leaned on a file "
                + "one command behind the others.",
                moneyCommand,
                $"{money.Options.Challenger.Name} over {money.Options.Reference.Name} at {cell.Label}",
                "side bet margin",
                cell.SideMargin,
                string.Empty));
        }

        var claimCommand = string.Create(CultureInfo.InvariantCulture,
            $"BurmesePoker.Sim -- tournament --strategies {arms[0].Name},{arms[1].Name} "
            + $"--pairs adjacent --seats {options.Seats} --games {options.GamesPerCell} "
            + $"--seed {options.MasterSeed}");

        measurements.Add(new SuiteMeasurement(
            "claim.permission.refuse-over-allow",
            "Is refusing the opener the turned-up money card worth anything? Every rung refuses "
            + "whenever RULES.md §4.5 allows it, which was decided from the rule's reasoning and "
            + "never measured — and refusing is a disclosure, since only a holder may refuse.",
            claimCommand,
            $"{arms[0].Name} over {arms[1].Name}",
            "win rate margin",
            claim.Margin,
            claim.Margin.IsSeparatedFromZero ? "separated" : "inside the interval"));

        measurements.Add(new SuiteMeasurement(
            "claim.permission.money.refuse-over-allow",
            "The same margin in money a round, reported beside the win rate because a claim is "
            + "a money-card decision and the two can come apart (BUILD-PLAN P22). ⚠️ Paired, "
            + "since review R3: the two arms share one table and money is zero-sum across it, "
            + "which is exactly the case Measurement.Paired's remarks call anti-conservative "
            + "for the independent formula. The ±0.18 P29 published was the unpaired interval.",
            claimCommand,
            $"{arms[0].Name} over {arms[1].Name}",
            "net per round margin",
            Measurement.Paired(
                claim.Player(arms[0].Name).NetPerRoundByGame, claim.Player(arms[1].Name).NetPerRoundByGame),
            string.Empty));

        // 🔥 The two numbers nothing reported before this packet: how big the branch P28 decided
        // actually is, and how long a round runs now that §5.1 can make a cover count fall.
        foreach (var (scope, cell, command) in new[]
        {
            ("ladder", tournament.FreeForAll, tournamentCommand),
            ("difficulty", difficulty.FreeForAll, difficultyCommand),
            ("claim-permission", claim, claimCommand)
        })
        {
            measurements.Add(new SuiteMeasurement(
                $"play.turns-per-round.{scope}",
                "How long a round runs at this table. ⚠️ Never published before P29, and it has "
                + "to be now: RULES.md §5.1 takes the just-taken card out of the discard choice, "
                + "so a seat's cover count can fall and a table of bots is no longer guaranteed "
                + "to converge by construction.",
                command,
                scope,
                "turns a round",
                new Measurement(cell.Rounds, cell.TurnsPerRound, 0),
                string.Empty));

            measurements.Add(new SuiteMeasurement(
                $"play.abandoned.{scope}",
                "Share of games given up at the turn cap. This is the number that would show a "
                + "table failing to converge, and a value above zero is a result rather than a "
                + "nuisance (BUILD-PLAN P29).",
                command,
                scope,
                "abandoned rate",
                new Measurement(cell.Games, cell.AbandonedRate, 0),
                cell.Abandoned == 0 ? "every game settled" : "SOME GAMES DID NOT SETTLE"));

            measurements.Add(new SuiteMeasurement(
                $"claim.refusal-rate.{scope}",
                "Of the claims the opener made, how many the seat before it vetoed (RULES.md "
                + "§4.5). ⚠️ The size of the branch P28's every-rung-refuses decision governs.",
                command,
                scope,
                "refusal rate",
                new Measurement(cell.ClaimAttempts, cell.RefusalRate, 0),
                string.Empty));

            measurements.Add(new SuiteMeasurement(
                $"bonus.jokerless-rate.{scope}",
                "Share of settled rounds won with no joker in the declared thirteen, which is "
                + "what RULES.md §7.3 pays ×2 at two, three or four seats and ×3 at five or "
                + "more. ⚠️ **A floor and not an estimate**: no rung in this field knows the "
                + "bonus exists — CoverScore.Potential returns int.MaxValue for a joker, so "
                + "every one of them holds a joker over everything — and §9 #33 is open on "
                + "whether a joker may be shed before the declaring discard, which P33 defaults "
                + "to no. A rung that played for the bonus would be a new rung under P15's "
                + "discipline and would be measured as one.",
                command,
                scope,
                "jokerless rate",
                new Measurement(cell.Rounds, cell.JokerlessRate, 0),
                string.Empty));

            measurements.Add(new SuiteMeasurement(
                $"bonus.deal-rate.{scope}",
                "Share of settled rounds won on the **initial deal** — the thirteen dealt "
                + "already won and nobody drew a card — which RULES.md §7.4 pays a further ×2 "
                + "for on top of §7.3. ⚠️ **Expected to be zero or near it, and published for "
                + "that reason**: §7.4 is the one rule packet P35 built that a "
                + "one-round-per-game harness can observe at all, so this is the row that would "
                + "explain a money figure moving under it. §7.5, the feeding blame, needs three "
                + "consecutive wins and cannot occur here — see docs/STRATEGY.md §11. ⚠️ It "
                + "counts §9 #38's recorded default, the dealt thirteen alone; under the "
                + "competing reading — the winner's first turn — it would be a commoner event "
                + "entirely.",
                command,
                scope,
                "deal-bonus rate",
                new Measurement(cell.Rounds, cell.DealWinRate, 0),
                string.Empty));

            measurements.Add(new SuiteMeasurement(
                $"claim.attempt-rate.{scope}",
                "Share of rounds in which the opener asked for the turned-up money card at all "
                + "— the denominator the refusal rate is out of.",
                command,
                scope,
                "claim rate",
                new Measurement(cell.Rounds, cell.ClaimRate, 0),
                string.Empty));

            // 🔥 P31's mechanism variable, and its denominator. Measured at the crossed table
            // only, which is the one cell with the whole field sitting down together — see
            // Tournament.Measure. The claim-permission cell reports zeroes here on purpose: it
            // is two arms of one rung and was never asked for the counterfactual.
            measurements.Add(new SuiteMeasurement(
                $"feeding.lock-live.{scope}",
                "Share of the discards chosen in which the feeding ban had taken a held card out "
                + "of the choice (RULES.md §5.1) — how often a lock was live. ⚠️ Not yet the rule "
                + "doing anything: a rank closed off a card the seat was never going to throw "
                + "costs it nothing.",
                command,
                scope,
                "restricted rate",
                new Measurement((int)cell.DiscardsChosen, cell.RestrictedRate, 0),
                string.Empty));

            measurements.Add(new SuiteMeasurement(
                $"feeding.lock-bit.{scope}",
                "Of those restricted discards, the share in which the ban changed the seat's "
                + "answer — the card it would have thrown over its whole hand is not the card it "
                + "threw over its legal set. 🔥 This is what separates 'the rule did nothing' "
                + "from 'the rung did nothing' (BUILD-PLAN P31).",
                command,
                scope,
                "lock bite rate",
                new Measurement((int)cell.RestrictedTurns, cell.LockBiteRate, 0),
                string.Empty));
        }

        clock.Stop();

        return new SuiteReport(options, measurements, tournament, difficulty, money, claim, clock.Elapsed);
    }

    /// <summary>
    /// What ε a level carries, out of the one list there is.
    /// </summary>
    /// <remarks>
    /// A calibration probe answers too, because a sweep is a legitimate thing to run the suite
    /// over — and a name that is neither is not a level, so it carries no rate.
    /// </remarks>
    private static double Rate(string level) => DifficultyLadder.FindOrProbe(level)?.MistakeRate ?? 0;

    /// <summary>
    /// The table sizes P12's headline is published at: <b>four, and the default</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Four is named rather than derived</b>, and deliberately not
    /// <c>RoundEngine.MinimumPlayers</c>: what is being kept is the continuity of one published
    /// series, so the number is the one that series was measured at, and it would survive
    /// somebody deciding a round may be played three-handed. If the default table ever becomes
    /// four again this is one row and not two.
    /// </remarks>
    private static IReadOnlyList<int> HeadlineSeats(SuiteOptions options) =>
        options.Seats == 4 ? [4] : [4, options.Seats];

    /// <summary>P12's headline run, at one table size, under one seating plan or the other.</summary>
    private static (int Games, IReadOnlyList<StrategySeries> Series) Headline(
        SuiteOptions options, int seats, bool balanced)
    {
        IReadOnlyList<Strategy> pair =
        [
            StrategyCatalog.Resolve("greedy"),
            StrategyCatalog.Resolve("simple")
        ];

        var assignments = balanced ? SeatingPlan.Balanced(pair, seats) : null;
        var passes = assignments?.Count ?? pair.Count;
        var games = (options.GamesPerCell + passes - 1) / passes * passes;

        var report = Simulator.Run(new SimulationOptions
        {
            Strategies = pair,
            Assignments = assignments,
            Seats = seats,
            Games = games,
            RoundsPerGame = 1,
            MasterSeed = options.MasterSeed,
            Stakes = options.Stakes,
            TurnCap = options.TurnCap,
            Parallel = options.Parallel
        }.Validated());

        return (games, StrategySeries.Of(report));
    }
}

/// <summary>The suite as a file (BUILD-PLAN §3.12: a number carries the command that made it).</summary>
public static class SuiteCsv
{
    /// <summary>Every measurement, one to a row.</summary>
    public static IEnumerable<string> Rows(SuiteReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        yield return "id,subject,metric,games,mean,error,interval,verdict,seed,seats,question,command";

        foreach (var measurement in report.Measurements)
        {
            // ⚠️ An error of "0.000000" reads as a zero-width interval, which is a much
            // stronger claim than "this statistic does not carry one". A mean margin over the
            // field and a standard-error ratio are both derived from figures that have
            // intervals and have none of their own, so their two columns are left empty.
            var error = measurement.Value.StandardError > 0 ? Number(measurement.Value.StandardError) : string.Empty;
            var interval = measurement.Value.StandardError > 0 ? Number(measurement.Value.Interval) : string.Empty;

            yield return string.Create(CultureInfo.InvariantCulture,
                $"{Quote(measurement.Id)},{Quote(measurement.Subject)},{measurement.Metric},"
                + $"{measurement.Value.Count},{Number(measurement.Value.Mean)},"
                + $"{error},{interval},"
                + $"{Quote(measurement.Verdict)},{report.Options.MasterSeed},{report.Options.Seats},"
                + $"{Quote(measurement.Question)},{Quote(measurement.Command)}");
        }
    }

    /// <summary>Writes them, making the directory if it is not there.</summary>
    public static void WriteTo(string path, SuiteReport report)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, Rows(report));
    }

    private static string Quote(string text) =>
        text.Contains(',', StringComparison.Ordinal) || text.Contains('"', StringComparison.Ordinal)
            ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : text;

    private static string Number(double value) =>
        double.IsFinite(value)
            ? value.ToString("0.000000", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}
