# Reconciliation Plan — SUPERSEDED

> ⚠️ **Superseded by `BUILD-PLAN.md`.** This plan assumed the 2023 implementation was the
> foundation and should be patched. That assumption was reconsidered: `BUILD-PLAN.md` §1
> decides to **rewrite the engine** and salvage only the lookup tables.
>
> Kept for its still-valid analysis — the defect list, and the exact-cover finding in §1,
> which carries over unchanged into `BUILD-PLAN.md` §3.4. **Do not work from the phases
> below.**

Bringing the implementation in line with `RULES.md` (rev 4).

**Status of inputs:** all rules conflicts are resolved. Two rules remain unrecorded
(deck exhaustion, match end) and are isolated to Phase 4. Nothing else blocks.

---

## 1. The finding that reshapes the work

Confirming that **play is fully concealed** (§6.3) both *removes* a subsystem and *replaces*
another.

**Removed.** Since melds never reach the table, `Table` needs no meld state, no meld
ownership, and no lay-off legality. That is a large chunk of a rummy engine that simply
does not need to exist here.

**Replaced — and this is the important part.** The game's central question becomes:

> *Can these 13 cards be partitioned into disjoint valid melds, using each card exactly once?*

This is an **exact-cover** problem. `CardPlaysFactory` currently answers a **different**
question — *what melds could be formed from this hand?* — and its output deliberately
overlaps: the same joker is offered to a diamond run and a heart run, and every sub-run of
a longer run is emitted separately.

**That overlap is not a bug.** As a *candidate generator* feeding an exact-cover search it
is exactly right — the search enforces disjointness itself, by card instance. What the
factory needs is not a rewrite of its purpose, but **correctness and completeness as a
candidate generator**, plus a new component on top of it.

Two existing defects are re-scoped by this:

- **D4 (duplicate-copy blindness)** is promoted from cosmetic to a **correctness bug**.
  `FirstOrDefault` always picks the same copy of a duplicated rank, so if you hold two 3♦
  only one candidate run is generated. The cover search needs candidates over *specific
  instances* or it will miss winning hands.
- **D3 (the empty `CalculatePermutationsRecursively`)** turns out to be **load-bearing**.
  Its purpose — visible in the failing test's comments — is to emit runs where a joker
  *substitutes for a card you actually hold*: from `2,3,4,J` it should also yield `2,3,J`
  (joker as the 4) and `2,J,4` (joker as the 3). Those look redundant until you need an
  exact cover — using the joker **frees the real 4 for another meld**. Without them the
  evaluator will reject hands that are genuinely winning.

---

## 2. Phases

Ordered so that each phase leaves the build green and the game no worse than before.

### Phase 0 — Land the work in progress

No behaviour change. Currently git sees three deletions plus an untracked directory.

- [ ] Stage and commit the `Factories/` → `Logic/Factories/` move so git records it as a
      rename rather than delete+add.

### Phase 1 — Verified defects, independent of rules

Each is self-contained and testable. Do these first; they make everything after safer.

- [ ] **Remove the ace wrap** — `CardPlaysFactory.MakeRunsFromSuit`. Delete the
      `Rank == Ace → Two` branch. Fixes the illegal `K-A-2` meld **and** the verified
      infinite loop (D1) in one change.
      *Then* seed low-ace runs explicitly, since `Ace = 12` in the enum means `A-2-3` no
      longer forms naturally. **Regression test: a hand of A–K in one suit must terminate.**
- [ ] **Delete dead code** rather than fixing it — `Common.DetermineCardRankSuitFromString`
      (which contains the always-throwing `[^0]`, D5), `Common.CardSuitFromChar`,
      `CardColorFromString`, `CardSuits_All`, `CardRankCodes_All`, and `Table.AllCards`.
      None have callers. If string parsing is wanted later it should be rebuilt against a
      real requirement.
- [ ] **Replace the shuffle** — `Deck.Shuffle()` uses `OrderBy(random.Next())`, which is not
      a uniform permutation. Use `Random.Shared.Shuffle(span)` (available in net8.0).

Existing tests must still pass: neither test involves an ace, so expectations are unchanged
(3 and 8).

### Phase 2 — Model corrections

Small, mechanical, driven directly by `RULES.md` §10.

- [ ] **Delete `Player.Score`** (§7.2). No deadwood penalty exists; money is the only ledger.
      Leaving the field implies a scoring model the game does not have.
- [ ] **Claimed money card: move, don't clone** (`GameMaster.HandleFirstTurn`, §4.5).
      Currently constructs a copy and leaves the original on the table, creating a 109th card.
- [ ] **Set `MoneyCardOwner` during the deal** (`Table.DealCardsToPlayers`, §4.4) — pending
      confirmation of open question #3. Dealt cards arrive blind from the deck, the same
      principle that governs draws.
- [ ] **Configurable player count, 4–6** (§2.1). Replace the five hardcoded names with a
      roster supplied at setup; keep the current names as the default.
- [ ] **Add stakes** (§4.3): a round value and a money card value, fixed once per game.
      No such concept exists today.

