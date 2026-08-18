# Burmese Poker — Build Plan

**Supersedes `RECONCILIATION-PLAN.md`**, which assumed the existing code was the foundation.
Rules authority remains `RULES.md` (rev 8).

Designed to be worked through across **many separate sessions**. Every packet in §5 is
self-contained: it names what to read, what it depends on, what to build, and how to know
it's done. See §6 for the cold-start protocol.

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

**Solution file format:** `BurmesePoker.slnx`, the XML format that is now the .NET
default (`dotnet new sln` emits it, VS 17.13+ and SDK 9.0.200+ read it). Nick's standing
preference is the newest supported .NET tooling, so prefer `.slnx` over the classic `.sln`
if the question comes up again.

**As built by P0:** `Cards/Rank.cs`, `Cards/Suit.cs`, `Cards/CardColor.cs`,
`Cards/CardText.cs`, `Melds/MeldKind.cs`. Everything else in the tree above is still to
come. The old exe project is renamed `BurmesePoker.Console`; the test project references
**Domain only**, so nothing can accidentally test through the front end.

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

---

## 4. Packet dependency graph

```
P0 ─► P1 ─┬─► P2 ──────────┐
          ├─► P3 ─┐        │
          └─► P4 ─┴─► P5 ──┴─► P7 ─► P8 ─► P9 ─► (P10 optional)
                            P6 ─────┘
```

**P2, P3, P4 are independent of one another** — good candidates for separate sessions in any
order. P6 needs P1 and P2 only.

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
| P10 | Bot opponents and hints (optional) | P9 | L |

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
- Use `Random.Shuffle(Span<T>)` (available in net8.0), **not** `OrderBy(r.Next())` — the old
  shuffle was not a uniform permutation.

**Acceptance tests.**
- Deck has exactly 108 cards; 4 jokers; 8 of each ranked value; all `CardId` distinct.
- `SameValueAs` is true for the two copies of 5♥ and false for 5♥ vs 5♦.
- `==` is false for the two copies of 5♥ (instance identity).
- Shuffling preserves the multiset of cards.
- `DrawFromTop`/`DrawFromBottom` reduce `Count` by one and return distinct ends.
- Drawing from an empty deck throws a **domain** exception, not `InvalidOperationException`
  from `.First()`.

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

**Done when.** Designation is a pure function of the turned-up cards, with no mutation of any
`Card`.

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
  P3 joker-substitution rationale under test; if P3 is wrong this fails.*
- `TryFindCover` melds are pairwise disjoint by `CardId` and cover the hand exactly.
- Evaluation of a 13-card hand completes in well under a second.

**Done when.** All pass. `IsWinning` is the only win authority in the codebase.

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

**Done when.** All pass, including the §4.3 worked example reproduced exactly.

---

### P7 — Round and turn engine

**Goal.** A full round, driven by interfaces, with no I/O.

**Read first.** `RULES.md` §3, §5, §7.1. §3.5 above.

**Build.** `PlayerId`, `PlayerState`, `TableState`, `TurnAction`, `TurnContext`,
`IPlayerAgent`, `IGameObserver`, `RoundEngine`.

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

**Open decisions.** Deck exhaustion and match end are **not** in this packet — see P9. Have
`RoundEngine` throw a clearly-named domain exception on exhaustion for now.

**Done when.** A scripted round runs end to end in tests with no console involvement.

---

### P8 — Console front end

**Goal.** A playable game.

**Read first.** §3.5. Old `UserPromptFactory` (in git at `pre-rewrite`) for Spectre patterns.

**Build.** `SpectrePlayerAgent : IPlayerAgent`, `ConsoleObserver : IGameObserver`,
`CardFormatting` using the salvaged `CardText`, and `Program.cs` wiring.

**Requirements.**
- Hand display sorted by the salvaged rank order, with money-card markers (`($)`, `($$)`).
- Prompts for: draw vs. pick up, which card to discard, claim the money card, declare.
- **Offer "declare" only when `HandEvaluator.IsWinning` is true**, and show the cover found.
- Configurable player count and stakes at startup.

**Acceptance.** Manual: `dotnet run --project BurmesePoker.Console` plays a full round to
settlement. No domain type references Spectre.

**Done when.** A round is playable start to finish and money changes hands.

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

---

### P10 — Bot opponents and hints (optional)

Only worth doing if you want to play solo. `MeldCandidates` plus `HandEvaluator` already give
a greedy bot most of what it needs: keep the largest cover found, prefer discarding cards in
no candidate, never discard an owned money card. A "best cover so far" hint for human players
falls out of `TryFindCover`.

---

## 6. Cold-start protocol

For picking up in a fresh session with no memory of this conversation.

1. Read `CLAUDE.md` (points here).
2. Read `docs/STATUS.md` — which packet is next and what state the tree is in.
3. Read this document's §2 and §3 — architecture and the settled design decisions.
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
| **P3 is the hard packet** — window-based generation with joker substitution is fiddly, and everything downstream depends on it. | The candidate-count tests are an exact, pre-existing spec (5, not the 2023 test's 8 — see `docs/spec/RUN-CANDIDATES.md` §4). Write the tests first. Do not start P5 until P3 is green. |
| Candidate explosion on joker-heavy hands. | Deduplicate by `CardId` set at generation. With only 4 jokers the bound is small; add a test asserting candidate counts stay in the hundreds, not millions. |
| The rewrite stalls half-finished, as in 2023. | Packets are individually shippable and each ends green. P1–P6 are pure domain with no UI dependency, so progress is real even if the console never gets built. |
| Rules drift as more is recalled. | `RULES.md` provenance tags make revisiting cheap; §9 tracks what is still unrecorded. |
| Three projects is over-engineering. | Noted in §2. The enforcement is the point, but a single project with `IGameObserver` is an acceptable fallback. |
