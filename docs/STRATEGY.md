# Strategy — what actually works

**The standing answer to *which way of playing is better, by how much, and how sure are we*.**

Companion to `RULES.md`, and held to the same discipline. `RULES.md` tags every rule with its
provenance because a rule without a source cannot be re-examined; **a measurement without its
origin is worse, because it looks like a fact**. So every number below is *generated* rather than
transcribed, out of `docs/strategy/measurements.csv`, which is written by one command:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- suite --games 8000 --seed 20260819
```

Last generated **2026-08-22** (BUILD-PLAN P31). **116 measurements in 11,020 s — three hours** —
the ladder ranked, the difficulty dial calibrated beside it, the money sweep, P12's headline under
both seatings, how long a round runs and what the claim's permission is worth, and, new in this
run, **`warden` and how often the feeding ban actually bites** (§13).

✅ **Everything here is measured under the rules as they actually are** — `RULES.md` rev 24, with
P25's win condition, P26's money layer, P27's feeding ban and P28's claim permission all in force.
P29 was the first regeneration under them; this is the second, and no rule moved between the two.

⚠️ **`RULES.md` is rev 26 as of 2026-08-22 and these figures still stand**, because neither rev 25
nor rev 26 changed play: rev 25's three expert answers all confirmed the standing default (§9 #19,
#27, #32), and rev 26 corrected an unbuilt scoring rule. 🔥 **But that rule is §7.3 — a bonus on the
winning prize for declaring *jokerless*, ×2 at two, three or four seats and ×3 at five or more — and
nothing implements it. Building it will invalidate every number in this document more thoroughly
than P25–P28 did.** Those packets changed what a winning hand *is*; this changes what winning is
*worth*, and it hands every rung a reason to throw a joker away that none of them currently has —
`CoverScore.Potential` returns `int.MaxValue` for a joker, so **every figure below is measured in a
world where no reason to part with one exists.**
⚠️ **And the bonus is worth most at five seats**, where §7.1.1 requires nothing clean — so it is the
*only* thing cleanliness is ever worth there. **Do not plan a five-handed regeneration (P32) around
these figures**; P33 must land first, and the two regenerations should be one.

🔥 **71 of the 88 rows that both runs contain came back byte-identical, and the 17 that moved are
exactly the rows a new rung must move.** Every head-to-head cell among the six older rungs, every
pairing ratio, **the whole difficulty dial and the whole money sweep** reproduced to the digit. The
17 are the free-for-all column (a different table — seven rungs crossed, not six), the mean-margin
ranking (over 21 comparisons, not 15), and the four ladder-scope statistics computed off the
free-for-all cell. ⚠️ **Contrast P29, which reproduced 4 of 91** — because P29 ran across a rules
change and this one did not. **"Does it reproduce" is a question with an answer again**, and the
answer is the strongest this document has carried: P23's best was 59 of 77.

**What P31 predicted, and it got the headline backwards.** `BUILD-PLAN.md` §5 P31 wrote three
predictions down before the run so that the packet could be wrong.

| Prediction | Outcome |
|---|---|
| **1. `warden` is a null or close to one** — denial has measured nothing twice, and the apparatus floor is about a point. | ❌ **Wrong, and not by a little.** It is **−9.3 ± 1.0 against `outs`** and about six points behind `greedy`, `cautious` and `counting` — the largest separated *loss* in this document. §3, §8. |
| **2. If it is a null, the mechanism is that the lock is cheap to escape** rather than that denial is worthless. | ⚠️ **The escape hatch is closed.** §5.1 takes the card a seat meant to throw on **9.4% of every turn** in the crossed field, and a lock is live on **30.5%**. The rule bites hard and the rung still loses. §13. |
| **3. `warden` should meld worse than `outs`**, because it takes cards for a reason other than its own hand. | ✅ **Consistent with everything measured.** An all-`warden` table runs **31.9 turns a round against `outs`' 24.1** (`sim bench`): it converts about a third of its draws into locks, and a draw is the only thing that improves a hand. |

🔥 **Prediction 1 being wrong is the packet's result, and prediction 2 is why it is a finding
rather than a shrug.** Had the mechanism variable not been built, `warden`'s loss would have been
open to the reading *"the ban does nothing, so a rung built on it can do nothing"*. §13 forecloses
that: **the ban is one of the most active rules in the game.** What `warden` gets wrong is the
*price* — it declines any lock that would cost it a melded card, and then pays for every lock it
takes with a **draw**, which nothing in its rule prices at all.

⚠️ **The suite is now a three-hour job**, 11,020 s against P29's 9,981 s. **A seventh ranked rung
is 21 head-to-head cells rather than 15** — 40% more — and it cost only 10% more wall clock,
because `warden` is *cheaper per turn* than `outs` (5.6× a `greedy` round against 6.1×; its
candidate set is smaller) even though its rounds are a third longer. **`--pairs adjacent` exists**
(P19) if it ever stops finishing, but that would throw away the matrix in §3, which is the
document's centre.

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
5. ⚠️ **A round-robin manufactures findings, so the family is corrected.** Seven rungs is
   **twenty-one** comparisons, and twenty-one 95% intervals means it is more likely than not that
   some pair clears zero by luck. Every margin below carries a **Holm–Bonferroni** verdict beside
   the raw one, and *a raw "separated" that does not survive is not a finding.* ⚠️ **The family
   grows quadratically with the field**, so a rung added makes every existing verdict harder to
   reach — which is the cost §2 weighs when it keeps `prospector` out of §3's matrix.

---

## 2. The ladder

Eight rungs, each differing from the rung it hangs off in **exactly one decision** — which is what
makes a difference in results attribute to that decision and to nothing else.

| rung | what it decides differently | packet |
|---|---|---|
| `random` | nothing. A legal move, chosen arbitrarily. The floor. | P15 |
| `simple` | throws whatever costs it the fewest melded cards. | P12 |
| `greedy` | `simple`, plus a tie-break towards the cards worth keeping. | P10 |
| `cautious` | `greedy`, plus the remaining ties decided by what the discard is worth to whoever picks it up. | P15 |
| `counting` | `cautious`, plus a **memory**: what is left in the shoe is estimated from every card it has been shown this round, not from its own thirteen. | P20 |
| `outs` | `greedy`, plus a **look ahead**: where the cover count ties, keep the thirteen that more of the values still out there would improve. | P21 |
| `warden` | `outs`, plus the only decision that is about somebody else's hand: take a card you do not want when it **closes that rank** against the seat that threw it (RULES.md §5.1), and then hold the rank rather than throwing it back. | P31 |
| `prospector` | `outs`, plus the only decision money may touch: take a card from anywhere but the deck only when it is worth more than the ownership a blind draw confers. ⚠️ **At $5/$1 that is never, so it is `outs` — see §10.** | P22 |

🔥 **It stopped being a ladder at P31 and became a tree, and that is worth saying plainly.** Every
rung up to `outs` is one change from the rung above it in this list, so *"in the order it was
built"* and *"weakest first"* were the same order for six rungs running. `warden` and `prospector`
are **both** one change from `outs` — two branches, not a seventh and eighth step — so the last
entry in the list is simply the branch written last. ⚠️ **A test had been asserting the
coincidence as a law** (`StandingAnswerTests` demanded that the ladder's last entry *be* the
strongest rung); it now asserts what was actually meant, which is that the strongest rung is one
the ladder tournament ranks.

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

⚠️ **Seven rungs and not the catalog's eight**: `prospector` is settled by §10 rather than here,
for the reason §2 gives. 🔥 **Its absence costs this matrix nothing, and that is measured rather
than argued** — the run that produced these figures had `prospector` in the catalog and out of
the field, and **every cell not involving `warden` came back byte-identical to the six-rung run
before it** (§11).

**The row's win rate less the column's, in points, game by game:**

| | `random` | `simple` | `greedy` | `cautious` | `counting` | `outs` | `warden` |
|---|---:|---:|---:|---:|---:|---:|---:|
| **`random`** | · | −49.8 ± 0.4 \* | −49.7 ± 0.4 \* | −49.7 ± 0.4 \* | −49.7 ± 0.4 \* | −49.8 ± 0.4 \* | −49.4 ± 0.4 \* |
| **`simple`** | +49.8 ± 0.4 \* | · | −9.2 ± 1.0 \* | −8.8 ± 1.0 \* | −9.9 ± 1.0 \* | −13.8 ± 1.0 \* | −2.5 ± 1.0 \* |
| **`greedy`** | +49.7 ± 0.4 \* | +9.2 ± 1.0 \* | · | +0.1 ± 1.0 | +0.4 ± 1.0 | **−3.0 ± 1.0** \* | **+6.2 ± 1.0** \* |
| **`cautious`** | +49.7 ± 0.4 \* | +8.8 ± 1.0 \* | −0.1 ± 1.0 | · | +0.5 ± 1.0 | **−3.0 ± 1.0** \* | **+6.1 ± 1.0** \* |
| **`counting`** | +49.7 ± 0.4 \* | +9.9 ± 1.0 \* | −0.4 ± 1.0 | −0.5 ± 1.0 | · | **−3.3 ± 1.0** \* | **+6.2 ± 1.0** \* |
| **`outs`** | +49.8 ± 0.4 \* | +13.8 ± 1.0 \* | **+3.0 ± 1.0** \* | **+3.0 ± 1.0** \* | **+3.3 ± 1.0** \* | · | **+9.3 ± 1.0** \* |
| **`warden`** | +49.4 ± 0.4 \* | +2.5 ± 1.0 \* | **−6.2 ± 1.0** \* | **−6.1 ± 1.0** \* | **−6.2 ± 1.0** \* | **−9.3 ± 1.0** \* | · |

\* survives Holm at α = 0.05 over the family of twenty-one.

| # | strategy | mean margin over the field | free-for-all win % | beat / lost / undecided |
|---|---|---:|---:|---:|
| 1 | `outs` | +13.7 | 34.8 ± 1.1 | **6 / 0 / 0** |
| 2 | `greedy` | +10.4 | 30.6 ± 1.1 | 3 / 1 / 2 |
| 3 | `cautious` | +10.3 | 30.8 ± 1.1 | 3 / 1 / 2 |
| 4 | `counting` | +10.3 | 30.0 ± 1.1 | 3 / 1 / 2 |
| 5 | `warden` | +4.0 | 25.6 ± 1.1 | 2 / 4 / 0 |
| 6 | `simple` | +0.9 | 23.0 ± 1.0 | 1 / 5 / 0 |
| 7 | `random` | −49.7 | 0.0 ± 0.1 | 0 / 6 / 0 |

🔥 **`warden` is the largest negative result this programme has produced, and it lands in the
middle of the ladder.** It plays `RULES.md` §5.1 *offensively* — it will take a card it does not
want in order to close that rank against the seat that threw it — and it is **−9.3 ± 1.0 against
`outs`, the rung it is one change from**, and **about six points behind each of `greedy`,
`cautious` and `counting`**. It beats only `simple` and `random`. **Every one of those six margins
survives the correction over a family of twenty-one.** ⚠️ **The packet predicted a null** and
wrote the prediction down first (`BUILD-PLAN.md` §5 P31); this is not a null, it is the biggest
separated loss in the document. **§13 is where the *why* is**, and the why is measured rather than
guessed.

⚠️ **Nothing in this table is comparable with a run from before 2026-08-21** — P25–P28 changed
what winning a round means, what a money card pays, which cards may be thrown and who may stop a
claim. The numbers below are the game as `RULES.md` rev 24 describes it, and the previous run's
figures are quoted here only where a comparison is the finding.

🔥 **`outs` is the first rung that beats `greedy`, and it beats everything: 5 / 0 / 0.** Where two
discards leave the hand equally melded it keeps the thirteen that **more of the values still out
there would improve** — `+3.0 ± 1.0` over `greedy`, `+3.0 ± 1.0` over `cautious`, `+3.3 ± 1.0`
over `counting`, all three surviving the correction over a family of twenty-one. It is the only
figure in this document that separates two *thinking* rungs.

🔥 **And it is the one margin the new win condition did not touch — which is P29's wrong
prediction, and worth more than the two right ones.** `BUILD-PLAN.md` §5 P29 predicted `outs`
would *narrow* against `greedy`, on the reasoning that every rung in `BotCatalog` maximises cover
count and cover count is no longer sufficient to win at four seats (`RULES.md` §7.1.1). It came
back **+3.0 against +3.1, inside a tenth of a point of an interval of one.** ⚠️ **What the new
condition actually cost is `simple`'s opponents, not `outs`' margin**: the gaps from `simple` to
the three middle rungs closed by about two points each while the top margin held. **A requirement
nobody optimises for is paid by everybody who could otherwise have optimised past it**, so it
compresses a ladder from the bottom rather than tilting it.

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

🔥 **`greedy` and `cautious` are still `+0.1 ± 1.0` apart, and `counting` is still inside the
interval against both.** Three rungs, one level, four packets of measurement — and **the sign of
that margin has now flipped twice without the number ever leaving its interval.** ⚠️ **Note what
their free-for-all order does here**: `greedy` 30.7, `counting` 30.6, `cautious` 30.1 — a
0.6-point spread among three players nothing can separate head to head. **That column is noise at
this resolution and §4 says so.**

**The family, and what the correction cost:**

| comparison | margin | p | Holm threshold | raw | corrected |
|---|---:|---:|---:|---|---|
| `random` vs `cautious` | −49.7 ± 0.4 | < 1e-300 | 0.00238 | separated | **survives** |
| `random` vs `counting` | −49.7 ± 0.4 | < 1e-300 | 0.00250 | separated | **survives** |
| `random` vs `greedy` | −49.7 ± 0.4 | < 1e-300 | 0.00263 | separated | **survives** |
| `random` vs `outs` | −49.8 ± 0.4 | < 1e-300 | 0.00278 | separated | **survives** |
| `random` vs `simple` | −49.8 ± 0.4 | < 1e-300 | 0.00294 | separated | **survives** |
| `random` vs `warden` | −49.4 ± 0.4 | < 1e-300 | 0.00313 | separated | **survives** |
| `simple` vs `outs` | −13.8 ± 1.0 | 3.2e-167 | 0.00333 | separated | **survives** |
| `simple` vs `counting` | −9.9 ± 1.0 | 9.6e-84 | 0.00357 | separated | **survives** |
| `simple` vs `greedy` | −9.2 ± 1.0 | 8.5e-73 | 0.00385 | separated | **survives** |
| `outs` vs `warden` | **+9.3 ± 1.0** | 1.7e-72 | 0.00417 | separated | **survives** |
| `simple` vs `cautious` | −8.8 ± 1.0 | 1.9e-66 | 0.00455 | separated | **survives** |
| `counting` vs `warden` | **+6.2 ± 1.0** | 2.2e-33 | 0.00500 | separated | **survives** |
| `greedy` vs `warden` | **+6.2 ± 1.0** | 3.2e-33 | 0.00556 | separated | **survives** |
| `cautious` vs `warden` | **+6.1 ± 1.0** | 1.3e-32 | 0.00625 | separated | **survives** |
| `counting` vs `outs` | −3.3 ± 1.0 | 9.0e-11 | 0.00714 | separated | **survives** |
| `greedy` vs `outs` | −3.0 ± 1.0 | 4.6e-09 | 0.00833 | separated | **survives** |
| `cautious` vs `outs` | −3.0 ± 1.0 | 8.5e-09 | 0.01000 | separated | **survives** |
| `simple` vs `warden` | −2.5 ± 1.0 | 1.3e-06 | 0.01250 | separated | **survives** |
| `cautious` vs `counting` | +0.5 ± 1.0 | 0.30 | 0.01667 | inside | does not survive |
| `greedy` vs `counting` | +0.4 ± 1.0 | 0.39 | 0.02500 | inside | does not survive |
| `greedy` vs `cautious` | +0.1 ± 1.0 | 0.81 | 0.05000 | inside | does not survive |

⚠️ **The `p` and `threshold` columns are recomputed here from the mean and standard error the CSV
carries**, by the two-sided normal test and the Holm ladder the harness uses; **the `corrected`
column is the CSV's own verdict**, and the two agree on all twenty-one rows.

🔥 **Adding a losing rung made every surviving verdict harder to reach and changed none of them.**
The family went from fifteen comparisons to twenty-one, so the strictest threshold tightened from
0.00333 to 0.00238 and **every row's threshold moved**; the three margins that did not survive
before still do not, and the twelve that did still do. ⚠️ **That is the cost §2 warns about paid in
the direction nobody minds** — `warden`'s six cells are real comparisons rather than duplicates, so
unlike `prospector`'s they buy something for what they charge.

⚠️ **Nothing was demoted here, and that is not an argument for dropping the correction.** The
same run puts `greedy` 0.6 points ahead of `cautious` in the free-for-all and 0.1 points ahead of
it head to head — where the run before it put those two figures 1.4 points apart and *pointing
opposite ways*. **A reader who ranks point estimates would have read a rung out of two coin
flips**; the corrected verdict is what says not to. 🔥 **And the family's internal order changed
under P29 without a single verdict changing** — `simple` vs `outs` is now the sixth-strongest
margin rather than the ninth, so four rows swapped Holm thresholds. **A threshold is a property
of the family, not of the comparison**, which is exactly why the correction is recomputed rather
than stored.

---

## 4. Everybody at one table

The fully crossed table — every assignment of the field across four seats. Seven rungs is
`7⁴ = 2,401` seatings, so 8,000 games rounds up to **9,604**, and a given strategy is at the table
in about 4,420 of them.

| strategy | win rate |
|---|---:|
| `outs` | 34.8 ± 1.1 |
| `cautious` | 30.8 ± 1.1 |
| `greedy` | 30.6 ± 1.1 |
| `counting` | 30.0 ± 1.1 |
| `warden` | 25.6 ± 1.1 |
| `simple` | 23.0 ± 1.0 |
| `random` | 0.0 ± 0.1 |

⚠️ **Every figure here moved when P21 added a rung, and none of that was a change in play.** Six
strategies crossed over four seats is a different field from five, so each is at the table less
often and against a stronger average opponent. **This is the column to distrust when a rung is
added; §3's margins are the column that survives it.**

🔥 **P20's run of `greedy` / `counting` / `cautious` in a five-rung field had them 32.8 / 33.5 /
34.3; P23's six-rung run had them 31.5 / 31.2 / 30.1; P29's had them 30.7 / 30.6 / 30.1; this one
has them 30.6 / 30.0 / 30.8.** **Four orderings of three players, and their head-to-head margins
have never left an interval of one point.** ⚠️ **A column that has now re-ranked the same three
rungs three times is not a ranking.**

🔥 **P31 is the cleanest demonstration of that this document has, because nothing about those
three rungs changed.** `warden` was added to the field and to nothing else; every head-to-head
cell among the older rungs came back **byte-identical** (§11), and this column re-ordered anyway.
**A crossed field's win rate is a statement about the field, and §3's matrix is the statement
about the players.**

⚠️ **`warden` at 25.6 is the one figure here that is not noise.** It is five points below the
three middle rungs and 2.6 above `simple`, which is where §3's matrix puts it too — a rung far
enough from its neighbours that both columns agree.

⚠️ **This answers a different question from §3 and the two are not interchangeable.** A
free-for-all win rate depends on *who else is in the field*; a head-to-head margin weights every
opponent once. P16 is the packet that learned they differ, and both are reported for that reason.

---

## 5. The seating matters, and by how much

P12's headline — `greedy` against `simple` at four seats — under both seating schemes, at the
same seed:

| seating | greedy | simple | gap |
|---|---:|---:|---:|
| rotated `[g,s,g,s]` (8,000 games) | **30.43 ± 0.53** | 19.57 ± 0.53 | 10.9 |
| balanced, all 16 assignments (7,500 games) | **29.00 ± 0.47** | 21.00 ± 0.47 | 8.0 |

⚠️ **The rotation flatters `greedy` by 1.43 points a seat — about a sixth of the gap** — because
it seats every `greedy` downstream of a `simple`. It is not wrong: it answers *"what happens at
that table"*. **The honest strategy-vs-strategy figure is the balanced one.**

⚠️ **Both gaps narrowed under P25's win condition** (12.0 → 10.9 rotated, 9.5 → 8.0 balanced),
which is §3's levelling effect showing up in the one measurement this project has published
longest. 🔥 **The seating bias itself did not narrow with them** — it went 1.25 → 1.43 points —
so *how much the rotation flatters `greedy`* is not simply a fraction of *how much better
`greedy` is*. **Two effects that had looked proportional for three packets came apart the moment
something moved one of them.**

**And who sits *where* is worth nothing** (P16, 64,000 games with a downstream control arm):
upstream skill is worth **+9.1 ± 2.1** points across the `random`-to-`greedy` gulf and
**−1.0 ± 2.1** across the gap between two thinking players. *A weaker player anywhere at your
table is worth four or five points to you; which side of you they sit on is worth nothing,
unless they are not really playing.*

---

## 6. The harness measuring itself

**The null test.** One rung against a copy of itself under another name, 8,008 games. Two labels
of one strategy differ in *nothing*, so any gap between them is the apparatus — a seat that opens
first, a seating that is not balanced, an estimator that counts seats as trials.

| | win rate | seats sat in |
|---|---:|---|
| `warden` | 25.4 ± 0.5 | 4004 / 4004 / 4004 / 4004 |
| `warden#mirror` | 24.6 ± 0.5 | 4004 / 4004 / 4004 / 4004 |
| **margin** | **+0.9 ± 1.0** | inside the interval |

