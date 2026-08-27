# Strategy — what actually works

**The standing answer to *which way of playing is better, by how much, and how sure are we*.**

Companion to `RULES.md`, and held to the same discipline. `RULES.md` tags every rule with its
provenance because a rule without a source cannot be re-examined; **a measurement without its
origin is worse, because it looks like a fact**. So every number below is *generated* rather than
transcribed, out of `docs/strategy/measurements.csv`, which is written by one command:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- suite --games 8000 --seed 20260819
```

Last generated **2026-08-25** (BUILD-PLAN P46). **235 measurements in about five hours** on a
24-core workstation (the same-shaped run took five to six hours on the laptop the earlier packets
were timed on — the wall clock is the machine as much as the run) — the ladder ranked, the
difficulty dial calibrated beside it, one money sweep per
money-ranked rung (§10 and §14), P12's
headline at **two** table sizes under both seatings, how long a round runs, what the claim's
permission is worth, how often the feeding ban bites and how often the clean bonus is collected.

🔥 **This document is about a five-handed table now, and that is the whole of P32.**
`SuiteOptions.Seats` read `RoundEngine.MinimumPlayers` until this run — so every figure published
between P12 and P33 was four-handed **because four is the smallest legal table and not because
anybody chose it.** ⚠️ **The two are not interchangeable**: by `RULES.md` §7.1.1 a four-handed
declaration owes a joker-free series and a five-handed one owes **no series at all**, and by §7.3 a
jokerless hand is paid **×2** at four and **×3** at five. **Four-handed is a different game, not an
old version of this one** — so it is kept, whole and frozen, at
`docs/strategy/measurements-4-handed.csv`, and a test asserts every row of it says `seats=4`.

✅ **Measured under the rules as the engine plays them** — `RULES.md` rev 26's play, with P25's win
condition, P26's money layer, P27's feeding ban, P28's claim permission and P33's clean bonus all
in force. ⚠️ **`RULES.md` is at rev 29 as this is written**, and the three revisions since are §7.4,
§7.5 (settlement, unbuilt — P35) and §3's corrected seating (unbuilt — P36). **None of them can
reach a figure here**: the first two are unimplemented, and the third cannot matter because every
experiment runs one round a game.

🔥 **The headline is a negative result about this document's own explanation, and it needed the
right instrument to see.** P29 attributed the four-handed levelling of `simple` to §7.1.1's
joker-free series requirement — *"a requirement nobody optimises for is paid by everybody who could
otherwise have optimised past it, so it compresses a ladder from the bottom rather than tilting
it."* **At five seats that requirement is gone**, so P32 predicted, before the run, that the gap
over `simple` would re-open. ❌ **It did not.**

⚠️ **Reading the raw margins would have said it narrowed, and that would have been wrong too.**
A fifth seat drops the base win rate from 25% to 20%, so **every** margin rescales by 0.800:

| cell | 4-handed | 5-handed | ratio |
|---|--:|--:|--:|
| `random` over each of the other six | −49.4 … −49.8 | −39.6 … −39.8 | **0.800** ×6 |
| `simple` over `greedy` | −9.20 | −7.42 | 0.806 |
| `simple` over `counting` | −9.87 | −7.96 | 0.807 |
| `simple` over `cautious` | −8.80 | −8.06 | 0.916 |

**Median ratio over all eighteen cells: 0.801, against a base-rate scale of 0.800.** The six
`random` rows land on 0.800 to three digits. So `simple`'s gaps to `greedy` and `counting` are
**exactly the rescaling** — pure scale predicts 7.36 and 7.90 and the run measured 7.42 and 7.96,
both within a tenth of a point of an interval of ±0.8. 🔥 **Removing the requirement did nothing,
so the requirement was not what caused the levelling.** `cautious` is the one cell that moved
(predicted 7.04, measured 8.06 — 1.0 point against ±0.8), and one of three barely outside its
interval is not an effect.

✅ **So the five-handed ladder is the four-handed ladder divided by 1.25**, and every Holm verdict
is identical at both sizes: the same eighteen separated, the same three inside. ⚠️ **P29's
explanation does not survive its own test, and what actually causes the levelling is unknown.**

**Four other predictions were written down before the run and all four held.**

| | predicted | measured |
|---|---|---|
| jokerless rate | **falls** — §7.1.1 pushes four-handed play toward clean hands and pushes five-handed play not at all | 15.4% → **12.1%** ✅ |
| what the bonus is worth | **rises** — ×3 to four losers, not ×2 to three | $15 → **$40** over flat ✅ |
| the two together | value wins | **$2.31 → $4.84** a round expected ✅ |
| the null cell | **20%**, not 25% | **20.3 / 19.7** ✅ |

🔥 **So the bonus is collected less often and is worth more than twice as much, and the second
effect is the larger by better than two to one.** ⚠️ **The one prediction about the dial was also
wrong, and in the useful direction** — see §9: the steps were expected to compress with the base
rate and **no ε had to move at all.**

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
   seats**, as large as the whole seating effect in §5. ⚠️ **That figure has not been re-measured
   at five**; the estimator is unchanged and the argument does not depend on the table size. Every figure here is the ratio
   estimator, so it is the same quantity P12, P15 and P16 published, now with an interval.
3. **Every interval is 95% and normal-approximate.** Honest at thousands of games; it would not
   be at a dozen.
4. 🔥 **A margin between two strategies at the same table is computed game by game.** Exactly
   one seat declares a round, so the two players' results are strongly *negatively* correlated
   and the usual add-the-variances formula **understates** the interval — by 41% here (§6).
5. ⚠️ **A round-robin manufactures findings, so the family is corrected.** Ten rungs is
   **forty-five** comparisons, and forty-five 95% intervals means it is more likely than not that
   some pair clears zero by luck. Every margin below carries a **Holm–Bonferroni** verdict beside
   the raw one, and *a raw "separated" that does not survive is not a finding.* ⚠️ **The family
   grows quadratically with the field**, so a rung added makes every existing verdict harder to
   reach — which is the cost §2 weighs when it keeps `prospector` out of §3's matrix.

---

## 2. The ladder

Twelve rungs, each differing from the rung it hangs off in **exactly one decision** — which is
what makes a difference in results attribute to that decision and to nothing else.

| rung | what it decides differently | packet |
|---|---|---|
| `random` | nothing. A legal move, chosen arbitrarily. The floor. | P15 |
| `simple` | throws whatever costs it the fewest melded cards. | P12 |
| `greedy` | `simple`, plus a tie-break towards the cards worth keeping. | P10 |
| `cautious` | `greedy`, plus the remaining ties decided by what the discard is worth to whoever picks it up. | P15 |
| `counting` | `cautious`, plus a **memory**: what is left in the shoe is estimated from every card it has been shown this round, not from its own thirteen. | P20 |
| `outs` | `greedy`, plus a **look ahead**: where the cover count ties, keep the thirteen that more of the values still out there would improve. | P21 |
| `warden` | `outs`, plus the only decision that is about somebody else's hand: take a card you do not want when it **closes that rank** against the seat that threw it (RULES.md §5.1), and then hold the rank rather than throwing it back. | P31 |
| `opportunist` | `warden`, **minus** the paid take: it takes only what improves its hand, exactly as `outs` does, and keeps `warden`'s hold on whatever ranks those ordinary takes happen to close. The lock at zero price. | P43 |
| `prospector` | `outs`, plus the only decision money may touch: take a card from anywhere but the deck only when it is worth more than the ownership a blind draw confers. ⚠️ **At $5/$1 that is never, so it is `outs` — see §10.** | P22 |
| `purist` | `outs`, plus a preference the scoring created: where the melded cards tie, shed a joker — a jokerless declaration pays **×3** here (RULES.md §7.3) and every other rung forfeits it by construction. ⚠️ **It changed about one round in eight thousand — see §14.** | P44 |
| `angler` | `outs`, plus a price on the take itself: a card off the pile costs this turn's blind draw, and the draw is priced in **cards** — live out-cards over cards unseen — so a card that melds nothing is still taken when it opens more doors than the deck was expected to. ⚠️ **The trigger almost never arms — see §8.** | P45 |
| `sprinter` | `outs`, plus a change of objective at the line: within one card of covering it stops keeping the thirteen more of the pack would **improve** and keeps the thirteen more of the pack would **win** — a worse hand in expectation for a faster fuse. 🔥 **The first rung to separate above `outs` — see §3 and §8.** | P46 |

🔥 **It stopped being a ladder at P31 and became a tree, and that is worth saying plainly.** Every
rung up to `outs` is one change from the rung above it in this list, so *"in the order it was
built"* and *"weakest first"* were the same order for six rungs running. `warden` and `prospector`
are **both** one change from `outs` — two branches, not a seventh and eighth step — P43 grew
the `warden` branch a step (`opportunist` is `warden` minus its paid take), P44 hung a third
branch off `outs` (`purist`), P45 a fourth (`angler`), and P46 a fifth (`sprinter`), so the last
entry in the list is simply the branch written last. ⚠️ **And for the first time since P21 the
branch written last is also the strongest**, but that is a fact the measurement establishes (§3),
not one the build order implies. ⚠️ **A test had been asserting the
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
where it is measured. ⚠️ **P44 added a second rung ranked on money, for the other reason there
is**: `purist` reads no stakes at all — it plays the same cards at every ratio, and the run
proves it — but its whole idea is trading rounds for a multiplied prize, so a win-rate ranking
would misjudge it *by construction* rather than by noise. It is measured in §14, beside the
bonus it plays for.

🔥 **Which is why it is not in §3's matrix, and that is a decision rather than an omission.** A
head-to-head cell is played at one stakes; at the stakes this game is played for, `prospector`
and `outs` deal the same rounds card for card (§10), so its ten cells would reproduce an identity
a unit test already asserts. **The run time is the smaller half of the
cost.** The larger half is that ten null cells join the Holm family in §3 and **make every real
verdict in it harder to reach** — a duplicate is not a free row. So each rung declares, in
`Domain/Agents/BotCatalog.cs`, which instrument settles it (`BotRung.Ranked`), the ladder
tournament measures one set and the money sweep the other, and **a test asserts that between them
they are the whole catalog** — so a rung can no more fall out of the programme than it can fail
to appear in it. ⚠️ **The same declaration keeps `purist` out of §3.** The crossing-cap decision
recurs with every win-rate rung, and P46 paid it a third time: `sprinter` is win-rate ranked, so
the free-for-all crossing is `10⁵ = 100,000` and `SeatingPlan.MaximumAssignments` was doubled
`65,536 → 131,072` — a **stated** decision (raise and pay, rather than drop a rung from the
crossed cell or subsample), measured first, full crossing, nothing subsampled (§4). ⚠️ **An
eleventh win-rate rung is `11⁵ = 161,051` and breaks the cap again** — the decision recurs at
whatever packet adds one.

---

## 3. The ranking

**Head to head at every seating in which both are at the table** — 14 of the 16 assignments of
two strategies across **five** seats — **30 of the 32 assignments**, the two homogeneous ones
excluded because a game one of them is not at says nothing about the pair. **8,010 games a cell**,
each strategy sitting in each seat exactly 1,602 times *by construction*.

⚠️ **Ten rungs and not the catalog's twelve**: `prospector` and `purist` are settled by §10 and
§14 rather than here, for the reason §2 gives. 🔥 **Their absence costs this matrix nothing, and
that is measured rather than argued** — the run that produced these figures had both in the
catalog and out of the field, and **every cell among the older nine rungs came back
byte-identical to the run before it** (§11) — P46 repeating P45's, P43's and P31's reproduction
exactly.

**The row's win rate less the column's, in points, game by game:**

| | `random` | `simple` | `greedy` | `cautious` | `counting` | `outs` | `warden` | `opportunist` | `angler` | `sprinter` |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| **`random`** | · | -39.8 ± 0.3 \* | -39.8 ± 0.3 \* | -39.8 ± 0.3 \* | -39.8 ± 0.3 \* | -39.8 ± 0.3 \* | -39.6 ± 0.4 \* | -39.8 ± 0.3 \* | -39.8 ± 0.3 \* | -39.8 ± 0.3 \* |
| **`simple`** | +39.8 ± 0.3 \* | · | -7.4 ± 0.8 \* | -8.1 ± 0.8 \* | -8.0 ± 0.8 \* | -10.8 ± 0.8 \* | -2.8 ± 0.8 \* | -10.1 ± 0.8 \* | -10.8 ± 0.8 \* | -11.5 ± 0.8 \* |
| **`greedy`** | +39.8 ± 0.3 \* | +7.4 ± 0.8 \* | · | +0.0 ± 0.8 | +0.6 ± 0.8 | **-2.7 ± 0.8 \*** | **+4.5 ± 0.8 \*** | **-2.0 ± 0.8 \*** | **-2.6 ± 0.8 \*** | **-3.5 ± 0.8 \*** |
| **`cautious`** | +39.8 ± 0.3 \* | +8.1 ± 0.8 \* | -0.0 ± 0.8 | · | +0.1 ± 0.8 | **-2.9 ± 0.8 \*** | **+4.7 ± 0.8 \*** | **-2.2 ± 0.8 \*** | **-2.8 ± 0.8 \*** | **-3.3 ± 0.8 \*** |
| **`counting`** | +39.8 ± 0.3 \* | +8.0 ± 0.8 \* | -0.6 ± 0.8 | -0.1 ± 0.8 | · | **-2.9 ± 0.8 \*** | **+5.0 ± 0.8 \*** | **-2.1 ± 0.8 \*** | **-2.9 ± 0.8 \*** | **-3.5 ± 0.8 \*** |
| **`outs`** | +39.8 ± 0.3 \* | +10.8 ± 0.8 \* | **+2.7 ± 0.8 \*** | **+2.9 ± 0.8 \*** | **+2.9 ± 0.8 \*** | · | **+7.3 ± 0.8 \*** | **-0.1 ± 0.8** | **-0.6 ± 0.8** | **-1.2 ± 0.8 \*** |
| **`warden`** | +39.6 ± 0.4 \* | +2.8 ± 0.8 \* | **-4.5 ± 0.8 \*** | **-4.7 ± 0.8 \*** | **-5.0 ± 0.8 \*** | **-7.3 ± 0.8 \*** | · | **-6.2 ± 0.8 \*** | **-6.9 ± 0.8 \*** | **-7.4 ± 0.8 \*** |
| **`opportunist`** | +39.8 ± 0.3 \* | +10.1 ± 0.8 \* | **+2.0 ± 0.8 \*** | **+2.2 ± 0.8 \*** | **+2.1 ± 0.8 \*** | **+0.1 ± 0.8** | **+6.2 ± 0.8 \*** | · | **-1.0 ± 0.8 †** | **-1.6 ± 0.8 \*** |
| **`angler`** | +39.8 ± 0.3 \* | +10.8 ± 0.8 \* | **+2.6 ± 0.8 \*** | **+2.8 ± 0.8 \*** | **+2.9 ± 0.8 \*** | **+0.6 ± 0.8** | **+6.9 ± 0.8 \*** | **+1.0 ± 0.8 †** | · | **-1.1 ± 0.8 †** |
| **`sprinter`** | +39.8 ± 0.3 \* | +11.5 ± 0.8 \* | **+3.5 ± 0.8 \*** | **+3.3 ± 0.8 \*** | **+3.5 ± 0.8 \*** | **+1.2 ± 0.8 \*** | **+7.4 ± 0.8 \*** | **+1.6 ± 0.8 \*** | **+1.1 ± 0.8 †** | · |

\* survives Holm at α = 0.05 over the family of forty-five. † raw only — separated before the
correction and not after it, which over a family of forty-five is weak evidence.

| # | strategy | mean margin over the field | free-for-all win % | beat / lost / undecided |
|---|---|---:|---:|---:|
| 1 | `sprinter` | +8.1 | 25.5 ± 0.4 | 8 / 0 / 1 |
| 2 | `angler` | +7.4 | 24.9 ± 0.4 | 6 / 0 / 3 |
| 3 | `outs` | +7.2 | 25.3 ± 0.4 | 6 / 1 / 2 |
| 4 | `opportunist` | +6.6 | 24.2 ± 0.4 | 6 / 1 / 2 |
| 5 | `greedy` | +4.6 | 22.2 ± 0.3 | 3 / 4 / 2 |
| 6 | `cautious` | +4.6 | 22.2 ± 0.3 | 3 / 4 / 2 |
| 7 | `counting` | +4.5 | 22.4 ± 0.3 | 3 / 4 / 2 |
| 8 | `warden` | +0.0 | 17.9 ± 0.3 | 2 / 7 / 0 |
| 9 | `simple` | -3.3 | 15.3 ± 0.3 | 1 / 8 / 0 |
| 10 | `random` | -39.8 | 0.1 ± 0.0 | 0 / 9 / 0 |

🔥 **`sprinter` is the first rung to separate above `outs`, and both columns agree it is the
strongest — but only just.** It tops the mean-margin ranking (+8.1) and the crossed table (25.5),
and its head-to-head cell against `outs` is **+1.2 ± 0.8, `p = 2.9e-03`, surviving Holm over the
family of forty-five**. It beats `opportunist` too (+1.6, separated) and `angler` (+1.1, raw
only). ⚠️ **The margin is real but small, and the crossed table hides it**: sprinter's 25.5 and
`outs`' 25.3 are inside each other's intervals, because a five-point edge over one opponent is
diluted to nothing when eight others share the table. **The head-to-head cell is where a
one-point effect is visible and the free-for-all is where it is not** — the two columns are not
in conflict, they are answering different questions (§4). It should still be re-read under P48's
fresh-seed replication before it is called settled, because a one-point margin is the smallest
this document has ever asked Holm to defend.

🔥 **And `sprinter` is the packet that says the mechanism variable earns its keep.** Three rungs
running — `opportunist`, `purist`, `angler` — measured a change that never armed, and each came
back a null with a flat mechanism. `sprinter`'s mechanism **moved**: at the crossed table it ends
its turn within one card of covering on **26.6 ± 0.2% of its discards against `outs`' 25.6 ± 0.2%**
(`ladder.race-reach.*`, this packet's new rows) — it genuinely steers into more near-wins, and
the win rate moved with it. **A moved mechanism and a moved margin, together, are the finding**;
the three flat mechanisms before it are why this one is believed. §8 is where the *why* is.

🔥 **`angler` is P45's answer, and it is a null with the same shape as P44's: the mechanism
never armed.** It prices the take in cards — a card off the pile costs this turn's blind draw,
worth its live out-cards over the cards unseen — and the price cuts both ways: an improving card
is refused when the hand's own outs outweigh it (which real hands never reach), and a card that
melds nothing is taken when it more than doubles the hand's out-cards (the **enrichment take**,
the one move no rung before it had). **At the crossed table `angler`'s take rate is
24.66 ± 0.09% against `outs`' 24.71 ± 0.09%** — the new move fires too rarely to lift the rate
visibly, because a hand poor enough for one card to double its outs is nearly absent from real
play. The written prediction (`BUILD-PLAN.md` §5 P45) called the null and the never-firing
refusal, but expected the enrichment take to arm on 1–5% of acquisitions; **it was wrong about
that**, and the flat rate is the finding: under a one-draw horizon, `outs`' improvement-only
take already collects essentially everything a card-priced model can see. ⚠️ **The one cell
that hints otherwise is `angler` over `opportunist` — `+1.0 ± 0.8`, separated raw and not under
the correction.** If it is real it is an interaction with the hold rather than with `outs` —
flagged for P48's composition-stratified margins rather than claimed.

🔥 **`opportunist` is P43's answer to the question `warden` left open, and the answer is a null
published on purpose: `+0.1 ± 0.8` against `outs`, inside the interval.** It is `outs`' take
exactly — a card is taken because it improves the hand, never for the lock — plus `warden`'s
hold: a rank an ordinary take has closed against the seat above stays closed (the two escapes
§5.1 gives itself and no more). **The lock arrives at zero price, and at zero price it is worth
nothing this apparatus can see.** ⚠️ **The same run settles where `warden`'s loss lives**: it
plays the identical hold and gives up `−6.2 ± 0.8` to `opportunist` — so the whole of the harm
is the *paid take*, the draws spent buying locks, and none of it is the holding. **Denial did
not fail because holding a rank costs something; it failed because `warden` paid a draw for a
thing measured here to be worth about nothing.** The prediction — null to small positive —
was written down first (`BUILD-PLAN.md` §5 P43), and the null is the finding: the free locks an
ordinary `outs` game arms net out to nothing, so a successor that *pays* anything for a lock is
betting its bought locks are better-targeted than the incidental ones measured here at zero —
a much narrower claim than the one `warden` was built on, and nothing yet suggests it is true.

🔥 **`warden` is the largest negative result this programme has produced, and it lands in the
middle of the ladder.** It plays `RULES.md` §5.1 *offensively* — it will take a card it does not
want in order to close that rank against the seat that threw it — and it is **−7.3 ± 0.8 against
`outs`, the rung it is one change from**, **−6.2 ± 0.8 against `opportunist`, which plays its own
hold without its paid take**, **−6.9 ± 0.8 against `angler`**, and **four and a half to five
points behind each of `greedy`, `cautious` and `counting`**. It beats only `simple` and
`random`. **Every one of those eight
margins survives the correction over a family of forty-five.** ⚠️ **The packet predicted a
null** and wrote the prediction down first (`BUILD-PLAN.md` §5 P31); this is not a null, it is the
biggest separated loss in the document. **§13 is where the *why* is**, and the why is measured
rather than guessed — and since P43, priced: the hold is worth nothing and the paid take is worth
about minus six points.

⚠️ **Nothing in this table is comparable with a run from before 2026-08-21** — P25–P28 changed
what winning a round means, what a money card pays, which cards may be thrown and who may stop a
claim. The numbers below are the game as `RULES.md` rev 24 describes it, and the previous run's
figures are quoted here only where a comparison is the finding.

🔥 **`outs` was the top of the ladder for five packets, and P46 is where something finally beat
it: 6 / 1 / 2, and the one loss is to `sprinter`.** Where two discards leave the hand equally
melded it keeps the thirteen that **more of the values still out there would improve** —
`+2.7 ± 0.8` over `greedy`, `+2.9 ± 0.8` over `cautious`, `+2.9 ± 0.8` over `counting`, all three
surviving the correction over a family of forty-five. Until P43 it was the only rung that
separated from the `greedy` trio upward; `opportunist` (`+2.0` to `+2.2 ± 0.8`), `angler`
(`+2.6` to `+2.9 ± 0.8`) and `sprinter` (`+3.3` to `+3.5 ± 0.8`) now do too. ⚠️ **What beat it
is not a better melder but a different objective**: `sprinter` keeps a hand `outs` would call
worse, because near the line the widest slice of the deck that would *improve* a hand is not the
widest slice that would *win* it (§8). Everything below `sprinter` that separates upward from the
`greedy` trio is still `outs` with one change measured at nothing; `sprinter` is `outs` with one
change measured at **`+1.2 ± 0.8`**.

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
| `random` vs `simple` | -39.8 ± 0.3 | < 1e-300 | 0.00111 | separated | **survives** |
| `random` vs `greedy` | -39.8 ± 0.3 | < 1e-300 | 0.00114 | separated | **survives** |
| `random` vs `cautious` | -39.8 ± 0.3 | < 1e-300 | 0.00116 | separated | **survives** |
| `random` vs `counting` | -39.8 ± 0.3 | < 1e-300 | 0.00119 | separated | **survives** |
| `random` vs `outs` | -39.8 ± 0.3 | < 1e-300 | 0.00122 | separated | **survives** |
| `random` vs `warden` | -39.6 ± 0.4 | < 1e-300 | 0.00125 | separated | **survives** |
| `random` vs `opportunist` | -39.8 ± 0.3 | < 1e-300 | 0.00128 | separated | **survives** |
| `random` vs `angler` | -39.8 ± 0.3 | < 1e-300 | 0.00132 | separated | **survives** |
| `random` vs `sprinter` | -39.8 ± 0.3 | < 1e-300 | 0.00135 | separated | **survives** |
| `simple` vs `greedy` | **-7.4 ± 0.8** | < 1e-300 | 0.00139 | separated | **survives** |
| `simple` vs `cautious` | **-8.1 ± 0.8** | < 1e-300 | 0.00143 | separated | **survives** |
| `simple` vs `counting` | **-8.0 ± 0.8** | < 1e-300 | 0.00147 | separated | **survives** |
| `simple` vs `outs` | **-10.8 ± 0.8** | < 1e-300 | 0.00152 | separated | **survives** |
| `simple` vs `opportunist` | **-10.1 ± 0.8** | < 1e-300 | 0.00156 | separated | **survives** |
| `simple` vs `angler` | **-10.8 ± 0.8** | < 1e-300 | 0.00161 | separated | **survives** |
| `simple` vs `sprinter` | **-11.5 ± 0.8** | < 1e-300 | 0.00167 | separated | **survives** |
| `greedy` vs `warden` | **+4.5 ± 0.8** | 1.3e-27 | 0.00172 | separated | **survives** |
| `greedy` vs `sprinter` | **-3.5 ± 0.8** | < 1e-300 | 0.00179 | separated | **survives** |
| `cautious` vs `warden` | **+4.7 ± 0.8** | 1.4e-30 | 0.00185 | separated | **survives** |
| `counting` vs `warden` | **+5.0 ± 0.8** | 1.8e-34 | 0.00192 | separated | **survives** |
| `counting` vs `sprinter` | **-3.5 ± 0.8** | < 1e-300 | 0.00200 | separated | **survives** |
| `outs` vs `warden` | **+7.3 ± 0.8** | 3.4e-71 | 0.00208 | separated | **survives** |
| `warden` vs `opportunist` | **-6.2 ± 0.8** | 3.1e-51 | 0.00217 | separated | **survives** |
| `warden` vs `angler` | **-6.9 ± 0.8** | 7.1e-65 | 0.00227 | separated | **survives** |
| `warden` vs `sprinter` | **-7.4 ± 0.8** | < 1e-300 | 0.00238 | separated | **survives** |
| `cautious` vs `sprinter` | **-3.3 ± 0.8** | 8.9e-16 | 0.00250 | separated | **survives** |
| `cautious` vs `outs` | **-2.9 ± 0.8** | 2.0e-12 | 0.00263 | separated | **survives** |
| `counting` vs `outs` | **-2.9 ± 0.8** | 2.5e-12 | 0.00278 | separated | **survives** |
| `counting` vs `angler` | **-2.9 ± 0.8** | 4.1e-12 | 0.00294 | separated | **survives** |
| `simple` vs `warden` | **-2.8 ± 0.8** | 5.6e-12 | 0.00313 | separated | **survives** |
| `cautious` vs `angler` | **-2.8 ± 0.8** | 7.6e-12 | 0.00333 | separated | **survives** |
| `greedy` vs `outs` | **-2.7 ± 0.8** | 1.1e-10 | 0.00357 | separated | **survives** |
| `greedy` vs `angler` | **-2.6 ± 0.8** | 2.4e-10 | 0.00385 | separated | **survives** |
| `cautious` vs `opportunist` | **-2.2 ± 0.8** | 2.1e-07 | 0.00417 | separated | **survives** |
| `counting` vs `opportunist` | **-2.1 ± 0.8** | 6.8e-07 | 0.00455 | separated | **survives** |
| `greedy` vs `opportunist` | **-2.0 ± 0.8** | 1.6e-06 | 0.00500 | separated | **survives** |
| `opportunist` vs `sprinter` | **-1.6 ± 0.8** | 1.7e-04 | 0.00556 | separated | **survives** |
| `outs` vs `sprinter` | **-1.2 ± 0.8** | 2.9e-03 | 0.00625 | separated | **survives** |
| `angler` vs `sprinter` | **-1.1 ± 0.8** | 7.9e-03 | 0.00714 | separated | does not survive |
| `opportunist` vs `angler` | **-1.0 ± 0.8** | 1.3e-02 | 0.00833 | separated | does not survive |
| `greedy` vs `counting` | +0.6 ± 0.8 | 1.4e-01 | 0.01000 | inside | does not survive |
| `outs` vs `angler` | -0.6 ± 0.8 | 1.5e-01 | 0.01250 | inside | does not survive |
| `cautious` vs `counting` | +0.1 ± 0.8 | 8.1e-01 | 0.01667 | inside | does not survive |
| `outs` vs `opportunist` | -0.1 ± 0.8 | 8.8e-01 | 0.02500 | inside | does not survive |
| `greedy` vs `cautious` | +0.0 ± 0.8 | 9.4e-01 | 0.05000 | inside | does not survive |

⚠️ **The `p` and `threshold` columns are recomputed here from the mean and standard error the CSV
carries**, by the two-sided normal test and the Holm ladder the harness uses; **the `corrected`
column is the CSV's own verdict**, and the two agree on all forty-five rows.

🔥 **Adding a rung made every surviving verdict harder to reach and changed none of them, for the
fourth time running.** The family went from thirty-six comparisons to forty-five, so the
strictest threshold tightened from 0.00139 to 0.00111 and **every row's threshold moved**; the
margins that did not survive before still do not, and the twenty-eight that did still do — joined
by `sprinter`'s eight real separations, **`outs` vs `sprinter` among them at `p = 2.9e-03`, five
rows inside the strictest surviving threshold**. ⚠️ **That is the cost §2 warns about, paid
knowingly** (`BUILD-PLAN.md` §5 P43 stated it in advance; P45 and P46 paid it again).
🔥 **And the family has two raw-only casualties now, both at the top**: `angler` vs `sprinter`
(`p = 0.0079` against a threshold of 0.0071) and `opportunist` vs `angler` (`p = 0.013`) both
separate raw and die under the correction. **Over forty-five comparisons, two hits at the
one-percent level among the closest-matched pairs in the field is what luck looks like** — the
three near-`outs` rungs (`opportunist`, `angler`, `sprinter`) are so alike that only the largest
of their mutual gaps, `sprinter` over `opportunist` at 1.6 points, clears the correction.

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

The fully crossed table — every assignment of the field across **five** seats. Ten rungs is
`10⁵ = 100,000` seatings, so 8,000 games rounds up to **100,000** — ✅ **one full pass, nothing
subsampled**. That is P46's crossing-cap decision, stated rather than defaulted:
`SeatingPlan.MaximumAssignments` was doubled 65,536 → **131,072** (raise and pay, over dropping a
rung from the crossed cell or subsampling), measured before it was chosen — `sprinter` benches at
1.21× `outs`, and the whole pass ran inside five hours with the rest of the suite. ⚠️ **An
eleventh win-rate rung is `11⁵ = 161,051` and breaks the cap again — the decision recurs.** A
given strategy is at the table in **41,000** of them.

| strategy | win rate |
|---|---:|
| `sprinter` | 25.5 ± 0.4 |
| `outs` | 25.3 ± 0.4 |
| `angler` | 24.9 ± 0.4 |
| `opportunist` | 24.2 ± 0.4 |
| `counting` | 22.4 ± 0.3 |
| `greedy` | 22.2 ± 0.3 |
| `cautious` | 22.2 ± 0.3 |
| `warden` | 17.9 ± 0.3 |
| `simple` | 15.3 ± 0.3 |
| `random` | 0.1 ± 0.0 |

⚠️ **Every figure here moved when P21 added a rung, and none of that was a change in play.** Six
strategies crossed over four seats is a different field from five, so each is at the table less
often and against a stronger average opponent. 🔥 **P32 moved every figure in this table again, and
this time for a third reason: a fifth seat.** Base win rate is 20% rather than 25%, so the whole
column is smaller and **none of that is play either**. **This is the column to distrust when a rung is
added; §3's margins are the column that survives it.**

🔥 **P20's run of `greedy` / `counting` / `cautious` in a five-rung field had them 32.8 / 33.5 /
34.3; P23's six-rung run had them 31.5 / 31.2 / 30.1; P29's had them 30.7 / 30.6 / 30.1; P43's
eight-rung field 23.9 / 24.1 / 23.6; P45's nine-rung field 23.1 / 23.0 / 22.9; P46's ten-rung
field 22.2 / 22.4 / 22.2.** **Seven orderings of three players, and their head-to-head margins
have never left an interval of one point.** ⚠️ **A column that has now re-ranked the same three
rungs six times is not a ranking.**

🔥 **P31 is the cleanest demonstration of that this document has, because nothing about those
three rungs changed.** `warden` was added to the field and to nothing else; every head-to-head
cell among the older rungs came back **byte-identical** (§11), and this column re-ordered anyway.
**A crossed field's win rate is a statement about the field, and §3's matrix is the statement
about the players.**

⚠️ **`warden` at 17.9 is the one low figure here that is not noise.** It is four and a half to
five points below the three middle rungs and 2.6 above `simple`, which is where §3's matrix puts
it too — a rung far enough from its neighbours that both columns agree. 🔥 **And at the top the
two columns finally agree on the winner**: `sprinter` leads the crossed table at 25.5 as it leads
§3's mean margin, the first time since P21 that the head-to-head champion is also the crossed
champion. ⚠️ **But only just, and P43's caution still bites underneath it**: `sprinter`'s 25.5 and
`outs`' 25.3 are inside each other's intervals, so the crossed table cannot actually tell them
apart — the +1.2 head-to-head margin is diluted here to 0.2. And `opportunist` still sits 1.1
points below `outs` in this column while their head-to-head cell is a dead heat. Whether that
dilution is the column being a statement about the field, or a mixed table charging for
something a head-to-head does not, is exactly the kind of question §3's matrix answers and this
column cannot — P48's composition-stratified margins are the instrument that could split it.

⚠️ **This answers a different question from §3 and the two are not interchangeable.** A
free-for-all win rate depends on *who else is in the field*; a head-to-head margin weights every
opponent once. P16 is the packet that learned they differ, and both are reported for that reason.

---

## 5. The seating matters, and by how much

P12's headline — `greedy` against `simple` — at **both** table sizes and under both seating
schemes, at the same seed. 🔥 **Two tables since P32**: §7.1.1 makes four- and five-handed
**different games**, and this is the longest-running measurement in the project — losing its
continuity to gain the default would have been a bad trade when both fit in one run.

| table | seating | greedy | simple | gap |
|---|---|---:|---:|---:|
| four seats | rotated `[g,s,g,s]` | **30.43** | 19.57 | 10.9 |
| four seats | balanced, all 16 assignments | **29.00** | 21.00 | 8.0 |
| **five seats** | rotated `[g,s,g,s,g]` | **24.54** | 15.47 | 9.1 |
| **five seats** | balanced, all 32 assignments | **23.86** | 16.13 | 7.7 |

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

**The null test.** One rung against a copy of itself under another name, 8,010 games. Two labels
of one strategy differ in *nothing*, so any gap between them is the apparatus — a seat that opens
first, a seating that is not balanced, an estimator that counts seats as trials.

| | win rate |
|---|---:|
| `sprinter` | 19.8 ± 0.4 |
| `sprinter#mirror` | 20.2 ± 0.4 |
| **margin** | **−0.4, inside the interval — the file's verdict is `holds`** |

