using Ashish_Backend_Folio.Models;

namespace Ashish_Backend_Folio.Interfaces
{
    public interface ITokenService
    {
         Task<string> CreateAccessTokenAsync(ApplicationUser user, IList<string> roles);
    }
}