A fair four-seat table gives each 25.0%. ✅ **It holds, and `sim suite` exits non-zero if it ever
stops holding.**

⚠️ **The subject changes with the field, and P31 is where that stopped being tidy.** P17 ran this
on `cautious`, P20 on `counting`, P23 and P29 on `outs` — the cell is played by the *last* rung the
catalog names, which for six rungs running was also the strongest. `warden` and `prospector` both
hang off `outs`, so the ladder became a tree and **the null cell changed hands to `warden` without
anybody choosing it**. 🔥 **It is left that way deliberately.** The cell's whole claim is that
*any* strategy against a copy of itself wins 1/n — a statement about the apparatus and not about
the rung — so a null test that depended on who played it would itself be the finding. **It held on
`outs` at +0.5 ± 1.0 and holds on `warden` at +0.9 ± 1.0**, which is a small piece of evidence for
the harness rather than against it.

🔥 **And the two rungs it has run on most recently are the two that could have broken it.** `outs`
carries a **cache across deals**, and `warden` carries a **memory of every card it has been
shown** — both exactly the kind of state that could make a game depend on the order the harness
happened to schedule it in. Neither does: the cache is keyed on card *values* rather than on a
round's shoe, and the memory is wiped by the deal. **This is the run that says so at scale rather
than in a unit test.**