A fair five-seat table gives each 20.0%. ✅ **It holds, and `sim suite` exits non-zero if it ever
stops holding.**

⚠️ **The subject changes with the field, and P31 is where that stopped being tidy.** P17 ran this
on `cautious`, P20 on `counting`, P23 and P29 on `outs`, P31–P42 on `warden`, P43–P44 on
`opportunist`, P45 on `angler` — the cell is played
by the *last* rung the catalog's ladder names, which for six rungs running was also the
strongest. The ladder became a tree at P31 and **the null cell has now changed hands four times
without anybody choosing it** — to `warden` when that branch appeared, to `opportunist` when P43
extended it, to `angler` when P45 hung a fourth branch off `outs`, to `sprinter` when P46 hung a
fifth. 🔥 **It is left that way deliberately.** The cell's whole claim is that *any*
strategy against a copy of itself wins 1/n — a statement about the apparatus and not about the
rung — so a null test that depended on who played it would itself be the finding. **It held on
`outs`, `warden`, `opportunist` and `angler`, and holds on `sprinter` at −0.4 with the fair 20%
inside every
interval** — and `sprinter` is the one rung it most needed to hold on, since a rung that changes
its play *near the end of a round* is exactly the kind that could make a game depend on scheduling
order. It does not.

🔥 **And the rungs it has run on most recently are the ones that could have broken it.** `outs`,
`opportunist`, `angler` and `sprinter` carry a **cache across deals**, and `warden` carries a **memory of
every card
it has been shown** — exactly the kind of state that could make a game depend on the order the
harness happened to schedule it in. None does: the cache is keyed on card *values* rather than
on a round's shoe, and the memory is wiped by the deal. **This is the run that says so at scale
rather than in a unit test.**

