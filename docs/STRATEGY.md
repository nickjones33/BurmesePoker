# Strategy — what actually works

**The standing answer to *which way of playing is better, by how much, and how sure are we*.**

Companion to `RULES.md`, and held to the same discipline. `RULES.md` tags every rule with its
provenance because a rule without a source cannot be re-examined; **a measurement without its
origin is worse, because it looks like a fact**. So every number below is *generated* rather than
transcribed, out of `docs/strategy/measurements.csv`, which is written by one command:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- suite --games 8000 --seed 20260819
```

Last generated **2026-08-20** (BUILD-PLAN P23). **77 measurements in 17,539 s — just under five
hours** — the ladder ranked, the difficulty dial calibrated and re-fitted beside it, the money
sweep, and P12's headline under both seatings.

🔥 **59 of the 77 rows came back byte-identical to the previous run**, which is the strongest
reproducibility statement this document has carried. Every ladder figure, the harness's own null
test, every pairing check and both headline rows are the same digits from a tree that has since
gained a rung and changed what the standing field is. **The seven rows that moved are the
difficulty dial and only the difficulty dial** — §9, where one ε was re-fitted on purpose — and
the twelve that are new are the money sweep, which §10 now quotes from here rather than from a
file of its own.

⚠️ **The suite is a five-hour job and there is no version of it that is not.** Head-to-head is
`k(k−1)/2`, `outs` costs about eight times a `greedy` round and is in five of the fifteen cells,
every difficulty level is built on `outs`, and the money sweep is four more cells of two `outs`
derivatives. **`--pairs adjacent` exists** (P19) if it stops finishing at all — a ladder's claim
is that each rung beats the one below it, which is k−1 cells rather than k(k−1)/2 — but that
would throw away the matrix in §3, which is the document's centre.

⚠️ **The `--strategies` list is gone from that command, and its absence is the point.** P20 added
a fifth rung and had to spell the field out in three places to make it appear; the field is a
**filter of `BotCatalog`** now (P18, P20, P23), so a rung cannot be added without being measured
— which is how `outs` came to be measured against all five of the others without anybody naming
it. ⚠️ **What is filtered out is not dropped**: a rung whose play reads the *stakes* is settled by
the money sweep instead of by a field played at one stakes, and §11 is where that promise is made
mechanical.

The fuller report the tables below are drawn from comes from the same seed:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- tournament --games 8000 --seed 20260819
```

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

Seven rungs, ordered, each differing from the one below it in **exactly one decision** — which is
what makes a difference in results attribute to that decision and to nothing else.

| rung | what it decides differently | packet |
|---|---|---|
| `random` | nothing. A legal move, chosen arbitrarily. The floor. | P15 |
| `simple` | throws whatever costs it the fewest melded cards. | P12 |
| `greedy` | `simple`, plus a tie-break towards the cards worth keeping. | P10 |
| `cautious` | `greedy`, plus the remaining ties decided by what the discard is worth to whoever picks it up. | P15 |
| `counting` | `cautious`, plus a **memory**: what is left in the shoe is estimated from every card it has been shown this round, not from its own thirteen. | P20 |
| `outs` | `greedy`, plus a **look ahead**: where the cover count ties, keep the thirteen that more of the values still out there would improve. | P21 |
| `prospector` | `outs`, plus the only decision money may touch: take a card from anywhere but the deck only when it is worth more than the ownership a blind draw confers. ⚠️ **At $5/$1 that is never, so it is `outs` — see §10.** | P22 |

🔥 **Where a rung's new key goes is not a detail, and §3 is about to make that the headline.**
`cautious` and `counting` both slide theirs in **beneath** greedy's tie-break, so they decide
only what greedy had already given up on. `outs` puts its key **above** it, and greedy's
tie-break is demoted to breaking *its* ties. **Two of those three rungs measure nothing and the
third is the only rung that has ever beaten `greedy`.**

⚠️ **A skill ladder is a research instrument, not a difficulty menu.** Its rungs are unevenly
spaced and one of them plays a *different and worse idea* rather than the right idea badly. See
`BUILD-PLAN.md` §3.12; **the difficulty dial is built from these but is not these — it is §9.**

⚠️ **And `prospector` is the first rung whose strength is not a property of the rung alone.** Its
one decision reads the **stakes**, which are fixed at the start of a game and not by the rules
(RULES.md §4.3), so *how well it plays* is a different question at $5/$1 and at $5/$40 — §10 is
where it is measured, and it is the only rung in this document ranked on money rather than on win
rate.

