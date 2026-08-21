# Burmese Poker — Official Rules

**This is the canonical rules source for this project.** `RULES-PRIMER.md` (short recall
aid) and `RULES-TECHNICAL.md` (implementation spec + defects) are subordinate to it.
Where they disagree, this document wins.

Last revised: 2026-08-20 (rev 17 — 🔥 **the win condition is no longer one rule.** §7.1.1 is
new: what a declared hand must contain **changes with the number of players** — series only at
two, at least two series at three, at least one at four, no requirement at five or more — and **the series a table size *requires* must be
joker-free** — both of them three-handed, the one of them four-handed, and cleanliness is
irrelevant two-handed and five-or-more-handed (§7.1.1). `EXPERT`, from a session with **Mya Lay and Aung
Aung** on 2026-08-20. The same session settled four more: **the discard has no exception, ever**
(§7.1, closing §9 #6); **the discards are public and may be looked through** (§5, closing §9 #15
*against* its standing recommendation and un-mooting §9 #9); **only the immediately-previous
player's discard is takeable** (§5, `CODE` `Tentative` → `EXPERT`); and **7♦ and A♠ really are
permanent money cards** (§4.1, `CODE` `Probable` → `EXPERT`). ⚠️ **Two players and three players
are real games**, so §2's "four minimum" was wrong and §2.1 now runs from two. §9 #22 and #24–#27 are what
those answers left unspecified, and §10 #14–#15 are the code they change. Rev 16 — **§5.1 is new and is the first rule here that constrains
*which* card you may discard**: you may not throw a rank the next player has taken in the open,
until they throw one back or you are going out. `EXPERT`, ruled directly by Mya Lay at a playtest
on 2026-08-20. It is **settled and wholly unimplemented** — §9 #16–#19 are its specification and
§10 #13 is the code it changes, with #27 added on top of them by checking the rule against `Card`
and #25 by rev 17's two-handed tables. **A banned discard is not an infraction but an impossible move**,
and where the ban would leave a player nothing legal to throw **the ban yields for that turn** —
both `PLAYER` rulings of the same day, which is why §9 #20 was raised and closed inside one
revision. Rev 15 gave §4.1 a `DERIVED` note that fell out of P22: **a
designation that lands on a permanent money card leaves the deck with *less* money in it, not
more.** No rule changed; it is a consequence of §4.1 and §3 step 4 together. Rev 14 added §9 #15
while planning the strategy programme: is a discard pile inspectable, or only its top card? Rev
13 measured §4.3's `DERIVED` balance argument — 600 simulated five-player rounds put the side-bet
at 42% of the round prize, against the 40% the derivation guessed; rev 12 added §9 #14, raised
while building the match engine, and the P9 defaults for §9 #4 and #5).

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
| **2 players minimum.** The game is played two- and three-handed. | `EXPERT` | Settled |
| **What a winning hand must contain changes with the player count** (§7.1.1). | `EXPERT` | Settled |
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
| 2 | 26 | 80 | ~40 |
| 3 | 39 | 67 | ~22 |
| 4 | 52 | 54 | ~13 |
| 5 | 65 | 41 | ~8 |
| 6 | 78 | 28 | ~4–5 |
| 7 | 91 | 15 | ~2 |
| 8 | 104 | **2** | ~0 |

A rummy hand needs roughly 5–10 draws per player to be winnable. **At 7 players the game
is barely playable; at 8 it cannot be dealt meaningfully.** So the ceiling is **6**.

> 🔥 **The floor is two, and rev 17 is where that was learned.** This section was written to
> find the *upper* limit and it found one; the lower end was Nick's recollection of "four
> minimum" and it was wrong. Two- and three-handed are real games — **and the rules are not the
> same games.** Read the top two rows of that table beside §7.1.1 and the reason is plain: a
> two-handed player draws **forty** cards where a six-handed player draws four or five, so the
> hand is made correspondingly harder to complete. **The requirement tightens as the deck
> opens up.** `DERIVED`, and it is reasoning about a rule rather than the rule itself.

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
| **7♦ and A♠ are permanent money cards** in every round. | `EXPERT` | Settled |
| …**all copies, both decks.** | `CODE` | Probable — ⚠️ the follow-up was put and not answered, §9 #24 |
| The **two turned-up cards** are money cards for that round. | `CODE` | Settled |
| If a turned-up money card is the 7♦ or A♠, it becomes a **double money card** instead of merely stacking. | `CODE` | Probable |
| Doubling is the maximum. There is no triple. | `CODE` | Probable |

> **`OPEN` — why 7♦ and A♠?** No source explains this and Nick doesn't recall. It may be
> arbitrary house tradition. Recorded as-is.

> ✅ **That they *are* permanent is no longer a reading of the old code.** Rev 17: asked flat —
> *"apart from the two you turn over, are there any cards that are always money cards?"* — and
> answered **yes**. It had stood at `CODE` `Probable` since rev 1, which is a weak tag under the
> whole money layer. ⚠️ **The three follow-ups behind it were not reached**: whether it is both
> copies out of both decks, what happens when a turned-up card *is* the 7♦, and whether it can
> stack past a double (§9 #24). Those remain `CODE` `Probable` and the doubling rules below are
> still unconfirmed by a player.

> 🔥 **`DERIVED` — a designation that lands on a permanent money card leaves *less* money in
> the deck, not more** (packet P22, 2026-08-20). It reads backwards and it is simply these rules
> put together. A turned-up card is **removed from the deck** to act as the designator (§3 step
> 4), so an ordinary designation — say a turned-up 5♥ — leaves **one** partner 5♥ in the deck
> paying $1, *and* the 7♦s and A♠s are untouched. Turning up a 7♦ instead makes that value a
> **double** and leaves **one** 7♦ in the deck paying $2 — but the card that would have been the
> second $1 designator is the one now lying on the table. Doubling one value is not the same as
> designating a second, and a player drawing blind in the hope of *acquiring* money (§4.4) has
> less to draw for. **This says nothing new about what the rule is** — it is what the existing
> rules already mean — and it is asserted as a test in
> `ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`.

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
| Only the immediately-previous player's top discard is available. | `EXPERT` | Settled |
| **You may not discard a rank the next player has taken in the open** — see §5.1. | `EXPERT` | Settled |
| **When the deck runs out, gather the discards, shuffle them, and they become the new draw pile.** | `PLAYER` | Settled |
| Reshuffling is **rare** in practice — most rounds end first. | `PLAYER` | Settled |
| **The discards are public.** A player may look through what has been thrown away, not merely see the top card. | `EXPERT` | Settled |
| **Each player keeps their own discard pile** in front of them. | `EXPERT` | Settled |

> ⚠️ Code gives every player a **private** discard list and has **no deck-exhaustion
> handling** — it crashes when the deck empties. Private piles remain workable: at
> exhaustion, gather *all* of them. Only the current top discard is ever takeable, so the
> distinction stays invisible during normal play.

> 🔥 **The discards are public, and rev 17 closed §9 #15 the opposite way to the default.**
> Asked flat — *"can you pick the pile up and look through it, or only see the top card?"* —
> and answered **yes, they're public**. Two consequences, and the second is the larger:
>
> 1. **Remembering what has gone is part of this game**, and a player may reconstruct it by
>    looking rather than by memory. P20 built a counting player under the *cautious* default —
>    it sees only what it has been shown, about ten cards beyond its own hand in a whole round
>    — and measured it at nothing. **That measurement is now conditional**: it was made under a
>    narrower information set than the rules allow. See §10 #14.
> 2. ✅ **It un-mooted #9 — and #9 was then answered the same day: one pile each.** §5.1's
>    release rule is *"a rank is banned unless it appears in the protected player's own
>    discards"*, which asks **whose** discard a card was. A single pile in the middle could not
>    have answered that by inspection; **per-player piles can, and per-player piles is what the
>    game uses.** 🔥 **So the release rule is readable straight off the table** — one look down
>    the protected player's own pile settles it — which is exactly what §5.1 claimed made the
>    rule cheap to enforce, and it turned out to depend on a question nobody had thought was
>    load-bearing. ⚠️ **The reshuffle still sweeps every pile** (§5), which is why #19 had to be
>    ruled on rather than read off.

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

### 5.1 Feeding — you may not discard a rank the next player has taken — `EXPERT`, Settled

**Observed and ruled on directly by Mya Lay during a playtest, 2026-08-20.** This is the first
rule in this document that constrains *which* card a player may discard.

**The rule, in one sentence:** once a player has publicly taken a card, the player who discards
to them may not discard **any card of the same rank** for the rest of the round.

Three terms carry the whole rule:

| Term | Meaning |
|---|---|
| **publicly taken** | The player took a card everyone could see — the discard offered to them, or the turned-up money card claimed on the opening turn (§4.5). |
| **the player who discards to them** | The **immediately preceding** seat in turn order. It is the only seat that can feed them, because only that seat's top discard is available to be taken (§5). |
| **the same rank** | **Rank only. Suit is irrelevant.** Taking the Q♦ closes off *every* Queen. |

> 🔥 **This is a *third* identity notion, and it is the one trap in the rule.** The two the game
> already has are set out in `BUILD-PLAN.md` §3.1 and are both on `Card`: `==` is **instance**
> identity (it includes the `CardId`, so the two 5♥s are not equal) and is what the exact-cover
> search uses; `SameValueAs` is **value** identity — *rank **and** suit **and** colour* — and is
> what money-card designation uses, because §4.2 matches a turned-up 5♥ to only the two 5♥s.
> **§5.1 matches on neither.** It is **rank alone**, which no predicate on `Card` expresses today.
> ⚠️ **Reaching for `SameValueAs` here would silently implement the wrong rule** — it would close
> only the Q♦s and leave the Q♣ that Mya Lay actually objected to perfectly legal. §4.2's
> narrowness and §5.1's breadth are unrelated decisions that happen to be about the same word.

> **`OPEN` — a joker has no rank at all.** `Card.Rank` is `null` for a joker, so *"the same rank"*
> has nothing to bite on: taking a joker in the open closes **nothing**, read literally. Four
> jokers are in the shoe and a joker is the most valuable card a rummy has, so *"you may not feed
> the player the joker they just took"* is very much the rule's spirit — but the spirit is not the
> rule. Tracked as §9 **#27**; the recommendation there is that **taking a joker closes jokers**.

**Why the rule exists.** Taking a card in the open announces what you are collecting. Handing the
collector another one is not bad luck, it is a gift, and the seat upstream of you should not be
able to hand you the game. Note that this is a **melding** rule, not a money rule: by §4.4 a card
picked up from a discard is *held but never owned*, so taking the Q♦ earned Mya Lay no money at
all. What it earned her was a visible second Queen.

**The ban accumulates and is tracked per rank.** Take a Queen and a 5 and both ranks are closed
to the seat above you, each independently.

**Everything about the ban is per round.** It is armed by takes made this round and released by
discards made this round, and it is **wiped by the deal** — hands and shoe are rebuilt each round
(§3), and a `CardId` names a card in *a round's* shoe, not across rounds. Nothing about the ban
carries into the next round.

#### Two exceptions

**1. Release — the protected player throws that rank back.** If the protected player themself
discards a card of that rank, the ban on that rank **lifts for the rest of the round and does not
come back** — not even if they later take another card of that rank. Discarding a Queen is a
public statement that you are not collecting Queens, and it is not retracted by a later pickup.

> 🔥 **This is what makes the rule cheap to enforce.** The release is not a history of who took
> what and when — it is a **set of ranks per seat**, one bit of state a rank. *"Has she thrown a
> Queen this round?"* settles it, and nothing about the order or the timing matters.
> ⚠️ **At a table that question is answered by looking down her discards — legitimately, since
> rev 17 closed §9 #15 and the discards are public — but in code it must not be.** §5 sweeps every
> discard pile back into the draw pile when the deck runs out, which would take the evidence away
> and silently re-arm a released rank mid-round — so the set is *kept*, not *read back*. See §9 #19, which is the same point as an open question about the rule rather
> than about the implementation.

**2. Going out — the winning discard.** The ban never stands between a player and a win. If the
discard would be the discarding player's **declaring discard** — the 14th card thrown immediately
before laying down all 13 (§7.1) — they may throw the banned rank.

> **`DERIVED` — why this exception costs nothing.** The round ends on that discard. The protected
> player never gets a turn in which to take the card, so the gift is never delivered.

#### The playtest, worked through

Mya Lay opened, so the seat that discards to her is the **last** seat in the rotation. On her
first turn she took the **Q♦**, a money card that round. Queens were closed to that last seat
from that moment. Later in the round it discarded a **Queen of another suit** — and that is the
violation she called. Neither the suit nor the Q♦'s money status is what made it one: the seat had
watched her take a Queen in the open, and gave her a second.

It would have been a legal discard if **either**:

- Mya Lay had discarded any Queen earlier in the round — in which case Queens stay open for the
  rest of the round, even if she picks another one up afterwards; **or**
- that seat was **going out** on it.

#### Enforcement: a banned discard is not a move — `PLAYER`, Settled

**There is no infraction and no remedy, because there is nothing to retract.** A card of a banned
rank is **not a legal discard**. It is never offered, it cannot be chosen, and the question *"what
happens if somebody throws one anyway?"* has no answer because the situation cannot arise. At a
table the other players simply would not let the card land; in this implementation the card is not
a choice the turn presents.

That is a stronger statement than *"it is against the rules"* and it has three consequences worth
writing down before anybody builds it:

1. **The upstream seat has to be *told* which ranks are closed** — you cannot make a move
   impossible for a player who does not know it is impossible. This costs the concealment model
   nothing, but the reason is worth being exact about, because it looks at first like a collision
   with §9 #15. **The ban is computed from things that happened in the open, not from reading a
   pile.** The seat below **took** its card in view of the table, and every discard that would
   release a rank was **thrown** in view of the table — so the upstream player knows the closed
   ranks by having *watched*. ✅ **Rev 17 then closed #15 the permissive way — the discards are
   public and may be looked through** — so the reading is doubly safe: the rule needed nothing
   concealed even under the strict answer, and it certainly needs nothing under the actual one.
   ⚠️ **The corollary is that the released ranks are bookkeeping, not a pile**: an implementation
   carries a set per seat and does not read the discards back (see #19, where a reshuffle takes the
   pile away and the release must survive it). `TurnContext` carries none of this today.
2. **Every agent's discard ranking must be filtered to the legal cards**, not merely scored and
   then checked. This includes the *runner-up*: a difficulty level makes its mistake by throwing
   the card its rung ranked **second**, and a mistake must still be a legal move.
3. **The set of legal discards must never be empty** — see the floor below. Impossible-move
   enforcement removes the social escape hatch, so the floor is not a nicety; without it a rare
   hand is a turn that cannot be completed.

#### The floor: the ban never leaves you unable to move — `PLAYER`, Settled

**If the ban would leave a player with no legal discard at all, the ban yields for that turn and
every card in the hand becomes legal again.** The discard is mandatory (§7.1); the ban is not.

This is reachable, barely. It needs all fourteen cards to be of banned ranks, and two ranks are
enough to do it — two decks put eight copies of a rank in the shoe — so it is vanishingly rare and
not impossible.

Three things the ruling fixes:

- **The yield is for that turn only.** It suspends nothing permanently. Draw a legal card next turn
  and the closed ranks are closed again.
- **It is not a release.** §5.1's release is the *protected* player throwing that rank back; a
  yielded discard is thrown by the player above them and frees nothing.
- ⚠️ **It is not §9 #6, and rev 17 closed #6 outright.** Earlier drafts of this section wondered
  whether the deadlock was the unrecovered exception to the mandatory discard that Nick remembers.
  **It is the opposite.** Nobody skips a discard here; the mandatory discard is what the ban gives
  way *to*. ✅ **And there is no such exception at all** — asked flat on 2026-08-20 and answered
  *no exceptions, you must discard* (§7.1). **That independently confirms this ruling**: the floor
  had to fall this way, because the rule it gives way to admits of no exception.

> 🔥 **`DERIVED` — this is one line in an implementation, and it is the line that makes
> impossible-move enforcement safe.** The legal discards are the hand minus the banned ranks, *or
> the whole hand if that is empty* — **so the choice presented to a player is never empty, by
> construction.** A turn cannot deadlock, and there is no unreachable state left unhandled to
> crash on.

> **`DERIVED` — §5.1 is subordinate to both of the things that keep the game moving.** Its two
> exceptions and its floor are the same shape three times over: **going out** outranks the ban
> (exception 2), and the **mandatory discard** outranks the ban (the floor). The feeding rule
> shapes an ordinary turn and gives way to anything that would otherwise stop one.

#### What this does not yet say

**None of this is implemented.** `RoundEngine` accepts any discard, `TurnContext` shows a seat only
the one discard it is being offered, and no agent knows the rule exists.

✅ **But the specification is now complete but for one case.** Every detail that was unrecorded on
2026-08-20 was settled the same day, and 🔥 **the narrow reading was confirmed on every single
one** — *the seat above you only, ranks only, public takes only, the round only*:

| Was | Question | Answer |
|---|---|---|
| #16 | Does the ban bind only the seat above you, or every seat? | **Only the seat above you.** `EXPERT` |
| #17 | Does a **blind draw** arm the ban? | **No — only public takes.** `EXPERT` |
| #18 | Any rank taken, or only money cards? | **Any rank.** `EXPERT` |
| #19 | Does a release survive the reshuffle? | **Yes** — ⚠️ a house ruling, not a recollection; see below |
| #27 | A joker has no rank — what does taking one close? | **The other jokers** — ⚠️ likewise |
| #20 | No legal discard at all? | **The ban yields** (the floor, above) |

⚠️ **#19 and #27 are not the same kind of answer as the other three, and the difference is
recorded rather than flattened.** #16, #17 and #18 are rulings from players who play the game.
**#19 was decided rather than recalled** — *"let's go with yes, this is a severe edge case so
nobody really knows"* — and **#27 was reasoned rather than recalled** — *"other jokers I'd assume,
I don't see any other interpretation."* Both are `PLAYER` house rulings taken so the build can
proceed, both are the reading that changes least, and **both are exactly the shape of thing this
document has been wrong about before** (§6.2, §7.1). If either is ever put to a player as a table
situation and comes back differently, it is a rule change and not a correction to a typo.

✅ **And the last case — the two-handed table, where the seat that feeds you is the seat you feed
— was ruled on the same day: the rule is the same in every game.** No player-count branch. The
mutual lock described below is a **legal state of the game**, not a defect to be special-cased.

#### Two-handed: the mutual lock — `PLAYER`, Settled

Two players alternate, so **each is simultaneously the feeder and the fed**. The bans run in both
directions independently, which is coherent as far as it goes:

> A throws the **Q♦**. B takes it in the open. **Queens are now closed to A**, because A is the
> seat that discards to B. Later B throws the **7♣** and A takes it: **sevens are now closed to
> B.** Two bans, two directions, no conflict.

🔥 **The deadlock is what happens when the *same* rank is taken in both directions.**

> A throws a **Queen**; B takes it → **Queens closed to A**.
> Later B throws a different **Queen**; A takes it → **Queens closed to B**.
>
> **Neither player may now discard a Queen for the rest of the round.**

And the release cannot clear it. A rank re-opens when **the protected player throws that rank
back** — so B's ban lifts only when **A** throws a Queen, and A's ban lifts only when **B** throws
one. **Each release is blocked by the other ban.** Both players may sit holding Queens that
neither can ever shed, until somebody goes out (exception 2) or a hand becomes entirely
unthrowable (the floor).

> **`DERIVED` — this is not unique to two-handed; it is merely *cheap* there.** The same lock
> needs a full cycle of takes at any table size — three-handed it takes all three seats taking a
> Queen, four-handed all four. **Two-handed it takes two takes**, and with eight copies of every
> rank in a two-deck shoe and only one opponent to take from, that is an ordinary round rather
> than a curiosity.

**The ruling: the rule is the same in every game** — `PLAYER`, Settled, 2026-08-20. There is no
two-handed variant of §5.1 and no player-count branch in it. ⚠️ **Decided rather than recalled**,
like #19 and #27, and taken because a rule that changes shape with the table size is a second rule
— and §7.1.1 is quite enough of that for one document.

> 🔥 **`DERIVED` — what a dead rank actually costs, which is less than it first looks and then
> suddenly more.** A banned card is not unplayable, only **unthrowable**. Three ways out, and the
> third is the one that bites:
>
> 1. **It melds.** A dead Queen inside a run or set never needs discarding, and the ban costs
>    nothing at all.
> 2. **It is your declaring discard.** Exception 2 lets a banned rank be thrown as the 14th card
>    immediately before laying down thirteen — so **one** unmeldable dead card is survivable.
> 3. ⚠️ **Two are not.** You may throw exactly one card on the turn you go out, so a player
>    holding **two** unmeldable dead cards cannot win the round at all: they cannot shed either
>    one before declaring, and declaring requires all thirteen to meld. **The floor does not help**
>    — it fires only when *every* card in hand is banned, and eleven live cards keep it shut.
>
> **So the lock does not stop the round and does not stop a turn; it can quietly remove a player
> from contention.** That is a real consequence of two settled rules meeting, and it is recorded
> here so nobody later reads it as a bug in whatever implements §5.1.

> ⚠️ **A strategy note, not a rule.** Two-handed, a public take is not merely acquisitive — **it
> is an attack**, because it closes a rank to the only other player at the table and there is
> nobody else who might have thrown it. Nothing in this document says a player should think that
> way; `docs/STRATEGY.md` is where that would have to be measured, and it never has been.

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
| **There is no exception. The discard is always mandatory.** | `EXPERT` | Settled |
| **The *required* series must be pure** — joker-free — and only where series are required at all (§7.1.1). | `EXPERT` | Settled |
| **What the thirteen must contain depends on how many are playing** — see §7.1.1. | `EXPERT` | Settled |

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

> ✅ **The exception does not exist — closed in rev 17 (§9 #6).** Asked flat — *"is there ever
> a time you can go out without throwing a card away?"* — and answered **no exceptions, you must
> discard**. Nick recalled an exception; there is none. The turn is *take one, throw one*, every
> turn, including the one you win on. **This is the rare case of a question closing by removing
> a rule rather than adding one**, and it makes §7.1 simpler than it has been since rev 1.
>
> ✅ **And it settles which way §5.1's floor had to fall.** If the discard is unconditionally
> mandatory, then a player every one of whose fourteen cards is a banned rank cannot be left with
> no move — so **the ban is what yields**, which is exactly the ruling §5.1's *floor* already
> took. The two answers were reached independently and agree: the mandatory discard outranks the
> feeding ban. ⚠️ **What this does *not* do is supply §9 #6's missing exception** — there is no
> missing exception, which is the whole of the answer.

> ✅ **Purity is a rule — closed in rev 17 (§9 #7), against the standing recommendation.** Asked
> flat — *"does at least one of your runs have to be made without any jokers in it?"* — and
> answered **"one of your runs has to be clean"** — ⚠️ **and corrected the same day: purity
> attaches to the *required* series and to nothing else** (§7.1.1). Rev 1 through 16 recommended treating purity
> as not-a-rule, reasoning that melding all 13 is already stricter than Indian Rummy and so
> needs no such safeguard. **That reasoning was wrong, exactly as it was wrong about duplicate
> suits in sets** (§6.2) — and it was wrong the same way, by arguing from the parent game's
> *purpose* rather than asking. ⚠️ **Where `IR` and a recollection disagree, `IR` has now won
> three times out of three.**
>
> ✅ **Its scope *is* settled, and it is narrower than the first answer sounded.** Purity is not a
> property of the hand; it is a property of **the series the table size requires**. Three-handed,
> **both** required series must be clean; four-handed, **the one** required series must be clean.
> Two-handed and five-or-more-handed, **cleanliness is irrelevant** — at two because the
> requirement is *series only* and says nothing about jokers, at five-plus because no series is
> required for purity to attach to. 🔥 **So the same rule that decides how many series you need
> decides how many of them must be clean, and the two move together**: 0 required → 0 clean, 1 →
> 1, 2 → 2. §7.1.1 carries the table.

### 7.1.1 What the thirteen must contain — by player count — `EXPERT`, Settled

**Recorded 2026-08-20 from Mya Lay and Aung Aung.** 🔥 **This is the first rule in this document
whose *content* changes with the number of players**, and it is why §2's "four minimum" was
wrong: two- and three-handed are real games, played to a stricter hand.

A **series** is a run (§6.1). A **set** is §6.2.

| Players | What a declared hand must contain | …of which **clean** (joker-free) | So the thirteen partition into |
|---:|---|:---:|---|
| **2** | **Series only. Sets are not allowed at all.** | **none required** | runs, and nothing else |
| **3** | **At least two series.** | **both** | ≥ 2 runs, ≥ 2 of them clean |
| **4** | **At least one series.** | **the one** | ≥ 1 run, ≥ 1 of them clean |
| **5 or more** | **No requirement.** | **none required** | any valid melds |

🔥 **Purity is not a property of the hand — it is a property of the series the table size
requires, and the two columns move together.** Nought required, nought clean; one and one; two and
two. **Two-handed is the case that shows it is not simply "all series must be clean"**: every meld
there is a series and yet cleanliness is irrelevant, because the requirement is about *sets being
banned* and says nothing about jokers. ⚠️ **A first reading of the answer — recorded earlier the
same day as "at least one of your runs must be clean" — was a flat rule over the whole hand and
was wrong.**

> **`DERIVED` — the pattern is a compensation, and it reads straight off §2.1.** A two-handed
> player has about **forty** draws from an 80-card pile; a six-handed player has **four or
> five** from 28. The fewer the players, the more of the deck each one sees, and the easier a
> hand is to complete — so the requirement tightens exactly as the deck opens up. **The game
> holds its difficulty roughly constant across table sizes by moving the win condition, not the
> hand size.** That is a deliberate-looking piece of design and it is worth recording as the
> reason the table has the shape it does. It is reasoning *about* the rule; the rule is the
> table above.

> ⚠️ **Three things this does not say, and an implementation needs all three** — §9 #22, #23,
> #28.
>
> 1. **At two players, is a set illegal *as a meld*, or merely not required?** The answer
>    recorded is *"sets are not allowed at all"*, which reads as the strong form — a declared
>    two-handed hand is runs and nothing else. It is written that way above because that is what
>    was said, but the strong form has never been put back to a player as a table situation. (#22)
> 2. 🔥 **What counts as *two* series?** Thirteen cards holding one six-card run can be declared
>    as `3+3` or as a single `6`. If the count is of melds *as laid down*, a three-handed player
>    satisfies "at least two series" out of one run by splitting it, and the requirement is
>    nearly free. If it is a count of *distinct* runs, it is a real constraint. **The whole
>    weight of the three-handed rule sits on this and nothing settles it.** (#23)
>    ⚠️ **The purity column makes this worse, not better.** Split a clean six-card run into `3+3`
>    and you have satisfied *two clean series* out of one run — so under the permissive reading
>    the three-handed rule costs a player almost nothing, and under the strict one it is the
>    hardest condition in the game. **The gap between the two readings is now the whole rule.**
> 3. **Does a *surplus* series have to be clean too?** Three-handed with three runs: are two
>    enough, or must all three be joker-free? The rule as recorded attaches purity to the
>    **required** count, so two — but a table that says *"both of your series must be clean"* has
>    not been asked about a third. The same question arises four-handed with two runs. (#28)

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
(§6.2), and the melding model (§6.3) — are resolved. What is left is unrecorded rather than
disputed.

⚠️ **Three groups, and they are not the same kind of question.** #4, #5 and #8–#14 are ordinary
gaps with safe defaults, and none blocks anything. ✅ **§5.1's unwritten half — #16–#20, #25 and #27 — was written on 2026-08-20 and none of it is
open any more**; the rule is now fully specified and still entirely unimplemented. 🔥 **#22, #24, #25 and #26 are new in rev 17** and are
the unwritten half of the answers that came *with* §7.1.1 — a rule that changes the win condition
by table size, recorded from the experts in one sentence per player count, with none of the edges
put back to them.

| # | Question | § | Status | Blocks |
|---:|---|---|---|---|



| 4 | Do the turned-up cards stay out of play permanently? | 4.5 | Open — recommend *yes*, **taken as the default by P9** | Deck exhaustion |
| 5 | Is the money-card claim once per game or per round, and who approves it? | 4.5 | Unknown — **P9 defaults to *per round*, approved by nobody** | Turn logic |
| 8 | Max jokers per meld — and may a meld be **entirely** jokers? | 6.1 | Unknown — recommend *unlimited*, which permits an all-joker meld | Meld validation |
| 10 | Why 7♦ and A♠ specifically? | 4.1 | Unknown, likely unrecoverable | None |
| 11 | If a **joker** is turned up as a money card, what does it designate? | 4.1 | Unknown — recommend *the two jokers of that colour* | Money designation (P2) |
| 12 | When the opening player **claims** the turned-up money card, does that card's value still pay for the rest of the round? | 4.5 | Unknown — recommend *yes* | Money designation (P7) |
| 13 | May a player discard the very card they just took? | 5 | Unknown — recommend *yes* | Turn logic (P7) |
| 14 | Does anything move between rounds — the seating, the deal, who goes first? | 3 | Unknown — recommend *no* | Match setup (P9) |
| 22 | At **two players**, are sets **illegal as melds**, or merely not required? | 7.1.1 | Unknown — recorded as *illegal*, which is what was said; the strong form was never put back | Win detection |
| 24 | The money-card follow-ups: **both copies, both decks?** What if a turned-up card **is** the 7♦? Can it stack past a **double**? | 4.1 | Unknown — put on 2026-08-20 and not reached; the current answers are `CODE` | Money designation |
| 26 | Does **anything else** change with the player count — hand size, number of decks, the money cards, the stakes? | 2 | Unknown — recommend *no*, but §7.1.1 is proof the question is real | Setup |
| 29 | Does an **all-joker meld count as a series** for the §7.1.1 requirement? It can never be a *clean* one, but it may still be a surplus. | 7.1.1 | Unknown — recommend *yes, it is a run like any other*; raised by §7.1.1 meeting #8 | Win detection |


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

> **#15 raised in rev 14, while planning the strategy programme (P17–P23).** §5 says the only
> public information is the discards, and lists per-player piles against one shared pile as
> *largely moot* (#9) — **it stops being moot the moment a player counts cards.** Whether a pile
> may be picked up and read, or whether only the card on top of it is visible, decides how much
> a player can know about what is left in the shoe, and it is the difference between a game of
> memory and a game of the current hand.
>
> Nothing has needed the answer until now: every bot built so far reasons from its own thirteen
> cards alone. **P20's counting rung is the first player of any kind that would use it**, and
> `TurnContext` — the type that *is* the concealment rule — currently shows a seat only the one
> discard it is being offered, which is **less** than §6.3 makes public and less than the browser
> client already draws to every watcher.
>
> **P20 takes the safe default: a seat may count only what it has actually been shown** — its own
> cards, the cards it took, the discard offered to it on each of its turns, and the turned-up
> cards. If the answer turns out to be that piles are inspectable, this bot is merely *weaker*
> than the rules allow; the opposite default would have it **seeing what the rules conceal**, and
> a bot that cheats is a worse error than a bot that is beatable. Reversing it widens one
> property on `TurnContext` and changes no rule.

> **#16–#19 raised in rev 16, by the playtest that produced §5.1** (with a #20 that was raised and
> closed the same day — see below; **#27** was added later in the same revision, and **#25** by rev
> 17). These are different in kind from everything above them. The
> other fifteen questions are details around rules that are *built*; §5.1 is a **settled rule with
> no implementation at all**, so these are not "worth settling for fidelity" — the first packet
> that implements feeding has to answer every one of them to compile. **#27 was added later in the
> same revision**, by checking §5.1's *"the same rank"* against `Card` and finding that a joker
> does not have one; **#25 arrived with rev 17**, because a two-handed table folds the rule in
> half.
>
> **#16 — who is bound.** Only the seat above you can hand you a card, because only its top
> discard is takeable (§5), so binding the whole table would forbid discards that can never
> reach the protected player. **Recommend the narrow reading**: the ban is a property of an
> *ordered pair* of adjacent seats, not of the table. The wide reading is worth a question to
> Mya Lay all the same, because a rule about *fairness* can easily be a table rule rather than
> a neighbour rule.
>
> **#17 — what arms it.** A blind draw is concealed; nobody at the table knows what you took, so
> a ban armed by one is unenforceable and, worse, invisible to the player it binds.
> **Recommend: only a card taken in the open arms the ban** — the discard offered to you, and
> the turned-up money card claimed on the opening turn (§4.5). The playtest needs the second of
> those: Mya Lay **opened**, so there was no discard in front of her, and the claim is the only
> public way she could have come by the Q♦.
>
> **#18 — money card or any card.** The Q♦ was a money card that round and it is tempting to read
> the rule as protecting the money. It cannot be: by §4.4 a card **picked up** from a discard is
> held but never owned and pays nobody, so taking the Q♦ moved no money whatsoever.
> **Recommend: any rank.** The advantage being denied is a melding advantage, and it is the same
> advantage whatever the card is worth.
>
> **#19 — a release across a reshuffle.** §5.1's cheap test is *"is that rank in the protected
> player's discards?"*, and §5 sweeps every discard pile back into the draw pile when the deck
> runs out — which would silently **re-arm** every released rank in the middle of a round.
> **Recommend: the release is remembered.** It is a fact about what a player did, not about where
> a piece of card is lying; the pile is where it is normally written down, not what it is. An
> implementation therefore needs a released-rank set per seat and cannot lean on the pile alone.
> The ban itself is **per round** in either reading — hands and shoe are rebuilt each round.
>
> **#20 was raised and closed inside rev 16, and the closing made §5.1 stronger rather than
> looser.** It asked two things. **What is the penalty for discarding a banned rank?** — *there is
> none, because it is not a move you can make*: a banned card is never offered and cannot be
> chosen (§5.1, Enforcement). **And what happens when the ban leaves a player with nothing legal to
> throw?** — *the ban yields for that turn* (§5.1, The floor).
>
> 🔥 **The two answers are load-bearing together and neither is safe alone.** Impossible-move
> enforcement takes away the social escape hatch a rare hand would otherwise rely on, so without
> the floor a fourteen-card hand of banned ranks is a turn that cannot be completed — an
> unreachable state that, unhandled, is a crash rather than a rare event. With the floor, the legal
> discards are *the hand minus the banned ranks, or the whole hand if that is empty*, and **the
> choice presented to a player is never empty by construction**.
>
> ⚠️ **One speculation this document made and then had to withdraw**: while #20 was open it
> wondered whether the deadlock was the unrecovered exception to the mandatory discard in **#6**.
> It is not — it is the reverse. Nobody skips a discard under the floor; the *ban* is what gives
> way. ⚠️ **Rev 17 then closed #6 outright — there is no exception, you always discard** — which
> settles the speculation in the same direction and makes the floor the *only* way §5.1 and the
> mandatory discard can both hold.

> **#25 — the ban two-handed, where the seat that feeds you is the seat you feed.** Rev 17 made
> two-handed a real game (§2), and §5.1 was recorded at a table of five. Head to head the rule
> folds in half: there is one ordered pair of seats and it points both ways at once, so **each
> player is simultaneously the protected player and the only seat bound by the ban** — and every
> release either of them makes frees the other. Whether that is the rule or an accident of a rule
> written for a ring is exactly what has not been asked.
>
> 🔥 **There is an argument from §7.1.1 that it may not apply, and it is worth putting to her with
> the question.** Two-handed, a declared hand is **series only — sets are not allowed at all**. A
> run is built from *consecutive cards of one suit*, so a rank-mate of a card the opponent took is
> very nearly useless to them: taking the Q♦ says they may want the J♦ or the K♦, and it says
> almost nothing about the Q♣. **The rank-only match (§5.1) is a good proxy for "what they are
> collecting" only in a game where sets are legal**, and two-handed is precisely the game where
> they are not. So the ban may be a ring-game rule, or it may bite on suit-neighbours instead, or
> it may apply unchanged and simply cost little. ⚠️ **Do not offer her any of those three** — ask
> the situation flat and record what comes back.

> **#27 — the joker, which has no rank.** §5.1 closes *ranks*, and `Card.Rank` is `null` for a
> joker, so read literally **taking a joker in the open closes nothing** and the seat above may
> keep throwing jokers at a player who has visibly just taken one. Four jokers are in the shoe
> (§2) and a joker is the most valuable card a rummy has, so that reading is hard to believe of a
> rule whose whole point is *do not hand the collector what they are collecting*.
>
> **Recommend: taking a joker closes jokers** — the rule applied to the only identity a joker has,
> rather than a new rule. ⚠️ **Colour is the question underneath it**, and it is §4.1's turned-up
> joker (#11) in a second place: `Card` tells the two jokers of a deck apart by `Color`, so *"a
> joker"* could mean all four or only the two of that colour. **Recommend all four**, because #11
> is about *designation*, where §4.2 makes matching deliberately narrow, and this is about
> *feeding*, where §5.1 makes it deliberately broad — the two should not be assumed to agree.
> ⚠️ Note that #18 does not settle this: *any rank* still says nothing about a card that has none.

**Nothing above #16 blocks the build**; each of those has a safe default recorded in
`BUILD-PLAN.md`, and they are worth settling for fidelity rather than for progress. **§5.1's set —
#16–#19, #25 and #27 — is not like that**: the rule is unimplemented, and they are its
specification.

> **Resolved in rev 17** — twelve questions in one day, from the session with Mya Lay and Aung
> Aung on 2026-08-20. ✅ **§5.1's specification is complete.**
> - **Does the feeding ban work two-handed** (was #25): **the rule is the same in every game.**
>   No player-count branch; the mutual lock is a legal state. ⚠️ Decided, not recalled.
> - **Does the feeding ban bind every seat** (was #16): **no — only the seat above you.** `EXPERT`
> - **Does a blind draw arm the ban** (was #17): **no — only public takes.** `EXPERT`
> - **Any rank, or only money cards** (was #18): **any rank.** `EXPERT`
> - **One pile or a pile each** (was #9): **a pile each.** `EXPERT` 🔥 This is what makes §5.1's
>   release rule readable off the table, and nobody knew it was load-bearing until #9 un-mooted.
> - **Does a release survive the reshuffle** (was #19): **yes** — ⚠️ **decided, not recalled**
>   (*"nobody really knows"*). `PLAYER`, and it stays flagged as a house ruling.
> - **What does taking a joker close** (was #27): **the other jokers** — ⚠️ **reasoned, not
>   recalled** (*"I'd assume"*). `PLAYER`, likewise.
> - **Does *"at least two series"* count melds as laid down** (was #23): **yes — a longer run may
>   be split.** `3+3` out of a six-card run is two series. 🔥 **The load-bearing one**: it makes
>   the three-handed requirement far weaker than it read, and it changes the shape of the win
>   question (§7.1.1).
> - **Does a surplus series have to be clean** (was #28): **no.** Purity attaches to the required
>   count and stops there.
> - **The exception to the mandatory discard** (was #6): **there is none.** You always discard.
>   The exception Nick recalled does not exist. ✅ It also settles which way §5.1's *floor* had to
>   fall — the ban yields, because the rule it gives way to admits of no exception.
> - **Is a pure sequence required** (was #7): **yes — the series the table size *requires* must
>   be clean**, and only where series are required at all: both of them three-handed, the one of
>   them four-handed, none two-handed or five-plus (§7.1.1). This reverses a recommendation that
>   stood from rev 1 to rev 16. ⚠️ **It also closes #21 on the day it was raised** — the first
>   recording of this answer was a flat "at least one series must be clean" over the whole hand,
>   which left the five-plus case open; the correction removed the question rather than answering
>   it, because purity has nothing to attach to where no series is required.
> - **Is a discard pile inspectable** (was #15): **yes, the discards are public.** This reverses
>   the cautious default P20 took, and makes P20's null result conditional (§10 #14).
>
> **Resolved since rev 4:**
> - **The feeding ban's enforcement and its floor** (was #20, raised and closed in rev 16):
>   discarding a banned rank is **not an infraction but an impossible move**, and where the ban
>   would leave a player no legal discard at all **the ban yields for that turn** (§5.1).
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

> ✅ **Note on #9 — it stopped being moot in rev 17, and was then answered: one pile each.**
> The old reasoning held: if only the top discard is takeable, a shared pile and per-player piles
> are observationally identical, because the top of a shared pile *is* the previous player's
> discard. **Both halves of that have now failed.** The discards are **public** and may be looked
> through (§5), so a pile is read rather than glanced at; and §5.1's release rule turns on
> whether a rank appears in **the protected player's own** discards, which a single pile in the
> middle cannot answer by inspection. **A settled rule turned out to depend on it** — and the
> answer, given the same day, is **per-player piles**, which is precisely what makes the release
> rule readable at the table.

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
7. **Make player count configurable, 2–6** (§2). ⚠️ **Rev 17 moved the floor from four to two**, and `RoundEngine.MinimumPlayers` is 4.
8. **Add configurable stakes** — round value and money card value (§4.3).
9. **Implement settlement** (§7.2): flat round payment, then pairwise money-card settlement.
10. **Set `MoneyCardOwner` during the deal** (§4.4), pending confirmation.
11. **Implement sets** (§6.2) and **the win condition** (§7.1).
12. **Handle deck exhaustion** (§5). Currently crashes.
13. **Enforce the feeding ban** (§5.1) — **by construction, not by validation.** A banned rank
    is not a legal discard: it is never offered and cannot be chosen. Nothing implements any of
    it. `RoundEngine` accepts any of the fourteen; `TurnContext` does not carry the banned ranks,
    though every fact they are computed from is already public; and every agent's discard
    ranking — **including the runner-up a difficulty level throws as its mistake** — must be
    filtered to the legal cards — and the legal set is **the hand minus the banned ranks, or the
    whole hand if that is empty** (§5.1, The floor), so it is never empty and a turn cannot
    deadlock. ⚠️ **The banned-rank test is rank alone** — neither `==` nor `SameValueAs`, both of
    which are narrower — so it needs a predicate `Card` does not have. Needs §9 #16–#19, #25 and
    #27 answered first.
14. 🔥 **The win condition is a function of the table size** (§7.1.1) — **both how many series a
    hand must contain and how many of those must be joker-free**, and the two counts are the same
    number (§7.1). `HandEvaluator` implements **neither**: it asks only whether thirteen
    cards partition into disjoint valid melds, which is the five-or-more-handed rule, and it
    accepts a hand whose every run carries a joker. ⚠️ **This is the largest single divergence in
    this list**, because unlike the rest it does not add a rule to the engine — it changes what
    *winning* means at three of the four table sizes, and every strategy figure in
    `docs/STRATEGY.md` was measured at four seats under the wrong condition. ⚠️ **And it is not a
    filter over the existing search** — §9 #23's answer makes the requirement a property of the
    *partition chosen*, so the evaluator may no longer return the first cover it finds (§7.1.1).
    Only §9 #22 is still open against it.
15. **The discards are public** (§5). `TurnContext` shows a seat only the single discard offered
    to it, which is **less** than the rules allow and less than the browser already draws for a
    watcher. ⚠️ **P20's card-counting null was measured under the narrower reading** and is
    conditional on it; widening this is the one change that could reopen a published result.

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
