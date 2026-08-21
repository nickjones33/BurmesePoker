# Burmese Poker — Rules Primer

> **`RULES.md` is the canonical source.** This is a one-page recall aid. Where the two
> disagree, `RULES.md` wins. Confidence tags here are abbreviated — see `RULES.md` for
> full provenance on every rule.
>
> Tags: **[✓]** settled · **[~]** probable · **[?]** tentative or unknown ·
> **[⚠]** code currently disagrees

---

## The shape of the game

A rummy played for money. Two decks, big hands, draw-and-discard, race to meld everything.
Layered on top is a side-bet: certain cards are **money cards** that pay out regardless of
who wins the hand. That money layer is what makes this game *this* game — and it's the part
with no documentation anywhere. It's a house rule.

The closest documented relative is **Indian Rummy**, which matches on nearly every
structural point (see `RULES.md` §8).

## Setup **[✓]**

- **Agree two stakes before playing:** a **round value** and a **money card value**.
  Standard is **$5 / $1**. **[✓]**
- **Two decks shuffled together, jokers in — 108 cards.**
- **2 to 6 players** — and **the rules change with the count**, see *Winning*. More than 6
  leaves too thin a draw pile. **[✓]** **[⚠ engine's minimum is 4]**
- **13 cards** each, dealt one at a time.
- Then **two money cards are turned up** — one from the bottom of the deck, one from the top.

## Money cards

Two ways a card becomes one:

1. **Permanently** — the **7♦** and **A♠**, every round, all copies. **[~]**
2. **For this round** — the two turned-up cards, and their matching copies. **[✓]**

A turned-up card that *is* the 7♦ or A♠ becomes a **double** money card. Doubling is the
ceiling. **[~]**

**Payout: each money card pays its *owner* the money card value from every other
player** — $1 each, at standard stakes. Own two in a 5-player game → **$8**. A double
counts as two. This settles regardless of who won the round. **[✓]**

**Ownership, not possession — and ownership is permanent.** Two parts, the second
counterintuitive:

1. You **own** a money card only if the **deck gave it to you** — **dealt in your opening 13,
   or drawn blind during play**. Both count. Cards picked up from a discard, or claimed off the
   table, are held but **never** owned. **[✓]**
2. **Ownership never transfers and is never lost.** Discard an owned money card and **you are
   still paid for it** — even if an opponent picks it up. **[✓]**

→ This is deliberate, and it's why the game plays cleanly: discarding a money card costs you
nothing, so you never have to choose between winning the hand and hoarding cash. Play to win;
the money settles on what the deck dealt you.

→ Because the deal counts, **most of the money is decided before anyone plays a card** — in a
5-player round roughly 4 of the ~6 live money cards are already owned when the deal ends.

**Exact match only.** A turned-up 5♥ makes money cards of **just the two 5♥s**, not all
the fives. **[✓]**

## A turn **[✓]**

1. Take either the previous player's discard, or the top of the deck blind.
2. Discard one card.

**You may not feed the next player. [✓] [⚠ not implemented]** If the player after you has
taken a card in the open, you may not discard **that rank** again — *any* suit — for the rest
of the round. Two ways out: they discard that rank themselves, which opens it up **permanently**
even if they pick another one up later; or you are **going out** on it. Rank taken → rank closed,
one rank at a time. **It isn't a foul, it's not a move** — a banned card simply isn't one you can
throw, so there's no penalty to argue about. And if the ban ever left you with **nothing** legal to
throw, **the ban yields** for that turn: you must discard, so you may. See `RULES.md` §5.1.

**The opening turn is special:** that player may take the turned-up top money card instead
of drawing — taking the **actual card**, which leaves the table. **[~]** **[⚠ code clones it]**
Whether this needs another player's permission, and whether it recurs each round, is
unknown. **[?]**

## Melds

**Runs** — 3+ cards, same suit, consecutive. **[✓]**
- **Aces don't wrap.** `A-2-3` ✓ · `Q-K-A` ✓ · `K-A-2` ✗ **[✓]** **[⚠ code allows K-A-2, and it causes a verified infinite loop]**
- Jokers substitute for a missing card. **[✓]**

**Sets** — 3+ cards of the same rank, **all different suits**. `9♥ 9♥ 9♠` is **not** a set,
even though two decks make it holdable. A set therefore caps at **4 cards**. **[✓]**
**[⚠ not implemented at all]**

## Concealment **[✓]**

**Nothing is ever played to the table.** You build entirely in hand and keep your intended
melds secret until you go out, then reveal all 13 at once. There is **no laying off** on
your own or anyone else's melds. The only public information is the discards — **and those are
properly public: you may pick a pile up and look through it, not just see the top card.** **[✓]**

## Winning **[⚠ not implemented]**

**Discard, then lay down all 13.** You hold 13, take one to make 14, **throw one away**, and
*then* reveal the thirteen melded. The discard comes first. **[✓]**

- **You always discard. There are no exceptions.** **[✓]**

**What the thirteen must contain depends on how many are playing** — this is the part people
forget, and **the runs you are *required* to have must be clean** (no jokers in them): **[✓]**

| Players | Your hand must contain | …clean? |
|---:|---|---|
| **2** | **runs only** — sets aren't allowed at all | doesn't matter |
| **3** | at least **two runs** | **both of them** |
| **4** | at least **one run** | **that one** |
| **5+** | anything — no requirement | doesn't matter |

*Fewer players means more of the deck each, so the hand is made harder to make up for it — and
the clean-run count is just the required-run count. Nought and nought, one and one, two and two.*

## Settlement **[✓]**

1. **Every loser pays the winner the round value** — flat, $5 at standard stakes. In a
   5-player game the winner collects **$20**.
2. **Money cards settle pairwise** — each owner collects the money card value per card from
   everyone else. The winner takes part in this too.

**No penalty for unmelded cards.** Losing costs exactly the round value whether you were
one card short or holding all thirteen. **[✓]**

→ So the game has **no points at all** — money is the only ledger, and `Player.Score`
should be deleted.

---

## What's still unrecorded

⚠️ **This section is stale** — both items below were settled long ago (deck exhaustion: gather
the discards and shuffle; match end: there isn't one, banks carry over and you stop when you
stop). **It is kept only so the rot is visible**; the live list is `RULES.md` §9, which currently
has **twenty** open items, none of which blocks play.

1. ~~**What happens when the deck runs out?**~~
2. ~~**What ends a match** — a money target, a fixed number of rounds, or last player standing?~~

Everything else has a safe default. Full list in `RULES.md` §9 — Mya Lay may be able to
settle several of them.

**One rule is settled but unwritten:** the feeding ban above (§5.1) is `EXPERT`-confirmed and
nothing in the code enforces it. Six details of it are unrecorded — `RULES.md` §9 #16–#19, #25
and #27 — and unlike everything else in §9 they are not optional: they are what the rule *means*.

Full list in `RULES.md` §9. Algorithms, edge cases and verified bugs in `RULES-TECHNICAL.md`.
