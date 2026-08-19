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
   a game you can start alone, at any hour, with no other people. **P10.**
2. **A console that is pleasant to sit at.** The UI stays terminal-based for a long time, and
   that is fine, *provided* the UX gets deliberate attention rather than being tolerated.
   **P11.**
3. **Strategy simulation at scale** — thousands of games run in parallel to compare ways of
   playing. **P12.**
4. **A multiplayer app**, where a lobby host fills empty seats with AI players. **P13.**

**What they demand of the architecture, and what already satisfies it.** These are stated
here because they change decisions taken *before* the packets that need them.

| Goal | What it needs | Where it stands |
|---|---|---|
| Solo play | A bot behind `IPlayerAgent` | The seam exists; the domain is drivable with no console at all (P7) |
| Console UX | Nothing structural | `Melds/` gives the vocabulary; the only gap is a *scored* cover (§3.4, P10) |
| Simulation | Determinism, no ambient randomness, no I/O, no static state, and **speed** | §3.7. Determinism and purity hold today; **speed is unmeasured and is the one live risk** |
| Multiplayer | A decision on whether agents block | §3.6, taken now rather than discovered later |

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
unit-tested**, by the same reference rule; P8 was verified by driving the binary.

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
   **P12 should pin this with a test**, because it is the kind of invariant a later convenience
   cache would quietly break.
4. ⚠️ **Speed, which is the one unmeasured thing.** `RoundEngine` calls
   `HandEvaluator.TryFindCover` after *every* discard by *every* player — it is how a
   declaration is offered only on a genuine win (P7). P5 measured three deliberately awful
   stress hands at ~100 ms *in total*, which is irrelevant to a human and possibly ruinous
   across a million rounds. **P12 measures before it optimises**, and any optimisation goes
   behind the existing evaluator rather than into it: `IsWinning` is the win authority
   (§3.4) and its answers may not change.

---

## 4. Packet dependency graph

```
P0 ─► P1 ─┬─► P2 ──────────┐                        ┌─► P11  console UX
          ├─► P3 ─┐        │                        │
          └─► P4 ─┴─► P5 ──┴─► P7 ─► P8 ─► P9 ─► P10┼─► P12  simulation at scale
                            P6 ─────┘               │
                                                    └─► P13  multiplayer app
```

**P2, P3, P4 are independent of one another** — good candidates for separate sessions in any
order. P6 needs P1 and P2 only.

**P10 is the fan-out point.** Everything the three end goals need turns out to run through
bots: solo play *is* bots, a discard hint is the same scored search a bot uses, a simulation
is bots playing each other, and a network timeout is a bot taking over a seat (§3.6). After
P10, **P11, P12 and P13 are independent of one another** and can be taken in any order — or
not at all.

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
| P10 | Bot opponents — **solo play** | P9 | L |
| P11 | Console UX pass | P10 | M |
| P12 | Simulation at scale | P10 | L |
| P13 | Multiplayer app | P10 | XL — will be split |

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

**Read first.** `RULES.md` §5, §7.2.

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
> **Amended after the P6 session (2026-08-18) — conservation is already half-proved.**
> Settlement's deltas **sum to zero for any configuration**, which a property test pins over
> 500 randomised rounds. So "money is conserved across the match" reduces to *banking the
> deltas correctly*: `MatchEngine` adds each round's deltas to the running banks and nothing
> else. If a match-level conservation test ever fails, the fault is in the banking, not in
> settlement.

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

> **Hints are P11, not here.** A hint is the same scored cover pointed at a human, and it is a
> presentation decision about when to interrupt somebody — it belongs with the rest of the UX
> pass, drawn next to the card it is advising against in `SpectrePlayerAgent.ChooseDiscard`.

---

### P11 — Console UX pass

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
- **Standings and a between-round summary** — banks carried over (P9), who won what.
- **A settled colour and marker language**, defined once: the `($)` / `($$)` / `★` set from P8
  extended rather than re-invented per screen.
- **`--seed`**, so a strange round can be replayed and reported.

