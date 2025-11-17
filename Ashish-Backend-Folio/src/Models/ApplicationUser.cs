using Microsoft.AspNetCore.Identity;

namespace Ashish_Backend_Folio.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; }
    }
}