🔥 **Which is why it is not in §3's matrix, and that is a decision rather than an omission.** A
head-to-head cell is played at one stakes; at the stakes this game is played for, `prospector`
and `outs` deal the same rounds card for card (§10), so its six cells would reproduce an identity
a unit test already asserts. **Three quarters of an hour of run time is the smaller half of the
cost.** The larger half is that six null cells join the Holm family in §3 and **make every real
verdict in it harder to reach** — a duplicate is not a free row. So each rung declares, in
`Domain/Agents/BotCatalog.cs`, which instrument settles it (`BotRung.Ranked`), the ladder
tournament measures one set and the money sweep the other, and **a test asserts that between them
they are the whole catalog** — so a rung can no more fall out of the programme than it can fail
to appear in it.

---

## 3. The ranking

**Head to head at every seating in which both are at the table** — 14 of the 16 assignments of
two strategies across four seats, the two homogeneous ones excluded because a game one of them
is not at says nothing about the pair. 8,008 games a cell, each strategy sitting in each seat
exactly 4,004 times *by construction*.

⚠️ **Six rungs and not the catalog's seven**: `prospector` is settled by §10 rather than here,
for the reason §2 gives. 🔥 **Its absence costs this matrix nothing, and that is measured rather
than argued** — the run that produced these figures had `prospector` in the catalog and out of
the field, and **every cell below came back to the digit the six-rung run before it gave**.

**The row's win rate less the column's, in points, game by game:**

| | `random` | `simple` | `greedy` | `cautious` | `counting` | `outs` |
|---|---:|---:|---:|---:|---:|---:|
| **`random`** | · | −49.7 ± 0.4 \* | −49.9 ± 0.4 \* | −49.9 ± 0.4 \* | −49.8 ± 0.4 \* | −49.9 ± 0.4 \* |
| **`simple`** | +49.7 ± 0.4 \* | · | −11.2 ± 1.0 \* | −10.8 ± 1.0 \* | −11.0 ± 1.0 \* | −14.2 ± 1.0 \* |
| **`greedy`** | +49.9 ± 0.4 \* | +11.2 ± 1.0 \* | · | −0.2 ± 1.0 | +0.3 ± 1.0 | **−3.1 ± 1.0** \* |
| **`cautious`** | +49.9 ± 0.4 \* | +10.8 ± 1.0 \* | +0.2 ± 1.0 | · | +0.8 ± 1.0 | **−2.8 ± 1.0** \* |
| **`counting`** | +49.8 ± 0.4 \* | +11.0 ± 1.0 \* | −0.3 ± 1.0 | −0.8 ± 1.0 | · | **−3.3 ± 1.0** \* |
| **`outs`** | +49.9 ± 0.4 \* | +14.2 ± 1.0 \* | **+3.1 ± 1.0** \* | **+2.8 ± 1.0** \* | **+3.3 ± 1.0** \* | · |

\* survives Holm at α = 0.05 over the family of fifteen.

| # | strategy | mean margin over the field | free-for-all win % | beat / lost / undecided |
|---|---|---:|---:|---:|
| 1 | `outs` | +14.6 | 34.9 ± 1.1 | **5 / 0 / 0** |
| 2 | `cautious` | +11.8 | 30.1 ± 1.0 | 2 / 1 / 2 |
| 3 | `greedy` | +11.6 | 31.5 ± 1.1 | 2 / 1 / 2 |
| 4 | `counting` | +11.3 | 31.2 ± 1.1 | 2 / 1 / 2 |
| 5 | `simple` | +0.5 | 22.0 ± 1.0 | 1 / 4 / 0 |
| 6 | `random` | −49.8 | 0.1 ± 0.1 | 0 / 5 / 0 |

⚠️ **The mean margin and the free-for-all win rates are not comparable with the five-rung run
above them in git history** — both depend on who else is in the field, and the field gained a
rung that beats everything in it. **The head-to-head margins do not**, and every one of the ten
that P20 published reproduced to the digit.

🔥 **`outs` is the first rung that beats `greedy`, and it beats everything: 5 / 0 / 0.** Where two
discards leave the hand equally melded it keeps the thirteen that **more of the values still out
there would improve** — `+3.1 ± 1.0` over `greedy`, `+2.8 ± 1.0` over `cautious`, `+3.3 ± 1.0`
over `counting`, all three surviving the correction over a family of fifteen. It is the only
figure in this document that separates two *thinking* rungs.