**What pairing is worth, and which way it points.** The per-game difference against the
add-the-variances formula, on identical data:

| scope | comparison | paired SE ÷ independent |
|---|---|---:|
| within-cell | `cautious` vs `counting` | **1.4142** |
| within-cell | `greedy` vs `cautious` | **1.4142** |
| within-cell | `greedy` vs `counting` | **1.4142** |
| within-cell | `cautious` vs `outs` | **1.4138** |
| within-cell | `greedy` vs `outs` | **1.4138** |
| within-cell | `counting` vs `outs` | **1.4137** |
| within-cell | `outs` vs `warden` | **1.4101** |
| within-cell | `simple` vs any thinking rung | 1.405–1.414 |
| within-cell | `random` vs anything | 1.009–1.011 |
| across-cells | `greedy`: vs `cautious` less vs `counting` | **0.56** |
| across-cells | `simple`: vs `greedy` less vs `cautious` | **0.61** |
| across-cells | `cautious`: vs `counting` less vs `outs` | 0.75 |
| across-cells | `random`: vs `simple` less vs `greedy` | 0.78 |
| across-cells | `counting`: vs `outs` less vs `warden` | 0.81 |
| across-cells | `outs`: vs `random` less vs `simple` | 0.92 |
| across-cells | `warden`: vs `random` less vs `simple` | 0.98 |

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
  correlation is positive, and pairing **narrows** — down to 0.56. That is free variance
  reduction, and it is what common random numbers are for.

