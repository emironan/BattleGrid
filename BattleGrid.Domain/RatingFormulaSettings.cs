namespace BattleGrid.Domain;

/// <summary>
/// Tunable parameters for <see cref="MatchRatingFormula"/> (bound from configuration under <see cref="SectionName"/>).
/// </summary>
public sealed class RatingFormulaSettings
{
    public const string SectionName = "Rating";

    /// <summary>
    /// Rating swing magnitude when |winner − loser| ≤ 200 (plateau), including equal ratings (winner +Δ / loser −Δ).
    /// </summary>
    public int EvenMatchDelta { get; set; } = 25;

    /// <summary> After rounding, the higher-rated winner gains at least this many points when the gap exceeds the plateau. </summary>
    public int MinFavoriteWinDelta { get; set; } = 5;

    /// <summary> Gaps larger than this are clamped for curve lookup (should match worst-case matchmaking spread). </summary>
    public int MaxRatingGapForCurve { get; set; } = 1000;
}
