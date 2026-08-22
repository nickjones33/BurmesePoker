using System.Globalization;
using System.Text;

using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Presentation;

/// <summary>
/// Why the computer would answer the way it would — one sentence, and the parts it was built
/// from (BUILD-PLAN P24.2).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Winner versus runner-up, and never a ranking table.</b> A table of every candidate with
/// all three keys renders the <em>decision procedure</em> instead of the decision, and what an
/// expert argues with is a claim. The sentence is assembled from <c>[0]</c> and <c>[1]</c> — the
/// first key on which they differ, said out loud.
/// </para>
/// <para>
/// ⚠️ <b>Two cases are the interesting ones and neither is hidden.</b>
/// <see cref="NothingSeparatedThem"/> — every key tied and the hand's own order decided, so the
/// computer is <em>indifferent</em> and an expert will not be, which is one of the more valuable
/// turns this instrument can catch. And <see cref="NoRunnerUp"/>: the ranking dedupes by value,
/// so a hand holding a pair yields a shorter list than it holds cards, and a turn can offer one
/// distinct move.
/// </para>
/// <para>
/// ⚠️ <b>It is advice, and a front end gates it on hints.</b> The rule text it sits beside is
/// not gated, because a rule is not advice — the same distinction <c>HandPanel</c> draws when it
/// tells the ear about RULES.md §5.1 outside the gate.
/// </para>
/// <para>
/// ⚠️ <b>Nothing here interprets a bare number.</b> Every value read comes through
/// <see cref="KeyReading"/> and every phrase through <see cref="DiscardKey"/>, so a fourth key
/// added to a rung later cannot reach a screen as <em>"2147483647 partners"</em>.
/// </para>
/// </remarks>
/// <param name="Sentence">What to draw. Complete sentences, and safe to show as it stands.</param>
/// <param name="Advised">The card the computer would throw, on the question where there is one.</param>
/// <param name="RunnerUp">What it would have thrown instead, when there was anything else to throw.</param>
/// <param name="SeparatedBy">The key that decided it, or null when nothing did.</param>
/// <param name="NothingSeparatedThem">Every key tied and the hand's own order decided.</param>
/// <param name="NoRunnerUp">The turn offered one distinct move, so there was nothing to compare.</param>
public sealed record AdviceRationale(
    string Sentence,
    Card? Advised = null,
    Card? RunnerUp = null,
    DiscardKey? SeparatedBy = null,
    bool NothingSeparatedThem = false,
    bool NoRunnerUp = false)
{
    /// <summary>
    /// Why that card, said out loud — the packet's target sentence.
    /// </summary>
    /// <param name="keys">The rung's keys, strongest first.</param>
    /// <param name="scored">The rung's own ordering, best first, with the keys read off it.</param>
    /// <param name="context">The turn, for the ban and for what wins at this table.</param>
    public static AdviceRationale ForDiscard(
        IReadOnlyList<DiscardKey> keys,
        IReadOnlyList<ScoredDiscard> scored,
        TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(scored);
        ArgumentNullException.ThrowIfNull(context);

        if (scored.Count == 0)
        {
            throw new ArgumentException("A turn always offers a card to throw.", nameof(scored));
        }

        var winner = scored[0];
        var said = new StringBuilder();

        if (scored.Count == 1)
        {
            said.Append(CultureInfo.InvariantCulture, $"There is nothing to compare {Name(winner.Card)} with: ")
                .Append("it is the only distinct card this turn offers.");

            return Finish(
                said,
                context,
                new AdviceRationale(string.Empty, winner.Card, NoRunnerUp: true));
        }

        var runnerUp = scored[1];
        var separator = -1;

        for (var key = 0; key < keys.Count && separator < 0; key++)
        {
            if (Read(winner, key) is { } mine && Read(runnerUp, key) is { } theirs && !mine.Equals(theirs))
            {
                separator = key;
            }
        }

        for (var key = 0; key < (separator < 0 ? keys.Count : separator); key++)
        {
            if (Read(winner, key) is { } tied && Read(runnerUp, key) is { } also && tied.Equals(also))
            {
                said.Append(Both(keys[key], winner.Card, runnerUp.Card, tied)).Append(' ');
            }
        }

        if (separator < 0)
        {
            said.Append(CultureInfo.InvariantCulture,
                $"Nothing separated them: {Name(winner.Card)} and {Name(runnerUp.Card)} score the same on every ")
                .Append("count, so the computer simply took the first. It has no real preference here — you may well have one.");

            return Finish(
                said,
                context,
                new AdviceRationale(string.Empty, winner.Card, runnerUp.Card, NothingSeparatedThem: true));
        }

        said.Append(Apart(keys[separator], winner.Card, Read(winner, separator)!.Value, runnerUp.Card, Read(runnerUp, separator)!.Value))
            .Append(" That is why.");

        return Finish(
            said,
            context,
            new AdviceRationale(string.Empty, winner.Card, runnerUp.Card, keys[separator]));
    }

    /// <summary>
    /// Why take that card, or why dig in the deck instead — a <b>gain</b>, because nothing is
    /// ranked here (BUILD-PLAN P24.2).
    /// </summary>
    /// <param name="offered">What is on offer, or null when nothing is.</param>
    /// <param name="melded">How many of the hand meld now.</param>
    /// <param name="melds">How many would meld with it taken.</param>
    /// <param name="taking">Whether the computer would take it.</param>
    public static AdviceRationale ForTake(Card? offered, int melded, int melds, bool taking)
    {
        if (offered is not { } card)
        {
            return new AdviceRationale(
                "Nobody has thrown anything yet, so there is nothing to take — the deck is the only way to a card.");
        }

        return new AdviceRationale(
            taking
                ? $"Taking {Name(card)} raises the cards of your hand that meld from {melded} to {melds}, "
                  + "which is worth the draw it costs you."
                : $"{Name(card)} melds nothing new — you would still have {melded} cards melding — so the "
                  + "computer would spend the turn on the deck instead, where a card might.",
            Advised: card);
    }

    /// <summary>Why claim the turned-up money card, or leave it (RULES.md §4.5).</summary>
    /// <param name="turnedUp">The claimable card, or null when there is none.</param>
    /// <param name="melded">How many of the hand meld now.</param>
    /// <param name="melds">How many would meld with it claimed.</param>
    /// <param name="claiming">Whether the computer would claim it.</param>
    public static AdviceRationale ForClaim(Card? turnedUp, int melded, int melds, bool claiming)
    {
        if (turnedUp is not { } card)
        {
            return new AdviceRationale("There is nothing on the table to claim.");
        }

        return new AdviceRationale(
            claiming
                ? $"Claiming {Name(card)} raises the cards of your hand that meld from {melded} to {melds}, "
                  + "which is worth the draw it costs you. It still pays nobody."
                : $"{Name(card)} melds nothing new — you would still have {melded} cards melding — and it "
                  + "costs you the draw and pays nobody, so the computer would leave it.",
            Advised: card);
    }

    /// <summary>
    /// Why refuse a claim, and the honest answer, which is that it does not matter much.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A null makes the explanation more interesting rather than less.</b> Every rung
    /// refuses whenever it may and none prices the disclosure (P28); P29 then measured the whole
    /// rule at nothing either way on win rate and on money (docs/STRATEGY.md §12). That is a
    /// thing this project knows and no player does, so it is what the sentence says.
    /// </remarks>
    /// <param name="refusing">Whether the computer would refuse.</param>
    public static AdviceRationale ForObjection(bool refusing) =>
        new(
            (refusing
                ? "The computer refuses whenever it is allowed to: letting the claim through closes that rank "
                  + "against you for the rest of the round. "
                : "The computer would allow it. ")
            + "But this one has been measured, and refusing is worth nothing either way — the difference is "
            + "inside the margin of error. It is genuinely yours to call, and what it really costs you is "
            + "telling the table you are holding that rank.");

    /// <summary>
    /// Why declare — which is near enough no reason at all, and says so.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Said plainly rather than dressed up.</b> Every rung's <c>Declare</c> is
    /// <c>=&gt; true</c> and the engine only asks when the hand already wins, so there is no
    /// judgement here to render. An explanation that sounded like one would be the same class of
    /// failure as justifying a difficulty level's deliberate slip: confidently right-looking and
    /// false.
    /// </remarks>
    public static AdviceRationale ForDeclaration(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new AdviceRationale(
            "There is no judgement here: the table only asks this when all thirteen already meld, and "
            + "nothing but a declaration ends a round — so the computer always says yes. "
            + WhatWinsHere(context.Rules));
    }

    /// <summary>
    /// The two clauses a discard sentence ends with, in the order they are read: what the rules
    /// took out of the choice, and what wins at this table.
    /// </summary>
    private static AdviceRationale Finish(StringBuilder said, TurnContext context, AdviceRationale parts)
    {
        // ⚠️ Per turn, and never from a ban worked out earlier in the round (P24.2 acceptance 7).
        // Where RULES.md §5.1's floor yields, every card is throwable again and this says nothing —
        // which is the one situation an explanation computed from a stale ban would get confidently
        // wrong.
        var banned = context.Hand.Count - context.LegalDiscards.Count;

        if (banned > 0)
        {
            said.Append(CultureInfo.InvariantCulture,
                $" {(banned == 1 ? "One" : banned.ToString(CultureInfo.InvariantCulture))} of your cards ")
                .Append(banned == 1 ? "was" : "were")
                .Append(" not in the choice at all: the player ")
                .Append("after you took ")
                .Append(banned == 1 ? "that rank" : "those ranks")
                .Append(" in the open, so you may not throw ")
                .Append(banned == 1 ? "it" : "them")
                .Append(". That is a rule and not the computer's opinion.");
        }

        // ⚠️ A joker is never thrown, and the true sentence is that and nothing more. It is not
        // held for RULES.md §7.3's clean bonus — no rung plays for that, and an explanation
        // implying one did would be false in the way that still looks right (P24.2 acceptance 9).
        if (context.Hand.Any(card => card.IsJoker))
        {
            said.Append(" It will never throw a joker: a joker fits into any meld there is.");
        }

        said.Append(' ').Append(WhatWinsHere(context.Rules));

        return parts with { Sentence = said.ToString() };
    }

    /// <summary>
    /// What a declaration must contain at <em>this</em> table (RULES.md §7.1.1).
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Read off the same <see cref="TableRules"/> the evaluator is, because the sentence is
    /// otherwise false at half the tables this game deals</b> (P24.2 acceptance 8, P32). A
    /// five-handed declaration owes <b>no series at all</b>, so an explanation phrased in runs is
    /// wrong at the table the browser now deals; four-handed it owes one, joker-free.
    /// </remarks>
    private static string WhatWinsHere(TableRules rules) => rules switch
    {
        { SetsAllowed: false } =>
            $"At {rules.Players} every meld must be a run — a set is not a meld at this table.",
        { RequiredSeries: 0 } =>
            $"At {rules.Players} any thirteen that all meld win, runs and sets alike.",
        { RequiredSeries: 1 } =>
            $"At {rules.Players} one of your melds must be a run, and that one must have no joker in it.",
        _ =>
            $"At {rules.Players} {rules.RequiredSeries} of your melds must be runs, "
            + "and those must have no joker in them."
    };

    private static KeyReading? Read(ScoredDiscard candidate, int key) =>
        key < candidate.Keys.Count ? candidate.Keys[key] : null;

    private static string Both(DiscardKey key, Card one, Card other, KeyReading reading) =>
        key.Subject == DiscardKeySubject.WhatIsLeftBehind
            ? $"{Name(one)} and {Name(other)} both leave {Value(key, reading)}."
            : $"{Name(one)} and {Name(other)} both have {Value(key, reading)}.";

    /// <remarks>
    /// ⚠️ <b>The second half names the number and not the noun again.</b> <em>"Q♣ leaves 10 cards
    /// of the pack that would improve the hand; 2♠ leaves 8 cards of the pack that would improve
    /// the hand"</em> is the same clause twice, and a sentence a person stops reading is a
    /// sentence that did not explain anything. A sentinel keeps its phrase, because there is no
    /// number to fall back to.
    /// </remarks>
    private static string Apart(DiscardKey key, Card one, KeyReading mine, Card other, KeyReading theirs) =>
        key.Subject == DiscardKeySubject.WhatIsLeftBehind
            ? $"{Name(one)} leaves {Value(key, mine)}; {Name(other)} leaves {Shorter(key, theirs)}."
            : $"{Name(one)} has {Value(key, mine)}; {Name(other)} has {Shorter(key, theirs)}.";

    /// <summary>
    /// A key's value as words — <b>never the packed number</b>. A sentinel reads as the phrase
    /// its key carries, which is what keeps <see cref="int.MaxValue"/> off the felt.
    /// </summary>
    private static string Value(DiscardKey key, KeyReading reading) =>
        reading.IsBeyondMeasure
            ? key.BeyondMeasure ?? "no number at all"
            : $"{reading.Value.ToString(CultureInfo.InvariantCulture)} {key.Name}";

    /// <summary>The same value with the noun left off, for the second half of a comparison.</summary>
    private static string Shorter(DiscardKey key, KeyReading reading) =>
        reading.IsBeyondMeasure
            ? key.BeyondMeasure ?? "no number at all"
            : reading.Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// A card as it is written on the felt — <c>3♠</c> — and a joker by name, because
    /// <c>🃏Red</c> is an identity and not something to read aloud in a sentence.
    /// </summary>
    private static string Name(Card card) =>
        card.IsJoker ? "the joker" : card.ToString();
}
