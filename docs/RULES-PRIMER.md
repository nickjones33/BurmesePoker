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
- **4 to 6 players.** Fewer than 4 isn't played; more than 6 leaves too thin a draw pile. **[~]** **[⚠ hardcoded to 5]**
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
your own or anyone else's melds. The only public information is the discards.

## Winning **[⚠ not implemented]**

**Meld all 13 cards, then discard.** You hold 13, draw to 14, meld 13, discard 1 — the
arithmetic closes cleanly. **[~]**

- There's **some exception** to the mandatory discard, not yet recovered. **[?]**
- A **pure** (joker-free) sequence is probably *not* required here — melding all 13 is
  already stricter than Indian Rummy's bar. **[?]**

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

Only two things block a complete playable game:

1. **What happens when the deck runs out?**
2. **What ends a match** — a money target, a fixed number of rounds, or last player standing?

Everything else has a safe default. Full list in `RULES.md` §9 — Mya Lay may be able to
settle several of them.

Full list in `RULES.md` §9. Algorithms, edge cases and verified bugs in `RULES-TECHNICAL.md`.
