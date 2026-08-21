# Playing

A short guide to sitting down alone against the computer. **`RULES.md` is the rules
authority** — this only explains the game as far as you need it to make the four decisions you
are asked for, and points there for everything else.

**There are two ways to sit down, and they ask you the same four things.** Most of this guide is
about the console, which came first; the browser is at the end and is the nicer of the two if
you have one open.

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
| **How hard should the computer be?** | Four settings, **hardest first**: *expert*, *hard*, *medium*, *easy*. They are the same player throughout — the best one there is — differing only in how often it slips and throws the wrong one of two good cards. At a table of all four, *expert* wins about **36%** of rounds and *easy* about **14%**, with *hard* and *medium* spread between them five to nine points apart (`STRATEGY.md` §9). Pressing return takes the top of the list, which is *expert*. |
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
| `--journal <file>` | Writes the whole match down — every decision, yours included — so it can be played back later. See below. |
| `--fidelity <thin\|rich>` | How much of each decision to write down. `thin` (the default) records what was decided; `rich` also records the hand it was decided from. |
| `--help` | The same table, shorter. |

### Keeping a match

`--seed` replays a match **only against the version of the game that played it**. Change how the
bots think and the same seed deals the same cards to different players; and a seed cannot replay
*you* at all — it reproduces the deal and the computer, not what you decided.

`--journal` writes down the decisions themselves, which fixes both:

```bash
dotnet run --project BurmesePoker.Console -- --journal last-night.jsonl
dotnet run -c Release --project BurmesePoker.Sim -- replay last-night.jsonl
```

The replay reports the rounds exactly as they went — same winners, same money, same turn counts
— and it will keep doing so however the game changes underneath it. It is one JSON object a
line, so it is also readable with anything that reads text. **Use it for the hand you want to
show somebody**, and `--fidelity rich` when you want to be able to see what everyone was holding
when they threw what they threw.

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
| `($$$)` | A triple — a designation that landed on a card that always pays, so it pays three times. |
| `★` | **You own it.** The deck gave you this card, so it pays *you*. |
| `closed` | **You may not throw it this turn.** The player after you took that rank in the open, so it is not among the cards you are offered (`RULES.md` §5.1). |

**A money card with no star pays somebody else**, and you are holding it for them.

**A card marked `closed` is not a choice.** Taking a card in the open announces what you are
collecting, and the seat that discards to you may not hand you another one — so once the player
*after* you takes, say, a Queen where everybody can see it, **every** Queen in your hand goes
closed and stays closed until they throw a Queen back. Suit does not come into it. Two things get
you out of it: they throw that rank away, which re-opens it for the rest of the round; or you are
going out on it, because the ban never stands between you and a win. And if *every* card you hold
is closed, the ban gives way for that turn — you always discard.

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

**What separates a thinking bot from a simple one is only the tie-break** — and it is worth
half again as many wins: **29.8% of rounds against 20.3%**, at four seats over every way of
seating the two (`docs/strategy/measurements.csv`, `headline.balanced.*`). Early in a round
almost every discard costs you nothing, so the count alone cannot choose. It then keeps:

- cards with **partners** — another suit of the same rank, or a neighbour in the same suit;
- **jokers over everything**, since a joker fits anywhere.

**And the one thing that beats that is looking one card further ahead.** Where two discards
leave the hand equally melded, the strongest player the computer has keeps the thirteen that
**more of the pack would improve** — it counts, for each card still out there, whether drawing
it would help. That is worth **3.1 ± 1.0 points** of win rate over the partner rule alone, and
it is the only idea in this project that has ever beaten it. Three others were tried and
measured nothing (`docs/STRATEGY.md`).

**What separates the four difficulty settings is only how often it gets the choice wrong.**
Every setting is that same strongest player; *easy* throws the wrong one of two good cards nine
times in ten, *medium* seven, *hard* two in five, and *expert* never. That is why a weaker
setting still plays a game you recognise instead of a stranger's — it plays the right idea and
slips, which is what a weaker person does.

### Which one should you play?

**Start at *hard* and move.** The four are spaced by measurement rather than by taste, about
**seven and a half points of win rate apart** at a table of all four — far enough that a step is
something you feel within a session, close enough that *"a bit easier"* is a real request. At a
four-handed table a fair share is 25% of the rounds, and against three of the same setting these
win **13.8% / 21.7% / 28.4% / 36.1%** of them, weakest first.

- ***easy*** feeds you cards. It knows what to keep and throws the wrong one nearly every turn,
  so the discard beside you is often the one you wanted.
- ***medium*** is the one to sit at while the melds are still unfamiliar. It gets the close
  choices wrong more often than not, and you will win more rounds than it does.
- ***hard*** is a real game. It slips about twice in five on the cards it is choosing between,
  which is roughly what a good player does when they are not concentrating.
- ***expert*** never slips at all, and beats the other three every way this has been measured.
  It is what a hint is asked of, and what takes over your seat if you stop answering — a hint
  that got worse as you lowered the difficulty would be absurd.

⚠️ **The setting is the *computer's*, not yours.** Nothing about your own hand, your hint or the
deal changes with it. And **a mixed table is a browser thing** — the console asks once for the
whole table; the lobby's *mixed* checkbox gives each computer seat a different setting, and names
each seat for how it plays so you can see who the easy one is.

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

## The same game in a browser

```bash
dotnet run --project BurmesePoker.Web
```

Then open the address it prints (`http://localhost:5188` by default). **That is the lobby, not a
table** — type a name, press **Sit down**, and you are at a table with the computer in the other
seats, at the same pace the console uses.

**It asks the same four questions**, in the same order. It does not say them the same way.

### The lobby

