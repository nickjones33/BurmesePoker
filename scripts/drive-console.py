#!/usr/bin/env python3
"""Drive BurmesePoker.Console through a pty, adaptively, and write down every byte it printed.

The console needs a terminal it can read keys from, so it cannot be piped and it cannot be
reached from BurmesePoker.Tests (which never references the front end — BUILD-PLAN §2). This
is how a front-end change is shown to be a refactor: capture a match before it and the same
match after it, and compare the two files.

    python3 scripts/drive-console.py --out before.raw --script human --seed 20260819
    …change the console…
    python3 scripts/drive-console.py --out after.raw  --script human --seed 20260819
    cmp before.raw after.raw

⚠️ The driver ANSWERS PROMPTS, it does not replay a key list (P30.2). Until that packet both
scripts were fixed key sequences, so a prompt added or removed anywhere shifted every key
after it onto the wrong question — which is how the repo went a year of captures believing
none contained a declaration. The driver now reads the pty until a prompt is standing,
answers that prompt, and keeps going until the round count is played and the console exits.
The capture is still a pure function of the seed and the choices, because every answer is
decided from the text of the prompt alone; two runs at one seed produce identical bytes.

What it answers:
  - every setup question takes its default (ENTER), except the people count (the script) and
    the difficulty, where --pick moves down the list first;
  - a human turn takes the first offered action, throws the first offered card, stays at the
    keyboard, declines the claim, refuses when asked, and DECLARES when the console offers it;
  - "Another round?" is answered yes until --rounds settlements have been seen, then no, so
    the console exits by its own path and the capture ends whole rather than mid-prompt.

And it VERIFIES the settlement arithmetic before reporting success (P30.2 build item 4): each
settlement panel's Round, Money and Net columns must each sum to zero, every row must satisfy
Net = Round + Money, and exactly one seat may collect the round — the flat (n−1)·round-value
that RULES.md §7.2 fixes, with no deadwood penalty possible. A capture that fails any of that
exits non-zero. (The engine's own payouts obey exactly these identities — Settlement is
zero-sum by construction and ConsoleObserver prints RoundResult directly — so this checks the
panel against the rules the engine settles by, not against a second run of the engine.)

⚠️ Keys are fed on *quiescence* — when the program has printed nothing for a moment — and
never on a clock; a timed driver races the renderer and produces a different file every run.
The pty is given a fixed size and TERM, because Spectre picks its colour profile and wrapping
from both.
"""

import argparse
import fcntl
import os
import pty
import re
import select
import signal
import struct
import sys
import termios
import time

QUIET = 0.6          # seconds of silence that count as "waiting for me"
HARD_LIMIT = 300.0   # a stalled match must not hang a build

ENTER = b"\r"
DOWN = b"\x1b[B"

ANSI = re.compile(r"\x1b\[[0-9;?]*[a-zA-Z]|\x1b\][^\x07]*\x07|\r")

# The people count is the one setup answer the two scripts differ on.
PEOPLE = {"bots": b"0", "human": b"1"}


def stripped(raw):
    """The capture as a person reads it: no ANSI, no carriage returns."""
    return ANSI.sub("", raw.decode("utf-8", "replace"))


def settlements(text):
    """How many settlement panels the console has printed so far."""
    return text.count("Money cards owned")


def answer(text, script, pick, rounds, picked):
    """The keys for the prompt standing at the end of `text`, or None if none is standing.

    Decided from the prompt's own words, never from position — the whole point of the
    rewrite. Returns (keys, picked) where `picked` records that the difficulty list has
    been answered (its DOWNs must be sent exactly once).
    """
    # Wide enough that a fourteen-option discard list still has its question in view.
    tail = [line.rstrip() for line in text.split("\n") if line.strip()][-30:]

    if not tail:
        return None, picked

    last = tail[-1]
    recent = "\n".join(tail)

    if "Another round?" in last:
        return (b"y" + ENTER if settlements(text) < rounds else b"n" + ENTER), picked

    if "How many of you are people?" in last:
        return PEOPLE[script] + ENTER, picked

    if "Declare and end the round?" in last:
        return b"y" + ENTER, picked

    # The difficulty is a selection list: its question sits a few lines above the options,
    # and --pick moves down the ladder before accepting (0 expert, 1 hard, 2 medium, 3 easy).
    if "How hard should the computer be?" in recent and not picked:
        return DOWN * pick + ENTER, True

    # Any other standing prompt — a text prompt or confirm ends its line with the question
    # (and the echoed default), a selection list highlights an option under its question.
    # Either way ENTER takes the default or the highlighted entry, which is always legal.
    if "?" in last or ("?" in recent and (last.startswith(">") or last.startswith("  "))):
        return ENTER, picked

    return None, picked