**What pairing is worth, and which way it points.** The per-game difference against the
add-the-variances formula, on identical data:

| scope | comparison | paired SE ÷ independent |
|---|---|---:|
| within-cell | the near-`outs` cluster (`outs`, `opportunist`, `angler`, `sprinter`) pairwise | **1.4141–1.4142** |
| within-cell | `cautious` vs `counting` | **1.4142** |
| within-cell | `greedy` vs `cautious` | **1.4142** |
| within-cell | `greedy` vs `counting` | **1.4142** |
| within-cell | the `greedy` trio vs the near-`outs` cluster | **1.4136–1.4139** |
| within-cell | `warden` vs the near-`outs` cluster | **1.4101–1.4113** |
| within-cell | `simple` vs any thinking rung | 1.405–1.414 |
| within-cell | `random` vs anything | 1.013–1.027 |
| across-cells | `opportunist`: vs `angler` less vs `sprinter` | **0.35** |
| across-cells | `warden`: vs `opportunist` less vs `angler` | **0.37** |
| across-cells | `greedy`: vs `cautious` less vs `counting` | **0.56** |
| across-cells | `simple`: vs `greedy` less vs `cautious` | **0.61** |
| across-cells | `cautious`: vs `counting` less vs `outs` | 0.73 |
| across-cells | `random`: vs `simple` less vs `greedy` | 0.74 |
| across-cells | `outs`: vs `warden` less vs `opportunist` | 0.79 |
| across-cells | `counting`: vs `outs` less vs `warden` | 0.80 |
| across-cells | `sprinter`/`angler`: vs `random` less vs `simple` | 0.91–0.92 |

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
  correlation is positive, and pairing **narrows** — down to 0.35. That is free variance
  reduction, and it is what common random numbers are for. ⚠️ **The 0.35 is a statement about the
  players, not the method**, and P46 sharpened the point P45 first made: `opportunist`'s margins
  against `angler` and against `sprinter` correlate almost perfectly because all three are `outs`
  with one small change, so from shared shoes their difference is nearly pure common noise. **The
  near-`outs` cluster has grown to four rungs** (`outs`, `opportunist`, `angler`, `sprinter`), and
  the tightest across-cells ratios in the file are exactly the comparisons among them.

