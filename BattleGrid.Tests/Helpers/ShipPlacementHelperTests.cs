using BattleGrid.Domain.GameLogic;
using Xunit;

namespace BattleGrid.Tests.GameLogic;

public class ShipPlacementHelperTests
{
    [Fact]
    public void Should_Generate_Horizontal_Coordinates()
    {
        var start = new Coordinate(2, 3);

        var coords = ShipPlacementHelper.Generate(start, 3, false).ToList();

        Assert.Contains(new Coordinate(2, 3), coords);
        Assert.Contains(new Coordinate(3, 3), coords);
        Assert.Contains(new Coordinate(4, 3), coords);
    }

    [Fact]
    public void Should_Generate_Vertical_Coordinates()
    {
        var start = new Coordinate(2, 3);

        var coords = ShipPlacementHelper.Generate(start, 3, true).ToList();

        Assert.Contains(new Coordinate(2, 3), coords);
        Assert.Contains(new Coordinate(2, 4), coords);
        Assert.Contains(new Coordinate(2, 5), coords);
    }

    [Fact]
    public void TryGenerateRectangle_Horizontal_2x3()
    {
        var ok = ShipPlacementHelper.TryGenerateRectangle(new Coordinate(1, 2), length: 3, width: 2, isVertical: false, out var cells);
        Assert.True(ok);
        Assert.Equal(6, cells!.Count);
        Assert.Contains(new Coordinate(1, 2), cells);
        Assert.Contains(new Coordinate(3, 3), cells);
    }

    [Fact]
    public void TryGenerateRectangle_Vertical_swaps_span()
    {
        var ok = ShipPlacementHelper.TryGenerateRectangle(new Coordinate(0, 0), length: 4, width: 2, isVertical: true, out var cells);
        Assert.True(ok);
        Assert.Equal(8, cells!.Count);
        Assert.Contains(new Coordinate(1, 0), cells);
        Assert.Contains(new Coordinate(0, 3), cells);
    }

    [Fact]
    public void TryGenerateRectangle_OutOfBounds_returns_false()
    {
        var ok = ShipPlacementHelper.TryGenerateRectangle(new Coordinate(8, 8), length: 3, width: 2, isVertical: false, out var cells);
        Assert.False(ok);
        Assert.Null(cells);
    }
}