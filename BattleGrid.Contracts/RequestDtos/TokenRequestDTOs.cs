using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace BattleGrid.Contracts.RequestDtos
{
    // This will be used for auto-login
    /* We check browser localStorage for any user related info,
     *  then we check current cookies that contain tokens
     * Take any token we can find and put it in this DTO
     * Services using this DTO will find the matching user from tokens
     *  and will decide to create a new TokenResponse or not 
     *  based on the user and the info at hand
     */
    public class AutoLoginRequestDto
    {
        // We will take the SessionID from the browser's localStorage
        [JsonIgnore]
        [Required]
        public int SessionID { get; set; }

        // We will also look for UserID in localStorage to simplify the process if possible
        [JsonIgnore]
        [Required]
        public int? UserID { get; set; }

        // We will look for both tokens in cookies. Take whatever we can find,
        //  and work our way from there to determine who this user is
        [MaxLength(500)]
        public string? AccessToken { get; set; } = string.Empty;

        [Base64String]
        [MaxLength(255)]
        public string? RefreshToken { get; set; } = string.Empty;

        public DateTimeOffset? ATExpiresAt { get; set; }

        public DateTimeOffset? RTExpiresAt { get; set; }
    }
}