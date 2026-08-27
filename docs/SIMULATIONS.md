# How this project measures things

There is no published ruleset for this game, and there is no published book on how to play it
well either. So the project measures: it plays the game against itself tens of thousands of
times and counts. This document explains **how that machinery works and why its numbers can be
trusted** — the instrument, the way `RULEBOOK.md` teaches the rules and `HOW-TO-PLAY-WELL.md`
teaches the decisions.

It is written for a curious person, not for a statistician. It carries **no figures of its own**
on purpose: every number this project has actually measured lives in `STRATEGY.md` (the full
report, with intervals) and `HOW-TO-PLAY-WELL.md` (the same numbers, chosen and phrased for a
player). This file explains what a *cell*, a *margin* and a *verdict* mean, so that when you
read one there you know what you are looking at. If you want the answers, go there; if you want
to know whether to believe them, read on.

---

## What a run is

A **run** is one command that deals a large number of complete games, plays every one of them to
its end, and writes down what happened. Three properties make it an instrument rather than a
demonstration.

- **It is seeded.** A run takes a starting number — the *seed* — and everything random after it
  is determined by that number: the shuffle of every shoe, every blind draw, every arbitrary
  choice a weak player makes. Give the same command the same seed and you get the same games,
  card for card. So a measurement is not a story about one lucky evening; it is a thing anyone
  can reproduce exactly.
- **It is parallel.** The games in a run do not depend on each other, so they are dealt across
  every core the machine has. That is the whole reason tens of thousands of games is a coffee
  break rather than a weekend — and it is also why the wall-clock time a run takes is a fact
  about the machine, not about the experiment.
- **It is replayable.** A run can be asked to keep a full record of every decision every player
  made — see the next section — and that record can be played back to reproduce the same result
  without the original seed or code.

