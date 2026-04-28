using BattleGrid.Application.Interfaces;
using BattleGrid.Infrastructure.Data;


namespace BattleGrid.Application.Services
{

    public class ReplayServices : IReplayServices
    {
        private readonly BattleGridDbContext _context;

        public ReplayServices(BattleGridDbContext context)
        {
            _context = context;
        }
        
        /*
        // Dto should a least include PlayerID, MatchID
        public async Task<GeneralResponseDto> Replay(StartReplayRequestDto dto)
        {
            /* Check PlayerID to make sure it is an admin OR a player trying to replay their own match
             * Then, take all of the ship placement data and match moves data IN ORDER from DB
             * User can click buttons to initiate actions below:
             *   - See next placement
             *   - Play automatically. Which, should show every placement and move in order with small delay after each one
             *   - Move to game start (placements are finished and first shot is fired (first move is made)
             *     - Should automatically move to game start after the last ship placement
             *     - After game start, user can click on either
             *       - Next move or
             *       - Again, play automatically.
             *   - Restart from the beginning and/or restart from the game start(after ships are placed)
             *   - When the replay ends we should allow user to stay on the same page and restart the replay etc
             /
        } */
    }
}