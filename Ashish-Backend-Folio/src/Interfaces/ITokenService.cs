using Ashish_Backend_Folio.Models;

namespace Ashish_Backend_Folio.Interfaces
{
    public interface ITokenService
    {
         Task<string> CreateTokenAsync(ApplicationUser user, IList<string> roles);
    }
}
