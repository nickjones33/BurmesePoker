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

✅ **None remain.** #22 was closed the next session (Q6 below), #24 and #26 the session after that
(Q7), and #25 on the day. Two more closed on the day they were raised:

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

1. ✅ **#22 — at two players, are sets illegal, or merely not required? Answered: illegal** —
   see Q6.
2. **#24 — the money-card follow-ups:** both copies out of both decks? What if a turned-up card
   *is* the 7♦? Can it stack past a double? *(These were asked and the conversation moved on.)*
3. ✅ **#25 — does the feeding ban work two-handed? Answered on the day: the rule is the same in
   every game**, and the mutual lock is a legal state.
4. **#26 — does anything else change with the player count** — hand size, decks, money cards,
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

---

## Q6 — The follow-up session **(the win condition finished)**

**2026-08-20/21, Mya Lay and Aung Aung — the same night, after rev 17 was written up.** Two questions put, two answered, promoted into `RULES.md`
**rev 18**. ✅ **Both were the last things open against §7.1.1**, and with them the win condition
is fully specified for the first time.

| # | Asked | Answer | Promoted to |
|---:|---|---|---|
| 22 | *"Two-handed, if you had three of a kind in your hand at the end — is that just not allowed, or is it fine as long as you've got your runs as well?"* | **Illegal.** *"In 2 player, you must go out with ALL SERIES, no sets."* | §7.1.1, closing §9 #22 |
| 29 | *"If you were holding three jokers, could you put those three down together as one of your melds?"* | **"Yes, but not clean."** | §6.1, closing §9 #29 **and §9 #8** |

**Neither answer changed a rule, and that is the point of both.**

- **#22 confirmed what was already written.** Rev 17 recorded the strong form because it was what
  was said, and flagged in the same breath that it had never been put back as a table situation.
  Put back, it held. 🔥 **A confirmation is not nothing** — §9 #25's reasoning about the feeding
  ban head-to-head leant on sets being illegal two-handed, and it was leaning on an unconfirmed
  recording until now.
- **#29 confirmed a piece of reasoning.** §9 #8 had recommended *unlimited jokers, which permits
  an all-joker meld* since rev 1, and P3 has emitted all-joker melds since rev 10 on the strength
  of it. 🔥 **This is the only question in the file where arguing ahead of the answer turned out
  right** — against three where it turned out wrong (§6.2 duplicate suits, §7.1 purity, §5
  discard piles). It is not a licence to reason instead of asking; it is one out of four.

🔥 **The half of #29 nobody asked for is a restriction.** *"But not clean"* means an all-joker
series is impure by construction, so under §7.1.1 it can discharge a **surplus** series and never
a **required** one — three-handed, three jokers can never be one of the two. That agrees exactly
with #28's *"a surplus series need not be clean"*, asked the day before in the opposite direction.

⚠️ **One thing here is `DERIVED`, not `EXPERT`.** Nobody was asked *"how many jokers may one meld
hold?"*. That a three-card meld may be entirely jokers forecloses any cap below a meld's own size,
which closes the maximum-jokers half of §9 #8 by deduction. It is recorded as a deduction in §6.1
and is one question if the chance comes up again.

### What was learned about asking

⚠️ **The strong form of #22 was put back and it survived, but the asking cost something.** The
question was phrased as *"illegal, or merely not required?"* — two named readings — and the reply
was *"Why are you asking me about sets? In 2 player you must go out with ALL SERIES, no sets."*
🔥 **The distinction was real and the framing was not: a rule can be described accurately in words
that leave an implementer two choices, and the player who knows the rule hears no ambiguity at
all.** The question that worked was not a sharper distinction, it was a table situation — *three
of a kind in your hand at the end*. **Ask the situation, and let the reading fall out of it.**

---

## Q7 — The money layer, and the two answers that were not about money

**2026-08-21, Mya Lay and Aung Aung.** Nine questions put, nine answered, promoted into
`RULES.md` **rev 19**. ✅ **§9 is down to two open questions** — the fewest since rev 1 — and
**eight of the nine confirmed a standing default.**

