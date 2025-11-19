using System.Security.Cryptography;

namespace Ashish_Backend_Folio.Helper
{
    public static class TokenGenerator
    {
        public static string CreateSecureToken(int size = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(size);
            return Convert.ToBase64String(bytes);
        }
    }
}