✅ **This is the one part of the document P25–P28 left alone, and that is a result about the
harness rather than about the game.** Every within-cell ratio is still √2 to four digits and every
across-cells ratio moved by at most four hundredths. **The correlation structure is a property of
"exactly one seat declares" and of the shared shoe** — neither of which any of the four rules
changes touched — so a run that moved 87 of 91 published numbers moved this table hardly at all.

---

## 7. What this apparatus can and cannot resolve

At 8,010 games a cell the standard error on a head-to-head margin between two thinking rungs is
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
| `warden` | **worse than nothing** — `−7.3 ± 0.8` against `outs` and four and a half to five points behind three more rungs | below, and §13 |
| `opportunist` | **nothing** — `+0.1 ± 0.8` against `outs`, the lock at zero price, and the null is the answer to `warden`'s open question | below, and §3 |
| `prospector` | **nothing at the stakes this is played for**, `+14.2 ± 4.8` a round at $5/$40 | §10 |
| `purist` | **nothing** — the clean bonus at zero price changed about one round in eight thousand; the accidental floor already collects every free clean win | below, and §14 |
| `angler` | **nothing** — `+0.6 ± 0.8` against `outs`, and the take rate never moved: the enrichment take its price permits almost never arms | below, and §3 |
| `sprinter` | **`+1.2 ± 0.8` over `outs`** — the endgame race, and the first rung to separate above `outs`; the mechanism armed (§3) | §3 |
| `outs/refuse` vs `outs/allow` | **nothing** — `+0.4 ± 1.0` for refusing a claim | §12 |

⚠️ **Six of the nine research rungs are entries below, `outs` and `sprinter` are in §3, one is
in §10 and P29 added a null in §12 that is not a rung at all.** Read `cautious` and `counting`
below with this in mind:
**both of them put their new idea underneath `CoverScore.Potential` and `outs` put its above it**,
and that is the only structural difference between them.

🔥 **`warden` is a fourth kind of answer and the first rung that actively lost.** `cautious` and
`counting` measured *nothing*; `warden` measured **four and a half to seven points of harm**,
separated under the correction. ⚠️ **A rung that loses that clearly is a stronger result
than a null**, because a null leaves open whether the instrument could see the effect and this does
not.

🔥 **And `opportunist` is a sixth kind: a null that closes a question rather than leaving one
open.** An ordinary null says *the instrument saw nothing*; this one was built as an instrument —
`warden`'s hold with `warden`'s paid take removed — so its `+0.1 ± 0.8` against `outs` *decides*
that the harm all lived in the price, and that denial free of charge is worth nothing this
apparatus can see. **The prediction (null to small positive) was written before the run and the
null was the more informative half.** ⚠️ **P44's `purist` is a second null of exactly this
kind, one packet later** (§14): the highest-expected-value unbuilt rung in the project came
back deciding that the free half of the clean bonus was *already being collected* — two
zero-price rungs in two packets, and both nulls say `outs`' plain cover maximisation is already
standing at the free-lunch frontier.

