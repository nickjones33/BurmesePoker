# Questions for the experts

Open rules questions best settled by an experienced player. Answers recorded here get
promoted into `RULES.md` with the `EXPERT` provenance tag.

⚠️ **Two experts since 2026-08-20: Mya Lay and Aung Aung.** `RULES.md`'s provenance scale has a
single `EXPERT` tier and named one person. It now needs to record **which** expert, and whether
two of them answered **independently** — two independent agreeing experts are worth more than one
answer with a second person nodding along. ⚠️ **The 2026-08-20 session below does not record
either**, which is a gap in the record rather than in the answers.

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

## Q4 — The feeding rule, in detail **(new — and the one with code waiting on it)**

Mya Lay ruled on this directly at the playtest on 2026-08-20 and it is recorded as `RULES.md`
§5.1: *you may not discard a rank the next player has taken in the open, until they discard that
rank themselves or you are going out on it.* **The rule is settled. Most of what it covers is
not**, and
these are what an implementation has to know. Each is phrased as a table situation, with no
option signalled.

1. **Who is bound.** *"Say I take the Queen you threw away. Now — can nobody at the table throw a
   Queen after that, or is it just you, because you're the one sitting before me?"* (§9 #16)

2. **What arms it.** *"This time I don't take your card — I take one off the top of the deck, and
   nobody sees what it is. Say it's a Queen. Are you still allowed to throw Queens?"* Follow-up if
   that is answered no: *"and what about at the very start, when the first player takes the
   turned-up money card — does that count the same as taking a card someone threw?"* (§9 #17 — the
   follow-up matters, because at the playtest Mya Lay went first and had no discard in front of
   her.)

3. **Money card or any card.** *"At the playtest the Queen was one of the money cards that round.
   If it had been an ordinary card — say I take a 6 you threw away — would you still be stopped
   from throwing 6s?"* (§9 #18)

4. **A release across a reshuffle.** *"I take a Queen, so you can't throw Queens. Then later I
   throw a Queen away myself, which frees you up. Then the deck runs out, and everything thrown
   away gets gathered up and shuffled to make a new deck — so my Queen isn't lying there any more.
   Are you still free to throw Queens?"* (§9 #19 — asked flat; either answer is easy to build, but
   they need different bookkeeping.)

5. **The joker, which has no rank.** *"I take the joker you threw away. Are you allowed to throw
   the other joker at me next turn?"* And if the answer is no: *"what if it's the other colour —
   does that matter?"* (§9 #27 — the rule closes *ranks* and a joker hasn't got one, so read
   literally it closes nothing. Asked flat; the colour follow-up matters because the game already
   tells the two jokers of a deck apart that way, and §4.1's turned-up-joker question (#11) turns
   on the same thing without the two needing to agree.)

⚠️ **A sixth §5.1 question, #25, is asked with the player-count set below** — *does the ban work
two-handed, where the seat that feeds you is the seat you feed?* — because it arrived with those
answers rather than with the playtest.

> ⚠️ **Two things not to ask, both ruled by Nick on 2026-08-20 and recorded as §5.1.** There is
> **no penalty** for discarding a banned rank, because it is not a move you can make — the card is
> never offered. And where the ban would leave a player with **nothing legal to throw**, the ban
> **yields for that turn**. Both were §9 #20, which is closed. If Mya Lay volunteers something
> different on either, that outranks a `PLAYER` ruling and is worth writing down.

---

## Lower-priority, if the conversation allows

Each of these has a safe default and does not block the build. Ask only if it's easy.

- ✅ **Going out without discarding.** *"Is there ever a time you can go out without discarding a
  card at the end of your turn?"* (§9 #6.) **Answered 2026-08-20: no exceptions, you must
  discard.** See Q5 below.
- **Jokers in one meld.** *"Can you use two jokers in the same run or set, or only one?"* (§9 #8)
- **A meld of nothing but jokers.** *"If you were holding three jokers, could you put those
  three down together as one of your melds?"* (§9 #8 — asked as a table situation, with no
  option offered either way.)
