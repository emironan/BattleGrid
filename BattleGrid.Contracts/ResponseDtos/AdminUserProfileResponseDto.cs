namespace BattleGrid.Contracts.ResponseDtos;

/// <summary> Administrator view of an operator profile (identity + competitive history). </summary>
public sealed class AdminUserProfileResponseDto
{
    public UserResponseDto User { get; init; } = null!;
    public int GlobalCurrentSeasonNo { get; init; }
    public IReadOnlyList<PlayerSeasonStatsResponseDto> SeasonStats { get; init; } = Array.Empty<PlayerSeasonStatsResponseDto>();

    public UserBanStatusResponseDto BanStatus { get; init; } = new();
}