The simulator is its own project, `BurmesePoker.Sim`. The simplest run just deals games between
the default field of players and prints a report:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- --games 2000
```

Everything else in this document is a *shape* of run — a way of arranging who plays whom, and
what gets counted — layered on that same seeded, parallel, replayable core.

## A seed, and a journal

There are two ways to preserve a run, and the difference matters.

A **seed** is a *pointer*. It is tiny — a single number — and it reproduces the run only against
the same build of the code. Change how a player decides, and the same seed now deals different
games, because the seed was never the games; it was the recipe for regenerating them.

A **journal** is the *artifact*. Asked for one, a run writes down every card dealt and every
decision taken as it goes, as plain lines of text. That record stands on its own: it can be
replayed to reproduce the exact games even after the code has moved on, because it does not
regenerate anything — it just re-narrates what already happened.

```bash
dotnet run -c Release --project BurmesePoker.Sim -- --games 100 --journal run.jsonl
dotnet run -c Release --project BurmesePoker.Sim -- replay run.jsonl
```

The distinction is load-bearing. When this project wants to prove that a change to the *plumbing*
did not change the *game* — a refactor, a faster search, a new way of drawing the table — it runs
the same seed before and after and checks that the journal and the report come out byte for byte
identical. A seed proves reproducibility; a journal proves the game itself did not move.

## Why the game is the trial, not the seat

Every measurement here is a rate: how often something happens, out of how many chances. To turn a
count into an interval — a *plus-or-minus* around the estimate — you have to say what counts as
one independent chance. **The chance is one whole game, not one seat and not one turn.**

The reason is that the seats of a single game are not independent of each other. They are dealt
from the *same shoe*, so one seat holding a card is exactly the reason another seat does not.
Treat the four or five seats of a game as four or five separate observations and you are claiming
far more independent evidence than you have — which makes every interval far too narrow. A margin
that is really inside the noise then looks like a discovery. This project made that mistake once;
counting the game as the trial is the fix, and it is the first rule everything else rests on.

## Why a win rate is the totals divided, not the average of rates

There is a subtle trap in how you combine games, and it is worth seeing because the wrong way
looks perfectly reasonable.

In a run where every kind of player is shuffled across the seats, a given strategy sits in one
seat in some games and in several seats in others. There are two ways to compute its win rate.
You could work out its rate *within each game* and then average those per-game rates; or you
could add up every win it took across the whole run and divide by every seat it ever held. These
are **not the same number**.

Averaging the per-game rates gives every game an equal vote regardless of how many seats the
strategy held in it — which quietly over-weights the games where it held the fewest seats. For a
strong strategy those are exactly the games it does best in, so the average-of-rates flatters it.
The honest quantity is the second one: the totals divided. Every win rate this project publishes
is computed that way, and the gap between the two methods is not academic — it has been measured
and it is about the size of a real effect.

## Pairing: why it can widen and why it can narrow

When you compare two strategies, you can compute the margin between them in two ways, and the
difference is the single most misunderstood thing in this document.

The naive way treats the two strategies' results as unrelated and adds up their separate
uncertainties. The **paired** way computes the margin *game by game* — this player's result minus
that player's result, in the very same game — and takes the uncertainty of that difference
directly. Pairing does not always tighten the interval. It measures the correlation that is
actually there, and here that correlation points **both ways**.

- **Within one table**, the two strategies are sitting in the same game, and in this game exactly
  one seat ever declares and wins. So when one of them wins, the other by definition did not:
  their results are strongly *opposed*. Two opposed series have a difference that varies *more*
  than either alone, so the honest paired interval is **wider** — for two evenly matched thinking
  players, wider by the square root of two, which turns out to hold to several digits. The
  dangerous consequence: the naive add-them-up formula is not merely imperfect here, it is
  *anti*-conservative — it reports a margin as more certain than it is, which is how a
  within-table comparison can manufacture a finding.
- **Across two different tables** dealt from the *same shoes*, the story reverses. One strategy
  plays two separate games built from the same shuffles; good luck in the shared shoe helps it in
  both, so the two results move *together*. Pairing then cancels that shared luck and the interval
  **narrows** — sometimes dramatically. This is the classic technique of common random numbers,
  and it is why comparing two nearly identical players from shared shoes is so much sharper than
  comparing two unrelated ones.

The upshot for a reader: an interval in `STRATEGY.md` is not just "how many games did they run".
It already accounts for whether the two players were fighting over the same win or riding the
same luck, and the direction of that correction is not something you can guess from the sample
size alone.

## Why many comparisons need correcting, and what "survives" buys

Rank ten players against each other and you are not making one comparison, you are making
dozens — every pair, both ways round. Each comparison uses an interval that is allowed to be
wrong a small fraction of the time by chance. Make enough of them and it becomes *more likely
than not* that at least one pair clears zero on luck alone. A round-robin, left uncorrected,
manufactures findings.

So every margin in the ranking carries two verdicts. The **raw** one asks whether that pair on
its own cleared the bar. The corrected one — by the **Holm–Bonferroni** method — raises the bar
to account for how many comparisons were made at once, and asks whether the pair still clears it.
A margin that is raw-separated but does *not* survive the correction is **not** reported as a
finding; it is flagged as exactly what it is, a result that might be the family being large. When
`STRATEGY.md` says a margin *survives Holm*, that is the strong claim; when it says *raw only*,
that is the honest hedge.

There is a cost to this that shapes the whole project: the family grows with the square of the
field, so **every new player makes every existing verdict harder to reach**. Adding a rung is not
free — it taxes every finding already on the board — which is a real part of why a new way of
playing has to earn its place in the ranking rather than merely being tried.

## The null cell: the harness checking itself

Before trusting any margin the apparatus reports, you want to know the apparatus reports *zero*
when the truth is zero. That is what the **null cell** is for. It plays one strategy against an
exact copy of itself under a different name. The two are the same player in every respect, so the
true margin between them is exactly nothing — and any gap the run reports is therefore not a fact
about the game but a flaw in the instrument: a seat that gets to act first, a seating that is not
evenly balanced, the seat-counting mistake from earlier.

The run checks that this cell comes back at a fair share of the table, within its interval, and
the suite command **fails outright** if it ever stops doing so. It is deliberately played by
whichever strategy was built most recently — including the ones that carry memory across games or
change their play near the end of a round, because those are exactly the kinds of player that
*could* make a result depend on the order the harness happened to schedule them in. It keeps not
doing so, and the null cell is where the project says that at scale rather than merely asserting
it.

---

## A tour of the experiment shapes

Each shape below answers one question well and, just as importantly, refuses to answer a
neighbouring one. Knowing which is which is most of what it takes to read `STRATEGY.md`.

### Head to head — the ranking

Every pair of players meets at every seating where both are at the table, over many thousands of
games, and the **margin** is the row player's win rate less the column player's, computed game by
game. This is the fairest strategy-versus-strategy statement the project has, because it weights
every opponent equally and pairs within the game. A single figure in that matrix is a **cell**;
its plus-or-minus is the paired interval; the mark beside it is the Holm verdict.

- **It answers:** is this way of playing better than that one, and by how much, all else equal?
- **It cannot answer:** how any of them does in a *mixed* crowd — because it only ever sits the
  two of them together with themselves.

```bash
dotnet run -c Release --project BurmesePoker.Sim -- tournament --games 2000
```

### Everybody at one table — the crossed free-for-all

Instead of pairs, every player is shuffled across every seat of one table, in every combination,
and each one's plain win rate is reported. This is the *field* result.

- **It answers:** if you dropped all of these players at one table, who comes out ahead?
- **It cannot answer:** a clean strategy-versus-strategy question — because a win rate here
  depends on *who else is in the field*, so adding or removing a player moves everyone's number
  without anyone changing how they play. This is the column to distrust when the field changes;
  the head-to-head matrix is the column that survives it.

### The neighbour experiment — does the seat before you matter?

This shape isolates one effect with a *control arm*. A focal player is measured with a weaker or
stronger neighbour seated immediately **before** it — and, on the same shoes, again with that
neighbour seated immediately **after** it. Two runs identical but for which side the neighbour
sits on, so any difference between them is the seating, and nothing else.

- **It answers:** is who sits *where* around you worth anything, separately from who is at the
  table at all?
- **It cannot answer:** it deliberately holds everything but position fixed, so it says nothing
  about deeper table composition; that is what the control arm buys.

```bash
dotnet run -c Release --project BurmesePoker.Sim -- neighbours --games 2000
```

### The money sweep — should you chase the side bet?

One player in this game weighs a decision the others cannot see: whether a card is worth more than
the ownership a blind draw would confer. That trade depends on the **stakes**, which are fixed at
the start of a game and not by the rules. So this player is not measured once but *swept* across
several stakes ratios, and judged on **dollars a round** rather than on how often it wins — because
its whole idea is trading rounds for money, and a win-rate ranking would misjudge it by
construction.

- **It answers:** at these stakes, does drawing for the money pay, in the currency the decision
  is actually about?
- **It cannot answer:** anything at a *single* stakes — the point of the sweep is that the answer
  is a function of the ratio, so one row of it would be a trap.

```bash
dotnet run -c Release --project BurmesePoker.Sim -- money --games 8000
```

### The dial's calibration — spacing the difficulty levels

The difficulty settings a person plays against are not different strategies; they are the
*strongest* strategy told to make a mistake some fraction of the time. Calibrating them is its own
shape of run: the same player at a ladder of mistake rates, compared only to its neighbours on the
ladder — because a difficulty dial only claims that each step is harder than the one below it, not
that every step beats every other.

- **It answers:** are the four difficulty levels actually spaced apart, and by how much?
- **It cannot answer:** whether the underlying player is any good — that is the ranking's job; the
  dial takes the best rung as given and only spaces the handicaps.

```bash
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --games 8000
```

### The whole set at once

One command regenerates every figure `STRATEGY.md` quotes — the ranking, the field, the seating,
the null cell, the dial, the money sweep and the mechanism rates behind each finding — into the
one file the document is generated from and never transcribes:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- suite --games 8000
```

That run is long — hours, not minutes, and how many hours is a property of the machine — because
the head-to-head family grows with the square of the field and the crossed table grows faster
still. Budget it with `bench`, which prices a single player's decision and a single round before
you commit to a suite:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- bench
```

---

## Where the figures live

This document has no numbers in it, and that is a rule rather than an accident: a figure copied
into a second place is a figure that goes stale when the first place is regenerated. So every
measured number has exactly one home.

| You want | Read |
|---|---|
| The full report — every ranking, interval and verdict | `STRATEGY.md`, generated from `docs/strategy/measurements.csv` |
| The same numbers chosen and phrased for a player | `HOW-TO-PLAY-WELL.md` |
| What each player actually decides differently | `STRATEGY.md` §2, the ladder |
| The rules being measured against | `RULES.md` |

If a number here would help, it belongs in one of those and this file should point at it — which
is the same discipline that keeps the whole documentation set honest.