🔥 **And `prospector` is a fifth kind of answer, which is why it has a section rather than a
bullet.** It did not lose. It asked a question the other four never asked — *what is the side bet
worth?* — and got back **a function of the stakes** rather than a number: literally the same
player as `outs` at $5/$1, `+14.2 ± 4.8` a round at $5/$40. ⚠️ **A rung can fail to be
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
  it (8,010 games a cell). 🔥 **It is not that the memory does not work — it does, and a
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
- **`warden` — play the feeding ban at somebody.** Worth **`−7.3 ± 0.8` points against `outs`**,
  `−6.2 ± 0.8` against `opportunist`, and `−4.5 / −4.7 / −5.0` against `greedy`, `cautious` and
  `counting`; it also gives up `−6.9 ± 0.8` to `angler` and `−7.4 ± 0.8` to `sprinter`, and beats
  `simple` by `+2.8 ± 0.8` and nothing else. **All nine margins survive
  Holm over a family of forty-five** (8,010 games a cell; `−7.4 ± 0.8` against `sprinter` joined
  the list at P46). 🔥 **It is the only rung whose
  failure has a measured mechanism rather than an argued one**, and §13 is that measurement: the
  rule it plays bites hard in a crossed field, so the idea was not starved of opportunity.
  ⚠️ **What it got wrong is the price, and the price is in the wrong currency.** It refuses to
  buy a lock that would cost it a *melded card* — and then pays for every lock it does buy with
  a *draw*, which is the only thing that improves a hand and which nothing in the rule prices.
  Measured, an all-`warden` table runs **31.9 turns a round against `outs`' 24.1** (`sim bench`):
  about a third of its draws have become locks. **P31 closed saying a successor rung has to
  price the draw; P43 priced the other half and settled it (next entry), and P45 built the
  price itself** — `angler` is that successor, and its null two entries down says the price,
  once written, almost never disagrees with `outs`.

- **`opportunist` — the feeding ban at zero price.** Worth **`+0.1 ± 0.8` points against
  `outs`** — inside the interval, the null the packet predicted — while beating `warden` by
  `+6.2 ± 0.8` and the `greedy` trio by `+2.0` to `+2.2`, all separated over the family of
  forty-five. ⚠️ **It now loses to `sprinter` too** (`−1.6 ± 0.8`, separated). 🔥 **It is
  `warden`'s hold without `warden`'s paid take**: it takes only cards
  that improve its hand, exactly as `outs` does, and then declines to release whatever ranks
  those ordinary takes closed (§5.1, exception 1 — a release is permanent). The hold is
  literally shared code (`HeldLocks`), so the 2×2 is complete: `outs` neither buys nor holds,
  `warden` buys and holds, `opportunist` holds without buying. ⚠️ **What the null decides**:
  `warden`'s loss lives entirely in the paid take, and holding a lock — the flexibility given
  up, the free denial delivered — nets to nothing this apparatus can see. **Denial did not fail
  for want of a better price; at the best possible price, zero, it still buys nothing.** The
  one door left open is targeting: the locks this rung holds are incidental, wherever an
  improving take happened to land, and a rung that *chose* its locks would be making a claim
  this null does not test — but it would be paying for them again, against a measured zero.
  ⚠️ **Its free-for-all reading is two points behind `outs` while the head-to-head is a dead
  heat** (§4) — either the column being a statement about the field again, or a cost only a
  mixed table charges; P48's composition-stratified margins could split those.

- **`purist` — play for the clean bonus, at zero price.** Its whole preference — where the
  melded cards tie, shed a joker — changed **about one round in eight thousand**: its clean-win
  share reads `12.8% ± 1.0` against `12.8%` for a control that is literally `outs` under
  another name, and its money margin sits within **half a cent a round** of the same control
  cell (§14 has the instrumented reading). 🔥 **The null's mechanism was visible in hindsight**:
  whenever throwing the joker is the *unique* winning discard, `outs` already throws it —
  thirteen melded beats everything before the joker's never-throw sentinel is consulted — so
  the accidental floor already contains every clean win that costs nothing, and what a
  zero-price preference can add is only the exact ties, of which the game supplies almost none.
  ⚠️ **So §14's arithmetic stands and its distance cannot be crossed free**: every clean win
  still on the table costs melded cards — win probability, the currency `warden` proved
  ruinous to spend on a side idea.