✅ **This is the one part of the document P25–P28 left alone, and that is a result about the
harness rather than about the game.** Every within-cell ratio is still √2 to four digits and every
across-cells ratio moved by at most four hundredths. **The correlation structure is a property of
"exactly one seat declares" and of the shared shoe** — neither of which any of the four rules
changes touched — so a run that moved 87 of 91 published numbers moved this table hardly at all.

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
where to look for the next one.** `outs` came in at `+3.0 ± 1.0` — three times the half-width, at
the ordinary run size, with no special design, and **it came back at three times the half-width
again under a changed win condition** (P29). ⚠️ **Read that beside `cautious`'s half a point
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
| `cautious` | **nothing** — `+0.1 ± 1.0` against `greedy` | below |
| `counting` | **nothing**, and pointing the wrong way — `+0.4 ± 1.0` to `greedy` | below |
| `outs` | **`+3.0 ± 1.0` over `greedy`**, and it beats the whole field | §3 |
| `warden` | **worse than nothing** — `−9.3 ± 1.0` against `outs` and about six points behind three more rungs | below, and §13 |
| `prospector` | **nothing at the stakes this is played for**, `+14.6 ± 4.5` a round at $5/$40 | §10 |
| `outs/refuse` vs `outs/allow` | **nothing** — `+0.4 ± 1.0` for refusing a claim | §12 |

