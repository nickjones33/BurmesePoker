# Burmese Poker — Technical Rules Specification — HISTORICAL

> ⚠️ **HISTORICAL — this describes code that no longer exists.** It specifies the abandoned 2023
> implementation and the places where that code departed from the agreed rules. That code was
> deleted; it survives in git history as the tree at `79d86bd` (⚠️ **not** at a `pre-rewrite`
> tag — no such ref exists; see `BUILD-PLAN.md` §5 P55). **Nothing here describes what the
> program does today**, and no rule may be inferred from it.
>
> Kept for its defect analysis, which is what the rewrite was planned against. What the engine
> does now is `docs/BUILD-PLAN.md` and the code; what the rules *are* is `docs/RULES.md`.

**Status:** implementation spec. **`RULES.md` is the canonical rules source** — this
document describes what the *code* does and where that departs from the agreed rules.
Where the two disagree, `RULES.md` states the rule and this document states the defect. In short: no published ruleset for this game exists, so this document specifies
**what the code actually does**, flags where that departs from documented 13-card rummy,
and lists what remains undecided.

Tags used throughout:
**[IMPL]** implemented and verified · **[STUB]** declared but empty ·
**[ABSENT]** not present at all · **[⚠]** defect or departure from standard rummy ·
**[?]** rule undecided

---

## 1. Card model

`Models/Card.cs`

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Per-instance reference identity. Never used in rule logic. |
| `Rank` | `CardRank` | |
| `Suit` | `CardSuit` | Jokers carry `CardSuit.Joker`, not a real suit. |
| `Color` | `CardColor` | Derived from suit for normal cards; explicit for jokers. |
| `MoneyCardStatus` | enum | `NotMoneyCard` / `MoneyCard` / `DoubleMoneyCard` |
| `MoneyCardOwner` | `Player?` | Who is credited. `null` = unowned / belongs to the deck. |

Two constructors: `(rank, suit)` for normal cards, and `(rank, color)` for jokers only —
the latter throws `IndexOutOfRangeException` for any non-joker rank.

### Identity semantics — critical

The game uses **two decks**, so value-identical cards coexist. Two distinct notions:

- **Reference identity** — `Id`, or object reference. Distinguishes the two 9♥.
- **Value identity** — `ValueEqualTo(card)` ⇔ same rank ∧ same suit ∧ same color.

All rule logic must use `ValueEqualTo`. It is currently used in exactly one place
(`Table.MarkDeckAndPlayerMoneyCards`).

### Two rank orderings — do not conflate

1. **Enum ordinal** (`CardRank`): `Two=0 … King=11, Ace=12, Joker=13`. Arithmetic like
   `Rank + 1` in run-building depends on this. **Ace is high**, and Joker sorts above Ace.
2. **`Common.CardRankOrder`**: same sequence, returned explicitly. Used for *display*
   sorting via `Card.RankOrder` and `Player.PrintOrderedHand`.

They agree today. Nothing enforces that they stay in sync. **[⚠]**

---

## 2. Deck composition **[IMPL]**

`Logic/Factories/CardFactory.cs` → `MakeDecks(2)`, called from `Table.SetupDeck()`.

One deck = 4 suits × 13 ranks + 1 red joker + 1 black joker = **54**.
Game deck = **2 × 54 = 108 cards** (104 ranked + 4 jokers).

Shuffled by `Deck.Shuffle()`, which returns a **new** `Deck` ordered by `Random.Next()`
rather than mutating in place.

**[⚠]** `OrderBy(x => random.Next())` is a weak shuffle — `Random.Next()` can collide,
and this is not a uniform permutation. Fine for a side project; wrong for money play.

---

## 3. Setup sequence **[IMPL]**

`GameMaster.InitializeTable()`, in strict order:

1. `SetupDeck()` — build 108 cards, shuffle.
2. `InitializePlayers()` — **5 hardcoded players**, **100 money each**, order randomized.
3. `DealCardsToPlayers(13)` — 13 rounds of one-card-each (65 cards dealt, 43 remain).
4. `SetCurrentRoundMoneyCards()` — draw **bottom** card, then **top** card. Deck: 41.
5. `MarkDeckAndPlayerMoneyCards()`.

Order matters: money cards are drawn **after** the deal, so a player may already hold a
copy of a card that is about to become a money card.

---

## 4. Money card algorithm **[IMPL]**

### Permanent money cards

Assigned in the `Card(rank, suit)` constructor, unconditionally:

```
7♦  →  MoneyCard
A♠  →  MoneyCard
```

Both copies (two decks) get it. **[?]** Why these two specifically is unrecorded.

### Round money cards

`Table.MarkDeckAndPlayerMoneyCards()` scans `Deck ∪ AllPlayerCards ∪ CurrentRoundMoneyCards`.
For each card value-equal to either turned-up money card:

```
if (!card.IsMoneyCard)  card.MoneyCardStatus = MoneyCard;
else                    card.MoneyCardStatus = DoubleMoneyCard;
```

Consequences, all intentional-looking:

- Marking is **by value**, so *both deck copies* of a money card are marked.
- A turned-up 7♦ or A♠ escalates to `DoubleMoneyCard`.
- The turned-up cards themselves are in the scan set, so they are marked too.

Consequences that look accidental:

- **[⚠] Not idempotent.** A second call escalates every existing `MoneyCard` to
  `DoubleMoneyCard`. Currently called once, at init — so any per-round re-marking must
  reset status first.
- **[⚠] Discards are invisible.** `AllCards` covers deck + hands + money cards, never
  `Player.Discard`. Once discarded, a card can never be re-marked.
- **[⚠] No triple.** If both turned-up cards are value-equal to each other (possible with
  two decks), matching cards are marked once — a single `MoneyCard`, not double.

### Ownership

`MoneyCardOwner` is set when a player **draws from the deck** (`GameLoop.cs:65`)
but **not** when a player **picks up a discard** (`GameLoop.cs:72`). **[⚠] [?]** —
deliberate (only fresh draws earn credit) or an oversight? This needs a ruling.

---

## 5. Turn structure **[IMPL]**

`StartRound()` iterates players by index; `StartTurn()` branches on whether a previous
player exists. `previousPlayer` is `null` **only** when `RoundNumber == 0 && i == 0` —
i.e. exactly once per game.

### Opening turn — `HandleFirstTurn`

1. Offer the player the **top** money card (`CurrentRoundMoneyCards.Last()`).
2. If accepted: construct a **clone** with the same rank/suit and `MoneyCardOwner = null`,
   add the clone to hand. The original stays in `CurrentRoundMoneyCards`. **[⚠] [?]** —
   this creates a 109th card. A physical game would move the card, not copy it.
3. If declined: draw from deck, `MoneyCardOwner = currentPlayer`.
4. Discard.

**[?]** `//TODO - ask sorta-previous player for permission` — the claim was meant to
require assent from another player. **[ABSENT]**

### Normal turn — `HandleNormalTurn`

1. Show `previousPlayer.Discard.Last()`.
2. Prompt: `Draw` or `Pickup`.
   - `Draw` → `Deck.DrawFromTop()`, set `MoneyCardOwner`.
   - `Pickup` → move the card from previous player's discard to hand. Owner untouched.
3. Discard.

### Discard — `HandlePlayerDiscard`

Prompts for a card, then resolves it against the hand by
`Rank ∧ Color ∧ Suit` via `FirstOrDefault`. **[⚠]** With two decks this picks an
**arbitrary copy**. Since `MoneyCardOwner` is per-instance, the player may discard the
owned copy while retaining the unowned one, or vice versa. Resolution should be
instance-aware.

**[⚠]** Each player keeps a **private** `Discard` list — there is no shared discard pile.
Only the immediately-previous player's top discard is ever reachable. **[?]** Is that the
real rule, or should there be one communal pile?

