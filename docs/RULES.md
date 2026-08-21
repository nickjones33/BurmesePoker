# Burmese Poker — Official Rules

**This is the canonical rules source for this project.** `RULES-PRIMER.md` (short recall
aid) and `RULES-TECHNICAL.md` (implementation spec + defects) are subordinate to it.
Where they disagree, this document wins.

Last revised: 2026-08-21 (rev 23 — ✅ **no rule moved; §5.1 is code now.** Packet P27 built the
feeding ban exactly as this document specifies it: enforced **by construction** (`FeedingBan`,
`TurnContext.LegalDiscards`), armed by public takes only, bound to the seat you discard to,
released permanently by a throw-back that survives the reshuffle, with the declaring-discard
exception and the floor. 🔥 **The rank-only predicate the document said the domain did not have is
`Card.SameRankAs`, and it is one method rather than two** — §9 #30 makes the claim's objection test
the same question, so §4.5 will read it rather than write a second one. ⚠️ **Both `PLAYER` house
rulings are marked as such in the code they decide** — the joker closing the other jokers (#27) and
a release surviving the reshuffle (#19) — so if either is ever put to a player and comes back
differently, the test that moves says whose ruling it was. §10 #13 is discharged; nothing in §5.1
is left unimplemented. Rev 22 — ✅ **no rule moved; the two `DERIVED` notes rev 21 left stale
are re-derived, with numbers.** Packet P26 built the money layer as rev 20 and rev 21 state it, and
re-measured what the side-bet is worth under it. **§4.3: the money cards move `$11.58 ± 0.34` a
round over 600 five-player rounds** — **58% of the $20 round prize** and **37% of all the money
that changed hands**, against $8.43 / 42% / 30% measured at rev 13 under four permanent cards and
no triple. 🔥 **The side-bet grew by more than a third and the game is still not inverted**, so
§4.2's balance argument survives with new numbers. **§4.4: 10 ownable money cards sit in the
106-card pool in the ordinary case, not 6, so ~6.1 are owned the moment the deal ends and ~3.9 are
left to be drawn for** — the *fraction* settled at the deal is unchanged at 61%, because it is
65/106 and always was; what nearly doubled is the **live** component. ✅ **And the ×3's
conservation argument is no longer a prediction**: a designation on the 7♦ and one on an ordinary
card leave **exactly the same** money loose in the shoe, asserted as an equality in
`ProspectorBotAgentTests`. ⚠️ **§9 #32 stays open and the code is fenced to the narrow reading.**
Rev 21 — 🔥 **jokers are permanent money cards, and there is a
jackpot.** Asked how much a turned-up joker's partner pays, the answer went behind the question:
**"7 of diamonds, ace of spades, AND jokers are always money cards"** — so the permanent side-bet
is **eight cards, not four** (§4.1), and two `DERIVED` arguments built on the old count are stale
(§4.3, §4.4). It also makes rev 20's ×3 list stop looking arbitrary: it was never a list of special
cards, it is **the list of permanent money cards**, and ×3 is what a designation landing on one
does. 🔥 **And a rule nobody asked for: if the two turned-up cards are the 7♦ and the A♠ and one
player owns *both* partners, they pay ×5 each rather than ×3** — **the first rule in this game
where a card's value depends on who holds what**, so a multiplier is a property of *(value,
ownership)* and `MoneyCardRegistry.Multiplier(Card)` can no longer answer alone (§10 #17). ✅ **§9
#30 is closed — an objection turns on the *rank*, which is §5.1's own predicate** — and #31 closed
by its premise turning out to be wrong. ⚠️ **One question left, §9 #32**: whether the ×5 needs the
7♦/A♠ pair specifically or any two tripled values — a combination that exists only because jokers
became permanent. Rev 20 — 🔥 **the money layer's last two questions closed, and both
answers reached past the question.** **(1) Claiming the turned-up money card needs the permission
of the player who goes *before* you in turn order — and they may refuse only if they hold that
card**, because your public take would lock them into holding it under §5.1 (§4.5, `EXPERT`).
**This is the first rule tying the money layer to the feeding ban**, and it independently confirms
two §5.1 rulings that were only recommendations when they were made — that the ban binds the seat
above you, and that the §4.5 claim arms it. **(2) A turned-up joker designates the other joker of
its own colour** — *"colour matters for jokers"* — so §4.2 applies unchanged and `SameValueAs`
already computes it (§4.1). ⚠️ **And an answer nobody asked for supersedes one given the day
before**: a turned-up 7♦, A♠ or joker **can never be owned, claimed or not, and its partner copy
pays ×3** — not the *double* rev 19 recorded. ✅ **The arithmetic backs the later answer**: ×3 makes
a designation on a permanent money card worth exactly what an ordinary designation is worth.
❌ **It withdraws P22's `DERIVED` note**, which said the opposite, and the shipped test that asserts
it — `MoneyCardRegistry.Multiplier` caps at 2 and must return 3, and `docs/STRATEGY.md` §10's money
sweep was measured under the struck rule (§10 #17, #18). ⚠️ **Two new questions, §9 #30 and #31**,
both raised by these answers and both blocking a correct implementation of them. Rev 19 — ✅ **the money layer is confirmed by a person for the first
time, and §9 is down to two questions.** Nine closed with **Mya Lay and Aung Aung**, eight of
which confirmed a standing default: **7♦ and A♠ are money cards in both copies out of both decks**
and a turned-up 7♦ makes it a **double, *"worth double"*** (§4.1, `CODE` `Probable` → `EXPERT`);
**a claimed money card's value goes on paying** — *"all 9s of hearts become money cards"* (§4.5);
**the turned-up cards stay out of the reshuffle** (§4.5); **you may throw back the card you just
took, *"as long as you aren't violating any other discard rules"*** (§5, which quietly settles how
it meets §5.1); **a turned-up joker designates jokers** (§4.1 — ⚠️ *which* jokers is still open);
and **7♦ and A♠ are what they are by "tradition"**, closing a question recorded as unrecoverable
since rev 1. 🔥 **One answer reversed a default and one arrived as a definition, and they are the
same answer**: *a game* means **from the turn-up to somebody going out** — a round (§3) — so the
money-card claim is offered **every round**, and **the seats are re-randomised every round too**
(§9 #14). `MatchEngine` holds one seating for a whole match and is now wrong about it (§10 #16).
✅ **No published measurement moves** — every experiment runs one round a game. Rev 18 — ✅ **the win condition is now fully specified.** The two
questions rev 17 left open against §7.1.1 are closed, both `EXPERT`, both from Mya Lay and Aung
Aung: **at two players a set is illegal *as a meld***, not merely unnecessary — *"you must go out
with ALL SERIES, no sets"* — so a two-handed hand must partition into runs alone (§9 #22); and
**a meld may be nothing but jokers**, which is a series like any other **but never a clean one**,
so it can discharge a *surplus* series and never a *required* one (§6.1, §9 #29). 🔥 **The second
also closes §9 #8**, open since rev 1 and the only question in this document that a piece of
reasoning got right before anybody was asked — P3 has emitted all-joker melds since rev 10 on
exactly that argument. **No rule changed and no code changes**; §10 #14 now has nothing open
against it. Rev 17 — 🔥 **the win condition is no longer one rule.** §7.1.1 is
new: what a declared hand must contain **changes with the number of players** — series only at
two, at least two series at three, at least one at four, no requirement at five or more — and **the series a table size *requires* must be
joker-free** — both of them three-handed, the one of them four-handed, and cleanliness is
irrelevant two-handed and five-or-more-handed (§7.1.1). `EXPERT`, from a session with **Mya Lay and Aung
Aung** on 2026-08-20. The same session settled four more: **the discard has no exception, ever**
(§7.1, closing §9 #6); **the discards are public and may be looked through** (§5, closing §9 #15
*against* its standing recommendation and un-mooting §9 #9); **only the immediately-previous
player's discard is takeable** (§5, `CODE` `Tentative` → `EXPERT`); and **7♦ and A♠ really are
permanent money cards** (§4.1, `CODE` `Probable` → `EXPERT`). ⚠️ **Two players and three players
are real games**, so §2's "four minimum" was wrong and §2.1 now runs from two. ✅ **Everything those answers left unspecified has since closed** — #22, #24, #25, #26 and #27 — and §10 #14–#15 are the code they change. Rev 16 — **§5.1 is new and is the first rule here that constrains
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
| **And nothing else does** — hand size, decks, money cards and stakes are the same at every table size. | `EXPERT` | Probable — ⚠️ *"not that I know of"*, rev 19 |
| **6 players practical maximum** (see §2.1). | `DERIVED` `IR` | Probable |
| Two stakes are agreed **before play** and hold all game: a **round value** and a **money card value** (§4.3). | `PLAYER` | Settled |
| Typical stakes: **$5 round / $1 money card**. | `PLAYER` | Probable |
| Each player starts with an equal bank. Code uses **100**; the real buy-in is unrecorded. | `CODE` | Tentative |
| **13 cards** dealt to each player, one at a time. | `PLAYER` `CODE` `IR` | Settled |
| Turn order fixed and circular. Code randomizes it at start. | `CODE` | Settled |

> ⚠️ The code hardcodes **5 players** with fixed names. Player count must become
> configurable.

> ⚠️ **Only the win condition moves with the count — asked in rev 19 (§9 #26), answered with a
> hedge.** *"Not that I know of."* It is recorded as **Probable rather than Settled** and the
> hedge is kept verbatim, because §7.1.1 is proof that this question has a real answer at least
> once: nobody volunteered the player-count win condition either, and it turned up on its own.
> **The question was asked deliberately wide** — hand size, number of decks, what the money cards
> are, what you play for — so that a second §7.1.1 had somewhere to appear. It did not.

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
2. **Randomize seating order — every round, not once.** `EXPERT`, rev 19 (§9 #14).
3. Deal **13 cards** to each player, one at a time around the table.
4. Turn up **two money cards** — one from the **bottom** of the deck, one from the **top**.
5. Designate money cards (§4).

Because money cards are turned up **after** the deal, a player may already be holding a
copy of a card that is about to become a money card. That appears to be the point.

> 🔥 **A "game" is one round — and that definition is a rule, not a gloss.** Asked how often the
> money-card claim is offered (§9 #5), the answer defined the unit it depends on: *"it happens at
> the beginning of the game, if game means from the time the money card is turned up to the time a
> player goes out."* `EXPERT`, rev 19. **So a game begins at the turn-up and ends when somebody
> goes out**, and what this document calls a *round* is what a player calls a *game*. A session is
> a sequence of them with the banks carrying over (§7.2), and **nothing about it is remembered
> except the money**.
>
> 🔥 **This is what makes §9 #14's answer bite.** Seating is re-randomised *between games*, and
> a game is a round — so **step 2 runs before every deal**. There is no fixed table, no dealer
> rotation, and no persistent seat: the player on your left this round is on the far side of the
> table the next. ⚠️ **The engine keeps the seating it was given for a whole match** and is wrong
> about it (§10 #16).
>
> ⚠️ **This document keeps saying *round*** — every rule, every cross-reference, and every column
> in `docs/strategy/measurements.csv` — because renaming the unit would break far more than it
> clarifies. **Read *round* and *game* as the same thing.** The one place the player's word must
> win is where a rule is stated *in terms of* it, which is exactly §9 #5 and #14.

---

## 4. Money cards

The distinguishing mechanic of the game.

### 4.1 What counts as a money card

| Rule | Provenance | Confidence |
|---|---|---|
| **7♦, A♠ and every joker are permanent money cards** in every round. | `EXPERT` | Settled — the jokers were added in rev 21 |
| …**all copies, both decks** — 2 × 7♦, 2 × A♠, **4 jokers: eight cards before anything is turned up.** | `EXPERT` | Settled — rev 19, widened rev 21 |
| The **two turned-up cards** are money cards for that round. | `CODE` | Settled |
| **A turned-up card is owned by nobody** — not by the table, and not by the opener who claims it. *"The ones shown cannot be owned whether you take them or not."* | `EXPERT` | Settled — rev 20 |
| **If the turned-up card is a 7♦, an A♠ or a joker, that value pays ×3.** The shown copy is worthless, because nothing unowned pays — so in practice the **partner copy** carries all of it. | `EXPERT` | Settled — rev 20, restated rev 21 |
| 🔥 **If the two turned-up cards are the 7♦ and the A♠, and one player owns *both* partners, the two pay ×5 each instead of ×3.** | `EXPERT` | Settled — rev 21; ⚠️ its reach is §9 #32 |
| A turned-up **joker** designates **the other joker of its own colour**. Colour is a joker's identity, so §4.2 applies unchanged. | `EXPERT` | Settled — rev 20 |
| ~~If a turned-up money card is the 7♦ or A♠, it becomes a **double money card**.~~ | ~~`EXPERT`~~ | ⚠️ **Superseded in rev 20**, one day after it was recorded — see below |
| Stacking past the triple is impossible to reach in a way that pays anyone. | `DERIVED` | Settled |

> ✅ **Why 7♦ and A♠ — answered in rev 19, and the answer is that there is no answer.**
> **"Tradition."** `EXPERT`. Rev 1 through 18 recorded this as *unknown, likely unrecoverable*
> and it was neither: it was recoverable, and what came back is that the cards are arbitrary.
> 🔥 **A question can close by confirming there is nothing behind it**, and that is worth more
> than leaving it open — an open *why* invites a future session to go looking for a pattern in
> `7♦`/`A♠` that does not exist.

> ✅ **Both copies, both decks — closed in rev 19 (§9 #24).** Asked flat — *"the 7♦, is it both of
> them out of the two decks, or just one?"* — **both**. It had stood at `CODE` `Probable` since
> rev 1, read off the 2023 source, under the whole money layer. **The code was right.**
> ✅ **And it agrees with §9 #12's answer given in the same breath** — *"all 9s of hearts become
> money cards"* — so a **designated** value and a **permanent** one both cover both copies. Two
> answers, one rule, and neither was asked in the other's terms.

> 🔥 **Rev 20 replaced *double* with *triple, on the other copy only* — and this supersedes an
> `EXPERT` answer given one day earlier.** Rev 19 asked what happens when a turned-up card is the
> 7♦ and recorded **"double money card, worth double."** Rev 20 asked something else — what a
> turned-up joker designates — and got the whole rule back with the framing that governs it:
>
> > *"7 of diamonds, ace of spades, any jokers, when those are shown at the start of the game,
> > those cards become money cards but specifically the ones shown can not be owned whether you
> > take them or not. The other copy of that card (colour matters for jokers) becomes three times
> > as valuable, but the original is worthless as it cannot be owned."*
>
> ⚠️ **Both answers are `EXPERT` and they do not agree, so the later and more specific one is the
> rule and the earlier one is struck rather than deleted.** This is the second time in this
> document that **an answer given before the framing that governs it turned out not to be an
> answer** — §7.1's purity ruling failed in exactly this way, and was corrected the same day by a
> rule about player counts that nobody had asked for. 🔥 **The framing here is ownership**:
> *"worth double"* is true of a value considered on its own, and stops being true the moment you
> ask **which copy** is worth it.

> 🔥 **Jokers are permanent money cards, and rev 21 is where the money layer got half again as
> big.** Asked how much a turned-up joker's partner pays, the answer went behind the question:
> **"7 of diamonds, ace of spades, AND jokers are always money cards."** `EXPERT`. Rev 1 to rev 20
> listed two permanent values; there are **three**, and jokers come four to a shoe.
>
> | | cards paying before anything is turned up |
> |---|---|
> | **rev 1 – rev 20** | 2 × 7♦ + 2 × A♠ = **4** |
> | **rev 21** | 4 + **4 jokers** = **8** |
>
> ⚠️ **This is not a detail; it doubles the permanent side-bet and moves two `DERIVED` arguments
> built on the old count.** §4.4's *"~4 of the 6 money cards are owned the moment the deal ends"*
> and §4.3's measured *"the side-bet is 42% of the round prize"* (rev 13, 600 rounds) were both
> computed with four permanent cards, and neither has been re-derived. ⚠️ **§4.2's balance
> argument is *not* re-opened by this** — it is comparative, and rank-matching would still put
> about twice as many cards in play again — **but its stated numbers are stale.** Recorded rather
> than silently recomputed (§10 #17).
>
> 🔥 **It also explains the ×3 rule's shape, which looked arbitrary until now.** Rev 20 named
> *"7 of diamonds, ace of spades, any jokers"* as the cards whose partner triples, against a §4.1
> that made only two of them permanent. **It was never a list of special cards — it is the list of
> permanent money cards**, and ×3 is what happens when a designation lands on one.

> 🔥 **And there is a jackpot — the first rule in this game where a card's value depends on who
> holds what.** *"If those two flipped cards happen to be the 7♦ and the A♠, then those each
> become triple money cards, but if you happen to own **both** of those money cards, they become
> 5× money cards instead of 3×."* `EXPERT`, rev 21.
>
> **So a multiplier is not a property of a card.** It is a property of *(value, ownership
> configuration)*: two players holding one tripled partner each are paid **×3 apiece**; one player
> holding both is paid **×5 apiece** — ten money-card values to a single player, **$10 a head at
> standard stakes against a $5 round prize**, which is the largest single swing in the game.
>
> ⚠️ **This is where one of the three headline design decisions has to widen.** *Money status is
> computed, never stored on cards* still holds and is still right — but
> `MoneyCardRegistry.Multiplier(Card)` **cannot answer alone any more**: it needs the round's
> **ownership** as well as its designators. The function stays pure; its signature does not stay
> the same (§10 #17).
>
> ⚠️ **Its reach was not asked, and that is §9 #32.** The rule was stated of *the 7♦ and the A♠*.
> Rev 21 makes **jokers permanent too**, so a turn-up can now produce two tripled values other
> ways — a 7♦ and a joker, or two jokers of opposite colours — and nobody knows whether those pay
> ×5 to a player holding both. **Do not generalise it in code before it is asked**; what is
> recorded is the narrow rule.

> 🔥 **Three is exactly the number that conserves what a designation is worth — `DERIVED`, and it
> is the strongest evidence that rev 20's answer is the real rule rather than a slip.** Work the
> money through for the 7♦, at $1 a money card:
>
> | | 7♦ money live in the round | |
> |---|---|---|
> | **Not designated** | two copies × 1 = **$2** | both ownable |
> | **Designated (turned up)** | shown copy **$0** + partner × 3 = **$3** | one ownable |
>
> **A designation adds exactly $1 of live money — and $1 is what an *ordinary* designation adds
> too.** Turn up a 5♥ and both 5♥s become money cards, but one of them is the designator lying on
> the table, so **one** reaches a player. 🔥 **Under the struck *double* rule the sum came to $2,
> and a designation landing on a permanent money card would have made the round poorer than an
> ordinary one.** The triple makes every designation worth the same wherever it lands. **Three is
> not an arbitrary multiplier: it is 1 for the partner's own permanence, 1 for the designation,
> and 1 inherited from the copy that can no longer be paid for.**
>
> ✅ **And it conserves for a joker too, which rev 20 could not see.** This note originally
> flagged that a joker pays nothing until it is turned up, so ×3 would *create* $3 rather than
> conserve $1 — recorded as §9 #31. **The premise was wrong: jokers are permanent money cards**
> (rev 21), so a joker's arithmetic is the 7♦'s arithmetic exactly. 🔥 **The question closed by
> its answer turning out to be about a different rule** — which is why it was asked flat, and
> without mentioning the 7♦.

> ✅ **Stacking past the triple is closed by construction, and rev 19's argument survives intact.**
> A stack needs **both** turned-up cards to be the same value, and two decks hold exactly two 7♦.
> In that case both copies lie on the table, there is **no partner** to be paid ×3, no player was
> dealt one (the deal is step 3, the turn-up step 4), and the value pays nothing at all. Reachable
> at about **one round in 5,800** — so an implementation must not assume it away, it must merely
> not care. The same covers a 7♦ and an A♠ turned up together, which is two separate triples and
> not a stack. `DERIVED`.
>
> ⚠️ **Rev 19's hedge — *"maybe… i don't think that's mechanically possible/relevant"* — was wrong
> about *possible* and right about *relevant*, and not flattening it is what left the question
> open long enough for rev 20 to contradict the rule it was hedging about.**

> ✅ **That they *are* permanent is no longer a reading of the old code.** Rev 17: asked flat —
> *"apart from the two you turn over, are there any cards that are always money cards?"* — and
> answered **yes**. It had stood at `CODE` `Probable` since rev 1, which is a weak tag under the
> whole money layer. ⚠️ **The three follow-ups behind it were not reached**: whether it is both
> copies out of both decks, what happens when a turned-up card *is* the 7♦, and whether it can
> stack past a double (§9 #24). ✅ **All three were reached and answered in rev 19** — see the
> notes below; the doubling rules are `EXPERT` now, bar the ceiling itself.

> ❌ **WITHDRAWN in rev 20. The claim below is false under the rule as it is now recorded, and it
> is asserted as a shipped test.** It was derived from the *double* rule that rev 20 struck
> (§4.1); under the **triple**, a designation landing on a permanent money card leaves the round
> with **exactly as much** live money as any other designation — $1 more than no designation at
> all — which is the arithmetic in the note above. 🔥 **The reasoning was sound and the premise
> moved**, which is the difference between a mistake and a dependency. ⚠️ **It is load-bearing in
> code**: `MoneyOdds` prices a blind draw from `MoneyCardRegistry.Multiplier`, which caps at 2,
> and `ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe` asserts the
> withdrawn direction outright. **Both are §10 #17.** ⚠️ **And `prospector` is the one rung whose
> decision reads the money**, so `docs/STRATEGY.md` §10 — the money sweep — was measured under a
> money model this document no longer holds. **The text below is kept unaltered as the record of
> what was believed and why.**
>
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

> ✅ **A turned-up joker designates the other joker of its own colour — closed in rev 20 (§9
> #11).** 4 of the 108 cards are jokers, so one of the two turned up at setup is a joker roughly
> one round in fourteen. Three readings were on the table: **the two of its own colour**, **all
> four**, or **jokers cannot be money cards** and the card is set aside and another turned. Rev 19
> eliminated the third — *"jokers become money cards"* — and rev 20 answered the rest in four
> words: **"colour matters for jokers."** `EXPERT`.
>
> ✅ **So §4.2 applies unchanged and the recommendation standing since rev 1 was right.** A joker
> has no rank and no suit, and `Card` tells the two of a deck apart by **colour** — so *the other
> copy of that card* means the other joker of the same colour, and `SameValueAs` already computes
> it. **The code needs no joker special case for designation**; what it needs is the ×3 (§10 #17).
>
> 🔥 **This is §5.1's #27 in a second place, and the two disagree — which was predicted and is
> correct.** #27 asked what taking a joker *closes* and was answered *the other jokers*, all four,
> because feeding is about what an opponent is collecting. Designation is deliberately narrow
> (§4.2 rejects the Indian Rummy rank match precisely to keep the side-bet small). **So a red
> joker taken in the open closes all four jokers, while a red joker turned up pays on two.** The
> same word means four cards in one rule and two in the other, and neither is inconsistent.
> ⚠️ **#27 is still the weaker of the two** — a `PLAYER` house ruling from *"I'd assume"*, where
> #11 is now `EXPERT`.

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
| A player owning 1 **tripled** money card collects | 3 × $1 × 4 = **$12** |
| A player owning **both** of a 7♦/A♠ turn-up's partners collects | (5 + 5) × $1 × 4 = **$40** |

| Detail | Provenance | Confidence |
|---|---|---|
| Flat round value from each loser to the winner. | `PLAYER` | Settled |
| Money card value per card, per opponent, to the **owner**. | `PLAYER` | Settled |
| Settled on top of the round payment, not instead of it. | `PLAYER` | Settled |
| A **tripled** money card counts as **three**; only the ownable copy can collect it (§4.1). | `EXPERT` | Settled — rev 20 |
| **×5 each** where one player owns both partners of a 7♦/A♠ turn-up (§4.1). | `EXPERT` | Settled — rev 21 |

✅ **Re-measured under rev 21, in packet P26 (2026-08-21).** The note below is the current
one; the rev 13 measurement it replaces is kept beneath it as the record of what the side-bet
was worth before jokers became permanent and before the ×3.

**`DERIVED` — the recalled stakes still produce a balanced game, with the side-bet larger
than it was.** Under the exact-match rule (§4.2) **~10 ownable money cards** circulate in the
106-card pool in the ordinary case — eight permanent (§4.1) plus one partner for each of the two
designators. Over **600 simulated five-player rounds** (`--games 600 --seats 5 --seed 20260821`,
greedy vs simple, summing each round's positive side-bet deltas) the money cards moved
**`$11.58 ± 0.34` a round** against the flat prize of $20 — **58% of it**, and **37% of all the
money that changed hands**.

> 🔥 **The side-bet grew by more than a third and the game is still not inverted, which is what
> §4.2's argument needed.** Rank-matching would put roughly twice as many cards in play again and
> take the side-bet clear past the round prize; the exact-match rule leaves it at a bit over half
> of it. **The comparative argument survives rev 21 intact and its numbers have moved with it.**
> ⚠️ **This measures the ×3 and the eight permanent cards. It does not usefully measure the
> ×5** — the turn-up is the 7♦/A♠ pair about **one round in 1,444**, so the expected number of
> jackpots in a 600-round run is **0.4**. Whatever this run happened to contain, the figure above
> is a measurement of the triple and of the eight permanent cards, and the jackpot's contribution
> to it is noise.

> ⚠️ **Superseded, and kept as the record.** **Measured at rev 13** (packet P12, 2026-08-18):
> over 600 simulated five-player rounds the money cards moved **$8.43 a round** against the flat
> prize of $20 — **42% of it**, and 30% of all the money that changed hands, from a pool of ~6
> money cards. 🔥 **The same command against the pre-P26 tree at a different seed reproduces it to
> the cent — $8.50, 42.5%, 29.8%** — which is why the $11.58 above is a measurement of the rules
> change rather than of a change of method.

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

> ✅ **A turned-up card is unownable, full stop — confirmed in rev 20.** *"The ones shown cannot
> be owned whether you take them or not."* `EXPERT`. Rev 1 to rev 19 had this as `CODE` `DERIVED`,
> reasoned from rule 1 rather than confirmed, and it was right. 🔥 **The volunteered half is the
> half that mattered**: *whether you take them or not*. A designator does not become ownable by
> being picked up, and it does not become ownable by being left alone — so a value with **both**
> copies on the table pays nobody at all (§4.1), and the ×3 exists precisely because the shown
> copy can never be paid for.

| How the card reached your hand | Pays you? | Provenance |
|---|:-:|---|
| **Dealt** in your opening 13 | ✅ | `EXPERT` |
| **Drawn** blind from the deck | ✅ | `EXPERT` |
| **Picked up** from the previous player's discard | ❌ | `EXPERT` |
| **Claimed** from the turned-up money cards (§4.5) | ❌ | `EXPERT` — confirmed in rev 20 |
| **Left lying on the table** as a designator, unclaimed | ❌ | `EXPERT` — confirmed in rev 20 |

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

> **`DERIVED` — the money layer is mostly settled at the deal.** *(Re-derived under rev 21 in
> packet P26, 2026-08-21.)* Since dealing confers ownership, most money cards are claimed before
> anyone acts. In a 5-player round, **~10** ownable money-bearing cards sit in the 106-card pool
> in the ordinary case — the **eight** permanent ones (§4.1: two 7♦, two A♠ and four jokers) plus
> one partner for each of the two designators. 65 of the 106 are dealt, so roughly **6.1 of the
> 10 are owned the moment the deal ends**, with the remaining **~3.9** claimable by drawing during
> play.
>
> ⚠️ **The fraction did not move; the counts did.** 65/106 is 61% whatever the pool holds, and it
> was 61% when this note said *4 of 6*. What rev 21 changed is the size of both halves — and the
> **live** component nearly doubled, from ~2 cards to ~3.9. 🔥 **That matters more than the total
> does**, because the live half is the only half a player can do anything about (§4.4, and the one
> rung that reads the money, `prospector`).
>
> The money side-bet is therefore **still substantially a deal lottery, with a larger live
> component than it had** — which fits its role: it is not meant to be played for, only settled.
> This is the direct consequence of the answer to question #3 and is worth remembering when
> tuning stakes.
>
> *(Exact, over every turn-up rather than the ordinary case: a turn-up that is itself a permanent
> money card takes a card out of the pool instead of adding a partner, so the expectation is 9.68
> ownable cards and **5.94 owned at the deal**.)*

### 4.5 Claiming the turned-up money card

| Rule | Provenance | Confidence |
|---|---|---|
| The opening player may take the top money card instead of drawing. | `CODE` | Probable |
| They take the **actual physical card**, which leaves the table. | `PLAYER` | Probable |
| **Claiming requires permission — from the player who takes their turn *before* you**, i.e. the last seat in the round order and the one that discards to you. | `EXPERT` | Settled — rev 20 |
| **That player may refuse only if they hold that card**, because your taking it would lock them into holding it under §5.1. | `EXPERT` | Settled — rev 20 |
| **Offered at the start of every round**, because a game *is* a round (§3). | `EXPERT` | Settled — rev 19 |

> ⚠️ The code **clones** the card, creating a 109th card and leaving the original on the
> table. Nick: *"you almost certainly do take the actual physical card, not a copy."*
> **This is a bug.** See `RULES-TECHNICAL.md` §5.

> **`DERIVED` — a claimed money card pays nothing, and that is the point.** By §4.4 a card
> taken **from the table** is held but never owned, so claiming the turned-up money card buys
> you melding utility with **no payout attached**. The code's `MoneyCardOwner = null` and its
> *"doesn't score for the new steward"* comment encode exactly this. Confirmed as intended.

> ✅ **The turned-up cards stay where they are — closed in rev 19 (§9 #4).** Asked with the
> reshuffle in the question — *"if the deck runs out and you gather the discards up to make a new
> deck, do those two go in as well, or do they stay where they are?"* — **they stay.** `EXPERT`.
> So two cards leave play each round purely to act as designators, one of which may be claimed on
> the opening turn, and the deck-exhaustion sweep (§5) takes the discard piles and nothing else.
> **This is what the code already does** and what P9 assumed; it had stood on a recommendation
> since rev 1.
>
> 🔥 **It is also why the doubling question is moot** (§4.1): a designator on the table is owned
> by nobody and stays there, so a value whose every copy is a designator pays nothing at all.

> 🔥 **Why permission is asked, and who from — closed in rev 20 (§9 #5), and it is the first
> rule in this document that ties the money layer to the feeding ban.**
>
> > *"When you go to pick up that first card at the start of the game, you must ask 'permission'
> > from the player who goes before you in turn order (last in the round), because if (and only
> > if) that player has that card, they can object to you picking up that card, since it would
> > lock them into holding the card via the discard rules."*
>
> `EXPERT`. The 2023 source had a permission check with a `TODO` against it and no explanation;
> rev 1 to rev 19 carried it as `Unknown`, and **P9 asks nobody**. It is real, and the reason is
> §5.1.
>
> 🔥 **Follow the mechanism, because it is the whole justification.** Claiming the turned-up card
> is a **public take** (§9 #17), so it arms the feeding ban against the seat that discards to the
> claimer (§9 #16) — and that seat is exactly *the player who goes before you in turn order*. If
> that player holds a copy, they may now never discard it: the ban runs until the protected player
> releases the rank themselves (§5.1), and the claimer, having just taken one, is in no hurry.
> **So the opener's free card is paid for out of the upstream player's hand, and the rule gives
> that player a veto.**
>
> 🔥 **This independently confirms two rulings that were recommendations when they were made.**
> §9 #17 — *only a public take arms the ban, and the §4.5 claim counts as one* — and §9 #16 — *the
> ban binds the seat above you and not the table*. Both were reasoned out in rev 16 from what
> would make the rule enforceable, both were confirmed flat in rev 17, and **this rule cannot be
> stated without them.** A permission rule that names the upstream seat is only sensible in a game
> where the ban is an ordered-pair rule armed by public takes. ⚠️ **Three independent routes to
> the same shape is the strongest structural evidence this document has that §5.1 is an old rule
> rather than a one-off table ruling.**
>
> ⚠️ **It also makes the claim an *attack*, which nothing had noticed.** A claimed card confers no
> ownership and pays nothing (§4.4) — so the opener spends their draw on melding utility alone,
> and gets, as a side effect, a lock on the hand of the player who will discard to them all round.
> 🔥 **The veto is what stops that being free**, and it prices the claim in a way no bot models:
> `prospector` (P22) values the claim purely as cards and money.
>
> ⚠️ **An objection is public information in a game that conceals everything else.** Only a holder
> may object, so **objecting tells the table you hold that rank** — the second public fact in the
> game after the discard piles (§5), and the first that a player discloses by choice. Whether that
> is a cost worth weighing is a strategy question nobody has asked; that it *is* a disclosure is
> forced by *"if and only if."*
>
> ⚠️ **Two things this does not say**, and an implementation needs both — §9 #30 and the note under
> §5.1. **Does the objection turn on the exact card or on the rank?** The rule says *"has that
> card"*, but the lock it describes is §5.1's, and §5.1 matches on **rank alone** — so a player
> holding the 9♣ is locked by a claimed 9♥ just as surely as one holding the other 9♥, and the
> stated reason covers them both. **The narrow reading and the stated justification disagree**,
> which is exactly the kind of gap this document has been wrong about before (§6.2, §7.1).

> ✅ **The claim is offered every round — closed in rev 19 (§9 #5), and the answer arrived as a
> definition.** *"It happens at the beginning of the game, if game means from the time the money
> card is turned up to the time a player goes out."* `EXPERT`. **A game is a round**, so the claim
> belongs to the setup that produced the card and not to the session — which is what P9 defaulted
> to and is now confirmed. See §3 for the definition, which turned out to govern §9 #14 as well.
>
> ✅ **The other half of #5 — *does anyone have to agree?* — was answered in rev 20: yes, the
> upstream player, conditionally.** See the block above; §9 #5 is closed.

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
| **You may discard the very card you just took** — *"as long as you aren't violating any other discard rules."* | `EXPERT` | Settled — rev 19 |

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

> ✅ **Throwing back what you just took is legal — closed in rev 19 (§9 #13), and the answer came
> with a rider that is the interesting half.** Asked flat, and answered **yes — *"as long as you
> aren't violating any other discard rules."*** `EXPERT`. Several rummies forbid it as a null
> move; this one does not. **P7's default was right and the engine already accepts it.**
>
> 🔥 **The rider names §5.1, which is the only other discard rule there is** — and it settles an
> interaction nobody had put in writing: **the feeding ban outranks the right to throw a card
> straight back.** Take the card the seat above you threw, find it is a rank the seat *below* you
> has taken in the open, and you may not return it — you must throw something else. ⚠️ **So the
> just-taken card is not a special case in either direction**: it is filtered like any other card
> in the hand, which is exactly what impossible-move enforcement already does (§5.1, §10 #13), and
> **an implementation needs no rule for it at all.**
>
> ⚠️ **It also confirms the shape of the answer rather than only its value.** The player did not
> say *"yes"*; they said *"yes, subject to the other rules"* — which is a player describing a
> **legal-move filter**, the same construction §5.1's enforcement takes. That is one more reason
> to build the filter once and apply it to the whole hand.

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

✅ **It is implemented — packet P27, 2026-08-21, and there is nothing of §5.1 left over.**
`Domain/Play/FeedingBan.cs` is the rule: two sets of ranks per seat, kept rather than read back off
a pile, and one method — `LegalDiscards(hand, rules)` — that is the whole of what a turn offers.
`PlayerState.MayNotBeFed` is the seat's own record and `TableState.SeatFedBy` finds the one seat
that reads it; `TurnContext.LegalDiscards` is what **every** player picks from, bot or person, and
`CoverScore.Ranking` takes a context rather than a hand so that no rung can forget. The two
exceptions and the floor are in that one method, and `RoundEngine` refuses a discard the turn never
offered — a guard against a broken agent, not the enforcement.

⚠️ **The one thing to know before touching it: the predicate is `Card.SameRankAs`, which is
`Rank == other.Rank` and nothing more.** It is deliberately neither of the two identity notions the
domain already had, and the nullable comparison is what makes a joker close the other jokers —
which is #27's house ruling falling out of the type rather than being derived. Widening it to
`SameValueAs` would compile, pass most of the tests, and implement the wrong rule.

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
| **A meld may be nothing but jokers.** Three jokers laid down together are a series — but never a *clean* one, so it can never be one of the series a table size **requires** (§7.1.1). | `EXPERT` | Settled |
| **No maximum jokers per meld.** | `DERIVED` | Probable |

> ⚠️ **The code currently allows `K-A-2`.** Its rank order is a full cycle. This both
> contradicts the rule above *and* causes a **verified infinite loop** — a hand holding all
> 13 ranks of one suit hangs forever. Removing the wrap fixes both at once. This is the
> highest-priority code fix.
>
> Indian Rummy states the rule precisely: *"The ace can be next to the two (in A-2-3) or
> next to the king (in Q-K-A), but not both at once."* (`IR`)

> ✅ **The all-joker meld is legal — closed in rev 18 (§9 #29), and it confirms what P3 already
> emits.** Asked as a table situation — *"if you were holding three jokers, could you put those
> three down together as one of your melds?"* — and answered **"yes, but not clean"**. The
> recommendation standing since rev 10 was *unlimited jokers, which permits an all-joker meld*,
> and P3 took it literally; the answer confirms it, so **nothing in the code changes**.
>
> 🔥 **The second half of the answer is the load-bearing half, and it is a restriction, not a
> permission.** An all-joker meld is a series that is by construction impure, so under §7.1.1 it
> can satisfy a **surplus** series and never a required one. Three-handed, where both required
> series must be clean, three jokers can never be one of the two. That is consistent with §9 #28
> — purity attaches to the required count and stops there — and it means the two answers agree
> without either having been asked in the other's terms.
>
> ⚠️ **The maximum-jokers half of the old question is `DERIVED`, not `EXPERT`.** Nobody was asked
> *"how many jokers may one meld hold?"*; the answer above is deduced from the fact that a
> three-card meld may be **entirely** jokers, which forecloses any cap below the meld's own size.
> Four jokers exist in the shoe (§2), so the effect is bounded either way. If a cap turns out to
> exist it is a filter in candidate generation and a rung's evaluation, not a rules restructure.

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

> ✅ **Three things this did not say. All three are now answered, and the last of them closed in
> rev 18** — §9 #22, #23, #28.
>
> 1. ✅ **At two players, a set is illegal *as a meld*, not merely unnecessary — closed in rev 18
>    (§9 #22).** The strong form was recorded on the day from one sentence and was never put back
>    as a table situation; put back, it held: **"in 2 player, you must go out with ALL SERIES, no
>    sets."** `EXPERT`. So a declared two-handed hand is runs and nothing else, and a hand holding
>    three of a kind at the end is not a winning hand however its runs fall.
>    🔥 **This is a rule about the partition, not about the cards.** Nothing stops a two-handed
>    player *holding* three of a kind; what is forbidden is **declaring** on a partition that uses
>    one as a meld. Those thirteen cards win if and only if they partition into runs alone.
>    ⚠️ **It also holds up §9 #25's reasoning**, which leant on sets being illegal head-to-head
>    when it argued the feeding ban might bite differently there. That argument was made against
>    the unconfirmed strong form; the strong form is confirmed.
> 2. ✅ **Two series may be counted as laid down — closed in rev 17 (§9 #23).** Thirteen cards
>    holding one six-card run may be declared as `3+3`, and that is **two series**. So the
>    three-handed requirement is satisfiable out of a single long run, purity included, and it is
>    far weaker than it reads.
>    🔥 **This is the answer that changes the shape of the win question**, because the
>    requirement becomes a property of **the partition chosen** rather than of the hand: an
>    evaluator may no longer return the first cover it finds, since a different cover of the same
>    thirteen may satisfy a requirement the first one misses.
> 3. ✅ **A surplus series need not be clean — closed in rev 17 (§9 #28).** Three-handed with
>    three runs, two clean ones are enough. Purity attaches to the required count and stops there,
>    which is what the second column above states.
>    ✅ **Rev 18 supplied the extreme case from the other end (§9 #29): an all-joker meld is a
>    legal series and is never a clean one**, so it can count as a surplus series and never as a
>    required one (§6.1). The two answers were reached independently and agree.

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

✅ **One question, and it exists because an answer made a combination the rule was never stated
about.** Rev 21 closed #30 (**an objection turns on the rank alone**) and #31 (**a turned-up
joker's partner pays ×3, same as any other permanent money card**) — and the second closed by its
answer turning out to be about a **different rule**: *"7 of diamonds, ace of spades, AND jokers
are always money cards."*

🔥 **Four consecutive revisions have each answered past the question asked**, and three of them
changed a rule nobody was asking about: rev 19 defined *a game* while answering how often a card
is claimed; rev 20 gave the ownership framing while answering about jokers, superseding an
`EXPERT` ruling one day old; rev 21 made **jokers permanent** and produced **a ×5 jackpot** while
answering how much one pays. ⚠️ **The pattern is worth naming: this game's rules are recalled as
wholes, not as answers.** Asking a narrow question and recording only the narrow answer has lost
material three times.

⚠️ **What is left is one row, and it does not block play — only a correct settlement.**

✅ **Nothing open blocks anything.** §5.1's specification — #16–#20, #25 and #27 — was written on
2026-08-20; rev 18 closed the win condition's last two, #22 and #29; and both survivors have a
recorded default that a shipped packet already runs on. ⚠️ **What is left unimplemented is not a
question but a decision already taken** — §5.1, §7.1.1 and rev 19's per-round seating are all
settled rules with no code behind them (§10 #13, #14, #16).

⚠️ **One of the two is worth more than its size.** #11's default sends a joker's money value
somewhere §5.1's #27 sends it elsewhere — two of the four jokers against all four — and the two
rules are deliberately scoped in opposite directions (§4.1). **The wrong guess is invisible: it
pays the wrong person a dollar in about one round in fourteen.**

| # | Question | § | Status | Blocks |
|---:|---|---|---|---|
| 32 | Does the **×5** need the turn-up to be the **7♦ and the A♠ specifically**, or does any two tripled values do — a 7♦ and a joker, two jokers of opposite colours? *(Raised by rev 21 making jokers permanent, which created combinations the rule was never stated about.)* | 4.1 | Unknown — recommend the **narrow** reading, which is what was said | Settlement |


> ✅ **#8 — closed in rev 18 by §9 #29, and the recommendation held.** The note below stands as
> written; what it recommended is what the experts ruled. **A meld may be nothing but jokers**
> (`EXPERT`), so no cap below a meld's own size exists (`DERIVED`) — see §6.1. 🔥 **P3 has been
> emitting all-joker melds since rev 10 on the strength of the reasoning below, and the reasoning
> was right**, which is the first time in this document that arguing from the generator's
> construction beat asking. It is also the only one of the three "confident wrong answers" this
> document warns about that came out the other way. ⚠️ **The answer arrived with a restriction
> attached**: an all-joker series is never clean, so it can never satisfy §7.1.1's required count.
>
> **#8 widened in rev 10, while building P3.** "Unlimited jokers" was recorded as a bound on
> substitution, but a window-based generator answers a sharper version of the question by
> construction: with four jokers in hand, `🃏🃏🃏` satisfies a three-card window with nothing
> real in it. **P3 takes the recommendation literally and emits all-joker melds** — a lower
> bound of one real card per meld is a rule nothing in §6.1 states, and inventing it would be
> the stricter, not the safer, choice. Four jokers exist in the shoe, so a hand can hold at
> most four; the effect is bounded and easy to reverse if Mya Lay says otherwise.

> ✅ **#12 and #13 — both closed in rev 19, and both defaults were right.** #12: *"yes, all 9s of
> hearts become money cards"* — designation is fixed at setup and does not move with the claimed
> card (§4.5). #13: *"yes, as long as you aren't violating any other discard rules"* (§5), which
> also settles the interaction with §5.1 that nobody had asked about. `EXPERT` both. **The note
> below stands as written**; it is kept because the reasoning that produced the right default is
> worth more than the default was.
>
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

> ✅ **#4, #5 and #14 — closed in rev 19, and one of the three went the other way.** #4: the
> turned-up cards **stay where they are**. #5: the claim is offered **every round**, because a
> game *is* a round — but ⚠️ **who approves it was not answered and #5 survives, narrowed.**
> 🔥 **#14 is the reversal: seats are re-randomised between rounds**, not held for the match, so
> `MatchEngine` is wrong and §10 #16 is the code. `EXPERT` throughout. **The note below stands as
> written and two thirds of its reasoning held.**
>
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

✅ **All of it is closed, and the paragraph that used to stand here is worth keeping as a marker of
what changed.** It said that everything above #16 had a safe default and was worth settling for
fidelity rather than for progress, while **§5.1's set — #16–#19, #25 and #27 — was not like that**,
because the rule was unimplemented and those questions were its specification. **They were answered
on 2026-08-20, and §5.1 is fully specified and still unimplemented — that is now a packet (P27),
not a question.**

> **Resolved in rev 21** — two, and 🔥 **the second answered past the question and changed §4.1
> twice over.**
> - **Does an objection turn on the exact card or the rank** (was #30): **rank alone.** `EXPERT`
>   The recommendation was right, and it matters: §5.1 is rank-only, so a player holding the 9♣ is
>   locked by a claimed 9♥ exactly as one holding the other 9♥ is. **The claim's predicate is
>   §5.1's predicate** and must not be written twice (§10 #18).
> - **Is a turned-up joker's partner really ×3** (was #31): **yes** — and the worry behind the
>   question dissolved, because **jokers are permanent money cards.** `EXPERT` The question was
>   *"×3 conserves the round's money on a 7♦ and creates it on a joker — is that intended?"*, and
>   the premise was wrong: a joker was never worth nothing. ✅ **Asking it flat and without
>   mentioning the 7♦ is what produced a correction instead of a confirmation.**
>
> 🔥 **Two rules arrived with that answer, neither of them asked for.**
> - **Jokers are permanent money cards** — *"7 of diamonds, ace of spades, AND jokers are always
>   money cards."* `EXPERT` **The permanent side-bet doubles, from 4 cards to 8**, and two
>   `DERIVED` arguments built on the old count are stale (§4.3, §4.4). It also makes rev 20's ×3
>   list stop looking arbitrary: it was never *special cards*, it is **the permanent money cards**.
> - **A ×5 jackpot** — if the two turned-up cards are the 7♦ and the A♠ **and one player owns both
>   partners**, they pay **×5 each** rather than ×3. `EXPERT` 🔥 **The first rule in this game where
>   a card's value depends on who holds what**, so a multiplier is a property of *(value,
>   ownership)* and `MoneyCardRegistry.Multiplier(Card)` cannot answer alone (§10 #17). ⚠️ **Its
>   reach is §9 #32**; the narrow reading is what is recorded.
>
> **Resolved in rev 20** — the last two money questions, from Mya Lay and Aung Aung. 🔥 **Both
> answers reached further than the questions did.**
> - **Who approves the claim** (was #5): **the player who goes before you in turn order** — the
>   last seat in the round, the one that discards to you — **and they may refuse only if they hold
>   that card.** `EXPERT` 🔥 **The reason is §5.1**: claiming is a public take, so it arms the
>   feeding ban against precisely that seat and would lock them into holding their copy. **This is
>   the first rule tying the money layer to the feeding ban**, and it independently confirms §9
>   #16 (the ban binds the seat above you) and §9 #17 (a public take arms it, the §4.5 claim
>   included) — both of which were *recommendations* when they were written and could not have
>   been guessed from this rule's existence. ⚠️ **P9 asks nobody** (§10 #18).
> - **Which jokers** (was #11): **the other joker of its own colour** — *"colour matters for
>   jokers."* `EXPERT` §4.2 applied unchanged, so `SameValueAs` already computes it and the
>   designation needs no joker special case. ⚠️ **It disagrees with §5.1's #27 on purpose** — a
>   joker taken in the open closes all four, a joker turned up pays on two.
>
> 🔥 **And an answer nobody asked for, which supersedes one given the day before.** The same reply
> gave the whole shape of a turned-up special card: **the shown card can never be owned, whether
> or not it is claimed, and its partner copy pays ×3** — not the **double** recorded in rev 19
> (§4.1). ⚠️ **Two `EXPERT` answers, one day apart, that do not agree**; the later and more
> specific is the rule and the earlier is struck. ✅ **The arithmetic backs the later one**: ×3
> makes a designation on a permanent money card worth exactly what an ordinary designation is
> worth, where ×2 would have made it worth less. ❌ **It withdraws P22's `DERIVED` note** and
> the shipped test that asserts it (§10 #17).
>
> **Resolved in rev 19** — nine, from Mya Lay and Aung Aung. ✅ **Eight of the nine confirmed a
> standing default; one reversed it.**
> - **Why 7♦ and A♠** (was #10): **"tradition."** `EXPERT` 🔥 Recorded as *likely unrecoverable*
>   from rev 1 to rev 18 and it was not — **it closes by confirming there is nothing behind it**,
>   which stops a future session hunting a pattern that does not exist.
> - **Both copies, both decks** (was #24a): **both.** `EXPERT` `CODE` `Probable` → Settled.
> - **A turned-up 7♦** (was #24b): **a double money card, *"worth double."*** `EXPERT` This also
>   promotes §4.3's `DERIVED` guess that a double counts as **two** at settlement.
> - **Can it stack past a double** (was #24c): ⚠️ **hedged — *"maybe… i don't think that's
>   mechanically possible/relevant."*** `PLAYER`, and it stays flagged. 🔥 **It is *possible* — both
>   7♦s turned up together, about one round in 5,800 — and it is *irrelevant*, for a reason nobody
>   stated**: in that case both copies are lying on the table as designators, so the value is
>   owned by nobody and pays nothing whatever it stacks to (§4.1, `DERIVED`).
> - **A turned-up joker** (was #11, first half): **jokers become money cards.** `EXPERT` It
>   eliminates the reading in which a joker cannot be a money card and is set aside. ⚠️ **Which
>   jokers is still open** and #11 survives, narrowed.
> - **Does a claimed money card's value still pay** (was #12): **yes — "all 9s of hearts become
>   money cards."** `EXPERT` Designation is fixed at setup and does not travel with the card. P7's
>   default confirmed.
> - **How often the claim is offered** (was #5, first half): **every round** — 🔥 **and the answer
>   arrived as a definition**: *"the beginning of the game, if game means from the time the money
>   card is turned up to the time a player goes out."* **A game is a round** (§3). P9's default
>   confirmed. ⚠️ **Who approves it was not answered** and #5 survives, narrowed.
> - **The turned-up cards and the reshuffle** (was #4): **they stay where they are.** `EXPERT`
>   The sweep takes the discard piles and nothing else. P9's default confirmed.
> - **Throwing back what you just took** (was #13): **yes — *"as long as you aren't violating any
>   other discard rules."*** `EXPERT` P7's default confirmed. 🔥 **The rider is the find**: it makes
>   the feeding ban outrank the throw-back, so the just-taken card is filtered like any other and
>   needs no rule of its own (§5).
> - **What moves between rounds** (was #14): 🔥 **the seating — every round.** `EXPERT` *"You
>   randomize seats between games, using the game definition from #5."* ⚠️ **This reverses P9,
>   which keeps one seating for a whole match**, and it is the only answer of the nine that
>   changes what the engine must do (§10 #16). ✅ **It moves no published measurement**: every
>   experiment in `BurmesePoker.Sim` runs `RoundsPerGame = 1`, so no measured game has a second
>   round to re-seat for.
> - **Does anything else change with the player count** (was #26): ⚠️ **hedged — *"not that I know
>   of."*** `EXPERT`, **Probable rather than Settled**, and the hedge is quoted because §7.1.1 is
>   proof the question is real. Hand size, decks, money cards and stakes are unchanged at every
>   table size; the win condition is the one thing that moves.
>
> **Resolved in rev 18** — the two that blocked a correct win condition, from Mya Lay and Aung
> Aung. ✅ **§7.1.1 is now fully specified, and §10 #14 has nothing open against it.**
> - **At two players, are sets illegal or merely not required** (was #22): **illegal.** *"In 2
>   player, you must go out with ALL SERIES, no sets."* `EXPERT` 🔥 **This confirms a rule that
>   was already written down** — rev 17 recorded the strong form because it was what was said,
>   and flagged that it had never been put back as a table situation. Put back, it held. **No
>   rule changes; a tag does**, from a recording to a confirmation.
> - **Does an all-joker meld count as a series** (was #29): **yes — but never a clean one.**
>   `EXPERT` So it can satisfy a **surplus** series and never a required one (§6.1, §7.1.1). ✅
>   **This also closes #8**, whose second half asked exactly this; the maximum-jokers half falls
>   out as `DERIVED` and is the one thing here nobody was asked.
>
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
    deadlock. ✅ **Rev 19 confirmed the filter is the whole of it**: a player may throw back the
    card they just took *"as long as you aren't violating any other discard rules"* (§9 #13), so
    the just-taken card is filtered like any other and needs no special case. ⚠️ **The banned-rank test is rank alone** — neither `==` nor `SameValueAs`, both of
    which are narrower — so it needs a predicate `Card` does not have. ✅ **§9 #16–#19, #25 and #27 are
    all answered** (2026-08-20), so nothing blocks this: it is **P27**. ✅ **And §9 #30 makes the
    claim's objection test the same rank-only predicate** — write it once (§10 #18).
    ✅ **Implemented 2026-08-21 (P27), and this ruling is discharged.** `FeedingBan` holds the two
    rank sets per seat; `TurnContext.LegalDiscards` is the whole of the choice a turn presents and
    is never empty by construction; `Card.SameRankAs` is the rank-only predicate, and it is the one
    §10 #18 must reuse rather than re-write. Every rung is filtered because `CoverScore.Ranking`
    takes the context — **including the runner-up a difficulty level throws as its mistake** — and
    both front ends draw a closed card as something that is not a control
    (`CardDisplayState.Unthrowable`). ⚠️ **What this does not reach: the ban changes play, so every
    figure in `docs/STRATEGY.md` is now measured under a third rule the game no longer plays by
    (P29).**
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
    ✅ **Nothing in §9 is open against it any more** — rev 18 closed #22 and #29, the last two.
    ⚠️ **Two-handed is a constraint on the partition, not a filter on the hand** (§9 #22): sets
    are illegal *as melds*, so the thirteen must partition into runs alone, and an evaluator that
    finds any cover has not answered the question. ⚠️ **And an all-joker series can never
    discharge the required count** (§9 #29), so the purity test cannot be applied to the melds a
    cover happens to return — it has to be part of choosing the cover.
    ✅ **Implemented 2026-08-21 (P25), and this ruling is discharged.** `TableRules.For(players)`
    is the §7.1.1 table as data and the only place it is written down; `HandEvaluator` takes it as
    a parameter and **has no parameterless overload**, so no caller can ask the five-handed
    question by accident. The search carries what is still owing **along** the partition rather
    than auditing a finished cover, sets are pruned out of the candidates two-handed, and
    `Meld.IsClean` is *a run with no joker in it* — which makes the all-joker series fail the
    purity test with no special case, since every one of its slots is a substitute.
    ⚠️ **What this does not reach: `RoundEngine.MinimumPlayers` is still 4** (#7), so the two- and
    three-handed rules above are correct, tested, and unreachable from a dealt game.
15. **The discards are public** (§5). `TurnContext` shows a seat only the single discard offered
    to it, which is **less** than the rules allow and less than the browser already draws for a
    watcher. ⚠️ **P20's card-counting null was measured under the narrower reading** and is
    conditional on it; widening this is the one change that could reopen a published result.

16. 🔥 **Seating is re-randomised every round** (§3 step 2, §9 #14) — **the only rule this
    document has ever recorded that the engine actively contradicts rather than merely lacks.**
    `MatchEngine` randomises once at setup and keeps that seating for the whole match, which is
    what P9 chose when §9 #14 was open and the recommendation was *nothing moves*. It is one
    place, and the round engine already takes its seating as given. ⚠️ **It moves no published
    figure** — every experiment in `BurmesePoker.Sim` runs `RoundsPerGame = 1`, so no measured
    game has a second round to re-seat for, and `--seating rotated`/`balanced` assign strategies
    to seats **within** a game and are untouched. ⚠️ **The two front ends are where it shows**:
    the console and the browser play round after round with the banks carrying over (§7.2), so a
    player's neighbours currently never change and should change every deal. 🔥 **It collides with
    a UX decision, not just a loop** — P13.5 puts *you at the front whichever seat you were dealt*,
    so re-seating every round means the table visibly rearranges itself around a fixed viewer, and
    `TableRing` has never been asked to do that between rounds. ⚠️ **And it interacts with §5.1**:
    the feeding ban is a property of an ordered pair of adjacent seats, and re-seating rebuilds
    every pair each round — which is harmless, because the ban is per round in any case, but it
    means the banned-rank sets are keyed to a seating that no longer exists the moment the round
    ends.

17. ✅ **Discharged 2026-08-21 (P26). The money layer is what §4 says it is.**
    `MoneyCardRegistry.Permanent` holds **three values and eight cards**; `Multiplier(Card)`
    returns 0, 1 or **3**; and the ×5 is `Multiplier(card, owner, MoneyOwnership)` under a
    configuration `Settlement` reads from `CardOwnership` **once a round**, never once a card.
    ✅ **The design decision held**: nothing is stored on a card, and the widened signature is
    documented as the inputs widening rather than the principle moving.
    ⚠️ **The ×5 is fenced to the 7♦/A♠ pair** (§9 #32), and `TwoTripledJokersInOneHandAreNotAJackpot`
    is the test that makes widening it visible.
    ✅ **The withdrawn `DERIVED` note's replacement was a prediction and it came out right**:
    `ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe` now asserts an
    **equality** — a designation on the 7♦ and one on an ordinary card leave exactly the same
    money loose in the shoe — and it passes. ⚠️ **What is *not* discharged: `docs/STRATEGY.md`
    §10's money sweep is still measured under the struck rule** and every rung's decision is
    unchanged, so no published strategy figure has been re-run. **That is P29.**
    ⚠️ **And `MoneyOdds` does not price the ×5** — it sums the value-only multiplier over the
    shoe, which understates a round in 1,444 in the conservative direction; stated in the code
    beside the two approximations that were already there.
    *(What it was, before P26.)* 🔥 **The money layer is three rules away from what is built, and
    the third changes the shape of the function** (§4.1, §4.3, §4.4).
    **(a) Jokers are permanent money cards** — `MoneyCardRegistry.Permanent` holds two values and
    must hold three; the permanent side-bet goes from **4 cards to 8**.
    **(b) A designation landing on a permanent money card pays ×3** — `Multiplier` is
    `(permanent ? 1 : 0) + (designated ? 1 : 0)`, caps at **2**, and must return **3**.
    **(c) ⚠️ If the turn-up is the 7♦ and the A♠ and one player owns both partners, both pay ×5** —
    **and this cannot be computed from the designators at all.** It needs the round's ownership, so
    `Multiplier(Card)` gains a parameter and `Settlement` can no longer ask the registry about a
    card in isolation. ✅ **The design decision survives**: money status is still *computed, never
    stored*; the inputs widen, the principle does not move. ⚠️ Needs §9 #32 before the predicate is
    generalised past the 7♦/A♠ pair. ✅ **Designation itself is already right** — `SameValueAs` matches a joker by colour,
    which is exactly §9 #11's answer. ⚠️ **This is the first rules change that invalidates a
    published measurement.** `MoneyOdds.PerBlindDraw` prices a blind draw from `Multiplier`;
    `prospector` is the one rung whose decision reads the money (P22); and `docs/STRATEGY.md` §10
    — the money sweep, twelve rows of `measurements.csv` — was measured under the struck rule.
    ⚠️ **`ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe` asserts the
    withdrawn direction and will go red**, which is the good case: the claim was written down as
    an assertion rather than as prose, so the rules change cannot pass silently. 🔥 **Its last
    assertion should invert to an equality** — a designation on the 7♦ and a designation on an
    ordinary card leave the same money loose in the shoe — **and that is a prediction, not a
    measurement; it has not been run.**
18. 🔥 **Claiming the turned-up money card requires the upstream player's permission** (§4.5).
    `RoundEngine` offers the claim and asks nobody. This needs **a third decision from an agent** —
    the first new one since the engine was built: not *what do I take* or *what do I throw* but
    *do I object*. ⚠️ **It is asked of a seat that is not on turn**, which no prompt in
    `BurmesePoker.Server` currently does — every `SeatPrompt` goes to the player whose turn it is
    — so the hosted table needs an out-of-turn question and the browser needs somewhere to put it.
    ⚠️ **And the answer is a disclosure**: only a holder may object, so an objection tells the
    table that seat holds that rank. **That is a `TableEvent` and it is public by construction**,
    which makes it the first thing a player reveals by choice rather than by discarding — a real
    decision for `ConcealmentTests` to pin down rather than a UI detail. ✅ **The predicate is
    settled — §9 #30 is *rank alone***, which is §5.1's own predicate, so the objection test is
    the same `HoldsRank` the ban needs and the two must not be written twice.

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
