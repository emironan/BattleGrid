using System;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using BattleGrid.Domain.Entities;
using BattleGrid.Application.Interfaces;
using BattleGrid.Contracts.RequestDtos;
using BattleGrid.Infrastructure.Data;
using BattleGrid.Contracts.ResponseDtos;

namespace BattleGrid.Application.Services
{

    public class AuthServices : IAuthServices
    {
        private readonly BattleGridDbContext _context;

        public AuthServices(BattleGridDbContext context)
        {
            _context = context;
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
                        Message = "This Username is taken."
                    };
                }
            }

            /* If the UserName field is empty, generate unique UserName "U" followed by a unique number
            if (string.IsNullOrEmpty(dto.UserName)
               { 
                    var userName = await UserNameHelper.GenerateUserNameAsync(_uniqueNumberChecker);
                    if (string.IsNullOrEmpty(userName))
                        {
                            return false;
                        }
               } */

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
                Message = "Kullanıcı başarıyla kaydedildi."
            };
        }
    }
}
