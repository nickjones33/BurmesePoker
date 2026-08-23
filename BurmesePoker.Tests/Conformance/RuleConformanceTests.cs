using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Conformance;

/// <summary>
/// ✅ <b>P30.2 acceptance 1: hundreds of ordinary rounds, dealt at random and played by the
/// rungs a person actually meets, break no Settled rule — and the audit is shown to be
/// non-vacuous.</b>
/// </summary>
/// <remarks>
/// <para>
/// The field is every rung in <see cref="BotCatalog"/> and every level of
/// <see cref="DifficultyLadder"/>, seated in rotation so that neighbours differ every game and
/// no table is ever homogeneous — <c>random</c> plays here too, because a 196-turn round with
/// reshuffles in it is exactly the round the scripted fixtures never deal.
/// </para>
/// <para>
/// ⚠️ <b>The mutant half is what makes the first half evidence</b> (P13.6: a test that a
/// stood-up seat refuses an answer was vacuous until a question stood in front of it). One
/// mutant per rule family: each shows the corresponding family of checks going red against a
/// deliberately broken narration, table or settlement.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class RuleConformanceTests
{
    /// <summary>
    /// One agent-maker per catalog rung and per difficulty level — the players people meet.
    /// </summary>
    private static readonly IReadOnlyList<Func<int, IPlayerAgent>> Field =
    [
        .. BotCatalog.All.Select(rung => rung.Create),
        .. DifficultyLadder.All.Select(level => (Func<int, IPlayerAgent>)level.Create)
    ];

    [Theory]
    [InlineData(4, 60)]
    [InlineData(5, 60)]
    [InlineData(6, 60)]
    public void OrdinaryRoundsBreakNoSettledRule(int seats, int games)
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, seats).Select(id => new PlayerId(id))];

        for (var game = 0; game < games; game++)
        {
            // Consecutive field entries differ, so every table mixes rungs; the seed is plain
            // arithmetic so the run is the same on every machine (BUILD-PLAN §3.7).
            var seed = seats * 100_000 + game;
            var agents = players.ToDictionary(
                player => player,
                player => Field[(game + player.Value) % Field.Count](seed + player.Value));

            var audit = new RuleConformance();
            var engine = RoundEngine.Shuffled(
                players, agents, Stakes.Standard, new Random(seed), observer: audit);

            audit.Watch(engine.Table);
            engine.Play();
            audit.RoundIsSettled();
        }
    }

    /// <summary>
    /// ✅ <b>§7.5, the first Settled rule that cannot be audited from one round</b> — so this is
    /// the first conformance case in the project that watches a <b>sequence</b> of them
    /// (packet P35, build item 5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The streak is counted here, by the test, and never read off the engine.</b> A round
    /// is played, the winner is written down, and the count the next round is handed comes from
    /// this loop's own arithmetic — so the audit is checking the rule rather than mirroring
    /// <c>MatchEngine</c>. The seat blamed is likewise re-derived inside
    /// <see cref="RuleConformance"/> from that round's seating (§9 #46).
    /// </para>
    /// <para>
    /// ⚠️ <b>The non-vacuity assertion is the point of the count.</b> Ordinary bots produce
    /// streaks by luck, so a run that happened to contain none would pass this test having
    /// checked nothing at all — which is exactly the failure mode P30.2's mutants exist to
    /// prevent. The seed is fixed, so the number below is a property of the run and not a hope.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void AStreakOfWinsBreaksNoSettledRuleAndIsBilledToTheSeatAbove(int seats)
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, seats).Select(id => new PlayerId(id))];
        var random = new Random(seats * 1_000 + 22);
        var agents = players.ToDictionary(
            player => player,
            player => Field[player.Value % Field.Count](seats * 7 + player.Value));

        PlayerId? last = null;
        var inARow = 0;
        var streaks = 0;

        for (var round = 1; round <= 120; round++)
        {
            var audit = new RuleConformance { Streak = (last, inARow) };

            // A held seating (§3 step 2, rev 28) — which is what makes "the seat above you" the
            // same person for a whole streak, and §7.5 coherent at all.
            var engine = RoundEngine.Shuffled(
                players, agents, Stakes.Standard, random, round, audit,
                streak: new WinStreak(last, inARow));

            audit.Watch(engine.Table);
            var result = engine.Play();
            audit.RoundIsSettled();

            if (result.Win.ThirdConsecutiveWin)
            {
                streaks++;

                // The seat above the winner carried the whole round, and nobody else paid a
                // penny towards it — asserted here as well as inside the audit, because this is
                // the sentence the rule is written in.
                var above = players[(players.ToList().IndexOf(result.Winner) + seats - 1) % seats];
                var rounds = Settlement.RoundPayments(
                    players, result.Winner, Stakes.Standard, result.Win);

                Assert.True(rounds[above] < 0);
                Assert.Equal(-rounds[above], rounds[result.Winner]);
                Assert.All(
                    players.Where(player => player != above && player != result.Winner),
                    player => Assert.Equal(0, rounds[player]));
            }

            inARow = last == result.Winner ? inARow + 1 : 1;
            last = result.Winner;
        }

        Assert.True(
            streaks > 0,
            $"120 rounds at {seats} seats produced no third consecutive win, so §7.5 was never "
            + "exercised and this test checked nothing (RULES.md §7.5).");
    }

    /// <summary>
    /// ✅ <b>§3 step 2, as rev 28 corrects it: a seating is drawn once and held</b> (§10 #22,
    /// packet P36) — and it can still be changed, which is the half that keeps it from being a
    /// revert to the pre-P28 engine.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>This check asserted the opposite until P36, and said so out loud while it did.</b>
    /// It is here rather than in <see cref="RuleConformance"/> because a seating is a property of
    /// a match and the audit watches a round: what an ordinary round can be asked is that it was
    /// dealt in the order it was given, which every check in the audit already assumes.
    /// </remarks>
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void TheSeatingIsDrawnOnceAndHeldUnlessThePolicySaysOtherwise(int seats)
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, seats).Select(id => new PlayerId(id))];

        Assert.All(Dealt(players, SeatingPolicy.Default), seating => Assert.Equal(players, seating));

        // …and the mechanism is not merely absent: asked to, the same match re-draws, so the
        // check above is a statement about the rule rather than about a missing feature.
        var moved = Dealt(players, SeatingPolicy.EveryRound);

        Assert.Equal(players, moved[0]);
        Assert.True(
            moved.Select(seating => string.Join(",", seating)).Distinct().Count() > 1,
            "A table asked to re-seat every round dealt eight rounds to one order (RULES.md §3 step 2).");

        // Every deal seats exactly the members, whichever policy is in force: a draw is a
        // permutation and nothing else.
        Assert.All(
            (IEnumerable<IReadOnlyList<PlayerId>>)[.. Dealt(players, SeatingPolicy.Default), .. moved],
            seating => Assert.Equal(
                players.OrderBy(player => player.Value), seating.OrderBy(player => player.Value)));
    }

    /// <summary>
    /// ✅ <b>§3 step 2's other half: a held seating is re-drawn when the players agree to it</b>
    /// (§9 #45, §10 #23, packet P37) — <b>and only then</b>.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Both halves, because either alone is a different rule.</b> A table that never moved
    /// its seats would be more rigid than the game is, and one that moved them whenever anybody
    /// consented would move them every deal — the check above is <em>held</em> and this one is
    /// <em>changed only by agreement</em>. ⚠️ <b>The bots are what make this an ordinary-play
    /// check</b>: they consent, every round, and the seating still stands until a seat asks.
    /// </remarks>
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void TheSeatingChangesWhenTheTableAgreesAndNotOtherwise(int seats)
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, seats).Select(id => new PlayerId(id))];
        var asked = false;

        var observer = new RecordingObserver();

        var match = new MatchEngine(
            players,
            players.ToDictionary(
                player => player,
                IPlayerAgent (player) => player == players[0]
                    // Asks once, in the gap before the third round, and consents every other time.
                    ? new AsksWhenTold(() => asked)
                    : new GreedyBotAgent()),
            Stakes.Standard,
            new Random(20260822),
            observer);

        for (var round = 0; round < 4; round++)
        {
            asked = round == 2;
            match.PlayRound();
        }

        // Consent everywhere until the asking, and the asking moves them exactly once.
        Assert.Equal(players, observer.Seatings[0]);
        Assert.Equal(players, observer.Seatings[1]);
        Assert.NotEqual(players, observer.Seatings[2]);
        Assert.Equal(observer.Seatings[2], observer.Seatings[3]);

        // Everybody is still at the table: an agreement is a permutation and nothing else.
        Assert.All(observer.Seatings, seating => Assert.Equal(
            players.OrderBy(player => player.Value), seating.OrderBy(player => player.Value)));

        Assert.Equal([$"{players[0]} asked"], observer.SeatingChanges);
    }

    /// <summary>A seat that asks for a change of seats when it is told to, and consents otherwise.</summary>
    private sealed class AsksWhenTold(Func<bool> asking) : IPlayerAgent
    {
        private readonly GreedyBotAgent _plays = new();

        public TurnAction ChooseAction(TurnContext context) => _plays.ChooseAction(context);

        public Card ChooseDiscard(TurnContext context) => _plays.ChooseDiscard(context);

        public bool ClaimTurnedUpMoneyCard(TurnContext context) => _plays.ClaimTurnedUpMoneyCard(context);

        public bool ObjectToClaim(TurnContext context) => _plays.ObjectToClaim(context);

        public bool Declare(TurnContext context) => _plays.Declare(context);

        public SeatingOpinion AskAboutTheSeating(SeatingQuestion question) =>
            asking() ? SeatingOpinion.Ask : SeatingOpinion.Consent;
    }

    /// <summary>Eight rounds' seatings, in order, under one policy.</summary>
    private static IReadOnlyList<IReadOnlyList<PlayerId>> Dealt(
        IReadOnlyList<PlayerId> players, SeatingPolicy policy)
    {
        var observer = new RecordingObserver();

        var match = new MatchEngine(
            players,
            players.ToDictionary(player => player, IPlayerAgent (_) => new GreedyBotAgent()),
            Stakes.Standard,
            new Random(20260821),
            observer,
            policy);

        for (var round = 0; round < 8; round++)
        {
            match.PlayRound();
        }

        Assert.Equal(8, observer.Seatings.Count);

        return observer.Seatings;
    }

    // ---- The mutants: one per rule family, each proving its checks can go red. ----

    /// <summary>Turn structure (§5): an engine that let a seat take twice would be caught.</summary>
    [Fact]
    public void MutantASecondTakeInOneTurnIsCaught()
    {
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => PlayMutated(
            forward => new DoubleTake(forward)));
    }

    /// <summary>The claim (§4.5): a claim narrated after the opening turn is caught.</summary>
    [Fact]
    public void MutantALateClaimIsCaught()
    {
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => PlayMutated(
            forward => new LateClaim(forward)));
    }

    /// <summary>
    /// The feeding ban (§5.1): an engine that let a seat feed a rank its neighbour had taken in
    /// the open — no floor, no declaration — is caught by the audit's own mirror of the ban.
    /// </summary>
    [Fact]
    public void MutantAFedClosedRankIsCaught()
    {
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => PlayMutated(
            forward => new FeedsAClosedRank(forward)));
    }

    /// <summary>Conservation (§2): a card leaving the round entirely is caught.</summary>
    [Fact]
    public void MutantAVanishedCardIsCaught()
    {
        var audit = new RuleConformance();
        var engine = Dealt(audit);
        audit.Watch(engine.Table);

        engine.Table.DrawPile.DrawFromTop();

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(audit.AuditNow);
    }

    /// <summary>Ownership (§4.4): ownership appearing outside a deal or a blind draw is caught.</summary>
    [Fact]
    public void MutantAConjuredOwnershipIsCaught()
    {
        var audit = new RuleConformance();
        var engine = Dealt(audit);
        audit.Watch(engine.Table);

        var unowned = engine.Table.DrawPile.Cards[0];
        engine.Table.Ownership.RecordFromDeck(unowned.Id, engine.Table.Players[0]);

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(audit.AuditNow);
    }

    /// <summary>
    /// The declaration (§6, §7.1.1): each way a laid-down hand can be illegal goes red — a
    /// duplicate suit in a set, an ace wrapped in a run, a card used twice, a cover short of
    /// thirteen, and a partition without the clean series the table size requires.
    /// </summary>
    [Fact]
    public void MutantAnIllegalDeclarationIsCaughtEachWayItCanBeIllegal()
    {
        var fourHanded = TableRules.For(4);
        var fiveHanded = TableRules.For(5);

        // 9♥ 9♥ 9♠ — the §6.2 example itself.
        var duplicateSuit = Declared(
            Set("9H", "9H", "9S"), Run("2H", "3H", "4H", "5H", "6H", "7H", "8H"), Set("KC", "KH", "KS"));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.ADeclaredHandSatisfiesTheTable(duplicateSuit.Melds, duplicateSuit.Held, fiveHanded));

        // K-A-2 — the wrap §6.1 forbids.
        var wrapped = Declared(
            Run("KD", "AD", "2D"), Run("4H", "5H", "6H", "7H", "8H", "9H", "10H"), Set("QC", "QH", "QS"));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.ADeclaredHandSatisfiesTheTable(wrapped.Melds, wrapped.Held, fiveHanded));

        // Q-K-A and A-2-3 are both legal, which is what makes the wrap above the defect.
        var aces = Declared(
            Run("QD", "KD", "AD"), Run("AH", "2H", "3H"), Run("5S", "6S", "7S", "8S", "9S", "10S", "JS"));
        RuleConformance.ADeclaredHandSatisfiesTheTable(aces.Melds, aces.Held, fiveHanded);

        // Sets alone cover thirteen at five seats and are caught at four, where §7.1.1
        // requires a clean series.
        var setsOnly = Declared(
            Set("KC", "KH", "KS"), Set("QC", "QH", "QS"), Set("5C", "5H", "5S"), Set("9C", "9H", "9S", "9D"));
        RuleConformance.ADeclaredHandSatisfiesTheTable(setsOnly.Melds, setsOnly.Held, fiveHanded);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.ADeclaredHandSatisfiesTheTable(setsOnly.Melds, setsOnly.Held, fourHanded));

        // Twelve cards are not a declaration however legal each meld is.
        var twelve = Declared(
            Set("KC", "KH", "KS"), Set("QC", "QH", "QS"), Set("5C", "5H", "5S"), Set("9C", "9H", "9S"));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.ADeclaredHandSatisfiesTheTable(twelve.Melds, twelve.Held, fiveHanded));

        // One card in two melds — the overlap §6.3's exact-cover forbids.
        var honest = Declared(
            Run("2H", "3H", "4H", "5H", "6H", "7H", "8H"), Set("KC", "KH", "KS"), Set("QC", "QH", "QS"));
        var overlapping = new List<Meld>(honest.Melds)
        {
            [2] = new Meld(
                MeldKind.Set,
                honest.Melds[2].Slots.Take(2).Append(honest.Melds[1].Slots[0]))
        };
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.ADeclaredHandSatisfiesTheTable(overlapping, honest.Held, fiveHanded));
    }

    /// <summary>
    /// The settlement (§4.3, §7.2): a deadwood-style penalty — one loser paying more than the
    /// round value — is caught even though it still sums to zero.
    /// </summary>
    [Fact]
    public void MutantADeadwoodPenaltyIsCaught()
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(id => new PlayerId(id))];
        var honest = new Dictionary<PlayerId, int>
        {
            [players[0]] = 15, [players[1]] = -5, [players[2]] = -5, [players[3]] = -5
        };
        var deadwood = new Dictionary<PlayerId, int>
        {
            [players[0]] = 17, [players[1]] = -5, [players[2]] = -5, [players[3]] = -7
        };

        // The declaration is jokered, so §7.3 pays nothing and the round payment is the flat
        // $5 these numbers were computed from.
        var jokered = WithAJoker();

        RuleConformance.TheSettlementIsTheRules(
            honest, players[0], players, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                deadwood, players[0], players, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered));
    }

    /// <summary>
    /// The clean bonus (§7.3): a settlement that pays a jokerless winner flat, or pays a
    /// jokered one the bonus, is caught — and the multiplier is checked against the seat count,
    /// not assumed.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>This is what converts §7.3's registry entry from <c>Exempt</c> to
    /// <c>Checked</c>.</b> The exemption said a check written before the code existed would
    /// fail every round somebody declared jokerless; the code exists now, so the check is the
    /// answer rather than the alarm.
    /// </remarks>
    [Fact]
    public void MutantTheCleanBonusIsCaughtBothWaysRoundAndBothWaysWrong()
    {
        var four = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(id => new PlayerId(id))];
        var five = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 5).Select(id => new PlayerId(id))];

        var clean = Declared(
            Run("2H", "3H", "4H", "5H"), Run("7D", "8D", "9D"),
            Run("4C", "5C", "6C"), Set("QS", "QD", "QC")).Melds;
        var jokered = WithAJoker();

        // Four-handed: ×2, so $10 a loser. Five-handed: ×3, so $15 a loser.
        RuleConformance.TheSettlementIsTheRules(
            Nets(four, 30), four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), clean);
        RuleConformance.TheSettlementIsTheRules(
            Nets(five, 60), five[0], five, Stakes.Standard, TurnUp(), Owned(), Shoe(), clean);

        // Paid flat although the hand was jokerless — the bug this rule was unbuilt as.
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                Nets(four, 15), four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), clean));

        // Paid the bonus although a joker is in the thirteen.
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                Nets(four, 30), four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered));

        // ⚠️ And the seam is checked, not assumed: five seats paid at the four-seat multiplier.
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                Nets(five, 40), five[0], five, Stakes.Standard, TurnUp(), Owned(), Shoe(), clean));
    }

    /// <summary>
    /// The deal bonus (§7.4): a settlement that pays a win from the initial deal flat, or pays
    /// the bonus to a winner who played a turn for it, is caught.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Half of what converts §7.4's registry entry from <c>Exempt</c> to <c>Checked</c>.</b>
    /// The exemption said there was nothing for an ordinary-play check to re-derive, because the
    /// engine had no path that could offer a declaration before the first turn. It has one now.
    /// </remarks>
    [Fact]
    public void MutantTheDealBonusIsCaughtBothWaysRound()
    {
        var four = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(id => new PlayerId(id))];
        var jokered = WithAJoker();

        // Jokered, four-handed, won on the deal: $5 × 2 = $10 a loser.
        RuleConformance.TheSettlementIsTheRules(
            Nets(four, 30), four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered,
            fromTheInitialDeal: true);

        // Paid flat although the round ended before anybody drew.
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                Nets(four, 15), four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered,
                fromTheInitialDeal: true));

        // Paid the bonus although the round was played out.
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                Nets(four, 30), four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered));

        // ⚠️ And the two bonuses multiply rather than replacing one another (§9 #39): a
        // jokerless win on the deal at four seats is ×2 × ×2 = $20 a loser, and $10 is wrong.
        var clean = Declared(
            Run("2H", "3H", "4H", "5H"), Run("7D", "8D", "9D"),
            Run("4C", "5C", "6C"), Set("QS", "QD", "QC")).Melds;

        RuleConformance.TheSettlementIsTheRules(
            Nets(four, 60), four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), clean,
            fromTheInitialDeal: true);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                Nets(four, 30), four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), clean,
                fromTheInitialDeal: true));
    }

    /// <summary>
    /// The feeding blame (§7.5): a settlement that spreads a third consecutive win's payment
    /// over the whole table, bills the wrong seat, or bills the right seat the wrong amount, is
    /// caught.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Half of what converts §7.5's registry entry from <c>Exempt</c> to <c>Checked</c>.</b>
    /// ⚠️ <b>The other half is the multi-round case</b>
    /// (<see cref="AStreakOfWinsBreaksNoSettledRuleAndIsBilledToTheSeatAbove"/>): this mutant
    /// shows the check can go red, and that one shows it is applied to a real sequence of rounds.
    /// </remarks>
    [Fact]
    public void MutantTheFeedingBlameIsCaughtWhoeverIsBilled()
    {
        var four = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(id => new PlayerId(id))];
        var jokered = WithAJoker();

        // Seat 0 won its third in a row: seat 3 — the seat above it — owes the whole $15.
        var billed = new Dictionary<PlayerId, int>
        {
            [four[0]] = 15, [four[1]] = 0, [four[2]] = 0, [four[3]] = -15
        };

        RuleConformance.TheSettlementIsTheRules(
            billed, four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered,
            blamed: four[3]);

        // Spread over the table, which is what an unbuilt §7.5 pays.
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                Nets(four, 15), four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered,
                blamed: four[3]));

        // Billed to the seat BELOW the winner — the one it feeds rather than the one that feeds
        // it. ⚠️ Three rules name this edge of the table and all three name the same side (§5.1,
        // §4.5, §7.5), so getting it backwards is the plausible mistake.
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                billed, four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered,
                blamed: four[1]));

        // Billed one loser's share rather than the winner's whole payment.
        var short_ = new Dictionary<PlayerId, int>
        {
            [four[0]] = 5, [four[1]] = 0, [four[2]] = 0, [four[3]] = -5
        };

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            RuleConformance.TheSettlementIsTheRules(
                short_, four[0], four, Stakes.Standard, TurnUp(), Owned(), Shoe(), jokered,
                blamed: four[3]));
    }

    // ---- Plumbing. ----

    /// <summary>A dealt table that has played nothing, for the state-mutation mutants.</summary>
    private static RoundEngine Dealt(RuleConformance audit)
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(id => new PlayerId(id))];

        return RoundEngine.Shuffled(
            players,
            players.ToDictionary(player => player, IPlayerAgent (_) => new GreedyBotAgent()),
            Stakes.Standard,
            new Random(4242),
            observer: audit);
    }

    /// <summary>
    /// Plays one ordinary greedy round with a lying narrator between the engine and the audit.
    /// The table is the real one; the narration is the mutant — an engine that did the
    /// forbidden thing would narrate exactly this.
    /// </summary>
    private static void PlayMutated(Func<IGameObserver, IGameObserver> mutate)
    {
        var players = (IReadOnlyList<PlayerId>)[.. Enumerable.Range(0, 4).Select(id => new PlayerId(id))];
        var audit = new RuleConformance();

        var engine = RoundEngine.Shuffled(
            players,
            players.ToDictionary(player => player, IPlayerAgent (_) => new GreedyBotAgent()),
            Stakes.Standard,
            new Random(4242),
            observer: mutate(audit));

        audit.Watch(engine.Table);
        engine.Play();
        audit.RoundIsSettled();
    }

    private static (MeldKind Kind, string[] Codes) Run(params string[] codes) => (MeldKind.Run, codes);

    private static (MeldKind Kind, string[] Codes) Set(params string[] codes) => (MeldKind.Set, codes);

    /// <summary>
    /// A hand laid down as the given melds, every card with its own <see cref="CardId"/> across
    /// the whole hand — two copies of one value stay two cards, as in a real shoe.
    /// </summary>
    private static (IReadOnlyList<Meld> Melds, IReadOnlyList<Card> Held) Declared(
        params (MeldKind Kind, string[] Codes)[] specs)
    {
        var held = Hands.Of([.. specs.SelectMany(spec => spec.Codes)]);
        var melds = new List<Meld>();
        var at = 0;

        foreach (var (kind, codes) in specs)
        {
            var cards = held.Skip(at).Take(codes.Length).ToList();
            at += codes.Length;
            melds.Add(new Meld(
                kind, cards.Select(card => new MeldSlot(card, card.Rank!.Value, card.Suit!.Value))));
        }

        return (melds, held);
    }

    private static IReadOnlyList<Card> TurnUp() => [Hands.Value("3C"), Hands.Value("4C")];

    /// <summary>Seat 0 collects <paramref name="take"/>; everybody else pays an equal share.</summary>
    private static Dictionary<PlayerId, int> Nets(IReadOnlyList<PlayerId> players, int take) =>
        players.ToDictionary(
            player => player,
            player => player == players[0] ? take : -take / (players.Count - 1));

    /// <summary>
    /// A declared thirteen with a joker in one of its <b>sets</b> — the case rev 25's withdrawn
    /// "all series clean" reading would have paid and rev 26 does not. Built by hand because
    /// <see cref="Declared"/> reads a rank off every card and a joker has none.
    /// </summary>
    private static IReadOnlyList<Meld> WithAJoker()
    {
        var held = Hands.Of(
            "2H", "3H", "4H", "5H", "7D", "8D", "9D", "4C", "5C", "6C", "QS", "QD", "RJ");

        Meld Ranked(MeldKind kind, IEnumerable<Card> cards) => new(
            kind, cards.Select(card => new MeldSlot(card, card.Rank!.Value, card.Suit!.Value)));

        return
        [
            Ranked(MeldKind.Run, held.Take(4)),
            Ranked(MeldKind.Run, held.Skip(4).Take(3)),
            Ranked(MeldKind.Run, held.Skip(7).Take(3)),
            new Meld(MeldKind.Set,
            [
                new MeldSlot(held[10], Rank.Queen, Suit.Spades),
                new MeldSlot(held[11], Rank.Queen, Suit.Diamonds),
                new MeldSlot(held[12], Rank.Queen, Suit.Clubs)
            ])
        ];
    }

    private static Dictionary<CardId, PlayerId> Owned() => [];

    private static IReadOnlyList<Card> Shoe() => DeckBuilder.BuildTwoDecks();

    /// <summary>Forwards everything, and narrates the first blind draw twice.</summary>
    private sealed class DoubleTake(IGameObserver audit) : IGameObserver
    {
        private bool _lied;

        public void RoundStarted(int round, IReadOnlyList<PlayerId> seating, IReadOnlyList<Card> turnedUp) =>
            audit.RoundStarted(round, seating, turnedUp);

        public void PlayerDrew(PlayerId player, Card card)
        {
            audit.PlayerDrew(player, card);

            if (!_lied)
            {
                _lied = true;
                audit.PlayerDrew(player, card);
            }
        }

        public void PlayerTookDiscard(PlayerId player, Card card) => audit.PlayerTookDiscard(player, card);
        public void MoneyCardClaimed(PlayerId player, Card card) => audit.MoneyCardClaimed(player, card);
        public void ClaimRefused(PlayerId objector, PlayerId claimant, Card card) => audit.ClaimRefused(objector, claimant, card);
        public void PlayerDiscarded(PlayerId player, Card card) => audit.PlayerDiscarded(player, card);
        public void DiscardsReshuffled(int cards) => audit.DiscardsReshuffled(cards);
        public void PlayerDeclared(PlayerId player, IReadOnlyList<Meld> melds) => audit.PlayerDeclared(player, melds);
        public void RoundSettled(RoundResult result) => audit.RoundSettled(result);
    }

    /// <summary>Forwards everything, and narrates a claim on the second turn's take.</summary>
    private sealed class LateClaim(IGameObserver audit) : IGameObserver
    {
        private int _discards;
        private bool _lied;
        private IReadOnlyList<Card> _turnedUp = [];

        public void RoundStarted(int round, IReadOnlyList<PlayerId> seating, IReadOnlyList<Card> turnedUp)
        {
            _turnedUp = turnedUp;
            audit.RoundStarted(round, seating, turnedUp);
        }

        public void PlayerDrew(PlayerId player, Card card)
        {
            if (Lying())
            {
                // The second turn's take arrives as a claim: an engine offering the
                // turned-up card after the opening turn.
                audit.MoneyCardClaimed(player, _turnedUp[^1]);
                return;
            }

            audit.PlayerDrew(player, card);
        }

        public void PlayerTookDiscard(PlayerId player, Card card)
        {
            if (Lying())
            {
                audit.MoneyCardClaimed(player, _turnedUp[^1]);
                return;
            }

            audit.PlayerTookDiscard(player, card);
        }

        private bool Lying()
        {
            if (_lied || _discards == 0)
            {
                return false;
            }

            _lied = true;
            return true;
        }

        public void MoneyCardClaimed(PlayerId player, Card card) => audit.MoneyCardClaimed(player, card);
        public void ClaimRefused(PlayerId objector, PlayerId claimant, Card card) => audit.ClaimRefused(objector, claimant, card);

        public void PlayerDiscarded(PlayerId player, Card card)
        {
            _discards++;
            audit.PlayerDiscarded(player, card);
        }

        public void DiscardsReshuffled(int cards) => audit.DiscardsReshuffled(cards);
        public void PlayerDeclared(PlayerId player, IReadOnlyList<Meld> melds) => audit.PlayerDeclared(player, melds);
        public void RoundSettled(RoundResult result) => audit.RoundSettled(result);
    }

    /// <summary>
    /// Forwards everything faithfully until a real public take has closed a rank, then narrates
    /// the protected seat's feeder throwing exactly that rank — the gift §5.1 forbids, with no
    /// floor and no declaration behind it.
    /// </summary>
    private sealed class FeedsAClosedRank(IGameObserver audit) : IGameObserver
    {
        private IReadOnlyList<PlayerId> _seating = [];
        private readonly HashSet<(PlayerId, Rank?)> _released = [];
        private PlayerId _taker;
        private Card? _closed;
        private bool _fed;

        public void RoundStarted(int round, IReadOnlyList<PlayerId> seating, IReadOnlyList<Card> turnedUp)
        {
            _seating = seating;
            audit.RoundStarted(round, seating, turnedUp);
        }

        public void PlayerDrew(PlayerId player, Card card) => audit.PlayerDrew(player, card);

        public void PlayerTookDiscard(PlayerId player, Card card)
        {
            // A real take, kept: it closes this rank against the seat that feeds the taker —
            // in the audit's mirror as at a table — unless the taker had already released it.
            if (_closed is null && !_released.Contains((player, card.Rank)))
            {
                _taker = player;
                _closed = card;
            }

            audit.PlayerTookDiscard(player, card);
        }

        public void MoneyCardClaimed(PlayerId player, Card card) => audit.MoneyCardClaimed(player, card);

        public void ClaimRefused(PlayerId objector, PlayerId claimant, Card card) =>
            audit.ClaimRefused(objector, claimant, card);

        public void PlayerDiscarded(PlayerId player, Card card)
        {
            _released.Add((player, card.Rank));

            if (!_fed && _closed is { } closed)
            {
                if (player == _taker && card.Rank == closed.Rank)
                {
                    // The taker threw the rank back before the lie could land; re-arm later.
                    _closed = null;
                }
                else if (FeederOf(_taker) == player)
                {
                    // The lie: this discard arrives as a card of the closed rank.
                    _fed = true;
                    audit.PlayerDiscarded(player, closed);
                    return;
                }
            }

            audit.PlayerDiscarded(player, card);
        }

        public void DiscardsReshuffled(int cards) => audit.DiscardsReshuffled(cards);
        public void PlayerDeclared(PlayerId player, IReadOnlyList<Meld> melds) => audit.PlayerDeclared(player, melds);
        public void RoundSettled(RoundResult result) => audit.RoundSettled(result);

        private PlayerId FeederOf(PlayerId taker)
        {
            for (var seat = 0; seat < _seating.Count; seat++)
            {
                if (_seating[seat] == taker)
                {
                    return _seating[(seat + _seating.Count - 1) % _seating.Count];
                }
            }

            throw new InvalidOperationException($"{taker} is not seated.");
        }
    }
}
