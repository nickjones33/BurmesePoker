# Build Status

Cross-session progress tracker. **`/poker` reads this first and updates it last.**

Plan: `BUILD-PLAN.md` · Rules: `RULES.md` (rev 13) · Skill: `.claude/skills/poker/SKILL.md`

State markers: `☐` not started · `◐` in progress · `☑` done

---

## Current state

**Next packet: P11 or P13 — they are independent, and either may also be dropped.** No blockers.

**P0 through P12 are done. The game is playable alone and measurable in bulk:** `dotnet run
--project BurmesePoker.Console` fills the empty seats with bots and plays rounds with the banks
carrying over, and `dotnet run -c Release --project BurmesePoker.Sim` plays thousands of seeded
games in parallel and reports how two strategies compare. **Of §0's four goals, solo play (P10)
and simulation (P12) are delivered; the console UX pass (P11) and the multiplayer app (P13)
remain.**

The 2023 implementation is gone from the tree and lives only at the `pre-rewrite` tag. The
solution is **four** projects — `BurmesePoker.Domain` (pure rules), `BurmesePoker.Console`
(Spectre.Console 0.57.2, the only project that prints), **`BurmesePoker.Sim`** (batch play,
Domain only) and `BurmesePoker.Tests`. ⚠️ **The test project now references Domain *and* Sim.**
The rule that matters is unchanged in substance and worth restating in its true form: **tests
never reference `BurmesePoker.Console`**, so nothing is tested through the front end. The
harness's determinism is itself an acceptance criterion, so it has to be reachable from a test.

Domain holds `Cards/{Rank,Suit,CardColor,CardText,CardId,Card,Deck,DeckBuilder,
DeckExhaustedException}`, `Melds/{MeldKind,MeldSlot,Meld,RunGenerator,SetGenerator,
MeldCandidates,MeldIndex,HandEvaluator,PartialCover}`,
`Money/{MoneyCardRegistry,CardOwnership,Stakes,Settlement}`,
`Play/{PlayerId,TurnAction,PlayerState,TableState,TurnContext,RoundResult,RoundEngine,
MatchEngine}`, `Abstractions/{IPlayerAgent,IGameObserver}` and
`Agents/{CoverScore,GreedyBotAgent,SimpleBotAgent}`. Console holds `{Program,CardFormatting,
SpectrePlayerAgent,ConsoleObserver}`. Sim holds `{Program,Simulator,GameRunner,
SimulationOptions,SimulationReport,StrategySummary,Strategy,SeedSequence,SeatRecorder,
SimObserver,RoundAbandonedException,Results,CsvReport}`.

✅ **Baseline green** — `dotnet build` clean and warning-free, `dotnet test` **235 passed,
0 failed**. **Any red tree is a real problem.**

⚠️ **One hazard a cold context must know before writing a test or a strategy:** with the
reshuffle built, **a round in which nobody's hand ever improves never ends.** Only a
declaration ends a round (RULES.md §7.1) and the cards now circulate for ever, so a table of
passive agents loops until it is killed. Every round-level test needs a seat that eventually
declares. `GreedyBotAgent` and `SimpleBotAgent` are both safe in practice — neither has ever
failed to finish a round — but **the simulation harness is the only place that is bounded**:
`SimulationOptions.TurnCap` and `SeatRecorder` give up on a stalled round and report it. A
test that plays a round outside the harness has no such protection.

---

## Packets

| | Packet | Depends on | Notes |
|:-:|---|---|---|
| ☑ | **P0** Restructure and salvage | — | done 2026-08-18 |
| ☑ | **P1** Cards, deck, identity | P0 | done 2026-08-18 |
| ☑ | **P2** Money designation and ownership | P1 | done 2026-08-18 — turned-up-joker default taken |
| ☑ | **P3** Run candidate generation | P1 | done 2026-08-18 — spec updated with what it found |
| ☑ | **P4** Set candidate generation | P1 | done 2026-08-18 |
| ☑ | **P5** Exact-cover hand evaluator | P3, P4 | done 2026-08-18 |
| ☑ | **P6** Stakes and settlement | P1, P2 | done 2026-08-18 — settlement takes the *unshuffled* shoe |
| ☑ | **P7** Round and turn engine | P5, P6 | done 2026-08-18 — deals from a draw order, not a shuffle |
| ☑ | **P8** Console front end | P7 | done 2026-08-18 — hotseat; verified by driving a pty |
| ☑ | **P9** End-to-end play | P8 | done 2026-08-18 — the reshuffle lives inside `RoundEngine.TakeCard` |
| ☑ | **P10** Bot opponents — solo play | P9 | done 2026-08-18 — `PartialCover` + `GreedyBotAgent`; every seed terminates |
| ☐ | **P11** Console UX pass | P10 | **candidate** — the terminal *is* the UI (§0); the round log is still the sorest spot |
| ☑ | **P12** Simulation at scale | P10 | done 2026-08-18 — `BurmesePoker.Sim`; the tie-break wins 30.7% to 19.3% |
| ☐ | **P13** Multiplayer app | P10 | **candidate** — XL, split it when it is next |

**P10 was the fork and it is done; P12 has been taken.** P11 and P13 depend on P10 and on
nothing else, so either can be picked up in any order — or dropped. The game is finished as a
game, and the simulation goal is delivered.

⚠️ **Four things the roadmap changed for work that was already planned**, all recorded in
`BUILD-PLAN.md` — **all four are now discharged**:
1. ✅ **P10's stated heuristic was wrong, and the correction shipped.** "Never discard an owned
   money card" contradicts `RULES.md` §4.4 — ownership never transfers, so a money card pays
   you after you throw it. `GreedyBotAgent` consults neither ownership nor the registry
   anywhere, and `MoneyCardsDoNotChangeWhatABotThrowsAway` is the test that says so.
2. ✅ **§3.6 settles that agents stay synchronous**, taken *before* P10 and P13 add callers to
   `IPlayerAgent`. A remote player blocks in the agent; one table is one task. **P10 and P12
   both added callers and neither needed anything** — a bot answers the same four questions a
   person does, and a simulation runs a whole table inside one `Parallel.For` body.
3. ✅ **§3.7's speed question is closed, by P12's measurement pass.** `PartialCover.Best` is
   **140 µs** a hand and `HandEvaluator.TryFindCover` **91 µs**; a round is **~20 ms** and a run
   does **50–90 rounds a second**, so 2,000 rounds took 34 seconds with nothing optimised. The
   one surprise: the work is **allocation-bound, not compute-bound** — eight threads bought 25%
   under the workstation GC and 70% under the server GC.
4. ✅ **§3.8's statistics constraint — delivered by P9.** `MatchEngine.PlayRound` returns a
   `RoundRecord(RoundResult, TableState)`, so how close the losers were and how much of the
   money was the side bet both stay reachable, and `RoundResult.Turns` is carried at the
   source. The engine keeps **no** per-round history: a consumer derives what it wants and
   drops the table. **P10 built the third seam too** — `RecordingAgent`, a decorator over
   `IPlayerAgent`, in the test project.

---

## Notes for the next session

*Anything a cold context would need: decisions taken, surprises, deliberate leftovers.*

**From P12 (most recent):**