Each card in the lobby is a table: what it is called, how many seats it has and how many of them
are for people, and what it is doing. **Sit down** takes a seat; **Watch this table** takes none.
*Open another table* opens a second one — and that form is where you say **how hard the computer
is**, from the same four settings the console offers, and whether you want a **mixed table**:
tick it and each computer seat plays at a different setting instead of all of them playing at
one. Each open table says what its computer seats are playing, and **each computer seat is named
for how it plays** — *Mya Lay (expert)*, *Cobra (hard)* — so you can see who the easy one is. You
can be at both tables in two windows.

⚠️ **A table waits for everybody it was opened for.** The one the site starts with has **one**
seat for a person, so it deals as soon as you sit down; start the site with `--people 2` and it
waits for a second of you — open another browser window and sit down again. A table with
`--people 0` is a room full of computer players and deals as soon as anybody watches.

**Your name is how you get your seat back.** If your browser reloads, or you close the tab and
come back, **sitting down under the same name takes that seat back off whoever was holding it** —
which, a moment after a reload, is you. ⚠️ **It is a name and not a password**: anybody who types
your name takes your seat. That is fine for a game with friends and would not be for anything
else.

⚠️ **The form is there even when the table is full**, because the person most likely to need it
is the one who has just reloaded and is looking at their own seat with their own ghost in it.

### The table

**You are at the front of it, whichever seat you were dealt** — the others go clockwise from your
left, in the order they play. Four, five and six seats all fit; five leaves the far end of the
table empty and six fills it.

**The middle of the felt is what everybody shares**: the deck with how many cards are left on it,
the money cards turned up this round, and **the one discard you may take**. If the middle offers
you a card, the button at the bottom offers you the same card — they are the same fact.

**Each seat around you is small on purpose**: a name, a bank, and the top of its discard pile.
That is everything the rules let you know about somebody else (RULES.md §7.1).

| On a seat | Means |
|---|---|
| **▶** | The table is waiting on them. **It is their turn**, not "they moved last". |
| **✓** | They have laid all thirteen down. |
| **♛** | They won the round. |
| **▾** | The top of their discard pile. |
| **⟳** | **The computer is playing this seat** — they ran out of time, or they left. It clears when they play a turn themselves. |

### Your hand, and the bar under it

Your hand is the bottom third and the cards are big, because they are the only thing on the page
you handle. **A card you can throw is a button** — press it and your turn ends.

**The small number on a card is what throwing it would cost you**: `−3` means throwing it gives
up three melded cards. **A card with no number costs nothing** — those are the ones worth
throwing, and they are the ones grouped under `loose`. `←` is the card the computer would throw;
`($)` and `($$$)` mark money cards, and **`★` marks the ones that pay *you***.

⚠️ **`★` is the one worth understanding.** A money card pays **whoever the deck gave it to**, and
only the deck — being dealt one or drawing one blind makes it yours, taking one from a discard or
off the table never does (RULES.md §4.4). Ownership never moves and never lapses, so **a starred
card goes on paying you after you have thrown it away**, and a money card *without* a star is one
you are merely holding for somebody else. That is why throwing a money card costs you nothing.

Under the hand is **one bar with the question on it**, at most two buttons, and a **"why?"** you
can open for the rule behind it.

### The rest of it

- **Everything else is a press away rather than on the felt.** *What the markers mean* is the
  whole legend, once. *About this table* has the seed, and `--seed` deals the same table again.
- **The round log is a panel you open.** Closed, it shows the last thing that happened; open, it
  scrolls back through the match. ⚠️ **It is still read aloud while it is closed** — the panel is
  folded up, not switched off.
- **The whole turn works from the keyboard.** When it becomes your turn the focus lands on the
  first control of the question, `Tab` moves between them, and `Enter` presses one. Choosing a
  discard means tabbing to a card and pressing it.
- **Your hand stays on screen while the others play.** It is the hand you kept, not a picture of
  an old one.
- **Nothing is hidden from you that the console shows you**, and nothing extra is shown. Your
  hand reaches the page because your seat was asked a question about it; nobody else's ever does
  — and a watcher is shown no hand at all, which the page says out loud.
- **If your connection drops, the table is still there.** It says so, stops you pressing things
  that would go nowhere, and offers to reload.


Useful options — all of them plain configuration, so `--` then the flag:

| Option | What it does |
|---|---|
| `--people 1` | How many of the seats are for people. `0` is a table of computer players you can only watch. |
| `--table "The kitchen"` | What the table the site opens is called in the lobby. |
| `--name Nick` | What the lobby's form suggests calling you. |
| `--seed 20260819` | The same cards again — the browser's `--seed`, same as the console's. |
| `--hints false` | Start with the computer's suggestions hidden. There is a checkbox for it too. |
| `--pace 400` | Milliseconds a computer seat pauses before it moves. |
| `--between 5` | Seconds between the settlement and the next deal. |
| `--patience 120` | Seconds a question waits for you before the computer plays your seat. |
| `--seats 5` | Four to six players (RULES.md §2.1). |
| `--difficulty medium` | How hard the computer is at the table the site opens: `expert`, `hard`, `medium` or `easy`. A name nobody knows opens the table on `expert` rather than refusing to start. ⚠️ These are difficulty settings and not the *rungs* the simulator ranks — `--difficulty greedy` is a name this does not know. |
| `--mixed true` | Give each computer seat a different setting instead of all of them the same. ⚠️ It takes a value, like `--hints`: a bare `--mixed` is silently ignored. |

⚠️ **If you walk away, the computer plays your seat** — the log says so and so does your seat, with
a **⟳**. Come back and the next question is yours again.

⚠️ **Landing on a table mid-turn puts the focus at the top of the page**, not on the question, because
arriving at a page is arriving at a page. Four presses of `Tab` reach the buttons.

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
