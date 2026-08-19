#!/usr/bin/env python3
"""Drive BurmesePoker.Console through a pty and write down every byte it printed.

The console needs a terminal it can read keys from, so it cannot be piped and it cannot be
reached from BurmesePoker.Tests (which never references the front end — BUILD-PLAN §2). This
is how a front-end change is shown to be a refactor: capture a match before it and the same
match after it, and compare the two files.

    python3 scripts/drive-console.py --out before.raw --script human --seed 20260819
    …change the console…
    python3 scripts/drive-console.py --out after.raw  --script human --seed 20260819
    cmp before.raw after.raw

⚠️ Keys are fed on *quiescence* — when the program has printed nothing for a moment — and
never on a clock. That is what makes the capture a function of the seed and the key list
alone; a timed driver races the renderer and produces a different file every run. The pty is
also given a fixed size and TERM, because Spectre picks its colour profile and its wrapping
from both.

Written for packet P13.1, whose third acceptance criterion is that the console plays a match
byte-identically at the same --seed after the view model was extracted out from under it.
"""

import argparse
import fcntl
import os
import pty
import select
import signal
import struct
import termios
import time

QUIET = 0.6          # seconds of silence that count as "waiting for me"
SETTLE = 1.5         # seconds of silence after the last key before giving up
HARD_LIMIT = 180.0   # a stalled round must not hang a build

ENTER = b"\r"

# The prompts a match asks, in order: how many seats, how many people, how well the computer
# plays, what the round pays, what a money card pays — then the seat's own questions.
SCRIPTS = {
    # Four seats, nobody at the keyboard: the observer, the settlement and the standings, and
    # not one prompt from SpectrePlayerAgent.
    "bots": [ENTER, b"0" + ENTER, ENTER, ENTER, ENTER, b"n" + ENTER],
    # Four seats, one person, everything defaulted: every panel the human seat draws.
    "human": [ENTER] * 6 + [ENTER] * 40 + [b"n" + ENTER],
}


def drive(exe, args, keys, out_path):
    """Run exe under a pty, feed keys when it goes quiet, and save everything it printed."""
    pid, fd = pty.fork()
    if pid == 0:
        os.environ["TERM"] = "xterm-256color"
        os.environ["COLUMNS"] = "120"
        os.environ["LINES"] = "40"
        os.execv(exe, [exe] + args)

    fcntl.ioctl(fd, termios.TIOCSWINSZ, struct.pack("HHHH", 40, 120, 0, 0))

    chunks = []
    sent = 0
    last = start = time.time()
    finished_at = None

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

        if sent < len(keys):
            os.write(fd, keys[sent])
            sent += 1
            last = time.time()
        elif finished_at is None:
            finished_at = time.time()
        elif time.time() - finished_at > SETTLE:
            break

    try:
        os.kill(pid, signal.SIGKILL)
        os.waitpid(pid, 0)
    except OSError:
        pass

    captured = b"".join(chunks)
    with open(out_path, "wb") as handle:
        handle.write(captured)
    return len(captured)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", default="BurmesePoker.Console/bin/Debug/net10.0/BurmesePoker.Console",
                        help="the built console. Not `dotnet run`, whose build output is noise.")
    parser.add_argument("--out", required=True, help="where to write the captured bytes")
    parser.add_argument("--script", choices=sorted(SCRIPTS), default="human")
    parser.add_argument("--seed", default="20260819")
    parser.add_argument("--extra", nargs=argparse.REMAINDER, default=[],
                        help="anything else for the console, e.g. --extra --no-hints")
    chosen = parser.parse_args()

    # Pace zero: the pause between computer turns is deliberate (P11) and pure latency here.
    args = ["--seed", chosen.seed, "--pace", "0"] + list(chosen.extra)
    size = drive(os.path.abspath(chosen.exe), args, SCRIPTS[chosen.script], chosen.out)
    print(f"{size} bytes -> {chosen.out}")


if __name__ == "__main__":
    main()
