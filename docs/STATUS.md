# Build Status

Cross-session progress tracker. **`/poker` reads this first and updates it last.**

Plan: `BUILD-PLAN.md` · Rules: `RULES.md` (rev 23) · Skill: `.claude/skills/poker/SKILL.md`

State markers: `☐` not started · `◐` in progress · `☑` done

---

## Current state

🔥 **P25, P26 and P27 have all shipped (2026-08-21). Two remain: P28 and P29.** Every packet from
§0 is done — P23 shipped 2026-08-20 — but **four sessions with Mya Lay and Aung Aung on
2026-08-20/21 closed twenty-three questions in `RULES.md` §9 and left four settled rules with no
implementation at all.** ✅ **Three of them are implemented now**: P25 the win condition by table
size, P26 the money layer as §4 actually states it, and P27 the feeding ban. **P28 the claim's
permission and per-round seating, P29 re-measure.** ⚠️ **This is a different kind of work from
everything above it: P11–P23 added capability to a correct engine, and P25–P28 make a working
engine play a different game.** **P29 needs all four.**
⚠️ **P24 is re-sequenced after P29 and that is a recommendation, not a decision** — see
`BUILD-PLAN.md` §4.

🔥 **The next packet is P28**, and P27 leaves it two gifts and one line of work: the objection
predicate already exists as **`Card.SameRankAs`** (§9 #30 — read it, do not write a second one),
a re-drawn seating gets fresh feeding bans **by construction** because a `FeedingBan` lives on a
`PlayerState` the deal builds, and ⚠️ **a *refused* claim must not arm §5.1** — which is the one
place P28 has to touch P27's code (`RoundEngine.TakeCard` calls `PlayerState.TookInTheOpen` on
every claim today).

Nothing is in progress and the tree is green at **642 passed / 0 failed** in 9 m 42 s (590 before
P27; the 52 new ones are `FeedingBanTests`, `FeedingBanInPlayTests`, `LegalDiscardsTests` — which
are theories over the whole of `BotCatalog` and `DifficultyLadder` — plus `SeatAnswerTests` and
cases in `CardTests`, `HandViewTests` and `MarkupStandardsTests`). ⚠️ **Only §3, per-round seating,
is now settled with no code behind it.**

⚠️ **The baseline was verified in a `git worktree` at `HEAD`, not in place** — **590 / 0 in
8 m 57 s** — so the session could start writing before it finished. **It is a cheap trick worth
reusing twice over**: `git worktree add <scratch> HEAD && dotnet test` proves the baseline without
stashing, **and the same worktree is then the *before* arm of any measurement the packet wants**,
which is how P27's before/after play numbers were taken.

🔥 **One prediction `STATUS.md` itself made was wrong, and the shape of the error is worth
keeping.** This block said `ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`
was *"expected to go red when P26 starts"*. **It was green at `HEAD`** — the code still implemented
the withdrawn rule, so the test agreed with the code and disagreed only with `RULES.md`.
**A test cannot go red for a rules change until somebody changes the code.** ✅ What the test
*did* do is exactly what it was for: its assertion had to be rewritten deliberately, and the
rewrite was a stated prediction that then came out right (below).
⚠️ **The test suite takes about eight minutes** — **7 m 35 s at P23**, on an otherwise idle
machine (six and a half before P22, two and a half to four before P21, one before P19, 18 s
before P17). ⚠️ **Measure it idle**: the same 548 tests took **16 minutes** in this session while a
`sim suite` was still finishing, which is contention and not a regression. **P21 is still most of it** — every difficulty
level is built on `outs`, so `DifficultyCalibrationTests` alone runs about four minutes — and P22
added a minute or so of its own in `MoneySweepTests` and `SuiteTests`. Everything heavy shares
`WallClockBudgets.Collection` with the two wall-clock budgets, so nothing is starved.