- **`BurmesePoker.Sim` is a fourth project and `BurmesePoker.Tests` now references it.** The
  rule to carry forward is **"tests never reference `BurmesePoker.Console`"**, which is what
  the Domain-only rule was always protecting. A fifth project existing only to test the fourth
  would have bought nothing, and the harness's determinism is an acceptance criterion.
- **The command line:** `dotnet run -c Release --project BurmesePoker.Sim -- --games 2000
  --strategies greedy,simple --seats 4 --csv out.csv`, plus `--rounds`, `--seed`, `--turn-cap`,
  `--serial`, `--threads`. There is also a **`bench`** verb that times the two searches over
  random hands. **Run it in Release** — Debug is roughly three times slower and says nothing
  useful about throughput.
- **The headline result: the tie-break is worth 1.6× the wins.** `SimpleBotAgent` is
  `GreedyBotAgent` with the discard tie-break removed and *nothing else* changed. Over **2,000
  four-seat rounds**: greedy **30.7%** of rounds and **+$1.24** a round, simple **19.3%** and
  **−$1.24**. P10's claim that the tie-break is what makes progress is now measured.
- ⚠️ **Seats are not equivalent, and a comparison has to rotate them.** Seat 0 opens every
  round and is the only seat ever offered the turned-up money card (RULES.md §4.5), because
  `RoundEngine.Play` starts turn 1 at `players[0]` every round. `SimulationOptions.Seating`
  puts strategy *(seat + game) mod k* in each seat. **A future harness that seats strategies
  any other way must still rotate them**, or it will be measuring the seat.
- **Determinism is per game, never per run.** `SeedSequence.GameSeed(master, index)` is
  SplitMix64's finaliser over the two packed into one word, so game 417 is the same game
  however the run was scheduled. Serial, parallel and two-thread runs give **byte-identical
  CSV**, which is the acceptance test the packet stands on. Never draw per-game seeds from one
  shared `Random`.
- **The turn cap lives in the agent, and has never fired in a real run.** `SeatRecorder` throws
  `RoundAbandonedException` past `SimulationOptions.TurnCap` (400 by default) and the game
  stops there and is *reported*, not dropped. Even a table of four `SimpleBotAgent`s — which
  has no tie-break to push it off a plateau — finished all 300 rounds tried, averaging 28.3
  turns. The only thing that has ever tripped the cap is `StallingAgent` in the test.
- ⚠️ **The reshuffle is a six-player phenomenon.** Over 300 bot rounds each: **0 reshuffles at
  four seats, 3 at five, 67 at six.** P10 recorded that no bot round had ever exhausted the
  draw pile; that was a four-seat observation. P9's rule is exercised by real play at a full
  table.
- **The money split, measured** — and it confirms `RULES.md` §4.3's `DERIVED` balance argument
  rather than upsetting it. At five seats the money cards moved **$8.43 a round** against the
  flat prize of $20 — **42%**, where the derivation guessed 40%. `RULES.md` is at **rev 13**
  for that one note; **no rule changed and no new question was raised.**
