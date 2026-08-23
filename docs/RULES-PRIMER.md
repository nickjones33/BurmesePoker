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
- **Draw for seats once, and keep them.** A "game" is one round — from the turn-up to somebody
  going out — and you do **not** re-shuffle the seats every deal. You re-draw **when the players
  agree to**, which is what makes *"the player before you"* the same person for a whole run of
  rounds. **[✓]**

## Money cards

Two ways a card becomes one:

1. **Permanently** — the **7♦**, the **A♠** and **every joker**, every round, all copies out of
   both decks. **That's eight cards paying before you turn anything up.** **[✓]**
2. **For this round** — the two turned-up cards, and their matching copies. **[✓]**

🔥 **Turn up a 7♦, an A♠ or a joker and the *other copy* pays triple.** The one on the table is
worth nothing — nobody can own a turned-up card, whether they take it or not — so its value moves
to its partner: **×3**. For a joker, "the other copy" means **the other joker of the same
colour**. **[✓]**

*Why three: one for the partner's own value, one for the designation, one inherited from the card
that can no longer be paid for. It makes turning up a permanent money card worth exactly what
turning up any other card is worth.*

🔥 **And the jackpot. If the two cards flipped are the 7♦ and the A♠, and one player ends up owning
both of the other copies — they pay ×5 each instead of ×3.** At standard stakes that is **$40 from
a five-player table**, against a $5 round prize. It's the only place in the game where what a card
pays depends on who's holding what. **[✓]** **[? whether two jokers, or a 7♦ and a joker, do the
same thing has never been asked — assume not]**

**The two turned-up cards stay on the table all round** — they're designators, nobody owns them,
and they do **not** go back in when the deck runs out and the discards are gathered. **[✓]**

**Payout: each money card pays its *owner* the money card value from every other
player** — $1 each, at standard stakes. Own two in a 5-player game → **$8**. A tripled card
counts as three, so owning one in a 5-player game is **$12**. This settles regardless of who won the round. **[✓]**

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
5-player round roughly **6 of the ~10** ownable money cards are already owned when the deal ends,
leaving about four to be drawn for.

**Exact match only.** A turned-up 5♥ makes money cards of **just the two 5♥s**, not all
the fives. **[✓]**

## A turn **[✓]**

1. Take either the previous player's discard, or the top of the deck blind.
2. Discard one card. **You may throw straight back the card you just took** — as long as no other
   discard rule stops you, which in practice means the feeding ban below. **[✓]**

**You may not feed the next player. [✓]** If the player after you has
taken a card in the open, you may not discard **that rank** again — *any* suit — for the rest
of the round. Two ways out: they discard that rank themselves, which opens it up **permanently**
even if they pick another one up later; or you are **going out** on it. Rank taken → rank closed,
one rank at a time. **It isn't a foul, it's not a move** — a banned card simply isn't one you can
throw, so there's no penalty to argue about. And if the ban ever left you with **nothing** legal to
throw, **the ban yields** for that turn: you must discard, so you may. See `RULES.md` §5.1.

**The opening turn is special:** that player may take the turned-up top money card instead
of drawing — taking the **actual card**, which leaves the table. **[~]**
**It is offered every round** **[✓]**, and taking it does **not** stop that value paying — the
other copy still pays whoever the deck gave it to. **[✓]**

🔥 **But you must ask permission first — from the player who plays *before* you**, the last one
round the table, the one who discards to you. **[✓]** They may say no
**only if they're holding that card**: your taking it in the open closes that rank to them under
the feeding ban, and they'd be stuck with it all round. So the opener's free card comes out of
that player's hand, and that player gets a veto.

→ **Objecting shows your hand.** Only a holder may object, so saying no tells the table you've got
one. It's the only thing in this game you reveal on purpose.

## Melds

**Runs** — 3+ cards, same suit, consecutive. **[✓]**
- **Aces don't wrap.** `A-2-3` ✓ · `Q-K-A` ✓ · `K-A-2` ✗ **[✓]**
- Jokers substitute for a missing card, with **no limit on how many** in one meld. **[~]**
- **Three jokers on their own are a run.** But it is never a *clean* one, so it can only ever be
  a run you did **not** need — never one the table size requires. **[✓]**

