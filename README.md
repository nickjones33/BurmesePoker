# Burmese Poker

A Burmese rummy played for money — reconstructed from the people who play it, written down
rule by rule, and built as a game you can actually sit down at: a console table, a browser
lobby you can play other people in, and a simulator that plays tens of thousands of hands to
work out which way of playing is better.

**No published ruleset for this game exists.** `docs/RULES.md` is a reconstruction, and every
rule in it carries where it came from and how confident that source is.

---

## The game in a screen

Two decks shuffled together with their jokers — **108 cards**. Everybody is dealt **thirteen**,
and two cards are turned face up beside the deck. On your turn you **take one card** — either
the previous player's discard or the top of the deck, unseen — and then **throw one away**.

Nothing is ever laid on the table. You build your hand entirely in secret and the round ends
when somebody discards and reveals **all thirteen at once**, melded into runs and sets.

**What a winning thirteen has to contain depends on how many are playing.** At five or more it
is any thirteen that melds. At four it must hold a run with no joker in it; at three, two of
them; at two, nothing but runs. Fewer players means more of the deck each, so the hand is made
harder to make up for it.

**You may not feed the player after you.** If they have taken a card in the open, that rank is
closed to you — any suit — until they throw it themselves or you are going out on it. It is not
a foul; a closed card simply is not one you can throw.

### The money

The money is what makes this game *this* game, and it is a second ledger running underneath the
round.

The **7♦**, the **A♠** and **every joker** are money cards in every round, all copies out of
both decks. So are the two cards turned up at the deal, and their matching copies. Each money
card pays **its owner** a fixed amount from every other player at the table — **whoever wins the
round.**

**Owning is not holding.** A money card is yours only if the deck gave it to you: dealt in your
opening thirteen, or drawn blind. One picked up from a discard is held but never owned — and
ownership never transfers, so throwing a money card away costs you nothing. You are still paid
for it.

A turned-up card belongs to nobody, so its value moves to its partner copy, which pays **triple**.
If the two cards turned up are the 7♦ and the A♠ and one player owns both partners, they pay
**five times** each — the one place in the game where what a card pays depends on who holds what.

### What a win is worth

Every loser pays the winner the round value, flat — there is no penalty for how much you were
left holding. Two things multiply it and one changes who pays it:

- a hand declared with **no joker in it** pays **double** at two to four players, **triple** at
  five or more;
- a hand that **already wins as dealt**, before anybody has drawn, pays **double** again;
- a **third win in a row** is paid **entirely by the player immediately before the winner in
  turn order** — blamed for feeding them — and the rest of the table pays nothing.

Nothing ends a session. Banks carry over and you stop when you stop.

---

## Play it

```bash
dotnet run --project BurmesePoker.Web       # a browser lobby at http://localhost:5188
dotnet run --project BurmesePoker.Console   # one table in the terminal
```

The browser lobby seats five by default; sit down and the empty seats are filled by the
computer. `--people 0` makes every seat a bot and just deals, which is worth watching once.
`--difficulty easy|medium|hard|expert` decides how hard the computer plays. `docs/PLAYING.md`
is the guide for a person at the keyboard — the prompts, the panels and what the hint arrow is
telling you.

To ask which way of playing is better rather than to play:

```bash
dotnet run -c Release --project BurmesePoker.Sim -- --games 2000
dotnet run -c Release --project BurmesePoker.Sim -- tournament --games 2000
```

## What is in here

| Project | What it is |
|---|---|
| `BurmesePoker.Domain` | The rules. Cards, melds, the win condition, the money, the engine and the computer players. No input, no output. |
| `BurmesePoker.Presentation` | What a hand looks like *as data* — display order, near-melds, per-card cost, the computer's hint — with no rendering technology in it at all. |
| `BurmesePoker.Server` | One table, hosted: a seat played from elsewhere, a bot that stands in when nobody answers, and the fan-out that decides what each viewer is told. |
| `BurmesePoker.Console` | The terminal front end. |
| `BurmesePoker.Web` | The browser lobby and the table you play at, over Blazor Server. |
| `BurmesePoker.Sim` | Batch play: seeded, parallel, thousands of games, CSV out. |
| `BurmesePoker.Tests` | The test suite, over everything but the console. |

A hand is fully concealed with money on it, so **the engine runs server-side, always** — the
browser client is sent only what its own seat is entitled to know.

## Where the answers live

| Question | Read |
|---|---|
| I've never played — teach me the game | `docs/RULEBOOK.md` — the whole game in reading order, with a worked round |
| What are the rules, and how sure is anybody? | `docs/RULES.md` — the only rules authority, provenance on every rule, open questions in §9 |
| I just want to remember how to play | `docs/RULES-PRIMER.md` |
| How do I actually play the thing? | `docs/PLAYING.md` |
| How do I get better at it? | `docs/HOW-TO-PLAY-WELL.md` — what has been measured about playing well, written for a player |
| Which way of playing is better, and by how much? | `docs/STRATEGY.md` — every figure generated from `docs/strategy/measurements.csv`, with an interval |
| How are those numbers measured, and can I trust them? | `docs/SIMULATIONS.md` — the measurement machinery taught for a curious person, digit-free |
| What is still unanswered about the rules? | `docs/RULES.md` §9, and `docs/QUESTIONS-FOR-MYA-LAY.md` |
| How is it built, and what is being built next? | `docs/BUILD-PLAN.md` and `docs/STATUS.md` |

## Building it

```bash
dotnet build
dotnet test
dotnet test --filter-class "*CardTextTests*"
```

Everything targets `net10.0`. The tests are xunit v3 on Microsoft.Testing.Platform, which is why
filtering is `--filter-class` and `--filter-method` rather than VSTest's `--filter`.