### Phase 3 — Complete the rules engine

The substantive work. Everything here is unit-testable without a console.

- [ ] **Fix D4 — generate candidates over card instances.** Where a rank is duplicated,
      emit a candidate per distinct copy. Deduplicate identical candidates by instance set,
      not by display value.
- [ ] **Implement joker substitution properly (D3/D2).** Rather than restoring the
      greedy-walk-plus-post-hoc-permutation shape, generate run candidates directly: for
      each (suit, start rank, length) window, choose for each position whether it is filled
      by the real card or by a specific joker instance. This subsumes both the current walk
      and the missing permutations, and removes the orphaned `alternativePermutations` list
      (D2) entirely.
      **When reimplementing, store copies** — the deleted version did
      `results.Add(new CardPlay(type, currentPlay))`, aliasing a list mutated by
      backtracking. Use `[.. currentPlay]`.
      **Target: the existing `CardPlays_Runs_HappyPath_Jokers` test passes at 8.**
- [ ] **Implement `MakeSetsFromHand`** (§6.2) — same rank, **all suits distinct**, so a set
      caps at 4 cards. Jokers substitute; the same instance-level care applies.
- [ ] **Build the exact-cover evaluator.** New component, the missing centre of the game:
      `bool IsWinningHand(IReadOnlyList<Card> hand)` — does a set of disjoint candidate
      melds cover all 13 cards exactly? Recursive backtracking over candidates, pinned to
      the lowest uncovered card to avoid re-exploring permutations of the same cover. At 13
      cards the search is tiny; no need for anything clever.
- [ ] **Wire `WinConditionObserver` to it** (§7.1), replacing `return false`.

### Phase 4 — Complete the game

- [ ] **Extract console I/O from `GameMaster`.** Narration is currently raw
      `Console.WriteLine` interleaved with game logic, so the loop cannot be tested at all.
      Put output behind an interface; `UserPromptFactory` already models the input half.
      **Do this first in the phase** — it makes everything below testable.
- [ ] **Declaration flow** (§7.1): after taking a card the player holds 14; if 13 of them
      form a valid cover, they may declare and discard the 14th.
- [ ] **Settlement engine** (§7.2): flat round value from every other player to the winner,
      then pairwise money-card settlement by **owner**, not holder.
- [ ] **Multi-round loop**: `StartGame` currently sets `gameIsOver = true` after one round.
      Money cards must be re-designated each round — note `MarkDeckAndPlayerMoneyCards` is
      **not idempotent** (§4, technical D-list) and will escalate everything to
      `DoubleMoneyCard` on a second call. Reset status before re-marking.
- [ ] **Deck exhaustion** — ⚠️ **needs rule, open question #1.** Currently throws from
      `Deck.DrawFromTop()` on `.First()`. Also decides whether per-player discard piles need
      to become one reshufflable pile (#9).
- [ ] **Match end** — ⚠️ **needs rule, open question #2.** Money target, fixed rounds, or
      last player standing.

### Phase 5 — Test coverage

Current coverage is two tests, both on run generation.

- [ ] Meld generation: runs (ace low, ace high, no wrap, jokers, duplicate copies), sets
      (distinct-suit rule, jokers).
- [ ] Evaluator: known winning and losing 13-card hands, including a hand that only wins if
      a joker substitutes for a held card — the D3 case.
- [ ] Money cards: designation by exact value, doubling on 7♦/A♠, non-idempotent re-marking,
      ownership on deal vs. draw vs. pickup.
- [ ] Settlement: worked example from §4.3 — 5 players, $5/$1, winner takes $20, a two-money-card
      owner takes $8.
- [ ] Regression: full-suit hand terminates (D1).

---

## 3. Decisions still needed

| # | Decision | Needed by | Default if unanswered |
|---:|---|---|---|
| 1 | What happens when the deck runs out? | Phase 4 | Reshuffle discards into a new deck |
| 2 | What ends a match? | Phase 4 | Play a fixed number of rounds |
| 3 | Does the deal confer money-card ownership? | Phase 2 | Yes |
| 5 | Money-card claim: once per game or per round? Whose approval? | Phase 4 | Once per game, no approval |
| 6 | Exception to the mandatory discard | Phase 4 | No exception |
| 7 | Pure sequence required? | Phase 3 | No |
| 8 | Max jokers per meld | Phase 3 | Unlimited |

Numbering follows `RULES.md` §9. Only 1 and 2 have no safe default worth shipping.

---

## 4. Sequencing notes

- **Phases 1–3 need no further rulings.** That is roughly all of the engine work, and it can
  proceed immediately.
- **Phase 1 is worth doing on its own merits** even if the project stalls again — it removes
  a hang, an always-throwing method, and a biased shuffle.
- **Phase 3 is the real project.** The evaluator is the piece the game has never had, and it
  is what makes `WinConditionObserver` meaningful.
- **The Phase 4 I/O extraction is a prerequisite, not a cleanup.** Without it the game loop
  stays untestable and every later change is verified by hand.