**Acceptance.** Manual, and the packet should say plainly what was played to check it. The one
mechanical check available: **no domain type references Spectre**, and `Program` still refuses
a non-interactive terminal with an explanation rather than a stack trace.

**Done when.** A full match against bots is enjoyable rather than merely possible.

---

### P12 — Simulation at scale

**Goal.** Run thousands of games in parallel and compare ways of playing.

**Read first.** §3.7, which is the contract this packet cashes in. §3.4 for why the evaluator
may not be "optimised" into a different answer.

**Build.**
- **A separate project, `BurmesePoker.Sim`** (recommended) referencing Domain only. Batch runs,
  parallelism and CSV output have nothing to do with the Spectre front end, and a fourth
  project keeps both honest. *Alternative if four projects feel heavy: a `--sim` mode inside
  `BurmesePoker.Console`.* It works, but it puts a throughput concern inside the interactive
  binary, and §2's argument against "it'll be fine in one project" applies again.
- **Seeding**: one master seed, per-game seeds derived from it, recorded with every result. A
  run must be **exactly reproducible from its seed**, which is what makes a surprising result
  investigable rather than folklore.
- **Parallel execution** over games, with per-strategy stats: win rate, money per round, turns
  to a declaration, how often the deck is exhausted (P9's reshuffle), how often the turned-up
  money card is claimed.
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

---

### P13 — Multiplayer app

**Goal.** A host opens a lobby, people join, and the host fills the empty seats with AI
players.

**Read first.** §3.5 and **§3.6** — the concurrency decision this packet is built on. §0.

**Expect to split this.** It is the only XL packet in the plan, and it should be broken into
its own P13.x list at the point it is next rather than pretended to be one session's work.

**Shape, as decided in §3.6.**
- **One table is one task**, running a `MatchEngine` that blocks exactly as it does today. The
  engine, the tests and the whole scripted-round apparatus are untouched.
- **`RemotePlayerAgent : IPlayerAgent`** blocks on a channel until the player's move arrives.
- ⚠️ **A timeout is a bot move** (P10) — "they dropped out, play the seat for them" is
  precisely what a bot does, which is why P10 comes first. It also means a disconnection
  degrades the game rather than ending it.
- **`IGameObserver` fans out per connection, filtered.** P8 established that filtering private
  information is the front end's job and the domain narrates everything; over a wire that
  becomes a **security property rather than a courtesy** — a client must never be *sent* what
  it may not see, so the filtering happens server-side, per viewer, before anything is
  transmitted.
- **The lobby is not a domain concept.** Seating, names, who is a bot and who is a person are
  all decided before `RoundEngine` is constructed — as `Program` already does today.

**Done when.** Two people and two bots play a round over a network.

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
| ⚠️ **The evaluator in the simulation hot loop** (new 2026-08-18). `RoundEngine` calls `TryFindCover` after every discard by every player; P5's ~100 ms for three stress hands is nothing to a human and possibly ruinous across a million rounds. | **Measure before optimising** (§3.7, P12), and put any speed-up *around* the evaluator rather than inside it — `IsWinning` is the win authority (§3.4) and its answers may not change. Nothing before P12 needs it. |
| **Scope growth beyond a playable game** (new 2026-08-18). §0 adds three further goals, and the 2023 failure was a half-finished thing nobody could play. | P9, P10 and P11 each ship something *more playable than before*; P12 and P13 are independent of one another after P10 (§4) and can be dropped without stranding anything. The game staying playable at every step is the mitigation. |
| **The synchronous-agent bet is wrong at scale** (new 2026-08-18). §3.6 parks a task per table rather than making the engine resumable. | At four to six players and a handful of tables the cost is a parked task, not a thread. If it is ever wrong, the fix is a resumable engine *behind the same interface* — the agents, tests and simulation loop do not change. Revisit only with a measured problem. |
| Rules drift as more is recalled. | `RULES.md` provenance tags make revisiting cheap; §9 tracks what is still unrecorded. |
| Three projects is over-engineering. | Noted in §2. The enforcement is the point, but a single project with `IGameObserver` is an acceptable fallback. |
