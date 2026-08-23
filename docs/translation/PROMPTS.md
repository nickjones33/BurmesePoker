# Translation prompts — the rulebook and the strategy guide in Burmese

**How to use this file.** Packet **P40** (`BUILD-PLAN.md` §5) wants `docs/RULEBOOK.md` and
`docs/HOW-TO-PLAY-WELL.md` in Burmese. The translation itself is done outside this repository,
with an LLM that is strong in Burmese (Gemini or ChatGPT), by a person: attach the document,
paste the matching prompt below, and save what comes back. **Translate with one model and
cross-check with the other** (prompt C) — repeat until the cross-check comes back clean, then
hand the vetted Markdown to the packet, which lands it and builds the fences.

Three things the prompts below insist on, and why the packet needs them:

1. **Latin digits everywhere, never Burmese numerals (၀–၉).** Every figure, dollar amount,
   `± ` interval and the `rev 30` stamp must survive translation byte-for-byte — that is what
   lets the build keep fencing a document no test can read.
2. **Card notation, backticked spans and Markdown structure verbatim.** Same headings in the
   same order, same tables, same emphasis — so the English and Burmese documents stay
   side-by-side comparable, section for section.
3. **Unicode Burmese, not Zawgyi.** The repository is UTF-8; Zawgyi-encoded text renders as
   the wrong characters everywhere outside Zawgyi fonts.

---

## Prompt A — translate the rulebook

*Attach `docs/RULEBOOK.md`, then paste:*

```text
You are translating a card game rulebook from English into Burmese (Myanmar language).

The game is a Burmese rummy, played for money with two decks — the game itself comes from
Myanmar, so this translation is bringing it home. The readers are Burmese-speaking card
players; write the way you would teach the game to a friend across the table, in natural,
clear standard Burmese. The English source is written in exactly that voice — match it.

Terminology comes first. Before translating, output a short glossary table of the Burmese
terms you will use for the key game concepts: card, deck, deal, hand, draw pile, discard,
discard pile, take (a discard), draw blind, meld, run/series, set, joker, declare / go out,
turn-up cards, money card, ownership, stakes, seat, dealer, claim, the feeding restriction,
jokerless win, win on the deal. Prefer the words Burmese card players actually use at the
table — established card-playing terms and common loanwords written in Burmese script (for
example ဂျိုကာ for joker) — over literal word-for-word renderings. Then use each term
consistently through the whole document.

Rules for the translation itself:

1. Output GitHub-flavored Markdown that mirrors the source exactly: the same headings in the
   same order, the same tables with the same columns, the same bold and italic emphasis, the
   same lists and horizontal rules. Translate heading text and prose. Do not merge, drop,
   reorder, add to, or summarize anything.
2. Keep every number in Western digits exactly as printed — dollar amounts, multipliers,
   counts, seat numbers, and the "rev 30" stamp near the top. Do NOT convert any number to
   Burmese numerals (၀–၉). Automated tests compare these against the English source, so they
   must be byte-for-byte identical.
3. Keep all card notation verbatim: the suit symbols ♠ ♥ ♦ ♣, the ranks (A, K, Q, J, 10 … 2),
   and combined forms like 7♦ and A♠.
4. Anything wrapped in `backticks` (file names, code) stays untranslated, backticks included.
5. Keep the players' names in the worked-round section in Latin script as printed, and keep
   every card, number and settlement figure in that section exactly as it appears — translate
   only the prose around them.
6. Where an English game term has no natural Burmese equivalent, give the natural Burmese
   explanation and put the English term in parentheses the first time it appears.
7. This is a rulebook someone will learn the game from: precision beats elegance wherever the
   two conflict.
8. Write Burmese in standard Unicode encoding, never Zawgyi.
9. Do not add translator's commentary inside the text. If a passage is ambiguous, translate it
   faithfully and list your questions at the very end under a final heading "Translator's
   notes".
10. If the document is too long for one reply, stop at the end of a section and continue
    exactly where you stopped when I say "continue" — do not restart or summarize.

Translate the attached document.
```

