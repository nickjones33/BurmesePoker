# Spec — Run candidates (packet P3)

Worked specification for the hardest packet. Derived from the 2023 test
`CardPlays_Runs_HappyPath_Jokers`, whose inline comments enumerate the intended result.

Rules: `RULES.md` §6.1. Design: `BUILD-PLAN.md` §3.4.

---

## 1. A distinction the old code did not make

**The reference hand:** `2♦ 3♦ 4♦ + one red Joker`.

The 2023 test asserts **8** results. Grouped by the card each run starts with, its comments read:

| Starts with | Runs |
|---|---|
| `2♦` | `2,3,4` · `2,3,J` · `2,J,4` · `2,3,4,J` |
| `3♦` | `3,4,J` |
| `4♦` | none |
| `J` | `J,2,3` · `J,3,4` · `J,2,3,4` |

That is 8 — but **only 5 distinct sets of cards**. The hand holds four cards, so there are
just four 3-card subsets and one 4-card subset available.

The 8 arises because a joker can stand for **different ranks in the same set of cards**:

| # | Card set | Joker stands for | Sequence |
|---|---|---|---|
| **S1** | `{2♦,3♦,4♦}` | — | `2-3-4` |
| **S2** | `{2♦,3♦,J}` | `4♦` | `2-3-4` |
| | | `A♦` | `A-2-3` |
| **S3** | `{2♦,4♦,J}` | `3♦` | `2-3-4` |
| **S4** | `{3♦,4♦,J}` | `5♦` | `3-4-5` |
| | | `2♦` | `2-3-4` |
| **S5** | `{2♦,3♦,4♦,J}` | `5♦` | `2-3-4-5` |
| | | `A♦` | `A-2-3-4` |

**5 card sets · 8 interpretations.** Both numbers are correct answers to different questions.

## 2. Which number P3 must produce

**P3 produces the 5 card sets.** Deduplicate by `CardId` set.

The consumer is the exact-cover search (P5), and cover cares only about **which cards a meld
consumes**. `{2♦,3♦,J}` occupies the same three cards whether the joker is playing the 4 or the
ace — as a cover element it is one thing, not two. Emitting 8 would make the search explore
duplicate branches for no gain.

> ⚠️ **This corrects an inconsistency in an earlier draft of `BUILD-PLAN.md`**, which asked P3
> both to deduplicate by `CardId` set *and* to return 8 for this hand. Those cannot both hold:
> deduplicating by card set yields 5.

**Keep one representative interpretation per meld** for display — when a player declares, the
UI should be able to show that the joker is playing the 4♦. Store it on `Meld`, but never let
it affect identity or deduplication.

## 3. Why joker substitution is load-bearing

**S3** is the case that justifies the whole feature. `{2♦,4♦,J}` plays the joker as the 3♦
*while the real 3♦ is still in the hand* — freeing that 3♦ for a different meld.

Without S3-style candidates, the evaluator rejects hands that genuinely win. This is exactly
what the abandoned `CalculatePermutationsRecursively` was reaching for, and why restoring it
is not cosmetic.

## 4. Acceptance criteria for P3

*All ticked by the P3 session (2026-08-18); see `BurmesePoker.Tests/Melds/RunGeneratorTests.cs`.*

Given `2♦ 3♦ 4♦ + red Joker`:

- [x] Exactly **5** candidates, matching S1–S5 by `CardId` set.
- [x] `{2♦,4♦,J}` is present — the substitution case (§3).
- [x] Each candidate carries a valid interpretation: a contiguous same-suit rank sequence
      where every joker maps to a rank not otherwise supplied by the run.
- [x] No candidate appears twice by `CardId` set.

Given `2♦ 3♦ 4♦ 5♦` (no jokers):

- [x] Exactly **3** candidates — `2-3-4`, `3-4-5`, `2-3-4-5`. *Ports the passing 2023 test.*

Ace handling (`RULES.md` §6.1):

- [x] `A♦ 2♦ 3♦` → valid (ace low).
- [x] `Q♦ K♦ A♦` → valid (ace high).
- [x] `K♦ A♦ 2♦` → **no** run containing all three (no wrap).

Robustness:

- [x] `A♦` through `K♦` — all 13 ranks of one suit — **terminates**, returning finitely many
      candidates. *Regression test for the verified infinite loop; the 2023 code hangs here.*
      The count is **76**: 77 windows (11 ace-low, 66 ascending), of which two — `A-2-…-K`
      and `2-…-K-A` — are the same thirteen cards read two ways, and so one candidate.
- [x] Two copies of `3♦` with `2♦ 4♦` → candidates exist using **each copy distinctly**
      (defect D4), and are not deduplicated into one.
- [x] A joker-heavy hand keeps the candidate count bounded. **Measured: 4,032** for
      `2♦…10♦` plus all four jokers, the worst case a 13-card hand can reach — thousands, not
      the "hundreds" this line estimated before P3 measured it, and nowhere near millions.
      The number is a property of the hand, not of the algorithm: a brute-force enumeration
      of all 8,192 subsets agrees on it card set for card set (§6).

## 5. Generation approach

Per `BUILD-PLAN.md` §3.4 — generate **by window**, not by greedy walk:

```
for each suit:
  for each start rank, for each length >= 3:
    for each assignment of positions to (held real card | specific joker instance):
      if every position is satisfied -> emit candidate
```

Then deduplicate by `CardId` set, keeping the first interpretation seen.

Ace handling is explicit, never arithmetic: a window either ascends within `[2..14]` (ace high,
final position only) or begins with the ace treated as `1` followed by `2,3,…`. A window may
never pass through the ace in the middle.

---

## 6. What P3 actually built (2026-08-18)

Implemented as `Domain/Melds/{Meld, MeldSlot, RunGenerator}`, exactly the window formulation
of §5. Four things are worth carrying forward — the first two are corrections to this
document, the last two are for **P4** and **P5**.

**Joker instances are chosen as a set, not placed as a permutation.** The recursion fills
positions left to right and may only take jokers in **ascending index order**. Two jokers
swapped between the same two positions are the same set of cards, so enumerating
permutations would generate every candidate `k!` times and then throw the duplicates away.
With four jokers that is a 24× saving on the worst case, and it is why the generator returns
4,032 candidates in milliseconds rather than tens of thousands of dead branches.

**De-duplication needs no custom comparer.** `HashSet<CardId>.CreateSetComparer()` gives
structural set equality off the shelf, so the seen-set is
`HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer())`. **P4 should do the same.**

**`Meld` is shared, and `MeldSlot` carries the interpretation.** A slot is
`(Card Card, Rank PlaysAs, Suit InSuit)` — for a real card those are its own rank and suit,
for a joker they are what it stands in for. This covers sets as well as runs: the joker in
`9♥ 9♠ 🃏` plays as, say, `9♦`, and P4 should record that rather than inventing a second
shape. `Meld` validates only what is universal (three cards or more, no card used twice);
run and set legality belong to the generators. Identity is `Meld.CardIds`, and
`Meld.Overlaps` is there for P5.

**All-joker melds are emitted.** `🃏🃏🃏` satisfies a three-card window with nothing real in
it. That follows from `RULES.md` §9 #8's *unlimited jokers* recommendation, which rev 10
widened to name this case explicitly. **P4 should match it** — a set of three jokers, for
consistency — and P5 must not assume every meld contains a ranked card.