⚠️ **Three of the five research rungs are entries below, one is in §3, one is in §10 and P29 added
a null in §12 that is not a rung at all.** Read `cautious` and `counting` below with this in mind:
**both of them put their new idea underneath `CoverScore.Potential` and `outs` put its above it**,
and that is the only structural difference between them.

🔥 **`warden` is a fourth kind of answer and the first rung that actively lost.** `cautious` and
`counting` measured *nothing*; `warden` measured **six to nine points of harm**, separated under a
correction over twenty-one comparisons. ⚠️ **A rung that loses that clearly is a stronger result
than a null**, because a null leaves open whether the instrument could see the effect and this does
not.

🔥 **And `prospector` is a fifth kind of answer, which is why it has a section rather than a
bullet.** It did not lose. It asked a question the other four never asked — *what is the side bet
worth?* — and got back **a function of the stakes** rather than a number: literally the same
player as `outs` at $5/$1, `+7.34 ± 3.29` a round at $5/$40. ⚠️ **A rung can fail to be
interesting at the stakes the game is played for and still be the right thing to have built**,
because the null is what settles the question. RULES.md §4.4 had a `DERIVED` remark saying the
money layer is *"not meant to be played for, only settled"*; §10 is that remark measured, and it
is now the reason nobody has to wonder.

- **`cautious` — throw the card least useful to the player you feed.** Worth `+0.5 ± 0.55`
  points (P15, 32,000 games), `−0.2 ± 1.0` at P23 and `+0.1 ± 1.0` here; **not separated from
  `greedy` under any design yet run, and the sign has now changed twice without the number ever
  leaving its interval.** ⚠️ It also could not be shown to *deny* anybody anything: sitting upstream of a focal
  seat it cost that seat `−0.3 ± 1.4` points and cost itself nothing (P16). **Denial and
  self-interest point the same way**, and what is left over is only what a hand cannot influence
  — how many runs a rank sits in at all, and second-order blockages.
- **`counting` — remember every card you have been shown.** Worth **`+0.4 ± 1.0` points to
  `greedy`**, i.e. not separated and pointing the wrong way; `cautious` is `+0.5 ± 1.0` ahead of
  it (8,008 games a cell). 🔥 **It is not that the memory does not work — it does, and a
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
  `cautious`'s tie-break — and §3 already measured that tie-break at `+0.1 ± 1.0`. **A sharper
  input to a decision rule worth nothing is worth nothing**, and the two nulls compound rather
  than add. ⚠️ **The lesson for P21 and P22: the next rung must change *which question is asked*,
  not improve an answer to a question already shown not to matter.**
  ✅ It costs nothing to run — **77 rounds/s against `cautious`'s 76 and `greedy`'s 88** (P12's
  baseline: 51 serial, 85–92 parallel), so the memory is free and only the idea is not.
- **`warden` — play the feeding ban at somebody.** Worth **`−9.3 ± 1.0` points against `outs`**,
  and `−6.2 / −6.1 / −6.2` against `greedy`, `cautious` and `counting`; it beats `simple` by
  `+2.5 ± 1.0` and nothing else. **All six margins survive Holm over a family of twenty-one**
  (8,008 games a cell). 🔥 **It is the only rung whose failure has a measured mechanism rather than
  an argued one**, and §13 is that measurement: the rule it plays **bites on 9.4% of every turn in
  the field**, so the idea was not starved of opportunity. ⚠️ **What it got wrong is the price, and
  the price is in the wrong currency.** It refuses to buy a lock that would cost it a *melded
  card* — and then pays for every lock it does buy with a *draw*, which is the only thing that
  improves a hand and which nothing in the rule prices. Measured, an all-`warden` table runs
  **31.9 turns a round against `outs`' 24.1** (`sim bench`): about a third of its draws have become
  locks. **A successor rung has to price the draw**, which `prospector` already does in money
  (`MoneyOdds.PerBlindDraw`) and nothing yet does in cards.

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

✅ **Re-fitted on 2026-08-20 against `outs`, and re-measured unchanged on 2026-08-21 under
P25–P28.** P19 placed these four against `greedy`; P21 promoted `outs` (§3) and re-based every
level onto it without re-spacing them, which left a dial that was still ordered and no longer
evenly spaced. **One value moved and three did not**: `hard` went from ε = 0.5 to **ε = 0.4**,
taking the reference table from steps of **8.2 / 4.3 / 10.3** points to 7.9 / 6.7 / 7.7. ✅ **P29
then changed the game underneath it and re-measured; the four ε values are the only rows in this
whole document that came back byte-identical**, and the steps they produce are now **8.1 / 7.9 /
7.1**. ⚠️ **They are byte-identical because a human chose them, not because anything reproduced**
— the win rates they produce all moved.

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
| `expert` | **36.74 ± 0.87** | — |
| `hard` | **28.67 ± 0.84** | −8.1 |
| `medium` | **20.84 ± 0.78** | −7.9 |
| `easy` | **13.75 ± 0.68** | −7.1 |

