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
the player before you does not decide your game — see "What P16 found".** ⚠️ **P13 is the only
outstanding packet in the plan.**

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

**Three of the four are done, and P12 was the one that could have demanded architecture.** It
did not: the harness is a fourth project that references Domain and asks it for nothing new.
**P11 asked for nothing either** — a whole UX pass, including a hint that has to agree with how
the bots play and a pause that must not exist in the domain, went in as presentation over the
seams that were already there. **Only P13 is left, and it is the only one that has ever looked
like it might change the shape of things.**

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
`CsvReport.WriteTo` already split. **By P16:** `{SeatingPlan, Measurement, NeighbourExperiment,
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
16. ✅ **DONE IN P13.5 — reconnection is part of the UX, not an error path.** A dropped circuit
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
```

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
**P13 is now the only outstanding packet — the only one that would change the architecture,
and the only one that is purely optional.**

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

#### P13.6 — The lobby, and a second person

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
| Rules drift as more is recalled. | `RULES.md` provenance tags make revisiting cheap; §9 tracks what is still unrecorded. |
| Three projects is over-engineering. | Noted in §2. The enforcement is the point, but a single project with `IGameObserver` is an acceptable fallback. |
