using BurmesePoker.Domain.Melds;
using BurmesePoker.Presentation;

namespace BurmesePoker.Tests.Presentation;

/// <summary>
/// WCAG 1.4.1 as a test, taken before there is a browser client to retrofit it into
/// (BUILD-PLAN §3.11 A2).
/// </summary>
/// <remarks>
/// <para>
/// <b>Colour never carries meaning on its own.</b> The game leans on colour — red and black
/// suits, money markers, an ownership star — and the console already pairs every one of them
/// with a glyph. The standard says the browser must too, and the way to keep a standard is to
/// make breaking it fail: a display state added later with no token fails here rather than
/// shipping as a colour nobody can see.
/// </para>
/// <para>
/// The assertion is over the <em>enum</em> rather than over a list of states somebody
/// remembered to write down, which is the whole reason it holds for states that do not exist
/// yet.
/// </para>
/// </remarks>
public class DisplayTokensTests
{
    [Fact]
    public void EveryDisplayStateHasANonColourToken()
    {
        Assert.NotEmpty(DisplayTokens.States);

        foreach (var state in DisplayTokens.States)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(DisplayTokens.For(state)),
                $"{state} has no non-colour token, so it could only be shown as a colour (§3.11 A2).");
        }
    }

    [Fact]
    public void NoTwoStatesShareAToken()
    {
        var tokens = DisplayTokens.States.Select(DisplayTokens.For).ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
    }

    /// <remarks>
    /// <see cref="CardDisplayState.None"/> is the absence of a state, not a state, and a
    /// combination is several things at once. Either would be a token that lies.
    /// </remarks>
    [Fact]
    public void NothingAndEverythingHaveNoTokenOfTheirOwn()
    {
        Assert.Throws<ArgumentException>(() => DisplayTokens.For(CardDisplayState.None));
        Assert.Throws<ArgumentException>(
            () => DisplayTokens.For(CardDisplayState.Melded | CardDisplayState.Owned));
    }

    [Fact]
    public void ACardWearingSeveralStatesShowsSeveralTokens()
    {
        var tokens = DisplayTokens
            .All(CardDisplayState.Melded | CardDisplayState.PaysOnce | CardDisplayState.Owned)
            .ToList();

        Assert.Equal(
            [
                DisplayTokens.For(CardDisplayState.Melded),
                DisplayTokens.For(CardDisplayState.PaysOnce),
                DisplayTokens.For(CardDisplayState.Owned)
            ],
            tokens);
    }

    [Fact]
    public void AMeldIsNamedByWhatItIs()
    {
        Assert.Equal("run", DisplayTokens.For(MeldKind.Run));
        Assert.Equal("set", DisplayTokens.For(MeldKind.Set));
    }

    /// <remarks>
    /// Not decoration: the console's <c>Palette</c> now takes its star and its arrow from
    /// here, so both front ends mark an owned money card and the computer's suggestion the
    /// same way.
    /// </remarks>
    [Fact]
    public void TheMarkersAreTheOnesTheConsoleAlreadyUses()
    {
        Assert.Equal("★", DisplayTokens.For(CardDisplayState.Owned));
        Assert.Equal("←", DisplayTokens.For(CardDisplayState.SuggestedThrow));
        Assert.Equal("($)", DisplayTokens.For(CardDisplayState.PaysOnce));
        Assert.Equal("($$)", DisplayTokens.For(CardDisplayState.PaysDouble));
    }
}
