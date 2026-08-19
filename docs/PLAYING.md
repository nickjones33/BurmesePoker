# Playing in the console

A short guide to sitting down alone against the computer. **`RULES.md` is the rules
authority** — this only explains the game as far as you need it to make the four decisions the
console asks you for, and points there for everything else.

---

## Starting a game

```bash
dotnet run --project BurmesePoker.Console
```

It needs a real terminal — it reads keys, so it refuses a pipe rather than crashing on one.

Then it asks four things:

| Question | What to say |
|---|---|
| **How many at the table?** | 4 to 6. A round is not played with fewer or more (`RULES.md` §2.1). |
| **How many of you are people?** | **1**, for solo play. The rest of the seats are filled by named bots — *Ruby (bot)*, *Sable (bot)*, … **0 is allowed**, and leaves the computer playing itself, which is worth watching once. |
| **How well should the computer play?** | *Hard* wins about a third of rounds, *easy* about a fifth. They differ in exactly one thing — see [Playing better](#playing-better). |
| **What do the stakes pay?** | A **round value** and a **money card value**, defaulting to $5 and $1. Both matter; the second is the whole side bet. |

Seating is shuffled, so you will not always open. The seat that opens is announced, and so is
the **seed**.

### Flags

```bash
dotnet run --project BurmesePoker.Console -- --seed 4242 --pace 250
```

| Flag | Does |
|---|---|
| `--seed <n>` | Plays a particular match. One is drawn and printed even when you do not pass it, so **any match can be replayed exactly** — same deals, same seating, same bots. Useful for showing somebody a strange round, or for reporting a bug. |
| `--pace <ms>` | How long a computer seat pauses before it moves. Default 450; `0` for none, which makes the bots' turns instantaneous. |
| `--no-hints` | Stops the console telling you what the computer would do. |
| `--help` | The same table, shorter. |

---

## Your turn, on screen

A turn begins by clearing the screen and asking whether you are the one at the keyboard —
play is **fully concealed** (`RULES.md` §6.3), and the game assumes anybody might be sitting
there. Then three panels:

**Round N so far** — everything public that happened while you were not looking: who drew,
who took whose discard, what was thrown. The bots move in milliseconds, so this panel is the
only trace of their turns. Read it first; it is the context the other two panels are read
against.

**The table** — the turned-up money cards, how many cards are left to draw, the one discard
you may take, and the stakes.

**Your hand** — not thirteen sorted cards, but the melds you nearly have:

```
┌─Your hand — 7 of 14 meld─────────┐
│ run   5♣  🃏B(6♣)  7♣  8♣        │
│ set   A♥  A♠ ($) ★  A♦           │
│ loose 6♥  7♥  A♥  8♠  9♠  J♦  Q♦ │
└──────────────────────────────────┘
```

`🃏B(6♣)` is a black joker standing in for the 6♣. The **loose** row is what is not melding
yet — the row you throw from.

The arrangement shown is *a* best one, not the only one. Thirteen cards of one suit in
sequence come back as four melds rather than one; that is the search reporting what it found,
not a judgement about your hand.

### The markers

| Marker | Means |
|---|---|
| `($)` | A money card. It pays its owner the money card value from **every** other player. |
| `($$)` | A double — it pays twice. |
| `★` | **You own it.** The deck gave you this card, so it pays *you*. |

**A money card with no star pays somebody else**, and you are holding it for them.

---

## The four questions

**1. "Take the turned-up money card instead of drawing?"** — only on the opening turn, and
only if you open. You take the actual card off the table, and it **costs you your draw**.
⚠️ **The table gave it to you, not the deck, so it pays you nothing** (`RULES.md` §4.4). Take
it only if it melds.

**2. "How will you take your card?"** — the previous player's discard, or the top of the deck
blind. Two things are different about them beyond the card itself: you can *see* the discard,
and **a blind draw is the only way to come to own a money card mid-round**. Only the
immediately-previous player's top discard is ever offered, so at a four-seat table you see one
card in four go past.

**3. "Which card will you throw away?"** — you are at fourteen and must go back to thirteen.
Each card is annotated with what throwing it costs:

```
> 3♣ (melds nothing)
  6♣ (melds nothing) ← the computer would throw this
  9♦ (breaks a meld — costs 4)
```

*Melds nothing* is deadwood and free to throw. *Breaks a meld — costs 4* means four cards
stop melding if it goes. You **may** throw back the card you just took (`RULES.md` §9 #13,
defaulted).

**4. "Declare and end the round?"** — offered only when all thirteen genuinely meld, so it is
never a trap. Say yes.

---

## Winning, and what it pays

You win by **melding all thirteen** — no laying off, nothing on the table until you go out.

- **Runs**: three or more of the same suit in sequence. **Aces do not wrap** — `A-2-3` and
  `Q-K-A` are runs, `K-A-2` is not.
- **Sets**: three or more of the same rank, **all different suits**. `9♥ 9♥ 9♠` is not a set
  even though two decks let you hold it, so a set caps at four cards.
- Jokers substitute for a missing card in either.

Settlement has two independent halves, and the console shows them as two columns:

1. **The round.** Every loser pays the winner the round value, flat. **There is no penalty for
   unmelded cards** — losing costs the same whether you were one card short or holding
   thirteen strangers.
2. **The money cards.** Each owner collects the money card value per card from everyone else,
   whoever won. The winner takes part in this like anybody else.

> **The rule that changes how you play: ownership is permanent, and possession is
> irrelevant.** You own a money card if the deck gave it to you — dealt in your opening
> thirteen, or drawn blind. **Discard it and you are still paid for it**, even if an opponent
> picks it up. So never hoard one. Play to win the round; the money settles on what the deck
> dealt you (`RULES.md` §4.4).

---

## Playing better

The computer's whole strategy is one question asked three ways: *of the thirteen I would be
left holding, how many meld?* Take the discard if it raises that number, claim the turned-up
card if it raises that number, throw whichever card leaves it highest.

**What separates the hard bot from the easy one is only the tie-break** — and it is worth
1.6× the wins (30.7% of rounds against 19.3%, measured over 2,000 four-seat rounds). Early in
a round almost every discard costs you nothing, so the count alone cannot choose. The hard bot
then keeps:

- cards with **partners** — another suit of the same rank, or a neighbour in the same suit;
- **jokers over everything**, since a joker fits anywhere.

Two things worth knowing at the table:

- **A fresh hand melds about 4 of its 13**, and about one hand in five melds nothing at all.
  Do not read a bad-looking deal as a lost round.
- **Most progress comes from taking discards**, not from drawing. With two decks, the card
  somebody throws away is very often somebody else's third of a rank.

---

## Between rounds

The round's settlement is shown in full, then **Rounds so far** (who won each one and what it
paid) and **Standings** (rounds won and the running bank). Banks carry over and nothing resets
them.

**Nothing ends a session but the players** (`RULES.md` §7.2) — there is no target score and no
round limit, so *"Another round?"* is asked until you say no.

If the draw pile runs out, every discard pile is gathered, shuffled, and becomes the new draw
pile; the table is told when it happens. In practice you will only see it at a **full table** —
it is common at six seats and essentially never happens at four.

---

## What is not settled

Three rules this guide states have been **defaulted rather than confirmed**, and a player might
notice all three. They are recorded in `RULES.md` §9 with the reasoning, and phrased for an
experienced player in `QUESTIONS-FOR-MYA-LAY.md`:

- Claiming the turned-up money card needs **nobody's permission** here, and is offered **every
  round** (§9 #5).
- A claimed card **still pays its owners** — designation is fixed at setup and does not move
  with the card (§9 #12).
- You **may** throw back the card you just took (§9 #13).

Answers from a real player outrank all of this. See `RULES.md` for how provenance is ranked.
