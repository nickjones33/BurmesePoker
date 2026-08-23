# How to play well

**What has actually been measured about playing this game, written for a player.** If you do
not play yet, `RULEBOOK.md` teaches the game; this picks up where it stops. It is organised by
decision — the ones you face at the table, in the order the table brings them to you — and it
gives the things that *don't* work as much room as the things that do, because knowing what
not to bother with is the cheapest improvement there is.

**One caveat, stated once.** Every figure here was measured between computer players, over
thousands of rounds a question, at a five-handed table. Nothing has been measured against
people. And the three scoring bonuses — a jokerless win, a win on the deal, a third win in a
row — are known to **no** computer player, so their value to a player who chased them is
**unpriced rather than small** (see *The bonuses nobody has priced*, below). The research
itself — intervals, corrections, how the measuring is done — is `STRATEGY.md`; every number
below comes from the same file that document is generated from,
`docs/strategy/measurements.csv`.

---

## The whole game in one sentence

Every good decision in this game is the same question asked three ways: **of the thirteen
cards you would be left holding, how many meld?** Take the discard if it raises that number.
Claim the turned-up card if it raises that number. Throw whichever card leaves it highest. The
strongest player this project has ever built asks almost nothing else — everything below is
about what to do when that question ties, which early in a round it nearly always does.

So: play to go out. Do not play to hold money cards (they pay you whether you hold them or
not), do not play to starve your neighbour (measured, it loses — below), and do not read a
bad-looking deal as a lost round — a fresh hand melds only a handful of its thirteen, one hand
in five melds nothing at all, and the rounds are decided by who improves fastest, not by who
started best. Most of that improving comes from **taking discards, not drawing**: with two
decks in the shoe, the card somebody throws away is very often somebody else's third of a
rank.

## When several discards look equal — and early on they all do

Early in a round almost every discard leaves your meld count exactly where it was, so the
count alone cannot choose for you. Everything that separates a decent player from a poor one
lives inside that tie. Keep:

- **cards with partners** — the same rank in another suit, or a neighbour in the same suit,
  because either is two thirds of a meld;
- **jokers over everything** — a joker finishes any meld you are one card short of.

That tie-break alone is worth half again as many wins: a player using it wins
**23.9% of rounds against 16.1%** for one who only counts melds, over every way of seating
the two at a five-handed table.

## The one refinement that works: count what could still arrive

Where two discards leave the hand equally melded *and* equally partnered, keep the thirteen
that **more of the pack would improve**. Ask, of each card you cannot see, whether drawing it
would help the hand you would be left with — and hold the hand with more such cards. That is
worth **+2.7 ± 0.8 points** of win rate over the partner rule alone, and it is the only
refinement, of the five this project has built and measured, that has ever beaten it.

The clearest case of what the partner rule cannot see: a second copy of a card already inside
a long run looks like a spare — same partners either way — but keeping it lets the run split
into two legal runs later. Counting arrivals sees that; counting partners cannot.

## The money: settle it, never chase it

Two facts about the money layer, one from the rules and one from measurement, and together
they say to ignore it while you play.

**Ownership is permanent, so a money card in your hand is not an asset — it is just a card.**
You are paid for the money cards the *deck* gave you — dealt to you, or drawn blind — and you
are paid whether you kept them, melded them or threw them away. **Never hoard a money card**;
throw it exactly as if it were plain.

**And digging in the deck to acquire more is not worth it at any stakes you would actually
play.** A player built to draw blind for ownership whenever the maths favoured it was measured
against the strongest ordinary player: at the standard $5-round/$1-card stakes the maths
*never* favours it — the two play identical rounds, and chasing the money measures
**-$0.23 ± $0.32 a round**, which is nothing. The trade only turns real when one money card is
worth about **four rounds**: at $5/$20 the chaser banks **+$3.99 ± $2.43** a round, and at
$5/$40 **+$14.20 ± $4.81** — while winning **16 points** fewer rounds, because a hand dug out
of the deck is a worse hand. Unless your table has agreed to stakes that lopsided, play the
rummy and let the money settle itself.

## Things that sound clever and measurably are not

Each of these was built, played for thousands of rounds, and could not be told apart from not
doing it — or lost outright. They are the most useful part of this page, because each is a
place you would otherwise spend attention for nothing.