| # | Asked | Answer | Promoted to |
|---:|---|---|---|
| 24a | *"The 7♦ — is it both of them out of the two decks, or just one?"* | **Both.** | §4.1 — `CODE` `Probable` → **`EXPERT` `Settled`** |
| 24b | *"What if one of the two cards you turn over at the start happens to be the 7♦ itself?"* | **"Double money card, worth double."** | §4.1, §4.3 |
| 24c | *"Can it stack up more than that — three times, four?"* | ⚠️ *"Maybe… I don't think that's mechanically possible/relevant."* | §4.1 — **`PLAYER`**, hedge quoted |
| 11 | *"What happens if one of the turned-up cards is a joker?"* | **"Jokers become money cards."** | §4.1 — closes half of §9 #11 |
| 12 | *"…the first player takes the 9♥. Does a 9♥ still pay anything?"* | **"Yes, all 9s of hearts become money cards."** | §4.5, closing §9 #12 |
| 5 | *"Does the claim happen every round, or only at the very beginning?"* | **"At the beginning of the game — if game means from the time the money card is turned up to the time a player goes out."** | §3, §4.5 — closes half of §9 #5 |
| 4 | *"If the deck runs out and you gather the discards, do those two go in as well?"* | **"They stay where they are."** | §4.5, closing §9 #4 |
| 13 | *"Are you allowed to throw away that same card you just took?"* | **"Yes, as long as you aren't violating any other discard rules."** | §5, closing §9 #13 |
| 14 | *"Does anything move between rounds?"* | 🔥 **"You randomize seats between games."** | §3, closing §9 #14 |
| 26 | *"Playing two- or three-handed, is anything else different?"* | ⚠️ *"Not that I know of."* | §2 — `EXPERT` **`Probable`**, hedge quoted |
| 10 | *"Is there a reason it's the 7♦ and the A♠ in particular?"* | **"Tradition."** | §4.1, closing §9 #10 |

### The three that were not simply confirmations

🔥 **#5 answered a different question than the one asked, and the different question was the
important one.** Asked *how often*, the reply defined the unit: **a game is one round — from the
turn-up to somebody going out.** Everything this document calls a *round*, a player calls a
*game*. That is a vocabulary note until #14 lands on it, and then it is a rule.

🔥 **#14 is the reversal, and it is the only one of the nine that changes what the engine must
do.** *"You randomize seats between games, using the game definition from #5"* — so **seats are
re-drawn before every deal.** `MatchEngine` randomises once and holds it for the whole match,
which is what P9 chose while #14 was open and the recommendation was *nothing moves*. Recorded as
`RULES.md` §10 #16. ✅ **No published measurement moves**: every experiment in `BurmesePoker.Sim`
runs `RoundsPerGame = 1`, so no measured game has a second round to re-seat for. ⚠️ **The front
ends are where it shows** — the console and the browser deal round after round with the banks
carrying over, so your neighbours currently never change and should change every deal.

🔥 **#13's rider is worth more than #13.** The answer was not *"yes"* but *"yes, as long as you
aren't violating any other discard rules"* — and §5.1 is the only other discard rule there is. So
**the feeding ban outranks the right to throw a card straight back**, the just-taken card is
filtered like every other card in the hand, and an implementation needs no special case for it.
⚠️ **Note the shape of the answer, not only its value**: a player describing a **legal-move
filter** unprompted is the same construction §5.1's impossible-move enforcement already takes.

### What was learned about asking

⚠️ **#10 had been recorded as *"unknown, likely unrecoverable"* since rev 1 and was recovered in
one word: "tradition."** 🔥 **A question can close by confirming there is nothing behind it**, and
that is worth more than leaving it open — an unanswered *why* invites a future session to go
hunting for a pattern in `7♦`/`A♠` that does not exist. **It was asked because it was cheap, with
an explicit expectation of nothing.** Ask the cheap ones.