🔥 **Why it paid when the two rungs before it did not, and the answer is structural rather than
clever.** `cautious` and `counting` both slide their new idea in **underneath**
`CoverScore.Potential`: they decide only what `greedy` had already given up on. `outs` puts its
key **above** it — where the cover count ties, the outs count decides, and greedy's own tie-break
is demoted to breaking *its* ties. ⚠️ **Greedy's leftovers are worth about half a point and this
apparatus cannot see half a point** (§7), so a rung that competes for the residue is
unmeasurable before it is written. **A rung that changes the question asked before greedy's
tie-break speaks is playing for something the instrument can see.**

⚠️ **And it does not contradict P15 — it is what P15 said to do.** *Every pairwise-additive
tie-break is greedy again*, because partnership is symmetric: "throw the card with the fewest
partners" and "keep the best-connected twelve" are the same rule. **A rung that improves on
greedy has to be combinatorial**, and a count of live outs is: it asks, of each value in turn, a
question about the *whole arrangement* of the thirteen that would be left. Two cards with
identical partners can leave different numbers of outs behind — the clearest case is a second
copy of a card already in a run of six, which no count of partners can see and which splits that
run into two legal runs covering one more card.

🔥 **`greedy` and `cautious` are still `−0.2 ± 1.0` apart, and `counting` is still inside the
interval against both.** Three rungs, one level, three packets of measurement. ⚠️ **Note what
their free-for-all order does here**: `greedy` 31.5, `counting` 31.2, `cautious` 30.1 — a
1.4-point spread among three players nothing can separate head to head, and last time it ranked
them the other way up. **That column is noise at this resolution and §4 says so.**

**The family, and what the correction cost:**

| comparison | margin | p | Holm threshold | raw | corrected |
|---|---:|---:|---:|---|---|
| `random` vs `simple` | −49.7 ± 0.4 | < 1e-300 | 0.00333 | separated | **survives** |
| `random` vs `greedy` | −49.9 ± 0.4 | < 1e-300 | 0.00357 | separated | **survives** |
| `random` vs `cautious` | −49.9 ± 0.4 | < 1e-300 | 0.00385 | separated | **survives** |
| `random` vs `counting` | −49.8 ± 0.4 | < 1e-300 | 0.00417 | separated | **survives** |
| `random` vs `outs` | −49.9 ± 0.4 | < 1e-300 | 0.00455 | separated | **survives** |
| `simple` vs `greedy` | −11.2 ± 1.0 | < 1e-300 | 0.00500 | separated | **survives** |
| `simple` vs `cautious` | −10.8 ± 1.0 | < 1e-300 | 0.00556 | separated | **survives** |
| `simple` vs `counting` | −11.0 ± 1.0 | < 1e-300 | 0.00625 | separated | **survives** |
| `simple` vs `outs` | −14.2 ± 1.0 | < 1e-300 | 0.00714 | separated | **survives** |
| `counting` vs `outs` | −3.3 ± 1.0 | 2.8e-10 | 0.00833 | separated | **survives** |
| `greedy` vs `outs` | −3.1 ± 1.0 | 1.9e-09 | 0.01000 | separated | **survives** |
| `cautious` vs `outs` | −2.8 ± 1.0 | 9.2e-08 | 0.01250 | separated | **survives** |
| `cautious` vs `counting` | +0.8 ± 1.0 | 0.11 | 0.01667 | inside | does not survive |
| `greedy` vs `counting` | +0.3 ± 1.0 | 0.55 | 0.02500 | inside | does not survive |
| `greedy` vs `cautious` | −0.2 ± 1.0 | 0.70 | 0.05000 | inside | does not survive |

⚠️ **The `p` and `threshold` columns are recomputed here from the mean and standard error the CSV
carries**, by the two-sided normal test and the Holm ladder the harness uses; **the `corrected`
column is the CSV's own verdict**, and the two agree on all fifteen rows.

⚠️ **Nothing was demoted here, and that is not an argument for dropping the correction.** The
same run puts `greedy` 1.4 points ahead of `cautious` in the free-for-all and 0.2 points behind
it head to head. **A reader who ranks point estimates would have read a rung out of two coin
flips**; the corrected verdict is what says not to. 🔥 **P21 grew the family from ten to fifteen
and the strictest threshold tightened from `0.00500` to `0.00333` — and the three new margins
cleared it by seven orders of magnitude.** A correction that only ever kills findings would be
easy to resent; this is the run that shows it letting one through.

---

## 4. Everybody at one table

The fully crossed table — every assignment of the field across four seats, 8,192 games, so a
given strategy is at the table in about 4,800 of them.