- **What P12 did *not* need from the domain: anything.** Win rate, the flat/side-bet split,
  turns, how close the losers were, take rate, claim rate and deck exhaustions are all derived
  by the harness from the three §3.8 seams. The only Domain additions are `Agents/CoverScore`
  (an extraction, shared so the two bots' scoring cannot drift) and `Agents/SimpleBotAgent`.
- **Speed, for the record:** `PartialCover.Best` **140 µs** a hand, `HandEvaluator.TryFindCover`
  **91 µs**, a round **~20 ms**, a run **51 rounds/s serial and 85–92 parallel** on a 4-core
  i7-1165G7. ⚠️ **The work is allocation-bound, not compute-bound** — the searches allocate an
  index, a memo and a list per call, and eight threads bought only 25% more throughput until
  `<ServerGarbageCollection>` went into the Sim csproj, which took it to 70%. If throughput
  ever matters, **attack allocation**, and still never inside the evaluator (§3.4).
- P12 shipped 10 tests (235 total). The suite went from 1.7 s to 4.3 s, almost all of it the
  16-game comparison the Sim tests share through a `Lazy<SimulationReport>`.

**Still current, from P10:**

- **`PartialCover.Best(hand)` → `{ Melds, Uncovered, CoveredCount, IsComplete }`** is the
  scored cover P5 deliberately did not build. It is `HandEvaluator`'s own search **plus one
  branch**: at the lowest card not yet settled it may take a meld covering it *or give the card
  up and move on*. Memoised on `(position, covered)`; a complete cover stops the search where
  it stands, so a winning hand is no dearer than it is in the evaluator. `IsComplete` and
  `HandEvaluator.IsWinning` are pinned as the same claim across 320 randomly dealt hands.
- ⚠️ **`Melds/MeldIndex` is an extraction, not an addition.** Both searches need the same
  candidate index — cards in `CardId` order, one bit each, candidates filed under the lowest
  card they consume — and keeping two copies of that would be two places for it to drift.
  `HandEvaluator` was rewritten over it in a separate step and the **208-test baseline
  re-run before anything else was touched**; it is the only proof its answers did not move.
  Do this again if either search is ever changed.
- **The whole strategy is one question asked three ways:** *of the thirteen I would be left
  holding, how many meld?* Take the discard iff it raises that count, claim the turned-up money
  card iff it raises that count, throw whichever card leaves it highest. **The take decision is
  deliberately asked of the fourteen rather than of the thirteen kept** — same answer, a
  fourteenth of the work, because any improvement must use the new card and every fourteen-card
  arrangement has a meld of four or more to give one back.
- **The tie-break is what actually makes progress**, since early on most discards score alike:
  prefer keeping cards with partners (another suit of the same rank, a neighbour in the same
  suit — with the ace a neighbour of both the two and the king, never through), and keep jokers
  over everything. **A tie on *taking* goes to the deck**, which is the only place money enters
  a decision at all: a blind draw confers ownership and a pickup does not (RULES.md §4.4).
- ⚠️ **Termination is a property, not a hope.** The score can never fall — throwing back the
  card just taken restores the hand exactly — so a bot's hand climbs monotonically to thirteen.
  Measured over twelve seeds and every table size: **every round terminated, 21–30 turns, ~40 ms
  a round, ~1.5 ms a turn**. No bot round has ever run the draw pile out, so **P9's reshuffle
  is unexercised by bots** — the tests that cover it are still `RoundEngineTests`'.
- **Two findings worth keeping for P12.** A freshly dealt hand covers **4 of 13 on average**
  (154 of 800 dealt hands cover nothing at all), and a bot reaches thirteen in seven or eight of
  its *own* turns — mostly by **taking discards**, because with two decks the card somebody
  throws away is very often somebody else's third of a rank. The take-the-discard rate is going
  to be the interesting statistic.
- **`RecordingAgent` (test project) is how a bot is tested at all.** `TurnContext` has an
  `internal` constructor, so a test cannot fabricate one — a strategy is only observable from
  inside a real round. The decorator wraps a bot, plays a scripted `DealBuilder` deal, and
  reads back what it was asked and what it answered. This is exactly BUILD-PLAN §3.8's item 2
  seam, and P12 should lift it rather than reinvent it.
- **The console asks two questions now** — *"how many at the table?"* then *"how many of you
  are people?"* (0 is allowed, and leaves the computer playing itself). Bots are named from a
  roster and marked: *Ruby (bot)*, *Sable (bot)*, … so narration reads as a table of players.
  **No Console tests, still by construction.** Verification was again a pty:
  `script -qec "dotnet run --project BurmesePoker.Console --no-build" /dev/null < keys` with a
  file of newlines — **20 full rounds** against three bots, every settlement summing to zero,
  bots declaring covers including joker substitutions.
- ⚠️ **A bot's turn is instantaneous, and that is a UX problem P11 inherits.** Three bot turns
  now flash past and are wiped by the human's screen clear, so the round-log panel P11 already
  wanted is the sorest thing in the console. **Do not put a sleep in `GreedyBotAgent`** — a
  domain type that waited would ruin P12.
- **No new rules question.** Everything the strategy needed was already settled: §4.4, §4.5,
  §5, §6, §7.1. `RULES.md` is unchanged at rev 12.
- P10 shipped 17 tests (225 total).

**Still current, from P9:**

- **`MatchEngine(players, agents, stakes, random, observer = null)`** holds the seating, the
  stakes, the banks and the one `Random`. **`PlayRound()`** shuffles a fresh shoe;
  **`PlayRound(drawOrder)`** is the scriptable twin, mirroring `RoundEngine`'s own pair. Both
  return a **`RoundRecord(RoundResult Result, TableState Table)`** — the §3.8 pair, which is
  also what the console's settlement report reads. `Banks` is a live view, everyone starts at
  zero, and `RoundsPlayed` counts. **Nothing is retained per round**; do not add a history to
  make a report easier (P11 remembers its own).
- ⚠️ **`RoundEngine`'s constructor now takes a `Random`, and it is required** — inserted after
  `drawOrder`, before `round` and `observer`. A round needs randomness for the reshuffle, and
  defaulting it would have made an exhausted round irreproducible in silence (BUILD-PLAN §3.7).
  Test call sites pass `new Random(seed)`; `RoundEngineTests.Engine(...)` takes an optional
  `seed` that only matters if the round actually reshuffles.
- **The reshuffle is in `RoundEngine.TakeCard`, at the moment of drawing** — `if
  (Table.DrawPile.IsEmpty)` → `TableState.ReplaceDrawPileWithTheDiscards(random)`, which sweeps
  **every** seat's pile, shuffles, and installs a new `Deck`. Safe to sweep all of them because
  the current player has already declined the offered discard by then. **The turned-up cards
  are not gathered** (RULES.md §9 #4's recommendation, taken). `DeckExhaustedException` now
  means what it says: nothing left anywhere, which is a real end state rather than a crash.
- **Ownership across the reshuffle is `CardOwnership.TryRecordFromDeck`** — keeps the first
  owner and returns `false` rather than throwing. The blind draw calls it; **the deal still
  calls the strict `RecordFromDeck`**, where a card leaving the deck twice really is a bug.
  RULES.md §5's "first acquisition wins" is now a method rather than a paragraph.
- ⚠️ **`TurnContext` gained `Round`, and it fixed a real concealment bug.** An agent lives for
  the whole match, so `SpectrePlayerAgent`'s "have I already begun this turn?" check — which
  compared `TurnNumber` alone — matched turn 1 of round 2 against turn 1 of round 1 and
  **skipped the screen clear at the start of every round after the first**, leaving the
  previous hand on screen. It now tracks `(Round, TurnNumber)`. The round number is public
  information, so nothing leaks; the P7 reflection test still passes.
- ⚠️ **A round nobody can win now runs for ever.** This is the biggest behavioural change in
  the packet and it deleted a test: `NobodyDeclaringRunsTheDrawPileOutAndSaysSo` used to assert
  the throw, and now hangs. Every round-level test needs a seat that declares —
  `RoundEngineTests.WaitingToDeclare(turns)` builds one that holds a winning hand and declines
  until its *n*th turn, which is how the long reshuffle rounds are driven (a 4-player round
  exhausts the pile on **turn 55**, having gathered exactly 54 cards).
- **`ReshuffleSeed = 2` is pinned deliberately.** Any seed reshuffles, but seed 2 is one where
  the 7♦ Bo threw away comes back to *somebody else*, which is the case the rule is about. If
  that test ever needs re-seeding, seeds 3, 4, 5, 7, 11… also work.
- **`RecordingObserver` grew** `Draws`, `Reshuffles`, `OwnersAtFirstReshuffle` and
  `DrawsAfterFirstReshuffle`, and `IGameObserver` gained **`DiscardsReshuffled(int cards)`** —
  which is also how P12 counts deck exhaustions (§3.8).
- **The console plays a match now**: `Program` loops `PlayRound()` → settlement report →
  standings → `AnsiConsole.Confirm("Another round?")`, and prints the rounds played on the way
  out. Standings is a small table of the running banks, title `Standings` with the round count
  as a caption (a `Title` long enough to exceed the table's width wraps ugly).
- **No Console tests, still by construction** — the test project references Domain only.
  Verification was again a throwaway harness under the scratchpad: it links
  `BurmesePoker.Console/*.cs` plus the test project's `Hands`, `DealBuilder` and
  `ScriptedPlayerAgent`, sets `<StartupObject>` to its own entry point so `Program.Main` does
  not clash, and **calls `Program`'s private `ReportSettlement` / `ReportStandings` by
  reflection** — same-assembly, so `BindingFlags.NonPublic` reaches them. Driven through a pty
  with `script -qec "dotnet run --no-build" /dev/null < keys`. **Two tricks worth keeping:**
  arrow-key escapes fed to a Spectre `SelectionPrompt` did not register, so rig the deal such
  that the card to discard **sorts first** (hearts sort first, jokers last) and plain Enter
  picks it; and give the winning hand no hearts so the drawn heart is that card. Two full
  rounds were played, banks reaching +$28 / −$4 / −$12 / −$12 — summing to zero.
- **Three rules defaults taken, all recorded in `RULES.md` rev 12 §9 and phrased neutrally in
  `QUESTIONS-FOR-MYA-LAY.md`:** **#4** the turned-up cards are not swept into the reshuffle;
  **#5** the money-card claim is offered every round, approved by nobody; **#14** (new) nothing
  moves between rounds — the seating the match was given is played all session.
- P9 shipped 16 tests and removed 1 (208 total).

**Still current, from P8:**

- **The console is a hotseat, and concealment is the screen clearing.** Every seat is a
  person at the same terminal, so `SpectrePlayerAgent.BeginTurn` clears the screen once per
  turn, names the player, and waits for *"are you at the keyboard?"* before drawing their
  hand. It fires on whichever of the four questions comes first, and tracks the turn by
  `TurnContext.TurnNumber` rather than by counting calls — **a turn asks a varying number of
  questions** (the opener is offered the money card, later turns the discard, only a winning
  hand the declaration).
- **`ConsoleObserver.PlayerDrew` deliberately does not print the card.** The domain narrates
  private information and says so; filtering is the front end's job (BUILD-PLAN §3.5). A
  pickup, a claim, a discard and a declaration are all public and are printed in full.
  A side effect of the per-turn clear: **public narration scrolls away**. The table panel
  reprints what still matters (turned-up cards, draw count, the takeable discard). If P9
  wants a running log it needs a panel that survives the clear, not more `WriteLine`s.
- **The star marks an owned *money* card, not any owned card.** Everything dealt is owned, so
  starring ownership alone marked all thirteen and said nothing. `CardFormatting.Of(card,
  registry, owned)` only stars when the multiplier is non-zero — a money card with no star
  came from a discard pile or off the table and pays somebody else.
- **The settlement breakdown lives in `Program`, not in the observer**, because settlement
  returns net deltas only and splitting them needs `TableState.Ownership` and
  `TableState.Shoe`, which are on the table rather than in the `RoundResult`. The round half
  is derived from the winner (flat, RULES.md §7.2) and the money-card half is the remainder,
  so **the two columns always add up to what the domain actually settled**. ⚠️ **This is the
  one thing P9 must re-plan around** — see BUILD-PLAN P9, amended.
- **Prompts are offered unconditionally and that is correct.** The engine only asks a question
  that has a legal answer, so neither the claim, the pickup nor the declaration needs a
  legality check on the console side; adding one would duplicate the rules outside the domain.
- **No tests were added — by construction.** The test project references Domain only, so
  nothing in `BurmesePoker.Console` is unit-testable, and P8 changed no Domain code: still
  **192 passed / 0 failed**. Verification was manual, and worth repeating the recipe:
  `script -qec "dotnet run --project BurmesePoker.Console" /dev/null < keys` gives Spectre a
  pty (piped stdin fails — `System.Console.ReadKey` throws when input is redirected, and
  `Program` refuses to start with a clear message rather than a stack trace). A second,
  throwaway harness under the scratchpad linked the test project's `DealBuilder`,
  `ScriptedPlayerAgent` and `Hands` sources, rigged a winning deal, and drove **a real
  `SpectrePlayerAgent` to a declaration** — that is how the declare prompt and the settlement
  table were seen working, arithmetic checked by hand (+16 / 0 / −8 / −8, summing to zero).
- **Spectre.Console is back at 0.57.2**, the current release, not the 2023 pin of 0.47.0.
  Only `BurmesePoker.Console` references it.
- **No new rules question.** Everything P8 needed was settled: §3 step 2 (seating is
  randomised in `Program`), §4.4/§4.5 (a claimed card is held but owned by nobody), §6.3
  (concealment), §7.2 (a flat round payment).

**Still current, from P7:**

- **`new RoundEngine(players, agents, stakes, drawOrder, round, observer)` deals in the
  constructor and `Play()` runs the turns**, so `engine.Table` is readable before the first
  turn and a setup test needs no play at all. `Play()` returns a `RoundResult` and refuses a
  second call. **`RoundEngine.Shuffled(..., Random)` is the real-game entry point.**
- **⚠️ A round is dealt from a `drawOrder`, not from a shuffle** — the 108 cards in the order
  they will leave the deck, validated as a permutation of the shoe. That is what makes a round
  scriptable: `BurmesePoker.Tests/Play/DealBuilder.cs` arranges the order so seat *s* gets
  positions *s*, *s+n*, *s+2n*…, then the turned-up top card, then the draw pile, with the
  turned-up bottom card last. **Its filler is never a money card**, so a settlement expectation
  can be worked out by hand; ask for a money card explicitly with `ThenDraw("7D")`.
- **The engine builds the shoe itself.** `Settlement.ForRound` needs the *unshuffled*
  index-aligned shoe, so `RoundEngine` calls `DeckBuilder.BuildTwoDecks()` at setup, keeps it
  as `TableState.Shoe`, and validates `drawOrder` against it. P6's warning that the caller must
  keep the builder list is now handled inside the engine — **callers pass nothing extra**.
- **`TurnContext` is the concealment rule expressed as a type**: own hand, available discard,
  draw-pile count, turned-up cards, stakes, registry, `TurnNumber`, `Taken`, `CanDeclare`,
  `YouOwn(card)`. **No `TableState`, no `PlayerState`, no `CardOwnership`** — exposing
  ownership would leak which money cards an opponent was dealt. A reflection test pins it.
- **The engine asks narrowly, so the front end needs no legality checks.** `ChooseAction` is
  asked only when a discard is available (so the opening turn just draws), `ClaimTurnedUpMoneyCard`
  only on turn 1, and `Declare` only when `HandEvaluator.TryFindCover` has already succeeded —
  the cover it found is what `RoundResult.Melds` carries, so it is never computed twice.
- **⚠️ Narrate *after* the card lands.** The conservation test caught a real ordering fault:
  raising `PlayerDrew` between `DrawFromTop` and `seat.Take` leaves an observer seeing 107
  cards. Every take now joins the hand before the event fires. Not a clone bug — but the same
  test that would have caught the 2023 clone bug caught this.
- **`IGameObserver` gained three events over BUILD-PLAN §3.5's sketch** — `PlayerTookDiscard`,
  `MoneyCardClaimed`, `PlayerDeclared` — because a pickup, a claim and a blind draw are three
  different things and only the draw confers ownership. **All methods are default no-ops.**
- **Ownership accounting is one invariant, and it is tested as one:** records == cards dealt +
  blind draws, ever. A claim records nothing, a pickup records nothing, a discard changes
  nothing. `OnlyTheDealAndABlindDrawEverConferOwnership` exercises all three routes in one
  round and asserts the count.
- ~~Deck exhaustion still propagates~~ — ✅ **done in P9**, inside `RoundEngine.TakeCard`
  exactly as P7 predicted it would have to be.
- **Seating is taken as given.** RULES.md §3 step 2 randomises it, but an engine that
  reshuffled its own seating could not be scripted, so P8's `Program.cs` randomises before
  constructing. The same list is what settlement is handed.
- **Two new rules questions, both defaulted, neither blocking** (`RULES.md` rev 11, and both
  phrased neutrally in `QUESTIONS-FOR-MYA-LAY.md`):
  **§9 #12** — when the opening player claims the turned-up money card, does that value still
  pay? **Default taken: yes**, designation is fixed at setup and does not move with the card;
  reversing it is one line in `TableState`'s constructor.
  **§9 #13** — may a player discard the very card they just took? **Default taken: yes**,
  nothing in §5 forbids it; it would be a single guard in `RoundEngine`.
- P7 shipped 18 tests (192 total).

**Still current, from P6:**

- **`Settlement.ForRound(players, winner, stakes, moneyCards, ownership, shoe)` →
  `IReadOnlyDictionary<PlayerId, int>`** of **net** deltas, positive to collect. Every player
  at the table appears, including zero deltas, and the deltas always sum to zero. It is a
  static class — settlement holds no state.
- **⚠️ The `shoe` parameter must be `DeckBuilder.BuildTwoDecks()` order, and this is checked.**
  Settlement resolves an owned `CardId` to a `Card` by *index*, and rejects any list where
  `shoe[i].Id.Value != i` — so **passing `Deck.Cards` throws**, because it is shuffled. That
  guard is deliberate: without it a shuffled shoe would settle the wrong cards in silence, and
  every arithmetic test would still pass. **P7 must keep the builder list it made the round's
  `Deck` from** and hand that over; `Deck` copies its cards, so the list is never disturbed.
- **Settlement is never given a hand, and a reflection test pins the parameter list** so it
  stays that way. This is RULES.md §4.4 encoded in a signature: the question is never who
  *holds* a card. The two headline tests — a money card its owner discarded, and one an
  opponent is holding — pass without any notion of a hand existing at all.
- **`Stakes` is a `sealed record`, not a struct.** A `readonly record struct` was the obvious
  reflex, but `default(Stakes)` would then be a silent $0/$0 game that still sums to zero and
  passes every property test. A class makes the omission a `NullReferenceException` instead.
  Both values must be **positive**; `Stakes.Standard` is $5 / $1.
- **The 7♦ pays *once* unless it is also turned up.** Permanent designation and turned-up
  designation are separate summands (P2), so an owned 7♦ in a round where the turned-up cards
  are something else pays `1 × MoneyCardValue`, not 2. A draft test asserted +8 for exactly
  that case and was wrong; the arithmetic is unchanged, the expectation was.
- **Guards, all `ArgumentException`:** winner not at the table, a player seated twice, an empty
  table, an ownership record naming somebody not at the table, an owned id outside the shoe,
  and the misaligned-shoe check above. **There is no no-winner settlement** — P7 rounds end on
  a declaration, and P9 reshuffles rather than ending one early.
- **Zero-sum is property-tested** over 500 randomised rounds (2–6 players, random stakes,
  random turned-up cards, up to 80 owned cards). Sample the deal with `Random.Shuffle` over a
  copy of the shoe — cards must be **distinct**, or `RecordFromDeck` rightly throws on the
  second owner of one physical card. This makes P9's match-level conservation test a test of
  *banking*, not of settlement; BUILD-PLAN P9 has been amended to say so.
- **P8 gets net deltas only** — no breakdown of round payment against side-bet, no per-card
  detail. If the console wants "−$5 for the round, +$3 in money cards" it computes the
  side-bet half itself from `ownership.Records` and `Multiplier`. BUILD-PLAN P8 amended.
- **No new rules question.** §4.3, §4.4 and §7.2 are all `PLAYER`/`EXPERT` Settled and the
  worked example is reproduced exactly; nothing needed a judgement call. `RULES.md` is
  untouched at rev 10.
- P6 shipped 26 tests (174 total).

**From P2:**

- **`MoneyCardRegistry(turnedUp).Multiplier(card)` → 0 / 1 / 2** is the whole designation API,
  and it is a pure function of the turned-up cards — no `Card` is written to, ever. The
  implementation is literally `(permanent ? 1 : 0) + (turnedUp ? 1 : 0)`, so **doubling is the
  overlap and the ceiling falls out with no clamp**: two copies of the 5♥ turned up still pay
  1, two copies of the 7♦ still pay 2.
- **The permanent designators (7♦, A♠) are two `Card`s carrying negative `CardId`s**, compared
  by `SameValueAs` like every other designator. They are values, never dealt; a negative id
  means a stray `==` against a real card can only be false.
- **The turned-up list is copied**, and any length is accepted — including empty, which the
  "permanent cards with nothing turned up" acceptance test needs. **No arity check.** If P7
  wants to insist there are exactly two, that is P7's rule to enforce at setup.
- **No joker branch was written**, per the P1 default: a turned-up red joker designates the two
  red jokers and neither black one, because `SameValueAs` discriminates jokers by colour.
  `ATurnedUpRedJokerDesignatesTheRedJokersAndNotTheBlackOnes` is the single test to change if
  `RULES.md` §9 #11 ever settles the other way.
- **`PlayerId` was brought forward from `Play/`** — `CardOwnership` needs it and P7 is a long
  way off. It is a `readonly record struct PlayerId(int Value)`. **P7 must not redefine it**;
  BUILD-PLAN P7's build list has been amended.
- **`RecordFromDeck` re-recording the *same* owner is a no-op; a *different* owner throws
  `InvalidOperationException`.** The packet allowed either "rejected or a no-op" — this splits
  it, because a genuine repeat is harmless while two owners for one physical card can only be
  a dealing bug worth surfacing. There is deliberately no transfer, clear or removal, and a
  reflection test asserts the public surface stays that way.
- **⚠️ P6 needs a card lookup, and this is the one thing that re-planned.** `Records` is keyed
  by **`CardId`** but `Multiplier` takes a **`Card`**, because designation is by value and
  ownership is by instance — the two identity notions meeting at exactly the seam BUILD-PLAN
  §3.1 predicted. So `Settlement.ForRound` must be given the shoe as well.
  **`DeckBuilder.BuildTwoDecks()` is index-aligned** (`CardId.Value` == list index), so the
  lookup is an array index — but **`Deck.Cards` is shuffled and is not**. Do *not* widen
  `RecordFromDeck` to take a whole `Card`; BUILD-PLAN §3.3 fixes its signature and ownership
  is about the physical card. BUILD-PLAN P6 has been amended.
- **`CardOwnership.Records` is a live read-only view** over the internal dictionary, in the
  same spirit as `Deck.Cards`. Snapshot it if you need one.
- **No new rules question.** The packet needed no judgement beyond the §9 #11 default P1 had
  already recorded and `QUESTIONS-FOR-MYA-LAY.md` already asks.
- P2 shipped 29 tests (148 total).

**Still current, from P5:**

- **`MeldCandidates.For(hand)` → `IReadOnlyList<Meld>`**: runs first, then the sets whose card
  set no run already covers. `HandEvaluator.IsWinning(hand)` and
  `HandEvaluator.TryFindCover(hand, out var cover)` are the win authority — **the only one**.
  Nothing else in the codebase may decide a hand has gone out.
- **`TryFindCover` returns *a* cover, never a canonical one.** Thirteen hearts in sequence come
  back as `3+3+3+4`, not as one thirteen-card run, because the search takes the first candidate
  containing the lowest uncovered card. `IsWinning` is what is settled; the shape of the cover
  is not. **P8 must not assume the tidy grouping** — BUILD-PLAN P8 has been amended.
- **A "wins only if a joker plays a card the hand holds" test cannot be built out of a run.**
  A joker can nearly always play *outward* from a run onto a rank the hand does not hold —
  below the bottom card or above the top — so a rival cover always exists in which the joker
  merely fills a gap, and a generator that never substituted for a held card would find it too.
  Blocking one end only moves the boundary a rank along, and the ace is the sole natural stop.
  **A set has no escape**: it is capped at four suits, so five fives (two decks) plus a joker
  can only cover as two three-card sets, and whichever suit the joker plays is one the hand
  holds. That is `AHandWinsOnlyByPlayingAJokerAsACardItAlsoHolds`. The run flavour is tested
  one level down, on the candidate `2♦ 3♦ [🃏] 5♦ 6♦` with the real 4♦ melded elsewhere.
- **Jokers make almost any hand winnable — mind this when writing negative tests.** With two
  spare jokers any orphan finds a set, so a hand that must evaluate `false` has to be
  joker-free or joker-poor. Two drafted "not winning" hands turned out to be winning that way.
- **Performance is a non-issue.** Three thirteen-card stress hands — the 4,032-candidate one,
  a two-deck hand holding every diamond twice, and a losing hand that forces full exhaustion —
  take about **100 ms** in total. The pinning does that work; the dead-end memo changed nothing
  measurable and is kept only as a bound.
- **No partial-cover search exists**, deliberately. `TryFindCover` is all-or-nothing. A bot's
  "largest cover found" and a player's "best so far" hint need a *scored* version of the same
  backtracking, and BUILD-PLAN P10 now says so.
- **No purity requirement is implemented** — `RULES.md` §7.1 leaves "is a pure sequence
  required" open and recommends treating it as not-a-rule. If it ever settles the other way it
  is a filter on the cover, not a change to the search.
- **Guards:** the same card instance twice throws `ArgumentException`; a hand over
  `HandEvaluator.MaximumHandSize` (64, one bit per card) throws; an empty hand is covered by no
  melds at all and returns `true`.
- **No new rules question.** P5 shipped 19 tests (119 total).

**Still current, from P4:**

- **`SetGenerator.Candidates(hand)` → `IReadOnlyList<Meld>`**, mirroring `RunGenerator` in
  every respect: eager, de-duplicated by card set, jokers taken in ascending index order.
  It walks the **four suits once per rank**, taking each suit as a held card, a joker, or
  nothing — a three-card set is a four-suit set with one suit left unfilled. That single
  formulation gives the ≥3-distinct-suits rule, the four-card maximum and joker substitution
  at once, with no subset enumeration anywhere.
- **⚠️ The two generators can emit the same card set — P5 must de-duplicate across them.**
  It happens for any meld holding **at most one real card**: `{9♦,🃏,🃏}` is a run (jokers as
  10♦ and J♦) *and* a set (jokers as 9♠ and 9♥), and `{🃏,🃏,🃏}` is both trivially. Not
  wrong — identity is the card set — but it makes the cover search try the same cover twice
  and makes `TryFindCover` report a kind arbitrarily. **`MeldCandidates.For` should
  de-duplicate with the usual set comparer and keep the run interpretation**, which is
  generated first. BUILD-PLAN P5 has been amended to say so.
- **Sets cannot explode the way runs can.** Measured worst case **639** candidates — nine
  cards of one rank split (3,2,2,2) across the suits plus all four jokers — against P3's
  4,032. The closed form is in the test: every choice of *k* real cards in distinct suits
  plus *j* jokers with 3 ≤ k+j ≤ 4. The §7 risk row now says the explosion risk is P3's alone.
- **The brute-force cross-check paid off again**, and was trivial for sets: a subset is a set
  iff it holds 3–4 cards whose real members share a rank and occupy distinct suits. Nothing
  had to be said about jokers at all — with at most four cards there is always a free suit.
- **All-joker sets are emitted**, matching P3 and `RULES.md` §9 #8's *unlimited jokers*
  recommendation. `AHandOfNothingButJokersStillMakesSets` and its `RunGenerator` twin are the
  pair to change together if that ever settles the other way.
- **No new rules question.** §6.2 was already `EXPERT`-confirmed and settled; the packet
  needed no judgement call beyond the all-joker default P3 had already taken.
- P4 shipped 18 tests (100 total).

**Still current, from P3:**

- **`RunGenerator.Candidates(hand)` returns `IReadOnlyList<Meld>`**, not the bare
  `IEnumerable<Meld>` of BUILD-PLAN §3.4 — generation is eager anyway, because
  de-duplication has to see every candidate. Deliberate; don't "fix" it to lazy.
- **`Meld` and `MeldSlot` are shared with P4 and P5.** A slot is
  `(Card Card, Rank PlaysAs, Suit InSuit)`: for a real card its own rank and suit, for a
  joker what it stands in for. A meld's **identity is `CardIds`** — never its display value.
  `Meld` validates only ≥ 3 cards and no card used twice; run/set legality is the
  generator's. `Meld.Overlaps` exists for P5.
- **Jokers are taken in ascending index order** inside the fill recursion. That is not a
  detail — it is what makes the search enumerate joker *combinations* once each instead of
  every permutation, and P4 needs the same trick.
- **De-duplicate with `new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer())`.**
  The BCL comparer does structural set equality; no custom comparer is needed.
- **Two numbers in the docs were wrong and are now measured.** All thirteen ranks of one suit
  gives **76** candidates, not 77 — `A-2-…-K` and `2-…-K-A` are the same thirteen cards. The
  joker-heavy worst case gives **4,032**, not "hundreds"; the spec and the §7 risk table both
  now say thousands. Neither is a bug: a brute-force enumeration of every subset agrees with
  the generator card set for card set.
- **A brute-force cross-check earns its keep.** `GeneratorFindsExactlyTheCardSetsThatCanFormARun`
  enumerates all 2ⁿ subsets of a hand and asks, backwards, which could fill some window. It
  is what proved the 4,032 correct rather than merely bounded. **Write the equivalent for P4.**
- **Assumption taken: all-joker melds are legal**, so `🃏🃏🃏` is a run candidate. This follows
  `RULES.md` §9 #8's *unlimited jokers* recommendation, which rev 10 widened to name the case;
  requiring one real card per meld would be a stricter rule that §6.1 nowhere states. A hand
  can hold at most four jokers, so the blast radius is five extra candidates.
  `QUESTIONS-FOR-MYA-LAY.md` asks it as a table situation. **P4 should match; P5 must not
  assume a meld contains a ranked card.**
- **Test hands come from `BurmesePoker.Tests/Hands.cs`** — `Hands.Of("2D", "3D", "RJ")`, with
  `CardId`s assigned in the order listed so duplicate copies stay distinguishable. `RJ`/`BJ`
  are the jokers. This is where `CardText.ParseRank` finally earns its keep.
- P3 shipped 22 tests (82 total).

**Still current, from P1:**

- **Build cards with `Card.Ranked(id, rank, suit)` and `Card.Joker(id, color)`**, not the
  positional constructor. They derive `Color` from `Suit`, so a card cannot be given a colour
  that contradicts its suit. The positional constructor stays public because §3.1 specifies
  it; the factories are the ergonomic path.
- **`Deck.TwoDecks()`** builds the 108-card shoe in one call. `Deck.Cards` is a **live**
  read-only view, top first — `.ToList()` it before drawing or shuffling if you need a
  snapshot. `Deck` copies the cards it is constructed with.
- **Index 0 is the top of the deck.** `DrawFromTop` takes index 0; `DrawFromBottom` takes the
  last. Both matter at setup: `RULES.md` §3 step 4 turns up one money card from the **bottom**
  and one from the **top**.
- **`DeckExhaustedException`** (in `Domain/Cards/`) derives from `Exception` directly, not
  `InvalidOperationException`, so an empty draw pile is distinguishable from a bug. **P7 must
  reuse it rather than inventing another** — BUILD-PLAN P7 has been amended to say so. P9 is
  where it actually gets caught and turned into the discard-pile reshuffle (`RULES.md` §5).
- **Shuffling is `Random.Shuffle(Span<T>)`** over `CollectionsMarshal.AsSpan`, not
  `OrderBy(r.Next())`. Don't "simplify" it back — the old form is not a uniform permutation.
- **New rules question, `RULES.md` §9 #11 — a turned-up joker.** `SameValueAs` compares
  colour as well as rank and suit, which is a no-op for ranked cards but discriminates
  jokers by colour. Nothing says what a joker turned up as one of the two money cards
  designates, and it will happen about one round in fourteen. **Safe default for P2: no
  special case — designate by `SameValueAs` like any other card**, so a turned-up red joker
  designates the two red jokers and neither black one. Phrased neutrally in
  `QUESTIONS-FOR-MYA-LAY.md`. **Do not block on it.**
- P1 shipped 32 tests (60 total). No surprises in the packet itself — it was mechanical.

**Still current, from P0:**

- **Run the app with `dotnet run --project BurmesePoker.Console`**. `Program.cs` is a
  one-line placeholder; P8 rebuilds the front end.
- `MoneyCardStatus` and `PlayerAction` were deliberately **not** ported — superseded by
  `MoneyCardRegistry.Multiplier` (P2) and `TurnAction` (P7). `CardPlayType` became
  `Melds/MeldKind`.
- `UserPromptFactory` was deleted with the rest of `Logic/`. **P8 should read it from the
  `pre-rewrite` tag:** `git show pre-rewrite:BurmesePoker/Logic/Factories/UserPromptFactory.cs`.
- `CardText.ParseRank` is still uncalled by the domain. It is expected to earn its keep
  building hands in P3/P4 test fixtures.
- **All three projects target `net10.0`** and the solution file is **`BurmesePoker.slnx`**.
  Both were chosen deliberately: Nick's standing preference is the newest supported .NET
  tooling. Don't "fix" either back. Tests are **xunit v3 4.0.0** on
  **Microsoft.Testing.Platform**; `Microsoft.NET.Test.Sdk` and `coverlet.collector` are gone,
  and so are `xunit.runner.visualstudio` and the coverage collector — nothing needs them.
  The Console project has **no `Spectre.Console` reference**; **P8 adds it back** at the
  current version.
- **Two MTP consequences a cold session will trip over:**
  1. `global.json` opts `dotnet test` into MTP mode. **Do not delete it** — without it
     `dotnet test` fails outright.
  2. **Filtering is `--filter-method "*Name*"` / `--filter-class "*Name*"`.** VSTest's
     `--filter "FullyQualifiedName~Name"` is rejected.
- The test project is `<OutputType>Exe</OutputType>` — expected for xunit v3, not a mistake.
- **`BUILD-PLAN` §5 P3's "Done when" said 8 candidates** while the packet body said 5. It was
  amended to **5** in P0, along with the same stale 8 in the §8 risk table.
  `docs/spec/RUN-CANDIDATES.md` was already correct.

---

## Decisions needed from Nick

**None. Every blocking rules question is closed** (`RULES.md` rev 10). P0 through P9 can all
proceed without further input.

The items left in `RULES.md` §9 are fidelity questions with safe defaults already recorded in
`BUILD-PLAN.md` — the discard exception, jokers per meld, whether the money-card claim recurs
each round, whether a pure sequence is required, what a turned-up joker designates, whether a
meld may be made of nothing but jokers, and (new in rev 11) whether a claimed money card still
designates its value and whether you may throw back the card you just took. `QUESTIONS-FOR-MYA-LAY.md` has them phrased ready to ask.
**Do not block on them.**

---

## Session log

| Date | Packet | Outcome |
|---|---|---|
| 2026-08-18 | P12 | ☑ Done. **Simulation at scale.** A fourth project, `BurmesePoker.Sim` (Domain only): seeded games run in parallel, a strategy per seat rotated by game, per-round rows carrying their own join keys, and a CSV writer. Per-game seeds are `SplitMix64(master, index)` so a game is the same game however the run was scheduled — **serial, parallel and two-thread runs are byte-identical**. The turn cap lives in a `SeatRecorder` decorator over `IPlayerAgent` and **reports** abandonment rather than dropping it, because the domain will not invent a rule the game does not have. Domain gained only `Agents/CoverScore` (extracted, so the bots' scoring cannot drift) and **`Agents/SimpleBotAgent`** — the greedy bot with the discard tie-break removed and nothing else changed. **The measured answer: greedy takes 30.7% of 2,000 four-seat rounds against simple's 19.3%, +$1.24 a round against −$1.24** — P10's claim about the tie-break, measured. Build clean, **235 passed / 0 failed** (10 new), including determinism, per-round money conservation, a reflection pin on mutable static state, and the abandonment path. ⚠️ Measurement pass first, as the packet required: `PartialCover.Best` **140 µs**, `TryFindCover` **91 µs**, a round **~20 ms**, **51 rounds/s serial and 85–92 parallel**; **nothing was optimised**, but the work turns out to be **allocation-bound** — the server GC took eight-thread scaling from 25% to 70%. ⚠️ Two findings: **the reshuffle is a six-player phenomenon** (0/3/67 reshuffles per 300 rounds at 4/5/6 seats), and the **turn cap has never fired in a real run**. `RULES.md` → **rev 13**: §4.3's `DERIVED` 40% side-bet estimate measured at 42% over 600 five-player rounds — a confirmation, not a rule change, and no new question raised. Amended BUILD-PLAN §0, §2, §3.7, §4, P11, P12, P13 and §7 (two risk rows retired). |
| 2026-08-18 | P10 | ☑ Done. **Solo play.** `Melds/PartialCover` — the evaluator's search with one extra branch (give a card up and move on), memoised on `(position, covered)` and short-circuited on a complete cover — and `Agents/GreedyBotAgent`, whose entire strategy is *of the thirteen I would keep, how many meld?* asked of the discard, the claim and every candidate throw. `Melds/MeldIndex` extracted so both searches share one candidate index; `HandEvaluator` rewritten over it and the 208-test baseline re-run before anything else changed. **Money is absent from every decision** (RULES.md §4.4) except the take tie-break, which favours the deck because a blind draw confers ownership. `Program` asks *"how many of you are people?"* and fills the rest with named bots. Build clean, **225 passed / 0 failed** (17 new). ⚠️ **Termination measured, not assumed**: twelve seeds × 4–6 players, every round finished in 21–30 turns at ~40 ms a round. Verified the console by playing **20 rounds** against bots through a pty. No new rules question — `RULES.md` stays at rev 12. Amended BUILD-PLAN §0, §2, §3.7, P11, P12, P13, §4 and §7 (the "never-improving strategy" risk retired; the hot loop re-aimed at `PartialCover`). |
| 2026-08-18 | P9 | ☑ Done. `MatchEngine` — repeated rounds, banks carrying over, no automatic end — returning a `RoundRecord(RoundResult, TableState)` per round and keeping no history. Deck exhaustion now **reshuffles inside `RoundEngine.TakeCard`**: every discard pile gathered and shuffled into a new draw pile, the turned-up cards left alone, ownership held by whoever acquired the card first (`CardOwnership.TryRecordFromDeck`). `RoundResult.Turns` added; `IGameObserver.DiscardsReshuffled` added; `RoundEngine` now **requires** a `Random`. ⚠️ Found and fixed a real concealment bug the match loop exposed — `SpectrePlayerAgent` compared turn numbers alone and so skipped its screen clear on turn 1 of every round after the first; `TurnContext` gained `Round`. `Program` now loops rounds, asks *"another round?"* and prints standings. Build clean, **208 passed / 0 failed** (16 new, 1 removed — a passive round no longer terminates, so the old exhaustion test would hang). Verified the console by driving two full rounds through a pty. Rules defaults taken for `RULES.md` §9 #4 and #5, and new #14 raised (rev 12). Amended BUILD-PLAN §2, P10, P11 and P12. |
| 2026-08-18 | — | **Statistics added as a design constraint** (doc-only, no code). `BUILD-PLAN.md` **§3.8**: the domain gains no notion of a statistic, and everything a strategy comparison wants is derived by the consumer from three seams — the observer stream, the per-round `(RoundResult, TableState)` pair, and a **recording decorator over `IPlayerAgent`** for anything decision-level (which needs no domain change and serves human replay too). Four constraints recorded, the sharpest being that ⚠️ **P9 must surface each round's table or two of the five stat families become unreachable**. P9 also gains `Turns` on `RoundResult`. §3.5 now says the observer event set is open but **hot** — events pass what the engine holds and never allocate. P12's build list rewritten against §3.8; §0, CLAUDE.md and the `/poker` skill's stale P0 baseline exception brought current. Build clean, **192 passed / 0 failed** — unchanged. |
| 2026-08-18 | — | **Roadmap extended** (doc-only, no code). Nick named four goals beyond a playable game; written up as `BUILD-PLAN.md` **§0**, with **§3.6** (agents stay synchronous — a remote player blocks in the agent, one table is one task) and **§3.7** (simulation is a first-class consumer: determinism ✅, no I/O ✅, no mutable statics ✅, speed ⚠️ unmeasured) taken now rather than discovered later. **P10 promoted out of "optional" and rewritten** — its "never discard an owned money card" heuristic contradicted §4.4 and is corrected, bots move to `Domain/Agents/` so they are testable and reusable, and the scored partial cover P5 left unbuilt is specified here. Added **P11** (console UX), **P12** (simulation at scale), **P13** (multiplayer). §4 graph and §7 risks updated. Build clean, **192 passed / 0 failed** — unchanged, nothing was built. |
| 2026-08-18 | P8 | ☑ Done. `CardFormatting`, `SpectrePlayerAgent`, `ConsoleObserver` and a `Program` that asks for players and stakes, randomises the seating, plays a round and reports the settlement split into its round and money-card halves. Hotseat concealment: clear and hand over the keyboard once a turn; blind draws are narrated without the card. Spectre.Console 0.57.2 added back. **No Domain change, so no new tests — 192 passed / 0 failed**, build clean; verified manually by driving the real binary and a rigged winning deal through a pty. Amended BUILD-PLAN P9 (the console's settlement report needs each round's `TableState`, so `MatchEngine` must surface it; between-round "stop playing" is the console's to ask) and P10. |
| 2026-08-18 | P7 | ☑ Done. `TurnAction`, `PlayerState`, `TableState`, `TurnContext`, `RoundResult`, `RoundEngine`, `IPlayerAgent`, `IGameObserver`. A round deals from a validated draw order (so it is scriptable), turns up bottom-then-top, offers the claim on the opening turn only, records ownership on deals and blind draws alone, discards before revealing, and settles on a declaration. Build clean, **192 passed / 0 failed** (18 new), including the four acceptance tests — expected settlement, 13/14 hand sizes, 108 distinct cards at every event, and a claim that grants no ownership. Raised `RULES.md` §9 #12 and #13 (rev 11), both defaulted. Amended BUILD-PLAN §2, §3.5, P8 (what the engine asks the console) and **P9 (the reshuffle must go inside the engine — catching `Play()` cannot resume a round)**. |
| 2026-08-18 | P6 | ☑ Done. `Stakes` (sealed record, positive-only, `Standard` = $5/$1) and `Settlement.ForRound` → per-player net deltas: flat round value from every loser to the winner, then each **owned** money card paying its owner `multiplier × money card value` from every other player. Walks ownership records and is never given a hand — pinned by a reflection test on the parameter list. Resolves an owned `CardId` through the **unshuffled** shoe by index and rejects a shuffled one outright. Build clean, **174 passed / 0 failed** (26 new), including the §4.3 worked example and a 500-round zero-sum property test. Amended BUILD-PLAN §2, P7 (keep the builder list; one roster; a round always has a winner), P8 (net deltas only) and P9 (conservation is a banking test). |
| 2026-08-18 | P2 | ☑ Done. `MoneyCardRegistry` (pure function of the turned-up cards; permanent 7♦/A♠ as negative-id value designators; multiplier is permanent + turned-up, so doubling is the overlap and its own ceiling) and `CardOwnership` (append-only, write-once, no transfer/clear/remove — enforced by a reflection test). `PlayerId` brought forward into `Play/`. Build clean, **148 passed / 0 failed** (29 new). Re-planned P6: `Records` is keyed by `CardId` while `Multiplier` takes a `Card`, so settlement needs the shoe passed in — `DeckBuilder.BuildTwoDecks()` is index-aligned, `Deck.Cards` is not. Amended BUILD-PLAN §2, P2, P6 and P7. |
| 2026-08-18 | P5 | ☑ Done. `MeldCandidates.For` (runs, then the sets no run already consumes) and `HandEvaluator.IsWinning` / `TryFindCover` — backtracking pinned to the lowest uncovered card, candidates indexed by their lowest card, coverage carried as a bitmask so dead ends memoise. Build clean, **119 passed / 0 failed** (19 new). Found that the joker-substitution acceptance hand has to be built from a set rather than a run, and that `TryFindCover`'s cover is not canonical; amended BUILD-PLAN P5, P8, P10 and the §7 risk table. |
| 2026-08-18 | P4 | ☑ Done. `SetGenerator` — one walk over the four suits per rank, each taken as a held card, a specific joker, or nothing; de-duplicated by card set. Duplicate suits impossible by construction, so a set is at most four cards. Build clean, **100 passed / 0 failed** (18 new), including a brute-force cross-check over every subset. Measured the worst case at 639 candidates and amended the §7 risk row. Re-planned P5: the two generators collide on any meld with ≤1 real card, so `MeldCandidates.For` must de-duplicate across them. |
| 2026-08-18 | P3 | ☑ Done. `MeldSlot`, `Meld` (identity is its `CardId` set) and `RunGenerator` — window-based generation with joker substitution, jokers chosen as combinations. Reference hand yields the specified **5** candidates. Build clean, **82 passed / 0 failed** (22 new). Corrected two counts in `docs/spec/RUN-CANDIDATES.md` (76, not 77; 4,032, not "hundreds"), widened `RULES.md` §9 #8 to cover all-joker melds (rev 10), and re-planned P4 and P5 around the shared `Meld` vocabulary. |
| 2026-08-18 | P1 | ☑ Done. `CardId`, `Card` (record struct: `==` is instance identity, `SameValueAs` is value identity), `DeckBuilder.BuildTwoDecks()` → 108 cards with sequential ids, `Deck` (draw from either end, Fisher–Yates shuffle), `DeckExhaustedException`. Build clean, **60 passed / 0 failed** (32 new). Raised `RULES.md` §9 #11 (turned-up joker) and amended BUILD-PLAN P1, P2 and P7. |
| 2026-08-18 | P0 | ☑ Done. Tagged `pre-rewrite`, then deleted `Models/`, `Logic/` and `Common.cs`. Solution restructured to Domain/Console/Tests. Salvaged the enums and display tables into `Cards/{Rank,Suit,CardColor,CardText}` and `Melds/MeldKind`. Build clean, **28 passed / 0 failed**. Amended P0's acceptance (tests, not zero tests) and P3's "Done when" (5 candidates, not 8). |
| 2026-08-18 | — | Rules reconstructed from a codebase abandoned in 2023. `RULES.md` reached rev 8 with all blocking questions closed. Rewrite decided (`BUILD-PLAN.md` §1); 11 packets defined. `docs/spec/RUN-CANDIDATES.md` written, correcting P3's acceptance count from 8 to 5. `/poker` skill created. No code written. |
