using Ashish_Backend_Folio.Data.Models;

namespace Ashish_Backend_Folio.Interfaces
{
    public interface ITokenService
    {
         Task<string> CreateAccessTokenAsync(ApplicationUser user, IList<string> roles);
    }
}