⚠️ **Two answers were hedged and are tagged accordingly** (#24c `PLAYER`, #26 `EXPERT`
`Probable`), with the hedge quoted verbatim in `RULES.md`. 🔥 **#24c is the interesting one,
because the hedge is half right and half wrong and the document can prove which half.** The case
*is* mechanically possible — both 7♦s turned up together, roughly one round in 5,800 — and it *is*
irrelevant, for a reason nobody stated: in that case both copies are lying on the table as
designators, nobody was dealt one, a card on the table is owned by nobody, and so the value pays
nothing whatever it stacks to. **Recording the hedge rather than flattening it is what left room
to work that out.**

### What is left

✅ **Two questions, both halves of questions that were answered:**

- **#5 — who approves the claim?** §4.5 carries a permission check from the 2023 source that no
  person has ever confirmed, and the engine asks nobody.
- **#11 — which jokers does a turned-up joker designate**, all four or the two of its colour?
  ⚠️ **The default sends the money somewhere §5.1's #27 sends it elsewhere** — deliberately, since
  designation is scoped narrow (§4.2) and feeding broad (§5.1) — and the wrong guess is invisible:
  it pays the wrong person a dollar about one round in fourteen.

> **#5** — *"When the first player takes the money card off the table at the start — can they just
> do that, or does somebody have to let them?"*
>
> **#11** — *"Say the card turned over is a red joker, so jokers are money cards this round. If I'm
> holding a black joker at the end, do I get paid for it?"*

---

## Q8 — The two that closed the money layer, and the one that reopened it

**2026-08-21, Mya Lay and Aung Aung.** Two questions put, two answered, promoted into `RULES.md`
**rev 20**. ✅ **Both were the last things open about money.** 🔥 **Both answers reached past the
question, and one of them superseded an `EXPERT` ruling given the day before.**

| # | Asked | Answer | Promoted to |
|---:|---|---|---|
| 11 | *"Say the card turned over is a red joker. If I'm holding a black joker at the end, do I get paid for it?"* | **"Colour matters for jokers"** — the other joker of its own colour, and no other. | §4.1, closing §9 #11 |
| 5 | *"When the first player takes the money card off the table at the start — can they just do that, or does somebody have to let them?"* | **The player who goes before you in turn order must permit it — and may refuse only if they hold that card.** | §4.5, closing §9 #5 |
| — | *volunteered* | 🔥 **A turned-up 7♦, A♠ or joker can never be owned, claimed or not — and its partner copy pays ×3.** | §4.1, §4.4 — **supersedes rev 19** |

### The permission rule is the find

> *"When you go to pick up that first card at the start of the game, you must ask 'permission'
> from the player who goes before you in turn order (last in the round), because if (and only if)
> that player has that card, they can object to you picking up that card, since it would lock them
> into holding the card via the discard rules."*

🔥 **This is the first rule in the document that ties the money layer to the feeding ban**, and it
arrived from a question about neither. Claiming is a **public take**, so it arms §5.1 against the
seat that discards to the claimer — and that seat is exactly *the player who goes before you*. If
they hold a copy they can never throw it, because the claimer has no reason to release the rank.
**The opener's free card is paid for out of the upstream player's hand, and the veto is the
price.**

🔥 **It independently confirms two rulings that were guesses when they were made.** §9 #16 — the
ban binds the seat above you, not the table — and §9 #17 — only a public take arms it, and the
§4.5 claim counts. **Both were reasoned out in rev 16 from what would make §5.1 enforceable, and
confirmed flat in rev 17. This rule cannot be stated without either of them.** ⚠️ **Three
independent routes to the same shape is the strongest structural evidence in this file that §5.1
is an old rule rather than a one-off table ruling** — a permission rule naming the upstream seat
makes no sense in a game where the ban binds everybody, or where a blind draw arms it.