**Sets** — 3+ cards of the same rank, **all different suits**. `9♥ 9♥ 9♠` is **not** a set,
even though two decks make it holdable. A set therefore caps at **4 cards**. **[✓]**

## Concealment **[✓]**

**Nothing is ever played to the table.** You build in hand and keep your intended melds
secret until you go out, then reveal all 13 at once. There is **no laying off** on your own or
anyone else's melds. The public information is the discards — **properly public: you may pick
any pile up and look through it, not just see the top card** — and **the cards you took in the
open, which sit face up in front of you for as long as you hold them** (rev 31). Only what the
deck gave you unseen stays hidden. **[✓]**

## Winning **[✓]**

**Discard, then lay down all 13.** You hold 13, take one to make 14, **throw one away**, and
*then* reveal the thirteen melded. The discard comes first. **[✓]**

- **You always discard. There are no exceptions.** **[✓]**

**What the thirteen must contain depends on how many are playing** — this is the part people
forget, and **the runs you are *required* to have must be clean** (no jokers in them): **[✓]**

| Players | Your hand must contain | …clean? |
|---:|---|---|
| **2** | **runs only** — a set isn't just unnecessary, it's **not allowed** | doesn't matter |
| **3** | at least **two runs** | **both of them** |
| **4** | at least **one run** | **that one** |
| **5+** | anything — no requirement | doesn't matter |

*Fewer players means more of the deck each, so the hand is made harder to make up for it — and
the clean-run count is just the required-run count. Nought and nought, one and one, two and two.*

## Settlement **[✓]**

1. **Every loser pays the winner the round value** — $5 at standard stakes, so in a 5-player
   game the winner collects **$20**. ⚠️ **Three rules can change that**, see below.
2. **Money cards settle pairwise** — each owner collects the money card value per card from
   everyone else. The winner takes part in this too.

**No penalty for unmelded cards.** Losing costs exactly the round value whether you were
one card short or holding all thirteen. **[✓]**

→ So the game has **no points at all** — money is the only ledger.

### What can change what a win is worth

- **No joker anywhere in the declared thirteen: ×2** at 2–4 players, **×3** at 5 or more.
  It is a property of the **cards you laid down**, not of which melds you needed. **[✓]**
- **Winning on the initial deal — the thirteen you were dealt already melds: ×2**, on top of
  the one above, so a jokerless hand dealt complete at five seats pays **×6**. **[✓]**
  **[? the two multiplying is the reading taken, and it has not been confirmed]**
- **A third win in a row: the whole payment comes from the player immediately before you in
  turn order**, blamed for feeding you, and everybody else pays nothing. **[✓]** It is the only
  rule in the game that reaches across rounds, and the only one that changes *who* pays.

→ Both bonuses multiply the **round payment only**. Neither touches the money cards, which
settle the same way whatever the round was worth. **[? recommended reading, not confirmed]**

---

## What's still unrecorded

✅ **Every question about the money layer is answered.** `RULES.md` §9 is the live list, and what
is on it now was raised *by* those answers rather than left over from before:

1. ✅ **Does the ×5 need the 7♦ and the A♠ specifically?** **Answered: it does** — no other pair
   of tripled cards is a jackpot.

What is open now is all downstream of the two scoring rules above: how the deal bonus combines
with the clean bonus, whether either reaches the money cards, whether a streak keeps firing at a
fourth win, and what happens if two players are dealt a winning thirteen at once. Every one of
them is being **played on a recorded default** rather than left undecided, and each default has a
test named after the question, so the day an expert answers, the failing test is the change list.

The two items this section used to carry were settled long ago and are kept struck through so the
rot stays visible:

1. ~~**What happens when the deck runs out?**~~ Gather the discards and shuffle.
2. ~~**What ends a match?**~~ Nothing does — banks carry over and you stop when you stop.

✅ **The gap between this page and the program is shut.** Every rule above is played, including
the feeding ban (a closed rank is not offered at all — the console leaves it out of the list and
the browser draws it as a card you cannot press), the win condition by player count, the seating
that holds until the table agrees to change it, and all three of the settlement rules.

⚠️ **One divergence is left**, and it is the one tagged in *Setup*: **the program deals for four
to six players**, so the two- and three-handed win conditions are implemented, tested, and
unreachable from a dealt game.

Full list in `RULES.md` §9, which is where every open question and its recorded default lives.
