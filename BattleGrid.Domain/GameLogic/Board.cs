using BattleGrid.Domain.Enums;

namespace BattleGrid.Domain.GameLogic;

public class Board
{
    private const int Size = 10;

    private readonly CellState[,] _grid = new CellState[Size, Size];
    private readonly Dictionary<int, int> _shipHealth = new();
    private readonly Dictionary<(int x, int y), int> _shipMap = new();

    private int _nextShipId = 1;

    public void PlaceShip(IEnumerable<Coordinate> coordinates)
    {
        var shipId = _nextShipId++;
        int length = 0;

        foreach (var c in coordinates)
        {
            if (_grid[c.X, c.Y] != CellState.Empty)
                throw new InvalidOperationException("Ship overlap.");

            _grid[c.X, c.Y] = CellState.Ship;
            _shipMap[(c.X, c.Y)] = shipId;

            length++;
        }

        _shipHealth[shipId] = length;
    }

    public ShotResult FireShot(Coordinate coord)
    {
        var state = _grid[coord.X, coord.Y];

        if (state == CellState.Hit || state == CellState.Miss || state == CellState.Sunk)
            //throw new InvalidOperationException("Cell already targeted.");
            return ShotResult.AlreadyTargeted;

        if (state == CellState.Empty)
        {
            _grid[coord.X, coord.Y] = CellState.Miss;
            return ShotResult.Miss;
        }

        if (state == CellState.Ship)
        {
            _grid[coord.X, coord.Y] = CellState.Hit;

            var shipId = _shipMap[(coord.X, coord.Y)];
            _shipHealth[shipId]--;

            if (IsShipSunk(shipId) && _shipHealth[shipId] <= 0)
            {
                // Mark all cells of the sunk ship as "sunk" for UI purposes
                foreach (var kvp in _shipMap.Where(kvp => kvp.Value == shipId))
                {
                    _grid[kvp.Key.x, kvp.Key.y] = CellState.Sunk;
                }
            }

            return ShotResult.Hit;
        }

        throw new InvalidOperationException("Invalid board state.");
    }

    // Function to calculate if the ship that was hit is now sunk
    public bool IsShipSunk(int shipId)
    {
        var result = _shipHealth.TryGetValue(shipId, out var health) && health <= 0;
        Console.WriteLine($"IsShipSunk: {result}");
        return result;
    }

    // Function to check if all ships of a player are sunk
    public bool AreAllShipsSunk()
    {
        var result = _shipHealth.Values.All(health => health <= 0);
        Console.WriteLine($"AreAllShipsSunk: {result}");
        return result;
    }

    public int GetShipIdAt(Coordinate coord)
    {
        if (_shipMap.TryGetValue((coord.X, coord.Y), out var shipId))
        {
            return shipId;
        }

        throw new InvalidOperationException("No ship found at the given coordinate.");
    }

    public CellState CellStateAt(Coordinate coord)
    {
        return _grid[coord.X, coord.Y];
    }
}