⚠️ **Two consequences nobody had noticed.** **(1) The claim is an attack.** It confers no ownership
and pays nothing (§4.4), so the opener buys melding utility and, free, a lock on the hand of the
player who will discard to them all round. `prospector` (P22) prices the claim as cards and money
and models none of that. **(2) An objection is a disclosure.** Only a holder may object, so
objecting tells the table you hold that rank — **the first thing in this game a player reveals by
choice**, everything else being concealed until the declaration.

### The answer that superseded yesterday's

> *"7 of diamonds, ace of spades, any jokers, when those are shown at the start of the game, those
> cards become money cards but specifically the ones shown can not be owned whether you take them
> or not. The other copy of that card (colour matters for jokers) becomes three times as valuable,
> but the original is worthless as it cannot be owned."*

⚠️ **Rev 19 recorded "double money card, worth double" from the same people one day earlier.**
Both `EXPERT`, and they do not agree. **The later and more specific one is recorded as the rule;
the earlier is struck rather than deleted** (§4.1). 🔥 **This is the second time an answer given
before the framing that governs it turned out not to be an answer** — §7.1's purity ruling failed
the same way, corrected the same day by a player-count rule nobody had asked for. **The framing
here is ownership**: *worth double* is true of a value considered alone, and stops being true once
you ask which copy is worth it.

✅ **The arithmetic backs the later answer, which is why it is recorded without hesitation.** At $1
a money card: an undesignated 7♦ is two ownable copies, $2. A designated one is a worthless shown
copy plus a partner at ×3, $3. **A designation adds exactly $1 — which is what an ordinary
designation adds**, since one of the two copies it designates is the designator itself. **Three is
1 for the partner's own permanence, 1 for the designation, and 1 inherited from the copy that can
no longer be paid for.** Under *double* the sum was $2 and a designation on a permanent money card
would have made the round poorer than any other.

❌ **It withdraws a `DERIVED` note and breaks a shipped test.** P22 derived that *a designation
landing on a permanent money card leaves less money in the deck, not more* and asserted it in
`ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`. **The reasoning was
sound and the premise moved.** ⚠️ `MoneyCardRegistry.Multiplier` caps at 2 and must return 3;
`MoneyOdds` prices blind draws from it; `prospector` is the one rung that reads the money; and
`docs/STRATEGY.md` §10's money sweep was measured under the struck rule. **The claim was written
down as an assertion rather than as prose, so the rules change cannot pass silently** — which is
the whole argument for asserting derivations.

### What is left

⚠️ **Two questions, both raised by these answers rather than left over from before** — `RULES.md`
§9 #30 and #31. Neither stops a game; both stop a correct implementation.

> **#30** — *"When they say no because they've got that card — does it have to be the very same
> card, or just the same number?"*
>
> **#31** — *"If a joker gets turned over at the start, how much does the other one pay?"*

*(#31 is a re-ask on purpose. It was answered inside a sentence about three different cards, and
on a 7♦ the ×3 keeps the round's money level while on a joker it adds three dollars from nothing.
⚠️ **Ask it flat and do not mention the 7♦** — the point is to see whether *three* comes back on
its own.)*

---

## Q9 — Two questions, four rules **(and the pattern that runs through all four sessions)**

**2026-08-21, Mya Lay and Aung Aung.** Two questions put, two answered, promoted into `RULES.md`
**rev 21**. 🔥 **The second answered past the question and changed §4.1 twice over.**

| # | Asked | Answer | Promoted to |
|---:|---|---|---|
| 30 | *"When they say no because they've got that card — does it have to be the very same card, or just the same number?"* | **Rank alone.** | §4.5, closing §9 #30 |
| 31 | *"If a joker gets turned over at the start, how much does the other one pay?"* | **×3 — and "7 of diamonds, ace of spades, AND jokers are always money cards."** | §4.1, closing §9 #31 |
| — | *volunteered* | 🔥 **The ×5 jackpot**: 7♦ and A♠ both turned up, one player owning both partners, pays ×5 each. | §4.1, §4.3 |

### #30 is small and saves a duplicate predicate

