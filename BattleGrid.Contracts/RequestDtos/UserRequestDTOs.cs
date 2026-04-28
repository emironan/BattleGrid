using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BattleGrid.Contracts.RequestDtos
{
    // Used for updating user's ban status etc by the system services.
    public class UserUpdateSystemRequestDto
    {
        [Required]
        public int UserID { get; set; }

        // Can be used for removing/updating inappropriate usernames
        public string? UserName { get; set; }

        // Only make it true when we are going to lift someones ban (like ban's duration has passed)
        public bool BanLifted { get; set; } = false;        
    }

    // Used by users to update their UserName, Email
    public class UserUpdateUserRequestDto
    {
        [Required]
        [JsonIgnore] // We will get it from browser (AccessToken via cookie or UserID from localStorage)
        public int UserID { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUniversalTime();
    }

    public class UserUpdateAdminRequestDto
    {
        [Required]
        [JsonIgnore]
        public int AdminID { get; set; }

        [Required]
        public int UserID { get; set; }

        public string? UserName { get; set; }

        public string? BanStatus { get; set; }

        public string? Reason { get; set; }

        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow.ToUniversalTime();
    }
}