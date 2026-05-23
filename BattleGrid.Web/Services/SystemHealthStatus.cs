namespace BattleGrid.Web.Services;

public sealed record SystemHealthStatus(bool ApiOnline, bool? DatabaseOnline);