**Rank alone**, which is what §5.1 matches on — so a player holding the 9♣ is locked by a claimed
9♥ exactly as one holding the other 9♥ is. ✅ **The objection test and the feeding ban's test are
one predicate**, and P28 depends on P27 for exactly that reason: writing it twice is the defect to
avoid.

### #31 closed because its premise was wrong

The question was *"×3 conserves the round's money on a 7♦ and creates it from nothing on a joker
— is that intended?"* **A joker was never worth nothing: jokers are permanent money cards.** So a
joker's arithmetic is the 7♦'s arithmetic exactly, and rev 20's list of cards whose partner
triples — *7♦, A♠, any joker* — was never a list of special cases. **It is the list of permanent
money cards.**

⚠️ **The permanent side-bet doubles: 4 cards to 8.** Two `DERIVED` arguments are now stale — §4.3's
measured *"the side-bet is 42% of the round prize"* (rev 13, 600 rounds) and §4.4's *"~4 of the 6
are owned the moment the deal ends"*. ✅ §4.2's matching argument is comparative and survives a
bigger base; its stated numbers do not. **P26 re-derives both with a number rather than an
argument.**

✅ **Asking it flat, and deliberately without mentioning the 7♦, is what produced a correction
instead of a confirmation.** Had the question named the 7♦, *"three, same as the 7♦"* was the
obvious reply and the permanence of jokers would still be unrecorded.

### The jackpot

> *"If those two flipped cards happen to be 7 of diamonds and ace of spades, then those each become
> triple money cards, but if you happen to own **both** of those money cards, they become 5x money
> cards instead of 3x money cards."*

🔥 **The first rule in this game where a card's value depends on who is holding what.** Two players
owning one tripled partner each are paid ×3 apiece; one player owning both is paid **×5 apiece** —
$40 a head from a five-player table against a $5 round prize, the largest single swing in the game.

⚠️ **It widens one of the three headline design decisions without breaking it.** *Money status is
computed, never stored on cards* still holds — but `MoneyCardRegistry.Multiplier(Card)` cannot
answer alone, because the ×5 is not a function of the designators. The function stays pure and its
signature does not stay the same.

⚠️ **Its reach was not asked, and is `RULES.md` §9 #32.** The rule names *the 7♦ and the A♠*, and
rev 21 has just made jokers permanent — so a turn-up can now produce two tripled values other ways.
**Do not generalise it in code before it is asked.**

> **#32** — *"Say the two cards you flip at the start are both jokers, one red one black. Does
> anything special happen if one player ends up with both of the other two?"*

### 🔥 The pattern, which is the most useful thing in this file

**All four sessions answered past the question asked, and three of them changed a rule nobody was
asking about.**

| Asked | Answered | And also |
|---|---|---|
| Are sets illegal two-handed? (rev 18) | Yes | — |
| How often is the money card claimed? (rev 19) | Every round | 🔥 **defined *a game* — which then decided the seating rule** |
| Which jokers does a turned-up joker designate? (rev 20) | The one of its colour | 🔥 **the ownership framing — superseding a one-day-old `EXPERT` ruling** |
| How much does a turned-up joker's partner pay? (rev 21) | ×3 | 🔥 **jokers are permanent — and there is a ×5 jackpot** |

⚠️ **This game's rules are recalled as wholes, not as answers.** Ask a narrow question and the
narrow answer comes back attached to the rule it belongs to — but **only if the question leaves
room for it.** Three of the four volunteered rules arrived after a question that named a situation
rather than a quantity, and the one time this file asked *"is it double or does it stack?"* it got
a hedge and a wrong number.

✅ **So the standing advice for the next session is unchanged and now has evidence**: ask the table
situation, never the taxonomy; never name the answer you expect; and **write down the part that
was not an answer to your question**, because three times out of four that has been the part that
mattered.


---

## Q10 — Three confirmations, and a scoring rule nobody knew about **(2026-08-22)**

**Three questions were put flat, all three came back confirming the standing default — and the
third one carried a rule this project had never heard of.** Promoted as `RULES.md` **rev 25**.