- **Claiming the turned-up money card.** *"When someone takes the turned-up money card at the
  start — does that happen every round or only at the very beginning of the game? And does
  anyone have to agree to it?"* (§9 #5)
- ✅ **Pure melds.** *"Does at least one of your runs have to be made without any jokers in it?"*
  (§9 #7.) **Answered 2026-08-20: yes — and corrected the same day.** Purity attaches to the
  *required* series, not to the hand: both of them three-handed, the one of them four-handed,
  irrelevant two-handed and five-plus. See Q5 below.
- **Why these two cards.** *"Is there a reason the 7♦ and A♠ in particular are the money
  cards?"* (§9 #10 — likely unrecoverable, but cheap to ask.)
- **After somebody takes the turned-up money card.** *"Say the money cards turned over are the
  9♥ and the 4♠, and the first player takes the 9♥ into their hand at the start. For the rest
  of that round, does a 9♥ still pay anything — say, if another player was dealt the other
  one?"* (§9 #12 — asked as a table situation, with no option offered either way.)
- **Throwing back what you just took.** *"You take a card — either the one the player before
  you threw away, or one off the top of the deck — and then you have to throw one away. Are
  you allowed to throw away that same card you just took?"* (§9 #13 — asked flat; the
  interesting half is the card taken from the discard pile.)
- **Where everybody sits, round after round.** *"When you finish a round and deal the next
  one, does everyone stay in the same seats and keep going round the same way, or does anything
  move — who deals, who goes first, where people sit?"* (§9 #14 — asked flat; the engine
  currently keeps the seating it was given for the whole session.)
- **The turned-up cards once the deck runs out.** *"The two cards you turned over for the money
  cards are lying there all round. If the deck runs out and you gather the discards up to make a
  new deck, do those two go in as well, or do they stay where they are?"* (§9 #4 — asked flat.)
- **Looking through the thrown-away cards.** *"While you are playing, can you pick up the pile
  of cards that have been thrown away and look through them, or can you only see the one card
  on top? And what about the cards the other players have thrown away — can you look at
  those?"* (§9 #15 — asked flat, and the whole of the answer matters: it decides whether
  remembering what has gone is part of the game.)
- **A joker turned up as a money card.** *"At the start of a round, when you turn the two
  cards over for the money cards, what happens if one of them is a joker?"* (§9 #11 — asked
  flat, with no options offered. If the answer is that it does become a money card, the
  follow-up is: *"and then which cards in your hand pay out for it?"*)


---

## Q5 — The 2026-08-20 session: Mya Lay and Aung Aung **(the biggest single day in this file)**

Six questions put, six answered, and one of the answers was not a question anybody had thought
to ask. Promoted into `RULES.md` **rev 17**.

### What was asked and what came back

| # | Asked | Answer | Promoted to |
|---:|---|---|---|
| 1 | *"Whose thrown-away card can you take — only the person right before you, or anyone?"* | **Right before you.** | §5 — `CODE` `Tentative` → **`EXPERT` `Settled`** |
| 2 | *"Can you pick the pile up and look through it, or only see the top card?"* | **Yes, they're public.** | §5, closing §9 #15 |
| 3 | *"Does at least one of your runs have to be made without any jokers in it?"* | **One of your runs has to be clean** — ⚠️ **corrected later the same day**, see below. | §7.1, closing §9 #7 |
| 4 | *"Is there ever a time you can go out without throwing a card away?"* | **No exceptions, you must discard.** | §7.1, closing §9 #6 |
| 5 | *"Apart from the two turned over, are any cards always money cards?"* | **Always money cards.** | §4.1 — `CODE` `Probable` → **`EXPERT`** |
| — | *volunteered* | **The rules change with the player count.** | §7.1.1 — **new section** |

### The one nobody asked for

🔥 **The rules are different at two, three, four, and five-or-more players**, and the difference
is the win condition:

| Players | What a declared hand must contain | …of which clean |
|---:|---|:---:|
| **2** | Series only. Sets are not allowed at all. | none required |
| **3** | At least two series. | **both** |
| **4** | At least one series. | **the one** |
| **5 or more** | No requirement — any valid meld. | none required |

🔥 **The clean column is a correction, and it is the most instructive thing in this file about how
to take an answer.** Question 3 was asked before the player-count rule surfaced, so *"one of your
runs has to be clean"* was recorded as a flat rule over the whole hand — which is what it sounds
like, and is wrong. **Purity is a property of the series the table size requires**, and the two
counts are the same number: nought and nought, one and one, two and two. ⚠️ **Two-handed is what
proves it is not simply "all series must be clean"**: every meld there is a series and cleanliness
still does not matter. **An answer given before the framing that governs it is not yet an
answer.**

**This is the most consequential thing in the file since Q1.** It makes two- and three-handed real
games, so §2's "four players minimum" was wrong; it makes the win condition a **function of the
table size**, which nothing in the engine models; and read beside §2.1 it looks deliberate — the
fewer the players, the more of the deck each one sees, so the hand is made harder to compensate.

### What these answers cost

- ⚠️ **Two of the six reversed a standing recommendation.** Purity was recommended *not-a-rule*
  from rev 1 to rev 16; discard piles were defaulted to *top card only* by P20. **Where `IR` and
  a recollection have disagreed, `IR` has now won three times out of three** (§6.2, §7.1, §5).
- ⚠️ **`HandEvaluator` implements none of the new win condition** — see `RULES.md` §10 #14.
- ⚠️ **Every figure in `docs/STRATEGY.md` was measured under the old win condition.**

### The follow-ups that were put and not reached

Four remain — `RULES.md` §9 **#22 and #24–#26**. Two more closed on the day they were raised:

- **#21** — did purity survive at five or more players? The correction below **dissolved** it:
  purity attaches to the required series, and at five-plus there are none.
- 🔥 **#23 — what counts as two series? Answered: a longer run may be laid down split.** `3+3`
  out of a six-card run **is** two series. **This was the load-bearing question of the day** —
  the three-handed rule is far weaker than it read, since one clean six-card run satisfies *two
  clean series* on its own. It also changes the shape of the win question: declaring is no
  longer *"does a cover exist?"* but *"does a cover exist with the right counts?"*, so the
  evaluator can no longer return the first cover it finds (`RULES.md` §7.1.1).
- **#28 — does a surplus series have to be clean? Answered: no.** The required count and no
  further. In the order I would ask them:

1. **#22 — at two players, are sets illegal, or merely not required?** Recorded as *illegal*,
   which is what was said, but the strong form was never put back as a table situation.
4. **#24 — the money-card follow-ups:** both copies out of both decks? What if a turned-up card
   *is* the 7♦? Can it stack past a double? *(These were asked and the conversation moved on.)*
5. **#25 — does the feeding ban work two-handed**, where the seat that feeds you is the seat you
   feed?
6. **#26 — does anything else change with the player count** — hand size, decks, money cards,
   stakes?

**Phrased as table situations, ready to ask:**

> **#23** — *"Say you're playing three-handed and you've got six cards of the same suit in a row.
> When you lay them down, can you put them down as two separate runs of three, and does that
> count as your two series?"*
>
> **#22** — *"Two-handed, if you had three of a kind in your hand at the end — is that just not
> allowed, or is it fine as long as you've got your runs as well?"*
>
> **#24** — *"The 7♦ — is it both of them out of the two decks, or just one? And what if one of
> the two cards you turn over at the start happens to be the 7♦ itself?"* Then: *"Can it stack
> up more than that — three times, four?"*
>
> **#25** — *"When it's just the two of you — you take the card I threw away, so I can't throw
> that rank any more. Does the same thing work the other way round at the same time?"*
>
> **#26** — *"When you play two- or three-handed, is anything else different — how many cards you
> get, how many decks, what the money cards are?"*