| strategy | win rate |
|---|---:|
| `outs` | 34.9 ± 1.1 |
| `greedy` | 31.5 ± 1.1 |
| `counting` | 31.2 ± 1.1 |
| `cautious` | 30.1 ± 1.0 |
| `simple` | 22.0 ± 1.0 |
| `random` | 0.1 ± 0.1 |

⚠️ **Every figure here moved when P21 added a rung, and none of it is a change in play.** Six
strategies crossed over four seats is a different field from five, so each is at the table less
often and against a stronger average opponent. **This is the column to distrust when a rung is
added; §3's margins are the column that survives it.**

🔥 **This run is the sharpest demonstration of that yet.** `greedy`, `counting` and `cautious`
come out 31.5 / 31.2 / 30.1 here — and P20's run of the same three, in a five-rung field, had
them 32.8 / 33.5 / 34.3, **in exactly the opposite order**. Nothing about any of the three
changed. Their head-to-head margins moved by at most a tenth of a point. ⚠️ **A 4.2-point swing
in a column, and a reversal, produced entirely by who else sat down.**

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

**The null test.** The strongest rung against a copy of itself under another name, 8,008 games.
Two labels of one strategy differ in *nothing*, so any gap between them is the apparatus — a seat
that opens first, a seating that is not balanced, an estimator that counts seats as trials.

| | win rate | seats sat in |
|---|---:|---|
| `outs` | 25.1 ± 0.5 | 4004 / 4004 / 4004 / 4004 |
| `outs#mirror` | 24.9 ± 0.5 | 4004 / 4004 / 4004 / 4004 |
| **margin** | **+0.3 ± 1.0** | inside the interval |

A fair four-seat table gives each 25.0%. ✅ **It holds, and `sim suite` exits non-zero if it ever
stops holding.** ⚠️ **The subject changed with the field** — P17 ran this on `cautious`, P20 on
`counting`, and it is `outs` now; the rung is whichever the catalog ends with, so **a rung added
is a rung the null test moves to.** 🔥 **This pass is worth more than either of the earlier
ones.** `outs` is the first rung that carries a **cache across deals**, and a cache is precisely
the kind of state that could make a game depend on the order the harness happened to schedule it
in. It does not, because the cache is keyed on card *values* rather than on the identities of a
round's shoe — and this is the run that says so at scale rather than in a unit test.

**What pairing is worth, and which way it points.** The per-game difference against the
add-the-variances formula, on identical data:

| scope | comparison | paired SE ÷ independent |
|---|---|---:|
| within-cell | `greedy` vs `cautious` | **1.4142** |
| within-cell | `greedy` vs `counting` | **1.4142** |
| within-cell | `cautious` vs `counting` | **1.4142** |
| within-cell | `cautious` vs `outs` | **1.4139** |
| within-cell | `greedy` vs `outs` | **1.4138** |
| within-cell | `counting` vs `outs` | **1.4137** |
| within-cell | `simple` vs any thinking rung | 1.404–1.409 |
| within-cell | `random` vs anything | 1.008–1.017 |
| across-cells | `greedy`: vs `cautious` less vs `counting` | **0.52** |
| across-cells | `simple`: vs `greedy` less vs `cautious` | **0.57** |
| across-cells | `cautious`: vs `counting` less vs `outs` | 0.72 |
| across-cells | `random`: vs `simple` less vs `greedy` | 0.79 |
| across-cells | `outs`: vs `random` less vs `simple` | 0.92 |
| across-cells | `counting`: vs `random` less vs `simple` | 0.94 |

⚠️ **The ratio is what `measurements.csv` carries**, so it is what is quoted; the two standard
errors behind it are in the `tournament` report the CSV is generated from.

🔥 **Pairing is not a synonym for a narrower interval. It measures the correlation that is
there, and here it points both ways.**

- **Within a cell**, two strategies sit at the *same* table and **exactly one seat declares**, so
  their results are strongly negatively correlated and the paired interval is **wider** — by
  **√2 to four digits** — `1.4142` — in every cell of two evenly matched thinking players, which
  is exactly what a perfectly opposed pair of equal-variance series predicts. ⚠️ **So the
  independent formula is not conservative here but *anti*-conservative.** (Against `random` the
  ratio collapses towards 1: a series that is almost always zero has almost nothing to correlate
  with. And where the two are *not* evenly matched — `outs` against the rungs it beats — it dips
  a shade below √2, because the opposition stops being symmetric.)
- **Across cells**, one strategy plays two different tables dealt from the *same shoes*, the
  correlation is positive, and pairing **narrows** — down to 0.52. That is free variance
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

