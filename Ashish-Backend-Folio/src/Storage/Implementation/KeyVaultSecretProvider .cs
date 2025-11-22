using Ashish_Backend_Folio.Storage.Interface;
using Azure.Security.KeyVault.Secrets;

namespace Ashish_Backend_Folio.Storage.Implementation
{
    public class KeyVaultSecretProvider : ISecretProvider
    {
        private readonly SecretClient _client;

        public KeyVaultSecretProvider(SecretClient client)
        {
            _client = client;
        }

        public async Task<string> GetSecretAsync(string name)
        {
            var secret = await _client.GetSecretAsync(name);
            return secret.Value.Value;
        }
    }
}
