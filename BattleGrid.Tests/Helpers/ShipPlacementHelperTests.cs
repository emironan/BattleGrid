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
}