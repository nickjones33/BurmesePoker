# The difficulty dial away from the default table — P32's monotonicity check

⚠️ **Not part of the standing set, and deliberately not a row in `measurements.csv`.**
`sim suite` measures one table size, and the standing table is five seats. This file is the
evidence for BUILD-PLAN P32's second acceptance — *the dial is separated at five seats, and its
monotonicity at four and six is stated* — kept verbatim rather than transcribed, because the
numbers below are the only ones in this project that are quoted from a console rather than
generated from a CSV.

**Four-handed** is not re-run here: P33's whole standing set was four-handed and is frozen at
`docs/strategy/measurements-4-handed.csv`, under `difficulty.step.*` — all three steps separated
under Holm at 8,000 games a cell.

**Five-handed** is in `docs/strategy/measurements.csv`, from the same command the rest of the
file comes from. The run below at 2,000 games a cell was the pre-check taken *before* the suite
was started, and is kept because it is what the decision to leave ε alone was actually made on.

**Six-handed** is this file's own reason to exist. Nothing else in the project measures it.

🔥 **The finding: one set of ε values, fitted at five seats, is monotone and separated at four,
five and six.** So the dial is one dial and not one per table size — which is the decision
BUILD-PLAN P32 could not take for itself, taken on measurement.

---

## Five seats — the pre-check (2,000 games a cell)

```
tournament: easy, medium, hard, expert at 5 seats, 3 pair(s) + free-for-all + null, 2000 games a cell, seed 20260819
reproduce with: BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --seats 5 --games 2000 --seed 20260819

10088 games in 803.0 s, 0 abandoned at the turn cap

Head to head — the row's win rate less the column's, game by game, in points.
A star is a margin that survived the family-wise correction below.

                        easy          medium            hard          expert
easy                       ·     -9.6 ± 1.6*                                
medium           +9.6 ± 1.6*               ·     -7.7 ± 1.6*                
hard                             +7.7 ± 1.6*               ·     -7.4 ± 1.6*
expert                                           +7.4 ± 1.6*               ·

Ranking, by mean head-to-head margin over the field:

#  strategy       mean margin    free-for-all win %   beat/lost/undecided
1  expert                +7.4           30.4 ±  1.5                 1/0/0
2  medium                +0.9           15.9 ±  1.3                 1/1/0
3  hard                  +0.2           23.8 ±  1.4                 1/1/0
4  easy                  -9.6            9.9 ±  1.1                 0/1/0

3 comparison(s) in the family. Holm-Bonferroni at alpha 0.05: a raw "separated" that does not survive is not a finding.

comparison                          margin            p    threshold  raw         corrected         
easy vs medium                 -9.6 ±  1.6     2.28e-32      0.01667  separated   survives          
medium vs hard                 -7.7 ±  1.6     1.65e-21      0.02500  separated   survives          
hard vs expert                 -7.4 ±  1.6     4.22e-20      0.05000  separated   survives          

Null test — expert against a copy of itself. A fair table gives each 20.0%:

expert                       19.4 ±  0.8   seats 1005/1005/1005/1005/1005
expert#mirror                20.6 ±  0.8   seats 1005/1005/1005/1005/1005
margin                       -1.2 ±  1.6   the harness finds no difference where there is none

Pairing — the per-game difference against the add-the-variances formula, same data:

scope         comparison                                 paired se  independent   ratio
within-cell   easy vs medium                                 0.806        0.573    1.41
within-cell   medium vs hard                                 0.810        0.575    1.41
within-cell   hard vs expert                                 0.802        0.569    1.41

⚠️ Within a cell pairing WIDENS the interval and that is correct: exactly one seat declares, so two strategies at one table are negatively correlated and the independent formula understates the margin. Across cells it narrows, which is the variance reduction common random numbers were for.

```

## Six seats (2,000 games a cell)

```
tournament: easy, medium, hard, expert at 6 seats, 3 pair(s) + free-for-all + null, 2000 games a cell, seed 20260819
reproduce with: BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --seats 6 --games 2000 --seed 20260819

12280 games in 1174.7 s, 0 abandoned at the turn cap

Head to head — the row's win rate less the column's, game by game, in points.
A star is a margin that survived the family-wise correction below.

                        easy          medium            hard          expert
easy                       ·     -7.0 ± 1.3*                                
medium           +7.0 ± 1.3*               ·     -7.0 ± 1.3*                
hard                             +7.0 ± 1.3*               ·     -5.6 ± 1.3*
expert                                           +5.6 ± 1.3*               ·

Ranking, by mean head-to-head margin over the field:

#  strategy       mean margin    free-for-all win %   beat/lost/undecided
1  expert                +5.6           25.3 ±  0.9                 1/0/0
2  hard                  +0.7           19.4 ±  0.8                 1/1/0
3  medium                +0.0           14.3 ±  0.8                 1/1/0
4  easy                  -7.0            7.7 ±  0.6                 0/1/0

3 comparison(s) in the family. Holm-Bonferroni at alpha 0.05: a raw "separated" that does not survive is not a finding.

comparison                          margin            p    threshold  raw         corrected         
easy vs medium                 -7.0 ±  1.3     7.97e-26      0.01667  separated   survives          
medium vs hard                 -7.0 ±  1.3     9.50e-26      0.02500  separated   survives          
hard vs expert                 -5.6 ±  1.3     1.70e-16      0.05000  separated   survives          

Null test — expert against a copy of itself. A fair table gives each 16.7%:

expert                       16.6 ±  0.7   seats 1023/1023/1023/1023/1023/1023
expert#mirror                16.8 ±  0.7   seats 1023/1023/1023/1023/1023/1023
margin                       -0.2 ±  1.3   the harness finds no difference where there is none

Pairing — the per-game difference against the add-the-variances formula, same data:

scope         comparison                                 paired se  independent   ratio
within-cell   easy vs medium                                 0.670        0.475    1.41
within-cell   medium vs hard                                 0.671        0.476    1.41
within-cell   hard vs expert                                 0.680        0.482    1.41

⚠️ Within a cell pairing WIDENS the interval and that is correct: exactly one seat declares, so two strategies at one table are negatively correlated and the independent formula understates the margin. Across cells it narrows, which is the variance reduction common random numbers were for.

```
