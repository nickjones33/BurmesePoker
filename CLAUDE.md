# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## ⚠️ START HERE

**Run `/poker`.** It encapsulates one full work cycle: orient from the plan, execute the next
build packet, update the docs, re-plan what follows, commit, and report. Defined in
`.claude/skills/poker/SKILL.md`. It is the intended way to work on this project — prefer it
over ad-hoc changes.

This project is **all but finished**. **P0–P12, P14 and P15 are done**: the 2023 implementation has been
deleted, the whole rules core is built and tested, `dotnet run --project BurmesePoker.Console`
fills the empty seats with paced, named bots and plays round after round with the banks carrying
over — showing a hand as the melds it nearly is, keeping a round log across the concealment
clear, hinting what the computer would do, and replaying any match from `--seed` — and
`dotnet run -c Release --project BurmesePoker.Sim` plays thousands of seeded games in parallel
to compare strategies. **Three of the four §0 goals are delivered — solo play (P10), console UX
(P11) and simulation (P12).** **P13 (multiplayer) remains and is droppable** — BUILD-PLAN §5
splits it into P13.1–P13.3 — and **P14–P16 were added on 2026-08-19**: game journals, a skill
ladder, and an experiment on whether the player before you decides your game. **P14 shipped on
2026-08-19**: `--journal <path>` on both front ends writes every decision every seat made as JSON
Lines, and `BurmesePoker.Sim -- replay <path>` plays it back — to a byte-identical CSV. A seed is
a pointer into the build that produced it; a journal is the record (§3.9). **P15 shipped on
2026-08-19 too**: `--strategies random,simple,greedy,cautious` is a skill ladder with **four
rungs and three separated skill levels** — `cautious` turned out to be indistinguishable from
`greedy`, because denial and self-interest point the same way. 🔥 **It also handed P16 a large
lead: two strategies that are level head to head finish 5.4 points apart when one is fed by a
weaker player than the other.** **P16 is the next packet.**

Whether or not you
use the skill, read these first:

1. **`docs/STATUS.md`** — which work packet is next, and the state of the tree. Update it at
   the end of every session.
2. **`docs/BUILD-PLAN.md`** — the rewrite plan. **§0 where the whole thing is heading**, §2
   target architecture, §3 settled design decisions, §5 self-contained work packets, §6
   cold-start protocol.
3. **`docs/RULES.md`** — **the only rules authority.** Every rule is tagged with provenance
   and confidence.

`BUILD-PLAN.md` **§0** records where this is heading beyond a playable game — solo play against
the computer, a console worth sitting at, strategy simulation at scale, and a multiplayer app
with AI seats. **§3.6, §3.7, §3.8 and §3.9 are the design constraints those goals impose**, taken
in advance: agents stay synchronous, simulation is a first-class consumer, statistics are
collected by consumers rather than computed by the domain, and a seed is a pointer while a
journal is the artifact. All four have now been paid off by packets that needed nothing from the
engine — **P11 shipped a whole UX pass without changing a line of the domain, and P14 added
record-and-replay without changing `RoundEngine` or `MatchEngine` at all.**

**The abandoned 2023 implementation is gone.** P0 deleted it; it survives only at the git tag
`pre-rewrite`. Roughly 180 lines of enums and lookup tables from `Common.cs` were salvaged into
`BurmesePoker.Domain/Cards/`. Do **not** restore the rest, and do not treat anything at
`pre-rewrite` as a source of rules — read it only as history (`BUILD-PLAN.md` §1).

The solution is:

```
BurmesePoker.Domain/     pure rules. no I/O, no Spectre. everything new goes here.
                         (System.Text.Json is in here for the journal format — strings, not files.)
BurmesePoker.Console/    Spectre.Console front end. the only project that prints. P8, reworked in P11.
BurmesePoker.Sim/        batch play: seeded, parallel, CSV out. Domain only. built in P12.
BurmesePoker.Tests/      xunit against Domain and Sim. never references Console.
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
dotnet test --filter-method "*SomeTestName*"     # single test (xunit v3 / MTP syntax)
dotnet test --filter-class "*CardTextTests*"     # single test class
dotnet run --project BurmesePoker.Console       # play a round (needs a real terminal)
dotnet run -c Release --project BurmesePoker.Sim -- --games 2000   # compare strategies
dotnet run -c Release --project BurmesePoker.Sim -- bench          # time the cover searches
dotnet run -c Release --project BurmesePoker.Sim -- --games 100 --journal run.jsonl   # keep every decision
dotnet run -c Release --project BurmesePoker.Sim -- replay run.jsonl                  # play them back
```

All four projects target **`net10.0`**, matching the installed SDK (10.0.111). Tests are
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
| `docs/RULES-PRIMER.md` | One-page rules recall aid for humans. |
| `docs/PLAYING.md` | **How to actually play** a solo game in the console — the prompts, the panels, the markers, the flags. Written for a person at the keyboard, not for a build session. |
| `docs/RULES-TECHNICAL.md` | What the **old** code does and where it diverges. Defect list. Historical reference. |
| `docs/spec/RUN-CANDIDATES.md` | **Worked spec for packet P3**, the hardest one. Read before touching run generation. |
| `docs/QUESTIONS-FOR-MYA-LAY.md` | Open rules questions phrased for an experienced player. Answers get promoted into `RULES.md` as `EXPERT`. |
| `docs/RECONCILIATION-PLAN.md` | **Superseded.** Kept for its defect analysis only. |