- **Protecting your money cards.** Costs nothing to skip — ownership is permanent (above).
  This is a rule, not a measurement, and it is the one most worth internalising.
- **Refusing the opener's claim.** When the opener asks your permission to take the turned-up
  money card, it makes no measurable difference whether you refuse or allow:
  **-0.3 ± 0.8 points** of win rate and **-$0.11 ± $0.32** a round between a table that always
  refuses and one that always allows. The veto is real and fires about one round in seven —
  it just does not decide anything. Answer it however you like.
- **Counting the cards.** A player that remembered every card it had ever been shown, and
  estimated the shoe from that instead of from its own hand, measured
  **+0.6 ± 0.8 points** *behind* the player that never bothered — a dead heat. The memory
  works; there is simply too little to learn (a round shows you about ten cards beyond your
  own hand, out of 108) and nowhere profitable to spend it.
- **Choosing discards to starve the player after you.** Throwing the card least useful to the
  neighbour who picks up after you measures **+0.0 ± 0.8 points** — indistinguishable from
  ignoring them. Denial and self-interest point the same way: the card you want to keep is
  usually the card they want you to throw.
- **Playing the feeding ban as a weapon.** Taking cards you do not want, in the open, purely
  to lock ranks against the player before you — and then holding those ranks — is the one
  idea that actively *lost*: it gives up **7.3 ± 0.8 points** against the same player without
  the habit. The locks are real and they do bite; they cost more to hold than they deny. Use
  the ban when it is free — do not pay for it.
- **Worrying about who sits where.** A weaker player *anywhere* at the table is worth several
  points to you; which side of you they sit on is worth nothing measurable between players who
  are both actually playing. When the table asks to change seats, agree or refuse on comfort —
  the measurement has no opinion.

## The bonuses nobody has priced

Three settlement rules multiply what a win pays, and **no measured player knows any of them
exist** — so everything above optimises win rate and none of it prices a bonus. This is the
one part of the game where the honest answer is *unknown*, not *no*.

- **The jokerless win** pays triple at this table, and even players holding jokers over
  everything — the exact opposite of trying — collect it in **12.1%** of rounds, purely by
  accident. That is a floor. Whether deliberately shedding a joker late, at some cost in
  speed, collects it often enough to pay has never been measured — and the arithmetic of the
  prize says a player who managed it even one round in four would be well ahead. If anything
  on this page is worth experimenting with at a real table, it is this.
- **The win on the deal** pays double, and it is luck: about one deal in thousands arrives
  already melded. There is no decision to make — if your dealt thirteen covers, declare.
- **The third consecutive win** is paid entirely by the player above the winner. Whether a
  table should change seats before somebody's third win — the one defence the rules offer —
  has never been measured, and the computer never asks for it.

## Which computer should you sit with

The four difficulty settings are one player — the strongest there is — slipping at four
measured rates: *easy* throws the wrong one of two good cards nine times in ten, *medium*
seven in ten, *hard* two in five, *expert* never. At a five-handed table holding all four
settings they win **10.3% / 16.2% / 23.0% / 30.6%** of the rounds, weakest first — steps of
six to nine points, far enough apart that moving one level is something you feel within a
session, close enough that *"a bit easier"* is a real request.

**Start at *hard* and move.**

- ***easy*** feeds you cards. It knows what to keep and throws the wrong one nearly every
  turn, so the discard beside you is often the one you wanted.
- ***medium*** is the one to sit at while the melds are still unfamiliar. It gets the close
  choices wrong more often than not, and you will win more rounds than it does.
- ***hard*** is a real game. It slips about twice in five on the cards it is choosing
  between, which is roughly what a good player does when they are not concentrating.
- ***expert*** never slips at all, and beats the other three every way this has been
  measured. It is what a hint is asked of, and what takes over your seat if you stop
  answering — a hint that got worse as you lowered the difficulty would be absurd.

The setting is the *computer's*, not yours: nothing about your hand, your hint or the deal
changes with it.

---

*Every figure on this page is checked against `docs/strategy/measurements.csv` by a build
test, so a number here cannot quietly outlive the measurement it came from. How each was
measured — and everything else the programme knows — is `STRATEGY.md`.*
