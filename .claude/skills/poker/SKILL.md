---
name: poker
description: Run one complete work cycle on the Burmese Poker project — orient from the plan docs, execute the next build packet, update the docs with what was learned, re-plan the following packets, commit, and report back. Use when the user types /poker, or asks to continue, resume, or make progress on Burmese Poker.
---

# Burmese Poker — one work cycle

Executes **exactly one build packet**, end to end, leaving the repo in a state where running
`/poker` again picks up cleanly. Designed to be run repeatedly from fresh contexts with no
memory of previous sessions.

**Do exactly one packet per run.** Finishing early is fine; doing two is not.

---

## Phase 1 — Orient

Read, in this order, and no more than this:

1. **`docs/STATUS.md`** — the next packet, its state, and any notes left by the last session.
2. **`docs/BUILD-PLAN.md`** — §2 (architecture), §3 (settled design decisions), and the target
   packet in §5.
3. Whatever the packet's **"Read first"** line names — usually specific `RULES.md` sections and
   sometimes a file under `docs/spec/`.

Do **not** read `RULES.md` end to end; read the sections the packet names. Do not read the
retired 2023 source for guidance — it contradicts the confirmed rules in several places.

If `STATUS.md` marks a packet **◐ in progress**, resume it from the notes. Do not restart it.

If every packet is complete, say so and stop — do not invent new work.

## Phase 2 — Verify the baseline

```bash
dotnet build && dotnet test
```

Must be green **before** changing anything.

- **There is no longer a known exception.** P0 retired the 2023 tests in 2026-08. `STATUS.md`
  records the count the tree should be green at; **any failure at all is a real problem.**
- **Any failure: stop.** Report what is broken and ask. Never start a packet on a tree that is
  unexpectedly red.
- **A packet with no new tests is possible but rare** — P8's front end could not be tested,
  because the test project references Domain only. If a packet ends with the test count
  unchanged, say so explicitly and say how it *was* verified.

## Phase 3 — Execute the packet

Follow the packet's **Build** section. Write the tests from its **Acceptance** section —
prefer writing them first; they are already specified precisely.

**Scope discipline.** Do only this packet. If you notice work belonging to a later packet,
write it into `STATUS.md` notes and leave it alone. Resist tidying adjacent code.

**Never modify the retired 2023 code** except where the packet explicitly directs (P0 deletes
it). Do not fix its bugs — it is being replaced.

## Phase 4 — Rules discipline

`docs/RULES.md` is the **only** rules authority. If a rules question comes up mid-packet:

1. **Never infer a rule from code, and never invent one.**
2. Add it to `RULES.md` §9 with a provenance tag and a recommendation.
3. Add a neutrally-phrased version to `docs/QUESTIONS-FOR-MYA-LAY.md` — a **concrete table
   situation**, with no hint at the expected answer. (A confidently-reasoned guess was wrong
   once already; a neutral question is worth more.)
4. Proceed on a safe default and **say so explicitly in the report**.

Only stop outright if there is no safe default.

Provenance ranks: `EXPERT` (Mya Lay) > `PLAYER` (Nick) > `IR` (Indian Rummy) > `CODE`.

## Phase 5 — Verify green

```bash
dotnet build && dotnet test
```

All green, including the new tests. If you cannot get green: **do not commit**, record the
exact state in `STATUS.md`, and report honestly. A red tree is a legitimate outcome to report;
a red tree marked done is not.

## Phase 6 — Update the docs

**Always:**

- **`docs/STATUS.md`** — set the packet's state (`☐` not started, `◐` in progress, `☑` done),
  add a session-log row, and write **Notes for the next session** (anything a cold context
  would need: decisions taken, surprises, leftovers).

**When warranted:**

- **`docs/RULES.md`** — any rules finding. Bump the rev number and the "Last revised" line.
  ⚠️ If the change alters play, bump `JournalHeader.CurrentRulesRevision` to match —
  `GameJournalTests.TheRevisionStampedIsTheRevisionRulesMdIsAt` binds the two, so a forgotten
  bump is a red build, not a stale journal.
- **`docs/spec/`** — any specification detail worked out in enough depth to be worth keeping.
- **`docs/BUILD-PLAN.md`** — ⚠️ **re-plan the following packets.** This step matters. If what
  you learned changes a later packet's design, acceptance criteria, or ordering, **amend it
  now** while the reasoning is fresh.

  *Precedent:* writing the P3 spec revealed that P3's stated acceptance count (8) contradicted
  its own deduplication rule; the correct answer is 5. Left unamended, a future session would
  have built the wrong thing. Look for exactly this kind of contradiction.

## Phase 7 — Commit

One packet per commit, prefixed with the packet id:

```
P3: run candidate generation
```

Use `P3 (partial): …` if the packet is unfinished. **Never push.** If the tree could not be
made green, do not commit — leave it dirty and say so.

## Phase 8 — Report

Keep it tight and factual:

- **Packet and state** — which one, done or partial.
- **What was built** — the substance, not a file list.
- **Test results** — actual numbers, e.g. "17 passed, 0 failed". Never say green without
  having run them.
- **Assumptions or defaults taken**, and any new rules question raised.
- **Next packet**, and whether it changed as a result of this work.

---

## Stop and ask, rather than guessing, when

- The baseline is red for a reason not listed in Phase 2.
- A rules question has no safe default.
- The packet turns out to be wrong or impossible as specified — say so, propose an amendment,
  and let the user decide.
- Finishing would require changing a different packet's scope.

## Never

- Infer a game rule from the code.
- Repair or extend the retired 2023 implementation.
- `git push`.
- Mark a packet done while tests are red.
- Run more than one packet in a single cycle.
