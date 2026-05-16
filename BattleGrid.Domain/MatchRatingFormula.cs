namespace BattleGrid.Domain;

/// <summary>
/// Zero-sum rating update from pre-match ratings: linear interpolation on |Δrating| between anchors,
/// with a flat plateau for close matches (gap ≤ 200). Higher-rated winners gain less when heavily favored;
/// lower-rated winners gain more when heavily outmatched.
/// </summary>
public static class MatchRatingFormula
{
    /// <summary> Matches with gap ≤ this use <see cref="RatingFormulaSettings.EvenMatchDelta"/> only (anchors assume this ceiling). </summary>
    private const int PlateauGapCeiling = 200;

    /// <summary> Knot points on rating gap (must align with <see cref="FavoriteMag"/> / <see cref="UpsetMag"/>). </summary>
    private static ReadOnlySpan<double> GapKnots => [200, 400, 600, 800, 1000];

    /// <summary> Win magnitude when the higher-rated player wins (positive rating change). </summary>
    private static ReadOnlySpan<double> FavoriteMag => [25, 20, 15, 10, 5];

    /// <summary> Win magnitude when the lower-rated player wins (positive rating change). </summary>
    private static ReadOnlySpan<double> UpsetMag => [25, 30, 35, 40, 45];

    /// <summary> Winner's rating delta (positive). Loser's is the negation. </summary>
    public static int ComputeWinnerRatingDelta(int winnerRating, int loserRating, RatingFormulaSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.MaxRatingGapForCurve <= 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "MaxRatingGapForCurve must be positive.");
        if (settings.MinFavoriteWinDelta < 1)
            throw new ArgumentOutOfRangeException(nameof(settings), "MinFavoriteWinDelta must be at least 1.");
        if (settings.EvenMatchDelta < 1)
            throw new ArgumentOutOfRangeException(nameof(settings), "EvenMatchDelta must be at least 1.");

        var wr = PlayerRatingBounds.ClampRating(winnerRating);
        var lr = PlayerRatingBounds.ClampRating(loserRating);

        var gap = Math.Abs(wr - lr);
        var cappedGap = Math.Min(gap, settings.MaxRatingGapForCurve);

        var upset = wr < lr;
        double magnitude;
        if (cappedGap <= PlateauGapCeiling)
            magnitude = settings.EvenMatchDelta;
        else if (upset)
            magnitude = InterpolateBeyondPlateau(cappedGap, UpsetMag);
        else
            magnitude = InterpolateBeyondPlateau(cappedGap, FavoriteMag);

        var delta = (int)Math.Round(magnitude);
        if (!upset && cappedGap > PlateauGapCeiling)
            delta = Math.Max(settings.MinFavoriteWinDelta, delta);

        return Math.Max(1, delta);
    }

    /// <returns> Rating deltas applied as Player1Delta / Player2Delta (loser gets the opposite of winner magnitude). </returns>
    public static (int Player1Delta, int Player2Delta) ComputeDeltasForPlayers(
        int player1Rating,
        int player2Rating,
        bool player1Won,
        RatingFormulaSettings settings)
    {
        if (player1Won)
        {
            var w = ComputeWinnerRatingDelta(player1Rating, player2Rating, settings);
            return (w, -w);
        }

        var win = ComputeWinnerRatingDelta(player2Rating, player1Rating, settings);
        return (-win, win);
    }

    /// <summary>
    /// gap &gt; <see cref="PlateauGapCeiling"/>; interpolates linearly between knots (first knot at gap 200 matches plateau edge).
    /// </summary>
    private static double InterpolateBeyondPlateau(double gap, ReadOnlySpan<double> magnitudesAtKnots)
    {
        var x0 = GapKnots[0];
        var y0 = magnitudesAtKnots[0];

        if (gap <= GapKnots[1])
            return LinearInterp(gap, x0, y0, GapKnots[1], magnitudesAtKnots[1]);

        for (var i = 1; i < GapKnots.Length - 1; i++)
        {
            if (gap <= GapKnots[i + 1])
                return LinearInterp(gap, GapKnots[i], magnitudesAtKnots[i], GapKnots[i + 1], magnitudesAtKnots[i + 1]);
        }

        return magnitudesAtKnots[^1];
    }

    private static double LinearInterp(double x, double x0, double y0, double x1, double y1)
    {
        if (Math.Abs(x1 - x0) < double.Epsilon)
            return y0;
        return y0 + (y1 - y0) * (x - x0) / (x1 - x0);
    }
}
