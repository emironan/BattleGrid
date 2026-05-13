using System.Net.Http;
using System.Text;
using System.Text.Json;

using BattleGrid.Domain.Enums;
using BattleGrid.Domain.GameLogic;

var game = new GameState(1, 2, 2);

var ship1 = ShipPlacementHelper.Generate(new Coordinate(0, 0), 2, false);
var ship2 = ShipPlacementHelper.Generate(new Coordinate(5, 5), 2, false);
var ship3 = ShipPlacementHelper.Generate(new Coordinate(2, 2), 1, true);
var ship4 = ShipPlacementHelper.Generate(new Coordinate(7, 7), 1, true);

game.PlaceShip(1, ship1);
game.PlaceShip(2, ship2);
game.PlaceShip(1, ship3);
game.PlaceShip(2, ship4);

Console.WriteLine("Game Started! \n");

while (game.Phase != (GamePhase.P1Won | GamePhase.P2Won | GamePhase.Abandoned))
{
    Console.WriteLine($"Player {game.CurrentTurnPlayerId} turn");

    Console.Write("X: ");
    int x = int.Parse(Console.ReadLine());

    Console.Write("Y: ");
    int y = int.Parse(Console.ReadLine());

    var result = game.FireShot(game.CurrentTurnPlayerId, new Coordinate(x, y));

    Console.WriteLine($"Result: {result} \n");

    // If the shot was a miss, GameState will automatically switch the turn to the other player.
    if (result == ShotResult.Miss)
    {
        Console.WriteLine("Turn ends. Switching player...");
    }

    /* If it was a hit, GameState will check if the hit sunk a ship and if all ships of the opponent are sunk
     * In that case it will set the game phase to P1Won or P2Won. Also, it will set the ShotResult to Win 
     *  and we can use it to detect if the game ended, then we can break the loop and finish the game.
     */
    if (result == ShotResult.Win)
    {
        break;
    }

    /* If the player targets a cell that was already targeted, we simply ignore it and let them try again without switching turns.
     *  This part is redundant since the Board class already returns ShotResult.AlreadyTargeted for such cases.
     *  And GameState doesn't switch turns on AlreadyTargeted results.
     */
    // Also, same applies if the ShotResult is Hit; turn doesn't switch on hits, so we want to allow the player to shoot again if they hit a ship.
    if (result == (ShotResult.AlreadyTargeted | ShotResult.Hit))
    {
        continue;
    }
}

Console.WriteLine($"Winner: Player {game.WinnerId}");