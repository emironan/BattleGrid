using BattleGrid.Application.Helpers;
using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Contracts.ResponseDtos;
using BattleGrid.Domain.Entities;
using BattleGrid.Infrastructure.Data;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Security.Claims;


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
            var exists = await _context.User.AnyAsync(x => x.Email == dto.Email);

            if (exists)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "This Email is already registered."
                };
            }

            // Check if the provided UserName is not null and if it already exists
            if (dto.UserName != null)
            {
                var userNameExists = await _context.User.AnyAsync(x => x.UserName == dto.UserName);
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
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            };

            await _context.User.AddAsync(newUser);
            await _context.SaveChangesAsync();
            //_logger.LogInformation("User registered successfully with email: {Email}", dto.Email);

            return new GeneralResponseDto
            {
                Success = true,
                Message = "User registered succesfully."
            };
        }

        public async Task<GeneralResponseDto> VerifyPassword(LoginRequestDto dto)
        {
            // Get the user info for the user with the matching email or username
            /* Did not use userServices.GetByLoginInfoAsync 
             * because it returns UserResponseDto 
             * and this dto does not include user's PasswordHash
             */
            var user = await _context.User
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == dto.LoginInfo 
                                       || u.Email == dto.LoginInfo);
            
            if (user == null)
            {
                return new GeneralResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            // Hash the password for initial test users that were inserted via SQL
            // They have plain txt passwords in DB, replace it
            var passwordHash = "";
            if (!user.PasswordHash.StartsWith("$2a$"))
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
                Message = "Password accepted"
            };
        }

        // Return type will be replaced with TokenResponseDto, once we set the services for access and refresh tokens
        public async Task<TokenResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var loginSuccess = await VerifyPassword(dto);

            if(!loginSuccess.Success)
            {
                throw new InvalidOperationException("Invalid username/email or password!");
            }

            var user = await _userServices.GetByLoginInfoAsync(dto.LoginInfo);
            
            // Redundant check, since, VerifyPassword service checks for an existing user
            if (user == null)
            {
                throw new InvalidOperationException("User does not exist");
            }

            string accessToken = await _jwtHelper.GenerateAccessToken(user.Email);
            string refreshToken = _jwtHelper.GenerateRefreshToken();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                throw new ArgumentNullException("Access token or refresh token is empty");
            }

            DateTimeOffset accessTokenExpiration = _jwtHelper.GetAccessTokenExpiration();
            DateTimeOffset refreshTokenExpiration = _jwtHelper.GetRefreshTokenExpiration();

            //aaa TODO: Add a check to see if there already is a session for the user. 
            /* If there is currently a session for this user, do not create a new one.
             * Update the existing one with new tokens, expiration dates, and LastLogin info
             */
            var session = new Session
            {
                UserID = user.UserID,
                RefreshToken = refreshToken,
                RT_ExpiresAt = refreshTokenExpiration,
                AccessToken = accessToken,
                AT_ExpiresAt = accessTokenExpiration,
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
    }
}