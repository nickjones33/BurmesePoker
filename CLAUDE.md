# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## ⚠️ START HERE

**Run `/poker`.** It encapsulates one full work cycle: orient from the plan, execute the next
build packet, update the docs, re-plan what follows, commit, and report. Defined in
`.claude/skills/poker/SKILL.md`. It is the intended way to work on this project — prefer it
over ad-hoc changes.

This project is **mid-rewrite**. **P0 is done** — the 2023 implementation has been deleted and
the solution is now three projects. Whether or not you use the skill, read these first:

1. **`docs/STATUS.md`** — which work packet is next, and the state of the tree. Update it at
   the end of every session.
2. **`docs/BUILD-PLAN.md`** — the rewrite plan. §2 target architecture, §3 settled design
   decisions, §5 self-contained work packets, §6 cold-start protocol.
3. **`docs/RULES.md`** — **the only rules authority.** Every rule is tagged with provenance
   and confidence.

**The abandoned 2023 implementation is gone.** P0 deleted it; it survives only at the git tag
`pre-rewrite`. Roughly 180 lines of enums and lookup tables from `Common.cs` were salvaged into
`BurmesePoker.Domain/Cards/`. Do **not** restore the rest, and do not treat anything at
`pre-rewrite` as a source of rules — read it only as history (`BUILD-PLAN.md` §1).

The solution is:

```
BurmesePoker.Domain/     pure rules. no I/O, no Spectre. everything new goes here.
BurmesePoker.Console/    Spectre.Console front end. the only project that prints. rebuilt in P8.
BurmesePoker.Tests/      xunit against Domain's public API. references Domain only.
```

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
dotnet test --filter "FullyQualifiedName~SomeTestName"   # single test
dotnet run --project BurmesePoker.Console       # run the game (a placeholder until P8)
```

All three projects target **`net10.0`**, matching the installed SDK (10.0.111). Nick's
standing preference is the newest supported .NET tooling — see the `.slnx` solution format
for the same call. Don't downgrade either back for compatibility's sake.

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
| `docs/RULES-PRIMER.md` | One-page rules recall aid for humans. |
| `docs/RULES-TECHNICAL.md` | What the **old** code does and where it diverges. Defect list. Historical reference. |
| `docs/spec/RUN-CANDIDATES.md` | **Worked spec for packet P3**, the hardest one. Read before touching run generation. |
| `docs/QUESTIONS-FOR-MYA-LAY.md` | Open rules questions phrased for an experienced player. Answers get promoted into `RULES.md` as `EXPERT`. |
| `docs/RECONCILIATION-PLAN.md` | **Superseded.** Kept for its defect analysis only. |
