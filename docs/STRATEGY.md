# Strategy — what actually works

**The standing answer to *which way of playing is better, by how much, and how sure are we*.**

Companion to `RULES.md`, and held to the same discipline. `RULES.md` tags every rule with its
provenance because a rule without a source cannot be re-examined; **a measurement without its
origin is worse, because it looks like a fact**. So every number below is *generated* rather than
transcribed, out of `docs/strategy/measurements.csv`, which is written by one command:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- suite --strategies random,simple,greedy,cautious --games 8000 --seed 20260819
```

Last generated **2026-08-19** (BUILD-PLAN P19). That run played **~120,000 games in 34 minutes**
— the ladder ranked, the difficulty dial calibrated beside it, and P12's headline under both
seatings. ⚠️ **P17's run of the same seed wrote every ladder figure identically**, which is the
only reason a document may quote a simulation at all; §10 is what P19 added.

The fuller report the tables below are drawn from comes from the same seed:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies random,simple,greedy,cautious --games 8000 --seed 20260819
```

> **64,248 games in 767.7 s, 31 abandoned at the turn cap.**

---

## 1. How to read a number here

**Five rules, and every one of them exists because getting it wrong produced a wrong answer in
this project at least once.**

1. 🔥 **The trial is the game.** Not the turn, not the seat. Four seats of one round share a
   shoe, so counting them as four independent observations produces an interval about half the
   width it should be — which is exactly the error that turns *no effect* into a finding.
2. 🔥 **A win rate is the totals divided, not the average of the per-game rates.** A strategy
   holds one seat in some games of a crossed run and three in others. Averaging the per-game
   ratios unweighted over-weights the games where it held fewest seats — which, for a strong
   strategy, are the games it does best in. **Measured, that is worth 1.05 points at four
   seats**, as large as the whole seating effect in §5. Every figure here is the ratio
   estimator, so it is the same quantity P12, P15 and P16 published, now with an interval.
3. **Every interval is 95% and normal-approximate.** Honest at thousands of games; it would not
   be at a dozen.
4. 🔥 **A margin between two strategies at the same table is computed game by game.** Exactly
   one seat declares a round, so the two players' results are strongly *negatively* correlated
   and the usual add-the-variances formula **understates** the interval — by 41% here (§6).
5. ⚠️ **A round-robin manufactures findings, so the family is corrected.** Six comparisons at a
   95% interval means roughly a one-in-four chance that some pair clears zero by luck. Every
   margin below carries a **Holm–Bonferroni** verdict beside the raw one, and *a raw
   "separated" that does not survive is not a finding.*

---

## 2. The ladder

Four rungs, ordered, each differing from the one below it in **exactly one decision** — which is
what makes a difference in results attribute to that decision and to nothing else.

| rung | what it decides differently | packet |
|---|---|---|
| `random` | nothing. A legal move, chosen arbitrarily. The floor. | P15 |
| `simple` | throws whatever costs it the fewest melded cards. | P12 |
| `greedy` | `simple`, plus a tie-break towards the cards worth keeping. | P10 |
| `cautious` | `greedy`, plus the remaining ties decided by what the discard is worth to whoever picks it up. | P15 |

⚠️ **A skill ladder is a research instrument, not a difficulty menu.** Its rungs are unevenly
spaced and one of them plays a *different and worse idea* rather than the right idea badly. See
`BUILD-PLAN.md` §3.12; **the difficulty dial is built from these but is not these — it is §9.**

---

## 3. The ranking

**Head to head at every seating in which both are at the table** — 14 of the 16 assignments of
two strategies across four seats, the two homogeneous ones excluded because a game one of them
is not at says nothing about the pair. 8,008 games a cell, each strategy sitting in each seat
exactly 4,004 times *by construction*.

**The row's win rate less the column's, in points, game by game:**

| | random | simple | greedy | cautious |
|---|---:|---:|---:|---:|
| **random** | · | −49.7 ± 0.4 \* | −49.9 ± 0.4 \* | −49.9 ± 0.4 \* |
| **simple** | +49.7 ± 0.4 \* | · | −11.2 ± 1.0 \* | −10.8 ± 1.0 \* |
| **greedy** | +49.9 ± 0.4 \* | +11.2 ± 1.0 \* | · | −0.2 ± 1.0 |
| **cautious** | +49.9 ± 0.4 \* | +10.8 ± 1.0 \* | +0.2 ± 1.0 | · |

