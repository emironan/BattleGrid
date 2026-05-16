namespace BattleGrid.Domain;

/// <summary> Absolute limits for competitive rating (current and historical peak). </summary>
public static class PlayerRatingBounds
{
    public const int Minimum = 400;
    public const int Maximum = 3000;

    /// <summary> Default rating for a new competitive season row (before first match of that row applies deltas). </summary>
    public const int DefaultStartingRating = 1000;

    public static int ClampRating(int rating) => Math.Clamp(rating, Minimum, Maximum);

    /// <summary> Peak rating after an update: at least the new current rating, never above <see cref="Maximum"/>. </summary>
    public static int ClampHighestAfterCurrent(int highestSoFar, int newCurrentRating) =>
        ClampRating(Math.Max(highestSoFar, newCurrentRating));
}