**[⚠]** No deck-exhaustion handling. `DrawFromTop()` on an empty deck throws from
`.First()`. With 41 cards and 5 players, the deck runs dry after ~8 rounds.

---

## 6. Run generation **[IMPL, incomplete]**

`Logic/Factories/CardPlaysFactory.cs`.

### Entry

`MakeAllPossiblePlaysFromHand(hand)` = `MakeRunsFromHand(hand)` ∪ `MakeSetsFromHand(hand)`.

**[⚠] Never called by the game.** Only the tests call it. The rules engine is not wired
into `GameMaster` at all.

### Partitioning — `MakeRunsFromHand`

Splits the hand into jokers and four suit lists (ordered by rank), then calls
`MakeRunsFromSuit(suit, jokers)` for each suit where `suitCount + jokerCount >= 3`.

Jokers are passed to **every** suit independently. **[⚠]** A single joker can therefore be
consumed by a diamond run *and* a heart run in the same result set — the enumeration
produces plays that cannot all be played simultaneously. Acceptable if the caller treats
the output as "candidate plays," but it must not be treated as a partition of the hand.

### Run walk — `MakeRunsFromSuit`

Two passes, identical inner loop:

**Pass A — anchored on a real card.** For each card in the suit, greedily extend:

```
next = suit.FirstOrDefault(c => c.Rank == last.Rank + 1)
if next == null:
    if last.Rank == Ace:      next = suit.FirstOrDefault(c => c.Rank == Two)   // WRAP
    elif jokers remain:       next = jokers.RemoveFirst()
    else:                     stop
emit CardPlay(Run) whenever length >= 3
```

**Pass B — anchored on a joker.** For each joker × each suit card, seed
`[joker, card]` and run the same loop with that joker excluded.

Returns `runsStartingWithNonJokers ∪ runsStartingWithJokers`.

### Rank adjacency and the ace **[⚠ departs from standard rummy]**

Because `Ace = 12` and `King = 11`, `King + 1 == Ace` — so **K-A** is natural adjacency.
The explicit ace branch then adds **A → 2**.

Net effect: rank order is a **cycle**, `2-3-…-K-A-2-…`. This permits both
**A-2-3** and **K-A-2**.

Documented 13-card rummy allows the ace high *or* low but **never wrapping** — `K-A-2` is
illegal there. **[?] Confirm whether your family played it wrapping.** If yes, record it as
a deliberate house rule; if no, remove the ace branch and seed low-ace runs explicitly.

Note the ace branch is only reachable by accident: `Ace + 1 == Joker`, and jokers carry
`CardSuit.Joker` so they never appear in a suit list — hence the lookup returns `null` and
falls through to the ace check. Fragile but correct today.

### Emission semantics

The run is emitted **every time** it reaches length ≥ 3, so a 5-card run emits its
3-, 4-, and 5-card prefixes as separate `CardPlay`s. Intentional (all legal plays), but the
result is prefix-closed and contains no maximal-run marker.

---

## 7. Known defects

### D1 — Infinite loop on a full-suit hand **[⚠ verified]**

If one suit contains all 13 ranks, the walk cycles `…K → A → 2 → 3 → …K → A → 2…` forever,
appending to `potentialCardsInPlay` until memory is exhausted. **Empirically confirmed:**
a hand of A–K of diamonds hangs `MakeAllPossiblePlaysFromHand` indefinitely (killed at 60s).

Reachable in play only as a perfect 13-card flush — but trivially reachable by any caller
that passes a larger card collection (e.g. melded table cards). A run must terminate once
it has consumed 13 ranks, or the ace-wrap must be forbidden from re-entering a used rank.

### D2 — Joker permutations computed then discarded **[⚠]**

`MakeRunsFromSuit` builds `alternativePermutations` and never returns it — the return
statement lists only the two run collections. Even a correct
`CalculatePermutationsRecursively` would have its output silently dropped.

### D3 — `CalculatePermutationsRecursively` is empty **[STUB]**