| # | Asked | Answer | Promoted to |
|---:|---|---|---|
| 19 | *"…the deck runs out, and everything thrown away gets gathered up and shuffled… Are you still free to throw Queens?"* | **Yes.** | §5.1, closing §9 #19 |
| 32 | *"Does the ×5 need the 7♦ and the A♠ specifically, or would any two tripled values do?"* | **"Specifically."** | §4.1, closing §9 #32 |
| 27 | *"I take the joker you threw away. Are you allowed to throw the other joker at me next turn?"* | **"Yeah."** — the other jokers, all four. | §5.1, closing §9 #27 |
| — | *volunteered* | 🔥 **A ×3 prize for declaring with all series clean.** | **§7.3** (new), §9 #33–#36 |

### The one nobody asked for, again

The answer to #27 did not stop at *"yeah"*. Verbatim:

> *"Yeah. Unless you want all series clean that got a 3-time winning game prize, you have a joker,
> so you discard the joker for the winning clean series."*

🔥 **That is a scoring rule, and §7.2 has said the round payment is *flat* since rev 1** —
`PLAYER`, Settled, and never questioned. An `EXPERT` answer outranks it. **A clean declaration
pays three times the winning prize**: $45 rather than $15 at four seats, $60 rather than $20 at
five, against a measured side bet of about $11.58 a round. **It is the largest single swing in the
game and it is unbuilt.**

⚠️ **It also answers a question nobody had thought to ask**: *why would anyone ever throw a joker
away?* Every rung in this project holds a joker over everything, because a joker fits anywhere —
and the expert's sentence names the one situation in which you part with one. **`docs/STRATEGY.md`
is measured in a world where that reason does not exist.**

### The four follow-ups, phrased flat

**Ask these before anything is built.** One of them has no safe default.

1. **When the joker may be thrown.** *"You've got a joker in your hand and you're going for the
   clean prize, so you need to get rid of it — but I've taken a joker, so jokers are shut against
   you. Can you throw it at me the turn before you go out, or only as the very last card when you
   put everything down?"* (§9 #33 — if it is only the last card, §5.1's existing exception already
   covers it and nothing changes; if it is any turn, the ban has a second exception.)

2. **What "all series clean" reaches.** *"Say I go out with two runs and a set. Both runs are
   clean, but I've used a joker in the set. Do I still get the triple?"* (§9 #34 — ⚠️ **do not
   offer the alternative**; this document has recorded a purity rule as flat over the whole hand
   and had to narrow it twice already.)

3. **Five-handed, where no run is needed.** *"Five of us are playing, so nobody has to have a run
   at all. I go out with four sets and no runs — none of them clean, because there are no runs to
   be clean. Do I get the triple, or does the triple need me to actually have runs?"* (§9 #35 —
   ⚠️ **the one with no safe default.** The three readings pay different amounts and one of them
   pays triple for a hand containing no series at all. **It blocks the five-handed re-measurement,
   P32.**)

4. **What the triple multiplies.** *"When somebody wins clean and everyone pays them triple — does
   the money-card money triple too, or is it just the $5 for the round?"* (§9 #36)

### What was learned about asking

🔥 **Three for three on the recommendations — the first session in this file where every standing
default was confirmed.** ⚠️ **And it is the fifth session running where the answer went past the
question.** The two facts sit together and the lesson is not *"trust the defaults"*: all three
confirmed answers were **narrow** readings, the reading that changes least, and every rule this
document has actually got wrong (§6.2, §7.1) was a **flat, broad** rule later narrowed. **#34 is
that exact shape**, which is why it is being asked rather than defaulted.

⚠️ **The volunteered rule arrived attached to a question about something else, for the fifth time.**
The pattern named at Q9 holds: **this game's rules are recalled as wholes, not as answers.** A
question about jokers and feeding produced a scoring rule, because to the person answering, *why
you would throw a joker* and *what throwing a joker costs you* are one thought.
