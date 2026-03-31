using BattleGrid.Domain.Enums;

namespace BattleGrid.Domain.GameLogic;

public class GameState
{
    public int Player1Id { get; }
    public int Player2Id { get; }

    public GamePhase Phase { get; private set; }

    public int CurrentTurnPlayerId { get; private set; }

    public int? WinnerId { get; private set; }

    private readonly Board _player1Board = new();
    private readonly Board _player2Board = new();

    private const int MaxShipsPerPlayer = 2; // Define the maximum number of ships per player
    private int _p1PlacedShipCount = 0;
    private int _p2PlacedShipCount = 0;

    private bool _p1ShipsPlaced = false;
    private bool _p2ShipsPlaced = false;

    public GameState(int player1Id, int player2Id)
    {
        Phase = GamePhase.WaitingForPlayers;
        Console.WriteLine("Waiting for players to join...");

        Player1Id = player1Id;
        Player2Id = player2Id;
        Console.WriteLine($"Player {Player1Id} and Player {Player2Id} have joined the game.");

        Phase = GamePhase.ShipPlacement;
        Console.WriteLine("Ship placement phase started. Players, place your ships!");
    }

    public void PlaceShip(int playerId, IEnumerable<Coordinate> coordinates)
    {
        if (Phase != GamePhase.ShipPlacement)
            throw new InvalidOperationException("Ship placement phase is over.");

        var board = GetBoard(playerId);

        board.PlaceShip(coordinates);

        if (playerId == Player1Id)
        {
            _p1PlacedShipCount++;
            if (_p1PlacedShipCount >= MaxShipsPerPlayer)
            {
                _p1ShipsPlaced = true;
            }
        }
        else if (playerId == Player2Id)
        {
            _p2PlacedShipCount++;
            if (_p2PlacedShipCount >= MaxShipsPerPlayer)
            {
                _p2ShipsPlaced = true;
            }
        }

        if (_p1ShipsPlaced && _p2ShipsPlaced)
        {
            Phase = GamePhase.InProgress;
            CurrentTurnPlayerId = Player1Id;
        }
    }

    public ShotResult FireShot(int playerId, Coordinate coord)
    {
        if (Phase != GamePhase.InProgress)
            throw new InvalidOperationException("Game is not in progress.");

        if (playerId != CurrentTurnPlayerId)
            throw new InvalidOperationException("Not your turn.");

        var opponentBoard = playerId == Player1Id ? _player2Board : _player1Board;

        var result = opponentBoard.FireShot(coord);

        if (result == ShotResult.Miss)
        {
            SwitchTurn();
        }

        
        if (result == ShotResult.Hit)
        {
            // Check, after the hit, if all opponent ships are sunk
            var isGameFinished = IsGameFinished(playerId);
            if (isGameFinished)
            { 
                result = ShotResult.Win; 
               // Console.WriteLine($"Shot result inside: {result}");
            }
        } 
        // Console.WriteLine($"Shot result outside: {result}");
        return result;
    }

    private void SwitchTurn()
    {
        CurrentTurnPlayerId =
            CurrentTurnPlayerId == Player1Id
                ? Player2Id
                : Player1Id;
    }

    private Board GetBoard(int playerId)
    {
        if (playerId == Player1Id)
            return _player1Board;

        if (playerId == Player2Id)
            return _player2Board;

        throw new InvalidOperationException("Invalid player.");
    }

    public Board GetPlayerBoard(int playerId)
    {
        return GetBoard(playerId);
    }

    public bool IsGameFinished(int playerId)
    {
        var result = false;
        var opponentBoard = playerId == Player1Id ? _player2Board : _player1Board;
        if (opponentBoard.AreAllShipsSunk())
        {
            Phase = playerId == Player1Id ? GamePhase.P1Won : GamePhase.P2Won;
            WinnerId  = playerId;
            result = true;
        }

        return result;
    }
}