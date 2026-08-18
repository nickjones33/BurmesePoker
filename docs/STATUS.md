# Build Status

Cross-session progress tracker. **`/poker` reads this first and updates it last.**

Plan: `BUILD-PLAN.md` · Rules: `RULES.md` (rev 8) · Skill: `.claude/skills/poker/SKILL.md`

State markers: `☐` not started · `◐` in progress · `☑` done

---

## Current state

**Next packet: P1 — Cards, deck, identity.** No blockers.

P0 is done. **The 2023 implementation is gone from the tree** and lives only at the
`pre-rewrite` tag. The solution is now three projects — `BurmesePoker.Domain` (pure rules),
`BurmesePoker.Console` (the renamed exe, Spectre.Console, prints a placeholder until P8), and
`BurmesePoker.Tests` (references **Domain only**).

Domain holds enums plus `CardText` and nothing else: `Cards/{Rank,Suit,CardColor,CardText}`,
`Melds/MeldKind`.

✅ **The baseline is now fully green** — `dotnet build` clean, `dotnet test` 28 passed,
0 failed. The old expected failure (`CardPlays_Runs_HappyPath_Jokers`) is gone with the test
that asserted it. **Any red tree from here on is a real problem.**

---

## Packets

| | Packet | Depends on | Notes |
|:-:|---|---|---|
| ☑ | **P0** Restructure and salvage | — | done 2026-08-18 |
| ☐ | **P1** Cards, deck, identity | P0 | **next** — enums already exist, build `Card`/`CardId`/`Deck` |
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

- **Run the app with `dotnet run --project BurmesePoker.Console`** now, not `--project
  BurmesePoker`. `Program.cs` is a one-line placeholder; P8 rebuilds the front end.
- **P1 does not need to create `Rank`, `Suit` or `CardColor`** — P0 ported them in their
  final §3.2 shape. Ranks are numeric (`Two = 2` … `Ace = 14`) and **neither `Rank` nor
  `Suit` has a `Joker` member**. A joker is `Rank = null, Suit = null`, told apart from the
  other joker by `Color` and `CardId`. P1's build list is amended accordingly.
- `MoneyCardStatus` and `PlayerAction` were deliberately **not** ported — superseded by
  `MoneyCardRegistry.Multiplier` (P2) and `TurnAction` (P7). `CardPlayType` became
  `Melds/MeldKind`.
- `UserPromptFactory` was deleted with the rest of `Logic/`, despite `BUILD-PLAN` §1.2
  listing it as a keep. P0 step 4 is explicit and §1.3 rules out a `legacy/` folder.
  **P8 should read it from the `pre-rewrite` tag:** `git show pre-rewrite:BurmesePoker/Logic/Factories/UserPromptFactory.cs`.
- **P0 shipped 28 tests, not the zero its acceptance line named.** A zero-test run exits 0 but
  only prints *"No test is available…"*, and `CardText` is newly ported code. `BUILD-PLAN`
  §5 P0 records the amendment.
- `CardText.ParseRank` was kept from the old `CardRankFromString` (it was not on P0's drop
  list) and is expected to earn its keep building hands in later packets' tests. Nothing in
  the domain calls it yet.
- The solution file is **`BurmesePoker.slnx`** (the newer XML format, now the `dotnet new
  sln` default). Nick's standing preference is the newest supported .NET tooling — don't
  "fix" it back to a classic `.sln`.
- `.gitignore` was broadened from four hard-coded project paths to plain `bin/` and `obj/`,
  since the project names changed.
- **`BUILD-PLAN` §5 P3's "Done when" said 8 candidates** while the packet body said 5 — the
  exact contradiction the re-plan step exists to catch. Amended to 5, along with the same
  stale 8 in the §8 risk table. `docs/spec/RUN-CANDIDATES.md` was already correct.
- No rules question arose in P0; `RULES.md` is untouched at rev 8.

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
| 2026-08-18 | P0 | ☑ Done. Tagged `pre-rewrite`, then deleted `Models/`, `Logic/` and `Common.cs`. Solution restructured to Domain/Console/Tests. Salvaged the enums and display tables into `Cards/{Rank,Suit,CardColor,CardText}` and `Melds/MeldKind`. Build clean, **28 passed / 0 failed**. Amended P0's acceptance (tests, not zero tests) and P3's "Done when" (5 candidates, not 8). |
| 2026-08-18 | — | Rules reconstructed from a codebase abandoned in 2023. `RULES.md` reached rev 8 with all blocking questions closed. Rewrite decided (`BUILD-PLAN.md` §1); 11 packets defined. `docs/spec/RUN-CANDIDATES.md` written, correcting P3's acceptance count from 8 to 5. `/poker` skill created. No code written. |