### The steps, head to head

**The family is the three adjacent steps and not the round-robin**, because *"n+1 beats n"* is
the only claim a monotone dial makes — correcting over all six pairs would throw power away for
comparisons nobody is making. Each cell is 8,008 games at every seating in which both levels are
at the table, and each margin is the **paired** one (§1 rule 4).

| step | margin | Holm |
|---|--:|---|
| `expert` over `hard` | **+7.03 ± 1.01** | separated |
| `hard` over `medium` | **+9.33 ± 1.01** | separated |
| `medium` over `easy` | **+11.06 ± 1.00** | separated |

✅ **Every step clears the correction, and the narrowest of them is 7.0× the half-width.** A level
that could not be separated from its neighbour would be deleted rather than shipped (§3.12
item 2) — that is why there are four and not five.

🔥 **This is P29's prediction 1, and it came out right in a way worth stating precisely.** The
prediction was that the dial *survives* P25–P28, on the ground that ε is a property of the mistake
rather than of the rung, and that RULES.md §5.1 filters the runner-up as well as the winner so a
mistake stays a legal move. ⚠️ **The stated risk was that §5.1 makes the ranking a level slips
down *shorter*** — where the feeding ban leaves one legal candidate there is no runner-up at all,
so ε does nothing on that turn — **which would have made the dial's steps depend on how often the
ban binds.** They do not, measurably: **no ε moved and every step still separates**, and the
reference table's spacing *improved* from 7.9 / 6.7 / 7.7 to **8.1 / 7.9 / 7.1**. ⚠️ **The ban
binds on about a fifth of turns** (P27), so this is evidence that ε is robust to it and not
evidence that the interaction does not exist.

⚠️ **The two tables above do not agree about which step is the widest, and that is worth
knowing.** At the reference table the steps are 8.1 / 7.9 / 7.1 and the *bottom* one is narrowest;
head to head they are 7.0 / 9.3 / 11.1 and the *top* one is. **They are not the same
measurement**: a head-to-head cell holds two levels and a reference table holds four, and a pair
of strong players plays a different — longer, more thoroughly dealt — round than a pair of weak
ones. The re-fit improved both readings over the P19 values (reference-table spread 6.0 → 1.1 points,
head-to-head spread 5.1 → 4.4), and the rules changes improved the reference table again — spread
**1.0 points** across three steps — which is why the choice between them has still not had to be
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

