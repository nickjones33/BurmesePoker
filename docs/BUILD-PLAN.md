# Burmese Poker — Build Plan

**Supersedes `RECONCILIATION-PLAN.md`**, which assumed the existing code was the foundation.
Rules authority remains `RULES.md` (rev 11).

Designed to be worked through across **many separate sessions**. Every packet in §5 is
self-contained: it names what to read, what it depends on, what to build, and how to know
it's done. See §6 for the cold-start protocol.

---

## 0. Where this is going

Stated by Nick on **2026-08-18**, after P8 made the game playable. The rewrite was scoped to
"a working game"; these are the four things it is a working game *towards*, in the order they
are wanted.

1. **Solo play against the computer.** The nearest of the four and the gateway to the rest —
   a game you can start alone, at any hour, with no other people. **P10 — ✅ done
   2026-08-18.**
2. **A console that is pleasant to sit at.** The UI stays terminal-based for a long time, and
   that is fine, *provided* the UX gets deliberate attention rather than being tolerated.
   **P11 — ✅ done 2026-08-18.**
3. **Strategy simulation at scale** — thousands of games run in parallel to compare ways of
   playing. **P12 — ✅ done 2026-08-18.**
4. **A multiplayer app**, where a lobby host fills empty seats with AI players. **P13.**
   ⚠️ **Restated 2026-08-19: this is one goal with the browser UI, not two.** Nick asked
   whether a rich browser UI or multiplayer should come first. They are the same track — see
   **§3.10** — because the only question that couples them is *where the engine runs*, and
   concealed hands with money on them settle that in advance. A browser client is a seat over
   a wire whether one person or four are using it, so **solo browser play is multiplayer with
   one connection** and there is no separate single-player UI to build and then replace.

**Goal 3 grew a tail rather than ending.** Simulation at scale is delivered, but having a
harness is what makes a *programme* of measurement worth running — hence **P14 (game journals,
✅ done 2026-08-19), P15 (a skill ladder, ✅ done 2026-08-19) and P16 (does the player before
you decide your game? ✅ done 2026-08-19)**, all hanging off P12. They serve the third goal;
none of them is a fifth one. **All three are now done, and the programme has an answer: no,
the player before you does not decide your game — see "What P16 found".** ⚠️ ~~**P13 is the
only outstanding packet in the plan.**~~ — **P13 shipped on 2026-08-19, and all four goals with
it.**

5. **A designed difficulty system, and a settled answer to what actually works.** Stated by Nick
   on **2026-08-19**, after all four goals landed, and it is **two jobs deliberately kept
   apart**: a *product* — difficulty as a first-class table setting a person chooses, per seat,
   in both front ends — and a *programme* — enough analysis, statistics and simulation to say
   which ways of playing are better, by how much, and with an interval. **P17–P23.**
   ⚠️ **Why it is one goal and not two:** the difficulty setting is only honest if it is
   calibrated by the programme, and the programme is only worth running if something consumes
   its answer. **§3.12 is the design decision they impose**, taken in advance: *difficulty is a
   dial, skill is a ladder, and they are not the same axis* — which is what stops the difficulty
   menu from being a list of research instruments, and what makes the difficulty system
   independent of whether any given rung turns out to be worth anything. 🔥 **P15 is the
   precedent that shaped it:** a plausible rung cost a whole packet and measured +0.5 ± 0.55
   points.

**What they demand of the architecture, and what already satisfies it.** These are stated
here because they change decisions taken *before* the packets that need them.

| Goal | What it needs | Where it stands |
|---|---|---|
| Solo play | A bot behind `IPlayerAgent` | ✅ **Delivered by P10.** `GreedyBotAgent` fills any seat the people do not; the console asks how many of them are breathing |
| Console UX | Nothing structural | ✅ **Delivered by P11.** And it needed nothing structural in the end: five presentation files in `BurmesePoker.Console` and **not one line of Domain or Sim changed** |
| Simulation | Determinism, no ambient randomness, no I/O, no static state, and **speed** | ✅ **Delivered by P12.** `BurmesePoker.Sim` runs seeded games in parallel with byte-identical results; ~20 ms a round, 50–90 rounds a second. No static state, and now a test that says so |
| Simulation, cont. | **Observability** — meaningful stats must be *derivable*, without the domain knowing what a statistic is | ✅ §3.8 held. Win rate, money split into flat and side bet, turns, how close the losers were, take and claim rates — **all derived by the harness, with no domain change whatever** |
| Simulation, cont. | **Durability** — a game worth keeping must survive the build that played it (§3.9) | ✅ **Delivered by P14.** Two decorators over `IPlayerAgent` and a JSON Lines format; the engine is unchanged, and a replayed run's CSV is byte-identical to the played one's |
| Simulation, cont. | **A design that can carry a controlled comparison**, not just a horse race | ✅ **Delivered by P16.** Seatings are enumerable rather than rotated, every row names who fed it, and effects are reported as a mean over games with an interval. ⚠️ The rotation could not have answered a question about neighbours *at any run size* — the cell was never played |
| Multiplayer | A decision on whether agents block | §3.6, taken now rather than discovered later — and ✅ **vindicated three times since**: P10, P12 and P16 all added callers to `IPlayerAgent` and none needed anything |
| Multiplayer + browser UI | A decision on **where the engine runs**, before a client exists | **§3.10, taken 2026-08-19.** Server-side, always. Concealment is a security property and a client-side engine cannot be made to honour it afterwards |
| Browser UI | Accessibility and render mode decided **before** the first component | **§3.11, taken 2026-08-19.** P11 proved *look* needs nothing structural; keyboard operability, focus and render mode are not look, and retrofitting them is a rewrite |

| Difficulty and strategy (goal 5) | **A ladder that is a research instrument and a menu that is a product, built from one mechanism and never confused for one another** | **§3.12, taken 2026-08-19, before P17.** The rungs are 0.0%/26.7%/36.1% apart and a lower rung plays a *different* idea rather than the right one badly — neither is what a difficulty menu needs |
| Difficulty and strategy, cont. | **Statistics honest enough to survive a round-robin** — intervals on every figure, paired comparisons, and a correction for the number of comparisons made | ✅ **Delivered by P17.** `tournament` ranks the field with a Holm-corrected verdict on every margin, `suite` generates `docs/strategy/measurements.csv`, and every figure in the ordinary report carries a 95% interval. 🔥 Adding the interval **moved a published number by a point** until the estimator was made a ratio over games rather than a mean of per-game ratios — see "What P17 found" |
| Difficulty and strategy, cont. | **A bot named in one place**, so a new rung reaches the console, the browser and the harness at once | **P18.** Today there are four independent notions of *which bot*, and the browser has no difficulty setting at all |

**All four are done, and none of them demanded architecture.** P12 was the one that could
have: it did not, being a fourth project that references Domain and asks it for nothing new.
**P11 asked for nothing either** — a whole UX pass, including a hint that has to agree with how
the bots play and a pause that must not exist in the domain, went in as presentation over the
seams that were already there. **P13 was the one that had always looked like it might change the
shape of things, and it changed the shape of the client instead** (§3.10, §3.11).

✅ **P17 shipped on 2026-08-19 and asked the engine for nothing either** — a fifth and sixth
verb, a ratio estimator, a family-wise correction and a generated measurements file, all over
seams P12 and P16 already had. `Simulator`, `GameRunner` and `Replay` are unchanged; so is
`BurmesePoker.Domain`.

⚠️ **Goal 5 is the first thing in this plan that is expected to ask the engine for something** —
P20's counting rung wants what `TurnContext` conceals and the rules do not (RULES.md §6.3 makes
the discards public, and P13.5's client already draws them to every watcher). That is one
addition of **public** information, guarded by the concealment tests P13.2 already shipped, and
it is the only one foreseen.

**The through-line: the domain never learns that any of this happened.** A bot, a simulation
harness and a network server are all the same shape — something that answers
`IPlayerAgent` and listens to `IGameObserver`. Nothing below §5's P10 belongs in
`BurmesePoker.Domain/Play/`.

---

## 1. Decision: rewrite the engine, salvage the tables

**Verdict: rewrite.** Keep roughly 180 lines of enums and lookup tables; rebuild the model,
engine, and loop.

This is not a close call, and the reason is the size: **the whole codebase is 758 lines.**
There is too little here for "keep and patch" to be the conservative option. Patching would
mean carrying three structural choices that actively fight the rules we have now pinned
down — and each has already produced a verified bug.

### 1.1 The three choices that have to go

**A. Cards are mutable and carry their own money status.**
`Card.MoneyCardStatus` has a public setter, mutated in place by
`Table.MarkDeckAndPlayerMoneyCards` across all 108 shared card objects. This has already
caused two confirmed defects: the clone bug (claiming the turned-up money card constructs a
**109th card**) and non-idempotent re-marking (a second call escalates every `MoneyCard` to
`DoubleMoneyCard`). Rounds repeat, so money cards must be re-designated every round — which
under this design means correctly resetting mutable state on 108 objects, every round,
forever. Holding designation as **round state computed from the turned-up cards** makes the
bug class impossible instead of merely fixed.

**B. There is no usable card instance identity.**
`Card.Id` is a `Guid` that nothing reads; all rule logic goes through `ValueEqualTo`. But an
exact-cover search **requires** instance identity — it must know that *this* 3♦ is not
*that* 3♦. The one place duplicates already matter, `FirstOrDefault` in run building, gets
it wrong (defect D4: holding two 3♦ generates only one candidate run). The identity exists
but is unused, and the distinction between value-identity and instance-identity is nowhere
expressed in the type system.

**C. `Joker` lives inside the rank enum.**
So `Ace + 1 == Joker`. The ace-wrap branch was only reachable "by accident of joker
filtering", as noted in `RULES-TECHNICAL.md` §6 — and that fragility is the proximate cause
of the **verified infinite loop** (a hand of A–K in one suit hangs until killed).

Two further problems are ordinary but real: the game loop interleaves `Console.WriteLine`
with logic and so **cannot be tested at all**, and both collection types inherit from BCL
collections (`Deck : List<Card>`, `PlayersInOrder : LinkedList<Player>` — where every access
goes through `ElementAt(i)`, making the LinkedList pointless).

Finally, the largest single file — `CardPlaysFactory`, 128 lines — needs replacing outright.
It is the wrong *shape* for the question the game actually asks (§3.4).

### 1.2 What is genuinely worth keeping

| Keep | Lines | Why |
|---|---:|---|
| `Common.cs` enums + display/order tables | ~170 | Boring, correct, tedious to retype. Rank/suit/colour display, glyphs, ordering. |
| `CardPlayFactoryTests` expectations | 35 | The two test cases are a **specification**, especially the 8-play joker enumeration whose comments document intended behaviour. |
| `UserPromptFactory` | 29 | Working Spectre.Console `SelectionPrompt`/`Confirm` patterns. Keep as reference. |
| `CardFactory` deck construction | 28 | Trivially correct; keep the shape. |
| Test project scaffolding | — | xunit wiring and `GlobalUsings.cs` carry over. |

**Discard from the build** (~400 lines): `Card`, `Deck`, `Player`, `PlayersInOrder`, `Table`,
`CardPlay`, `GameLoop`, `WinConditionObserver`, `CardPlaysFactory`, and the dead parsing half
of `Common.cs`.

The old code's real value was as evidence of intent — the `MoneyCardOwner = null` comment,
the test enumeration, the turn-flow skeleton. **That value has been extracted into
`RULES.md`.** What remains in the code is low.

### 1.3 Nothing is lost

Before restructuring, Packet **P0** commits the current tree and tags it. The old
implementation stays reachable in git forever. No `legacy/` folder is kept — it would only
invite a future session to "fix" code we have decided to retire.

---

## 2. Target architecture

Three projects. The split exists to make one thing impossible: the domain cannot reference
Spectre.Console, so game logic cannot print.

```
BurmesePoker.Domain/        pure rules. no I/O, no Spectre. 100% unit-testable.
BurmesePoker.Console/       Spectre.Console front end. the only project that prints.
BurmesePoker.Tests/         xunit against Domain's public API.
```

> **Simpler alternative, if three projects feel heavy for a side project:** one project with
> an `IGameObserver` interface and folder separation. It works, but nothing *enforces* the
> boundary, and an untestable loop is the specific failure this rewrite exists to fix.
> Recommendation: take the three projects.

Tests target a **public** domain API. `InternalsVisibleTo` goes away — its current placement
at the top of `WinConditionObserver.cs` is a latent hazard anyway.

```
BurmesePoker.Domain/
  Cards/       Rank, Suit, CardColor, CardId, Card, DeckBuilder, CardText
  Melds/       MeldKind, Meld, RunGenerator, SetGenerator, MeldCandidates, HandEvaluator
  Money/       MoneyCardRegistry, CardOwnership, Stakes, Settlement
  Play/        PlayerId, PlayerState, TableState, TurnAction, RoundEngine, MatchEngine
  Abstractions/ IPlayerAgent, IGameObserver
```

**Tooling.** The solution file is `BurmesePoker.slnx`, the XML format that is now the .NET
default (`dotnet new sln` emits it). All three projects target **`net10.0`**, matching the
installed SDK. Tests are **xunit v3 on Microsoft.Testing.Platform** — the test project is an
`Exe`, `global.json` opts `dotnet test` into MTP mode, and filtering uses `--filter-method` /
`--filter-class` rather than VSTest's `--filter`. Nick's standing preference is the newest
supported .NET tooling — prefer the newer option whenever this kind of question comes up
again, rather than the backwards-compatible one.

**As built by P0:** `Cards/Rank.cs`, `Cards/Suit.cs`, `Cards/CardColor.cs`,
`Cards/CardText.cs`, `Melds/MeldKind.cs`. **Added by P1:** `Cards/CardId.cs`,
`Cards/Card.cs`, `Cards/Deck.cs`, `Cards/DeckBuilder.cs`, `Cards/DeckExhaustedException.cs`
— the last is not in the sketch above but belongs to `Cards/`. **Added by P3:**
`Melds/MeldSlot.cs`, `Melds/Meld.cs`, `Melds/RunGenerator.cs`. **By P4:**
`Melds/SetGenerator.cs`. **By P5:** `Melds/MeldCandidates.cs`, `Melds/HandEvaluator.cs` —
which completes `Melds/`. **By P2:** `Money/MoneyCardRegistry.cs`, `Money/CardOwnership.cs`
and `Play/PlayerId.cs` — the last is brought forward out of `Play/` because ownership is
recorded against a player, and P2 needs the type. **By P6:** `Money/Stakes.cs` and
`Money/Settlement.cs`, which completes `Money/`. **By P7:** `Play/{TurnAction, PlayerState,
TableState, TurnContext, RoundResult, RoundEngine}.cs` and `Abstractions/{IPlayerAgent,
IGameObserver}.cs`, which completes `Abstractions/`. Only `Play/MatchEngine.cs` is still to
come, in P9. The old exe project is renamed `BurmesePoker.Console`; the test project references
**Domain only**, so nothing can accidentally test through the front end. **By P8:**
`BurmesePoker.Console/{Program, CardFormatting, SpectrePlayerAgent, ConsoleObserver}.cs` and
a `Spectre.Console` 0.57.2 reference — the one project that prints, and the one project that
knows Spectre exists. **A consequence worth stating: nothing in `BurmesePoker.Console` is
unit-tested**, by the same reference rule; P8 was verified by driving the binary. **By P9:**
`Play/MatchEngine.cs` — which **completes `Play/`, and with it every folder in the sketch
above**. **By P10:** `Melds/MeldIndex.cs` and `Melds/PartialCover.cs`, and the first folder beyond the
sketch — `Agents/GreedyBotAgent.cs`. `MeldIndex` is an extraction rather than an addition: the
candidate index `HandEvaluator` already built, now shared with the partial cover so the two
searches cannot drift apart. **By P12:** a fourth project,
**`BurmesePoker.Sim`** — `{Program, Simulator, GameRunner, SimulationOptions, SimulationReport,
StrategySummary, Strategy, SeedSequence, SeatRecorder, SimObserver, RoundAbandonedException,
Results, CsvReport}` — referencing Domain only, plus `Agents/{CoverScore, SimpleBotAgent}` in
Domain. ⚠️ **`BurmesePoker.Tests` now references Domain *and* Sim.** The rule that matters is
unchanged and is worth restating in its true form: **the test project never references
`BurmesePoker.Console`**, so nothing is tested through the front end. The harness is a headless
consumer whose determinism is itself an acceptance criterion, and a fifth project existing only
to test the fourth would buy nothing. **By P11:** five more files in `BurmesePoker.Console` —
`{Options, Palette, RoundLog, HandView, PacedAgent}.cs` — and **nothing anywhere else**. A whole
UX pass, including a hint that must agree with how the bots actually play and a deliberate pause
between computer turns, went in as presentation over seams that already existed. ⚠️ **The pause
is a decorator over `IPlayerAgent` in the console** (`PacedAgent`) and not a sleep in
`GreedyBotAgent`, which would have put a wait inside the simulation's hot loop. The test project
gained `LayeringTests` — Domain references neither Spectre nor `System.Console`, Sim references
no Spectre — which is the only mechanical check available to a packet whose subject is
unreachable from the test project. **By P14:** `Play/{GameJournal, JournalFormat}.cs` and
`Agents/{JournalingAgent, JournalPlayerAgent}.cs` in Domain, `Replay.cs` in Sim (which is both
`Replay` and `JournalReport`), and **no engine change at all** — `RoundEngine` and `MatchEngine`
are byte-for-byte what P9 left. ⚠️ **`System.Text.Json` is now referenced by Domain**, which is
the first framework assembly beyond the base library in there; it is a *string* API and the
layering rule it must not break is I/O, which it does not — `JournalFormat` returns
`IEnumerable<string>` and the two front ends own the `File` calls, exactly as `CsvReport` and
`CsvReport.WriteTo` already split. **By P18:** `Agents/BotCatalog.cs` in Domain — the ladder as `BotRung`s, named once for the
whole solution — and **nothing new anywhere else**: `Sim.StrategyCatalog` became an adapter over
it, the console's private `Difficulty` enum was deleted, `ComputerAdvice` and
`TableOptions.StandIn` ask it for the hardest rung, and `TablePlan` carries a rung's *name*.
⚠️ **`LayeringTests` gained the rule that goes with it** — nothing outside `Domain/Agents`
constructs a rung, scanned over every project but the test one, with the types taken from the
catalog so a fifth rung is covered the day it is added. **By P16:** `{SeatingPlan, Measurement, NeighbourExperiment,
NeighbourCsv}.cs` in Sim, one property on `SimulationOptions` and two columns on `CsvReport` —
**and nothing in Domain**, which now stands unchanged across three consecutive packets.

**✅ Two thirds built, by P13.1 and P13.2 on 2026-08-19 — the first structural change since
P12.** Two new projects were planned; **three exist or are planned, because the table server
turned out to need a home of its own**:

```
BurmesePoker.Presentation/  a hand as a view *model*: near-melds, per-card cost, display state.
                            Domain only — no Spectre, no HTML, no rendering technology at all.
                            ✅ BUILT (P13.1): {CardDisplayState, DisplayTokens, CardOrder,
                            CardView, MeldView, HandView, ComputerAdvice}.
BurmesePoker.Server/        one table, hosted: a seat somebody plays from elsewhere, and the
                            fan-out that decides what each viewer is told. Domain +
                            Presentation, and **no transport at all**.
                            ✅ BUILT (P13.2): {TableSession, TableSeat, TableOptions,
                            TableFanOut, TableEvent, SeatConnection, SeatPrompt, SeatQuestion,
                            SeatAnswer, RemotePlayerAgent, BoundedAgent, TableAbandonedException}.
                            ✅ EXTENDED (P13.6): TableSession learned who is sitting where —
                            SitDown/StandUp/WaitingFor/IsFull — and TableEvent gained
                            SeatTaken/SeatLeft. **The claim is the table's, not the lobby's.**
BurmesePoker.Web/           Blazor Server. the second project that draws.
                            Domain + Presentation + Server.
                            ✅ BUILT (P13.3): {Program, TableHost, TableBoard, CardWords} and
                            Components/{App, Routes, _Imports, Layout/MainLayout,
                            Pages/{Table, Rules, Error},
                            Table/{TableView, SeatPanel, CardChip, RoundLogPanel,
                            SettlementPanel}} — every table component with its
                            own .razor.css (§3.11 C17), plus wwwroot/{theme.css, app.css}.
                            ✅ BUILT (P13.4): {SeatBoard} and
                            Table/{YourSeat, TurnPrompt, HandPanel} — the seat, and the four
                            questions as controls. TableBoard is the public game folded from a
                            watcher; **SeatBoard is one seat's, folded from its own connection**.
                            ✅ RESHAPED (P13.5): Table/{TableCentre, TableLegend, AboutTable}
                            added and every other table component rewritten — a felt of named
                            grid areas, seats at positions, one action bar, and a round log you
                            open. **No new route to anything**: TableView still takes the board
                            and YourSeat still takes the seat.
                            ✅ BUILT (P13.6): {Lobby, HostedTable, TablePlan} replacing
                            TableHost, and Pages/Tables.razor. A lobby holds tables by id;
                            /table/{Id} names one. **A SeatBoard belongs to a viewer**, so
                            TableView sits down, holds it, and stands up in Dispose.
```

**By P13.3:** the seventh project above, `BurmesePoker.Presentation/PacedAgent.cs` **moved out of
`BurmesePoker.Console`**, and one row in `LayeringTests`. ⚠️ **The test project now references
`BurmesePoker.Web`, and still never `BurmesePoker.Console`** — the rule was never *"no front
end"*, it is that nothing is tested **through** a front end. A Spectre console is an interactive
loop that needs a terminal and its verification is a pty; a component tree is data, and §3.11 A5
asks for a reflection test over it *by name*. ⚠️ **`PacedAgent` went to Presentation and not to
`Domain/Agents/`**, which P13.3 named first: the domain is the pure rules and a wall-clock sleep
is not one of them, and — the deciding argument — **`BurmesePoker.Sim` references Domain**, so a
sleep in there would be reachable from the hot loop P11 wrote a layering test to protect.
Presentation is reachable from both front ends and from neither harness.

⚠️ **Why the server is a project and not a folder in `BurmesePoker.Web`.** It has to be
reachable from `BurmesePoker.Tests`, and P13.2's whole claim is that the concealment is
mechanically checkable *before* any UI exists — a fan-out tested through a web project would be
tested after the fact. It references Presentation because a seat's prompt carries a `HandView`,
and it references **nothing that transports anything**: `LayeringTests` forbids Spectre,
ASP.NET, `System.Console` and `System.Net` in there, which is the assertion that Blazor Server
really is supplying the wire (§3.10 item 4).

**By P13.1:** the seven files above, plus `BurmesePoker.Console/HandPanel.cs` replacing the
console's own `HandView` — the console now renders a view model it did not build. ⚠️ **The test
project references Presentation**, which is the first time anything presentational has been
reachable from a test at all; `BurmesePoker.Console` remains unreachable and that rule is
unchanged. **By P13.2:** the twelve files of `BurmesePoker.Server` above and **nothing else** —
Domain, Presentation, Console and Sim are byte-for-byte what P13.1 left them, and the only
edits outside the new project are one line of `BurmesePoker.slnx`, one `ProjectReference`, and
a row in `LayeringTests`. ⚠️ **The test project now references Server too**, and the rule that
matters is unchanged: **never `BurmesePoker.Console`**. `scripts/drive-console.py` is checked in beside them: a pty driver that captures
every byte a scripted match prints, which is how *"the console still plays exactly as it did"*
stops being an assurance and becomes a comparison.

⚠️ **`BurmesePoker.Presentation` exists because the browser client must not re-derive the
hand view.** `HandView` today answers *which cards nearly meld, and what does each cost to
throw* and then emits Spectre markup in the same breath. A Razor client wants the first half
and none of the second, and re-implementing it would put a second answer to a question
`PartialCover` already answers — the exact drift `MeldIndex` and `CoverScore` were extracted to
prevent. **The cheaper alternative is stated so it can be chosen instead:** let the Blazor
project reference `BurmesePoker.Console` and take `HandView` from there. It is rejected because
it would make a browser client depend on a Spectre console, and because `LayeringTests` exists
precisely to stop that class of reference. **A third option — let the browser re-derive it —
is rejected on drift.**

⚠️ **`BurmesePoker.Web` is the second project that may draw, and the layering rule generalises
rather than changes**: `BurmesePoker.Presentation` must reference **no** rendering technology,
which `LayeringTests` can assert exactly as it asserts Domain references no Spectre.

---

## 3. Key design decisions

Settle these once here so individual packets stay mechanical.

### 3.1 Two identity notions, both explicit

```csharp
public readonly record struct CardId(int Value);   // 0..107, assigned at deck construction

public readonly record struct Card(CardId Id, Rank? Rank, Suit? Suit, CardColor Color)
{
    public bool IsJoker => Rank is null;
    public bool SameValueAs(Card other) =>
        Rank == other.Rank && Suit == other.Suit && Color == other.Color;
}
```

A `record struct` gives value equality that **includes `Id`** — so `==` *is* instance
identity, and `SameValueAs` is the separate, explicitly-named value comparison used for
money-card designation. The distinction that the old code left implicit becomes impossible
to conflate. Fixes root cause **B**.

### 3.2 Jokers are rankless; ranks are numeric

```csharp
public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
                   Jack = 11, Queen = 12, King = 13, Ace = 14 }
```

`Rank?` is `null` for jokers, so joker-adjacency arithmetic cannot happen. Fixes root
cause **C**.

**Ace handling, stated precisely** (§6.1 of `RULES.md`): a run's ranks are contiguous, and
either ascend within `[2..14]` (ace high, only at the end) or begin with Ace treated as `1`
followed by `2,3,…`. **A run may never pass through Ace in the middle.** No wrap, by
construction rather than by a guard.

### 3.3 Money designation is computed, never stored on cards

```csharp
public sealed class MoneyCardRegistry
{
    public MoneyCardRegistry(IReadOnlyList<Card> turnedUp);
    public int Multiplier(Card card);   // 0 = none, 1 = money, 2 = double
}
```

A pure function of the two turned-up cards. Permanent money cards (7♦, A♠) are baked in;
doubling falls out of the overlap. Re-designating each round means constructing a new
registry — **idempotent by construction**. Fixes root cause **A**.

Ownership is separate and append-only:

```csharp
public sealed class CardOwnership
{
    public void RecordFromDeck(CardId card, PlayerId owner);  // deal or draw — both confer
    public PlayerId? OwnerOf(CardId card);
}
```

The method name is the whole rule: **`FromDeck` is the discriminator.** A deal and a draw both
call it; a pickup from a discard and a claim from the table never do (`RULES.md` §4.4).

Append-only is not an implementation convenience — it is **the rule**. Ownership is
**permanent and never transfers** (`RULES.md` §4.4): a money card you drew and later discarded
still pays *you* at settlement, even if an opponent is holding it. So there is deliberately no
`Transfer`, no `Clear`, and no removal. Cards taken from the table or a discard pile are simply
never recorded, and own nothing.

This encodes §4.4 in the shape of the API rather than in a comment, and it means settlement
never inspects hands at all — see P6.

### 3.4 Candidate generation vs. exact cover

The single most important structural point, and the reason `CardPlaysFactory` is replaced
rather than repaired.

```csharp
IEnumerable<Meld> MeldCandidates.For(IReadOnlyList<Card> hand);  // MAY overlap
bool HandEvaluator.IsWinning(IReadOnlyList<Card> hand);          // exact cover
```

Candidates **deliberately overlap** — the same joker instance is offered to a diamond run
and a heart run, and every sub-run of a longer run appears. That is correct for a candidate
generator; the cover search enforces disjointness itself, by `CardId`.

Run candidates are generated **directly by window**, not by the old greedy walk: for each
suit, each start rank, each length ≥ 3, and each assignment of positions to either the held
real card or a **specific joker instance**. This single formulation subsumes both the old
walk and the missing joker-substitution permutations, and the orphaned
`alternativePermutations` list (defect D2) never needs to exist.

> **Why joker substitution is load-bearing, not cosmetic.** From `2,3,4,J` the generator must
> also produce `2,3,J` (joker as the 4) and `2,J,4` (joker as the 3). These look redundant
> until you need an exact cover: **using the joker frees the real 4 for another meld.**
> Without them the evaluator rejects genuinely winning hands. This is exactly what the
> abandoned `CalculatePermutationsRecursively` was for — its purpose is documented in the
> existing test's inline comments.

Exact cover is recursive backtracking pinned to the **lowest uncovered `CardId`**, which
avoids re-exploring permutations of the same cover. At 13 cards the search is small; nothing
clever is required.

**When building candidate lists, store copies.** The deleted recursive method did
`results.Add(new CardPlay(type, currentPlay))`, aliasing a list mutated by backtracking.
Use `[.. currentPlay]`.

### 3.5 Domain drives; the console obeys

```csharp
public interface IPlayerAgent
{
    TurnAction ChooseAction(TurnContext context);
    Card ChooseDiscard(TurnContext context);
    bool ClaimTurnedUpMoneyCard(TurnContext context);
    bool Declare(TurnContext context);
}

public interface IGameObserver   // narration only; never asked for input
{
    void RoundStarted(int round, IReadOnlyList<Card> turnedUp);
    void PlayerDrew(PlayerId player, Card card);
    void PlayerDiscarded(PlayerId player, Card card);
    void RoundSettled(RoundResult result);
}
```

`RoundEngine` never prints and never reads the console. A `ScriptedPlayerAgent` in the test
project drives whole rounds deterministically.

> **As built by P7.** `IPlayerAgent` is exactly the four methods above. `IGameObserver` grew
> three events the sketch lacked — `PlayerTookDiscard`, `MoneyCardClaimed` and
> `PlayerDeclared` — because a pickup, a claim and a blind draw are three different things to
> a player and only one of them confers ownership. **Every observer method has a default
> no-op body**, so a front end implements only what it draws. The engine narrates
> *everything*, private information included: filtering per viewer is presentation, and
> pushing it into the domain would be the same mistake this rewrite exists to undo.
>
> **The event set is deliberately not closed** (§3.8): a later consumer may need an event that
> is not there yet, and adding one costs nothing because every method has a default no-op body.
> What it must not cost is *allocation* — an observer runs in the simulation hot loop, so an
> event passes what the engine already holds and never builds a list or a string to do it.

### 3.6 Agents are synchronous, and stay that way

Taken **2026-08-18**, ahead of P10, because every packet from here adds callers to
`IPlayerAgent` and the cost of changing it only rises.

`IPlayerAgent` returns values, not tasks, and `RoundEngine.Play()` blocks until the round is
over. A networked table wants a decision that arrives seconds later over a wire, and there are
two ways to have one:

- **Make the engine resumable** — turn `Play()` into a state machine that yields a "waiting for
  P3's discard" state and is fed the answer later. Correct in the abstract, and it is what a
  web request/response cycle would want.
- **Keep the engine blocking, and let the *agent* wait** — a `RemotePlayerAgent` blocks on a
  channel until the player's move arrives, and one table is one task.

**Take the second.** The first would rewrite `RoundEngine` and invalidate the scripted-round
apparatus that every P7 test and the whole of P12 depends on; the second costs one parked
task per table, which at this game's scale — four to six people, a handful of tables — is
nothing. A blocked task is not a blocked thread.

Two consequences worth stating now:

- **A remote agent needs a timeout, and the timeout policy is a bot** (P10). "Player timed out,
  play their turn for them" is exactly a bot move, which is another reason bots come before
  multiplayer rather than after.
- **Do not make the interface `async` "just in case".** It would infect `RoundEngine`, every
  test agent, and the simulation loop — where an `await` per decision across millions of
  decisions is pure overhead — to buy something §3.6 says is not needed.

### 3.7 Simulation is a first-class consumer, not an afterthought

Taken **2026-08-18**, ahead of P12. Running thousands of games in parallel asks four things of
the domain, and **three of them already hold**:

1. **No ambient randomness.** `RoundEngine.Shuffled` takes a `Random`; `Deck.Shuffle` takes a
   `Random`. `Random.Shared` appears **only** in `BurmesePoker.Console/Program.cs`. ✅ Keep it
   that way: a domain type that reaches for `Random.Shared` breaks reproducibility silently.
2. **No I/O.** The observer is optional and defaults to a silent one. ✅
3. **No mutable static state**, so that games in parallel cannot interfere. ✅ today — the only
   statics are immutable tables (`CardText`, `MoneyCardRegistry.Permanent`, `Stakes.Standard`).
   ✅ **Pinned by a test in P12** —
   `NeitherTheDomainNorTheHarnessHoldsMutableStaticState` walks both assemblies by reflection
   and fails on any static field that is not `readonly` or `const`. It pins the *reference*,
   not the object graph behind it: a `static readonly Card[]` passes and would still be shared,
   so an array that starts being written to is the one thing it will not catch.
4. ✅ **Speed — measured by P12, and it is not a problem.** `RoundEngine` calls
   `HandEvaluator.TryFindCover` after *every* discard by *every* player — it is how a
   declaration is offered only on a genuine win (P7). P5 measured three deliberately awful
   stress hands at ~100 ms *in total*. **P10 took the first end-to-end measurement: a
   bot-only round costs about 40 ms and runs 21–30 turns**, so a thousand rounds is under a
   minute on one core and the goal is not in danger. But it also **moved where the cost is**:
   a bot asks `PartialCover.Best` up to fifteen times per decision, so a strategy comparison's
   inner loop is the *partial* cover, not the evaluator.

   **P12's measurement pass, on an i7-1165G7 (4 cores, 8 threads), Release build:**
   `PartialCover.Best` is **140 µs** on a random thirteen-card hand and
   `HandEvaluator.TryFindCover` is **91 µs**, so a bot decision — a dozen-odd partial covers —
   is a couple of milliseconds and a whole round is **~20 ms**. A run does **~50 rounds a
   second serially and ~90 in parallel**; 2,000 rounds took 34 s. **Nothing was optimised, and
   nothing needs to be**: the goal in §0 is comparing strategies over thousands of games, and
   thousands of games is a coffee break. Should that change, the optimisation goes *around* the
   evaluator, never into it — `IsWinning` is the win authority (§3.4) and its answers may not
   change.

   ✅ **P21 collected on this, and the prediction was right down to the line.** A rung that
   counts live outs asks the partial cover a couple of hundred times a turn, so the cost of one
   answer became the packet. Profiling put **three quarters of it in candidate generation** —
   and not in generating candidates, but in a **fixed per-call cost that did not depend on the
   hand at all**: `RunGenerator` allocated all ninety rank windows afresh for each of four
   suits on every call, and both generators walked suits and ranks that could not possibly hold
   a meld. Precomputing the windows and skipping the impossible suits and ranks — **no change
   to what is generated, and the console's byte-for-byte capture proves it** — made
   `PartialCover.Best` and therefore *every rung and every engine turn* about **45% faster**,
   and it is the single largest speed-up the solution has had. ⚠️ **The lesson is that "the
   inner loop is the partial cover" was true and still misleading**: the inner loop of the
   partial cover was `new`.

   ⚠️ **One measured surprise worth keeping: the work is allocation-bound, not compute-bound.**
   Both searches allocate a meld index, a memo and a candidate list per call, and under the
   default workstation GC eight threads bought only **25%** more throughput than one. The
   server GC — one line in `BurmesePoker.Sim.csproj`, a harness knob rather than a domain
   change — took that to **70%**, and the rest is a 4-core laptop that cannot hold its turbo
   clock across eight threads. **If throughput ever does matter, allocation is the thing to
   attack**, and the first candidate is a per-turn cache in the bot rather than anything in
   `Melds/`.

### 3.8 Statistics are *collected*, never computed by the domain

Taken **2026-08-18**, alongside §3.7 and for the same reason: a simulation that runs a million
games is worthless if the interesting questions cannot be answered afterwards, and what makes
them answerable is decided long before P12.

**The domain gains no notion of a statistic.** No counters on `RoundEngine`, no `Stats` type in
`Domain`, no "record this for later" parameter anywhere. Everything a strategy comparison wants
is *derived* by the consumer from three seams that already exist — and the point of writing
this down is that **all three must survive the packets between here and P12**.

| What a strategy comparison wants to know | Where it comes from | State |
|---|---|---|
| Who won, how often, and for how much | `RoundResult` | ✅ |
| How long a round ran; how often the discard was taken over a blind draw; how often the turned-up money card was claimed; how often the deck was exhausted | the `IGameObserver` event stream | ✅ |
| How *close* the losers were when it ended | each loser's hand at round end, through the round's `TableState.Seats` | ✅ **only if the round's table is kept** — see P9 |
| What the money side-bet contributed against the flat round payment | `Ownership` + `Shoe` + the registry, exactly as P8's settlement report splits it | ✅ same condition |
| **Why a strategy chose what it chose** | *nothing today* — no seam carries the alternatives that were offered | ⚠️ see below |

**The four constraints that follow.**

1. ⚠️ **The round's `TableState` must remain reachable after the round ends.** Two of the five
   rows above die if `MatchEngine` hands back banks and nothing else. P8 already needs this for
   the settlement report, so **one mechanism serves both** — see the P9 amendment. This is the
   single most likely way to make the simulation goal expensive by accident.
2. **Per-decision introspection belongs in a decorator over `IPlayerAgent`, not in the
   observer.** A recording agent wraps a strategy, sees the exact `TurnContext` it was given
   and the answer it gave, and needs **no domain change whatever**. That is the seam for "why
   did it throw that away" — and it works for a human seat too, which is where P11's hints and
   a replay would come from. Do not grow `IGameObserver` an event per decision.
3. **The observer's event set is not closed — but it is hot.** Adding an event is cheap
   (every method has a default no-op, so nothing existing breaks). *Allocating* in one is not:
   a million rounds times fifty-odd turns means an event that copies a hand or formats a string
   costs real time. **Events pass what the engine already holds, by reference, and nothing
   more.** Presentation formats; the observer does not.
4. **A statistic is only meaningful with its seed and its seating.** Which strategy sat in
   which seat, and which seed produced the game, are the join keys for every result — and both
   are the harness's bookkeeping (§3.7), never the domain's.

> **One thing worth adding at the source rather than deriving.** Turn count is the most-wanted
> round statistic and the engine has it exactly, in the loop variable it already keeps;
> counting `PlayerDiscarded` events reconstructs it only for a consumer that attached an
> observer. **P9 should put `Turns` on `RoundResult`** — it is one field, it is not a
> statistic in the sense forbidden above, and it spares every future consumer a private tally.

### 3.9 A seed is a pointer; a journal is the artifact

Taken **2026-08-19**, after P11, in answer to a direct question: *what are we persisting?*

**Today, almost nothing.** The whole tree contains exactly one write to disk —
`CsvReport.WriteTo`, in `BurmesePoker.Sim`. Nothing else persists at all: `MatchEngine` keeps
no per-round history by design (§3.8), the console's standings live in a `List<RoundResult>`
that dies with the process, and a game played at the keyboard leaves no trace whatever. **There
is no persistence layer, and until now there has not needed to be one.**

**The reason there has not is that a bot game is a pure function of its seed.** P12 proved it to
the byte: `SeedSequence.GameSeed(master, index)` plus the seating plus the strategies reproduces
a game exactly, however the run was scheduled, and P11 gave the console the same property. At
that point a full log of a simulated game is *redundant at the moment it is written* — eight
bytes replay it.

**Three things break that, and all three are permanent.**

1. ⚠️ **A person is not a function of a seed.** `--seed` reproduces the deal, the seating and
   every bot; it cannot reproduce what the human did. **The one kind of game most worth keeping
   is the one kind a seed cannot recover.**
2. ⚠️ **A seed only replays against the code that produced it.** It is a pointer into a version
   of `GreedyBotAgent`, not a recording of a game. P12 already edited that class — extracting
   `CoverScore` — and a comparison run stored as seeds before an edit and replayed after it is
   quietly measuring two different games. **Seeds do not survive refactors; a journal does.**
3. **Decision-level analysis has no seam today.** §3.8's table has exactly one ⚠️ row — *why a
   strategy chose what it chose* — and it is still open. Aggregates in the CSV say a seat took
   the discard 41% of the time; nothing says what it was holding when it declined.

**The decision.** Keep both. **The seed stays the cheap handle and the journal becomes the
durable record**, and neither replaces the other. What follows from that:

- **The domain learns what a *recording* is, and never what a *file* is.** A journal is written
  by a decorator over `IPlayerAgent` and read by an agent that answers from it — the seam §3.8
  item 2 already named, and the shape `RecordingAgent` (tests) and `SeatRecorder` (Sim) have
  both already taken. **`RoundEngine` and `MatchEngine` do not change.**
- **Replay is a strategy, not a mode.** An agent that answers from a journal is just another
  `IPlayerAgent`, so replaying a game is playing it with different seats — no second engine, no
  resumable state machine, and it works for a human seat and a bot seat identically.
- ⚠️ **Fidelity is a throughput decision and must be measured, not assumed.** Recording the
  *answers* is a few bytes a turn and costs nothing. Recording the *hand at each decision* —
  which is what makes the log worth analysing — copies thirteen cards fifty times a round, and
  §3.7 measured this work to be **allocation-bound rather than compute-bound**. So the rich
  form is **opt-in and off in a throughput run**, and the packet that builds it measures the
  cost the way P12 did rather than guessing.

  ✅ **Measured by P14, and the guess above was wrong in an interesting way.** Serially — the
  only regime quiet enough to measure on this machine — 400 games ran at **46–49 rounds/s with
  no journal, 48–49 thin and 48–50 rich**: three interleaved repetitions, and the difference is
  inside the noise at *both* levels. The arithmetic says why. A thirteen-`CardId` copy is tens
  of nanoseconds against a `PartialCover.Best` that §3.7 clocked at **140 µs**, so the rich
  snapshot is about one part in a thousand of the decision it is recording. **The expensive
  axis turned out to be bytes, not time** — rich is 2× the file (9.6 KB a round against 5.0 KB)
  — so it stays opt-in for what it costs to *keep*, not for what it costs to *take*.
- **A journal joins to the CSV or it is an island.** Every record carries the same keys a CSV
  row does — master seed, game, game seed, round, seat, strategy (§3.8 item 4). ✅ **P14 made
  that literal:** a replayed run is summarised by the same code a played one is, so "it replays
  identically" is a `diff` of two CSV files rather than an impression.
- ⚠️ **A journal records the deal by seed, so nothing else may draw from the match's generator.**
  Discovered while building P14. The header carries the seed the match's `Random` was
  constructed with — that alone reproduces every shuffle *and* every mid-round reshuffle — but
  it only works if the first thing that generator is asked for is the first round's deal. **The
  console used to seat its table from the same generator**, so a replay would have dealt a
  different game; it now runs two generators out of the one seed, one for setup and one for the
  match. A consequence worth stating plainly: **a `--seed` from a build before P14 no longer
  plays the same console match**, which is this very section's point 2 happening to the front
  end rather than to a bot. The alternative — recording all 108 cards per round — was rejected
  because the reshuffle would still need the generator, and injecting a shuffle source into
  `RoundEngine` is the engine change this design exists to avoid.

**This is P14.** It is independent of P13 and can be taken first; §0's goal 3 is the one it
serves.

### 3.10 The engine runs on the server; a client is a view

Taken **2026-08-19**, after P16, in answer to a direct question: *rich browser UI first, or
multiplayer first — and are they related?*

**They are related, through exactly one variable: where the engine runs.** Everything else
about a browser client — what it looks like, what draws it, how it animates — is independent
of multiplayer and can be decided later. That one variable cannot.

**And it is already decided by wanting multiplayer at all.** A hand is **fully concealed** until
a declaration (`RULES.md` §7.1) and there is money on the table. P13 already recorded that a
client must never be *sent* what it may not see — **a security property, not a courtesy**. An
engine running in the browser holds every player's hand in every player's client; the DOM and
the WASM heap are both inspectable, and no amount of care in the view fixes it. **It is also
not retrofittable**: moving the engine to the server later changes the client's entire
relationship to the game, which is a rewrite of the client rather than a refactor.

**The decision.** The engine runs server-side for every front end except the local hotseat
console. **Blazor Server** is the first browser client. What follows:

1. ⚠️ **No WASM engine, ever.** `InteractiveServer` keeps the C# on the server and ships DOM
   diffs, so a seat's hidden state never leaves it. `InteractiveAuto` and WebAssembly remain
   available for **presentational components that hold no concealed state** — but the table and
   the hand are not those, and the burden of proof is on any component that wants to move.
2. ✅ **BUILT IN P13.2. A browser client is a `RemotePlayerAgent`, and §3.6 is what makes that
   cheap.** A remote player blocks inside the agent; the circuit pushes the answer into a
   channel. One table is one task, exactly as decided before P10 — and now demonstrated by P10,
   P12, P16 **and P13.2** each adding a caller to `IPlayerAgent` without the interface moving.
   **The bet is collected in full**: a seat played from somewhere else, with a bot standing in
   when nobody answers, cost the domain **not one line**.
3. 🔥 **Solo browser play is multiplayer with one connection.** There is no single-player client
   to build and then throw away: the bots fill the other seats exactly as `Program` does today.
   This is the whole reason the two goals collapse into one track.
4. ✅ **CASHED IN P13.2. Blazor Server supplies the transport, so there is no protocol to
   design.** This is the one thing the framework choice genuinely buys and it is worth naming:
   no bespoke wire format, no client-side state to synchronise, no serialising a `TurnContext`.
   **The "server" in P13.2 is a seat and a fan-out, testable in-process with no sockets at
   all** — and that is exactly what it turned out to be. `LayeringTests` now forbids ASP.NET and
   `System.Net` in `BurmesePoker.Server`, which is the claim stated as an assertion: **a test
   holds the very `SeatConnection` a browser circuit will hold.**
5. **The console is untouched and stays in-process.** It is a hotseat game on one machine;
   routing it through a server would be a downgrade bought with nothing.

⚠️ **What this does *not* decide.** Whether the eventual UI is Blazor or a JS client is still
open, and deliberately so. A JS SPA over the same server is a client-side change; the property
worth buying now is that the engine is on the server **whichever way that goes**.

### 3.11 The browser client's UX standards, taken in advance

Taken **2026-08-19**, alongside §3.10 and for a reason P11 makes precise.

**P11 proved that *look* needs nothing structural** — a whole UX pass went in as five
presentation files with no domain change. ⚠️ **That is true of look and false of accessibility
and render mode.** Keyboard operability, focus behaviour and colour-independence are decided by
*how the markup is built*, so retrofitting them is a rewrite of every component rather than a
pass over them. The render mode is worse: an app made interactive at its root cannot be made
selectively interactive later without moving every component in it.

So the standards are written down **before P13.3 exists** rather than discovered inside it. They
are split by how they are checked, because a standard nobody can verify is a wish.

#### A. Mechanically checkable — and therefore a test

1. ✅ **DONE IN P13.2 — no client is ever sent a card it may not see.** The strictest item and
   the most important. A seat's view model is built server-side; it must contain no `CardId`
   belonging to another seat's hand. **The test:** build the view model for every seat of a
   scripted round and assert each one's `CardId` set is disjoint from every other seat's hand.
   **Hiding a card with CSS is a leak**, and this is the test that says so out loud. **Shipped
   as `ConcealmentTests`, four assertions over one round played by four connected seats with a
   watcher**: pairwise-disjoint hands, no seat told what anybody else drew blind, a sweep over
   *every* card in *every* event against what that seat may see, and a watcher sent nothing but
   the public game. ⚠️ **It is the events rather than the view models that needed the test** —
   a `TurnContext` has no route to another hand, so the leak could only ever have been in what
   the server added. **Three of the five go red if the blind-draw filter is removed**, which is
   how it was checked rather than assumed.
2. ✅ **DONE IN P13.1 — colour never carries meaning on its own** (WCAG 1.4.1). The game leans
   on colour today — red and black suits, money-card and ownership markers. P11's `Palette`
   already pairs every one with a glyph; the browser must too. **The test:** every display state
   in the presentation view model carries a non-colour token, asserted over the enum so a new
   state cannot be added without one. **Shipped as `DisplayTokensTests`, one packet earlier than
   this list expected**, because `CardDisplayState` and `DisplayTokens` are exactly the enum it
   asserts over. ⚠️ **`Palette.OwnedMark` and `Palette.AdviceMark` are now aliases of
   `DisplayTokens`**, so the console and the browser cannot drift to two stars. **Three of these
   five items are left, all for P13.3** — items 3, 4 and 5 — now that P13.2 has taken item 1.
3. ✅ **DONE IN P13.3 — contrast is computed, never eyeballed** (WCAG 1.4.3 and 1.4.11): ≥4.5:1
   for body text, ≥3:1 for large text and for the boundaries of interactive components.
   **Shipped as `PaletteContrastTests`, and it reads `wwwroot/theme.css` — the file the browser
   loads — rather than a copy of the values in C#**, because a palette a stylesheet does not use
   is not the palette. 🔥 **The pairs are discovered rather than listed**: `--on-x` is drawn on
   `--x` and `--edge-x` is a line on it, and a token's base is the longest declared name its own
   name begins with, so `--on-raised-muted` is measured against `--raised`. **A token added later
   is measured without anybody remembering to add it to a list**, and one whose base does not
   exist fails rather than being skipped. Both themes are computed, and a third test asserts
   every `var(--…)` in every stylesheet names a token that exists — a typo in a custom property
   is silent in CSS.
4. ✅ **DONE IN P13.3, and it started biting in P13.4 — every action is a real control.** No
   `<div @onclick>`: buttons are `<button>` and navigation is `<a>`/`NavLink`. ⚠️ **Until the
   seat shipped it passed vacuously, because there was not one control on the table**; it now
   counts what it scanned and fails if the client goes quiet again. **A card you throw is a
   `<button>`.** **Shipped as a source scan in
   `MarkupStandardsTests`**, which walks back from every `@on…` to the tag it sits on.
   ⚠️ **The scan reads markup with the commentary stripped out** — every component in this client
   explains the standard it obeys in a comment, and a scan that read the prose would fail on the
   files that were most careful. Found by writing the scan and watching it fail on four of its
   own subjects.
5. ✅ **DONE IN P13.3 — anything holding a subscription disposes it.** **Shipped as
   `ComponentDisposalTests`**, reflecting over every `ComponentBase` in the client for a
   `TableHost`, `SeatConnection` or `TableSession` member — ⚠️ **including private ones, because
   that is what `@inject` generates** — and asserting `IDisposable`/`IAsyncDisposable`. A second
   test asserts the subscription is actually *unhooked*, since a `Dispose` that does nothing
   passes the first one and leaks exactly as much as no `Dispose` at all. ⚠️ **And never call
   `StateHasChanged` from `Dispose`** — scanned for, as well as stated.

#### B. Only playing it finds these — reviewed the way P11 was reviewed

6. ✅ **DONE IN P13.4, and re-verified against a ring of seats in P13.5 — fully playable from
   the keyboard, with no pointer at all.** ⚠️ **A ring of seats is exactly where tab order
   breaks**, so the felt is positioned with **CSS grid areas and never with `order:` or absolute
   coordinates**: the markup order is turn order, clockwise from your left and ending with yours,
   which is also the order the felt stacks in when it is too narrow to be a ring. There is no
   focusable control in a seat panel at all, so what that really protects is the order a screen
   reader reads the table in — and turn order is the one that means something at a card table. Tab order
   matches reading order; every prompt is reachable and answerable. For a turn-based card game
   this is not a concession — it is how a quick player will want to play anyway. **Reviewed the
   way P11 was: five rounds played in a headless browser with `Tab` and `Enter` and nothing
   else, 86 questions answered, 393 tab presses walking the hand.**
7. ✅ **DONE IN P13.4 — focus moves when the turn does.** When it becomes your turn, focus lands
   on the first control of the prompt; when a dialog opens focus enters it, and on close it
   returns to what opened it. **There is no dialog in this client and there does not need to
   be** — a question is a region of the page, not something modal over it. ⚠️ **Only the first
   control captures an element reference**: Blazor invokes a capture on *insertion*, not on
   every diff, so a `@ref` inside a loop holds the last card rather than the first.
8. ✅ **DONE IN P13.3, and asserted — it is the only live region on the page.** **The round log
    is an ARIA live region** (`aria-live="polite"`) — it is the browser's
   `ConsoleObserver.Say`, and this is WCAG 4.1.3 Status Messages. ⚠️ **Polite, never
   assertive**: a bot table emits a line every few hundred milliseconds and `assertive` would
   interrupt a screen reader continuously. **The hand is not a live region**; only the log is.
9. ✅ **DONE IN P13.5 — nothing meaningful lives in hover, or in a tooltip alone.** The per-card
   cost and the computer's hint are content, not chrome — P11 already treats them that way, and
   P13.5 shrank both to glyphs **on the card** rather than moving either into a tooltip. Board
   Game Arena says the same thing: a tooltip supplements and never duplicates. ✅ **Now a
   standard with a number on it**: `MarkupStandardsTests.NoParagraphOnTheTableIsAWallOfText`
   allows a visible paragraph on the felt **80 characters** of prose, and exempts exactly two
   things — a `<details>` the player can open, and a `<span class="said">`, which is the
   accessible name of the glyph. ⚠️ **Text moved into `.said` costs nothing against the budget,
   deliberately**: an icon with no accessible name is worse than the prose it replaced, and a
   budget that punished the fix would produce exactly that.
10. ✅ **DONE IN P13.5 — `prefers-reduced-motion` and `prefers-color-scheme` are both honoured.**
    The pause between computer turns is *pacing* and stays (it is what makes a bot seat legible —
    P11); anything that actually moves does not. **P13.5 added no animation at all**, and the
    blanket `prefers-reduced-motion` rule in `app.css` is there so that anything added later
    inherits the honouring rather than the habit. ⚠️ **It does not cross shadow boundaries**, so
    motion added inside a component's own stylesheet has to repeat it. Both themes are drawn and
    both are computed (A3).
11. ✅ **DONE IN P13.4 and raised in P13.5 — touch targets at least 24×24 CSS px** (WCAG 2.2 SC
    2.5.8), and comfortably larger for a card you are being asked to throw away for money.
    **Board Game Arena asks 32×32 and recommends 40–44, and P13.5 took the recommendation**: a
    card you press is 3.4rem × 4.4rem, every answer button is 44px tall, and the two disclosure
    summaries are 40px. A chip you are only shown stays 30px.

#### C. Blazor mechanics that are UX decisions in disguise

12. ✅ **TAKEN IN P13.3, and asserted.** ⚠️ **Static SSR is the default; interactivity is opted
    into per component. Do not put `@rendermode InteractiveServer` at the root.** The table and the prompts are interactive
    islands; the shell, the rules help and the settlement report are static. **This is the one
    decision on the list that cannot be walked back cheaply.**
13. ✅ **HANDLED IN P13.3 — `TableHost.Start()` is idempotent and locked.** **Prerendering runs a
    component twice.** `OnInitializedAsync` executes during prerender and
    again when the circuit starts, so anything that joins a table must be idempotent — or carry
    the prerendered result across with `PersistentComponentState`. Joining a table twice is a
    real bug, not a theoretical one.
14. ✅ **ASSERTED IN P13.3, and it caught something the plan did not predict** — see P13.3's
    findings. **`@key` on every card and every seat.** Without it the diff reorders DOM nodes and drags
    focus with them — and it shows up exactly when a hand is re-sorted after a draw, which this
    game does constantly.
15. ✅ **ASSERTED IN P13.3.** **Do not call `StateHasChanged` inside an event handler** — `ComponentBase` already
    re-renders after one. **Do call it, through `InvokeAsync`, when the observer stream pushes
    from a non-UI thread**, which is how every opponent's move will arrive.
16. ✅ **DONE IN P13.5 AND FINISHED IN P13.6 — reconnection is part of the UX, not an error
    path.** P13.5 owned the outage overlay; **P13.6 says it in the seat** — the felt marks a seat
    the computer is standing in at, and sitting down under a name already at the table takes that
    seat back, which is what makes a browser refresh put you in your own chair. ⚠️ **The marker
    is per turn** and is cleared at that seat's next `TurnBegan`: a player who timed out once and
    came back is not still away. A dropped circuit
    must not look like a dropped game. P13 already decides that a timeout is a bot move; the
    client has to *say* so rather than freeze and hope.
    🔥 **Blazor ships an overlay if you do not, and it is the ugliest screen in the app**: a
    hard-coded light-theme modal that ignores `prefers-color-scheme` and — the real problem —
    **leaves the page underneath live**. `main` is not inert, every button stays focusable and
    pressable, and focus never enters the dialog, so during an outage a player can tab to a card
    and press it and nothing happens and nothing says why. **P13.5 owns it**: the themed palette,
    a real `role="dialog"` that focus moves into, everything else on the page made `inert`, and a
    plain `<a href="">` to reload, which needs no circuit because there isn't one.
    ⚠️ **It is not a live region** — the round log is the only one (B8), and a second voice
    competing with it during an outage is exactly the wrong moment for two.
    ⚠️ **The element id and the four class names are Blazor's contract; the markup inside is
    ours.** The `inert` half needs JavaScript and is a vanilla script at the end of the body,
    deliberately not a component: *a circuit that has dropped cannot render the thing that says
    the circuit has dropped.*
17. ✅ **DONE IN P13.3** — every one of the five table components has its own `.razor.css`.
    **CSS isolation (`.razor.css`) over one global stylesheet**, so a component's styling cannot
    leak into the table.

⚠️ **One thing deliberately not carried over: `RoundLog`.** It exists because the hotseat console
clears the screen between turns; a browser client has its own scrollback and never loses it.
**Port the stream, not the panel** — the plan has said this since P11 and it is now a standard.

---

### 3.12 Difficulty is a dial; skill is a ladder; they are not the same axis

**Taken 2026-08-19, before P17, and it is the decision the whole strategy programme hangs on.**

Three of the four rungs the project already has were built to answer *which way of playing is
better*. It is tempting — and P11's console already does it — to hand those same rungs to a
person as a difficulty menu. **That is two different jobs wearing one name, and the ladder does
neither of them well as a menu.**

**A skill ladder is a research instrument.** Its rungs must differ in **exactly one decision**
(P15), so that a difference in results attributes to that decision and to nothing else. It has
no obligation to be evenly spaced, and it is not: at four balanced seats the rungs measured
0.0%, 26.7% and 36.1%. It has no obligation to be complete, either — a rung that plays
differently to no measurable effect (`cautious`) is a **result**, and results are kept.

**A difficulty dial is a product.** It must be **monotone** — level *n+1* beats level *n*, and a
person can tell. It must be **fine-grained enough to ask for "a bit easier"**. And it must
produce an opponent that reads as *a weaker player*, which a lower rung does not: `simple`
throws whatever card it reaches first, so it plays a **different and worse idea**, not the right
idea badly. A weaker human plays the right idea and slips.

**So the two are built from one mechanism and exposed as two things.** Difficulty is the
**strongest available rung with a mistake rate** — the ladder is what the mistakes are made
against, and it stays a research instrument. This has three consequences worth taking in
advance:

1. **The difficulty system does not depend on research succeeding.** P19 finishes it with the
   rungs that exist today. Every later rung raises the *ceiling* and moves the calibration; none
   of them is required for a person to get a good opponent. ⚠️ **This is the direct lesson of
   P15**, which spent a packet on a plausible rung worth +0.5 ± 0.55 points.
2. **A level is deleted if it is not separated from its neighbour.** Three real levels beat five
   imaginary ones, and a menu whose middle two entries are the same player is a lie told to
   every person who reads it.
3. **A mistake must be a plausible move.** Substituting a *random* legal move produces a bot
   that throws jokers away, which no person does and which reads as broken rather than as weak.
   The mistake is the next move down the agent's own ordering.

**And one rule about every number this programme produces: a published figure carries the
command that made it and the games it came from.** `RULES.md` has provenance tags because a rule
without its source cannot be re-examined; a measurement without its origin is worse, because it
looks like a fact. `sim suite` exists (P17) so the documented numbers are **generated rather than
transcribed**, and P23 makes that a test rather than a habit.

---

### 3.13 A computer seat consents; consent is not desire

**Settled by P37, 2026-08-22.** `RULES.md` §3 step 2 says a seating is re-drawn *when the players
agree to it* (§9 #45). Building that raised one question the rules cannot answer and one the
architecture had to: **what does a computer seat do**, and **what shape is a question put to
everybody at once**.

🔥 **A computer seat consents, and that is a design decision rather than a rule.** A rung decides
about cards; *"shall we move seats"* is not a card decision. **A rung that answered it on some
invented basis would be a strategy claim nobody measured** (P15's discipline — two of the first
three research rungs returned nothing, and the discipline is what made that publishable), and **a
rung that abstained or refused would make §3's rule dead at every table with a computer at it** —
which at a solo table is every table. ✅ **§3 says *the players* agree, and a bot is not a player
in the sense the rule is about**, so this is recorded here rather than invented in `RULES.md`.
⚠️ **It lives on `IPlayerAgent` as a default implementation and on no rung at all**, so a rung
written next year is covered without anybody remembering it exists.

🔥 **Three answers, not two, because consent is not desire.** *Agreement* is somebody wanting a
thing and nobody objecting, and a yes-or-no question cannot express it: if a consenting bot
answers *yes*, an all-bot table re-seats itself every deal, which is the opposite of the rule.
`SeatingOpinion` is therefore `Consent` (the default), `Ask` and `Refuse`, and the engine's rule is
**one Ask and no Refuse**. ✅ **Silence is safe by construction**: a seat nobody is answering, a
table nobody is at and every bot in the game all consent, and consent moves nothing on its own —
so *fail closed* needed no clock and no timeout.

⚠️ **A public question is a standing answer, not a pending prompt.** Every other question blocks
one seat while the table waits (§3.6). This one is put to **every** seat, so a table that blocked
on it would spend one patience per seat settling a single question. A person says what they think
whenever they like — the control sits on the table beside the hand — and it stands on the seat's
`SeatChannel` until the engine asks between rounds, which **consumes** it. **One press moves the
seats once.**

🔥 **And it is public in the strong sense: every answer is broadcast the moment it is given.** A
`SeatPrompt` is seat-private by construction and a `TableEvent` is public by construction (P13.2,
P24.2), so a public question could not wear either type without breaking what the other one means.
It wears its own: `TableEvent.SeatingOpinionGiven`, `SeatingChanged` and `SeatingRefused`, asserted
by `ConcealmentTests` to carry no card, no hand and no rationale — **the one conversation at this
table that is heard in full by everybody, including the watcher who holds no seat.**

---

### 3.14 What a win *was* is a record; settlement is told, never made to remember

**Settled by P35, 2026-08-22**, building `RULES.md` §7.4 (a win from the initial deal pays ×2) and
§7.5 (a third consecutive win is paid entirely by the seat above the winner).

🔥 **The round payment has taken four qualifications in three days and a fifth should be expected.**
§7.2 step 1 was *flat* for twenty-four revisions; it is now not flat in **how** the winner won
(§7.3), not flat in **how many** are playing (§7.3), not flat in **when** they won (§7.4), and not
even a payment from everybody (§7.5). ✅ **So `Settlement.RoundPayment` takes a `Win` record rather
than a growing list of flags**, and `RoundResult` carries the same record — because two of the
three facts on it are things no consumer can re-derive from the cards.

🔥 **The division that made §7.5 buildable: `Settlement` is *told*, and never made to remember.**
A streak is a property of a sequence of rounds and settlement is a pure function of one. The count
lives on `MatchEngine.Streak`, where a sequence of rounds is owned, and is handed **down** to each
round as it is dealt — exactly as P33 handed the declared thirteen down rather than deriving
jokerlessness inside the settlement. ⚠️ **`Settlement` still takes no table, no player state, no
match and no history**, which is a parameter-list guarantee asserted by a test.

⚠️ **A round can now contain no turn at all, and that is the first change to the shape of a round
since P0.** §9 #38's recorded default makes §7.4 about *the dealt thirteen alone*, so
`RoundEngine.Play` offers the declaration before the first take. **`TurnNumber` 0 is now a real
value** — a question asked outside any turn — which was already true of P37's seating question and
is now true of a declaration too. Anything keying on `(Round, TurnNumber)` sees it: the journal
(where both live at turn 0, in the order they are asked), the console's turn heading, the server's
`TurnBegan`. ⚠️ **A rule that reaches a front end through a turn number is a rule that has to say
what 0 means.**

🔥 **And where a net delta is split, the split is asked for rather than re-derived.** The console's
settlement panel and the harness's per-seat CSV row both cut a net into *the round* and *the side
bet*, and both did it by assuming every loser paid the same amount — true from rev 1 until rev 27.
`Settlement.RoundPayments` is that column, computed once in the domain and read by both. ⚠️ **The
failure mode is silent by nature**: a split at the wrong place posts the difference into the
side-bet column, where every money measurement reads it, and the totals still add up.

---

## 4. Packet dependency graph

```
P0 ─► P1 ─┬─► P2 ──────────┐                        ┌─► P11  console UX ☑
          ├─► P3 ─┐        │                        │
          └─► P4 ─┴─► P5 ──┴─► P7 ─► P8 ─► P9 ─► P10┼─► P12  simulation ☑ ─┬─► P14  game journals ☑
                            P6 ─────┘               │                      └─► P15  skill ladder ☑ ─► P16  seating-order analysis ☑
                                                    │
                                                    └─► P13  ← everything left, and it is one line
                                                          P13.1  presentation view model   (no UI)
                                                          P13.2  table server              (no UI)
                                                          P13.3  a table you can watch     ← first UI
                                                          P13.4  a seat you can play       ← solo, in a browser
                                                          P13.5  a table, not a document   ← the layout pass
                                                          P13.6  the lobby                 ← goal 4

P15 ─┬─► P17  the tournament ☑ ┬─► P19  difficulty as a dial ☑ ← goal 5's product, finished here
     │   (stats + ranking)     ├─► P20  counting rung   (memory)   ☑ ─┐
P16 ─┘                         ├─► P21  outs rung       (lookahead)  ☑ ─┼─► P23  the standing answer ☑
          P18  one catalog ☑ ──┘   P22  prospector rung (the money)  ☑ ─┘   ← the last *planned* packet
                          P31  warden rung (the feeding ban) ☑     ← a second branch off `outs`

                    the experts, 2026-08-20/21  ←  not from §0, and not from the code
                              │
        ┌─────────────────────┼──────────────────────┬─────────────────────┐
        ▼                     ▼                      ▼                     │
   P25  win condition ☑  P26  the money         P27  the feeding ban ☑     │
        by table size         layer as it is         (a legal turn         │
        (§7.1.1)              (§4, ×3 and ×5)         changes)             │
        │                     │                      │                     │
        │                     │                      └─► P28  the claim, the permission, ☑
        │                     │                               and the seat you sit in
        │                     │                               (§3, §4.5)   │
        └─────────────────────┴──────────────────────────────────┬─────────┘
                                                                 ▼
                                                    P29  re-measure, under the ☑
                                                         rules as they are
                                                              │
                                                              ▼
   P30.1  a thorough code ☑ ──┐        ← Fable 5
          review               │
                              ▼
   P24.1  a journal for the ☑ ──► P30.2  conformance: the rules ☑   ← Fable 5
          hosted table                     as *played*
                                           │
                                           ├─► P31  warden — the feeding ☐   ← Opus
                                           │        ban as a weapon
                                           │        │
                                           └───────┴─► P32  five-handed is the ☐   ← Opus
                                                             default table

   P24.2  the computer's reasoning ☑  ← Opus 5, 2026-08-22 — asked for by Nick, from the browser
```

🔥 **P25–P29 are new on 2026-08-21 and they are the first packets in this plan that came from
neither §0 nor the code.** Four sessions with **Mya Lay and Aung Aung** on 2026-08-20/21 closed
twenty-three questions in `RULES.md` §9 and left **four settled rules with no implementation at
all** — the win condition by table size, the money layer's ×3 and ×5 with jokers permanent, the
feeding ban, and per-round seating with the claim's permission rule. ⚠️ **This is a different kind
of work from everything above it**: P11 through P23 added capability to a correct engine, and
**P25–P28 make a working engine play a different game.**

🔥 **A third branch opened on 2026-08-21, and it came from the owner rather than from §0 or from
`RULES.md`: verify, then arm, then move the table.** Three packets — **P30** conformance,
**P31** the offensive feeding-ban rung, **P32** five seats as the default — plus **P24.1**, split
out of P24 because P30's browser half needs a journal the browser has never had.

⚠️ **The order is deliberate and the reason is wall clock, not taste.** P31 adds a rung and P32
re-measures everything; `StandingAnswerTests` demands a published row for every rung, so **each of
them costs a full suite regeneration** (~2h45 at four seats, more at five). Running P31 before P32
means the new rung is measured at four seats — **comparable with every historical figure** — and
then the five-seat move measures a field that is already complete. 🔥 **Merging them into one
packet would save about three hours and cost the attribution**: a run that changed the table size
*and* the field at once could not say which of the two moved a number. **P29 is the packet that
just demonstrated why that matters** — its wrong prediction was only legible because nothing else
moved with it.

⚠️ **P30 goes first because it is the only one of the three that can invalidate the other two.**
If the engine or a front end is not playing by a Settled rule, then P31 is a rung built on a
misunderstanding and P32 is a long measurement of the wrong game.

🔥 **P30 is itself two packets and the review is the first of them** (owner's call, 2026-08-21):
**P30.1 a thorough code review, then P30.2 the conformance harness and the front-end tests — both
on Fable 5, with P31 and P32 back on Opus.** ⚠️ **The review is not ceremony in front of the
tests; it is the half that writes their list.** A conformance harness checks the rules somebody
thought to check, and every defect class this project has actually shipped — a predicate written
twice, a `switch` default that means something, a test that cannot fail — was found by reading,
not by running.

✅ **P25–P29 are all built as of 2026-08-21, and that branch is closed.** P29 regenerated
`docs/strategy/measurements.csv` under the four rules changes above — **91 measurements in
9,981 s, 4 rows reproduced, and those four are the ε constants a human chose.** 🔥 **Of the three
predictions this plan wrote down before the run, two held and one was wrong**; the wrong one
(`outs` narrowing) is the finding, because chasing it located an effect the plan had not
predicted at all: **the win condition levelled the bottom of the ladder instead of tilting the
top.** ⚠️ **Nothing here needs a follow-up packet** — P29 raised no rules question and changed no
play, and `RULES.md` stands at rev 24.

⚠️ **P25, P26 and P27 were independent of one another** and could be taken in any order or in
separate sessions — they touch `Melds/`, `Money/` and the turn respectively, and nothing crosses.
**P28 depends on P27** because the objection predicate *is* the feeding ban's predicate (rank
alone, `RULES.md` §9 #30) and writing it twice is the defect to avoid. **P29 depends on all
four**, because each of them changes what a round is worth.

✅ **P24.2 shipped on 2026-08-22, the same day Nick asked for it.** He was playing a five-handed
table, saw the hint arrow and went looking for the sentence behind it — this packet's acceptance
criterion arriving as a bug report. 🔥 **The arrow read as a promise**, and the promise is the half
of P24 that had never shipped: P24.1's journal landed inside P30.2, P24.2 did not. ✅ **Both halves
of P24 are now done.** The re-plan block in §5 was right on both of its bets: P31 had already built
half of build item 1 (**the keys were missing, not the ranking**), and P32's trap was real — the
closing clause reads the same `TableRules` the evaluator does, and is asserted at four seats and at
five because they are different games.

⚠️ **The paragraph below is the argument that kept it waiting, and it is kept because it was
right and is now spent.**

⚠️ **P24 was for a long time the only unbuilt packet, and the question was whether to build it
rather than when.** It was re-sequenced on 2026-08-21 and its own reason for going first is spent. It was
placed ahead of the §5.1 work deliberately — *"§5.1 filters the very ranking P24 renders, but it
is blocked on §9 #16–#19, and P24 is what makes that conversation productive."* **That conversation
happened and §5.1 is fully specified**, so the argument has expired. 🔥 **And the case for moving
it is stronger than the case for leaving it**: P24 explains *why the computer chose this card*, and
P25–P27 change what a good card is at three of the four table sizes, what a card is worth, and
which cards are legal to throw at all. **Shipping an explanation of decisions that are about to
change means writing the sentence twice and believing it once.** ✅ **Those decisions have now
changed and been measured**, so the objection that moved it has been discharged and the packet can
be taken as written. ⚠️ **This was a sequencing recommendation, not a decision taken** — P24's
scope was set by Nick on 2026-08-20 and whether to build it is his call.

🔥 **P29 left one candidate that is still not planned, and P30–P32 do not cover it.** `docs/STRATEGY.md`'s tables are
still *typed* out of `measurements.csv` by hand, which is the last transcription step in a chain
this project has otherwise mechanised twice (P18, P20, P23). P29 validated a scratch script that
re-derives §3's whole matrix and Holm column from the CSV — it reproduced the published text on
all fifteen rows before the new run replaced them — but the script is not in the repo. **A small
packet could make "generated, never transcribed" literally true rather than nearly true.**

⚠️ **P13.1–P13.4 are strictly sequential**, which nothing else in this plan has been. Each one
is the ground the next stands on: the view model before anything renders it, the seat and the
fan-out before anything connects to them, the render model and the accessibility decisions
before interaction, and interaction before a second person. **The one place to break the chain
if time runs short is after P13.4** — that is a finished single-player browser game, and P13.6
is the only part that needs another person to be worth anything. ⚠️ **P13.5 and P13.6 are not
sequential with each other and were deliberately swapped** (2026-08-19, on the owner's call): the
layout pass is markup, CSS and one presentation helper, the lobby is `TableHost`/`SeatBoard`
ownership, and taking the layout first means the lobby arrives at a table worth joining.

**P2, P3, P4 are independent of one another** — good candidates for separate sessions in any
order. P6 needs P1 and P2 only.

**P10 is the fan-out point.** Everything the three end goals need turns out to run through
bots: solo play *is* bots, a discard hint is the same scored search a bot uses, a simulation
is bots playing each other, and a network timeout is a bot taking over a seat (§3.6). After
P10, **P11, P12 and P13 are independent of one another** and can be taken in any order — or
not at all. **P11 and P12 are both done (2026-08-18).** ⚠️ **P12 opened a second branch rather
than closing one:** having a harness makes journals (P14) and a strategy-comparison programme
(P15 → P16) worth building, and all three hang off P12 rather than off P13. **P14 is done
(2026-08-19) and closed its branch without opening another** — it needed nothing from the engine
and asked nothing of the plan. **P15 is done (2026-08-19) and needed nothing from the engine
either**, but it did not leave P16 alone: it sized the upstream effect at several points and cut
the intervention P16 was counting on down to about half a point (see the amendment under P16).
**P16 is done (2026-08-19) and closed the branch**: the question has an answer with an interval
and a control, and nothing in the domain, the engine or the round row changed to get it.

⚠️ **The P12 branch reopened on 2026-08-19 as goal 5, and it is now the only live one.**
**P17 (statistics and ranking) and P18 (one catalog) are independent of each other**; both feed
**P19**, which finishes the difficulty product with the rungs that exist today. ✅ **P17–P23 are
all done, and the plan is finished.** The three research rungs were independent of one another and of
P19 and were droppable in preference order; all three were built. ⚠️ **Two returned nothing**
(`cautious` before them, then `counting` at `+0.3 ± 1.0` the wrong way), **`outs` separated at
`+3.1 ± 1.0`**, and **`prospector` answered a different question entirely** — the side bet is not
worth playing for at $5/$1 and is worth `+7.3 ± 3.3` a round at $5/$40 (P22, STRATEGY §10).
✅ **P23 closed the branch** on 2026-08-20 by re-calibrating against the ladder that actually
landed: **one ε moved** (`hard`, 0.5 → 0.4), the reference table went from steps of 8.2/4.3/10.3
to 7.9/6.7/7.7, and **59 of the suite's 77 rows reproduced byte-identically** while the seven that
moved were the dial and only the dial. It also made "a rung cannot be added without being
measured" an assertion rather than a default, and paid P22's bill by having each rung declare
which instrument settles it rather than by shortening a field by hand. 🔥 **The dependency that matters is P17 before P19**: a difficulty
ladder calibrated with the interval-free report today's harness prints would be a guess wearing
a number.
**P13 is now the only outstanding packet — the only one that would change the architecture,
and the only one that is purely optional.** ⚠️ **Stale since 2026-08-19: P13 is done, and so is
everything else through P23.**

🔥 **A third branch opened on 2026-08-20 and grew into five packets on 2026-08-21, and none of it
came from the plan — it came from four sessions with the experts.** **P25–P29** implement rules
that are **settled and unbuilt**: the win condition by table size (§7.1.1), the money layer's
permanent jokers and its ×3 and ×5 (§4), the feeding ban (§5.1), per-round seating and the claim's
permission rule (§3, §4.5), and then a re-measurement of everything, because every figure in
`docs/STRATEGY.md` was produced under rules this project no longer holds. **See §4 for how they
depend on one another** — P25, P26 and P27 are independent, P28 needs P27, P29 needs all four.

⚠️ **The sequencing note that used to stand here is spent, and is corrected rather than deleted.**
It read: *"§5.1 filters every agent's discard ranking, which is the very ranking P24 renders — so
P24 first means one small amendment later, and §5.1 first means waiting on a conversation. **P24
first.**"* **The conversation happened**, so waiting costs nothing, and the amendment is no longer
small: P25–P27 change what a good card is at three of four table sizes, what a card is worth, and
which cards may be thrown at all. 🔥 **Shipping an explanation of decisions that are about to
change means writing the sentence twice and believing it once.** ⚠️ **P24 is therefore recorded
after P29 as a recommendation, not a decision** — its scope was set by Nick on 2026-08-20 and
moving it is his call. **P24** still hangs off **P13.6, P14, P18 and P21**.

| Packet | Title | Depends on | Size |
|---|---|---|---|
| P0 | Restructure and salvage | — | S |
| P1 | Cards, deck, identity | P0 | M |
| P2 | Money designation and ownership | P1 | M |
| P3 | Run candidate generation | P1 | L |
| P4 | Set candidate generation | P1 | M |
| P5 | Exact-cover hand evaluator | P3, P4 | M |
| P6 | Stakes and settlement | P1, P2 | M |
| P7 | Round and turn engine | P5, P6 | L |
| P8 | Console front end | P7 | M |
| P9 | End-to-end play, remaining rules | P8 | M |
| P10 | Bot opponents — **solo play** | P9 | L — ☑ done 2026-08-18 |
| P11 | Console UX pass | P10 | M — ☑ done 2026-08-18 |
| P12 | Simulation at scale | P10 | L — ☑ done 2026-08-18 |
| P13 | **The browser client and multiplayer** | P10 | XL — **re-split 2026-08-19 into P13.1–P13.6** below |
| P13.1 | A presentation view model, rendered two ways | P10 | M — no UI; a fifth project |
| P13.2 | The table server | P13.1 | M — no UI; the leak test lives here |
| P13.3 | A browser table you can watch | P13.2 | L — the first UI; Blazor Server |
| P13.4 | A seat you can play | P13.3 | M — **solo browser play, complete** |
| P13.5 | **A table, not a document** — the layout pass | P13.4 | M — ☑ done 2026-08-19 |
| P13.6 | The lobby, and a second person | P13.4 (P13.5 ☑, so it joins a table worth joining) | M — §0's goal 4 |
| P14 | Game journals — record and replay | P12 | L — ☑ done 2026-08-19 |
| P15 | A skill ladder | P12 | M — ☑ done 2026-08-19 |
| P16 | Does the player before you decide your game? | P15 (**P14 ☑, so rich journals are available**) | M — ☑ done 2026-08-19 |
| P17–P23 | The strategy programme | P15, P16, P18 | ☑ all done 2026-08-19/20 — see §4's second graph |
| P24 | ~~The computer's reasoning, said out loud~~ — **split 2026-08-21 into P24.1 and P24.2** | — | ☑ **both halves done** — P24.1 2026-08-21, P24.2 2026-08-22 |
| **P24.1** | **A journal for the hosted table** | P13.6, P14 | S — ☑ **done 2026-08-21** · **Fable 5** — `TableOptions.Journal` opts a `TableSession` into `JournalingAgent.Wrap`; `TableSession.Journal()` hands the record back and the host writes the file (`--journal` on the Web, flushed after every settled round). Same format, replays identically |
| **P24.2** | **The computer's reasoning, said out loud** | P24.1 ✅, P18 ✅, P21 ✅, P31 ✅, P32 ✅ | M — ☑ **done 2026-08-22** · **Opus 5** — `IExplainsDiscards` (the described sibling of `IRanksDiscards`), `CoverScore.Scored` with `Ranking` defined as its projection, `AdviceRationale`, five gated paragraphs inside the browser's existing `<details>`, and `JournalDecision.Advice` + `DisagreedWithTheComputer`. One ranking serves the arrow, the sentence and the journal — asserted by `ComputerAdvice.RankingsBought`. Console untouched; capture byte-identical |
| **P25** | **The win condition is a function of the table size** | — | L — ☑ **done 2026-08-21** — `TableRules`, and the search carries the counts |
| **P26** | **The money layer as it actually is** | — | M — ☑ **done 2026-08-21** — eight permanent cards, ×3, and a ×5 that needs the round's ownership |
| **P27** | **The feeding ban** | — | L — ☑ done 2026-08-21 — **the first work since P0 that changed what a legal turn is**; a bot's cover count can now fall |
| **P28** | **The claim, the permission, and the seat you sit in** | **P27** ✅ | M — ☑ **done 2026-08-21** — a fifth `IPlayerAgent` question, the only one asked off turn; the seats re-draw every deal |
| **P29** | **Re-measure, under the rules as they are** | P25 ✅, P26 ✅, P27 ✅, P28 ✅ | L — ☑ **done 2026-08-21**: 91 measurements in 9,981 s, **4 rows reproduced**, two of three predictions held ⚠️ the suite is **2h45**, not five hours |
| **P30.1** | **A thorough code review** | — | M — ☑ **done 2026-08-21** · **Fable 5** — `docs/REVIEW-2026-08.md`: 37 triaged findings; its P30.2 buckets are P30.2's checklist |
| **P30.2** | **Conformance — the rules as *played*** | **P30.1**, P24.1 (browser half only) | L — ☑ **done 2026-08-21** · **Fable 5** — `RuleConformance` + coverage registry + both front ends driven to a declaration; R1 and R8 fixed; 29 review fixes landed |
| **P31** | **`warden` — the feeding ban as a weapon** | P27 ✅, P30.2 | L — ☑ **done 2026-08-22** · **Opus** — it **lost**, `−9.3 ± 1.0` against `outs`; the ban bites on 9.4% of turns and the rung is what failed |
| **P33** | **The clean bonus (§7.3)** | P31 ✅ | M — ☑ **done 2026-08-22 (Opus 5)** · jokerless pays ×2 at 2/3/4 and ×3 at 5+; **§10 #19 discharged** and the suite regenerated at four seats — **111 of 116 shared rows byte-identical, the 5 that moved are exactly the rows denominated in dollars a round** |
| **P32** | **Five-handed is the default table** | P30.2, P31 ✅, **P33 ✅** | L — ☑ **done 2026-08-22** · **Opus 5** — the standing set is five-handed; **123 measurements in 12,445 s**. 🔥 **P29's explanation is falsified**: the five-handed ladder is the four-handed ladder divided by 1.25 (median margin ratio **0.801** against a base-rate scale of 0.800), so removing §7.1.1's series requirement did **nothing** to `simple`'s gaps. ✅ Full crossing, **no ε moved**, four-handed set kept frozen |
| **P34** | **A front door, and docs that cannot go stale quietly** | — | S — ☑ **done 2026-08-23** · **Opus 5** — `README.md` is the only current-only document here; the three historical documents carry banners; **eight tests in `BurmesePoker.Tests/Docs/`**, each proved able to fail by mutating the document. 🔥 **The test count is discovered by reflection**, so a packet that adds a test and leaves the prose alone is a red build — and **only the first count and rev in each newest-first document are checked**, so the narrative keeps every superseded figure. ⚠️ **What was actually stale was the two documents written for people**: `PLAYING.md` quoted a four-handed reference table on a five-handed page, `RULES-PRIMER.md` carried four divergence tags closed at P25–P28 |
| **P35** | **The two scoring rules that reach outside a round** | P33 ✅, P32 ✅, P36 ✅ | L — ☑ **done 2026-08-23** · **Opus 5** — §7.4 and §7.5 played; **§10 #20 and #21 discharged**. 🔥 **§7.4 changed the shape of a round** — a dealt thirteen that already wins is offered the declaration before the first take, so a round can run **no turns at all** — and 🔥 **`MatchEngine.Streak` is the first state in this game that reaches across rounds and is not money**. ✅ **107 of 124 shared rows byte-identical**; nine rounds in 33,008 ended on the deal and **no win rate, margin or ε moved**. ⚠️ **§7.5 is not in the standing set and cannot be** while every experiment plays one round a game |
| **P36** | **How long a seating holds** | — | S–M — ☑ **done 2026-08-22 (Opus 5)** — `Domain/Play/SeatingPolicy.cs`: **held by default**, `RoundsBetweenSeatings` of *N* re-draws every *N* rounds, and **0 is never**. §10 **#22 discharged**; `RuleConformance`'s seating check inverted; two fences named for §9 #45 and #47. ✅ **No measurement moved** and it is asserted (`AOneRoundGameIsTheSameGameUnderEveryPolicy`). ⚠️ **A seed or journal from between P28 and P36 replays differently**; the journal header records the policy so a front end's does not |
| **P37** | **Asking the table to change seats** | **P36 ✅** | M — ☑ **done 2026-08-22 (Opus 5)** — §10 **#23 discharged**, and §9 #45 as Nick ruled it: a re-seating happens **when the players agree**. 🔥 **Consent is not desire** — `SeatingOpinion` is three answers, so an all-bot table never re-seats itself and *fail closed* fell out for free. ⚠️ **A public question is a standing answer, not a pending prompt** (§3.13); **six decorators had to forward the first default interface member this project has.** 🔥 **The first *public* question this project has ever asked** — `SeatPrompt` is seat-private by construction and this one is put to everybody at once — and the first asked **between** rounds. ✅ **A computer seat consents** (a design decision, recorded in §3, not a rule); ⚠️ **§9 #47 open — everybody or most?** |
| **P38** | **The rulebook — the game taught, not reconstructed** | — | M — ☑ **done 2026-08-23 (Fable 5)** — `docs/RULEBOOK.md`: the game as a board game ships it, in reading order, for somebody who has never seen it. **One answer per rule, no provenance tags, no open questions, no packet numbers.** ⚠️ **`RULES.md` stays the sole authority** — the rulebook is *derived*, stamps the rev it was derived from, and a test binds the two, so a rules change makes it red rather than stale. 🔥 **The hard part is that eleven §9 rows are played on a recorded default**: a rulebook must state one, which silently promotes a default to a rule for the reader — so it carries a short *house readings* appendix in a player's language |
| **P39** | **How to play well — the strategy guide a player can use** | — | S–M — ☑ **done 2026-08-23 (Fable 5)** — `docs/HOW-TO-PLAY-WELL.md`: what was actually measured, organised by decision, **the nulls given as much room as the margins** and the three bonuses stated as *unpriced rather than small*. ⚠️ **`STRATEGY.md` stays the measurement authority, untouched** — the guide quotes only `measurements.csv`-fenced figures, and the fence covers **verdicts** as well as numbers (a null that separates is a red build). 🔥 **A figure has one home, asserted as an absence**: `PLAYING.md` points at the guide and may contain no `±`, no reference-table quad, no headline pair. ⚠️ **Found on the way**: `PLAYING.md`'s difficulty prompt row still quoted the four-handed 36%/14% — unfenced because P34's regex targeted the other sentence |
| **P40** | **The game in Burmese — translated rulebook and strategy guide** | P38 ✅, P39 ✅, **the vetted Burmese text (Nick, outside the repo)** | S–M — ☐ **new 2026-08-23, at Nick's direction** — `docs/RULEBOOK.my.md` and `docs/HOW-TO-PLAY-WELL.my.md`: the two player-facing documents in the language the game actually comes from. 🔥 **The translation itself happens outside this repository** — Nick runs `docs/translation/PROMPTS.md` against Gemini/ChatGPT, cross-checks each model's output with the other, and hands the packet vetted Markdown; **the packet cannot start until that text exists.** ⚠️ **The fences fence what survives translation**: the rev stamp in Latin digits bound to `JournalHeader.CurrentRulesRevision`, and every figure, `±` interval, dollar amount and card symbol of the English source asserted present byte-for-byte in the Burmese — which is why the prompts forbid Burmese numerals. ⚠️ **Acceptance is a human read, not a test** — no test can read Burmese |
| **P41** | **The table shows what the rules make public** | rev 31 ✅ | M — ☑ **done 2026-08-23, same day it was added** — §5's browsable piles and §5.2's face-up taken cards reach every place a player sits. 🔥 **The engine needs no rule**: open takes and discards are public events by `CardId`, so the face-up set and every pile are folds over the event stream — no engine state, no journal change, **journals and a seeded CSV byte-identical, asserted not argued**. ⚠️ **Concealment is the constraint** — a blind-drawn card must never acquire the mark, mutation-proved beside `ConcealmentTests`. ⚠️ **`TurnContext` is deliberately not widened**: no bot changes, no measurement moves; a rung that reads the piles is a new rung and arrives measured. Discharges §10 #24; fences §9 #49 and #50; registry §5.2 → Checked, ceiling back to 6 |
| **P42** | **Playtest readiness: the console's fifth seat, the ×5 said out loud, a played round in a real browser** | P41 ✅ | S–M — ☐ **new 2026-08-23, at Nick's direction** — three non-rules gaps before real people sit down. **(1)** The console seat prompt defaults `MinimumPlayers` → `DefaultPlayers` (P32's leftover, absorbed from candidate 3) with a `drive-console.py` re-capture. **(2)** The ×5 jackpot is settled and never shown: the domain carries the jackpot fact on the result (it is not watcher-computable — ownership is partly private), both settlement panels say it, the table centre notes the pair when it is up, `CardDisplayState` deliberately stays ×5-free, **§9 #32 not generalised**. Domain is touched, so **P41's byte-identity procedure is repeated**. **(3)** The session itself plays a browser round to settlement with Claude in Chrome (real browser, never headless) against a checklist and reports what was exercised |

⚠️ **P25–P29 are the only packets in this table that do not descend from §0.** They come from
`RULES.md` — four sessions with Mya Lay and Aung Aung on 2026-08-20/21 that closed twenty-three
open questions and left four settled rules with no implementation. **P11–P23 added capability to a
correct engine; P25–P28 make a working engine play a different game.**

---

## 5. Work packets

### P0 — Restructure and salvage

**Goal.** A three-project solution that builds green and contains no retired code.

**Read first.** This document §1–2.

**Steps.**
1. `git add -A && git commit` the current tree (the `Logic/Factories/` move plus the docs),
   then `git tag pre-rewrite`. **Do this before deleting anything** — `docs/` is currently
   untracked and would otherwise be lost.
2. Create `BurmesePoker.Domain` (classlib), retarget `BurmesePoker.Console` (the existing exe
   project, renamed), keep `BurmesePoker.Tests`. Wire project references and update the solution file.
3. Port `Common.cs` lines 26–197 into `Domain/Cards/CardText.cs` (display glyphs, display
   codes, rank ordering) and the enum files. **Drop** `DetermineCardRankSuitFromString`,
   `CardSuitFromChar`, `CardColorFromString`, `CardSuits_All`, `CardRankCodes_All` — all dead,
   and the first contains the verified always-throwing `[^0]` bug.
4. Delete `Models/`, `Logic/`, and the old `Common.cs`. Remove `InternalsVisibleTo`.
5. Delete the two existing tests. Their content is already captured — and corrected — in
   `docs/spec/RUN-CANDIDATES.md`, and they remain in git under `pre-rewrite`.

**Enum fates, decided in the P0 session.** Of the five enums in the old `Common.cs`:
`CardRank` → `Rank` (numeric, joker member dropped — §3.2); `CardSuit` → `Suit` (joker member
dropped, for the same reason and so that `Card.Suit` being `null` is the single joker
signal); `CardColor` carries over unchanged; `CardPlayType` → `Melds/MeldKind`;
`MoneyCardStatus` **dropped**, superseded by `MoneyCardRegistry.Multiplier` returning
`0/1/2` (§3.3); `PlayerAction` **dropped**, superseded by `TurnAction` in P7.

`UserPromptFactory` is **not** carried into the Console project despite §1.2 listing it as a
keep — step 4 deletes `Logic/` wholesale, and §1.3 rules out a `legacy/` folder. It survives
at the `pre-rewrite` tag, which is where P8 should read it from. Same disposition as the
kept-then-deleted tests.

**Acceptance.** `dotnet build` green. `dotnet test` green. No file references a deleted type.
`git tag pre-rewrite` exists.

> **Amended after the P0 session (2026-08-18).** The original acceptance said *"green with
> zero tests"*. A zero-test suite exits 0 but only prints *"No test is available…"*, and it
> would leave the freshly-ported `CardText` uncovered. P0 therefore ships `CardTextTests` —
> 28 cases over the display, ordering and parse tables it ports. Coverage of what a packet
> writes belongs to that packet.

**Done when.** The solution is three projects and the only domain code is enums plus `CardText`.

---

### P1 — Cards, deck, identity

**Goal.** The card model and a 108-card deck, with both identity notions explicit.

**Read first.** §3.1, §3.2. `RULES.md` §2.

**Build.**
- `Rank`, `Suit`, `CardColor` — **already built by P0** in `Domain/Cards/`, in their target
  §3.2 shape: numeric ranks 2..14, no `Joker` member in either `Rank` or `Suit`. Nothing to
  do here beyond using them.
- `CardId`, `Card` as `readonly record struct` (§3.1). Jokers carry `Rank = null` **and**
  `Suit = null`, and are distinguished from each other by `Color` plus `Id`.
- `DeckBuilder.BuildTwoDecks()` → 108 cards, `CardId` 0..107, sequential. Each deck is
  52 ranked + 2 jokers (one red, one black).
- `Deck` as a **plain class wrapping a list** — *not* a `List<Card>` subclass. Expose
  `DrawFromTop`, `DrawFromBottom`, `Count`, and `Shuffle(Random)`.
- Use `Random.Shuffle(Span<T>)`, **not** `OrderBy(r.Next())` — the old
  shuffle was not a uniform permutation.

**Acceptance tests.**
- Deck has exactly 108 cards; 4 jokers; 8 of each ranked value; all `CardId` distinct.
- `SameValueAs` is true for the two copies of 5♥ and false for 5♥ vs 5♦.
- `==` is false for the two copies of 5♥ (instance identity).
- Shuffling preserves the multiset of cards.
- `DrawFromTop`/`DrawFromBottom` reduce `Count` by one and return distinct ends.
- Drawing from an empty deck throws a **domain** exception, not `InvalidOperationException`
  from `.First()`.

> **Amended after the P1 session (2026-08-18).** Built as specified, plus four small
> additions the acceptance tests wanted and later packets will use:
> - `Card.Ranked(id, rank, suit)` and `Card.Joker(id, color)` factories. The positional
>   constructor stays public (§3.1), but the factories derive `Color` from `Suit`, so a card
>   whose colour contradicts its suit cannot be built by accident. **Prefer them everywhere.**
> - `Deck.TwoDecks()` — a shoe in one call, since `new Deck(DeckBuilder.BuildTwoDecks())`
>   appears in every test and will appear in `RoundEngine`.
> - `Deck.IsEmpty` and `Deck.Cards` (a **live**, read-only view, top first — copy it before
>   drawing or shuffling if you need a snapshot).
> - `Card.ToString()` renders `5♥` / `🃏Red` via `CardText`, so assertion failures are
>   readable. It is debug text, not front-end formatting; P8 owns display.
>
> `DeckExhaustedException` lives in `Domain/Cards/` and derives from `Exception` directly,
> **not** from `InvalidOperationException` — the point of the acceptance criterion is that a
> caller can tell an empty draw pile (a real game situation, `RULES.md` §5) apart from a
> programming error.

**Done when.** All of the above pass and no other packet's types are needed.

---

### P2 — Money designation and ownership

**Goal.** Round money-card designation and blind-acquisition ownership, both immutable-friendly.

**Read first.** §3.3. `RULES.md` §4 in full — especially §4.2 (exact match), §4.4 (ownership).

**Build.**
- `MoneyCardRegistry(IReadOnlyList<Card> turnedUp)` with `int Multiplier(Card)`.
  Permanent: 7♦ and A♠. Designation is by **`SameValueAs`** — exact rank *and* suit, so a
  turned-up 5♥ designates only the two 5♥ copies (`RULES.md` §4.2).
- `CardOwnership` with `RecordFromDeck(CardId, PlayerId)` and `OwnerOf(CardId)`.
  Called on **deal and draw**; never on pickup or table-claim (`RULES.md` §4.4).

**Acceptance tests.**
- 7♦ and A♠ are money cards with no turned-up cards at all.
- A turned-up 5♥ designates both 5♥ copies and **neither** 5♦ nor 5♠ (guards against the
  rejected rank-matching reading).
- A turned-up 7♦ yields `Multiplier == 2` for both 7♦ copies.
- Two turned-up cards that are value-equal to each other still yield `Multiplier == 1`, not 3
  (`RULES.md` §4.1 — doubling is the ceiling).
- **Constructing a second registry from the same turned-up cards yields identical results** —
  the regression test for the old non-idempotence bug.
- A card never recorded has `OwnerOf == null`.
- Ownership is write-once: re-recording a card that already has an owner is either rejected or
  a no-op — it must never transfer (`RULES.md` §4.4 rule 2).

> **Amended after the P1 session (2026-08-18) — a turned-up joker.** `SameValueAs` compares
> rank, suit **and colour**, so for ranked cards it is exactly §4.2's rank-and-suit match
> (colour is a function of suit). For a joker it also discriminates by colour — and nothing
> says what happens if one of the two turned-up money cards **is** a joker, which it can be:
> 4 of the 108 cards are jokers, so it happens roughly one round in fourteen.
> **Safe default: no special case at all.** Designate by `SameValueAs` like any other card,
> which makes a turned-up red joker designate the two red jokers and neither black one.
> Do **not** write a joker branch. Tracked as `RULES.md` §9 #11.

**Done when.** Designation is a pure function of the turned-up cards, with no mutation of any
`Card`.

> **As built (2026-08-18).** Both types are exactly as specified above. `Multiplier` is
> `(permanent ? 1 : 0) + (turnedUp ? 1 : 0)`, so doubling is the overlap and the ceiling falls
> out with no clamp. The permanent designators are two `Card`s with **negative ids**, compared
> by `SameValueAs` like any other designator — they are values, never dealt. `RecordFromDeck`
> re-recording the **same** owner is a no-op; re-recording a **different** owner throws
> `InvalidOperationException`, since one physical card cannot come off the deck twice.
> `CardOwnership.Records` is a live `IReadOnlyDictionary<CardId, PlayerId>` — **the surface P6
> settles from.** No joker branch was written, per the amendment above.

---

### P3 — Run candidate generation

**Goal.** All valid run candidates from a hand, including joker substitutions. The largest
single piece of rules logic.

**Read first.** **`docs/spec/RUN-CANDIDATES.md` — the worked specification for this packet.**
Then §3.2 (ace rule), §3.4 (why substitutions matter), `RULES.md` §6.1.

**Build.** `RunGenerator.Candidates(hand)` → `IEnumerable<Meld>`.

Generate **by window**, per §3.4: for each suit, each start rank, each length ≥ 3, and each
assignment of each position to either the held real card of that rank/suit or a **specific
joker instance**. Do not port the greedy walk.

**Rules to honour.**
- Same suit, contiguous ranks.
- **No wrap.** `A-2-3` ✓, `Q-K-A` ✓, `K-A-2` ✗ (`RULES.md` §6.1).
- Duplicate held copies each produce their own candidates (defect **D4**).
- Candidates may overlap across suits — that is correct here (§3.4).
- Deduplicate by **set of `CardId`**, never by display value. Carry one representative joker
  interpretation on each `Meld` for display, but never let it affect identity.

**Acceptance tests.** Full list in `docs/spec/RUN-CANDIDATES.md` §4.
- `2♦3♦4♦5♦` → 3 candidates. *Ports the existing passing test.*
- `2♦3♦4♦ + joker` → **5 candidates** (S1–S5 in the spec), **not 8**. The 2023 test asserts 8
  because it counted *joker interpretations* rather than card sets; 5 is the cover-relevant
  answer. **Read the spec before writing this test** — the distinction is the whole packet.
- `{2♦,4♦,J}` must be among them: the joker plays the 3♦ while the real 3♦ stays free. This is
  the case that makes the evaluator work.
- `A♦2♦3♦` → a valid run (ace low).
- `Q♦K♦A♦` → a valid run (ace high).
- `K♦A♦2♦` → **no** run containing all three.
- **A hand of A♦ through K♦ (all 13 ranks, one suit) terminates** and produces finitely many
  candidates. *Direct regression test for the verified infinite loop.*
- Two copies of 3♦ with `2♦4♦` → candidates using each copy distinctly.

**Done when.** All pass, including the **5**-candidate joker case.

> The count is 5, not 8. `docs/spec/RUN-CANDIDATES.md` §4 corrects the 2023 test, which
> counted joker *interpretations* rather than distinct `CardId` sets. This "Done when" line
> said 8 until the P0 session caught the contradiction with the packet's own body.

> **Amended after the P3 session (2026-08-18).** Built as specified. Four things later
> packets need to know, worked out in full in `docs/spec/RUN-CANDIDATES.md` §6:
> - **P3 also built `Meld` and `MeldSlot`**, which P4 and P5 share. A `MeldSlot` is
>   `(Card Card, Rank PlaysAs, Suit InSuit)` — for a joker, what it is standing in for. A
>   `Meld` is `Kind` plus slots; its **identity is `CardIds`**, and it validates only what is
>   universal (≥ 3 cards, no card twice). Run and set legality stay with the generators.
> - **Jokers are chosen as a set, not placed as a permutation.** The recursion may only take
>   jokers in ascending index order, which is what stops it generating each candidate `k!`
>   times.
> - **Two counts here were wrong and are now measured.** All thirteen ranks of one suit gives
>   **76** candidates, not the 77 the window arithmetic suggests: `A-2-…-K` and `2-…-K-A` are
>   the same thirteen cards. The joker-heavy worst case (`2♦…10♦` + four jokers) is **4,032**
>   candidates, not "hundreds" — thousands is the true bound, and it is inherent to the hand.
> - **All-joker melds are emitted**, following `RULES.md` §9 #8's *unlimited jokers*
>   recommendation, which rev 10 widened to name that case. P4 matches it; P5 must not assume
>   a meld contains a ranked card.

---

### P4 — Set candidate generation

**Goal.** All valid set candidates from a hand.

**Read first.** `RULES.md` §6.2 — **duplicate suits are forbidden** (confirmed by Mya Lay).

**Build.** `SetGenerator.Candidates(hand)` → `IEnumerable<Meld>`.

For each rank, every subset of ≥ 3 **distinct suits** drawn from held cards, plus each way of
filling absent suits with a specific joker instance.

**Rules to honour.**
- Same rank, **all suits distinct**. A set therefore holds **at most 4 cards**.
- `9♥ 9♥ 9♠` is **not** a set even though two decks make it holdable.
- Jokers substitute; each joker instance is distinct.
- Deduplicate by `CardId` set.

**Acceptance tests.**
- `9♥9♠9♦` → one 3-card set.
- `9♥9♠9♦9♣` → four 3-card sets plus one 4-card set.
- `9♥9♥9♠` (duplicate hearts) → **no** set. *Guards the confirmed rule.*
- `9♥9♠ + joker` → a valid 3-card set.
- No candidate ever exceeds 4 cards.

**Done when.** All pass.

> **Re-planned by the P3 session (2026-08-18).** P3 built the shared vocabulary, so P4 is
> smaller than it looks — reuse, do not re-invent:
> - **Return `IReadOnlyList<Meld>` built from `MeldSlot`s**, as `RunGenerator.Candidates`
>   does. A set's slot interpretation is the **suit** a joker plays as: the joker in
>   `9♥ 9♠ 🃏` records `PlaysAs = Nine, InSuit = Diamonds` (or Clubs — either, kept once).
> - **De-duplicate the same way**, with
>   `new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer())`. No custom comparer.
> - **Choose joker instances as a combination, not a permutation** — the same ascending-index
>   trick. Which joker fills which absent suit does not change the card set, so the naive form
>   emits duplicates and throws them away.
> - **Emit the all-joker set** (`🃏🃏🃏`), for consistency with P3 and `RULES.md` §9 #8.
> - **Two copies of one suit is not a wider set, it is two candidates.** `9♥9♥9♠9♦` yields the
>   sets that pick *one* of the two 9♥ — the duplicate-copy rule of defect D4 again.
> - Test hands come from `BurmesePoker.Tests/Hands.cs` — `Hands.Of("9H", "9S", "RJ")`, ids
>   assigned in the order listed. It exists already; extend it rather than writing another.
> - A **brute-force cross-check** over every subset of the hand was worth more than any single
>   count assertion in P3 — it catches both over- and under-generation. Sets are cheap to
>   check this way (same rank, distinct suits, jokers fill the rest). Write one.

---

### P5 — Exact-cover hand evaluator

**Goal.** The component the game has never had.

**Read first.** §3.4. `RULES.md` §6.3, §7.1.

**Build.**
- `MeldCandidates.For(hand)` — concatenates P3 and P4 output.
- `HandEvaluator.IsWinning(IReadOnlyList<Card> hand)` — true iff the hand partitions into
  disjoint valid melds covering **every** card exactly once.
- Also expose `TryFindCover(hand, out IReadOnlyList<Meld> melds)` so a declaration can be
  displayed and audited.

**Algorithm.** Recursive backtracking. At each step take the **lowest uncovered `CardId`**,
try every candidate containing it, recurse on the remainder. Pinning to the lowest uncovered
card prevents re-exploring permutations of the same cover.

**Acceptance tests.**
- A hand of `3+3+3+4` melds → winning, and `TryFindCover` returns 4 disjoint melds totalling
  13 cards.
- 13 unrelated cards → not winning.
- 12 meldable cards plus one orphan → **not** winning (partial cover must fail).
- **A hand that wins only if a joker substitutes for a card it holds** → winning. *This is the
  P3 joker-substitution rationale under test; if P3 is wrong this fails.* **Build it out of a
  set, not a run** — see the P5 note below.
- `TryFindCover` melds are pairwise disjoint by `CardId` and cover the hand exactly.
- Evaluation of a 13-card hand completes in well under a second.

> **Re-planned by the P4 session (2026-08-18).** P3 and P4 are both built, so
> `MeldCandidates.For` is now a concatenation of two `IReadOnlyList<Meld>`s — but not a naive
> one:
> - **The two generators can emit the same card set**, and `MeldCandidates.For` should
>   de-duplicate across them with the same
>   `new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer())`. It happens for any
>   meld holding **at most one real card**: `{9♦,🃏,🃏}` is a run (the jokers play the 10♦ and
>   J♦) *and* a set (they play the 9♠ and 9♥), and `{🃏,🃏,🃏}` is both trivially. Keeping both
>   is not wrong — identity is the card set, so the cover search would simply try the same
>   cover twice — but it doubles that branch of the search for nothing, and it makes
>   `TryFindCover` report a meld's kind arbitrarily. Keep the **run** interpretation, which is
>   the first one generated.
> - **Both generators already return `IReadOnlyList<Meld>`**, eagerly. Match that rather than
>   the `IEnumerable<Meld>` of §3.4.
>
> **Re-planned by the P3 session (2026-08-18).**
> - **Use `Meld.CardIds` and `Meld.Overlaps`** — both exist for this search. Nothing else on
>   `Meld` is identity; the interpretation on each `MeldSlot` is display only.
> - **Budget for thousands of candidates, not dozens.** A pathological hand (nine consecutive
>   cards of one suit plus four jokers) produces 4,032 run candidates on its own. Pinning to
>   the lowest uncovered `CardId` is therefore not just a de-duplication trick, it is what
>   keeps the search tractable: **index the candidates by `CardId` once** so each step tries
>   only the melds containing that card, rather than scanning the whole list.
> - **A meld may be nothing but jokers** (`RULES.md` §9 #8). Do not assume a ranked card.

**Done when.** All pass. `IsWinning` is the only win authority in the codebase.

> **Built by the P5 session (2026-08-18).** As specified, with three findings worth carrying.
> - **The "wins only if a joker substitutes for a card it holds" hand has to be a set.** With
>   a run it cannot be built at all: a joker can nearly always play *outward* from a run onto
>   a rank the hand does not hold — one below the bottom card, or one above the top — so there
>   is always a rival cover in which the joker substitutes for a card that is merely missing,
>   and a generator that only ever filled gaps would still find it. The escape closes only at
>   the ace, and blocking the other end just moves the boundary one rank along, all the way to
>   holding the whole suit. A **set** has no such escape, because it is capped at four suits:
>   hold all four suits of one rank plus a fifth copy (two decks) and a joker — six cards that
>   can only cover as two three-card sets — and whichever suit the joker plays, the hand is
>   holding it. That is the acceptance test. The run flavour is tested one level down instead,
>   on `MeldCandidates`: the five-card window `2♦ 3♦ [🃏] 5♦ 6♦` while the real 4♦ is melded
>   into a set of fours.
> - **`TryFindCover` returns *a* cover, not a canonical one**, and not the one a human would
>   draw. Thirteen hearts in sequence come back as `3+3+3+4`, because the search takes the
>   first candidate containing the lowest uncovered card. Nothing downstream may assume a
>   particular decomposition — see the notes on P8 and P10.
> - **Jokers make almost any hand winnable**, which matters when writing *negative* tests: with
>   two spare jokers any orphan finds a set, so a hand that must come back `false` has to be
>   joker-free (or joker-poor). Two of the first negative hands drafted for this packet were
>   winning by that route.
>
> **Measured.** The whole suite of three thirteen-card stress hands — including the
> 4,032-candidate one, and a two-deck hand holding every diamond twice — evaluates in about
> **100 ms** all told. Pinning to the lowest uncovered card is what makes that true; the
> memoisation of dead ends changed nothing measurable and is kept only as a bound on hands
> nobody has thought of.

---

### P6 — Stakes and settlement

**Goal.** Turn a finished round into money movements.

**Read first.** `RULES.md` §4.3, §4.4, §7.2 — including the worked example.

**Build.**
- `Stakes(int RoundValue, int MoneyCardValue)`, defaults 5 and 1.
- `Settlement.ForRound(...)` → per-player deltas.

**Rules to honour.**
- Every non-winner pays `RoundValue` to the winner. **Flat** — no per-card penalty exists
  (`RULES.md` §7.2).
- For each **owned** money card, its owner collects `MoneyCardValue × Multiplier` from
  **every other player**.
- **Iterate over ownership records, never over hands.** Ownership is permanent and
  non-transferring (`RULES.md` §4.4), so where a card *now sits* is irrelevant — a money card
  its owner discarded still pays them, and one an opponent picked up pays its original owner.
  Settlement should not look at a single hand.
- Money-card settlement runs regardless of who won; the winner participates.
- **No `Score` concept anywhere.** Money is the only ledger.

**Acceptance tests.**
- 5 players, $5/$1, no money cards → winner `+20`, each other `-5`.
- Winner + one player owning two money cards → that player `+8` from the side-bet; deltas
  across all players **sum to zero**.
- A double money card pays `2 × MoneyCardValue` per opponent.
- A money card **held but not owned** pays nothing, and pays its **original owner** instead
  (`RULES.md` §4.4).
- **A money card its owner discarded still pays that owner** — the headline test for the
  permanent-ownership rule. If settlement reads hands, this fails.
- A money card left undrawn in the deck is owned by nobody and pays nobody.
- Property test: deltas always sum to zero for any configuration.

> **Amended after the P2 session (2026-08-18) — settlement needs a card lookup.**
> `CardOwnership.Records` is keyed by **`CardId`**, but `MoneyCardRegistry.Multiplier` takes a
> **`Card`** (designation is by value). So `Settlement.ForRound` must be able to resolve an id
> back to a card, and needs the round's shoe passed in alongside the registry and the
> ownership. **`DeckBuilder.BuildTwoDecks()` returns the 108 cards with `CardId.Value` equal
> to the list index**, so the lookup is an array index, not a dictionary — but note that
> `Deck.Cards` is *shuffled* and therefore **not** index-aligned. Do not widen
> `RecordFromDeck` to take a whole `Card` instead; BUILD-PLAN §3.3 fixes its signature, and
> ownership is deliberately about the physical card, not its value.

**Done when.** All pass, including the §4.3 worked example reproduced exactly.

---

### P7 — Round and turn engine

**Goal.** A full round, driven by interfaces, with no I/O.

**Read first.** `RULES.md` §3, §5, §7.1. §3.5 above.

**Build.** `PlayerState`, `TableState`, `TurnAction`, `TurnContext`, `IPlayerAgent`,
`IGameObserver`, `RoundEngine`. **`PlayerId` already exists** — P2 brought it forward into
`Play/`; do not redefine it.

**Flow to implement.**
1. Setup: shuffle, deal 13 each — **recording ownership on every dealt card** (`RULES.md`
   §4.4, confirmed: the deal confers ownership) — then turn up bottom then top money card and
   construct the registry.
2. Opening turn only: offer the top money card. If claimed, **move** the actual card — never
   clone — and record **no** owner (`RULES.md` §4.5).
3. Each turn: take the previous player's discard *or* draw blind (recording ownership on a
   draw only — **never** on a pickup), then **discard**, then — if the remaining 13 satisfy
   `HandEvaluator.IsWinning` — lay them down and declare. **Order matters: discard first, reveal
   second** (`RULES.md` §7.1). **Discarding never affects ownership** (`RULES.md` §4.4).
4. Round ends on a declaration; hand off to `Settlement`.

**Constraints.**
- Player count configurable, **4–6** (`RULES.md` §2.1). No hardcoded roster.
- Nothing prints. No `Console` reference anywhere in Domain.
- Concealed play: no meld state on the table (`RULES.md` §6.3).

**Acceptance tests.** Using a `ScriptedPlayerAgent`:
- A scripted round reaches a declaration and produces the expected settlement.
- Hand sizes are invariant: 13 between turns, 14 mid-turn.
- Card conservation: **108 cards at all times**, across deck + hands + discards + turned-up.
  *Direct regression test for the clone bug.*
- Claiming the turned-up money card leaves the table with one fewer card and grants no
  ownership.
- Drawing a money card, then discarding it, then having another player pick it up leaves
  ownership with the **original drawer**.
- Setup rejects 3 players and 7 players.

**Open decisions.** Deck exhaustion and match end are **not** in this packet — see P9. The
domain exception already exists: P1 built `Cards.DeckExhaustedException`, which `Deck` throws
from both draw methods. **Let it propagate — do not invent a second one**, and do not catch it
until P9 implements the reshuffle.

**Done when.** A scripted round runs end to end in tests with no console involvement.

> **Amended after the P6 session (2026-08-18) — what settlement now demands of the engine.**
> `Settlement.ForRound(players, winner, stakes, moneyCards, ownership, shoe)` is built and is
> the only thing that moves money. Three consequences for `RoundEngine`:
> 1. **Keep the unshuffled shoe.** Settlement resolves an owned `CardId` back to a `Card` by
>    *index*, and validates that `shoe[i].Id.Value == i` — passing `Deck.Cards` throws, by
>    design, because it is shuffled. So hold on to the `DeckBuilder.BuildTwoDecks()` list the
>    round's `Deck` was built from and pass **that**. `Deck` copies its cards, so the builder
>    list is never disturbed by shuffling or drawing.
> 2. **One roster, used for both.** Settlement throws if the winner is not among the players,
>    if a player appears twice, or if an ownership record names somebody not at the table. The
>    list handed to settlement must be the round's seating.
> 3. **A round always has a winner.** There is deliberately no no-winner settlement — the round
>    ends on a declaration (P9 reshuffles rather than ending a round early).
>
> `Stakes` is a `sealed record` (never null, both values positive) with `Stakes.Standard` =
> $5 / $1. It belongs to the match, not the round: fix it once at setup and pass it down.

---

### P8 — Console front end

**Goal.** A playable game.

**Read first.** §3.5. Old `UserPromptFactory` (in git at `pre-rewrite`) for Spectre patterns.

**Build.** `SpectrePlayerAgent : IPlayerAgent`, `ConsoleObserver : IGameObserver`,
`CardFormatting` using the salvaged `CardText`, and `Program.cs` wiring.

**First step: add the Spectre.Console package back.** P0 carried the 2023 `Spectre.Console`
0.47.0 reference forward, but nothing referenced it once `Logic/` was deleted, so it was
dropped as dead weight. Add the current version — `dotnet add BurmesePoker.Console package
Spectre.Console` — rather than restoring the 2023 pin.

**Requirements.**
- Hand display sorted by the salvaged rank order, with money-card markers (`($)`, `($$)`).
- Prompts for: draw vs. pick up, which card to discard, claim the money card, declare.
- **Offer "declare" only when `HandEvaluator.IsWinning` is true**, and show the cover found.
  ⚠️ **`TryFindCover` returns *a* cover, not the tidiest one** (P5): a whole suit in sequence
  comes back as four melds, not one. If the declaration should read the way a player would lay
  it out, that is P8's own presentation problem — re-cover the hand preferring longer melds,
  or sort what comes back. Do not "fix" the evaluator for it.
- Configurable player count and stakes at startup.

**Acceptance.** Manual: `dotnet run --project BurmesePoker.Console` plays a full round to
settlement. No domain type references Spectre.

**Done when.** A round is playable start to finish and money changes hands.

> **Amended after the P7 session (2026-08-18) — what the engine actually asks the console.**
> The domain side is built and the console's whole job is to answer four questions and draw
> seven events.
> 1. **`RoundEngine.Shuffled(players, agents, stakes, random)` is the entry point**, and
>    `engine.Play()` runs the round. `Program.cs` picks the player count (4–6) and the stakes,
>    **randomises the seating itself** — the engine takes the list as given (RULES.md §3 step
>    2), because a round that reshuffled its own seating could not be scripted — and builds one
>    `SpectrePlayerAgent` per human seat.
> 2. **Prompt only when asked.** The engine asks `ChooseAction` *only* when there is a discard
>    to take, `ClaimTurnedUpMoneyCard` *only* on the opening turn, and `Declare` *only* when
>    `HandEvaluator` already says the hand wins. So none of those three prompts needs its own
>    legality check — offering them unconditionally would be the bug.
> 3. **`TurnContext` is the whole view a player gets**, and it is deliberately narrow: their
>    own hand, the available discard, the draw-pile count, the turned-up cards, the stakes, the
>    registry, and `YouOwn(card)`. There is **no route to another player's hand, to
>    `TableState`, or to `CardOwnership`** — a reflection test pins that. Money-card markers
>    come from `context.MoneyCards.Multiplier(card)`; "don't throw away your own money card"
>    hints come from `context.YouOwn(card)`.
> 4. **`IGameObserver` narrates private cards too** — `PlayerDrew` reports what an opponent
>    drew. **The console must filter**; the domain will not do it for you. Every method has a
>    default no-op, so `ConsoleObserver` overrides only what it draws.
>
> **Amended after the P6 session (2026-08-18) — settlement reports net deltas only.**
> `Settlement.ForRound` returns an unordered `IReadOnlyDictionary<PlayerId, int>` of **net**
> movements — one number per player, positive to collect. There is **no breakdown** of the
> round payment against the money-card side-bet, and no per-card detail. If the console wants
> to say *"−$5 for the round, +$3 in money cards"*, P8 builds that itself: the side-bet half is
> a short walk of `ownership.Records` through `registry.Multiplier`, exactly as settlement does
> it. Don't assume the domain hands it over, and don't widen `ForRound` for presentation.

> **As built (2026-08-18).** Four files in `BurmesePoker.Console`, none of them known to the
> domain: `CardFormatting` (glyphs, ordering, money markers, cover rendering),
> `SpectrePlayerAgent` (the four questions), `ConsoleObserver` (narration) and `Program`
> (setup, one round, settlement report). **Spectre.Console 0.57.2**, the current release.
> Decisions worth knowing:
> - **It is a hotseat.** Every seat is a person at one terminal, so a turn starts by clearing
>   the screen, naming the player and waiting for *"are you at the keyboard?"*. The handover
>   fires on whichever question comes first and is keyed on `TurnContext.TurnNumber`, because
>   the number of questions in a turn varies.
> - **`PlayerDrew` narrates the player, not the card** — the domain hands over private
>   information by design and the console filters it. Pickups, claims, discards and
>   declarations are public and printed in full.
> - **The money star is only ever on a money card.** Everything dealt is owned, so starring
>   ownership alone marks the whole hand; what a player needs is which of the cards that *pay*
>   pay them.
> - **The cover is sorted, not recomputed** — longest meld first. Re-covering the hand to lay
>   it out the way a player would remains unbuilt and unneeded.
> - **Manual verification, since Tests references Domain only**: Spectre needs a pty, so
>   `script -qec "dotnet run --project BurmesePoker.Console" /dev/null < keys` drives it;
>   piped stdin cannot work (`Console.ReadKey` throws on redirected input) and `Program` says
>   so instead of crashing. A throwaway scratch harness linking the test project's
>   `DealBuilder`/`ScriptedPlayerAgent`/`Hands` drove a real `SpectrePlayerAgent` to a
>   declaration, which is how the declare prompt and the settlement table were seen.

---

### P9 — End-to-end play and the remaining rules

**Goal.** A complete multi-round game. **No open rulings — fully unblocked.**

**Read first.** `RULES.md` §5, §7.2. Here: **§3.8**, which adds two requirements to this
packet — surface each round's `TableState`, and put `Turns` on `RoundResult`.

**Build.**
- **Deck exhaustion (settled, `RULES.md` §5):** when the draw pile empties, gather **all**
  discard piles, shuffle, and make that the new draw pile. Rare in practice but must not crash.
  Per-player piles are fine — gather them all at exhaustion.
- ⚠️ **Ownership is write-once across a reshuffle.** A discarded money card can be redrawn by
  someone else; its **original** owner keeps it (`RULES.md` §5 note). P2's write-once
  constraint is what makes this correct — do not weaken it here.
- `MatchEngine`: repeated rounds, **re-designating money cards each round** by constructing a
  fresh `MoneyCardRegistry` — trivially correct under §3.3, where the old design's
  non-idempotence bug lived.
- **No automatic match end** (`RULES.md` §7.2): rounds repeat and banks carry over
  indefinitely. Provide a "stop playing" action and show standings on exit. Do **not** invent
  a target score or round limit.

**Acceptance tests.**
- A scripted 3-round match settles each round, with banks carrying over correctly.
- Money is conserved across the whole match: all player banks sum to the starting total.
- Money cards are re-designated each round and do **not** accumulate multipliers.
- **Deck exhaustion mid-round reshuffles the discards and play continues** — no crash, no lost
  cards, still 108 in total.
- **A money card discarded, reshuffled, and redrawn by another player still pays its original
  owner.**

**Done when.** A full match is playable and banks reconcile.

> **Amended after the P7 session (2026-08-18) — ⚠️ the reshuffle goes *inside* the engine.**
> P7 left deck exhaustion to propagate, and it is worth being precise about what that means:
> `DeckExhaustedException` comes out of `RoundEngine.Play()` from the middle of a turn, after
> the player has been asked for a decision and before they have a card. **There is no resume
> point**, so P9 cannot implement §5 by catching it around `Play()` — that would abandon a
> round that is still legally in progress. The reshuffle belongs at the one place that draws:
> `RoundEngine.TakeCard`. Gather every `PlayerState.Discards` pile, shuffle, and make that the
> new draw pile *before* drawing, and the exception becomes what it should be — the signal
> that there is genuinely nothing left anywhere, which is a real end state, not a crash.
> Two details that fall out:
> - **Only the top discard is takeable, so gathering all the piles is safe** — but the discard
>   a player is about to be offered must not be swept into the deck mid-turn. Gather at the
>   moment of drawing, and the current top discard is still where it was.
> - **`TableState.AllCards` is the conservation test.** It already spans draw pile, hands,
>   discards and the turned-up cards; the reshuffle must leave it at 108 distinct cards, and
>   P7's tests show the shape to copy.
>
> `MatchEngine` otherwise has little to do: **a new `RoundEngine` per round re-designates the
> money cards for free**, because the registry is built in `TableState`'s constructor from that
> round's turned-up cards and nothing is ever mutated. Bank the `RoundResult.Payouts` and go
> again. A `RoundEngine` plays exactly one round and refuses a second `Play()`.
>
> **Amended after the P8 session (2026-08-18) — what the console now needs from a match.**
> The front end exists and works a round at a time; three things follow for `MatchEngine`.
> 1. ⚠️ **The console's settlement report needs each round's `TableState`.** `RoundResult`
>    carries net deltas only, and splitting them into the round payment and the money-card
>    side bet needs `Ownership` and `Shoe`, which live on the table. Today `Program` reads
>    `engine.Table` after `Play()`. **So `MatchEngine` must surface the round's table** — the
>    cheapest shape is a per-round result that pairs the `RoundResult` with the `TableState`
>    it settled from, or an event raised as each round ends. Do *not* solve it by widening
>    `RoundResult` with presentation data or by handing `CardOwnership` to `TurnContext`; the
>    second would leak which money cards an opponent was dealt, which P7's reflection test
>    forbids outright.
> 2. **"Stop playing" is the console's question, not the domain's** (RULES.md §7.2 — no
>    automatic match end). `MatchEngine` should play a round when asked and hand back the
>    banks; `Program` asks *"another round?"* between rounds and prints standings on the way
>    out. An `IPlayerAgent` method for it would be wrong — it is not a move.
> 3. **Banks are the console's to display and the engine's to keep.** Every player starts at
>    zero and each round's deltas are added; conservation is then a property of the addition
>    (see the P6 note below). The settlement report's two columns already reconcile per round,
>    so a standings table only needs the running total.
>
> **Amended again after the 2026-08-18 roadmap session — the same seam carries the statistics.**
> Point 1 above is no longer only the console's need: §3.8 shows that **two of the five things
> a strategy comparison wants** — how close the losers were, and how much of the money was the
> side bet — are reachable *only* through the round's `TableState`. So a `MatchEngine` that
> returns banks alone quietly forecloses on P12. Surface the pair, and both consumers are done
> at once. Two small additions follow:
> - **Put `Turns` on `RoundResult`.** The engine has the count in the loop variable it already
>   keeps; without it, every consumer that wants the most obvious round statistic has to attach
>   an observer and tally `PlayerDiscarded` (§3.8). One field, added where it is free.
> - **A match must be playable with no observer at all** — it already is, and P12 depends on
>   it. Do not let `MatchEngine` require one.
>
> **Amended after the P6 session (2026-08-18) — conservation is already half-proved.**
> Settlement's deltas **sum to zero for any configuration**, which a property test pins over
> 500 randomised rounds. So "money is conserved across the match" reduces to *banking the
> deltas correctly*: `MatchEngine` adds each round's deltas to the running banks and nothing
> else. If a match-level conservation test ever fails, the fault is in the banking, not in
> settlement.

> ⚠️ **Superseded in one respect on 2026-08-21: `MatchEngine` holds *one* seating and should
> re-draw it every round.** `RULES.md` §9 #14 was open when this was built and its recommendation
> was *nothing moves*; the answer came back the other way — **seats are re-randomised between
> games, and a game is a round** (§3, `EXPERT`). This is the only place in the tree that actively
> contradicts a recorded rule rather than merely lacking one, and it is recorded as `RULES.md`
> §10 #16. ✅ **It moves no published measurement** — every experiment in `BurmesePoker.Sim` runs
> `RoundsPerGame = 1`, so no measured game has a second round to re-seat for. ⚠️ **The bill is in
> the two front ends**, which deal round after round with the banks carrying over, and in
> `TableRing`, which draws *you at the front whichever seat you were dealt* (§3.11) and has never
> been asked to rearrange a table between rounds. **The rest of the note below stands.**

> **As built (2026-08-18).** `MatchEngine` holds the seating, the stakes, the banks and one
> `Random`; `PlayRound()` shuffles and `PlayRound(drawOrder)` is the scriptable twin, both
> returning a **`RoundRecord(RoundResult, TableState)`** — the §3.8 pair. It keeps **no history**,
> so a long simulation does not pay for tables it has already read. Four things worth carrying
> forward:
> - ⚠️ **`RoundEngine` now takes a `Random`, and it is required.** A round needs randomness for
>   the reshuffle, and defaulting it would have made an exhausted round irreproducible in
>   silence (§3.7). A round is reproducible from its draw order **and** its seed.
> - **`CardOwnership.TryRecordFromDeck`** is the reshuffle's half of write-once: it keeps the
>   first owner and answers *false* instead of throwing. `RecordFromDeck` stays strict for the
>   deal, where a card leaving the deck twice really is a bug.
> - **`TurnContext.Round`** was added, because an agent lives for a whole match and turn 1 of
>   round 2 was indistinguishable from turn 1 of round 1 — which left the opening player's hand
>   on screen at the start of every round after the first. Public information; no leak.
> - ⚠️ **A round that nobody can win no longer ends.** Before the reshuffle, passive play ran
>   the deck out and threw; now the cards circulate for ever, and only a declaration ends a
>   round (RULES.md §7.1). The rules working as written — but it bites P10 and P12, below.

---

### P10 — Bot opponents (solo play against the computer)

**Goal.** A game you can start alone. **Promoted out of "optional" on 2026-08-18** — §0 makes
this the nearest of the four goals, and §4 shows the other three all run through it.

**Read first.** `RULES.md` **§4.4** (ownership never transfers — the whole heuristic turns on
it), §5, §6. Here: §3.4 (candidates vs. cover), §3.6, and the P5 note below on partial covers.

**Build.**
- **`BurmesePoker.Domain/Agents/`** — bots live in **Domain, not Console**. They are rules
  reasoning with no I/O, the simulation harness (P12) and any future server (P13) both need
  them without dragging Spectre along, and — unlike everything in `BurmesePoker.Console` —
  **a bot in Domain is unit-testable**, because the test project references Domain only.
- **A scored, partial cover.** ⚠️ P5 deliberately did not build one: `TryFindCover` is
  all-or-nothing and returns *nothing at all* when a hand cannot be covered exactly, which is
  every hand a bot is ever asked about. What a bot needs is the same backtracking over the
  same index, **maximising cards covered instead of demanding all thirteen**. Put it beside
  the evaluator as its own type — `HandEvaluator.IsWinning` is the win authority (§3.4) and
  **its answers may not change**.
- **`GreedyBotAgent : IPlayerAgent`.** A strategy is just another implementation of the
  interface; do not invent a strategy abstraction on top of one that already exists.
- **`Program`** asks how many seats are people and fills the rest with bots, naming them.

**The heuristic, and one correction that matters.**

⚠️ **This packet's original line — "never discard an owned money card" — is wrong**, and was
caught when P8 drew the settlement report. Ownership is **permanent and never transfers**
(`RULES.md` §4.4, encoded in `CardOwnership` having no transfer or removal): a money card the
deck gave you **pays you whether you are still holding it or threw it away four turns ago**.
So holding one gains nothing at all, and a bot that hoards money cards is simply playing a
worse hand for no return. **Money cards are near-irrelevant to a discard decision**, and the
one test that pins this is worth more than the rest of the heuristic put together.

What is left is ordinary rummy reasoning:
- **Take the discard** only if it joins a meld candidate that improves the best partial cover;
  otherwise draw. A blind draw also confers ownership, which is a small tiebreak *in favour of
  drawing* — the only place money enters the decision at all.
- **Discard** the card that costs the least cover, jokers last.
- **Claim the turned-up money card** only if it improves the hand. It costs the turn's draw and
  the table — not the deck — gives it, so it **pays nobody** (RULES.md §4.5); there is no
  money reason to take it.
- **Declare** whenever offered. The engine only offers on a genuine win.

**Acceptance tests.**
- A scripted deal plays a whole round with **every seat a bot** and reaches a declaration.
- **`MoneyCardsDoNotChangeWhatABotThrowsAway`** — the same hand, evaluated under two different
  turned-up designations, discards the same card. This is §4.4 expressed as behaviour.
- A bot never discards a card that a meld it is already holding needs.
- The scored cover agrees with `HandEvaluator.IsWinning` on a winning hand: thirteen of
  thirteen covered, and never a claim of thirteen on a hand the evaluator rejects.
- A bot-only **match** (P9) of several rounds terminates and conserves money.

**Done when.** `dotnet run --project BurmesePoker.Console` offers *"how many of you are
people?"*, and one person can play a full match against bots.

> **Amended after the P9 session (2026-08-18) — the termination test is not a formality.**
> With the reshuffle built (RULES.md §5), a round only ends when somebody declares, so **a
> strategy that never improves its hand plays for ever**. "A bot-only match terminates" is
> therefore a real property of the heuristic rather than a box to tick, and the first bot
> should be checked against it before anything is built on top. Two smaller notes: a bot that
> keeps per-round state must key it on **`TurnContext.Round`** as well as `TurnNumber`, for the
> reason P9 found in the console agent; and `TurnContext` already answers `CanDeclare`, so a
> bot never needs to run the evaluator itself to know whether to go out.

> **Hints are P11, not here.** A hint is the same scored cover pointed at a human, and it is a
> presentation decision about when to interrupt somebody — it belongs with the rest of the UX
> pass, drawn next to the card it is advising against in `SpectrePlayerAgent.ChooseDiscard`.

> ### ✅ Built 2026-08-18 — what it turned out to be
>
> - **`Melds/PartialCover.Best(hand)`** → the melds, the deadwood, `CoveredCount`, and
>   `IsComplete`. It is the evaluator's own walk with **one extra branch**: at the lowest card
>   not yet settled it may take a meld covering it *or give the card up and move on*. That is
>   the whole difference between the two questions. Memoised on `(position, covered)`, and a
>   complete cover stops the search where it stands, so a winning hand costs no more here than
>   it does in the evaluator.
> - **`Melds/MeldIndex`** was extracted so both searches share one candidate index rather than
>   two copies of a subtle thing. `HandEvaluator`'s answers are unchanged, and its 208-test
>   baseline is what says so.
> - **The whole strategy is one question asked three ways:** *of the thirteen I would be left
>   holding, how many meld?* Take the discard iff it raises that count; claim the turned-up
>   money card iff it raises that count; throw whichever card leaves it highest. **The score
>   can never fall** — throwing back what was just taken restores the hand exactly — which is
>   what makes a table of bots terminate rather than a hope that it does.
> - **The tie-break is what makes progress**, because early on most discards score alike. It
>   prefers keeping cards with partners (another suit of the same rank, a neighbour in the same
>   suit) and keeps jokers over everything. **Ties on taking go to the deck**, which is the
>   only place money touches a decision at all (a blind draw confers ownership; a pickup does
>   not).
> - **Measured, and better than feared.** Bot-only matches over twelve seeds and every table
>   size from four to six: **every round terminated**, 21–30 turns each, ~40 ms a round,
>   ~1.5 ms a turn. No round ever ran the draw pile out, so the P9 reshuffle stayed unexercised
>   by bots. A hand's opening cover averages **4 of 13**, and a bot reaches thirteen in seven
>   or eight of its own turns — mostly by taking discards, because with two decks the card
>   somebody throws away is very often somebody else's third of a rank.
> - **No new rules question.** Everything the strategy needed was already settled: §4.4
>   (ownership never transfers), §4.5 (a claimed card pays nobody), §5, §6, §7.1.

---

### P11 — Console UX pass ☑ done 2026-08-18

**Goal.** A console game that is pleasant to sit at for an hour. **The UI stays terminal-based
for a long time by choice (§0), so this is not polish deferred until a "real" UI arrives — it
is the UI.**

**Read first.** §0. `STATUS.md`'s P8 notes, which list what P8 knowingly left rough.

**Build.** Roughly in order of how much they are missed:
- ⚠️ **A round log that survives the per-turn clear.** P8 clears the screen every turn for
  concealment, so all public narration scrolls away — a player cannot see what the last three
  people discarded. This wants a panel rebuilt each turn from remembered events, **not more
  `WriteLine`s**; `ConsoleObserver` already receives everything it needs.
- **Show the hand as the melds it nearly is** — group by P10's scored cover so a player sees
  the three melds they have and the four loose cards, rather than thirteen sorted cards.
- **A discard hint**, off the same scored cover, next to the card it advises against.
- **Standings and a between-round summary** — banks carried over (P9), who won what. ⚠️ **P9
  built the plain version**: a settlement table split into the round payment and the money-card
  side bet, a standings table of the running banks, and *"another round?"* between rounds. What
  is missing is the history — who won each round, and how the banks moved — which needs
  remembering, because `MatchEngine` deliberately keeps no per-round history.
- **A settled colour and marker language**, defined once: the `($)` / `($$)` / `★` set from P8
  extended rather than re-invented per screen.
- **`--seed`**, so a strange round can be replayed and reported.

**Acceptance.** Manual, and the packet should say plainly what was played to check it. The one
mechanical check available: **no domain type references Spectre**, and `Program` still refuses
a non-interactive terminal with an explanation rather than a stack trace.

**Done when.** A full match against bots is enjoyable rather than merely possible.

> **Amended after the P10 session (2026-08-18) — three things bots changed here.**
> - ⚠️ **The round-log gap got worse, and it is now the top item by some distance.** Before
>   P10 a player missed the turns of three other people between their own; now those turns
>   happen in **milliseconds** and vanish behind the next screen clear, so the only trace of
>   what four bots did is the one discard the table panel happens to show. The panel rebuilt
>   from remembered events is what fixes it.
> - **The scored cover exists and is public**: `PartialCover.Best(hand)` hands back the melds
>   *and* the deadwood, so "show the hand as the melds it nearly is" and the discard hint are
>   both readings of one call. The hint that matches how the computer plays is literally
>   `PartialCover.Best(hand without this card).CoveredCount`.
> - **Bots are named and marked already** — `Program` calls them *Ruby (bot)*, *Sable (bot)*
>   and so on, so narration reads as a table of players. What is missing is any sense of them
>   *thinking*: a bot's turn is instantaneous, which reads as nothing having happened. A
>   deliberate pause is a UX decision and belongs here, not in `GreedyBotAgent` — a domain type
>   that slept would ruin P12.

> **Amended after the P12 session (2026-08-18) — three things the simulation hands P11.**
> - **A difficulty setting exists for free.** `SimpleBotAgent` is a measurably weaker seat —
>   19.3% of rounds against the greedy bot's 30.7% over 2,000 of them — so *"easy or hard?"* at
>   the table-setup prompt is a strategy name, not a packet. Both live in `Domain/Agents/`, so
>   the console can already reach them.
> - ⚠️ **The reshuffle needs narrating at six seats, and only there.** 300 bot rounds produced
>   no reshuffle at four players, three at five and 67 at six, so a full table sees it every
>   few rounds while a small one never does. `ConsoleObserver` already receives
>   `DiscardsReshuffled(cards)`; a player who is not told will simply see the deck grow back.
> - **`--seed` is a one-liner and worth having.** `MatchEngine` takes the one `Random` and the
>   sim now proves a match replays exactly from it; the console needs only to pass
>   `new Random(seed)` instead of `Random.Shared` and print the seed it used.

> **As built (2026-08-18). Every item above shipped. Five new files, no Domain or Sim change.**
> - **`RoundLog`** — `ConsoleObserver` now *says and files* each line of narration through one
>   `Say(markup)` helper, so there is exactly one copy of every sentence the game speaks;
>   `SpectrePlayerAgent.BeginTurn` redraws the panel above the table and the hand, because the
>   log is the context the other two panels are read against. **It remembers markup, not
>   events** — re-rendering from events would have put a second copy of the wording next to the
>   first. Per round, and nothing survives the round.
> - **`HandView`** — one `PartialCover.Best` call drawn as `run`/`set`/`loose` rows, headed by
>   *"9 of 13 meld"*, with jokers still showing what they stand in for. It also prices each card
>   at `covered(13) − covered(12)`, which is what the discard list annotates itself with.
> - ⚠️ **The discard hint is `GreedyBotAgent`'s own answer, not a re-derivation.** The agent
>   holds a bot and asks it `ChooseAction` / `ClaimTurnedUpMoneyCard` / `ChooseDiscard` on the
>   very `TurnContext` in hand. `CoverScore` is `internal` and the tie-break is not trivial, so
>   a second implementation would have been a different strategy wearing the first one's name.
>   `--no-hints` turns the lot off.
> - **`PacedAgent`** pauses once per `(Round, TurnNumber)` — the pair `SpectrePlayerAgent`
>   already hands the keyboard over on, because a turn asks a varying number of questions.
>   `Wrap` returns the agent unchanged at zero, so `--pace 0` costs nothing.
> - **`Palette`** gathers what P8 had invented in three files. The rule it records: yellow is
>   money, green is yours or in your favour, red is money leaving you, grey is context — and a
>   card's own red or black is the card's and means nothing else.
> - **`Options`** is `--seed`, `--pace`, `--no-hints`, `--help` and nothing more; everything
>   about the *table* is still asked at the prompt, where the player already is. **A seed is
>   drawn when none is given and always printed**, so any match can be replayed after the fact;
>   ⚠️ the **seating** is taken from the match's own `Random` too, or `--seed` would replay the
>   same deals to a different table.
> - **The difficulty prompt cost one method.** *Hard* is `GreedyBotAgent`, *easy* is P12's
>   `SimpleBotAgent`, and the prompt quotes the measured win rates.
> - ⚠️ **Spectre markup must balance and nothing but running it will tell you.**
>   `Palette.Legend` shipped with two opening `[grey]` tags and one `[/]`, compiled clean,
>   passed 239 tests, and threw `Unbalanced markup stack` on the first hand drawn. Any constant
>   that interpolates a colour deserves a second look.
> - **Verified by playing**, through a pty as P8 and P9 were: 13+ rounds at four seats with
>   settlements summing to zero, a six-seat table to see the reshuffle narrated (it never fires
>   at four — P12's finding from the other side), and seed 1 for the opening turn, which is the
>   only turn that offers the money-card claim. `--seed 99` twice is byte-identical; `--seed
>   100` is not.

---

### P12 — Simulation at scale

**Goal.** Run thousands of games in parallel and compare ways of playing.

**Read first.** §3.7 and **§3.8**, which are the contracts this packet cashes in. §3.4 for why
the evaluator may not be "optimised" into a different answer.

**Build.**
- **A separate project, `BurmesePoker.Sim`** (recommended) referencing Domain only. Batch runs,
  parallelism and CSV output have nothing to do with the Spectre front end, and a fourth
  project keeps both honest. *Alternative if four projects feel heavy: a `--sim` mode inside
  `BurmesePoker.Console`.* It works, but it puts a throughput concern inside the interactive
  binary, and §2's argument against "it'll be fine in one project" applies again.
- **Seeding**: one master seed, per-game seeds derived from it, recorded with every result. A
  run must be **exactly reproducible from its seed**, which is what makes a surprising result
  investigable rather than folklore.
- **Parallel execution** over games, with per-strategy stats. **§3.8 is the contract for how
  they are gathered** — the domain knows nothing about any of them; the harness derives them
  from the event stream, the per-round `(RoundResult, TableState)` pair, and a recording
  decorator over `IPlayerAgent` for anything decision-level. Worth having from the first run:
  win rate, money per round split into the flat payment and the side bet, turns to a
  declaration, how close the losers were when it ended, take-the-discard rate, claim rate, and
  how often the deck is exhausted (P9's reshuffle).
- **Results carry their join keys** — seed and seat→strategy map on every row (§3.8 item 4),
  or a surprising result cannot be reproduced or attributed.
- ⚠️ **A measurement pass first.** `RoundEngine` calls `TryFindCover` after every discard by
  every player, so it is the inner loop of the whole harness (§3.7 item 4). **Measure it,
  record the number in the packet notes, and only then decide whether anything needs doing.**
  Any speed-up goes *around* the evaluator — a cheap pre-filter, a memo — never into changing
  what it answers.

**Acceptance tests.**
- **Determinism**: the same master seed produces byte-identical results, run serially or in
  parallel. This is the test the whole packet stands on.
- **No mutable static state** — pinned deliberately (§3.7 item 3), because it is what a later
  convenience cache would break silently.
- Money is conserved in **every** simulated match, not merely on average.
- A strategy comparison over enough games to be meaningful, with the throughput recorded.

**Done when.** Two strategies can be played off against each other over thousands of games and
the winner is reproducible from a seed.

> **Amended after the P9 session (2026-08-18) — three things the match engine settles.**
> - ⚠️ **A run needs a turn cap, and it belongs to the harness.** A round ends only on a
>   declaration (RULES.md §7.1) and the reshuffle keeps the cards moving, so a weak strategy can
>   play for ever. The domain will not invent a limit — that would be inventing a rule — so the
>   harness must bound a round itself, and **report how many rounds it abandoned**, because a
>   strategy that stalls is a result rather than an error.
> - **`MatchEngine` keeps no history.** `PlayRound` hands back `(RoundResult, TableState)` and
>   forgets it, so the harness derives what it wants per round and drops the table. That is the
>   §3.8 seam working as intended; do not add a history to the engine to make a report easier.
> - **The seed is the whole story.** `MatchEngine` takes one `Random` and uses it for every deal
>   and every reshuffle, so a match is reproducible from its seed and its seating alone
>   (§3.7 item 1) — which is exactly the join key §3.8 item 4 asks for.

> **Amended after the P10 session (2026-08-18) — the measurement pass has a head start, and
> the hot loop moved.**
> - **First numbers, from `GreedyBotAgentTests`:** a bot-only round is **21–30 turns and about
>   40 ms**, over twelve seeds and every table size. A thousand rounds is therefore well under
>   a minute on one core before any parallelism at all. **The speed risk is smaller than §3.7
>   feared** — but these are *this* strategy's numbers, and a deeper one will cost more.
> - ⚠️ **`PartialCover.Best` is the inner loop, not `TryFindCover`.** A greedy bot calls it
>   once per candidate discard — up to fourteen a turn, plus two on the take decision — against
>   the engine's one `TryFindCover` per discard. Measure **that** first. The obvious win, if
>   one is needed, is a per-turn cache: `ChooseAction` and `ChooseDiscard` score overlapping
>   hands within a single turn and share nothing today. That is a change to the bot, not to the
>   evaluator, and so is free of the §3.4 constraint.
> - **The turn cap is still needed, and now has a baseline.** `GreedyBotAgent` terminated in
>   every round of every seed tried, so the cap is not for *this* strategy — it is for the next
>   one. A cap of a few hundred turns would not have fired once in P10's runs, which is roughly
>   the right calibration.
> - **A recording decorator over `IPlayerAgent` is written and works** —
>   `BurmesePoker.Tests/Agents/RecordingAgent.cs`, the §3.8 item 2 seam, built because a
>   `TurnContext` is the engine's to make and a bot's decisions are only testable from inside a
>   real round. P12 wants the same shape; lift it rather than re-inventing it.

> **As built (2026-08-18). ☑ Done** — `BurmesePoker.Sim`, a fourth project referencing Domain
> only, plus a second strategy to compare against.
> - **Two strategies that differ in exactly one thing.** `SimpleBotAgent` is `GreedyBotAgent`
>   with the discard tie-break removed and nothing else changed — same take rule, same claim
>   rule, same dedup, so a difference in results is attributable to the tie-break alone. The
>   shared question moved into `Agents/CoverScore` for the reason `MeldIndex` is shared: two
>   copies of *"does this card improve my hand"* would be two places for it to drift.
> - **The answer: the tie-break is worth 1.6× the wins.** Over **2,000 four-seat rounds**,
>   greedy won **30.7%** and simple **19.3%**, for **+$1.24** and **−$1.24** a round. P10's
>   claim — that the cover count alone cannot separate early discards, and the tie-break is
>   what makes progress — is now measured rather than argued.
> - **Seats are rotated between games, and they had to be.** Seat 0 opens every round and is
>   the only seat ever offered the turned-up money card (RULES.md §4.5), so a fixed seating
>   would measure the seat as much as the strategy. Strategy *i* sits in seat *(i + game) mod
>   n*.
> - **Determinism is per game, not per run.** A game's seed is `SplitMix64(master, index)`, so
>   game 417 is the same game whether it ran first, last, alone, or on a different number of
>   cores — which is what makes a surprising result investigable. Serial, parallel and
>   two-thread runs produce byte-identical rows.
> - **The turn cap fires from the agent**, because that is the only seam that sees a turn
>   number without the domain inventing a rule. `SeatRecorder` throws `RoundAbandonedException`
>   past the cap; the game stops there and the run reports how many it abandoned. ⚠️ **It has
>   never fired in a real run** — even an all-`SimpleBotAgent` table finished all 300 rounds
>   tried, in 28.3 turns on average. It exists for a strategy that genuinely plateaus, and the
>   test uses one written for the purpose.
> - **Money conservation is checked per round, not per run** — every round's nets sum to zero,
>   and the flat/side-bet split (derived by subtraction, as the console does it) sums to zero
>   twice over.
> - ⚠️ **The reshuffle is a six-player phenomenon.** P10 never saw a bot round exhaust the draw
>   pile; at 300 rounds it happens **0 times at four seats, 3 at five, and 67 at six**. P9's
>   rule is exercised by real play after all — but only at a full table.

> **Amended 2026-08-19 — the rotation is not general enough, and the headline needs a caveat.**
> `Seating(game)` rotates **one fixed pattern**: `Strategies[(seat + game) % Strategies.Count]`.
> That is exactly right for the question P12 asked — it stops a strategy owning seat 0 — and it
> is **not** enough for the question P16 asks. With two strategies at four seats it produces only
> `[A,B,A,B]` and `[B,A,B,A]`, so the pair *(my strategy, the strategy feeding me)* never varies:
> every A is fed by a B, always. ✅ **P16 fixed this (`SimulationOptions.Assignments`,
> `SeatingPlan.Balanced`, `--seating balanced`) and measured what it was worth: greedy 29.6% vs
> simple 20.4% balanced, against 30.7% vs 19.3% rotated.** ⚠️ **A consequence for the headline
> result:** 30.7% against
> 19.3% was measured with every greedy seat sitting downstream of a simple seat, so it is the
> honest answer to *"what happens at that table"* rather than a clean strategy-vs-strategy
> figure. **Nothing here is wrong and nothing needs re-deriving** — but P16 owns separating the
> two, and should re-run the comparison under balanced assignment and report both.

---

### P13 — The browser client and multiplayer

**Goal.** A person opens the game in a browser and plays a table; then several people do, with
the host filling the empty seats with AI players.

**Read first.** **§3.10** (where the engine runs — the decision this whole packet is built on),
**§3.11** (the UX standards, taken in advance and not negotiable inside a sub-packet), §3.5,
§3.6, §0.

> #### ⚠️ Re-planned 2026-08-19, after P16, and the change is not cosmetic
>
> **The old P13.1 was written on an assumption Blazor makes false.** It read *"lift the front
> end off `AnsiConsole`… take an `IAnsiConsole`"*, which is the right refactor for **a second
> Spectre front end** — and a Razor client is not one. It shares no drawing with the console at
> all. What it wants from `BurmesePoker.Console` is the **decisions**: which cards nearly meld,
> what each costs to throw, what state each card is in, and what the computer would do. Today
> `HandView` answers those *and* emits Spectre markup in one breath.
>
> **So P13.1 becomes an extraction rather than an injection**: pull the view *model* out into
> `BurmesePoker.Presentation` (§2), which both front ends render their own way. `IAnsiConsole`
> injection is still worth doing for testability, but it is no longer the point of the packet.
>
> **And P13 splits five ways rather than three**, because §3.10 collapsed the browser UI and
> multiplayer into one track and each half of that track ships something playable.

**The five sub-packets.** Take them in order. **Each ends green, and each is more playable than
the one before it** — the property §7 names as the mitigation for this project's one historical
failure mode.

---

#### P13.1 — A presentation view model, rendered two ways ☑ done 2026-08-19

**Build.** A fifth project, `BurmesePoker.Presentation`, referencing **Domain only**.

- **The hand as data.** `HandView`'s two questions — *which cards group into which near-meld*
  (one `PartialCover.Best` call) and *what does each card cost to throw* (`covered(13) −
  covered(12)`) — move here and return a **view model**, not markup.
- **Display state as an enum, not a colour.** Money card, owned, near-melded, deadwood, the
  computer's suggested throw. ⚠️ **Every state carries a non-colour token** (§3.11 A2) — the
  console already has the glyphs, so this is a move rather than an invention.
- **The hint stays the computer's own answer.** `GreedyBotAgent` asked directly on the very
  `TurnContext` in hand (P11). ⚠️ **Do not re-derive a recommendation** — a second
  implementation is a different strategy wearing the first one's name.
- `BurmesePoker.Console` is rewritten *onto* the view model and keeps rendering exactly what it
  renders now. `Palette` and `CardFormatting` stay in the console: they are Spectre markup and
  that is correct.
- Secondary: take `IAnsiConsole` where the console reaches for the static one, so the front end
  is drivable from a test.

**Acceptance.**
1. `LayeringTests` grows a row: **`BurmesePoker.Presentation` references no rendering
   technology** — no Spectre, no ASP.NET, no `System.Console`.
2. The view model is unit-tested directly — the first time anything in the presentation layer
   has been testable at all, since `BurmesePoker.Console` is unreachable from the test project.
3. ⚠️ **The console still plays a match through a pty exactly as it does today**, byte-identical
   at the same `--seed`. This is a refactor and it must be provable as one.

**Done when.** The console draws its hand from a view model it did not build itself, and a test
project can build the same view model without a terminal.

##### What P13.1 found

**All three acceptance criteria met.** 294 tests before, **324 after** — thirty of them the
first assertions ever made about this project's presentation layer. The console prints the same
bytes it printed at HEAD: five scripted pty matches across **three seeds**, including one under
`--no-hints`, every one `cmp`-identical.

1. 🔥 **A view model that aliases the engine's list is not a view model, and the test caught it
   on the first run.** `TurnContext.Hand` hands back the seat's *own live list*, and `RoundEngine`
   discards from it the moment the answer comes back (step 2 of `TakeTurn`). A `HandView` that
   kept the reference reported fourteen cards and then held thirteen. **The console never showed
   it** — it renders the panel and drops the view in the same breath — and **a Blazor component
   holding a view across a render is precisely the case that does.** `HandView.Of` now copies.
   ⚠️ **This generalises, and P13.2 inherits it:** anything the server hands a seat must be a
   *snapshot*, because between building it and drawing it the engine has moved on.
2. ⚠️ **Two copies of the same card cost the same to throw, and that is correct.** A spare copy
   is a standing replacement for the one the cover used, so throwing either leaves the same
   thirteen melding. What differs between them is only *which one the arrangement happened to
   use* — so a browser must not imply the melded copy is dearer than its twin. The instance
   identity that matters here is that they are **two entries keyed on `CardId`** (§3.1), not that
   they are priced differently.
3. ⚠️ **The `IAnsiConsole` injection is done and its stated benefit is still unreachable.** The
   console now takes the terminal everywhere and reaches for the static one exactly once, in
   `Main`. But "drivable from a test" cannot follow while §2 forbids the test project from
   referencing `BurmesePoker.Console` — and that rule is worth more than the benefit. **So the
   console's verification is the pty, and `scripts/drive-console.py` is checked in to make it
   repeatable.** Do not resolve the tension by adding the reference.
4. **Costs are computed eagerly, all thirteen up front**, where the console memoised them
   lazily. A view model is data a component can iterate twice and a test can assert over, not a
   computation graph that searches a hand when a renderer touches it. It is one
   `PartialCover.Best` per card — a few milliseconds a hand at P12's measured speeds, built once
   a turn per seat.
5. **No rules question arose.** `RULES.md` stays at **rev 13**, unchanged across five
   consecutive packets. The one rule the tests had to be taught was already written down:
   doubling is the *overlap* of the two designations, so a card pays twice only when a permanent
   money card is also turned up (§4.1, §4.3) — turning the same card up twice still pays once.

---

#### P13.2 — The table server ☑ done 2026-08-19 *(still no UI)*

**Build.** In-process, no transport, no sockets.

- **`RemotePlayerAgent : IPlayerAgent`** blocking on a channel until an answer arrives —
  §3.6's decision, finally cashed.
- **A timeout is a bot move.** Reuse `SeatRecorder`'s shape (a decorator that watches every
  question and can answer differently); `new GreedyBotAgent()` answers the same `TurnContext`
  the absent player was given, and needs no handover because the strategy keeps no state
  between turns. ⚠️ Wrap it the way P11 wraps a bot seat, so a taken-over seat *looks like a
  seat playing* rather than a freeze.
- **Per-seat observer fan-out, filtered server-side.** P8 established that filtering private
  information is the front end's job; over a connection it becomes a **security property**
  (§3.10). A viewer is *not sent* what it may not see.
- ⚠️ **Every per-seat view is a snapshot, never a live reference** — amended after P13.1, which
  hit this the first time it built a view inside a real turn. `TurnContext.Hand` is the seat's
  own list and the engine mutates it as soon as the answer comes back. `HandView` copies; **so
  must anything else the fan-out hands out**, because a circuit holds what it was given until it
  next renders.
- ✅ **The seat's own view is already built:** `HandView.Of(TurnContext)` (P13.1). The server does
  not compose a view of a hand — it decides *which* `TurnContext` a connection is entitled to and
  calls that.
- **A table carries its seed**, exactly as a console match does — `--seed` is the bug-report
  format and it is one `Random` per `MatchEngine` (P11).
- **Abandoning a table is the host's job, not the engine's.** A round ends only on a
  declaration (`RULES.md` §7.1), so a table whose people all walk away never ends. P12 bounds a
  round by turn count and *reports* it; a host wants the same, bounded by wall clock.

**Acceptance.**
1. 🔥 **Two scripted "remote" agents and two bots play a full round through the server's own
   plumbing, in a test, with no sockets involved.** The first time a multiplayer packet in this
   plan has had a mechanical acceptance criterion — structure the packet around it.
2. ⚠️ **The leak test** (§3.11 A1): for a scripted round, each seat's outbound view contains no
   `CardId` from any other seat's hand. **This is the packet's most important test**, and it
   belongs here rather than in the UI packet, because by the UI packet it would be too late.
   **P13.1 makes it writable as it stands**: build `HandView.Of(context)` for every seat of a
   scripted round and assert the `CardId` sets are pairwise disjoint. The type already forbids
   the leak — a `TurnContext` has no path to another seat's hand — so what the test guards is
   everything the server *adds* around it.
3. A seat that stops answering is played by a bot, the round finishes, and the takeover is
   reported rather than silent.

**Done when.** A round completes with a mix of remote and bot seats, and nothing that must stay
concealed has crossed the fan-out.

##### What P13.2 found

**All three acceptance criteria met.** 324 tests before, **342 after** — eighteen of them, in
three new classes, and the whole suite still runs in eighteen seconds. **`BurmesePoker.Server`
is a sixth project and it is the only thing that changed**: Domain, Presentation, Console and
Sim were not touched at all, so the engine now stands unaltered across four consecutive packets.

1. 🔥 **Exactly one event in the whole narration is private, and the security boundary is
   therefore one `if`.** A discard, a taken discard, a claimed money card and a declaration all
   happen in front of the table (RULES.md §4.5, §5, §6.3, §7.1); the **blind draw** is the only
   thing the engine narrates that a viewer may not hear. That is a smaller surface than the
   plan implied — and it makes `TableFanOut` worth reading, because everything difficult about
   it is the care around lists rather than the filtering. **Mutation-tested rather than
   asserted**: broadcasting the drawn card to every connection turns three of the five
   concealment tests red.
2. 🔥 **P13.1's snapshot rule generalised exactly as predicted, and it caught two more live
   lists.** `TableState.TurnedUpOnTable` is aliased by *both* `TurnContext.TurnedUpMoneyCards`
   and `IGameObserver.RoundStarted`, and **the opening player's claim removes a card from it**
   (§4.5) — so a prompt or an event that kept the reference would quietly stop saying what it
   said when it was sent. `EverythingASeatWasSentStaysWhatItWasWhenItWasSent` forces the claim
   and pins both; removing either copy fails it. ⚠️ **Assume every list the engine hands over is
   live until shown otherwise.**
3. 🔥 **Answering inside the connection's own notification makes a whole round deterministic**,
   and it is why eighteen tests that play real rounds cost four seconds and no flakiness.
   `SeatConnection.Updated` is raised on the round's thread *before* the seat begins waiting, so
   a handler that answers immediately latches the answer and the wait returns at once — no
   sleeps, no polling, no timing. One test drives the genuinely cross-thread path as well,
   because that is the shape a circuit has.
4. ⚠️ **A client bug must not be able to end a table's round.** `RoundEngine` throws on a
   discard the seat is not holding, which is right in a domain and wrong over a connection.
   `SeatConnection.Answer` **refuses** an answer that does not fit the question or names a card
   the seat is not holding: it returns false, **the prompt stands**, and the client may correct
   itself. `SeatAnswer.Fits` is where each case checks itself.
5. ⚠️ **Pacing was deliberately not applied to the stand-in, and P13.3 inherits the decision.**
   The packet said to wrap the takeover bot the way P11 wraps a bot seat; a sleep belongs to
   whatever is *drawing* the table and not to a server that may be hosting many of them, so
   `TableOptions.StandIn` is a **factory** and a host hands over an agent that is already paced.
   ⚠️ **`PacedAgent` currently lives in `BurmesePoker.Console`, which `BurmesePoker.Web` may not
   reference.** P13.3 has to move it to a shared home (Domain's `Agents/` is the obvious one) or
   write the browser's own — **do not solve it by referencing the console.**
6. **The wall-clock bound catches what a turn cap cannot.** P12 bounds a simulated round by
   turns because what goes wrong there is the *play*; a hosted round is bounded by the clock
   because what goes wrong here is the *people*. Every seat is wrapped, **bots included** — a
   table nobody is left at would otherwise play bot-perfect rounds for ever. The round is
   announced abandoned before the exception leaves, and the session is playable afterwards.
7. **The takeover is per question and announced once a turn** — the same `(Round, TurnNumber)`
   key, and the same reasoning, as P11's pacing decorator. A player who misses one prompt and
   comes back for the next one is simply back, because a strategy keeps no state to hand over
   (P15).
8. **No rules question arose.** `RULES.md` stays at **rev 13**, unchanged across six
   consecutive packets.

---

#### P13.3 — A browser table you can watch ☑ done 2026-08-19 *(the first UI, and no seat yet)*

**Build.** `BurmesePoker.Web` — a Blazor Web App, **Blazor Server** (§3.10).

- **Static SSR shell, one interactive island.** ⚠️ **`@rendermode InteractiveServer` on the
  table component and nowhere near the root** (§3.11 C12). The shell, the rules help and the
  settlement report are static.
- The table component subscribes to the filtered observer stream from P13.2 and renders: seats
  and banks, discard piles, the turned-up money cards, the round log, and the settlement when a
  round ends. **Every seat is a bot**; there is nothing to click yet.
- ✅ **The connection it subscribes to already exists and is already the right one.**
  `TableSession.Watch()` hands back a `SeatConnection` with no seat: the public narration, never
  a question, and nothing it may not see (P13.2). The component is `connection.Updated` →
  `InvokeAsync(StateHasChanged)` (C15) and its disposal is `TableSession.Leave(connection)` plus
  unhooking the handler (A5). ⚠️ **Never `StateHasChanged` from `Dispose`.**
- ⚠️ **`PacedAgent` has to move before a bot seat reads properly in a browser.** P13.2 left the
  stand-in unpaced on purpose — a sleep belongs to whatever draws the table, and
  `TableOptions.StandIn` is a factory so a host can hand over an already-paced agent. But the
  decorator itself lives in `BurmesePoker.Console`, which `BurmesePoker.Web` may not reference.
  **Move it to `BurmesePoker.Domain/Agents/` (it has no console dependency and never did) or
  write the browser's own — not a reference to the console.**
- `@key` on every seat and every card (C14). `IDisposable` on anything subscribing (A5).
  `StateHasChanged` through `InvokeAsync` from the observer thread (C15). ✅ **The key is
  `CardView.Card.Id.Value`, and `CardOrder.Display` is a total order** (P13.1, tested), so a hand
  re-sorted after a draw cannot swap two identical-looking cards past each other and drag focus
  with them — which is the exact failure C14 exists to prevent, and it is already closed.
- ⚠️ **Port the observer *stream*, not `RoundLog`'s panel** — a browser never loses scrollback.
  The log is a `aria-live="polite"` region (B8).

**Why this is a real packet and not scaffolding.** A concealed game watched from outside is
**the strictest possible concealment case** — there is no seat whose cards may legitimately be
shown — so the leak test has its cleanest form here. And it settles the render model, the
layout, the palette and every accessibility decision **before** interaction complexity arrives.

**Acceptance.**
1. A bot-only round plays out in a browser, start to settlement.
2. **The rest of the §3.11 A list passes as tests** — computed contrast in both themes, real
   controls, disposal. ⚠️ **Colour tokens (A2) shipped in P13.1 and the leak test (A1) shipped
   in P13.2**; what is left for this packet is the other three, plus the fact that the markup
   *uses* the tokens rather than that they exist. ⚠️ **The leak test is not re-derived here** —
   it already covers a watcher, which is this packet's own case; what a component could add is a
   second route to the session that bypasses the fan-out, **which is the thing to refuse.**
3. **The §3.11 B list is reviewed by playing**, the way P11 was: keyboard, focus, live region,
   reduced motion, target sizes. ⚠️ **Say in the report what was actually exercised**, as P11
   did — "13+ rounds through a pty" is the standard to match.

**Done when.** You can watch a whole round in a browser, with a screen reader, without a mouse.

---

##### What P13.3 found

**All three acceptance criteria met.** 342 tests before, **371 after** — twenty-nine of them,
in five new classes, and the suite still runs in twenty seconds. `BurmesePoker.Web` is a
**seventh** project; **Domain, Server and Sim were not touched at all**, so the engine now
stands unaltered across five consecutive packets, and the only edits outside the new project are
one moved file, one `ProjectReference`, one `FrameworkReference` and one `LayeringTests` row.

1. 🔥 **The framework's own files are static web assets, and `UseStaticFiles` does not serve
   them.** The page rendered, looked finished, and was **dead**: `_framework/blazor.web.js` and
   the CSS-isolation bundle both 404'd, so no circuit ever started and the table would have sat
   at whatever the prerender said for ever. `MapStaticAssets` is the fix and it is what the .NET
   10 template uses. ⚠️ **It was found by asking the server for every URL the page references,
   not by looking at the page** — a prerendered Blazor Server page is a perfect photograph of a
   broken one. **Ask for the assets.** ⚠️ **And run from source in `Development`**: the
   endpoints manifest a build writes is a development one, so `Properties/launchSettings.json`
   is checked in and sets it. A published output is fine in Production.
2. 🔥 **A trimmed log must not take its `@key` from the length of the log.** The board keeps the
   last 240 lines because a match is unbounded; a sequence taken from `Log.Count` starts
   repeating the moment it trims, and **a repeated `@key` is Blazor reusing the wrong DOM node**
   — which is the exact failure C14 exists to prevent, arriving from the direction the standard
   did not name. `TableBoard.Narrated` counts every line ever said and the key comes from that.
3. 🔥 **Two hundred and forty visually-hidden spans made the document ten thousand pixels
   tall.** The `position: absolute` in the standard visually-hidden recipe needs a positioned
   ancestor; without one, a hidden span on log line 240 has its static position measured from
   the page rather than from the scrolled box it lives in, and `document.scrollHeight` went to
   **10,295px** for a page whose body was 1,814. ⚠️ **Measured, not seen** — every element was
   in the right place and the screenshot looked correct. `position: relative` on the chip and
   the seat name closes it.
4. ⚠️ **A source scan must read the markup and not the prose about it.** Four of the six scans
   failed on their first run, every one of them on a comment in the very file that was obeying
   the standard — *"there is deliberately no `@rendermode` here"*, *"polite, never assertive"*.
   `Sources.Markup` strips Razor comments, block comments and whole-line `//` comments before
   anything is scanned, and leaves a `//` inside a URL alone.
5. ⚠️ **A `@key` check that looks *nearby* is not a `@key` check.** The first version searched
   600 characters after each `@foreach`; removing the key from the log's `<li>` left the nested
   `<CardChip @key>` to cover for it and **the mutation survived**. It now reads the first
   element the loop opens and looks in that tag alone. **Eleven mutations, eleven red** — the
   root going interactive, the live region turning assertive, a clickable `div`, two different
   loops losing their keys, a render outside `InvokeAsync`, the table not unhooking, body text
   losing contrast, a dark token being forgotten, a stylesheet naming an undeclared colour, and
   the log key repeating.
6. 🔥 **The board is folded from the event stream, and that is what keeps the concealment
   test in front of the page.** `TableSession` was right there and could have been asked for its
   banks and its seating directly; a component that did so would be *a second route to the table
   that bypasses the fan-out* — acceptance 2's *"the thing to refuse"*. `TableBoard` takes
   nothing but `TableEvent`s, so **the page can draw nothing the fan-out did not send**, and
   `ConcealmentTests` (P13.2) stands in front of all of it. A watcher's banks are accumulated
   from the settlements it was told about, which is also true of somebody pulling up a chair at
   round three.
7. ⚠️ **`PacedAgent` went to `BurmesePoker.Presentation`, not to `Domain/Agents/`.** The plan
   named the domain first; the deciding argument against it is that **`BurmesePoker.Sim`
   references Domain**, so a sleep in there is reachable from the hot loop P11 wrote a layering
   test to protect. Presentation is reachable from both front ends and from neither harness.
   **The console plays byte-identically after the move** — two pty captures at two seeds,
   `cmp`-identical, which is what `scripts/drive-console.py` was checked in for.
8. **A CSS-only log that stays on its latest line.** A `column-reverse` flex box wrapping a
   normally-ordered `<ol>`: the DOM order stays oldest-first, which is the reading order and
   what the live region announces, while the box's scroll origin is the bottom. **No JavaScript
   and no interop.** ⚠️ The scroller is `tabindex="0"` with a name, because an overflowing box
   the keyboard cannot reach hides its own history (WCAG 2.1.1).
9. **No rules question arose.** `RULES.md` stays at **rev 13**, unchanged across seven
   consecutive packets.

---

#### P13.4 — A seat you can play ☑ done 2026-08-19 *(solo play in the browser, complete)*

> **⚠️ Amended after P13.3, and every amendment is something already built.** The page, the
> palette, the card, the log and the settlement all exist and are asserted over; what this
> packet adds is the seat. In particular: **`TableHost` opens the table with every seat a bot
> and one shared watcher connection** — seating a person means giving one seat no agent
> (`TableSeat.Person`) and handing that circuit its *own* `SeatConnection` from
> `TableSession.ConnectionFor`, **not the watcher's**. 🔥 **The board must stay folded from
> events**: a seated page draws its own hand from the `SeatPrompt` it was sent and from no
> other source, which is exactly what keeps `ConcealmentTests` in front of it (P13.3 finding 6).
> ⚠️ **And `TableBoard.After` already reads `Drew.Card` rather than assuming it null** — a
> watcher is never told, a seat is, and the line is written for both.

**Build.** The four questions `IPlayerAgent` asks, as interactive components pushing answers
into P13.2's channel: take the discard or draw blind, claim the turned-up money card, choose a
discard, declare. ✅ **All four already have a type on each side**: `SeatConnection.Pending` is a
`SeatPrompt` carrying the question and the hand, and `SeatConnection.Answer` takes a
`SeatAnswer` — which **refuses** anything that does not fit rather than throwing, so a
half-built component cannot end somebody's round (P13.2).

- The hand comes from P13.1's view model — near-melds grouped, each card priced, the computer's
  suggestion marked, and **a toggle for whether the hints show at all** (`--no-hints`'s
  equivalent). ✅ **The toggle is already a parameter**: `HandView.Of(context, suggestedThrow)`
  takes null for no hint, and `ComputerAdvice` is where the answer comes from. ⚠️ **Do not
  re-derive a recommendation in a component** — that is the one thing P13.1 wrote a type to
  prevent.
- **Focus lands on the first control of the prompt when the turn arrives** (B7); the whole turn
  is answerable from the keyboard (B6). ✅ **The focus ring and the tab order are already
  right** — P13.3 walked the page with `Tab` through the DevTools protocol and every stop was
  visible and outlined; what this packet adds is *moving* focus when the turn does.
- ⚠️ **This packet is where `MarkupStandardsTests.EveryHandlerIsOnARealControl` starts earning
  its keep.** P13.3 has no controls on the table at all, so the scan passes vacuously today. A
  discard is a card you click — **and a card you click is a `<button>`**, never a styled `div`.
- ⚠️ **Prerender-safe joining** (C13): `OnInitializedAsync` runs twice, and joining a table
  twice is a real bug.

**Acceptance.**
1. A person plays a full match against bots in a browser, several rounds, banks carrying over.
2. ⚠️ **A round is played end to end using only the keyboard**, and reported as such.
3. The leak test still passes with a *seated* viewer. ⚠️ **Amended after P13.2, which already
   covers it**: `ConcealmentTests` plays a round with four connected seats and asserts their
   `CardId` sets pairwise disjoint. What is left for this packet is that the *component* does
   not widen it — a seated player's page must render its own hand and nobody else's, from the
   `SeatPrompt` it was sent and from no other source.

**Done when.** §0's goal 2 has a second, better answer, and it was reached through the
multiplayer architecture rather than around it. ✅ **It has.**

**What it shipped.** `SeatBoard` — *your seat, as you have it* — is the private counterpart of
`TableBoard`, folded out of **your own** `SeatConnection` and nothing else; `TableHost` opens the
table with `--seat 1` yours and the rest the computer's (`--seat 0` gives back P13.3's room with
nobody in it); and three components draw it — `YourSeat`, `TurnPrompt` and `HandPanel`. **All
four questions are controls, all of them inside P13.3's single interactive island**, so the
render-mode standard is untouched. **86 questions answered in a real browser with no pointer at
all, over five rounds, with 393 tab presses walking the hand.**

**🔥 What it found.**

1. **A `CardId` names a card in a round's shoe, and the shoe is rebuilt every deal.** The first
   version of the cross-seat leak assertion compared whole matches and failed immediately — on
   the same physical eight of hearts being dealt to two different people in two different
   rounds. ⚠️ **Anything comparing hands across seats must compare them a round at a time.**
   `ConcealmentTests` never met this because it plays exactly one round.
2. **Your hand between turns is not stale, and it is worth rebuilding.** After you discard,
   your thirteen are fixed until your next turn — nobody draws from your hand, and what another
   player takes comes off your discard pile. So the resting hand is the prompt's hand minus the
   card thrown, with the near-melds worked out again over what is left. ⚠️ **The ownership it
   needs is read back off the `CardView`s you were sent**, never worked out again: a prompt
   carries the registry but not `TurnContext.YouOwn`.
3. **A refusal must not raise "something changed".** `SeatBoard.Answer` returning false while
   the question still stands is the ordinary case, and a client that answers on the change event
   — which is exactly what a component does — would answer the same refused question for ever.
   **It says something only if the question really moved on.** Found by writing the test that
   answers wrongly on purpose.
4. **Focus lands on the first control, and only the first control may capture a reference.**
   Blazor invokes an element-reference capture when the element is *inserted*, not on every
   diff, so a `@ref` inside a loop ends up holding the last card. The first card is rendered
   through its own branch and that is the one that captures. Verified by walking the page with
   `Tab`: every prompt in five rounds already had focus on a `<button>` before a key was pressed.
5. **The unattended seated table is why the deal no longer starts itself.** Every question a
   seat is asked spends the whole of its patience before the stand-in answers, so an unattended
   round is over an hour of nothing. A table with nobody in it still deals from boot (P13.3's
   *a table is a place, not a button*); a table with a seat waits for the first page.

---

#### P13.5 — A table, not a document ☑ done 2026-08-19 *(the layout pass)*

**Goal.** Make the browser client look like a table people are sitting at rather than a document
about a game they are playing. Nick's words: *"each player at a certain location at the table…
minimize the amount of prose… more icons/symbols, perhaps larger… and relegate the prose into a
log that is optional to view instead of being the main way to observe the info in the game."*

⚠️ **Renumbered on the owner's call before committing.** This was going to be P13.6, taken after
the lobby. It is P13.5 and the lobby is P13.6, because the two are close to orthogonal — the
lobby's work is in `TableHost`/`SeatBoard` ownership and this is markup, CSS and one presentation
helper — and doing the layout first means **the lobby arrives at a table worth joining** rather
than forcing a second redesign of everything it touches.

**What it built.**

- **`BurmesePoker.Presentation/TableRing.cs`** — `RingSeat`, `RingPlace`, `TableRing.Around`.
  🔥 **You are at the front of the table whichever seat you were dealt**, and the others go
  clockwise from your left. It is a pure function of the seating and of which seat is yours, so
  it is here rather than inline in a component: **an expression buried in Razor is unreachable
  from a test**, and this one has thirteen. The same call `CardOrder` made in P13.1.
- **The felt.** `TableView` is a five-column grid of *named areas* — `topleft top topright /
  left centre right / bottom` — and every seat is placed in one **by name, never by coordinate**.
  One template seats four, five and six; an area with no seat in it simply goes unused.
- **`TableCentre`** — the deck, the turned-up money cards, and the one discard the seat being
  waited on may take. These were three bordered boxes in a sidebar; they are the middle of the
  table, so they are drawn in the middle of it.
- **`TableLegend` and `AboutTable`** — the two disclosures the prose moved into.
- **The action bar.** `TurnPrompt` is one bar with at most two buttons, the main action first,
  and the rules citation behind a *"why?"*.

**The three decisions worth keeping.**

1. 🔥 **Whose turn it is became public, and that was the open question.** `TableBoard.IsActing`
   meant *whose move was last* — the nearest the public game got — and a felt with seats at
   positions cannot spotlight that without lying, because the glyph would point at the seat that
   had just finished. **The easy road was available and was not taken.** Whose turn it is leaks
   nothing: everybody at a real table can see who is being waited on, and the concealment is
   about what is *in a hand* (RULES.md §7.1). So `TableEvent.TurnBegan(Player, Round, Turn)` is
   **broadcast to everybody**, raised by `BoundedAgent` — the one decorator every seat already
   passes through, so a bot's turn is as public as a person's — once per turn rather than once
   per question. ⚠️ **`ConcealmentTests` was extended to cover it rather than merely still
   passing**: the event names a seat and carries nothing else, everybody is told the same one,
   and it is one per turn. Two mutations red.
   ⚠️ **`BoundedAgent` is outermost, which is why the spotlight lands before the pause** — a host
   wraps a bot in `PacedAgent` and hands it here, so a seat lights up and is *seen to think*.
2. 🔥 **The draw pile is counted rather than sent.** The middle of the felt wants to say how many
   cards are left. The shoe is 108 (§2), thirteen go to each seat and the turned-up cards come
   off it (§3), a blind draw takes one, and a reshuffle says how many the discards made (§5) —
   so a watcher can do the arithmetic, and `TableBoard` does. **The alternative was a count on
   `IGameObserver.PlayerDrew`, which is a domain change for a decoration.** Checked against a
   round the engine really played, two ways round.
3. ⚠️ **A discard pile is a pile, not a top card.** The felt shows the one discard on offer, and
   the board used to keep only each seat's most recent throw — so once somebody *took* a discard,
   the board went on showing a card that was by then in their hand. `TableBoard.DiscardPiles`
   folds `Discarded` and `TookDiscard`, finds the taken card **by identity rather than by
   assuming whose pile it must have been** (§3.1), and is emptied by a reshuffle.

**The prose budget, and where every sentence went.** *Prose leaves the screen; it does not leave
the page.* Every string removed from the felt went to exactly one of three places — the
accessible name of the thing it described, the round log, or a disclosure — and **never to a
`title=` or a hover** (§3.11 B9).

| Was on the felt | Went to |
|---|---|
| The page lede, three sentences | `AboutTable`, and the rules page |
| *"Seed 435220648 — every card…"* | `AboutTable`, with `--seed` beside it |
| *"Both copies of each of these pay their owner…"* | `TableLegend` |
| *"Every hand is concealed until somebody declares…"* | The rules page's new *At the table* section |
| *"($) pays its owner once and ($$) twice; ★…"* | `TableLegend`, once, for the whole table |
| **14 × *"melds nothing"* / *"breaks a meld — costs 3"*** | A `−3` badge on the card; the phrase into the card's accessible name |
| *"— taking it is not the same as being dealt it (RULES.md §4.4)"* | The action bar's *"why?"* |
| *"The others are playing. Your hand is below."* | A state marker on your seat |

🔥 ***"Melds nothing" was also wrong copy, and this is where it was fixed.*** It is a **cost**
label that read as a **status** label, and it appeared under a row headed `run of 3`,
contradicting it. It is a signed number now — `−3`, and **nothing at all when the answer is
zero**, because the group already says the card is loose — and the sentence it replaced is in
the accessible name of the very button that throws the card: *"seven of diamonds, a money card,
and it is yours — it pays you even if you throw it away, throwing it gives up nothing."*
⚠️ **An icon with no accessible name is worse than the prose it replaced**, so more symbols meant
*more* `.said` text, not less.

⚠️ **The ★ wording was rewritten mid-packet, after Nick asked what it was actually saying.** It
explained the *mechanism* — "the deck gave that one to you" — and left the reader to work out why
that deserved a glyph. It deserves one because **holding is not owning** (RULES.md §4.4) and that
is the only thing deciding who gets paid. It now says the consequence: *"Yours: it pays you at
settlement even after you throw it away. A money card without a star is one you are only holding
— it pays somebody else."*

##### What P13.5 found

1. 🔥 **A focus call can kill the circuit, and a dead circuit is a page that looks perfect.**
   §3.11 B7 moves focus when the turn does, through `ElementReference.FocusAsync`. ⚠️ **An
   `ElementReference` keeps its id after the element it names has gone**, so the emptiness guard
   only ever protected the first render. Everything after it raced: `OnAfterRenderAsync` decides
   to focus because a question is standing, and between that decision and the interop call
   reaching the browser the question can be answered — the buttons become spans and the call
   lands on nothing. **Blazor turns an unhandled interop exception into a torn-down circuit.**
   ⚠️ **Found by playing 1,429 turns and reading the server log, not by looking at the page**:
   the browser showed a hand, a prompt and a spotlight, and every card refused every press in
   silence. It is a race a quick human wins too. **Focus is best-effort by construction now**,
   and `MarkupStandardsTests` asserts every component holding an `ElementReference` catches
   `JSException` and `JSDisconnectedException` around it. **After the fix: 39 rounds, 473
   answers, no circuit failures — before it, dead in round 3.**
2. 🔥 **A hidden live region announces nothing, and this packet is the one that would have done
   it.** The round log became a panel you open in the same packet that increased how much the
   page leans on it. `display:none`, `[hidden]`, or a closed `<details>` would take the entire
   narration away from the people relying on it most (WCAG 4.1.3). **The list is always rendered
   and always live**; closing it clips it the way `.said` is clipped — off screen, still laid
   out, still in the accessibility tree — and the only thing that depends on being open is
   whether the box is a tab stop. Asserted, and two mutations red.
3. ⚠️ **A glyph is not automatically better than a word, and the joker cost two drawings to
   learn it.** The 🃏 character is a colour photograph at chip size, so it had to go. A jester's
   cap in SVG was the obvious replacement and was **wrong twice**: three lobes above a band reads
   as a **crown** whether the lobes are pointed or rounded — and ♛ already means *won the round*
   two panels away on the same felt — while tilting two of them down to hang like bells stopped
   it being a crown and made it an abstract blob at the twenty-odd pixels it is actually drawn
   at. **It is the letters `JKR`**, which is what a real joker card has printed on it. ⚠️ Settled
   by zooming into a screenshot at true size, which is the only thing that was ever going to
   settle it.
4. ⚠️ **A hidden twin is a duplicate, and three of them shipped before the accessibility tree was
   read back.** `.said` beside visible text makes a screen reader say it twice: *"Cobra, Cobra is
   waiting"*, *"Round log, open the round log, 1 lines"*, *"36 turns, in 36 turns"*. **A `.said`
   is for text the eye is given as a glyph — never for text the eye is given as text.** Found by
   reading the page's own accessibility tree out of the browser, and it is worth doing once per
   UI packet.
5. ⚠️ **`display: contents` is what lets a list of seats be a grid of positions.** The seats must
   stay one `<ol>` for semantics and become individual items of the felt's grid for layout.
   `role="list"` is kept explicitly on it, because an implicit list role has historically been
   dropped when the list box leaves layout.
6. ⚠️ **A ring that fits is not a ring that reads.** The felt stacks below **56rem** rather than
   at the width where the ring stops fitting: at around 780 px the outer columns squeeze a panel
   until the name in it truncates, and a table where you cannot read who is sitting where is
   worse than a list. Five and six seats also get a wider felt, because *"Khine Myat Zin (bot)"*
   is what decides how much room a seat panel needs.

##### What P13.5 deliberately did not do

- **The money cards you own but have already thrown away are not shown.** The legend now promises
  they still pay you (§4.4), and the felt cannot show them: `SeatPrompt` carries ownership only
  for the cards currently in your hand. A running *"★ 4 owned"* tally on your seat would need the
  server to send it. **Worth doing, and it is a server change rather than a layout one.**
- **No animation at all.** `prefers-reduced-motion` is honoured by a blanket rule in `app.css`
  that does not cross shadow boundaries, so anything that moves has to be written inside it. The
  pacing between bot turns is not motion and stays (§3.11 B10).
- **Chrome will not make a window narrower than 500 px**, so 360 was checked by measuring the
  layout in a 360 px content box — the same media-query branch that applies there — rather than
  by screenshotting a 360 px viewport.

**Acceptance.** ✅ **420 tests, up from 389, none removed.** ✅ Played at four, five and six seats
and with `--seat 0`; a whole turn answered with `Tab` and `Enter` and nothing else; 39 rounds
soaked with the banks summing to zero on every tick and no horizontal overflow at any width.

---

#### P13.6 — The lobby, and a second person ☑ done 2026-08-19 *(§0's goal 4, delivered)*

**Build.** Open a table, join it, the host fills the rest with bots, play. **The lobby is not a
domain concept** — it decides seating and names and then constructs what `Program` constructs
today.

- **Reconnection is UX, not an error path** (C16): a dropped circuit says *"Sable is playing
  your seat"* and lets the player back in. ✅ **The event already exists and the board already
  says it**: `TableEvent.SeatPlayedByTheComputer` is broadcast once a turn (P13.2) and
  `TableBoard` turns it into a line of narration (P13.3). What is left is saying it *in the
  seat* rather than only in the log.
- ⚠️ **`TableHost` becomes a lobby and stops being a singleton with one table in it.** It opens
  one table from configuration today, which is the whole of what P13.3 needed; a second table
  means a dictionary of them and a route that names one. **Nothing else in the client knows how
  many tables there are** — `TableView` takes a board and a connection.
- ⚠️ **Amended after P13.4, and this is the one thing that has to change rather than be added
  to.** `TableHost.Yours` is *one* `SeatBoard`, built when the table is opened, and every circuit
  that draws the page is handed the same one — which is right for solo play and wrong the moment
  two people are here. **A `SeatBoard` belongs to a viewer, not to a host**: the lobby decides
  who a circuit is, and the circuit builds its own `SeatBoard` over
  `TableSession.ConnectionFor(itsPlayer)` and disposes it. ✅ **The component is already written
  for that** — `YourSeat` reads its seat once in `OnInitialized` and unhooks in `Dispose`; only
  where the seat comes from changes.
- ⚠️ **And it is what makes the deal start itself again.** P13.4 stopped dealing at boot because
  an unattended seat spends its whole patience on every question. A lobby knows whether anybody
  is connected, so the honest rule — *deal while somebody is at the table* — becomes available
  for the first time. **Do not solve it by shortening the patience.**
- **Two people at one table is also the first time the hints toggle is per-viewer for real.** It
  already is: it lives in the component, and `TableOptions.Hints` decides only whether the
  server works a suggestion out at all (P13.4).

**Acceptance.** **Two people and two bots play a round over a network** — P13's original "done
when", finally reached with everything under it already tested.

**Done when.** §0's goal 4 is delivered.

##### What P13.6 found

1. 🔥 **A test that a stood-up seat refuses an answer is vacuous unless a question is standing
   in front of it.** The first version sat two people down at a table that was not dealing,
   evicted one, and asserted the ghost refused — which it did, because there was nothing to
   refuse. ⚠️ **Found by mutating `SeatBoard.Dispose` and watching the test stay green**, not by
   reading it. `TwoPeopleTests.AGhostCannotAnswerTheQuestionItWasLookingAt` runs a live round,
   waits until the seat really is being asked something, and only then reconnects. **The pair
   `_left` + `Asking = null` is red when both are removed**; either alone is redundant, which is
   deliberate — the flag is what closes the in-flight-notification race a test cannot schedule.
2. 🔥 **Two `<AntiforgeryToken />`s is worse than none, and the page renders perfectly either
   way.** `EditForm` emits one itself for a static SSR post; adding a second put two inputs of
   the same name in the form, the two arrived comma-joined, and every post was rejected with a
   bare error page. ⚠️ **Found by pressing the button** — P13.3's rule generalises from URLs to
   verbs: *ask the server for everything the page says it will do*, not only for everything it
   names.
3. 🔥 **A marker that means "away" has to stop meaning it.** `SeatPlayedByTheComputer` is
   broadcast once per turn, and marking the seat for the rest of the *round* told a player who
   had timed out once and come back that he was still away — in his own seat, while he was
   answering it. **Cleared at that seat's next `TurnBegan`**, which is the only moment the table
   has that means *we are about to find out*. ⚠️ **And a seat somebody walked away from is a
   different fact**: `TableBoard.Vacated` survives the deal, because a player who left is still
   gone, while a seat that was always the computer's is marked by neither — **nobody has gone
   anywhere at a bot's seat.**
4. ⚠️ **The lobby's sit-down form must not hide itself when the table is full.** Blazor keeps a
   dropped circuit for minutes before disposing it, so somebody who reloaded finds their own
   seat occupied by their own ghost — and a form that vanished would lock them out of the table
   they are sitting at. It is always shown; sitting down under a name already here takes that
   seat back. ⚠️ **A name is not a credential and the code says so out loud**: two people who
   type the same name take each other's seat. A lobby with accounts is beyond this plan.
5. ⚠️ **A claim on a table must not be made while the page is only being prerendered** (§3.11
   C13, which had been a warning about *joining* and is now about two things). Being counted as
   present and taking a seat are both claims, and a claim made twice is a bug rather than a
   wasted call — so both sit behind `RendererInfo.IsInteractive`, asserted by
   `ComponentDisposalTests.NothingIsClaimedWhileThePageIsOnlyBeingPrerendered`.
6. ⚠️ **A parameter handed to an interactive root component is serialised**, so `Table.razor`
   passes the table's **id** and the island looks it up in the lobby it is injected with. A
   `HostedTable` is not serialisable and must not be; asserted by
   `MarkupStandardsTests.OnlyTheTableIsInteractive`.
7. ⚠️ **A settlement is not a resting state.** With a one-millisecond gap between rounds the
   acceptance test caught round *seventeen* rather than round one, by which time the 240 lines
   the log keeps had trimmed away the two lines saying who had sat down. A table that stops
   between rounds is also what a person actually sees.
8. ⚠️ **`FocusOnNavigate` wins the race against §3.11 B7 on a reconnect, and that is left
   alone.** Landing on the table mid-turn puts focus on the `<h1>` rather than on the question
   standing in front of you — because a fresh page load is navigation, and focus belongs at the
   top of a document you have just arrived at. B7 is about the turn *moving*; the prompt is four
   tab stops away. **Written down so it is not rediscovered as a defect.**

##### What P13.6 changed rather than added to

- **`TableHost` is gone.** `Lobby` (a singleton) holds `HostedTable`s by id; `TablePlan` is what
  one is opened with. `TableView` injects the lobby and finds its table; every other component
  still takes a board or a seat, and **nothing else in the client counts tables**.
- **`SeatBoard` belongs to a viewer.** `TableView` sits down, holds it, hands it to `YourSeat`
  as a parameter, and stands up in `Dispose`. `YourSeat` no longer injects anything at all.
- **`TableSession` learned who is sitting where** — `SitDown`, `StandUp`, `RemoteSeats`,
  `WaitingFor`, `IsFull`, and the two events `SeatTaken`/`SeatLeft`. ⚠️ **The claim is the
  table's and not the lobby's**, because two viewers handed the same `SeatConnection` are two
  people answering one question, and a seat is a property of a table.
- **The table deals while somebody is at it** — at least one viewer attending *and* every seat
  either the computer's or somebody's. **The patience was not shortened**, which is what P13.4
  asked for.

---

> **What earlier sessions established, and it all still holds.**
> - ✅ **Confirmed by P13.2. `BurmesePoker.Sim/SeatRecorder.cs` is the shape P13.2's remote seat
>   wants** — a decorator over `IPlayerAgent` that watches every question and can refuse to
>   answer normally. The simulation uses it to give up on a stalled round; **P13.2's
>   `BoundedAgent` is the same shape bounded by the clock instead of by turns**, and the waiting
>   itself sits in `RemotePlayerAgent`. **No domain change either time**, which is §3.6's bet
>   paying off.
> - **The simulation is the load test.** Four to six players a table and ~20 ms of compute a
>   round with the whole search in it, so a host runs many tables per core. The parked-task cost
>   §3.6 accepted is not the thing to worry about.
> - **A dropped player's move already reads correctly to the table.** P11 gives every bot seat a
>   pace and a name, so a takeover *looks* like a seat playing.
> - ⚠️ **The console is bigger than it was and none of its drawing is reusable across a wire.**
>   Nine files, five from P11. That was P11's finding and P13.1 is the answer to it — but note
>   the correction above: the line to cut along is **decision versus drawing**, not
>   *`AnsiConsole` versus `IAnsiConsole`*.

### P14 — Game journals: record and replay ☑ done 2026-08-19

**Goal.** A game can be written down completely enough to be played back later — every
decision, by every seat, human or bot — so that a strategy analysis reads a file instead of
re-running code that has since changed, and a memorable hand can be kept.

**Read first.** **§3.9** (the decision this packet exists to deliver), §3.8 (the seams, and its
one still-open ⚠️ row), §3.7 item 4 (the allocation finding that decides the fidelity levels).
`STATUS.md`'s P12 notes for how the CSV's join keys work.

**Build.**

- **`Play/GameJournal`** — the record types, pure data, no I/O. A journal is a header (master
  seed, game seed, seating, strategy per seat, stakes, table size, and the rules revision it was
  played under) plus a flat list of **decisions**: `(round, turn, seat, question, answer)`. The
  four questions are exactly `IPlayerAgent`'s four. ⚠️ **Answers are recorded by `CardId`, not
  by value** — two decks mean a value is ambiguous and a replay that picks the other copy is a
  different game (§3.1).
- **`Agents/JournalingAgent`** — a decorator over any `IPlayerAgent` that appends what it was
  asked and what it answered. **This is `RecordingAgent`, lifted out of the test project rather
  than reinvented** (§3.8 item 2; P12 said the same about `SeatRecorder` and was right).
- **`Agents/JournalPlayerAgent`** — answers from a journal. Replay is then *playing the game
  with different seats*, which needs no engine change at all. ⚠️ It must **fail loudly** when
  the journal and the engine disagree — asked a question the journal does not have, or asked to
  discard a card the seat is not holding — because silent divergence would make a replay look
  successful while being a different game.
- **Two fidelity levels, and the expensive one is opt-in.** *Thin* records answers only, and is
  what a throughput run uses. *Rich* additionally snapshots the hand and the table at each
  decision, which is what makes the journal analysable without a replay. §3.7 measured this
  work to be allocation-bound, so **rich must never be the default**.
- **Serialisation as lines, writing as the consumer's job.** `JournalFormat` turns a journal
  into JSON Lines and back — one object per decision, streamable and greppable — and returns
  `IEnumerable<string>`, exactly as `CsvReport.Rows` already does. **`File.WriteAllLines` stays
  in `Sim` and `Console`**, so the domain still contains no I/O. ⚠️ **One format, in one place**:
  two consumers writing journals is fine, two consumers *defining* the format is not.
- **`--journal <path>` on both front ends.** `BurmesePoker.Sim` writes one journal per game (or
  per run, sharded); `BurmesePoker.Console` writes the match it just played, which is the only
  way a human game becomes reproducible at all.
- **A `replay` verb on `BurmesePoker.Sim`** — read a journal, play it back, and report. This is
  also how the acceptance test is driven.

**Acceptance.**

1. **A journalled game replays identically.** Play a seeded bot game, journal it, replay it with
   `JournalPlayerAgent` in every seat, and assert the two runs agree on every `RoundResult` —
   winner, payouts, turns — and on the observer stream. **This is the packet's whole claim** and
   it is mechanically checkable, unlike P11's.
2. **A replay is immune to the strategy changing.** Journal a game played by `GreedyBotAgent`,
   replay it with `SimpleBotAgent` installed in those seats, and assert the replay is unchanged.
   **This is the argument for the packet in a single test** (§3.9 point 2) — a seed fails it and
   a journal passes it.
3. **A journal round-trips through its format** — write, read, replay, same result.
4. **A corrupt or short journal fails loudly**, at the decision where it diverges, naming it.
5. ⚠️ **Thin journalling costs a measured, stated fraction of throughput.** Re-run P12's
   comparison with journalling on and report rounds/second against the recorded 51 serial and
   85–92 parallel. **If thin costs more than a few percent it is built wrong**; if rich costs a
   lot, say so and leave it off.

**Done when.** `dotnet run -c Release --project BurmesePoker.Sim -- --games 100 --journal
run.jsonl` writes a file that `-- replay run.jsonl` plays back to identical results, and a
console match played by a person can be replayed the same way.

> **Two things to resist.**
> - **Do not make the engine resumable.** Replay is a seat that answers from a file; a
>   mid-round save point is a different and much larger packet, and nothing here needs one.
> - **Do not put the journal in `MatchEngine`.** It keeps no history on purpose (§3.8), and a
>   journal is a consumer's artifact. If a consumer wants one it wraps the agents, exactly as
>   it already wraps them to count takes.

> **What this does *not* deliver, deliberately.** It is a file format and two decorators, not a
> database. Nothing here indexes, queries or aggregates journals — the CSV remains the thing
> analysis reads, and a journal is what you go to when the CSV raises a question it cannot
> answer. If a store is ever wanted, it wants this format to exist first.

**What it found.**

- **Every acceptance criterion is met, and the headline one is a `diff`.** `--games 20 --rounds
  2 --journal run.jsonl --csv run.csv` then `replay run.jsonl --csv replay.csv` produces
  **byte-identical CSV files** — 20 games, 40 rounds, every winner, payout, turn count, take,
  draw and claim. `Replay.Run` reuses `Simulator.Summarise`, so a replayed run and a played one
  are added up by the same arithmetic rather than by two implementations that agree until they
  do not.
- ⚠️ **The generator split described in §3.9 is the one thing a cold context must not undo.**
  `BurmesePoker.Console` now draws two `Random`s from `--seed`: `setup` seats the table, and the
  match's own is constructed from `setup.Next()`. Putting the seating back on the match's
  generator would break replay silently — the deal would be right and every card different.
- **Rich fidelity costs nothing measurable** (see §3.9): 48–50 rounds/s against a 46–49
  baseline, serially, three interleaved repetitions. It stays off by default because a journal
  is *kept*, not because it is slow.
- **Replaying is not re-running.** `Replay.OptionsOf` builds each strategy as one that throws
  if anybody tries to seat it, and the seats answer from the file. A journal whose strategies
  are renamed to something that has never existed replays unchanged —
  `AReplaySeatsNoStrategyAtAll` — which is §3.9 point 2 stated as a test rather than an
  argument.
- **A console match replays under the harness.** Played four bot seats at `--seed 4242 --pace 0
  --journal match.jsonl` through a pty; `Sim -- replay match.jsonl` reports the same two rounds
  the console printed — Sable out both times, +$16 in 29 turns then +$13 in 29 turns. Two runs
  at the same seed also produce byte-identical journals, so P11's determinism survived the
  split.
- **The format is the identity, not the object graph.** `GameJournal` and friends are plain
  records whose list members compare by reference, so the round-trip test compares *lines*.
  That is the right notion for a journal — it is only worth having because it survives being
  written down — but a later packet that wants to deduplicate decisions should know it, and add
  structural equality rather than assume it.
- **20 tests, 239 → 259.** Fourteen in `Play/GameJournalTests` — eleven facts and a three-case
  theory, covering replay, the format, both fidelities and four separate ways of failing loudly
  — and six in `Sim/JournalReplayTests`: identical rows, journalling being transparent to the
  game, nothing kept unless asked for, and the whole file round trip.

---

### P15 — A skill ladder ☑ done 2026-08-19

**Goal.** Several strategies whose strengths are *separated and measured*, so that "skill" is a
dial with more than two settings. Everything in P16 needs one, and today there are two
strategies whose only difference is a tie-break.

**Read first.** §3.7 (determinism, and no ambient randomness), P12's measured baseline in
`STATUS.md`, `Agents/GreedyBotAgent` and `Agents/CoverScore`. **P14 is done, so `--journal` is
available to every rung** — see the amendment under Acceptance 2.

**Build.** Four rungs at least, each a `Domain/Agents/` type behind `IPlayerAgent`:

- **`RandomBotAgent` — the floor.** Legal moves chosen arbitrarily. ⚠️ **It must take a
  `Random` handed to it**, seeded from the game seed, and never touch `Random.Shared` — §3.7's
  "no ambient randomness" is what makes a run reproducible, and one careless strategy breaks it
  for the whole harness. ⚠️ It is also the first strategy with **no monotone score**, so it may
  genuinely stall; `SimulationOptions.TurnCap` exists for exactly this and the abandonment must
  be **reported, not dropped** (P12).
- **`SimpleBotAgent`** — exists. Cover count, no tie-break. 19.3%.
- **`GreedyBotAgent`** — exists. Cover count plus the partner/joker tie-break. 30.7%.
- **At least one rung above greedy.** The one worth building is **`CautiousBotAgent`: greedy,
  but breaking ties towards the card least likely to help the player it feeds.** It is a better
  player *and* it is P16's intervention — see below — so one type earns twice.

⚠️ **Keep the rungs comparable.** P12's whole result rests on `SimpleBotAgent` differing from
`GreedyBotAgent` in exactly one decision and nothing else. Each new rung should change **one
thing** against the rung below it, or a difference in results attributes to nothing.

**Acceptance.**

1. Win rates **measured, separated and ordered**, with an interval — not asserted. State the
   games each number came from.
2. **Every rung is deterministic**: the same seed gives the same game, byte-identical, including
   the random one. ⚠️ **Still required, and P14 does not excuse it.** A journal reproduces a
   game a seed cannot — including a game played by a strategy that reached for
   `Random.Shared` — which makes it tempting to treat determinism as optional for the random
   rung. It is not: the harness's whole comparison rests on a run being a pure function of its
   master seed, and a journal is the record of one game, not a substitute for reproducing
   thousands. **What P14 does buy the random rung is that a surprising game of it can be kept
   and studied**, which is worth having and is not the same thing.
3. Every rung **terminates or is reported as abandoned**; no rung hangs the harness.
4. `MoneyCardsDoNotChangeWhatABotThrowsAway` holds for every rung — money is not a strategy
   input (RULES.md §4.4), and a new bot is the likeliest place for that to be got wrong.
5. **Every rung journals and replays**, which is one line of test each now that P14 exists
   (`AJournalledRunReplaysToTheSameRows` with `--strategies` pointed at the new rung). It is
   cheap and it catches the one thing a new strategy can break here: an agent that answers a
   question the four `IPlayerAgent` methods do not cover cannot be journalled at all, and this
   is where that would be discovered rather than in P16's analysis.

**Done when.** `--strategies random,simple,greedy,cautious` runs, and the four win rates are
separated by more than their intervals.

---

#### What P15 found

**All four rungs are built and every acceptance but the first is met outright.** The command
line takes `--strategies random,simple,greedy,cautious`; every rung is deterministic from the
master seed, including the random one; a table of pure chance is abandoned at the turn cap and
reported; money changes no rung's discard; and each rung journals and replays to the same rows.

**Acceptance 1 was a measurement, and the measurement came back with three separated levels
rather than four.** Head to head at four seats, where two strategies alternate and so each is
fed **exclusively by the other**:

| match-up | games | win rates | difference |
|---|---:|---|---|
| random vs greedy | 4,000 | 0.1% vs 49.9% | 49.8 pts |
| simple vs greedy | 4,000 | 18.5% vs 31.5% | 12.9 ± 1.5 pts |
| simple vs cautious | 4,000 | 18.4% vs 31.6% | 13.2 ± 1.5 pts |
| **greedy vs cautious** | **32,000** (two seeds) | **24.8% vs 25.2%** | **0.5 ± 0.6 pts** |

⚠️ **Read the last row differently from the others.** Two strategies alternating means each is
fed only by the other, which is *exactly* symmetric when they are of equal strength and
**amplifying** when they are not: the stronger one is fed by the weaker, and the weaker by the
stronger. So the `greedy` vs `cautious` row is a clean figure — that is the point of it — while
`simple` vs `greedy` carries P12's existing caveat, that some unknown part of the gap is the
feeding arrangement. (It is also a straight confirmation of P12's headline at twice the games:
31.5% against 18.5%, where P12 measured 30.7% against 19.3%.)

⚠️ **`cautious` is not distinguishable from `greedy`.** Two independent 16,000-game runs put it
0.6 and 0.4 points ahead; pooled, that is +0.48 ± 0.55 points, an interval that contains zero.
The ladder therefore has **four rungs and three skill levels**: *nothing* ≪ *cover count* <
*cover count + tie-break*, and then a fourth strategy that plays differently to no measurable
advantage.

**Why, and it is not a bug in the rung.** ⚠️ **Denial and self-interest point the same way.**
The partners a hand holds are exactly the partners an opponent cannot hold, so "least use to
me" and "least use to them" are very nearly the same ordering, and `CoverScore.Potential` has
already spent that information. Worse, both natural ways of writing the denial measure —
counting unseen partners, or counting the unseen *pairs* that would complete a meld — turn out
to be **`Supply(rank) − Potential` to within a point or two**, because the weights that make a
good keep-heuristic also make a good give-away heuristic. What is left over is only what a hand
cannot influence: how many runs a rank sits in at all (three for most ranks, two for A, 2 and
K) and the second-order blockages. That residue is what `CautiousBotAgent` plays on, and it is
worth about half a point.

⚠️ **A trap for anyone trying to do better: every *pairwise-additive* tie-break is greedy
again.** Partnership is symmetric, so the total partnership of the twelve cards kept is the
hand's total less twice the thrown card's — which means "throw the card with the fewest
partners" and "keep the best-connected twelve" are the *same rule*. A rung that improves on
greedy has to be combinatorial, not additive; the obvious candidate is counting **live outs**
(how many unseen values would raise the cover count of the thirteen kept), and the obvious
problem is that it costs a `PartialCover.Best` per value per candidate — roughly 100× a
decision today. That is a packet of its own, not a tie-break.

**And one result that belongs to P16, found by accident.** In the four-way run
(`--games 1000 --seats 4 --seed 20260819`) the rungs came out **random 0.1%, simple 27.9%,
cautious 33.3%, greedy 38.7%** — a 5.4-point gap between two strategies that are level head to
head. `Seating` rotates one fixed pattern in list order, and turn order is seat order, so in
*every* game **greedy was fed by simple and cautious was fed by greedy**. The gap is not the
strategies; it is the feeding. See the amendment under P16.

---

### P16 — Does the player before you decide your game? ☑ done 2026-08-19

**Goal.** Answer a specific hypothesis with a number and an interval.

> **The hypothesis** (Nick's friend, **2026-08-19**): *the main factor in the game is the
> relative skill of the player preceding you — if their discards give you an advantage, you
> win easily.*

**It is well-posed, and the rules make the mechanism exact.** Only the **immediately-previous**
player's top discard is ever available (`RULES.md` §5), so a table is a directed cycle: seat *i*
is fed by seat *i−1* and by nobody else. Every seat has exactly one upstream neighbour, and the
hypothesis is a claim about that edge. ⚠️ **This is a strategy question, not a rules question —
it does not go in `RULES.md`.**

**Read first.** §3.8 item 4 (join keys), §3.9, P12's `SimulationOptions.Seating`, P15 — and
**"What P15 found" above, which changed two things in this packet.**

> #### ⚠️ Amended 2026-08-19, by P15
>
> **1. There is already a lead, and it is a large one.** P15 measured `greedy` and `cautious`
> at **0.5 ± 0.6 points apart head to head** over 32,000 games — indistinguishable. In the
> four-way rotation they came out **38.7% and 33.3%**, 5.4 points apart. The rotation seats the
> strategies in list order and turn order is seat order, so in every game **greedy was fed by
> `simple` and cautious was fed by `greedy`**, and *the whole 5.4 points sits in that
> difference*. Two strategies of equal strength, differing only in who feeds them, five points
> apart.
>
> ⚠️ **This is a lead, not a result, and the reason is exactly this packet's control.** The
> downstream neighbour differs too — greedy feeds cautious, cautious feeds random — so
> "fed by a weaker player" and "feeding a weaker player" are still tangled together. But it
> sizes the thing: **if the upstream effect is real it is worth several points, not tenths**,
> which means 2,000 games per cell is comfortable and the experiment is worth running.
>
> **2. The intervention is weaker than this packet assumed.** It reads *"P15's
> `CautiousBotAgent` throws what is least useful to the player it feeds … the focal seat's take
> rate and win rate should both fall"*. P15 found that **denial and self-interest coincide**:
> the cards a hand least wants to keep are already the cards an opponent can least use, so
> `greedy` is *already* nearly a maximal denier and `cautious` improves on it by about half a
> point. **State the prediction in advance anyway** (acceptance 4) — but state it at the size
> P15 measured: swapping a greedy upstream neighbour for a cautious one should move the focal
> seat's take rate a little and its win rate barely at all. ⚠️ **If that intervention comes
> back large, something other than denial is doing the work**, and the observational half of
> the design is the thing to trust less, not more.
>
> **3. Three levels, not four, for the skill dial.** Vary the upstream neighbour across
> `random` / `simple` / `greedy` — those are separated by 12.9 and 36.9 points and are the
> honest settings. `cautious` is the intervention arm, not a fourth level.

**Build.**

- ⚠️ **First, fix the seating scheme, because the current one cannot answer the question.**
  `Seating(game)` is `Strategies[(seat + game) % Strategies.Count]` — a **rotation of a fixed
  pattern**. With two strategies at four seats it only ever produces `[A,B,A,B]` or `[B,A,B,A]`,
  so **every A is fed by a B and every B is fed by an A**. The pair *(my strategy, upstream
  strategy)* has exactly one value per strategy and the effect is **perfectly confounded** — not
  merely underpowered. P16 needs assignment to vary **independently of the seat**, which means
  enumerating assignments rather than rotating one.
- **Two columns on the CSV: `upstream_strategy` and `downstream_strategy`.** Both are pure
  functions of the seating the row already knows, but §3.8 item 4's rule is that a row carries
  its own join keys — a consumer that has to reconstruct the table to know who fed whom will
  eventually reconstruct it wrong.
- **The mechanism variable already exists.** `takes` — how often a seat took the discard offered
  rather than drawing blind — *is* "how useful was what my upstream threw". It is already in
  every CSV row. If the hypothesis is true, **`takes` is the path** the effect travels along,
  and a rise in win rate with no rise in takes would mean the effect is something else.
- **The experiment: a focal seat, and a control.**
  - Fix a **focal seat** to one strategy. Hold every other seat constant. Vary **only the
    upstream neighbour** across the P15 ladder. The change in the focal seat's win rate is the
    upstream effect, and nothing else moved.
  - ⚠️ **Then run the same design varying only the *downstream* neighbour.** Discards flow one
    way, so downstream skill should move the focal win rate *much less*. **Without this control
    the result is just "strong tables win more" and proves nothing about the edge.** This is the
    single most important part of the packet.
  - Hold the **seat index** fixed across cells, or rotate it and report both — seat 0 opens and
    is the only seat offered the money card (P12), so seat is a confounder in its own right.
- **The intervention, which is stronger than any observation.** P15's `CautiousBotAgent` throws
  what is *least* useful to the player it feeds. Seat it upstream of the focal seat and the
  hypothesis makes a directional prediction it can fail: **the focal seat's take rate and win
  rate should both fall**, while the cautious bot's own win rate should not. A correlation
  survives; a failed prediction kills the hypothesis outright.
- **Size the runs from the arithmetic, not by feel.** At a ~30% win rate, 2,000 games gives a
  standard error near **1 percentage point**, so a 3-point effect is comfortable, a 1-point
  effect needs roughly twenty times the games, and anything smaller is not worth claiming. A
  round is ~20 ms and a run does 50–90 rounds a second (§3.7), so **state the detectable effect
  size before running**, and report the interval next to every number.
- **Journals make the surprises answerable** (§3.9/P14): when a cell comes out strange, the CSV
  says *that* it did and a journal says *why*. ✅ **P14 is done, and it came out cheaper than
  §3.9 expected** — rich journalling costs nothing measurable in time (2× the bytes), so the
  cells worth understanding can be re-run at `--fidelity rich` and the hand behind every take
  or refusal is on disk. The mechanism variable this packet turns on is `takes`, and *rich* is
  what turns "it took 41% of the time" into "here is what it was holding when it did not".
- ⚠️ **Two P14 facts that constrain how this packet adds its columns.**
  1. **A journal header already records the seating**, strategy by seat, in turn order — so an
     enumerated-assignment scheme journals correctly with no change to P14's code, and
     `upstream_strategy` / `downstream_strategy` are derivable from a journal alone.
  2. ⚠️ **But `Replay.OptionsOf` reconstructs a run from the headers and reads only the master
     seed, the table size, the stakes and the strategy names.** Any new CSV column that comes
     from `SimulationOptions` rather than from the seating or the rows will make a replayed CSV
     stop matching a played one, and `AJournalledRunReplaysToTheSameRows` will fail. That test
     failing is the *correct* signal — the fix is to derive the column from the journal too,
     not to weaken the test.

**Acceptance.**

1. A **stated effect size with an interval** for upstream skill on win rate, and the same for
   downstream, from the same design.
2. The **confound is gone**: the analysis shows every *(focal, upstream)* cell was actually
   played, in balance, rather than assumed.
3. The **`takes` path is reported** — whether the effect travels the way the mechanism says.
4. The **intervention prediction is stated in advance and then checked**.
5. The whole thing is **reproducible from a master seed** and the run that produced the report
   is named in it.

**Done when.** The hypothesis has a number, an interval, and a verdict — including "smaller
than we can detect", which is a real answer and must be reportable as one.

> ⚠️ **A caveat this packet owes P12's headline.** The 30.7% vs 19.3% result was measured under
> `[greedy, simple, greedy, simple]` seating, in which **every greedy seat sat downstream of a
> simple seat and every simple seat downstream of a greedy one**. If the upstream effect is
> real, some unknown part of that 1.6× is the feeding arrangement rather than the tie-break. The
> number is not wrong — it is the honest answer to "what happens at that table" — but it is
> **not** a clean strategy-vs-strategy figure, and P16 is what would separate them. Re-run it
> under balanced assignment and report both. ✅ **Done below: 30.7/19.3 reproduced exactly, and
> the balanced figure is 29.6/20.4.**

---

#### What P16 found

**The hypothesis is false as stated, and true in a corner of the space that does not contain
competent players.** The answer has a number, an interval and a control:

> **Upstream skill is worth `+9.1 ± 2.1` points of win rate across the largest gap on the
> ladder — random against greedy — and `-1.0 ± 2.1` points across the gap between two thinking
> players. Who is at your table matters. Which side of you they sit on does not, unless they
> are not really playing.**

**The design.** Four seats. A focal `greedy` seat, `simple` in the two seats that are neither
focal nor varied, and one varied seat carrying the ladder — `random`, `simple`, `greedy`,
`cautious`. Two arms: the varied seat **immediately before** the focal seat, and the varied seat
**immediately after** it. ⚠️ **Both arms seat the same four strategies**, so the table's
composition is identical and the arms differ only in which way the discards flow between the
focal seat and the varied one. 4,000 games a cell, 8 cells, **two master seeds** (20260819 and
7): **64,000 games**. Each cell cycles its games through the four rotations of its pattern, so
the focal seat sits in each seat exactly 1,000 times — seat 0 opens and is the only seat offered
the turned-up money card (P12), and this removes it by arithmetic rather than by hope.

```
dotnet run -c Release --project BurmesePoker.Sim -- neighbours --games 4000 --seed 20260819
dotnet run -c Release --project BurmesePoker.Sim -- neighbours --games 4000 --seed 7
```

**The focal seat's win rate, by what sat beside it** (pooled over both seeds, 8,000 games a
cell, 95% intervals):

| in the varied seat | **upstream** (before the focal seat) | **downstream** (after it) |
|---|---:|---:|
| `random` | **50.9 ± 1.1** | 40.9 ± 1.1 |
| `simple` | 35.9 ± 1.1 | 35.9 ± 1.1 |
| `greedy` | 31.6 ± 1.0 | 30.6 ± 1.0 |
| `cautious` | 31.3 ± 1.0 | 30.4 ± 1.0 |

*(The `simple` row is identical in both arms to the last digit, and must be: `simple` is also
the filler, so at that level the two cells are literally the same table played from the same
seed. It is a free consistency check on the whole apparatus and it passed on both seeds.)*

**The contrast that answers the question** — each level against `greedy` in the same seat, then
**upstream less downstream**, which cancels everything that acts through the table's strength:

| level vs `greedy` | upstream effect | downstream effect (control) | **the edge itself** | takes |
|---|---:|---:|---:|---:|
| `random` | +19.4 ± 1.5 | +10.3 ± 1.5 | **+9.1 ± 2.1** ✅ | +1.6 ± 0.9 |
| `simple` | +4.3 ± 1.5 | +5.3 ± 1.5 | **−1.0 ± 2.1** ✗ | −0.8 ± 0.9 |
| `cautious` | −0.3 ± 1.4 | −0.2 ± 1.4 | **−0.2 ± 2.1** ✗ | −0.4 ± 0.9 |

**Four things follow, and the second is the finding.**

1. **The edge is real at the top of the gap.** Replacing the greedy player before you with one
   who is not thinking is worth **+19.4** points — but **+10.3** of that is simply the table
   getting weaker, because putting the *same* player *after* you is worth that much. What is
   left, `+9.1 ± 2.1`, is the edge. ⚠️ **Without the downstream arm the answer would have been
   19.4 and it would have been wrong by a factor of two.** The control was the most important
   part of the packet, exactly as the packet said it would be.
2. ⚠️ **Between two thinking players the edge is nothing.** `simple` and `greedy` are 12.9
   points apart in skill (P15) and sit in every real game. Moving that gap to the seat before
   the focal player changes its win rate by `−1.0 ± 2.1` points — an interval that contains zero
   and rules out anything above about one point in the hypothesis's favour. Meanwhile the
   *table* effect is unambiguous and symmetric: the same swap is worth `+4.3` upstream and
   `+5.3` downstream. **A weaker player anywhere at the table is worth four or five points to
   you. Which side of you they sit on is worth nothing.**
3. 🔥 **P15's 5.4-point lead does not survive the control, and now has an explanation.** In the
   four-way ladder run greedy (fed by simple) beat cautious (fed by greedy) by 5.4 points, and
   `STATUS.md` recorded that "all of it is who fed whom". **It is not.** It is the same
   four-or-five-point *neighbour* effect measured here, which the observational design could
   not tell apart from an upstream one because a rotation moves both neighbours at once. The
   lead was real and its interpretation was wrong — which is what the control exists to catch.
4. ⚠️ **The mechanism variable barely moves, which is a finding in itself.** `takes` was the
   predicted path: if better discards reach you, you take more of them. Across the contrast
   where the win rate moves **9.1 points**, the focal seat's take rate moves **1.6 ± 0.9**. The
   effect is real and in the right direction and it is **an order of magnitude too small to be
   the channel**. What upstream skill changes is evidently *what* is offered, not *how often*
   something worth taking is. ⚠️ **That is precisely the question a rich journal answers and
   the CSV cannot** (§3.9/P14): re-run the two `random`-upstream cells at `--fidelity rich` and
   the hand behind every take is on disk.

**The intervention, predicted in advance and then checked** (P16 acceptance 4). The prediction,
written into this packet by P15 before the run: *swapping a greedy upstream neighbour for a
cautious one should move the focal seat's take rate a little and its win rate barely at all.*
**The win-rate half is confirmed and the take-rate half is not observed**: the focal seat's win
rate moved `−0.3 ± 1.4` and its take rate `−0.3 ± 0.9`, both indistinguishable from zero, and
the directional figure is `−0.2 ± 2.1`. `CautiousBotAgent` also did not pay for denying —
its own win rate in the upstream seat was 31.8% against greedy's 31.3% there. **A strategy
built to deny the player it feeds cannot be shown to deny them anything**, which is the
strongest form of P15's finding that denial and self-interest point the same way.

**P12's headline, re-run under balanced assignment** (8,000 games, seed 20260818, four seats):

| seating | greedy | simple | gap |
|---|---:|---:|---:|
| rotated `[g,s,g,s]` — P12's arrangement | **30.7%** | 19.3% | 11.4 pts |
| balanced — all 16 assignments | **29.6%** | 20.4% | 9.2 pts |

P12's numbers reproduce **exactly** at four times the games, and **the rotation flatters greedy
by 1.1 points a seat — about a fifth of the gap.** ⚠️ **The honest strategy-vs-strategy figure
at four seats is 29.6% against 20.4%**, and `--seating balanced` is how to get it. The rotation
is not wrong; it answers "what happens at *that* table", and that is a different question.

**What the design can and cannot resolve.** At 4,000 games a cell the standard error on a cell
is about 0.75 points, on an arm effect about 1.05, and on the directional figure about 1.5 —
so one seed resolves a directional effect of about **3 points** and the two seeds pooled about
**2 points**. Anything smaller than that is reported as "inside the interval" and is not
claimed. **"Smaller than we can detect" is a real answer** and rows 2 and 3 of the contrast
table are it.

**What was built, and what it cost the rest of the tree.** `SimulationOptions.Assignments` (an
explicit list of seatings, cycled by game — null keeps the rotation), `SeatingPlan`
(`Balanced` = every assignment there is; `Rotations` = one pattern walked round the cycle),
`Measurement` (a mean over **games**, with the standard error of that mean),
`NeighbourExperiment` / `NeighbourCsv`, a `neighbours` verb and a `--seating balanced` flag.
**Two CSV columns**, `upstream_strategy` and `downstream_strategy`, derived from the seating and
never from the run's options — which is what let a replayed journal produce them too. **The
domain did not change by a line, and neither did `Simulator`, `GameRunner` or `Replay`.**

---

### P17 — The tournament: a harness that ranks players ☐

**Goal.** Turn "run these strategies and read the table" into "**rank every player against every
other, with an interval you can trust, from one command, re-runnably**". Everything after this
packet is a measurement, and today the harness cannot make one honestly at the size the
questions need.

**Read first.** §3.7 (determinism, no ambient randomness), §3.8 (statistics are collected, never
computed by the domain), §3.12, P15's *What P15 found* and P16's *What P16 found* —
between them they are the two ways this apparatus has already been wrong.

**Depends on.** P12, P15, P16. Sim only; **no domain change, and none is expected.**

**Why it comes first.** 🔥 **The default report prints no interval at all.** A balanced run on
2026-08-19 gave `greedy` 36.1% and `cautious` 36.8% over 2,048 games — two numbers a reader
will rank, and which P15 needed **32,000 games** to establish are the same number. The harness
already knows how to say `± 2.1` (`Measurement`, built for P16's cells) and says it in exactly
one verb. **A ladder built on point estimates is a ladder built on nothing.**

**Build.**

- **Intervals on the ordinary run.** Every figure in the `Report` table becomes a
  `Measurement` — a mean over **games**, one value per game (§3.8, and `Measurement`'s own
  remark on why the game and not the turn or the seat).
- **`Measurement.Paired`, and the common random numbers it rests on.** `SeedSequence.GameSeed`
  makes game *i* the **same shoe in every cell of the same master seed**, whoever is seated —
  so a comparison between two cells is a paired one, and `Measurement.Difference` says in its
  own remark that it is throwing that away to be conservative. Pairing is free variance
  reduction and it is available today. ⚠️ **It must take the per-game values, not two finished
  `Measurement`s** — pairing two cells that did not share seeds is silently wrong, and a
  signature that cannot express the mistake is the only defence.
- **`tournament`, a fifth verb.** Round-robin: every unordered pair of the named strategies gets
  a balanced-seating cell of *N* games at the given seats. Out comes a **matrix** (row beats
  column by *x* ± *y* points), a **ranking**, and a CSV row per cell. The free-for-all — every
  strategy at one table, which is what `--seating balanced` already plays — is reported beside
  it, because the two answer different questions and P16 is the packet that learned they differ.
- ⚠️ **A round-robin manufactures findings, and this is the packet that has to refuse to.** *k*
  strategies is *k(k−1)/2* comparisons; at 95% intervals roughly one in twenty clears zero by
  chance, so six pairs make a false finding likelier than not over a few runs. **Report the
  comparison count and a Holm-corrected verdict beside the raw one.** An uncorrected
  round-robin is a machine for promoting noise to a rung.
- **`suite`, a sixth verb**: plays the standing set of measurements the docs quote and writes
  them to `docs/strategy/measurements.csv`. 🔥 **The numbers in the documentation stop being
  transcribed and start being generated.** Same discipline as `RULES.md`'s provenance tags — a
  number without its origin is not evidence (§3.12).

**Acceptance.**

1. `tournament --strategies random,simple,greedy,cautious` prints a matrix and a ranking; every
   cell states its games and every difference carries an interval.
2. 🔥 **The harness's own null test: a strategy against itself measures 1/seats within its
   interval** — 25.0% at four seats. It costs one cell and it is the test that would have caught
   P16's seating artifact from the inside.
3. **Paired intervals are narrower than unpaired ones on the same data, and the two means agree
   to rounding.** State the ratio. If pairing does not narrow them, the pairing is wrong and
   *that* is the finding.
   ⚠️ **Amended by P17, which found this half right and half backwards.** Pairing does not mean
   *narrower*; it means *the correlation that is actually there*. **Across cells** — one strategy
   at two tables of the same master seed — the shoes are shared, the correlation is positive and
   the interval narrows, which is the variance reduction this bullet was written about. **Within
   a cell** — two strategies at the *same* table — exactly one seat declares, so the two series
   are strongly negatively correlated and the paired interval is **wider**: there the
   add-the-variances formula is not conservative but *anti-conservative*, and the paired one is
   the honest answer. Both directions are measured and reported; see "What P17 found".
4. The comparison count and the corrected threshold are printed. A raw "separated" that does not
   survive Holm is shown as not surviving it.
5. Deterministic: the same seed writes a byte-identical CSV, and `--serial` agrees with parallel.
6. The four existing rungs are **re-measured** and land in `docs/STRATEGY.md` (created here),
   each with the command that produced it.

**Done when.** One command produces the ranking table `docs/STRATEGY.md` quotes, and re-running
it reproduces the file's data byte-for-byte.

---

#### What P17 found

**Built and every acceptance met, one of them by correcting the acceptance.** `tournament` and
`suite` are the fifth and sixth verbs, the ordinary report carries an interval on every figure,
and `docs/strategy/measurements.csv` is **generated**: two independent runs of

```
dotnet run -c Release --project BurmesePoker.Sim -- suite --strategies random,simple,greedy,cautious --games 8000 --seed 20260819
```

wrote a **byte-identical** file, at 15 minutes and ~64,000 games each. `docs/STRATEGY.md` quotes
that file and nothing else.

🔥 **The finding that changed the code, and it was found by reading a number that should not have
moved.** "Every figure becomes a `Measurement` — a mean over games, one value per game" was built
literally, and the balanced headline came back **30.6% / 19.3%** where P16 published **29.6% /
20.4%**. Neither was wrong: they are **two different estimands**. A strategy holds a different
number of seats in different games of a crossed run, so the *unweighted average of the per-game
ratios* is not the *ratio of the totals*, and it over-weights the games where the strategy held
fewest seats — which for a strong strategy are the games it does best in. ⚠️ **The gap was 1.05
points, the same size as P16's entire rotated-versus-balanced effect.** Adding an interval to a
figure must not change the figure (§3.8 item 4), so `GameValue` carries **a total and the trials
it is out of**, and `Measurement.Of` is the textbook **ratio estimator**: the totals divided,
with a standard error built from the per-game residual `total − ratio × trials`. The game is
still the trial. Where the denominator is constant it reduces *exactly* to the per-game mean, so
a rotated run reads the same either way — and the balanced headline now reproduces
**29.75% / 20.25%**, P16's number, with an interval on it for the first time.

🔥 **Acceptance 3 was half backwards, and the correction is a real fact about this game.**
Pairing is not a synonym for a narrower interval; it measures the correlation that is there.

- **Within a cell** — two strategies at the *same* table — **exactly one seat declares**, so the
  two series are strongly negatively correlated and the paired interval is **wider**. Measured
  across the three cells of thinking players the ratio is **1.408, 1.409 and 1.414** — √2 to
  three digits, which is what a perfectly opposed pair of equal-variance series predicts. ⚠️ **So
  the add-the-variances formula is not conservative here but *anti*-conservative**, understating
  a four-seat head-to-head margin by 41%. (In the cells containing `random` the ratio is 1.01:
  a series that is almost always zero has almost nothing to correlate with.)
- **Across cells** — one strategy at two tables of the same master seed — the shoes are shared,
  the correlation is positive, and pairing **narrows**: **0.57 to 0.95**. That is free variance
  reduction, and it is what common random numbers are for. ⚠️ **Both cells must seat the strategy
  the same way round**; a head-to-head cell enumerates its seatings as an odometer over its two
  players, so comparing a cell it leads with one it follows in moves it round the table and
  throws the shared shoe away.

**The measured ladder, 8,008 games a head-to-head cell, 95% intervals, points of win rate:**

| | random | simple | greedy | cautious |
|---|---:|---:|---:|---:|
| **random** | · | −49.74 ± 0.43\* | −49.86 ± 0.42\* | −49.88 ± 0.42\* |
| **simple** | +49.74 ± 0.43\* | · | −11.24 ± 0.99\* | −10.75 ± 0.99\* |
| **greedy** | +49.86 ± 0.42\* | +11.24 ± 0.99\* | · | −0.20 ± 1.02 |
| **cautious** | +49.88 ± 0.42\* | +10.75 ± 0.99\* | +0.20 ± 1.02 | · |

\* survives Holm at α = 0.05 over the family of six.

⚠️ **`greedy` and `cautious` are `−0.20 ± 1.02` apart under a design P15 never ran, which is the
strongest confirmation of P15's finding yet**: the two rungs are level head to head, level in the
free-for-all (35.8 ± 0.9 against 36.5 ± 0.9) and level in the ranking (+20.30 against +20.28
mean margin). **Three separated skill levels, four rungs**, and the corrected verdict says so
rather than a reader's eye.

🔥 **The correction earns its place on this very run.** Six comparisons, thresholds
0.05/6 … 0.05/1. Five p-values are below 1e-99 and the sixth is **0.70** — so nothing was
demoted here. ⚠️ **That is the point, not a reason to drop it**: the same run reports
`cautious` ahead of `greedy` in the free-for-all by 0.66 points and behind it in the ranking by
0.02, and a reader who ranks point estimates would have read a rung out of two coin flips.

**The harness's own null test passes.** `cautious` against a copy of itself under another name:
**25.15% ± 0.51 and 24.85% ± 0.51** of 8,008 games, both containing the fair share of 25.0%,
with the paired margin inside its interval. It costs one cell out of eight and it is checked on
every run — and on every `suite`, which exits non-zero if it ever fails.

**P12's headline, re-measured with an interval for the first time** (seed 20260819, 8,000 games
rotated and 7,500 balanced):

| seating | greedy | simple | gap |
|---|---:|---:|---:|
| rotated `[g,s,g,s]` | **31.00 ± 0.53** | 19.00 ± 0.53 | 12.0 |
| balanced — all 16 assignments | **29.75 ± 0.47** | 20.25 ± 0.47 | 9.5 |

P16's figures were 30.7/19.3 and 29.6/20.4 at a different master seed; both reproduce inside the
intervals this packet can now print, and **the rotation flatters `greedy` by 1.25 points**
against P16's 1.1.

**What this apparatus can and cannot resolve.** At 8,008 games a cell the standard error on a
head-to-head margin between two thinking rungs is **0.52 points**, so the 95% half-width is
**1.02** and anything smaller than about a point is reported as inside the interval. ⚠️ **To
resolve half a point — the size of P15's `cautious` effect — costs about 34,000 games a cell**,
which is four hours of the machine this was measured on and is exactly the 32,000 P15 had to
run. **Any later rung worth less than a point is not promotable at the default run size**, and
the packet that proposes one has to say what it will cost to measure.

⚠️ **Two things left for a later packet, deliberately.** (1) `neighbours` still computes its
effects with `Measurement.Difference`, whose own remark says it is being conservative about a
pairing it could have taken — `Measurement.Paired` exists now and would narrow P16's intervals,
but re-running would change published numbers and belongs with a re-measurement, not with this
packet. (2) `NeighbourCell.TakeRate` averages per-game ratios unweighted, which is the estimand
this packet just moved away from everywhere else; its win rate is unaffected, because a cell has
exactly one focal seat and so a constant denominator.

⚠️ **One display defect fixed in passing, because it is one shared helper and it changes no
number.** A .NET two-section format picks its section from the **rounded** value, so `-0.04`
under `"+0.0;-0.0"` prints as **`-+0.0`** — the positive section supplies the plus and the
runtime prepends the minus. `ReportNeighbours` had the same latent bug in every signed column.
Signed figures are now built by hand.

---

### P18 — One catalog: the same players everywhere ☑ done 2026-08-19

**Goal.** A bot is **named in one place**, and every front end resolves the name. No new
strategy, no behaviour change — the enabling refactor that makes every later rung land in the
console, the browser and the harness at once instead of three times.

**Read first.** §2 (what each project may reference), §3.8 item 4 (a strategy name is half of
every row's join key), `Sim/Strategy.cs`, `Console/Program.cs`'s private `Difficulty` enum.

**Depends on.** Nothing. Can be built before P17 if preferred; it is put second only because
P17 is what the programme rests on.

**The state it is fixing.** There are **four independent notions of "which bot"**:
`Sim.StrategyCatalog` (four rungs, names that CSV rows join on), `Console.Program.Difficulty`
(a private two-value enum mapping to two `new`s), `Web.HostedTable` (`new GreedyBotAgent()`,
twice, hard-coded — **the browser has no difficulty setting at all**), and
`Server.TableOptions.StandIn`. A fifth rung today means editing four places and the browser
still would not offer it.

**Build.**

- **`BurmesePoker.Domain/Agents/BotCatalog.cs`** — the ladder, ordered: a name, a one-line
  description **written for a person choosing an opponent** rather than for a reader of the
  plan, and a factory taking a seat seed. Domain-resident because agents already are and this
  names them; no I/O, so it breaks no layering rule.
- `Sim.StrategyCatalog` becomes an adapter over it. ⚠️ **The four existing names may not
  change** — §3.8 item 4: rename a strategy and yesterday's numbers stop joining.
  ⚠️ **Added by P17: a bot name is not the only thing wearing a strategy name.** The null cell
  seats one strategy twice under `"{name}#mirror"` (`Tournament.MirrorSuffix`), which is a
  *label* and not a rung. The catalog must refuse a name containing `#`, and `BotCatalog` must
  not be the thing that resolves a mirror — a front end offering `greedy#mirror` as an opponent
  would be absurd, and the suffix was chosen to be unusable precisely so this is easy to hold.
- Console's `Difficulty` enum is **deleted**; the prompt is built from the catalog, and the
  journal header's bot name comes from the same place it already writes (`"simple"`/`"greedy"`).
- Web: `TablePlan.Difficulty`, a `--difficulty` argument, and **the choice on the lobby's
  open-a-table form**. `HostedTable` builds its stand-in from the catalog.
- ⚠️ **The hint is not the difficulty.** `RemotePlayerAgent`'s advice stays the *strongest* bot
  whatever the table's opponents are set to — a hint that got worse as you lowered the
  difficulty would be absurd, and the code should say so out loud where somebody would otherwise
  "fix" it.

**Acceptance.**

1. ✅ Console and Web both offer the whole ladder, from one list, by name.
2. 🔥 **`scripts/drive-console.py` at a fixed seed is byte-identical before and after** for the
   same choice. This is a refactor and `cmp` is what proves it — P13.1 built the tool for
   exactly this.

   ⚠️ **Amended while building it, twice, and both amendments are findings rather than
   excuses.**

   - **The whole capture cannot be identical, because acceptance 1 changes the prompt.** Two
     entries became four, so the comparison is of the *match* — every byte from the `Seating:`
     line to the end — and the prompt above it is expected to differ. ✅ **It is identical:
     6,625 bytes for `--script bots` and 89,233 for `--script human`.**
   - 🔥 **"The same choice" had to be *made*, because the old prompt was not choosing what it
     looked like it was choosing.** A `SelectionPrompt<T>` opens on `default(T)` when that value
     is one of the choices, and the deleted enum was `Easy = 0` — so the list read *Hard,
     Easy*, and everybody who pressed return got the **easy** bot. Measured with a probe prompt
     offering `(7, 0, 5)`, which comes back `0`. A rung is a reference type, `default` is null,
     and the cursor stays on the first entry. **`--pick n` was added to the driver** so a
     capture names the rung it played; the pre-P18 captures compare against `--pick 2`
     (`simple`). ⚠️ **P19 inherits this the moment it makes the choice a value type again.**
3. ✅ Sim's CSV strategy names are unchanged; a P16 CSV from yesterday still joins.
   `BotCatalogTests` freezes the four names and the ladder order in a literal, and asserts the
   harness's catalog is the same list in the same order.
4. ✅ A grep test: no project outside `Domain/Agents` constructs a concrete agent type.
   ⚠️ **It landed in `LayeringTests` rather than `MarkupStandardsTests`** — the claim is about
   all seven projects and not about markup, and it reads as a layering rule beside "the domain
   does not reference Spectre". ⚠️ **It bans constructing a *rung*, and the types come from
   `BotCatalog` rather than from a list in the test**, so a fifth rung is covered the day it is
   added; decorators and stand-ins (`PacedAgent`, `JournalingAgent`, `JournalPlayerAgent`,
   `SeatRecorder`, `RemotePlayerAgent`) are not ways of playing and are untouched.
5. ✅ The journal header records the catalog name for a bot seat, unchanged for the two that
   already existed — a P14 journal from yesterday still replays. Checked by replaying a journal
   written by the *old* console beside one written by the new one at the same seed and rung:
   the two reports are the same.

**Done when.** ✅ `dotnet run --project BurmesePoker.Web -- --difficulty simple` deals a table of
the easy bot, and the lobby form offers the same choice. **Verified in the real browser** — the
house table says *the computer plays simple*, and a table opened from the form on `random` deals
`random` and says so in the lobby. (P13.6's rule: found by pressing the button.)

**What P18 found.**

1. 🔥 **The console's difficulty default was a lie, and had been since P10.** See acceptance 2:
   `Hard` was listed first and `Easy` was what return picked. Nobody noticed because nothing
   printed which bot it seated — the journal header is what finally said so out loud.
2. ⚠️ **A record's `with` expression does not re-run a property initialiser.** `BotRung.Name`
   validated in an initialiser refused a `#` in a constructor and allowed one in
   `rung with { Name = … }` — which is *exactly* how `Tournament` makes its mirror. The check
   moved into the `init` accessor. **Found by writing the test that says a mirror is not a
   rung**, not by reading the code.
3. ⚠️ **Two orders, and neither is derivable from the other.** `BotCatalog.All` is ladder order
   (what was added), `ByStrength` is menu order (what was measured), and `greedy`/`cautious`
   share a level because P17's verdict on them is *inside the interval*. A `Strength` ordinal
   rather than a number: it is a reading of `docs/strategy/measurements.csv`, not a copy of it.
4. ⚠️ **The driver's difficulty question is not the same key in both scripts** — a match with a
   person in it asks that person's *name* first, so it is the fourth prompt there and the third
   when every seat is the computer's. A `--pick` that was right for `--script bots` silently did
   nothing for `--script human`, and the capture looked like a failed refactor.

---

### P19 — Difficulty as a dial, not a list ☑ done 2026-08-19

**Goal.** A difficulty setting a person can **feel**, monotone by construction, calibrated by
measurement rather than by assertion, and **independent of whether any later research packet
succeeds**. This is where the difficulty system is finished.

**Read first.** §3.12, P15's *What P15 found* (why a plausible rung is worth nothing until
measured), `RandomBotAgent`'s remark on why it declares whenever it may, P17's calibration
output.

**Depends on.** P17 (nothing is calibrated without it), P18 (nothing is exposed without it).

⚠️ **What P18 left on the table for this packet, and what it changed about it.**

- **The plumbing is done and the shape is fixed.** A level goes in a `BotRung` — name,
  description, `Strength`, and a factory taking a seat seed — and *everything* already resolves
  one: `Console`'s prompt, `Lobby`'s `--difficulty`, the lobby form's `<select>`, `TablePlan`,
  `HostedTable`, `Sim`'s `StrategyCatalog`, the journal header. **A level added to the catalog
  arrives in all of them without a line of front-end work.**
- ⚠️ **But `BotCatalog` today is the ladder, and P19's levels are not.** §3.12 says these are two
  jobs; P18 built one list because there was one kind of thing in it. When levels arrive there
  are two — *rungs* (a research instrument, ordered by what was added) and *levels* (a product,
  ordered by measurement and evenly spaced). **Decide which list a front end offers**; the
  strong default is that the console and the lobby offer **levels only** and the harness offers
  **rungs**, with `BotRung.Strength` retired in favour of the level's own ordering. What must
  *not* happen is a menu with both in it.
- ⚠️ **`Difficulty` on `TablePlan` is a single name today** and P19 wants it per seat. The
  single-value shorthand is therefore the *existing* field, and the assignment is the new one.
- 🔥 **A `SelectionPrompt<T>` opens on `default(T)` if it is one of the choices.** P18 deleted an
  enum whose `Easy = 0` meant everybody who pressed return at the console got the easy bot while
  the list said *Hard* first. **If a level is an enum or any other value type, this is back** —
  and this time it would be a difficulty dial silently defaulting to its bottom.
- ⚠️ **`ComputerAdvice` and `TableOptions.StandIn` are `BotCatalog.Hardest` on purpose**, in
  three places that each say so. When the hardest thing in the catalog stops being a rung and
  becomes a level, that expression still has to mean *play as well as you can* rather than
  *play at whatever the table is set to*.

**The design decision, stated before the code.** 🔥 **Skill rungs are not difficulty levels.**
The ladder is discrete and its rungs are far apart — 0.0%, 26.7%, 36.1% at four balanced seats —
so there is no way to ask for *a bit easier*. Worse, a lower rung plays a **different and worse
idea** (`simple` throws whatever it reaches first), which reads as an alien opponent rather than
a weaker one. **A weaker player plays the right idea and slips.** So difficulty is the strongest
rung with a **mistake rate**, and the ladder is what the mistakes are made *against*.

**Build.**

- **`FallibleAgent(inner, mistakeRate, Random)`** — a decorator. With probability ε it plays a
  plausible worse move; otherwise it defers entirely.
  - ⚠️ **A mistake is a legal, plausible move, not a random one.** Throwing a joker is not a
    mistake anybody makes; throwing the *second*-best card is. So the mistake is "the next card
    down the inner agent's own ordering", which needs `CoverScore.Discard` to be able to return
    a **ranking** rather than a winner — a small shared change, and the same shape P20 and P21
    both want.
  - ⚠️ **It never fumbles a declaration.** Refusing a won hand is not a worse player but a
    different game — `RandomBotAgent` already settles this and the reasoning carries.
  - Seeded from the seat seed. **Never `Random.Shared`** (§3.7 item 1) — one careless decorator
    takes every other seat's reproducibility down with it.
- **Named levels in the catalog**, each a rung plus an ε. ⚠️ **The ε values are set by
  measurement in this packet, not guessed**; the packet is not done until they are calibrated.
- ⚠️ **Difficulty is per seat.** A table may mix levels, because a table of four identical bots
  is the least interesting table in the game. `TablePlan` takes an assignment with a
  single-value shorthand, and P13.6's named bots pair a name with a level so a seat reads as a
  character rather than a setting.

**Acceptance.**

1. 🔥 **Monotone, measured and separated**: every level's win rate at a stated reference table is
   ordered, and adjacent levels are separated by more than their **paired, Holm-corrected**
   intervals. ⚠️ **A level that is not separated from its neighbour is deleted, not shipped** —
   P15's lesson applied in advance. Three real levels beat five imaginary ones.
   ⚠️ **Amended by P17, which built the instrument and measured what it costs.** Three things
   follow and all three are cheap to get wrong. (a) `tournament --strategies easy,medium,hard`
   is the check, and the margin it prints is already the **paired** one — within a cell that is
   *wider* than the independent formula by about √2, so a level pair that looks separated under
   the old arithmetic may not be. (b) **The family is the adjacent pairs, not the round-robin.**
   Only "does *n+1* beat *n*" is a claim the menu makes, so correct over the *k−1* adjacent
   comparisons; correcting over all *k(k−1)/2* is a needless power loss, and `Holm.Correct`
   takes whatever family it is handed. (c) **The spacing has a floor.** At 8,008 games a cell
   the 95% half-width on a margin between two thinking players is **1.02 points**, so ε values
   whose levels are closer than about a point cannot be separated at the default run size —
   space them further apart, or state the games. ⚠️ **This is the acceptance most likely to
   fail**, because ε is a dial that can be turned to any value and the temptation is five
   levels.
2. A test asserts the **published** calibration's ordering against a re-run at a size a test can
   afford. The ladder may not silently invert.
3. **ε = 0 is byte-identical to the undecorated rung.** A decorator that does nothing must be
   transparent, and this is the cheap mutation-proof test of the whole mechanism.
   ⚠️ **P18 makes the console half of this cheap**: `scripts/drive-console.py --pick n` names the
   rung a capture played, so *"level `hard` at ε = 0 plays the match `greedy` plays"* is a `cmp`
   of two captures from the `Seating:` line on, exactly as P18 proved its own refactor.
4. Every level is deterministic, journals and replays (P15 acceptance 2 and 5).
5. `MoneyCardsDoNotChangeWhatABotThrowsAway` holds for the decorator too — a new wrapper is the
   likeliest place for money to leak into a discard (RULES.md §4.4).
6. Both front ends expose it, and the browser lobby can open a **mixed** table.

**Done when.** A table can be opened at any level from either front end, the levels are
measurably ordered, and `docs/STRATEGY.md` publishes the calibration **with the command that
produced it**.

⚠️ **P20–P22 are independently droppable, in preference order.** They widen the ladder; none of
them is required for the difficulty system to exist and be good. Stopping after P19 leaves a
finished, calibrated, honest difficulty setting — not a half-built one.

#### What P19 found

🔥 **ε is a far bigger dial than anything on the skill ladder, and it is violently non-linear.**
`greedy@0` against `greedy@1` head to head is **+33.3 ± 1.6** points — three times the whole
`simple`-to-`greedy` gulf (11.2), from a decorator that only ever substitutes the *second* card
on the rung's own ranking. The sweep that placed the levels
(`--strategies greedy@0,greedy@0.1,greedy@0.2,greedy@0.35,greedy@0.5,greedy@0.75,greedy@1
--seating balanced --games 4802`) came back **33.7 / 31.7 / 28.6 / 28.5 / 25.5 / 18.3 / 8.6** —
about 8 points across the first half of ε and about 17 across the second. ⚠️ **So levels spaced
evenly in ε would not be spaced evenly in results**, and the shipped values are spaced by the
measurement instead: **ε = 0.9, 0.7, 0.5, 0.0**, which is about seven points a step.

⚠️ **The packet shipped four levels rather than three, and the count is a measurement rather
than a taste.** §3.12 warns that "the temptation is five levels"; the discipline it asks for is
that a level unseparated from its neighbour is deleted. At the reference table the four measure
**~7 points apart against a 95% half-width of about a point**, which is seven times the floor —
so a fourth level is a real setting and a fifth would be arguable. The names are `easy`,
`medium`, `hard`, `expert`; the plan's own illustration said `easy,medium,hard`, and `hard` is
still the third of them.

🔥 **A `TurnContext`'s hand is the engine's own list, and P13.1's finding arrived in the test
project.** The obvious way to test "the mistake is the card the rung ranked second" is to record
the context and ask the rung for its ranking afterwards — which asks it about the **thirteen**
that were kept rather than the fourteen it chose from, and produces a confidently wrong expected
value. `RecordingAgent` records the ranking **during** the turn now. Found by writing the test.

⚠️ **The mistake has exactly one site, and that was a design decision rather than an omission.**
Taking the discard, claiming the turned-up card and declaring are all strict-improvement or
must-answer questions with no plausible second-best, so ε is one dial rather than three — which
is what makes "seven points a step" attributable to anything at all.

⚠️ **`--pairs adjacent` was needed and was not in the plan's build list.** Acceptance 1b asks for
the family to be the k−1 steps rather than the round-robin; the tournament corrected over every
pair it played, so the family and the cells had to become the same choice. It also halves the
cost of calibrating a four-level dial. 🔥 **It found a latent assumption**: `PairingChecks` read
the field to decide which two cells to compare across, and threw `Sequence contains no matching
element` on a run where most pairs never met. It reads the **cells that were played** now, and
that is the same answer for a round-robin.

⚠️ **`sim suite` now fails the run if the dial is not monotone**, alongside the null test. A rung
that raises the ceiling (P20–P22) raises every level and moves the calibration, so this is a
standing check rather than a one-off in the packet that set the values.

---

### P20 — Memory: the card-counting rung ☑

> ✅ **Done 2026-08-20, and the answer is no.** `counting` is `greedy` + `cautious`'s tie-break
> fed a *real* estimate of what is left in the shoe rather than a guess made from its own hand.
> It measures **`+0.3 ± 1.0` points to greedy's side** at 8,008 games a cell — not separated, and
> the point estimate pointing the wrong way; `cautious` is `+0.8 ± 1.0` ahead of it.
> **A null result, published** (`docs/STRATEGY.md` §8), which acceptance 1 asked for in advance.
>
> 🔥 **The finding is *why*, and it is a constraint on P21 and P22.** The memory works — a test
> shows the supply estimate falling below `cautious`'s for every card watched go by and holding
> at the full two for every value not. It cannot pay for two measured reasons.
> **(1) The information set is tiny**: under the cautious default the memory runs **12 → 23 cards
> across a whole round, out of 108** — about ten cards learned beyond its own hand, one a turn.
> **(2) It enters where nothing is paid**: it sharpens `ThreatScore`, which *is* `cautious`'s
> tie-break, and §3 had already measured that tie-break at `−0.2 ± 1.0`.
> ⚠️ **A sharper input to a decision rule already shown not to matter is worth nothing, and the
> two nulls compound rather than add.** The next rung must change **which question is asked**,
> not improve an answer to a question that does not matter — which is exactly P21.
>
> ✅ **Free to run**: 77 rounds/s against `cautious`'s 76 and `greedy`'s 88 (acceptance 5; P12's
> baseline was 51 serial and 85–92 parallel). The memory costs nothing; only the idea does.
>
> ⚠️ **Two things the packet did that were not in its build list.**
> **(1) `ThreatScore` was extracted from `CautiousBotAgent` unchanged**, because the two rungs
> differ in exactly one thing — a `Supply` delegate — and two copies of that arithmetic would be
> two places for it to drift. P15's "one change against the rung below" is a claim about code
> before it is a claim about results.
> **(2) 🔥 The ladder was written out in three places and adding a rung made all three wrong at
> once** — `tournament` and `suite` both defaulted `--strategies` to a hand-typed
> `random,simple,greedy,cautious`, so the new rung was measured only when somebody remembered to
> name it. **The default is now `BotCatalog` itself.** This is P18's defect one layer up, in a
> front end nobody thinks of as one, and it moves part of P23's job forward.
>
> ✅ **The rules question was not decided in code.** RULES.md §9 #15 — *is a discard pile
> inspectable, or only its top card?* — is open, and `QUESTIONS-FOR-MYA-LAY.md` carries it as a
> flat table situation. The bot counts only what it has been shown, which is **wrong in the
> direction that does not cheat**. If the answer turns out to be that the piles may be read, this
> rung gets a much larger information set and deserves re-measuring before it is written off.

**Goal.** The first rung that knows more than its own hand — and **the first packet in the whole
strategy programme that asks the engine for anything.**

**Read first.** RULES.md §5 and §6.3, §9 #9, `TurnContext`'s remark ("the type is the
concealment rule"), P13.5's finding that the browser derives the discard piles publicly.

**Depends on.** P17, P18.

**The finding that motivates it.** 🔥 **`TurnContext` conceals more than the rules do.**
RULES.md §6.3 makes the discards the public information, and P13.5's client already draws **a
discard pile per seat to every watcher** — so a card is public in the fan-out and withheld from
the bots, which see only the one discard they are offered. A counting bot cannot be written
honestly without closing that gap, and the gap is a real inconsistency independent of this
packet.

⚠️ **This is a rules question and must not be decided in code.** Is a discard pile face-up and
inspectable, or is only its top card visible? RULES.md §5 does not say, and §9 #9 ("one shared
pile or per-player piles?", recorded as *largely moot*) **stops being moot the moment a player
counts cards**. Raise it in §9 with provenance and put a neutrally-phrased table situation in
`QUESTIONS-FOR-MYA-LAY.md`.

**The safe default, and why that direction.** A seat may count **only what it has actually been
shown** — its own hand, the cards it took, the discard offered to it each turn whether or not it
took it, and the turned-up cards. If the real rule turns out to be *full piles are inspectable*,
this bot is merely **weaker** than the rules allow; the other default would have it **seeing
what the rules conceal**. Be wrong in the direction that does not cheat.

**Build.**

- Decide and write down the information set **before writing the bot**: what a seat may see is a
  subset of what a watcher at the table can see, and a test says so.
- Whatever `TurnContext` gains, it gains as **public information only** — `ConcealmentTests` and
  the fan-out already own the mechanical assertion (P13.2), and a new test asserts the bot's
  view ⊆ the watcher's view.
- ⚠️ **A new rung is one entry in `BotCatalog` (P18), and that holds for P21 and P22 too.** It
  is a name, a line of description written for somebody choosing an opponent, a `Strength` and a
  factory; the console's prompt, the browser lobby's form, `--difficulty`, the journal header
  and `StrategyCatalog` all pick it up with no front-end work at all. **Nothing outside
  `Domain/Agents` may construct it** — `LayeringTests` fails the build if anything does.
- **`CountingBotAgent`** — greedy's decision with `Unseen` computed against everything seen
  rather than against the hand alone, which sharpens `CoverScore.Potential` and
  `CautiousBotAgent.Threat` at once. ⚠️ **One change against the rung below it**, or a
  difference in results attributes to nothing (P15).
- Within-round state, reset when `context.Round` moves. ⚠️ **The round-boundary trap is the
  hazard here** — `GreedyBotAgent`'s remark records the console agent falling into it, and this
  is the first rung that remembers anything at all.

**Acceptance.**

1. Measured against greedy, **paired**, with an interval, at a stated *N*. ⚠️ **A null result is
   a result and gets published** — `cautious` is already such an entry and its *why* is worth
   more than its number.
   ⚠️ **P17 fixed the bar and the price.** `tournament --strategies greedy,counting` is the
   measurement, and the margin it prints is the paired one. At the default 8,000 games a cell
   the 95% half-width between two thinking rungs is **1.02 points**, so **a rung worth less than
   about a point is not promotable at that size** — and buying half a point of resolution costs
   about **34,000 games a cell**, roughly four hours. The packet must say up front which it is
   asking for. The same applies to P21 and P22.
2. The bot's information set is a strict subset of a watcher's, asserted by test.
   ⚠️ **Amended by P20, because as worded it is false.** A seat sees its own blind draws and a
   watcher does not — exactly one event in the whole narration is private and it is the draw
   (P13.2). The claim that is true, and the one asserted, is that **a counting seat remembers
   exactly the cards it was shown** — its own hand and the one discard offered to it each turn
   — **and nothing else that happened at the table**, though a watcher saw plenty of it.
3. Nothing carries across a round boundary, asserted by playing two rounds and mutating the
   reset to watch the test go red.
4. Deterministic, journals, replays, money-blind.
5. Throughput stated against P12's recorded rounds a second.

**Done when.** `--strategies greedy,counting` runs and `docs/STRATEGY.md` records what counting
is worth — whatever it is.

---

### P21 — Outs: the first rung that looks ahead ☑ done 2026-08-20

🔥 **The answer is yes, and it is the first one.** `outs` beats `greedy` by **`+3.1 ± 1.0`
points** head to head at 8,008 games — `p = 1.9e-09`, and the only comparison in its family, so
it survives Holm outright — and takes the free-for-all 26.3 ± 0.5 against 23.7 ± 0.5. **Two
rungs and two packets had tried to beat `greedy` and neither could be told apart from it; this
one clears the apparatus's resolution three times over.** ⚠️ **It is therefore `Strength: 3`, which re-bases the
difficulty dial** — every level is `BotCatalog.Hardest` with an ε, so all four are now `outs` with
a mistake rate and the ε values 0.9/0.7/0.5/0.0 are no longer the ones that were measured. The
dial is still *ordered* (`DifficultyCalibrationTests` at the reference table, and the suite's
standing monotonicity check), but **re-spacing it is P23's, and it is no longer optional.**
✅ **P23 did it, and one value moved**: `hard` 0.5 → **0.4**, taking the reference table from steps
of 8.2 / 4.3 / 10.3 points to 7.9 / 6.7 / 7.7. ⚠️ **The failure mode this paragraph predicted was
an inversion and what actually happened was a flat spot** — the dial passed every standing check
for two packets with a middle step less than half its neighbours', which is why the check P23
added compares the *published* ε values with the *offered* ones rather than only their order.

**What made it work, stated as the contrast, because two rungs before it did not.** `cautious`
and `counting` both put their new idea *underneath* `CoverScore.Potential` — they decide only
what greedy had already given up on, and greedy's leftovers are a residue worth about half a
point. `outs` puts its key *above* it: where the cover count ties, the count of live outs decides
and greedy's own tie-break is demoted to breaking **its** ties. 🔥 **"Change which question is
asked" (P20) turned out to mean "and ask it earlier than the question you are replacing."**

🔥 **The cost was the packet, exactly as written — and the profile was the surprise.** A naive
reading came in at **12.6× a greedy round**, over the budget. Four things, all of them *around*
the evaluator, brought it to **8.2×**:

- **Refine only what is tied at the top.** `CoverScore.Ranking` takes an optional
  `Refinement`, asked only of candidates that already leave the most melded — **7.1 candidates a
  turn rather than thirteen.**
- **A prune with a proof.** A value with fewer than two cards in the hand that could sit beside
  it cannot enter a meld, whatever the arrangement — **34 values searched of 53.** The packet
  said to verify rather than assume it, and `ThePruneNeverThrowsAwayARealOut` counts the long
  way and demands the same answer.
- **A bar, not a maximum.** `PartialCover.CoversAtLeast` is the same walk asked as a yes/no: it
  stops at the first arrangement that clears the bar and abandons branches that cannot reach it.
  **The search fell from ~98 µs to ~10 µs**, and `PartialCover.Best` was not touched.
- **One index a candidate, not one a probe.** `CoverProbe` builds the melds of the thirteen once
  and generates only the melds that use the drawn value — a run through it is in its suit, a set
  on it is its rank, either may borrow a joker, so a handful of cards go to the generator instead
  of thirteen.

🔥 **And then the measurement that was worth more than the rung.** With the search cheap,
**three quarters of what was left was candidate generation — and it was a fixed cost that did
not depend on the hand at all.** `RunGenerator` allocated all ninety rank windows afresh for each
of four suits on every call, and both generators walked suits and ranks that could not hold a
meld. A precomputed window table, one slot buffer a length, and two feasibility checks —
**generating nothing different, and the console's byte-for-byte capture says so** — made
`PartialCover.Best` and therefore **every rung, every hint and every engine turn about 45%
faster**: `greedy` went from 48.8 to 71.8 rounds a second serial in the same process. §3.7 item 4
predicted "allocation is the thing to attack" and was right; what it got wrong was where —
**the inner loop of the partial cover was `new`.**

⚠️ **Three things worth knowing before touching this rung.** (1) **The cache keys on card
*values* and never on a `CardId`**, which is why it may outlive a deal where `counting`'s memory
may not (P13.4) — and it earns only a **9% hit rate**, so it is kept for being free rather than
for being clever. (2) **Only the discard changed.** Taking, claiming and declaring are greedy's,
so the difference attributes to one decision (P15). (3) **The suite is `k(k−1)/2` and `outs` is
in five of the fifteen cells**; it went from 35 minutes to **105**, and `--pairs adjacent` exists
if it stops finishing in a sitting.

🔥 **And one finding that has nothing to do with strategy: a stronger bot is a longer round.**
Promoting `outs` broke two concealment tests written in P13.2 and P13.4, and **they were wrong
rather than the code.** Both asserted that four seats' hands, taken over a whole round, are
pairwise disjoint by `CardId`. With `outs` standing in for the unanswered seats the fixture's
round ran **67 turns and exhausted the draw pile**, so the discards were shuffled back into it
(RULES.md §5) — public on the way out, private on the way back, and a breach at neither end.
⚠️ **Disjointness was never the property; it was a coincidence of short rounds.** Both now allow
exactly what the table handed round (`Tests/Server/PublicRelease.cs`), and the strict form of the
same rule — `EveryCardASeatIsSentIsOneItMaySee`, which asks what the fan-out *sent* rather than
what a hand happened to hold — **never needed relaxing and stayed green.** ⚠️ **A test over a
played round can be asserting a property of that round's length**, and nothing says so until
something changes how long rounds are. Worth remembering at P22, which changes when a seat draws
blind.

**The packet as it was planned, below.**

---


**Goal.** The rung P15 named and specified, and the optimisation it forces.

**Read first.** P15's closing paragraph ("*a trap for anyone trying to do better*"), §3.4
(candidate generation vs. exact cover — `IsWinning` is the win authority and its answers may not
change), §3.7 item 4 (the work is **allocation**-bound, not compute-bound), P12's bench numbers.

**Depends on.** P17, P18. Independent of P20, though it composes with it.

⚠️ **Amended by P20, and P20 raised this packet's stakes rather than lowering them.** Counting
was the cheap way to test whether *better information* helps, and it does not — because it fed
the tie-break `cautious` already showed to be worth `−0.2 ± 1.0`. **A sharper answer to a
question that does not matter is worth nothing.** P21 is now the first packet in the programme
that changes *which question is asked*, which is what P15's "has to be combinatorial" always
meant, and it is the only remaining candidate for a rung that separates.
🔥 **Two amendments follow.**
**(1) Acceptance 1 said "against greedy **and** counting". Measure against `greedy` only.**
`greedy`, `cautious` and `counting` are mutually inside the interval (§3 of `STRATEGY.md`), so a
`counting` cell buys a second measurement of the same reference at full price — 8,008 games.
Spend it on *N* against `greedy` instead, where acceptance 1's `1.02`-point floor is the binding
constraint.
**(2) 🔥 The suite now costs k(k−1)/2 cells and P20 made k five.** With `--strategies` defaulting
to `BotCatalog`, a rung is measured automatically — and the standing set went from 6 head-to-head
cells to 10, the suite from 34 to 35 minutes. **P21 makes it 15 cells**, and `outs` is the
expensive rung. ⚠️ **Check the suite still finishes in a sitting before promoting the rung**;
if it does not, `--pairs adjacent` already exists (P19) and the ladder is a monotone claim.

**What P15 established, quoted because it is the whole specification.** *Every pairwise-additive
tie-break is greedy again* — partnership is symmetric, so "throw the card with the fewest
partners" and "keep the best-connected twelve" are the same rule. **A rung that improves on
greedy has to be combinatorial**, and the obvious candidate is counting **live outs**: how many
unseen values would raise the cover count of the thirteen kept. The obvious problem is that it
costs a `PartialCover.Best` per value per candidate — **roughly 100× a decision**.

**Build.**

- **`OutsBotAgent`** — for each candidate discard, count the distinct unseen values that would
  raise the cover count of the thirteen kept; throw whichever leaves the most. Ties fall back to
  greedy's key, so it is one change against the rung below.
- ⚠️ **The cost is the packet, not a footnote.** 140 µs × 100 is 14 ms a decision; ~28 turns a
  round makes a round ~0.4 s, and P12's 2,000 rounds in 34 s becomes a quarter of an hour. **The
  optimisation §3.7 has been pointing at since P12 is due here**: attack **allocation**, and put
  every speed-up **around** the evaluator, never inside it.
- Levers, in the order they are cheap: score only the candidates already tied on cover count;
  prune candidate values to those with a partner in hand (**verify that a value with none cannot
  raise the count — do not assume it**); memoise `PartialCover.Best` by the kept multiset within
  a turn.
- **A budget, stated in advance: the outs rung may not cost more than 10× greedy's rounds a
  second.** Over that, it is built wrong — and a rung nobody can afford to run cannot be
  measured, which makes it worthless whatever it plays like.

**Acceptance.**

1. Measured against greedy **and** counting, paired, Holm-corrected, at a stated *N*.
2. Throughput measured and inside the budget. `bench` is extended to time **the rung's
   decision**, not only the primitives underneath it.
3. 🔥 **`HandEvaluator.IsWinning`'s answers are unchanged by every optimisation.** It is the win
   authority (§3.4); any cache is asserted transparent, and the existing evaluator tests are the
   guard that says so.
4. Deterministic, journals, replays, money-blind.

**Done when.** The ladder has a rung separated from greedy — **or** the packet reports that it
does not and says what it cost to find out. Both are publishable.

---

### P22 — Money: is there a strategy in the side bet? ☑ done 2026-08-20

🔥 **The answer is "no, and here is how far away yes lives".** `prospector` is `outs` with one
change — a card taken from anywhere but the deck must be worth more than the ownership a blind
draw would have conferred (RULES.md §4.4) — and at **$5/$1 the rule never fires at all**. It is
not "measured as no difference": it is the *same player under two names*, proved by playing two
tables of one rung each from the same shoes and getting the same rounds card for card. The
head-to-head cell at the standard stakes is therefore a null cell, and at **`+0.01 ± 0.22`** a
round it is the tightest one this harness has produced.

🔥 **And the yes, which is what makes it a sweep and not a cell.** At **$5/$40** — a money card
worth eight rounds — the rung all but stops taking (0.1% against `outs`' 24.9%), wins **20.1 ±
0.9 points fewer rounds**, and banks **`+7.34 ± 3.29` a round**, surviving Holm at `p = 1.3e-05`.
The four cells are monotone in the stakes, with $5/$20 at `+0.95 ± 1.63` (break-even, inside the
interval) and $5/$10 at `−0.86 ± 0.82` (raw only). ⚠️ **This is the first published divergence
between money and win rate in the programme** — a reader ranking the $5/$40 cell by win rate
would rank the better player last — and P12 split the two columns three packets before anybody
needed them apart.

⚠️ **What it cost the standing suite, and P23 inherits the bill.** `prospector` is one entry in
`BotCatalog` (P18), so the suite's ladder field picks it up by construction — **21 head-to-head
cells against 15**, taking `sim suite` from an hour and three quarters to about **three and a
quarter hours**. It has therefore **not been re-run**: `docs/strategy/measurements.csv` is one
rung behind the catalog, §10 of `docs/STRATEGY.md` is generated from `docs/strategy/money.csv`
instead, and **P23's re-run is where the two rejoin**. ⚠️ **Six of those 21 cells are known in
advance to be `outs` against itself**, which is an argument for `--pairs adjacent` and against
adding a rung to a round-robin merely because it exists.

**Goal.** The one strategy axis in this game that is **not rummy**, and the first question in the
programme whose answer is not a win rate.

**Read first.** RULES.md §4.4 (ownership is permanent, first acquisition wins, a blind draw
confers it and a take does not) and §4.5, `GreedyBotAgent`'s remark on why money is absent from
all of its decisions, `StrategySummary.SideBetPerRound`.

**Depends on.** P17, P18.

⚠️ **Amended by P21, and the amendment is about where a new key goes.** Three research rungs
have now been measured and the one that separated is the one whose key sits **above** the key it
was trying to beat, not beneath it. `ProspectorBotAgent` changes the *take* decision, which has
no key underneath it at all, so it is not in danger of being a residue — but it is in danger of
the opposite: a take rule that overrides the cover comparison plays a worse hand on purpose, and
the packet's own acceptance (judged on `$/round`, not win rate) is what decides whether that was
worth it. **Report both, and say which moved.** ⚠️ **`outs` is `Strength: 3` now**, so "greedy,
except…" is no longer "the best rung, except…": say explicitly which rung this one is one change
away from, and measure it against **that** rung.

**The question nobody has asked.** Every rung is money-blind **on purpose**, and
`MoneyCardsDoNotChangeWhatABotThrowsAway` makes it a test. But money-blindness of the *discard*
does not settle the *draw*: **a blind draw confers ownership and a take does not** (§4.4), so a
seat that draws blind more often acquires more money cards — while playing a worse hand. Greedy
concedes exactly one tie-break to this and nothing else. **What that trade is worth has never
been measured**, and the harness has reported `side $/r` separately from the flat payment since
P12 without anybody asking it this.

**Build.**

- **`ProspectorBotAgent`** — greedy, except that it takes the discard only when the improvement
  outweighs the expected ownership value of drawing blind instead: a function of the stakes, the
  money values in play, and how much of the shoe is left.
- ⚠️ **The discard stays money-blind.** Only the take/draw decision changes, which keeps the
  money-blindness test green and keeps the difference attributable to one decision (P15).
- ⚠️ **Judged on `$/round`, not on win rate.** A rung that wins fewer rounds and banks more money
  is the better player, and **this is the first packet where the two can come apart** — the
  reporting has split flat from side bet since P12 precisely so that they could.
- **Stakes are a variable, not a constant.** The answer plausibly depends on `MoneyCardValue`
  against `RoundValue`, so the packet runs a sweep rather than one cell.

**Acceptance.**

1. Measured on net per round, paired, with an interval, across **at least three stakes ratios**.
2. Win rate reported beside it, and any divergence between the two stated **explicitly** rather
   than left for a reader to notice.
3. Discard behaviour provably unchanged: the money-blindness test still holds.
4. Deterministic, journals, replays.

**Done when.** `docs/STRATEGY.md` answers *"should you draw blind for the money?"* with a number
and the stakes it depends on. ✅ **§10 does, at four ratios.**

**What it actually found, beyond the headline.**

- 🔥 **A rung's strength stopped being a property of the rung.** Every rung before this plays the
  same game whatever the table is played for; `prospector`'s one decision reads
  `Stakes.MoneyCardValue` against `Stakes.RoundValue`, so *"how good is it"* has no answer until
  somebody says what the stakes are. That is why `BotCatalog.Strength` is an ordinal and not a
  score, and it is why this rung shares `outs`' number rather than being ranked above or below it.
- ❌ **WITHDRAWN 2026-08-21 by `RULES.md` rev 20 — the premise moved, not the reasoning.** A shown
  7♦, A♠ or joker **can never be owned** and its **partner copy pays ×3**, not the double this note
  was derived from — so a designation on a permanent money card leaves **exactly as much** live
  money as any other designation, and the note below is false in its conclusion.
  ⚠️ **`MoneyCardRegistry.Multiplier` caps at 2 and must return 3** (`RULES.md` §10 #17);
  `MoneyOdds` prices a blind draw from it; and **§10 of `docs/STRATEGY.md` — the money sweep — was
  measured under the struck rule.** ✅ **`WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe` asserts
  the withdrawn direction and will go red**, which is the argument for asserting a derivation
  rather than writing it down: **its last assertion should invert to an equality**, and that is a
  prediction nobody has run. **The note is kept unaltered below as the record of what was believed
  and why.**
- 🔥 **A `DERIVED` rules note fell out of the arithmetic and is now RULES.md §4.1 rev 15**: a
  designation that lands on a **permanent** money card leaves the deck with *less* money in it,
  not more. Turning up a 7♦ makes that value a double but takes the physical card out of the deck
  (§3 step 4), so one 7♦ worth $2 is left where an ordinary designation leaves a partner worth $1
  **and** the 7♦s untouched. **Doubling one value is not the same as designating a second.**
  Found by writing a test that asserted the opposite and watching it fail.
- ⚠️ **The exchange rate is the rung's one free parameter and it is a constant on purpose.** A
  rung with a knob is a *family* of rungs and a family cannot be measured against the one below
  it in a single cell (P15). It is documented as a model, and its bias is stated: it overvalues
  early melded cards, so it takes the discard more often than a sharper model would, which moves
  the crossover **down** rather than up.
- ⚠️ **The identity at $5/$1 cannot be shown from a head-to-head cell.** Two labels of one player
  sit in different seats there, so their aggregates differ by seat luck however identical they
  are — the small-`n` version of that test failed exactly this way. What settles it is two
  *homogeneous* tables dealt from the same shoes.

---

### P23 — The standing answer ☑

**Goal.** The programme's output as a **maintained document**, not a session report — and a
difficulty ladder re-calibrated against the ladder the programme actually ended up with.

**Read first.** Everything P17–P22 wrote into `docs/STRATEGY.md`, §3.12, `docs/PLAYING.md`.

**Depends on.** P17 and P19. P20–P22 to whatever extent they were built.

**Build.**

- **`docs/STRATEGY.md`** as the standing answer: the ranking matrix, the difficulty calibration,
  and a short entry per rung — **including the ones that failed.** `cautious` is already such an
  entry, and P15's account of *why* denial and self-interest point the same way is worth more
  than its number ever was.
  ⚠️ **P17 created the document and `sim suite` behind it, and P19 added §10 and the
  `difficulty.*` rows, so this packet extends rather than writes.** What it still owes: an entry
  per rung that P20–P22 added, and acceptance 2 — the test that the levels published are the
  levels offered. ⚠️ **P19 got half of acceptance 2 for nothing**: `sim suite` exits non-zero if
  the dial stops being monotone, so a *stale* calibration now fails the run rather than the
  proofreading. What is still missing is the join between the CSV's `difficulty.*` rows and the
  menu the front ends draw. ⚠️ ~~**The suite's standing set is a list in `Suite.Run` and it is the thing that goes
  stale**: a rung added to the catalog does not appear in the document until it is added there
  too, and P23 is where that stops being a habit.~~ ✅ **Half-discharged by P20**, which hit
  exactly this: `--strategies` now defaults to `BotCatalog` for both `tournament` and `suite`, so
  **the ladder field follows the catalog by construction**. ⚠️ **What is left for P23** is that a
  new *kind of measurement* is still added to `Suite.Run` by hand, and that the default is a
  default rather than a test — nothing fails the build if a future front end types the list out
  again. **Make it a test**, in the shape `LayeringTests` already uses.
- ⚠️ **Every figure carries the command that produced it and the games it came from**, and
  `sim suite` regenerates the data behind it. The same discipline `RULES.md` applies to
  provenance: a number without its origin is not evidence (§3.12).
- **Re-run the suite over the final ladder and re-calibrate P19's levels against it.** Adding
  rungs moves them; a calibration published before the last rung landed is stale by
  construction.
  🔥 **P21 turned this from a tidy-up into the packet's reason for existing.** `outs` separated
  from `greedy` and so carries `Strength: 3`, which makes it `BotCatalog.Hardest` — and every
  difficulty level is `Hardest` with an ε. **All four levels are now a rung nobody calibrated
  them against.** The dial is still ordered (the suite's standing check and
  `DifficultyCalibrationTests` both hold), so nothing is broken; what is stale is the *spacing*,
  which P19 measured to be even in results rather than in ε and which was measured on a rung that
  is no longer underneath the dial. ⚠️ **Re-run P19's sweep before re-spacing**: ε was violently
  non-linear on `greedy` (0 → 0.5 worth ~8 points, 0.5 → 1 worth ~17) and there is no reason
  to assume the curve has the same shape on a rung that looks ahead.
  ⚠️ **And the dial got expensive.** Every level now pays `outs`' price — `FallibleAgent` asks
  its inner rung to *rank*, so even `easy` runs the outs search —
  and `DifficultyCalibrationTests` went from seconds to **3m 51s**, which is most of the test
  suite's wall clock. If P22 adds a stronger rung still, this compounds.
  ✅ **P22 did not.** `prospector` shares `outs`' `Strength`, so `BotCatalog.Hardest` is still
  `outs`, the dial is still built on the rung P19 measured, and **nothing about the difficulty
  calibration moved.** What P22 changed is only what P23 has to *pay*, below.
  🔥 **And that bill is now the packet's biggest single risk.** Seven rungs is **21 head-to-head
  cells against 15**, and `sim suite` went from an hour and three quarters to roughly **three and
  a quarter hours** — so `docs/strategy/measurements.csv` was left at the six-rung field and P23
  is where it is caught up. ⚠️ **Six of the new cells are `outs` against itself in all but name**
  (P22: `prospector` and `outs` play the same rounds card for card at `Stakes.Standard`), which
  is roughly three quarters of an hour spent reproducing a fact a unit test already asserts.
  **Consider `--pairs adjacent` for the ladder as well as for the dial**, or a suite that ranks
  the field once and measures a stakes-sensitive rung only where it is not a duplicate — but
  ⚠️ **do not fix it by hand-typing a shorter field**, which is the exact defect P18 and P20
  each had to remove one layer at a time.
- **The money sweep is in the standing set already** (P22): `Suite.Run` plays it and writes
  `money.net-per-round.*`, `money.win-rate.*` and `money.take-rate.*` rows, so the regeneration
  picks it up with no work. What P23 owes it is the **join** — §10 of `docs/STRATEGY.md` quotes
  `docs/strategy/money.csv` today because the suite has not been re-run, and after the re-run it
  should quote `measurements.csv` like every other section.
- The final exposure: level names, descriptions and honest one-line explanations in both front
  ends, **read from the catalog** rather than typed into a UI.
- `docs/PLAYING.md` gains how to choose an opponent.

**Acceptance.**

1. `docs/STRATEGY.md` exists; every figure names its command and its *N*; `sim suite` reproduces
   the underlying CSV byte-identically.
2. 🔥 **The levels published are the levels offered** — asserted by a test, not by proofreading.
   A difficulty document that drifts from the difficulty menu is worse than none.
3. Failed rungs are documented **as failures**, with the reasoning that makes them worth having
   tried.

**Done when.** Somebody asking *"which bot should I play, and what actually works in this
game?"* has one document to read, and it regenerates from one command.

**What P23 found (2026-08-20). ✅ Done — and it is the last packet.**

🔥 **1. The dial was re-fitted against `outs` and exactly one value moved.** P19 placed the four
levels against `greedy`; P21 re-based them onto `outs` without re-spacing, which left the
reference table at steps of **8.2 / 4.3 / 10.3** points — ordered, monotone, passing every check,
and visibly not a dial. Re-sweeping ε on `outs` over P19's own seven probes and inverting the
curve moved **`hard` from 0.5 to 0.4 and left `easy`, `medium` and `expert` alone**: the reference
table is now **7.9 / 6.7 / 7.7**. ⚠️ **The finding is that only one value moved.** ε = 0 → 0.5
costs 9.5 points of win rate on `outs` against about 8 on `greedy`, and 0.5 → 1 costs 16.5
against about 17 — **the curve has nearly the same shape on a rung that looks ahead as on one that
does not**, so ε is close to being a property of *the mistake* rather than of the rung it is made
against. **Re-check it after the next rung; do not expect to re-derive it.** And it is not close
enough to skip: leaving P19's values in place is what produced the flat spot above.

🔥 **2. Two instruments, one dial, and they disagree about spacing.** The reference table's steps
are 7.7 / 6.7 / 7.9 with the *middle* narrowest; the head-to-head steps are 6.8 / 9.8 / 11.2 with
the *top* narrowest. They are different measurements — a head-to-head cell holds two levels and a
reference table four, and two strong players deal a longer round than two weak ones. The re-fit
improved both (reference spread 6.0 → 1.1, head-to-head 5.1 → 4.4), so no choice had to be made;
⚠️ **`STRATEGY.md` §9 records that if a future rung makes them disagree in *ordering*, the
reference table is what the shipped values are fitted to**, because a mixed table is exactly it.

🔥 **3. A rung now declares which instrument settles it, and that is what shortened the suite
without hand-typing a field.** `BotRung.Ranked` is `RankedOn.WinRate` or `RankedOn.Money`;
`BotCatalog.Ladder` and `BotCatalog.StakesSensitive` are **filters of `All`**, so the ladder
tournament measures one set and the money sweep the other and a rung still cannot be added
without being measured. ⚠️ **The wall clock was the smaller half of the argument.** Six of the 21
cells were `prospector` against the ladder, reproducing an identity a unit test already asserts —
and **six null cells in a Holm family make every real verdict in it harder to reach**. A duplicate
is not a free row. ✅ **Measured, not argued**: the run that dropped them reproduced **every ladder
figure to the digit**.

🔥 **4. The reproduction is the headline. 59 of 77 rows came back byte-identical.** Every ladder
cell, the null test, all twelve pairing ratios and both headline rows, from a tree that had since
gained a rung and changed what the standing field is. **The seven that moved are the dial and only
the dial**; the twelve that are new are the money sweep, which §10 now quotes from
`measurements.csv` like every other section — the join P22 left open.

⚠️ **5. Acceptance 2 is a test now, in two directions.** `StandingAnswerTests` fails the build if
the ε values `measurements.csv` publishes are not the ε values `DifficultyLadder` offers; if a
published step did not survive Holm; if a rung in `BotCatalog` is the subject of no published row;
if the ladder and the sweep are not between them the whole catalog; or if a front end writes out a
level's or a rung's description instead of asking for it. 🔥 **A default is not a guarantee** —
P20 made the field *default* to the catalog and nothing failed when nobody re-ran the suite, which
is precisely the state this packet found the tree in.

⚠️ **6. The suite is a two-and-three-quarter-hour job and there is no shorter honest version.**
(⚠️ **Five hours when this was written; re-measured at 9,981 s by P29.**) 17,539 s for 77
measurements. `--pairs adjacent` on the ladder would take fifteen cells to five and **throw away
the matrix in §3, which is the document's centre** — it stays available and stays unused. The
structural saving that *was* available has been taken; there is not a second one.

✅ **7. Re-spacing the dial is not a play change, proved byte for byte.** `expert` is ε = 0 and
`FallibleAgent` defers entirely at ε = 0, so a console capture at `--pick 0` from `HEAD` and from
this tree is **identical from the `Seating:` line on — 7,025 bytes for `--script bots` and 88,805
for `--script human`**. The prompt above that line differs by one word, because `hard`'s
description says *two times in five* now.

---

### P24 — The computer's reasoning, said out loud ☑ — **split 2026-08-21, both halves done**

> 🔥 **Split into P24.1 and P24.2 on 2026-08-21, because P30 needs the first half and not the
> second.** The packet always contained two unlike things: **a journal for the hosted table**,
> which is plumbing the browser has never had (`Web` and `Server` contain the string `journal`
> **zero times**), and **the computer's reasoning drawn beside its move**, which is a product
> feature. **A conformance audit of the browser (P30) wants the journal and has no use for the
> explanation**, so the journal goes first and alone.
>
> - **P24.1 — a journal for the hosted table.** `--journal` on `BurmesePoker.Web`, writing the
>   same `GameJournal` format the console and the harness already write and `sim replay` already
>   reads. ⚠️ **Nothing about the *why*.** ✅ **A hosted table records what every seat was asked
>   and what it answered**, which is what makes a browser round auditable at all — and what P30's
>   browser half is written against.
>   ✅ **Built 2026-08-21 (Fable 5).** The split it settled: `TableSession` owns the
>   `GameJournalBuilder` (the agents are built there, wrapped **outermost** so the record is the
>   answer that reached the engine — a stand-in's or the clock's included) and hands the record
>   back through `TableSession.Journal()`; **the file stays the host's**, exactly the console's
>   split — `HostedTable` flushes it **after every settled round, on the dealer's own thread**,
>   because nothing ends a hosted match but the table closing, and a mid-round build from any
>   other thread would tear the decision list. `TableSeat` gained `Strategy` (a journal's
>   attribution: a level's name for the Web's bots, `human` for a remote seat), an abandoned
>   round marks `Header.Abandoned` rather than poisoning the file, and the lobby's form does
>   not offer the flag on purpose — two tables writing one path would take turns overwriting
>   each other. ⚠️ **The journal still stamps rules rev 13** — that is R2, P30.2's fix, and one
>   constant covers console, Sim and Web alike.
> - **P24.2 — the reasoning, said out loud.** Everything below: the hint arrow grows a *why*, and
>   the journal grows an **opinion beside an answer** so that *where an expert disagreed with the
>   computer* is a query rather than something somebody has to notice. **Scope unchanged** —
>   browser only, all four questions, winner versus runner-up. ✅ **The *whether* is settled: Nick
>   asked for it on 2026-08-22 and it is next after P32** — see the re-plan block below.
>
> ⚠️ **The warning below about `FallibleAgent` belongs to P24.2 and is the most important line in
> this packet.**

**The body below is P24.2's, and P24.1 is the first two paragraphs of its *Build* section.**

---

#### ✅ Built 2026-08-22 (Opus 5) — what it turned out to be, and what it leaves for the packets after it

**The re-plan below was right on both of its bets and the record is kept as written.** What is here
is the difference between the plan and the thing.

🔥 **1. `CoverScore.Ranking` became a projection, and that is the whole of acceptance 2.**
`CoverScore.Scored` returns the candidates with the three keys the sort computes and discards a
line later; `Ranking` is now `Scored(...).Select(c => c.Card)`. **There is one ordering, not two
that agree by inspection** — the same discipline `Discard` already kept against `Ranking`, and the
reason an explanation costs a front end nothing over the arrow it already draws.
⚠️ **`ScoredCandidate.Refined` is `long?` and null means the key was never asked.** The expensive
refinement is only put to candidates already tied at the top (P21), so a candidate that lost on
cover count was never scored on it. **Null and zero sort identically there, so no published
measurement moved** — what the null buys is that nobody can read back a key nobody took.

🔥 **2. What crosses the layer boundary is a *described* key, and the sentinel is the point.**
`DiscardKey` carries a name, a direction and `BeyondMeasure` — the phrase to print when the value
is not a count. There is exactly one sentinel today: `CoverScore.Potential` returns
`int.MaxValue` for a joker, which is a **refusal** rather than a partnership. Two assertions guard
it, both in `BotCatalogTests`: every key a rung declares has a name and a direction, and
**`BotCatalog.Hardest` must implement `IExplainsDiscards`** or the browser loses the feature
silently. ⚠️ The second names `Hardest` and not `Ladder[^1]`, which is now a **fourth** place
somebody could re-commit P31's coincidence-asserted-as-law.

🔥 **3. `ComputerAdvice` holds one decision, keyed on the identity of the `TurnContext`.** That is
the right key precisely because the engine builds a fresh context per decision (P7), so the memo
remembers one decision and forgets it when the next arrives. The arrow, the sentence and the
journal's second opinion are **one ranking between them**, and
`ComputerAdvice.RankingsBought` is the observable that makes it an assertion rather than a hope.
⚠️ **It must never become a cache across turns** — a context's hand is the seat's own live list,
and an answer kept past the discard describes cards that have gone. ⚠️ It is safe for a table's
seats to share **because a table plays one turn at a time** (§3.6).

🔥 **4. The journal's advice needed a seam in the domain, because the domain cannot see
Presentation.** `ISecondOpinion` is that seam and `ComputerAdvice` implements it;
`JournalingAgent` asks it **before** the seat answers, exactly as it takes the snapshot.
`JournalDecision.DisagreedWithTheComputer` is the query — by `CardId`, human seats only, and
**recorded with the hints box off**, because a record of where somebody disagreed must not depend
on whether they wanted to be told.

⚠️ **5. What this leaves for P37, and it is the first thing that packet meets.**
`ConcealmentTests.NoTableEventCanCarryTheComputersReasoning` now asserts over the **type** that no
`TableEvent` carries an `AdviceRationale`, and `SeatPrompt` is where one rides. P37's *"shall we
change seats?"* is the first **public** question this project has ever asked and the first asked
*between* rounds — so it can ride on neither. **Decide where before writing it.**
⚠️ **And a sixth `SeatQuestion` now fails quietly in two more places**: `RemotePlayerAgent.Why`'s
null arm and `TurnPrompt.razor`'s inert final arm. Both are the `JournalFormat.Name` lesson applied
— *a default arm is a mistranslation waiting for the next case* — but quiet is still quiet.

⚠️ **6. The claim's *why* was scoped in, and the null is why it is interesting.**
`AdviceRationale.ForObjection` says out loud that refusing has been measured and is worth nothing
either way (§12). It **carries no number on purpose**, so it cannot rot into a wrong figure — but
if that measurement ever separates, the sentence is wrong. **P34 owns knowing it is there.**

⚠️ **7. Two test-fixture findings.** `ScriptedSeat`'s old no-hint fallback — *throw the first loose
card* — **does not terminate**: it leaves the hand it started from rearranged, and a table of such
seats runs until the round is abandoned on the clock. It throws the card just taken back now, which
stands still while the bots race. And a table with two person-seats needs **both** scripted, or the
unattended one spends its whole patience per question and the round dies on the clock rather than
on the assertion.

---

#### ✅ Re-planned 2026-08-22 (Opus 5) — **Nick said build it**, and eight packets have shipped underneath it

🔥 **The *whether* is answered: yes.** The scope was set by Nick on 2026-08-20 and *whether to
build it at all* was left his call and stayed open for two days. **He asked for it on 2026-08-22**,
from the browser, playing a five-handed table — *"I see the suggested card for me to discard but I
don't see where the explanation for that is."* ⚠️ **That is the packet's own acceptance criterion
arriving as a bug report**, and it is worth recording as the reason rather than as an anecdote: the
arrow reads as a promise of a sentence that is not there. **P24.2 is next after P32.**

⚠️ **What a person sees today, stated precisely — because getting this wrong once already sent
this re-plan out with a false premise in it.** 🔥 **The browser has a *Why?* affordance already,
and it is not this packet's.** `TurnPrompt.razor` carries **five `<details class="why">`
disclosures**, one per question, and they hold **static rule text**: the discard's reads *"Every
turn is take one, throw one (RULES.md §5). The number on a card is what throwing it costs you."*
**Every player sees the same sentence every turn.** ✅ **They explain the game.** ⚠️ **They are
deliberately *not* gated on the hints checkbox**, and correctly so — a rule is not advice, which
is the same distinction `HandPanel.Words` draws when it tells the ear about §5.1's ban outside the
gate.

⚠️ **What is missing is the other kind of sentence entirely: a *decision* explanation, computed
per turn.** The hint itself is still a bare `Card?` — `ComputerAdvice` returns one, `HandView`
turns it into `CardDisplayState.SuggestedThrow`, `DisplayTokens` draws `←` — and **nothing
anywhere says why that card**. The nearest things are the **cost badge** (`−1`, `−2`: melded cards
given up) and the card button's accessible name, *"throwing it gives up 2 melded cards, the
computer would throw this one"* — which reports the arrow rather than explaining it. **The keys
that actually decided it are computed, compared and thrown away inside `CoverScore.Ranking`.**

🔥 **So the browser half of this packet is cheaper than the body below assumes, and its one real
design constraint changes shape.** The body plans *"a `<details>` under your seat, gated on the
existing hints checkbox"*; **the `<details>` exists, is in the right place, and already passes
`MarkupStandardsTests.NoParagraphOnTheTableIsAWallOfText`** (which exempts exactly a `<details>`
and a `<span class="said">` from its 80-character prose budget). ⚠️ **What the packet adds is a
*computed paragraph inside an existing disclosure* — and that paragraph, unlike the rule text
around it, must be gated on hints**, because it is advice. **A Why block that is half ungated rule
and half gated advice is the fiddly part of this packet**, and it is fiddly in markup rather than
in the domain.

**Five things have changed under this packet since the body below was written (2026-08-20), and
three of them make it cheaper.**

1. 🔥 **Half of build item 1 is already built, by a packet that wanted it for something else.**
   P31 added **`IRanksDiscards.RankDiscards(context, candidates)`** — *an instrument the engine may
   never call* — to measure the feeding ban's counterfactual, and it hands back an **ordered**
   candidate list over an arbitrary candidate set. ⚠️ **What is missing is not the ranking; it is
   the keys.** `IExplainsDiscards` should be written as **the described sibling of a shipped
   interface** rather than as a new capability, and the packet should say so in one line rather
   than re-deriving it. **Read `Domain/Agents/IRanksDiscards.cs` before writing a line of it.**
2. ✅ **The sequencing objection is fully spent, and this is the last note that needs to say so.**
   The paragraph at the end of this packet reversed itself once already; it is now moot. §5.1 is
   built (P27), the win condition is a function of the table size (P25), the money layer is as it
   really is (P26), the claim's permission exists (P28) and the clean bonus is paid (P33) — and
   P29, P33 and P32 have each re-measured underneath them. **The decisions this packet renders are
   no longer about to change.** *An explanation of decisions that are about to change is a sentence
   written twice and believed once* — that was the objection, and the second writing has happened.
3. 🔥 **The feeding ban is enforced as an impossible move, so the fourth sentence is buildable
   today and one half of it already exists.** `TurnContext.LegalDiscards` is the whole of the
   choice, `CardView.CanBeThrown` already crosses into Presentation, and the browser already says
   *"you may not throw it: the player after you took that rank in the open"* **to the ear and not
   behind the hints gate** — because it is a rule and not advice (`HandPanel.Words`). ⚠️ **So the
   explanation must not repeat it as though it were a reason the computer chose**, and the packet's
   own warning about the ban's **floor** (§5.1 yields where nothing else is legal, so every card is
   throwable again on that turn) is now a real state with a named test rather than a hypothetical.
4. ⚠️ **The default table is five seats now (P32), and that changes what a true sentence says.**
   By `RULES.md` §7.1.1 a five-handed declaration owes **no series at all**, where four-handed owes
   a joker-free run — so an explanation phrased in terms of runs is **wrong at the table the
   browser now deals**. 🔥 **The rationale has to read the same `TableRules` the evaluator does**,
   and *"why that card"* is a different sentence at four seats and at five. **Nothing in the body
   below anticipated this**; it is the newest thing in the packet.
5. ⚠️ **§7.3's clean bonus adds a sentence the computer is not entitled to say.** A jokerless
   declaration pays ×2 or ×3 (P33), and **no rung plays for it**: `CoverScore.Potential` returns
   `int.MaxValue` for a joker, so the adviser will never part with one and every clean win it
   collects is an accident. 🔥 **So the honest sentence about a joker is *"it will never throw
   this"*, not *"it is holding it for the bonus"*** — and an explanation that implies the second is
   the same class of failure as justifying `FallibleAgent`'s slip: **confidently right-looking and
   false.** Say it in the packet, because the arithmetic (§14: the bonus is collected about one
   round in six and is worth +$5 a head at four seats) makes the wrong sentence *tempting*.

**Two costs re-measured, so the budget is current rather than quoted.**

- ⚠️ **`outs` is 7.7× a greedy round at five seats and 6.3× at four** (`sim bench --seats 5`, P32),
  not the 8.2× the body quotes from P21. **The one-call rule below is unchanged and is the whole
  budget**: the discard path calls the explaining ranking **once** and takes its head.
- ⚠️ **`BotCatalog.Hardest` is not `BotCatalog.Ladder[^1]`.** The catalog became a *tree* at P31 —
  `warden` and `prospector` both hang off `outs` — and P31 found three places that had written the
  old coincidence down as a law. **The assertion in *Two mechanical guarantees* below must name
  `Hardest`**, which is what it already says; this note exists so that nobody "simplifies" it.

⚠️ **One thing to decide in the packet and not before it**: whether the §4.5 claim's *why* is in
scope. The body flags it as the question a rationale helps most on — refusing costs the seat a rank
**and discloses that it holds one** — and P29 then measured that refusing is worth **nothing**
(`+0.4 ± 1.0` on win rate, `+0.06 ± 0.29` on money). 🔥 **A null makes the explanation more
interesting rather than less**: the honest sentence is *"it makes no measurable difference either
way"*, which is a thing this project knows and no player does.

---


**Goal.** The arrow on the card grows a sentence: **why that card, and what it beat.** A debug
instrument first and a teaching aid second — built so that a session played beside an expert
leaves a **record of where the expert disagreed**, computable rather than transcribed.

**Read first.** §3.5 (*the engine asks for a move and must never be able to ask for a ranking*),
§3.9 (a seed is a pointer; a journal is the artifact), §3.11 items **A1**, **A4**, **B9** and
**C12**, §3.12. In the tree: `Domain/Agents/CoverScore.cs`, `Domain/Agents/IRanksDiscards.cs`,
`Domain/Agents/OutsBotAgent.cs`, `Domain/Agents/FallibleAgent.cs`,
`Domain/Agents/JournalingAgent.cs`, `Presentation/ComputerAdvice.cs`, `Server/SeatPrompt.cs`,
`Server/TableSession.cs`, `Web/Components/Table/YourSeat.razor`.

**Depends on.** P13.6 (the browser seat), P14 (journals), P18 (one catalog), P21 (`outs` is the
rung that advises).

**Scope, decided by Nick 2026-08-20.** **Browser only** — the console keeps its arrow and gains
nothing, so `drive-console.py` captures stay comparable and no Spectre work is in this packet.
**All four questions**, not the discard alone. ⚠️ **There are five now** (P28's claim
permission, `RULES.md` §4.5), and it is the one a *why* would help most on — refusing costs the
seat a rank and discloses that it holds one — so scope it in or say why not. The explanation is **winner versus runner-up**,
not a ranking table. And it is **written down** as well as drawn.

⚠️ **What was considered and not taken, so nobody re-opens it silently.** A **full ranking table**
— every candidate with all three keys — was the obvious instrument and is the wrong product: it
renders the decision procedure instead of the decision, and what an expert argues with is a
*claim*, not a spreadsheet. **Both** (a sentence with the table behind a second disclosure) was
rejected as the same table wearing a summary. A **separate `--explain <path>` sidecar** was
rejected in favour of the journal: a second file with its own format is a second thing to join,
and P14 already owns *what happened at this table*. And the **console** was left alone
deliberately — it is where a debug tool is cheapest, but it is not where you sit with somebody,
and touching it would spend P21's and P23's byte-identical captures for no reader.

**Build.**

- **A rung describes its own keys.** `CoverScore.Ranking` already computes everything an
  explanation needs and throws all of it away except the order: cover count, the optional
  `Refinement`, and the tie-break. A variant returns the *scored* candidates, and a new public
  interface — `IExplainsDiscards`, a sibling of `IRanksDiscards` and separate from
  `IPlayerAgent` for the same reason — lets a rung hand that back **with its keys labelled**.
  Implemented on `OutsBotAgent`; `GreedyBotAgent` gets it for two keys instead of three.
  🔥 **The keys are packed for sorting, not for reading, and this is the trap the packet exists
  to avoid.** `outs` stores its key as `-LiveOuts.Count(…)` because the sort takes lowest first;
  `CoverScore.Potential` returns `int.MaxValue` for a joker; `CautiousBotAgent` packs two keys
  into one `long`. A front end that read the raw numbers would draw *"−14 outs"* and
  *"2147483647 partners"*. **A rung supplies each key's name and direction; presentation never
  interprets a bare `long`.**
- **The other three questions.** Take-or-draw, the §4.5 claim and the declaration have no ranking
  behind them — only a `CoverScore.Improves` call — so their rationale is a **gain**: *"taking
  the 7♦ raises your melded count from 8 to 11."* One `Gain` call each. ⚠️ **Say plainly that the
  declaration's explanation is near-vacuous** (`Declare` is `=> true`; the engine only asks when
  the hand already wins) rather than dressing it up into something that sounds like judgement.
- **`ComputerAdvice` explains, at today's price.** One new method per question returning an
  `AdviceRationale`. ⚠️ **The discard path calls the explaining ranking once and takes its head.**
  `outs` is 8.2× a greedy round (P21); a second call per turn would double what every human turn
  at every table costs, for a sentence.
- **The sentence is assembled from `[0]` and `[1]`** — the first key on which the winner and the
  runner-up differ, said out loud. **The target, written down so the packet has something to be
  measured against:**

  > 3♠ and 9♦ both leave 8 of your cards melded. 3♠ leaves the hand 14 cards of the pack could
  > improve; 9♦ leaves 11. That is why.

  ⚠️ **Two cases are the interesting ones and neither may be hidden**: *nothing separated them*
  (every key tied and the hand's own order decided — **the bot is indifferent and the expert will
  not be**, which is one of the more valuable turns this instrument can catch), and *there is no
  runner-up* (`Ranking` dedupes by value, so a hand holding a pair yields a shorter list than it
  holds cards).
- ⚠️ **The explaining interface is public; everything it reports on is internal.** `CoverScore`,
  `LiveOuts` and `ThreatScore` are `internal`, and the domain's only `InternalsVisibleTo` is
  `BurmesePoker.Tests` (P21) — which Presentation is not. So the keys cross the boundary as a
  **described** result on a public interface, and not by widening `InternalsVisibleTo`, which
  would hand a front end the machinery instead of the answer.
- ⚠️ **`ComputerAdvice` resolves the adviser from the catalog and must keep doing so.**
  `LayeringTests` fails the build if any project outside `Domain/Agents` constructs a rung (P18);
  asking `BotCatalog.Hardest` for one is how Presentation is allowed to have an adviser at all.
- 🔥 **`FallibleAgent` must never be in the advice path, and this is a new trap P19 created.** A
  difficulty *level* is `Hardest` wrapped in a mistake rate, and `FallibleAgent`'s mistake is
  **the runner-up of the very ranking this packet renders**. Explain through a level and the page
  would confidently justify a move the computer chose *because it was second best*. The adviser is
  the bare rung at ε = 0 — as `ComputerAdvice` already says in prose, and as this packet is the
  first thing that could get wrong.
- **The browser: a `<details>` under your seat**, gated on the existing hints checkbox.
  ⚠️ **Amended 2026-08-22 — the `<details>` already exists.** `TurnPrompt.razor` has five of them
  holding static rule text, ungated because a rule is not advice. **This packet adds a computed
  paragraph inside them, and that paragraph is gated while the rule text around it is not.**
  ⚠️ **This is fixed by a test rather than by taste.** `MarkupStandardsTests.NoParagraphOnTheTableIsAWallOfText`
  allows **80 characters** of visible prose on the felt and exempts exactly two things — a
  `<details>` and a `<span class="said">` (§3.11 B9). A three-sentence explanation is well over
  it. `<summary>` at 40px (§3.11 A11); no new `CardDisplayState`, so `DisplayTokens` and the
  contrast tests are untouched.
- ⚠️ **The rationale rides on `SeatPrompt` and never on a `TableEvent`.** `SeatPrompt` is already
  seat-private; `ConcealmentTests` sweeps every event against what each viewer may see, and one
  seat's reasoning on the bus is precisely the leak §3.11 A1 exists to catch.
  `IRanksDiscards`' own remarks say this in advance.
- ✅ **Journalling the hosted table — built by P24.1 on 2026-08-21; this bullet is discharged.**
  `TableOptions.Journal` opts the session in, `JournalingAgent.Wrap` runs outermost over the
  agent dictionary, `TableSession.Journal()` hands the record back, and `HostedTable` writes
  `TablePlan.Journal`'s path after every settled round. **What P24.2 adds here is only the
  advice beside the answer** — the plumbing below this line already exists.
- 🔥 **What is recorded is not a rationale — it is an *opinion beside an answer*.**
  `JournalingAgent` records the answer *the seat gave*, and at a table with an expert in it that
  answer is **hers**. The computer's recommendation is a different agent's opinion about the same
  moment, so the record is `JournalDecision` + **advice** — the card the adviser would have
  thrown, the rationale that names why, and the rung that said it. **Then disagreement is a query
  (`Answer != Advice.Card`) rather than something a person notices and writes down.** That is the
  artifact this packet is for.
  ⚠️ **It contradicts `JournalingAgent`'s stated stance — *"it records answers, not intentions"* —
  and the contradiction is deliberate and narrower than it looks.** The intention recorded is a
  **different agent's**, taken on the same context, which is a fact about the game rather than a
  guess about the player. Amend the remarks; do not leave the file saying the opposite of what it
  does.
  ⚠️ **Advice is attached only for seats a person is playing.** A bot seat's advice is its own
  answer, and recording it would run `outs` twice a turn to learn nothing.
  ⚠️ **The advised card is written by `CardId`, exactly as the answer already is** (§3.1): two
  decks hold two 5♥, and a comparison that said *"she agreed"* because the values matched would be
  wrong on precisely the hands worth studying.
  ✅ **Replay is unaffected**: `JournalPlayerAgent` ignores the field and
  `JournalHeader.CurrentRulesRevision` does not move.

**Two mechanical guarantees, in the shape P18/P20/P23 each had to build one layer at a time.**

- 🔥 **`BotCatalog.Hardest` must implement `IExplainsDiscards`**, asserted. The adviser is always
  `Hardest` (`ComputerAdvice`); promote a rung that cannot explain itself and the browser loses
  the feature **silently**. This is `StandingAnswerTests`' shape and `LayeringTests`' teeth.
- **Every key a rung declares carries a name and a direction**, asserted over the catalog, so a
  fourth key added later cannot reach the screen as a bare number.

**Acceptance.**

1. Playing a browser seat with hints on, every one of the questions can be opened and read,
   and the discard's explanation names **the key that separated the chosen card from the next
   best** — including when the answer is *nothing did*.
2. The explanation costs **no extra `PartialCover.Best` calls** over today's hint — asserted, not
   assumed.
3. A browser table played with `--journal` writes a file in which a human seat's decisions carry
   the adviser's card and rationale, and **`Answer != Advice.Card` picks out the disagreements**.
4. `ConcealmentTests` still passes with no rationale reachable from any `TableEvent`, and
   `MarkupStandardsTests` still passes with the prose inside a `<details>`.
5. A rung promoted to `Hardest` without an explanation **fails the build**.
6. 🔥 **The explanation is the bare rung's and never a level's** — asserted at every difficulty,
   including `easy`. A page that justified `FallibleAgent`'s deliberate slip would be the worst
   failure this packet can have, because it is the one that still looks right.
7. ✅ **Added 2026-08-22.** A card the feeding ban has taken out of the choice is explained as a
   **rule and not as a judgement** — and on a turn where the ban's floor yields (§5.1: every card
   legal again, because nothing else was), the explanation **stops saying it**. ⚠️ A rationale
   computed from a stale ban is confidently wrong in the one situation nobody has seen.
8. ✅ **Added 2026-08-22.** The sentence is true **at the table it is played at**: it reads the
   same `TableRules` the evaluator does, so it does not tell a five-handed player that a card
   matters to a series requirement five-handed play does not have (`RULES.md` §7.1.1). **Asserted
   at four and five seats**, which are different games.
9. ✅ **Added 2026-08-22.** No explanation implies the computer is **playing for the clean bonus**
   (§7.3). It is not: `CoverScore.Potential` returns `int.MaxValue` for a joker, so the true
   sentence is that it will never throw one — asserted on a hand holding a joker.

**Done when.** You can sit beside Mya Lay, ask the computer why, and afterwards run a query over
the file that lists every turn on which she and it chose differently.

⚠️ **Sequencing, and the one real concern.** `RULES.md` §5.1 — the feeding ban — is **Settled and
wholly unimplemented**: `RoundEngine` accepts any of the fourteen, so `CoverScore.Ranking` can
rank — and this feature would then **confidently justify** — a discard the rule forbids. Beside an
expert that produces disagreements which are noise about a rule rather than signal about
strategy — the opposite of what the instrument is for.

⚠️ **This paragraph's conclusion was reversed on 2026-08-21 and the reasoning is kept because it
was right.** It read: ***Build P24 first anyway** — §5.1 is blocked on six unanswered specification
questions (§9 #16–#19, #25 and #27), and this is the thing that makes the next conversation with
the person who raised the rule productive.* ✅ **That conversation happened and all six are
answered**, so waiting costs nothing — and P25–P27 have since made the concern above much larger
than one rule: they change **what a good card is** at three of four table sizes, **what a card is
worth**, and **which cards are legal to throw**. 🔥 **An explanation of decisions that are about to
change is a sentence written twice and believed once.** **P24 is recorded after P29 in §4 — a
recommendation, not a decision.** 🔥 **But when §5.1 lands, it lands here too** — Nick's two
rulings of 2026-08-20 are that a banned card is *not a legal discard* and that **where nothing is
legal the ban yields for that turn** (§9 #20, raised and closed the same day). So **every agent's
ranking is filtered to legal cards** — *the hand minus the banned ranks, or the whole hand if that
is empty*, which is **never empty by construction** — and the explanation gains a fourth thing it
can say: *"9♦ was not throwable — you would be feeding."* ⚠️ **And a fifth, on the rarest turn
there is**: under the floor every card is legal again, so an explanation that has been saying
*"not throwable"* all round must not keep saying it — the filter is per turn, and a rationale
computed from a stale ban would be confidently wrong in exactly the situation nobody has seen
before.


---

### P25 — The win condition is a function of the table size ☑

**Goal.** `HandEvaluator` stops answering the five-handed question at every table. What thirteen
cards must contain — and how many of their series must be joker-free — becomes a function of the
player count (`RULES.md` §7.1.1).

**Read first.** `RULES.md` §7.1, **§7.1.1**, §9's resolved entries for #7, #22, #23, #28, #29.
In the tree: `Domain/Melds/HandEvaluator.cs`, `Domain/Melds/PartialCover.cs`,
`Domain/Melds/MeldCandidates.cs`, `Domain/Engine/RoundEngine.cs`.

**Depends on.** Nothing. Domain only, and it is the largest single divergence in the tree.

🔥 **This is not a filter over the existing search, and that is the whole difficulty.** §9 #23's
answer makes the requirement a property of **the partition chosen**: a hand can have one cover
that satisfies the table's series count and another that does not, so `TryFindCover` may no
longer return the first cover it finds. The search has to be asked a **harder question** —
*is there a cover with at least N series, of which at least N are joker-free?* — not asked for a
cover and then audited.

⚠️ **Two-handed is a constraint of a different kind.** Sets are **illegal as melds** (§9 #22), so
there the requirement prunes the candidate set before the search starts, and is cheap. Three- and
four-handed are the expensive cases.

**Build.**

- `TableRules.For(playerCount)` — the §7.1.1 table as data: required series, required clean
  series, sets legal or not. One place, because it is quoted in three.
- `HandEvaluator.IsWinning(hand, rules)`; the parameterless overload goes.
- The cover search carries **counts along the partition** rather than auditing a finished one.
- `Meld.IsClean` — a run with no joker in it. ⚠️ A joker-only meld is a series and is never clean
  (§9 #29), which is the boundary case worth a test of its own.

**Done when.** A three-handed hand that covers only with a set is **not** a win; the same
thirteen with a clean `3+3` split **is**; a two-handed hand containing a set is not a win however
its runs fall; and a five-handed hand is judged exactly as it is today, byte for byte.

⚠️ **Cost, stated in advance.** Every strategy figure in `docs/STRATEGY.md` was measured at four
seats under the five-handed rule, so **P29 re-measures**. And `outs` reasons about *cover count*
(P21) — a rung whose objective is now the wrong objective at four seats, which is a finding to
publish rather than a bug to fix.

**What it shipped, 2026-08-21.** `Domain/Melds/TableRules.cs` is the §7.1.1 table as data and the
only place it is written down; `TableState.Rules` and `TurnContext.Rules` are the one place the
engine and a seat read it from; `HandEvaluator.IsWinning(hand, rules)` and
`TryFindCover(hand, rules, out melds)` are the whole public surface, and **the parameterless
overloads are gone**, so every caller in the tree had to say which table it was asking about.
26 new tests, 574 passing.

🔥 **The search carries what is still owing, and that is what made the memo the interesting
part.** The state is `(covered, seriesStillOwed, cleanStillOwed)` rather than `covered` alone: a
covered-set from which no completion can supply *two* more clean series may perfectly well supply
one, so the old key would have poisoned the second question with the first question's dead ends.
The counts are clamped at nought — a clean series discharges both, an impure one discharges the
series count alone (§9 #28, #29) — and there is one prune, that a hand with fewer than three
uncovered cards per series still owed cannot pay for them however it is arranged.

✅ **Two-handed really is the cheap case, and it was cheaper than expected.** Sets are illegal *as
melds*, which is a property of a meld rather than of the partition, so `MeldIndex.Build` takes
`setsAllowed` and the search never sees a set. ⚠️ **Filtering on `Meld.Kind` keeps more than it
looks like it does**: `MeldCandidates` already emits a card set that is both — `{9♦,🃏,🃏}`,
`{🃏,🃏,🃏}` — once, as the **run** interpretation, so those survive the filter, which is right.

✅ **`Meld.IsClean` needed no special case for the all-joker meld** (§9 #29). It is
`Kind == Run && JokerCount == 0`, and three jokers fail it because every slot is a substitute.

⚠️ **`PartialCover` was deliberately not touched, and it is now measuring a different thing from
the evaluator.** `IsComplete` agrees with `IsWinning` **only at five or more players**; at two,
three or four a hand can cover exactly and still lose. That is the P29 finding arriving early —
every rung's objective is cover count — and it is recorded in the type's own remarks so nobody
re-derives it from a failing test.

🔥 **The change is real and `drive-console.py` cannot see it.** Four-handed greedy-vs-simple over
200 games at one seed goes from **25.1 to 26.6 turns a round** and from 102 to 86 rounds/s — the
condition is strictly harder and rounds are longer, exactly as predicted — and yet **both console
captures are byte-identical to the pre-P25 tree** (`--script bots` 8,417 bytes, `--script human`
90,251). ⚠️ **Not because nothing changed: because both scripts quit in round 2 and neither
capture contains a declaration at all.** The instrument that proved P21 and P23 were refactors is
blind to the win condition by construction. **Do not read a clean `cmp` as evidence about play.**

⚠️ **Left behind, and owned by no packet: `RoundEngine.MinimumPlayers` is still 4** (`RULES.md`
§10 #7). `TableRules.For(2)` and `For(3)` are correct, tested, and unreachable from a game — the
engine cannot deal a two- or three-handed round, so the two strictest rules in §7.1.1 are
implemented and never executed. That is a **separate packet**, not a line in this one: it needs
the deal, the money layer and both front ends to agree that a table can be smaller than four.

---

### P26 — The money layer as it actually is ☑ done 2026-08-21

**Goal.** Settlement pays what `RULES.md` §4 now says: jokers are permanent money cards, a
designation landing on a permanent card pays **×3**, and a 7♦/A♠ turn-up whose partners are owned
by **one player** pays **×5** each.

**Read first.** `RULES.md` §4.1, §4.3, §4.4, §10 #17, and §9 #32 — **which is open, and bounds
this packet.** In the tree: `Domain/Money/MoneyCardRegistry.cs`, `Domain/Money/Settlement.cs`,
`Domain/Money/CardOwnership.cs`, `Domain/Agents/MoneyOdds.cs`,
`Tests/Agents/ProspectorBotAgentTests.cs`.

**Depends on.** Nothing. Domain only.

🔥 **The third rule changes the shape of the function, and that is the packet.** (a) and (b) are
edits to `Permanent` and to `Multiplier`'s arithmetic. **(c) cannot be computed from the
designators at all** — the ×5 depends on *who owns what*, so `Multiplier(Card)` gains the round's
ownership and `Settlement` stops asking the registry about a card in isolation. ✅ **The design
decision in `CLAUDE.md` survives intact** — money status is still computed and never stored — but
say so in the code, because a reviewer will read a widened signature as the decision being
abandoned.

⚠️ **`ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe` is expected to be
red before this packet starts.** It asserts P22's withdrawn derivation. 🔥 **Its last assertion
should invert to an equality** — a designation on the 7♦ and one on an ordinary card leave the
same money loose in the shoe, because ×3 is exactly what conserves it. **That is a prediction and
it has not been run**; if it does not come out equal, the arithmetic in `RULES.md` §4.1 is wrong
and the rule needs re-asking before the code is changed to match.

**Build.**

- `Permanent` gains the jokers. ⚠️ A joker's identity is its **colour** (§4.2), so the
  permanence test is `SameValueAs` and needs no special case — but the *count* changes from 4
  cards to 8, which is what moves every derived figure.
- `Multiplier(card, ownership)` → 1, 3 or 5.
- `Settlement` computes the ownership configuration once a round, not once a card.
- Re-derive the two stale `DERIVED` notes with a number rather than an argument: §4.3's *"the
  side-bet is 42% of the round prize"* (measured at rev 13 with four permanent cards) and §4.4's
  *"~4 of the 6 are owned when the deal ends"*.

**Done when.** A round in which one player owns both partners of a 7♦/A♠ turn-up settles at $40 a
head at standard stakes; the same round with the partners split settles at $24; and the two stale
notes carry measured numbers.

⚠️ **Scope fence: do not generalise the ×5 past the 7♦/A♠ pair.** §9 #32 asks whether two jokers,
or a 7♦ and a joker, do the same thing, and it is unanswered. **Write the narrow rule and a test
that pins the narrowness**, so widening it later is a visible change.

#### What P26 found

🔥 **The prediction the packet was told to check came out right, and that is the headline.** It was
written as *a prediction, not a measurement*: under the ×3, a designation on the 7♦ and one on an
ordinary card should leave **exactly the same** money loose in the shoe. They do —
`WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`'s last assertion is now
`Assert.Equal(spread, tripled, 9)` and it passes. **So `RULES.md` §4.1's arithmetic is right and
the rule did not need re-asking.** ⚠️ **The test was *green* at `HEAD`, not red** — `STATUS.md`
said it was "expected to be red before P26 starts" and that was wrong in a way worth naming: the
code still implemented the withdrawn rule, so the test agreed with the code and disagreed only
with the document. **A test cannot go red for a rules change until somebody changes the code.**

🔥 **The ×5 is a property of a *(value, ownership)* pair and the signature says so.**
`MoneyOwnership` is a one-field record struct — *who, if anybody, owns both partners of a 7♦/A♠
turn-up* — computed by `MoneyCardRegistry.ConfigurationOf(ownership, shoe)` **once a round**, and
`Multiplier(card, owner, ownership)` is the whole answer. ✅ **`Multiplier(Card)` survives as the
value-only question**, which is what every view that draws one card at a time is actually asking;
that is why nothing outside `Settlement` needed a parameter. ⚠️ **And no view can show the ×5** —
`CardView.Multiplier` is 0, 1 or 3 by construction, and a card's marker cannot depend on the rest
of the round's ownership. The jackpot is settled and never drawn. **That is a real UX gap and no
packet owns it.**

⚠️ **The narrowness is a test, because §9 #32 is open.** `TwoTripledJokersInOneHandAreNotAJackpot`
(both colours, and both copies of one colour) and `ASevenAndAJokerTurnedUpAreNotAJackpotEither`
assert that the combinations rev 21 *created* pay ×3 and not ×5. Widening the rule has to break
them.

🔥 **The side-bet is half again as big and the game is still not inverted.** Measured with the same
command before and after — `--games 600 --seats 5 --seed 20260821`, greedy vs simple, summing each
round's positive `side_bet` deltas out of `--csv` — the money cards move **`$11.58 ± 0.34` a round
against `$8.50 ± 0.26`**: **58% of the $20 round prize, up from 42.5%**, and 37% of all the money
that changed hands. ✅ **The method is validated rather than asserted**: the *before* run reproduces
P12's rev-13 figures ($8.43, 42%, 30%) at a different seed, so the difference is the rules change
and not a change of instrument. ✅ **§4.2's exact-match argument survives with new numbers** —
rank-matching would still roughly double it again, and *that* would invert the game.

⚠️ **Play did not move and the run proves it.** Same seed, before and after: identical wins,
identical turns, identical cover. **The money layer is genuinely decoupled from the melding game**
(§4.4), which is the design property the rules claim and this is the first time it has been
measured across a change to the money layer.

⚠️ **The bill landed in the front ends after all, and it is one word.** `($$)` had to become
`($$$)`, `CardDisplayState.PaysDouble` became `PaysTriple`, and four user-facing strings said
*double*. **The packet is domain-only in its logic and not in its text** — leaving them would have
printed a false number to a player. ⚠️ **So a `drive-console.py` capture from before P26 no longer
compares** (see P29).

---

### P27 — The feeding ban ☑ done 2026-08-21

**Goal.** §5.1 becomes real: you may not discard a rank the next player has taken in the open —
**enforced by construction**, so a banned card is never offered and cannot be chosen.

**Read first.** `RULES.md` **§5.1** entire, §9's resolved entries for #16–#20, #25 and #27, and
§10 #13. In the tree: `Domain/Engine/RoundEngine.cs`, `Domain/Agents/TurnContext.cs`,
`Domain/Agents/CoverScore.cs`, `Domain/Agents/FallibleAgent.cs`, `Domain/Cards/Card.cs`.

**Depends on.** Nothing in code. It is the first work since P0 that changes **what a legal turn
is**.

🔥 **Three things this needs that the domain does not have.**

1. **A rank-only predicate.** `==` is instance identity and `SameValueAs` is rank *and* suit *and*
   colour. **Reaching for `SameValueAs` here implements the wrong rule** — it would leave the Q♣
   Mya Lay actually objected to perfectly legal. ⚠️ **And a joker has no rank at all**: taking one
   closes *the other jokers*, all four (§9 #27) — a `PLAYER` house ruling, so mark it in code as
   the one line here that is not `EXPERT`.
2. **A released-rank set per seat.** The release is *"has this player thrown that rank this
   round?"*, and §5's reshuffle sweeps the pile that would answer it — so the set is **kept, not
   read back off the table** (§9 #19).
3. **The floor.** The legal discards are *the hand minus the banned ranks, **or the whole hand if
   that is empty*** — never empty by construction, so a turn cannot deadlock. ⚠️ Reachable on two
   ranks, because two decks put eight copies of a rank in the shoe.

⚠️ **Every agent's ranking is filtered, `FallibleAgent`'s runner-up included** — a mistake still
has to be a legal move. ✅ **It costs nothing against the concealment model**: every fact the ban
is computed from is already public (§9 #15).

**Done when.** A seat is never offered a banned discard; a released rank stays released across a
reshuffle; a hand of nothing but banned ranks completes its turn; and a difficulty level's mistake
is always a legal card.

**Done when, second.** ✅ **The card a player just took is filtered like any other** (§9 #13) —
*"yes, as long as you aren't violating any other discard rules"* — so there is no special case for
it, and a test says so.

**What it turned out to be.** ✅ **All four "done when"s, and the rule needed no code outside the
one type.** `Domain/Play/FeedingBan.cs` is two `HashSet<Rank?>` and one method; `PlayerState`
carries one per seat and `TableState.SeatFedBy` finds the seat that reads it;
`TurnContext.LegalDiscards` is the whole of the choice a turn presents.

🔥 **The filter is a property of the ladder rather than a line each rung remembers.**
`CoverScore.Discard` and `CoverScore.Ranking` take a `TurnContext` instead of a hand, so every rung
that goes through them is filtered by construction and **the runner-up a difficulty level throws is
filtered with it**. Only `RandomBotAgent` had to say so itself, because it is the one rung that
does not rank.

🔥 **The predicate is `Card.SameRankAs`, and it is literally `Rank == other.Rank`.** Nullable
equality is what makes a joker close the other jokers — §9 #27's house ruling falling out of the
type rather than being written as a case. ⚠️ **It is one method because §9 #30 says P28 must reuse
it**, not two that agree by inspection.

⚠️ **The floor is one line and the declaring-discard exception is not.** Exception 2 costs a
`HandEvaluator.IsWinning` per banned card, gated behind one `PartialCover.CoversAtLeast(hand, 13)`
so it is asked only of a hand that could actually go out — and only when a rank is closed *and*
held, which is the case that pays for the whole computation.

🔥 **The finding that outlives the packet: a bot's cover count can now fall.** Every rung's score
was monotone — "throwing back the card just taken restores the hand exactly", which is
`GreedyBotAgent`'s stated reason a table of bots terminates at all. **§5.1 removes that card from
the choice.** A seat whose only legal discards are melded ones gives up a meld, so a round is no
longer guaranteed to converge and the argument in `GreedyBotAgent`'s remarks is now false as
written. In practice the tree still finishes; `SimulationOptions.TurnCap` and the hosted table's
`RoundTimeLimit` are what stand behind it. ⚠️ **P29 should report round lengths and abandoned
rounds, not only win rates.**

⚠️ **Both front ends needed a change, and it is not a hint.** A closed card is
`CardDisplayState.Unthrowable` — a state with a token (`closed`), a legend entry and an accessible
name — because a card that cannot be pressed must say why. The browser draws it as a `<span>`
rather than a disabled button, and the server refuses a remote answer naming one
(`SeatPrompt.MayThrow`) so a hosted table does not fall over on a bad client.

---

### P28 — The claim, the permission, and the seat you sit in ☑ done 2026-08-21

**Goal.** Two rules the engine currently contradicts rather than lacks: **the seating is
re-randomised every round** (§3), and **claiming the turned-up money card needs the upstream
player's permission** (§4.5).

**Read first.** `RULES.md` §3, §4.5, §10 #16 and #18, §9's resolved #5, #14 and #30. In the tree:
`Domain/Engine/MatchEngine.cs`, `Domain/Engine/RoundEngine.cs`, `Domain/Agents/IPlayerAgent.cs`,
`Server/SeatPrompt.cs`, `Server/TableSession.cs`, `Web/Components/Table/TableRing.razor`.

**Depends on.** **P27**, ✅ **which is done** — the objection predicate *is* §5.1's predicate
(rank alone, §9 #30), and it is now **`Card.SameRankAs`**. ⚠️ **Read it rather than writing a
second one**: the objection test is `hand.Any(held => held.SameRankAs(turnedUp))` and nothing more.
🔥 **And the permission and the ban are one mechanism, not two that agree** — the reason the
upstream seat may refuse is that a claim *arms* §5.1 against them, which
`RoundEngine.TakeCard` already does through `PlayerState.TookInTheOpen`. **A refused claim must not
arm it**, which is the one line where P28 touches P27's code.

🔥 **The permission rule needs a third kind of agent decision, the first new one since the engine
was built.** Not *what do I take* or *what do I throw* but **do I object** — and ⚠️ **it is asked
of a seat that is not on turn**, which no `SeatPrompt` in `BurmesePoker.Server` does. The hosted
table needs an out-of-turn question and the browser needs somewhere to put it.

🔥 **And the answer is a disclosure.** Only a holder may object, so an objection tells the table
that seat holds that rank — **the first thing in this concealed game a player reveals by choice**,
everything else being hidden until the declaration. That is a `TableEvent`, public by
construction, and it belongs in `ConcealmentTests` as a decision rather than in the UI as a
detail.

⚠️ **Re-seating is small in the engine and awkward in the browser.** `MatchEngine` holds one
seating for a whole match; it should re-draw before every deal, and the round engine already takes
its seating as given. **But P13.5 puts *you at the front whichever seat you were dealt***, so
re-seating means the table visibly rearranges itself around a fixed viewer every round, and
`TableRing` has never been asked to do that. ⚠️ **It also rebuilds every §5.1 ordered pair each
round** — harmless, since the ban is per round anyway. ✅ **P27 makes that structural rather than a
thing to remember**: a `FeedingBan` lives on a `PlayerState`, a `PlayerState` is built by the deal,
and `TableState.SeatFedBy` reads the seating it was constructed with — so a re-drawn seating gets
fresh bans by construction and there is nothing to reset.

**Done when.** Seats differ between consecutive rounds of one match; a claim is refused when the
upstream seat holds that rank and it chooses to refuse; the refusal is visible to every watcher;
and a bot in the upstream seat answers the question without being on turn.

✅ **All four, 2026-08-21.** `RULES.md` is **rev 24** and **§10 #16 and #18 are discharged — with
them, every rule this document records as Settled is implemented.**

🔥 **What it found, in the order the findings cost something.**

1. 🔥 **A serializer's catch-all is a mistranslation waiting for the next case, and this one had
   been waiting since P14.** `JournalFormat.Name(JournalQuestion)` ended `_ => "declare"`, so the
   fifth question was written to file as a **declaration** — a journal that reads back as a
   different game. ⚠️ **The in-memory replay test could not see it**, because it never crosses the
   format; only `AJournalWrittenToAFileReplaysFromIt` went red. Every case is named now and the
   default arm throws.
2. 🔥 **Re-seating and the scripted deal are in tension, and the resolution is P7's own.** A deal
   written down card by card is a deal written down *for a seating*, so `PlayRound(drawOrder)`
   draws no seats and `PlayRound()` — shuffle, then seat, then deal, which is §3's order — draws
   them. Every production caller uses the second; every scripted test uses the first. **Putting
   the draw in the shared path instead hangs the suite**: the winning hand goes to whichever seat
   is dealt first, the agent that knows how to declare is somewhere else, and a table of passive
   seats plays for ever.
3. ⚠️ **Three places in the server assumed *asked* meant *on turn*; exactly one was
   load-bearing.** `BoundedAgent` announces `TurnBegan` on the first question of a turn, and the
   permission carries the **opener's** turn number — so announcing it moves every client's
   spotlight onto the wrong seat mid-turn. It checks the clock and stays quiet.
   `RemotePlayerAgent`'s takeover announcement is left alone deliberately: the computer really did
   play that seat's decision. `PacedAgent` beats once, which reads as the seat thinking.
4. 🔥 **A test that answers "no" to a question can hide the question after it.** `ClickingPlayer`
   and `ScriptedSeat` both declined every claim, so the permission was unreachable from either
   fixture and `EverySeatIsAskedAllFourQuestionsOverAMatch` would have gone red on a fifth case it
   could never produce. **`ClickingPlayer` claims now** — a page can press *Claim* as easily as
   *Leave it* — and the assertion is honest again.
5. ⚠️ **Whether to object is a decision and not a derivation, and no rung prices half of it.**
   Every bot refuses whenever it may, on §4.5's own reasoning; **nothing models the disclosure**,
   which is real — objecting tells the table you hold that rank. It is the one place a future rung
   could differ without touching the engine, and P29 is where the cost of the policy first shows
   up in a number.
6. ⚠️ **A seed from before this packet no longer plays the same match** (§3.9 point 2), and **a
   `drive-console.py` capture no longer compares either** — for the third packet running. The
   console prints the seats every round now instead of once at setup, and a human seat can be
   asked for permission.

---

### P29 — Re-measure, under the rules as they are ☑ done 2026-08-21

**Goal.** `docs/strategy/measurements.csv` regenerated under P25–P28, and `docs/STRATEGY.md`
saying plainly which of its published numbers moved and why.

**Read first.** `docs/STRATEGY.md` §§4, 9, 10, 11; `Tests/Sim/StandingAnswerTests.cs`; §3.8.

**Depends on.** P25, P26, P27, P28 — all four, because each changes what a round is worth.
✅ **All four are in as of 2026-08-21, so nothing blocks this.**

⚠️ **Every figure in `STRATEGY.md` was measured under rules this document no longer holds**: four
seats judged by the five-handed win condition (P25), a money model without permanent jokers or the
×3 (P26), and no feeding ban at all (P27). **The money sweep is the worst affected** — `prospector`
is the one rung whose decision reads the money.

🔥 **Three predictions, written down before the run so the packet can be wrong.**
1. **The difficulty dial survives.** ε is a property of *the mistake*, not of the rung (P23), and
   §5.1 filters both the winner and the runner-up. **If the four levels stop being ordered, that
   is the finding.** ⚠️ **P27 sharpened the risk rather than removing it**: the ranking a level
   slips down is now often *shorter*, because closed ranks are dropped from it — and where the ban
   leaves one candidate there is no runner-up at all, so ε does nothing on that turn. **A dial
   whose steps depend on how often the ban binds is a dial that moves with the table.**
2. **`outs` loses some of its margin at four seats.** Its objective is cover count, which is no
   longer sufficient for a win — so a rung that looks ahead at the wrong target should narrow
   against `greedy`.
3. **`prospector` separates at lower stakes than $5/$40.** There is more money on the table (8
   permanent cards, not 4) and a ×5 case worth $40 a head. ✅ **P26 priced the first half of this
   and it is bigger than it looks: the side-bet went from `$8.50` to `$11.58 ± 0.34` a round at
   five seats** — 42.5% of the round prize to **58%** — measured with `--games 600 --seats 5`
   before and after, greedy vs simple. ⚠️ **The ×5 is not in that figure** (one round in 1,444) and
   **`MoneyOdds` does not price it at all**, so `prospector`'s *estimate* of a blind draw moved
   only by the triple and the jokers.

🔥 **A fifth thing, and P28 created it: how often a claim is refused, and what refusing is worth.**
Every rung objects whenever it may, which is a *decision* taken in P28 on §4.5's own reasoning and
not a measured one — and the seat that may object is holding the rank roughly half the time, so
this is not a rare branch. ⚠️ **Two numbers nothing reports yet**: how often the opener's claim is
refused, and what a table of always-refusers wins against a table of always-allowers. The second is
a one-cell head-to-head between two policies of the same rung — the shape P22 used for
`prospector` — and it is the only way to find out whether the disclosure is worth what the lock
costs. **If it is a null, publish it** (P20).

🔥 **A fourth thing to measure, and P27 created it: how long a round runs, and how many do not
finish.** Every rung's cover count used to be monotone, which is the stated reason a table of bots
terminates; §5.1 takes the just-taken card out of the choice, so a seat with nothing but melded
legal discards gives up a meld and a round is no longer guaranteed to converge. ⚠️ **`Sim` reports
abandoned rounds already (`TurnCap`) and nothing published has ever quoted the number.** If it is
not zero, that is a result and not a nuisance.

⚠️ **`sim suite` was five hours at P23** and P25 makes the evaluator's question harder. (✅ **It
came in at 9,981 s — see the outcome below.**) **Budget
it, measure the new per-round cost with `sim bench` first, and say in the packet what was
dropped** if anything is. ✅ **P25's own cost is now measured and it is small**: four-handed
greedy-vs-simple went from **102 to 86 rounds/s** at one seed, about **16%**, and roughly half of
that is the round being longer (25.1 → 26.6 turns) rather than the evaluator being slower. **That
is one measurement at one seed with two weak rungs, not a budget** — re-time it with `sim bench`
once P26–P28 are in.

⚠️ **Prediction 2 has a first data point and it points the right way — treat it as a smoke test,
not a result.** In that same 200-game run `greedy` fell from 32.3% to 31.3% and `simple` rose from
17.8% to 18.8%, which is inside the interval and proves nothing on its own. 🔥 **What is *not*
inside an interval is the reason**: `PartialCover` was left alone by P25 on purpose, so every rung
in the catalog is still maximising cover count at a table where cover count is no longer the win
condition. Prediction 2 is a claim about that gap, and P29 is where it is priced.

⚠️ **And a capture from before P28 no longer compares either, for the third packet running**: the
console prints the seats at the top of every round now, because they are re-drawn every round, and
a human seat can be asked for permission mid-way through somebody else's turn.

⚠️ **A capture from before P26 no longer compares, and that is new.** P26 changed what the
console *prints*: `($$)` is `($$$)`, `CardDisplayState.PaysDouble` is `PaysTriple`, and the legend
reads *pays triple*. **P23's promise that a pre-P23 capture still compares is spent** — re-capture
the baseline from `HEAD` before using `cmp` to prove anything about a front-end refactor.

⚠️ **`drive-console.py` cannot help here and P25 proved it.** Both scripts quit in round 2, so no
capture in this repo contains a declaration; a byte-identical `cmp` across a win-condition change
is what it looks like when the instrument cannot see the question. If P29 wants a front-end
regression check that covers going out, **the script has to be extended to play a round to its
end** — which is new work, and worth pricing before promising it.

**Done when.** `measurements.csv` regenerates from one command, `StandingAnswerTests` is green,
and §11 records which rows moved, in which direction, and whether each was predicted.

---

**✅ Done 2026-08-21. What it found, kept here because the predictions above are what it is
judged against.**

**91 measurements in 9,981 s**, and **4 of the 91 rows byte-identical — the four ε constants.**
⚠️ **That is not a determinism failure**: the only rows that can survive a rules change are the
rows that are not measurements, and here they are literally the four numbers a person chose.
**"Does it reproduce" is not a question that can be asked across a rules change**, which is worth
knowing before the next packet changes one and goes looking for the bug.

| Prediction | Outcome |
|---|---|
| **1. The dial survives.** | ✅ **Held.** All three steps separate under Holm, **no ε moved**, reference spacing improved to **8.1 / 7.9 / 7.1** from 7.9 / 6.7 / 7.7. The stated risk — that §5.1 shortens the ranking a level slips down, making the steps depend on how often the ban binds — did not materialise at a ban that binds on about a fifth of turns. |
| **2. `outs` loses some of its margin.** | ❌ **Wrong, and the estimate did not even drift**: `+3.0 ± 1.0` over `greedy` against +3.1 ± 1.0, mean margin **+14.6** both times. |
| **3. `prospector` separates below $5/$40.** | ✅ **Held, by a whole ratio.** $5/$20 went from `+0.95 ± 1.63` *inside the interval* to **`+5.32 ± 2.27` separated under Holm**, with the take rate collapsing **8.4% → 0.1%** — the mechanism variable, so it is a measurement and not a coincidence. |

🔥 **Prediction 2 is the one worth keeping, because being wrong about it located the effect.**
The reasoning was that every rung maximises cover count and cover count no longer wins at four
seats (§7.1.1) — so the rung that looks ahead at the wrong target should narrow. It did not.
**What moved instead is `simple`**, which gained about two points on each of `greedy`, `cautious`
and `counting` (11.2 → 9.2, 10.8 → 8.8, 11.0 → 9.9). ⚠️ **The four-handed condition demands a
joker-free series and nothing in any rung is aimed at it, so the better melder pays the same
tax**: a requirement nobody optimises for **levels a ladder from the bottom rather than tilting
it**. The prediction had the mechanism right and the direction of its consequence wrong.

✅ **The fourth and fifth things were measured and both are published** (`STRATEGY.md` §12).
**Round length and abandoned rounds**: 28.6 turns a round for the ladder, 30.0 for the dial, 23.9
for the two-arm claim cell; **the only non-zero abandoned count is the field containing `random`**
— 8 games of 9,072 — and both all-`outs` fields settled every game they played. ⚠️ **The honest
statement is narrow**: no table of thinking rungs has yet failed to converge, not that none can.
**What refusing a claim is worth**: `outs/refuse` over `outs/allow` at 8,008 games is
**`+0.4 ± 1.0`** on win rate and **`+0.02 ± 0.18`** on money — **a null, published**, and
published *with its denominator*, since the opener asks in about a quarter of rounds and the
upstream seat holds the rank about half the time.

⚠️ **The wall-clock warning above was wrong in the cheap direction.** The suite is **two and three
quarter hours**, not five, *with a cell added* — `outs` costs **7.0×** a `greedy` round against
8.2× at P21, because P25's win condition prunes the cover search earlier than it lengthens the
round. **Nothing was dropped.** ✅ **`sim bench` first was the right instruction and the right
answer came out of it**; the stale figure had been quoted in three documents.

🔥 **And one thing this packet did not do: it raised no rules question.** `RULES.md` is untouched
at rev 24 — the first packet since the rules sessions began to measure without discovering a rule,
which is what a re-measurement packet is for.

---

### P30 — Verify what we have: a review, then a conformance harness ☐

> 🔥 **Two halves, in this order, and both are run on Fable 5** (owner's call, 2026-08-21).
> **P30.1 is a thorough code review; P30.2 is the conformance harness and the front-end tests.**
> **P31 and P32 return to Opus.** Set the model with `/model` before starting the session; the
> packet does not choose it.
>
> 🔥 **Why the review comes first, and it is not ceremony.** A conformance test can only check a
> rule somebody thought to check — **the harness in P30.2 is exactly as good as its list**. A
> review is the half that finds the rule nobody thought about, the predicate written twice, the
> `switch` whose default arm quietly means something. ⚠️ **This project has shipped that last one
> already**: `JournalFormat.Name` ended `_ => "declare"`, so the moment a fifth question existed
> every objection was written to file as a declaration, and **only a file round-trip test could
> see it**. **The review's findings become P30.2's checklist**, which is why they are two packets
> and not one.

---

### P30.1 — A thorough code review ☑ — **Fable 5**

> ✅ **Done 2026-08-21.** `docs/REVIEW-2026-08.md` — 37 findings, every one triaged, none
> unassigned; the baseline was 672/0 and no code changed. **All three acceptance criteria are
> met** (a prioritised list with file/line/severity/class; every finding assigned; the unread
> boundary stated). The spec below is kept as the record of what it was run against.

**Goal.** A written, prioritised findings list over the whole tree, from a reader who is not the
author, with `docs/RULES.md` open beside the code.

**Read first.** `docs/RULES.md` §§4–7 and §10; `BUILD-PLAN.md` §3 (the settled design decisions);
`docs/STATUS.md`'s *Notes for the next session* for the last five packets — **the findings there
are the shapes of defect this codebase actually produces**, and they are the best prior a reviewer
can have.

⚠️ **Not a style pass.** The tree is heavily commented on purpose and the comments carry the
reasoning; **rewriting them is a loss, not a tidy-up.**

**What to look for, in priority order.** Each of these is a defect class this project has already
shipped at least once — the packet that found it is named, and **the review's job is to find the
next instance rather than to re-read the one that was fixed.**

1. 🔥 **A rule implemented in two places.** §9 #30's whole lesson: the claim's objection test and
   the feeding ban's test are **one predicate**, and writing the second would have produced code
   that agrees by inspection and drifts by maintenance. **Look for a second implementation of any
   rule in `RULES.md`.**
2. 🔥 **A `switch` or `match` with a default arm that means something.** `JournalFormat.Name`
   (P28). **A default arm in a translator is a mistranslation waiting for the next case.**
3. ⚠️ **A test that cannot fail.** `ClickingPlayer` declined every claim, so the permission was
   unreachable from that fixture (P28); a stood-up seat's refusal was vacuous until a question
   stood in front of it (P13.6). **Ask of each rules test: what would make this red?**
4. ⚠️ **A test asserting a property of *round length* without saying so.** Two concealment tests
   asserted pairwise-disjoint hands, which was a coincidence of short rounds and broke when a
   stronger rung made rounds longer (P21). ⚠️ **P27 made this worse, not better** — a cover count
   can now fall, so round length is less stable than it has ever been.
5. ⚠️ **Text that contradicts code.** P26 found `($$)`, `PaysDouble` and four user-facing strings
   still saying *double* after the rule said triple. **"Domain only" is routinely true of the
   logic and false of the words.**
6. ⚠️ **Identity confusion.** Three notions now — `==` (instance), `SameValueAs` (rank + suit +
   colour), `SameRankAs` (rank). **Each is the wrong answer to the other two's question**, and
   §5.1's is the one that compiles while implementing a different rule.
7. ⚠️ **Determinism and shared state in `Sim`.** An agent that remembers across games makes a run
   depend on scheduling order. `OutsCache` is the one cache in the tree and it is keyed on card
   *values* deliberately — **check nothing has been added that is not.**
8. ⚠️ **Dead or unreachable code, named rather than deleted.** `TableRules.For(2)` and `For(3)`
   are correct, tested and unreachable while `MinimumPlayers` is 4 (§10 #7). **The review should
   say what else is in that state** — unreachable code that looks live is how a reader comes to
   believe a rule is enforced.
9. ⚠️ **The known gap, to be confirmed rather than rediscovered**: no view can draw a ×5 —
   `CardView.Multiplier` is 0, 1 or 3 by construction — so the jackpot is settled and never shown.
   **No packet owns it.** The review should say whether it is one line or a design change.

**Build.** Nothing. **This packet writes a document, not code.**

⚠️ **Fixes are a separate decision.** Anything the review finds that is a **rules** mismatch goes
straight into P30.2's checklist; anything that is a defect gets a line in `STATUS.md` and, if it
is small and safe, a fix in P30.2. **A review that silently refactors is a review nobody can
audit.**

✅ **`/code-review` is available and is a reasonable way to run part of this** — and
`/code-review ultra` runs a deep multi-agent review in the cloud. ⚠️ **It is user-triggered and
billed; a session cannot launch it for you.** It is a complement to the list above rather than a
substitute: it reviews a *diff* well and *nine packets of accumulated design* less well.

**Acceptance.**
1. A findings document exists — `docs/REVIEW-2026-08.md` — with every finding carrying a file,
   a line, a severity, and **which of the nine classes above it belongs to** (or *new class*,
   which is itself a finding).
2. **Every finding is triaged**: fix now, P30.2's checklist, a named later packet, or *won't
   fix, because*.
3. ⚠️ **The review states what it did not read**, so the next reader knows the boundary.

**Done when.** There is a list, it is prioritised, and nothing on it is unassigned.

---

### P30.2 — Conformance: the rules as *played* ☑ — **Fable 5**

> ✅ **Done 2026-08-21, on Fable 5.** Everything below was built as specified, with three
> findings a cold session should have: **(1) the premise about the console driver was stale** —
> both fixed-key captures had quietly started reaching round 1's settlement (the "no capture
> has ever contained a win" grep used `went out`; the console says `declares`), and the rewrite
> was still right, because the fixed lists were answering prompts by *position* and had already
> drifted twice; **(2) R1's fix is engine-declares** — a discard legal only under §5.1
> exception 2 *is* the declaration, so the agent is not asked again (a journal from before the
> fix that contains such a turn will replay with a loud divergence); **(3) R8's fix is a
> `SeatChannel`** — the seat's question state is stable across occupants, each `SitDown` mints
> a fresh `SeatConnection`, and a superseded connection is dead server-side, which also means a
> seat re-taken mid-round starts its event history at the handover (like a watcher who joined
> late). R30 (jokers as `DealBuilder` filler) was fixed by correcting the remark, not the
> filler — re-basing it would re-derive every hand-computed payout in the suite to delete
> comments that are currently true.

**Goal.** An executable answer to *"does the game we ship actually obey every rule `RULES.md`
records as Settled?"* — checked against **ordinary played rounds** rather than against fixtures,
and checked **through both front ends**, not only through the engine.

**Read first.** `docs/RULES.md` §§4–7 and §10; `Tests/Server/ConcealmentTests.cs`;
`scripts/drive-console.py`; BUILD-PLAN §3.8.

**Depends on.** **P30.1** ✅ — its findings are this packet's checklist — and **P24.1** ✅ for the
browser half (a hosted table that writes down what it did; `--journal` on the Web since
2026-08-21, flushed after every settled round).

> ✅ **P30.1 ran 2026-08-21 and the checklist exists: `docs/REVIEW-2026-08.md`, triage table at
> the bottom.** What it adds to the build list below, named so a cold session cannot miss it:
> **(1) R1** — §5.1 exception 2 is offered but never bound to the declaration; fix in this
> packet (a discard legal *only* under exception 2 commits the seat to declaring) and make
> `RuleConformance`'s §5.1 item read *"a discard is never a closed rank unless the floor
> applied **or the round ended on it**"*. **(2) R2** — set `JournalHeader.CurrentRulesRevision`
> to the current rev and **bind it to `RULES.md`'s header in the P23 idiom** so it cannot go
> stale again. **(3) R3** — pair the claim cell's money margin (a `NetPerRoundByGame` on
> `CellPlayer`); annotate `STRATEGY.md` §12 now, and let **P31's suite run** regenerate the row.
> **(4) R6/R7** — the fifth-question coverage holes and the §7.1.1-sensitive exception-2
> fixture are conformance tests this packet writes. **(5) R8** — fix seat-handover revocation
> server-side, then let the browser half test handover. **(6) The 29 P30.2-fix items** are
> small and safe; land them here, each with the review id in the commit message. ⚠️ **R13's
> new `SideMargin` rows also wait for P31's regeneration** — do not run the suite from this
> packet.

🔥 **Why this is worth a packet, in one sentence: everything in the tree today proves a rule
*can* hold, and nothing proves it *does*.** Every rules test is a scripted fixture — a deal built
card by card to put one situation in front of the engine — or a unit test on a helper. Those are
the right tests for a rule's edges. **What no test asserts is that a thousand ordinary rounds,
dealt at random and played by the rungs a person actually meets, never once break anything.**
⚠️ **This is the shape of defect the project has already shipped twice**: P21's concealment tests
were asserting a property of *round length* without knowing it, and P28's `JournalFormat.Name`
mistranslated a question for as long as the format had a default arm.

**Build.**

1. **`RuleConformance` — an `IGameObserver` that audits a round as it is played**, in the test
   project, asserting on every turn of every round handed to it:
   - **108 cards, always** (§2). At every moment an observer can look, hands + piles + deck +
     turned-up = the shoe. ⚠️ P13.2 *claims* this in a comment; nothing asserts it.
   - **One taken, one discarded, every turn** (§5), and ownership conferred **only** by the deal
     or a blind draw — never by a take and never by a claim (§4.4) — and **never transferred once
     recorded**.
   - **A discard is never a closed rank** unless the floor applied, and the floor applied only
     when the whole hand was closed (§5.1). **Release is permanent** once the protected seat
     throws that rank.
   - **A claim happens only on turn 1, only from the opener, only once** (§4.5); an objection
     comes **only from the seat immediately upstream of the claimant** and **only from a holder of
     that rank** (§4.5, §10 #18).
   - **A declared hand satisfies `TableRules.For(seats)`** — the series count *and* the clean
     count (§7.1.1) — and its melds **partition all thirteen, disjoint by `CardId`** (§6.3), with
     no set holding a duplicate suit (§6.2) and no run wrapping the ace (§6.1).
   - **Settlement**: payouts sum to zero, every multiplier is 1, 3 or 5 (§4.1, §4.3), and **a
     loser's flat component is exactly the round value however many cards it held** — §7.2's *no
     deadwood penalty*, which is a rule about something that must **not** happen and so has never
     had a test.
   - **The seats are re-drawn every round after the first** (§3) under `MatchEngine.PlayRound()`.
2. **Run it over a real field**, not a fixture: a few hundred games across the catalog and the
   dial, at **every playable table size — 4, 5 and 6**. ⚠️ **It shares
   `WallClockBudgets.Collection`**, and it is a *cheap* observer by §3.8 item 3 — integers only,
   no formatting, no hand copies — or it changes what it measures.
3. 🔥 **A coverage test, in the P23 idiom: every rule `RULES.md` marks Settled is either checked
   above or names why it cannot be.** Parse the § headings and their `Settled` tags out of the
   document, require each to appear in a conformance registry with either a check or a written
   exemption, and **fail the build when a rule is added to the document and to nothing else.**
   ⚠️ **This is the acceptance criterion that makes the packet more than a pile of assertions** —
   without it "every single rule" is a claim nobody can re-check.
4. **The console, driven to a declaration for the first time.** ⚠️ **P25 priced this and it is the
   real work in this packet**: `drive-console.py`'s two scripts are **fixed key lists** that quit
   in round 2, so **no capture in this repo has ever contained a win** (`grep -c "went out"` is 0).
   The driver has to answer **adaptively** — read the pty until a prompt appears, answer it, and
   keep going until the settlement panel — which is a different program from the one that exists.
   Then assert the **settlement panel's arithmetic against the engine's own payouts**.
5. **The browser, driven to a declaration.** `ClickingPlayer` already presses buttons; play a
   hosted table out through the seat interface a person uses, and assert **the board never showed
   the viewer a card they may not see** (the strict `ConcealmentTests` form, over a whole round)
   **and the settlement drawn matches the engine's**.

**Acceptance.**
1. `RuleConformance` runs over hundreds of games at 4, 5 and 6 seats with no violation, **and is
   shown to be non-vacuous** — a deliberately broken engine (a mutant per rule family) must make
   it red. ⚠️ **P13.6 learned this the hard way**: a test that a stood-up seat refuses an answer
   was vacuous until a question was standing in front of it.
2. The coverage test is green, and **its exemption list is short enough to read** and each entry
   says why.
3. A console capture exists that contains a declaration and a settlement, and the driver produces
   it repeatably.
4. The browser plays a round to a declaration in a test, with concealment asserted throughout.

**Done when.** A single `dotnet test` says, mechanically, that the game as played obeys every rule
the document records as Settled — or names the ones it cannot check and why.

⚠️ **What this packet is not.** It is not a rules *review* — it cannot tell you `RULES.md` is
wrong about the game, only that the code and the document agree. **The document's own provenance
tags are the other half of that question and stay a human job.**

---

### P31 — `warden`: the feeding ban as a weapon ☑ done 2026-08-22

**Goal.** The first rung that plays `RULES.md` §5.1 **offensively** rather than merely obeying it,
and a measurement of what that is worth.

**Read first.** `docs/RULES.md` §5.1 in full — especially **Release**; `docs/STRATEGY.md` §§2, 3,
7, 8, 12; `Domain/Play/FeedingBan.cs`; `Domain/Agents/OutsBotAgent.cs`.

🔥 **The gap, stated exactly.** §5.1 reaches the agents as a **legality filter and nothing else**:
`TurnContext.LegalDiscards` is read in exactly two places, `RandomBotAgent` and
`CoverScore.Ranking`, both of which use it to avoid an illegal move. **No rung reads
`MayNotBeFed` to decide anything.** So the strongest rule in the game about *other people's
hands* is, to every player the computer offers, a rule about its own.

🔥 **The play, and it uses only public information.** Taking a card in the open closes that
**rank** against the seat that discards to you, for the rest of the round. The card most often
available to take is **the one your upstream neighbour has just thrown away** — a card they have
told the table they do not want. **Take it, and they may never throw that rank again**; with two
decks in the shoe a second copy is likely, and **a card that cannot be discarded and cannot be
melded is a hand that cannot go out.**

⚠️ **And the release is the interesting half: it is under the *taker's* control.** The ban lifts
only when **the protected player** — the taker — throws that rank back, and then permanently
(§5.1, Exception 1). So the rung that takes for denial must also **not throw the rank it just
locked**, or it pays for a lock and opens it on the same turn. 🔥 **That is the trap to name in
advance: a take chosen for denial and a discard chosen for value will fight each other**, and
`outs` will happily throw back a card it took for a reason `outs` knows nothing about.

**Build.**

1. **`warden` = `outs` with one changed decision — the take.** Take the discard offered when the
   rank it closes is worth more in denial than the card costs in hand value, and **hold that rank
   afterwards rather than releasing it.** ⚠️ **The two clauses are one idea and not two** — "lock
   a rank and keep it locked" — which is what keeps this inside P15's ladder discipline. **If a
   reader disagrees, the fallback is two rungs and this packet says so rather than arguing.**
2. **The denial estimate is public arithmetic**, like `prospector`'s: how many copies of that rank
   are unaccounted for, and whether the upstream seat has shown it wants to shed them. ✅ **Reuse
   `counting`'s supply estimate rather than writing a second one** — P20's memory returned nothing
   as a tie-break, and this is the first decision in the programme where a supply estimate feeds a
   rule that might matter. **A null rung's machinery earning its keep in another rung is a result
   worth recording either way.**
3. 🔥 **The mechanism variable, and P29 is why it is in the build list rather than the notes.**
   Publish **how often a lock actually bit** — per turn, whether the seat's top-ranked discard
   over its *whole hand* differs from its top-ranked discard over its *legal* set. **That is the
   number that separates "the rule did nothing" from "the rung did nothing"**, and nothing
   computes it today.
4. **Regenerate the standing suite at 4 seats.** `StandingAnswerTests` requires every rung in
   `BotCatalog` to be the subject of a published row, so the packet is not green until it runs.
   ⚠️ **This run also lands two corrections P30.2 made to the suite's output** (2026-08-21):
   `claim.permission.money.refuse-over-allow` comes back **paired** — expect the interval to
   widen from the published ±0.18 to ~±0.25 (review R3; the null survives) — and four new
   `money.side-margin.*` rows appear (review R13). **Neither is a regression; both are the
   fixes arriving.** Update `STRATEGY.md` §12's R3 annotation to quote the corrected row.

**Three predictions, written down before the run so the packet can be wrong.**
1. ⚠️ **It is a null or close to one.** Denial has been measured twice and returned nothing both
   times — `cautious` at `−0.2 ± 1.0`, and P16's finding that a weak player *anywhere* is worth
   the same as a weak player upstream. The apparatus floor is about a point (§7).
2. 🔥 **If it is a null, the most likely mechanism is that the lock is cheap to escape**, not that
   denial is worthless: the upstream seat may simply not want to throw that rank, or the floor
   (§5.1) may yield the ban on the turn it would have bitten. **Item 3 above is what tells those
   apart**, and it is the reason to build it even if the rung fails.
3. ⚠️ **`warden` should show a *worse* hand than `outs`** — lower cover-when-losing — because it
   takes cards for a reason other than its own hand. **If it wins while melding no worse, the
   claimed mechanism is not the one operating.**

**Acceptance.**
1. `warden` is in `BotCatalog`, reachable from both front ends, and the suite publishes it.
2. The head-to-head against `outs` is published **with the mechanism variable beside it**, and
   **the entry is written whichever way it comes out** (§8 of `STRATEGY.md`, the P20 discipline).
3. A test shows the lock is actually taken and held — that `warden` closes ranks `outs` does not,
   and does not release them.

**Done when.** `docs/STRATEGY.md` says what playing the feeding ban offensively is worth, with an
interval, and says how often the lock bit.

#### What it came back with — 2026-08-22, Opus 5 (the suite ran overnight from the 21st)

🔥 **Prediction 1 was wrong and it was wrong by nine points.** `warden` is **`−9.3 ± 1.0` against
`outs`** and about six behind `greedy`, `cautious` and `counting`; it beats only `simple`
(`+2.5 ± 1.0`) and `random`. **All six margins survive Holm over a family of twenty-one**, at 8,008
games a cell. It is **the largest separated *loss* this programme has produced**, where the packet
had predicted a null on the strength of denial having measured nothing twice before.

🔥 **Prediction 2 is why that is a finding rather than a shrug, and building item 3 is what earned
it.** The packet said in advance that a null would most likely mean *the lock is cheap to escape*
rather than *denial is worthless*, and named the mechanism variable as the way to tell those apart.
**It closes the escape hatch.** At the crossed table §5.1 had removed a held card from the choice
on **30.5%** of all discards, and on **30.8% of those it changed the seat's answer** — so the rule
takes the card a seat meant to play on **9.4% of every turn**. ⚠️ **The rule is one of the most
active in the game and the rung still loses.** Without this number, `warden`'s loss would have been
readable as *"the ban does nothing"*; it is not.

✅ **Prediction 3 held.** An all-`warden` table runs **31.9 turns a round against `outs`' 24.1**
(`sim bench`) — it is melding worse, exactly as a rung that takes cards for somebody else's reasons
should.

🔥 **The *why*, which is the deliverable (P20's discipline).** `warden` prices a lock in **melded
cards** — it declines any lock it cannot absorb by shedding a partner-less card — and then pays for
every lock it takes with a **draw**, which is the only thing that improves a hand and which nothing
in its rule prices at all. It converts about a third of its draws into locks. ⚠️ **A successor rung
has to price the draw**, and `prospector` shows the shape: `MoneyOdds.PerBlindDraw` prices a draw
in money, and nothing yet prices one in cards. `LiveOuts` is the obvious currency and it is already
in the file.

🔥 **The reproduction is the strongest this project has recorded: 71 of the 88 shared rows came back
byte-identical**, including **every head-to-head cell among the six older rungs, every pairing
ratio, the whole difficulty dial and the whole money sweep**. The 17 that moved are exactly the
rows a seventh rung must move — the free-for-all column, the mean-margin ranking, and the four
ladder-scope statistics computed off the free-for-all cell. ⚠️ **P29 reproduced 4 of 91 and was
right to**; the difference is that P29 ran across a rules change and this did not. **"Does it
reproduce" is a question with an answer again.**

🔥 **The finding nobody planned: "the ladder's last entry is its strongest" was a coincidence
asserted as a law, in three places at once.** For six rungs running, *the last rung named* and *the
strongest rung* were the same rung. `warden` and `prospector` both hang off `outs`, so the ladder
became a **tree** and the coincidence broke:
- `SuiteOptions.MoneyReference` read `BotCatalog.Ladder[^1]` and **would have swept the side bet
  against `warden`** — a rung that had never been ranked above anything. Fixed to
  `BotCatalog.Hardest`.
- `StandingAnswerTests` asserted `Assert.Same(Hardest, Ladder[^1])`. Fixed to assert what was
  meant: the strongest rung is one the ladder tournament ranks.
- `TournamentOptions.NullTestStrategy` takes the last strategy named, so **the null cell changed
  hands from `outs` to `warden` without anybody choosing it**. ✅ **Left alone deliberately** — the
  cell's claim is that *any* strategy against a copy of itself wins 1/n, so a null test that
  depended on who played it would itself be the finding. It holds at both.

⚠️ **The lesson generalises past ladders**: a coincidence that has held for every case so far reads
exactly like an invariant, and the way to tell is to ask what the expression is *for* rather than
whether it is currently true.

⚠️ **Two costs to carry forward.** `sim suite` is **11,020 s (three hours)**, up from 9,981 s — 40%
more head-to-head cells for 10% more wall clock, because `warden` is *cheaper per turn* than `outs`
(**5.6× a `greedy` round against 6.1×**, its candidate set being smaller) even though its rounds
are a third longer. And **`warden` is `Strength: 3`, level with `outs`**, so `BotCatalog.Hardest` is
unchanged, the difficulty dial did not move, and **no front end gained an option** — the same
standing `prospector` has.

✅ **No rules question was raised and `RULES.md` did not move.** `warden` deliberately **declines to
lock jokers**: whether taking a joker closes the other jokers is §9 #27, a `PLAYER` house ruling,
and the one rung whose whole claim rests on the lock must not be built on the one part of §5.1
nobody has confirmed. `JournalHeader.CurrentRulesRevision` is unchanged at 24.

---

### P33 — The clean bonus (§7.3) ☑ — **added, unblocked, rewritten and built 2026-08-22 (Opus 5)**

✅ **DONE.** `RULES.md` §7.3 is built, **§10 #19 is discharged**, and the standing set was
regenerated under it. **The whole packet is below as it was written; what it became follows here.**

🔥 **The result is a reproduction that separates two kinds of change, and it is the cleanest this
document has recorded: 111 of the 116 shared CSV rows came back byte-identical, and the five that
moved are exactly — and only — the five rows denominated in dollars a round.** The prediction was
written down before the run: no rung reads the bonus, so play cannot move and only the money can.
**The file shows precisely that with nothing left over.** ⚠️ **Contrast P29's 4 of 91**: P25–P28
changed what a winning hand *is* and nothing could survive; this changed what winning *pays*.
🔥 **"Does it reproduce" has a third answer now — *it reproduces everywhere the change could not
reach* — which is a sharper instrument than either of the first two.**

🔥 **The five rows are a finding rather than bookkeeping: the clean bonus is a tax on trading wins
for money.** Every one is a `net per round margin` for `prospector`, the one rung that sells rounds
to buy money cards, and every one **fell** — `+5.32 → +4.13` at $5/$20 and `+14.63 → +13.43` at
$5/$40, about **$1.20 a round** at both separated cells, because `outs` wins 20 points more rounds
and so collects the multiplier far more often. ✅ **Both still separate under Holm.** ✅ **And the
`money.side-margin.*` rows did not move at all**, which is the check that the bonus landed in the
round column — **the failure mode this packet had to fix in two places to avoid** (below).

🔥 **The bonus is collected in about one round in six** — 15.4% at the ladder, 16.2% at the dial,
17.4% at the two-arm cell (`docs/STRATEGY.md` §14) — **by rungs that have never heard of the rule.**
⚠️ **Published as a floor and for two independent reasons**, both stated in the document: no rung
will part with a joker (`CoverScore.Potential` returns `int.MaxValue`), and §9 #33's default may
make the bonus unreachable for a hand that has to shed one early.

⚠️ **Three things a later packet needs from this one.**

1. 🔥 **The seam a net delta is split at is a real defect surface, and it bit in two places.**
   `Settlement` pays the round and the side bet as one number; **two consumers re-derive the split
   in order to show it** — the console's settlement panel and `Sim`'s `SeatRow.Flat`/`SideBet`.
   Both computed the round half as `RoundValue`, so **the bonus would have landed silently in the
   side-bet column that every money measurement reads**, and every `money.side-margin.*` row would
   have moved for a reason that has nothing to do with the side bet. ✅ **`Settlement.RoundPayment`
   is public for exactly this** — the split is done once, in the domain, and the consumers ask.
   ⚠️ **The unchanged side-margin rows are the evidence it was done right**, and they are the first
   thing to check if this ever happens again.
2. ⚠️ **The packet did *not* fold P32 in, against this file's own recommendation, and the reason is
   attribution** — the same argument this plan used to put P31 before P32. A run that changed the
   scoring rule *and* the table size could not have said which moved a number, and the whole result
   above is a statement about which rows a change can reach. **It cost a second three-hour suite and
   bought the cleanest reproduction in the project.** ✅ **P32 now starts from a complete, current
   four-handed baseline** and its own decision (full crossing or a stated subsample) is untouched.
3. ⚠️ **No rung plays for the bonus and that was deliberate** (build item 3). A rung that sheds a
   joker for the multiplier is a **new rung** under P15's discipline — measured before it joins the
   ladder — not a change to `outs`. 🔥 **The arithmetic says it is worth trying**: at four seats the
   bonus pays **+$5 a head** against a whole round's flat prize of $5 a head, so turning one round
   in six into one in three is worth about a sixth of a round's prize every round — **well inside
   what the apparatus resolves** (`STRATEGY.md` §7).

✅ **What was built, precisely.** `TableRules.JokerlessMultiplier` (the §7.3 table as data, beside
§7.1.1's, in the one place a per-seat-count rule is written down); `Settlement.IsJokerless` (the
predicate — a scan of the declared thirteen, and **`Meld.IsClean` is not consulted anywhere**);
`Settlement.RoundPayment`; `Settlement.ForRound` takes the winner's declared thirteen and
`RoundEngine` hands it that seat's hand after the discard; both front ends say when the bonus was
paid; `RuleConformance` re-derives it from the seat count and the melds laid down, with a mutant
catching a clean hand paid flat, a jokered one paid the bonus, **and five seats paid at the
four-seat multiplier**; and §7.3's registry entry in `SettledRuleCoverageTests` is
`Checked(...)` rather than `Exempt(...)`.

⚠️ **Six tests changed their expected payouts and none of them was wrong before** — every
hand-computed four-handed settlement in the suite was a jokerless declaration, so all six doubled.
🔥 **That is itself a measurement of a kind**: the scripted hands this project reaches for when it
wants a winning thirteen are clean ones, which is a hint that the 15.4% floor above understates how
often a *player* would get there.

**The packet as written follows.**

✅ **UNBLOCKED 2026-08-22 by `RULES.md` rev 26.** The expert corrected her own rule unprompted and
closed §9 **#34 and #35** — #35 being the row with no safe default that stopped this packet.
⚠️ **The goal below changed with it, in three ways, and the packet got *smaller*.**

**Goal.** Build `RULES.md` §7.3 — **a jokerless declaration pays ×2 at two, three or four seats and
×3 at five or more** — and re-measure under it.

**Read first.** `docs/RULES.md` §7.3, §7.2, §7.1.1, §10 #19 and §9 #33, #36, #37;
`docs/QUESTIONS-FOR-MYA-LAY.md` Q10 **and Q11**; `Domain/Money/Settlement.cs`;
`Domain/Melds/TableRules.cs`.

🔥 **Why it goes before P32.** §9 **#35** asks whether the bonus exists at five or more seats, where
§7.1.1 requires **no series at all** — so a five-handed re-measurement taken before it is answered
is a measurement of a game whose scoring is unknown. **It also invalidates every figure in
`docs/STRATEGY.md` more thoroughly than P25–P28 did**: those changed what a winning hand *is*, this
changes what winning is *worth*, and it hands every rung a reason to throw a joker away that none
of them currently has.

✅ **Two rows are still open and neither blocks the build**: **#36** (does the multiplier reach the
money settlement — recommend **not**, now backed by three separate sayings that name *the winning
prize* and never the money) and **#37** (is six-plus also ×3 — recommend **yes**, matching
§7.1.1's own five-or-more grouping). **Proceed on both defaults and say so in the report.**
⚠️ **#33 stays open too** — whether the joker may be shed before the declaring discard — and
**it has a safe default that changes nothing**: §5.1 exception 2 as it stands. ⚠️ **Note what that
default means for the measurement**: if the ban only yields on the declaring discard, a hand
needing to shed a joker a turn early cannot reach the bonus at all, so **any measured bonus rate is
a floor, not an estimate.** Say that in `STRATEGY.md` rather than leaving it to be rediscovered.

**Build.**

1. **`Settlement` stops paying a constant, and stops being seat-count-independent.** §7.2 step 1 is
   the round value from every loser; it becomes the round value **×2 or ×3** when the declared hand
   is jokerless. 🔥 **The multiplier is a function of the table size**, so it belongs beside
   §7.1.1's table in **`Domain/Melds/TableRules.cs`** — the one place a per-seat-count rule is
   written down — rather than as a constant in `Settlement`. ⚠️ **`Settlement` does not take a seat
   count today.**
2. 🔥 **The qualifying test is a property of the *hand*, not of the partition — and that dissolves
   the hardest thing this packet used to contain.** Rev 25 read the condition as being about series
   and flagged that a hand can partition more than one way, so the winner might be paid on the best
   partition or on the one the engine found. **Jokerless is jokerless under every partition**, so
   the question does not arise, `HandEvaluator` needs nothing, and `Meld.IsClean` is not the
   predicate. **The test is *no joker in the declared thirteen*.**
   ⚠️ **Do not reach for `Meld.IsClean` out of habit** — it implements §7.1.1's *required clean
   series*, which is a different rule that happens to share a word.
3. **The rungs get a reason to shed a joker**, which is the behaviour the expert named and which no
   rung can currently produce — `CoverScore.Potential` returns `int.MaxValue` for a joker.
   ⚠️ **Keep it out of the ladder unless it is measured**: a rung that plays for the bonus is a new
   rung under P15's discipline, not a change to `outs`.
4. **Regenerate the standing suite.** Three hours at four seats (P31's measured price).
   🔥 **Consider doing it once, at five seats, and folding P32 in** — see the note under P32.

**Acceptance.**
1. A jokerless declaration pays **×2** at four seats and **×3** at five, and a declaration holding
   a joker **anywhere — including in a set** — pays flat at both. Asserted against `Settlement`
   directly and through a played round.
2. **A hand whose jokers sit only in sets is the discriminating case** and gets its own test: it
   would qualify under rev 25's withdrawn reading and does not qualify under rev 26.
3. **A five-handed hand of four sets and no series at all, jokerless, pays ×3** — §9 #35's answer,
   which is what a hand-wide predicate buys and a series-wide one cannot express.
4. `RuleConformance` re-derives the bonus independently, and §7.3's registry entry in
   `SettledRuleCoverageTests` **turns from `Exempt(...)` into `Checked(...)`**. 🔥 **That entry
   already exists**: §7.3's heading claimed Settled at rev 26, the coverage test went red the same
   hour, and the exemption written to silence it is **the only one in the registry excused because
   *the code is missing*** rather than because no ordinary-play check could exist. ⚠️ **Convert it,
   do not delete it** — deleting it makes the alarm silent instead of answered.
5. `docs/STRATEGY.md` says what the bonus did to the ladder, **and states the #33 floor** (point
   above).

**Done when.** §10 #19 is discharged and `RULES.md` again records nothing Settled that nothing
implements.

---

### P32 — Five-handed is the default table ☑ — **done 2026-08-22 (Opus 5)**

> ✅ **Shipped.** `RoundEngine.DefaultPlayers = 5` is the one place the default table is written
> down; `SuiteOptions.DefaultSeats`, every `--seats` default in `Sim` and the browser lobby all
> read it. `sim bench` gained `--seats` (it had none, so a five-handed round could not be priced).
>
> 🔥 **The headline is a negative result about this project's own explanation.** P29 attributed the
> four-handed levelling of `simple` to §7.1.1's joker-free series requirement; at five seats that
> requirement is gone and **the gap did not re-open**. ⚠️ **The raw margins would have said it
> narrowed and that is also wrong** — a fifth seat rescales *every* margin by 0.800. **Median ratio
> over eighteen cells: 0.801**, the six `random` rows on 0.800 to three digits, and `simple`'s gaps
> to `greedy` and `counting` within a tenth of a point of pure scale. **Every Holm verdict is
> identical at both sizes.** ⚠️ **What causes the levelling is now unknown.**
>
> ✅ **Four of five written-down predictions held**: jokerless rate fell 15.4% → **12.1%**, the
> bonus's value rose +$15 → **+$40**, value beat rate (**$2.31 → $4.84** a round), the null cell
> read 20%. ❌ **The dial prediction was wrong and usefully so** — the steps did not compress and
> **no ε moved**, so ε is close to a property of *the mistake* rather than of the rung **or the
> table**.
>
> ✅ **All three open decisions were taken on measurement.** *Full crossing* — ⚠️ **this entry's own
> fear that the free-for-all would be "plausibly the majority of the run" was wrong**: a full pass
> at four seats is 2,401 games in **51 s**, so the five-handed pass is ~7 min of a 12,445 s suite;
> `SeatingPlan.MaximumAssignments` went 4,096 → 32,768. *One dial* — separated at four, five and
> six (`docs/strategy/dial-away-from-the-default-table.md`). *Four-handed kept* — frozen at
> `docs/strategy/measurements-4-handed.csv` and fenced by a test.
>
> ⚠️ **One leftover, deliberately**: `BurmesePoker.Console`'s seat prompt still defaults to
> `MinimumPlayers`. Changing it invalidates `drive-console.py`'s captures and was not in the build
> list. **One line plus a re-capture.**

**The record of what it was built from follows.**

⚠️ **The recommendation to fold this packet into P33 was not taken, and the amendment is here so
that nobody re-litigates it.** Rev 26 observed that at five seats the bonus is the only thing
cleanliness is ever worth, and concluded that measuring the five-handed game and measuring the
bonus are one measurement. **P33 ran its regeneration at four seats anyway**, on this plan's own
P31-before-P32 argument: *a run that changes two things at once cannot say which moved a number.*

🔥 **The choice paid for itself.** P33's four-handed run reproduced **111 of 116 rows byte-identical
and moved exactly the five denominated in dollars a round** — which is a clean statement that the
bonus reaches the money and nothing else, and it would have been unrecoverable from a run that had
also changed the table size. ✅ **So this packet inherits a complete, current four-handed baseline**,
and every figure it produces at five seats is comparable with one measured under the same rules.
⚠️ **The cost was a second three-hour suite, and it is spent.** Do not spend a third: **P32 is one
regeneration, and there is nothing left to fold into it.**

🔥 **What P33 leaves this packet to look for, and it is now a *prediction* rather than a hope.**
§7.1.1 requires a joker-free series at four seats and **nothing at all** at five, while §7.3 pays
**×2** at four and **×3** at five. So the two rules pull in opposite directions across the seam:
- **The jokerless rate should *fall*** at five seats — a four-handed hand must already carry one
  clean run, and at five nothing pushes it that way. `STRATEGY.md` §14's 15.4% is the four-handed
  figure to beat.
- **What the bonus is *worth* should rise** — ×3 rather than ×2, and paid to more losers.
- ⚠️ **Which of those wins is unmeasured**, and it is the single most interesting number this
  packet can produce. **Write the prediction down before the run**, as P29 and P31 both did.

**P33's other bequest**: the `bonus.jokerless-rate.*` rows and
`StandingAnswerTests.TheDocumentSaysHowOftenTheCleanBonusIsActuallyCollected` exist already, so the
five-handed rate arrives in the file without any new harness work.

**Goal.** Move the whole standing set to **five seats**, which is the size this game is actually
played at, and re-fit the difficulty dial there.

**Read first.** `docs/STRATEGY.md` §§1, 3, 4, 9, 11; `docs/RULES.md` §7.1.1 and §2.1;
`Sim/SeatingPlan.cs`; `Tests/Sim/StandingAnswerTests.cs`.

⚠️ **Everything published is four-handed, and four is the *minimum*, not the default.**
`RoundEngine.MinimumPlayers` is 4 and `MaximumPlayers` is 6, so **five- and six-handed are legal,
playable and entirely unmeasured** — and by §7.1.1 they are a *different game*: at five or more a
declared hand needs **no series at all**, where four-handed needs one joker-free run. **The rule
the whole of P29's headline turned on does not apply at the default table size.**

**Build.**

1. **`SuiteOptions.Seats` becomes an explicit 5**, not `RoundEngine.MinimumPlayers` — the default
   table size is a decision, and spelling it as "the minimum" is how it came to be four by
   accident.
2. **`sim bench` gains `--seats`.** It hard-codes four (`Program.cs`, the bench block), so **there
   is no way to price a five-handed round today** — and pricing it is step one.
3. **Re-fit the difficulty dial at five seats.** ⚠️ **This is the risk that could end the packet
   red.** A five-handed table's base win rate is 20%, not 25%, so **every level's reference figure
   falls and the steps compress** — and `sim suite` exits non-zero if the dial stops being
   separated. **Re-sweep ε first** (P19's seven probes, P23's method) and budget the re-fit.
4. **Keep the four-handed headline row and add a five-handed one.** P12's `greedy`-vs-`simple` at
   four seats is this project's longest-running measurement; **losing the continuity to gain the
   default is a bad trade when both fit in one run.**
5. **Re-measure the money sweep at five seats**, where `RULES.md` §4.3's own worked example
   already lives.

⚠️ **Two decisions this packet needs and cannot take for itself.**
- 🔥 **One dial or one dial per table size?** `DifficultyLadder` has no table-size parameter, and
  a level ought to mean the same thing wherever you sit. **Recommend: one set of ε values, fitted
  at five, checked monotone at four and six** — and if it is not monotone at all three, *that* is
  the finding and the decision comes back. **A per-size dial is four calibrations to keep true and
  four ways for the menu to lie.**
- ⚠️ **Does the four-handed data stay published?** Recommend **yes, as a named second table**
  rather than deleted — §7.1.1 makes four-handed a genuinely different game, and P29's whole
  finding was about the four-handed series requirement.

⚠️ **The wall clock, and there is a blow-up hiding in it.** Head-to-head cells barely move — 30
assignments at five seats against 14 at four, so ~8,010 games against 8,008. **The ladder
free-for-all is the problem**: `Balanced(k strategies, 5 seats)` is `k⁵` assignments against `k⁴`,
and rounding up to whole passes makes that cell the whole budget on its own. ⚠️ **Budget the
free-for-all separately from everything else, measure it with `sim bench --seats 5` first, and say
in the packet what was dropped** if anything. **Do not discover this at hour four.**

🔥 **P31 made this materially worse and the packet must be re-costed before it is started**
(amended 2026-08-21). `warden` is a seventh rung ranked on win rate, so:

- **The round-robin is 21 head-to-head cells, not 15** — a 40% rise in the part of the suite that
  was already the largest. P31's own regeneration is the measured price of that at four seats;
  quote its elapsed time from `STATUS.md` rather than P29's.
- 🔥 **The free-for-all goes from `6⁵ = 7,776` assignments to `7⁵ = 16,807`** — so that one cell is
  **16,807 games** rather than 15,552 at six rungs, or 9,072 at four seats today. It is now
  plausibly *the majority of the run*.
- ⚠️ **`warden` also makes rounds longer**, which multiplies through everything: `sim bench` at
  P31 put an all-`warden` table at **31.9 turns a round against `outs`' 24.1**, because a rung
  that spends draws on locks converges more slowly. A five-handed round is longer again.

⚠️ **So P32 has a decision to take that P31 did not**: whether the five-handed free-for-all is run
at the full crossing or at a stated subsample. **Either is fine and neither may be silent** — see
`STRATEGY.md` §11's "no silent caps" standing, and P29's own remark that a number without its
denominator is not a measurement.

**Acceptance.**
1. `measurements.csv` regenerates at five seats from one command, and `StandingAnswerTests` is
   green.
2. The dial is separated at five seats, and its monotonicity at four and six is **stated** —
   whether or not it holds.
3. `docs/STRATEGY.md` says which of its findings are properties of the *game* and which were
   properties of *four seats*. 🔥 **P29's headline is the one to check first**: the four-handed
   levelling of `simple` was attributed to the joker-free series requirement, and **at five seats
   that requirement is gone — so if the levelling goes with it, P29's explanation is confirmed;
   if it does not, P29's explanation is wrong.** ⚠️ **That is a prediction and it should be
   written down before the run.**

**Done when.** The standing answer is about the table this game is played at, and the four-handed
figures are kept beside it as the different game they measure.

---

### P37 — Asking the table to change seats ☑ — **added and built 2026-08-22 (Opus 5)**

**Goal.** `RULES.md` §3 step 2's other half, and §10 **#23**: a held seating can be **re-drawn when
the players agree to it** (§9 #45, `PLAYER`, Nick 2026-08-22).

**Read first.** `RULES.md` **§3**, **§9 #45 and #47**, **§10 #23**; `BUILD-PLAN` **§3.5**
(the engine asks for a move and must never be able to ask for a ranking), **§3.10** and **§3.11**.
In the tree: `Server/SeatPrompt.cs`, `Server/TableSession.cs`, `Server/HostedTable.cs`,
`Domain/Abstractions/IPlayerAgent.cs`, `Domain/Play/JournalFormat.cs`,
`Web/Components/Table/TurnPrompt.razor`, `Tests/Server/ConcealmentTests.cs`.

**Depends on.** ✅ **P36, shipped 2026-08-22** — there was nothing to change until a seating was
held.

🔥 **Amended 2026-08-22 by P36, and it names the one design question P36's shape forces.**
`MatchEngine.SeatingPolicy` is a **get-only** property and `SeatingPolicy.ReseatsBefore(int)` is a
**pure function of the round count** — deliberately, because a policy that could be talked to would
have answered §9 #45 by accident. ⚠️ **So a table agreeing to move is not expressible as a policy**,
and P37 has three shapes to choose between:

1. **A settable policy.** Cheapest and worst: *agreed once* is not *every N rounds*, and the type
   would then mean two things.
2. 🔥 **An explicit `MatchEngine.Reseat()`, called between rounds by whatever ran the asking.**
   ✅ **Recommended.** An agreement is an **event**, not a rule about round counts; this leaves
   `ReseatsBefore` a pure function, leaves the default *held*, and keeps
   `LayeringTests.NothingOutsideTheSeatingPolicyDecidesWhenTheSeatsAreDrawnAgain` true —
   `Reseat()` is the engine doing the drawing, not a second place deciding *when*.
3. **A policy that takes a callback.** Puts the question inside the engine's own loop, which is
   §3.5's line (the engine asks for a move; who is asked is the host's business).

⚠️ **The journal cannot record it as a number.** P36's header field is `seating_rounds`, and an
agreed re-draw is not a rounds-between value — so P37 needs a **decision kind**, which is exactly
where P28's mistranslation lived (`JournalFormat.Name`'s default arm wrote a fifth question to file
as a *declaration*, and only the round-trip test could see it). ⚠️ **And a replay must re-seat at
the same round**, which a policy alone will not say: the decision line is what carries it.

⚠️ **Amended 2026-08-22 by P24.2, and it tightens the constraint below rather than adding one.**
`SeatPrompt` now carries an `AdviceRationale`, and
`ConcealmentTests.NoTableEventCanCarryTheComputersReasoning` asserts over the **type** that no
`TableEvent` may — a rationale names cards of a hand and says what the computer would keep, so an
event carrying one hands every watcher a commentary on somebody else's thirteen (§3.11 A1).
🔥 **So the two places a question could have lived are now both spoken for in opposite
directions**: a `SeatPrompt` is private by construction and a `TableEvent` is public by
construction and may not carry reasoning. **Decide where a public question lives before writing a
line of it**, and say why in the packet.
⚠️ **A sixth `SeatQuestion` now fails quietly in two more places** — `RemotePlayerAgent.Why`'s null
arm and `TurnPrompt.razor`'s inert final arm. Both are `JournalFormat.Name`'s lesson applied (*a
default arm is a mistranslation waiting for the next case*), but a question drawn as nothing and
explained as nothing is still a seat with no way to answer. **Visit both.**
⚠️ **And if the new question is one a person can be advised on, it needs a `Why` arm** — every
other question has one, and the one that does not would read as the place somebody forgot.

🔥 **This packet's difficulty is a shape, not a feature: it is the first *public* question this
project has ever asked.** Every question the engine asks is put to **one seat** and answered
privately — `SeatPrompt` is seat-private by construction, and `ConcealmentTests` sweeps every event
against what each viewer may see (P13.2: *exactly one event in the whole narration is private, so
the security boundary is one `if`*). **"Shall we change seats?" is put to everybody at once and
every answer is public.** ⚠️ **Do not reach for `SeatPrompt` and widen it** — a public question
wearing a private question's type is how a concealment boundary stops meaning anything.

⚠️ **And it is asked *between* rounds, which is a place no question is asked from.** All five
existing questions belong to a turn. This one belongs to the gap `HostedTable` already has
(`BetweenRounds`, 12 s by default) — so **the patience clock, the stand-in bot and the
handed-over-seat rules all apply to it and none of them was written with it in mind.**

#### The decision this packet takes, and it is a design decision rather than a rules one

🔥 **A computer seat consents.** A rung decides about cards; *"shall we move seats"* is not a card
decision, and **a bot that abstained would make the rule dead at every table with a computer in
it** — which at a solo table is every table. ⚠️ **A rung answering it on some invented basis would
be a strategy claim nobody measured**, which P15's discipline forbids. ✅ **`RULES.md` §3 says *the
players* agree, and a bot is not a player in the sense the rule is about** — so this belongs in
`BUILD-PLAN` §3 as a design decision, and **no rule is invented in `RULES.md` to justify it.**

⚠️ **§9 #47 is open — does *agree* mean everybody, or most of them?** Recommended **unanimous among
the people**, because a majority vote moves somebody's seat against their will in a game with money
on it. **Build the default, fence it with a test named for the question**, and say so in the report
— P26's and P33's idiom.

#### Build

1. **A public question**, and a type of its own rather than a widened `SeatPrompt`. It is asked of
   every seat, its answers are public, and ⚠️ **`ConcealmentTests` must be extended to say so
   deliberately** rather than passing because nothing new was added to the bus.
2. **A sixth `IPlayerAgent` question**, defaulting to consent for every rung — **one
   implementation on the base and none per rung**, so a new rung cannot forget it and cannot
   accidentally make a strategy claim by answering it.
3. ⚠️ **The journal.** A new decision kind, and 🔥 **`JournalFormat.Name`'s default arm is exactly
   where P28's mistranslation lived** — a fifth question was written to file as a *declaration*,
   and **only the file round-trip test could see it**. **Write the round-trip test first.**
4. **Both front ends.** The browser's between-rounds gap is where it goes; the console gets the
   same question in its own idiom. ⚠️ **`drive-console.py` will need a re-capture** and the driver
   answers prompts adaptively (P30.2), so a new prompt is a driver change too.
5. **What happens when nobody answers.** The patience clock ends a table nobody is at (P13.2); an
   unanswered re-seat question must **fail closed — the seats do not change** — because the rule is
   that people agree, and silence is not agreement.

#### Acceptance

1. A table of people can agree to re-seat between rounds, and the next deal uses the new seating.
2. `RULES.md` §10 **#23 is discharged**, and §3 step 2 is checked by `RuleConformance` in both
   halves: **held by default, changed only by agreement.**
3. 🔥 **The question is public and asserted to be public** — `ConcealmentTests` has a case for it,
   rather than being silent about it.
4. A journal round-trips a re-seating agreement, and **replay reproduces the new seating** — the
   test written before the serializer.
5. **A solo table with four bots can re-seat**, which is the case the consent decision exists for,
   and it is asserted.
6. §9 #47's default is **named in the report and fenced by a test named for the question.**
7. **No figure in `docs/strategy/measurements.csv` moves** — `RoundsPerGame = 1` makes it
   impossible, and a moved row means the question leaked into the harness.

**Done when.** The table stays as it is until the people at it decide otherwise, and the deciding
is something they do together.

---

#### ✅ What it built (2026-08-22, Opus 5)

**The sixth question is `IPlayerAgent.AskAboutTheSeating`, it returns a `SeatingOpinion`, and the
rule is one `Ask` and no `Refuse`.** The three shapes this entry offered are all declined in
favour of a fourth: **the engine asks every seat between rounds**, in `MatchEngine.NextSeating`,
beside the policy P36 put there — so the agreement is asked first and the policy is not asked on
top of it. ✅ **That is what makes it replayable**: `JournalingAgent` records it, `JournalPlayerAgent`
answers it, and `GameRunner.Replay` needed no new driving path at all. **`Reseat()` was not built**
— an explicit method the host calls would have left the harness and the replay unable to see the
agreement, which is acceptance 4.

🔥 **Consent is not desire, and that is the finding.** The entry's own framing — *a computer seat
consents* — is only half a design until you notice that a **yes-or-no** question cannot carry it:
a consenting bot answering *yes* re-seats an all-bot table every deal. **Three answers**, with
`Consent` the default and a no-op, is what makes the packet's build item 5 (*fail closed*)
disappear as a problem: silence, an unattended seat and every bot in the game all consent, and
consent moves nothing. **No clock, no timeout, no special case.**

⚠️ **A public question is a standing answer, not a pending prompt** — recorded as a design decision
in §3.13. Blocking would have cost one patience per seat to settle one question, so a person says
what they think whenever they like and it stands on the seat's `SeatChannel` until the engine asks,
which **consumes** it. One press moves the seats once, asserted at the browser and at the server.

✅ **Acceptance 3 is a type assertion and a broadcast assertion**, not a silence:
`ConcealmentTests.TheSeatingConversationIsPublicAndCarriesNoHand` shows the three seating events
carry no card, hand or rationale, and that a watcher who holds no seat hears every word of the
conversation. ⚠️ **A superseded connection may not say anything either** (review R8) — the public
question is the one thing a dead connection could otherwise have said out loud in somebody else's
name, so it follows `Answer`'s rule and not the fan-out's.

🔥 **The trap the packet did not name, and it is the one that would have shipped quietly.** This is
the **first member of `IPlayerAgent` with a default implementation**, so a decorator that does not
override it does not fail to compile and does not throw — it answers *consent* in its own name and
drops what it wraps. For `JournalingAgent` that is a re-seating that never reaches the file; for
`JournalPlayerAgent` a replay that quietly deals to different seats. Six decorators needed it, and
`SeatingAgreementTests.EveryDecoratorForwardsTheSeatingQuestion` finds them **by type** — anything
taking an `IPlayerAgent` in its constructor — so the next one is covered without anybody
remembering.

⚠️ **`JournalPlayerAgent` peeks rather than consuming, and that is a deliberate narrowing of
*divergence is loud, always*.** Every journal written before P37 has no seating decisions, and
absence has to mean `Consent` or no old journal replays. The narrowing is safe precisely because
consent changes nothing: the only journal this can answer silently is one where nothing happened
to record.

✅ **Two leftovers were taken with it, both in this packet's own subject.** The console's
round-start line still said *"the seats are re-drawn every round"* — **P36 missed it**, and it was
a false sentence in the product for a day. And `AboutTable` now says what the seats are doing,
which no view had ever said (P36's recorded leftover).

⚠️ **The console capture changed**, and the driver did not: a new `SelectionPrompt<SeatingOpinion>`
between rounds is answered by `drive-console.py`'s generic ENTER arm, which takes the highlighted
first option — *leave them*, which is the rule. ⚠️ **It is only visible in a two-round capture**;
the standing one-round capture never reaches the question at all.

---

### P36 — How long a seating holds ☑ — **added and built 2026-08-22 (Opus 5)**

**Goal.** `RULES.md` §3 step 2, as rev 28 corrects it: a seating is **drawn once and held** rather
than re-drawn as a step of the deal. ⚠️ **Changing it is P37's** — this packet stops the engine
contradicting the document and builds the policy P37 needs.

**Read first.** `RULES.md` **§3** (the whole of it, including the withdrawn paragraph), **§7.5**,
**§9 #45 and #46**, **§10 #22**. In the tree: `Domain/Play/MatchEngine.cs`,
`Server/HostedTable.cs`, `Server/TableSession.cs`, `Web/TablePlan.cs`, `Web/Components/Table/`,
`Console/Program.cs`.

**Depends on.** Nothing. ⚠️ **P35 depends on *this*** — §7.5 blames the seat above the winner for a
three-round streak, and that is only a coherent sentence once a seating survives three rounds.

🔥 **This is a revert-shaped packet that must not be a revert.** Before P28 a seating was drawn
once and held for a whole match; P28 replaced that with a fresh draw before every deal, on rev 19's
reading; rev 28 withdraws that reading. ⚠️ **The pre-P28 behaviour is *nearly* right and is not
right** — it has no way to re-seat at all, and the rule is precisely that you can. **`git revert`
is the wrong instinct here and the diff will look like it is the right one.**

✅ **No published measurement moves, and the argument is P28's own, run backwards.** Every
experiment in `BurmesePoker.Sim` runs `RoundsPerGame = 1`, so there is never a second round for a
re-draw to precede. **This packet needs no re-measurement**, which is the whole reason it is an M
and P35 is an L.

---

#### ✅ Split on 2026-08-22, the day it was written — P36 holds the seating, **P37** is the agreeing

⚠️ **§9 #45 was ruled while this packet was being written, and it made the packet bigger.** Nick:
a re-seating happens **"when people agree to do it"** — not when one player asks. 🔥 **That reverses
this entry's own recommendation.** It had argued for *one asking is enough* precisely because it
invents no machinery; **agreement is the expensive reading and it is the one taken**, by the person
whose game it is. `PLAYER`, so `EXPERT` may still overturn it.

**So the work is two packets, and the seam between them is the one this entry already identified:**
*when a re-draw happens* is a **policy**. **P36 builds the policy and the held seating** — which is
what §10 #22 actually asks for and needs no agreement machinery at all. **P37 puts the table's
agreement behind it** (§10 #23).

| | **P36 — hold the seating** | **P37 — ask the table** |
|---|---|---|
| **Discharges** | §10 **#22** — the engine stops contradicting §3 | §10 **#23** — a held seating can be changed |
| **Domain** | `MatchEngine` re-draws on a **policy** instead of unconditionally — one field, one condition | the policy's answer comes from the table |
| **Server** | nothing | 🔥 **a public, table-level question, which does not exist.** `SeatPrompt` is **seat-private by construction** (P13.2: exactly one event in the whole narration is private, and `ConcealmentTests` sweeps for it). *"Shall we change seats"* is asked of **everybody at once** — the first of its kind |
| **Agents** | nothing | ⚠️ **a sixth `IPlayerAgent` question that no rung can answer.** Every rung decides about cards; this is not a card decision |
| **Front ends** | one lobby field, one console prompt | a between-rounds flow in both, a timeout, and what happens when a seat has stopped answering (`HostedTable`'s patience clock) |
| **Journal** | one header field | a new decision kind — ⚠️ and `JournalFormat.Name`'s default arm is **exactly where P28's mistranslation lived**: a fifth question was written to file as a *declaration*, and only the round-trip test could see it |
| **Sim** | invisible (`RoundsPerGame = 1`) | invisible |
| **Size** | **S–M** | **M** |

🔥 **The design question P37 cannot avoid, stated here so it is not discovered late: what does a
computer seat do when the table is asked to agree?** A bot cannot meaningfully want this either
way, and **a bot that abstained would make the rule dead at every table with a computer in it** —
which, at a solo table, is every table. ⚠️ **A rung answering it arbitrarily would be a strategy
claim nobody measured** (P15's discipline). **The honest shape is that a computer seat consents:
the rule is about people, and a bot is not one.** ✅ **That is a design decision and not a rules
decision** — `RULES.md` §3 says *the players agree*, and a bot is not a player in the sense the rule
is about. **P37 records it in `BUILD-PLAN` §3 rather than inventing a rule in `RULES.md`.**

⚠️ **What P36 must not do is call its setting the rule.** §3 says *when the players agree*, and a
number fixed when the table opens is **not** people agreeing. **The policy is the mechanism; P37 is
the rule.** ✅ **But P36 is independently correct and worth shipping alone**: a table that re-seats
every *N* rounds with *N* chosen at the start is a legitimate house arrangement, is what a solo
player against bots actually wants, and is the only shape that works before P37 exists.

#### Build

1. **A seating policy on the match.** `MatchEngine` stops re-drawing unconditionally. The default
   is **hold** — drawn once, kept — because that is the rule; a `RoundsBetweenSeatings` of *N*
   re-draws every *N* rounds and **0 or absent means never**. ⚠️ **One condition in one place**;
   the temptation is a bool beside a number and that is two states too many.
2. ⚠️ **The seed story must be stated.** P28 recorded that *"a seed from before P28 no longer plays
   the same match — the seating draw takes numbers the deal used to take."* **This packet moves
   those numbers again**, so a journal or seed from between P28 and P36 replays differently.
   `JournalHeader.CurrentRulesRevision` is **28** already, which is what makes that detectable
   rather than mysterious — **say it in the report and check `JournalReplayTests`.**
3. **Both front ends offer the setting**, in the shape they already offer difficulty (P18/P19: one
   list, resolved through the domain, never re-typed). ⚠️ **The console's capture changes** —
   `drive-console.py` prints the seating line — so **re-capture and say so**.
4. 🔥 **The browser is where this is visible and it is an improvement.** P13.5 puts *you at the
   front whichever seat you were dealt*, so today the table visibly rearranges itself around a
   fixed viewer every deal — something `TableRing` was never asked to do and which P28's own entry
   flagged as a UX question it had not answered. **Holding the seating removes that**, and the
   packet should check it rather than assume it.
5. **Leave the *changing* to P37, and fence it.** §9 #45 is ruled (`PLAYER`: the players agree)
   and §9 #47 is open (everybody, or most). A test named for each, so neither can be quietly
   assumed by a policy that only counts rounds.

#### Acceptance

1. A match of *n* rounds deals **one** seating unless the policy says otherwise, and `RULES.md` §10
   **#22 is discharged**.
2. The policy is **one decision in one place**, asserted — no front end and no harness re-derives
   *when a re-draw happens*.
3. ⚠️ **`RuleConformance` checks §3 step 2 as corrected** — it re-derives every Settled rule, and
   `SettledRuleCoverageTests` fails the build on a Settled section nothing checks. **§3 is Settled
   and has just changed**, so this is the packet's own alarm.
4. **No figure in `docs/strategy/measurements.csv` moves**, and that is asserted rather than hoped:
   `RoundsPerGame = 1` makes it impossible, and a moved row means the policy leaked into a place it
   has no business being.
5. A journal from a held-seating match replays byte-identically, and the report says plainly that
   seeds from between P28 and P36 do not.

**Done when.** You sit down, the table stays as it is, and changing it is something somebody does
rather than something that happens to you.

---

#### ✅ What it built (2026-08-22, Opus 5)

**`Domain/Play/SeatingPolicy.cs` is *when a re-draw happens*, and it is one condition in one
place.** `ReseatsBefore(roundsPlayed)` is the whole of the decision; `MatchEngine` is its only
caller; `SeatingPolicy.Held` is the default and the rule. ⚠️ **Zero rounds between seatings *is*
"never"** — the flag-beside-a-number the packet warned about was not built, and
`ZeroOrAbsentRoundsBetweenSeatingsMeansNever` says so.

🔥 **Acceptance 2 is a source scan in the P18/P19 idiom**, and it is the part worth keeping:
`LayeringTests.NothingOutsideTheSeatingPolicyDecidesWhenTheSeatsAreDrawnAgain` bans *asking* the
question outside the engine **and** bans doing arithmetic on `RoundsBetweenSeatings` outside the
policy. ✅ **A front end may carry a policy and hand it over; it may not reason about one** — which
is what keeps P37's agreeing a change in one file rather than in four. ⚠️ **It caught a real
second copy while it was being written**: `JournalFormat` was deciding what 0 meant in order to
omit the field, and now asks `header.Seating != SeatingPolicy.Default` instead.

✅ **The journal's field is written only when the seating was not held**, so **every journal in
existence is byte-identical** and *absence means the rule*. ⚠️ **The one journal that cannot say
what it did is one written between P28 and P36**: it carries no field, reads back as held, and
replays differently — `CurrentRulesRevision` 28 is what makes that detectable rather than
mysterious. `GameRunner.Replay` reads `header.Seating` rather than this build's default.

⚠️ **Build item 4 was right and it cost a test.** `SeatBoardTests`'s fixture played **three**
rounds and `EverySeatIsAskedEveryQuestionOverAMatch` went red: holding the seating makes the same
seed deal a different match (the draw the seating took is not taken any more), and the fifth
question — the claim's permission — needs a claim *and* the seat above holding the rank. It turns
up again at **five** rounds. 🔥 **That is the assertion doing its job**, and it is the second time
this project has watched a seed stop meaning what it meant (§3.9 point 2).

✅ **The browser really does stop rearranging itself**, checked rather than assumed:
`TableJournalTests.AHostedTableHoldsItsSeatingAndWritesTheDownPolicyDown` compares the deal order
of two rounds, and the sibling test asks a table to re-seat and watches it move.

⚠️ **The console's capture changed by exactly the new prompt and one sentence** — a `<details>`-free
16-line diff against `HEAD` at `--seed 20260819 --pick 0`, everything from the deal on identical.
The prompt is a `SelectionPrompt<SeatingPolicy>` over `SeatingPolicy.Offered`, which
`drive-console.py`'s generic ENTER arm answers without a script change.

---

### P35 — The two scoring rules that reach outside a round ☑ — **added and built 2026-08-22 (Opus 5)**

**Goal.** Build `RULES.md` **§7.4** (a win from the initial deal pays ×2) and **§7.5** (a third
consecutive win is paid entirely by the seat above the winner), and re-measure underneath them.

**Read first.** `RULES.md` **§7.2, §7.3, §7.4, §7.5**, **§3**, and **§9 #38–#44** — every one of
the seven is this packet's. In the tree: `Domain/Money/Settlement.cs`, `Domain/Melds/TableRules.cs`,
`Domain/Play/RoundEngine.cs`, `Domain/Play/MatchEngine.cs`, `Sim/SimulationOptions.cs`,
`Tests/Conformance/RuleConformance.cs`.

**Depends on.** P33 (§7.3 is the multiplier this composes with), P32 (a current five-handed
baseline to re-measure against).

✅ **Unblocked on 2026-08-22, the day it was written.** §9 #43 was put to Aung Aung and closed:
**the seats do not re-draw every round** — *"only when people ask for it"* — so §7.5's blame names
the same person for the whole streak and is coherent. `RULES.md` is at **rev 28** and §3 step 2 is
corrected.

⚠️ **But the answer moved the dependency rather than removing it: P35 depends on P36.** A
three-round streak blamed on *the seat above you* needs a seating that survives three rounds.
✅ **P36 shipped on 2026-08-22, so that dependency is discharged**: `MatchEngine`'s default is
`SeatingPolicy.Held`, and *the seat above you* is now the same person for a whole match.

🔥 **P36 also made §9 #46 reachable, which it was not when this entry was written.** A re-seating
*mid*-streak requires a re-seating to exist at all, and one does: a table set to
`SeatingPolicy.Every(n)` will move the seats under a two-round winner. **So #46 is no longer
hypothetical and its safe default — settle from the round being settled, old #42's recommendation
— needs a test rather than a note.** ⚠️ **Read the streak off `Seating` and never off `Players`**:
the membership is fixed and the order is not, and a streak rule that read the membership would
blame the wrong seat at exactly the tables where the question can arise.

---

#### Why this is an L and not the M it looks like

🔥 **§7.5 is the first rule in this project that cannot be settled from a single round**, and that
breaks two things at once.

1. ⚠️ **`Settlement` is a pure function of one round and holds no history.** A streak lives above
   it. The count belongs to whatever owns a sequence of rounds — `MatchEngine` — and **the
   settlement must be told the answer rather than computing it**, exactly as P33 made
   `Settlement.RoundPayment` take `jokerless` rather than deriving it. 🔥 **P33's own trap applies
   here verbatim and harder**: a net delta is split in three places and only one of them is the
   domain — the console's settlement panel and `Sim`'s `SeatRow.Flat`/`SideBet` **both re-derive
   the round/side-bet split to display it**. §7.5 changes *who pays*, which neither of them models
   at all: both assume every loser pays the same amount. **Left alone, a streak round would be
   displayed as an ordinary one and every money measurement would read a number that never
   happened.**
2. 🔥 **No measurement in this project can currently observe §7.5.** `BurmesePoker.Sim` plays
   `RoundsPerGame = 1` — a deliberate design decision (§3.8), because the game is the unit of
   independence and a match of correlated rounds would break every interval in `STRATEGY.md`.
   **A three-round streak cannot occur in a one-round game.** ⚠️ **So the packet must decide, and
   say, whether §7.5 is measured at all** — and *"it is built, fenced by conformance, and
   deliberately not in the standing set"* is a legitimate answer that must be **stated** rather
   than arrived at by silence (`STRATEGY.md` §11, "no silent caps").

⚠️ **And §7.4 may change the shape of a round rather than its arithmetic.** Under §9 #38's
recommended reading — *the dealt thirteen alone* — `RoundEngine` must offer a declaration
**before the first turn**, and it has no such path: a round begins by asking seat 0 to take a card.
🔥 **That is the first change to the shape of a round since P0.** Under the other reading (the
winner's first turn) it is a turn counter and nothing more. **The two readings differ by a whole
engine path, which is why #38 is listed first in §9.**

#### Build

1. **§7.4, the deal bonus.** A second multiplier beside `TableRules.JokerlessMultiplier`.
   ⚠️ **Unlike §7.3's, it is not a function of the player count**, so it does not belong in
   `TableRules`' per-seat-count table — putting it there would imply a seam it does not have.
   `Settlement.RoundPayment` grows one more argument, or takes a small record of *what this win
   was*; **prefer the record** — this is the third thing to multiply the round prize in three days
   (§7.2's step 1 now carries four qualifications) and a fourth should be expected.
2. **How §7.3 and §7.4 compose** — §9 #39, recommended **multiply**, and ⚠️ **fenced by a named
   test either way**, in P26's and P33's idiom. State the default in the packet report.
3. **§7.5, the streak.** `MatchEngine` counts consecutive wins by the same player and hands the
   round the answer; `Settlement` takes *who pays* rather than assuming everybody does.
   ⚠️ **The seat is identified by the round's own seating** (§9 #42's recommendation) and the
   engine already re-draws it (§3, P28) — `MatchEngine.PlayRound(drawOrder)` is where a deal
   written down for a seating lives.
4. 🔥 **Both consumers that re-derive the split must be fixed in the same packet**, and this is
   item 3's real cost. `Settlement` returning one number per seat is not enough: the console panel
   and `SeatRow` each need to be able to show *"everybody paid nothing; the seat above paid the
   lot"* without inventing it. **P33's unchanged `money.side-margin.*` rows are the evidence that
   this can be done right; do the same check.**
5. **Conformance.** `RuleConformance` re-derives every Settled rule independently, and
   `SettledRuleCoverageTests` **fails the build on a Settled section nothing checks or exempts** —
   so §7.4 and §7.5 will make the tree red the moment they are marked Settled and unchecked.
   ⚠️ **They are Settled *now***, which means that test is the packet's own alarm and must be
   answered with `Checked(...)`, not silenced with `Exempt(...)` — P33's precedent.
   ⚠️ **§7.5 needs a multi-round conformance case**, which `RuleConformance` has never had: it
   audits 180 *ordinary rounds*. That is new harness, not a new assertion.
6. **Re-measure.** A settlement change invalidates every money figure, exactly as P33's did.
   ⚠️ **Expect P33's reproduction shape, not P29's**: no rung reads either rule, so **play cannot
   move and only money can** — and if a win rate moves, that is a bug and not a finding. **Write
   the prediction down before the run.** §7.5 is the exception and it is the interesting one: it
   is invisible to a one-round-per-game suite, so **its rows should not move at all**.

#### Acceptance

1. §7.4 and §7.5 are implemented, and `SettledRuleCoverageTests` passes with both **`Checked`**.
2. Every §9 default this packet builds on is **named in the report** and **fenced by a test named
   for the question**, so the answer arriving is a one-line change with a failing test to find it
   (P26 #32, P33 #36 and #37 are the precedent).
3. The console's settlement panel and `Sim`'s `SeatRow` both show a §7.5 round correctly, and the
   `money.side-margin.*` rows are **unchanged** — the proof the substitution landed in the round
   column.
4. 🔥 **Whether §7.5 is in the standing set is stated either way**, with its reason, in
   `docs/STRATEGY.md` §11.
5. `RULES.md` §10 #20 and #21 are discharged, and the document records nothing Settled that
   nothing implements.

**Done when.** The two rules Aung Aung gave on 2026-08-22 are played, and the one that reaches
across rounds is either measured or explicitly and visibly not.

---

#### ✅ Built 2026-08-22 (Opus 5) — what it turned out to be

**`RULES.md` is rev 30 and §10 is empty: every rule the document records as Settled is
implemented.** Six §9 defaults were built on, every one fenced by a test named for the question,
and **one new question was opened (§9 #48)**.

🔥 **(1) The costly half was §7.4, not §7.5, and this entry had it the other way round.** §9 #38's
recorded default — *the dealt thirteen alone* — needed a whole new path through `RoundEngine`:
every seat whose **dealt** hand already covers is offered the declaration, in turn order, before
the first take. **A round can now run no turns at all** (`RoundResult.Turns` is 0), which is the
first change to the shape of a round since P0, and it makes `TurnNumber` **0** a real value that
reaches the journal, the console's turn heading and the server's `TurnBegan`. ⚠️ **It also opened
§9 #48** — two seats dealt a winning thirteen at once — which no saying covers and which the engine
answers *earlier in turn order*.

🔥 **(2) §7.5 was cheap once the division was seen: settlement is *told*, never made to remember.**
The count lives on `MatchEngine.Streak` and is handed down to each round as it is dealt.
**`Settlement` still holds no history and takes no match** — asserted over the parameter list — and
`Win` is the record it is told. ⚠️ **The reading matters, and the first implementation got it
backwards**: *"pays your whole payout"* means the winner collects **exactly what they would have
collected**, out of one pocket. Paying one loser's share instead would leave a player four fifths
worse off for winning three in a row, and it took a test to notice.

🔥 **(3) The consumer trap this entry predicted was real, and the fix was to delete the
re-derivation rather than to extend it.** `Settlement.RoundPayments` is the round column, computed
once in the domain; the console's settlement panel and `SeatRow.Flat` both read it. ⚠️ **Both had
assumed every loser pays the same amount** — true from rev 1 until rev 27 — and a split at the
wrong place posts the difference into the side-bet column, where every money measurement reads it,
**with the totals still adding up**.

⚠️ **(4) A scripted test agent was silently declining the new question, and that was luck rather
than design.** `ScriptedPlayerAgent` advances its script by turn number and was constructed at turn
0, so it answered *no* to the deal declaration without ever being asked to. **A great many tests
deal a seat a winning thirteen in order to script what it does on turn 1**, and a default of *yes*
would have ended most of those rounds before they began. It is now an explicit `DeclaresOnTheDeal`,
defaulted to no and documented as a decision. **Exactly two tests really did change behaviour, and
both for the right reason**: the three-round bank test's third round is now billed to one seat, and
the round-number test now sees turn 0 before turn 1.

🔥 **(5) The conformance harness gained its first multi-round case, and the exemption that said it
could never have one was half wrong.** §7.5's `Exempt` reasoned that `RuleConformance` watches
ordinary rounds and a streak is not a property of a round. **It still watches one round — but it
can be *told* what the rounds before it did**, with the count kept by the driver rather than read
off `MatchEngine`, and re-derive the consequence.
`AStreakOfWinsBreaksNoSettledRuleAndIsBilledToTheSeatAbove` plays 120 rounds at four and five seats
and **fails if no streak occurred**, so it cannot pass vacuously. ✅ **Both registry entries are
`Checked`, and there are now no whole exemptions at all** — the first time since P30.2 wrote that
test; the ceiling came down 7 → 6.

✅ **(6) Acceptance 4, stated either way.** §7.5 is **not** in the standing measurement set and
cannot be while `RoundsPerGame = 1` stands (§3.8) — `docs/STRATEGY.md` §11 says so, and says what
is therefore unknown: **what §7.5 is worth, and whether asking to change seats before somebody's
third win is a strategy.** §7.4 *is* observable in a one-round game, so `bonus.deal-rate.*` is in
`measurements.csv` beside `bonus.jokerless-rate.*` — which is also the row that would explain a
money figure moving.


---

### P34 — A front door, and a documentation set that cannot go stale quietly ☑ — **done 2026-08-23**

**Goal.** Somebody who has never seen this repository can arrive at it, understand what it is
within a screen, and **not be told anything that stopped being true three packets ago**.

**Read first.** Every file in `docs/`, and `CLAUDE.md`. That is the packet.

⚠️ **Amended 2026-08-22 by P24.2 — one measured claim now ships as prose *in the product*, and
this packet's staleness sweep has to know it is there.** `AdviceRationale.ForObjection` tells a
player that refusing a claim *"has been measured, and refusing is worth nothing either way — the
difference is inside the margin of error"*, which is P29's `docs/STRATEGY.md` §12 null said out
loud on the felt. 🔥 **A null makes an explanation more interesting rather than less** — it is a
thing this project knows and no player does — and it deliberately **carries no number**, so it
cannot rot into a wrong figure the way a quoted interval would. **But it is still a measurement,
and it is the first one outside `docs/`.** If that cell ever separates, the sentence is wrong and
nothing currently notices. **Either fence it with a test or list it in the staleness inventory; do
not leave it as the one measured claim nobody is watching.**

🔥 **There is no `README.md`.** A visitor's first sight of this project is a directory listing and
then `CLAUDE.md` — which opens with a wall of accumulated packet history written *for a cold Claude
session*, not for a person deciding whether this repository is interesting. **The two audiences want
opposite documents**: a session wants everything that was ever learned, in priority order; a visitor
wants what is true now, in a page.

⚠️ **And the second half of the goal is the hard half.** These documents are written as a **running
narrative** — they accumulate, they mark findings with 🔥 and ⚠️, and they preserve superseded
reasoning on purpose, because *why* a wrong default was taken has repeatedly been worth more than
the default. **That is a genuine strength and it must not be flattened.** But it means a reader
cannot tell *current fact* from *historical record* without knowing the packet history, and three
of the ten documents are wholly historical (`RECONCILIATION-PLAN.md` is marked superseded,
`RULES-TECHNICAL.md` describes code that was deleted at P0, `REVIEW-2026-08.md` is a closed review).

**Build.**

1. **`README.md` — the front door, and the only document in the repository that is *only*
   current.** What the game is (two decks, 13 cards, fully concealed, played for money); that no
   published ruleset exists and `RULES.md` is a sourced reconstruction; what the seven projects are;
   how to play it in thirty seconds; where the answers live. ⚠️ **No packet numbers, no 🔥, no
   history.** If a sentence needs *"since P27"* to be true, it belongs somewhere else.
2. **A staleness banner on every historical document**, at the top, above the fold — what it was
   for, when it stopped being current, and what replaced it.
3. 🔥 **Turn the habit into a test, which is this project's own idiom** (P18 → P20 → P23 made
   "a rung cannot be added without being measured" a habit, then a default, then an assertion;
   P30.2 did the same for Settled rules). **At minimum:**
   - **Every document in `docs/` appears in the documentation map, and every entry in the map
     exists.** An orphan document is one nobody maintains.
   - **Every project and command named in a fenced `bash` block resolves.** `--seat` became
     `--people` at P13.6 and prose elsewhere kept saying `--seat`; a command that no longer runs is
     the most expensive kind of stale, because a visitor tries it.
   - **The tree's test count and `RULES.md`'s rev, where prose quotes them, match reality.**
     `GameJournalTests` already binds the rev; the test count has been wrong at least once
     (P31 wrote 709 for 715 and it was caught by running, not by reading).
   - **A number that also exists in `docs/strategy/measurements.csv` matches it.** §11 already
     forbids quoting a figure from prose — this makes it checkable rather than a rule people
     remember.
4. **One pass over the prose for claims that expired.** ⚠️ **Expect the "was true when it was
   written" class**, not outright falsehoods: *"every rule this document records as Settled is
   implemented"* was true from P28 to rev 24 and is false again as of §7.3.

⚠️ **What this packet must not do.** It must not delete the narrative, compress the findings into
bullet points, or remove a superseded default and the reasoning behind it. **The accumulated *why*
is the most valuable thing in `docs/`** and three packets have been shaped by re-reading it. The
job is a front door and a smoke alarm, not a rewrite.

**Acceptance.**
1. `README.md` exists, is current-only, and a reader who knows nothing can say what the game is,
   what the repository contains and how to run it.
2. Every historical document says so in its first three lines.
3. At least the four checks above are tests, and each is proved able to fail.
4. `dotnet build && dotnet test` green, and the count in `STATUS.md` matches it.

**Done when.** A stranger can read the front door and be right about the project, and a document
cannot go stale without something turning red.

⚠️ **This packet is independent of the rules work and of the measurement programme** — it needs no
expert answer and regenerates nothing. ⚠️ **P33 has shipped and so has everything else, so it is no
longer the packet to run *instead* of anything: it is the only packet left on the plan.**

---

#### Re-planned by P35, 2026-08-22 — a fifth check, and two more claims to watch

🔥 **Add a fifth check, and it is the one this project has most earned: every recorded default in
`RULES.md` §9 that says *"built on this default"* must name a test that exists.** P26, P33, P35,
P36 and P37 have now all shipped rules built on open questions, and the discipline each time was
*fence it with a test named for the question, so the day an answer arrives the failing test is the
change list*. **P35 alone did it six times** (§9 #38, #39, #40, #41, #44, #46). ⚠️ **A fence that
has been renamed or deleted is worse than no fence**, because the document goes on promising it —
and nothing currently checks that the names in §9 resolve. The parse is the same shape as
`SettledRuleCoverageTests`', which already reads `RULES.md` and matches against code.

⚠️ **Two more claims for the staleness pass to know about.** **(1)** `docs/STRATEGY.md` §11 now
carries a *negative* measurement claim — that §7.5 is **not** in the standing set and cannot be —
which is prose that would go quietly wrong the day somebody builds a match-level harness. **(2)**
The exemption ceiling in `SettledRuleCoverageTests` is a number in a test that a status file also
quotes; P35 moved it 7 → 6.

✅ **And the "was true when it was written" example in build item 4 can be retired.** *"Every rule
this document records as Settled is implemented"* is **true again** as of P35 — §10 is empty — so
the sentence to hunt is no longer that one. **It is a better example for having been false twice
and true three times**; keep it as the illustration and stop using it as the bug.

---

#### What it built, 2026-08-23 — and the four things that were not in the plan

✅ **All five checks landed and each was proved able to fail by mutating the document**, which is
the P30.2 discipline applied to prose: a deleted map row, a stripped banner, `--people` renamed
back to `--seat`, a fence renamed away, a nudged reference-table cell, the four-handed figures put
back into `PLAYING.md`, and the claim-permission null made to separate — **seven mutants, seven red
tests.** `BurmesePoker.Tests/Docs/` is three files: `Documentation` (the set itself),
`DocumentationTests` (map, banners, commands, fences) and `PublishedFigureTests` (the figures, the
count, the rev, and the product's one spoken measurement).

🔥 **(1) The test count is *discoverable*, which the plan did not assume.** `[Fact]`s plus theory
rows counted by reflection over the test assembly comes to exactly the number a run reports, so the
check is against reality rather than against another document. ⚠️ **The counter knows `InlineData`
and `MemberData` and throws by name on anything else** — a `ClassData` arriving needs one arm added
rather than a silent undercount.

🔥 **(2) The narrative form is what makes the current figure findable, and that turned into the
rule.** Every one of these documents is newest-first, so **the first count in a file is the current
count** and everything after it is dated record. Checking only the first is not a hedge: the log
records the tree at 677, 697, 715 and 795, and **a check demanding they all agree would ask the
project to delete its own history** — which is precisely what this packet was told not to do.

⚠️ **(3) What was actually stale was neither of the documents the plan named.** It was
`docs/PLAYING.md` and `docs/RULES-PRIMER.md` — **the two written for a person rather than for a
session**, and the two nothing else in the tree depends on. The guide quoted a **four-handed**
difficulty reference table on a page describing a five-handed table, a `headline.balanced.*` pair
matching no row in either CSV, and *"your neighbours change every round"* (false since P36); the
primer carried **four `[⚠ code disagrees]` tags for divergences closed at P25–P28** and a
settlement section that stopped at *flat*. 🔥 **The lesson is where to look: the documents that go
stale are the ones nothing in the tree depends on.**

⚠️ **(4) One documentation-accuracy finding in `RULES.md`, recorded rather than fixed silently.**
§10's headline says *empty* and there is a standing exception — **#7**, `RoundEngine.MinimumPlayers`
is 4 against §2's Settled 2-to-6 — so the two- and three-handed win conditions are implemented,
tested and unreachable from a dealt game. **It is the oldest entry in that list and no packet owns
it**, which is why it is now the first candidate in `STATUS.md`'s *What is next*. No rule changed
and the rev did not move.

⚠️ **`AdviceRationale.ForObjection` is fenced rather than inventoried.**
`PublishedFigureTests.TheOnlyMeasuredClaimTheProductSpeaksAloudIsStillANull` asserts the sentence
still exists **and** that the cell it speaks for is still inside its interval in both currencies —
so the day refusing a claim starts to be worth something, the product's own prose goes red.

---

### P38 — The rulebook: the game taught, not reconstructed ☑ — **done 2026-08-23 (Fable 5)**

✅ **Built as specified, with the joints tighter than the plan asked.** `docs/RULEBOOK.md` is the
game in the reading order above; **four tests in `Tests/Docs/RulebookTests.cs`**, each proved able
to fail by mutating the document. 🔥 **The worked round went further than "stamp the seed"**: the
test *replays* the printed construction — seed 15, five seats of `outs` — and asserts the dealt
hands, the turn-up, the owned money cards, the winner, the melds and every settlement cell, so an
invented figure cannot survive a run. Seed 15 was scanned for: the winner declares **jokerless**
(the ×3 in a real settlement) and had **discarded an owned A♠**, so permanent ownership is
demonstrated rather than asserted. 🔥 **The house-readings set is derived from `RULES.md` §9's own
table shape** (numbered un-struck rows, five columns) and fenced **both ways** — a question closing
fails the build until the reading folds into the body; a new default fails it until the reader is
told. ⚠️ **One deliberate widening**: the appendix carries #45 (a `PLAYER` ruling, not a default)
beside the eleven defaults, because it is a house choice in exactly the sense the appendix exists
for and because deriving the set cleanly includes it. The record of what it was planned from
follows.

**Goal.** Somebody who has never seen this game can be handed **one document**, read it front to
back, and play a correct round — the way a board game's rulebook works. **No prior knowledge, no
provenance tags, no open questions, no packet numbers, no confidence scale.**

**Read first.** `docs/RULES.md` end to end (this is the one packet that needs all of it),
`docs/RULES-PRIMER.md`, and P34's `BurmesePoker.Tests/Docs/` so the new document arrives already
inside the staleness machinery.

⚠️ **This is not a rewrite of `RULES.md` and must not become one.** `RULES.md` stays **the sole
rules authority**: it is where a rule is decided, where its provenance lives, and where an open
question is recorded. **The rulebook is derived from it and decides nothing.** If writing the
rulebook raises a question, it goes into `RULES.md` §9 with a provenance tag exactly as any other
packet's would — and the rulebook proceeds on the recorded default.

🔥 **What is missing today is an audience, not content.** `RULES.md` is 2,500 lines organised as a
*reconstruction*: every rule carries who said it and how sure they were, §9 is a live ledger of
what nobody knows, and §10 is a ledger about the code. That organisation is the reason this project
has not silently invented a rule in a year, **and it is exactly wrong for a new player** — it asks
them to hold provenance in their head while learning to play. `RULES-PRIMER.md` is closer but is a
**recall aid** by its own first line: it assumes the game and reminds you of it, it is organised by
rule area rather than by reading order, and it prints `[✓]`/`[~]`/`[?]` on nearly every sentence.

**Build.**

1. **`docs/RULEBOOK.md`, in reading order**, which is a different order from `RULES.md`'s:
   *what this is* → *what you need* (two decks, jokers, 108 cards, two stakes agreed before
   play) → *setting up* (seats drawn once, thirteen each, the two cards turned up) → *a turn*
   (take one, throw one) → *the one restriction on what you may throw* (§5.1, taught as a table
   manners rule, which is what it is) → *the opening turn's extra option and the permission it
   needs* → *how a round ends* → *what it pays*, both ledgers → *the money cards*, which are the
   part with no parallel in any game a reader will know → *reference*: the melds, the win
   condition by player count, a settlement worked example.
2. 🔥 **A worked round, generated rather than invented.** A short seeded round printed hand by
   hand, with the settlement arithmetic done in full. ⚠️ **Generate it from a real game** — a
   journal or a `--seed`ed console capture — so the example cannot be wrong, and say which seed it
   is. **An invented example in a rulebook is a bug that teaches itself to every reader.**
3. **A one-page reference at the end**: the turn, the melds, the win condition by table size, the
   multipliers, what each money card pays. It should be the thing a table actually keeps out.
4. **A *house readings* appendix, short and in a player's language.** 🔥 **This is the packet's
   real problem.** Eleven §9 rows are being **played on a recorded default** (#33, #36, #37,
   #38–#41, #44, #46, #47, #48), and a rulebook has to state one answer — which silently promotes
   a default to a rule for the reader. The honest form is not a provenance table: it is a closing
   section saying *these few points came up rarely enough that we had to choose, and here is how we
   play them*. ⚠️ **A player needs to know which lines are house choices when they sit down with
   somebody who learned the game elsewhere**, and that is the only reason this appendix exists.
5. **Bind it to the rules it was derived from.** The document stamps the `RULES.md` revision it
   was written against, and a test asserts the two agree — the
   `JournalHeader.CurrentRulesRevision` idiom, which already binds a journal to the rules that
   produced it. 🔥 **A rulebook is the highest-consequence stale document this project could own**,
   because it is the one a person plays from, and it is the furthest from anything a build would
   break. **A rules change must make it red.**
6. **Join it to P34's machinery**: a row in `CLAUDE.md`'s documentation map (`DocumentationTests`
   fails without one), no historical banner, and `README.md` points at it as the way in.

⚠️ **What this packet must not do.** It must not quote a strategy figure (that is P39, and a
figure in two documents is a figure that drifts), must not restate `RULES.md`'s reasoning, must not
carry a §9 or a §10, and must not use the word *reconstruction* anywhere in the body. **The
rulebook's voice is a rulebook's**: it says what happens, not who remembered it.

**Acceptance.**
1. A reader who knows only ordinary rummy can play a correct round from `RULEBOOK.md` alone,
   including the money layer, the feeding ban and the win condition at their table size.
2. Every rule in it traces to a `RULES.md` section, and nothing in it decides a rule `RULES.md`
   leaves open — the defaults taken are the recorded ones and the appendix names them.
3. The worked example is generated from a real seeded round and the seed is printed.
4. The rev stamp is bound to `RULES.md` by a test, proved able to fail.
5. It is in the documentation map, and `dotnet build && dotnet test` green with the count in
   `STATUS.md` matching.

**Done when.** A stranger can be handed one file and taught the game from it, and the file cannot
fall behind the rules without something turning red.

---

### P39 — How to play well: the strategy guide a player can use ☑ — **added 2026-08-23, re-planned by P38, done 2026-08-23 (Fable 5)**

⚠️ **Three amendments from P38, written while the reasoning is fresh.** **(1) Moving `PLAYING.md`'s
*Playing better* figures moves their fences**: `PublishedFigureTests.TheFiguresThePlayersGuideQuotes`
holds two regexes against `PLAYING.md`'s exact wording (the four-way dial figure and the headline
pair) — when that section becomes a pointer, those regexes must move to `HOW-TO-PLAY-WELL.md` with
the figures, or the test fails on a sentence that rightly no longer exists. **(2) Quote only
CSV-fenced figures.** `RULEBOOK.md`'s worked round prints real dollar figures that are
*engine-replayed*, not `measurements.csv` rows — the guide must not grow a second worked example;
point at the rulebook's. **(3) The new document lands in `CLAUDE.md`'s map or
`DocumentationTests` goes red**, and its first-three-lines must not read as a historical banner.

**Goal.** A player who can already play asks *how do I get better?* and gets **one document** that
answers it in plain language, from what this project has actually measured — not a research report.

**Read first.** `docs/STRATEGY.md` end to end, `docs/strategy/measurements.csv`,
`docs/PLAYING.md` §*Playing better*, and `BurmesePoker.Tests/Docs/PublishedFigureTests.cs`.

⚠️ **`STRATEGY.md` stays the measurement authority** and every number still comes from
`measurements.csv`. **This packet changes who a number is written for, not where it comes from.**

🔥 **The gap.** `STRATEGY.md` is a research document and is right to be: §1 is five rules about
reading an interval, §2 is eight bot rungs indexed by packet number, and the findings are stated as
paired margins with Holm verdicts. **A player cannot use any of that**, and the four figures that
would actually change how they play are scattered through it. ⚠️ **`PLAYING.md` already has a
*Playing better* section carrying four of them** — and it is the section that went stale for two
whole measurements before P34 fenced it, **because a figure with two homes has none.**

**Build.**

1. **`docs/HOW-TO-PLAY-WELL.md`**, organised by decision rather than by experiment: *what you are
   actually optimising* (how many of the thirteen you would be left holding meld — the whole game
   in one sentence) → *choosing a discard when several look equal* (the tie-break, and it is worth
   more than anything else here) → *looking one card ahead* → *what the money is worth and when to
   chase it* → *what does not matter*, which is where the nulls go.
2. 🔥 **Publish the nulls, and give them the space they deserve.** This project knows several
   things a player would guess wrong: **discarding a money card costs you nothing** (ownership is
   permanent), **most of the money is decided at the deal**, **refusing a claim is worth nothing
   either way**, **counting cards is worth nothing** as the shoe is visible here, and **which side
   of you a weaker player sits is worth nothing** though their presence at all is worth several
   points. ⚠️ **A null is more useful to a player than a margin**, because it is where they would
   otherwise spend attention for free.
3. **State the caveat once, plainly, and then stop.** Every figure here is measured **between
   computer players**, over thousands of rounds, at a five-handed table. ⚠️ **Nothing measures the
   three scoring rules** — no rung knows §7.3, §7.4 or §7.5 exists — so the guide must say the
   bonuses are **unpriced** rather than quietly leave them out.
4. **Take ownership of the figures.** `PLAYING.md`'s *Playing better* section becomes a pointer;
   `STRATEGY.md` is untouched. **One home per number.**
5. **Fence every figure it quotes** by extending
   `PublishedFigureTests.TheFiguresThePlayersGuideQuotes` to the new document, which is where
   `PLAYING.md`'s four already live.

**Acceptance.**
1. A player can read it in one sitting and name three things they will do differently.
2. Every number in it resolves to a row of `measurements.csv`, checked by a test proved able to
   fail.
3. The nulls are published, not omitted, and the measured-between-bots caveat is stated once.
4. No figure appears in both this document and `PLAYING.md`.
5. Green, with `STATUS.md`'s count matching.

**Done when.** The answer to *"which bot should I play, and what actually works?"* has a research
document and a player's document, and neither is pretending to be the other.

---

### P40 — The game in Burmese: translated rulebook and strategy guide ☐ — **added 2026-08-23, at Nick's direction**

**Goal.** `docs/RULEBOOK.md` and `docs/HOW-TO-PLAY-WELL.md` exist in Burmese — as
`docs/RULEBOOK.my.md` and `docs/HOW-TO-PLAY-WELL.my.md` — so that the game's own tradition can
read the two documents written for players. 🔥 **This is the first packet whose main input is
produced outside the repository**: the translations are made by Nick with an LLM that is strong
in Burmese (Gemini or ChatGPT — reportedly the two best at it), using the prompts in
`docs/translation/PROMPTS.md`, cross-checking each model's output with the other model, and the
packet lands the vetted text and builds the fences. **It cannot start until that text exists.**

⚠️ **A first round already ran, on 2026-08-23, and its artifacts are in `docs/translation/`** —
Gemini's rulebook translation (`promptA_geminiResponse1.md`), ChatGPT's cross-check
(`promptC_chatgptResponse1.md` — it caught Burmese numerals, glossary collisions and one
meaning change, so the two-model loop earns its keep), and Gemini's corrected pass
(`corrections_geminiResponse1.md`). 🔥 **None of it is landable yet**: all three were made from
the **rev-30** rulebook, hours before rev 31 added §5.2's face-up rule to it, and the corrected
pass has not itself been cross-checked. **The packet re-runs the loop against the rev-31
rulebook** — the glossary already vetted in round one carries forward, so the re-run is cheaper
than the first. The strategy-guide translation (prompt B) has not started.

**Read first.** `docs/translation/PROMPTS.md` (the workflow and the three prompts),
`BurmesePoker.Tests/Docs/RulebookTests.cs` (the rev-stamp binding to copy), and
`BurmesePoker.Tests/Docs/PublishedFigureTests.cs` (what is already fenced in the English guide).

⚠️ **Why these two documents and not `RULES.md`.** `RULES.md` is a provenance ledger written for
the project — half of its value is the tags and the open-question tables, which are exactly what
a reader-facing translation should not carry. The rulebook and the guide are the two documents
written *for a player*, and they are already derived: translating them adds one more derivation
step to documents built to be derived. **`RULES.md` stays English and stays the sole authority;
a Burmese sentence decides nothing**, same as the English rulebook it is translated from.

🔥 **The hard problem is staleness, and it is worse here than anywhere else in `docs/`.** A
translation is the furthest document from the tree this project would own — no build breaks when
it rots, and *nobody working in the repository necessarily reads the language it is written in.*
No test can read Burmese, so the fences fence **what survives translation**, which is precisely
what the prompts insist stays byte-for-byte:

**Build.**

1. **Land the vetted translations** as `docs/RULEBOOK.my.md` and `docs/HOW-TO-PLAY-WELL.my.md`,
   each opening with a line naming the English source it was translated from and the rev it was
   derived at — in Latin digits, so the fence below can read it.
2. **The rev-stamp fence, same mechanism as `RulebookTests`**: the first `rev N` in each
   translation equals `JournalHeader.CurrentRulesRevision`. A play-changing revision is then a
   red build until somebody re-checks the Burmese against what changed — the maintenance,
   compelled, exactly as it is for the English rulebook.
3. **The figure fence, derived from the English source rather than written twice**: extract from
   `RULEBOOK.md` every dollar figure and card form (7♦, A♠, ×2, ×3, ×5 …), and from
   `HOW-TO-PLAY-WELL.md` every `x ± y` pair, percentage and dollar figure, and assert each
   appears **verbatim** in the corresponding translation. A translation that drifts a number —
   or converts one to Burmese numerals (၀–၉) — is a red build. ⚠️ **This is why the prompts
   demand Latin digits**; the fence is what the demand is *for*.
4. **The joins the documentation tests already enforce**: both files in `CLAUDE.md`'s map, no
   historical banner, and `README.md`'s answers table gains the Burmese way in.
5. **A structural sanity check**, cheap and worth having: the translation has the same number of
   headings at each level as its source, so a silently dropped section is a red build even
   though no test can read what the sections say.

**Acceptance.**

1. Both translations landed, each stamped with the rev it was derived from, bound by a test.
2. Every figure, interval, dollar amount and card symbol of each English source appears
   byte-for-byte in its translation, checked by a test proved able to fail.
3. ⚠️ **A human read is the real acceptance and a test cannot substitute for it** — the
   cross-check prompt (`PROMPTS.md` prompt C) has come back clean from the model that did not
   produce the translation, and the packet's report says so per document.
4. Green, with `STATUS.md`'s count matching.

🔥 **What this buys beyond the documents: it feeds candidate 2.** The most valuable unstarted
work in this project is an expert session on the eleven §9 rows played on recorded defaults —
and the experts are Burmese speakers being asked about a Burmese game through an English
reconstruction. **A Burmese rulebook is the best instrument this project could hand them**: a
reading of the rulebook in their own language surfaces a mis-recorded rule the way no
question-list can, and this project's own history says rules conversations answer past the
question asked. ⚠️ **The worked round translates with the rulebook and is the most fragile part
of it** — if a rules change re-derives the English worked round (P38's red-build-on-purpose),
the Burmese one is stale with **no test to say so beyond the rev stamp**. That is what build
item 2 exists to catch.

⚠️ **Risks, named.** LLM Burmese is good and not native — the two-model cross-check is in the
prompts because a single model reviewing itself is not a review; register matters (a card table,
not a textbook) and the glossary-first instruction is what holds terminology consistent across
two documents translated in different sessions. **If the cross-check keeps finding meaning-level
errors after two rounds, stop and say so** — a wrong rulebook in the experts' own language is
worse than none, because they will trust it.

---

### P41 — The table shows what the rules make public ☑ — **added 2026-08-23, from rev 31; done the same day**

✅ **Shipped 2026-08-23 exactly as specified, and the load-bearing bet held: the whole packet is
one fold.** `Presentation/TableLook.cs` is the single implementation of what the rules make
public — every seat's pile and every seat's face-up cards — and the console's `ConsoleObserver`,
the server's `TableFanOut` and the browser's `TableBoard` all hold one and feed it the same
events they were already narrating (the browser's own pile fold moved *into* it, so the pile
logic that existed since P13.3 is now written once). Acceptance 4 was asserted, not argued: a
seeded 300-game `Sim` run wrote a **byte-identical CSV and byte-identical journal** either side
of the change, and the HEAD journal replays under the new tree. The concealment discriminator
and both §9 fences were each proved able to fail by mutation. ⚠️ **One deviation worth a line:
the §5.2 registry entry is `Checked` with no ⚠**, because both of its recorded defaults (#49,
#50) are presentation-only — the engine plays identically whichever way the expert answers — so
unlike §7.4's defaults there is no engine behaviour standing on them; the fences alone carry it.
The original packet text follows.

**Goal.** `RULES.md` §5 has said since rev 17 that every discard pile may be looked through, and
rev 31 added §5.2 — a card taken in the open lies face up in front of its taker for as long as
it is held. **Neither is visible anywhere a player sits.** This packet puts both in front of a
person at either front end, discharges §10 #24, and converts the registry's §5.2 exemption to
`Checked`.

**Read first.** `RULES.md` §5 (the public-piles rule and its rev-31 corroboration), §5.2 whole,
§9 #49 and #50; `BUILD-PLAN.md` §3.10–§3.11 (the browser's constraints); `Tests/Server`'s
`ConcealmentTests` for the boundary this packet must not soften.

🔥 **The load-bearing fact: the engine already plays these rules.** Open takes and discards are
public events carrying a `CardId`, so **the face-up set is a pure fold**: a card enters it when
taken in the open — the offered discard, and the claimed turn-up on §9 #49's recorded default —
and leaves when that same `CardId` leaves the hand. Every pile's contents are likewise already
in the event stream. **No engine state, no journal change, no new question to any agent.**
⚠️ **So acceptance 4 is byte-identity, asserted rather than argued**: journals unchanged, and a
seeded `Sim` CSV identical before and after.

**Build.**

1. **The face-up fold**, in Presentation/Server where the other event folds live. Two fences
   named for the §9 rows, in the `…UntilTheExpertSaysOtherwise` idiom: the claimed turn-up
   acquires the mark (#49), and the duplicate case — a face-up Q♣ beside a concealed Q♣, the
   concealed copy thrown, the face-up card **staying** (#50, which is what discard-by-`CardId`
   already does; the fence keeps it true on purpose rather than by accident).
2. 🔥 **Concealment is the constraint, not the feature.** The face-up mark is the first
   per-card public fact about a concealed hand, so a blind-drawn card must never acquire it —
   extend `ConcealmentTests` with that discriminating case and prove it can fail by mutation,
   exactly as P13.2 did for the blind draw itself.
3. **Every seat's full pile in the view models and the fan-out**, in discard order, public to
   every viewer — own pile included.
4. **The console**: face-up cards shown at every seat and marked in your own hand panel (a
   marker in the idiom of `★` and `($)`), pile contents reachable from the table panel. A
   `drive-console.py` re-capture — ⚠️ the capture **will** differ, this is a presentation
   change; byte-identity lives in the journal and the CSV, not the screen.
5. **The browser**: each seat on the felt shows its face-up cards beside it — big symbols,
   minimal prose (§3.11, and the browser's standing direction: a table, not a document) — and
   each seat's pile opens on demand. Your own face-up cards are marked in your hand.
6. ⚠️ **`TurnContext` is deliberately not widened.** No rung reads any of this today and none
   starts to: a bot that reads the piles or the face-up marks is a **new rung** under P15's
   discipline and arrives measured (`STRATEGY.md` §11's rule). P20's counting rung stays
   as-measured; its §8 entry already records the narrower information set it chose.
7. **Close the ledgers**: §10 #24 discharged; registry `["5.2"]` → `Checked` naming the fold
   and the mutation test; exemption ceiling back **7 → 6**.

**Acceptance.**

1. A person at either front end can see every card of every discard pile and every face-up
   card at every seat, their own included.
2. The blind draw is still the only private fact — `ConcealmentTests` extended with the
   face-up discriminator and proved able to fail.
3. §9 #49 and #50's defaults each fenced by a test named for the row.
4. **Byte-identity asserted**: journals unchanged and a seeded `Sim` CSV identical, so no
   measurement moved and none could have.
5. Green, `STATUS.md`'s count matching, ceiling back at 6.

---

### P42 — Playtest readiness: the console's fifth seat, the ×5 said out loud, and a played round in a real browser ☐ — **added 2026-08-23, at Nick's direction**

**Goal.** Three things standing between the tree and a playtest with real people, none of them a
rule: **(1)** the console still deals four (P32's leftover, candidate 3 below — this packet
absorbs it); **(2)** the ×5 jackpot settles correctly and **no screen ever explains it** (P26's
leftover, which until now no packet owned); **(3)** nobody — human or agent — has ever verified
the browser table end-to-end *by playing it in a browser*, so this packet has the session do
exactly that with the Claude-in-Chrome tools before a person is asked to.

**Read first.** `RULES.md` §4.1 (the ×5: 7♦/A♠ turn-up, one owner of both partners, $40 a head
at standard stakes) and **§9 #32 (open — do not generalise past the 7♦/A♠ pair; two tests fence
it)**; `MoneyCardRegistry.ConfigurationOf` / `Multiplier(card, owner, ownership)` and
`MoneyOwnership` (the domain already answers everything — this packet only *says* it);
`Settlement.Settle`'s configuration comment; P41's byte-identity procedure in `STATUS.md`'s
session log (this packet repeats it, because build item 2 touches Domain).

**Build.**

1. **The console deals five.** `Console/Program.cs` ~line 339: the seat prompt's
   `.DefaultValue(RoundEngine.MinimumPlayers)` becomes `RoundEngine.DefaultPlayers` — the range
   text and the validator keep the floor (⚠️ candidate 1's interaction: if the floor ever moves,
   the default must not follow it, which is exactly why the two constants exist). A
   `drive-console.py` re-capture of both scripts — ⚠️ **the captures change**: the scripts take
   the seat prompt's default, so every capture becomes a five-seat table. That is the fix
   working, not a regression; the settlement checks in the script are adaptive.
2. **The jackpot fact is carried by the domain, once, and both settlements say it.**
   `RoundEngine.Settle` already stands next to everything it needs; `Settlement` already
   computes `moneyCards.ConfigurationOf(ownership, shoe)` internally. Surface the fact rather
   than re-deriving it anywhere (P35 build item 3's lesson — *ask the domain, do not re-derive*):
   recommended shape is a nullable jackpot owner on `RoundResult` (or on `Win`, whichever reads
   better beside `Jokerless`), filled from the same configuration `Settlement` reads.
   ⚠️ **A watcher cannot compute this** — ownership is partly private until settlement — which
   is exactly why it must ride the result to the browser's `TableEvent.Settled` rather than be
   folded client-side. Then:
   - **Console `ReportSettlement`**: a bonus line in the idiom of the jokerless/deal/streak
     lines — who owned both partners, ×5 apiece, the per-head dollars — and the owned-cards
     column may decorate the pair via `Multiplier(card, owner, ownership)`.
   - **Web `SettlementPanel`**: the same sentence, from the result alone.
   - **The possibility is public from the deal**: when the turn-up *is* the 7♦/A♠ pair, that
     fact is on the table for everybody — a quiet line at the table centre (web) and in the
     round-start narration (console): *the jackpot pair is up; one player owning both partners
     is paid ×5 apiece*. No concealment question arises; it reads off the turned-up cards.
   - ⚠️ **`CardDisplayState` deliberately stays without a ×5 state** (P26's design decision
     holds: no view that draws one card at a time can compute it, and this packet does not make
     one try — the settlement and the table centre are not per-card views).
   - ⚠️ **Do not generalise §9 #32**: the fact is the 7♦/A♠ pair's and no other; the two
     existing fences must still pass untouched.
3. **Fences for item 2.** A settlement test that constructs the jackpot round (the ×5 cases in
   `SettlementTests` are the template — the pair does not turn up by accident at any usable
   rate) and asserts the carried fact; the console line and the web sentence asserted at the
   highest layer the harness reaches for each.
4. 🔥 **Byte-identity re-asserted, because item 2 touches Domain.** Exactly P41's procedure: a
   seeded `Sim` run (journal + CSV) from a HEAD worktree against the changed tree, `cmp` both,
   and replay the HEAD journal under the new tree. The carrier is additive and read by nothing
   in `Sim`, so the assertion should come back byte-identical — if it does not, the packet is
   wrong, not the assertion.
5. 🔥 **A round of UI tests, played by the session itself in a real browser** (Claude in
   Chrome — ⚠️ Nick's standing direction: **the real browser via the extension, never headless;
   if the browser tools are unavailable, say so and report the item blocked rather than routing
   around it**). Launch `dotnet run --project BurmesePoker.Web -- --people 1 --seed 20260823
   --pace 300`, open the lobby, sit down, and **play at least one full round to settlement by
   clicking**, exercising and verifying as a checklist:
   - sit-down and the deal; the turn prompt and taking a card both ways (discard and blind);
   - throwing a card, including that a `closed` card is not a control (§5.1) and a `▲` face-up
     card reads as one (P41);
   - **P41's two features from the other side of the glass**: another seat's face-up chips on
     the felt, and a pile opened through the ▾ disclosure;
   - the *why?* disclosure with hints on; the legend; the settlement panel (and item 2's
     sentence if the seed offers it — it will not; verify that panel by its test instead);
   - the round log, and the reconnect overlay if it happens to show.
   Screenshot the key states (a GIF of the round is welcome but not owed). 🔥 **The deliverable
   is P11's rule: the report says what was actually exercised**, item by item. A defect found
   is fixed in-packet when it is small and written into `STATUS.md`'s notes with a repro when
   it is not — the packet does not grow to swallow it.

**Acceptance.**

1. The console's seat prompt defaults to **five** and still offers 4–6; both `drive-console.py`
   scripts re-captured clean at five seats.
2. A round in which one player owns both partners of a 7♦/A♠ turn-up **says the ×5 out loud at
   both front ends**, fenced by tests that construct that round; §9 #32's fences pass untouched.
3. **Byte-identity asserted across the Domain change**: seeded journal and CSV byte-identical,
   HEAD journal replays.
4. The browser round was **actually played to settlement by the session through the real
   browser**, and the report lists what the checklist exercised, with screenshots.
5. Green, `STATUS.md`'s count matching.

---

### Where the plan stands, 2026-08-23

🔥 **Every packet built from §0 is done, and P34 was the last of them.** What is left is written
up as two packets and four candidates, and the split is deliberate.

✅ **Later the same day: P41 is done too** — rev 31's two visibility rules reached both front
ends, §10 #24 is discharged, §10 is empty again (the standing #7 exception aside) and the
registry's exemption ceiling is back at 6. 🔥 **`P42` — playtest readiness — was added the same
evening at Nick's direction and is the next packet**: the console's four-seat default, the ×5
jackpot display, and a browser round played by the session itself (§5 P42; it absorbs candidate
3 below). **Behind it, P40 is blocked on input only Nick can produce**: vetted Burmese text made with
`docs/translation/PROMPTS.md` against the **rev-31** rulebook — the first-round translations
under `docs/translation/` are rev-30-based and must be re-run. Behind P40 stand the four
candidates below, unchanged; the expert-session candidate now carries **thirteen** §9 rows
played on recorded defaults (#33, #36–#41, #44, #46–#48 and rev 31's #49–#50, all fenced).

**Two packets, both new on 2026-08-23 and neither descended from §0.** They come from a question
Nick asked after P34 shipped — *do we have a rules onboarding document for a new player, and a
definitive guide to strategy?* — and the answer to both was **no**. 🔥 **The gap is real and it is
a gap of *audience*, not of content**: everything a rulebook needs is in `RULES.md` and everything
a strategy guide needs is in `STRATEGY.md`, but both are written for the project rather than for a
player. **`P38` is the rulebook and `P39` is the strategy guide**, specified below.

✅ **Both are done, on 2026-08-23 — P38 first and P39 behind it, as planned.** P38 was the one
that was asked for and needed nothing; P39's guide was worth more once a reader had a rulebook to
be strategic about, and that is the reader it was written for. **The plan is empty: everything
below this line is a candidate, not a packet.**

**Four candidates behind them.** Nothing below is a packet yet — each is a paragraph, in the order
this plan would take them, and whoever takes one writes it up properly first.

1. 🔥 **`RULES.md` §10 #7 — the table sizes nobody can deal.** `RoundEngine.MinimumPlayers` is 4;
   §2 records **2 to 6** as Settled. So `TableRules.For(2)` and `For(3)` are correct, tested and
   **unreachable from a dealt game** — the only Settled rule the program does not play, and the
   oldest entry in §10 that no packet owns. ⚠️ **Not a constant.** Two-handed is a different game:
   series only, **a set is illegal as a meld**, and §7.3's multiplier and §5.1's mutual lock both
   have two-handed readings that are written down and have never been played. **Small in the
   engine, real in the front ends** (a lobby that offers two seats, a console prompt that allows
   them), and it would let `RuleConformance` audit at 2 and 3 for the first time — the caveat
   `SettledRuleCoverageTests`' §2 entry has carried since P30.2.
2. 🔥 **An expert session, which is worth more than any code here.** Eleven §9 rows are being
   **played on a recorded default**, each fenced by a test named for the question (#33, #36, #37,
   #38–#41, #44, #46, #47, #48). **The failing tests would be the change list**, which is the whole
   point of the discipline — and this project's own history says a rules conversation answers past
   the question asked, so the value is not bounded by the list.
3. ⚠️ **The console still deals four.** `Console/Program.cs`'s seat prompt defaults to
   `RoundEngine.MinimumPlayers` rather than `DefaultPlayers`. **One line and a `drive-console.py`
   re-capture**, outstanding since P32 and through five packets. ⚠️ It interacts with candidate 1:
   if the floor moves, the default must not follow it. ✅ **Absorbed into P42 (2026-08-23)** —
   it stops being a candidate the moment that packet ships.
4. ⚠️ **A rung that knows the scoring rules exist.** No rung reads §7.3, §7.4 or §7.5 —
   deliberately, under P15's discipline — so **nothing in this project prices a joker thrown for
   the clean bonus**, and §14's rate is published as a floor for that reason. 🔥 **And §7.5 is the
   first rule a player can act on *between* rounds** (ask to change seats before somebody's third
   win), which no instrument here can measure at all while `RoundsPerGame = 1`. Either is a new
   rung and arrives measured; the second needs the harness to play matches first, which is a
   packet of its own and would reopen §3.8.

⚠️ **Whatever is chosen, `STATUS.md`'s *What is next* is the copy a session reads first** — keep
the two in step.

---

## 6. Cold-start protocol

For picking up in a fresh session with no memory of this conversation.

1. Read `CLAUDE.md` (points here).
2. Read `docs/STATUS.md` — which packet is next and what state the tree is in.
3. Read this document's §2 and §3 — architecture and the settled design decisions. **§0 says
   where the whole thing is heading**; read it before re-planning anything, and before
   deciding a packet is finished with a goal it was supposed to serve.
4. Read the target packet in §5, plus only the `RULES.md` sections it names.
5. Build, run tests, confirm green **before** changing anything.
6. On finishing: tick the packet in `STATUS.md`, note anything surprising, commit.

**Rules of engagement across sessions.**
- `RULES.md` is the only rules authority. Never infer a rule from code.
- Any new rules question goes into `RULES.md` §9 with provenance — do not decide it silently.
- Each packet ends with a green build and green tests. Never leave the tree broken between
  sessions.
- One packet per commit, message prefixed with the packet id (`P3: run candidate generation`).

---

## 7. Risks

| Risk | Mitigation |
|---|---|
| ~~**P3 is the hard packet**~~ — **done 2026-08-18.** Window-based generation with joker substitution was fiddly, and everything downstream depends on it. | The candidate-count tests were an exact, pre-existing spec (5, not the 2023 test's 8 — see `docs/spec/RUN-CANDIDATES.md` §4), and they were written first. P5 may now proceed. |
| ~~Candidate explosion on joker-heavy hands~~ — **contained, measured 2026-08-18.** | Deduplicate by `CardId` set at generation, and choose joker instances as combinations rather than permutations. **Measured worst case: 4,032 run candidates in milliseconds** — thousands, not the hundreds this row assumed, and nowhere near millions. **Sets add almost nothing**: a set holds at most four cards, so no hand can produce many. P4's measured worst case is **639** — nine cards of one rank split (3,2,2,2) across the suits plus all four jokers. The risk is P3's alone. P5 does index candidates by `CardId` — by the *lowest* one in each meld — and evaluates three thirteen-card stress hands in about 100 ms in total. |
| The rewrite stalls half-finished, as in 2023. | Packets are individually shippable and each ends green. P1–P6 are pure domain with no UI dependency, so progress is real even if the console never gets built. **As of 2026-08-18 P0–P6 are all done**, so the entire rules core — cards, melds, the win authority, and money — exists and is tested; what remains is the engine and the front end. |
| ~~**The evaluator in the simulation hot loop**~~ (new 2026-08-18) — **retired 2026-08-18 by P12's measurement pass.** `RoundEngine` calls `TryFindCover` after every discard by every player; P5's ~100 ms for three stress hands is nothing to a human and possibly ruinous across a million rounds. | **Measured end to end at last: a bot-only round costs ~40 ms over 21–30 turns**, so a thousand rounds is under a minute on one core — the goal is not in danger. The live cost has **moved to `PartialCover.Best`**, which a bot calls up to fifteen times a decision against the engine's one. Still **measure before optimising** (§3.7, P12), and still put any speed-up *around* the evaluator rather than inside it — `IsWinning` is the win authority (§3.4) and its answers may not change. A per-turn cache inside the bot is the free win, and touches neither. **P12 measured it and closed the row:** `PartialCover.Best` is 140 µs a hand and `TryFindCover` 91 µs, a round is ~20 ms, and a run does 50–90 rounds a second — 2,000 rounds in 34 s, with nothing optimised. What the measurement did find is that the work is **allocation-bound rather than compute-bound**: eight threads bought 25% under the workstation GC and 70% under the server GC (§3.7 item 4). If throughput ever matters again, attack allocation, and still not inside the evaluator. |
| ~~**A strategy that never improves plays for ever**~~ (new 2026-08-18, **retired the same day**). With the P9 reshuffle, only a declaration ends a round (RULES.md §7.1), so a table of bots that never improve never finishes. | `GreedyBotAgent`'s score is **monotone by construction** — throwing back the card just taken restores the hand exactly, so the best-keep score can never fall — and every round of every seed tried terminated in 21–30 turns. The property is a test (`ABotOnlyMatchTerminatesAndConservesMoney`), not a hope. It remains a real hazard for *future* strategies, which is why P12 owns a turn cap. **P12 built the cap and it has never fired** — even a table of four `SimpleBotAgent`s, which has no tie-break to push it off a plateau, finished all 300 rounds tried in 28.3 turns on average. The cap is insurance, and the only thing that has ever tripped it is an agent written to stall. |
| ~~**Scope growth beyond a playable game**~~ (new 2026-08-18, **all but retired 2026-08-18**). §0 adds three further goals, and the 2023 failure was a half-finished thing nobody could play. | P9, P10 and P11 each ship something *more playable than before*; P12 and P13 are independent of one another after P10 (§4) and can be dropped without stranding anything. The game staying playable at every step is the mitigation. **Three of the four goals are now delivered and the tree has been green at every one of them.** Only P13 is outstanding, and it is the one that can be dropped outright without taking anything with it — **stopping after P11 leaves a finished game, a finished console and a working simulation**, not a half-built anything. ⚠️ **Re-split into five on 2026-08-19, which is where this risk bites hardest** — five sequential sub-packets is the longest unbroken chain in the plan. The mitigation is unchanged and deliberate: **P13.3 and P13.4 each ship something you can look at and play**, and stopping after P13.4 leaves a finished single-player browser game rather than a half-built lobby. |
| **An accessibility standard retrofitted is a rewrite** (new 2026-08-19). Keyboard operability, focus behaviour and colour-independence are decided by how the markup is built, not by how it is styled — and a root-level `@rendermode InteractiveServer` cannot be narrowed later without moving every component. | **§3.11, taken before a single component exists**, and split by how each item is *checked*. Five of the seventeen are mechanical tests — the concealment leak, colour tokens, computed contrast in both themes, real controls, subscription disposal — and they land in **P13.2 and P13.3**, before there is anything to retrofit. The rest are reviewed by playing, the way P11 was reviewed, and the report has to say what was actually exercised. |
| **A fifth project is over-engineering** (new 2026-08-19). `BurmesePoker.Presentation` exists so that one hand view serves two renderers. | The alternatives are named in §2 and both are worse: a Blazor project referencing a Spectre console, or a second implementation of a question `PartialCover` already answers — the exact drift `MeldIndex` and `CoverScore` were extracted to prevent. **The enforcement is the point** (§2), and it is one `LayeringTests` row. If it turns out thin after P13.1, folding it into Domain is a one-session reversal; folding a drifted second implementation back is not. |
| **Blazor Server holds a live circuit per player** (new 2026-08-19). Every interaction is a round trip, and a dropped connection is a dropped game. | Four to six people a table makes the concurrency cost irrelevant; §3.10 chose the model for **concealment**, not for scale, and a JS client over the same server stays available if the feel is ever wrong. The real risk is the *reconnection experience*, which **P13.5 took ownership of** — the client has its own themed reconnect overlay now, and the plan already has an answer to: a timeout is a bot move (P10), and the client must say so rather than freeze. |
| **The synchronous-agent bet is wrong at scale** (new 2026-08-18). §3.6 parks a task per table rather than making the engine resumable. | At four to six players and a handful of tables the cost is a parked task, not a thread. If it is ever wrong, the fix is a resumable engine *behind the same interface* — the agents, tests and simulation loop do not change. Revisit only with a measured problem. |
| ~~**A measured result is really a seating artifact**~~ (new 2026-08-19, **retired the same day by P16**). Turn order is a directed cycle — only the immediately-previous player's discard is available (`RULES.md` §5) — so who sits behind whom is a variable, and P12's rotation holds it fixed rather than varying it. | **Measured, and it is real but small and not directional.** A weaker player anywhere at the table is worth 4–5 points of win rate to you; which *side* of you they sit costs nothing between two thinking strategies (−1.0 ± 2.1 pts) and 9.1 ± 2.1 points only across the random-to-greedy gulf. The size of the artifact in P12's own headline is now known: **the rotation flatters greedy by 1.1 points a seat, 2.2 of the 11.4-point gap.** The mitigation is permanent: `--seating balanced` plays every assignment, and every CSV row names `upstream_strategy` and `downstream_strategy`, so a rotated result and a balanced one can both be quoted and told apart. |
| **Journalling slows the harness** (new 2026-08-19). §3.7 measured the work as allocation-bound, and a per-decision recorder allocates. | P14 keeps two fidelity levels and makes the rich one opt-in, and its acceptance criteria include measuring the throughput cost against P12's recorded 51/85–92 rounds a second rather than assuming it is small. If thin journalling costs more than a few percent it is built wrong. |
| **A round-robin manufactures findings** (new 2026-08-19, P17). *k* strategies is *k(k−1)/2* comparisons, so at 95% intervals roughly one in twenty clears zero by chance — a six-way tournament makes a false rung likelier than not. | **Holm correction reported beside the raw verdict, and the comparison count printed** (P17 acceptance 4). Plus the harness's own null test: **a strategy against itself must measure 1/seats**, which costs one cell and would have caught P16's seating artifact from the inside. |
| **A research rung is worth nothing** (new 2026-08-19). `cautious` cost a packet and measured +0.5 ± 0.55 points; there is no reason to expect the next one to fare better. | **§3.12: the difficulty system does not depend on research succeeding.** P19 finishes it with the rungs that exist today, and **P20–P22 are independently droppable in preference order**. A null result is published rather than buried (P20 acceptance 1), because *why* `cautious` failed was worth more than its number. ⚠️ **P20 is the second such rung and it happened exactly as this row predicted** — `counting` measured `+0.3 ± 1.0` the wrong way, was published as a failure, and its *why* narrowed P21: **better information fed to a decision rule that does not matter is worth nothing**, so the remaining rung has to change the question rather than the answer. **Two of three research rungs returned nothing.** 🔥 **P21 is the third and it did not** — `outs` measured `+3.1 ± 1.0` over `greedy` and beat every rung in the field, which is the first time this row has been wrong. ⚠️ **It does not retire the row, it sharpens it**: the two that failed both refined the *residue* greedy leaves behind, worth about half a point against an instrument that resolves one; the one that paid changed the question asked *before* greedy's tie-break spoke. **Plan a research rung by asking where its key sits, not by how clever the idea is.** ⚠️ And note the cost of success: a rung that separates becomes `Hardest`, re-bases every difficulty level onto itself, and made both the suite and the test tree three times slower. 🔥 **P22 is the fourth and it broke the row's frame rather than confirming or refuting it.** `prospector` did not ask "is this a better way to play rummy" at all — it asked what the *side bet* is worth, so it is judged on `$/round`, and its answer is **a function of the stakes**: nothing at $5/$1 (where it is literally the same player as `outs`), `+7.3 ± 3.3` a round at $5/$40. ⚠️ **The new lesson is about cost rather than about odds.** A rung that measures nothing at the standard stakes still joins the catalog, still costs six head-to-head cells in every future suite run, and took `sim suite` from 1h45 to about 3h15 — **so the question to ask of the next rung is not only where its key sits but whether the standing instrument is the right place to measure it.** |
| **The outs rung makes the programme too slow to run** (new 2026-08-19, P21). A live-outs measure costs a `PartialCover.Best` per value per candidate — ~100× a decision, which turns P12's 34-second run into a quarter of an hour. | **A budget stated in advance: no more than 10× greedy's rounds a second, measured** — over it, the rung is built wrong. The optimisation is the one §3.7 has pointed at since P12: **attack allocation**, and put every speed-up **around** the evaluator, never inside it, because `IsWinning` is the win authority (§3.4) and its answers may not change. |
| **A counting bot sees what the rules conceal** (new 2026-08-19, P20). It is the first strategy to want information beyond its own hand, and `TurnContext` is the concealment rule expressed as a type. | **The information set is decided and asserted before the bot is written**, and the safe default is *only what this seat has actually been shown* — wrong in the direction that makes the bot weak rather than the direction that makes it cheat. ⚠️ Whether a discard pile is inspectable is a **rules** question and goes to §9 and to Mya Lay, not into code. ✅ **Discharged by P20 as designed**: §9 #15 stayed open, the question went to Mya Lay flat, and the bot counts only what it was shown — **12 → 23 cards a round out of 108**. 🔥 **The cost of the cautious default is now a measured quantity rather than a worry**, and it is one of the two reasons the rung returned nothing. If the answer comes back *the piles may be read*, the rung deserves re-measuring before it is written off. |
| ~~**A standing suite that nobody re-runs**~~ (new 2026-08-20, P22; **half-retired 2026-08-20 by P23**). `sim suite` is the join between the code and every number the docs quote, and it grew from 18 s (P17) to 35 min (P20) to 1h45 (P21) to **about 3h15** (P22) — at which point "regenerate it" stops being something a session does casually and `measurements.csv` starts drifting behind the catalog, which is exactly what it did here. | ⚠️ **The drift is recorded rather than hidden**: `docs/STRATEGY.md` §11 says which tables are the six-rung field and why, and P23 owns the catch-up. The structural answers are `--pairs adjacent` for the ladder as well as the dial (P19 built it), and **not measuring a rung in a cell where it is a known duplicate** — six of P22's new cells are `outs` against itself under another name. ⚠️ **What must not happen is a hand-typed shorter field**: P18 and P20 each removed exactly that defect, one layer apart. ✅ **P23 took the structural saving and refused the hand-typed one.** A rung declares its instrument (`BotRung.Ranked`), the ladder field is a *filter* of the catalog, and a test asserts the ladder and the sweep are between them the whole of it — so the six duplicate cells are gone and a rung still cannot be added without being measured. ⚠️ **What is not retired is the wall clock** — though P29 measured it *down* rather than up: the suite is **two and three quarter hours** (9,981 s) with a sixteenth cell added, because `outs` costs 7.0× a `greedy` round rather than 8.2×. `--pairs adjacent` on the ladder remains the only further saving, would cost §3's matrix, and **there is no longer a wall-clock reason to take it**; the drift this row is about is now a red build rather than a paragraph in §11. |
| ~~**The difficulty ladder becomes a lie**~~ (new 2026-08-19, **retired 2026-08-20 by P23**). Levels are calibrated against a ladder that later packets widen, so a published calibration is stale the moment a rung lands. | ✅ **Done.** P23 re-ran the suite and re-fitted the spacing (`hard` 0.5 → 0.4), and **`StandingAnswerTests` asserts that the levels published are the levels offered, at the ε they are offered at** — so a rate moved without a re-run is a red build rather than a document nobody checked. A level not separated from its neighbour is **deleted rather than shipped** (P19 acceptance 1); all three steps survive Holm at 6.8× the half-width or better. 🔥 **The row was right about the mechanism and wrong about the size**: re-basing the dial onto a stronger rung left it *ordered* and only unevenly spaced, which no existing check would ever have caught — the alarm that fires is for inversion, and the failure mode that actually happened was a flat spot. |
| Rules drift as more is recalled. | `RULES.md` provenance tags make revisiting cheap; §9 tracks what is still unrecorded. |
| Three projects is over-engineering. | Noted in §2. The enforcement is the point, but a single project with `IGameObserver` is an acceptable fallback. |
