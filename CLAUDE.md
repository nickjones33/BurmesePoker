# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## ⚠️ START HERE

**Run `/poker`.** It encapsulates one full work cycle: orient from the plan, execute the next
build packet, update the docs, re-plan what follows, commit, and report. Defined in
`.claude/skills/poker/SKILL.md`. It is the intended way to work on this project — prefer it
over ad-hoc changes.

**Every packet in the plan is done — P0–P12, P13.1–P13.6 and P14–P23.**
🔥 **P24 is the next one, planned 2026-08-20 and not started**: *the computer's reasoning, said
out loud* — the browser's hint arrow grows a **why**, and the hosted table gains a journal that
records **where an expert disagreed with it**. Browser only, all four questions, winner versus
runner-up. ⚠️ **Two things a cold session must know before opening it**: `BurmesePoker.Web` and
`BurmesePoker.Server` contain the string `journal` **zero times**, and what gets recorded is an
**opinion beside an answer** rather than a rationale on a decision — which is what makes
disagreement a query instead of a transcription job. See `BUILD-PLAN.md` §5 P24.
🔥 **The plan is no longer the whole of the work.** A playtest with Mya Lay on **2026-08-20**
produced a rule the game does not implement: **`RULES.md` §5.1, the feeding ban** — *you may not
discard a rank the next player has taken in the open*, until they throw that rank back (which
frees it for the rest of the round, permanently) or you are going out on it. **Rank only; suit is
irrelevant.** It is `EXPERT` and **Settled**, and it is **the first rule in the document that
constrains which card a player may discard** — `RoundEngine` accepts any of the fourteen.
🔥 **It is enforced by construction: a banned card is not an infraction but an impossible move**,
never offered and unchoosable, so there is no penalty. That is free of the concealment model (every
fact the ban is computed from is already public) but it means `TurnContext` must carry the banned
ranks and **every agent's discard ranking must be filtered to legal cards — `FallibleAgent`'s
runner-up included**, because a mistake still has to be a legal move.
🔥 **And it has a floor, which is the line that makes impossible-move enforcement safe**: where the
ban would leave a player **no legal discard at all**, the ban **yields for that turn**. So the legal
set is *the hand minus the banned ranks, **or the whole hand if that is empty*** — **never empty by
construction**, and a turn cannot deadlock. The discard is mandatory (§7.1); the ban is not. ⚠️ It
is **not** the unrecovered exception to the mandatory discard in §9 #6 — it is the reverse, and #6
is still open.
⚠️ **Do not write the packet yet.** Six details are unrecorded (`RULES.md` §9 **#16–#19, #25 and
#27**) and,
unlike the rest of §9, they are not fidelity questions with safe defaults — they are the rule's
specification. `QUESTIONS-FOR-MYA-LAY.md` **Q4** carries them for the person who raised the rule.
🔥 **And the ban matches on *rank alone* — a third identity notion the domain has not got.** `==`
on `Card` is instance identity; `SameValueAs` is rank **and** suit **and** colour, which is what
§4.2's money designation needs. **Reaching for `SameValueAs` here implements the wrong rule** — it
would leave the Q♣ Mya Lay actually objected to perfectly legal.

**All five of §0's goals are delivered**: the 2023 implementation is deleted, the
whole rules core is built and tested, `dotnet run --project BurmesePoker.Console` fills the empty
seats with paced, named bots and plays round after round with the banks carrying over,
`dotnet run -c Release --project BurmesePoker.Sim` plays thousands of seeded games in parallel to
compare strategies, and `dotnet run --project BurmesePoker.Web` is **a browser lobby you sit down
in and play other people at**.

✅ **A fifth goal was stated on 2026-08-19: a designed difficulty
system, and a settled answer to what actually works.** That is **P17–P23**, and **all seven are
done as of 2026-08-20.** It is two jobs kept apart on
purpose — a *product* (difficulty as a table setting, per seat, in both front ends: **finished in
P19**) and a *programme* (analysis and simulation enough to say which ways of playing are better,
by how much, and with an interval).
⚠️ **Read `BUILD-PLAN.md` §3.12 first**: *difficulty is a dial, skill is a ladder, and they are
not the same axis* — which is what keeps the difficulty menu from being a list of research
instruments, and what makes the product independent of whether any given rung is worth anything.
✅ **The product half is finished.** `Domain/Agents/DifficultyLadder.cs` holds four levels —
`easy`, `medium`, `hard`, `expert` — each the strongest rung there is with a **measured mistake
rate** (0.9, 0.7, 0.4, 0.0), and both front ends offer **levels only**. See §9 of
`docs/STRATEGY.md` for the calibration.

🔥 **Two facts set the order, and both are now discharged.** The ordinary `Sim` report used to
print no interval at all, so P17 — statistics and ranking — came before P19 — calibration. And
there were **four independent notions of *which bot*** across Sim, Console, Web and Server, so
P18 made it one catalog before any new rung is written: **`Domain/Agents/BotCatalog.cs` is the
only place a bot is named**, `LayeringTests` fails the build if any project outside
`Domain/Agents` constructs a rung, and a new rung reaches the console prompt, the browser lobby
and the harness with no front-end work at all.
⚠️ **P19 finished the difficulty product with today's rungs.** P15 spent a whole packet on a
plausible rung worth +0.5 ± 0.55 points and P20 spent another and got less — `counting` is
`+0.3 ± 1.0` the *wrong* way — **so two of the first three research rungs returned nothing.**
🔥 **P21 is the third and it separated**: `outs` beats `greedy` by **`+3.1 ± 1.0`**, and the
difference between it and the two nulls is *where the new key went* — above greedy's tie-break
rather than beneath it. 🔥 **A new rung is not free of the dial, and this is the day that bill
came due**: every level is `BotCatalog.Hardest` with an ε, `outs` is `Strength: 3`, so all four
levels are `outs` now and the ε values had been spaced against `greedy`. ✅ **P23 re-fitted them,
and exactly one moved — `hard`, 0.5 → 0.4.**
🔥 **P22 is the fourth and it asked a different question**: `prospector` is judged on `$/round`
rather than on win rate, and the answer is **a function of the stakes**. At $5/$1 its rule never
fires and it is literally the same player as `outs`; at $5/$40 it wins **20 points fewer rounds**
and banks **`+7.3 ± 3.3` a round**. ⚠️ **The dial did not move** — `prospector` shares `outs`'
`Strength`, so `BotCatalog.Hardest` is unchanged and no front end needed a line — but it left
**`docs/strategy/measurements.csv` a rung behind the catalog**. ✅ **P23 caught it up**, and paid
the bill structurally: `BotRung.Ranked` has each rung declare whether **win rate** or **money**
settles it, so the ladder tournament measures one set and the money sweep the other, and
`prospector`'s six duplicate cells are gone without anybody typing a shorter field.

⚠️ **Before touching the browser client, read `BUILD-PLAN.md` §3.10 and §3.11.** The engine runs
**server-side, always** (a hand is fully concealed with money on it, so a client-side engine cannot
honour that, and it is not retrofittable), the client is **Blazor Server**, and §3.11 fixes
seventeen UX standards — several of them mechanical tests — that a component either obeys or
breaks a test.

**The P13 sub-packets, in order, and the finding from each that a cold context needs.**

- ✅ **P13.1 — a presentation view model.** `BurmesePoker.Presentation`; the console renders a view
  model it did not build, byte-identically at the same `--seed`. 🔥 **A view model that aliases the
  engine's hand list is not a view model** — everything handed to a seat is a **snapshot**.
- ✅ **P13.2 — the table server.** `BurmesePoker.Server`: a hosted table, a remote seat, a bot
  stand-in, a filtered fan-out, **and no transport at all**. 🔥 **Exactly one event in the whole
  narration is private (the blind draw), so the security boundary is one `if`** — and
  `ConcealmentTests` shipped with it, mutation-tested.
- ✅ **P13.3 — a table you can watch.** `BurmesePoker.Web`, Blazor Server. 🔥 **`UseStaticFiles`
  does not serve `_framework/blazor.web.js`, so the page rendered perfectly and was dead** — a
  prerendered Blazor Server page is a photograph of a broken one, so **ask the server for every URL
  the page names**. `MapStaticAssets` is the fix.
- ✅ **P13.4 — a seat you can play.** 🔥 **A `CardId` names a card in a *round's* shoe**, which is
  rebuilt every deal, so anything comparing hands across seats compares them a round at a time; **a
  refusal must not raise "something changed"**; and **only the first control may capture a `@ref`**,
  because Blazor captures on insertion rather than on every diff.
- ✅ **P13.5 — a table, not a document.** Seats at positions on a felt (`TableRing`), **you at the
  front whichever seat you were dealt**, one action bar, a round log you open. 🔥 **A focus call can
  kill the circuit** — an `ElementReference` outlives its element and Blazor turns an unhandled
  interop exception into a torn-down circuit, **which is a page that looks perfect and does
  nothing**. Also: **whose turn it is was made public on purpose** (`TableEvent.TurnBegan`), **a
  hidden live region announces nothing**, and **a glyph is not automatically better than a word**.
- ✅ **P13.6 — the lobby, and a second person.** `Lobby` holds `HostedTable`s by id; `/` is the
  lobby and `/table/{id}` is one table; **two people and two bots play a round over a network**.
  🔥 **A `SeatBoard` belongs to a viewer, not to a host.** 🔥 **The table deals while somebody is at
  it** — a viewer attending *and* every seat either the computer's or somebody's — which is how
  P13.4's "an unanswered seat spends its whole patience on every question" got an honest fix
  **without shortening the patience**. 🔥 **Two `<AntiforgeryToken />`s is worse than none**:
  `EditForm` emits one itself, the second made every post fail, and the page rendered perfectly
  either way — **found by pressing the button**. 🔥 **A test that a stood-up seat refuses an answer
  is vacuous unless a question is standing in front of it** — found by mutating `Dispose` and
  watching the test stay green. ⚠️ **`--seat` is gone; `--people` replaces it**, because a lobby
  seats you.

**P14–P16 were added on 2026-08-19 and all three shipped the same day.** P14: `--journal <path>` on
both front ends writes every decision every seat made as JSON Lines, and
`BurmesePoker.Sim -- replay <path>` plays it back — to a byte-identical CSV. A seed is a pointer
into the build that produced it; a journal is the record (§3.9). P15: `--strategies
random,simple,greedy,cautious` is a skill ladder with **four rungs and three separated skill
levels** — `cautious` is indistinguishable from `greedy`, because denial and self-interest point
the same way. 🔥 **P16 answered the question the other two were built for, and the answer is no.**
`BurmesePoker.Sim -- neighbours` runs a focal seat against a skill dial in the seat before it **and
a control arm with the dial in the seat after it**: upstream skill is worth **`+9.1 ± 2.1` points**
of win rate across the `random`-to-`greedy` gulf and **`−1.0 ± 2.1`** across the gap between two
thinking players. A weaker player *anywhere* at your table is worth 4–5 points to you; **which side
of you they sit on is worth nothing.** ⚠️ P16 also fixed the seating scheme it needed —
`--seating balanced` and two new CSV columns — and **re-measured P12's headline: 30.7%/19.3%
rotated against 29.6%/20.4% balanced.**

Whether or not you
use the skill, read these first:

1. **`docs/STATUS.md`** — which work packet is next, and the state of the tree. Update it at
   the end of every session.
2. **`docs/BUILD-PLAN.md`** — the rewrite plan. **§0 where the whole thing is heading**, §2
   target architecture, §3 settled design decisions, §5 self-contained work packets, §6
   cold-start protocol.
3. **`docs/RULES.md`** — **the only rules authority.** Every rule is tagged with provenance
   and confidence.
4. **`docs/STRATEGY.md`** — **the only measurement authority.** Which way of playing is better,
   by how much, and with an interval. ⚠️ **Never quote a strategy number from a session log or
   from this file's prose** — quote `docs/strategy/measurements.csv`, which `sim suite`
   regenerates and which two runs of one seed write byte-identically.

`BUILD-PLAN.md` **§0** records where this is heading beyond a playable game — solo play against
the computer, a console worth sitting at, strategy simulation at scale, and a multiplayer app
with AI seats. **§3.6, §3.7, §3.8 and §3.9 are the design constraints those goals impose**, taken
in advance: agents stay synchronous, simulation is a first-class consumer, statistics are
collected by consumers rather than computed by the domain, and a seed is a pointer while a
journal is the artifact. All four have now been paid off by packets that needed nothing from the
engine — **P11 shipped a whole UX pass without changing a line of the domain, P14 added
record-and-replay without changing `RoundEngine` or `MatchEngine` at all, and P16 ran a
controlled experiment without changing `Simulator`, `GameRunner` or `Replay`.**

**The abandoned 2023 implementation is gone.** P0 deleted it; it survives only at the git tag
`pre-rewrite`. Roughly 180 lines of enums and lookup tables from `Common.cs` were salvaged into
`BurmesePoker.Domain/Cards/`. Do **not** restore the rest, and do not treat anything at
`pre-rewrite` as a source of rules — read it only as history (`BUILD-PLAN.md` §1).

The solution is:

```
BurmesePoker.Domain/        pure rules. no I/O, no Spectre. everything new goes here.
                            (System.Text.Json is in here for the journal format — strings, not files.)
BurmesePoker.Presentation/  what a hand looks like, as data: near-melds, per-card cost, display
                            state, display order, the computer's hint. Domain only, and no
                            rendering technology at all. built in P13.1.
BurmesePoker.Server/        one table, hosted: a seat played from elsewhere, who is sitting in
                            it, a bot that stands in when nobody answers, and the fan-out that
                            decides what each viewer is told. Domain + Presentation, and no
                            transport at all. built in P13.2, extended in P13.6.
BurmesePoker.Console/       Spectre.Console front end. the only project that prints. P8, reworked
                            in P11 and rewritten onto the view model in P13.1.
BurmesePoker.Sim/           batch play: seeded, parallel, CSV out. Domain only. built in P12, P16,
                            and the experiments since — tournament (P17), suite (P19), money (P22).
BurmesePoker.Web/           Blazor Server. the second project that draws: a lobby, a table you
                            can watch and a seat you can play, folded out of the event stream and
                            the prompts your own seat was sent, and nothing else. Domain +
                            Presentation + Server. built in P13.3–P13.6.
BurmesePoker.Tests/         xunit against Domain, Presentation, Server, Sim and Web. never
                            references Console.
scripts/drive-console.py    drives the console under a pty and writes down every byte, so a
                            front-end refactor can be proved with `cmp`. built in P13.1.
```

✅ **P17 shipped 2026-08-19: the tournament.** `BurmesePoker.Sim -- tournament` ranks every
player against every other with a **paired** margin, a Holm-corrected verdict and a null cell in
which the harness measures its own bias; `-- suite` generates `docs/strategy/measurements.csv`,
which `docs/STRATEGY.md` quotes. Every figure in the ordinary report now carries an interval.
🔥 **Adding the interval moved a published number by a point** until the estimator was made a
*ratio over games* rather than a mean of per-game ratios — a strategy holds a different number of
seats in different games of a crossed run, and the two are not the same quantity. 🔥 **And
"paired is narrower" turned out to be half backwards**: across cells it narrows (0.57–0.95),
within a cell it *widens by exactly √2*, because only one seat declares — so the independent
formula was **anti**-conservative on every head-to-head margin.

✅ **P18 shipped 2026-08-19: one catalog.** A bot is named once, in
`Domain/Agents/BotCatalog.cs`, and the console, the browser and the harness all resolve the name;
the browser gained a difficulty setting it never had. 🔥 **It found a user-facing bug that had
stood since P10: a Spectre `SelectionPrompt<T>` opens on `default(T)` if that value is one of the
choices, and the console's enum was `Easy = 0`** — so the menu said *Hard* first and gave
everybody who pressed return the easy bot. ⚠️ **A console capture is only comparable with one
that made the same choice**: pass `--pick n` and compare from the `Seating:` line on.

✅ **P19 shipped 2026-08-19: difficulty as a dial, not a list.** A level is the strongest rung
with a mistake rate — `FallibleAgent` substitutes the card the rung ranked **second**, which is
why `CoverScore.Discard` is now *defined as the head of* `CoverScore.Ranking` and why
`IRanksDiscards` exists. 🔥 **ε is a far bigger dial than the whole skill ladder and violently
non-linear**: `greedy@0` beats `greedy@1` by **+33.3 ± 1.6** points, with ε = 0→0.5 worth ~8 and
ε = 0.5→1 worth ~17 — so the four levels are spaced evenly in *results*, not in ε, and all three
steps survive Holm at 8,008 games a cell. 🔥 **A `TurnContext`'s hand is the engine's own list**,
so a context kept and asked afterwards is asked about the *thirteen* — P13.1's finding arriving
in the test project, found by writing the test. ⚠️ **`--pairs adjacent`** was needed and was not
planned: a dial claims only that *n+1* beats *n*, so the family is k−1 comparisons and only k−1
cells are played.

✅ **P20 shipped 2026-08-20: memory, and the answer is no.** `counting` is `cautious` with one
substitution — what is left in the shoe estimated from every card it has been shown rather than
from its own thirteen — and it measures **`+0.3 ± 1.0` points the wrong way** against `greedy`.
**A null result, published** (`docs/STRATEGY.md` §8), which the packet demanded in advance.
🔥 **The *why* is the deliverable and it constrains P21.** The memory works, but **(1)** the
information set is tiny — **12 → 23 cards a round out of 108** under the cautious default — and
**(2)** it sharpens `ThreatScore`, which *is* `cautious`'s tie-break, already measured at
`−0.2 ± 1.0`. ⚠️ **A sharper input to a decision rule that does not matter is worth nothing, and
the two nulls compound.** The next rung must change **which question is asked**. 🔥 **It also
found the ladder written out in three places**: `tournament` and `suite` defaulted `--strategies`
to a hand-typed list, so a fifth rung was measured only if somebody named it — **the default is
`BotCatalog` now**, which is P18's defect one layer up.

✅ **P21 shipped 2026-08-20: outs — the first rung that looks ahead, and the first that beats
`greedy`.** Where two discards leave the hand equally melded, `outs` keeps the thirteen that
**more of the pack would improve**: `+3.1 ± 1.0` points head to head at 8,008 games, `p = 1.9e-09`,
surviving Holm. 🔥 **Why it paid when two rungs before it did not: its key sits *above*
`CoverScore.Potential`, not beneath it** — `cautious` and `counting` both refined greedy's
leftovers, and greedy's leftovers are worth about half a point, which is below what the harness
can resolve.
🔥 **The cost was the packet and the profile was the surprise.** Naive, it ran at 12.6× a greedy
round; four shortcuts *around* the evaluator — refine only what is tied at the top, prune values
that cannot enter a meld, ask the search for **a bar rather than a maximum**
(`PartialCover.CoversAtLeast`), and build **one meld index a candidate rather than one a probe**
(`CoverProbe`) — took it to 8.2×. **`PartialCover.Best` was not touched and `HandEvaluator` does
not know any of it exists.** Then three quarters of what was left turned out to be a **fixed
per-call allocation cost in candidate generation** — ninety window arrays × four suits every
call, whatever the hand held. Fixing that made **every rung, every hint and every engine turn
about 45% faster**, and `drive-console.py` proves it was a refactor byte for byte. ⚠️ **The
domain now has an `InternalsVisibleTo BurmesePoker.Tests`**: each shortcut is a claim about
answers and is asserted against the search it replaces.
🔥 **One finding that is not about strategy: a stronger bot is a longer round.** Promoting `outs`
broke two concealment tests from P13.2 and P13.4 that asserted four seats' hands are pairwise
disjoint over a whole round — but the round now runs long enough to **exhaust the draw pile**, so
the discards are shuffled back in (RULES.md §5) and a card one seat threw legitimately reaches
another. **Disjointness was never the property; it was a coincidence of short rounds.**
`Tests/Server/PublicRelease.cs` carries the argument. ⚠️ **The suite went from 35 minutes to 105
and the local tests from 2m to 6m 33s**, both because six rungs is fifteen head-to-head cells and
every difficulty level now pays `outs`' price.

✅ **P23 shipped 2026-08-20: the standing answer, and the last packet.** `docs/STRATEGY.md` is one
document that answers *"which bot should I play, and what actually works in this game?"* and
regenerates from one command. 🔥 **The headline is the reproduction — 59 of the suite's 77 rows
came back byte-identical**, and the seven that moved are the difficulty dial and only the dial.
🔥 **The dial was re-fitted against `outs` and exactly one ε moved** (`hard`, 0.5 → 0.4), taking
the reference table from steps of 8.2/4.3/10.3 points to **7.9/6.7/7.7** — because the ε curve has
nearly the same shape on a rung that looks ahead as on one that does not, so a mistake rate is
close to being a property of *the mistake* rather than of the rung it is made against.
⚠️ **The failure mode P21 left behind was a flat spot, not an inversion** — the dial was monotone
and passing every standing check the whole time, which is why `Tests/Sim/StandingAnswerTests.cs`
now asserts that **the ε values published are the ε values offered** and that **every rung in
`BotCatalog` is the subject of a published row**. ⚠️ **`sim suite` is now about five hours**;
budget it before adding a rung. See BUILD-PLAN §2 for how the seven projects fit together — the
strategy programme added no eighth project and, in the end, changed nothing in the engine.

## Rules of engagement

- **`docs/RULES.md` is the sole rules authority. Never infer a game rule from the code** — the
  old code contradicts the confirmed rules in several places, deliberately documented in
  `docs/RULES-TECHNICAL.md`.
- **Any new rules question goes into `RULES.md` §9 with a provenance tag.** Do not decide a
  rule silently. Provenance ranks: `EXPERT` (Mya Lay, an experienced player) > `PLAYER`
  (Nick's recollection) > `IR` (Indian Rummy, the closest documented relative) > `CODE`.
- **Every packet ends with a green build and green tests.** Never leave the tree broken
  between sessions.
- **One packet per commit**, message prefixed with the packet id — e.g. `P3: run candidate generation`.

## Commands

```bash
dotnet build                                    # build solution
dotnet test                                     # run all tests
dotnet test --filter-method "*SomeTestName*"     # single test (xunit v3 / MTP syntax)
dotnet test --filter-class "*CardTextTests*"     # single test class
dotnet run --project BurmesePoker.Console       # play a round (needs a real terminal)
dotnet run --project BurmesePoker.Web           # a browser lobby at http://localhost:5188 — sit down and play
dotnet run --project BurmesePoker.Web -- --people 1                   # …a solo table; it deals as soon as you sit
dotnet run --project BurmesePoker.Web -- --people 0                   # …just watch; every seat is a bot
dotnet run --project BurmesePoker.Web -- --seed 20260819 --pace 400   # …the same table, faster
dotnet run --project BurmesePoker.Web -- --difficulty medium          # …how hard the computer is: easy, medium, hard, expert; the lobby form offers the same list
dotnet run --project BurmesePoker.Web -- --mixed true                # …a different level in each computer seat (it takes a value, like --hints)
dotnet run -c Release --project BurmesePoker.Sim -- --games 2000   # compare strategies
dotnet run -c Release --project BurmesePoker.Sim -- bench          # time the cover searches, and every rung's decision
dotnet run -c Release --project BurmesePoker.Sim -- bench --rounds 200 --strategies greedy,outs   # …is a rung affordable? (P21's budget is 10x greedy)
dotnet run -c Release --project BurmesePoker.Sim -- --games 100 --journal run.jsonl   # keep every decision
dotnet run -c Release --project BurmesePoker.Sim -- replay run.jsonl                  # play them back
dotnet run -c Release --project BurmesePoker.Sim -- neighbours --games 2000          # does the seat before you matter?
dotnet run -c Release --project BurmesePoker.Sim -- --games 2000 --seating balanced  # every seating, not one rotated pattern
dotnet run -c Release --project BurmesePoker.Sim -- tournament --games 2000          # rank every player against every other
dotnet run -c Release --project BurmesePoker.Sim -- tournament --strategies easy,medium,hard,expert --pairs adjacent --games 8000   # calibrate the difficulty dial
dotnet run -c Release --project BurmesePoker.Sim -- money --games 8000               # should you draw blind for the money? a sweep over four stakes ratios
dotnet run -c Release --project BurmesePoker.Sim -- suite --games 8000               # regenerate docs/strategy/measurements.csv (⚠️ ~5h, measured at P23)

python3 scripts/drive-console.py --out before.raw --seed 20260819 --pick 0   # capture a scripted match (0 expert, 1 hard, 2 medium, 3 easy)
python3 scripts/drive-console.py --out before.raw --seed 20260819 --pick 0 --script human   # …the longer one, with a person in it
python3 scripts/drive-console.py --out after.raw  --seed 20260819 --pick 0   # …after a front-end change
cmp before.raw after.raw                                                    # prove it was a refactor
```

All seven projects target **`net10.0`**, matching the installed SDK (10.0.111). Tests are
**xunit v3** running on **Microsoft.Testing.Platform**, not VSTest — `global.json` opts
`dotnet test` into MTP mode, which the .NET 10 SDK requires for MTP test projects. The test
project is therefore an `Exe`, and **test filtering uses `--filter-method` / `--filter-class`,
not VSTest's `--filter "FullyQualifiedName~…"`**, which MTP rejects.

Nick's standing preference is the newest supported .NET tooling — the `.slnx` solution format
is the same call. Don't downgrade any of it back for compatibility's sake.

## What the game is

A Burmese rummy played for money — two decks (108 cards), 13-card hands, draw-and-discard,
**fully concealed** until a player melds all 13 and declares. Layered on top: certain cards
are **money cards** that pay their *owner* a per-card amount from every other player,
independently of who won the round.

No published ruleset exists for this game; `RULES.md` is a reconstruction from player
recollection, expert confirmation, and the code, cross-checked against Indian Rummy — which
matches on 8 of 12 structural features and is almost certainly the parent game.

## Three design decisions that shape everything

Detail in `BUILD-PLAN.md` §3. These exist because the old design got each one wrong, with a
verified bug to show for it.

1. **Two identity notions, both explicit.** Two decks mean value-identical cards coexist.
   `Card` is a `readonly record struct` carrying a `CardId`, so `==` is *instance* identity
   while `SameValueAs` is *value* identity. Money-card designation uses value; the exact-cover
   search uses instance.
2. **Money status is computed, never stored on cards.** `MoneyCardRegistry` is a pure function
   of the round's turned-up cards. The old design mutated `Card.MoneyCardStatus` in place,
   which produced both a non-idempotent re-marking bug and a card-cloning bug.
3. **Candidate generation is not the same question as winning.** Declaring asks whether 13
   cards *partition into disjoint melds* — an **exact-cover** problem. Meld candidates
   deliberately overlap (the same joker offered to several suits); the cover search enforces
   disjointness by `CardId`. The old `CardPlaysFactory` only ever answered the enumeration
   question, which is why it is replaced rather than repaired.

## Documentation map

| File | Purpose |
|---|---|
| `.claude/skills/poker/SKILL.md` | The `/poker` work cycle. |
| `docs/STATUS.md` | Cross-session progress. Read first, update last. |
| `docs/BUILD-PLAN.md` | The rewrite: architecture, design decisions, work packets. |
| `docs/RULES.md` | **Canonical rules.** Provenance and confidence per rule; §9 open questions. |
| `docs/STRATEGY.md` | **What actually works** — the ranking, with intervals and a corrected verdict, **§9 the difficulty calibration** and **§10 the side bet**. Every figure is generated from `docs/strategy/measurements.csv`, never transcribed, and since P23 **one `sim suite` regenerates all of it** — §10 included. ⚠️ **§11 is where "a rung cannot be added without being measured" stopped being a habit and became a test.** |
| `docs/RULES-PRIMER.md` | One-page rules recall aid for humans. |
| `docs/PLAYING.md` | **How to actually play** a solo game — the console's prompts, panels, markers and flags, and the browser table at the end of it. Written for a person at the keyboard, not for a build session. |
| `docs/RULES-TECHNICAL.md` | What the **old** code does and where it diverges. Defect list. Historical reference. |
| `docs/spec/RUN-CANDIDATES.md` | **Worked spec for packet P3**, the hardest one. Read before touching run generation. |
| `docs/QUESTIONS-FOR-MYA-LAY.md` | Open rules questions phrased for an experienced player. Answers get promoted into `RULES.md` as `EXPERT`. |
| `docs/RECONCILIATION-PLAN.md` | **Superseded.** Kept for its defect analysis only. |
