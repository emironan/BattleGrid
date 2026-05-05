using BattleGrid.Application.Helpers;
using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Entities;
using BattleGrid.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace BattleGrid.Application.Services
{

    public class AuthServices : IAuthServices
    {
        private readonly BattleGridDbContext _context;
        private readonly IUserServices _userServices;
        private readonly IJwtHelper _jwtHelper;
        private readonly INormalizationHelper _normalizationHelper;

        public AuthServices(BattleGridDbContext context, 
                            IUserServices userServices,
                            IJwtHelper jwtHelper,
                            INormalizationHelper normalizationHelper)
        {
            _context = context;
            _userServices = userServices;
            _jwtHelper = jwtHelper;
            _normalizationHelper = normalizationHelper;
        }

        public async Task<GeneralResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            // Check if a user with the same email already exists
            // Make sure email is in a proper format (trimmed and lowercase) before checking for existing users
            dto.Email = await _normalizationHelper.NormalizeLoginInfoAsync(dto.Email);

            var exists = await _context.User
                /* NOTE: We won't make any changes to this user entity in this service.
                 * We only need to read the user info and verify the email does not already exist in our DB.
                 * So, add no tracking (AsNoTracking()) to improve performance.
                 */
                .AsNoTracking()
                .AnyAsync(x => x.Email == dto.Email);

            if (exists)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "This email is already registered."
                };
            }

            // Check if the DTO's UserName field is not null and if the provided UserName already exists
            if (dto.UserName != null)
            {
                dto.UserName = await _normalizationHelper.NormalizeLoginInfoAsync(dto.UserName);

                var userNameExists = await _context.User
                    .AsNoTracking()
                    .AnyAsync(x => x.UserName == dto.UserName);

                if (userNameExists)
                {
                    return new GeneralResponseDto
                    {
                        Success = false,
                        Message = "This user name is taken."
                    };
                }
            }

            // A random and unique username generator might be helpful in the future
            /* If the UserName field is empty, generate unique UserName "U" followed by a unique number
            if (string.IsNullOrEmpty(dto.UserName)
               { 
                    var userName = await UserNameHelper.GenerateUserNameAsync(_uniqueNumberChecker);
                    if (string.IsNullOrEmpty(userName))
                        {
                            return false;
                        }
               }
            */

            // Create new user
            var newUser = new User
            {
                UserName = dto.UserName,                                        // Save the properly formatted username (if it is not null)
                Email = dto.Email,                                              // Save the properly formatted email
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)     // and the hashed password
                // The rest takes the default values
            };

            var result = await _context.User.AddAsync(newUser);
            await _context.SaveChangesAsync();

            if (result == null)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "An error occured while saving user data!"
                };
            }

            return new GeneralResponseDto
            {
                Success = true,
                Message = "User registered succesfully."
            };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
        {
            dto.LoginInfo = await _normalizationHelper.NormalizeLoginInfoAsync(dto.LoginInfo);

            // Get the user info
            var user = await _userServices.GetByLoginInfoAsync(dto.LoginInfo);

            if (user == null)
            {
                throw new InvalidOperationException("User does not exist");
            }

            // Deleted account (or maybe permanently banned account)
            if (!user.IsActive)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Account is deactivated (deleted by the user or the system)",
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    ATExpiresAt = DateTimeOffset.MinValue,
                    RTExpiresAt = DateTimeOffset.MinValue
                };
            }

            // Banned users can not even login!
            if (user.IsBanned)
            {
                // Get the latest ban record for the user
                var ban = await _context.BanList
                    .Where(b => b.PlayerID == user.UserID)
                    .OrderByDescending(b => b.BanID)
                    .FirstOrDefaultAsync();

                /* If there is no ban record or the ban is reverted, the user is not actually banned!
                 * However, it is marked as IsBanned = true in the User table!
                 * We need to call the service that checks and updates ban status among User and BanList tables
                 */
                if (ban == null || ban.IsReverted)
                {
                    throw new ArgumentNullException("User table shows user as banned. However, the possibilities are as follows: " +
                        "1) Ban's duration has ended," +
                        "2)Ban is reverted," +
                        "3)There is no ban records for this player");
                }

                //aaa TODO: We need a BanListServices to implement a service that synchronizes ban status among User and BanList tables
                /* If a ban is reverted or there is no ban record but the user table still shows as banned there is a problem!
                 * For now, above if-check throws an exception for us to check the situation
                 * 
                 * if (ban == null || ban.IsReverted)
                 * {
                 *      // Update user table with IsBanned = false
                 *      // break out of this part and continue with login
                 * }
                 */

                string detail = "";

                if (!ban.IsTemporary)
                {
                    detail = "INDEFINITELY!";
                }
                else if (ban.BannedUntil != null)
                {
                    //aaa var banStatus = await _banServices.CheckBanStatusAsync(userId);
                    /* if (ban.BannedUntill < UTCNow.ToUniversalTime)
                     * {
                     *     // Marks the BanList entry as IsRevoked = true if its duration has already passed
                     *     await _banServices.UpdateBanStatusAsync(banId, userId);
                     * }
                     */

                    // YYYY.MM.DD HH:MM:SS => 19 chars total. In C# it is .Substribg(startingIndex, length)
                    string endDate = ban.BannedUntil.Value.ToString().Trim().Substring(0, 19);
                    detail = $"until {endDate} UTC";
                }
                else
                {
                    DateTimeOffset bannedAt = ban.BannedAt;
                    TimeSpan banDuration = (ban.Duration != null) ? ban.Duration.Value : TimeSpan.FromDays(36500);
                    DateTimeOffset endDate = bannedAt.Add(banDuration);
                    detail = endDate.ToString().Trim().Substring(0, 19);
                }

                return new LoginResponseDto
                {
                    Success = false,
                    Message = $"You are banned {detail}",
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    ATExpiresAt = DateTimeOffset.MinValue,
                    RTExpiresAt = DateTimeOffset.MinValue
                };
            }

            // User is not banned and account is active
            // Not bool but var because VerifyPasswordAsync returns GeneralResponseDto with bool success and string message fields
            var authSuccess = await VerifyPasswordAsync(dto);

            if (!authSuccess.Success)
            {
                return new LoginResponseDto
                {
                    Success = authSuccess.Success,
                    Message = authSuccess.Message,
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    ATExpiresAt = DateTimeOffset.MinValue,
                    RTExpiresAt = DateTimeOffset.MinValue
                };
            }

            /* NOTE: Since we might update the session entity, we will not use AsNoTracking() here!
             * We will have only one active session per user.
             * If there is already an active session for the user, 
             * we will update the session with new access and refresh tokens.
             */
            var existingSession = await _context.Session
                .Where(s => s.UserID == user.UserID)
                .FirstOrDefaultAsync();

            user.Email = await _normalizationHelper.NormalizeLoginInfoAsync(user.Email);

            string accessToken = await _jwtHelper.GenerateAccessTokenAsync(user.Email);
            string refreshToken = await _jwtHelper.GenerateRefreshTokenAsync();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                throw new ArgumentNullException("Access token or refresh token is empty");
            }

            DateTimeOffset accessTokenExpiration = _jwtHelper.GetAccessTokenExpiration();
            DateTimeOffset refreshTokenExpiration = _jwtHelper.GetRefreshTokenExpiration();

            /* If there is currently a session for this user, do not create a new one.
             * Update the existing one with new tokens, expiration dates, and LastLogin info
             * ** LastLogin info is updated by the DB itself
             */
            if (existingSession != null)
            {
                existingSession.AccessToken = accessToken;
                existingSession.AT_ExpiresAt = accessTokenExpiration;
                existingSession.RefreshToken = refreshToken;
                existingSession.RT_ExpiresAt = refreshTokenExpiration;

                await _context.SaveChangesAsync();

                // Finish login process by returning the tokens and expiration dates.
                return new LoginResponseDto
                {
                    Success = true,
                    Message = "Login successful.",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ATExpiresAt = accessTokenExpiration,
                    RTExpiresAt = refreshTokenExpiration
                };
            }

            // When there is no session for the user, create one.
            var session = new Session
            {
                UserID = user.UserID,
                AccessToken = accessToken,
                AT_ExpiresAt = accessTokenExpiration,
                RefreshToken = refreshToken,
                RT_ExpiresAt = refreshTokenExpiration,
                LastLogin = DateTimeOffset.UtcNow.ToUniversalTime(),
                IsRevoked = false
            };

            await _context.Session.AddAsync(session);
            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Success = true,
                Message = "Login successful.",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ATExpiresAt = accessTokenExpiration,
                RTExpiresAt = refreshTokenExpiration
            };
        }

        public async Task<GeneralResponseDto> VerifyPasswordAsync(LoginRequestDto dto)
        {
            dto.LoginInfo = await _normalizationHelper.NormalizeLoginInfoAsync(dto.LoginInfo);
            
            // Get the user info for the user with the matching email or username (or ID for background service uses)
            /* NOTE: Did not use userServices.GetByLoginInfoAsync because it returns UserResponseDto 
             * and this DTO does not include user's PasswordHash
             * Instead, called the entire user info with EF Core context directly
             */
            var user = await _context.User
                .Where(u => u.UserName == dto.LoginInfo
                         || u.Email == dto.LoginInfo
                         || u.UserID == dto.UserID)
                .AsNoTracking() 
                .FirstOrDefaultAsync();
            
            if (user == null)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "User not found!"
                };
            }

            // Hash the password for initial test users that were inserted via SQL
            // They have plain txt passwords in DB, replace it
            var passwordHash = "";
            if (!user.PasswordHash.StartsWith("$2a$")) // $2a$ is the standart first 4 characters for BCrypt hashes
            {
                passwordHash = await _userServices.HashPasswordAsync(user.UserID);
            }
            else
            {
                passwordHash = user.PasswordHash;
            }

            // Check if the passwords match
            bool passwordsMatch = BCrypt.Net.BCrypt.Verify(dto.Password, passwordHash);

            if (!passwordsMatch)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "Invalid password!"
                };
            }

            return new GeneralResponseDto
            {
                Success = true,
                Message = "Password accepted."
            };
        }

        public async Task<GeneralResponseDto> UpdatePasswordAsync(PasswordUpdateRequestDto dto)
        {
            dto.Email = await _normalizationHelper.NormalizeLoginInfoAsync(dto.Email);

            var user = await _context.User
                .Where(u => u.UserID == dto.UserID
                         && u.Email == dto.Email)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "User not found!"
                };
            }

            var verificationDto = new LoginRequestDto
            {
                UserID = dto.UserID,
                LoginInfo = dto.Email,
                Password = dto.OldPassword
            };

            // Verify the old password. Also verifies the user-password match
            var authSuccess = await VerifyPasswordAsync(verificationDto);

            /* If user is not authenticated return the output of VerifyPasswordAsync directly
             * Since they both return GeneralResponseDto,
             * and VerifyPasswordAsync already returns the appropriate response
             */
            if (!authSuccess.Success)
            {
                return authSuccess;
            }

            // If new password can match with old password's hash, it is the same password. 
            if (dto.OldPassword == dto.NewPassword)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "New password can not be the same as old password."
                };
            }

            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordHash = newPasswordHash;
            user.LastUpdatedAt = DateTimeOffset.UtcNow.ToUniversalTime();

            await _context.SaveChangesAsync();

            var verifySuccess = BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash);

            if (!verifySuccess)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "Something went wrong while updating the password!"
                };
            }

            return new GeneralResponseDto
            {
                Success = true,
                Message = "Password updated successfuly."
            };
        }
    }
}