def drive(exe, args, script, pick, rounds, out_path):
    """Run exe under a pty, answer its prompts, and save everything it printed."""
    pid, fd = pty.fork()
    if pid == 0:
        os.environ["TERM"] = "xterm-256color"
        os.environ["COLUMNS"] = "120"
        os.environ["LINES"] = "40"
        os.execv(exe, [exe] + args)

    fcntl.ioctl(fd, termios.TIOCSWINSZ, struct.pack("HHHH", 40, 120, 0, 0))

    chunks = []
    last = start = time.time()
    picked = False
    answered_at = -1  # how much output had arrived when the last answer was sent

    while time.time() - start < HARD_LIMIT:
        readable, _, _ = select.select([fd], [], [], 0.1)

        if readable:
            try:
                data = os.read(fd, 65536)
            except OSError:      # the child closed the pty: it has exited
                break
            if not data:
                break
            chunks.append(data)
            last = time.time()
            continue

        if time.time() - last < QUIET:
            continue

        # Quiet, and nothing new since the last answer means the same prompt is still being
        # waited on by us both — never answer one prompt twice.
        seen = sum(len(chunk) for chunk in chunks)
        if seen == answered_at:
            continue

        keys, picked = answer(stripped(b"".join(chunks)), script, pick, rounds, picked)

        if keys is not None:
            os.write(fd, keys)
            answered_at = seen
            last = time.time()

    try:
        os.kill(pid, signal.SIGKILL)
        os.waitpid(pid, 0)
    except OSError:
        pass

    captured = b"".join(chunks)
    with open(out_path, "wb") as handle:
        handle.write(captured)
    return captured


def money(cell):
    """Every signed dollar amount in a settlement cell, summed: '-$5' → -5, '+$15' → 15."""
    amounts = re.findall(r"([+-])\$(\d+)", cell)
    return sum(int(size) if sign == "+" else -int(size) for sign, size in amounts)


def verify(text, rounds):
    """The settlement panels obey RULES.md §4.3 and §7.2, or say exactly how they do not."""
    problems = []
    panels = 0

    if text.count("declares") < 1:
        problems.append("no declaration was captured at all")

    # A settlement panel row: │ name │ owned │ Round │ Money │ Net │.
    for panel in re.findall(
        r"│ Player\s+│[^╰]*?╰", text, re.DOTALL
    ):
        rows = []
        for line in panel.split("\n"):
            cells = [cell.strip() for cell in line.split("│")]
            if len(cells) == 7 and re.search(r"[+-]\$\d", cells[3] + cells[5]):
                rows.append((cells[1], money(cells[3]), money(cells[4]), money(cells[5])))

        if not rows:
            continue

        panels += 1

        for name, flat, side, net in rows:
            if net != flat + side:
                problems.append(f"{name}: net {net} is not round {flat} + money {side}")

        for column, label in ((1, "round"), (2, "money"), (3, "net")):
            total = sum(row[column] for row in rows)
            if total != 0:
                problems.append(f"the {label} column sums to {total}, not zero")

        collectors = [row for row in rows if row[1] > 0]
        if len(collectors) != 1:
            problems.append(f"{len(collectors)} seats collected the round; §7.2 pays one winner")
        elif collectors[0][1] != -sum(row[1] for row in rows if row[1] < 0):
            problems.append("the winner's round take is not what the losers paid")

    if panels < rounds:
        problems.append(f"{panels} settlement panel(s) captured; {rounds} round(s) were asked for")

    return panels, problems


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--exe", default="BurmesePoker.Console/bin/Debug/net10.0/BurmesePoker.Console",
                        help="the built console. Not `dotnet run`, whose build output is noise.")
    parser.add_argument("--out", required=True, help="where to write the captured bytes")
    parser.add_argument("--script", choices=sorted(PEOPLE), default="human",
                        help="bots seats nobody; human puts one person at the keyboard")
    parser.add_argument("--seed", default="20260819")
    parser.add_argument("--rounds", type=int, default=1,
                        help="how many rounds to play to settlement before declining another")
    parser.add_argument("--pick", type=int, default=0,
                        help="how far down the difficulty list to move before accepting it: "
                             "0 expert, 1 hard, 2 medium, 3 easy (P19 made the list levels only).")
    parser.add_argument("--extra", nargs=argparse.REMAINDER, default=[],
                        help="anything else for the console, e.g. --extra --no-hints")
    chosen = parser.parse_args()

    # Pace zero: the pause between computer turns is deliberate (P11) and pure latency here.
    args = ["--seed", chosen.seed, "--pace", "0"] + list(chosen.extra)
    captured = drive(
        os.path.abspath(chosen.exe), args, chosen.script, chosen.pick, chosen.rounds, chosen.out)

    panels, problems = verify(stripped(captured), chosen.rounds)
    print(f"{len(captured)} bytes -> {chosen.out} ({panels} settlement panel(s))")

    if problems:
        for problem in problems:
            print(f"  FAIL: {problem}", file=sys.stderr)
        sys.exit(2)


if __name__ == "__main__":
    main()
