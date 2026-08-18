# Build Status

Cross-session progress tracker. **`/poker` reads this first and updates it last.**

Plan: `BUILD-PLAN.md` · Rules: `RULES.md` (rev 8) · Skill: `.claude/skills/poker/SKILL.md`

State markers: `☐` not started · `◐` in progress · `☑` done

---

## Current state

**Next packet: P0 — Restructure and salvage.** No blockers.

Nothing has been built yet. The tree still holds the **2023 implementation**, which
`BUILD-PLAN.md` §1 has decided to retire. Do not fix its bugs — it is being replaced.

⚠️ **The working tree is dirty and `docs/`, `CLAUDE.md` and `.claude/` are untracked.**
P0's first step commits and tags `pre-rewrite` before deleting anything. Run P0 before any
cleanup.

⚠️ **Expected baseline failure until P0 runs:** `CardPlays_Runs_HappyPath_Jokers` fails,
asserting 8 candidates where the corrected spec says 5 (`docs/spec/RUN-CANDIDATES.md`). This
is by design. Every other check should be green.

---

## Packets

| | Packet | Depends on | Notes |
|:-:|---|---|---|
| ☐ | **P0** Restructure and salvage | — | **next** |
| ☐ | **P1** Cards, deck, identity | P0 | |
| ☐ | **P2** Money designation and ownership | P1 | |
| ☐ | **P3** Run candidate generation | P1 | ⚠️ hardest — read `docs/spec/RUN-CANDIDATES.md` first |
| ☐ | **P4** Set candidate generation | P1 | easiest of the three |
| ☐ | **P5** Exact-cover hand evaluator | P3, P4 | |
| ☐ | **P6** Stakes and settlement | P1, P2 | |
| ☐ | **P7** Round and turn engine | P5, P6 | |
| ☐ | **P8** Console front end | P7 | |
| ☐ | **P9** End-to-end play | P8 | |
| ☐ | **P10** Bot opponents and hints | P9 | optional |

**P2, P3 and P4 are mutually independent** — once P1 is done, pick whichever suits the session.

---

## Notes for the next session

*Anything a cold context would need: decisions taken, surprises, deliberate leftovers.*

- Nothing yet — P0 is a clean start.

---

## Decisions needed from Nick

**None. Every blocking rules question is closed** (`RULES.md` rev 8). P0 through P9 can all
proceed without further input.

The items left in `RULES.md` §9 are fidelity questions with safe defaults already recorded in
`BUILD-PLAN.md` — the discard exception, jokers per meld, whether the money-card claim recurs
each round, whether a pure sequence is required. `QUESTIONS-FOR-MYA-LAY.md` has them phrased
ready to ask. **Do not block on them.**

---

## Session log

| Date | Packet | Outcome |
|---|---|---|
| 2026-08-18 | — | Rules reconstructed from a codebase abandoned in 2023. `RULES.md` reached rev 8 with all blocking questions closed. Rewrite decided (`BUILD-PLAN.md` §1); 11 packets defined. `docs/spec/RUN-CANDIDATES.md` written, correcting P3's acceptance count from 8 to 5. `/poker` skill created. No code written. |
