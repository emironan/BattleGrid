using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Entities;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace BattleGrid.Application.Services
{

    public class AdminServices : IAdminServices
    {
        private readonly BattleGridDbContext _context;
        private readonly IUserServices _userServices;
        private readonly INormalizationHelper _normalizationHelper;

        public AdminServices(BattleGridDbContext context, 
                             IUserServices userServices,
                             INormalizationHelper normalizationHelper)
        {
            _context = context;
            _userServices = userServices;
            _normalizationHelper = normalizationHelper;
        }

        // NOTE: This service assumes User.IsBanned and BanList entries are in sync and up-to-date!
        public async Task<GeneralResponseDto> BanPlayerAsync(BanRequestDto dto)
        {
            //aaa Ban a player
            /* +1 - Check if the AdminID is valid
             * 
             * +2 - Check if the Player info is valid
             * 
             * +3 - Check if the player is already banned
             * What we do here depends on different things
             * 
             *   +3.1 - If the player is already permanently banned; what is the point of a temporary ban
             *   or another permanent ban? Do NOTHING!
             *   
             *   +3.2 - If the player is temporarily banned and the admin wants to ban that player permanently, 
             *   we can go ahead and create a new record for permanent ban.
             *   Since we are checking for the last ban entry for any player before preventing certain actions
             *   or updating BanList entries, this should be fine
             *   
             *   +3.3 - If the player is temporarily banned and the admin wants to issue another temporary ban
             *   we can simply choose the longer duration and apply it to the existing ban
             *   
             * +4 - Update User and BanList tables accordingly
             */

            // Create the response with default and/or null values
            var response = new GeneralResponseDto
            {
                Success = false,        // False by default
                Message = string.Empty  // Empty. It will be filled based on where do we return
            };

            var admin = await _userServices.GetByIdAsync(dto.AdminID);

            // Check if the AdminID is valid and belongs to an actual admin
            if (admin == null || !admin.IsAdmin)
            {
                response.Message = "You do NOT have permissions to complete this action!";
                return response;
            }

            // Normalize the username or email
            dto.PlayerInfo = await _normalizationHelper.NormalizeLoginInfoAsync(dto.PlayerInfo);

            // We get the user with the DbContext instead of UserServices because we may update its IsBanned and IsActive fields.
            var user = await _context.User
                .Where(u => u.Email == dto.PlayerInfo
                         || u.UserName == dto.PlayerInfo)
                .FirstOrDefaultAsync();

            // Check if the Player info is valid
            if (user == null)
            {
                response.Message = "There is no such user. You can not ban someone if they do not exist!";
                return response;
            }

            if (user.UserID == admin.UserID)
            {
                response.Message = "Why are you trying to ban yourself?";
                return response;
            }

            // From this point forward, we issue a ban or update an existing ban. Return result should be success
            response.Success = true;

            // If the player is already marked as banned
            if (user.IsBanned)
            {
                // Get the latest ban info on the user
                var latestBan = await _context.BanList
                    .Where(b => b.PlayerID == user.UserID)
                    .OrderByDescending(b => b.BanID)
                    .FirstOrDefaultAsync();

                if (latestBan != null) // We won't need this check as soon as we make sure User.IsBanned and BanList tables are in sync, via other means
                {

                    // If the current ban is permanent and it is not reverted, we do not need to do anything. Just return
                    if (!latestBan.IsTemporary && !latestBan.IsReverted) // Second condition won't be necessary after we make sure User.IsBanned and BanList tables are in sync
                    {
                        response.Message = "This user is already permanently banned. No further action is required.";
                        return response;
                    }

                    // If the current ban is temporary but the new ban request is for a permanent ban, 
                    //  create a new BanList entry for the new permanent ban. Old, temporary one can stay as is.
                    // Does not matter if the current ban's duration has ended or not, we are gonna create a new permanent record
                    if (latestBan.IsTemporary && !dto.IsTemporary)
                    {
                        var newBan = new BanList
                        {
                            AdminID = dto.AdminID,
                            PlayerID = user.UserID,
                            // NOTE: IsReverted will be false by default
                            // NOTE: RevertingAdminID will be null by default
                            BanReason = dto.BanReason,
                            IsTemporary = dto.IsTemporary,
                            // NOTE: BannedAt has a default value, NOW() in DB
                            // Duration = TimeSpan.FromDays(36500), // Permanently banned. 100 years by default
                            // NOTE: BannedUntil should be calculated by DB based on BannedAt and Duration fields
                            //aaa TODO: Add a SP and a trigger to calcuate and update the BannedUntil field of each new TEMPORARY ban entry
                        };

                        // Save changes to the DB
                        await _context.BanList.AddAsync(newBan);
                        await _context.SaveChangesAsync();

                        response.Message = "Previously temporarily banned player is now permanently banned!";
                        return response;
                    }

                    if (latestBan.IsTemporary && !latestBan.IsReverted && dto.IsTemporary)
                    {
                        // Increase the current ban's duration if the new ban request is longer             
                        if (latestBan.Duration < dto.Duration)
                        {
                            latestBan.Duration = dto.Duration;
                            await _context.SaveChangesAsync();

                            response.Message = "Ban duration is increased.";
                            return response;
                        }

                        response.Message = "Current ban has a longer duration. No action is taken!";
                        return response;
                    }
                }
            }

            // Player is not currently banned. Create a new BanList entry
            //aaa TODO: Remove the 'if (latestBan.IsTemporary && !dto.IsTemporary)' section. Since, it can simply be handled here
            //aaa Player is temporarily banned, we are issuing a permanent ban. Create a new BanList entry
            //
            var banEntry = new BanList
            {
                AdminID = dto.AdminID,
                PlayerID = user.UserID,
                // NOTE: IsReverted will be false by default
                // NOTE: RevertingAdminID will be null by default
                BanReason = dto.BanReason,
                IsTemporary = dto.IsTemporary, // false by default, meaning permanent ban!
                BannedAt = dto.BannedAt,
                Duration = dto.Duration, // 100 years by default, meaning permanent ban!
                // NOTE: BannedUntil should be calculated by DB based on BannedAt and Duration fields. For now, let's set it with the DTO
                BannedUntil = dto.BannedUntil
                //aaa TODO: Add a SP and a trigger to calcuate and update the BannedUntil field of each new TEMPORARY ban entry
            };

            // Update the User table entry, as well
            user.IsBanned = true;
            user.LastUpdatedAt = dto.BannedAt;
            string banType = (dto.IsTemporary) ? "temporarily" : "permanently";
            user.UpdateReason = $"User is banned {banType}";

            // Save changes to the DB
            await _context.BanList.AddAsync(banEntry);
            await _context.SaveChangesAsync();

            response.Message = $"Player {dto.PlayerInfo} is banned for {dto.Duration} days.";
            return response;
        }
                

        /*
        public async Task<GeneralResponseDto> UndoPermanentBan(UndoPermanentBanRequestDto dto)
        {
            //aaa TODO: Undo a permanent ban
            /* 1 - Check if the AdminID is valid
             * 2 - Check if the PlayerID is valid
             * 3 - Check if the ban record is valid / player is actually banned
             *  3.1 - Make sure it is a permanent ban
             * 4 - Undo the ban. Do not forget to update the RevertingAdminID field of the BanList table
             * We may need it for audit purposes
             *
             
            // Do not forget to update User.IsBanned, as well 
            user.IsBanned = false;
            user.LastUpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime();
            user.UpdateReason = "Permanent ban is reverted by admins!";

            await _context.SaveChangesAsync();
            *
            *
        }
        */

        /*
        public async Task<GeneralResponseDto> UpdateBanStatus(UpdateBanStatusRequestDto dto)
        {
            //aaa TODO: Update ban status accross User and BanList tables
            /* 1 - Check if the AdminID is valid //***AdminID = 0 means background services made the changes automatically
             * 2 - Check if the PlayerID is valid
             * 3 - Update the ban status accordingly. Make sure to update both User and BanList tables when required
             *
        }
        */
    }
}