## Prompt B — translate the strategy guide

*Attach `docs/HOW-TO-PLAY-WELL.md`, then paste:*

```text
You are translating a strategy guide for a card game from English into Burmese (Myanmar
language).

The game is a Burmese rummy played for money. This document tells a player, in plain
language, what has actually been measured about playing it well — including several things
that sound clever and measurably do not work. The readers are Burmese-speaking card players;
write natural, clear standard Burmese in the voice of good advice from an experienced friend,
which is the voice the English is written in.

Terminology comes first. Before translating, output a short glossary table of the Burmese
terms you will use for the key concepts: meld, run/series, set, discard, draw pile, take (a
discard), draw blind, declare / go out, joker, money card, ownership, stakes, win rate,
tie-break, difficulty setting, measured / measurement. Prefer the words Burmese card players
actually use, and use each term consistently. If you translated this game's rulebook earlier
in this conversation, reuse that glossary exactly.

This document quotes measurements, and they must survive translation untouched:

1. Keep every number in Western digits exactly as printed — percentages, dollar amounts,
   figures with "± " intervals (for example a margin written as one number ± another), and
   counts. Do NOT convert any number to Burmese numerals (၀–၉). Automated tests compare these
   against the English source, so they must be byte-for-byte identical.
2. Translate the meaning around the figures plainly. Where the English says a difference is
   too small to measure, or that two ways of playing cannot be told apart, say that in
   natural Burmese — the reader is a player, not a statistician.

And the same structural rules as any document here:

3. Output GitHub-flavored Markdown mirroring the source exactly: same headings in the same
   order, same tables and columns, same bold and italic emphasis, same lists. Do not merge,
   drop, reorder, add to, or summarize anything.
4. Keep card notation verbatim: ♠ ♥ ♦ ♣, ranks, and forms like 7♦ and A♠.
5. Anything in `backticks` (file names, code) stays untranslated, backticks included.
6. Write Burmese in standard Unicode encoding, never Zawgyi.
7. No translator's commentary inside the text; questions go at the very end under a final
   heading "Translator's notes".
8. If the document is too long for one reply, stop at the end of a section and continue
   exactly where you stopped when I say "continue".

Translate the attached document.
```

## Prompt C — cross-check one model's translation with the other

*Attach the English source **and** the Burmese translation, then paste this into the model
that did **not** produce the translation:*

```text
You are reviewing a Burmese (Myanmar language) translation of an English card game document.
Both are attached: the English source and the Burmese translation. The game is a Burmese
rummy played for money; the translation is for Burmese-speaking card players.

Compare them section by section and report — do not rewrite the translation. List every
finding in a numbered table with: the section heading it is under, the English text, the
Burmese text, and what is wrong. Check, in this order of importance:

1. Meaning: any mistranslation that changes a rule, a number's meaning, or a recommendation —
   including a subtle one, such as a reversed condition, a wrong player ("the player before
   you" versus "after you"), or an obligation turned into an option.
2. Omissions and additions: any sentence, list item, table row or qualifier present in one
   document and missing from the other.
3. Figures: any number, dollar amount, "± " interval, percentage, or card symbol
   (♠ ♥ ♦ ♣, 7♦, A♠ …) that is not byte-for-byte identical to the source, including any
   number converted into Burmese numerals (၀–၉) — all numbers must be Western digits.
4. Terminology: the same game term rendered with different Burmese words in different
   sections.
5. Register and naturalness: places where the Burmese reads as stiff, machine-translated, or
   uses a word no card player would use — suggest the natural term.
6. Encoding: any Zawgyi-encoded text — the whole document must be standard Unicode Burmese.
7. Structure: headings, tables, emphasis or backticked spans that do not mirror the source.

End with one line: either "Safe to use as-is" or "Needs the fixes above", and nothing else.
```

---

*When the translations come back clean, packet P40 lands them as `docs/RULEBOOK.my.md` and
`docs/HOW-TO-PLAY-WELL.my.md` and builds the fences that keep them from going stale — see
`BUILD-PLAN.md` §5 P40 for what those are.*