- **`angler` — price the take against the blind draw, in cards.** Worth **`+0.6 ± 0.8` points
  against `outs`** — inside the interval, the predicted null — while beating the `greedy` trio
  by `+2.6` to `+2.9` and `warden` by `+6.9`, all separated over the family of forty-five.
  🔥 **It is the successor `warden`'s autopsy called for**: nothing had priced a draw in cards,
  and this rung is that price — a card off the pile costs this turn's blind draw, and a blind
  draw is expected to deliver its **live out-cards over the cards unseen** (`LiveOuts` weighted
  by loose copies, `MoneyOdds`' unseen pool, one stated model: the horizon is one draw). The
  price cuts both ways: an improving take is refused when the hand's own outs outweigh a
  certain melded card — which needs half the unseen pool to be live and **never happens to a
  real hand** — and a card that melds nothing is taken when it more than doubles the hand's
  out-cards, the **enrichment take**, the first new take any rung has added since `warden`'s.
  ⚠️ **The mechanism came back flat, which is the finding** (P44's shape, one packet later):
  `angler`'s take rate at the crossed table is **24.66 ± 0.09%** against `outs`' 24.71 ± 0.09%
  (`ladder.take-rate.*`, rows this packet added for every rung), so the enrichment take arms
  too rarely to see — the prediction expected 1–5% of acquisitions and was wrong. A hand poor
  enough that one card doubles its outs is nearly absent from real play, so under a one-draw
  horizon **`outs`' improvement-only take already collects everything a card-priced model can
  see**. Three zero-or-stated-price rungs in three packets — `opportunist`, `purist`, `angler`
  — and all three nulls said the same thing from different directions: **`outs` was standing at
  the free-lunch frontier, and what is left costs currency**. ⚠️ **One loose end, stated
  rather than hidden**: `angler` over `opportunist` reads `+1.0 ± 0.8`, separated raw at
  `p = 0.013` and dead under Holm's 0.0083 — over the family it was measured in that is what luck
  looks like, and it is flagged for P48 rather than claimed.

🔥 **`sprinter` is where P46 found what the frontier costs, and the currency is not a better hand
— it is a different objective.** After three nulls saying `outs`' cover maximisation already
collects every free improvement, `sprinter` changes *what is being maximised* in the endgame: one
card from covering, it keeps the thirteen the most of the deck would **win** rather than the
thirteen the most would **improve**, accepting a worse hand in expectation for a faster chance to
declare (`RULES.md` §6's fact that exactly one seat goes out). **`+1.2 ± 0.8` against `outs`,
surviving Holm over the family of forty-five — the first rung to separate above `outs` since it
was built** (§3). 🔥 **And this is the packet that vindicates the mechanism variable.** The three
nulls before it each measured a change that never armed — a flat take rate, a flat clean-win
share, a flat lock-bite; `sprinter`'s mechanism *moved*: its race-reach (the share of discards
that leave it one card from a win) is **26.6 ± 0.2% against `outs`' 25.6 ± 0.2%**, ~4 SE apart, so
it demonstrably steers into more near-wins — and the win rate moved with it. **A moved mechanism
and a moved margin arriving together, after three packets where neither moved, is why the +1.2 is
believed rather than dismissed as the family's luckiest cell.** ⚠️ **It is still small** — a dead
heat at the crossed table, and a one-point margin is the tightest this document has asked Holm to
hold — so P48's fresh-seed replication is where it graduates from *measured* to *settled*.

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

🔥 **Re-fitted a third time by P32, at five seats — and this time nothing moved at all, against a
prediction that at least one value would have to.** The reasoning behind the prediction was sound:
a five-handed table's base win rate is 20% rather than 25%, so every margin rescales by 0.800 (see
the top of this document) and the dial's steps should compress with them toward the ~1-point
resolution floor. ❌ **They did not compress.** The sweep at five seats asks for `hard` ≈ 0.42 and
`medium` ≈ 0.67 — **the shipped 0.4 and 0.7 inside the rounding** — and the reference table's steps
came out **6.0 / 7.9 / 6.6** against four-handed's 7.1 / 7.9 / 8.0. ✅ **All three adjacent margins
still survive Holm**, at five seats and, separately checked, at four and six
(`docs/strategy/dial-away-from-the-default-table.md`).

🔥 **So ε is close to being a property of the mistake rather than of the rung *or of the table*** —
P23's finding holding across a second axis it was never tested on. ✅ **That is what makes *one
dial* the right product decision** rather than one calibration per table size: a level means the
same thing wherever you sit, and three calibrations would be three ways for the menu to lie.

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

⚠️ **ε is not the dial; results are.** Re-swept at **five seats** by P32 — seven mistake rates on
`outs`, **16,807 games**, one full pass of every balanced seating
(`docs/strategy/epsilon-sweep-5-handed.txt`):

| ε | 0 | 0.1 | 0.2 | 0.35 | 0.5 | 0.75 | 1 |
|---|--:|--:|--:|--:|--:|--:|--:|
| win % | 27.5 | 26.3 | 24.7 | 22.9 | 19.4 | 13.1 | 6.0 |

**Levels spaced evenly in ε would not be spaced evenly in play** — the last quarter of the dial is
worth more than the first half of it — so the four are placed by inverting that curve. 🔥 **And
ε = 0 is indistinguishable from ε = 0.1**: 33.6 against 33.7, with a ±1.6 interval on each. *The
best card and the second-best card are usually the same card*, which is the sentence that explains
why the top of the dial has to be ε = 0 and why a fifth level between `expert` and `hard` would be
two names for one opponent.

### The reference table — all four levels at one table, fully crossed

**1,024** assignments across **five** seats, 8,192 games, 6,248 of them holding any one level.
⚠️ **A five-handed table's base win rate is 20%, not 25%**, so every figure here is lower than
the four-handed one by construction — the **steps** are the measurement.

| level | win % | step |
|---|--:|--:|
| `expert` | **30.58 ± 0.73** | — |
| `hard` | **22.97 ± 0.69** | −7.6 |
| `medium` | **16.15 ± 0.63** | −6.8 |
| `easy` | **10.30 ± 0.55** | −5.8 |

### The steps, head to head

**The family is the three adjacent steps and not the round-robin**, because *"n+1 beats n"* is
the only claim a monotone dial makes — correcting over all six pairs would throw power away for
comparisons nobody is making. Each cell is 8,008 games at every seating in which both levels are
at the table, and each margin is the **paired** one (§1 rule 4).

| step | margin | Holm |
|---|--:|---|
| `expert` over `hard` | **+6.54 ± 0.80** | separated |
| `hard` over `medium` | **+7.93 ± 0.80** | separated |
| `medium` over `easy` | **+9.12 ± 0.79** | separated |

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

# the dial away from the default table — P32's monotonicity check, kept verbatim in
# docs/strategy/dial-away-from-the-default-table.md because it is not part of the standing set
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --seats 6 --games 2000 --seed 20260819

# the sweep that placed the four values
dotnet run -c Release --project BurmesePoker.Sim -- --strategies outs@0,outs@0.1,outs@0.2,outs@0.35,outs@0.5,outs@0.75,outs@1 --seating balanced --seats 5 --games 16807 --seed 20260819

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

⚠️ **The instrument generalised at P44 and the row ids moved with it.** There are two
money-ranked rungs now, so `sim suite` runs **one sweep per challenger** — this section is
`prospector`'s; `purist`'s lives in §14, beside the bonus it plays for — and **the challenger is
part of every `money.*` id**: `money.net-per-round.5-1` became `money.net-per-round.5-1.prospector`,
values unmoved, exactly as the seat count joined the headline ids at P32. An unqualified
`money.*` id in anything below this line is a pre-P44 spelling of the same row.

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

Head to head against `outs`, 8,010 games a cell, every seating in which both are at the table,
paired margins (§1 rule 4), Holm-corrected over the four cells. ⚠️ **The money column is the
verdict and the win rate is beside it**, because a rung that wins fewer rounds and banks more is
the better player.

| stakes | money card is worth | `prospector` take % | **$ a round** | win % | Holm |
|---|--:|--:|--:|--:|---|
| **$5/$1** — as played | 0.2 rounds | 24.8 | **-0.23 ± 0.32** | -0.6 ± 0.8 | inside |
| $5/$10 | 2 rounds | 14.7 | **-0.35 ± 1.24** | -5.9 ± 0.8 | inside |
| $5/$20 | 4 rounds | 0.1 | **+3.98 ± 2.43** | -16.0 ± 0.8 | **separated** |
| $5/$40 | 8 rounds | 0.0 | **+14.20 ± 4.81** | -16.0 ± 0.8 | **separated** |

`outs` takes about 24% of its cards off the pile in every cell, which is the control: the only
thing moving down that column is the rule firing.

🔥 **The money column fell at P33 and the win-rate column did not move by a digit** — which is the
whole of §7.3's effect on this section, isolated. **The clean bonus multiplies the round prize, and
the round prize is exactly what `prospector` sells.** Both separated cells lost about **$1.20 a
round** (`+5.32 → +4.13`, `+14.63 → +13.43`) because `outs` wins 20 points more rounds and so
collects the multiplier far more often. ⚠️ **The take rates and the `money.side-margin.*` rows are
byte-identical to P31's**, so this is a change in the *price of a round*, not in anybody's play or
in the side bet itself. **The crossover is still at or below four rounds, and it is a little
further off than it was.**

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
$5/$1 the rule **never fires at all**: one melded card is worth about a dollar at this
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
# the standing set — writes docs/strategy/measurements.csv. ⚠️ The wall clock is a property of
# the machine as much as the run: ~5 h on a 24-core workstation at P46 (ten rungs), where the
# same-shaped run took ~5-6 h on the laptop the earlier packets were timed on. Growth in *work*:
# 12,445 s at P32 (seven ladder rungs); ~15,200 s at P43 (eight); ~18,600 s at P44 (a second
# money sweep, four outs-priced cells); ~22,500 s at P45 (nine); more again at P46, whose tenth
# rung adds nine head-to-head cells and grows the free-for-all crossing to 10⁵ = 100,000 under
# the cap P46 doubled to 131,072. ⚠️ Trust `sim bench` and CPU accounting over any pasted wall
# clock: the P45 laptop slept mid-run, and the P46 workstation is ~an order of magnitude faster,
# so neither number is portable.
# It is five seats by default since P32 — `--seats 4` regenerates
# the four-handed set, which is kept frozen at docs/strategy/measurements-4-handed.csv.
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

# the money sweeps on their own — §10 (prospector) and §14 (purist). A bare `money` sweeps
# every money-ranked rung in turn; --challenger names just one.
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
published figure**: the money null in §12 was `+0.02 ± 0.25` rather than `+0.02 ± 0.18`, and the
null survives. ⚠️ **That row has since moved twice more for reasons that have nothing to do with the pairing** —
P33's clean bonus changed what a round pays, and P32 moved the whole standing set to five seats. It
reads **`-0.11 ± 0.32`** now, which is what §12 quotes. **Still a null, and still the same null**;
the three events are separate and this file should not be read as though the R3 pairing moved it
three times. ⚠️ **The `+0.06 ± 0.29` that stood here until P35 was a four-handed figure left behind
by P32** — the exact class of staleness build item 4 of packet P34 exists to catch, found by
re-deriving the section from the CSV rather than by reading it.

### 🔥 What this file does **not** measure, and why — RULES.md §7.5

⚠️ **The feeding blame is built and is not in the standing set.** It cannot be: §7.5 pays a
**third consecutive win** out of the pocket of the seat above the winner, and every experiment in
this project runs `RoundsPerGame = 1` — a deliberate design decision (BUILD-PLAN §3.8), because the
game is the unit of independence and a match of correlated rounds would break every interval above.
**A three-round streak cannot occur in a one-round game, so no figure in this document is measured
under §7.5 and none ever will be while that decision stands.**

✅ **Stated rather than arrived at by silence**, which is this section's own rule (P35 acceptance 4).
The rule is not unchecked — it is **audited** instead of measured: the conformance harness plays a
sequence of 120 rounds at four and five seats, keeps its own count of consecutive wins, re-derives
who owes what, and **fails if no streak occurred at all**, so the audit cannot pass vacuously.
⚠️ **What that leaves genuinely unknown is what §7.5 is *worth*** — whether a table where somebody
is on two in a row plays differently, and whether asking to change seats before somebody's third
win is a strategy. **Nobody has measured it, no rung knows the rule exists, and this file should
not be read as though somebody had.** Measuring it would need a harness whose unit of independence
is a *match*, which is a different instrument from the one every figure above comes from.

✅ **Its sibling is measured, because it can be.** §7.4's deal bonus fires inside a single round, so
`bonus.deal-rate.*` is in `measurements.csv` beside `bonus.jokerless-rate.*` — see §14.

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
| the ladder, all ten rungs crossed | 28.3 | **0.001%** — 1 game of 100,000 |
| the difficulty dial, all four levels crossed | 31.9 | 0 of 8,192 |
| `outs/refuse` against `outs/allow` | 25.5 | 0 of 8,010 |

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
| rounds in which the opener asks for the card | **25.3–27.9%** |
| of those, how often the upstream seat vetoes | **51.8%** at the ladder, **54.2%** at the dial |
| `outs/refuse` over `outs/allow`, win rate | **-0.3 ± 0.8** — inside the interval |
| `outs/refuse` over `outs/allow`, money a round | **-0.11 ± 0.32** — inside the interval |

✅ **The money row is paired now and the correction has landed** (review R3, raised 2026-08-21,
regenerated by P31 the same day). It had been computed with the independent formula on a
*within-cell* margin: the two arms share one table and money is zero-sum across it, which is
exactly the case `Measurement.Paired`'s own remarks call anti-conservative. **The interval went
from ±0.18 to ±0.25 and the mean did not move at all** — `+0.020230` before and after, to six
decimal places. ⚠️ **Widening keeps a null a null**, so the finding is unchanged; what changed is
that the printed interval is no longer understated.

⚠️ **P33 then moved this row for a different reason, and it is the only row in §12 that moved.**
The clean bonus (§7.3, §14) multiplies the round payment on a jokerless declaration, so every
figure denominated in dollars a round is measured against a bigger prize: `+0.020` became
**`+0.058 ± 0.292`**. ✅ **The win-rate row above it is byte-identical**, as are the ask rate, the
veto rate and the round lengths — **the two arms play the same rounds they always did.** The null
stands, on a slightly larger currency.

🔥 **A null, published** (the discipline P20 set). Two arms of one rung differing in exactly that
one answer, 8,010 games, and neither the win rate nor the money can tell them apart. ⚠️ **This is
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
| the ladder, all ten rungs crossed | 2,832,144 | **23.3%** | **30.0%** | **7.0%** |
| the difficulty dial, all four levels crossed | 260,975 | **23.0%** | **21.9%** | **5.0%** |

*Live* means the ban had removed at least one held card from the choice. *Bit* means the card the
seat would have thrown over its whole hand is not the card it threw over its legal set.

🔥 **So the rule does a great deal.** At a table of thinking players **§5.1 takes the card a seat
meant to play about once every fourteen turns**, and a lock is live on a quarter of all turns.
⚠️ **This is the reading that makes `warden`'s loss attributable.** `BUILD-PLAN.md` §5 P31 wrote
down, before the run, that a null would most likely mean *the lock is cheap to escape* rather than
*denial is worthless* — and named this variable as the way to tell those apart. **The escape hatch
is closed: the locks bite, and the rung still loses.** The idea is not starved
of opportunity; it is simply a bad trade at the price `warden` pays for it. 🔥 **And since P43 the
two halves of that sentence are separately priced**: `opportunist` arms locks off ordinary takes
only, holds them with the same code, and measures level with `outs` (§3, §8) — so the bites this
section counts are delivered free of charge or not worth delivering, and the paid take is where
all of `warden`'s loss lives.

⚠️ **Two things this does not say.** It does not say the bites *cost* the bitten seat anything —
the second-choice discard may be nearly as good, and this counts occurrences rather than damage.
And **the bite rate is a property of the field**: a table of weaker rungs takes fewer cards in the
open, so it arms fewer locks. The dial's 23.0% / 21.9% against the ladder's 23.3% / 30.0% is that
effect, and it is the reason both fields are published rather than one.

⚠️ **The claim-permission cell publishes zeroes for the bite and is not evidence of anything.**
The counterfactual costs a second ranking on every restricted turn, which on an `outs`-family rung
is the expensive thing a turn does, so **the suite buys it in the two crossed cells only** — the
two with a whole field sitting down together. That cell reports its *denominator* honestly (21.5%
of its discards were restricted) because a list length is free.

✅ **The instrument changes no card.** `LockBiteTests` asserts that a run with the counterfactual
on and a run with it off produce byte-identical CSVs — which is what makes the two cells that
bought it comparable with the 108 rows that did not.

---

## 14. How often the clean bonus is actually collected

**Added by P33, the packet that built `RULES.md` §7.3 — and the first number in this document
about what a win is *worth* rather than about who wins.**

🔥 **The rule.** A declaration with **no joker anywhere in the thirteen** pays the winner **×2** the
round value at two, three or four seats and **×3** at five or more — so at this document's table it
is **×3**. It is `EXPERT` and Settled, and
it was volunteered — and then corrected the same day — by Mya Lay while she was answering a
question about something else. At the standard $5 stakes it takes a four-handed win from $15 to
**$30**, and a five-handed one from $20 to **$60**.

| field | rounds settled | **won jokerless** |
|---|---:|---:|
| the ladder, all ten rungs crossed | 99,999 | **12.2%** |
| the difficulty dial, all four levels crossed | 8,192 | **12.0%** |
| the two-arm claim-permission cell | 8,010 | **12.4%** |

**So about one round in eight is worth triple**, at a five-handed table of rungs that have never
heard of the rule.

⚠️ **Read that as a floor, and there are two independent reasons why.**

1. 🔥 **No rung is trying.** `CoverScore.Potential` returns `int.MaxValue` for a joker, so every
   rung in this project holds a joker over everything else it could keep. **Every one of the
   declarations counted above came out clean by accident.** The expert named shedding a joker as
   *the* reason a joker is ever thrown away, and **that behaviour still cannot arise here** — a rung
   that played for the bonus would be a **new rung** under P15's discipline (§11), measured before
   it joined the ladder, rather than a change to `outs`.
2. ⚠️ **The rule may not even be reachable when it would pay.** `RULES.md` §9 **#33** is open on
   whether the joker may be thrown **before** the declaring discard. P33 built the default that
   says it may not — §5.1 exception 2 and nothing more — so **a hand that needed to shed a joker a
   turn early could not reach the bonus at all.** If that answer comes back the other way, this
   number can only go up.

🔥 **What it is worth to the field, and it is the finding.** The bonus does not change anybody's
play, so its whole effect is on the money — and the money moved in exactly one direction. **Every
row in this document denominated in dollars a round fell for the rung that trades rounds for money
cards** (§10): `prospector` over `outs` went `+5.32 → +4.13` at $5/$20 and `+14.63 → +13.43` at
$5/$40. **A prize that is sometimes doubled is a tax on selling rounds.** ⚠️ **That is the first
measured interaction between the money layer and the scoring layer in this project**, and it
arrived without either being changed for the other's sake.

⚠️ **What this section used to say it did not measure, P44 has now measured — the half of it
that is free.** The paragraph that stood here called a rung that valued the bonus *"the obvious
next experiment"*, and the arithmetic made it the highest-expected-value unbuilt rung in the
project: at five seats the bonus pays **+$10 a head** against a whole round's flat prize of $5 a
head, so a rung that turned one round in eight into one in four would collect an extra half of a
round's prize every round — far inside what this apparatus resolves (§7). **The experiment ran
on 2026-08-24 and the answer is below.** The arithmetic was sound; what it priced turned out not
to be purchasable at zero.

### What P44 measured: at zero price there is nothing left to buy

**The rung.** `purist` is `outs` with one change: a *fewest-jokers-kept* preference between
`outs`' two ranking keys, so it sheds a joker whenever that costs **no melded card** — paying
any number of live outs, never a meld. The exchange rate is stated rather than tuned
(`prospector`'s precedent), and it is lexicographic rather than a number: a numeric rate would
need a win-probability estimate nothing here supplies. It is ranked on money (`BotRung.Ranked`),
measured by its own money sweep against `outs`, 8,010 games a cell, its own Holm family of four.

**The prediction, written before the run** (BUILD-PLAN P44): `+$0.5` to `+$1.5` a round at
$5/$1, clean-win share up from the ~12% floor to 20–35%. ❌ **Both halves were wrong, and the
wrong prediction is again worth the most** (P29's precedent).

**The measurement, and the instrument that sharpens it.** At $5/$1 the sweep reads
`−$0.23 ± 0.32` a round — inside the interval — but the paired design gives this cell an exact
control: `prospector`'s $5/$1 cell is played from the same master seed over the same seatings,
and at $5/$1 `prospector` **is** `outs` card for card (§10's identity), so that cell's
`−$0.228 ± 0.32` is pure seat luck. **The difference between the two cells isolates `purist`'s
real effect: about `−$0.005` a round and `−0.01` points of win rate — one round in eight
thousand.** Its clean-win share (`money.clean-win-rate.5-1.purist`) reads **12.81% ± 1.04**
against the identity control's **12.83%** and the field's 12.22% floor. 🔥 **The mechanism did
not move at all.**

**Why, and it was visible in hindsight.** Whenever throwing the joker is the *unique* winning
discard, `outs` already throws it — thirteen melded beats everything before the joker's
never-throw sentinel is ever consulted — **so the accidental floor already contains every clean
win that costs nothing.** A zero-price preference can only add the exact ties (a clean and a
dirty declaration available on the same turn) and the free mid-round sheds (a joker gluing
nothing, at no cost in melds), and the game supplies almost none of either: a held joker almost
always earns its place in the cover, which is exactly why every rung holds one over everything.

**What the null decides, and what it leaves.** *"One round in eight is a floor"* stays true and
stops being promising: **the floor is the whole of the free lunch.** Every clean win still on
the table costs melded cards — win probability, the currency `warden` (§8) proved ruinous to
spend on a side idea — so a *paying* clean-bonus rung is the remaining unknown, and after two
zero-price nulls in two consecutive packets (P43, P44) it joins the anti-recommendation idiom:
**a packet proposing one must first say what changed.** ⚠️ Two standing caveats: `purist`'s
ceiling is bounded by §9 **#33**'s recorded default (a *locked* joker may leave only as the
declaring discard), so if the expert session flips #33 this rung must be re-measured; and its
verdict should be re-read under P48's composition-stratified margins like every head-to-head
figure (review F1).

**Two checks and one aside, from the same rows.** The rung reads no stakes and the run proves
it — its take rate (24.81%, `outs`' own control value) and win margin are byte-identical at all
four ratios, with only the side-bet noise widening. The higher-ratio cells (`−$1.1`, `−$2.0`,
`−$3.8`, all inside their intervals) are that same seat-luck deficit rescaled by the money-card
value, not a stakes effect. 🔥 **And the aside**: `prospector`'s own clean-win share collapses
to **~5.6%** in the cells where its rule fires ($5/$20, $5/$40) — a blind-draw-heavy hand
reaches the declaration holding more jokers, the first observed interaction between the draw
decision and the clean bonus, and one more tax on chasing the side bet that §10's totals were
already carrying.

✅ **P32 answered the paragraph that used to stand here, which said this had not reached five
seats.** It has: everything above **is** five-handed now, and the four-handed figures it is
compared against are frozen at `docs/strategy/measurements-4-handed.csv`. 🔥 **Both halves of the prediction
held, and they pull opposite ways.** §7.1.1 requires a joker-free series at four seats and nothing
at all at five, so four-handed play is pushed toward clean hands and five-handed play is not — and
the rate **fell**, 15.4% → **12.1%**. But the bonus pays **×3 to four losers** rather than ×2 to
three, so a clean win is worth **+$40** over flat rather than +$15. ✅ **Expected value a round went
$2.31 → $4.84**: collected less often, worth more than twice as much, **and value wins by better
than two to one.** ⚠️ **So the incentive to build a rung that plays for the bonus is larger at the
default table than the four-handed arithmetic suggested**, not smaller.

```bash
# the ladder cell this rate comes from — the bonus rows are bonus.jokerless-rate.*
dotnet run -c Release --project BurmesePoker.Sim -- tournament --games 8000 --seed 20260819
```

⚠️ **`StandingAnswerTests.TheDocumentSaysHowOftenTheCleanBonusIsActuallyCollected` fails the build
if this section's rate stops being published** — the same mechanism §11 describes for a rung, applied
to a scoring rule. **A bonus nobody measured is a rule the document cannot price.**

---

## 15. How often a hand wins before anybody plays it

**Added by P35, the packet that built `RULES.md` §7.4 and §7.5 — and it is the smallest published
rate in this document by two orders of magnitude.**

🔥 **The rule.** A winner whose **dealt** thirteen already melded pays **×2**, on top of §7.3's
clean bonus if the thirteen hold no joker — so a jokerless deal at this five-handed table pays
**×6**, $30 a head against a flat $5. ⚠️ **"On the initial deal" means the dealt thirteen alone**,
which is `RULES.md` §9 #38's recorded default and not a confirmed answer; under the competing
reading — *the winner's first turn* — this would be a completely different, and far commoner,
number.

| field | rounds settled | **won on the deal** | rounds |
|---|---:|---:|---:|
| the ladder, all ten rungs crossed | 99,999 | **0.050%** | 50 |
| the difficulty dial, all four levels crossed | 8,192 | **0.012%** | 1 |
| the two-arm claim-permission cell | 8,010 | **0.012%** | 1 |

**Twenty-eight rounds in 75,250 — about one deal in 2,700.** ⚠️ **Read the interval off the
counts, not off the rate**: twenty-eight events is twenty-eight events, and the difference
between the three cells here is noise.

🔥 **The finding is what it did *not* move, and it was worth the whole re-measurement to know.**
This packet changed when a round can end, so a round that used to run turns now ends at turn 0 —
and **not one win rate, margin, Holm verdict, ranking, pairing ratio, mistake rate or reference-table
figure moved by so much as a millionth.** **107 of the 124 rows this suite shares with P32's came
back byte-identical.** The seventeen that moved are exactly the ones that count *turns* or *money*:

- **Turns fell by 18 across 16,806 ladder rounds** — 502,830 → 502,812, which is precisely the
  turns those seven rounds used to take (2.6 each, about what a seat waits for its own first turn
  at a five-handed table). ✅ **The feeding-ban denominator — a different column, computed a
  different way — fell by the same 18.**
- **Two claim attempts disappeared**, because a round that ends on the deal never offers the opener
  the turned-up card. ✅ **Seven rounds × the 28.6% attempt rate is 2.0.**
- **The money-sweep margins moved in the fourth decimal**, because nine payouts doubled.

✅ **So §7.4 changed *when* those rounds ended and not *who won them*.** A thirteen dealt complete
stays complete, every rung declares at its first opportunity, and in all nine rounds the seat that
was going to win won — it simply stopped waiting for its turn.

⚠️ **A prediction written down before the run was wrong, and usefully.** It said that if a deal win
occurred, **a win rate and a money figure would move together**, and that money moving alone would
mean the split had landed in the wrong column. **Money moved alone, and the split is fine.** The
column that actually discriminates is the side bet: **all four `money.side-margin.*` rows are
byte-identical**, which is the proof the doubled payment landed in the round column where it
belongs (BUILD-PLAN P35 acceptance 3, and P33's precedent). **A falsifier has to name the column
that could only move if the bug were real, not merely a column that would move if the rule fired.**

### ⚠️ And §7.5 is not here, deliberately

**`RULES.md` §7.5 — a third consecutive win paid entirely by the seat above the winner — has no
row in this document and cannot have one.** Every experiment runs `RoundsPerGame = 1`, so a
three-round streak never occurs. It is audited rather than measured; see §11 for what that leaves
unknown, which is **what the rule is worth** and whether moving seats before somebody's third win
is a strategy.

## 16. The measurement-hardening run

**Added by P48, the packet that verified the tree and discharged the statistical review's
recommendations (findings F1–F7) so that P49 and P50 write against numbers that have been
hardened rather than only regenerated.** Everything here is a *readout over the run the rest of
this document already quotes* — one suite, one master seed — except §16.5, which is a second
seed. ⚠️ **Nothing in §1–§15 moved because of this section**; these are new rows beside the old
ones, not a re-measurement of them.

### 16.1 The margins split by seating composition (F1)

**A head-to-head cell pools every mix in which both rungs sit — from one row seat against four to
four against one. Splitting the margin by that mix answers the question the pooled figure hides:
does a rung lose by the same per-seat amount whoever it is outnumbered by, or does its weakness
compound as its own copies fill the table?** The new rows are `ladder.composition.{pair}.{k}-of-5`,
at the two extremes — the row outnumbered one-to-four, and outnumbering four-to-one.

⚠️ **Read the two compositions of a pair against *each other*, never against the pooled figure.**
The pooled margin is a difference of two ratios-of-sums over different seat-round denominators, so
it is **not** the seat-round-weighted average of the strata and can sit outside their range — for
`random` against `outs` the strata are `−25.0` (one random seat, which never wins) and `−99.4`
(four random seats, the lone `outs` winning almost every game) while the pool is `−39.8`. The
strata carry a raw interval (±2.4–3.0 points, an eighth the games of the pooled cell) and no
correction of their own.

🔥 **`warden`'s loss is partly self-play compounding, and this is the row that shows it.** Against
`warden`, the anti-`warden` margin **widens as the table fills with `warden`s**:

| pair | 1 `warden`, 4 opponents | 4 `warden`s, 1 opponent | pooled |
|---|---:|---:|---:|
| `outs` over `warden` | +6.3 ± 2.4 | **+10.6 ± 3.0** | +7.3 |
| `greedy` over `warden` | +4.3 ± 2.5 | **+6.1 ± 2.9** | +4.5 |
| `simple` over `warden` | −4.3 ± 2.8 | **−2.5 ± 2.6** | −2.8 |

**All three comparators point the same way**: a `warden` does worse the more `warden`s share its
table. `outs` beats a lone `warden` by +6.3 and a table of four by +10.6; even `simple`, which
*loses* to `warden`, loses by less (−2.5 rather than −4.3) once the `warden`s are crowding one
another. ⚠️ **Each pair's two strata overlap within their wide intervals**, so the evidence is the
**consistent direction across three independent comparators**, not any single cell. This refines
§13: `warden` pays a draw for every lock, and a table of `warden`s inflates the round length
together (31.9 turns against `outs`' 24.9 at the bench), so the tax compounds. **The free-for-all
17.9% and the head-to-head +7.3 each describe a *different* composition, and neither is the whole
story.**

✅ **P43's `opportunist` null holds at every composition.** `outs` over `opportunist` is
`−0.8 ± 2.7` with one `opportunist` and `−0.3 ± 2.7` with four — both inside the interval, flat
across the mix. **Denial at zero price buys nothing whoever is holding the locks**, which is a
stronger statement than the pooled null alone: the composition-stratified margin was the exact
instrument P43 asked P48 to point at the free-for-all-vs-head-to-head gap, and it finds no gap.

⚠️ **`sprinter`'s edge over `outs` does not reverse at any composition, but the strata cannot
resolve it.** `outs` over `sprinter` reads `−1.3 ± 2.6` (four `sprinter`s) and `−0.3 ± 2.7` (one),
both inside their intervals — a one-point effect is below what a stratum of ~1,335 games can
separate. It is *consistent* with the pooled `+1.2 ± 0.8` and never points the other way, but this
is the free-for-all's lesson again (§4): the effect is real, small, and only the full paired
sample resolves it. §16.5 is the test that matters for it.

### 16.2 Every ladder pair in dollars (F2)

**The §3 ranking is by win rate, but the game's object is money (RULES.md §4).** A rung that wins
fewer rounds and banks more would be misranked by win rate, so P48 publishes every head-to-head
pair a *second* time in dollars a round — `ladder.money-margin.{pair}`, its own Holm family of
forty-five, from the per-game net series each cell already kept.

🔥 **The two currencies never disagree in direction: zero of the forty-five money margins point
the opposite way to their win-rate twin.** Where win rate separates a pair, money mostly does too
(38 of 45 separated under Holm, 5 inside the interval, 2 raw only) and always with the same sign.
**The win-rate ladder is the money ladder** — a reassurance that was not guaranteed and is now
measured rather than assumed.

⚠️ **The magnitudes are their own small findings.** `outs` beats `warden` by only +7.3 points of
win rate but **+$2.71 ± 0.32 a round** — `warden` bleeds money through the very takes and long
rounds that cost it the win rate. **`outs` banks +$0.97 ± 0.33 a round over `greedy`** (which it
beats by +2.7 points), and — the one that matters most — **`sprinter` banks +$0.45 ± 0.32 a round
over `outs`**, separated under Holm. **A second, independent currency confirms `sprinter`'s edge**:
it is not only winning a shade more often, it is banking more, and both clear their own
correction. That is one more reason the `+1.2` is believed (§3, §8).

### 16.3 A bootstrap coverage check on the money cells (F6)

**Money a round is the heaviest-tailed statistic in this document** — a ×5 jackpot (RULES.md §4)
is a rare, large, one-sided contribution the normal interval's symmetry cannot see. So P48
resamples the two money cells that actually separated — `prospector` at $5/$20 and $5/$40 — ten
thousand times, whole games with replacement, and reads a 95% percentile interval off the
resamples (`money.net-per-round-bootstrap.*`).

✅ **The normal intervals hold.** At $5/$20 the margin is `+$3.99` with a resampled half-width of
±2.41 against the normal ±2.28; at $5/$40, `+$14.20` at ±4.81 against ±4.85. **The percentile
interval sits essentially on top of the normal one** — the tails are not fat enough at eight
thousand games to make the symmetric interval dishonest, so §10's separations stand on the
interval it already published, now with a heavy-tail check behind them.

### 16.4 Intervals on the field rates (F7)

**The rates §12 and §13 compare across fields — round length, how often a lock was live, how often
it bit — were bare points until this packet.** A difference between two of them (the ladder's
lock-bite against the dial's, say) is a claim, and a claim needs an error bar. P48 keeps each as a
per-game (total, trials) series and reports it as a ratio with a standard error, by the same house
method every win rate uses:

| rate | the ladder | the difficulty dial | the claim-permission cell |
|---|---:|---:|---:|
| turns a round | 28.3 ± 0.07 | 31.9 ± 0.28 | 25.5 ± 0.20 |
| lock live | 23.3% ± 0.1 | 23.0% ± 0.3 | 17.8% ± 0.3 |
| lock bit | 30.0% ± 0.2 | 21.9% ± 0.6 | — |

🔥 **The ladder's lock-bite rate (30.0%) is now *separated* from the dial's (21.9%)** — a gap §13
described without an interval and could not previously defend. The dial's field is four `outs`-ε
levels, so its lower bite rate is a real property of a field of near-identical players, not noise.
(The claim-permission cell is two arms of one rung and was never asked for the counterfactual, so
its lock-bite is structurally zero — §13.)


### 16.5 The fresh-seed replication (F5)

**Every figure in §1–§15 descends from one master seed (20260819). Two runs of that seed agree to
the last bit and say nothing about whether a *second draw of the world* would find the same
thing.** P48 runs §3's forty-five head-to-head margins and the dial's three adjacent steps at a
**second** master seed (20260826) and sets them beside the published ones —
`docs/strategy/replication.csv`, the project's first reproducibility statement that is statistics
rather than determinism.

🔥 **The prediction, written before the run: every Holm verdict holds, and every margin lands
inside its own interval of the published value. The first half held exactly; the second was
optimistic.**

- ✅ **Every separation held: 0 of 48 verdicts fell.** Every margin that survived Holm at the
  published seed survives it at the fresh seed. **Not one ranking claim in §3 or §9 depends on the
  seed it was drawn from.**
- ⚠️ **45 of 48 margins landed inside their published interval; three landed just outside**
  (`simple` over `opportunist`, and the `hard`-over-`medium` and `expert`-over-`hard` dial steps),
  each by a thousandth or two, each keeping its separation. **This is not instability — it is the
  interval geometry, and the prediction was the thing that was slightly wrong.** Two independent
  95% intervals leave the second estimate inside the first only about 83% of the time, so **~8 of
  48 were expected outside and only 3 were** — the estimates are *more* stable than the strict
  all-inside prediction assumed. A margin is reproduced by its verdict surviving, not by two
  independent draws landing within one interval of each other.

🔥 **The load-bearing case reproduced.** `sprinter` over `outs` — the tightest separation in the
document, `+1.2` surviving Holm but a dead heat at the crossed table — reads **+1.23 (Holm) at the
published seed and +1.19 (Holm) at the fresh seed.** **The one verdict most likely to fall did not
move.** Together with the money margin (§16.2, `sprinter` banks +$0.45/round over `outs`) and the
moved race-reach mechanism (§3), the fresh seed is what graduates `sprinter`'s edge from *measured*
to *settled*.

⚠️ **The two raw-only casualties split, and both ways are informative.** `angler` over
`opportunist` — `+1.0` raw only at the published seed — **fell inside the interval at the fresh
seed**, so it is read as the null it was flagged as. `sprinter` over `angler` — raw only at the
published seed — **cleared Holm at the fresh seed**, so that small edge is more likely real than
noise, and in the direction the rest of this section already points (`sprinter` at the top). **A
raw-only cell is exactly the one a second seed is for, and here it moved one each way.**