\* survives Holm at α = 0.05 over the family of six.

| # | strategy | mean margin over the field | free-for-all win % | beat / lost / undecided |
|---|---|---:|---:|---:|
| 1 | `greedy` | +20.3 | 35.8 ± 0.9 | 2 / 0 / 1 |
| 2 | `cautious` | +20.3 | 36.5 ± 0.9 | 2 / 0 / 1 |
| 3 | `simple` | +9.2 | 27.3 ± 0.8 | 1 / 2 / 0 |
| 4 | `random` | −49.8 | 0.1 ± 0.1 | 0 / 3 / 0 |

**Three skill levels, four rungs.** *Nothing* ≪ *cover count* < *cover count + tie-break*, and
then a fourth strategy that plays differently to no measurable advantage.

🔥 **`greedy` and `cautious` are `−0.2 ± 1.0` apart, and that is the strongest confirmation of
P15's finding yet** — it comes from a design P15 never ran. The two are level head to head, level
in the free-for-all (35.8 ± 0.9 against 36.5 ± 0.9) and level in the ranking (+20.30 against
+20.28). **Why**, from P15: *denial and self-interest point the same way.* The partners a hand
holds are exactly the partners an opponent cannot hold, so "least use to me" and "least use to
them" are very nearly one ordering, and the cover score has already spent that information.
⚠️ And every *pairwise-additive* tie-break is greedy again — partnership is symmetric, so
"throw the card with the fewest partners" and "keep the best-connected twelve" are the same rule.
**A rung that improves on greedy has to be combinatorial.**

**The family, and what the correction cost:**

| comparison | margin | p | Holm threshold | raw | corrected |
|---|---:|---:|---:|---|---|
| `random` vs `cautious` | −49.9 ± 0.4 | < 1e-300 | 0.00833 | separated | **survives** |
| `random` vs `greedy` | −49.9 ± 0.4 | < 1e-300 | 0.01000 | separated | **survives** |
| `random` vs `simple` | −49.7 ± 0.4 | < 1e-300 | 0.01250 | separated | **survives** |
| `simple` vs `greedy` | −11.2 ± 1.0 | 2.1e-109 | 0.01667 | separated | **survives** |
| `simple` vs `cautious` | −10.8 ± 1.0 | 3.3e-100 | 0.02500 | separated | **survives** |
| `greedy` vs `cautious` | −0.2 ± 1.0 | 0.70 | 0.05000 | inside | does not survive |

⚠️ **Nothing was demoted here, and that is not an argument for dropping the correction.** The
same run puts `cautious` 0.66 points ahead of `greedy` in the free-for-all and 0.02 points behind
it in the ranking. **A reader who ranks point estimates would have read a rung out of two coin
flips**; the corrected verdict is what says not to.

---

## 4. Everybody at one table

The fully crossed four-way table — all 256 assignments of four strategies across four seats,
8,192 games, so a given strategy is at the table in about 5,600 of them.

| strategy | win rate |
|---|---:|
| `cautious` | 36.5 ± 0.9 |
| `greedy` | 35.8 ± 0.9 |
| `simple` | 27.3 ± 0.8 |
| `random` | 0.1 ± 0.1 |

⚠️ **This answers a different question from §3 and the two are not interchangeable.** A
free-for-all win rate depends on *who else is in the field*; a head-to-head margin weights every
opponent once. P16 is the packet that learned they differ, and both are reported for that reason.

---

## 5. The seating matters, and by how much

P12's headline — `greedy` against `simple` at four seats — under both seating schemes, at the
same seed:

| seating | greedy | simple | gap |
|---|---:|---:|---:|
| rotated `[g,s,g,s]` (8,000 games) | **31.00 ± 0.53** | 19.00 ± 0.53 | 12.0 |
| balanced, all 16 assignments (7,500 games) | **29.75 ± 0.47** | 20.25 ± 0.47 | 9.5 |

⚠️ **The rotation flatters `greedy` by 1.25 points a seat — about a fifth of the gap** — because
it seats every `greedy` downstream of a `simple`. It is not wrong: it answers *"what happens at
that table"*. **The honest strategy-vs-strategy figure is the balanced one.**