🔥 **P21 is the first rung this apparatus has been able to see, and the margin is what tells you
where to look for the next one.** `outs` came in at `+3.1 ± 1.0` — three times the half-width, at
the ordinary run size, with no special design. ⚠️ **Read that beside `cautious`'s half a point
and it says something about *kinds* of idea rather than about sample sizes**: a rung that refines
what greedy had already given up on is competing for a residue that is smaller than the
instrument, and no affordable number of games rescues it. A rung that changes the question asked
*before* greedy's tie-break gets to speak is playing for something the instrument can see.

---

## 8. Entries for the rungs that failed

**Kept deliberately. A rung that plays differently to no measurable effect is a *result*, and
the reasoning that made it plausible is worth more than its number.**

**Every rung this project has built has an entry somewhere in this document, and this is where
the ones that returned nothing live.** The complete map, so nothing has to be hunted for:

| rung | verdict | where |
|---|---|---|
| `random`, `simple`, `greedy` | the three separated skill levels | §3 |
| `cautious` | **nothing** — `−0.2 ± 1.0` against `greedy` | below |
| `counting` | **nothing**, and pointing the wrong way — `+0.3 ± 1.0` to `greedy` | below |
| `outs` | **`+3.1 ± 1.0` over `greedy`**, and it beats the whole field | §3 |
| `prospector` | **nothing at the stakes this is played for**, `+7.3 ± 3.3` a round at $5/$40 | §10 |

⚠️ **Two of the four research rungs are entries below, one is in §3 and one is in §10.** Read the
two below with this in mind: **both of them put their new idea underneath `CoverScore.Potential`
and `outs` put its above it**, and that is the only structural difference between them.

🔥 **And `prospector` is a fifth kind of answer, which is why it has a section rather than a
bullet.** It did not lose. It asked a question the other four never asked — *what is the side bet
worth?* — and got back **a function of the stakes** rather than a number: literally the same
player as `outs` at $5/$1, `+7.34 ± 3.29` a round at $5/$40. ⚠️ **A rung can fail to be
interesting at the stakes the game is played for and still be the right thing to have built**,
because the null is what settles the question. RULES.md §4.4 had a `DERIVED` remark saying the
money layer is *"not meant to be played for, only settled"*; §10 is that remark measured, and it
is now the reason nobody has to wonder.

- **`cautious` — throw the card least useful to the player you feed.** Worth `+0.5 ± 0.55`
  points (P15, 32,000 games) and `−0.2 ± 1.0` here; not separated from `greedy` under any design
  yet run. ⚠️ It also could not be shown to *deny* anybody anything: sitting upstream of a focal
  seat it cost that seat `−0.3 ± 1.4` points and cost itself nothing (P16). **Denial and
  self-interest point the same way**, and what is left over is only what a hand cannot influence
  — how many runs a rank sits in at all, and second-order blockages.
- **`counting` — remember every card you have been shown.** Worth **`+0.3 ± 1.0` points to
  `greedy`**, i.e. not separated and pointing the wrong way; `cautious` is `+0.8 ± 1.0` ahead of
  it (P20, 8,008 games a cell). 🔥 **It is not that the memory does not work — it does, and a
  test says so.** A counting seat's estimate of what is left really does fall below what
  `cautious` would say for every card it has watched go by, and stays at the full two copies for
  every value it has not.
  ⚠️ **Two reasons it cannot pay, and both were visible in advance.**
  **(1) The information set is tiny.** Under the cautious default — a seat counts only what it
  has *actually been shown* (RULES.md §9 #15 is open, so the alternative would have been to let
  it read what the rules may conceal) — the memory runs **12 → 23 cards across a whole round, out
  of 108**. It ends the round having learned about **ten cards beyond its own hand**, roughly one
  a turn, and knowing a fifth of the shoe.
  **(2) It enters where nothing is paid.** The estimate feeds `ThreatScore`, which is
  `cautious`'s tie-break — and §3 already measured that tie-break at `−0.2 ± 1.0`. **A sharper
  input to a decision rule worth nothing is worth nothing**, and the two nulls compound rather
  than add. ⚠️ **The lesson for P21 and P22: the next rung must change *which question is asked*,
  not improve an answer to a question already shown not to matter.**
  ✅ It costs nothing to run — **77 rounds/s against `cautious`'s 76 and `greedy`'s 88** (P12's
  baseline: 51 serial, 85–92 parallel), so the memory is free and only the idea is not.
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
strongest rung there is wearing a **mistake rate** — with probability ε it throws the card that
rung ranked *second* instead of the one it ranked first, and nothing else about it changes.

