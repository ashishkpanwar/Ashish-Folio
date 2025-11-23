using System.ComponentModel.DataAnnotations;

namespace Ashish_Backend_Folio.Data.Models
{
    // Models/RefreshToken.cs
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Token { get; set; } = null!; // opaque value stored hashed or plain (see note)

        public DateTime Expires { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public bool IsRevoked { get; set; } = false;

        // optional: allow rotation history
        public string? ReplacedByToken { get; set; }

        // relation to Identity user
        public string UserId { get; set; } = null!;
        public ApplicationUser? User { get; set; }
    }

}
