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
        private readonly JwtHelper _jwtHelper;

        public AuthServices(BattleGridDbContext context, 
                            IUserServices userServices,
                            JwtHelper jwtHelper)
        {
            _context = context;
            _userServices = userServices;
            _jwtHelper = jwtHelper;
        }

        public async Task<GeneralResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            // Check if a user with the same email already exists
            var properEmail = dto.Email.Trim().ToLowerInvariant();

            var exists = await _context.User
                .AsNoTracking()
                .AnyAsync(x => x.Email == properEmail);

            if (exists)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "This email is already registered."
                };
            }

            // Check if the provided UserName is not null and if it already exists
            if (dto.UserName != null)
            {
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
                UserName = dto.UserName,
                Email = properEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            };

            await _context.User.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return new GeneralResponseDto
            {
                Success = true,
                Message = "User registered succesfully."
            };
        }

        public async Task<GeneralResponseDto> VerifyPasswordAsync(LoginRequestDto dto)
        {
            // Get the user info for the user with the matching email or username (or ID for internal service uses)
            /* Did not use userServices.GetByLoginInfoAsync 
             * because it returns UserResponseDto 
             * and this dto does not include user's PasswordHash
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

        // Return type will be replaced with TokenResponseDto, once we set the services for access and refresh tokens
        public async Task<TokenResponseDto> LoginAsync(LoginRequestDto dto)
        {
            // Not bool but var because VerifyPasswordAsync returns GeneralResponseDto with bool success and string message fields
            var authSuccess = await VerifyPasswordAsync(dto); 

            if(!authSuccess.Success)
            {
                throw new InvalidOperationException("Invalid username/email or password!");
            }

            var user = await _userServices.GetByLoginInfoAsync(dto.LoginInfo);
            
            // Redundant check, since, VerifyPasswordAsync service checks for an existing user
            if (user == null)
            {
                throw new InvalidOperationException("User does not exist");
            }

            // Banned users can not even login!
            if (user.IsBanned)
            {
                var ban = await _context.BanList
                    .Where(b => b.PlayerID == user.UserID)
                    .OrderByDescending(b => b.BanID)
                    .FirstOrDefaultAsync();

                if (ban == null || ban.IsReverted)
                {
                    throw new ArgumentNullException("User table shows user as banned. " +
                        "However, the possibilities are as follows: 1) Ban's duration has ended, 2)Ban is reverted, 3)There is no ban records for this player");
                }

                //aaa TODO if a ban is reverted or there is no ban record but the user table still shows as banned there is a problem!
                /* For now, above if-check throws an exception for us to check the situation
                 * We need a BanListServices to implement a service that synchronizes ban status among User and BanList tables
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
                     *     // Marks the BanList entry as IsRevoked = true if  
                     *     await _banServices.SyncBanStatusAsync(banId, userId);
                     * }
                     */
                    string endDate = ban.BannedUntil.Value.ToString().Trim().Substring(0,19); // YYYY.MM.DD HH:MM:SS => 19 chars total
                    detail = $"until {endDate} UTC";
                }
                else
                {
                    DateTimeOffset bannedAt = ban.BannedAt;
                    TimeSpan banDuration = ban.Duration.Value;
                    DateTimeOffset endDate = bannedAt.Add(banDuration);
                    detail = endDate.ToString().Trim().Substring(0, 19);
                }

                throw new UnauthorizedAccessException($"You are banned {detail}");
            }

            var existingSession = await _context.Session
                .Where(s => s.UserID == user.UserID)
                .FirstOrDefaultAsync();

            string accessToken = await _jwtHelper.GenerateAccessToken(user.Email);
            string refreshToken = await _jwtHelper.GenerateRefreshToken();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                throw new ArgumentNullException("Access token or refresh token is empty");
            }

            DateTimeOffset accessTokenExpiration = _jwtHelper.GetAccessTokenExpiration();
            DateTimeOffset refreshTokenExpiration = _jwtHelper.GetRefreshTokenExpiration();

            /* If there is currently a session for this user, do not create a new one.
             * Update the existing one with new tokens, expiration dates, and LastLogin info
             */
            if (existingSession != null)
            {
                existingSession.AccessToken = accessToken;
                existingSession.AT_ExpiresAt = accessTokenExpiration;
                existingSession.RefreshToken = refreshToken;
                existingSession.RT_ExpiresAt = refreshTokenExpiration;

                await _context.SaveChangesAsync();

                return new TokenResponseDto
                {
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

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ATExpiresAt = accessTokenExpiration,
                RTExpiresAt = refreshTokenExpiration
            };
        }

        public async Task<GeneralResponseDto> UpdatePasswordAsync(PasswordUpdateRequestDto dto)
        {
            var properEmail = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.User
                .Where(u => u.UserID == dto.UserID
                         && u.Email == properEmail)
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
                LoginInfo = user.Email,
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