**And who sits *where* is worth nothing** (P16, 64,000 games with a downstream control arm):
upstream skill is worth **+9.1 ± 2.1** points across the `random`-to-`greedy` gulf and
**−1.0 ± 2.1** across the gap between two thinking players. *A weaker player anywhere at your
table is worth four or five points to you; which side of you they sit on is worth nothing,
unless they are not really playing.*

---

## 6. The harness measuring itself

**The null test.** `cautious` against a copy of itself under another name, 8,008 games. Two
labels of one strategy differ in *nothing*, so any gap between them is the apparatus — a seat
that opens first, a seating that is not balanced, an estimator that counts seats as trials.

| | win rate | seats sat in |
|---|---:|---|
| `cautious` | 25.1 ± 0.5 | 4004 / 4004 / 4004 / 4004 |
| `cautious#mirror` | 24.9 ± 0.5 | 4004 / 4004 / 4004 / 4004 |
| **margin** | **+0.3 ± 1.0** | inside the interval |

A fair four-seat table gives each 25.0%. ✅ **It holds, and `sim suite` exits non-zero if it ever
stops holding.**

**What pairing is worth, and which way it points.** The per-game difference against the
add-the-variances formula, on identical data:

| scope | comparison | paired SE | independent | ratio |
|---|---|---:|---:|---:|
| within-cell | `simple` vs `greedy` | 0.506 | 0.359 | **1.41** |
| within-cell | `simple` vs `cautious` | 0.506 | 0.359 | **1.41** |
| within-cell | `greedy` vs `cautious` | 0.523 | 0.370 | **1.41** |
| within-cell | `random` vs anything | 0.214–0.218 | 0.212–0.214 | 1.01–1.02 |
| across-cells | `simple`: vs `greedy` less vs `cautious` | 0.203 | 0.359 | **0.57** |
| across-cells | `random`: vs `simple` less vs `greedy` | 0.028 | 0.035 | 0.79 |
| across-cells | `greedy`: vs `random` less vs `simple` | 0.312 | 0.331 | 0.94 |
| across-cells | `cautious`: vs `random` less vs `simple` | 0.313 | 0.331 | 0.95 |

🔥 **Pairing is not a synonym for a narrower interval. It measures the correlation that is
there, and here it points both ways.**

- **Within a cell**, two strategies sit at the *same* table and **exactly one seat declares**, so
  their results are strongly negatively correlated and the paired interval is **wider** — by
  **√2 to three digits** in every cell of thinking players, which is what a perfectly opposed
  pair of equal-variance series predicts. ⚠️ **So the independent formula is not conservative
  here but *anti*-conservative.** (Against `random` the ratio collapses to 1.01: a series that
  is almost always zero has almost nothing to correlate with.)
- **Across cells**, one strategy plays two different tables dealt from the *same shoes*, the
  correlation is positive, and pairing **narrows** — down to 0.57. That is free variance
  reduction, and it is what common random numbers are for.

---

## 7. What this apparatus can and cannot resolve

At 8,008 games a cell the standard error on a head-to-head margin between two thinking rungs is
**0.52 points**, so the 95% half-width is **1.02**.

- **Anything smaller than about a point is reported as inside the interval** and is not claimed.
  *"Smaller than we can detect"* is a real answer.
- ⚠️ **To resolve half a point — the size of P15's `cautious` effect — costs about 34,000 games
  a cell**, roughly four hours on the machine this was measured on. That is very nearly the
  32,000 P15 had to run to settle the same question.
- **So a new rung worth less than about a point is not promotable at the default run size**, and
  a packet proposing one has to say in advance what it will cost to measure.

---

## 8. Entries for the rungs that failed

**Kept deliberately. A rung that plays differently to no measurable effect is a *result*, and
the reasoning that made it plausible is worth more than its number.**

- **`cautious` — throw the card least useful to the player you feed.** Worth `+0.5 ± 0.55`
  points (P15, 32,000 games) and `−0.2 ± 1.0` here; not separated from `greedy` under any design
  yet run. ⚠️ It also could not be shown to *deny* anybody anything: sitting upstream of a focal
  seat it cost that seat `−0.3 ± 1.4` points and cost itself nothing (P16). **Denial and
  self-interest point the same way**, and what is left over is only what a hand cannot influence
  — how many runs a rank sits in at all, and second-order blockages.
