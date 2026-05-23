using BattleGrid.Contracts.RequestDtos;

namespace BattleGrid.Web.GameVisuals;

public enum ShipSegmentState
{
    Intact,
    Damaged,
    Destroyed
}

public static class ShipVisualHelper
{
    public const string BattleAssetRoot = "/battle";

    public static string SlugFromShipName(string shipName) =>
        shipName.Trim().ToLowerInvariant().Replace(' ', '-');

    public static int GetSliceIndexFromBow(PlacedShipDto placement, int cellX, int cellY, int shipLength)
    {
        var fromAnchor = placement.IsVertical
            ? cellY - placement.StartY
            : cellX - placement.StartX;

        return shipLength - 1 - fromAnchor;
    }

    public static int GetSliceIndexFromBow(
        int startX,
        int startY,
        bool isVertical,
        int cellX,
        int cellY,
        int shipLength)
    {
        var fromAnchor = isVertical ? cellY - startY : cellX - startX;
        return shipLength - 1 - fromAnchor;
    }

    public static string? GetShotFxUrl(ShotCellFxPhase phase) => phase switch
    {
        ShotCellFxPhase.MissAnimation => GetEffectAssetUrl("miss-animation"),
        ShotCellFxPhase.MissSettled => GetEffectAssetUrl("miss-mark"),
        ShotCellFxPhase.HitAnimation => GetEffectAssetUrl("hit-animation"),
        ShotCellFxPhase.HitSettled => GetEffectAssetUrl("hit-mark"),
        _ => null
    };

    /// <summary>Settled marker when reloading from server without phase data.</summary>
    public static string? GetSettledShotFxUrl(string result) => result switch
    {
        "Miss" => GetEffectAssetUrl("miss-mark"),
        "Hit" or "Sunk" or "Win" => GetEffectAssetUrl("hit-mark"),
        _ => null
    };

    public static string GetHullAssetUrl(string shipSlug) =>
        $"{BattleAssetRoot}/hulls/{shipSlug}.png";

    public static string GetEffectAssetUrl(string effectName) =>
        $"{BattleAssetRoot}/effects/{effectName}.png";

    public static string? ResolveShipSlug(
        int shipId,
        int length,
        IReadOnlyDictionary<int, string>? shipNamesById)
    {
        if (shipNamesById is not null && shipNamesById.TryGetValue(shipId, out var name) && !string.IsNullOrWhiteSpace(name))
            return SlugFromShipName(name);

        return length switch
        {
            4 => "carrier",
            5 => "battleship",
            2 => "destroyer",
            3 => shipId switch
            {
                4 => "submarine",
                _ => "cruiser"
            },
            _ => null
        };
    }

    public static string HullLayerModifierClass(ShipSegmentState state) => state switch
    {
        ShipSegmentState.Damaged => "battle-cell__ship--hull-damaged",
        ShipSegmentState.Destroyed => "battle-cell__ship--hull-destroyed",
        _ => ""
    };

    public static IReadOnlyList<string> GetPreloadEffectUrls() =>
    [
        GetEffectAssetUrl("miss-animation"),
        GetEffectAssetUrl("miss-mark"),
        GetEffectAssetUrl("hit-animation"),
        GetEffectAssetUrl("hit-mark"),
        GetEffectAssetUrl("crosshair")
    ];
}
