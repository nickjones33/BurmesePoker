# Burmese Poker — Official Rules

**This is the canonical rules source for this project.** `RULES-PRIMER.md` (short recall
aid) and `RULES-TECHNICAL.md` (implementation spec + defects) are subordinate to it.
Where they disagree, this document wins.

Last revised: 2026-08-18 (rev 13 — §4.3's `DERIVED` balance argument is now measured rather
than estimated: 600 simulated five-player rounds put the side-bet at 42% of the round prize,
against the 40% the derivation guessed. No rule changed; rev 12 added §9 #14, raised while
building the match engine, and the P9 defaults for §9 #4 and #5).

---

## How to read this document

Every rule carries a **provenance** tag and a **confidence** level. Nothing here is
presented as settled unless it genuinely is.

### Provenance

| Tag | Meaning |
|---|---|
| **`EXPERT`** | Confirmed by **Mya Lay**, an experienced Burmese Poker player consulted directly. **Highest authority** — outranks both recollection and `IR`. |
| **`PLAYER`** | Recalled by Nick, who learned the game in person. |
| **`CODE`** | Implemented in this repo. Evidence of past intent, not proof of correctness. |
| **`IR`** | Corroborated by **Indian Rummy** as documented on [pagat.com](https://www.pagat.com/rummy/indian.html) — the closest documented relative (see §8). Supporting evidence only. |
| **`DERIVED`** | A mathematical consequence of other rules. |
| **`OPEN`** | Undecided. Needs a ruling. |

### Confidence

| Level | Meaning |
|---|---|
| **Settled** | Confirmed by `EXPERT`, or player recollection corroborated by `IR`/`CODE`. Build on it. |
| **Probable** | Player recollection, stated with hedging, uncorroborated or partially so. |
| **Tentative** | Player explicitly unsure ("maybe", "not positive"). Treat as a placeholder. |
| **Unknown** | Nobody knows. Must be decided before implementation. |

> **Why no external ruleset?** There is no published rulebook for this game. Pagat.com has
> no Myanmar section at all. The commercial Myanmar game **မြန်မာ ပိုကာ / "13 ချပ်"** is a
> rummy despite the "poker" name (package ID `com.aod.rummy`), but teaches its rules only
> in-app. The money-card mechanic appears in no source anywhere. This is a house ruleset.

---

## 1. Overview

A rummy for money. Each player is dealt 13 cards and races to arrange the whole hand into
**runs** and **sets** by drawing and discarding one card per turn. Layered on top is a
side-settlement: certain cards are **money cards**, and holding them pays out
independently of who wins the hand.

The money-card layer is what makes this game distinct. It is also the layer with the least
documentation and the lowest confidence below.

---

## 2. Players and equipment

| Rule | Provenance | Confidence |
|---|---|---|
| **Two full decks shuffled together, jokers included — 108 cards** (2 × 52 ranked + 2 × 2 jokers). | `CODE` | Settled |
| **4 players minimum.** | `PLAYER` | Probable |
| **6 players practical maximum** (see §2.1). | `DERIVED` `IR` | Probable |
| Two stakes are agreed **before play** and hold all game: a **round value** and a **money card value** (§4.3). | `PLAYER` | Settled |
| Typical stakes: **$5 round / $1 money card**. | `PLAYER` | Probable |
| Each player starts with an equal bank. Code uses **100**; the real buy-in is unrecorded. | `CODE` | Tentative |
| **13 cards** dealt to each player, one at a time. | `PLAYER` `CODE` `IR` | Settled |
| Turn order fixed and circular. Code randomizes it at start. | `CODE` | Settled |

> ⚠️ The code hardcodes **5 players** with fixed names. Player count must become
> configurable.

### 2.1 Where the player limit actually falls — `DERIVED`

Nick's intuition that "you'll probably hit a limit at some point" is correct, and the
arithmetic pins it down. Of 108 cards, 2 are removed as money cards, leaving **106**:

| Players | Dealt (13 each) | Draw pile left | Draws available *per player* |
|---:|---:|---:|---:|
| 4 | 52 | 54 | ~13 |
| 5 | 65 | 41 | ~8 |
| 6 | 78 | 28 | ~4–5 |
| 7 | 91 | 15 | ~2 |
| 8 | 104 | **2** | ~0 |

A rummy hand needs roughly 5–10 draws per player to be winnable. **At 7 players the game
is barely playable; at 8 it cannot be dealt meaningfully.** So the ceiling is **6**.

This matches Indian Rummy exactly, which specifies **2 decks for up to 6 players and a
third deck beyond that** (`IR`). If you ever want 7–8 players, the documented fix is to
add a third deck — 162 cards — rather than reduce the hand size.

---

## 3. Setup

Order matters. `CODE`, Settled:

1. Shuffle both decks together.
2. Randomize seating order.
3. Deal **13 cards** to each player, one at a time around the table.
4. Turn up **two money cards** — one from the **bottom** of the deck, one from the **top**.
5. Designate money cards (§4).

Because money cards are turned up **after** the deal, a player may already be holding a
copy of a card that is about to become a money card. That appears to be the point.

---

## 4. Money cards

The distinguishing mechanic of the game.

### 4.1 What counts as a money card

| Rule | Provenance | Confidence |
|---|---|---|
| **7♦ and A♠ are permanent money cards** in every round. All copies, both decks. | `CODE` | Probable |
| The **two turned-up cards** are money cards for that round. | `CODE` | Settled |
| If a turned-up money card is the 7♦ or A♠, it becomes a **double money card** instead of merely stacking. | `CODE` | Probable |
| Doubling is the maximum. There is no triple. | `CODE` | Probable |

> **`OPEN` — why 7♦ and A♠?** No source explains this and Nick doesn't recall. It may be
> arbitrary house tradition. Recorded as-is.

> **`OPEN` — what if a turned-up card is a joker?** 4 of the 108 cards are jokers, so one of
> the two cards turned up at setup is a joker roughly one round in fourteen, and no source
> says what that does. Three readings: it designates the **jokers of its own colour** (2
> cards — what §4.2's exact match says if a joker's colour is read as its identity), it
> designates **all four jokers**, or jokers **cannot be money cards** and the card is set
> aside and another turned. **Recommend the first**, because it is §4.2 applied unchanged
> rather than a new rule. Tracked as §9 #11; the safe default is recorded in `BUILD-PLAN.md`
> P2.

### 4.2 Matching — `PLAYER`, Settled

**Money cards match by exact rank *and* suit.** A turned-up 5♥ designates only the two
5♥ copies across the two decks — **2 money cards**, not 8.

This confirms the code's existing behaviour and rejects the Indian Rummy reading, under
which the turned-up card would designate all cards of that rank across every suit.

Two independent lines of evidence agree:

- **Direct ruling** by the player.
- **`DERIVED`, the stake ratio** — at $5 rounds and $1 money cards, rank-matching would put
  roughly twice as many money cards in play and the side-bet would routinely outweigh the
  round prize, inverting the game. Only exact-matching balances at 5:1. (§4.3)

### 4.3 Payout — `PLAYER`, Settled

Two stakes are fixed **at the start of the game** and apply to every round:

| Stake | Typical | Meaning |
|---|---|---|
| **Round value** | **$5** | What each player pays the winner when a round ends. |
| **Money card value** | **$1** | What each player pays, per money card, to that card's owner. |

At the end of each round:

1. **Every other player pays the winner the round value.**
2. **For each money card, its owner collects the money card value from every other player.**

Money-card settlement is pairwise and mutual — *"everyone owes everyone else."* The winner
takes part in it too, both collecting and paying. It resolves **independently of who won**.

**Worked example** — 5 players at the standard $5 / $1:

| | |
|---|---|
| Winner collects | 4 × $5 = **$20** |
| A player owning 2 money cards collects | 2 × $1 × 4 = **$8** |
| A player owning 1 **double** money card collects | 2 × $1 × 4 = **$8** |

| Detail | Provenance | Confidence |
|---|---|---|
| Flat round value from each loser to the winner. | `PLAYER` | Settled |
| Money card value per card, per opponent, to the **owner**. | `PLAYER` | Settled |
| Settled on top of the round payment, not instead of it. | `PLAYER` | Settled |
| A **double** money card counts as **two**. | `DERIVED` | Probable |

**`DERIVED` — the recalled stakes produce a balanced game.** Under the code's exact-match
rule (§4.2), ~6 money cards circulate beyond the two face-up. In a 5-player round the
winner takes $20, while a player owning two money cards takes $8 — roughly 40% of the
round prize. Significant without dominating: exactly what a side-bet should be.

> **Measured, and the estimate holds** (packet P12, 2026-08-18). Over **600 simulated
> five-player rounds** the money cards moved **$8.43 a round** against the flat prize of
> $20 — **42% of it**, and 30% of all the money that changed hands. The derivation above
> guessed 40% from two owned cards, so the recalled 5:1 ratio really does produce the
> balance it was justified by. This is a measurement of the *game*, not a rule: it says
> nothing new about what the rule is, only that the rule is a sensible one.

This also **strengthens the case for exact-match over rank-match** in §4.2. Rank-matching
would put ~12 money cards in play, and the side-bet would routinely outweigh the round
prize — inverting the game. The 5:1 round-to-money-card ratio only makes sense with the
narrower rule.

### 4.4 Eligibility — `EXPERT`, Settled

**Payment follows ownership alone. Ownership is conferred by the deck, and is permanent.**

Two rules. The second is the counterintuitive one, confirmed from experience as *"a key
element of Burmese Poker"*.

1. **You own a money card if — and only if — the deck gave it to you.** Dealt or drawn, both
   count. Anything you took from the table or from another player's discard is **held but never
   owned**.
2. **Ownership never transfers and is never lost.** Discard an owned money card and **you are
   still paid for it at settlement** — even if an opponent is now holding it, and even if it
   sits untouched in a discard pile for the rest of the round.

So at settlement the question is never *who holds this card?* but *who did the deck give it to?*

| How the card reached your hand | Pays you? | Provenance |
|---|:-:|---|
| **Dealt** in your opening 13 | ✅ | `EXPERT` |
| **Drawn** blind from the deck | ✅ | `EXPERT` |
| **Picked up** from the previous player's discard | ❌ | `EXPERT` |
| **Claimed** from the turned-up money cards (§4.5) | ❌ | `CODE` `DERIVED` — consistent with rule 1, not separately verified |

This vindicates the 2023 code's design, which was more deliberate than it first appeared.
`MoneyCardOwner` is a field **on the card**, set once when the card comes from the deck and
carried with it thereafter — exactly the right shape for a permanent, non-transferring claim.
And the first-turn claim sets it to `null` with the comment *"belongs to the deck, doesn't
score for the new steward"*, which is precisely rule 1 in action.

> **`DERIVED` — why this is good design, not merely quirky.** Permanent ownership
> **decouples the money side-bet from the melding game entirely.** Because discarding a money
> card costs you nothing, you never have to choose between chasing the win and hoarding cash.
> Your meld strategy is undistorted: play the hand to win, and the money settles on the luck
> of what the deck dealt you. Under a hold-based rule the two would fight constantly, and
> players would sit on useless money cards rather than melding.
>
> It also explains why claiming the turned-up money card is worth offering at all (§4.5): it
> gives you a card's *melding utility* with no money attached — a pure hand decision.

> **`DERIVED` — the money layer is mostly settled at the deal.** Since dealing confers
> ownership, most money cards are claimed before anyone acts. In a 5-player round, ~6
> money-bearing cards sit in the 106-card pool; 65 of those are dealt, so roughly **4 of the 6
> are owned the moment the deal ends**, with the remaining ~2 claimable by drawing during play.
>
> The money side-bet is therefore **substantially a deal lottery, with a smaller live
> component** — which fits its role: it is not meant to be played for, only settled. This is
> the direct consequence of the answer to question #3 and is worth remembering when tuning
> stakes.

### 4.5 Claiming the turned-up money card

| Rule | Provenance | Confidence |
|---|---|---|
| The opening player may take the top money card instead of drawing. | `CODE` | Probable |
| They take the **actual physical card**, which leaves the table. | `PLAYER` | Probable |
| Claiming requires **permission from another player**. | `CODE` (TODO) | Unknown |
| Offered once per game, or once per round? | `OPEN` | Unknown |

> ⚠️ The code **clones** the card, creating a 109th card and leaving the original on the
> table. Nick: *"you almost certainly do take the actual physical card, not a copy."*
> **This is a bug.** See `RULES-TECHNICAL.md` §5.

> **`DERIVED` — a claimed money card pays nothing, and that is the point.** By §4.4 a card
> taken **from the table** is held but never owned, so claiming the turned-up money card buys
> you melding utility with **no payout attached**. The code's `MoneyCardOwner = null` and its
> *"doesn't score for the new steward"* comment encode exactly this. Confirmed as intended.

> **`OPEN` — what happens to the turned-up cards afterwards?** They are removed from the
> deck at setup and, in the code, stay in `CurrentRoundMoneyCards` forever — never
> returned, never re-drawn. So two cards leave play each round purely to act as
> designators (one may be claimed on the opening turn). Probably right, but unconfirmed —
> and it matters for deck exhaustion (§5).

---

## 5. Play

`CODE` `IR`, Settled unless noted.

Each turn, in order:

1. **Take one card** — either the previous player's most recent discard, or the top of the
   deck, blind.
2. **Discard one card.**

You are therefore always at 13 cards between turns, and 14 during your turn.

| Detail | Provenance | Confidence |
|---|---|---|
| Only the immediately-previous player's top discard is available. | `CODE` | Tentative |
| **When the deck runs out, gather the discards, shuffle them, and they become the new draw pile.** | `PLAYER` | Settled |
| Reshuffling is **rare** in practice — most rounds end first. | `PLAYER` | Settled |
| One shared discard pile, vs. per-player piles. | `OPEN` | Largely moot — see below |

> ⚠️ Code gives every player a **private** discard list and has **no deck-exhaustion
> handling** — it crashes when the deck empties. Private piles remain workable: at
> exhaustion, gather *all* of them. Only the current top discard is ever takeable, so the
> distinction stays invisible during normal play.

> ✅ **Built in P9.** The gather happens at the moment somebody draws into an empty pile, not
> around the round, because there is no resume point mid-turn. Every pile is swept; the
> turned-up cards are left where they are (§9 #4). One consequence worth knowing: with the
> reshuffle in place, **a round in which nobody's hand ever improves does not end**, because
> only a declaration ends a round (§7.1) and the cards now circulate for ever. That is the
> rules working as written, not a defect — but it means a table of players who never try to
> win never finishes, which matters to anything driving the game automatically.

> ⚠️ **Reshuffling collides with permanent ownership — resolved.** A genuine edge case, produced
> by combining two settled rules rather than by any one of them:
>
> Alice is dealt the 7♦ and so owns it (§4.4 rule 1). She discards it. The deck runs out, the
> discards are reshuffled, and **Bob draws the 7♦ from the new deck.** Rule 1 says the deck
> just gave it to Bob, so Bob owns it. Rule 2 says ownership *never transfers*, so Alice keeps
> it. Both cannot hold.
>
> **Ruling: first acquisition wins — Alice keeps it.** `PLAYER`, Settled. Ownership is
> **write-once**; rule 2 governs. The build plan's `CardOwnership` enforces this (packet P2).

---

## 6. Melds

### 6.1 Runs (sequences)

| Rule | Provenance | Confidence |
|---|---|---|
| Three or more cards, **same suit**, **consecutive rank**. | `PLAYER` `CODE` `IR` | Settled |
| **Aces do not wrap.** `A-2-3` is legal; `Q-K-A` is legal; **`K-A-2` is not**. An ace is high or low within a run, never both. | `PLAYER` `IR` | Settled |
| Jokers substitute for a missing card. | `CODE` `IR` | Settled |
| Maximum jokers per meld — **including whether a meld may be nothing but jokers**. | `OPEN` | Unknown |

> ⚠️ **The code currently allows `K-A-2`.** Its rank order is a full cycle. This both
> contradicts the rule above *and* causes a **verified infinite loop** — a hand holding all
> 13 ranks of one suit hangs forever. Removing the wrap fixes both at once. This is the
> highest-priority code fix.
>
> Indian Rummy states the rule precisely: *"The ace can be next to the two (in A-2-3) or
> next to the king (in Q-K-A), but not both at once."* (`IR`)

### 6.2 Sets

| Rule | Provenance | Confidence |
|---|---|---|
| Three or more cards of the **same rank**. | `PLAYER` `IR` | Settled |
| **Duplicate suits are forbidden.** `9♥ 9♥ 9♠` is **not** a valid set. | `EXPERT` `IR` | Settled |

Because duplicates are forbidden, a set contains **at most 4 cards** — one per suit — even
though two decks make a fifth copy physically holdable.

> **Calibration note.** This reverses the provisional call in rev 1. Nick's hedged
> recollection ("I think sets allow duplicate suits, not positive") pointed one way and
> Indian Rummy the other; this document reasoned that direct experience of a house variant
> should outrank the presumed parent game. **That reasoning was wrong here** — Mya Lay
> confirmed the Indian Rummy rule.
>
> The lesson for the remaining unknowns: wherever this game has been checked against Indian
> Rummy, it has matched on **every structural point**. `IR` deserves more weight as a prior
> than rev 1 gave it. It is still not authoritative — §7.2 is a genuine divergence — but it
> should not be discounted against an uncertain recollection.

**Not implemented at all** — `MakeSetsFromHand` returns an empty list.

### 6.3 How melds reach the table — `PLAYER`, Settled

**Play is fully concealed.** In Nick's words: *"you never actually play anything until you
end the round. You keep all your intended plays secret until that point."*

- Melds are **never** placed on the table during play. Nothing is revealed until a player
  goes out and declares the whole hand at once.
- There is **no laying off** on your own or anyone else's melds.
- Opponents' intentions are invisible; the only public information is the discards.

This matches Indian Rummy (`IR`) and is consistent with the all-13-melded win condition —
a single declaration event.

> **`DERIVED` — this removes an entire subsystem.** The `Table` never needs to model melds,
> meld ownership, or lay-off legality.
>
> **But the evaluation the game actually needs is not what the code computes.**
> `MakeAllPossiblePlaysFromHand` *enumerates candidate melds*, and those candidates
> **overlap** — it hands the same joker to a diamond run and a heart run, and emits every
> sub-run of a longer run. Declaring a win asks a different question:
>
> > *Can these 13 cards be partitioned into disjoint valid melds, using each card exactly
> > once?*
>
> That is an **exact-cover / set-partition** problem. Enumeration is a useful input to it,
> but not a solution. See `RECONCILIATION-PLAN.md`.

---

## 7. Winning and settlement

### 7.1 Going out

| Rule | Provenance | Confidence |
|---|---|---|
| **All 13 cards must be melded** into valid runs and sets. | `PLAYER` `IR` | Settled |
| You **discard first, then lay down all 13 melded cards.** | `PLAYER` | Settled |
| There is **some exception** to the discard requirement. | `PLAYER` | Unknown |
| A **pure** (joker-free) sequence is required. | `OPEN` | Unknown |

**The order matters.** In Nick's words, a player *"discards and then plays all 13 of their
cards melded"* — the discard comes **first**, and the reveal follows it. So a turn that wins
runs: hold 13 → take a card, now 14 → **discard 1** → **lay down all 13**.

Earlier revisions of this document had the narration backwards ("meld all 13, then discard").
The arithmetic was right either way, but the sequence matters to the engine and the UI: the
declaration is resolved **after** the discard, not before.

**`DERIVED` — the arithmetic confirms it.** Hand size, the all-13 rule, and the mandatory
discard are mutually consistent, and 13 partitions cleanly into melds of 3+ (`3+3+3+4`,
`4+4+5`, `3+10`). Indian Rummy resolves identically: *"discards the 14th card and declares
Rummy."*

> **`OPEN` — the exception.** Nick recalls one but not what it is. In related rummies the
> usual candidates are going out concealed in one turn, or going out on the drawn card
> with nothing left to discard. Needs recovery.
>
> **`OPEN` — purity.** Indian Rummy requires at least one pure sequence and at least two
> sequences total to declare. Nick doesn't recall purity being a rule here. Since this
> version requires melding **all 13** — a stricter condition than Indian Rummy's — the
> pure-sequence safeguard may simply be unnecessary. **Recommendation: treat purity as
> not-a-rule** unless memory says otherwise.

### 7.2 Settlement

Full order of operations at the end of a round — `PLAYER`, Settled:

1. **Round payment.** Every player except the winner pays the **round value** to the
   winner. **Flat** — typically $5, regardless of what is left in hand.
2. **Money card settlement.** For each money card, its **owner** collects the **money card
   value** from every other player. Pairwise and mutual; the winner participates. (§4.3)

**There is no per-card penalty for unmelded cards.** `PLAYER`, Settled — an earlier
recollection of a deadwood penalty was withdrawn on review. Losing costs exactly the round
value, whether you were one card short or holding all thirteen.

> **`DERIVED` — this deletes a whole subsystem.** With no per-card penalty there is no
> deadwood count, so the game needs **no card point values at all** and **`Player.Score`
> has no purpose**. Money is the only ledger. Remove the field rather than leaving it to
> imply a scoring model that does not exist.
>
> This is a clean divergence from Indian Rummy, which does total deadwood and pay it to the
> winner (`IR`). Burmese Poker replaces per-card scoring with the flat round value plus the
> money-card side-bet.

| Detail | Provenance | Confidence |
|---|---|---|
| Flat round value to the winner from each other player. | `PLAYER` | Settled |
| Money cards settle separately and pairwise, by owner. | `PLAYER` | Settled |
| **No** penalty for unmelded cards. | `PLAYER` | Settled |
| **Nothing ends the session automatically.** Rounds repeat and banks carry over; players stop when they choose. | `PLAYER` | Settled |

---

## 8. Comparison with documented games

I swept for any documented game resembling this one. Results:

| Candidate | Verdict |
|---|---|
| **Shan Koe Mee**, **Shwe Shan** (Myanmar) | ❌ 3-card banking games against a dealer. No melding, no draw/discard. Unrelated despite being the best-known Burmese card games. |
| **Penang Rummy** (Malaysia) | ❌ 20–25 cards, 8 jokers, and *no draw or discard at all*. Structurally very distant. |
| **Tong-its** (Philippines) | ❌ 3 players, one 52-card deck, no jokers, 12 cards. A Tonk descendant, not a relative. |
| **Three Thirteen**, **Gin**, **Canasta** | ❌ Wrong hand size, wrong deck count, wrong structure. |
| **မြန်မာ ပိုကာ / "13 ချပ်"** (Myanmar) | ⚠️ **Almost certainly the same game family** — Burmese players call this rummy "poker", which is exactly this project's name. Rules are in-app only; nothing published to compare against. |
| **Indian Rummy** | ✅ **The closest documented relative by a wide margin.** |

### 8.1 Indian Rummy alignment

| Feature | Indian Rummy | Burmese Poker | |
|---|---|---|:-:|
| Decks | 2 (up to 6 players) | 2 | ✅ |
| Cards dealt | 13 | 13 | ✅ |
| Player ceiling | 6 on two decks | 6 (derived, §2.1) | ✅ |
| Melds | runs + sets | runs + sets | ✅ |
| Aces | high or low, **never wrapping** | never wrapping | ✅ |
| Sets with duplicate suits | forbidden | **forbidden** | ✅ |
| Concealed until declaration | yes | yes | ✅ |
| Going out | meld all, discard 14th | meld all 13, discard | ✅ |
| Printed jokers | 1 per deck | 2 per deck | ⚠️ |
| Round-start ritual | turn up 1 card → designates **wilds** | turn up 2 cards → designate **money** | ⚠️ |
| Losers | pay **per unmelded card** | pay a **flat round value** | ❌ |
| Money cards | none | core mechanic | ❌ |

**Eight of twelve features match exactly**, and the two ⚠️ rows are variations rather than
contradictions. Every point of structure — deck count, hand size, player ceiling, meld
definitions, ace handling, set composition, concealment, and the going-out condition — is
identical.

### 8.2 The central hypothesis

> **Burmese Poker looks like Indian Rummy with the scoring layer replaced by a money layer.**

Everything structural is shared (§8.1). The divergences are confined to one coherent
substitution, and they fit together:

| Indian Rummy | Burmese Poker |
|---|---|
| Turn up a card at round start → matching cards become **wild** | Turn up two cards → matching cards become **money** |
| Losers pay **per unmelded card** (deadwood scoring) | Losers pay a **flat round value**; money cards settle separately |
| Rank-based wilds **plus** printed jokers | **Only** printed jokers are wild — 2 per deck rather than 1 |

The internal consistency is the striking part. Once the turn-up ritual is reassigned from
designating wilds to designating cash, the game **loses its rank-based wilds** — and
Burmese Poker compensates with double the printed jokers. Meanwhile per-card scoring
disappears entirely, replaced by the flat round value plus the money-card side-bet.
Three changes, one idea.

**Transmission is historically plausible.** Myanmar borders India and had a large
Indian-Burmese community through the colonial period; rummy variants propagate this way.

**Still a hypothesis, not a finding.** It has earned weight: every structural prediction it
made has since been confirmed, including forbidden duplicate suits (§6.2) and concealed
play (§6.3). Use it as a **prior for recovering forgotten rules**, not as authority — §7.2
shows the game does genuinely depart.

*(A coincidence, not evidence: the canonical Indian Rummy example of a turned-up wild is
the **7♦** — one of your two permanent money cards. Almost certainly chance.)*

---

## 9. Outstanding questions

**No conflicts remain.** All three — money-card matching (§4.2), duplicate suits in sets
(§6.2), and the melding model (§6.3) — are resolved. What is left is simply unrecorded.

| # | Question | § | Status | Blocks |
|---:|---|---|---|---|



| 4 | Do the turned-up cards stay out of play permanently? | 4.5 | Open — recommend *yes*, **taken as the default by P9** | Deck exhaustion |
| 5 | Is the money-card claim once per game or per round, and who approves it? | 4.5 | Unknown — **P9 defaults to *per round*, approved by nobody** | Turn logic |
| 6 | What is the exception to the mandatory discard? | 7.1 | Unknown | Win detection |
| 7 | Is a pure sequence required? | 7.1 | Unknown — recommend *no* | Win detection |
| 8 | Max jokers per meld — and may a meld be **entirely** jokers? | 6.1 | Unknown — recommend *unlimited*, which permits an all-joker meld | Meld validation |
| 9 | One shared discard pile or per-player piles? | 5 | Unknown — largely moot | Low |
| 10 | Why 7♦ and A♠ specifically? | 4.1 | Unknown, likely unrecoverable | None |
| 11 | If a **joker** is turned up as a money card, what does it designate? | 4.1 | Unknown — recommend *the two jokers of that colour* | Money designation (P2) |
| 12 | When the opening player **claims** the turned-up money card, does that card's value still pay for the rest of the round? | 4.5 | Unknown — recommend *yes* | Money designation (P7) |
| 13 | May a player discard the very card they just took? | 5 | Unknown — recommend *yes* | Turn logic (P7) |
| 14 | Does anything move between rounds — the seating, the deal, who goes first? | 3 | Unknown — recommend *no* | Match setup (P9) |


> **#8 widened in rev 10, while building P3.** "Unlimited jokers" was recorded as a bound on
> substitution, but a window-based generator answers a sharper version of the question by
> construction: with four jokers in hand, `🃏🃏🃏` satisfies a three-card window with nothing
> real in it. **P3 takes the recommendation literally and emits all-joker melds** — a lower
> bound of one real card per meld is a rule nothing in §6.1 states, and inventing it would be
> the stricter, not the safer, choice. Four jokers exist in the shoe, so a hand can hold at
> most four; the effect is bounded and easy to reverse if Mya Lay says otherwise.

> **#12 and #13 raised in rev 11, while building the round engine (P7).**
>
> **#12 — a claimed money card.** The two turned-up cards do two jobs at once: they *designate*
> which values pay (§4.2) and one of them is a physical card the opening player may *take*
> (§4.5). Taking it separates the two jobs for the first time. **P7 takes the safe default:
> designation is fixed at setup and does not move with the card**, so the claimed value goes
> on paying its owners for the rest of the round — the claimer simply is not one of them,
> because the table gave them the card and not the deck (§4.4). The alternative — that
> claiming un-designates the value — would silently stop paying the *other* copy of that card,
> which somebody may have been dealt and now owns; that seems a much stranger rule than the
> one taken. Reversing it is a one-line change in `TableState`'s constructor.
>
> **#13 — discarding what you just took.** RULES.md §5 says take one card and discard one
> card, and says nothing about them having to be different. Several rummies forbid discarding
> the card just picked up from a discard pile, precisely because it is a null move. **P7 takes
> the safe default: nothing forbids it**, and the engine accepts it. If it turns out to be a
> rule it is a single guard in `RoundEngine`, and the question is really only about the
> *pickup* — throwing back a card drawn blind is unremarkable.

> **#4, #5 and #14 all came due in rev 12, while building the match engine (P9).** None of the
> three blocks anything; each is one line to reverse.
>
> **#4 — the turned-up cards and the reshuffle.** RULES.md §5 says to gather *the discards*
> when the draw pile runs out, and says nothing about the one or two cards lying turned up on
> the table. **P9 takes #4's standing recommendation literally: they stay out of play**, so the
> reshuffle sweeps every discard pile and leaves the table alone. The alternative would put the
> designators back into circulation mid-round, which would also mean a player could draw the
> very card that designates the money — a stranger rule than the one taken, and one nothing in
> §4 hints at. Reversing it is a line in `TableState.ReplaceDrawPileWithTheDiscards`.
>
> **#5 — how often the claim is offered.** A match is a sequence of rounds, each with its own
> two cards turned up (§3 step 4), so **P9 offers the claim on the opening turn of *every*
> round** — the reading on which the claim belongs to the setup that produced the card, not to
> the session. "Once per game" would mean a round after the first turns two money cards up and
> lets nobody take either, which is hard to square with §4.5 describing it as part of the
> opening turn. Nobody approves it: the opener is simply asked. If it turns out to be
> once-a-game, `MatchEngine` would pass the fact along to the round it builds.
>
> **#14 — what moves between rounds.** §3 step 2 randomises seating at setup and the rules say
> nothing at all about a second round: no dealer, no rotation, no "the winner deals next".
> **P9 keeps the seating it was given for the whole match**, so the same player opens every
> round. Whether that confers an advantage is a question for P12 rather than a rules question,
> but if seating does move, the change is in `MatchEngine` alone — the round engine already
> takes its seating as given.

**Nothing here blocks the build.** Every remaining item has a safe default recorded in
`BUILD-PLAN.md`. They are worth settling for fidelity, not for progress.

> **Resolved since rev 4:**
> - **Deck exhaustion** (was #1): gather the discards, shuffle, and that becomes the new draw
>   pile. Rare in practice (§5).
> - **Match end** (was #2): **there is none.** Rounds repeat and banks carry over; the session
>   ends when players decide to stop (§7.2).
> - **Ownership across a reshuffle**: **first acquisition wins** — ownership is write-once
>   (§5). Confirmed.
> - Whether an owned money card must still be *held* at settlement. It must not —
>   **ownership alone pays, permanently** (§4.4). The provisional "hold and own" recommendation
>   was wrong.
> - Whether the **initial deal** confers ownership (was #3). **It does** — confirmed by
>   `EXPERT`, along with the fact that picking a money card up from a discard confers nothing.
>   See the acquisition table in §4.4.

> **Note on #9.** Largely moot: if only the top discard is takeable, a shared pile and
> per-player piles are observationally identical — the top of a shared pile *is* the
> previous player's discard. It matters only for reshuffling on exhaustion (#1).

---

## 10. Rulings that change the code

Decisions here that the implementation contradicts or lacks:

1. **Remove the ace wrap** (§6.1). Fixes the illegal `K-A-2` meld *and* the verified
   infinite loop.
2. **Delete `Player.Score`** (§7.2). No deadwood penalty exists; money is the only ledger.
3. **Build a partition-based hand evaluator** (§6.3). Declaring a win asks whether 13 cards
   partition into *disjoint* melds — not what `MakeAllPossiblePlaysFromHand` computes.
4. **Sets must forbid duplicate suits** (§6.2), capping a set at 4 cards.
5. **No table meld state** (§6.3). Play is concealed; `Table` needs no meld model.
6. **Move the claimed money card, don't clone it** (§4.5). Currently invents a 109th card.
7. **Make player count configurable, 4–6** (§2).
8. **Add configurable stakes** — round value and money card value (§4.3).
9. **Implement settlement** (§7.2): flat round payment, then pairwise money-card settlement.
10. **Set `MoneyCardOwner` during the deal** (§4.4), pending confirmation.
11. **Implement sets** (§6.2) and **the win condition** (§7.1).
12. **Handle deck exhaustion** (§5). Currently crashes.

See `RULES-TECHNICAL.md` §7 for defects not driven by rules decisions, and
`RECONCILIATION-PLAN.md` for sequencing.

---

## Sources

- [Indian Rummy — pagat.com](https://www.pagat.com/rummy/indian.html) (authoritative reference for the closest relative)
- [Penang Rummy — Wikipedia](https://en.wikipedia.org/wiki/Penang_rummy)
- [Tong-its — Wikipedia](https://en.wikipedia.org/wiki/Tong-its)
- [မြန်မာ ပိုကာ ZingPlay 13 ချပ် — Google Play](https://play.google.com/store/apps/details?id=com.aod.rummy) (package ID `com.aod.rummy`)
- [Shan Koe Mee — Quora](https://www.quora.com/How-do-I-play-the-Myanmar-card-game-Shan-Koe-Mee)
- [Indian Rummy joker rules — A23](https://www.a23.com/rummy/rummy-rules.html)
