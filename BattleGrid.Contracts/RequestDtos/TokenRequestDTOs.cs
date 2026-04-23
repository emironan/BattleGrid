using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace BattleGrid.Contracts.RequestDtos
{
    internal class RefreshTokenRequestDto
    {
        [JsonIgnore]
        [Required]
        public int SessionID { get; set; }

        [StringLength(500)]
        public string? AccessToken { get; set; } = string.Empty;

        [StringLength(255)]
        public string? RefreshToken { get; set; } = string.Empty;

        public DateTimeOffset? ATExpiresAt { get; set; }

        public DateTimeOffset? RTExpiresAt { get; set; }
    }
}