# and the candidate spacing, checked at the reference table before the full run
dotnet run -c Release --project BurmesePoker.Sim -- --strategies outs@0.9,outs@0.7,outs@0.4,outs@0 --seating balanced --games 8192 --seed 20260819
```

⚠️ **`outs@0.35` is a calibration probe, not a level** — a rung at an arbitrary mistake rate,
nameable by the harness so that the sweep above can be re-run by somebody who doubts it. It is
deliberately unreachable from a menu, a form field or `--difficulty`.

---

## 10. The side bet — should you draw blind for the money?

**The one strategy axis in this game that is not rummy, and the first question in the programme
whose answer is not a win rate** (P22, 32,032 games; re-measured under P25–P28 by P29). ✅
**Generated from `docs/strategy/measurements.csv` like every other section since P23** — the sweep
is part of the standing set, so one `sim suite` regenerates the whole document's data, and
`money.*` are the rows. ❌ **This is the section P29 expected to move most and it did**: three of
its four cells changed verdict or doubled, because `prospector` is the one rung whose decision
reads the money and P26 changed what the money is.

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
| **$5/$1** — as played | 0.2 rounds | 24.3 | **+0.05 ± 0.25** | +0.5 ± 1.0 | inside |
| $5/$10 | 2 rounds | 13.8 | **−0.21 ± 1.15** | −7.9 ± 1.0 | inside |
| $5/$20 | 4 rounds | 0.1 | **+5.32 ± 2.27** | −20.2 ± 0.9 | **separated** |
| $5/$40 | 8 rounds | 0.0 | **+14.63 ± 4.48** | −20.2 ± 0.9 | **separated** |

`outs` takes about 24% of its cards off the pile in every cell, which is the control: the only
thing moving down that column is the rule firing.

🔥 **P29's prediction 3, and it was right by a whole ratio.** P26 made the permanent money cards
eight rather than four and made a designation landing on one pay **×3**, so a blind draw's
ownership is worth more than it was — and the packet predicted the rung would separate below
$5/$40. **It separates at $5/$20 now**, where the same cell was `+0.95 ± 1.63` and *inside the
interval* before. ⚠️ **The mechanism column is what makes this a measurement rather than a
coincidence**: the take rate at $5/$20 collapsed from **8.4% to 0.1%**, so the rule went from
firing half the time to firing essentially always. **The crossover moved from "somewhere near
four rounds" to "at or below four rounds".**

### The answer

🔥 **No — at the stakes this game is actually played for, and P26 did not change that.** At
$5/$1 the rule **never fires at all**: one melded card is worth about a dollar at a four-handed
table and the ownership a blind draw confers is worth pennies, so the comparison is never close.
`prospector` and `outs` are **the same player under two names at $5/$1**, and that is an identity
rather than a measurement — two tables of one rung each, dealt from the same shoes, play the same
rounds card for card. ✅ **P29 re-checked this at 400 games (1,600 seat-rounds) after P26 made the
permanent money cards eight rather than four, and every column of every round is byte-identical**;
`MoneySweepTests.AtTheStandardStakesTheTwoRungsPlayOneGameUnderTwoNames` is the standing
assertion. The top row above is therefore still a **null cell**.

🔥 **Yes — from a money card worth four rounds, which is one ratio lower than it used to be.**
At $5/$20 the rung has already stopped taking (0.1%), wins **20 points fewer rounds**, and banks
**$5.32 ± 2.27 more a round**, surviving Holm; at $5/$40 it banks **$14.63 ± 4.48**, twice what
the same cell paid before P26. Below that, $5/$10 is `−0.21 ± 1.15` and inside its interval —
where before it was separated raw and pointing down. **The order of the four is monotone in the
stakes**, which is the shape the hypothesis predicted and which has now survived a change to the
money layer itself.

⚠️ **Note what happened at $5/$10 and $5/$20 together: the whole curve shifted left, it did not
steepen.** The rung's take rate at $5/$20 went 8.4% → 0.1% while its win-rate cost went −13.2 →
−20.2 points — it is now paying the *full* price of never taking, one ratio earlier. **More money
on the table does not make chasing it cleverer; it makes the same decision fire sooner.**

⚠️ **This is the first published divergence between money and win rate in the programme**, and
the reporting has split the flat prize from the side bet since P12 precisely so it could be seen.
At $5/$40 — **and now at $5/$20 too** — a reader ranking by win rate would rank `prospector` last
and be wrong. 🔥 **The harness says so out loud rather than leaving it to be noticed**: the
`money.win-rate.*` rows carry the verdict *points the other way to the money*, and P29 is the run
where a second cell earned it.

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
# the standing set — writes docs/strategy/measurements.csv. ⚠️ About three hours (11,020 s at P31).
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

✅ **The two corrections review R3 and R13 left owing have arrived** (P31's run, 2026-08-21).
`money.side-margin.*` is in the file for all four ratios — §10's mechanism variable had been
computed on every run since P22 and published nowhere the suite writes — and
`claim.permission.money.refuse-over-allow` is **paired** now, which widened its interval from
`±0.18` to **`±0.25`** exactly as predicted. ⚠️ **Neither was a regression and both change a
published figure**: the money null in §12 is `+0.02 ± 0.25` rather than `+0.02 ± 0.18`, and the
null survives.

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

🔥 **P31 found the same defect one layer above all three of them, in three places at once.** For
six rungs running, *the last rung the catalog names* and *the strongest rung there is* were the
same rung — and three separate pieces of the system had written the coincidence down as a law:
`SuiteOptions.MoneyReference` read `Ladder[^1]` (and would have swept the side bet against
`warden`), `StandingAnswerTests` asserted `Assert.Same(Hardest, Ladder[^1])`, and the tournament's
**null cell** is played by whichever strategy is named last. The first two are fixed to say what
they meant; **the third is left alone on purpose**, because a null test that depended on who played
it would itself be the finding (§6). ⚠️ **The lesson is not about ladders.** It is that a
coincidence which has held for every case so far reads exactly like an invariant, and the way to
tell is to ask what the expression is *for* rather than whether it is currently true.

⚠️ **What is still a habit** is that a *new kind of measurement* has to be added to `Suite.Run`
by hand, and that P12's headline row is deliberately `greedy` against `simple` rather than the
field. 🔥 **P29 is the packet that paid that habit's bill and did not remove it**: round length,
the abandoned count and the claim's refusal rate were all things the harness already knew and
nothing published, and each needed a hand-written block in `Suite.Run` to reach this file. **A
statistic the runner collects and the suite does not publish is invisible in exactly the way a
rung outside the catalog used to be.**

⚠️ **And one habit is genuinely gone.** The tables above are still typed out of
`measurements.csv` by a human rather than generated from it, which is the last transcription step
in the chain — but P29 checked every table it did not change by re-deriving it from the CSV
first, including §3's Holm column, and the derivation agreed with the published text on all
fifteen rows *before* the new run replaced them. **The instrument was validated against the
document before it was used to rewrite it.**

---

## 12. The rounds themselves, and what the claim's permission is worth

**Added by P29, and both halves are things the programme has always been able to measure and
never published.**

### How long a round runs, and whether the rounds finish

| field | turns a round | games abandoned at the turn cap |
|---|--:|--:|
| the ladder, all seven rungs crossed | 28.8 | **0.094%** — 9 games of 9,604 |
| the difficulty dial, all four levels crossed | 30.0 | 0 of 8,192 |
| `outs/refuse` against `outs/allow` | 23.9 | 0 of 8,008 |

🔥 **Why this is here at all, and it is P27's doing.** Every rung's cover count used to be
monotone — *throwing back the card just taken restores the hand exactly* — which is
`GreedyBotAgent`'s own stated reason a table of bots reaches a declaration. **RULES.md §5.1 takes
the just-taken card out of the choice**, so a seat whose only legal discards are melded ones gives
up a meld, and convergence stopped being guaranteed by construction. What stands behind it now is
`SimulationOptions.TurnCap`. ⚠️ **A document that publishes win rates and not whether the rounds
finished is publishing a conditional probability without its condition**, so `sim suite` publishes
both — and `StandingAnswerTests` fails the build if these rows stop being published, or if the
verdict beside the abandoned rate stops matching the number it sits next to. ⚠️ **It does not
assert the count is zero**, because a table that does not converge is a result the document has to
be able to state.

✅ **The abandoned games are `random`'s and not §5.1's.** The only field with a non-zero count is
the one containing `random`, which plays **196 turns to a round** against every thinking rung's 24
to 32 (`sim bench`, 2026-08-21) and has needed a turn cap since P12. **Both all-`outs` fields
settled every game they played.** ⚠️ **That is evidence and not a proof** — 0.094% of a field is
not zero, and the honest statement is that no table of thinking rungs has yet failed to converge,
not that none can.

⚠️ **P31 is the first packet that could have broken this and did not.** `warden` deliberately
declines to throw whole ranks, which is a *self*-imposed narrowing on top of §5.1's, and an
all-`warden` table runs **31.9 turns a round against `outs`' 24.1**. It still settled every game,
and adding it to the crossed field moved the ladder's round length only 28.6 → 28.8 and its
abandoned count from 8 to 9. **The floor holds** — a rung that refuses to throw a rank throws
anyway rather than deadlocking (§5.1's own escape, applied to itself).

⚠️ **The dial's rounds are the longest of the three fields at 30.0 turns, and the reason is not difficulty.** A table
of four levels is a table of four `outs` with mistake rates: a seat that throws its second-best
card is a seat that takes longer to go out, so **a weaker table is a slower one**, and the
strongest and most homogeneous field — two arms of `outs` — is the fastest at 23.9.

### What refusing a claim is worth: nothing

**RULES.md §4.5 gives the seat before the opener a veto over the opener's claim of the turned-up
money card, and only a holder of that rank may use it.** P28 built it and made every rung refuse
whenever it may, reasoning from the rule itself: the claim is a public take, so allowing it closes
that rank against the refusing seat for the rest of the round (§5.1). ⚠️ **That was a decision and
not a derivation** — and what nothing priced is that **refusing is a disclosure**, since only a
holder may refuse. So P29 measured it.

| | value |
|---|--:|
| rounds in which the opener asks for the card | **24.8–28.5%** |
| of those, how often the upstream seat vetoes | **51.7%** at the ladder, **53.7%** at the dial |
| `outs/refuse` over `outs/allow`, win rate | **+0.4 ± 1.0** — inside the interval |
| `outs/refuse` over `outs/allow`, money a round | **+0.02 ± 0.25** — inside the interval |

✅ **The money row is paired now and the correction has landed** (review R3, raised 2026-08-21,
regenerated by P31 the same day). It had been computed with the independent formula on a
*within-cell* margin: the two arms share one table and money is zero-sum across it, which is
exactly the case `Measurement.Paired`'s own remarks call anti-conservative. **The interval went
from ±0.18 to ±0.25 and the mean did not move at all** — `+0.020230` before and after, to six
decimal places. ⚠️ **Widening keeps a null a null**, so the finding is unchanged; what changed is
that the printed interval is no longer understated.

🔥 **A null, published** (the discipline P20 set). Two arms of one rung differing in exactly that
one answer, 8,008 games, and neither the win rate nor the money can tell them apart. ⚠️ **This is
not "the rule does not matter"** — it is *"which way you answer it does not matter, at this
resolution, to a rung that models neither the disclosure nor the lock"*. §7 puts the floor at
about a point of win rate; the effect is somewhere inside that.

✅ **And the branch is real rather than rare, which is what makes the null worth having.** The
opener asks for the card in about a quarter of rounds, and **the seat upstream is holding that
rank about half the time** — so the veto is exercised in roughly one round in seven. **A null over
a branch that never fires would say nothing; this one says the decision P28 took costs nothing
either way.**

⚠️ **The refusal rate in the two-arm cell is 27.7% rather than ~50%, and that is arithmetic, not a
finding**: half the seats in that cell never refuse by construction.

```bash
# the claim's permission, on its own — two arms of one rung
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies outs/refuse,outs/allow --pairs adjacent --games 8000 --seed 20260819
```

⚠️ **`sim suite` plays that as a single cell and this command plays three**, adding a free-for-all
that reproduces the pair and a null cell that reproduces a unit test. **The margin is the same
number from the same seed** — the extra cells cost wall clock and tell you nothing, which is why
the suite calls `Tournament.HeadToHead` rather than `Tournament.Run`.

⚠️ **`outs/refuse` is an experiment's arm and not a rung** — like a calibration probe, it is
nameable by the harness so this figure carries the command that made it, and it is deliberately
unreachable from `BotCatalog`, from `DifficultyLadder` and from every menu. **`outs/refuse` is
exactly what every rung in the catalog already does**, and a test asserts that the two play
identical rounds.

---

## 13. How often the feeding ban actually bites

**Added by P31, and it is the number that made §8's `warden` entry a diagnosis rather than a
shrug.**

🔥 **`RULES.md` §5.1 is the strongest rule this game has about *other people's hands*, and until
P31 nothing had ever measured whether it does anything.** *You may not discard a rank the next
player has taken in the open.* It reaches every seat as an **impossible move** — the card is
simply not among the ones the turn offers — so a seat never reports being blocked and the engine
never records an infraction. **A rule enforced by construction leaves no trace by construction.**

⚠️ **Counting the closed ranks would not answer it.** A rank closed off a card the seat was never
going to throw costs that seat nothing, so a count of locks reports a rule that bites hard and a
rule that does not exist as the same number. **The only honest question is the counterfactual**:
what would this seat have thrown if the ban were not there? Only the player can answer that, so
the harness asks it — `IRanksDiscards.RankDiscards(context, candidates)`, an instrument that
nothing in the engine may call, ranking the *whole hand* beside the ranking of the legal set.

| field | discards chosen | the lock was **live** | of those, the lock **bit** | share of every turn |
|---|---:|---:|---:|---:|
| the ladder, all seven rungs crossed | 275,958 | **30.5%** | **30.8%** | **9.4%** |
| the difficulty dial, all four levels crossed | 246,168 | **26.3%** | **22.3%** | **5.9%** |

*Live* means the ban had removed at least one held card from the choice. *Bit* means the card the
seat would have thrown over its whole hand is not the card it threw over its legal set.

🔥 **So the rule does a great deal.** At a table of thinking players **§5.1 takes the card a seat
meant to play about once every eleven turns**, and a lock is live on nearly a third of all turns.
⚠️ **This is the reading that makes `warden`'s loss attributable.** `BUILD-PLAN.md` §5 P31 wrote
down, before the run, that a null would most likely mean *the lock is cheap to escape* rather than
*denial is worthless* — and named this variable as the way to tell those apart. **The escape hatch
is closed: the locks bite, and the rung still loses six to nine points.** The idea is not starved
of opportunity; it is simply a bad trade at the price `warden` pays for it.

⚠️ **Two things this does not say.** It does not say the bites *cost* the bitten seat anything —
the second-choice discard may be nearly as good, and this counts occurrences rather than damage.
And **the bite rate is a property of the field**: a table of weaker rungs takes fewer cards in the
open, so it arms fewer locks. The dial's 26.3% / 22.3% against the ladder's 30.5% / 30.8% is that
effect, and it is the reason both fields are published rather than one.

⚠️ **The claim-permission cell publishes zeroes for the bite and is not evidence of anything.**
The counterfactual costs a second ranking on every restricted turn, which on an `outs`-family rung
is the expensive thing a turn does, so **the suite buys it in the two crossed cells only** — the
two with a whole field sitting down together. That cell reports its *denominator* honestly (21.5%
of its discards were restricted) because a list length is free.

✅ **The instrument changes no card.** `LockBiteTests` asserts that a run with the counterfactual
on and a run with it off produce byte-identical CSVs — which is what makes the two cells that
bought it comparable with the 108 rows that did not.