- **The upstream-neighbour hypothesis** — *the main factor is the skill of the player before
  you.* **False between thinking players** (`−1.0 ± 2.1`), true only across the gulf to someone
  not really playing (`+9.1 ± 2.1`). ⚠️ **Without the downstream control arm the answer would
  have been +19.4 and wrong by a factor of two** (P16).

---

## 9. The difficulty dial

**Not the ladder.** A person choosing an opponent is offered these four and never the rungs
above, and `BUILD-PLAN.md` §3.12 is why: a ladder is a research instrument — unevenly spaced,
entitled to be incomplete, and its lower rungs play a *different and worse idea* rather than the
right idea badly. **A weaker player plays the right idea and slips.** So every level is the
strongest rung there is (`greedy`) wearing a **mistake rate** — with probability ε it throws the
card that rung ranked *second* instead of the one it ranked first, and nothing else about it
changes.

| level | ε | what it is |
|---|--:|---|
| `expert` | 0.00 | throws the best card it can see, every turn |
| `hard` | 0.50 | slips about half the time on the cards it is choosing between |
| `medium` | 0.70 | throws the wrong one of two good cards more often than not |
| `easy` | 0.90 | gets it wrong nearly every time |

⚠️ **ε is not the dial; results are.** Measured over a sweep of seven mistake rates, ε = 0 to
0.5 costs about **8 points** of win rate and ε = 0.5 to 1 costs about **17** — so levels spaced
evenly in ε would not be spaced evenly in play. These four are spaced by the measurement.

### The reference table — all four levels at one table, fully crossed

256 assignments across four seats, 8,192 games, 5,600 of them holding any one level.

| level | win % | step |
|---|--:|--:|
| `expert` | **36.11 ± 0.87** | — |
| `hard` | **27.22 ± 0.83** | −8.9 |
| `medium` | **22.17 ± 0.79** | −5.1 |
| `easy` | **14.50 ± 0.69** | −7.7 |

### The steps, head to head

**The family is the three adjacent steps and not the round-robin**, because *"n+1 beats n"* is
the only claim a monotone dial makes — correcting over all six pairs would throw power away for
comparisons nobody is making. Each cell is 8,008 games at every seating in which both levels are
at the table, and each margin is the **paired** one (§1 rule 4).

| step | margin | Holm |
|---|--:|---|
| `expert` over `hard` | **+9.90 ± 1.00** | separated |
| `hard` over `medium` | **+5.69 ± 1.01** | separated |
| `medium` over `easy` | **+10.79 ± 1.00** | separated |

✅ **Every step clears the correction, and the narrowest of them is 5.7× the half-width.** A
level that could not be separated from its neighbour would be deleted rather than shipped
(§3.12 item 2) — that is why there are four and not five. ⚠️ **`sim suite` exits non-zero if the
dial ever stops being monotone**, which is how a rung that raises the ceiling (P20–P22) is
stopped from quietly invalidating the menu.

```bash
# the calibration, on its own
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --games 8000 --seed 20260819

# the sweep that placed the four values
dotnet run -c Release --project BurmesePoker.Sim -- --strategies greedy@0,greedy@0.1,greedy@0.2,greedy@0.35,greedy@0.5,greedy@0.75,greedy@1 --seating balanced --games 4802 --seed 20260819
```

⚠️ **`greedy@0.35` is a calibration probe, not a level** — a rung at an arbitrary mistake rate,
nameable by the harness so that the sweep above can be re-run by somebody who doubts it. It is
deliberately unreachable from a menu, a form field or `--difficulty`.

---

## 10. Regenerating this document's data

```bash
# the standing set — writes docs/strategy/measurements.csv
dotnet run -c Release --project BurmesePoker.Sim -- suite --strategies random,simple,greedy,cautious --games 8000 --seed 20260819

# the full report the tables above are drawn from
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies random,simple,greedy,cautious --games 8000 --seed 20260819 --csv tournament.csv

# one ordinary run, now with an interval on every figure
dotnet run -c Release --project BurmesePoker.Sim -- --strategies greedy,simple --seating balanced --games 4096 --seed 20260819

# the difficulty dial on its own — §9
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --games 8000 --seed 20260819
```

`measurements.csv` carries, for every row, the **command that produced it**, the games it came
from, the seed, the mean, the standard error and the 95% half-width. ⚠️ **The standing set is a
list in `Suite.Run`**: a rung added to the catalog does not appear here until it is added there
too. BUILD-PLAN **P23** is where that stops being a habit and becomes a test.
