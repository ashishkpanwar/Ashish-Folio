using Ashish_Backend_Folio.Storage.Interface;
using Azure.Security.KeyVault.Secrets;

namespace Ashish_Backend_Folio.Storage.Implementation
{
    public class KeyVaultSecretProvider : ISecretProvider
    {
        private readonly SecretClient _client;
        private readonly ILogger<SecretClient> _logger;


        public KeyVaultSecretProvider(SecretClient client, ILogger<SecretClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<string> GetSecretAsync(string name)
        {
            try
            {
                var secret = await _client.GetSecretAsync(name);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError($"not able to get secret from key vault, exception - {ex.Message}");
                throw;
            }
            
        }
    }
}
