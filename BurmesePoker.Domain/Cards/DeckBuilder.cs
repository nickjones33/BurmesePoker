namespace BurmesePoker.Domain.Cards;

/// <summary>
/// Builds the shoe: two standard decks shuffled together, jokers included — 108 cards
/// (RULES.md §2).
/// </summary>
public static class DeckBuilder
{
    /// <summary>Cards in one standard deck: 52 ranked plus a red and a black joker.</summary>
    public const int CardsPerDeck = 54;

    /// <summary>Cards in the shoe: two decks.</summary>
    public const int TotalCards = CardsPerDeck * 2;

    /// <summary>
    /// Builds the 108-card shoe in a fixed, unshuffled order, with sequential
    /// <see cref="CardId"/>s 0..107. Shuffling is <see cref="Deck.Shuffle"/>'s job.
    /// </summary>
    public static IReadOnlyList<Card> BuildTwoDecks()
    {
        var cards = new List<Card>(TotalCards);

        for (var deck = 0; deck < 2; deck++)
        {
            foreach (var suit in CardText.AllSuits)
            {
                foreach (var rank in CardText.AllRanks)
                {
                    cards.Add(Card.Ranked(new CardId(cards.Count), rank, suit));
                }
            }

            cards.Add(Card.Joker(new CardId(cards.Count), CardColor.Red));
            cards.Add(Card.Joker(new CardId(cards.Count), CardColor.Black));
        }

        return cards;
    }
}
