using BattleGrid.Domain.GameLogic;
using Xunit;

namespace BattleGrid.Tests.GameLogic;

public class CoordinateTests
{
    [Fact]
    public void Coordinate_Should_Create_Valid_Coordinate()
    {
        var coord = new Coordinate(3, 5);

        Assert.Equal(3, coord.X);
        Assert.Equal(5, coord.Y);
    }

    /* Instead of throwing an exception, we need to consider a validation method that pushes the player to 
     *  place ships within the grid boundaries. This way, we can provide feedback to the player 
     *  and allow them to correct their input without crashing the game.
     */
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(10, 0)]
    [InlineData(0, 10)]
    public void Coordinate_Should_Throw_When_OutOfBounds(int x, int y)
    {
        Assert.Throws<ArgumentException>(() => new Coordinate(x, y));
    }
}