✅ **Re-fitted on 2026-08-20 against `outs`, which is what P23 owed this section.** P19 placed
these four against `greedy`; P21 promoted `outs` (§3) and re-based every level onto it without
re-spacing them, which left a dial that was still ordered and no longer evenly spaced. **One value
moved and three did not**: `hard` went from ε = 0.5 to **ε = 0.4**, and the reference table below
went from steps of **8.2 / 4.3 / 10.3** points to **7.9 / 6.7 / 7.7**.

🔥 **That one value is the finding.** ε was re-swept on `outs` over the same seven probes P19 used
on `greedy`, and **the curve has very nearly the same shape**: ε = 0 → 0.5 costs 9.5 points of win
rate where on `greedy` it cost about 8, and ε = 0.5 → 1 costs 16.5 where it cost about 17. A
mistake rate is therefore close to being **a property of the mistake rather than of the rung it is
made against** — so the next rung to raise the ceiling should expect to *re-check* this table and
not to re-derive it. ⚠️ **It is not close enough to skip the check**: leaving the P19 values in
place is what produced the 4.3-point middle step above, which is a dial with a flat spot in it.

| level | ε | what it is |
|---|--:|---|
| `expert` | 0.00 | throws the best card it can see, every turn |
| `hard` | 0.40 | slips about two times in five on the cards it is choosing between |
| `medium` | 0.70 | throws the wrong one of two good cards more often than not |
| `easy` | 0.90 | gets it wrong nearly every time |

⚠️ **ε is not the dial; results are.** Measured over a sweep of seven mistake rates on `outs`,
4,802 games at every balanced seating:

| ε | 0 | 0.1 | 0.2 | 0.35 | 0.5 | 0.75 | 1 |
|---|--:|--:|--:|--:|--:|--:|--:|
| win % | 33.6 | 33.7 | 30.9 | 27.1 | 24.1 | 18.1 | 7.5 |

**Levels spaced evenly in ε would not be spaced evenly in play** — the last quarter of the dial is
worth more than the first half of it — so the four are placed by inverting that curve. 🔥 **And
ε = 0 is indistinguishable from ε = 0.1**: 33.6 against 33.7, with a ±1.6 interval on each. *The
best card and the second-best card are usually the same card*, which is the sentence that explains
why the top of the dial has to be ε = 0 and why a fifth level between `expert` and `hard` would be
two names for one opponent.

### The reference table — all four levels at one table, fully crossed

256 assignments across four seats, 8,192 games, 5,600 of them holding any one level.

| level | win % | step |
|---|--:|--:|
| `expert` | **36.13 ± 0.86** | — |
| `hard` | **28.39 ± 0.84** | −7.7 |
| `medium` | **21.67 ± 0.79** | −6.7 |
| `easy` | **13.81 ± 0.68** | −7.9 |

### The steps, head to head

**The family is the three adjacent steps and not the round-robin**, because *"n+1 beats n"* is
the only claim a monotone dial makes — correcting over all six pairs would throw power away for
comparisons nobody is making. Each cell is 8,008 games at every seating in which both levels are
at the table, and each margin is the **paired** one (§1 rule 4).

| step | margin | Holm |
|---|--:|---|
| `expert` over `hard` | **+6.82 ± 1.01** | separated |
| `hard` over `medium` | **+9.80 ± 1.00** | separated |
| `medium` over `easy` | **+11.20 ± 1.00** | separated |

✅ **Every step clears the correction, and the narrowest of them is 6.8× the half-width.** A level
that could not be separated from its neighbour would be deleted rather than shipped (§3.12
item 2) — that is why there are four and not five.

⚠️ **The two tables above do not agree about which step is the widest, and that is worth
knowing.** At the reference table the steps are 7.7 / 6.7 / 7.9 and the middle one is narrowest;
head to head they are 6.8 / 9.8 / 11.2 and the *top* one is. **They are not the same
measurement**: a head-to-head cell holds two levels and a reference table holds four, and a pair
of strong players plays a different — longer, more thoroughly dealt — round than a pair of weak
ones. The re-fit improves both readings over the P19 values (reference-table spread 6.0 → 1.1
points, head-to-head spread 5.1 → 4.4), which is why the choice between them did not have to be
made. ⚠️ **If a future rung makes them disagree in *ordering* rather than in spacing, the
reference table is the one the shipped values are fitted to**, because a person who asks for a
mixed table is sitting at exactly it.

