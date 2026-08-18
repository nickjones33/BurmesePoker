# Questions for Mya Lay

Open rules questions best settled by an experienced player. Answers recorded here get
promoted into `RULES.md` with the `EXPERT` provenance tag.

> **Please don't signal the expected answer when asking.** In rev 1 this project guessed that
> sets allowed duplicate suits, reasoned its way to a confident recommendation, and was wrong.
> A neutrally-asked question is worth more than a confirmed guess. The questions below are
> deliberately phrased as concrete table situations with no hinting.

---

## Q1 — Which money cards actually pay you? **(the important one)**

> At the end of a round, when you pay out for money cards — which money cards actually pay you?
>
> Take three situations:
>
> 1. The **7♦ was in the 13 cards dealt to you** at the start. You never drew it — it was just
>    there in your opening hand.
> 2. You **drew the 7♦ from the deck yourself**, on one of your turns.
> 3. The player before you **discarded the 7♦, and you picked it up.**
>
> In which of those does the 7♦ pay you at the end of the round?

**Why it matters.** Situation 2 is confirmed to pay, and situation 3 is believed not to — but
that comes from reading the old code, not from a player. **Situation 1 is genuinely open, and
it decides the character of the whole money layer.** Because ownership is permanent
(`RULES.md` §4.4), if dealt cards pay then most of a round's money is fixed before anyone
plays a card, and can never change. If only drawn cards pay, money cards are actively won
during play.

Tracked as `RULES.md` §9 #3. Blocks: settlement shape (packets P2, P6, P7).

**Answer (2026-08-18, verified with Mya Lay): ✅ RESOLVED**

| Situation | Pays you? |
|---|:-:|
| 1. Dealt in your opening 13 | **Yes** |
| 2. Drawn from the deck yourself | **Yes** |
| 3. Picked up from the previous player's discard | **No** |

So **the deck confers ownership — deal and draw alike — and a pickup confers nothing.**
Promoted into `RULES.md` §4.4 (rev 6) with the `EXPERT` tag. This also upgraded the
pickup rule, which had previously rested on reading the old code rather than on a player.

---

## Q2 — What happens when the deck runs out?

> If nobody has gone out yet and the deck runs out of cards, what happens? Do you gather up the
> discards and shuffle them into a new deck, or does the round just end there — and if it ends,
> does anybody win it?

**Why it matters.** The game currently crashes here. Also decides whether discards need to be
one shared pile rather than per-player.

Tracked as `RULES.md` §9 #1. Blocks: packet P9.

**Answer (2026-08-18): ✅ RESOLVED.** Gather the discards, shuffle them, and that becomes the
new draw pile. Noted as **rare** in practice — most rounds end before the deck empties.
Promoted into `RULES.md` §5 (rev 7).

---

## Q3 — How does a whole game end?

> How do you decide when the whole game is over, rather than just one round? Do you play a set
> number of rounds, keep going until someone runs out of money, play to a target amount, or
> just stop whenever everyone's had enough?

**Why it matters.** The game currently plays exactly one round.

Tracked as `RULES.md` §9 #2. Blocks: packet P9.

**Answer (2026-08-18): ⚠️ AMBIGUOUS — needs one more pass.** The reply described a player
*"discards and then plays all 13 of their cards melded"*, which is the **round**-ending
condition (now recorded precisely in `RULES.md` §7.1, including the discard-then-reveal order).
It does not say whether anything ends a **match** of several rounds. Likely there is no formal
match end — money simply carries over and players stop when they stop — but this needs
confirming.

**Follow-up answer (2026-08-18): ✅ RESOLVED. Nothing ends the session automatically.**
Rounds repeat and banks carry over; players stop when they choose. Recorded in `RULES.md` §7.2
(rev 8). The engine must **not** invent a target score or round limit.

---

## Lower-priority, if the conversation allows

Each of these has a safe default and does not block the build. Ask only if it's easy.

- **Going out without discarding.** *"Is there ever a time you can go out without discarding a
  card at the end of your turn?"* (§9 #6 — Nick recalls an exception but not what it is.)
- **Jokers in one meld.** *"Can you use two jokers in the same run or set, or only one?"* (§9 #8)
- **Claiming the turned-up money card.** *"When someone takes the turned-up money card at the
  start — does that happen every round or only at the very beginning of the game? And does
  anyone have to agree to it?"* (§9 #5)
- **Pure melds.** *"Does at least one of your runs have to be made without any jokers in it?"*
  (§9 #7 — Nick doesn't recall this being a rule.)
- **Why these two cards.** *"Is there a reason the 7♦ and A♠ in particular are the money
  cards?"* (§9 #10 — likely unrecoverable, but cheap to ask.)
- **A joker turned up as a money card.** *"At the start of a round, when you turn the two
  cards over for the money cards, what happens if one of them is a joker?"* (§9 #11 — asked
  flat, with no options offered. If the answer is that it does become a money card, the
  follow-up is: *"and then which cards in your hand pay out for it?"*)
