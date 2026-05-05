using BattleGrid.Application.Interfaces;
using Microsoft.Extensions.Configuration;
//using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BattleGrid.Application.Helpers
{
    public class NormalizationHelper : INormalizationHelper
    {

        public NormalizationHelper()
        {
        }

        // Take loginInfo (can be UserName or Email) and normalize it to use in login and registration services.
        public async Task<string> NormalizeLoginInfoAsync(string loginInfo)
        {
            // first, trim the login info to remove leading and trailing whitespace
            var normalizedLoginInfo = loginInfo.Trim();

            // If it is an email (user names can not contain "@" and ".") make it lowercase.
            if (normalizedLoginInfo.Contains("@") && normalizedLoginInfo.Contains("."))
            {
                normalizedLoginInfo = normalizedLoginInfo.ToLowerInvariant();
            }
            return normalizedLoginInfo;
        }
    }
}