✅ **The feeding ban is built — P27, 2026-08-21, `RULES.md` rev 23.** The block below is the
record of what it was built *from*; what it became is in **Notes for the next session**.
`RULES.md` **§5.1, the feeding ban** —
*you may not discard a rank the next player has taken in the open, until they throw that rank back
or you are going out on it* — is `EXPERT` and **Settled**, and it is **the first rule in the
document that constrains *which* card a player may discard.** `RoundEngine` accepts any of the
fourteen, and `TurnContext` does not show a seat the information the rule is decided from.
🔥 **Two rulings by Nick the same day settle how it is enforced, and neither is safe alone.**
*(a)* **A banned discard is not an infraction but an impossible move** — never offered, cannot be
chosen — so there is no penalty and nothing to retract. *(b)* **Where the ban would leave a player
no legal discard at all, the ban yields for that turn**: the discard is mandatory (§7.1), the ban
is not. **(b) is what makes (a) safe** — impossible-move enforcement removes the social escape
hatch, so without the floor a fourteen-card hand of banned ranks is a turn that cannot be
completed. Together they are one line: the legal discards are *the hand minus the banned ranks, or
the whole hand if that is empty*, **never empty by construction**.
⚠️ **The bill is on `TurnContext` and on every agent**: the upstream seat must be *told* which
ranks are closed (free — the ban is computed from things done in the open, not from reading a
pile, so it holds either way §9 #15 is answered), and every discard ranking must be filtered to
the legal cards, **`FallibleAgent`'s runner-up included**, because a mistake still has to be a
legal move.
✅ **The packet can be written now.** All six details — `RULES.md` §9 **#16–#19, #25 and #27** —
were closed on 2026-08-20 by a session with **Mya Lay and Aung Aung**, and §5.1's specification is
complete. Four are `EXPERT`; ⚠️ **two are `PLAYER` house rulings and stay flagged as such** — that
a release survives the reshuffle (*"nobody really knows"*, was #19) and that taking a joker closes
the other jokers (*"I'd assume"*, was #27). A house ruling that later turns out wrong is a **rule
change**, not a typo fix.

✅ **The same run of sessions changed something bigger than the feeding ban, and P25 implemented
it on 2026-08-21: the win condition is a function of the table size** (`RULES.md` §7.1.1,
`EXPERT`). Two-handed is **series only, sets illegal as melds**; three-handed needs **two**
series; four-handed **one**; five-or-more none — and **the series a table size requires must be
joker-free**, while surplus ones need not be. A longer run may be laid down **split**, so `3+3`
out of a six-card run counts as two series. ✅ **`HandEvaluator` implements all of it since P25**:
`TableRules.For(players)` is the table as data, the evaluator takes it as a parameter and **has no
parameterless overload**, and the search carries what is still owing *along* the partition rather
than auditing a cover it has already chosen. ⚠️ **Every figure in `docs/STRATEGY.md` is still
measured at four seats under the wrong condition** — that is P29, and P25 gave it a first data
point: greedy-vs-simple over 200 games at one seed goes from **25.1 to 26.6 turns a round** and
from **102 to 86 rounds/s**, with greedy's share slipping 32.3% → 31.3%. **A smoke test, not a
measurement.** ⚠️ **§2's "four players minimum" was wrong** — two- and three-handed are real games,
and `RoundEngine.MinimumPlayers` is still 4 (§10 #7), **so the two strictest rules in §7.1.1 are
correct, tested and unreachable from a dealt game.** That is not P25's to fix and no packet owns
it.

🔥 **And a third settled rule has no code behind it, added 2026-08-21: seats are re-randomised
every round** (`RULES.md` §3, §9 #14, `EXPERT`). A *game* means from the turn-up to somebody going
out — **a game is a round** — and the seating is re-drawn between games, so a player's neighbours
change every deal. ⚠️ **`MatchEngine` randomises once at setup and keeps that seating for a whole
match**, which is what P9 chose while the question was open and the recommendation was *nothing
moves*. Recorded as `RULES.md` §10 #16. ✅ **No published measurement moves** — every experiment in
`BurmesePoker.Sim` runs `RoundsPerGame = 1`, so no measured game has a second round to re-seat
for, and `--seating rotated`/`balanced` assign strategies to seats *within* a game. ⚠️ **The two
front ends are where it shows**, and it is not only a loop: P13.5 puts *you at the front whichever
seat you were dealt*, so re-seating every round means the table visibly rearranges itself around a
fixed viewer, and `TableRing` has never been asked to do that between rounds.

✅ **P26 built the money layer on 2026-08-21 and `RULES.md` §10 #17 is discharged.** Jokers are
permanent (`Permanent` holds three values and eight cards), a designation landing on a permanent
value pays **×3**, and the **×5** is `MoneyCardRegistry.Multiplier(card, owner, MoneyOwnership)`
under a configuration `Settlement` reads from `CardOwnership` **once a round**. 🔥 **The design
decision held**: nothing is stored on a card, `Multiplier(Card)` survives as the value-only
question — which is what every view drawing one card at a time is really asking — and that is why
no caller outside `Settlement` needed a parameter. ⚠️ **The ×5 is fenced to the 7♦/A♠ pair** while
§9 #32 is open, and two tests pin the narrowness so widening it later cannot pass silently.
⚠️ **No view can show a ×5** — `CardView.Multiplier` is 0, 1 or 3 by construction — so the jackpot
is settled and never drawn. **A real UX gap, and no packet owns it.**

🔥 **What P26 measured, and it is bigger than the packet expected.** The side-bet went from
**`$8.50` to `$11.58 ± 0.34` a round** at five seats — **42.5% → 58% of the $20 round prize** —
measured with the *same* command before and after (`--games 600 --seats 5 --seed 20260821`, greedy
vs simple, summing each round's positive `side_bet` deltas out of `--csv`). ✅ **The instrument is
validated rather than asserted**: the *before* run reproduces P12's rev-13 published figures
($8.43 / 42% / 30%) at a different seed. ✅ **And play did not move at all** — same seed, same
wins, same turns, same cover — which is the first time §4.4's decoupling claim has been measured
*across* a change to the money layer. 🔥 **The packet's own stated prediction came out right**: a
designation on the 7♦ and one on an ordinary card leave **exactly the same** money loose in the
shoe, so §4.1's arithmetic is sound and the rule did not need re-asking.

⚠️ **P26's one surprise is that "domain only" was true of the logic and not of the text.** `($$)`
had to become `($$$)`, `CardDisplayState.PaysDouble` became `PaysTriple`, and four user-facing
strings said *double*; leaving them would have printed a false number to a player. 🔥 **So a
`drive-console.py` capture from before P26 no longer compares** — P23's promise that a pre-P23
capture still does is spent.

🔥 **And on 2026-08-21 the money layer's last two answers added a fourth unbuilt rule and broke a
shipped test.** *(The record of what P26 was built from.)* `RULES.md` is **rev 20**. **(1) Claiming the turned-up money card needs the
permission of the seat that plays *before* you**, who may refuse only if they hold that card —
because your public take arms §5.1 against them and locks them into holding it (§4.5, `EXPERT`).
**The first rule tying the money layer to the feeding ban**, and it independently confirms §9 #16
and #17, which were recommendations when they were written. ⚠️ **It needs a third kind of agent
decision — *do I object* — asked of a seat that is not on turn**, which no `SeatPrompt` in
`BurmesePoker.Server` does; and **the answer is a disclosure**, since only a holder may object
(§10 #18). **(2) A turned-up 7♦, A♠ or joker can never be owned, claimed or not, and its partner
copy pays ×3** — superseding the *double* recorded from the same people one day earlier.

❌ **This is the first rules change that invalidates a published measurement.**
`MoneyCardRegistry.Multiplier` is `(permanent ? 1 : 0) + (designated ? 1 : 0)` and caps at **2**;
it must return **3**. `MoneyOdds.PerBlindDraw` prices a blind draw from it, `prospector` is the one
rung whose decision reads the money, and **`docs/STRATEGY.md` §10 — the money sweep, twelve rows of
`measurements.csv` — was measured under the struck rule.** ⚠️ **P22's `DERIVED` note is withdrawn**
(*a designation landing on a permanent money card leaves less money in the deck*) — the reasoning
was sound and the premise moved. ✅ **`ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`
asserts the withdrawn direction and will go red**, which is the good case: the derivation was
written as an assertion rather than as prose, so the rules change cannot pass silently. 🔥 **Its
last assertion should invert to an equality** — a designation on the 7♦ and one on an ordinary card
leave the same money loose — **and that is a prediction, not a measurement.**

⚠️ **§9 has two questions and both were raised by rev 20's answers**: whether an objection turns on
the exact card or the **rank** (#30 — §5.1 is rank-only, and the stated reason covers a rank-mate),
and whether a turned-up **joker**'s partner is really ×3 (#31 — the same multiplier *conserves* the
money on a 7♦ and *creates* it on a joker). **`QUESTIONS-FOR-MYA-LAY.md` Q8 carries both, and says
to ask #31 without mentioning the 7♦.** See **Decisions needed from Nick**.

🔥 **P23 is done (2026-08-20): the standing answer, and the last packet in the plan.**
`docs/STRATEGY.md` is one document that answers *"which bot should I play, and what actually works
in this game?"*, and it regenerates from one command. **Read `STRATEGY.md` before anything else if
the question is about strategy; read this block if the question is about how it is kept true.**

🔥 **The headline is the reproduction. 59 of the suite's 77 rows came back byte-identical.** Every
ladder cell, the harness's own null test, all twelve pairing ratios and both of P12's headline
rows — from a tree that had since gained a rung and changed what the standing field is. **The
seven rows that moved are the difficulty dial and only the dial**, which is the one thing this
packet meant to move; the twelve that are new are the money sweep, which §10 now quotes from
`measurements.csv` instead of from a file of its own. **That is the join P22 left open, closed.**

🔥 **The re-calibration moved exactly one value, and that is the finding.** P19 placed the four
levels against `greedy`; P21 re-based them onto `outs` without re-spacing, leaving a reference
table with steps of **8.2 / 4.3 / 10.3** points — monotone, passing every standing check, and
visibly not a dial. Re-sweeping ε on `outs` over P19's own seven probes and inverting the curve
moved **`hard` from ε = 0.5 to ε = 0.4 and left `easy` (0.9), `medium` (0.7) and `expert` (0.0)
alone**; the reference table is now **7.9 / 6.7 / 7.7** and the head-to-head steps are
**+11.20 / +9.80 / +6.82**, ± 1.00, all three surviving Holm at 6.8× the half-width or better.
⚠️ **Why only one: the ε curve has nearly the same shape on a rung that looks ahead as on one that
does not** — 0 → 0.5 costs 9.5 points on `outs` against about 8 on `greedy`, and 0.5 → 1 costs
16.5 against about 17. **A mistake rate is close to being a property of the mistake rather than of
the rung it is made against**, so the next rung to raise the ceiling should expect to *re-check*
this and not to re-derive it — but not to skip it, because skipping is what produced the flat spot.

🔥 **Two instruments, one dial, and they do not agree about spacing.** The reference table's steps
are 7.7 / 6.7 / 7.9 with the **middle** narrowest; head to head they are 6.8 / 9.8 / 11.2 with the
**top** narrowest. They are different measurements — a head-to-head cell holds two levels and a
reference table four, and two strong players deal a longer round than two weak ones. The re-fit
improved *both* (reference spread 6.0 → 1.1 points, head-to-head 5.1 → 4.4), so nothing had to be
chosen; ⚠️ **`STRATEGY.md` §9 records the tie-break for next time — the reference table wins**,
because a person who asks for a mixed table is sitting at exactly it.

🔥 **How P22's bill was paid, and it was not by typing a shorter field.** `BotRung.Ranked` is new:
each rung declares whether **win rate** or **money** settles it (`RankedOn`), `BotCatalog.Ladder`
and `BotCatalog.StakesSensitive` are **filters of `All`**, and the ladder tournament measures one
while the money sweep measures the other. So `prospector`'s six duplicate cells are gone and
**a rung still cannot be added without being measured**. ⚠️ **The wall clock was the smaller half
of the argument**: six null cells in a Holm family make **every real verdict in it harder to
reach**, and a duplicate is not a free row. ✅ **Measured rather than argued** — the run without
them reproduced every ladder figure to the digit.

⚠️ **Acceptance 2 is a test now, and it caught the tree in the state it was warning about.**
`Tests/Sim/StandingAnswerTests.cs` reads `docs/strategy/measurements.csv` and fails the build if
the published ε values are not the ones `DifficultyLadder` offers; if a published step did not
survive Holm; if a rung in `BotCatalog` is the subject of no published row; if the ladder and the
sweep are not between them the whole catalog; or if a front end writes out a level's or a rung's
description rather than asking for it. 🔥 **On the first run it failed on `prospector`** — exactly
the drift §11 had been recording in prose. **A default is not a guarantee**: P20 made the field
default to the catalog and nothing went red when nobody re-ran the suite.

⚠️ **Three things a future session should know.**
1. **The suite is five hours** (17,539 s for 77 measurements; 105 min at P21 for a smaller
   standing set). **The structural saving has been taken and there is not a second one.**
   `--pairs adjacent` on the ladder would take fifteen cells to five and **throw away §3's
   matrix**, which is the document's centre — it stays available and stays unused.
2. **Re-spacing the dial is not a play change and it was proved byte for byte.** `expert` is
   ε = 0 and `FallibleAgent` defers entirely at ε = 0, so a capture at `--pick 0` from `HEAD` and
   from this tree is **identical from the `Seating:` line on — 7,025 bytes (`--script bots`) and
   88,805 (`--script human`)**. Only the prompt above that line differs, because `hard`'s
   description reads *two times in five* now. ⚠️ **A capture from before P23 still compares**,
   which is not true of one from before P21.
3. 🔥 **ε = 0 and ε = 0.1 are indistinguishable** — 33.6% against 33.7%, ±1.6 on each. *The best
   card and the second-best card are usually the same card*, which is why the top of the dial has
   to be ε = 0 and why a fifth level between `expert` and `hard` would be two names for one
   opponent.

🔥 **P22 is done (2026-08-20) and the answer is "no, and here is how far away yes lives".**
`prospector` is `outs` with one change: a card taken from anywhere but the deck — the previous
player's discard, or the turned-up money card — must be worth more than the **ownership** a blind
draw would have conferred (RULES.md §4.4). At **$5/$1 the rule never fires at all**, and that is
an identity rather than a measurement: two tables, one rung each, dealt from the same shoes, play
the same rounds card for card. So the standard-stakes head-to-head cell is a **null cell**, and
at **`+0.01 ± 0.22`** a round it is the tightest one this harness has produced.
🔥 **And the yes, which is why it is a sweep and not a cell.** At **$5/$40** — a money card worth
eight rounds — `prospector` all but stops taking (**0.1%** against `outs`' 24.9%), wins **20.1 ±
0.9 points fewer rounds**, and banks **`+7.34 ± 3.29` a round**, surviving Holm at `p = 1.3e-05`.
The four cells are monotone in the stakes: $5/$10 is `−0.86 ± 0.82` (raw only), $5/$20 is
`+0.95 ± 1.63` (break-even, inside the interval). ⚠️ **This is the programme's first published
divergence between money and win rate** — at $5/$40 a reader ranking by win rate would rank the
better player last — and P12 split the two columns three packets before anybody needed them
apart. `docs/STRATEGY.md` §10 carries it; `docs/strategy/money.csv` is the data.

⚠️ **The bill P22 leaves behind is P23's, and it is the biggest single thing to know.**
`prospector` is one entry in `BotCatalog` (P18), so the suite's ladder field picks it up **by
construction** — **21 head-to-head cells against 15**, taking `sim suite` from an hour and three
quarters to about **three and a quarter hours**. It was therefore **not re-run**:
**`docs/strategy/measurements.csv` is one rung behind the catalog**, §10 of `STRATEGY.md` is
generated from `money.csv` instead, and §11 says so out loud. 🔥 **Six of those 21 cells are
`outs` against itself in all but name** — about three quarters of an hour reproducing a fact a
unit test already asserts — which is an argument for `--pairs adjacent` on the ladder as well as
the dial. ⚠️ **Do not fix it by hand-typing a shorter field**: that is the exact defect P18 and
P20 each had to remove, one layer apart.

✅ **The difficulty dial did not move and needed nothing.** `prospector` shares `outs`'
`Strength: 3`, and `BotCatalog.ByStrength` breaks the tie in ladder order, so
`BotCatalog.Hardest` is still `outs` — every level is still built on the rung P19 measured, the
stand-in seat and the browser's hint are unchanged, and no front end needed a line. A console
capture from before this packet still compares.

🔥 **Two findings worth more than the rung.** **(1) A rung's strength stopped being a property of
the rung.** Every rung before this plays the same game whatever the table is played for;
`prospector`'s one decision reads `Stakes.MoneyCardValue` against `Stakes.RoundValue`, so *"how
good is it"* has no answer until somebody says what the stakes are — which is why it shares
`outs`' ordinal rather than sitting above or below it. **(2) A `DERIVED` rules note fell out of
the arithmetic: RULES.md is rev 15.** A designation that lands on a **permanent** money card
leaves the deck with *less* money in it, not more — turning up a 7♦ makes that value a double but
takes the physical card out of the deck (§3 step 4), so one 7♦ worth $2 is left where an ordinary
designation leaves a partner worth $1 **and** the 7♦s untouched. **Doubling one value is not the
same as designating a second.** Found by writing a test that asserted the opposite.

⚠️ **Two things that will catch the next session out.** **(1) The $5/$1 identity cannot be shown
from a head-to-head cell**: two labels of one player sit in different seats there, so their
aggregates differ by seat luck however identical they are — the small-`n` version of that test
failed exactly this way before being rewritten around two *homogeneous* tables. **(2) The
exchange rate is the rung's one free parameter and it is a constant on purpose** — a rung with a
knob is a family of rungs, and a family cannot be measured against the one below it in a single
cell (P15). It is documented as a model and its bias is stated: it overvalues early melded cards,
so it takes the discard more often than a sharper model would, which moves the crossover **down**
rather than up.

🔥 **P21 is done (2026-08-20) and the answer is yes — the first rung in the programme that
beats `greedy`.** `outs` is `greedy` with one thing inserted: where two discards leave the hand
equally melded, it keeps the thirteen that **more of the pack would improve** — a count, per
candidate, of how many of the values still out there would raise the cover count of what is left
(`Domain/Agents/OutsBotAgent.cs`, `LiveOuts.cs`). At 8,008 games a cell it measures
**`+3.1 ± 1.0` points against `greedy`**, `p = 1.9e-09`, surviving Holm, and takes the
free-for-all **26.3 ± 0.5 to 23.7 ± 0.5**. Three research packets had produced nothing above
`greedy`; this clears the apparatus's resolution three times over.

🔥 **The *why* is a one-line contrast, and it is what P22 inherits.** `cautious` and `counting`
both put their new idea **underneath** `CoverScore.Potential` — they decide only what greedy had
already given up on, and that residue is worth about half a point. `outs` puts its key **above**
it: where the cover count ties, the outs count decides, and greedy's own tie-break is demoted to
breaking *its* ties. ⚠️ **P20's "change which question is asked" turns out to mean "and ask it
earlier than the question you are replacing."**

⚠️ **It is `Strength: 3`, and that re-bases the difficulty dial.** Every level is
`BotCatalog.Hardest` with an ε, so `easy`/`medium`/`hard`/`expert` are all `outs` now, and the ε
values **0.9 / 0.7 / 0.5 / 0.0 are no longer the ones that were measured** — they were spaced
against `greedy`. The dial is still *ordered* (`DifficultyCalibrationTests` at the reference
table, and the suite's standing monotonicity check), so nothing is broken and nothing fails;
what is stale is the **spacing**, and **P23 owns re-spacing it — it is no longer optional.**
⚠️ Re-run P19's ε sweep first: ε was violently non-linear on `greedy` and there is no reason the
curve has the same shape on a rung that looks ahead. ⚠️ **A console capture from before P21 no
longer compares**, because `expert` is by definition the strongest rung and the strongest rung
changed.

🔥 **The cost was the packet, and the profile was the surprise.** The naive rung came in at
**12.6× a greedy round**, over the budget the packet set itself. Four things, every one of them
*around* the evaluator and none inside it, brought it to **8.2×**: refine only the candidates
already tied at the top (**7.1 a turn, not 13**); prune values that could not enter a meld at all
(**34 searched of 53**); ask the search for a **bar to clear rather than a maximum**
(`PartialCover.CoversAtLeast` — the search fell from ~98 µs to ~10 µs); and build **one meld
index per candidate instead of one per probe** (`CoverProbe`). ⚠️ **`PartialCover.Best` was not
touched and `HandEvaluator` does not know any of it exists.**

🔥 **And then the finding that outlives the rung.** With the search cheap, **three quarters of
what was left was candidate generation — and it was a fixed cost that did not depend on the hand
at all.** `RunGenerator` allocated all ninety rank windows afresh for each of four suits on every
call, and both generators walked suits and ranks that could not hold a meld. A precomputed window
table, one slot buffer a length, and two feasibility checks made `PartialCover.Best` — and so
**every rung, every hint and every engine turn — about 45% faster** (`greedy` went 48.8 → 71.8
rounds a second serial, in the same process). **§3.7 item 4 said allocation was the thing to
attack and was right; what it got wrong was where.** ✅ **Proved to be a refactor and not a
change**: `scripts/drive-console.py` captured both scripts at `--pick 0` from `HEAD` and from
this tree before the promotion, **identical byte for byte, 8,763 and 92,172 bytes.**

⚠️ **Three things to know before touching this rung.** (1) **`OutsCache` keys on card *values*
and never on a `CardId`**, which is exactly why it may outlive a deal where `counting`'s memory
may not (P13.4) — and it earns only a **9% hit rate**, so it is kept for being free rather than
clever. (2) **Only the discard changed**; taking, claiming and declaring are greedy's, so the
difference attributes to one decision (P15). (3) 🔥 **The prune is a claim and is asserted, not
argued**: `ThePruneNeverThrowsAwayARealOut` counts the outs the long way — every value, through
`PartialCover.Best` — and demands the same number, and the same discipline covers the bar search,
the probe and the cache. That is P21 acceptance 3, and it is the reason to trust any of the
speed-ups.

🔥 **The promotion broke two concealment tests, and they were wrong rather than the code.**
`ConcealmentTests` (P13.2) and `SeatBoardTests` (P13.4) both asserted that four seats' hands,
taken over a whole round, are **pairwise disjoint by `CardId`**. With `outs` standing in for the
unanswered seats the fixture's round ran **67 turns and exhausted the draw pile**, so the
discards were shuffled back into it (`DiscardsReshuffled`, 54 cards, RULES.md §5) — and a king
one seat threw away on turn nine became a king another seat drew on turn fifty. ⚠️ **Public on
the way out, private on the way back, and a breach at neither end.** Both tests now allow exactly
the cards the table itself handed round (`Tests/Server/PublicRelease.cs`, which carries the whole
argument), and the strict form of the same rule —
`ConcealmentTests.EveryCardASeatIsSentIsOneItMaySee`, which asks what the fan-out *sent* rather
than what a hand happened to hold — **never needed relaxing and was green throughout.**
⚠️ **The lesson is not about concealment.** It is that a test over a *played round* can be
asserting a property of that round's length, and nothing says so until something changes how
long rounds are. **A stronger bot is a longer round.**

⚠️ **The suite is now an hour and three quarters** (105 minutes; 35 before P21), and 65
measurements. Head-to-head is `k(k−1)/2`, so six rungs is fifteen cells against ten; `outs` is in
five of them at ~8× a `greedy` round, and the dial's four cells went up with it because every
level is `outs` now. **`--pairs adjacent` is the escape** (P19) if it stops finishing in a
sitting. ⚠️ **The local test suite went the same way for the same reason** — 2m 16s → 6m 33s.

⚠️ **`Domain` now has an `InternalsVisibleTo BurmesePoker.Tests`, and it was needed.** The
prune, the cache and the probe search are *how* a rung is written rather than what it does, so
they are internal — but each is a shortcut past a search that already works, and a test cannot
assert what it cannot see. `BurmesePoker.Tests/Play/TurnContexts.cs` uses the same access to ask
a rung one question without a round around it.

✅ **P20 is done (2026-08-20) and the answer is no — a null result, published.** `counting` is
`cautious` with a **memory**: it estimates what is left in the shoe from every card it has been
shown this round rather than from its own thirteen. At 8,008 games a cell it measures
**`+0.3 ± 1.0` points to `greedy`'s side of the margin** — not separated, and the point estimate
pointing the *wrong way*; `cautious` is `+0.8 ± 1.0` ahead of it. `docs/STRATEGY.md` §8 carries
the entry, which is what P20 acceptance 1 asked for in advance.

🔥 **The finding is *why*, and it is the constraint P21 inherits.** The memory works — a test
shows the supply estimate falling below `cautious`'s for every card watched go by and holding at
the full two copies for every value never shown. It cannot pay for two measured reasons.
**(1) The information set is tiny.** Under the cautious default the memory runs **12 → 23 cards
across a whole round, out of 108** — about ten cards learned beyond its own hand, roughly one a
turn, a fifth of the shoe. **(2) It enters where nothing is paid.** It sharpens `ThreatScore`,
which *is* `cautious`'s tie-break, and P17 had already measured that tie-break at `−0.2 ± 1.0`.
⚠️ **A sharper input to a decision rule already shown not to matter is worth nothing, and the two
nulls compound rather than add.** So P21 must change **which question is asked**, not improve an
answer to a question that does not matter — which is exactly what P15's *"has to be
combinatorial"* meant. **Two of three research rungs have now returned nothing.** ✅ **P21 was the
third and it separated** — see the block above; the prediction in this paragraph was right about
*what to change* and wrong about the odds.

⚠️ **Two things P20 did that were not in its build list.** (1) **`ThreatScore` was extracted from
`CautiousBotAgent` unchanged**, because the two rungs differ in exactly one thing — a `Supply`
delegate — and two copies of that arithmetic would be two places for it to drift; P15's "one
change against the rung below" is a claim about code before it is a claim about results. (2) 🔥
**The ladder was written out in three places and a fifth rung made all three wrong at once**:
`tournament` and `suite` both defaulted `--strategies` to a hand-typed
`random,simple,greedy,cautious`, so a new rung was measured only when somebody remembered to name
it. **The default is now `BotCatalog` itself** — P18's defect one layer up, in a front end nobody
thinks of as one, and it moves half of P23's job forward.

✅ **The rules question was not decided in code.** RULES.md §9 #15 — *is a discard pile
inspectable, or only its top card?* — stays open at **rev 14**, `QUESTIONS-FOR-MYA-LAY.md` carries
it as a flat table situation, and the bot counts only what it has been shown: **wrong in the
direction that does not cheat.** ⚠️ **If the answer comes back that the piles may be read, this
rung gets a far larger information set and deserves re-measuring before it is written off** —
the 12 → 23 figure is exactly the cost of the cautious default.

✅ **Acceptance 5, throughput: the memory is free.** `counting` does **77 rounds/s** against
`cautious`'s **76** and `greedy`'s **88**, at 2,000 games, 4 seats (P12's baseline: 51 serial,
85–92 parallel). Only the idea costs anything.

⚠️ **The suite is 52 measurements in 35 minutes now** (41 in 34 before), and it is `k(k−1)/2`:
five rungs is 10 head-to-head cells where four was 6. **P21 makes it 15, and `outs` is the
expensive rung** — check the suite still finishes in a sitting before promoting it, and remember
`--pairs adjacent` exists. ✅ **Discharged, and the warning was justified**: P21 took it to 65
measurements in **105 minutes**, which is still one sitting and will not survive another rung
like it.

✅ **P19 is done (2026-08-19): the difficulty dial, calibrated — and §0's fifth goal now has its
*product* half finished.** `BurmesePoker.Domain/Agents/DifficultyLadder.cs` holds four
`DifficultyLevel`s — `easy`, `medium`, `hard`, `expert` — each the **strongest rung there is
(`greedy`) with a measured mistake rate**: with probability ε it throws the card that rung ranked
*second* rather than the one it ranked first, and nothing else about it changes.
`FallibleAgent` is the decorator, `IRanksDiscards` is what makes a plausible mistake possible,
and `CoverScore.Discard` is now **defined as the head of `CoverScore.Ranking`** rather than as a
second loop that agrees with one. Both front ends offer **levels only**; `BotCatalog` stays the
research instrument and is what the harness ranks (§3.12).

🔥 **The calibration, and it is the packet.** ε turns out to be a far bigger dial than anything
on the skill ladder — `greedy@0` against `greedy@1` is **+33.3 ± 1.6** points head to head,
three times the whole `simple`-to-`greedy` gulf — **and violently non-linear**: the sweep put
ε = 0 → 0.5 at about **8** points of win rate and ε = 0.5 → 1 at about **17**. ⚠️ **So levels
spaced evenly in ε would not be spaced evenly in play**, and the shipped values are spaced by the
measurement instead: **0.9, 0.7, 0.5, 0.0**. At the reference table (all four at one table, 256
assignments, 8,192 games) they measure **14.50 ± 0.69 / 22.17 ± 0.79 / 27.22 ± 0.83 /
36.11 ± 0.87**, and head to head at 8,008 games a step the margins are **+10.79 ± 1.00**,
**+5.69 ± 1.01**, **+9.90 ± 1.00** — **every one separated under Holm**, the narrowest of them
5.7× the half-width. **Four levels rather than three, and the count is a measurement.**

⚠️ **`--pairs adjacent` was needed and was not in the plan's build list.** Acceptance 1b asks
that the family be the k−1 steps rather than the round-robin — only "n+1 beats n" is a claim the
menu makes — so `TournamentOptions.Pairs` decides which pairs are *played* as well as which are
corrected, and a four-level dial costs three head-to-head cells instead of six. 🔥 **It found a
latent assumption**: `PairingChecks` read the *field* to choose the two cells it compares across
and threw `Sequence contains no matching element` on a run where most pairs never met. It reads
the **cells that were played** now, which is the same answer for a round-robin.

🔥 **A `TurnContext`'s hand is the engine's own list, and P13.1's finding arrived in the test
project.** The obvious way to test "the mistake is the card the rung ranked second" is to keep
the context and ask the rung for its ranking afterwards — which asks it about the **thirteen**
that were kept rather than the fourteen it chose from, and yields a confidently wrong expected
card. `RecordingAgent` records the ranking **during** the turn now. Found by writing the test.

⚠️ **The mistake has exactly one site, deliberately.** Taking the discard, claiming the turned-up
card and declaring are all strict-improvement or must-answer questions with no plausible
second-best, so ε is one dial rather than three — which is what makes "about seven points a step"
attributable to anything. A `FallibleAgent` **refuses an inner player that cannot rank its own
discards**, so `random` can never carry an ε that silently does nothing.

✅ **Acceptance 3 verified end to end.** `expert` (ε = 0) plays the match `greedy` plays, byte
for byte: **7,371 bytes for `--script bots` and 90,726 for `--script human`, identical from the
`Seating:` line on**, captured from `HEAD` and from this tree with `--pick 0` in both. ⚠️ **The
`--pick` map has changed**: it is now `0` expert, `1` hard, `2` medium, `3` easy.

⚠️ **`sim suite` gained a second standing check.** It already exited non-zero when the harness's
null test failed; it now also fails if **the dial stops being monotone** — a rung that raises the
ceiling (P20–P22) raises every level and moves the calibration, so the menu cannot go stale
quietly. The suite is **41 measurements in 34 minutes at `--games 8000`** (~120,000 games), and
**every ladder figure reproduced P17's exactly**.

⚠️ **Three things only pressing the buttons found** (§3.11 B — "only playing it finds these").
(1) The mixed-table **checkbox row inherited the form's ten-rem label column** — its label is the
question rather than the caption, and it wrapped onto three lines; `.opening .mixed` is the
override. (2) 🔥 **`--mixed` is silently ignored without a value.** The command-line
configuration provider records a switch only when it carries one, so the site booted, said
nothing, and gave every seat `expert`; it is **`--mixed true`**, exactly as `--hints false` is.
(3) A seat name is truncated with an ellipsis at the panel's width, and `Aung Aung (medium)` now
reaches it — the whole name is on a `title` so a hover says it, which helps every long name and
not only these.

⚠️ **Two things P19 deliberately left.** (1) A computer seat in the browser is now named for how
it plays — `Mya Lay (expert)` rather than `Mya Lay (bot)` — because a mixed table nobody can tell
apart is a mix nobody asks for twice; the console's bot names are untouched, which is what keeps
the byte-comparison above meaningful. (2) The console offers one level for the whole table; only
the browser can open a **mixed** one (`--mixed`, or the lobby form's checkbox), which is all
acceptance 6 asks for.

✅ **P17 is done (2026-08-19): the tournament✅ **P17 is done (2026-08-19): the tournament, and the first honest interval this harness has
printed.** `BurmesePoker.Sim -- tournament` plays every unordered pair head to head, reports the
free-for-all beside it, ranks the field, and puts every margin through a **Holm–Bonferroni**
correction; `-- suite` generates `docs/strategy/measurements.csv`; `docs/STRATEGY.md` is created
and quotes that file. **Two independent 15-minute, ~64,000-game runs of the suite wrote a
byte-identical file.**

🔥 **The finding that changed the code, and it was found by reading a number that should not have
moved.** P17 says "every figure becomes a `Measurement` — a mean over games, one value per game".
Built literally, the balanced headline came back **30.6% / 19.3%** where P16 published
**29.6% / 20.4%**. Neither is wrong: they are **two different estimands**. A strategy holds a
different number of seats in different games of a crossed run, so *the unweighted average of the
per-game ratios* is not *the ratio of the totals* — and it over-weights the games where the
strategy held fewest seats, which for a strong strategy are the games it does best in. ⚠️ **The
gap was 1.05 points, the size of P16's entire rotated-versus-balanced effect.** Adding an interval
to a figure must not change the figure (§3.8 item 4), so `GameValue` carries **a total and the
trials it is out of** and `Measurement.Of` is the **ratio estimator** — totals divided, with the
standard error built from the per-game residual `total − ratio × trials`. The game is still the
trial. Where the denominator is constant it reduces *exactly* to the per-game mean, and the
balanced headline now reads **29.75 ± 0.47 / 20.25 ± 0.47** — P16's number, with an interval on
it for the first time.

🔥 **"Paired is narrower" was half backwards, and the correction is a fact about this game.**
Pairing measures the correlation that is actually there. **Across cells** — one strategy at two
tables of one master seed — the shoes are shared, the correlation is positive, and the interval
**narrows** (ratios 0.57–0.95). **Within a cell** — two strategies at the *same* table —
**exactly one seat declares**, so the series are strongly negatively correlated and the interval
**widens**: measured at **1.408, 1.409, 1.414** across the three cells of thinking players, which
is √2 to three digits. ⚠️ **So the add-the-variances formula is *anti*-conservative on a
head-to-head margin, by 41%.** BUILD-PLAN P17 acceptance 3 is amended to say so.

**The measured answer, 8,008 games a head-to-head cell** (`docs/STRATEGY.md` has the matrix):
`random` loses by **49.7–49.9 ± 0.4**, `simple` loses to `greedy` by **11.2 ± 1.0** and to
`cautious` by **10.8 ± 1.0**, and 🔥 **`greedy` vs `cautious` is `−0.2 ± 1.0`, p = 0.70** — the
strongest confirmation of P15's negative result yet, from a design P15 never ran. **Three
separated skill levels, four rungs.** The ranking is `greedy` +20.30, `cautious` +20.28, `simple`
+9.25, `random` −49.83 mean margin; the free-for-all (8,192 games, 256 seatings) is 35.8 ± 0.9,
36.5 ± 0.9, 27.3 ± 0.8, 0.1 ± 0.1. ⚠️ **`cautious` is ahead of `greedy` in one table and behind
it in the other, both inside the interval** — which is precisely what the correction exists to
stop a reader promoting to a rung.

✅ **The harness's own null test passes and is checked on every run.** `cautious` against a copy
of itself under another name: **25.1 ± 0.5 and 24.9 ± 0.5**, margin **+0.3 ± 1.0**, against a
fair share of 25.0%, with each label sitting in each seat exactly 4,004 times. `sim suite` exits
non-zero if it ever fails.

⚠️ **What P17 can and cannot resolve, which is what P19–P22 have to budget against.** At 8,008
games a cell the 95% half-width on a margin between two thinking rungs is **1.02 points**. **A
rung worth less than about a point is not promotable at the default run size**, and half a point
costs about **34,000 games a cell** — four hours, and very nearly the 32,000 P15 had to run.

⚠️ **Three things P17 deliberately left.** (1) `neighbours` still uses `Measurement.Difference`,
whose own remark admits it is being conservative about a pairing it could take — `Paired` exists
now and would narrow P16's intervals, but re-running would change published numbers and belongs
with a re-measurement. (2) `NeighbourCell.TakeRate` averages per-game ratios unweighted, the
estimand P17 moved away from everywhere else; its *win* rate is unaffected, because a cell has
exactly one focal seat and so a constant denominator. (3) The suite's standing set is a hand-kept
list in `Suite.Run` — P23 owns making that a test.

⚠️ **One display defect fixed in passing**, because it is one shared helper and changes no
number: a .NET **two-section format picks its section from the *rounded* value**, so `-0.04`
under `"+0.0;-0.0"` prints as **`-+0.0`** — the positive section supplies the plus and the
runtime prepends the minus. `ReportNeighbours` had it in every signed column. Signed figures are
built by hand now.

**Everything built so far is done.** P0–P12, P13.1–P13.6 and P14–P16, and **all four of §0's
original goals are delivered**: solo play against the computer (P10), a console worth sitting at
(P11), simulation at scale (P12), and — as of P13.6 — **a multiplayer browser app with AI
seats.**

🔥 **A fifth goal was stated on 2026-08-19 and planned the same day: a designed difficulty
system, and a settled answer to what actually works.** It is **P17–P23**, and it is two jobs
kept deliberately apart — a *product* (difficulty as a table setting, per seat, in both front
ends) and a *programme* (enough analysis and simulation to say which ways of playing are better,
by how much, with an interval). **§3.12 is the design decision taken in advance**: *difficulty is
a dial, skill is a ladder, and they are not the same axis.*

**The two facts that shaped the plan, both established while writing it.**

1. 🔥 **The default report prints no interval at all.** A balanced 2,048-game run on 2026-08-19
   gave `random` 0.0%, `simple` 26.7%, `greedy` 36.1%, `cautious` 36.8% — and the last two are
   two numbers P15 needed **32,000 games** to establish are the same number. `Measurement`
   exists and is used by exactly one verb (`neighbours`). **P17 comes first because a ladder
   calibrated on point estimates is calibrated on nothing.**
2. ⚠️ **There are four independent notions of *which bot*** — `Sim.StrategyCatalog`,
   `Console.Program.Difficulty` (a private two-value enum), `Web.HostedTable` (`new
   GreedyBotAgent()`, hard-coded, twice) and `Server.TableOptions.StandIn`. **The browser has no
   difficulty setting at all.** P18 makes it one list, so every later rung reaches all three
   front ends for free.

⚠️ **P19 is where the difficulty system is finished** — with the rungs that exist today.
**P20–P22 are independently droppable, in preference order**; each adds one rung and one measured
answer, and P15's precedent (a plausible rung worth +0.5 ± 0.55 points) is why none of them is
allowed to be load-bearing.

✅ **P13.6 is done (2026-08-19): the lobby, a second person, and §0's goal 4.** `dotnet run
--project BurmesePoker.Web` opens a lobby at `/`, a table at `/table/{id}`, and **two people and
two bots play a round over a network** — P13's original "done when", reached with everything
under it already tested. **Verified twice: in a test (`TwoPeopleTests`) and in the real browser,
two tabs, two seats, each shown its own hand and neither shown the other's.**

**What it changed rather than added to.**

- **`TableHost` is gone.** `Lobby` is the singleton and holds `HostedTable`s by id; `TablePlan`
  is what one is opened with. `TableView` injects the lobby and finds its table by the id its
  URL carries; **nothing else in the client counts tables.**
- 🔥 **A `SeatBoard` belongs to a viewer, which is the thing P13.4 said had to change.**
  `TableView` sits down, holds its own seat, hands it to `YourSeat` as a parameter, and stands
  up in `Dispose`. `YourSeat` no longer injects anything at all.
- **`TableSession` learned who is sitting where** — `SitDown`, `StandUp`, `RemoteSeats`,
  `WaitingFor`, `IsFull`, plus `TableEvent.SeatTaken` and `SeatLeft`. ⚠️ **The claim is the
  table's and not the lobby's**: two viewers handed the same `SeatConnection` are two people
  answering one question, and a seat is a property of a table.
- 🔥 **The table deals while somebody is at it** — at least one viewer attending *and* every seat
  either the computer's or somebody's. That is P13.4's leftover finally answerable, because a
  lobby knows whether anybody is connected. **The patience was not shortened**, which is what
  P13.4 asked for.

🔥 **Eight findings a cold context needs.**

1. 🔥 **A test that a stood-up seat refuses an answer is vacuous unless a question is standing in
   front of it.** The first version evicted a ghost at a table that was not dealing and asserted
   the ghost refused — which it did, because there was nothing to refuse. ⚠️ **Found by mutating
   `SeatBoard.Dispose` and watching the test stay green**, not by reading it.
   `AGhostCannotAnswerTheQuestionItWasLookingAt` runs a live round now and waits until the seat
   really is being asked something. **`_left` + `Asking = null` is red when both are removed**;
   either alone is redundant, deliberately — the flag closes an in-flight-notification race a
   test cannot schedule.
2. 🔥 **Two `<AntiforgeryToken />`s is worse than none, and the page renders perfectly either
   way.** `EditForm` emits one itself for a static SSR post; a second put two inputs of the same
   name in the form, the two arrived comma-joined, and every post was rejected with a bare error
   page. ⚠️ **Found by pressing the button.** P13.3's rule generalises from URLs to verbs: *ask
   the server for everything the page says it will do*, not only for everything it names.
3. 🔥 **A marker that means "away" has to stop meaning it.** `SeatPlayedByTheComputer` is
   broadcast once per turn, and marking the seat for the rest of the *round* told a player who
   had timed out once and come back that he was still away — in his own seat, while he was
   answering. **Cleared at that seat's next `TurnBegan`**, the only moment that means *we are
   about to find out*. ⚠️ **A seat somebody walked away from is a different fact**:
   `TableBoard.Vacated` survives the deal, and a seat that was always the computer's is marked by
   neither — **nobody has gone anywhere at a bot's seat.**
4. ⚠️ **The lobby's sit-down form must not hide itself when the table is full.** Blazor keeps a
   dropped circuit for minutes before disposing it, so somebody who reloaded finds their own seat
   occupied by their own ghost, and a form that vanished would lock them out of the table they
   are sitting at. **Sitting down under a name already here takes that seat back.** ⚠️ **A name
   is not a credential and the code says so out loud** — two people who type the same name take
   each other's seat. A lobby with accounts is beyond this plan.
5. ⚠️ **A claim on a table must not be made while the page is only being prerendered** (§3.11
   C13, which was a warning about *joining* and is now about two things). Being counted as
   present and taking a seat are both claims; both sit behind `RendererInfo.IsInteractive`, and
   `ComponentDisposalTests.NothingIsClaimedWhileThePageIsOnlyBeingPrerendered` asserts the order.
6. ⚠️ **A parameter handed to an interactive root component is serialised**, so `Table.razor`
   passes the table's **id** and the island looks it up. A `HostedTable` is not serialisable and
   must not be.
7. ⚠️ **A settlement is not a resting state.** With a one-millisecond gap between rounds the
   acceptance test caught round *seventeen*, by which time the 240 lines the log keeps had
   trimmed away the two lines saying who sat down. A table that stops between rounds is also what
   a person actually sees.
8. ⚠️ **`FocusOnNavigate` beats §3.11 B7 on a reconnect, and that is left alone.** Landing on the
   table mid-turn puts focus on the `<h1>` rather than the question in front of you, because a
   fresh page load is navigation and focus belongs at the top of a document you have just arrived
   at. B7 is about the turn *moving*; the prompt is four tab stops away. **Written down so it is
   not rediscovered as a defect.**

✅ **P13.5 is done (2026-08-19), and the browser client is a table rather than a document about
one.** Every seat sits at a position on a felt, **you are at the front of it whichever seat you
were dealt**, the shared things — the deck, the money cards, the discard on offer — are in the
middle, your hand is the big pressable thing at the bottom, the four questions are one action
bar, and the narration is a round log you open.

**What it added.** `BurmesePoker.Presentation/TableRing.cs` — the rotation, as a pure function
with thirteen tests, because *an expression buried in Razor is unreachable from a test*. Three
new components (`TableCentre`, `TableLegend`, `AboutTable`) and every other table component
rewritten. **420 tests, up from 389, none removed.**

🔥 **Six findings a cold context needs.**

1. 🔥 **A focus call can kill the circuit, and a dead circuit is a page that looks perfect.** An
   `ElementReference` keeps its id after its element has gone, so §3.11 B7's focus-on-turn races
   the answer: the question is answered, the buttons become spans, and the interop call lands on
   nothing — and **Blazor turns an unhandled interop exception into a torn-down circuit**.
   ⚠️ **Found by playing 1,429 turns and reading the server log, not by looking at the page**:
   the browser showed a hand, a prompt and a spotlight, and every card refused every press in
   silence. A quick human wins the same race. **Focus is best-effort now** and
   `MarkupStandardsTests` asserts it. **After the fix: 39 rounds, 473 answers, no failures.**
2. 🔥 **Whose turn it is is now public, and it was the packet's one open design question.**
   `IsActing` meant *moved last*, which a felt with seats at positions cannot spotlight without
   lying. **The easy road was available and was not taken**: `TableEvent.TurnBegan` is broadcast
   to everybody, raised by `BoundedAgent` (the one decorator every seat passes through, so a
   bot's turn is as public as a person's), once per turn. ⚠️ **`ConcealmentTests` was extended to
   cover it**, not merely left passing.
3. 🔥 **A hidden live region announces nothing** — and this was the packet that would have done
   it, since it is the one that demoted the log to a panel you open. The list is **always
   rendered and always live**; closing it clips it the way `.said` is clipped. Two mutations red.
4. ⚠️ **A hidden twin is a duplicate.** Three shipped before the accessibility tree was read
   back: *"Cobra, Cobra is waiting"*, *"Round log, open the round log, 1 lines"*, *"36 turns, in
   36 turns"*. **A `.said` is for text the eye gets as a glyph — never for text the eye gets as
   text.** Read the page's own accessibility tree once per UI packet.
5. ⚠️ **A glyph is not automatically better than a word.** The 🃏 character is a colour photograph
   at chip size; a jester's cap in SVG replaced it and was wrong **twice** — three lobes above a
   band is a *crown* (and ♛ already means *won the round* two panels away), and hanging the side
   lobes made it an abstract blob at true size. **It is the letters `JKR`.** Settled by zooming
   into a screenshot, which is the only thing that was going to settle it.
6. ⚠️ **The draw count and the discard piles are derived from the public game, not sent.** 108
   less thirteen a seat less the turned-up cards, one off per blind draw, reset by a reshuffle —
   a watcher could do the same arithmetic, and the alternative was changing `IGameObserver` for a
   decoration. And a discard **pile** rather than a top card, because a card leaves it again.

⚠️ **One thing P13.5 left, and P13.6 did not take it either.** The legend promises that a money
card you own pays you *even after you throw it away* (RULES.md §4.4) — and the felt cannot show
those cards, because `SeatPrompt` carries ownership only for the cards currently in your hand. A
running **"★ 4 owned"** tally on your seat would need the server to send it. **Still the one
named piece of unfinished business in the browser client.**

✅ **P13.4 is done (2026-08-19), and this project has a game you can sit down and play in a
browser.** `dotnet run --project BurmesePoker.Web` deals you into seat 1 against three bots, and
all four questions `IPlayerAgent` asks are controls: take the discard or draw blind, claim the
turned-up card, throw one away, declare. **Verified by playing it with no pointer at all** —
five rounds in a headless browser driven by `Tab` and `Enter`, 86 questions answered, 393 tab
presses walking the hand, banks carrying over and summing to zero.

**What it added.** `SeatBoard` — *your seat, as you have it* — is the private counterpart of
`TableBoard`: the question standing, the hand it is about, and the hand you kept between turns,
folded out of **your own** `SeatConnection` and out of nothing else. `TableHost` seats you
(`--seat 1`, `--seat 0` for P13.3's room with nobody in it, `--name`, `--hints`, `--patience`),
and three components draw it — `YourSeat`, `TurnPrompt`, `HandPanel` — **all inside P13.3's one
interactive island**, so §3.11 C12 is untouched.

🔥 **Five findings a cold context needs.**

1. ⚠️ **A `CardId` names a card in a round's shoe, and the shoe is rebuilt every deal.** The
   first cross-seat leak assertion compared whole matches and went red at once — on the same
   physical eight of hearts being dealt to two people in two different rounds. **Anything
   comparing hands across seats compares them a round at a time.** `ConcealmentTests` never met
   this because it plays exactly one round.
2. 🔥 **Your hand between turns is not stale, and it is worth rebuilding.** After you discard,
   your thirteen are fixed until your next turn — nobody draws from your hand, and what somebody
   takes comes off your discard pile. So the resting hand is the prompt's hand minus the card
   thrown with the near-melds worked out again. ⚠️ **Ownership is read back off the `CardView`s
   you were sent**, never recomputed: a `SeatPrompt` carries the money registry but not
   `TurnContext.YouOwn`.
3. ⚠️ **A refusal must not raise "something changed".** `SeatBoard.Answer` returning false while
   the question still stands is the ordinary case, and a client that answers on the change event
   — which is exactly what a component does — would answer the same refused question for ever.
   It says something only if the question really moved on. **Found by writing the test that
   answers wrongly on purpose**, which recursed until it was fixed.
4. ⚠️ **Only the first control may capture an element reference.** Blazor invokes a `@ref`
   capture when the element is *inserted*, not on every diff, so a `@ref` inside a loop ends up
   holding the **last** card. The first card is rendered through its own branch and captures
   there. That is what makes §3.11 B7 work: focus was already on a `<button>` at every one of
   the 86 prompts, before a key was pressed.
5. ⚠️ **The seated table no longer deals from boot.** Every question an unanswered seat is asked
   spends the whole of its patience before the stand-in plays, so an unattended round is over an
   hour of nothing. A table with nobody in it still deals from boot (P13.3's *a table is a
   place, not a button*); a table with a seat waits for the first page. **Do not fix that by
   shortening the patience** — P13.5's lobby knows whether anybody is connected, which is the
   honest answer.

⚠️ **What P13.6 has to change rather than add to.** `TableHost.Yours` is **one** `SeatBoard`,
built when the table is opened, and every circuit is handed the same one. Right for solo play,
wrong the moment two people are here: **a `SeatBoard` belongs to a viewer, not to a host.** The
component is already written for it — `YourSeat` reads its seat once and unhooks in `Dispose`.


✅ **P13.3 is done (2026-08-19), and this project has a UI.** `BurmesePoker.Web` is a **seventh**
project — Blazor Server, Domain + Presentation + Server — and
`dotnet run --project BurmesePoker.Web` deals round after round at a table you can watch in a
browser: seats and banks, the turned-up money cards, each seat's top discard, the narration as a
polite live region, and the settlement with the winner's melds and what each seat took home.
**Three of the four §0 goals were already delivered; this is the first half of the fourth.**

🔥 **Four findings a cold context needs.**

1. **The framework's own files are static web assets, and `UseStaticFiles` does not serve them.**
   The page rendered, looked finished, and was **dead** — `_framework/blazor.web.js` 404'd, so no
   circuit ever started. ⚠️ **A prerendered Blazor Server page is a perfect photograph of a
   broken one**: ask the server for every URL the page references. `MapStaticAssets` is the fix,
   and `Properties/launchSettings.json` is checked in because a build's endpoints manifest is a
   *Development* one.
2. **A trimmed log must not take its `@key` from the length of the log.** `TableBoard` keeps the
   last 240 lines; a sequence taken from `Log.Count` repeats the moment it trims, and a repeated
   `@key` is Blazor reusing the wrong DOM node. `TableBoard.Narrated` counts every line ever
   said.
3. **240 visually-hidden spans made the document 10,295px tall** for a page whose body was 1,814.
   The standard `position: absolute` recipe needs a positioned ancestor, or a hidden span inside
   a scrolled box is measured from the page. ⚠️ **Found by measuring, not by looking** — the
   screenshot was correct.
4. **A source scan must read the markup, not the prose about it.** Four of six scans failed on
   their first run, each on a comment in the file that was obeying the standard.
   `Sources.Markup` strips commentary first. And **a `@key` check that looks *nearby* is not a
   `@key` check** — the first version let a nested `<CardChip @key>` cover for an unkeyed `<li>`,
   and the mutation survived. **Eleven mutations now, eleven red.**

⚠️ **Two decisions P13.3 took that differ from what the plan named.**

- **`PacedAgent` moved to `BurmesePoker.Presentation`, not to `Domain/Agents/`.** The domain is
  the pure rules and a wall-clock sleep is not one — and, deciding it, **`BurmesePoker.Sim`
  references Domain**, so a sleep in there would be reachable from the hot loop P11 wrote a
  layering test to protect. Presentation is reachable from both front ends and neither harness.
  **The console is byte-identical after the move** (two pty captures, two seeds, `cmp`).
- **`BurmesePoker.Tests` now references `BurmesePoker.Web`** — and still never
  `BurmesePoker.Console`. The rule was never *"no front end"*; it is that nothing is tested
  **through** one. A Spectre console is an interactive loop needing a terminal, verified by a
  pty; a component tree is data, and §3.11 A5 asks for a reflection test over it by name.

✅ **The §3.11 A list is finished.** A1 shipped in P13.2, A2 in P13.1, and **A3 (computed
contrast, both themes), A4 (real controls) and A5 (disposal) shipped here** — plus C12, C14, C15
and B8 as scans. ⚠️ **The contrast test reads `wwwroot/theme.css`, the file the browser loads**,
and **discovers its own pairs**: `--on-x` needs 4.5:1 against `--x`, `--edge-x` needs 3:1, and a
token's base is the longest declared name it begins with. A token added later is measured
without anybody listing it.

✅ **P13.2 is done (2026-08-19).** `BurmesePoker.Server` exists — a **sixth** project, Domain +
Presentation, **and no transport at all** — and a round is played by two scripted remote seats
and two bots through the server's own plumbing, in a test, with no sockets involved. 🔥 **The
leak test landed with it, which is the point of doing this packet before any UI**: for one
round played by four connected seats with somebody watching, no seat is shown a card from
another seat's hand, no seat is told what anybody else drew blind, every card in every event a
seat receives is one it may see, and **a watcher is sent nothing but the public game.**
⚠️ **It was mutation-tested, not assumed** — broadcasting the drawn card to every connection
turns three of the five concealment tests red.

🔥 **Two findings a cold context needs before P13.3.**

1. **Exactly one event in the whole narration is private — the blind draw.** A discard, a taken
   discard, a claimed money card and a declaration all happen in front of the table
   (RULES.md §4.5, §5, §6.3, §7.1). So the security boundary is one `if` in `TableFanOut`, and
   everything else in that file is care about lists rather than filtering.
2. **P13.1's snapshot rule generalised, and it caught two more live lists.**
   `TableState.TurnedUpOnTable` is aliased by *both* `TurnContext.TurnedUpMoneyCards` and
   `IGameObserver.RoundStarted`, and **the opening player's claim removes a card from it**. A
   prompt or an event that kept the reference would stop saying what it said when it was sent.
   ⚠️ **Assume every list the engine hands over is live until shown otherwise.**

⚠️ **One thing P13.2 deliberately did not do, and P13.3 inherits it.** The packet said to wrap
the takeover bot in P11's pacing decorator so a taken-over seat reads as a seat playing. It
does not: a sleep belongs to whatever is *drawing* the table, not to a server that may host
many of them, so **`TableOptions.StandIn` is a factory** and a host hands over an agent that is
already paced. ⚠️ **`PacedAgent` lives in `BurmesePoker.Console`, which `BurmesePoker.Web` may
not reference — move it to `BurmesePoker.Domain/Agents/` or write the browser's own. Do not
solve it by referencing the console.**

✅ **P13.1 is done (2026-08-19).** `BurmesePoker.Presentation` exists — a fifth project, Domain
only, no rendering technology — and the console renders a view model it did not build itself,
**byte-identically at the same `--seed`**. It is the first presentation code in this project a
test can reach at all. 🔥 **Its most useful finding was a defect the first new test caught: a
view model that aliases the engine's hand list is not a view model.** `TurnContext.Hand` is the
seat's own live list, and `RoundEngine` discards from it the instant the answer comes back — so
a view built at the discard said fourteen and then held thirteen. The console never showed it
(render and drop, one breath); **a Blazor component holding a view across a render is exactly
the case that does.** ✅ **P13.2 inherited the rule and found two more instances of it.**

⚠️ **P13 was re-planned on 2026-08-19 and is now five sequential sub-packets, P13.1–P13.5.**
Nick asked whether a rich browser UI or multiplayer made more sense first, and **the answer is
that they are one track, not two** — see `BUILD-PLAN.md` **§3.10**. The only thing that couples
them is *where the engine runs*, and concealed hands with money on them settle that in advance:
**server-side, always.** A browser client is a seat over a wire whether one person or four are
using it, so **solo browser play is multiplayer with one connection** and there is no separate
single-player UI to build and then throw away. **Blazor Server** is the choice; it also means
P13.2 has **no protocol to design**.

✅ **The re-plan's judgement was right, and P13.1 is the evidence.** It said the cut line is
**decision versus drawing**, not `IAnsiConsole` injection. Both halves shipped, and only one of
them was worth anything: the extraction gave thirty new tests and a shared vocabulary, while the
injection — done, and the console reaches for the static console exactly once now — **cannot
deliver its stated benefit.** "Drivable from a test" needs the test project to reference
`BurmesePoker.Console`, which §2 forbids and which is the better rule. ⚠️ **Do not resolve that
by adding the reference.** The console's verification is a pty, and `scripts/drive-console.py`
is now checked in so a future packet can prove its own refactor the same way — capture, change,
`cmp`.

⚠️ **`BUILD-PLAN.md` §3.11 is a seventeen-item UX standard taken before a single component
exists**, and it is not optional inside a sub-packet. **Five of the seventeen are mechanical
tests** — the concealment leak, colour-not-alone tokens, computed contrast in both themes, real
controls rather than clickable divs, and subscription disposal. ✅ **A2 (colour never alone)
shipped in P13.1 and A1 (the concealment leak) shipped in P13.2** — the two that could not be
retrofitted cheaply, both taken before there was anything to retrofit. **Three are left, all in
P13.3**: computed contrast, real controls, subscription disposal. The rest are reviewed by playing, the way P11
was. **The one item that cannot be walked back cheaply is C12: static SSR by default, with
`@rendermode InteractiveServer` on the table component and nowhere near the root.**

**P13 is still the only optional part of the plan — stopping now is a legitimate end state**,
and so is stopping after **P13.5**, which is a finished single-player browser game that looks
like one.

✅ **P16 is done (2026-08-19), and the whole P14–P16 branch with it.** The question Nick's
friend raised has an answer, an interval and a control:

> **Upstream skill is worth `+9.1 ± 2.1` points of win rate across the biggest gap on the
> ladder (`random` against `greedy`) and `−1.0 ± 2.1` points across the gap between two
> thinking players.** A weaker player *anywhere* at your table is worth 4–5 points to you;
> **which side of you they sit on is worth nothing** unless they are not really playing.
> 64,000 games, two seeds, with a downstream control arm that seats exactly the same four
> strategies.

🔥 **And it corrected P15's headline lead rather than confirming it.** P15 found greedy and
cautious 5.4 points apart in the ladder run and recorded that "all of it is who fed whom". **It
is not.** It is the same symmetric *neighbour* effect — a rotation moves both neighbours at
once, which is exactly what the downstream arm separates. The lead was real and its reading was
wrong, and the control is what caught it.

⚠️ **Two things every future measurement inherits from P16.** `--seating balanced` plays every
assignment of the strategies across the seats instead of rotating one pattern, and **every CSV
row now names `upstream_strategy` and `downstream_strategy`.** **P12's headline re-measured:
30.7% vs 19.3% rotated (reproduced exactly at 8,000 games) against 29.6% vs 20.4% balanced** —
so **the rotation flatters greedy by 1.1 points a seat, about a fifth of the gap.** Quote the
balanced figure for "which strategy is better" and the rotated one only for "what happens at
that table".

**P0 through P12 and P14–P16 are done. The game is playable alone, pleasant to sit at, and measurable in
bulk:** `dotnet run --project BurmesePoker.Console` fills the empty seats with bots, paces them
so a person can follow, shows a hand as the melds it nearly is, keeps a round log across the
concealment clear, and replays any match from `--seed`; `dotnet run -c Release --project
BurmesePoker.Sim` plays thousands of seeded games in parallel and reports how two strategies
compare. **Three of §0's four goals are delivered — solo play (P10), console UX (P11) and
simulation (P12). The multiplayer app (P13) remains, and P14–P16 were added on 2026-08-19 to
carry the simulation goal further; all three shipped the same day.**

✅ **There is a persistence layer now, and it is a file format rather than a store.** P14 built
**game journals**: `Play/{GameJournal, JournalFormat}` and `Agents/{JournalingAgent,
JournalPlayerAgent}` in Domain, `Replay`/`JournalReport` in Sim, `--journal <path>` on both front
ends and a `replay` verb on the harness. A journal is a header plus every answer every seat gave;
replaying is **playing the game with different seats**, so the engine did not change by a line.
**A journalled run and its replay produce byte-identical CSV.** `MatchEngine` still keeps no
history (§3.8) and the console's standings still die with the process — a journal is a
consumer's artifact, which is the point.

The 2023 implementation is gone from the tree and lives only at the `pre-rewrite` tag. The
solution is **seven** projects — `BurmesePoker.Domain` (pure rules), **`BurmesePoker.Presentation`**
(what a hand looks like, as data; Domain only, **and no rendering technology at all**),
**`BurmesePoker.Server`** (one table, hosted: a remote seat and a filtered fan-out; Domain +
Presentation, **and no transport at all**), `BurmesePoker.Console` (Spectre.Console 0.57.2, the
only project that prints), **`BurmesePoker.Sim`** (batch play, Domain only), **`BurmesePoker.Web`** (Blazor Server — the
second project that draws; Domain + Presentation + Server) and `BurmesePoker.Tests`.
⚠️ **The test project references Domain, Presentation, Server, Sim *and* Web.**
The rule that matters is unchanged in substance and worth restating in its true form: **tests
never reference `BurmesePoker.Console`**, so nothing is tested through the front end. The
harness's determinism is itself an acceptance criterion, so it has to be reachable from a test.

Domain holds `Cards/{Rank,Suit,CardColor,CardText,CardId,Card,Deck,DeckBuilder,
DeckExhaustedException}`, `Melds/{MeldKind,MeldSlot,Meld,RunGenerator,SetGenerator,
MeldCandidates,MeldIndex,HandEvaluator,PartialCover}`,
`Money/{MoneyCardRegistry,CardOwnership,Stakes,Settlement}`,
`Play/{PlayerId,TurnAction,PlayerState,TableState,TurnContext,RoundResult,RoundEngine,
MatchEngine}`, `Abstractions/{IPlayerAgent,IGameObserver}` and
`Agents/{CoverScore,RandomBotAgent,SimpleBotAgent,GreedyBotAgent,CautiousBotAgent}`. Console holds `{Program,CardFormatting,
SpectrePlayerAgent,ConsoleObserver}`. Sim holds `{Program,Simulator,GameRunner,
SimulationOptions,SimulationReport,StrategySummary,Strategy,SeedSequence,SeatRecorder,
SimObserver,RoundAbandonedException,Results,CsvReport,Replay}` **plus P16's
`{SeatingPlan,Measurement,NeighbourExperiment,NeighbourCsv}`**. **Console gained five files in
P11**: `{Options,Palette,RoundLog,HandView,PacedAgent}` — ⚠️ **`HandView` is gone as of P13.1**,
replaced by `HandPanel`, which draws a view model rather than building one. **P13.1 added
`BurmesePoker.Presentation/{CardDisplayState, DisplayTokens, CardOrder, CardView, MeldView,
HandView, ComputerAdvice}` and `scripts/drive-console.py`; ⚠️ **P13.3 moved `PacedAgent` in
beside them, out of the console.** **P13.3 added `BurmesePoker.Web/{Program, TableHost,
TableBoard, CardWords}`, `Components/{App, Routes, _Imports, Layout/MainLayout,
Pages/{Watch, Rules, Error}, Table/{TableView, SeatPanel, CardChip, RoundLogPanel,
SettlementPanel}}` — each table component with its own `.razor.css` — `wwwroot/{theme.css,
app.css}` and `Properties/launchSettings.json`. P13.2 added
`BurmesePoker.Server/{TableSession, TableSeat, TableOptions, TableFanOut, TableEvent,
SeatConnection, SeatPrompt, SeatQuestion, SeatAnswer, RemotePlayerAgent, BoundedAgent,
TableAbandonedException}` — and nothing anywhere else at all.** **P14 added `Play/{GameJournal,
JournalFormat}` and `Agents/{JournalingAgent,JournalPlayerAgent}` to Domain and `Replay.cs`
(which holds both `Replay` and `JournalReport`) to Sim. P15 added
`Agents/{RandomBotAgent,CautiousBotAgent}` and moved the discard loop into `CoverScore`, and
added nothing to Sim at all** — a strategy is an `IPlayerAgent` and the harness already knew
how to seat one. ⚠️ **Domain now references
`System.Text.Json`** — the first framework assembly in there beyond the base library. It is a
string API, not an I/O one: `JournalFormat` hands back `IEnumerable<string>` and the two front
ends own every `File` call, the same split `CsvReport` already had.

✅ **Baseline green** — `dotnet build` clean and warning-free, `dotnet test` **389 passed,
0 failed**, in twenty seconds. **Any red tree is a real problem.**

⚠️ **One hazard a cold context must know before writing a test or a strategy:** with the
reshuffle built, **a round in which nobody's hand ever improves never ends.** Only a
declaration ends a round (RULES.md §7.1) and the cards now circulate for ever, so a table of
passive agents loops until it is killed. Every round-level test needs a seat that eventually
declares. `GreedyBotAgent` and `SimpleBotAgent` are both safe in practice — neither has ever
failed to finish a round — but **the simulation harness is the only place that is bounded**:
`SimulationOptions.TurnCap` and `SeatRecorder` give up on a stalled round and report it. A
test that plays a round outside the harness has no such protection.

---

## Packets

| | Packet | Depends on | Notes |
|:-:|---|---|---|
| ☑ | **P0** Restructure and salvage | — | done 2026-08-18 |
| ☑ | **P1** Cards, deck, identity | P0 | done 2026-08-18 |
| ☑ | **P2** Money designation and ownership | P1 | done 2026-08-18 — turned-up-joker default taken |
| ☑ | **P3** Run candidate generation | P1 | done 2026-08-18 — spec updated with what it found |
| ☑ | **P4** Set candidate generation | P1 | done 2026-08-18 |
| ☑ | **P5** Exact-cover hand evaluator | P3, P4 | done 2026-08-18 |
| ☑ | **P6** Stakes and settlement | P1, P2 | done 2026-08-18 — settlement takes the *unshuffled* shoe |
| ☑ | **P7** Round and turn engine | P5, P6 | done 2026-08-18 — deals from a draw order, not a shuffle |
| ☑ | **P8** Console front end | P7 | done 2026-08-18 — hotseat; verified by driving a pty |
| ☑ | **P9** End-to-end play | P8 | done 2026-08-18 — the reshuffle lives inside `RoundEngine.TakeCard` |
| ☑ | **P10** Bot opponents — solo play | P9 | done 2026-08-18 — `PartialCover` + `GreedyBotAgent`; every seed terminates |
| ☑ | **P11** Console UX pass | P10 | done 2026-08-18 — round log, meld-grouped hand, hints, pacing, `--seed` |
| ☑ | **P12** Simulation at scale | P10 | done 2026-08-18 — `BurmesePoker.Sim`; the tie-break wins 30.7% to 19.3% |
| ☑ | **P13** The browser client and multiplayer | P10 | XL — **re-split 2026-08-19 into the six below**; done 2026-08-19, and the only one that changed the architecture |
| ☑ | **P13.1** A presentation view model, rendered two ways | P10 | done 2026-08-19 — a fifth project; the console is byte-identical, and a view model must be a **snapshot** |
| ☑ | **P13.2** The table server | P13.1 | done 2026-08-19 — a sixth project, no transport; **the concealment leak test shipped, and it fails when broken** |
| ☑ | **P13.3** A browser table you can watch | P13.2 | done 2026-08-19 — a seventh project; **the first UI**, and the rest of the §3.11 A list shipped with it |
| ☑ | **P13.4** A seat you can play | P13.3 | done 2026-08-19 — **solo browser play, complete**; 86 questions answered with no pointer at all |
| ☑ | **P13.5** A table, not a document | P13.4 | done 2026-08-19 — the layout pass; **a focus call can kill the circuit**, and whose turn it is was made public on purpose |
| ☑ | **P13.6** The lobby, and a second person | P13.5 | done 2026-08-19 — **§0's goal 4**: two people and two bots play a round over a network |
| ☑ | **P14** Game journals — record and replay | P12 | done 2026-08-19 — a run and its replay produce byte-identical CSV |
| ☑ | **P15** A skill ladder | P12 | done 2026-08-19 — four rungs, **three** separated skill levels |
| ☑ | **P16** Does the player before you decide your game? | P15 | done 2026-08-19 — **no**, not between thinking players: −1.0 ± 2.1 pts |
| ☑ | **P17** The tournament: a harness that ranks players | P15, P16 | done 2026-08-19 — **a win rate is a ratio over games, not a mean of ratios**; pairing widens within a cell by √2 |
| ☑ | **P18** One catalog: the same players everywhere | — | done 2026-08-19 — `BotCatalog` in Domain; **the console's difficulty default had been the *easy* bot since P10** |
| ☑ | **P19** Difficulty as a dial, not a list | P17, P18 | done 2026-08-19 — **goal 5's product, finished**: four levels, one mistake rate each, **calibrated to ~7 points a step** |
| ☑ | **P20** Memory: the card-counting rung | P17, P18 | done 2026-08-20 — **a null, published**: `counting` is `+0.3 ± 1.0` the *wrong* way against `greedy` |
| ☑ | **P21** Outs: the first rung that looks ahead | P17, P18 | done 2026-08-20 — **it separates**: `+3.1 ± 1.0` over `greedy`, and the whole solution got 45% faster on the way |
| ☑ | **P22** Money: is there a strategy in the side bet? | P17, P18 | done 2026-08-20 — **no at $5/$1, where the rule never fires at all; `+7.3 ± 3.3` a round at $5/$40** |
| ☑ | **P23** The standing answer | P17, P19 | done 2026-08-20 — **the last packet**: the dial re-fitted (one ε moved), **59 of 77 rows byte-identical**, and "a rung cannot be added without being measured" made a test |
| ☐ | **P24** The computer's reasoning, said out loud | P13.6, P14, P18, P21 | **planned 2026-08-20, not started** — the browser's arrow grows a *why*, and a journal that records where an expert disagreed |
| ☑ | **P25** The win condition is a function of the table size | — | **done 2026-08-21** — `TableRules.For(players)` is the §7.1.1 table as data; `HandEvaluator` takes it and **has no parameterless overload**. The search carries what is still owing **along** the partition; two-handed prunes sets out of the candidates; `Meld.IsClean` needs no case for the all-joker meld. 🔥 **The change is real and `drive-console.py` cannot see it** — both captures are byte-identical because neither script reaches a declaration. |
| ☑ | **P26** The money layer as it actually is | — | **done 2026-08-21** — `Permanent` is **three values and eight cards**, `Multiplier(Card)` returns 0/1/**3**, and the ×5 is `Multiplier(card, owner, MoneyOwnership)` under a configuration `Settlement` reads **once a round**. 🔥 **The packet's stated prediction held**: a designation on the 7♦ and one on an ordinary card leave *exactly the same* money loose in the shoe, now an equality assertion. 🔥 **The side-bet went from `$8.50` to `$11.58 ± 0.34` a round at five seats — 42.5% → 58% of the round prize — and play did not move at all.** ⚠️ **The ×5 is fenced to the 7♦/A♠ pair** (§9 #32) by two tests. |
| ☑ | **P27** The feeding ban | — | **done 2026-08-21** — §5.1 enforced **by construction**: `FeedingBan` is two rank sets a seat, `TurnContext.LegalDiscards` is the whole of the choice a turn presents and is **never empty**, and `CoverScore.Ranking` takes a context so **every rung including the runner-up is filtered without any rung remembering to**. 🔥 **The predicate is `Card.SameRankAs`, which is `Rank == other.Rank`** — nullable equality is what makes a joker close the other jokers (§9 #27), so the house ruling falls out of the type. 🔥 **A bot's cover count can now fall**, which breaks the monotonicity argument that says a table of bots terminates. ⚠️ Both front ends draw a closed card as **not a control** (`CardDisplayState.Unthrowable`). |
| ☐ | **P28** The claim, the permission, and the seat you sit in | **P27** ✅ | **next; planned 2026-08-21, not started** — seats re-randomise **every round** (§3, §10 #16) and claiming needs the **upstream player's permission** (§4.5, §10 #18). 🔥 **A third kind of agent decision — *do I object* — asked of a seat that is not on turn**, and the answer is a **disclosure**. Depends on P27 because the objection predicate *is* the ban's predicate. |
| ☐ | **P29** Re-measure, under the rules as they are | P25, P26, P27, P28 | **planned 2026-08-21, not started** — every figure in `STRATEGY.md` was measured under rules the document no longer holds. 🔥 **Three predictions written down before the run**, so the packet can be wrong. ⚠️ `sim suite` was five hours at P23 and P25 makes the evaluator's question harder. |

**P14, P15 and P16 are all done, and not one of them needed a line of the engine.** P14 cost
nothing measurable in throughput at either fidelity; P15 and P16 raised no rules question
either — how well a strategy plays, and who it plays next to, are not rules (`RULES.md` stays
at **rev 13**). **P16 changed `BurmesePoker.Sim` only**: `SimulationOptions` gained one property,
`CsvReport` two columns, and four new files went in beside them.

**P10 was the fork; P11 and P12 have both been taken through it.** ⚠️ **P12 opened a branch
rather than closing one** — having a harness is what makes journals and a strategy-comparison
programme worth building, so P14, P15 and P16 hung off P12 and not off P13. ✅ **That branch
closed with P16** — the game is finished as a game, the console as a console, the simulation goal
is delivered and the neighbour question has an answer with a control. **Stopping there was a
legitimate end state.**

⚠️ **It reopened on 2026-08-19 as goal 5, and it is now the only live branch.** P17–P23 hang off
P15/P16 (the measurement apparatus) and off nothing in P13. **P17 and P18 are independent of each
other**, both feed **P19** — where the difficulty product is finished — and **P20, P21 and P22 are
independent of one another and droppable in that preference order**. 🔥 **The dependency that
matters is P17 before P19**: calibrating a difficulty ladder with the interval-free report the
harness prints today would be a guess wearing a number.

⚠️ **Four things the roadmap changed for work that was already planned**, all recorded in
`BUILD-PLAN.md` — **all four are now discharged**:
1. ✅ **P10's stated heuristic was wrong, and the correction shipped.** "Never discard an owned
   money card" contradicts `RULES.md` §4.4 — ownership never transfers, so a money card pays
   you after you throw it. `GreedyBotAgent` consults neither ownership nor the registry
   anywhere, and `MoneyCardsDoNotChangeWhatABotThrowsAway` is the test that says so.
2. ✅ **§3.6 settles that agents stay synchronous**, taken *before* P10 and P13 add callers to
   `IPlayerAgent`. A remote player blocks in the agent; one table is one task. **P10 and P12
   both added callers and neither needed anything** — a bot answers the same four questions a
   person does, and a simulation runs a whole table inside one `Parallel.For` body.
3. ✅ **§3.7's speed question is closed, by P12's measurement pass.** `PartialCover.Best` is
   **140 µs** a hand and `HandEvaluator.TryFindCover` **91 µs**; a round is **~20 ms** and a run
   does **50–90 rounds a second**, so 2,000 rounds took 34 seconds with nothing optimised. The
   one surprise: the work is **allocation-bound, not compute-bound** — eight threads bought 25%
   under the workstation GC and 70% under the server GC.
4. ✅ **§3.8's statistics constraint — delivered by P9.** `MatchEngine.PlayRound` returns a
   `RoundRecord(RoundResult, TableState)`, so how close the losers were and how much of the
   money was the side bet both stay reachable, and `RoundResult.Turns` is carried at the
   source. The engine keeps **no** per-round history: a consumer derives what it wants and
   drops the table. **P10 built the third seam too** — `RecordingAgent`, a decorator over
   `IPlayerAgent`, in the test project.

---

## Notes for the next session

🔥 **P25, P26 and P27 are done. The next packet is P28.** ⚠️ **P24 was re-sequenced after P29 on
2026-08-21 — a recommendation, not a decision**, since Nick set its scope: its stated reason for
going first (*§5.1 is blocked, and P24 makes that conversation productive*) is spent, and it
explains decisions that P26 and P27 have now changed. **P29 needs all four.**

### What P27 found, and what it leaves behind

1. 🔥 **The finding that outlives the packet: a bot's cover count can now fall, and that breaks
   the argument that a table of bots terminates.** `GreedyBotAgent`'s own remarks say the score
   *"can never fall — throwing back the card just taken restores the hand exactly"*, and that is
   the stated reason a round of bots reaches a declaration at all. **§5.1 takes the just-taken card
   out of the choice.** A seat whose only legal discards are melded ones gives up a meld, so
   convergence is no longer guaranteed by construction. ⚠️ **In practice the tree finishes and the
   suite is green**; what stands behind it is `SimulationOptions.TurnCap` and the hosted table's
   `RoundTimeLimit`, both of which existed for `RandomBotAgent` and now carry a second job.
   **P29 should publish round lengths and abandoned-round counts, not only win rates** — nothing
   published has ever quoted the abandoned count, and if it is not zero that is a result.
2. 🔥 **The rank-only predicate is one line, and the house ruling falls out of the type.**
   `Card.SameRankAs` is `Rank == other.Rank` — nullable equality, so two jokers match and a joker
   matches nothing else, which *is* §9 #27's ruling that taking a joker closes the other jokers.
   ⚠️ **It reads like a trivial field comparison and it is not**: `SameValueAs` would compile, pass
   most of the tests, and implement the wrong rule. **P28 must reuse this method** for the claim's
   objection (§9 #30), not write a second one that agrees by inspection.
3. 🔥 **The filter became a property of the ladder rather than a line each rung remembers.**
   `CoverScore.Discard` and `CoverScore.Ranking` take a `TurnContext` instead of a hand, so every
   rung that ranks is filtered by construction — **and so is the runner-up a difficulty level
   throws as its mistake**, which was the thing §5.1 said was easiest to get wrong. Only
   `RandomBotAgent` needed a line of its own, because it is the one rung that does not rank.
   ✅ **A rung added later cannot forget**, and `LegalDiscardsTests` runs over `BotCatalog.All` and
   `DifficultyLadder.All` rather than over a sample.
4. ⚠️ **The floor is one line; the declaring-discard exception is not.** Exception 2 needs
   `HandEvaluator.IsWinning` per banned card, gated behind one
   `PartialCover.CoversAtLeast(hand, 13)` so it is asked only of a hand that could actually go out
   — and only when a rank is closed *and* held. **An ordinary turn returns the hand itself**, same
   reference, no allocation, no search.
5. ⚠️ **"Domain only" was false again, and in a new way.** §5.1 makes a card *unpressable*, so both
   front ends had to change: `CardDisplayState.Unthrowable` with the token `closed`, a legend
   entry, an accessible name, the console dropping the card from its `SelectionPrompt`, and the
   browser drawing it as a `<span>` rather than a disabled button — **a control that does nothing
   is exactly the failure P13.4 spent a packet chasing.** 🔥 **So a `drive-console.py` capture from
   before P27 no longer compares either**, for the second packet running.
6. ✅ **The server refuses a banned discard where it already refuses a card you do not hold.**
   `SeatPrompt.MayThrow` replaces `Holds` in `SeatAnswer.Discard.Fits`, so a bad client's answer
   does not fit, the question stays standing and the stand-in eventually plays — **rather than the
   engine's guard throwing and taking a hosted table down with it.** The engine guard stays, for
   anything that gets past that.
7. ⚠️ **A test that plays a round has to be given a way to end, and this bit twice.** Two of the
   new engine tests hung on the first run: a table of seats that draw blind and throw back what
   they drew never finishes, because only a declaration ends a round (§7.1) and §5's reshuffle
   keeps the cards circulating. **`ScriptedPlayerAgent` advances per turn *that seat* is asked**,
   not per table turn, which is the other half of the same trap.
8. ✅ **`FeedingBanInPlayTests.AnOrdinaryRoundClosesRanksAndNeverOffersOne` is the anti-vacuity
   test.** Everything else in that file scripts the ban into existence; this one plays a round of
   greedy bots and asserts §5.1 **binds somebody** in it. Without it a filter that quietly did
   nothing would pass the whole file.
9. 🔥 **Measured, and the headline is how little it costs: the ban is nearly free at four seats.**
   Same command before and after (`--games 200 --strategies greedy,simple --seats 4 --seed
   20260821`), *before* being the P26 tree in a worktree at `HEAD`:

   | | turns a round | rounds/s | greedy | simple |
   |---|---|---|---|---|
   | before (P26) | 26.6 | 69 | 31.3 ± 3.4 | 18.8 ± 3.4 |
   | after (P27) | **27.1** | **69** | 31.5 ± 3.4 | 18.5 ± 3.4 |

   **Play moved by one round out of 200 and throughput did not move at all** — so the exception-2
   gate is doing its job and the filter costs nothing measurable. ⚠️ **The reason it is this small
   is worth knowing before P29 reads too much into it: a seat takes in the open on only about a
   fifth of its turns** (`take %` is 20–23%), so a rank is closed for far less of a round than the
   rule's prominence suggests. **At a table of stronger rungs, which take more, expect more.**
10. ⚠️ **Two shipped tests went red and both for the same reason — a fixture that had been legal
   became an illegal move.** `RoundEngineTests.OnlyTheDealAndABlindDrawEverConferOwnership` had a
   passive seat draw a 3♠ and throw it straight back, one turn after Alice claimed the 3♣ in the
   open; the fixture draws a 4♠ now. And `SkillLadderRunTests.ThinkingBeatsNotThinkingByAMile`
   asserted `random < simple` over **eight** games, and `simple` drew a blank — it wins **29.0%**
   over two hundred, so eight was a coin toss the ban tipped. **Thirty-two games now.** 🔥 **Both
   are the same lesson P21 left about round length: a test over a *played round* can be asserting
   a property of that round nobody wrote down.**

### What P26 found, and what it leaves behind

1. 🔥 **The packet's own prediction was the deliverable and it came out right.** `RULES.md` §10 #17
   said, in advance, that
   `ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`'s last assertion
   *"should invert to an equality"* — that under the ×3 a designation on the 7♦ and one on an
   ordinary card leave **exactly the same** money loose in the shoe — and that **it had not been
   run**. It was run. **They are equal to nine decimal places.** So §4.1's conservation arithmetic
   is sound and the rule did **not** need re-asking, which was the stated fallback.
2. ⚠️ **`STATUS.md` predicted that test would be *red* before P26 started. It was green at
   `HEAD`.** The code still implemented the withdrawn rule, so the test agreed with the code and
   disagreed only with the document. 🔥 **A test cannot go red for a rules change until somebody
   changes the code** — what a shipped assertion buys you is that the change *cannot be made
   quietly*, not that the tree tells you it is due.
3. 🔥 **Seven shipped tests went red and every one of them for the same reason: jokers are dealt.**
   Four of 108 cards are jokers, so an arbitrary four-handed deal hands out one or two of them —
   and five tests carried the comment *"nobody owns a money card, so only the flat round value
   moves"*, which stopped being true the moment `Permanent` grew. **The blast radius of rev 21 is
   not the money layer; it is every golden settlement number in the tree.** ✅ All seven are
   genuine expectation moves and each now names the jokers rather than restating a number.
4. 🔥 **The side-bet is `$11.58 ± 0.34` a round at five seats, against `$8.50 ± 0.26` before** —
   **58% of the $20 round prize, up from 42.5%**, and 37% of all the money that changes hands.
   ✅ **The instrument was validated before it was used**: the *before* run reproduces P12's
   rev-13 published figures ($8.43 / 42% / 30%) at a different seed, so the difference is the
   rules change. **Method** — `--games 600 --seats 5 --seed 20260821 --csv`, then sum each round's
   positive `side_bet` column; it is worth keeping, because nothing in `Sim` reports gross
   side-bet movement and the CSV does.
5. ✅ **Play did not move at all.** Same seed before and after: identical wins, turns and cover.
   **This is the first time §4.4's decoupling claim — money never distorts the melding game — has
   been measured *across* a change to the money layer**, and it held exactly.
6. ⚠️ **"Domain only" was true of the logic and false of the text.** `($$)` had to become `($$$)`,
   `CardDisplayState.PaysDouble` became `PaysTriple`, and four user-facing strings said *double*.
   🔥 **So a `drive-console.py` capture from before P26 no longer compares** — P23's promise that
   a pre-P23 capture still does is spent. Re-capture from `HEAD` first.
7. ⚠️ **No view can show a ×5, and no packet owns the gap.** `CardView.Multiplier` is 0, 1 or 3 by
   construction, because a marker beside one card cannot depend on the rest of the round's
   ownership. The jackpot is settled and never drawn — a player owning both partners sees `($$$)`
   and is paid ×5. **Candidate work for P24 or a UX packet; it is not a bug in P26.**
8. ⚠️ **`MoneyOdds` does not price the ×5 either**, and says so beside the two approximations it
   already had. It sums the value-only multiplier over the shoe; the turn-up is the 7♦/A♠ pair
   about **one round in 1,444**, and the understatement is against drawing blind, which is the
   conservative direction for `prospector`, the one rung that reads it.
9. ⚠️ **§9 #32 is fenced by two tests, not by a comment.**
   `TwoTripledJokersInOneHandAreNotAJackpot` (both colours, and both copies of one colour) and
   `ASevenAndAJokerTurnedUpAreNotAJackpotEither` assert ×3 for the combinations rev 21 *created*.
   **Widening the rule has to break them.**
10. ✅ **The baseline was verified in a `git worktree` at `HEAD` rather than by stashing** — 574/0
   in 9 m 34 s, run alongside the work. `git worktree add <scratch> HEAD && dotnet test` is worth
   reusing whenever a session has already started editing before the baseline is green.

### What P25 found, and what it leaves behind

1. 🔥 **The memo key was the interesting part, not the requirement.** The search state is
   `(covered, seriesStillOwed, cleanStillOwed)`, not `covered` alone — a covered-set from which no
   completion can supply *two* more clean series may perfectly well supply one, so the old key
   would have poisoned the second question with the first question's dead ends. Everything else in
   the search is unchanged: same index, same pin to the lowest uncovered card.
2. ✅ **`Meld.IsClean` needed no special case for the all-joker meld** (`RULES.md` §9 #29). It is
   `Kind == Run && JokerCount == 0`, and three jokers fail it because every slot is a substitute.
   **The rule and the definition agreed without either being bent.**
3. ✅ **Two-handed was the cheap case and cheaper than expected.** Sets are illegal *as melds*, a
   property of a meld rather than of the partition, so `MeldIndex.Build` takes `setsAllowed` and
   the search never sees one. ⚠️ **Filtering on `Meld.Kind` keeps more than it looks like it
   does** — `MeldCandidates` already emits `{9♦,🃏,🃏}` and `{🃏,🃏,🃏}` once, as the **run**
   interpretation, so they survive the filter and a two-handed player may lay them down.
4. 🔥 **`drive-console.py` is blind to this change, and that is the finding worth carrying.** Both
   captures at `--seed 20260819 --pick 0` are **byte-identical to the pre-P25 tree** —
   `--script bots` 8,417 bytes, `--script human` 90,251 — **not because play is unchanged but
   because both scripts quit in round 2 and neither capture contains a declaration at all**
   (`grep -c "went out"` is 0). ⚠️ **Do not read a clean `cmp` as evidence about play.** The
   instrument that proved P21 and P23 were refactors cannot see the win condition. A front-end
   check that covers going out needs the script extended to play a round to its end — new work,
   and it should be priced before it is promised.
5. ⚠️ **`PartialCover` was deliberately left alone and is now measuring something else.**
   `IsComplete` agrees with `HandEvaluator.IsWinning` **only at five or more players**; at two,
   three or four a hand can cover exactly and still lose. Every rung in `BotCatalog` maximises
   cover count, so **every rung's objective is now the wrong objective at the table every
   published figure was measured at.** That is prediction 2 of P29 and it is where it gets priced,
   not fixed.
6. ⚠️ **`RoundEngine.MinimumPlayers` is still 4, and no packet owns changing it** (`RULES.md`
   §10 #7). `TableRules.For(2)` and `For(3)` are correct and tested and **unreachable from a dealt
   game** — the two strictest rules in §7.1.1 never execute. Making a table smaller than four
   playable needs the deal, the money layer and both front ends to agree, which is a packet of its
   own rather than a line in someone else's.
7. ⚠️ **A console capture from before P25 still compares** (proved, above) — but only by accident,
   and the accident is item 4. **The comparability note in `CLAUDE.md` is about front-end
   rendering and has never been about play.**

**The older note below is kept for its account of where the work came from.**
 P0–P23 are all done and every goal in §0 is delivered — the rules core, a
console worth sitting at, simulation at scale, a browser lobby two people can play in, and a
difficulty system with a settled answer to what actually works — ⚠️ **but the plan stopped being
the whole of the work on 2026-08-20**, when a playtest produced both `RULES.md` §5.1 and the
reason P24 exists.

**What P24 is for, in one line: playing beside an expert and coming away with a record of where
she disagreed.** The scope is Nick's, taken 2026-08-20 — **browser only**, **all four questions**,
**winner versus runner-up** rather than a ranking table, and **written to a journal** as well as
drawn. Three things a cold session should know before opening the packet:

- 🔥 **`BurmesePoker.Web` and `BurmesePoker.Server` have no journal at all.** `--journal` is on the
  console and the harness; the hosted table has never written one. That is a piece of work inside
  P24, not a field on an existing writer.
- 🔥 **What gets recorded is an opinion beside an answer, not a rationale on a decision.** At a
  table with an expert in it the answer is *hers*; the adviser's card and reasoning go beside it,
  so **disagreement is a query rather than something a person notices**.
- ⚠️ **The keys the explanation renders are packed for sorting, not for reading** — `outs` stores
  its key negated, `Potential` returns `int.MaxValue` for a joker, `cautious` packs two into one
  `long`. A rung describes its own keys; presentation never reads a bare number.
- 🔥 **Never explain through `FallibleAgent`.** A difficulty level is `Hardest` with a mistake
  rate, and P19's mistake *is* the runner-up of the very ranking P24 renders — so a level in the
  advice path would produce a page confidently justifying a move chosen because it was second
  best. The adviser is the bare rung at ε = 0, and P24 is the first thing that could get this
  wrong. **Acceptance 6 asserts it at every level, `easy` included.**

⚠️ **P24 and the §5.1 packet touch the same code and P24 goes first.** §5.1 makes a banned card an
illegal discard, which filters every agent's ranking — the very ranking P24 renders — but it is
blocked on four unrecorded specification questions (§9 #16–#19 — ⚠️ **all since answered; this line is history**) and P24 is what makes that conversation
productive. See **Decisions needed from Nick**.

**If you are picking this up cold and want to change something, the two documents that say what is
true are `docs/RULES.md` (rev 15) and `docs/STRATEGY.md`.** Neither is prose to be edited by hand:
a rule carries a provenance tag and a measurement is generated by `sim suite`.

⚠️ **The three ways this tree can go quietly wrong, and what fails when they do.**

1. **Add a rung and never re-run the suite.** `StandingAnswerTests` goes red: every rung in
   `BotCatalog` must be the subject of a published row in `docs/strategy/measurements.csv`. The
   run is **five hours** — budget it before adding the rung, not after.
2. **Move a level's ε.** The same file goes red: the ε values published are asserted to be the ε
   values `DifficultyLadder` offers. ⚠️ **Re-run the suite, do not edit the CSV.**
3. **Raise the ceiling.** A rung that separates becomes `BotCatalog.Hardest`, and every difficulty
   level, the browser's hint and the stand-in seat re-base onto it. `sim suite` exits non-zero if
   the dial stops being *monotone* — but 🔥 **P23's actual finding is that the failure mode is not
   inversion, it is a flat spot**: P21 left the dial ordered and passing every check with a middle
   step less than half the size of its neighbours. **Re-sweep ε and re-fit the spacing.** Expect
   to re-check rather than re-derive — the curve barely changed shape between `greedy` and `outs`.

⚠️ **The one saving left in the suite, and why it has not been taken.** `--pairs adjacent` on the
ladder would take fifteen head-to-head cells to five. It would also **throw away the matrix in
`STRATEGY.md` §3**, which is the document's centre and the only place `outs` is shown beating
every rung rather than the one below it. It stays available and stays unused. The saving that
*was* free — not measuring a rung in a cell where it is a known duplicate — was taken in P23 via
`BotRung.Ranked`, and cost nothing: every ladder figure reproduced to the digit without it.

⚠️ **What P21 and P22 changed about how to think about a new rung** — read this before writing
one. **P22 added items 5 and 6, and they are about cost and about scope rather than about odds.**

1. 🔥 **Where the new key goes decides whether it can pay.** Three research rungs are now
   measured. `cautious` and `counting` both slid their idea in *beneath* greedy's tie-break and
   both returned nothing (`+0.5 ± 0.55` and `+0.3 ± 1.0` the wrong way); `outs` put its idea
   *above* it and returned `+3.1 ± 1.0`. **Greedy's leftovers are worth about half a point and
   the apparatus cannot see half a point** (§7 of `STRATEGY.md`), so a rung that only refines
   the residue is unmeasurable before it is written.
2. ⚠️ **A rung is not free of the dial.** `Strength` is *known* strength, so a rung that
   separates gets a higher number, `BotCatalog.Hardest` moves, and all four levels re-base onto
   it — along with the browser's hint and the stand-in seat. That is correct and intended; it
   also costs, because `FallibleAgent` asks its inner rung to **rank**, so even `easy` pays the
   new rung's price.
3. ⚠️ **State the throughput budget in advance, and time the *decision*.** `sim bench` now plays
   whole rounds of one rung at four seats and prints rounds/s, µs/turn and a multiple of greedy;
   the primitives underneath it were never the whole story and for `outs` they were a quarter of
   it.
4. ⚠️ **A speed-up is a claim about answers.** Every one of P21's four is asserted against the
   search it replaces over real hands (`OutsBotAgentTests`), and the generator work was proved a
   refactor by capturing the console byte for byte at `--pick 0` from `HEAD` and from the tree.
   **Do not ship a shortcut whose only evidence is a comment explaining why it is safe.**
5. 🔥 **A rung that measures nothing still costs the standing suite for ever** (P22). A new name
   in `BotCatalog` is `k−1` new head-to-head cells in every future `sim suite` run, whatever it
   turns out to be worth — `prospector` added six and about three quarters of an hour, and all
   six are `outs` against itself in all but name. **Before adding a rung, ask whether the
   standing instrument is the right place to measure it**, and if it is not, say where is.
6. ⚠️ **A rung whose decision reads the *stakes* is not one player** (P22). The stakes are fixed
   at the start of a game and are not a rule (RULES.md §4.3), so `prospector` is literally
   `outs` at $5/$1 and a different player at $5/$40. That is why `BotCatalog.Strength` is an
   **ordinal of known strength** and not a score, why such a rung shares its neighbour's number,
   and why it is measured in a sweep of its own rather than ranked in a field played at one
   stakes.

⚠️ **P15's precedent is still designed into the plan** — a whole packet on a plausible rung
worth +0.5 ± 0.55 points — and P17 priced it: at 8,008 games a cell a rung worth less than about
a point is not promotable, and half a point costs ~34,000 games a cell.

⚠️ **What P19 leaves for whoever adds a rung.**

1. 🔥 **A new rung changes the difficulty dial, whether or not anybody meant it to.** Every level
   is `BotCatalog.Hardest` with an ε, so a rung stronger than `greedy` becomes the base of all
   four levels the day it lands, and the ε values stop being the ones that were measured. **`sim
   suite` fails the run if the dial stops being monotone**, which catches the worst case but not
   a dial that has merely gone uneven — re-run the calibration and re-space it (P23 owns this).
2. ⚠️ **A rung that a level can be built on has to implement `IRanksDiscards`.** Three of the
   four do; `random` does not, and `FallibleAgent` refuses it rather than accepting an ε that
   does nothing. A rung whose discard does not come out of `CoverScore.Ranking` gets the
   interface for free only if it is written that way.
3. ⚠️ **`ComputerAdvice` and `TableOptions.StandIn` are `BotCatalog.Hardest` — the *rung*, not a
   level — and now say so in three places.** A hint that got worse as you lowered the difficulty
   would be absurd, and a seat the computer took over for somebody should not start playing
   badly.
4. ⚠️ **`--strategies` resolves three vocabularies now**: a rung, a difficulty level, and a
   calibration probe such as `greedy@0.35`. `LayeringTests` bans constructing either a rung or a
   `FallibleAgent` outside `Domain/Agents`.

**What P19 built, in one paragraph.** Three new files in `BurmesePoker.Domain/Agents` —
`DifficultyLadder.cs` (with `DifficultyLevel`), `FallibleAgent.cs`, `IRanksDiscards.cs` — a
`Ranking` on `CoverScore` that `Discard` is now the head of, `PairFamily` and
`TournamentReport.Met` in the harness, a second tournament inside `Suite`, and the two front ends
rewired from `BotCatalog` to `DifficultyLadder`. **`TablePlan` gained `Difficulties`** — a level
per computer seat, with the existing single-value `Difficulty` as its shorthand. **Nothing in the
engine changed**, which is the fifth packet in a row to add a whole capability over seams that
already existed.

**How to prove a console change is a refactor, now that the prompt is a list.** Capture before
and after and compare **from the `Seating:` line on** — the prompt above it is allowed to differ
— and pass `--pick n` so both captures chose the same setting: `--pick 0` is `expert`, `1` `hard`,
`2` `medium`, `3` `easy`. ⚠️ **P19 changed that map** — before it the list was the ladder
(`greedy`, `cautious`, `simple`, `random`) — which is why a capture is only comparable with one
that made the same choice *in the same build*. ⚠️ **The difficulty question is the third prompt with
`--script bots` and the fourth with `--script human`**, because a match with a person in it asks
that person's name first; `DIFFICULTY_KEY` in the driver holds both, and getting it wrong makes
`--pick` do nothing at all rather than fail.

**What P17 built, in one paragraph.** Six new files in `BurmesePoker.Sim` — `GameValue`,
`Normal`, `Holm`, `StrategySeries`, `Tournament`, `TournamentCsv`, `Suite` (with `SuiteCsv`) —
plus a rewritten `Measurement`, `SeatingPlan.HeadToHead`, two verbs and intervals on the ordinary
report. **`BurmesePoker.Domain` is unchanged, and so are `Simulator`, `GameRunner` and
`Replay`** — the fourth packet in a row to add a whole capability over seams that already
existed (§3.7/§3.8 paying off again).

⚠️ **The two things most likely to be got wrong by a future session.**

1. 🔥 **A win rate is `Σwins / Σseat-rounds`, not the average of the per-game rates.** They differ
   by about a point at four seats whenever a strategy holds a different number of seats in
   different games, and only the first is comparable with P12, P15 and P16. `GameValue` carries a
   **total and its trials** so that a measurement cannot accidentally become a mean of ratios;
   `Measurement.Of` is the ratio estimator and reduces exactly to the per-game mean when the
   denominator is constant. `SuiteTests.AMeasuredWinRateIsTheSameNumberTheHarnessHasAlwaysPublished`
   is the pin, and it deliberately runs a **crossed** seating, because under a rotation the two
   readings agree and the test would be vacuous.
2. 🔥 **`Measurement.Paired` takes per-game values and not two finished `Measurement`s, and joins
   on the game's *seed* rather than its index.** Two cells of one master seed deal game *i* from
   the same shoe and may be paired; two runs of different master seeds share nothing, and pairing
   those by position is silently wrong. The seed join makes that mistake *fail* instead. ⚠️ And
   the across-cell comparison only buys anything if **both cells seat the strategy the same way
   round** — a head-to-head cell enumerates its seatings as an odometer over its two players, so
   comparing a cell it leads with one it follows in moves it round the table and throws the
   shared shoe away. That is why `Tournament.PairingChecks` picks two opponents from the same
   side of the field.

**Regenerating the published numbers** costs about 15 minutes and ~64,000 games:

```
dotnet run -c Release --project BurmesePoker.Sim -- suite --strategies random,simple,greedy,cautious --games 8000 --seed 20260819
```

**Do not hand-edit `docs/strategy/measurements.csv`.** `docs/STRATEGY.md` quotes it, and P23
turns "quotes it" into a test.

*Anything a cold context would need: decisions taken, surprises, deliberate leftovers.*

**From the 2026-08-19 planning session — read these before starting P17:**

- 🔥 **`Measurement` already exists and is used by one verb.** `Sim/Measurement.cs` is a mean over
  **games** with a standard error and a 95% interval, built for P16's cells. The ordinary
  `Report` table in `Sim/Program.cs` uses none of it. **P17 is mostly wiring what is there into
  the path everybody actually runs**, plus the paired form.
- 🔥 **Common random numbers are already available and are being thrown away on purpose.**
  `SeedSequence.GameSeed(master, game)` makes game *i* the same shoe in every cell of a master
  seed, whoever is seated, so a comparison between cells is **paired**. `Measurement.Difference`
  says in its own remark that it adds the variances anyway to be conservative. ⚠️ **The paired
  form must take per-game values, not two finished `Measurement`s** — pairing cells that did not
  share seeds is silently wrong, and only the signature can prevent it.
- ⚠️ **Four strategy names are load-bearing.** `random`, `simple`, `greedy`, `cautious` are half
  of every CSV row's join key (§3.8 item 4) and appear in P14 journal headers. ✅ **P18 did not
  rename them, and `BotCatalogTests` now freezes all four in a literal** — a test that read the
  catalog to check the catalog would agree with any rename at all.
- ✅ **`scripts/drive-console.py` + `cmp` is how P18 proved it was a refactor** — with the two
  amendments above (compare from `Seating:`, and pass `--pick`).
- ✅ **The hint does not follow the difficulty.** `ComputerAdvice` and `TableOptions.StandIn` both
  ask `BotCatalog.Hardest`, and all three sites say why out loud, because it is the obvious thing
  for a later session to "fix".
- ⚠️ **P20 raises a rules question and must not decide it in code.** RULES.md §6.3 makes the
  discards public and P13.5's client already draws a discard pile per seat to every watcher, but
  `TurnContext` shows a seat only the one discard it is offered. Whether a pile is inspectable,
  or only its top card, goes to §9 and to `QUESTIONS-FOR-MYA-LAY.md`. **Safe default: a seat
  counts only what it has actually been shown** — wrong in the direction that makes the bot weak
  rather than the direction that makes it cheat.
- **Measured while planning, for sizing:** a balanced 4-seat run of the four rungs did
  **2,040 rounds in 18.2 s (112 rounds/s)**, 27.9 turns a round, 8 games abandoned at the turn
  cap (the random-heavy tables). So a six-pair tournament at 2,000 games a cell is on the order
  of two minutes. `random` took 48.7% and claimed 46.2% — coin flips, which is a free sanity
  check that the harness is wiring the agents up the way it thinks it is.

⚠️ **Two tests are wall-clock budgets and fail on a busy machine.**
`HandEvaluatorTests.EvaluatingThirteenCardsIsFast` and `PartialCoverTests.ScoringThirteenCardsIsFast`
each assert that three pathological thirteen-card hands are searched in under a second. They
normally take single-digit milliseconds — P12 measured 91 µs and 140 µs a hand — so the budget
has three orders of magnitude of headroom, and they still fail if `dotnet test` is run while
something else is saturating the cores. **Reproduced deliberately on 2026-08-19** by running the
suite against three concurrent `BurmesePoker.Sim` runs: one or the other failed on two attempts
out of three, and six unloaded runs in a row were green. **They are the only two tests in the
suite that can fail for a reason other than a defect**, they predate P14 (P5 and P10 built them),
and a cold context following the `/poker` baseline rule — *any failure at all is a real problem* —
should re-run on a quiet machine before believing either of them. Nothing has been changed about
them; loosening a performance guard is a decision for whoever owns it, not a tidy-up.

**From P13.6 — the state of the finished thing:**

- 🔥 **Run it: `dotnet run --project BurmesePoker.Web`, then open `http://localhost:5188/`.**
  That is the **lobby**. Type a name, press **Sit down**, and you are at `/table/1?you=<name>`.
  **The table does not deal until every person seat is filled** — open a second browser (or a
  second profile) and sit down again to start it, or open a table with `--people 1`. `--people 0`
  is P13.3's room with nobody in it, which deals as soon as anybody watches.
- **The command line names the table opened at boot**: `--table`, `--seats`, `--people`,
  `--seed`, `--pace`, `--between`, `--patience`, `--name` (what the lobby's form suggests calling
  you), `--hints false`. ⚠️ **`--seat` is gone** — a lobby seats you.
- ⚠️ **`Properties/launchSettings.json` fixes the port at 5188 and beats `ASPNETCORE_URLS`.**
  Setting the environment variable and then curling the port you set is a wasted five minutes;
  read the startup line.
- ⚠️ **Do not `pkill -f BurmesePoker.Web` from a bash tool call** — the pattern matches the shell
  running the command and kills the tool call itself (exit 144, twice in this session). Kill by
  port: `ss -lptn 'sport = :5188'`.
- 🔥 **`Lobby` → `HostedTable` → `TableSession`, and that is the whole stack above the engine.**
  A component may reach the lobby and the table it names; `MarkupStandardsTests` still forbids
  every route round the fan-out, `ConnectionFor` included.
- ⚠️ **`SeatBoard` is per viewer now.** A component that wants one takes it as a parameter. The
  only thing that builds one is `HostedTable.SitDown`, and the only thing that disposes one is
  `HostedTable.StandUp` — which is why `TableView.Dispose` calls it.
- ⚠️ **`ClickingPlayer` is the tool for anything multi-seat.** It drives a `SeatBoard`, so
  everything it writes down is something a page would have drawn.

**From P13.4 — still true, and read them before touching the seat:**

- 🔥 **Run it: `dotnet run --project BurmesePoker.Web`.** You are seat 1. The options are plain
  configuration: `--seed`, `--pace`, `--between`, `--seats`, and now **`--seat` (which seat is
  yours, `0` to only watch), `--name`, `--hints false`, `--patience` (seconds a question waits
  for you; 90 by default rather than the server's 45).**
- 🔥 **`SeatBoard` is the only place a hand comes from, and `TableBoard` is the only place the
  public game comes from.** Two connections, and the difference between them is the whole of the
  concealment: the watcher is folded into the board every page draws, your seat is folded into
  the half only you draw. ⚠️ **`MarkupStandardsTests.NoComponentFindsASecondRouteToTheTable`
  now scans for that** — no component may name `TableSession`, `MatchEngine`, `TableState`,
  `TurnContext`, `PartialCover`, `HandEvaluator` or `ConnectionFor`.
- ⚠️ **The public board is still the watcher's, deliberately, even when you are seated.** So the
  round log never says what *you* drew — your own draw arrives as `SeatPrompt.Taken` and
  `TurnPrompt` says *"You took 7♦"* there. A second tab on the same page must not be able to
  read your hand out of the log.
- ⚠️ **`ClickingPlayer` (tests) is the browser's `ScriptedSeat`.** It drives a `SeatBoard` rather
  than a `SeatConnection`, so everything it sees is something a page would draw — which is why
  P13.4's acceptance is asserted through it. It answers inside `Changed`, on the round's own
  thread, so three whole rounds run with no sleeps and no polling.
- 🔥 **Read a control's *accessible name*, not just look at it.** `CardChip`'s hidden words now
  end in a full stop, because a card is always said next to something else — the per-card cost in
  a hand, the rest of a line in the log — and without one the button read *"five of clubs melds
  nothing"*, which means the opposite of itself. **Nothing on screen changed.**
- ⚠️ **`favicon.ico` 404s.** A browser asks for it unprompted; the page names no such URL. It is
  the only 404 in a run and it predates this packet. Not fixed — nobody asked for a favicon.
- **How the UI was verified, and it is worth checking in next time.** Headless Chromium over the
  DevTools protocol driven from `node` with the built-in `WebSocket`: navigate, poll for a
  question, `Input.dispatchKeyEvent` `Tab` until the focused control is the one wanted, `Enter`,
  repeat. It played five rounds. **It was not checked in** — three short scripts in a scratch
  directory — and P13.5 wants two people, which is two of them at once.

**From P13.3:**

- 🔥 **Run it: `dotnet run --project BurmesePoker.Web`.** It uses `Properties/launchSettings.json`,
  which sets `ASPNETCORE_ENVIRONMENT=Development`. ⚠️ **Running it with `--no-launch-profile` and
  no environment gives you Production, where `MapStaticAssets` cannot resolve the build's
  development endpoints manifest and `blazor.web.js` 500s** — the page still renders, because a
  Blazor Server page prerenders, and then never moves again. A *published* output is fine.
  Options are plain configuration: `--seed`, `--pace`, `--between`, `--seats`.
- 🔥 **The page draws from `TableBoard`, which is folded from `TableEvent`s and nothing else.**
  `TableHost` holds one `TableSession`, one watcher connection from `TableSession.Watch()`, and
  the board it folds. ⚠️ **Do not give a component the session.** The banks, the seating and the
  turned-up cards are all on the board *because they arrived as events* — a component that asked
  `TableSession` for its banks would be the second route round the fan-out that P13.3's
  acceptance calls "the thing to refuse", and `ConcealmentTests` would stop standing in front of
  the page.
- ⚠️ **Seating a person is `TableSeat.Person` plus that circuit's own connection.** Today every
  seat is `TableSeat.Computer(..., PacedAgent.Wrap(new GreedyBotAgent(), pace))`. A seated page
  gets its `SeatConnection` from `TableSession.ConnectionFor(player)` — **never the watcher's** —
  and draws its hand from `SeatPrompt.Hand`, which is a `HandView` and already carries the
  computer's suggestion when the table offers hints.
- ⚠️ **`TableHost.Start()` is idempotent because prerendering runs `OnInitialized` twice** (C13)
  and because every browser that opens the page runs it too. Keep it that way when it becomes a
  lobby.
- ✅ **Discharged by P13.4. `MarkupStandardsTests.EveryHandlerIsOnARealControl` passed vacuously**
  — there was not one control on the table. It now counts what it scanned and fails if the
  client ever goes quiet again. **A card you throw is a `<button>`**, and eight handlers are
  scanned.
- ⚠️ **`@key` comes from `LogLine.Sequence`, which is `TableBoard.Narrated` and not
  `Log.Count`** — the log trims at 240 lines and a key from the length would repeat. Cards are
  keyed on `CardView.Card.Id.Value` and seats on `PlayerId.Value`.
- ⚠️ **The visually-hidden spans (`.said`) need a positioned ancestor.** `.chip` and `.who` are
  `position: relative` for exactly that reason; a new one without it stretches the document by
  however far down the page its static position falls.
- **How the UI was verified, so the next packet can do the same.** Headless Chromium over the
  DevTools protocol, driven from `node` with the built-in `WebSocket` — open the page, wait,
  read the DOM twice, `Page.captureScreenshot`, and walk the tab order with
  `Input.dispatchKeyEvent`. It is the browser's equivalent of `scripts/drive-console.py` and it
  is how "the circuit really pushes updates" stopped being an assumption. **It was not checked
  in** — it is four short scripts in a scratch directory — and it is worth writing properly if
  P13.4 wants a regression test rather than a one-off.

**From P13.2:**

- 🔥 **Read this before P13.3. The concealment is done and it is one `if`.** Exactly one event
  the engine narrates is private — the blind draw — because a discard, a taken discard, a
  claimed money card and a declaration all happen in front of the table (RULES.md §4.5, §5,
  §6.3, §7.1). `TableFanOut.PlayerDrew` sends the card to the drawer and null to everyone else,
  and that is the whole filter. ⚠️ **A component must not find a second route to the session.**
  Everything a page renders comes from the `SeatConnection` it was given; a component that
  reached for `TableSession`'s `MatchEngine`, or for a `TableState`, would walk straight round
  the boundary these tests guard.
- 🔥 **The snapshot rule caught two more live lists, and it will catch more.**
  `TableState.TurnedUpOnTable` is aliased by *both* `TurnContext.TurnedUpMoneyCards` and
  `IGameObserver.RoundStarted`, and the opening player's claim removes a card from it (§4.5). A
  prompt or an event that kept the reference would stop saying what it said when it was sent.
  ⚠️ **Assume every list the engine hands over is live until shown otherwise** — `Deck.Cards`,
  `PlayerState.Hand` and `PlayerState.Discards` all say so in their own doc comments.
- 🔥 **How to test a table without threads.** `SeatConnection.Updated` is raised on the round's
  own thread *before* the seat begins waiting, so a handler that answers immediately latches the
  answer and the wait returns at once. `Tests/Server/ScriptedSeat.cs` is built on it, and it is
  why eighteen tests that play real rounds run in about four seconds with no sleeps, no polling
  and no flakiness. **A Blazor component is the other shape** — it answers later, from a circuit
  — and `AnAnswerFromAnotherThreadIsWaitedFor` covers that one deliberately.
- ✅ **`PacedAgent` moved in P13.3 — to `BurmesePoker.Presentation`, not to Domain.** P13.2 left
  the stand-in unpaced on purpose: a sleep belongs to whatever draws the table, not to a server
  that may host many of them, so `TableOptions.StandIn` is a `Func<IPlayerAgent>` a host wraps —
  and `TableHost` now does. **Domain was rejected because `BurmesePoker.Sim` references it**, so
  a sleep there is reachable from P12's hot loop. The console plays byte-identically after the
  move.
- ⚠️ **A client bug must not end a round.** `SeatConnection.Answer` returns **false** for an
  answer that does not fit the question or names a card the seat is not holding, and **the
  prompt stands** so the client may correct itself. `RoundEngine` throws in the same situation,
  which is right in a domain and wrong over a connection. `SeatAnswer.Fits` is where each case
  checks itself, so a new question type cannot skip the check.
- ⚠️ **The takeover is per question, announced once a turn.** Keyed on `(Round, TurnNumber)`,
  the same key and the same reasoning as P11's pacing decorator: a turn asks a varying number of
  questions, so announcing per question would make a silent seat noisier on some turns than
  others for no visible reason. §3.11 C16 wants the client to *say* it — *"Sable is playing your
  seat"* — and `TableEvent.SeatPlayedByTheComputer` is the event to say it from.
- **The wall-clock bound wraps every seat, bots included**, because what goes wrong at a hosted
  table is the people rather than the play — a table nobody is left at would otherwise play
  bot-perfect rounds for ever. `TableOptions.RoundTimeLimit` defaults to two hours; **a test sets
  it to `TimeSpan.Zero` to abandon deterministically**, and to a couple of minutes so a round
  that will not finish fails the suite rather than hanging it.
- **What P13.3 already has, and should not rebuild.** `TableSession.Watch()` is the watcher
  connection — public narration, never a question, nothing it may not see — and
  `TableSession.Leave(connection)` is its disposal. `SeatConnection.Updated` is the
  `InvokeAsync(StateHasChanged)` signal. `CardView.Card.Id.Value` is the `@key` and
  `CardOrder.Display` is a total order (P13.1), so C14 is already closed.
- **Deliberately not built:** any lobby (P13.5), any journal wiring into the server (a
  `JournalingAgent` wraps a seat the same way a `BoundedAgent` does, and nobody has asked),
  and any notion of a *player* separate from a seat. A `TableSeat` is a `PlayerId`, a name and
  optionally an agent, and that has been enough twice now.

**From P13.1:**

- 🔥 **Read this before P13.2. A view model that aliases the engine's list is not a view
  model.** `TurnContext.Hand` returns the seat's **own live list**, and `RoundEngine.TakeTurn`
  discards from it the moment `ChooseDiscard` returns. The first draft of the extracted
  `HandView` kept the reference and so reported fourteen cards while holding thirteen. **The
  console never showed the bug** — it draws the panel and drops the view in the same statement —
  and it only appeared because a test held the view past the decision. ⚠️ **A browser holds
  every view past the decision.** `HandView.Of` copies now; **anything P13.2's fan-out hands a
  seat must copy too.** ✅ *Done — and P13.2 found two more live lists doing it.*
- ⚠️ **The `IAnsiConsole` half of the packet is done and its stated benefit is unreachable.**
  `Program` reaches for `AnsiConsole.Console` exactly once and hands it to everything below, so
  the console *is* drivable by any `IAnsiConsole`. But the test project may not reference
  `BurmesePoker.Console` (§2), so no test can drive it — **and that rule is worth more than the
  benefit. Do not resolve the tension by adding the reference.**
- ✅ **`scripts/drive-console.py` is checked in, and it is how a front-end refactor gets
  proved.** It drives the built console under a pty and writes down every byte. ⚠️ **Keys are fed
  on *quiescence*, never on a clock** — that is the whole trick; a timed driver races the
  renderer and produces a different file every run. Capture at `HEAD` (a `git worktree` of the
  old tree builds in seconds), change, capture again, `cmp`. P13.1 did five runs across three
  seeds including `--no-hints`; all identical.
- ⚠️ **Two copies of the same card cost the same to throw, and that is the right answer.** A
  spare copy is a standing replacement for the one the cover used, so throwing either leaves the
  same thirteen melding. The instance identity that matters (§3.1) is that they are **two
  entries keyed on `CardId`** and that exactly one of them is `Melded` — not that they are
  priced differently. **A browser must not imply the melded copy is dearer than its twin.**
- **Costs are computed eagerly now, where the console memoised them lazily.** A view model is
  data a component iterates twice and a test asserts over, not a lazy graph that searches a hand
  when a renderer touches it. One `PartialCover.Best` per card, a few milliseconds a hand, once
  a turn per seat.
- ⚠️ **`DisplayTokens.States` is the enumeration §3.11 A2 asserts over**, and it is public
  because a legend wants it. A `CardDisplayState` added without a token fails
  `DisplayTokensTests`, which is the point — the standard keeps itself.
- **Where the boundary actually fell.** Presentation owns *what to show*: near-melds, per-card
  cost, display state, display order, the computer's suggestion. The console kept *how*: Spectre
  markup, colours, panels, label padding. `Palette` stayed in the console and is correct there —
  but its **glyphs** are now `DisplayTokens`', so a browser marks an owned money card with the
  same star.
- ⚠️ **`MoneyCardRegistry.Multiplier` doubles on the *overlap* of the two designations**, not on
  a repeat. Turning the Q♣ up twice still pays once; turning up the 7♦ — already permanent —
  pays twice (RULES.md §4.1, §4.3). A test was written on the wrong assumption first.

**From the 2026-08-19 planning session (P13 re-plan — docs only):**

- 🔥 **The question that drove it: "rich browser UI or multiplayer first?" The answer is that
  it is one question.** Only *where the engine runs* couples them, and concealment settles it:
  a hand is fully concealed with money on it (`RULES.md` §7.1), so an engine in the browser
  holds every player's hand in every player's client. **Not fixable by discipline and not
  retrofittable** — moving the engine later rewrites the client's whole relationship to the
  game. `BUILD-PLAN.md` §3.10.
- ⚠️ **What a cold context must not undo: no WASM engine, ever.** `InteractiveServer` keeps the
  C# server-side. WASM stays available for components holding **no concealed state**, and the
  burden of proof is on any component that wants to move there.
- ⚠️ **P13.1 was replaced rather than renumbered**, and the reasoning matters more than the
  packet: the old one assumed the second front end would also be Spectre. It will not be. The
  cut line is **decision versus drawing**, which is why `BurmesePoker.Presentation` exists.
- ⚠️ **A fifth project is a real cost and the alternatives are written down** (§2) so it can be
  argued with: Blazor referencing the Spectre console (rejected — `LayeringTests` exists to stop
  exactly that), or the browser re-deriving the hand view (rejected on drift — the same argument
  that produced `MeldIndex` and `CoverScore`).
- ✅ **Blazor Server means P13.2 has no protocol to design.** No wire format, no client-side
  state sync, no serialising a `TurnContext`. The "server" is a seat and a fan-out, **testable
  in-process with no sockets** — which is what gives P13.2 a mechanical acceptance criterion,
  the first a multiplayer packet in this plan has ever had.
- ⚠️ **§3.11's seventeen UX items are not advisory.** Five are tests and land before there is
  anything to retrofit; the rest are reviewed by playing. **The irreversible one is C12** —
  static SSR by default, `@rendermode InteractiveServer` per component, never at the root.
- **The vibe check on Blazor, since it was asked:** first-class inside .NET since the .NET 8
  render-mode unification, .NET 10 is LTS to 2028, and this repo is already on net10.0. It is
  invisible *outside* .NET, its ecosystem is component-suite-shaped rather than npm-shaped, and
  anything genuinely animated is JS interop. A concealed-hand card game is forms-and-state
  shaped, which is the centre of what Blazor is good at.
- **Nothing was built.** Still **294 passed / 0 failed**; `RULES.md` untouched at rev 13.

**From P16:**

- 🔥 **The result, in one line: a weaker player anywhere at your table is worth 4–5 points to
  you, and which side of you they sit on is worth nothing** — unless they are `random`, where
  the edge is `+9.1 ± 2.1` points. Between `simple` and `greedy`, a 12.9-point skill gap, the
  directional effect is `−1.0 ± 2.1`: an interval that contains zero. `BUILD-PLAN.md`
  "What P16 found" has every cell.
- ⚠️ **The downstream control arm is what makes the number mean anything, and it moved the
  answer by a factor of two.** The gross upstream effect of `random` over `greedy` is +19.4
  points; the *same* swap made downstream is worth +10.3. Without the second arm the packet
  would have reported 19.4 and been wrong. **Any future neighbour claim needs the same arm.**
- ⚠️ **P15's "all 5.4 points of that gap is who fed whom" was wrong, and is corrected in
  place below.** It is a *neighbour* effect, not an *upstream* one — a rotation moves both
  neighbours at once. Nothing about P15's own measurements changes; only the reading of the
  accident it found.
- ⚠️ **`SimulationOptions.Assignments` is the seating fix and it is opt-in.** Null keeps the old
  rotation, so nothing already measured moved. `SeatingPlan.Balanced(strategies, seats)` is
  every assignment (capped at 4,096 — six seats of the four-rung ladder is exactly that);
  `SeatingPlan.Rotations(pattern)` walks one pattern round the cycle, which is what a cell uses
  to average the opening seat away. **Both are validated**: an assignment naming a strategy the
  run did not list would play and then be missing from every total.
- ⚠️ **The CSV grew two columns and one existing test had to be taught about them.**
  `JournalReplayTests.Outcomes` blanks the strategy field so a renamed replay compares equal —
  it now blanks fields 5, 6 **and** 7, because `upstream_strategy` and `downstream_strategy`
  carry the same names. Both derive from the *seating*, never from `SimulationOptions`, which is
  what P14 warned about and is why `AJournalledRunReplaysToTheSameRows` still passes untouched.
- ⚠️ **The mechanism variable barely moved, and that is the open question this packet leaves.**
  `takes` was supposed to be the road the effect travels; across a contrast worth 9.1 points of
  win rate it moved **1.6 ± 0.9**. So upstream skill changes *what* is offered, not *how often*
  something worth taking is. **The tool for that is a rich journal** (§3.9/P14) — re-run the two
  `random`-upstream cells at `--fidelity rich` and read the hands. It is a question, not a
  packet, and nobody has asked for it.
- **The intervention came back null, and `cautious` did not pay for it either.** Focal win rate
  `−0.3 ± 1.4` and take rate `−0.3 ± 0.9` against a greedy upstream; cautious's own win rate in
  that seat was 31.8% against greedy's 31.3%. **A strategy built to deny cannot be shown to deny
  anything** — P15's "denial and self-interest coincide" in its strongest form.
- **What a run costs.** Eight cells × 4,000 games is **32,000 games in 8–12 minutes** on this
  machine (parallel, Release); the two seeds together took about twenty. A quick look is
  `--games 500` — about a minute, and it resolves nothing finer than ~10 points.
- ⚠️ **Two new test classes, and only one of them joins `WallClockBudgets.Collection`.**
  `NeighbourExperimentTests` plays simulations and joins it; `SeatingPlanTests` plays no games
  at all, because every claim it makes is a property of an assignment list — which is the level
  the confound actually lives at.

**From P15:**

- 🔥 **Read this before starting P16.** ✅ *P16 is done — see above. Kept for the reasoning, and
  ⚠️ **its last sentence is the part P16 corrected**.* `greedy` and `cautious` are **0.5 ± 0.6 points apart
  head to head over 32,000 games** and **5.4 points apart in the four-way ladder run**. The
  rotation feeds greedy from `simple` and cautious from `greedy` in every game, so the gap is
  the feeding and not the strategies. It is **a lead, not a result** — the downstream neighbour
  differs too, which is exactly what P16's control separates — but it sizes the effect at
  **points, not tenths**, which makes 2,000 games a cell comfortable.
- ⚠️ **The ladder has four rungs and three skill levels.** For P16's skill dial use
  `random` / `simple` / `greedy` (separated by 36.9 and 12.9 points). `cautious` is the
  **intervention arm**, not a fourth level.
- ⚠️ **P16's intervention is weaker than P16 assumed, and the prediction should be stated at
  the measured size.** P15 found that denial and self-interest coincide: the cards a hand least
  wants are already the cards an opponent can least use, so `greedy` is *already* nearly a
  maximal denier. Swapping a greedy upstream for a cautious one should move the focal seat's
  take rate a little and its win rate barely at all. **If it comes back large, something other
  than denial is doing the work.**
- ⚠️ **Do not try to beat greedy with another tie-break.** Partnership is symmetric, so the
  total partnership of the twelve cards kept is the hand's total less twice the thrown card's:
  "throw the fewest partners" and "keep the best-connected twelve" are the *same rule*, and
  every pairwise-additive measure collapses back into `GreedyBotAgent`. Both denial measures
  tried reduce to `Supply(rank) − Potential` to within a point or two. **A genuine rung above
  greedy has to be combinatorial** — counting live outs (unseen values that would raise the
  cover count of the thirteen kept) is the obvious candidate and costs a `PartialCover.Best`
  per value per candidate, roughly **100× a decision**. That is a packet of its own, and it is
  **not** in the plan; raise it with Nick before building it.
- **The rungs are one function apart, in code.** `CoverScore.Discard(hand, tieBreak)` is the
  loop all three thinking rungs throw through — simple passes `NoPreference`, greedy passes
  `Potential`, cautious packs `Potential` and the threat into one `long`. ⚠️ **Keep it that
  way**: P12's whole result rests on the rungs differing in exactly one decision, and the
  refactor was verified by the 259-test baseline staying byte-identical before anything new
  was added.
- ⚠️ **A random rung must never draw from `Random.Shared`** (§3.7 item 1).
  `SeedSequence.SeatSeed(gameSeed, seat)` is where a seat's generator comes from, and
  `Strategy.Create` takes that seed as its argument so there is nowhere else to get one.
- ⚠️ **A table of nothing but `random` essentially never declares.** Every game of it hits the
  turn cap and is reported abandoned — which is correct, and which is why the tests that use it
  read the **journal** rather than the CSV: an abandoned round produces no row.
- ⚠️ **`WallClockBudgets.cs` is new, and it is load-bearing.**
  `HandEvaluatorTests.EvaluatingThirteenCardsIsFast` and
  `PartialCoverTests.ScoringThirteenCardsIsFast` started failing on a *quiet* machine once the
  ladder tests ran simulations beside them. The four heavy Sim/ladder classes and the two
  budget classes now share one xunit collection, so they never run concurrently; four
  consecutive full runs are green. **Neither budget was loosened** — a performance guard is the
  owner's call, not a tidy-up. **A future test class that plays whole simulations should join
  that collection.**

**From P14:**

- ⚠️ **The one thing not to undo: `BurmesePoker.Console` now draws two `Random`s from
  `--seed`.** `setup` seats the table, and the match's generator is `new Random(setup.Next())`.
  A journal reproduces the deal by re-seeding the *match's* generator, so anything that draws
  from it before round 1 makes a replay deal a different game — which is exactly what the old
  single-generator arrangement did. **A consequence, stated plainly: a `--seed` from a build
  before P14 no longer plays the same console match.** Verified afterwards that two runs at
  `--seed 4242` are still byte-identical to each other, so P11's determinism survived.
- **The strongest acceptance the tree has: a replayed run's CSV is byte-identical to the played
  one's.** `Replay.Run` reuses `Simulator.Summarise` and `GameRunner`'s row-building, so it is a
  `diff` rather than a judgement. `dotnet run -c Release --project BurmesePoker.Sim -- --games 20
  --rounds 2 --journal run.jsonl --csv run.csv` then `-- replay run.jsonl --csv replay.csv`
  produces identical files.
- ⚠️ **Rich fidelity turned out to cost nothing measurable, which §3.9 did not expect.** Serial
  (the only quiet regime on this machine), three interleaved repetitions of 400 games:
  **46–49 rounds/s with no journal, 48–49 thin, 48–50 rich.** The arithmetic is why — a
  thirteen-`CardId` copy is tens of nanoseconds against a `PartialCover.Best` that P12 clocked at
  140 µs. **The expensive axis is bytes, not time**: rich is 9.6 KB a round against thin's
  5.0 KB. It stays off by default for what it costs to keep. ⚠️ **Parallel throughput could not
  be measured here** — the baseline itself swung 68–98 rounds/s run to run — so the serial figures
  are the honest ones.
- **`GameRunner` was refactored, not duplicated.** `Play` and `Replay` share one private `Run`,
  so a replayed game and a played one build their rows through the same code. The row builder now
  takes the players list rather than assuming seats are `PlayerId(0..n-1)` — **which is what lets
  a console match, whose seats are `PlayerId(1..n)`, replay under the harness at all.**
- **The journal is data, and its equality is its text.** `GameJournal`, `JournalHeader` and
  `DecisionSnapshot` are plain records whose list members compare by reference, so the round-trip
  tests compare *lines*. That is the right notion here — a journal is worth having because it
  survives being written down — but a later packet that wants to deduplicate or group decisions
  should add structural equality rather than assume it.
- **Answers are `CardId`s, never values** (§3.1), and `JournalPlayerAgent` fails loudly three
  ways: the journal running out, the next entry not being the question asked, and a discard the
  seat is not holding. All three are tested, and all three print a clean message at the CLI
  rather than a stack trace.
- **A console match replays under the harness.** Verified through a pty: four bot seats at
  `--seed 4242 --pace 0 --journal match.jsonl`, then `Sim -- replay match.jsonl` reporting the
  same two rounds the console printed (Sable out both times, +$16 in 29 turns then +$13 in 29).
- **Deliberately not built:** anything that indexes, queries or aggregates journals. The CSV is
  still what analysis reads; a journal is where you go when the CSV raises a question it cannot
  answer.

**Planning session, 2026-08-19 (docs only — no code, still 239 passed / 0 failed):**

- **Asked what the persistence layer is. The answer is that there isn't one**, and that turned
  out to be a defensible position rather than an oversight: `CsvReport.WriteTo` is the only
  write to disk in the tree, and a bot game is a pure function of its seed, so a log of one is
  redundant the moment it is written. **`BUILD-PLAN.md` §3.9** records why that stops being true
  — a person is not a function of a seed, a seed only replays against the code that produced it
  (P12 already edited `GreedyBotAgent`), and §3.8's one open ⚠️ row wants the decisions
  themselves. **P14** is the packet: journal types and two `IPlayerAgent` decorators in Domain,
  the format in one place, `File.WriteAllLines` in the consumers, and replay as *a seat that
  answers from a file* rather than a resumable engine.
- **A hypothesis worth testing arrived from outside** (Nick's friend): *the main thing that
  decides your game is the skill of the player before you.* It is well-posed — `RULES.md` §5
  makes a table a directed cycle, since only the immediately-previous player's discard is
  available — and it is **a strategy question, not a rules question, so it does not go in
  `RULES.md`.** **P15** builds a ladder of strategies to have a skill dial at all; **P16** runs
  the experiment.
- ⚠️ **The finding that matters most, and a cold context must not miss it:
  `SimulationOptions.Seating` cannot answer P16's question.** It rotates *one fixed pattern*,
  `Strategies[(seat + game) % Strategies.Count]`, so at two strategies and four seats it only
  ever produces `[A,B,A,B]` and `[B,A,B,A]` — **every A is fed by a B, always**. The pair
  *(my strategy, upstream strategy)* is perfectly confounded, not merely underpowered. P16 has
  to enumerate assignments rather than rotate one.
- ⚠️ **Which puts a caveat on P12's headline.** 30.7% against 19.3% was measured with **every
  greedy seat sitting downstream of a simple seat**. Nothing is wrong and nothing needs
  re-deriving — it is the honest answer to *"what happens at that table"* — but it is not a
  clean strategy-vs-strategy figure, and P16 owns separating the two.
- **Two design notes worth carrying into P16.** The mechanism variable is **already collected**:
  `takes` in the CSV is "how useful was what my upstream threw", so if the hypothesis is true
  that is the path the effect travels. And the packet's sharpest piece is the **control** — run
  the same design varying the *downstream* neighbour, which should move the focal win rate far
  less; without it the result is only "strong tables win more".
- **`docs/PLAYING.md` added** — a player-facing guide to solo play in the console. Listed in
  CLAUDE.md's documentation map.

**From P11:**

- **Five new files in `BurmesePoker.Console`, and every one of them is presentation.** `Options`
  (the command line), `Palette` (the colour and marker language), `RoundLog` (what survives the
  clear), `HandView` (a hand as its melds, and what each card costs to throw) and `PacedAgent`
  (a decorator that makes a bot wait). **Domain and Sim are byte-for-byte unchanged** — the
  packet needed nothing from either, which is the strongest evidence yet that §3.5's seams are
  the right ones.
- ⚠️ **The pause is a decorator in the console and must stay there.** `PacedAgent.Wrap(inner,
  pause)` sleeps once per `(Round, TurnNumber)` — the same pair `SpectrePlayerAgent` hands the
  keyboard over on, because a turn asks a varying number of questions and pausing per *call*
  would make some turns three times as long for no visible reason. **Putting the sleep in
  `GreedyBotAgent` would have put it inside P12's hot loop**; `LayeringTests` now exists partly
  to make that class of mistake fail loudly. `Wrap` returns the agent itself when the pause is
  zero, so `--pace 0` costs nothing.
- **The round log remembers markup, not events.** `ConsoleObserver.Say(markup)` prints a line
  *and* files it, so there is exactly one copy of every sentence the game says. Re-rendering
  from remembered events would have put a second copy of the wording next to the first.
  `SpectrePlayerAgent.BeginTurn` draws the log panel **first**, above the table and the hand,
  because it is the context the other two panels are read against. **The log is per round and
  nothing survives it** — the between-round history is `Program`'s own `List<RoundResult>`,
  since `MatchEngine` still deliberately keeps none (§3.8).
- **The hint is the computer's own answer, asked directly.** `SpectrePlayerAgent` holds a
  `GreedyBotAgent` and calls `ChooseAction` / `ClaimTurnedUpMoneyCard` / `ChooseDiscard` on the
  very `TurnContext` it was handed. ⚠️ **Do not re-derive a recommendation** — `CoverScore` is
  `internal` and the tie-break is not trivial, so a second implementation would be a different
  strategy wearing the first one's name. The per-card *cost* beside it is `HandView`'s own
  reading of `PartialCover` (`covered(13) − covered(12)`) and agrees with the advice by
  construction. `--no-hints` turns the lot off.
- ⚠️ **Spectre markup must balance, and the compiler will not tell you.** `Palette.Legend` was
  written with two opening `[grey]` tags and one `[/]`, built clean, passed 239 tests, and threw
  `InvalidOperationException: Unbalanced markup stack` on the first hand drawn. **Only playing
  it finds this.** Every constant that interpolates a colour needs looking at twice.
- ⚠️ **`Cover(...)` no longer takes a method group.** `CardFormatting.Meld` gained optional
  `MoneyCardRegistry` / ownership arguments (a declaration is public and shows plain cards; your
  own hand wants the markers), which broke `.Select(Meld)` — it now reads `.Select(meld =>
  Meld(meld))`. Harmless, but the error message blames a Spectre extension method and is
  thoroughly confusing.
- **Every match is seeded, whether or not one was asked for.** `Options` draws a seed when
  `--seed` is absent and `Program` prints it, so any match can be replayed after the fact. ⚠️
  **The seating comes from the match's own `Random`, not `Random.Shared`** — otherwise `--seed`
  would replay the same deals to a different table. **Verified:** two runs at `--seed 99` with
  identical keystrokes produce byte-identical output; `--seed 100` does not.
- **The difficulty prompt is a strategy name and nothing more.** *Hard* is `GreedyBotAgent`,
  *easy* is P12's `SimpleBotAgent`, and the prompt quotes the measured win rates (a third
  against a fifth). It is only asked when there is at least one bot.
- **What was actually played, since a UX packet is verified by playing it.** Through a pty
  (`script -qec "dotnet run --project BurmesePoker.Console --no-build -- --seed N --pace 0"`),
  fed newlines: **13+ rounds at four seats** on seed 4242 — settlements summing to zero,
  history truncating to "last 12 of 15", the log panel carrying three bot turns across the
  clear, hints reading *"(breaks a meld — costs 3)"* and *"← the computer would throw this"*;
  **seed 1 for the opening turn**, which is the only turn that offers the money-card claim, and
  the only way to see that advice line; **two six-seat runs**, where the reshuffle narration
  fires (it does not at four — P12's finding, confirmed from the other side). `--pace` measured
  by counting turns in a fixed window: ~163× fewer at 1,000 ms than at 0.
- **P11 shipped 4 tests (239 total), and they are the only mechanical check the packet allows.**
  `LayeringTests` asserts that Domain references no Spectre assembly, that Domain references no
  `System.Console` either, that Sim references no Spectre, and — guarding the other three —
  that the reference lists are not empty. **Still no Console tests, by construction.**
- **No new rules question. `RULES.md` is unchanged at rev 13.** Nothing in a UX pass is a rules
  decision; the two places one could have crept in — what the hint recommends, and what the
  claim prompt says a claimed card pays — were both already settled (§4.4, §4.5) and are quoted
  rather than re-decided.

**Still current, from P12:**

- **`BurmesePoker.Sim` is a fourth project and `BurmesePoker.Tests` now references it.** The
  rule to carry forward is **"tests never reference `BurmesePoker.Console`"**, which is what
  the Domain-only rule was always protecting. A fifth project existing only to test the fourth
  would have bought nothing, and the harness's determinism is an acceptance criterion.
- **The command line:** `dotnet run -c Release --project BurmesePoker.Sim -- --games 2000
  --strategies greedy,simple --seats 4 --csv out.csv`, plus `--rounds`, `--seed`, `--turn-cap`,
  `--serial`, `--threads`. There is also a **`bench`** verb that times the two searches over
  random hands. **Run it in Release** — Debug is roughly three times slower and says nothing
  useful about throughput.
- **The headline result: the tie-break is worth 1.6× the wins.** `SimpleBotAgent` is
  `GreedyBotAgent` with the discard tie-break removed and *nothing else* changed. Over **2,000
  four-seat rounds**: greedy **30.7%** of rounds and **+$1.24** a round, simple **19.3%** and
  **−$1.24**. P10's claim that the tie-break is what makes progress is now measured.
- ⚠️ **Seats are not equivalent, and a comparison has to rotate them.** Seat 0 opens every
  round and is the only seat ever offered the turned-up money card (RULES.md §4.5), because
  `RoundEngine.Play` starts turn 1 at `players[0]` every round. `SimulationOptions.Seating`
  puts strategy *(seat + game) mod k* in each seat. **A future harness that seats strategies
  any other way must still rotate them**, or it will be measuring the seat.
- **Determinism is per game, never per run.** `SeedSequence.GameSeed(master, index)` is
  SplitMix64's finaliser over the two packed into one word, so game 417 is the same game
  however the run was scheduled. Serial, parallel and two-thread runs give **byte-identical
  CSV**, which is the acceptance test the packet stands on. Never draw per-game seeds from one
  shared `Random`.
- **The turn cap lives in the agent, and has never fired in a real run.** `SeatRecorder` throws
  `RoundAbandonedException` past `SimulationOptions.TurnCap` (400 by default) and the game
  stops there and is *reported*, not dropped. Even a table of four `SimpleBotAgent`s — which
  has no tie-break to push it off a plateau — finished all 300 rounds tried, averaging 28.3
  turns. The only thing that has ever tripped the cap is `StallingAgent` in the test.
- ⚠️ **The reshuffle is a six-player phenomenon.** Over 300 bot rounds each: **0 reshuffles at
  four seats, 3 at five, 67 at six.** P10 recorded that no bot round had ever exhausted the
  draw pile; that was a four-seat observation. P9's rule is exercised by real play at a full
  table.
- **The money split, measured** — and it confirms `RULES.md` §4.3's `DERIVED` balance argument
  rather than upsetting it. At five seats the money cards moved **$8.43 a round** against the
  flat prize of $20 — **42%**, where the derivation guessed 40%. `RULES.md` is at **rev 13**
  for that one note; **no rule changed and no new question was raised.**
- **What P12 did *not* need from the domain: anything.** Win rate, the flat/side-bet split,
  turns, how close the losers were, take rate, claim rate and deck exhaustions are all derived
  by the harness from the three §3.8 seams. The only Domain additions are `Agents/CoverScore`
  (an extraction, shared so the two bots' scoring cannot drift) and `Agents/SimpleBotAgent`.
- **Speed, for the record:** `PartialCover.Best` **140 µs** a hand, `HandEvaluator.TryFindCover`
  **91 µs**, a round **~20 ms**, a run **51 rounds/s serial and 85–92 parallel** on a 4-core
  i7-1165G7. ⚠️ **The work is allocation-bound, not compute-bound** — the searches allocate an
  index, a memo and a list per call, and eight threads bought only 25% more throughput until
  `<ServerGarbageCollection>` went into the Sim csproj, which took it to 70%. If throughput
  ever matters, **attack allocation**, and still never inside the evaluator (§3.4).
- P12 shipped 10 tests (235 total). The suite went from 1.7 s to 4.3 s, almost all of it the
  16-game comparison the Sim tests share through a `Lazy<SimulationReport>`.

**Still current, from P10:**

- **`PartialCover.Best(hand)` → `{ Melds, Uncovered, CoveredCount, IsComplete }`** is the
  scored cover P5 deliberately did not build. It is `HandEvaluator`'s own search **plus one
  branch**: at the lowest card not yet settled it may take a meld covering it *or give the card
  up and move on*. Memoised on `(position, covered)`; a complete cover stops the search where
  it stands, so a winning hand is no dearer than it is in the evaluator. `IsComplete` and
  `HandEvaluator.IsWinning` are pinned as the same claim across 320 randomly dealt hands.
- ⚠️ **`Melds/MeldIndex` is an extraction, not an addition.** Both searches need the same
  candidate index — cards in `CardId` order, one bit each, candidates filed under the lowest
  card they consume — and keeping two copies of that would be two places for it to drift.
  `HandEvaluator` was rewritten over it in a separate step and the **208-test baseline
  re-run before anything else was touched**; it is the only proof its answers did not move.
  Do this again if either search is ever changed.
- **The whole strategy is one question asked three ways:** *of the thirteen I would be left
  holding, how many meld?* Take the discard iff it raises that count, claim the turned-up money
  card iff it raises that count, throw whichever card leaves it highest. **The take decision is
  deliberately asked of the fourteen rather than of the thirteen kept** — same answer, a
  fourteenth of the work, because any improvement must use the new card and every fourteen-card
  arrangement has a meld of four or more to give one back.
- **The tie-break is what actually makes progress**, since early on most discards score alike:
  prefer keeping cards with partners (another suit of the same rank, a neighbour in the same
  suit — with the ace a neighbour of both the two and the king, never through), and keep jokers
  over everything. **A tie on *taking* goes to the deck**, which is the only place money enters
  a decision at all: a blind draw confers ownership and a pickup does not (RULES.md §4.4).
- ⚠️ **Termination is a property, not a hope.** The score can never fall — throwing back the
  card just taken restores the hand exactly — so a bot's hand climbs monotonically to thirteen.
  Measured over twelve seeds and every table size: **every round terminated, 21–30 turns, ~40 ms
  a round, ~1.5 ms a turn**. No bot round has ever run the draw pile out, so **P9's reshuffle
  is unexercised by bots** — the tests that cover it are still `RoundEngineTests`'.
- **Two findings worth keeping for P12.** A freshly dealt hand covers **4 of 13 on average**
  (154 of 800 dealt hands cover nothing at all), and a bot reaches thirteen in seven or eight of
  its *own* turns — mostly by **taking discards**, because with two decks the card somebody
  throws away is very often somebody else's third of a rank. The take-the-discard rate is going
  to be the interesting statistic.
- **`RecordingAgent` (test project) is how a bot is tested at all.** `TurnContext` has an
  `internal` constructor, so a test cannot fabricate one — a strategy is only observable from
  inside a real round. The decorator wraps a bot, plays a scripted `DealBuilder` deal, and
  reads back what it was asked and what it answered. This is exactly BUILD-PLAN §3.8's item 2
  seam, and P12 should lift it rather than reinvent it.
- **The console asks two questions now** — *"how many at the table?"* then *"how many of you
  are people?"* (0 is allowed, and leaves the computer playing itself). Bots are named from a
  roster and marked: *Ruby (bot)*, *Sable (bot)*, … so narration reads as a table of players.
  **No Console tests, still by construction.** Verification was again a pty:
  `script -qec "dotnet run --project BurmesePoker.Console --no-build" /dev/null < keys` with a
  file of newlines — **20 full rounds** against three bots, every settlement summing to zero,
  bots declaring covers including joker substitutions.
- ⚠️ **A bot's turn is instantaneous, and that is a UX problem P11 inherits.** Three bot turns
  now flash past and are wiped by the human's screen clear, so the round-log panel P11 already
  wanted is the sorest thing in the console. **Do not put a sleep in `GreedyBotAgent`** — a
  domain type that waited would ruin P12.
- **No new rules question.** Everything the strategy needed was already settled: §4.4, §4.5,
  §5, §6, §7.1. `RULES.md` is unchanged at rev 12.
- P10 shipped 17 tests (225 total).

**Still current, from P9:**

- **`MatchEngine(players, agents, stakes, random, observer = null)`** holds the seating, the
  stakes, the banks and the one `Random`. **`PlayRound()`** shuffles a fresh shoe;
  **`PlayRound(drawOrder)`** is the scriptable twin, mirroring `RoundEngine`'s own pair. Both
  return a **`RoundRecord(RoundResult Result, TableState Table)`** — the §3.8 pair, which is
  also what the console's settlement report reads. `Banks` is a live view, everyone starts at
  zero, and `RoundsPlayed` counts. **Nothing is retained per round**; do not add a history to
  make a report easier (P11 remembers its own).
- ⚠️ **`RoundEngine`'s constructor now takes a `Random`, and it is required** — inserted after
  `drawOrder`, before `round` and `observer`. A round needs randomness for the reshuffle, and
  defaulting it would have made an exhausted round irreproducible in silence (BUILD-PLAN §3.7).
  Test call sites pass `new Random(seed)`; `RoundEngineTests.Engine(...)` takes an optional
  `seed` that only matters if the round actually reshuffles.
- **The reshuffle is in `RoundEngine.TakeCard`, at the moment of drawing** — `if
  (Table.DrawPile.IsEmpty)` → `TableState.ReplaceDrawPileWithTheDiscards(random)`, which sweeps
  **every** seat's pile, shuffles, and installs a new `Deck`. Safe to sweep all of them because
  the current player has already declined the offered discard by then. **The turned-up cards
  are not gathered** (RULES.md §9 #4's recommendation, taken). `DeckExhaustedException` now
  means what it says: nothing left anywhere, which is a real end state rather than a crash.
- **Ownership across the reshuffle is `CardOwnership.TryRecordFromDeck`** — keeps the first
  owner and returns `false` rather than throwing. The blind draw calls it; **the deal still
  calls the strict `RecordFromDeck`**, where a card leaving the deck twice really is a bug.
  RULES.md §5's "first acquisition wins" is now a method rather than a paragraph.
- ⚠️ **`TurnContext` gained `Round`, and it fixed a real concealment bug.** An agent lives for
  the whole match, so `SpectrePlayerAgent`'s "have I already begun this turn?" check — which
  compared `TurnNumber` alone — matched turn 1 of round 2 against turn 1 of round 1 and
  **skipped the screen clear at the start of every round after the first**, leaving the
  previous hand on screen. It now tracks `(Round, TurnNumber)`. The round number is public
  information, so nothing leaks; the P7 reflection test still passes.
- ⚠️ **A round nobody can win now runs for ever.** This is the biggest behavioural change in
  the packet and it deleted a test: `NobodyDeclaringRunsTheDrawPileOutAndSaysSo` used to assert
  the throw, and now hangs. Every round-level test needs a seat that declares —
  `RoundEngineTests.WaitingToDeclare(turns)` builds one that holds a winning hand and declines
  until its *n*th turn, which is how the long reshuffle rounds are driven (a 4-player round
  exhausts the pile on **turn 55**, having gathered exactly 54 cards).
- **`ReshuffleSeed = 2` is pinned deliberately.** Any seed reshuffles, but seed 2 is one where
  the 7♦ Bo threw away comes back to *somebody else*, which is the case the rule is about. If
  that test ever needs re-seeding, seeds 3, 4, 5, 7, 11… also work.
- **`RecordingObserver` grew** `Draws`, `Reshuffles`, `OwnersAtFirstReshuffle` and
  `DrawsAfterFirstReshuffle`, and `IGameObserver` gained **`DiscardsReshuffled(int cards)`** —
  which is also how P12 counts deck exhaustions (§3.8).
- **The console plays a match now**: `Program` loops `PlayRound()` → settlement report →
  standings → `AnsiConsole.Confirm("Another round?")`, and prints the rounds played on the way
  out. Standings is a small table of the running banks, title `Standings` with the round count
  as a caption (a `Title` long enough to exceed the table's width wraps ugly).
- **No Console tests, still by construction** — the test project references Domain only.
  Verification was again a throwaway harness under the scratchpad: it links
  `BurmesePoker.Console/*.cs` plus the test project's `Hands`, `DealBuilder` and
  `ScriptedPlayerAgent`, sets `<StartupObject>` to its own entry point so `Program.Main` does
  not clash, and **calls `Program`'s private `ReportSettlement` / `ReportStandings` by
  reflection** — same-assembly, so `BindingFlags.NonPublic` reaches them. Driven through a pty
  with `script -qec "dotnet run --no-build" /dev/null < keys`. **Two tricks worth keeping:**
  arrow-key escapes fed to a Spectre `SelectionPrompt` did not register, so rig the deal such
  that the card to discard **sorts first** (hearts sort first, jokers last) and plain Enter
  picks it; and give the winning hand no hearts so the drawn heart is that card. Two full
  rounds were played, banks reaching +$28 / −$4 / −$12 / −$12 — summing to zero.
- **Three rules defaults taken, all recorded in `RULES.md` rev 12 §9 and phrased neutrally in
  `QUESTIONS-FOR-MYA-LAY.md`:** **#4** the turned-up cards are not swept into the reshuffle;
  **#5** the money-card claim is offered every round, approved by nobody; **#14** (new) nothing
  moves between rounds — the seating the match was given is played all session.
- P9 shipped 16 tests and removed 1 (208 total).

**Still current, from P8:**

- **The console is a hotseat, and concealment is the screen clearing.** Every seat is a
  person at the same terminal, so `SpectrePlayerAgent.BeginTurn` clears the screen once per
  turn, names the player, and waits for *"are you at the keyboard?"* before drawing their
  hand. It fires on whichever of the four questions comes first, and tracks the turn by
  `TurnContext.TurnNumber` rather than by counting calls — **a turn asks a varying number of
  questions** (the opener is offered the money card, later turns the discard, only a winning
  hand the declaration).
- **`ConsoleObserver.PlayerDrew` deliberately does not print the card.** The domain narrates
  private information and says so; filtering is the front end's job (BUILD-PLAN §3.5). A
  pickup, a claim, a discard and a declaration are all public and are printed in full.
  A side effect of the per-turn clear: **public narration scrolls away**. The table panel
  reprints what still matters (turned-up cards, draw count, the takeable discard). If P9
  wants a running log it needs a panel that survives the clear, not more `WriteLine`s.
- **The star marks an owned *money* card, not any owned card.** Everything dealt is owned, so
  starring ownership alone marked all thirteen and said nothing. `CardFormatting.Of(card,
  registry, owned)` only stars when the multiplier is non-zero — a money card with no star
  came from a discard pile or off the table and pays somebody else.
- **The settlement breakdown lives in `Program`, not in the observer**, because settlement
  returns net deltas only and splitting them needs `TableState.Ownership` and
  `TableState.Shoe`, which are on the table rather than in the `RoundResult`. The round half
  is derived from the winner (flat, RULES.md §7.2) and the money-card half is the remainder,
  so **the two columns always add up to what the domain actually settled**. ⚠️ **This is the
  one thing P9 must re-plan around** — see BUILD-PLAN P9, amended.
- **Prompts are offered unconditionally and that is correct.** The engine only asks a question
  that has a legal answer, so neither the claim, the pickup nor the declaration needs a
  legality check on the console side; adding one would duplicate the rules outside the domain.
- **No tests were added — by construction.** The test project references Domain only, so
  nothing in `BurmesePoker.Console` is unit-testable, and P8 changed no Domain code: still
  **192 passed / 0 failed**. Verification was manual, and worth repeating the recipe:
  `script -qec "dotnet run --project BurmesePoker.Console" /dev/null < keys` gives Spectre a
  pty (piped stdin fails — `System.Console.ReadKey` throws when input is redirected, and
  `Program` refuses to start with a clear message rather than a stack trace). A second,
  throwaway harness under the scratchpad linked the test project's `DealBuilder`,
  `ScriptedPlayerAgent` and `Hands` sources, rigged a winning deal, and drove **a real
  `SpectrePlayerAgent` to a declaration** — that is how the declare prompt and the settlement
  table were seen working, arithmetic checked by hand (+16 / 0 / −8 / −8, summing to zero).
- **Spectre.Console is back at 0.57.2**, the current release, not the 2023 pin of 0.47.0.
  Only `BurmesePoker.Console` references it.
- **No new rules question.** Everything P8 needed was settled: §3 step 2 (seating is
  randomised in `Program`), §4.4/§4.5 (a claimed card is held but owned by nobody), §6.3
  (concealment), §7.2 (a flat round payment).

**Still current, from P7:**

- **`new RoundEngine(players, agents, stakes, drawOrder, round, observer)` deals in the
  constructor and `Play()` runs the turns**, so `engine.Table` is readable before the first
  turn and a setup test needs no play at all. `Play()` returns a `RoundResult` and refuses a
  second call. **`RoundEngine.Shuffled(..., Random)` is the real-game entry point.**
- **⚠️ A round is dealt from a `drawOrder`, not from a shuffle** — the 108 cards in the order
  they will leave the deck, validated as a permutation of the shoe. That is what makes a round
  scriptable: `BurmesePoker.Tests/Play/DealBuilder.cs` arranges the order so seat *s* gets
  positions *s*, *s+n*, *s+2n*…, then the turned-up top card, then the draw pile, with the
  turned-up bottom card last. **Its filler is never a money card**, so a settlement expectation
  can be worked out by hand; ask for a money card explicitly with `ThenDraw("7D")`.
- **The engine builds the shoe itself.** `Settlement.ForRound` needs the *unshuffled*
  index-aligned shoe, so `RoundEngine` calls `DeckBuilder.BuildTwoDecks()` at setup, keeps it
  as `TableState.Shoe`, and validates `drawOrder` against it. P6's warning that the caller must
  keep the builder list is now handled inside the engine — **callers pass nothing extra**.
- **`TurnContext` is the concealment rule expressed as a type**: own hand, available discard,
  draw-pile count, turned-up cards, stakes, registry, `TurnNumber`, `Taken`, `CanDeclare`,
  `YouOwn(card)`. **No `TableState`, no `PlayerState`, no `CardOwnership`** — exposing
  ownership would leak which money cards an opponent was dealt. A reflection test pins it.
- **The engine asks narrowly, so the front end needs no legality checks.** `ChooseAction` is
  asked only when a discard is available (so the opening turn just draws), `ClaimTurnedUpMoneyCard`
  only on turn 1, and `Declare` only when `HandEvaluator.TryFindCover` has already succeeded —
  the cover it found is what `RoundResult.Melds` carries, so it is never computed twice.
- **⚠️ Narrate *after* the card lands.** The conservation test caught a real ordering fault:
  raising `PlayerDrew` between `DrawFromTop` and `seat.Take` leaves an observer seeing 107
  cards. Every take now joins the hand before the event fires. Not a clone bug — but the same
  test that would have caught the 2023 clone bug caught this.
- **`IGameObserver` gained three events over BUILD-PLAN §3.5's sketch** — `PlayerTookDiscard`,
  `MoneyCardClaimed`, `PlayerDeclared` — because a pickup, a claim and a blind draw are three
  different things and only the draw confers ownership. **All methods are default no-ops.**
- **Ownership accounting is one invariant, and it is tested as one:** records == cards dealt +
  blind draws, ever. A claim records nothing, a pickup records nothing, a discard changes
  nothing. `OnlyTheDealAndABlindDrawEverConferOwnership` exercises all three routes in one
  round and asserts the count.
- ~~Deck exhaustion still propagates~~ — ✅ **done in P9**, inside `RoundEngine.TakeCard`
  exactly as P7 predicted it would have to be.
- **Seating is taken as given.** RULES.md §3 step 2 randomises it, but an engine that
  reshuffled its own seating could not be scripted, so P8's `Program.cs` randomises before
  constructing. The same list is what settlement is handed.
- **Two new rules questions, both defaulted, neither blocking** (`RULES.md` rev 11, and both
  phrased neutrally in `QUESTIONS-FOR-MYA-LAY.md`):
  **§9 #12** — when the opening player claims the turned-up money card, does that value still
  pay? **Default taken: yes**, designation is fixed at setup and does not move with the card;
  reversing it is one line in `TableState`'s constructor.
  **§9 #13** — may a player discard the very card they just took? **Default taken: yes**,
  nothing in §5 forbids it; it would be a single guard in `RoundEngine`.
- P7 shipped 18 tests (192 total).

**Still current, from P6:**

- **`Settlement.ForRound(players, winner, stakes, moneyCards, ownership, shoe)` →
  `IReadOnlyDictionary<PlayerId, int>`** of **net** deltas, positive to collect. Every player
  at the table appears, including zero deltas, and the deltas always sum to zero. It is a
  static class — settlement holds no state.
- **⚠️ The `shoe` parameter must be `DeckBuilder.BuildTwoDecks()` order, and this is checked.**
  Settlement resolves an owned `CardId` to a `Card` by *index*, and rejects any list where
  `shoe[i].Id.Value != i` — so **passing `Deck.Cards` throws**, because it is shuffled. That
  guard is deliberate: without it a shuffled shoe would settle the wrong cards in silence, and
  every arithmetic test would still pass. **P7 must keep the builder list it made the round's
  `Deck` from** and hand that over; `Deck` copies its cards, so the list is never disturbed.
- **Settlement is never given a hand, and a reflection test pins the parameter list** so it
  stays that way. This is RULES.md §4.4 encoded in a signature: the question is never who
  *holds* a card. The two headline tests — a money card its owner discarded, and one an
  opponent is holding — pass without any notion of a hand existing at all.
- **`Stakes` is a `sealed record`, not a struct.** A `readonly record struct` was the obvious
  reflex, but `default(Stakes)` would then be a silent $0/$0 game that still sums to zero and
  passes every property test. A class makes the omission a `NullReferenceException` instead.
  Both values must be **positive**; `Stakes.Standard` is $5 / $1.
- **The 7♦ pays *once* unless it is also turned up.** Permanent designation and turned-up
  designation are separate summands (P2), so an owned 7♦ in a round where the turned-up cards
  are something else pays `1 × MoneyCardValue`, not 2. A draft test asserted +8 for exactly
  that case and was wrong; the arithmetic is unchanged, the expectation was.
- **Guards, all `ArgumentException`:** winner not at the table, a player seated twice, an empty
  table, an ownership record naming somebody not at the table, an owned id outside the shoe,
  and the misaligned-shoe check above. **There is no no-winner settlement** — P7 rounds end on
  a declaration, and P9 reshuffles rather than ending one early.
- **Zero-sum is property-tested** over 500 randomised rounds (2–6 players, random stakes,
  random turned-up cards, up to 80 owned cards). Sample the deal with `Random.Shuffle` over a
  copy of the shoe — cards must be **distinct**, or `RecordFromDeck` rightly throws on the
  second owner of one physical card. This makes P9's match-level conservation test a test of
  *banking*, not of settlement; BUILD-PLAN P9 has been amended to say so.
- **P8 gets net deltas only** — no breakdown of round payment against side-bet, no per-card
  detail. If the console wants "−$5 for the round, +$3 in money cards" it computes the
  side-bet half itself from `ownership.Records` and `Multiplier`. BUILD-PLAN P8 amended.
- **No new rules question.** §4.3, §4.4 and §7.2 are all `PLAYER`/`EXPERT` Settled and the
  worked example is reproduced exactly; nothing needed a judgement call. `RULES.md` is
  untouched at rev 10.
- P6 shipped 26 tests (174 total).

**From P2:**

- **`MoneyCardRegistry(turnedUp).Multiplier(card)` → 0 / 1 / 2** is the whole designation API,
  and it is a pure function of the turned-up cards — no `Card` is written to, ever. The
  implementation is literally `(permanent ? 1 : 0) + (turnedUp ? 1 : 0)`, so **doubling is the
  overlap and the ceiling falls out with no clamp**: two copies of the 5♥ turned up still pay
  1, two copies of the 7♦ still pay 2.
- **The permanent designators (7♦, A♠) are two `Card`s carrying negative `CardId`s**, compared
  by `SameValueAs` like every other designator. They are values, never dealt; a negative id
  means a stray `==` against a real card can only be false.
- **The turned-up list is copied**, and any length is accepted — including empty, which the
  "permanent cards with nothing turned up" acceptance test needs. **No arity check.** If P7
  wants to insist there are exactly two, that is P7's rule to enforce at setup.
- **No joker branch was written**, per the P1 default: a turned-up red joker designates the two
  red jokers and neither black one, because `SameValueAs` discriminates jokers by colour.
  `ATurnedUpRedJokerDesignatesTheRedJokersAndNotTheBlackOnes` is the single test to change if
  `RULES.md` §9 #11 ever settles the other way.
- **`PlayerId` was brought forward from `Play/`** — `CardOwnership` needs it and P7 is a long
  way off. It is a `readonly record struct PlayerId(int Value)`. **P7 must not redefine it**;
  BUILD-PLAN P7's build list has been amended.
- **`RecordFromDeck` re-recording the *same* owner is a no-op; a *different* owner throws
  `InvalidOperationException`.** The packet allowed either "rejected or a no-op" — this splits
  it, because a genuine repeat is harmless while two owners for one physical card can only be
  a dealing bug worth surfacing. There is deliberately no transfer, clear or removal, and a
  reflection test asserts the public surface stays that way.
- **⚠️ P6 needs a card lookup, and this is the one thing that re-planned.** `Records` is keyed
  by **`CardId`** but `Multiplier` takes a **`Card`**, because designation is by value and
  ownership is by instance — the two identity notions meeting at exactly the seam BUILD-PLAN
  §3.1 predicted. So `Settlement.ForRound` must be given the shoe as well.
  **`DeckBuilder.BuildTwoDecks()` is index-aligned** (`CardId.Value` == list index), so the
  lookup is an array index — but **`Deck.Cards` is shuffled and is not**. Do *not* widen
  `RecordFromDeck` to take a whole `Card`; BUILD-PLAN §3.3 fixes its signature and ownership
  is about the physical card. BUILD-PLAN P6 has been amended.
- **`CardOwnership.Records` is a live read-only view** over the internal dictionary, in the
  same spirit as `Deck.Cards`. Snapshot it if you need one.
- **No new rules question.** The packet needed no judgement beyond the §9 #11 default P1 had
  already recorded and `QUESTIONS-FOR-MYA-LAY.md` already asks.
- P2 shipped 29 tests (148 total).

**Still current, from P5:**

- **`MeldCandidates.For(hand)` → `IReadOnlyList<Meld>`**: runs first, then the sets whose card
  set no run already covers. `HandEvaluator.IsWinning(hand)` and
  `HandEvaluator.TryFindCover(hand, out var cover)` are the win authority — **the only one**.
  Nothing else in the codebase may decide a hand has gone out.
- **`TryFindCover` returns *a* cover, never a canonical one.** Thirteen hearts in sequence come
  back as `3+3+3+4`, not as one thirteen-card run, because the search takes the first candidate
  containing the lowest uncovered card. `IsWinning` is what is settled; the shape of the cover
  is not. **P8 must not assume the tidy grouping** — BUILD-PLAN P8 has been amended.
- **A "wins only if a joker plays a card the hand holds" test cannot be built out of a run.**
  A joker can nearly always play *outward* from a run onto a rank the hand does not hold —
  below the bottom card or above the top — so a rival cover always exists in which the joker
  merely fills a gap, and a generator that never substituted for a held card would find it too.
  Blocking one end only moves the boundary a rank along, and the ace is the sole natural stop.
  **A set has no escape**: it is capped at four suits, so five fives (two decks) plus a joker
  can only cover as two three-card sets, and whichever suit the joker plays is one the hand
  holds. That is `AHandWinsOnlyByPlayingAJokerAsACardItAlsoHolds`. The run flavour is tested
  one level down, on the candidate `2♦ 3♦ [🃏] 5♦ 6♦` with the real 4♦ melded elsewhere.
- **Jokers make almost any hand winnable — mind this when writing negative tests.** With two
  spare jokers any orphan finds a set, so a hand that must evaluate `false` has to be
  joker-free or joker-poor. Two drafted "not winning" hands turned out to be winning that way.
- **Performance is a non-issue.** Three thirteen-card stress hands — the 4,032-candidate one,
  a two-deck hand holding every diamond twice, and a losing hand that forces full exhaustion —
  take about **100 ms** in total. The pinning does that work; the dead-end memo changed nothing
  measurable and is kept only as a bound.
- **No partial-cover search exists**, deliberately. `TryFindCover` is all-or-nothing. A bot's
  "largest cover found" and a player's "best so far" hint need a *scored* version of the same
  backtracking, and BUILD-PLAN P10 now says so.
- **No purity requirement is implemented** — `RULES.md` §7.1 leaves "is a pure sequence
  required" open and recommends treating it as not-a-rule. If it ever settles the other way it
  is a filter on the cover, not a change to the search.
- **Guards:** the same card instance twice throws `ArgumentException`; a hand over
  `HandEvaluator.MaximumHandSize` (64, one bit per card) throws; an empty hand is covered by no
  melds at all and returns `true`.
- **No new rules question.** P5 shipped 19 tests (119 total).

**Still current, from P4:**

- **`SetGenerator.Candidates(hand)` → `IReadOnlyList<Meld>`**, mirroring `RunGenerator` in
  every respect: eager, de-duplicated by card set, jokers taken in ascending index order.
  It walks the **four suits once per rank**, taking each suit as a held card, a joker, or
  nothing — a three-card set is a four-suit set with one suit left unfilled. That single
  formulation gives the ≥3-distinct-suits rule, the four-card maximum and joker substitution
  at once, with no subset enumeration anywhere.
- **⚠️ The two generators can emit the same card set — P5 must de-duplicate across them.**
  It happens for any meld holding **at most one real card**: `{9♦,🃏,🃏}` is a run (jokers as
  10♦ and J♦) *and* a set (jokers as 9♠ and 9♥), and `{🃏,🃏,🃏}` is both trivially. Not
  wrong — identity is the card set — but it makes the cover search try the same cover twice
  and makes `TryFindCover` report a kind arbitrarily. **`MeldCandidates.For` should
  de-duplicate with the usual set comparer and keep the run interpretation**, which is
  generated first. BUILD-PLAN P5 has been amended to say so.
- **Sets cannot explode the way runs can.** Measured worst case **639** candidates — nine
  cards of one rank split (3,2,2,2) across the suits plus all four jokers — against P3's
  4,032. The closed form is in the test: every choice of *k* real cards in distinct suits
  plus *j* jokers with 3 ≤ k+j ≤ 4. The §7 risk row now says the explosion risk is P3's alone.
- **The brute-force cross-check paid off again**, and was trivial for sets: a subset is a set
  iff it holds 3–4 cards whose real members share a rank and occupy distinct suits. Nothing
  had to be said about jokers at all — with at most four cards there is always a free suit.
- **All-joker sets are emitted**, matching P3 and `RULES.md` §9 #8's *unlimited jokers*
  recommendation. `AHandOfNothingButJokersStillMakesSets` and its `RunGenerator` twin are the
  pair to change together if that ever settles the other way.
- **No new rules question.** §6.2 was already `EXPERT`-confirmed and settled; the packet
  needed no judgement call beyond the all-joker default P3 had already taken.
- P4 shipped 18 tests (100 total).

**Still current, from P3:**

- **`RunGenerator.Candidates(hand)` returns `IReadOnlyList<Meld>`**, not the bare
  `IEnumerable<Meld>` of BUILD-PLAN §3.4 — generation is eager anyway, because
  de-duplication has to see every candidate. Deliberate; don't "fix" it to lazy.
- **`Meld` and `MeldSlot` are shared with P4 and P5.** A slot is
  `(Card Card, Rank PlaysAs, Suit InSuit)`: for a real card its own rank and suit, for a
  joker what it stands in for. A meld's **identity is `CardIds`** — never its display value.
  `Meld` validates only ≥ 3 cards and no card used twice; run/set legality is the
  generator's. `Meld.Overlaps` exists for P5.
- **Jokers are taken in ascending index order** inside the fill recursion. That is not a
  detail — it is what makes the search enumerate joker *combinations* once each instead of
  every permutation, and P4 needs the same trick.
- **De-duplicate with `new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer())`.**
  The BCL comparer does structural set equality; no custom comparer is needed.
- **Two numbers in the docs were wrong and are now measured.** All thirteen ranks of one suit
  gives **76** candidates, not 77 — `A-2-…-K` and `2-…-K-A` are the same thirteen cards. The
  joker-heavy worst case gives **4,032**, not "hundreds"; the spec and the §7 risk table both
  now say thousands. Neither is a bug: a brute-force enumeration of every subset agrees with
  the generator card set for card set.
- **A brute-force cross-check earns its keep.** `GeneratorFindsExactlyTheCardSetsThatCanFormARun`
  enumerates all 2ⁿ subsets of a hand and asks, backwards, which could fill some window. It
  is what proved the 4,032 correct rather than merely bounded. **Write the equivalent for P4.**
- **Assumption taken: all-joker melds are legal**, so `🃏🃏🃏` is a run candidate. This follows
  `RULES.md` §9 #8's *unlimited jokers* recommendation, which rev 10 widened to name the case;
  requiring one real card per meld would be a stricter rule that §6.1 nowhere states. A hand
  can hold at most four jokers, so the blast radius is five extra candidates.
  `QUESTIONS-FOR-MYA-LAY.md` asks it as a table situation. **P4 should match; P5 must not
  assume a meld contains a ranked card.**
- **Test hands come from `BurmesePoker.Tests/Hands.cs`** — `Hands.Of("2D", "3D", "RJ")`, with
  `CardId`s assigned in the order listed so duplicate copies stay distinguishable. `RJ`/`BJ`
  are the jokers. This is where `CardText.ParseRank` finally earns its keep.
- P3 shipped 22 tests (82 total).

**Still current, from P1:**

- **Build cards with `Card.Ranked(id, rank, suit)` and `Card.Joker(id, color)`**, not the
  positional constructor. They derive `Color` from `Suit`, so a card cannot be given a colour
  that contradicts its suit. The positional constructor stays public because §3.1 specifies
  it; the factories are the ergonomic path.
- **`Deck.TwoDecks()`** builds the 108-card shoe in one call. `Deck.Cards` is a **live**
  read-only view, top first — `.ToList()` it before drawing or shuffling if you need a
  snapshot. `Deck` copies the cards it is constructed with.
- **Index 0 is the top of the deck.** `DrawFromTop` takes index 0; `DrawFromBottom` takes the
  last. Both matter at setup: `RULES.md` §3 step 4 turns up one money card from the **bottom**
  and one from the **top**.
- **`DeckExhaustedException`** (in `Domain/Cards/`) derives from `Exception` directly, not
  `InvalidOperationException`, so an empty draw pile is distinguishable from a bug. **P7 must
  reuse it rather than inventing another** — BUILD-PLAN P7 has been amended to say so. P9 is
  where it actually gets caught and turned into the discard-pile reshuffle (`RULES.md` §5).
- **Shuffling is `Random.Shuffle(Span<T>)`** over `CollectionsMarshal.AsSpan`, not
  `OrderBy(r.Next())`. Don't "simplify" it back — the old form is not a uniform permutation.
- **New rules question, `RULES.md` §9 #11 — a turned-up joker.** `SameValueAs` compares
  colour as well as rank and suit, which is a no-op for ranked cards but discriminates
  jokers by colour. Nothing says what a joker turned up as one of the two money cards
  designates, and it will happen about one round in fourteen. **Safe default for P2: no
  special case — designate by `SameValueAs` like any other card**, so a turned-up red joker
  designates the two red jokers and neither black one. Phrased neutrally in
  `QUESTIONS-FOR-MYA-LAY.md`. **Do not block on it.**
- P1 shipped 32 tests (60 total). No surprises in the packet itself — it was mechanical.

**Still current, from P0:**

- **Run the app with `dotnet run --project BurmesePoker.Console`**. `Program.cs` is a
  one-line placeholder; P8 rebuilds the front end.
- `MoneyCardStatus` and `PlayerAction` were deliberately **not** ported — superseded by
  `MoneyCardRegistry.Multiplier` (P2) and `TurnAction` (P7). `CardPlayType` became
  `Melds/MeldKind`.
- `UserPromptFactory` was deleted with the rest of `Logic/`. **P8 should read it from the
  `pre-rewrite` tag:** `git show pre-rewrite:BurmesePoker/Logic/Factories/UserPromptFactory.cs`.
- `CardText.ParseRank` is still uncalled by the domain. It is expected to earn its keep
  building hands in P3/P4 test fixtures.
- **All three projects target `net10.0`** and the solution file is **`BurmesePoker.slnx`**.
  Both were chosen deliberately: Nick's standing preference is the newest supported .NET
  tooling. Don't "fix" either back. Tests are **xunit v3 4.0.0** on
  **Microsoft.Testing.Platform**; `Microsoft.NET.Test.Sdk` and `coverlet.collector` are gone,
  and so are `xunit.runner.visualstudio` and the coverage collector — nothing needs them.
  The Console project has **no `Spectre.Console` reference**; **P8 adds it back** at the
  current version.
- **Two MTP consequences a cold session will trip over:**
  1. `global.json` opts `dotnet test` into MTP mode. **Do not delete it** — without it
     `dotnet test` fails outright.
  2. **Filtering is `--filter-method "*Name*"` / `--filter-class "*Name*"`.** VSTest's
     `--filter "FullyQualifiedName~Name"` is rejected.
- The test project is `<OutputType>Exe</OutputType>` — expected for xunit v3, not a mistake.
- **`BUILD-PLAN` §5 P3's "Done when" said 8 candidates** while the packet body said 5. It was
  amended to **5** in P0, along with the same stale 8 in the §8 risk table.
  `docs/spec/RUN-CANDIDATES.md` was already correct.

---

## Decisions needed from Nick

✅ **Nothing blocks a packet. Every rules question that ever blocked one is closed** — twenty-three
of them across four sessions with **Mya Lay and Aung Aung** on 2026-08-20/21, including all six of
§5.1's specification (§9 #16–#19, #25, #27), both of the win condition's (#22, #29) and the whole
money layer (#4, #5, #10–#14, #24, #26, #30, #31).

⚠️ **Four calls are Nick's rather than a rules matter, and none of them stops P28 starting.**

1. 🔥 **P24's position.** It was sequenced ahead of the §5.1 work deliberately, on the argument
   that §5.1 was blocked and P24 would make that conversation productive. **The conversation
   happened**, so the argument is spent, and P24 explains *why the computer chose this card* while
   **P25–P27 changed what a good card is at three of four table sizes, what a card is worth, and
   which cards are legal to throw at all — all three have now shipped.** ⚠️ **`BUILD-PLAN.md` §4 records P24 after P29 as a
   recommendation, not a decision** — P24's scope was set by Nick on 2026-08-20 and moving it is
   his call.
2. ⚠️ **When to re-measure.** P29 regenerates `docs/strategy/measurements.csv` under the corrected
   rules, and `sim suite` was **five hours** at P23. ✅ **P25's share of the bill is now measured
   and it is small** — four-handed greedy-vs-simple went from 102 to 86 rounds/s at one seed,
   about 16%, roughly half of it the round being longer rather than the evaluator being slower.
   **Everything in `docs/STRATEGY.md` is wrong until it runs**, and it can only run once the other
   three are in.
3. 🔥 **Whether a table smaller than four is a goal at all.** P25 implemented the two- and
   three-handed win conditions because §7.1.1 states them; `RoundEngine.MinimumPlayers` is still
   **4** (`RULES.md` §10 #7), so they are correct, tested and **never executed**. Making a smaller
   table playable touches the deal, the money layer and both front ends — **a packet of its own,
   and nobody has asked for one.** It is fine to leave it exactly as it is; it is not fine to
   leave it undecided and unrecorded.

4. 🔥 **Whether the browser should ever show a ×5, and where.** P26 leaves the jackpot
   **settled but never drawn**: `CardView.Multiplier` is 0, 1 or 3 by construction, because a
   marker beside one card cannot depend on the rest of the round's ownership, so a player who owns
   both partners of a 7♦/A♠ turn-up sees `($$$)` and is paid ×5. **It is the largest single swing
   in the game and the only one the table cannot see coming.** Showing it means a per-hand fact
   rather than a per-card one — plausibly P24's territory, plausibly a UX packet of its own, and
   **no packet owns it today.**

✅ **One rules question is open and it blocks nothing** — `RULES.md` §9 **#32**: does the ×5 need
the 7♦/A♠ pair specifically, or would any two tripled values do (a 7♦ and a joker, two jokers of
opposite colours)? **The combination exists only because rev 21 made jokers permanent.** ✅ **P26
wrote the narrow rule and pinned it with two tests** — `TwoTripledJokersInOneHandAreNotAJackpot`
and `ASevenAndAJokerTurnedUpAreNotAJackpotEither` — so widening it later has to break a test.
`QUESTIONS-FOR-MYA-LAY.md` **Q9** has it phrased as a table situation.

⚠️ **Three rulings in the rules are `PLAYER` house rulings rather than recall, and stay flagged**:
that a §5.1 release survives the reshuffle (*"nobody really knows"*), that taking a joker closes
the other jokers (*"I'd assume"*), and that doubling is the ceiling — **superseded anyway by rev
20's triple.** A house ruling that later turns out wrong is a **rule change**, not a typo fix.

**Do not block on them.**

---

## Session log

| Date | Packet | Outcome |
|---|---|---|
| 2026-08-21 | P27 | **Done — §5.1 is code, and a legal turn changed for the first time since P0.** `Domain/Play/FeedingBan.cs` is the whole rule: two `HashSet<Rank?>` a seat — taken-in-the-open and released — and one method, `LegalDiscards(hand, rules)`, carrying both exceptions and the floor. `PlayerState.MayNotBeFed` is the seat's own record, `TableState.SeatFedBy` is the one seat that reads it, and **`TurnContext.LegalDiscards` is the whole of the choice a turn presents** — never empty by construction, and on an ordinary turn the hand itself, same reference, no allocation. Build clean, **642 passed / 0 failed** in 9 m 42 s (590 before; baseline verified at `HEAD` in a worktree, 590/0 in 8 m 57 s). 🔥 **The filter became a property of the ladder rather than a line each rung remembers**: `CoverScore.Discard` and `CoverScore.Ranking` take a `TurnContext` instead of a hand, so every rung that ranks is filtered by construction **and so is the runner-up a difficulty level throws as its mistake** — the thing §5.1 warned was easiest to get wrong. Only `RandomBotAgent` needed a line of its own. `LegalDiscardsTests` runs over **all** of `BotCatalog` and `DifficultyLadder`, so a rung added later cannot forget. 🔥 **The rank-only predicate is `Card.SameRankAs`, and it is literally `Rank == other.Rank`** — nullable equality is what makes a joker close the other jokers, so §9 #27's `PLAYER` house ruling falls out of the type rather than being written as a case; ⚠️ **P28 must read it for the claim's objection (§9 #30), not write a second one.** ⚠️ **The floor is one line; exception 2 is not** — a `HandEvaluator.IsWinning` per banned card, gated on the hand being fourteen *and* on `PartialCover.CoversAtLeast(hand, 13)`, so it is asked only of a hand that could actually go out. 🔥 **The finding that outlives the packet: a bot's cover count can now fall.** `GreedyBotAgent`'s stated reason a table of bots terminates is that the score can never fall, *because throwing back the card just taken restores the hand* — §5.1 removes that card from the choice, so convergence is no longer guaranteed and `TurnCap` / `RoundTimeLimit` are what stand behind it. **P29 should publish round lengths and abandoned-round counts.** ⚠️ **It already broke two shipped tests, both fixtures rather than code**: a passive seat throwing back the 3♠ it drew became illegal one turn after Alice claimed the 3♣, and `SkillLadderRunTests` asserted an ordering over **eight** games in which `simple` — which wins 29.0% over two hundred — drew a blank. 🔥 **Measured, and the surprise is how cheap it is**: same seed, greedy vs simple, four seats, 200 games — **26.6 → 27.1 turns a round, 69 → 69 rounds/s, 31.3% → 31.5%**. ⚠️ **The reason is that a seat takes in the open on only about a fifth of its turns**, so a rank is closed for far less of a round than the rule's prominence suggests — **expect more at a table of stronger rungs.** ⚠️ **"Domain only" was false again, and in a new way**: a closed card is `CardDisplayState.Unthrowable` with the token `closed`, a legend entry and an accessible name, the console drops it from its `SelectionPrompt` and the browser draws it as a `<span>` rather than a disabled button — **a control that does nothing is exactly the failure P13.4 spent a packet chasing** — so **a `drive-console.py` capture from before P27 no longer compares**, for the second packet running. ✅ **The server refuses a banned answer where it already refuses a card you do not hold** (`SeatPrompt.MayThrow` in `SeatAnswer.Discard.Fits`), so a bad client does not take a hosted table down; the engine's guard stays behind it. **No rules question arose**; `RULES.md` is rev 23 — §5.1's *"none of this is implemented"* replaced and §10 #13 discharged, both status rather than a rule moving. |
| 2026-08-21 | P26 | **Done — the money layer is what `RULES.md` §4 actually says.** `MoneyCardRegistry.Permanent` holds **three values and eight cards** (the jokers joined), `Multiplier(Card)` returns 0/1/**3**, and the ×5 is `Multiplier(card, owner, MoneyOwnership)` under a configuration `Settlement` reads from `CardOwnership` **once a round**. ✅ **Design decision 2 held**: nothing is stored on a card, and `Multiplier(Card)` survives as the value-only question — which is what every view drawing one card at a time is really asking, and why no caller outside `Settlement` needed a parameter. Build clean, **590 passed / 0 failed** (574 before; 16 new). Baseline verified at `HEAD` **in a `git worktree`**, 574/0. 🔥 **The packet's own stated prediction was the deliverable and it came out right**: under the ×3 a designation on the 7♦ and one on an ordinary card leave **exactly the same** money loose in the shoe, so §4.1's conservation arithmetic is sound and the rule did not need re-asking; `ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe`'s last assertion is an equality now. ⚠️ **`STATUS.md` had predicted that test would be *red* before the packet started and it was green** — the code still implemented the withdrawn rule, so it agreed with the code and disagreed only with the document. 🔥 **Seven shipped tests went red and all seven for one reason: jokers are dealt.** Four of 108 cards, so an arbitrary four-handed deal hands out one or two, and five tests carried the comment *"nobody owns a money card"*. **The blast radius of rev 21 is every golden settlement number in the tree, not the money layer.** 🔥 **Measured: the side-bet moved `$8.50 ± 0.26` → `$11.58 ± 0.34` a round at five seats, 42.5% → 58% of the round prize**, with the *before* run reproducing P12's rev-13 figures at a different seed so the instrument is validated rather than asserted; `RULES.md` §4.3 and §4.4's stale `DERIVED` notes are re-derived (rev 22) and §10 #17 is discharged. ✅ **Play did not move** — same seed, same wins, turns and cover: §4.4's decoupling claim measured *across* a money-layer change for the first time. ⚠️ **"Domain only" was true of the logic and false of the text**: `($$)` → `($$$)`, `PaysDouble` → `PaysTriple`, four user-facing strings, **so a `drive-console.py` capture from before P26 no longer compares**. ⚠️ **No view can show a ×5** and no packet owns that gap; **`MoneyOdds` does not price it** either, and says so. ⚠️ **§9 #32 stays open and is fenced by two tests**, not a comment. |
| 2026-08-21 | P25 | **Done — the win condition is a function of the table size, and the engine finally asks the right question.** `Domain/Melds/TableRules.cs` is `RULES.md` §7.1.1 as data and the only place it is written down; `HandEvaluator.IsWinning(hand, rules)` and `TryFindCover(hand, rules, out melds)` are the whole surface and **the parameterless overloads are gone**, so no caller can ask the five-handed question by accident. `TableState.Rules` and `TurnContext.Rules` are where the engine and a seat read it. **574 passed / 0 failed**, up 26. 🔥 **The interesting part was the memo, not the requirement**: the search state is `(covered, seriesStillOwed, cleanStillOwed)`, because a covered-set from which no completion can supply *two* more clean series may perfectly well supply one, and the old key would have poisoned the second question with the first question's dead ends. The counts are carried **along** the partition and clamped at nought — a clean series discharges both, an impure one the series count alone (§9 #28, #29) — with one prune, that fewer than three uncovered cards per series still owed cannot pay for them however they fall. ✅ **Two-handed was the cheap case**: sets are illegal *as melds*, a property of a meld rather than of the partition, so `MeldIndex.Build` takes `setsAllowed` and the search never sees one — ⚠️ **and filtering on `Meld.Kind` keeps more than it looks like it does**, since `MeldCandidates` already emits `{9♦,🃏,🃏}` and `{🃏,🃏,🃏}` once, as the **run** interpretation. ✅ **`Meld.IsClean` needed no special case for the all-joker meld** — `Kind == Run && JokerCount == 0`, and three jokers fail it because every slot is a substitute, so §6.1 and §9 #29 agreed without either being bent. 🔥 **The change is real and `drive-console.py` cannot see it.** Four-handed greedy-vs-simple over 200 games at one seed goes from **25.1 to 26.6 turns a round** and **102 to 86 rounds/s** — strictly harder, longer rounds, as predicted — and yet **both console captures are byte-identical to `HEAD`** (8,417 and 90,251 bytes). ⚠️ **Not because nothing changed: both scripts quit in round 2 and neither capture contains a declaration at all.** The instrument that proved P21 and P23 were refactors is blind to the win condition by construction — **do not read a clean `cmp` as evidence about play.** ⚠️ **`PartialCover` was left alone on purpose and now measures something else**: `IsComplete` agrees with `IsWinning` only at five or more, so **every rung in `BotCatalog` is maximising cover count at a table where cover count is no longer the win condition** — P29's prediction 2, to be priced rather than fixed. ⚠️ **Left behind and owned by nobody: `RoundEngine.MinimumPlayers` is still 4** (§10 #7), so `TableRules.For(2)` and `For(3)` are correct, tested and unreachable from a dealt game. **No rules question arose and `RULES.md` stays at rev 21** — §10 #14 gained a ✅ discharge note, which is a status annotation and not a rule moving. |
| 2026-08-21 | P25–P29 | **Planned, not started — and the plan has a second half again.** 🔥 **`RULES.md` is rev 21, and the last two answers changed §4.1 twice over.** Asked how much a turned-up joker's partner pays, the answer went behind the question: **"7 of diamonds, ace of spades, AND jokers are always money cards"** — so the **permanent side-bet doubles, 4 cards to 8**, two `DERIVED` arguments built on the old count are stale (§4.3's measured *42% of the round prize*, §4.4's *~4 of 6 owned at the deal*), and rev 20's ×3 list stops looking arbitrary: **it was never a list of special cards, it is the list of permanent money cards.** 🔥 **And a rule nobody asked for — a jackpot**: if the two turned-up cards are the 7♦ and the A♠ and one player owns **both** partners, they pay **×5 each** rather than ×3, which at standard stakes is **$40 a head against a $5 round prize**. ⚠️ **It is the first rule in this game where a card's value depends on who holds what**, so a multiplier is a property of *(value, ownership)* and `MoneyCardRegistry.Multiplier(Card)` cannot answer alone. ✅ **The headline design decision survives** — money status is still *computed, never stored*; the inputs widen, the principle does not. ✅ **§9 #30 closed: an objection turns on the *rank*, which is §5.1's own predicate**, so the claim's test and the ban's test are one predicate and must not be written twice. ✅ **#31 closed by its premise turning out to be wrong** — asking it flat, without mentioning the 7♦, produced a correction instead of a confirmation. ⚠️ **One question left, §9 #32**: whether the ×5 needs the 7♦/A♠ pair specifically or any two tripled values — a combination that exists **only because jokers became permanent**. 🔥 **The pattern across four revisions is worth naming: every one of them answered past the question asked, and three changed a rule nobody was asking about** — rev 19 defined *a game*, rev 20 supplied the ownership framing and superseded a one-day-old `EXPERT` ruling, rev 21 made jokers permanent and produced the jackpot. **This game's rules are recalled as wholes, not as answers**, and asking narrowly has lost material three times. **Then the plan: P25 the win condition by table size, P26 the money layer as it actually is, P27 the feeding ban, P28 the claim's permission and per-round seating, P29 re-measure** — written into `BUILD-PLAN.md` §5 with a new dependency graph in §4. ⚠️ **P24 re-sequenced after P29 and flagged as a recommendation rather than a decision**: its stated reason for going first (*§5.1 is blocked and P24 makes that conversation productive*) is spent, and shipping an explanation of decisions that P25–P27 are about to change means writing the sentence twice and believing it once. 🔥 **P29 carries three predictions written down before the run**, so it can be wrong: the difficulty dial survives, `outs` **loses** margin at four seats (its objective is cover count, which is no longer sufficient for a win), and `prospector` separates at lower stakes than $5/$40. Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md`, `BUILD-PLAN.md`, `CLAUDE.md`, this file. **No code touched.** |
| 2026-08-21 | — | 🔥 **The money layer's last two questions closed — `RULES.md` is rev 20 — and both answers reached past the question.** **(1) Claiming the turned-up money card requires the permission of the player who goes *before* you in turn order**, who may object **only if they hold that card**: your take is public, so it arms §5.1 against exactly that seat and locks them into holding their copy all round. `EXPERT`, closing §9 #5, which the 2023 source carried as a `TODO` nobody could explain. 🔥 **It is the first rule tying the money layer to the feeding ban, and it independently confirms two §5.1 rulings that were only recommendations when they were made** — #16 (the ban binds the seat above you, not the table) and #17 (only a public take arms it, the §4.5 claim included). **A permission rule naming the upstream seat cannot be stated in a game where either is false**, which is the strongest structural evidence yet that §5.1 is an old rule rather than a one-off table ruling. ⚠️ **Two consequences nobody had noticed**: the claim is an **attack** — it pays nothing (§4.4) and buys a lock on the hand of the player who discards to you all round, which `prospector` models not at all — and an **objection is a disclosure**, the first thing in this concealed game a player reveals by choice. **(2) A turned-up joker designates the other joker of its own colour** — *"colour matters for jokers"* — closing §9 #11 and confirming that `SameValueAs` already computes it. ⚠️ **And an answer nobody asked for superseded an `EXPERT` ruling from the day before**: a shown 7♦, A♠ or joker **can never be owned, claimed or not, and its partner copy pays ×3**, not the *double* rev 19 recorded. **Both `EXPERT`, one day apart, not in agreement — the later and more specific is the rule and the earlier is struck rather than deleted.** 🔥 **The second time an answer given before the framing that governs it turned out not to be an answer** (§7.1's purity ruling failed identically); the framing here is **ownership**. ✅ **The arithmetic backs the later answer**: ×3 is 1 for the partner's own permanence, 1 for the designation and 1 inherited from the copy that can no longer be paid for, which makes a designation on a permanent money card worth exactly what an ordinary designation is worth — where ×2 would have made it worth *less*. ❌ **First rules change to invalidate a published measurement.** `MoneyCardRegistry.Multiplier` caps at 2 and must return 3; `MoneyOdds` prices blind draws from it; `prospector` is the one rung reading the money; **`STRATEGY.md` §10's money sweep was measured under the struck rule**; and P22's `DERIVED` note is **withdrawn** — the reasoning was sound and the premise moved. ✅ **`ProspectorBotAgentTests.WhatABlindDrawIsWorthIsWhatIsStillLooseInTheShoe` asserts the withdrawn direction and will go red — the derivation was written as an assertion rather than as prose, so this cannot pass silently.** ⚠️ **Two new §9 questions, both raised by these answers**: #30 (does an objection turn on the exact card or the rank — §5.1 is rank-only) and #31 (is a joker's partner really ×3, when the same multiplier conserves money on a 7♦ and creates it on a joker). Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md` (**new Q8**), `BUILD-PLAN.md`, `CLAUDE.md`, this file. **No code touched; the tree is still green, and it is green against a rule this document no longer holds.** |
| 2026-08-21 | — | ✅ **The money layer is confirmed by a person for the first time — `RULES.md` is rev 19, and §9 is down to two questions.** Nine put to **Mya Lay and Aung Aung**, nine answered, and **eight confirmed a standing default**: **7♦ and A♠ are money cards in both copies out of both decks** and a turned-up 7♦ makes it a **double, *"worth double"*** (§4.1, `CODE` `Probable` → `EXPERT`, and it promotes §4.3's `DERIVED` guess that a double counts as two); **a claimed money card's value goes on paying** — *"all 9s of hearts become money cards"*, confirming P7; **the turned-up cards stay out of the reshuffle**, confirming P9; **you may throw back the card you just took**; **a turned-up joker designates jokers**; and **7♦ and A♠ are what they are by "tradition"** — a question recorded as *likely unrecoverable* since rev 1, recovered in one word. 🔥 **One answer reversed a default and one arrived as a definition, and they are the same answer.** Asked *how often* the money-card claim is offered, the reply defined the unit: **a game is from the turn-up to somebody going out — a game is a round.** Asked what moves between rounds, the reply used it: **"you randomize seats between games."** 🔥 **So the seating is re-drawn before every deal, and `MatchEngine` — which holds one seating for a whole match, P9's choice while the question was open — is the first thing in this tree that actively contradicts a recorded rule rather than merely lacking one** (§10 #16). ✅ **No published figure moves**: every experiment in `BurmesePoker.Sim` runs `RoundsPerGame = 1`, checked rather than assumed, so no measured game has a second round to re-seat for. ⚠️ **The front ends are where it shows, and it is a UX question as much as a loop** — P13.5 puts *you at the front whichever seat you were dealt*, so the table would visibly rearrange itself around a fixed viewer every deal. 🔥 **The most useful answer was a rider.** #13 came back not as *"yes"* but **"yes, as long as you aren't violating any other discard rules"** — and §5.1 is the only other discard rule there is, so **the feeding ban outranks the right to throw a card straight back**, the just-taken card is filtered like any other, and the §5.1 packet needs no special case for it. ⚠️ **A player describing a legal-move filter unprompted is the same construction §5.1's enforcement already takes.** ⚠️ **Two answers were hedged and are tagged as house rulings with the hedge quoted** — *"maybe… i don't think that's mechanically possible/relevant"* on whether money cards stack past a double (#24c, `PLAYER`), and *"not that I know of"* on whether anything else changes with the player count (#26, `EXPERT` **Probable**, kept unsettled because §7.1.1 is proof the question is real). 🔥 **#24c is half wrong and the document can prove which half**: a triple *is* reachable — both 7♦s turned up together, about one round in 5,800 — and it *is* irrelevant, because both copies are then lying on the table as designators, nobody was dealt one, a card on the table is owned by nobody (§4.4), and the value pays nothing whatever it stacks to. **Not flattening the hedge is what left room to find that.** ✅ **What remains is #5's *who approves the claim* and #11's *which jokers*** — and #11's default sends a joker's money to two cards where §5.1 #27 sends its feeding ban to all four, deliberately, since designation is scoped narrow and feeding broad. Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md` (**new Q7**), `CLAUDE.md`, this file. **No code touched.** |
| 2026-08-21 | — | ✅ **The win condition is fully specified — `RULES.md` is rev 18.** Two questions put to **Mya Lay and Aung Aung**, the last two open against §7.1.1, both answered `EXPERT`, and **neither changed a rule**. **(1) §9 #22 — at two players a set is illegal *as a meld*, not merely unnecessary**: *"in 2 player, you must go out with ALL SERIES, no sets."* Rev 17 had recorded the strong form because it was what was said, and flagged that it had never been put back as a table situation; put back, it held. 🔥 **A confirmation is not nothing** — §9 #25's reasoning about the feeding ban head-to-head leant on sets being illegal two-handed, and until now it was leaning on an unconfirmed recording. **(2) §9 #29 — an all-joker meld is a series, *"but not clean"***, so it can discharge a **surplus** series and never a **required** one (§6.1, §7.1.1) — three-handed, three jokers can never be one of the two. 🔥 **That also closes §9 #8**, open since rev 1, and it is **the only question in this document that a piece of reasoning got right before anybody was asked**: the recommendation was *unlimited jokers, which permits an all-joker meld*, and **P3 has been emitting them since rev 10 on exactly that argument** — against three where reasoning ahead came out wrong (§6.2 duplicate suits, §7.1 purity, §5 discard piles). ⚠️ **One piece of it is `DERIVED`, not `EXPERT`**: nobody was asked *how many* jokers a meld may hold, and that a three-card meld may be entirely jokers merely forecloses any cap below a meld's own size. ⚠️ **No code changes and no measurement moves** — P3 already emits all-joker melds, and `HandEvaluator` was already wrong about the two-handed rule before rev 18 and is no more wrong after it (§10 #14). 🔥 **The finding is about asking, and it cost an exchange.** #22 was first put as *"illegal, or merely not required?"* — two named readings — and came back *"Why are you asking me about sets?"* **A rule can be described accurately in words that leave an implementer two choices, and the player who knows the rule hears no ambiguity at all.** What worked was not a sharper distinction but a **table situation** — *three of a kind in your hand at the end*. ⚠️ **Bookkeeping found while writing this up: rev 17 never got a session-log row**, so its twelve answers and §7.1.1 reached `RULES.md` but not this file; the **Current state** block above has been corrected (it still said §5.1's six details were unrecorded, and still called this file's rules pointer rev 16), and `CLAUDE.md`'s start-here block was corrected the same way. Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md` (**new Q6**), `CLAUDE.md`, this file. **No code touched.** |
| 2026-08-20 | P24 | **Planned, not started.** 🔥 **The first packet in the plan that came from a playtest rather than from §0**: the browser's arrow gets a *why*, so a session played beside an expert leaves a **record of where the expert disagreed**. Scope taken by Nick the same day — **browser only** (the console keeps its arrow, so `drive-console.py` captures stay comparable), **all four questions**, **winner versus runner-up** rather than a ranking table, and **written to a journal** as well as drawn. 🔥 **Two findings from the survey, both taken before a line is written.** **(1) `BurmesePoker.Web` and `BurmesePoker.Server` contain the string `journal` zero times** — `--journal` is on the console and the harness only, and `TableSession` hands its agents to `MatchEngine` with nothing wrapping them, so journalling the hosted table is a piece of work *inside* P24 rather than a field on an existing writer. **(2) A rationale field on a decision records the wrong thing.** `JournalingAgent` writes down the answer *the seat gave*, and at a table with Mya Lay in it that answer is hers; the computer's recommendation is a different agent's opinion about the same moment, so the record is the decision **plus the advice** — and **disagreement becomes a query (`Answer != Advice.Card`) instead of something a person notices and transcribes**. ⚠️ That contradicts `JournalingAgent`'s stated stance (*"it records answers, not intentions"*) deliberately and narrowly: the intention is a *different* agent's, taken on the same context. ⚠️ **The explanation costs nothing new if it is built right** — `CoverScore.Ranking` already computes every key and throws all of them away except the order, so the discard path calls the explaining ranking **once** and takes its head; `outs` is 8.2× a greedy round and a second call per turn would double every human turn for a sentence. ⚠️ **The keys are packed for sorting, not for reading**: `outs` stores its key negated, `Potential` is `int.MaxValue` for a joker, `cautious` packs two into one `long` — so a rung describes its own keys and presentation never interprets a bare number. ⚠️ **Two constraints are already tests**: the rationale rides on `SeatPrompt` and never on a `TableEvent` (`ConcealmentTests`, §3.11 A1), and three sentences of prose must live in a `<details>` (`MarkupStandardsTests.NoParagraphOnTheTableIsAWallOfText`, 80 characters, §3.11 B9). 🔥 **A third trap surfaced on the second pass and it is P19's, not P21's: never explain through `FallibleAgent`.** A difficulty level is `Hardest` wrapped in a mistake rate, and that mistake is **the runner-up of the very ranking this packet renders** — so a level in the advice path yields a page confidently justifying a move chosen *because it was second best*, which is the failure mode that still looks right. **Acceptance 6 asserts the bare rung at every level, `easy` included.** ⚠️ **Two smaller ones written down with it**: the explaining interface is **public while everything it reports on is `internal`** (the domain's only `InternalsVisibleTo` is the test project, P21), so the keys cross as a *described* result rather than by widening it; and the advised card is journalled **by `CardId`** exactly as the answer is (§3.1), because two decks hold two 5♥ and a value comparison would report agreement on precisely the hands worth studying. ⚠️ **The rejected options are recorded rather than dropped** — a full ranking table (renders the procedure, not the decision), a sentence-plus-table (the same table wearing a summary), a `--explain <path>` sidecar (a second format to join, when P14 already owns what happened at this table), and the console (cheapest place for a debug tool, not the place you sit with somebody, and it would spend P21's and P23's byte-identical captures for no reader). ⚠️ **Sequenced ahead of the §5.1 packet on purpose** — §5.1 filters the very ranking P24 renders, but it is blocked on §9 #16–#19 and P24 is what makes that conversation productive. Docs only: `BUILD-PLAN.md` §4 and new §5 packet, this file. **No code touched; the tree is unchanged at 548 passed / 0 failed.** |
| 2026-08-20 | — | 🔥 **A playtest with Mya Lay produced a new rule, and it is the first one that constrains *which* card you may discard.** `RULES.md` is **rev 16**; the rule is **§5.1, the feeding ban**, `EXPERT` and Settled: **once a player has taken a card in the open, the seat that discards to them may not discard that rank again for the rest of the round** — *rank only, suit irrelevant* — with two exceptions, **release** (the protected player discards that rank themselves, which frees it **permanently**, even if they pick another one up afterwards) and **going out** (the ban never stands between a player and a win, and costs nothing there because the round ends on that discard). 🔥 **The release is what makes it cheap**: it is a **set of ranks per seat**, one bit a rank — *"has she thrown a Queen this round?"* — rather than a history of who took what and when. ⚠️ **At a table you answer that by looking down her discards — legitimately, since rev 17 closed §9 #15 and the discards are public — but in code you must not**, because §5's reshuffle sweeps the pile away and would silently re-arm a released rank mid-round, so the set is kept rather than read back. 🔥 **And the match is *rank alone*, which is a third identity notion the domain hasn't got**: `==` on `Card` is instance identity and `SameValueAs` is rank **and** suit **and** colour (what §4.2's money designation needs) — **reaching for `SameValueAs` here would leave the Q♣ Mya Lay actually objected to perfectly legal.** 🔥 **The Q♦ in the playtest was a money card and that is a red herring**: by §4.4 a card *picked up* from a discard is held but never owned and pays nobody, so taking it moved no money at all — the advantage being denied is a **melding** advantage, which is why §9 #18 recommends the ban covers any rank. ⚠️ **Settled and wholly unimplemented.** `RoundEngine` accepts any discard, `TurnContext` does not even show a seat the information the rule is decided from, and no agent knows it exists — recorded as `RULES.md` §10 #13. ⚠️ **Six details are unrecorded and they are the rule's specification, not fidelity questions**: §9 **#16–#19, #25 and #27** — who is bound (recommend the seat above you only), what arms it (recommend public takes only; a blind draw is concealed and unenforceable, and the §4.5 claim must count, because Mya Lay **opened** and had no discard in front of her), money card or any card (recommend any rank), whether a release survives the deck-exhaustion reshuffle that sweeps away the pile carrying it (recommend yes — which means an implementation needs a released-rank set per seat and cannot lean on the pile alone), 🔥 **what taking a joker closes given that a joker has no rank at all** (#27, recommend *jokers*, added later the same day by checking §5.1's *"the same rank"* against `Card`), and whether the ban works **two-handed** where the seat that feeds you is the seat you feed (#25, arrived with rev 17's player-count answers). 🔥 **§9 #20 was raised and closed the same day, in two rulings that only work together.** *(a)* **A banned discard is not an infraction but an impossible move** — never offered, cannot be chosen — so there is no penalty and nothing to retract. *(b)* **Where the ban would leave a player no legal discard at all, the ban yields for that turn** — the discard is mandatory (§7.1), the ban is not. 🔥 **(b) is what makes (a) safe**: impossible-move enforcement takes away the social escape hatch a rare hand would otherwise rely on, so without the floor a fourteen-card hand of banned ranks is a turn that cannot be completed — an unreachable state that, unhandled, is a crash rather than a rare event. It is reachable on **two ranks**, because two decks put eight copies of a rank in the shoe. **Together they collapse to one line**: the legal discards are *the hand minus the banned ranks, **or the whole hand if that is empty***, so the choice presented to a player is **never empty by construction** and a turn cannot deadlock. ⚠️ **Enforcement by construction still has a bill**: the upstream seat has to be *told* which ranks are closed — free, and 🔥 **not a collision with §9 #15**, because the ban is computed from things done **in the open** (the seat below *took* its card in view; a release was *thrown* in view) rather than from reading a pile — safe even under #15's strict default, and rev 17 then closed #15 the permissive way anyway — and every agent's discard ranking must be filtered to legal cards **including `FallibleAgent`'s runner-up**, because a mistake still has to be a legal move. ⚠️ **One speculation raised and withdrawn inside the same revision**: while #20 was open this document wondered whether the deadlock was §9 #6's unrecovered exception to the mandatory discard. **It is the reverse** — nobody skips a discard under the floor; the *ban* is what gives way. ⚠️ **Rev 17 then closed #6 outright — there is no exception, you always discard** — which settles it the same way and makes the floor the only way §5.1 and the mandatory discard can both hold. ✅ `QUESTIONS-FOR-MYA-LAY.md` **Q4** has five of the six phrased as flat table situations (#25 is asked with the player-count set), plus a note saying which two questions not to ask. Docs only — `RULES.md`, `RULES-PRIMER.md`, `QUESTIONS-FOR-MYA-LAY.md`, this file. **No code touched; the tree is unchanged at 548 passed / 0 failed.** ⚠️ **There is no packet for this yet** — implementing §5.1 needs one, and it is the first work since P0 that changes what a legal turn *is*. |
| 2026-08-20 | P23 | ☑ Done. **The standing answer — and the last packet in the plan.** `docs/STRATEGY.md` answers *"which bot should I play, and what actually works in this game?"* in one document that regenerates from one command, and the two ways it could quietly stop being true are now assertions rather than habits. **548 passed, up from 543.** 🔥 **The headline is the reproduction: 59 of the suite's 77 rows came back byte-identical** — every ladder cell, the null test, all twelve pairing ratios and both of P12's headline rows, from a tree that had since gained a rung and changed what the standing field is. **The seven that moved are the difficulty dial and only the dial**; the twelve that are new are the money sweep, so §10 quotes `measurements.csv` like every other section and P22's join is closed. 🔥 **The re-calibration moved exactly one value.** P21 re-based the dial onto `outs` without re-spacing, leaving reference-table steps of **8.2 / 4.3 / 10.3** points — monotone, passing every standing check, and visibly not a dial. Re-sweeping ε on `outs` over P19's own seven probes moved `hard` from **0.5 to 0.4** and left the other three alone: **7.9 / 6.7 / 7.7** at the reference table, **+11.20 / +9.80 / +6.82 ± 1.00** head to head, all surviving Holm at 6.8× the half-width or better. ⚠️ **Only one moved because the ε curve barely changes shape between rungs** (0 → 0.5 costs 9.5 points on `outs` against ~8 on `greedy`; 0.5 → 1 costs 16.5 against ~17) — **a mistake rate is nearly a property of the mistake, not of the rung it is made against**, so re-check it after the next rung rather than re-deriving it. 🔥 **And two instruments disagree about spacing**: the reference table is narrowest in the middle, head-to-head narrowest at the top, because a cell holds two levels and a table holds four — the re-fit improved both, and §9 records that the reference table is the tie-break. 🔥 **P22's bill was paid structurally, not by typing a shorter field**: `BotRung.Ranked` (`RankedOn.WinRate` / `Money`) has each rung declare which instrument settles it, `BotCatalog.Ladder` and `StakesSensitive` are **filters of `All`**, and `prospector`'s six duplicate cells are gone — ⚠️ **the wall clock was the smaller half of that argument, because six null cells in a Holm family make every real verdict in it harder to reach.** ✅ Measured, not argued: every ladder figure reproduced to the digit without them. ⚠️ **Acceptance 2 is `Tests/Sim/StandingAnswerTests.cs`**, which reads the published CSV and goes red if the ε values it publishes are not the ones both front ends offer, if a step did not survive Holm, if a rung in the catalog is the subject of no row, if the ladder and the sweep are not between them the whole catalog, or if a front end writes out a description instead of asking for it — 🔥 **it failed on `prospector` first time**, which is exactly the drift §11 had been recording in prose. ✅ **Re-spacing is not a play change, proved byte for byte**: `--pick 0` captures from `HEAD` and this tree are identical from the `Seating:` line on, **7,025 and 88,805 bytes**. ⚠️ **The suite is now five hours** (17,539 s) and **the structural saving has been taken; there is not a second one** — `--pairs adjacent` on the ladder would cost §3's matrix and stays unused. No rules question raised; `RULES.md` stays at rev 15. |
| 2026-08-20 | P22 | ☑ Done. 🔥 **Money: is there a strategy in the side bet? — no at the stakes the game is played for, and `+7.3 ± 3.3` a round at eight times them.** `Domain/Agents/ProspectorBotAgent.cs` is `outs` with one change: a card taken from anywhere but the deck — the previous player's discard, *or* the turned-up money card — must be worth more than the **ownership** a blind draw would have conferred (RULES.md §4.4, §4.5). `Domain/Agents/MoneyOdds.cs` prices that from public information only: the stakes, the designation, how many players pay, and how much of the shoe this seat can see. `Sim/MoneySweep.cs` is the experiment — the challenger against `outs` at four stakes ratios, every cell dealt from the same shoes, **judged on `$/round` with the win rate beside it**, Holm-corrected over the four. **543 passed / 0 failed**, up from 529 (14 new: 6 in `Agents/ProspectorBotAgentTests`, 5 in `Sim/MoneySweepTests`, the rest catalog and ladder cases that widened with the field). 🔥 **At $5/$1 the rule never fires and this is an identity, not a measurement**: two tables of one rung each, dealt from the same shoes, play the same rounds card for card — so the standard-stakes cell is a **null cell**, and at **`+0.01 ± 0.22`** it is the tightest one this harness has produced. 🔥 **At $5/$40 it separates the other way**: take rate **0.1%** against `outs`' 24.9%, **20.1 ± 0.9 points fewer rounds won**, **`+7.34 ± 3.29` a round banked**, `p = 1.3e-05`, surviving Holm — the side bet alone moves `+11.36 ± 3.29`, buying the whole of the win rate it gives away. $5/$10 is `−0.86 ± 0.82` (raw only) and $5/$20 is `+0.95 ± 1.63` (break-even), so **the four cells are monotone in the stakes**. ⚠️ **The programme's first published divergence between money and win rate** — a reader ranking the $5/$40 cell by win rate would rank the better player last — and the report says so itself rather than leaving it to be noticed (acceptance 2). ✅ **Acceptance 3 holds**: the discard is `outs`' card at every stakes tried, `MoneyCardsDoNotChangeWhatABotThrowsAway` covers the new rung, and a sharper test shows the take differing between two designations while the discard does not. ✅ **The difficulty dial did not move and no front end needed a line**: `prospector` shares `outs`' `Strength: 3`, ladder order breaks the tie, `BotCatalog.Hardest` is still `outs` — and `drive-console.py` at `--seed 20260819 --pick 0` captured **90,251 bytes identical byte for byte** from `HEAD` and from this tree. 🔥 **A `DERIVED` rules note fell out of the arithmetic — RULES.md is rev 15**: a designation landing on a **permanent** money card leaves the deck with *less* money in it, not more, because doubling one value is not the same as designating a second and the designator itself leaves the deck (§3 step 4). **Found by writing a test that asserted the opposite.** 🔥 **And the finding that outlives the rung: a rung's strength stopped being a property of the rung.** `prospector`'s one decision reads the *stakes*, which are fixed per game and are not a rule, so *"how good is it"* has no answer until somebody says what the table is played for. ⚠️ **The bill, and it is P23's.** A new name in `BotCatalog` is `k−1` new head-to-head cells for ever: seven rungs is **21 cells against 15** and `sim suite` went from ~1h45 to about **3h15**, so it was **not re-run** — `docs/strategy/measurements.csv` is one rung behind the catalog, `STRATEGY.md` §10 is generated from the new `docs/strategy/money.csv` instead, and §11 records the gap. **Six of the 21 cells are `outs` against itself in all but name.** Amended BUILD-PLAN **P22 (done, with four findings), P23 (the suite cost, and the join §10 owes `measurements.csv`), §4's graph and prose, and two risk rows — the research-rung row gains P22's cost lesson, and a new row for a standing suite nobody re-runs**. |
| 2026-08-20 | P21 | ☑ Done. 🔥 **Outs: the first rung that looks ahead — and the first that beats `greedy`.** `Domain/Agents/OutsBotAgent.cs` is `greedy` with one thing inserted: where two discards leave the hand equally melded, it keeps the thirteen that **more of the pack would improve** — a count, per candidate, of how many of the values still out there would raise the cover count of what is left. **529 tests, up from 516.** 🔥 **It measures `+3.1 ± 1.0` points against `greedy` at 8,008 games a cell**, `p = 1.9e-09`, surviving Holm, and takes the free-for-all 26.3 ± 0.5 to 23.7 ± 0.5. **Three research packets had produced nothing above `greedy`; this clears the apparatus's resolution three times over.** 🔥 **The *why* is a contrast**: `cautious` and `counting` both put their new idea *underneath* `CoverScore.Potential` and decide only what greedy had already given up on — a residue worth about half a point, which is below what the harness can see; `outs` puts its key *above* it, and greedy's tie-break is demoted to breaking **its** ties. ⚠️ **P20's "change which question is asked" meant "and ask it earlier than the question you are replacing."** ⚠️ **It is `Strength: 3`, so the difficulty dial re-based onto it**: all four levels are `outs` with an ε now, the dial is still ordered but the ε spacing was measured against `greedy`, and **P23 owns re-spacing it and is no longer optional**. 🔥 **The cost was the packet.** Naive, it ran at **12.6× a greedy round**, over the stated budget; four shortcuts *around* the evaluator took it to **8.2×** — refine only what is tied at the top (7.1 candidates a turn, not 13), prune values that cannot enter a meld (34 searched of 53), ask the search for a **bar rather than a maximum** (`PartialCover.CoversAtLeast`, ~98 µs → ~10 µs), and build **one meld index a candidate instead of one a probe** (`CoverProbe`). 🔥 **Then the finding that outlives the rung**: three quarters of what remained was candidate generation, and it was a **fixed per-call allocation cost** — ninety window arrays × four suits every call, and both generators walking suits and ranks that could hold no meld. A window table, one slot buffer a length and two feasibility checks made **every rung, every hint and every engine turn about 45% faster** (greedy 48.8 → 71.8 rounds/s serial, same process). ✅ **Proved a refactor, not a change**: `drive-console.py` at `--pick 0`, both scripts, byte-identical from `HEAD` (8,763 and 92,172 bytes). ⚠️ **Two things not in the build list.** (1) `BurmesePoker.Domain` gained `InternalsVisibleTo BurmesePoker.Tests` — every one of the four shortcuts is a claim about answers and is asserted against the search it replaces, and a test cannot assert what it cannot see. (2) `sim bench` now times **the rung's decision** in whole rounds, and the `--help` ladder is read from `BotCatalog` rather than typed out, which was P20's defect surviving in one more place. |
| 2026-08-20 | P20 | ☑ Done. **Memory: the card-counting rung — and the answer is no, published as a null.** `Domain/Agents/CountingBotAgent.cs` is `cautious` with one substitution: what is left in the shoe is estimated from every card this seat has been shown this round rather than from its own thirteen. **516 tests, up from 506** (10 new — 7 in `Agents/CountingBotAgentTests`, the rest catalog and ladder cases that widened with the field). 🔥 **It measures `+0.3 ± 1.0` points to `greedy`'s side at 8,008 games a cell** — not separated, and the point estimate pointing the *wrong way*; `cautious` is `+0.8 ± 1.0` ahead of it. P20 acceptance 1 asked for a null to be publishable and it is: `docs/STRATEGY.md` §8. 🔥 **The finding is *why*, and it narrows P21 to one idea.** The memory demonstrably works — a test shows the supply estimate falling below `cautious`'s for every card watched go by and holding at the full two copies for every value never shown — but it cannot pay, for two reasons that are now measured rather than argued. **(1) The information set is tiny**: under the cautious default it runs **12 → 23 cards across a whole round out of 108**, about ten cards learned beyond its own hand, one a turn. **(2) It enters where nothing is paid**: it sharpens `ThreatScore`, which *is* `cautious`'s tie-break, and P17 measured that tie-break at `−0.2 ± 1.0`. ⚠️ **A sharper input to a decision rule already shown not to matter is worth nothing, and the two nulls compound rather than add** — so P21 must change *which question is asked*, which is what P15's "has to be combinatorial" always meant. **Two of three research rungs have now returned nothing.** ⚠️ **Two things not in the build list.** (1) **`ThreatScore` extracted from `CautiousBotAgent` unchanged** — the two rungs differ in exactly one thing, a `Supply` delegate, and two copies of that arithmetic would be two places for it to drift; P15's "one change against the rung below" is a claim about code before it is one about results. (2) 🔥 **The ladder was written out in three places and a fifth rung made all three wrong at once**: `tournament` and `suite` both defaulted `--strategies` to a hand-typed `random,simple,greedy,cautious`, so a new rung was measured only when somebody remembered to name it. **The default is now `BotCatalog` itself** — P18's defect one layer up, in a front end nobody thinks of as one, and it discharges half of P23's standing-set caveat. ✅ **The rules question was not decided in code**: RULES.md §9 #15 — *is a discard pile inspectable, or only its top card?* — stays open at **rev 14**, `QUESTIONS-FOR-MYA-LAY.md` carries it flat, and the bot counts only what it has been shown, **wrong in the direction that does not cheat**. ⚠️ If the answer comes back that the piles may be read, the rung gets a far larger information set and deserves re-measuring before it is written off — the 12 → 23 figure is exactly what the cautious default costs. ✅ **Acceptance 5, throughput: the memory is free** — **77 rounds/s** against `cautious`'s 76 and `greedy`'s 88 (P12's baseline: 51 serial, 85–92 parallel), nowhere near a budget. ⚠️ **Acceptance 3's round-boundary test is asserted on the *size* of the memory, not on ids across rounds**, because a `CardId` names a card in a *round's* shoe (P13.4) — so a memory that survived a deal would not be stale, it would be a memory of cards that no longer exist, and the same deal is replayed twice so that a surviving memory looks *plausible* rather than crashing. ⚠️ **`docs/strategy/measurements.csv` regenerated: 52 measurements in 35 minutes**, and the free-for-all and mean-margin columns all moved — five strategies crossed over four seats is a different field from four. **The head-to-head margins did not**, and every one reproduced to the digit; STRATEGY.md §4 now says which column to distrust when a rung is added. Amended BUILD-PLAN **P20 (done, with the finding), P21 (measure against `greedy` only — `counting` is not a distinct reference, and the suite is now k(k−1)/2 cells) and P23 (the standing-set caveat is half-discharged; make the default a test)**. |
| 2026-08-19 | P19 | ☑ Done. **Difficulty as a dial — four levels, one mistake rate each, and every step measured.** `Domain/Agents/DifficultyLadder.cs` holds `easy`, `medium`, `hard`, `expert`; each is `BotCatalog.Hardest` wrapped in a `FallibleAgent` that, with probability ε, throws the card that rung ranked **second** rather than first. `IRanksDiscards` is what makes that a *plausible* mistake rather than a random one (§3.12 item 3 — a bot that threw jokers away would read as broken, not weak), and `CoverScore.Discard` is now **defined as the head of `CoverScore.Ranking`** so the winner and the runner-up cannot drift apart. Both front ends offer levels only; the ladder stays the research instrument (§3.12). **506 tests, up from 477.** 🔥 **The calibration is the packet, and ε is violently non-linear**: `greedy@0` vs `greedy@1` is **+33.3 ± 1.6** points head to head — three times the whole `simple`-to-`greedy` gulf — with ε = 0→0.5 worth about 8 points and ε = 0.5→1 worth about 17. **So the four are spaced evenly in *results* and not in ε**: 0.9, 0.7, 0.5, 0.0, measuring **14.50 / 22.17 / 27.22 / 36.11** at the reference table and **+10.79 ± 1.00, +5.69 ± 1.01, +9.90 ± 1.00** head to head at 8,008 games a step — **all three separated under Holm**, the narrowest 5.7× the half-width. **Four levels rather than three, and the count is a measurement rather than a taste.** ⚠️ **`--pairs adjacent` had to be built and was not in the plan's build list**: acceptance 1b wants the family to be the k−1 steps, so `TournamentOptions.Pairs` decides which pairs are *played* as well as corrected — and 🔥 it exposed a latent assumption, `PairingChecks` reading the *field* rather than the *cells played* to pick its across-cell comparison, which threw on any run where most pairs never meet. 🔥 **Second finding, and it is P13.1's arriving in the test project: a `TurnContext`'s hand is the engine's own list**, so keeping a context and asking the rung for its ranking afterwards asks about the **thirteen** kept rather than the fourteen it chose from — `RecordingAgent` records the ranking during the turn now, and this was found by writing the test. ✅ **Acceptance 3 verified end to end**: `expert` at ε = 0 plays the match `greedy` plays, **7,371 bytes (`bots`) and 90,726 (`human`) identical from the `Seating:` line on**, captured from `HEAD` and from this tree at `--pick 0`. ⚠️ **The `--pick` map changed** — `0` expert, `1` hard, `2` medium, `3` easy. ⚠️ **`sim suite` gained a second standing check** and exits non-zero if the dial stops being monotone, beside the null test; the regenerated suite is **41 measurements, ~120,000 games, 34 minutes**, and every ladder figure reproduced P17's exactly. ⚠️ A computer seat in the browser is now named for how it plays (`Mya Lay (expert)`), because a mixed table nobody can tell apart is a mix nobody asks for twice; the console's names are untouched, which is what keeps the byte-comparison meaningful. ⚠️ **Three defects found only by opening the lobby in Chrome and pressing the buttons**: the new checkbox row inherited the form's ten-rem label column and wrapped onto three lines; 🔥 **`--mixed` without a value is silently ignored** by the command-line configuration provider, so it is `--mixed true` like `--hints false`, found by reading the seat names and seeing four `expert`s; and a seat name is ellipsised at the panel's width, which `Aung Aung (medium)` now reaches — the full name went onto a `title`. |
| 2026-08-19 | P18 | ☑ Done. **One catalog — a bot is named in one place, and every front end resolves the name.** `BurmesePoker.Domain/Agents/BotCatalog.cs` holds the four rungs as `BotRung`s (name, a line written for somebody choosing an opponent, a `Strength`, a factory taking a seat seed); `Sim.StrategyCatalog` is an adapter over it, the console's private two-value `Difficulty` enum is **deleted**, `ComputerAdvice` and `TableOptions.StandIn` ask it for the hardest rung, and the browser — which had **no difficulty setting at all**, just two hard-coded `new GreedyBotAgent()`s — gained `--difficulty`, `TablePlan.Difficulty` and **the whole ladder on the lobby's open-a-table form**. **477 tests, up from 461.** 🔥 **The finding: the console's difficulty default had been the *easy* bot since P10.** A `SelectionPrompt<T>` opens on `default(T)` when that value is one of the choices, and the enum was `Easy = 0` — so the list read *Hard* first, read as though hard were the default, and handed `SimpleBotAgent` to everybody who pressed return. **Found because the refactor's byte-comparison failed**: the pre- and post-P18 captures played different games at the same seed, and the journal header — which now writes the catalog name — is what finally said which bot was in the seat. Confirmed rather than guessed with a probe prompt offering `(7, 0, 5)`, which comes back `0`. A rung is a reference type, so `default` is null and the cursor stays on the first entry. ⚠️ **P19 inherits the bug the moment it makes a level a value type.** 🔥 **Second finding: a record's `with` does not re-run a property initialiser.** `BotRung.Name` refuses a `#` because the tournament's null cell labels a copy `"{name}#mirror"` and a front end offering `greedy#mirror` as an opponent would be absurd — but validating in an initialiser refused it in a constructor and *allowed* it in `rung with { Name = … }`, which is exactly how `Tournament` makes that copy. The check moved into the `init` accessor; **found by writing the test, not by reading the code.** ⚠️ **Acceptance 2 was amended twice**: the whole capture cannot be byte-identical because acceptance 1 changes the prompt, so the comparison is of the match — **6,625 bytes (`bots`) and 89,233 (`human`), identical from the `Seating:` line on** — and `--pick n` was added to `drive-console.py` so a capture names the rung it played. ⚠️ The difficulty question is the **third** prompt with `--script bots` and the **fourth** with `--script human` (a person is asked their name first), which made a `--pick` that worked for one silently do nothing for the other. Acceptance 4's grep test went into `LayeringTests` rather than `MarkupStandardsTests` — it is a claim about seven projects, not about markup — and it bans constructing a **rung**, with the types taken from the catalog so a fifth one is covered the day it is added. Verified end to end in the real browser: the house table opened with `--difficulty simple` says so, and a table opened from the form on `random` deals `random`. |
| 2026-08-19 | P17 | ☑ Done. **The tournament — and the first honest interval this harness has printed.** `BurmesePoker.Sim -- tournament` plays every unordered pair head to head at every seating in which both are at the table, reports the fully crossed free-for-all beside it (P16's lesson that they answer different questions), ranks the field by mean margin, and puts all six comparisons through **Holm–Bonferroni**; `-- suite` generates **`docs/strategy/measurements.csv`**, and **`docs/STRATEGY.md` is created and quotes that file rather than a session's console** (§3.12). **Two independent 15-minute, ~64,000-game runs of the suite wrote a byte-identical file.** 🔥 **The finding that changed the code was a number that should not have moved:** built as the packet literally said — "a mean over games, one value per game" — the balanced headline came back **30.6/19.3** where P16 published **29.6/20.4**. Neither is wrong; they are **two different estimands**. A strategy holds a different number of seats in different games of a crossed run, so the unweighted average of per-game *ratios* over-weights the games it held fewest seats in — which, for a strong strategy, are the games it does best in. ⚠️ **The gap was 1.05 points, the size of P16's whole seating effect**, and adding an interval to a figure must not change the figure (§3.8 item 4). So `GameValue` carries **a total and the trials it is out of**, and `Measurement.Of` is the **ratio estimator** — totals divided, standard error from the per-game residual `total − ratio × trials`, the game still the trial, and *exactly* the per-game mean when the denominator is constant. The balanced headline now reads **29.75 ± 0.47 / 20.25 ± 0.47**. 🔥 **The second finding: "paired is narrower" is half backwards.** Across cells (one strategy, two tables, same shoes) pairing narrows — ratios **0.57–0.95**. Within a cell (two strategies, one table) **exactly one seat declares**, the series are strongly negatively correlated, and pairing **widens** — measured **1.408 / 1.409 / 1.414**, √2 to three digits — so the add-the-variances formula is **anti**-conservative on a head-to-head margin by 41%. P17 acceptance 3 amended to say so. **The measured ladder at 8,008 games a cell:** `random` loses by 49.7–49.9 ± 0.4; `simple` loses to `greedy` by **11.2 ± 1.0** and to `cautious` by **10.8 ± 1.0**; 🔥 **`greedy` vs `cautious` is `−0.2 ± 1.0`, p = 0.70 — P15's negative result confirmed under a design P15 never ran.** Free-for-all (8,192 games, 256 seatings): 0.1/27.3/35.8/36.5 ± 0.9. ⚠️ `cautious` leads `greedy` in one table and trails it in the other, both inside the interval — **exactly what the correction exists to stop a reader promoting to a rung.** ✅ **The null test passes**: `cautious` against a copy of itself is 25.1 ± 0.5 / 24.9 ± 0.5 against a fair 25.0%, margin +0.3 ± 1.0, each label in each seat exactly 4,004 times — and `sim suite` exits non-zero if it ever fails. **Resolution stated for P19–P22: 1.02 points at 8,000 games a cell; half a point costs ~34,000, four hours** — so a rung worth less than a point is not promotable at the default size. Seven new files in Sim (`GameValue`, `Normal`, `Holm`, `StrategySeries`, `Tournament`, `TournamentCsv`, `Suite`), plus `SeatingPlan.HeadToHead` and a rewritten `Measurement`; **`BurmesePoker.Domain`, `Simulator`, `GameRunner` and `Replay` are all unchanged.** Build clean, **461 passed / 0 failed** (23 new: 9 in `Sim/StatisticsTests`, 10 in `Sim/TournamentTests`, 4 in `Sim/SuiteTests`); ⚠️ the suite now takes **~60 s** against 18 s, because the tournament tests play real simulations. ⚠️ **One display defect fixed in passing** — a .NET two-section format picks its section from the **rounded** value, so `-0.04` under `"+0.0;-0.0"` prints `-+0.0`; `ReportNeighbours` had it in every signed column and signed figures are built by hand now. No new rules question — measurement is not a rule, so `RULES.md` stays at **rev 14**. Amended BUILD-PLAN §0, P17 (acceptance 3 and a "What P17 found" section), **P18 (the mirror label is not a rung), P19 (the family is the adjacent pairs, and the spacing has a 1-point floor), P20 (the promotion bar and its price) and P23 (STRATEGY.md now exists; what it still owes)**. |
| 2026-08-19 | — (planning) | 🔥 **Goal 5 stated and planned: a designed difficulty system and a settled answer to what works.** No packet executed and no code changed — the deliverable is the plan. Added **§3.12** (*difficulty is a dial, skill is a ladder, and they are not the same axis*), **§0's fifth goal** with the three architecture rows it demands, **seven packets P17–P23** in §5, a second branch on the §4 graph, and **five risk rows** in §7. Baseline verified first: build clean, **438 passed / 0 failed**. 🔥 **Two findings drove the ordering.** (1) A balanced 2,048-game run gave `random` 0.0% / `simple` 26.7% / `greedy` 36.1% / `cautious` 36.8% — **and the ordinary report prints no interval at all**, so P17 (statistics) precedes P19 (calibration); `Measurement` already exists and is reachable from one verb only. (2) **Four independent notions of *which bot*** across Sim, Console, Web and Server, and **the browser has no difficulty setting** — so P18 unifies the catalog before any new rung is written. ⚠️ **P19 finishes the difficulty product with today's rungs**; P20–P22 are droppable in preference order, which is P15's +0.5 ± 0.55 lesson applied in advance. ⚠️ **P20 will need a rules answer** — whether a discard pile is inspectable or only its top card (`RULES.md` §9 #9 stops being moot once a bot counts cards); the safe default is *only what this seat has been shown*, which errs towards a weak bot rather than a cheating one. |
| 2026-08-19 | P13.6 | ☑ Done. **The lobby, a second person, and §0's goal 4 — the plan is finished.** `Lobby` (singleton) holds `HostedTable`s by id, opened from a `TablePlan`; `Pages/Tables.razor` is the lobby at `/` (static SSR, two real forms) and `/table/{Id}` names one table. **`TableHost` is gone.** 🔥 **A `SeatBoard` belongs to a viewer**: `TableView` sits down, holds it, hands it to `YourSeat` as a parameter and stands up in `Dispose` — the one thing P13.4 said had to change rather than be added to. **`TableSession` learned who is sitting where** (`SitDown`, `StandUp`, `RemoteSeats`, `WaitingFor`, `IsFull`, `SeatTaken`/`SeatLeft`), because two viewers handed one `SeatConnection` are two people answering one question and a seat is a property of a table. 🔥 **The table deals while somebody is at it** — a viewer attending *and* every seat accounted for — which is P13.4's leftover, answerable at last, **without shortening the patience**. **Acceptance met:** `TwoPeopleTests.TwoPeopleAndTwoBotsPlayARound` settles round 1 with both people asked their own questions, hands pairwise disjoint a round at a time, banks summing to zero; **and it was played for real in two browser tabs**, Nick and Mya Lay, a whole round each answering their own prompts with `Tab`/`Enter`. §3.11 C16 finished: the felt marks a seat the computer is standing in at, and a reload takes your own seat back off your own ghost. **18 new tests, 438 passed / 0 failed, 0 warnings; five mutations applied, five red.** 🔥 **Findings: a test that a stood-up seat refuses is vacuous unless a question is standing — found by mutating `Dispose` and watching it stay green; two `<AntiforgeryToken />`s are worse than none and the page renders perfectly either way, found by pressing the button; a marker that means "away" has to stop meaning it, so standing-in is per turn and cleared at the next `TurnBegan`; the sit-down form must not hide itself when the table is full, because the person locked out is the one who just reloaded; a claim on a table must not be made while prerendering, and there are two of them now; a parameter to an interactive root is serialised, so the page passes an id; a settlement is not a resting state; and `FocusOnNavigate` beats §3.11 B7 on a reconnect, deliberately left alone.** |
| 2026-08-19 | P13.4 | ☑ Done. **A seat you can play — solo browser play, complete, and a legitimate stopping point.** `BurmesePoker.Web` gains `SeatBoard` (your seat, folded out of **your own** `SeatConnection`: the question standing, the hand it is about, and the hand you kept between turns) and three components — `YourSeat`, `TurnPrompt`, `HandPanel` — **all inside P13.3's single interactive island**, so §3.11 C12 is untouched. `TableHost` seats you: `--seat` (1 by default, 0 to only watch), `--name`, `--hints`, `--patience`. **All three acceptance criteria met.** ✅ **1:** a match played from the seats themselves — three rounds, four connected seats, banks carrying over and summing to zero, asserted in `SeatBoardTests` through `ClickingPlayer` (the browser's `ScriptedSeat`, which drives a `SeatBoard` so everything it sees is something a page would draw); and five rounds played for real in headless Chromium. ✅ **2 — a round played end to end with no pointer at all:** 86 questions answered with `Tab` and `Enter` over five rounds, **393 tab presses walking the hand**, and focus was already on a `<button>` at every prompt before a key was pressed (§3.11 B7); a second run on the final build played three rounds, **went out from the seat**, and so exercised all four branches of `TurnPrompt` in a browser. ✅ **3:** `NoSeatEverHeldACardFromAnotherSeatsHand` asserts every hand that was ever on any of four pages pairwise disjoint **a round at a time**, and `MarkupStandardsTests.NoComponentFindsASecondRouteToTheTable` forbids any component naming `TableSession`, `MatchEngine`, `TableState`, `TurnContext`, `PartialCover`, `HandEvaluator` or `ConnectionFor`. **§3.11 B6, B7 and B11 shipped**, and **A4 stopped passing vacuously** — it counts what it scanned now (eight handlers). **18 new tests, 389 passed / 0 failed**, and **eight mutations applied, eight red.** 🔥 **Findings: a `CardId` names a card in a *round's* shoe, which is rebuilt every deal, so hands are compared across seats a round at a time; your hand between turns is not stale and is worth rebuilding, with ownership read back off the `CardView`s you were sent rather than recomputed; a refusal must not raise "something changed", or a client answering on the change event answers the same refused question for ever; and only the first control may capture a `@ref`, because Blazor captures on insertion and not on every diff.** ⚠️ **A seated table no longer deals from boot** — an unanswered seat spends its whole patience on every question — and `TableHost.Yours` is one `SeatBoard` shared by every circuit, which P13.5 must change. 🔥 **And one thing only reading a control's accessible name out of the browser found: `CardChip`'s hidden words needed a full stop.** A card sits next to whatever else is said about it, and without one a screen reader ran the two together into *"five of clubs melds nothing"* — a sentence that means the opposite of itself. `docs/PLAYING.md` gained a browser section; `BUILD-PLAN.md` P13.4, P13.5, §2 and §3.11 (A4, B6, B7, B11) amended. |
| 2026-08-19 | P13.3 | ☑ Done. **The first UI in this project's history — a seventh project, and a browser table you can watch.** `BurmesePoker.Web` (Blazor Server; Domain + Presentation + Server): `TableHost` (one table, dealing itself round after round, one watcher connection, idempotent `Start`), `TableBoard` (the public game folded out of `TableEvent`s and **nothing else**), `CardWords` (a card said out loud, for a screen reader), and eleven components — a static SSR shell and rules page, and one interactive island: `TableView`, `SeatPanel`, `CardChip`, `RoundLogPanel`, `SettlementPanel`, each with its own `.razor.css`. **All three acceptance criteria met.** ✅ **1:** bot-only rounds play out in a browser start to settlement — verified in headless Chromium over the DevTools protocol, which watched the page go from Round 8 to Round 10 with no reload, no console errors and no reconnect modal, and screenshotted both themes at a settlement. ✅ **2 — the rest of the §3.11 A list shipped as tests:** `PaletteContrastTests` (A3 — computed contrast in **both themes**, read from `wwwroot/theme.css`, **pairs discovered by naming convention** rather than listed, plus a check that every `var(--…)` names a declared token), `MarkupStandardsTests` (A4 real controls, C12 no render mode at the root, C14 `@key`, C15 `InvokeAsync`, B8 one polite live region, and no `StateHasChanged` in a `Dispose`), `ComponentDisposalTests` (A5 — reflection over every `ComponentBase` for a table member, private ones included, plus that the subscription is actually unhooked). ✅ **3 — the B list reviewed by driving it:** tab order walked with `Input.dispatchKeyEvent` (skip link → nav, every stop visible and outlined 3px), the log made keyboard-reachable and named, `prefers-reduced-motion` and `prefers-color-scheme` honoured, 24px minimum targets, and every card carrying words as well as a glyph. 🔥 **Finding 1: `UseStaticFiles` does not serve the framework's own files.** `_framework/blazor.web.js` 404'd and the page looked perfect — **a prerendered Blazor Server page is a photograph of a broken one.** `MapStaticAssets`, and `launchSettings.json` because a build's endpoints manifest is a Development one. 🔥 **Finding 2: a trimmed log must not key on the length of the log** — `TableBoard.Narrated` counts every line ever said; `Log.Count` repeats the moment it trims, and a repeated `@key` is Blazor reusing the wrong DOM node. 🔥 **Finding 3: 240 visually-hidden spans made the document 10,295px tall** for a body of 1,814 — the absolute-positioning recipe needs a positioned ancestor. **Measured, not seen.** 🔥 **Finding 4: a source scan must read markup, not the prose about it** — four of six scans failed on comments in the files obeying the standard; and a `@key` check that looks *nearby* let a nested key cover for a missing one. **Eleven mutations applied, eleven red.** ⚠️ **`PacedAgent` moved to Presentation, not Domain** — Sim references Domain and must not be able to reach a sleep; **console byte-identical after the move** (two pty captures, two seeds, `cmp`). ⚠️ **`BurmesePoker.Tests` now references `BurmesePoker.Web`** and still never the console: the rule is that nothing is tested *through* a front end, and a component tree is data. **Build clean and warning-free, 371 passed / 0 failed** (29 new). **Domain, Server and Sim untouched** — the engine now stands unaltered across five consecutive packets. No new rules question — `RULES.md` stays at **rev 13**. Amended BUILD-PLAN §2, §3.11 (A3, A4, A5, B8, C12–C15, C17 all marked), P13.3 (a "What P13.3 found" section) and **P13.4 and P13.5, each of which inherits something now built.** |
| 2026-08-19 | P13.2 | ☑ Done. **The table server — a sixth project, and no transport anywhere in it.** `BurmesePoker.Server` (Domain + Presentation): `TableSession` (one hosted table: seats, connections, rounds, banks, its own seed), `TableSeat`/`TableOptions`, `SeatConnection` (a mailbox — the events a connection may hear and the question it is being asked), `SeatPrompt`/`SeatQuestion`/`SeatAnswer`, `TableFanOut` (the `IGameObserver` that applies the concealment), `TableEvent` (narration as a closed record hierarchy), `RemotePlayerAgent` (blocks on the connection, stands in with a bot when nobody answers), `BoundedAgent`/`TableClock`/`TableAbandonedException`. **All three acceptance criteria met.** ✅ **1:** two scripted remote seats and two `GreedyBotAgent`s play a full round through the server's own plumbing, in a test, with no sockets. ✅ **2 — the packet's most important test:** `ConcealmentTests` plays one round with **four connected seats and a watcher** and asserts pairwise-disjoint hands, no seat told what anybody else drew blind, a sweep over every card in every event against what that seat may see, and a watcher sent nothing but the public game. ⚠️ **Mutation-tested rather than assumed** — broadcasting the drawn card to everyone turns **three of the five red**. ✅ **3:** a seat that stops answering is played by the computer, the round finishes, and the takeover is broadcast (once a turn, keyed on `(Round, TurnNumber)` exactly as P11's pacing decorator is); a player who misses one prompt and comes back for the next one is simply back. 🔥 **Finding 1: exactly one event in the whole narration is private — the blind draw.** A discard, a taken discard, a claimed money card and a declaration all happen in front of the table, so the security boundary is one `if` and everything else in `TableFanOut` is care about lists. 🔥 **Finding 2: P13.1's snapshot rule generalised and caught two more live lists** — `TableState.TurnedUpOnTable` is aliased by *both* `TurnContext.TurnedUpMoneyCards` and `IGameObserver.RoundStarted`, and the opening player's claim removes a card from it; `EverythingASeatWasSentStaysWhatItWasWhenItWasSent` forces the claim and fails if either copy is removed. 🔥 **Finding 3: answering inside `SeatConnection.Updated` makes a whole round deterministic** — the event is raised on the round's thread before the seat waits, so eighteen tests that play real rounds cost four seconds and no flakiness; one test drives the genuinely cross-thread path because that is the shape a circuit has. ⚠️ **A client bug must not end a round:** `SeatConnection.Answer` **refuses** an answer that does not fit the question or names a card the seat is not holding — returns false, the prompt stands — where the engine would throw. ⚠️ **Deliberate deviation, and P13.3 inherits it: the stand-in is not paced.** A sleep belongs to whatever draws the table, so `TableOptions.StandIn` is a factory; **`PacedAgent` lives in `BurmesePoker.Console` and must move (Domain's `Agents/` is the obvious home) rather than be reached by a reference.** **The wall clock bounds the table, and every seat is wrapped including the bots** — what goes wrong at a hosted table is the people, not the play; the round is announced abandoned before the exception leaves and the session survives it. **Build clean and warning-free, 342 passed / 0 failed** (18 new: 12 `TableSessionTests`, 5 `ConcealmentTests`, 1 new `LayeringTests` row forbidding Spectre, ASP.NET, `System.Console` and `System.Net` in the server). **Domain, Presentation, Console and Sim were not touched at all** — the only edits outside the new project are one line of `BurmesePoker.slnx`, one `ProjectReference` and the layering row. No new rules question — `RULES.md` stays at **rev 13**, unchanged across six packets. Amended BUILD-PLAN §2, §3.10 (items 2 and 4 cashed), §3.11 A1, P13.2 (a "What P13.2 found" section) and **P13.3 and P13.4 — each inherits something already built, and P13.4's leak acceptance is narrowed because P13.2 already covers the seated case.** |
| 2026-08-19 | P13.1 | ☑ Done. **A presentation view model — a fifth project, and the first presentation code this project can test.** `BurmesePoker.Presentation` (Domain only): `CardDisplayState` (flags, not colours), `DisplayTokens` (a non-colour token for every one of them — §3.11 **A2, shipped a packet early**), `CardOrder` (the display sort, moved out of the console and now proved *total*), `CardView`, `MeldView`, `HandView` and `ComputerAdvice`. The console was rewritten *onto* it: its own `HandView` is gone, replaced by `HandPanel`, which draws and decides nothing; `CardFormatting` takes its ordering and its glyphs from Presentation, and `Palette.OwnedMark`/`AdviceMark` are now aliases of `DisplayTokens`, so the two front ends cannot drift to two stars. Secondary item done: the console takes `IAnsiConsole` everywhere and reaches for the static one **exactly once**, in `Main`. 🔥 **The test caught a real defect on its first run: a view model that aliases the engine's list is not a view model.** `TurnContext.Hand` is the seat's *own live list* and `RoundEngine` discards from it as soon as the answer comes back, so a view built at the discard reported fourteen cards and then held thirteen. The console never showed it — it renders and drops the view in one breath — and **a Blazor component holding a view across a render is exactly the case that does.** `HandView.Of` now copies; **P13.2 inherits the rule for everything the fan-out hands a seat.** ⚠️ **Two copies of the same card cost the same to throw, and that is correct** — a spare is a standing replacement for the one the cover used — so a browser must not imply otherwise; what makes them two cards is the `CardId` key, not the price. ⚠️ **The `IAnsiConsole` benefit is still unreachable and should not be chased**: "drivable from a test" cannot follow while §2 forbids the test project referencing `BurmesePoker.Console`, and that rule is worth more. **So the pty is the console's verification, and `scripts/drive-console.py` is checked in to make it repeatable.** Build clean and warning-free, **324 passed / 0 failed** (30 new: 13 `HandViewTests`, 6 `DisplayTokensTests`, 5 `CardOrderTests`, 4 `ComputerAdviceTests`, 2 new `LayeringTests` rows). **Acceptance 3 met the hard way: five scripted pty matches across three seeds, one under `--no-hints`, every one `cmp`-identical to the same match at `HEAD`.** No new rules question — `RULES.md` stays at **rev 13**, unchanged across five packets. Amended BUILD-PLAN §2, §3.11 A2, P13.1 (a "What P13.1 found" section) and **P13.2, P13.3 and P13.4 — each inherits something already built or already closed.** |
| 2026-08-19 | — | **P13 re-planned; the browser UI and multiplayer are one track** (docs only, no code — still **294 passed / 0 failed**, `RULES.md` at rev 13). Nick asked whether a rich JS browser UI or multiplayer made more sense first, and whether they were related. **They are related through exactly one variable — where the engine runs — and concealment decides it in advance.** `BUILD-PLAN.md` **§3.10**: a hand is fully concealed with money on it, so an engine in the browser holds every player's hand in every player's client; that is a security property, not a courtesy, and it is not retrofittable. **Engine server-side, Blazor Server, no WASM engine ever**; a browser client is a `RemotePlayerAgent` (§3.6 finally cashed), and **solo browser play is multiplayer with one connection** — there is no single-player client to build and replace. ✅ **The framework choice buys one concrete thing: P13.2 has no protocol to design**, so the "server" is a seat and a fan-out, testable in-process with no sockets. **§3.11** is new too — **seventeen UX standards taken before a single component exists**, split by how each is *checked*: **five are mechanical tests** (the concealment leak; colour never alone; contrast computed in both themes; real controls, no clickable divs; subscriptions disposed — and never `StateHasChanged` in `Dispose`) and land in P13.2/P13.3 before anything can be retrofitted; the rest are reviewed by playing, as P11 was. ⚠️ **The irreversible item is C12** — static SSR by default, `@rendermode InteractiveServer` per component and never at the root. ⚠️ **P13.1 was replaced rather than renumbered:** the old one ("lift the front end off `AnsiConsole`") was right for a second *Spectre* front end and a Razor client is not one — the cut line is **decision versus drawing**, so P13.1 is now an extraction into a fifth project, **`BurmesePoker.Presentation`** (Domain only, no rendering technology, one new `LayeringTests` row). Alternatives to the fifth project are recorded in §2 so it can be argued with. **P13 re-split three ways → five, strictly sequential**, each ending green and more playable than the last: P13.1 view model → P13.2 table server → **P13.3 a table you can watch** (first UI) → **P13.4 a seat you can play** (solo browser play, a legitimate stopping point) → P13.5 the lobby. Amended BUILD-PLAN §0, §2, §3.10 (new), §3.11 (new), §4 (graph + six table rows) and §7 (three new risk rows, and the scope-growth row updated for the longest sequential chain in the plan). |
| 2026-08-19 | P16 | ☑ Done. **Does the player before you decide your game? No — not between players who are both thinking.** 🔥 **The answer, with an interval and a control: upstream skill is worth `+9.1 ± 2.1` points of win rate across the `random`-to-`greedy` gulf and `−1.0 ± 2.1` across the `simple`-to-`greedy` gap.** A weaker player *anywhere* at the table is worth 4–5 points to you; **which side of you they sit on is worth nothing** unless they are not really playing. ⚠️ **The packet had to fix the harness before it could ask the question**: `SimulationOptions.Seating` rotates one pattern, so *(my strategy, the one feeding me)* was **perfectly confounded** — the cell was never played, at any run size. `SimulationOptions.Assignments` (opt-in; null keeps the rotation) plus `SeatingPlan.{Balanced, Rotations}` fixes it, and `CsvReport` gained **`upstream_strategy` and `downstream_strategy`**, derived from the *seating* and never from the options — which is why P14's replay identity survived untouched (one test, `JournalReplayTests.Outcomes`, had to blank three name fields instead of one). **The design:** focal `greedy`, filler `simple`, the ladder in one varied seat, **two arms — varied *before* the focal seat and varied *after* it, seating the identical four strategies** — 4,000 games a cell, 8 cells, two seeds, **64,000 games**; each cell cycles the four rotations of its pattern so the focal seat opens exactly a quarter of the time. ⚠️ **The downstream control moved the answer by a factor of two**: the gross upstream effect of `random` is +19.4 points and the same swap downstream is +10.3, so without the second arm the packet would have reported 19.4 and been wrong. 🔥 **It also corrected P15's own headline**: the 5.4-point ladder gap is a *neighbour* effect, not an *upstream* one — a rotation moves both neighbours at once. ⚠️ **The mechanism barely moves**: across a contrast worth 9.1 points of win rate, `takes` moves **1.6 ± 0.9** — so upstream skill changes *what* is offered, not *how often* something worth taking is, and a rich journal is what would say more. **The intervention, predicted in advance by P15, came back null**: `cautious` upstream costs the focal seat `−0.3 ± 1.4` points and costs `cautious` nothing (31.8% against greedy's 31.3% in the same seat). **P12's headline re-run at 8,000 games: 30.7%/19.3% rotated — reproduced exactly — against 29.6%/20.4% balanced, so the rotation flatters greedy by 1.1 points a seat, about a fifth of the gap.** Four new files in Sim (`SeatingPlan`, `Measurement`, `NeighbourExperiment`, `NeighbourCsv`), a `neighbours` verb and a `--seating balanced` flag; **`BurmesePoker.Domain` unchanged, and so are `Simulator`, `GameRunner` and `Replay`.** Build clean, **294 passed / 0 failed** (16 new: 9 in `Sim/NeighbourExperimentTests` — one of them the interval arithmetic itself — 6 in `Sim/SeatingPlanTests`, and 1 in `Sim/SimulationTests` for the two new columns). No new rules question — who you sit next to is not a rule, so `RULES.md` stays at **rev 13**. Amended BUILD-PLAN §0, §4, P12's caveat, P16 (a "What P16 found" section) and §7 (the seating-artifact risk retired, with its size measured). |
| 2026-08-19 | P15 | ☑ Done. **A skill ladder — four rungs, and only three skill levels.** Domain gained `Agents/RandomBotAgent` (the floor: legal moves, no thought, ⚠️ **a `Random` handed in and never `Random.Shared`** — `SeedSequence.SeatSeed(gameSeed, seat)` derives it, so a run is still a pure function of its master seed and two random seats do not play in lockstep) and `Agents/CautiousBotAgent` (greedy, plus a last-resort tie-break towards the card least useful to whoever picks it up). `CoverScore` grew the **shared discard loop** every rung throws through, so simple/greedy/cautious now differ in *one function argument* and nothing else — a refactor verified by the 259-test baseline staying byte-identical. `Strategy.Create` became `Func<int, IPlayerAgent>`; `StrategyCatalog` is now `random, simple, greedy, cautious` **in ladder order**. **Sim gained no file.** ⚠️ **The headline is a negative result, and it is the useful part: `cautious` is not distinguishable from `greedy` — +0.48 ± 0.55 points over 32,000 head-to-head games across two seeds.** Head to head at four seats: random 0.1% vs greedy 49.9%; simple 18.5% vs greedy 31.5% (a confirmation of P12's 30.7/19.3 at twice the games); simple 18.4% vs cautious 31.6%. **Why: denial and self-interest coincide.** The partners a hand holds are exactly the ones an opponent cannot hold, so both natural ways of measuring "least use to them" reduce to `Supply(rank) − Potential` to within a point — and ⚠️ **every *pairwise-additive* tie-break is greedy again**, because partnership is symmetric. A rung above greedy has to be combinatorial (live outs), which costs ~100× a decision — a packet, not a tie-break. 🔥 **And an accident worth more than the packet: in the four-way run the rungs came out random 0.1%, simple 27.9%, cautious 33.3%, greedy 38.7% — 5.4 points between two strategies that are level head to head, and the rotation feeds greedy from simple and cautious from greedy in every game. All of it is who fed whom.** Build clean, **278 passed / 0 failed** (19 new: 10 in `Agents/SkillLadderTests`, 9 in `Sim/SkillLadderRunTests`). ⚠️ **Added `WallClockBudgets.cs`** — the two timing-budget tests began failing on a *quiet* machine because the new ladder tests run simulations beside them, so the heavy classes and the budgets now share one xunit collection and never run concurrently; **neither budget was loosened**. No new rules question — `RULES.md` stays at **rev 13**. Amended BUILD-PLAN §0, §4, P15 (a "What P15 found" section) and **P16 (three amendments: the lead, a much weaker intervention than it assumed, and three usable skill levels rather than four)**. |
| 2026-08-19 | P14 | ☑ Done. **Game journals — record and replay.** The tree's first persistence layer, and it is a *format* rather than a store. Domain gained `Play/{GameJournal, JournalFormat}` — pure record types plus JSON Lines in and out, `IEnumerable<string>` exactly as `CsvReport.Rows` already did — and `Agents/{JournalingAgent, JournalPlayerAgent}`, a decorator that writes down every answer and a seat that answers from a file. **`RoundEngine` and `MatchEngine` are byte-for-byte unchanged**: replaying is playing the game with different seats, so no second engine and no resumable state machine. Sim gained `Replay` (which reuses `Simulator.Summarise` and `GameRunner`'s row builder) and `JournalReport`; both front ends gained `--journal <path>` and `--fidelity thin|rich`, and the harness a `replay` verb. **The headline acceptance is a `diff`: a 20-game, 40-round journalled run and its replay produce byte-identical CSV.** ⚠️ **The console now draws two `Random`s from `--seed`** — one to seat the table, one for the match — because a journal reproduces the deal by re-seeding the match's generator, and the old single generator had the seating consuming from it first; **a pre-P14 `--seed` no longer plays the same console match**, and two runs at the same seed are still byte-identical to each other. ⚠️ **Rich fidelity costs nothing measurable, which §3.9 expected to be false**: 400 games serially, three interleaved repetitions, **46–49 rounds/s with no journal, 48–49 thin, 48–50 rich** — a thirteen-`CardId` copy is tens of nanoseconds against a 140 µs cover search. **The expensive axis is bytes** (9.6 KB a round against 5.0), so rich stays opt-in for what it costs to keep. Divergence is loud three ways — journal exhausted, wrong question, card not in hand — each with a clean CLI message. Build clean, **259 passed / 0 failed** (20 new: 14 in `Play/GameJournalTests`, 6 in `Sim/JournalReplayTests`). Verified a console match through a pty and replayed it under the harness to the same two rounds. **No new rules question — `RULES.md` stays at rev 13.** Amended BUILD-PLAN §0, §2, §3.9, §4, P14, **P15 (a new acceptance: every rung journals and replays) and P16 (rich journals are now affordable; ⚠️ a new CSV column derived from `SimulationOptions` rather than from the seating would break replay identity)**. |
| 2026-08-19 | — | **Persistence answered and three packets added** (docs only, no code — still 239 passed / 0 failed). The tree has **no persistence layer**: `CsvReport.WriteTo` is its only write to disk, and that is an outcome table. `BUILD-PLAN.md` **§3.9** records why that has been fine (a bot game is a pure function of its seed — P12 proved it byte-identical) and why it stops being fine: **a person is not a function of a seed, and a seed only replays against the code that produced it.** **P14** — game journals, as record types plus a journalling decorator and a replaying agent over `IPlayerAgent`, with the format in one place and file writing left to the consumers; replay is *a seat that answers from a file*, not a resumable engine, and the rich fidelity level is opt-in because §3.7 measured this work allocation-bound. **P15** — a skill ladder, ≥4 separated strategies including a `RandomBotAgent` (⚠️ must take a seeded `Random`, never `Random.Shared`) and a `CautiousBotAgent` that throws what least helps the seat it feeds. **P16** — the upstream-neighbour hypothesis, raised by Nick's friend: *the skill of the player before you is what decides your game.* Well-posed, because `RULES.md` §5 makes a table a directed cycle; **a strategy question, not a rules question, so `RULES.md` is untouched at rev 13.** ⚠️ **The finding of the session: `SimulationOptions.Seating` cannot answer it** — it rotates one fixed pattern, so at two strategies and four seats *(me, upstream)* is perfectly confounded, every greedy fed by a simple. That also puts a caveat on P12's 30.7%-vs-19.3% headline, which P16 owns separating. Also added `docs/PLAYING.md`, a player-facing guide to solo play, and listed it in CLAUDE.md's documentation map. Amended BUILD-PLAN §3.9 (new), §4, P12, P14–P16 (new) and §7 (two risk rows). |
| 2026-08-18 | P11 | ☑ Done. **Console UX pass — the terminal is the UI, so this is the UI (§0).** Five new presentation files and **not one line of Domain or Sim changed**. `RoundLog` fixes the sorest thing in the console: the per-turn concealment clear used to destroy every public thing said while a player was away, and with bots those turns pass in milliseconds — `ConsoleObserver` now says each line *and* files the same markup, and the panel is drawn above the table and the hand. `HandView` shows a hand as the melds it nearly is plus its deadwood, off one `PartialCover.Best` call, and prices every card by `covered(13) − covered(12)`. **The discard hint is `GreedyBotAgent`'s own answer**, asked of the very `TurnContext` in hand, so it cannot drift from how the table actually plays; `--no-hints` turns it off. `PacedAgent` — a decorator, **deliberately not a sleep in the bot**, which would have sat inside P12's hot loop — makes computer seats wait once per `(Round, TurnNumber)`. `Palette` gathers P8's three files' worth of ad-hoc colour into one language. A difficulty prompt (`SimpleBotAgent` vs `GreedyBotAgent`) came free out of P12. **Every match is seeded and says so**: one is drawn if `--seed` is absent, and the seating is taken from the match's own `Random` — two runs at `--seed 99` are byte-identical, `--seed 100` is not. ⚠️ **Found by playing, not by building:** `Palette.Legend` shipped with an unbalanced markup tag, compiled clean, passed every test, and threw on the first hand drawn. Build clean, **239 passed / 0 failed** (4 new — `LayeringTests`, the one mechanical check a console packet allows: Domain references neither Spectre nor `System.Console`, Sim references no Spectre). Verified through a pty: **13+ rounds at four seats**, a six-seat table for the reshuffle narration, and seed 1 for the opening turn, which is the only one that offers the money-card claim. No new rules question — `RULES.md` stays at **rev 13**. Amended BUILD-PLAN §0, §2, §3.5, P11 and **P13 (split into three sub-packets, with what P11 proved about the seams)**. |
| 2026-08-18 | P12 | ☑ Done. **Simulation at scale.** A fourth project, `BurmesePoker.Sim` (Domain only): seeded games run in parallel, a strategy per seat rotated by game, per-round rows carrying their own join keys, and a CSV writer. Per-game seeds are `SplitMix64(master, index)` so a game is the same game however the run was scheduled — **serial, parallel and two-thread runs are byte-identical**. The turn cap lives in a `SeatRecorder` decorator over `IPlayerAgent` and **reports** abandonment rather than dropping it, because the domain will not invent a rule the game does not have. Domain gained only `Agents/CoverScore` (extracted, so the bots' scoring cannot drift) and **`Agents/SimpleBotAgent`** — the greedy bot with the discard tie-break removed and nothing else changed. **The measured answer: greedy takes 30.7% of 2,000 four-seat rounds against simple's 19.3%, +$1.24 a round against −$1.24** — P10's claim about the tie-break, measured. Build clean, **235 passed / 0 failed** (10 new), including determinism, per-round money conservation, a reflection pin on mutable static state, and the abandonment path. ⚠️ Measurement pass first, as the packet required: `PartialCover.Best` **140 µs**, `TryFindCover` **91 µs**, a round **~20 ms**, **51 rounds/s serial and 85–92 parallel**; **nothing was optimised**, but the work turns out to be **allocation-bound** — the server GC took eight-thread scaling from 25% to 70%. ⚠️ Two findings: **the reshuffle is a six-player phenomenon** (0/3/67 reshuffles per 300 rounds at 4/5/6 seats), and the **turn cap has never fired in a real run**. `RULES.md` → **rev 13**: §4.3's `DERIVED` 40% side-bet estimate measured at 42% over 600 five-player rounds — a confirmation, not a rule change, and no new question raised. Amended BUILD-PLAN §0, §2, §3.7, §4, P11, P12, P13 and §7 (two risk rows retired). |
| 2026-08-18 | P10 | ☑ Done. **Solo play.** `Melds/PartialCover` — the evaluator's search with one extra branch (give a card up and move on), memoised on `(position, covered)` and short-circuited on a complete cover — and `Agents/GreedyBotAgent`, whose entire strategy is *of the thirteen I would keep, how many meld?* asked of the discard, the claim and every candidate throw. `Melds/MeldIndex` extracted so both searches share one candidate index; `HandEvaluator` rewritten over it and the 208-test baseline re-run before anything else changed. **Money is absent from every decision** (RULES.md §4.4) except the take tie-break, which favours the deck because a blind draw confers ownership. `Program` asks *"how many of you are people?"* and fills the rest with named bots. Build clean, **225 passed / 0 failed** (17 new). ⚠️ **Termination measured, not assumed**: twelve seeds × 4–6 players, every round finished in 21–30 turns at ~40 ms a round. Verified the console by playing **20 rounds** against bots through a pty. No new rules question — `RULES.md` stays at rev 12. Amended BUILD-PLAN §0, §2, §3.7, P11, P12, P13, §4 and §7 (the "never-improving strategy" risk retired; the hot loop re-aimed at `PartialCover`). |
| 2026-08-18 | P9 | ☑ Done. `MatchEngine` — repeated rounds, banks carrying over, no automatic end — returning a `RoundRecord(RoundResult, TableState)` per round and keeping no history. Deck exhaustion now **reshuffles inside `RoundEngine.TakeCard`**: every discard pile gathered and shuffled into a new draw pile, the turned-up cards left alone, ownership held by whoever acquired the card first (`CardOwnership.TryRecordFromDeck`). `RoundResult.Turns` added; `IGameObserver.DiscardsReshuffled` added; `RoundEngine` now **requires** a `Random`. ⚠️ Found and fixed a real concealment bug the match loop exposed — `SpectrePlayerAgent` compared turn numbers alone and so skipped its screen clear on turn 1 of every round after the first; `TurnContext` gained `Round`. `Program` now loops rounds, asks *"another round?"* and prints standings. Build clean, **208 passed / 0 failed** (16 new, 1 removed — a passive round no longer terminates, so the old exhaustion test would hang). Verified the console by driving two full rounds through a pty. Rules defaults taken for `RULES.md` §9 #4 and #5, and new #14 raised (rev 12). Amended BUILD-PLAN §2, P10, P11 and P12. |
| 2026-08-18 | — | **Statistics added as a design constraint** (doc-only, no code). `BUILD-PLAN.md` **§3.8**: the domain gains no notion of a statistic, and everything a strategy comparison wants is derived by the consumer from three seams — the observer stream, the per-round `(RoundResult, TableState)` pair, and a **recording decorator over `IPlayerAgent`** for anything decision-level (which needs no domain change and serves human replay too). Four constraints recorded, the sharpest being that ⚠️ **P9 must surface each round's table or two of the five stat families become unreachable**. P9 also gains `Turns` on `RoundResult`. §3.5 now says the observer event set is open but **hot** — events pass what the engine holds and never allocate. P12's build list rewritten against §3.8; §0, CLAUDE.md and the `/poker` skill's stale P0 baseline exception brought current. Build clean, **192 passed / 0 failed** — unchanged. |
| 2026-08-18 | — | **Roadmap extended** (doc-only, no code). Nick named four goals beyond a playable game; written up as `BUILD-PLAN.md` **§0**, with **§3.6** (agents stay synchronous — a remote player blocks in the agent, one table is one task) and **§3.7** (simulation is a first-class consumer: determinism ✅, no I/O ✅, no mutable statics ✅, speed ⚠️ unmeasured) taken now rather than discovered later. **P10 promoted out of "optional" and rewritten** — its "never discard an owned money card" heuristic contradicted §4.4 and is corrected, bots move to `Domain/Agents/` so they are testable and reusable, and the scored partial cover P5 left unbuilt is specified here. Added **P11** (console UX), **P12** (simulation at scale), **P13** (multiplayer). §4 graph and §7 risks updated. Build clean, **192 passed / 0 failed** — unchanged, nothing was built. |
| 2026-08-18 | P8 | ☑ Done. `CardFormatting`, `SpectrePlayerAgent`, `ConsoleObserver` and a `Program` that asks for players and stakes, randomises the seating, plays a round and reports the settlement split into its round and money-card halves. Hotseat concealment: clear and hand over the keyboard once a turn; blind draws are narrated without the card. Spectre.Console 0.57.2 added back. **No Domain change, so no new tests — 192 passed / 0 failed**, build clean; verified manually by driving the real binary and a rigged winning deal through a pty. Amended BUILD-PLAN P9 (the console's settlement report needs each round's `TableState`, so `MatchEngine` must surface it; between-round "stop playing" is the console's to ask) and P10. |
| 2026-08-18 | P7 | ☑ Done. `TurnAction`, `PlayerState`, `TableState`, `TurnContext`, `RoundResult`, `RoundEngine`, `IPlayerAgent`, `IGameObserver`. A round deals from a validated draw order (so it is scriptable), turns up bottom-then-top, offers the claim on the opening turn only, records ownership on deals and blind draws alone, discards before revealing, and settles on a declaration. Build clean, **192 passed / 0 failed** (18 new), including the four acceptance tests — expected settlement, 13/14 hand sizes, 108 distinct cards at every event, and a claim that grants no ownership. Raised `RULES.md` §9 #12 and #13 (rev 11), both defaulted. Amended BUILD-PLAN §2, §3.5, P8 (what the engine asks the console) and **P9 (the reshuffle must go inside the engine — catching `Play()` cannot resume a round)**. |
| 2026-08-18 | P6 | ☑ Done. `Stakes` (sealed record, positive-only, `Standard` = $5/$1) and `Settlement.ForRound` → per-player net deltas: flat round value from every loser to the winner, then each **owned** money card paying its owner `multiplier × money card value` from every other player. Walks ownership records and is never given a hand — pinned by a reflection test on the parameter list. Resolves an owned `CardId` through the **unshuffled** shoe by index and rejects a shuffled one outright. Build clean, **174 passed / 0 failed** (26 new), including the §4.3 worked example and a 500-round zero-sum property test. Amended BUILD-PLAN §2, P7 (keep the builder list; one roster; a round always has a winner), P8 (net deltas only) and P9 (conservation is a banking test). |
| 2026-08-18 | P2 | ☑ Done. `MoneyCardRegistry` (pure function of the turned-up cards; permanent 7♦/A♠ as negative-id value designators; multiplier is permanent + turned-up, so doubling is the overlap and its own ceiling) and `CardOwnership` (append-only, write-once, no transfer/clear/remove — enforced by a reflection test). `PlayerId` brought forward into `Play/`. Build clean, **148 passed / 0 failed** (29 new). Re-planned P6: `Records` is keyed by `CardId` while `Multiplier` takes a `Card`, so settlement needs the shoe passed in — `DeckBuilder.BuildTwoDecks()` is index-aligned, `Deck.Cards` is not. Amended BUILD-PLAN §2, P2, P6 and P7. |
| 2026-08-18 | P5 | ☑ Done. `MeldCandidates.For` (runs, then the sets no run already consumes) and `HandEvaluator.IsWinning` / `TryFindCover` — backtracking pinned to the lowest uncovered card, candidates indexed by their lowest card, coverage carried as a bitmask so dead ends memoise. Build clean, **119 passed / 0 failed** (19 new). Found that the joker-substitution acceptance hand has to be built from a set rather than a run, and that `TryFindCover`'s cover is not canonical; amended BUILD-PLAN P5, P8, P10 and the §7 risk table. |
| 2026-08-18 | P4 | ☑ Done. `SetGenerator` — one walk over the four suits per rank, each taken as a held card, a specific joker, or nothing; de-duplicated by card set. Duplicate suits impossible by construction, so a set is at most four cards. Build clean, **100 passed / 0 failed** (18 new), including a brute-force cross-check over every subset. Measured the worst case at 639 candidates and amended the §7 risk row. Re-planned P5: the two generators collide on any meld with ≤1 real card, so `MeldCandidates.For` must de-duplicate across them. |
| 2026-08-18 | P3 | ☑ Done. `MeldSlot`, `Meld` (identity is its `CardId` set) and `RunGenerator` — window-based generation with joker substitution, jokers chosen as combinations. Reference hand yields the specified **5** candidates. Build clean, **82 passed / 0 failed** (22 new). Corrected two counts in `docs/spec/RUN-CANDIDATES.md` (76, not 77; 4,032, not "hundreds"), widened `RULES.md` §9 #8 to cover all-joker melds (rev 10), and re-planned P4 and P5 around the shared `Meld` vocabulary. |
| 2026-08-18 | P1 | ☑ Done. `CardId`, `Card` (record struct: `==` is instance identity, `SameValueAs` is value identity), `DeckBuilder.BuildTwoDecks()` → 108 cards with sequential ids, `Deck` (draw from either end, Fisher–Yates shuffle), `DeckExhaustedException`. Build clean, **60 passed / 0 failed** (32 new). Raised `RULES.md` §9 #11 (turned-up joker) and amended BUILD-PLAN P1, P2 and P7. |
| 2026-08-18 | P0 | ☑ Done. Tagged `pre-rewrite`, then deleted `Models/`, `Logic/` and `Common.cs`. Solution restructured to Domain/Console/Tests. Salvaged the enums and display tables into `Cards/{Rank,Suit,CardColor,CardText}` and `Melds/MeldKind`. Build clean, **28 passed / 0 failed**. Amended P0's acceptance (tests, not zero tests) and P3's "Done when" (5 candidates, not 8). |
| 2026-08-18 | — | Rules reconstructed from a codebase abandoned in 2023. `RULES.md` reached rev 8 with all blocking questions closed. Rewrite decided (`BUILD-PLAN.md` §1); 11 packets defined. `docs/spec/RUN-CANDIDATES.md` written, correcting P3's acceptance count from 8 to 5. `/poker` skill created. No code written. |