⚠️ **`sim suite` exits non-zero if the dial ever stops being monotone**, which is how a rung that
raises the ceiling is stopped from quietly invalidating the menu — **P21 is the packet that fired
that alarm in anger**, and the re-fit above is the packet that answered it. 🔥 **A second check
landed with it** (P23): `StandingAnswerTests` fails the build if the ε values in this table stop
being the ε values the two front ends offer. **A calibration that drifts from the menu is worse
than none, and proofreading is not a mitigation.**

```bash
# the calibration, on its own
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --games 8000 --seed 20260819

# the sweep that placed the four values
dotnet run -c Release --project BurmesePoker.Sim -- --strategies outs@0,outs@0.1,outs@0.2,outs@0.35,outs@0.5,outs@0.75,outs@1 --seating balanced --games 4802 --seed 20260819

# and the candidate spacing, checked at the reference table before the five-hour run
dotnet run -c Release --project BurmesePoker.Sim -- --strategies outs@0.9,outs@0.7,outs@0.4,outs@0 --seating balanced --games 8192 --seed 20260819
```

⚠️ **`outs@0.35` is a calibration probe, not a level** — a rung at an arbitrary mistake rate,
nameable by the harness so that the sweep above can be re-run by somebody who doubts it. It is
deliberately unreachable from a menu, a form field or `--difficulty`.

---

## 10. The side bet — should you draw blind for the money?

**The one strategy axis in this game that is not rummy, and the first question in the programme
whose answer is not a win rate** (P22, 32,032 games). ✅ **Generated from
`docs/strategy/measurements.csv` like every other section since P23** — the sweep is part of the
standing set, so one `sim suite` regenerates the whole document's data; `money.*` are the rows,
and every figure below reproduced P22's separate run to the digit.

**The question.** Ownership of a money card is permanent and never transfers (RULES.md §4.4), so
holding one is worth nothing and throwing one away costs nothing — which is why every rung's
*discard* is money-blind and why a test says so. What that rule does **not** settle is where a
card comes **from**: *"you own a money card if — and only if — the deck gave it to you"*, so a
blind draw confers ownership and picking a card off the pile beside you never does. A seat that
digs in the deck therefore acquires more money while playing a worse hand, and **nobody had
measured what that trade is worth.** `greedy` concedes exactly one tie-break to it and nothing
else.

**The rung.** `prospector` is `outs` with one change: it takes a card from anywhere but the deck
only when the melded cards it buys are worth more than the ownership the blind draw would have
conferred. The ownership value is public arithmetic — the stakes (§4.3), the designation (§4.1),
how many players pay, and how much of the shoe this seat can see. The exchange rate from *melded
cards* to *money* is the rung's **one modelling assumption**, stated rather than tuned: thirteen
melded cards is the round, so one card is a thirteenth of it.

### The sweep

Head to head against `outs`, 8,008 games a cell, every seating in which both are at the table,
paired margins (§1 rule 4), Holm-corrected over the four cells. ⚠️ **The money column is the
verdict and the win rate is beside it**, because a rung that wins fewer rounds and banks more is
the better player.

| stakes | money card is worth | `prospector` take % | **$ a round** | win % | Holm |
|---|--:|--:|--:|--:|---|
| **$5/$1** — as played | 0.2 rounds | 25.0 | **+0.01 ± 0.22** | +0.3 ± 1.0 | inside |
| $5/$10 | 2 rounds | 16.8 | **−0.86 ± 0.82** | −6.6 ± 1.0 | raw only |
| $5/$20 | 4 rounds | 8.4 | **+0.95 ± 1.63** | −13.2 ± 1.0 | inside |
| $5/$40 | 8 rounds | 0.1 | **+7.34 ± 3.29** | −20.1 ± 0.9 | **separated** |

`outs` takes 24.9–25.1% of its cards off the pile in every cell, which is the control: the only
thing moving down that column is the rule firing.

### The answer

🔥 **No — at the stakes this game is actually played for, and by a wide margin.** At $5/$1 the
rule **never fires at all**: one melded card is worth about $1.15 at a four-handed table and the
ownership a blind draw confers is worth about 20 cents, so the comparison is never close.
`prospector` and `outs` are **the same player under two names at $5/$1**, and that is an
identity rather than a measurement — two tables of one rung each, dealt from the same shoes, play
the same rounds card for card
(`MoneySweepTests.AtTheStandardStakesTheTwoRungsPlayOneGameUnderTwoNames`). The top row above is
therefore a **null cell**, and at `+0.01 ± 0.22` it is the tightest one this harness has produced
— which is what a null cell of two genuinely identical players should look like.