Body removed mid-rewrite. Intended purpose (inferred from the call site and test comments):
given a clean run, enumerate the variants where a joker **substitutes for** a real card
already in the run — e.g. `2,3,4` with a joker available also yields `2,3,J` and `2,J,4`.

The version in git history was itself unfinished, and its base case had an aliasing bug:

```csharp
results.Add(new CardPlay(CardPlayType.Run, currentPlay));   // stores a live reference
```

`currentPlay` is mutated by backtracking, so every stored play would alias the same list.
**Reimplement with a copy** — `[.. currentPlay]`.

This is the sole cause of `CardPlays_Runs_HappyPath_Jokers` failing 6 ≠ 8. The test's
inline comments enumerate the 8 intended plays.

### D4 — Duplicate-copy blindness **[⚠]**

`FirstOrDefault` always selects the same copy of a duplicated rank, so with two decks:
- alternate-copy runs are never enumerated;
- two identical starting cards produce two identical `CardPlay`s (duplicates in output).

### D5 — `DetermineCardRankSuitFromString` always throws on jokers **[⚠ verified]**

`Common.cs:24`:

```csharp
return (CardRank.Joker, CardColorFromString(input.ToUpper()[^0]), CardSuit.Joker);
```

`[^0]` is `input[input.Length]` — always out of range. **Verified:** throws
`IndexOutOfRangeException`. Should be `[^1]`.

Currently harmless: the whole method is **dead code**, along with `CardSuits_All`,
`CardRankCodes_All`, and `Table.AllCards` — none have callers.

### D6 — Weak shuffle

See §2.

---

## 8. Unimplemented surface

| Element | Location | State |
|---|---|---|
| Sets | `MakeSetsFromHand` | **[STUB]** returns empty list |
| Win condition | `WinConditionObserver.IsWinningHand()` | **[STUB]** always `false`, no callers |
| Settlement | — | **[ABSENT]** `Money` never changes. `Player.Score` is now **obsolete** — no deadwood penalty exists (`RULES.md` §7.2); delete it. |
| Multi-round play | `StartGame` | `gameIsOver = true` after one round |
| Melding / laying down | — | **[ABSENT]** no way to place a play on the table |
| Rules engine wiring | — | **[ABSENT]** `CardPlaysFactory` unreachable from `GameMaster` |

---

## 9. Open rules questions

**Superseded.** Rules questions are now tracked canonically in **`RULES.md` §9**, which
records provenance and confidence for each. Several items previously listed here have been
resolved by player recollection — notably:

| Previously open | Now |
|---|---|
| Ace wrap legal? | **No.** `K-A-2` is illegal. Removing the wrap also fixes defect **D1**. |
| Win condition | **All 13 cards melded**, then discard. |
| Losers penalized? | **Yes**, on unmelded cards. Scale still open. |
| Money card claim: copy or real card? | **The real card.** The clone in §5 is a bug. |
| Player count | **4–6.** Above 6 the draw pile is too thin (`RULES.md` §2.1). |
| Money card payout | **Money card value per card, per opponent, to the owner** — settled. |

| Winner's collection | **Flat round value from each loser** ($5 standard). |
| Payment eligibility | **Ownership, not possession** — confirms the existing `MoneyCardOwner` design. |
| Stakes | **Two configurable values** per game: round value and money card value. |

| Unmelded-card penalty | **None.** Losing costs exactly the round value. `Player.Score` is obsolete. |

Still unresolved and blocking implementation: **whether melds are declared concealed or
laid down progressively** (`RULES.md` §6.3 — decides whether `Table` must model melds at
all), whether the initial deal confers money-card ownership, money-card match semantics (exact card vs.
whole rank), duplicate suits in sets, and the discard exception. See `RULES.md` §9.

**Note for implementation:** `MoneyCardOwner` is currently set only on a draw during play.
`DealCardsToPlayers` never sets it — see `RULES.md` §4.4.
