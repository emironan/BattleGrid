namespace BattleGrid.Contracts.ResponseDtos;

public sealed class AccountSettingsUpdateResponseDto
{
    public string Message { get; init; } = string.Empty;
    public UserResponseDto? User { get; init; }
}