🔥 **Yes — from about a money card worth eight rounds, and the crossover is somewhere near four.**
At $5/$40 the rung stops taking almost entirely (0.1%), wins **20 points fewer rounds**, and
banks **$7.34 ± 3.29 more a round** — surviving Holm at `p = 1.3e-05`. The side bet alone moves
`+11.36 ± 3.29`, so it buys the whole of the win rate it gives away and more. Below that: $5/$20
is `+0.95 ± 1.63`, positive and inside its interval — *break-even, as far as this apparatus can
see* — and $5/$10 is `−0.86 ± 0.82`, separated raw but not surviving the correction. **The order
of the four is monotone in the stakes**, which is the shape the hypothesis predicted.

⚠️ **This is the first published divergence between money and win rate in the programme**, and
the reporting has split the flat prize from the side bet since P12 precisely so it could be seen.
At $5/$40 a reader ranking by win rate would rank `prospector` last and be wrong.

⚠️ **What it does not say.** The exchange rate above is a model, and a crude one — the thirteenth
melded card is worth far more than the third, so the rung takes the discard rather more often
than a sharper model would. The bias runs **towards playing the hand**, which is the conservative
direction for a rung claiming the money is worth chasing: a better exchange rate would move the
crossover **down**, not up. And the crossover is nowhere near $5/$1 either way — **at the real
stakes the side bet is not worth one melded card, and the right way to play it is not to play it
at all.** RULES.md §4.4's `DERIVED` remark said the money layer is *"not meant to be played for,
only settled"*; this is that claim measured.

```bash
# the sweep, on its own — writes docs/strategy/money.csv
dotnet run -c Release --project BurmesePoker.Sim -- money --games 8000 --seed 20260819
```

⚠️ **`--challenger` and `--reference` are no longer typed into that command** (P23). The
challenger is whichever rung declares itself ranked on money and the reference is the top of the
ladder, both read off `BotCatalog` — because a second stakes-reading rung added to the catalog
and measured by nothing is the failure this whole section exists to make impossible.

---

## 11. Regenerating this document's data

```bash
# the standing set — writes docs/strategy/measurements.csv. ⚠️ About five hours.
# No --strategies: the field is a filter of BotCatalog, and between the ladder tournament
# and the money sweep every rung there is gets measured exactly once.
dotnet run -c Release --project BurmesePoker.Sim -- suite --games 8000 --seed 20260819

# the full report the tables above are drawn from
dotnet run -c Release --project BurmesePoker.Sim -- tournament --games 8000 --seed 20260819 --csv tournament.csv

# one ordinary run, now with an interval on every figure
dotnet run -c Release --project BurmesePoker.Sim -- --strategies greedy,simple --seating balanced --games 4096 --seed 20260819

# what a rung costs, in rounds a second and microseconds a turn (P21's budget is 10x greedy)
dotnet run -c Release --project BurmesePoker.Sim -- bench --rounds 200

# the difficulty dial on its own — §9
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --games 8000 --seed 20260819

# the money sweep on its own — §10
dotnet run -c Release --project BurmesePoker.Sim -- money --games 8000 --seed 20260819 --csv docs/strategy/money.csv
```

`measurements.csv` carries, for every row, the **command that produced it**, the games it came
from, the seed, the mean, the standard error and the 95% half-width. **Every table above is
generated from it**, including §10's — the money sweep is part of the standing set, so a single
`sim suite` regenerates the whole document's data.

### The rule that keeps this file honest

🔥 **A rung cannot be added without being measured, and that is a test rather than a default.**
It took three packets to get there and each one closed a different half:

- **P18** made a bot named once, in `BotCatalog`, so a rung reaches every front end and the
  harness at the same moment.
- **P20** made `--strategies` *default* to the catalog for both `tournament` and `suite`. Until
  then the field was written out in three places, and adding `counting` made all three wrong at
  once.
- **P23** made it an assertion. A default is not a guarantee: nothing failed if a rung was added
  and the document was never regenerated, and nothing would fail if a future caller wrote the
  field out again. `StandingAnswerTests` now demands that **every rung in `BotCatalog` is the
  subject of a published row**, that **the ladder and the sweep between them are exactly the
  catalog**, and — separately — that **the difficulty levels this file publishes are the levels
  the two front ends offer, at the ε values they offer them at**. A level whose rate is moved
  without re-running the suite is a red build.

⚠️ **What is still a habit** is that a *new kind of measurement* has to be added to `Suite.Run`
by hand, and that P12's headline row is deliberately `greedy` against `simple` rather than the
field.
