using BattleGrid.Domain.Enums;
using BattleGrid.Domain.GameLogic;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace BattleGrid.Tests.GameLogic;

public class GameStateTests
{
    [Fact]
    public void GamePhase_At_Start_Should_Be_In_ShipPlacement_Phase()
    {
        var game = new GameState(1, 2, 2);

        Assert.Equal(GamePhase.ShipPlacement, game.Phase);      // After both players have joined, the game should transition to ShipPlacement phase
    }

    /* Corresponding part of the actual code (and ofcourse, some of these tests, as well) needs a re-write
     * Currently, the game does not get the ship list from DB and requires a pre-determined number of ships to be placed.
     * For testing purposes, it is defined as 2 ships per player in GameState class, and the test is written accordingly. 
     * This is not ideal, and we should ideally fetch the ship list from DB and place them accordingly.
     */
    [Fact]
    public void Game_Should_Start_When_Both_Players_Place_All_Ships()
    {
        var game = new GameState(1, 2, 2);

        var ship1 = ShipPlacementHelper.Generate(new Coordinate(0, 0), 1, false);
        var ship2 = ShipPlacementHelper.Generate(new Coordinate(1, 0), 1, false);

        game.PlaceShip(1, ship1);
        game.PlaceShip(2, ship1);
        game.PlaceShip(1, ship2);
        game.PlaceShip(2, ship2);

        Assert.Equal(GamePhase.InProgress, game.Phase);     // After both players have placed all their ships, the game should transition to InProgress phase
    }

    [Fact]
    public void GameState_Should_Start_With_Player1_Turn()
    {
        var game = new GameState(1, 2, 2);

        // First place ships for both players to transition to InProgress phase
        var ship1 = ShipPlacementHelper.Generate(new Coordinate(0, 0), 1, false);
        var ship2 = ShipPlacementHelper.Generate(new Coordinate(1, 0), 1, false);

        game.PlaceShip(1, ship1);
        game.PlaceShip(2, ship1);
        game.PlaceShip(1, ship2);
        game.PlaceShip(2, ship2);

        Assert.Equal(1, game.CurrentTurnPlayerId);      // Game always starts with Player 1's turn after transitioning to InProgress phase
    }

    [Fact]
    public void Game_Should_Switch_Turns_On_Miss()
    {
        var game = new GameState(1, 2, 2);

        // First place all ships for both players to transition to InProgress phase
        var ship1 = ShipPlacementHelper.Generate(new Coordinate(0, 0), 1, false);
        var ship2 = ShipPlacementHelper.Generate(new Coordinate(1, 0), 1, false);

        game.PlaceShip(1, ship1);
        game.PlaceShip(2, ship1);
        game.PlaceShip(1, ship2);
        game.PlaceShip(2, ship2);

        // Player 1 fires a shot that misses
        game.FireShot(1, new Coordinate(5, 5));

        // Turn should switch to Player 2
        Assert.Equal(2, game.CurrentTurnPlayerId);
    }

    [Fact]
    public void GameState_Should_Alternate_Turns_On_Miss()
    {
        var game = new GameState(1, 2, 2);

        // First place ships for both players to transition to InProgress phase
        var ship1 = ShipPlacementHelper.Generate(new Coordinate(0, 0), 1, false);
        var ship2 = ShipPlacementHelper.Generate(new Coordinate(1, 0), 1, false);

        game.PlaceShip(1, ship1);
        game.PlaceShip(2, ship1);
        game.PlaceShip(1, ship2);
        game.PlaceShip(2, ship2);

        game.FireShot(1, new Coordinate(5, 5)); // Player 1 misses
        game.FireShot(2, new Coordinate(6, 6)); // Player 2 misses

        Assert.Equal(1, game.CurrentTurnPlayerId);      // After both players missed, it should be Player 1's turn again
    }

    [Fact]
    public void Game_Should_Not_Switch_Turn_After_Hit()
    {
        var game = new GameState(1, 2, 2);

        // First place ships for both players to transition to InProgress phase
        var ship1 = ShipPlacementHelper.Generate(new Coordinate(0, 0), 1, false);
        var ship2 = ShipPlacementHelper.Generate(new Coordinate(1, 0), 1, false);

        game.PlaceShip(1, ship1);
        game.PlaceShip(2, ship1);
        game.PlaceShip(1, ship2);
        game.PlaceShip(2, ship2);

        game.FireShot(1, new Coordinate(0, 0));         // Player 1 fires a shot that hits Player 2's ship

        Assert.Equal(1, game.CurrentTurnPlayerId);      // Turn should remain with Player 1
    }

    [Fact]
    public void GameState_Should_Throw_When_Wrong_Player_Shoots()
    {
        var game = new GameState(1, 2, 2);

        // First place ships for both players to transition to InProgress phase
        var ship1 = ShipPlacementHelper.Generate(new Coordinate(0, 0), 1, false);
        var ship2 = ShipPlacementHelper.Generate(new Coordinate(1, 0), 1, false);

        game.PlaceShip(1, ship1);
        game.PlaceShip(2, ship1);
        game.PlaceShip(1, ship2);
        game.PlaceShip(2, ship2);

        game.FireShot(1, new Coordinate(5, 5));             // Player 1 misses, turn switches to Player 2
        Assert.Throws<InvalidOperationException>(() =>      // Player 1 tries to shoot, it should throw an exception
            game.FireShot(1, new Coordinate(3, 3)));
    }

    [Fact]
    public void GameState_Should_Not_Switch_Turn_On_Win()
    {
        var game = new GameState(1, 2, 2);

        // First place ships for both players to transition to InProgress phase
        var ship1 = ShipPlacementHelper.Generate(new Coordinate(0, 0), 1, false);
        var ship2 = ShipPlacementHelper.Generate(new Coordinate(1, 0), 1, false);

        game.PlaceShip(1, ship1);
        game.PlaceShip(2, ship1);
        game.PlaceShip(1, ship2);
        game.PlaceShip(2, ship2);

        game.FireShot(1, new Coordinate(0, 0)); // Hit
        game.FireShot(1, new Coordinate(1, 0)); // Hit and win

        Assert.Equal(1, game.CurrentTurnPlayerId);
    }

    [Fact]
    public void Game_Should_End_When_A_Player_Wins()
    {
        var game = new GameState(1, 2, 2);

        // First place ships for both players to transition to InProgress phase
        var ship1 = ShipPlacementHelper.Generate(new Coordinate(0, 0), 1, false);
        var ship2 = ShipPlacementHelper.Generate(new Coordinate(1, 0), 1, false);

        game.PlaceShip(1, ship1);
        game.PlaceShip(2, ship1);
        game.PlaceShip(1, ship2);
        game.PlaceShip(2, ship2);

        game.FireShot(1, new Coordinate(5, 5)); // Player 1 misses. Turn switches to Player 2
        game.FireShot(2, new Coordinate(0, 0)); // Player 2 Hit
        game.FireShot(2, new Coordinate(1, 0)); // Player 2 Hit and win

        Assert.Equal(GamePhase.P2Won, game.Phase);
    }
}