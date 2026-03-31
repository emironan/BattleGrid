using BattleGrid.Domain.Enums;
using BattleGrid.Domain.GameLogic;
using BattleGrid.Tests.Helpers;
using System.Drawing;
using System.Numerics;
using Xunit;

namespace BattleGrid.Tests.GameLogic;

public class BoardTests
{
    [Fact]
    public void Board_Should_Register_Miss()
    {
        var board = new Board();

        var ship = TestShipFactory.Horizontal(0, 0, 2);
        board.PlaceShip(ship);

        var result = board.FireShot(new Coordinate(5, 5));
        var cellState = board.CellStateAt(new Coordinate(5, 5));

        Assert.Equal(ShotResult.Miss, result);      // Shot at (5,5) - should be a miss
        Assert.Equal(CellState.Miss, cellState);    // CellState should be Miss after the shot, as well
    }

    [Fact]
    public void Board_Should_Register_Hit()
    {
        var board = new Board();

        var ship = TestShipFactory.Horizontal(0, 0, 2);
        board.PlaceShip(ship);

        var result = board.FireShot(new Coordinate(0, 0));
        var cellState = board.CellStateAt(new Coordinate(0, 0));

        Assert.Equal(ShotResult.Hit, result);       // Shot at (0,0) - should be a hit
        Assert.Equal(CellState.Hit, cellState);     // CellState should be Hit after the shot, as well
    }

    [Fact]
    public void Board_Should_Detect_AlreadyTargeted_Cells()
    {
        var board = new Board();

        var ship = TestShipFactory.Horizontal(0, 0, 2);
        board.PlaceShip(ship);

        var result1 = board.FireShot(new Coordinate(0, 0));         
        var cellState1 = board.CellStateAt(new Coordinate(0, 0));   
        var result2 = board.FireShot(new Coordinate(0, 0));
        var cellState2 = board.CellStateAt(new Coordinate(0, 0));   

        var result3 = board.FireShot(new Coordinate(5, 5));         
        var cellState3 = board.CellStateAt(new Coordinate(5, 5));   
        var result4 = board.FireShot(new Coordinate(5, 5));         
        var cellState4 = board.CellStateAt(new Coordinate(5, 5));   

        Assert.Equal(ShotResult.Hit, result1);                  // First shot at (0,0) - should be a hit
        Assert.Equal(CellState.Hit, cellState1);                // CellState should be Hit after first shot
        Assert.Equal(ShotResult.AlreadyTargeted, result2);      // Second shot at (0,0) - should be already targeted
        Assert.Equal(CellState.Hit, cellState2);                // CellState should not change after second shot

        Assert.Equal(ShotResult.Miss, result3);                 // First shot at (5,5) - should be a miss
        Assert.Equal(CellState.Miss, cellState3);               // CellState should be Miss after first shot
        Assert.Equal(ShotResult.AlreadyTargeted, result4);      // Second shot at (5,5) - should be already targeted
        Assert.Equal(CellState.Miss, cellState4);               // CellState should not change after second shot
    }

    /* We will change this behaviour later to return a bool instead of throwing an exception
     * When a Player tries to place a ship that overlaps with an already placed ship, we should just play a warning sound
     *  and let the player continue and try placing it in a valid spot, instead of throwing an error
     */
    [Fact]
    public void Board_Should_Not_Allow_Overlapping_Ships()
    {
        var board = new Board();

        var ship1 = TestShipFactory.Horizontal(0, 0, 3);
        var ship2 = TestShipFactory.Vertical(0, 0, 3);

        board.PlaceShip(ship1);

        // Attempting to place ship2 which overlaps with ship1 should throw an exception
        Assert.Throws<InvalidOperationException>(() =>
            board.PlaceShip(ship2)                          
        );
    }

    [Fact]
    public void Board_Should_Detect_Hits_On_Correct_Ship()
    {
        var board = new Board();

        var ship1 = TestShipFactory.Horizontal(0, 0, 1);
        var ship2 = TestShipFactory.Horizontal(5, 5, 2);

        board.PlaceShip(ship1);
        board.PlaceShip(ship2);

        board.FireShot(new Coordinate(6, 5));

        var result = board.CellStateAt(new Coordinate(6, 5));
        var otherShipsCoordinates = board.CellStateAt(new Coordinate(0, 0));

        Assert.Equal(CellState.Hit, result);                    // Shot at (6,5) - should be a hit on ship2
        Assert.Equal(CellState.Ship, otherShipsCoordinates);    // CellState at the coordinates of ship1 should still be Ship, as it was not hit
    }

    [Fact]
    public void Board_Should_Handle_Multiple_Misses()
    {
        var board = new Board();

        var ship = TestShipFactory.Horizontal(0, 0, 2);
        board.PlaceShip(ship);

        var result1 = board.FireShot(new Coordinate(1, 1));
        var result2 = board.FireShot(new Coordinate(2, 2));
        var result3 = board.FireShot(new Coordinate(3, 3));

        Assert.Equal(ShotResult.Miss, result1);     // All these shots are at empty coordinates, so they should all be misses
        Assert.Equal(ShotResult.Miss, result2);
        Assert.Equal(ShotResult.Miss, result3);
    }
}