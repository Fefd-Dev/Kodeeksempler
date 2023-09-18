using System.ComponentModel.DataAnnotations;

namespace OperationCHAN.Web.Api.Endpoints;

public class AuthModels
{
    public class AuthRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
    
    public class AuthResponse
    {
        public string Id { get; set; } = null!;
        public string Nickname { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string DiscordTag { get; set; } = null!;
    }
    
    
    
    public class RegistrationRequest
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Nickname { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
        public string DiscordTag { get; set; } = null!;
    }
    
    public class RegistrationResponse
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Nickname { get; set; } = null!;
        public string DiscordTag { get; set; } = null!;
    }
    
    public class DiscordRegistrationRequest
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Nickname { get; set; } = null!;
        [Required]
        public string DiscordTag { get; set; } = null!;

        public string DiscordId { get; set; } = String.Empty;
    }
    
    public class DiscordAuthRequest
    {
        public string DiscordId { get; set; } = null!;
    }
}